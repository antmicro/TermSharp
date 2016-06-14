//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;
using System.Linq;
using Xwt;
using Xwt.Drawing;

namespace Terminal
{
    public class Terminal : HBox
    {
        public Terminal()
        {
            rows = new List<IRow>();
            heightMap = new double[0];
            layoutParameters = new LayoutParameters(Font);
            Margins = new Rectangle();
            BackgroundColor = Colors.Black;
            PrepareLayoutParameters();
            canvas = new TerminalCanvas(this);
            PackStart(canvas, true, true);
            scrollbar = new VScrollbar();
            scrollbar.Sensitive = false;
            PackEnd(scrollbar);

            canvas.MouseScrolled += OnCanvasMouseScroll;

            scrollbar.ValueChanged += delegate 
            {
                canvas.QueueDraw();
            };
        }

        public void AppendRow(IRow row)
        {
            var weWereAtEnd = scrollbar.Value == Math.Max(0, GetMaximumHeight() - canvas.Bounds.Height);
            rows.Add(row);
            AddToHeightMap(row.PrepareForDrawing(layoutParameters));
            canvas.QueueDraw();

            scrollbar.Sensitive = GetMaximumHeight() > canvas.Bounds.Height;

            scrollbar.PageSize = canvas.Bounds.Height;
            scrollbar.UpperValue = GetMaximumHeight();
            if(weWereAtEnd)
            {
                scrollbar.Value = scrollbar.UpperValue - canvas.Bounds.Height;
            }
        }

        public IEnumerable<IRow> GetAllRows()
        {
            for(var i = rows.Count - 1; i >= 0; i--)
            {
                yield return rows[i];
            }
        }

        public new void Clear()
        {
            rows.Clear();
            Refresh();
        }

        public int Count
        {
            get
            {
                return rows.Count;
            }
        }

        public Rectangle Margins { get; set; }

        private void OnCanvasMouseScroll(object sender, MouseScrolledEventArgs e)
        {
            int modifier;
            switch(e.Direction)
            {
            case ScrollDirection.Up:
                modifier = -1;
                break;
            case ScrollDirection.Down:
                modifier = 1;
                break;
            default:
                modifier = 0;
                break;
            }
            var finalValue = Math.Max(0, scrollbar.Value + scrollbar.PageSize * modifier);
            finalValue = Math.Min(finalValue, scrollbar.UpperValue - canvas.Bounds.Height);
            scrollbar.Value = finalValue;
        }

        private void PrepareLayoutParameters()
        {
            layoutParameters.Font = Font.WithFamily("Monaco").WithSize(12);
        }

        private void RebuildHeightMap()
        {
            heightMap = new double[rows.Count];
            var heightSoFar = 0.0;
            for(var i = 0; i < heightMap.Length; i++)
            {
                heightSoFar += rows[i].PrepareForDrawing(layoutParameters);
                heightMap[i] = heightSoFar;
            }
        }

        private void AddToHeightMap(double value)
        {
            if(heightMap.Length == 0)
            {
                heightMap = new[] { value };
                return;
            }
            var oldHeightMap = heightMap;
            heightMap = new double[oldHeightMap.Length + 1];
            Array.Copy(oldHeightMap, heightMap, oldHeightMap.Length);
            heightMap[oldHeightMap.Length] = value + heightMap[oldHeightMap.Length - 1];
        }

        private double GetMaximumHeight()
        {
            if(heightMap.Length == 0)
            {
                return 0;
            }
            return heightMap[heightMap.Length - 1];
        }

        private int RowIndexAtPosition(double position, out double rowStart)
        {
            var result = Array.BinarySearch(heightMap, position);
            if(result < 0)
            {
                result = ~result;
            }
            if(result == heightMap.Length)
            {
                result--;
            }
            rowStart = heightMap[result];
            return result;
        }

        private IEnumerable<IRow> EnumerateRowsFromPosition(double position)
        {
            var startingIndex = Array.BinarySearch(heightMap, position);
            if(startingIndex < 0)
            {
                startingIndex = ~startingIndex;
            }
            for(var i = startingIndex - 1; i >= 0; i--)
            {
                yield return rows[i];
            }
        }

        private void Refresh()
        {
            RebuildHeightMap();
            canvas.QueueDraw();
        }

        private double[] heightMap;

        private readonly List<IRow> rows;
        private readonly LayoutParameters layoutParameters;
        private readonly VScrollbar scrollbar;
        private readonly TerminalCanvas canvas;

        private sealed class TerminalCanvas : Canvas
        {
            public TerminalCanvas(Terminal parent)
            {
                this.parent = parent;
            }

            protected override void OnDraw(Context ctx, Rectangle dirtyRect)
            {
                parent.layoutParameters.Width = Size.Width; // TODO
                if(parent.rows.Count == 0)
                {
                    return;
                }
                ctx.SetColor(Colors.White);

                var rowsToDraw = new List<IRow>();
                var heights = new List<double>();
                var heightSoFar = 0.0;

                double rowPosition;
                var index = parent.RowIndexAtPosition(parent.scrollbar.Value + Bounds.Height, out rowPosition);
                var additionalSpaceToDraw = rowPosition - parent.scrollbar.Value - Bounds.Height;
                if(additionalSpaceToDraw < 0)
                {
                    additionalSpaceToDraw = 0;
                }
                while(index >= 0 && heightSoFar <= Bounds.Height + additionalSpaceToDraw)
                {
                    var row = parent.rows[index];
                    var height = row.PrepareForDrawing(parent.layoutParameters);
                    heightSoFar += height;
                    heights.Add(height);
                    rowsToDraw.Add(row);
                    index--;
                }

                ctx.Translate(0, additionalSpaceToDraw);
                ctx.Save();
                ctx.Translate(0, Math.Min(heightSoFar, Bounds.Height));
                for(var i = 0; i < rowsToDraw.Count; i++)
                {
                    ctx.Translate(0, -heights[i]);
                    rowsToDraw[i].Draw(ctx);
                }
                ctx.Restore();
            }

            private readonly Terminal parent;
        }
    }
}

