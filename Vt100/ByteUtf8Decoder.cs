//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Globalization;
using System.Text;

namespace TermSharp.Vt100
{
    public sealed class ByteUtf8Decoder
    {
        public ByteUtf8Decoder(Action<string> charDecodedCallback)
        {
            this.charDecodedCallback = charDecodedCallback;
            utfBytes = new byte[4];
            utf8Decoder = Encoding.UTF8.GetDecoder();
            result = new char[4];
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
                var utf16CharCount = utf8Decoder.GetChars(utfBytes, 0, currentCount, result, 0, true);
                var resultAsString = new string(result, 0, utf16CharCount);
                var textElements = StringInfo.GetTextElementEnumerator(resultAsString);
                while(textElements.MoveNext())
                {
                    charDecodedCallback((string)textElements.Current);
                }
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
        private readonly Action<string> charDecodedCallback;
        private readonly char[] result;
        private readonly System.Text.Decoder utf8Decoder;
    }
}

