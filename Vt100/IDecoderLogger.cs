//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//

namespace TermSharp.Vt100
{
    public interface IDecoderLogger
    {
        void Log(string format, params object[] args);
    }
}

