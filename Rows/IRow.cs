//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using TermSharp.Misc;
using Xwt;
using Xwt.Drawing;

namespace TermSharp.Rows
{
    public interface IRow
    {
        double PrepareForDrawing(ILayoutParameters parameters);
        void Draw(Context ctx, Rectangle selectedArea, SelectionDirection selectionDirection, SelectionMode selectionMode);
        void ResetSelection();
        void DrawCursor(Context ctx, int offset, bool focused);
        void FillClipboardData(ClipboardData data);
        void Erase(int from, int to, Color? background);
        int CurrentMaximalCursorPosition { get; }
    }
}

