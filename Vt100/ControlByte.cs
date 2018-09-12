//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//

namespace TermSharp.Vt100
{
    public enum ControlByte : byte
    {
        Bell = 0x7,
        Backspace = 0x8,
        LineFeed = 0x0A,
        HorizontalTab = 0x09,
        CarriageReturn = 0xD,
        LockShiftG1 = 0xE,
        LockShiftG0 = 0xF,
        Escape = 0x1B,
        ControlSequenceIntroducer = (byte)'[',
        Delete = 0x7F
    }
}

