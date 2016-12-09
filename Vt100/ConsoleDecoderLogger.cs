//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;

namespace TermSharp.Vt100
{
    public class ConsoleDecoderLogger : IDecoderLogger
    {
        public void Log(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}

