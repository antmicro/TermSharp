//
// Copyright (c) Antmicro
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
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

        public void Draw(Context ctx, Rectangle selectedArea)
        {
            ctx.SetColor(Colors.White);
            textLayout.Text = content;
            if(textLayout.Text.Length > 1)
            {
                var charsOnLine = (int)Math.Floor(lineSize.Width / charWidth);
                var result = new StringBuilder();
                var counter = 0;
                result.Append(textLayout.Text.Substring(result.Length, Math.Min(charsOnLine, textLayout.Text.Length - result.Length)));
                while((result.Length - counter) < textLayout.Text.Length)
                {
                    result.Append('\n');
                    counter++;
                    result.Append(textLayout.Text.Substring(result.Length - counter, Math.Min(charsOnLine, textLayout.Text.Length - (result.Length - counter))));
                }
                textLayout.Text = result.ToString();
            }

            if(selectedArea != default(Rectangle))
            {
                var startingColumn = Math.Round(selectedArea.X / charWidth);
                var endingColumn = Math.Min(content.Length, Math.Round((selectedArea.X + selectedArea.Width) / charWidth));

                var startIndex = (int)startingColumn;
                var endIndex = (int)endingColumn;
                textLayout.SetBackground(Colors.White, startIndex, endIndex - startIndex);
                textLayout.SetForeground(Colors.Black, startIndex, endIndex - startIndex);
                selectedContent = content.Substring(startIndex, endIndex - startIndex);
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

