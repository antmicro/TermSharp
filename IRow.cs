//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using Xwt;
using Xwt.Drawing;

namespace Terminal
{
    public interface IRow
    {
        double PrepareForDrawing(ILayoutParameters parameters);
        void Draw(Context ctx, Rectangle selectedArea, SelectionDirection selectionDirection);
        void FillClipboardData(ClipboardData data);
    }
}

