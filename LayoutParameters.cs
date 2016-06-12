//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using Xwt.Drawing;

namespace Terminal
{
    internal class LayoutParameters : ILayoutParameters
    {
        public LayoutParameters(Font font)
        {
            Font = font;
        }

        public Font Font { get; set; }
        public double Width { get; set; }
    }
}

