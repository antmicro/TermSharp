//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using TermSharp.Misc;
using Xwt.Drawing;

namespace TermSharp
{
    public interface ILayoutParameters : IGenerationAware
    {
        Font Font { get; }
        double Width { get; }
        Color DefaultForeground { get; }
        Color SelectionColor { get; }
    }
}

