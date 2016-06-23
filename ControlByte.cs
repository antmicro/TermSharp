//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
namespace Terminal
{
    public enum ControlByte : byte
    {
        LineFeed = 0x0A,
        Escape = 0x1B,
        Csi = (byte)'['
    }
}

