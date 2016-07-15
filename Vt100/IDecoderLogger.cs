//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;

namespace Terminal.Vt100
{
    public interface IDecoderLogger
    {
        void Log(string message);
    }
}

