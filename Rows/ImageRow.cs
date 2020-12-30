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
    public class ImageRow : IRow
    {
        public ImageRow(Image image, int? preferedHeightInLines = null)
        {
            this.image = image;
            this.preferedHeightInLines = preferedHeightInLines;
        }

        public double PrepareForDrawing(ILayoutParameters parameters)
        {
            var lineSize = RowUtils.LineSizeCache.GetValue(parameters);
            var charWidth = RowUtils.CharSizeCache.GetValue(parameters).Width;
            // Math.Max used to prevent division by zero errors on windows narrower than one character
            MaximalColumn = Math.Max(((int)(lineSize.Width / charWidth)) - 1, 0);
            var preferredHeight = preferedHeightInLines.HasValue
                ? lineSize.Height * preferedHeightInLines.Value
                : image.Height;

            SublineCount = Math.Max(1, (int)(preferredHeight / lineSize.Height));
            LineHeight = lineSize.Height;

            return LineHeight * SublineCount;
        }

        public void Draw(Context ctx, Rectangle selectedArea, SelectionDirection selectionDirection, SelectionMode selectionMode)
        {
            var scale = (LineHeight * SublineCount) / image.Height;
            ctx.Scale(scale, scale);
            ctx.DrawImage(image, 0, 0);
        }

        public void ResetSelection()
        {
            // Intentionally left blank
        }

        public void DrawCursor(Context ctx, int offset, bool focused)
        {
            // Intentionally left blank
        }

        public void FillClipboardData(ClipboardData data)
        {
            // Intentionally left blank
        }

        public void Erase(int from, int to, Color? background)
        {
            // Intentionally left blank
        }

        public override string ToString()
        {
            return $"[ImageRow:: SublineCount={SublineCount}, Height={(LineHeight * SublineCount)}]";
        }

        public int CurrentMaximalCursorPosition => MaximalColumn;
        public int SublineCount { get; private set; }
        public int MaximalColumn { get; private set; }
        public double LineHeight { get; private set; }

        private readonly Image image;
        private readonly int? preferedHeightInLines;
    }
}
