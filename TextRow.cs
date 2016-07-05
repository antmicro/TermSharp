//
// Copyright (c) Antmicro
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;
using System.Linq;
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
            defaultForeground = parameters.DefaultForeground;
            textLayout = TextLayoutCache.GetValue(parameters);
            lineSize = LineSizeCache.GetValue(parameters);
            charWidth = CharSizeCache.GetValue(parameters).Width;
            MaxOffset = (int)(lineSize.Width / charWidth);
            return lineSize.Height * Math.Ceiling((content.Length == 0 ? 1 : content.Length) * charWidth / lineSize.Width);
        }

        public void Draw(Context ctx, Rectangle selectedArea, SelectionDirection selectionDirection)
        {
            ctx.SetColor(defaultForeground);
            textLayout.Text = content;
            var newLinesAt = new List<int> { 0 };
            var charsOnLine = (int)Math.Floor(lineSize.Width / charWidth);
            if(textLayout.Text.Length > 1)
            {
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

            var foregroundColors = specialForegrounds != null ? specialForegrounds.ToDictionary(x => x.Key + x.Key / charsOnLine, x => x.Value) : new Dictionary<int, Color>();
            var backgroundColors = specialBackgrounds != null ? specialBackgrounds.ToDictionary(x => x.Key + x.Key / charsOnLine, x => x.Value) : new Dictionary<int, Color>();
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

                for(var i = startIndex; i < endIndex; i++)
                {
                    foregroundColors[i] = (specialForegrounds != null && specialForegrounds.ContainsKey(i)) ? specialForegrounds[i].WithIncreasedLight(0.2) : Colors.Black;
                    backgroundColors[i] = Colors.LightSlateGray;
                }
                selectedContent = textWithNewLines.Substring(startIndex, endIndex - startIndex);
            }
            else
            {
                selectedContent = null;
            }

            foreach(var entry in GetColorRanges(foregroundColors))
            {
                textLayout.SetForeground(entry.Item3, entry.Item1, entry.Item2);
            }
            foreach(var entry in GetColorRanges(backgroundColors))
            {
                textLayout.SetBackground(entry.Item3, entry.Item1, entry.Item2);
            }

            ctx.DrawTextLayout(textLayout, 0, 0);
            textLayout.ClearAttributes();
        }

        public void DrawCursor(Context ctx, int offset, bool focused)
        {
            var column = offset % MaxOffset;
            var row = offset / MaxOffset;
            ctx.SetColor(defaultForeground);
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

        public void Erase(int from, int to, Color? background)
        {
            var builder = new StringBuilder(content);
            to = Math.Min(to, content.Length - 1);
            for(var i = from; i <= to; i++)
            {
                builder[i] = ' ';
                if(background.HasValue)
                {
                    CheckDictionary(ref specialBackgrounds);
                    specialBackgrounds[i] = background.Value;
                }
                else
                {
                    if(specialBackgrounds != null)
                    {
                        specialBackgrounds.Remove(i);
                    }
                }
                if(specialForegrounds != null)
                {
                    specialForegrounds.Remove(i);
                }
            }
            // TODO: maybe trim trails
            content = builder.ToString();
        }

        public void InsertCharacterAt(int x, char what, Color? foreground, Color? background)
        {
            if(foreground.HasValue)
            {
                CheckDictionary(ref specialForegrounds);
                specialForegrounds[x] = foreground.Value;
            }
            else
            {
                if(specialForegrounds != null)
                {
                    specialForegrounds.Remove(x);
                }
            }

            if(background.HasValue)
            {
                CheckDictionary(ref specialBackgrounds);
                specialBackgrounds[x] = background.Value;
            }
            else
            {
                if(specialBackgrounds != null)
                {
                    specialBackgrounds.Remove(x);
                }
            }

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

        private void CheckDictionary(ref Dictionary<int, Color> dictionary)
        {
            if(dictionary == null)
            {
                dictionary = new Dictionary<int, Color>();
            }
        }

        private static Size GetLineSizeFromLayoutParams(ILayoutParameters parameters)
        {
            var textLayout = Utilities.GetTextLayoutFromLayoutParams(parameters);
            textLayout.Text = "a\na";
            return new Size(parameters.Width, textLayout.GetCoordinateFromIndex(2).Y);
        }

        private static IEnumerable<Tuple<int, int, Color>> GetColorRanges(Dictionary<int, Color> entries)
        {
            var colors = entries.Values.Distinct();
            foreach(var color in colors)
            {
                var entriesThisColor = new HashSet<int>(entries.Where(x => x.Value == color).Select(x => x.Key));
                var begins = entriesThisColor.Where(x => !entriesThisColor.Contains(x - 1)).OrderBy(x => x).ToArray();
                var ends = entriesThisColor.Where(x => !entriesThisColor.Contains(x + 1)).OrderBy(x => x).ToArray();
                for(var i = 0; i < begins.Length; i++)
                {
                    yield return Tuple.Create(begins[i], ends[i] - begins[i] + 1, color);
                }
            }
        }

        private double charWidth;
        private Size lineSize;
        private TextLayout textLayout;
        private string selectedContent;
        private string content;
        private Color defaultForeground;
        private Dictionary<int, Color> specialForegrounds;
        private Dictionary<int, Color> specialBackgrounds;

        private static readonly SimpleCache<ILayoutParameters, TextLayout> TextLayoutCache = new SimpleCache<ILayoutParameters, TextLayout>(Utilities.GetTextLayoutFromLayoutParams);
        private static readonly SimpleCache<ILayoutParameters, Size> LineSizeCache = new SimpleCache<ILayoutParameters, Size>(GetLineSizeFromLayoutParams);
        private static readonly SimpleCache<ILayoutParameters, Size> CharSizeCache = new SimpleCache<ILayoutParameters, Size>(Utilities.GetCharSizeFromLayoutParams);
    }
}

