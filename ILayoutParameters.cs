//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using Terminal.Misc;
using Xwt.Drawing;

namespace Terminal
{
    public interface ILayoutParameters : IGenerationAware
    {
        Font Font { get; }
        double Width { get; }
        Color DefaultForeground { get; }
        Color SelectionColor { get; }
    }
}

