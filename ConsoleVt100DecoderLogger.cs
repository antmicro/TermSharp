//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
namespace Terminal
{
    public class ConsoleVt100DecoderLogger : IVt100DecoderLogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}

