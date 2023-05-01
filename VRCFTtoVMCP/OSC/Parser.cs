﻿using System.Collections.Generic;
using System.Diagnostics;
using VRCFTtoVMCP.Osc;

namespace VRCFTtoVMCP.OSC
{
    internal class Parser
    {
        public static readonly object[] EmptyObjectArray = new object[0];

        object lockObject_ = new object();
        Queue<Message> messages_ = new Queue<Message>();

        public int messageCount
        {
            get { return messages_.Count; }
        }

        public void Parse(byte[] buf, ref int pos, int endPos, ulong timestamp = 0x1u)
        {
            var first = Reader.ParseString(buf, ref pos);

            if (first == Identifier.Bundle)
            {
                ParseBundle(buf, ref pos, endPos);
            }
            else
            {
                var values = ParseData(buf, ref pos);
                lock (lockObject_)
                {
                    messages_.Enqueue(new Message(first, values)
                    {
                        timestamp = new Timestamp(timestamp),
                    });
                }
            }

            if (pos != endPos)
            {
                // $"The parsed data size is inconsitent with the given size: {pos} / {endPos}"
            }
        }

        public Message Dequeue()
        {
            if (messageCount == 0)
            {
                return Message.none;
            }

            lock (lockObject_)
            {
                return messages_.Dequeue();
            }
        }

        void ParseBundle(byte[] buf, ref int pos, int endPos)
        {
            var time = Reader.ParseTimetag(buf, ref pos);

            while (pos < endPos)
            {
                var contentSize = Reader.ParseInt(buf, ref pos);
                if (Util.IsMultipleOfFour(contentSize))
                {
                    Parse(buf, ref pos, pos + contentSize, time);
                }
                else
                {
                    // $"Given data is invalid (bundle size ({contentSize}) is not a multiple of 4)."
                    pos += contentSize;
                }
            }
        }

        object[] ParseData(byte[] buf, ref int pos)
        {
            // remove ','
            var types = Reader.ParseString(buf, ref pos).Substring(1);

            var n = types.Length;
            if (n == 0) return EmptyObjectArray;

            var data = new object[n];

            for (int i = 0; i < n; ++i)
            {
                switch (types[i])
                {
                    case Identifier.Int: data[i] = Reader.ParseInt(buf, ref pos); break;
                    case Identifier.Float: data[i] = Reader.ParseFloat(buf, ref pos); break;
                    case Identifier.String: data[i] = Reader.ParseString(buf, ref pos); break;
                    case Identifier.Blob: data[i] = Reader.ParseBlob(buf, ref pos); break;
                    case Identifier.True: data[i] = true; break;
                    case Identifier.False: data[i] = false; break;
                    default:
                        // Add more types here if you want to handle them.
                        break;
                }
            }

            return data;
        }
    }
}
