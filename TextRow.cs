//
// Copyright (c) Antmicro
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;
using System.Text;
using Xwt;
using Xwt.Drawing;

namespace Terminal
{
    public class TextRow : IRow
    {
        public TextRow(string content)
        {
#if DEBUG
            if(content.Contains("\n"))
            {
                throw new ArgumentException("Content cannot contain a newline character.");
            }
#endif
            this.content = content;
        }

        public double PrepareForDrawing(ILayoutParameters parameters)
        {
            textLayout = TextLayoutCache.GetValue(parameters);
            lineSize = LineSizeCache.GetValue(parameters);
            charWidth = CharSizeCache.GetValue(parameters).Width;
            return lineSize.Height * Math.Ceiling(content.Length * charWidth / lineSize.Width);
        }

        public void Draw(Context ctx, Rectangle selectedArea, SelectionDirection selectionDirection)
        {
            ctx.SetColor(Colors.White);
            textLayout.Text = content;
            var newLinesAt = new List<int> { 0 };
            if(textLayout.Text.Length > 1)
            {
                var charsOnLine = (int)Math.Floor(lineSize.Width / charWidth);
                var result = new StringBuilder();
                var counter = 0;
                result.Append(textLayout.Text.Substring(result.Length, Math.Min(charsOnLine, textLayout.Text.Length - result.Length)));
                while((result.Length - counter) < textLayout.Text.Length)
                {
                    result.Append('\n');
                    newLinesAt.Add(result.Length);
                    counter++;
                    result.Append(textLayout.Text.Substring(result.Length - counter, Math.Min(charsOnLine, textLayout.Text.Length - (result.Length - counter))));
                }
                textLayout.Text = result.ToString();
            }

            if(selectedArea != default(Rectangle))
            {
                var textWithNewLines = textLayout.Text;

                var startRow = (int)Math.Min(newLinesAt.Count - 1, Math.Floor(selectedArea.Y / lineSize.Height));
                var endRow = (int)Math.Min(newLinesAt.Count - 1, Math.Floor((selectedArea.Y + selectedArea.Height) / lineSize.Height));
                var startColumn = (int)Math.Round(selectedArea.X / charWidth);
                var endColumn = (int)Math.Round((selectedArea.X + selectedArea.Width) / charWidth);

                if(selectionDirection == SelectionDirection.NW)
                {
                    Utilities.Swap(ref startColumn, ref endColumn);
                    Utilities.Swap(ref startRow, ref endRow);
                }

                var startIndex = startColumn + newLinesAt[startRow];
                var endIndex = endColumn + newLinesAt[endRow];

                if(endIndex < startIndex)
                {
                    Utilities.Swap(ref startIndex, ref endIndex);
                }

                startIndex = Math.Max(0, Math.Min(textWithNewLines.Length, startIndex));
                endIndex = Math.Max(0, Math.Min(textWithNewLines.Length, endIndex));

                textLayout.SetBackground(Colors.White, startIndex, endIndex - startIndex);
                textLayout.SetForeground(Colors.Black, startIndex, endIndex - startIndex);
                selectedContent = textWithNewLines.Substring(startIndex, endIndex - startIndex);
            }
            else
            {
                selectedContent = null;
            }

            ctx.DrawTextLayout(textLayout, 0, 0);
            textLayout.ClearAttributes();
        }

        public void FillClipboardData(ClipboardData data)
        {
            if(selectedContent != null)
            {
                data.AppendText(selectedContent);
            }
        }

        private static Size GetLineSizeFromLayoutParams(ILayoutParameters parameters)
        {
            var textLayout = Utilities.GetTextLayoutFromLayoutParams(parameters);
            textLayout.Text = "a\na";
            return new Size(parameters.Width, textLayout.GetCoordinateFromIndex(2).Y);
        }

        private double charWidth;
        private Size lineSize;
        private TextLayout textLayout;
        private string selectedContent;
        private readonly string content;

        private static readonly SimpleCache<ILayoutParameters, TextLayout> TextLayoutCache = new SimpleCache<ILayoutParameters, TextLayout>(Utilities.GetTextLayoutFromLayoutParams);
        private static readonly SimpleCache<ILayoutParameters, Size> LineSizeCache = new SimpleCache<ILayoutParameters, Size>(GetLineSizeFromLayoutParams);
        private static readonly SimpleCache<ILayoutParameters, Size> CharSizeCache = new SimpleCache<ILayoutParameters, Size>(Utilities.GetCharSizeFromLayoutParams);
    }
}

