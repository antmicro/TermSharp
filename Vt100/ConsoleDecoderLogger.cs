//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;

namespace Terminal.Vt100
{
    public class ConsoleDecoderLogger : IDecoderLogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}

