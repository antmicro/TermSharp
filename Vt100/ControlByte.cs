//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;

namespace Terminal.Vt100
{
    public enum ControlByte : byte
    {
        Bell = 0x7,
        Backspace = 0x8,
        LineFeed = 0x0A,
        HorizontalTab = 0x09,
        CarriageReturn = 0xD,
        Escape = 0x1B,
        Csi = (byte)'['
    }
}

