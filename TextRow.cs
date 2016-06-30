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
            MaxOffset = (int)(lineSize.Width / charWidth);
            return lineSize.Height * Math.Ceiling((content.Length == 0 ? 1 : content.Length) * charWidth / lineSize.Width);
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

        public void DrawCursor(Context ctx, int offset, bool focused)
        {
            var column = offset % MaxOffset;
            var row = offset / MaxOffset;
            ctx.SetColor(Colors.White);
            ctx.Rectangle(new Rectangle(column * charWidth, row * lineSize.Height, charWidth, lineSize.Height));
            if(focused)
            {
                ctx.Fill();
            }
            else
            {
                ctx.Stroke();
            }
        }

        public void FillClipboardData(ClipboardData data)
        {
            if(selectedContent != null)
            {
                data.AppendText(selectedContent);
            }
        }

        public void Erase(int from, int to)
        {
            var builder = new StringBuilder(content);
            to = Math.Min(to, content.Length - 1);
            for(var i = from; i <= to; i++)
            {
                builder[i] = ' ';
            }
            // TODO: maybe trim trails
            content = builder.ToString();
        }

        public void InsertCharacterAt(int x, char what)
        {
            var builder = new StringBuilder(content, x);
            for(var i = builder.Length; i <= x; i++)
            {
                builder.Append(' ');
            }
            builder[x] = what;
            content = builder.ToString();
        }

        public string Content
        {
            get
            {
                return content;
            }
            set
            {
                content = value;
            }
        }

        public int MaxOffset { get; set; }

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
        private string content;

        private static readonly SimpleCache<ILayoutParameters, TextLayout> TextLayoutCache = new SimpleCache<ILayoutParameters, TextLayout>(Utilities.GetTextLayoutFromLayoutParams);
        private static readonly SimpleCache<ILayoutParameters, Size> LineSizeCache = new SimpleCache<ILayoutParameters, Size>(GetLineSizeFromLayoutParams);
        private static readonly SimpleCache<ILayoutParameters, Size> CharSizeCache = new SimpleCache<ILayoutParameters, Size>(Utilities.GetCharSizeFromLayoutParams);
    }
}

