//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;
using System.Text;

namespace Terminal
{
    public sealed class ByteUtf8Decoder
    {
        public ByteUtf8Decoder(Action<char> charDecodedCallback)
        {
            this.charDecodedCallback = charDecodedCallback;
            utfBytes = new byte[4];
            utf8Decoder = Encoding.UTF8.GetDecoder();
            result = new char[1];
        }

        public void Feed(byte b)
        {
            if(currentCount == 0)
            {
                currentCount = GetUtf8ByteCount(b);
            }
            utfBytes[currentIndex++] = b;
            if(currentIndex == currentCount)
            {
                utf8Decoder.GetChars(utfBytes, 0, currentCount, result, 0);
                charDecodedCallback(result[0]);
                currentCount = 0;
                currentIndex = 0;
            }
        }

        private static int GetUtf8ByteCount(byte leadingByte)
        {
            if(leadingByte < 128)
            {
                return 1;
            }
            if(((leadingByte ^ 0xC0) >> 5) == 0)
            {
                return 2;
            }
            if(((leadingByte ^ 0xE0) >> 4) == 0)
            {
                return 3;
            }
            return 4;
        }

        private int currentCount;
        private int currentIndex;
        private readonly byte[] utfBytes;
        private readonly Action<char> charDecodedCallback;
        private readonly char[] result;
        private readonly Decoder utf8Decoder;
    }
}

