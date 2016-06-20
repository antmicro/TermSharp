//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            canvas.BoundsChanged += OnCanvasBoundsChanged;
            canvas.ButtonPressed += OnCanvasButtonPressed;
            canvas.ButtonReleased += OnCanvasButtonReleased;
            canvas.MouseMoved += OnCanvasMouseMoved;
            scrollbar.StepIncrement = 15; // TODO

            scrollbar.ValueChanged += OnScrollbarValueChanged;
        }

        public void AppendRow(IRow row)
        {
            rowsGeneration++;
            var weWereAtEnd = scrollbar.Value == GetMaximumScrollbarValue();
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

        public new void Clear()
        {
            rowsGeneration++;
            rows.Clear();
            Refresh();
        }

        public ClipboardData CollectClipboardData()
        {
            var result = new ClipboardData();
            foreach(var row in rows)
            {
                row.FillClipboardData(result);
            }
            return result;
        }

        public int Count
        {
            get
            {
                return rows.Count;
            }
        }

        public SelectionMode SelectionMode
        {
            get
            {
                return canvas.SelectionMode;
            }
            set
            {
                canvas.SelectionMode = value;
            }
        }

        public Rectangle Margins { get; set; } // TODO

        private void OnScrollbarValueChanged(object sender, EventArgs e)
        {
            double rowOffset;
            canvas.FirstRowToDisplay = FindRowIndexAtPosition(scrollbar.Value, out rowOffset);
            canvas.FirstRowHeight = rowOffset;
            canvas.OffsetFromFirstRow = scrollbar.Value - rowOffset;
            canvas.QueueDraw();
        }

        private void OnCanvasButtonPressed(object sender, ButtonEventArgs e)
        {
            currentScrollStart = e.Position;
        }

        private void OnCanvasButtonReleased(object sender, ButtonEventArgs e)
        {
            if(e.Position == (currentScrollStart ?? default(Point)))
            {
                canvas.SelectedArea = default(Rectangle);
            }
            currentScrollStart = null;
            canvas.QueueDraw();
        }

        private void OnCanvasMouseMoved(object sender, MouseMovedEventArgs e)
        {
            if(!currentScrollStart.HasValue)
            {
                return;
            }
            var scrollStart = currentScrollStart.Value;
            canvas.SelectedArea = new Rectangle(scrollStart, new Size(e.X - scrollStart.X, e.Y - scrollStart.Y));
            canvas.QueueDraw();
        }

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
            var finalValue = Math.Max(0, scrollbar.Value + scrollbar.StepIncrement * modifier);
            finalValue = Math.Min(finalValue, scrollbar.UpperValue - canvas.Bounds.Height);
            scrollbar.Value = finalValue;
        }

        private async void OnCanvasBoundsChanged(object sender, EventArgs e)
        {
            canvas.SelectedArea = default(Rectangle);

            var ourRowsGeneration = rowsGeneration;
            double oldPosition;
            var firstDisplayedRowIndex = FindRowIndexAtPosition(scrollbar.Value, out oldPosition);
            var oldScrollbarValue = scrollbar.Value;

            layoutParameters.Width = canvas.Size.Width;

            if(!RebuildHeightMap(false))
            {
                var boundChangedGeneration = ++canvasBoundChangedGeneration;

                await Task.Delay(TimeSpan.FromMilliseconds(200));
                if(ourRowsGeneration != rowsGeneration || boundChangedGeneration != canvasBoundChangedGeneration)
                {
                    return;
                }
                RebuildHeightMap(true);
            }

            scrollbar.PageSize = canvas.Bounds.Height;
            scrollbar.UpperValue = GetMaximumHeight();

            // difference between old and new position of the first displayed row:
            var diff = GetStartHeightOfTheRow(firstDisplayedRowIndex) - oldPosition;
            scrollbar.Value = Math.Min(oldScrollbarValue + diff, GetMaximumScrollbarValue());

            canvas.QueueDraw();
        }

        private void PrepareLayoutParameters()
        {
            layoutParameters.Font = Font.WithFamily("Monaco").WithSize(12);
        }

        private bool RebuildHeightMap(bool continueEvenIfLongTask = true)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            double[] newHeightMap;
            if(unfinishedHeightMap != null && unfinishedHeightMap.Length == rows.Count)
            {
                newHeightMap = unfinishedHeightMap;
            }
            else
            {
                newHeightMap = new double[rows.Count];
                unfinishedHeightMap = newHeightMap;
            }
            var heightSoFar = 0.0;
            for(var i = 0; i < newHeightMap.Length; i++)
            {
                heightSoFar += rows[i].PrepareForDrawing(layoutParameters);
                newHeightMap[i] = heightSoFar;
                if(!continueEvenIfLongTask && (i % HeightMapCheckTimeoutEveryNthRow) == 1 && stopwatch.Elapsed > HeightMapRebuildTimeout)
                {
                    return false;
                }
            }
            heightMap = newHeightMap;
            unfinishedHeightMap = null;
            return true;
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

        private double GetStartHeightOfTheRow(int rowIndex)
        {
            return rowIndex > 0 ? heightMap[rowIndex - 1] : 0.0;
        }

        private double GetMaximumScrollbarValue()
        {
            return Math.Max(0, GetMaximumHeight() - scrollbar.PageSize);
        }

        private double GetMaximumHeight()
        {
            if(heightMap.Length == 0)
            {
                return 0;
            }
            return heightMap[heightMap.Length - 1];
        }

        private int FindRowIndexAtPosition(double position, out double rowStart)
        {
            var result = Array.BinarySearch(heightMap, position);
            if(result < 0)
            {
                result = ~result;
            }
            else
            {
                result++; // because heightMap[i] shows where ith row *ends* and therefore where (i+1)th starts
            }
            if(result == heightMap.Length)
            {
                result--;
            }
            rowStart = GetStartHeightOfTheRow(result);
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
            RebuildHeightMap(true);
            canvas.QueueDraw();
        }

        private double[] heightMap;
        private double[] unfinishedHeightMap;
        private int canvasBoundChangedGeneration;
        private int rowsGeneration;
        private Point? currentScrollStart;

        private readonly List<IRow> rows;
        private readonly LayoutParameters layoutParameters;
        private readonly VScrollbar scrollbar;
        private readonly TerminalCanvas canvas;

        private static readonly TimeSpan HeightMapRebuildTimeout = TimeSpan.FromMilliseconds(30);
        private const int HeightMapCheckTimeoutEveryNthRow = 1000;

        private sealed class TerminalCanvas : Canvas
        {
            public TerminalCanvas(Terminal parent)
            {
                this.parent = parent;
            }

            public int FirstRowToDisplay { get; set; }

            public double FirstRowHeight { get; set; }

            public double OffsetFromFirstRow { get; set; }

            public Rectangle SelectedArea { get; set; }

            public SelectionMode SelectionMode { get; set; }

            protected override void OnDraw(Context ctx, Rectangle dirtyRect)
            {
                var screenSelectedArea = SelectedArea;
                var selectionDirection = SelectionDirection.SE;
                if(screenSelectedArea.Width < 0)
                {
                    selectionDirection = (SelectionDirection)((int)selectionDirection + 1);
                    screenSelectedArea.X += screenSelectedArea.Width;
                    screenSelectedArea.Width = -screenSelectedArea.Width;
                }
                if(screenSelectedArea.Height < 0)
                {
                    selectionDirection = (SelectionDirection)((int)selectionDirection + 2);
                    screenSelectedArea.Y += screenSelectedArea.Height;
                    screenSelectedArea.Height = -screenSelectedArea.Height;
                }
                screenSelectedArea.Y -= FirstRowHeight;
                
                parent.layoutParameters.Width = Size.Width;
                if(parent.rows.Count == 0)
                {
                    return;
                }

                var heightSoFar = 0.0;

                ctx.Translate(0, -OffsetFromFirstRow);
                var i = FirstRowToDisplay;
                while(i < parent.rows.Count && heightSoFar - OffsetFromFirstRow < Bounds.Height)
                {
                    var height = parent.rows[i].PrepareForDrawing(parent.layoutParameters);
                    var rowRectangle = new Rectangle(0, heightSoFar, parent.layoutParameters.Width, height);
                    var selectedAreaInRow = rowRectangle.Intersect(screenSelectedArea);
                    if(SelectionMode == SelectionMode.Normal && selectedAreaInRow != default(Rectangle) &&
                       (screenSelectedArea.Y <= rowRectangle.Y || screenSelectedArea.Y + screenSelectedArea.Height >= rowRectangle.Y + rowRectangle.Height))
                    {
                        if(rowRectangle.Y < screenSelectedArea.Y)
                        {
                            // I'm the first row (and there is a second row)
                            selectedAreaInRow.X = SelectedArea.Height > 0 ? SelectedArea.X : SelectedArea.X + SelectedArea.Width;
                            selectedAreaInRow.Width = parent.layoutParameters.Width - selectedAreaInRow.X;
                        }
                        else if(rowRectangle.Y + rowRectangle.Height > screenSelectedArea.Y + screenSelectedArea.Height)
                        {
                            // I'm the last row (and there is some other row)
                            selectedAreaInRow.Width = SelectedArea.Height > 0 ? SelectedArea.X + SelectedArea.Width : SelectedArea.X;
                            selectedAreaInRow.X = 0;
                        }
                        else
                        {
                            // nor the first neither the last one - must be one of the middle rows
                            selectedAreaInRow.X = 0;
                            selectedAreaInRow.Width = parent.layoutParameters.Width;
                        }
                    }
                    if(selectedAreaInRow != default(Rectangle))
                    {
                        selectedAreaInRow.Y -= heightSoFar;
                    }

                    ctx.Save();
                    parent.rows[i].Draw(ctx, selectedAreaInRow, selectionDirection);
                    ctx.Restore();

                    heightSoFar += height;
                    ctx.Translate(0, height);
                    i++;
                }
            }

            private readonly Terminal parent;
        }
    }
}

