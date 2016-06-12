//
// Copyright (c) Antmicro
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Text;
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
            textLayout = new TextLayout();
        }

        public double PrepareForDrawing(ILayoutParameters parameters)
        {
            textLayout.Font = parameters.Font;

            lineWidth = parameters.Width;
            textLayout.Text = "a\na";
            var heightOfALine = textLayout.GetCoordinateFromIndex(2).Y;
            textLayout.Text = content;
            return heightOfALine * (int)Math.Ceiling(textLayout.GetCoordinateFromIndex(content[content.Length - 1]).X / lineWidth);
        }

        public void Draw(Context ctx)
        {
            if(textLayout.Text.Length > 1)
            {
                var widthOfAChar = textLayout.GetCoordinateFromIndex(1).X - textLayout.GetCoordinateFromIndex(0).X;
                var charsOnLine = (int)Math.Floor(lineWidth / widthOfAChar);
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
            ctx.DrawTextLayout(textLayout, 0, 0);

        }

        private double lineWidth;
        private readonly TextLayout textLayout;
        private readonly string content;
    }
}

