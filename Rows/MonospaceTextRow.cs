//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using TermSharp.Misc;
using Xwt;
using Xwt.Drawing;

namespace TermSharp.Rows
{
    public class MonospaceTextRow : IRow
    {
        public MonospaceTextRow(string content)
        {
            Debug.Assert(!content.Contains("\n"));
            this.content = content;
            lengthInTextElements = new StringInfo(content).LengthInTextElements;
        }

        public virtual double PrepareForDrawing(ILayoutParameters parameters)
        {
            cursorInRow = null;
            defaultForeground = parameters.DefaultForeground;
            selectionColor = parameters.SelectionColor;
            textLayout = TextLayoutCache.GetValue(parameters);
            lineSize = LineSizeCache.GetValue(parameters);
            charWidth = CharSizeCache.GetValue(parameters).Width;

            if(lineSize.Width == 0)
            {
                return 0;
            }

            // Math.Max used to prevent division by zero errors on windows narrower than one character
            MaximalColumn = Math.Max(((int)(lineSize.Width / charWidth)) - 1, 0);

            var charsOnLine = MaximalColumn + 1;
            var lengthInTextElementsAtLeastOne = lengthInTextElements == 0 ? 1 : lengthInTextElements; // because even empty line has height of one line
            lineCount = Math.Max(minimalSublineCount, DivisionWithCeiling(lengthInTextElementsAtLeastOne, charsOnLine));
            return lineSize.Height * lineCount;
        }

