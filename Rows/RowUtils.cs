//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using TermSharp.Misc;
using Xwt;
using Xwt.Drawing;

namespace TermSharp.Rows
{
    internal class RowUtils
    {
        public static SimpleCache<ILayoutParameters, TextLayout> TextLayoutCache => new SimpleCache<ILayoutParameters, TextLayout>(Utilities.GetTextLayoutFromLayoutParams);
        public static SimpleCache<ILayoutParameters, Size> LineSizeCache => new SimpleCache<ILayoutParameters, Size>(Utilities.GetLineSizeFromLayoutParams);
        public static SimpleCache<ILayoutParameters, Size> CharSizeCache => new SimpleCache<ILayoutParameters, Size>(Utilities.GetCharSizeFromLayoutParams);
    }
}
