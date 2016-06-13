//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
namespace Terminal
{
    public interface IGenerationAware
    {
        int Generation { get; }
    }
}