        public virtual void Draw(Context ctx, Rectangle selectedArea, SelectionDirection selectionDirection, SelectionMode selectionMode)
        {
            ctx.SetColor(defaultForeground);
            var newLinesAt = new List<int> { 0 }; // contains indices of line wraps (i.e. \n)
            var charsOnLine = MaximalColumn + 1;

            var result = new StringBuilder();
            var enumerator = StringInfo.GetTextElementEnumerator(content);
            var textElementsThisLine = 0;
            while(enumerator.MoveNext())
            {
                textElementsThisLine++;
                result.Append(enumerator.GetTextElement());
                if(textElementsThisLine == charsOnLine)
                {
                    result.Append('\n');
                    newLinesAt.Add(enumerator.ElementIndex + newLinesAt.Count + 1);
                    textElementsThisLine = 0;
                }
            }
            textLayout.Text = result.ToString();

            var foregroundColors = specialForegrounds != null ? specialForegrounds.ToDictionary(x => x.Key + x.Key / charsOnLine, x => x.Value) : new Dictionary<int, Color>();
            var backgroundColors = specialBackgrounds != null ? specialBackgrounds.ToDictionary(x => x.Key + x.Key / charsOnLine, x => x.Value) : new Dictionary<int, Color>();
            if(selectedArea != default(Rectangle) && lengthInTextElements > 0)
            {
                var textWithNewLines = textLayout.Text;

                var firstSubrow = (int)Math.Min(newLinesAt.Count - 1, Math.Floor(selectedArea.Y / lineSize.Height));
                var lastSubrow = (int)Math.Min(newLinesAt.Count - 1, Math.Floor((selectedArea.Y + selectedArea.Height) / lineSize.Height));
                var firstColumn = (int)Math.Round(selectedArea.X / charWidth);
                var lastColumn = (int)Math.Floor((selectedArea.X + selectedArea.Width) / charWidth);
                if(selectionMode == SelectionMode.Block)
                {
                    for(var i = firstSubrow; i <= lastSubrow; i++)
                    {
                        for(var j = firstColumn; j <= lastColumn; j++)
                        {
                            foregroundColors[(charsOnLine + 1) * i + j] = GetSelectionForegroundColor(i, charsOnLine);
                            backgroundColors[(charsOnLine + 1) * i + j] = selectionColor;
                        }
                    }
                }
                else
                {
                    if(selectionDirection == SelectionDirection.NW)
                    {
                        Utilities.Swap(ref firstColumn, ref lastColumn);
                        Utilities.Swap(ref firstSubrow, ref lastSubrow);
                    }

                    var firstIndex = firstColumn + newLinesAt[firstSubrow];
                    var lastIndex = lastColumn + newLinesAt[lastSubrow];

                    if(lastIndex < firstIndex)
                    {
                        Utilities.Swap(ref firstIndex, ref lastIndex);
                    }

                    var textWithNewLinesStringInfo = new StringInfo(textWithNewLines);
                    firstIndex = Math.Max(0, Math.Min(textWithNewLinesStringInfo.LengthInTextElements - 1, firstIndex));
                    lastIndex = Math.Max(0, Math.Min(textWithNewLinesStringInfo.LengthInTextElements - 1, lastIndex));

                    for(var i = firstIndex; i <= lastIndex; i++)
                    {
                        foregroundColors[i] = GetSelectionForegroundColor(i, charsOnLine);
                        backgroundColors[i] = selectionColor;
                    }
                    selectedContent = textWithNewLinesStringInfo.SubstringByTextElements(firstIndex, lastIndex - firstIndex + 1);
                }
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
            if(cursorInRow.HasValue)
            {
                // we draw a rectangle AND set background so that one can see cursor in a row without character
                textLayout.SetForeground(Colors.Black, cursorInRow.Value, 1);
            }
            ctx.DrawTextLayout(textLayout, 0, 0);
            textLayout.ClearAttributes();
        }

        public void ResetSelection()
        {
            selectedContent = null;
        }

        public virtual void DrawCursor(Context ctx, int offset, bool focused)
        {
            var maxColumn = MaximalColumn + 1;
            var column = offset % maxColumn;
            var row = offset / maxColumn;
            ctx.SetColor(defaultForeground);
            ctx.Rectangle(new Rectangle(column * charWidth, row * lineSize.Height, charWidth, lineSize.Height));
            if(focused)
            {
                ctx.Fill();
                //We add the row number (0-based) to account for \n characters added in subrows.
                cursorInRow = offset + row;
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

        public void Erase(int from, int to, Color? background = null)
        {
            // due to TrimEnd() (near the end of this method) the number of sublines can go down - but this in fact
            // cannot happen during erase operation, so we make sure that the minimal subline count is the current count
            minimalSublineCount = SublineCount;

            var builder = new StringBuilder();
            var stringInfo = new StringInfo(content);
            from = Math.Max(0, Math.Min(from, stringInfo.LengthInTextElements));

            if(from > 0)
            {
                builder.Append(stringInfo.SubstringByTextElements(0, from));
            }
            if(to > from)
            {
                builder.Append(' ', to - from);
            }
            if(to < stringInfo.LengthInTextElements)
            {
                builder.Append(stringInfo.SubstringByTextElements(to));
            }

            for(var i = from; i <= to; i++)
            {
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
            content = builder.ToString().TrimEnd().PadRight(from, ' ');
            lengthInTextElements = new StringInfo(content).LengthInTextElements;
        }

        public bool PutCharacterAt(int position, string what, Color? foreground = null, Color? background = null)
        {
            Debug.Assert(new StringInfo(what).LengthInTextElements == 1);

            if(foreground.HasValue)
            {
                CheckDictionary(ref specialForegrounds);
                specialForegrounds[position] = foreground.Value;
            }
            else
            {
                if(specialForegrounds != null)
                {
                    specialForegrounds.Remove(position);
                }
            }

            if(background.HasValue)
            {
                CheckDictionary(ref specialBackgrounds);
                specialBackgrounds[position] = background.Value;
            }
            else
            {
                if(specialBackgrounds != null)
                {
                    specialBackgrounds.Remove(position);
                }
            }

            var stringInfo = new StringInfo(content);
            StringBuilder builder;
            if(position > 0)
            {
                if(lengthInTextElements <= position) // append at the end of the current string, possibly enlarging it
                {
                    builder = new StringBuilder(content);
                    builder.Append(' ', position - lengthInTextElements).Append(what);
                }
                else // insert in the middle of the current string
                {
                    builder = new StringBuilder(stringInfo.SubstringByTextElements(0, position)).Append(what);
                }
            }
            else // insert at the beginning of the current string
            {
                builder = new StringBuilder(what);
            }
            // append the rest of the current string
            if(lengthInTextElements > position + 1)
            {
                builder.Append(stringInfo.SubstringByTextElements(position + 1));
            }

            content = builder.ToString();
            var oldLengthInTextElements = lengthInTextElements;
            lengthInTextElements = new StringInfo(content).LengthInTextElements;
            // Math.Max used to prevent division by zero errors on windows narrower than one character
            var charsOnLine = Math.Max((int)Math.Floor(lineSize.Width / charWidth), 1);
            var result = DivisionWithCeiling(oldLengthInTextElements == 0 ? 1 : oldLengthInTextElements, charsOnLine)
                != DivisionWithCeiling(lengthInTextElements == 0 ? 1 : lengthInTextElements, charsOnLine);
            return result;
        }

        public virtual int SublineCount
        {
            get
            {
                return lineCount;
            }
        }

        public double LineHeight
        {
            get
            {
                return lineSize.Height;
            }
        }

        public int CurrentMaximalCursorPosition
        {
            get
            {
                return MaximalColumn * lineCount;
            }
        }

        public int MaximalColumn { get; private set; }

        private void CheckDictionary(ref Dictionary<int, Color> dictionary)
        {
            if(dictionary == null)
            {
                dictionary = new Dictionary<int, Color>();
            }
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

        private static int DivisionWithCeiling(int dividend, int divisor)
        {
            Debug.Assert(divisor > 0 && dividend > 0);
            return (dividend + divisor - 1) / divisor;
        }

        private Color GetSelectionForegroundColor(int index, int charsOnLine)
        {
            // the index may be shifted by new line characters in wrapped lines, so we subtract the amount of subrows.
            index -= index / charsOnLine;
            return (specialForegrounds != null && specialForegrounds.ContainsKey(index)) ? specialForegrounds[index].WithIncreasedLight(0.2) : Colors.Black;
        }

        private double charWidth;
        private int lineCount;
        private Size lineSize;
        private TextLayout textLayout;
        private string selectedContent;
        private string content;
        private Color defaultForeground;
        private Color selectionColor;
        private int lengthInTextElements;
        private int? cursorInRow;
        private int minimalSublineCount;
        private Dictionary<int, Color> specialForegrounds;
        private Dictionary<int, Color> specialBackgrounds;

        private static readonly SimpleCache<ILayoutParameters, TextLayout> TextLayoutCache = new SimpleCache<ILayoutParameters, TextLayout>(Utilities.GetTextLayoutFromLayoutParams);
        private static readonly SimpleCache<ILayoutParameters, Size> LineSizeCache = new SimpleCache<ILayoutParameters, Size>(Utilities.GetLineSizeFromLayoutParams);
        private static readonly SimpleCache<ILayoutParameters, Size> CharSizeCache = new SimpleCache<ILayoutParameters, Size>(Utilities.GetCharSizeFromLayoutParams);
    }
}

