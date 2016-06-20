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
            charWidth = CharWidthCache.GetValue(parameters);
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

                var startColumn = (int)Math.Round(selectedArea.X / charWidth);
                var endColumn = (int)Math.Round((selectedArea.X + selectedArea.Width) / charWidth);
                var startRow = (int)Math.Min(newLinesAt.Count - 1, Math.Floor(selectedArea.Y / lineSize.Height));
                var endRow = (int)Math.Min(newLinesAt.Count - 1, Math.Floor((selectedArea.Y + selectedArea.Height) / lineSize.Height));

                if(selectionDirection == SelectionDirection.SW || selectionDirection == SelectionDirection.NW)
                {
                    Utilities.Swap(ref startColumn, ref endColumn);
                }

                if(selectionDirection == SelectionDirection.NE || selectionDirection == SelectionDirection.NW)
                {
                    Utilities.Swap(ref startRow, ref endRow);
                }

                var startIndex = startColumn + newLinesAt[startRow];
                var endIndex = endColumn + newLinesAt[endRow];

                if(endIndex < startIndex)
                {
                    Utilities.Swap(ref startIndex, ref endIndex);
                }
                endIndex = Math.Min(endIndex, textWithNewLines.Length);

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

        private static TextLayout GetTextLayoutFromLayoutParams(ILayoutParameters parameters)
        {
            var result = new TextLayout();
            result.Font = parameters.Font;
            return result;
        }

        private static Size GetLineSizeFromLayoutParams(ILayoutParameters parameters)
        {
            var textLayout = GetTextLayoutFromLayoutParams(parameters);
            textLayout.Text = "a\na";
            return new Size(parameters.Width, textLayout.GetCoordinateFromIndex(2).Y);
        }

        private static double GetCharWidthFromLayoutParams(ILayoutParameters parameters)
        {
            var textLayout = GetTextLayoutFromLayoutParams(parameters);
            textLayout.Text = "a";
            return textLayout.GetCoordinateFromIndex(1).X;
        }

        private double charWidth;
        private Size lineSize;
        private TextLayout textLayout;
        private string selectedContent;
        private readonly string content;

        private static readonly SimpleCache<ILayoutParameters, TextLayout> TextLayoutCache = new SimpleCache<ILayoutParameters, TextLayout>(GetTextLayoutFromLayoutParams);
        private static readonly SimpleCache<ILayoutParameters, Size> LineSizeCache = new SimpleCache<ILayoutParameters, Size>(GetLineSizeFromLayoutParams);
        private static readonly SimpleCache<ILayoutParameters, double> CharWidthCache = new SimpleCache<ILayoutParameters, double>(GetCharWidthFromLayoutParams);
    }
}

