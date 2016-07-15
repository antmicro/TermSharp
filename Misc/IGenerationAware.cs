//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;

namespace Terminal.Misc
{
    public interface IGenerationAware
    {
        int Generation { get; }
    }
}

