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
            cursor = new Cursor(this, canvas);
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
            autoscrollEnabled = new TaskCompletionSource<bool>();
            HandleAutoscrollAsync();
        }

        public void AppendRow(IRow row)
        {
            rowsGeneration++;
            var weWereAtEnd = scrollbar.Value == GetMaximumScrollbarValue();
            rows.Add(row);
            row.PrepareForDrawing(layoutParameters);
            AddToHeightMap(row.PrepareForDrawing(layoutParameters));
            canvas.QueueDraw();

            scrollbar.Sensitive = GetMaximumHeight() > canvas.Bounds.Height;

            scrollbar.UpperValue = GetMaximumHeight();
            if(weWereAtEnd)
            {
                SetScrollbarValue(scrollbar.UpperValue - canvas.Bounds.Height);
            }
        }

        public new void Clear()
        {
            rowsGeneration++;
            rows.Clear();
            RebuildHeightMap(true);
            canvas.QueueDraw();
        }

        public void Redraw()
        {
            canvas.QueueDraw();
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

        public IRow GetScreenRow(int screenPosition)
        {
            return rows[GetScreenRowId(screenPosition)];
        }

        public int Count
        {
            get
            {
                return rows.Count;
            }
        }

        public int ScreenRowsCount
        {
            get
            {
                double unused;
                var firstRowNo = FindRowIndexAtPosition(GetMaximumScrollbarValue(), out unused);
                return Count - firstRowNo;
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

        public new Cursor Cursor
        {
            get
            {
                return cursor;
            }
        }

        private void OnScrollbarValueChanged(object sender, EventArgs e)
        {
            double rowOffset;
            canvas.FirstRowToDisplay = FindRowIndexAtPosition(scrollbar.Value, out rowOffset);
            canvas.FirstRowHeight = rowOffset;
            canvas.OffsetFromFirstRow = scrollbar.Value - rowOffset;
            RefreshSelection();
        }

        private void OnCanvasButtonPressed(object sender, ButtonEventArgs e)
        {
            var position = e.Position;
            position.Y += scrollbar.Value;
            currentScrollStart = position;
        }

        private void OnCanvasButtonReleased(object sender, ButtonEventArgs e)
        {
            SetAutoscrollValue(0);
            var mousePosition = e.Position;
            mousePosition.Y += scrollbar.Value;
            if(mousePosition == (currentScrollStart ?? default(Point)))
            {
                canvas.SelectedArea = default(Rectangle);
            }
            currentScrollStart = null;
            RefreshSelection();
        }

        private void OnCanvasMouseMoved(object sender, MouseMovedEventArgs e)
        {
            if(!currentScrollStart.HasValue)
            {
                return;
            }
            lastMousePosition = e.Position;
            if(e.Position.Y < 0)
            {
                SetAutoscrollValue((int)e.Position.Y);
            }
            else if(e.Position.Y > canvas.Bounds.Height)
            {
                SetAutoscrollValue((int)(e.Position.Y - canvas.Bounds.Height));
            }
            else
            {
                SetAutoscrollValue(0);
            }
            RefreshSelection();
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
            SetScrollbarValue(scrollbar.Value + scrollbar.StepIncrement * modifier);
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

            scrollbar.UpperValue = GetMaximumHeight();

            // difference between old and new position of the first displayed row:
            var diff = GetStartHeightOfTheRow(firstDisplayedRowIndex) - oldPosition;
            SetScrollbarValue(oldScrollbarValue + diff);
            canvas.QueueDraw();
        }

        private void SetAutoscrollValue(int value)
        {
            autoscrollStep = value;
            if(value != 0)
            {
                autoscrollEnabled.TrySetResult(true);
            }
            else
            {
                if(autoscrollEnabled.Task.IsCompleted)
                {
                    autoscrollEnabled = new TaskCompletionSource<bool>();
                }
            }
        }

        private void SetScrollbarValue(double value)
        {
            var finalValue = Math.Max(0, value);
            finalValue = Math.Min(finalValue, GetMaximumScrollbarValue());
            scrollbar.Value = finalValue;
        }

        private void RefreshSelection()
        {
            if(currentScrollStart.HasValue)
            {
                var scrollStart = currentScrollStart.Value;
                canvas.SelectedArea = new Rectangle(scrollStart.X, scrollStart.Y, lastMousePosition.X - scrollStart.X, lastMousePosition.Y + scrollbar.Value - scrollStart.Y);
            }
            canvas.QueueDraw();
        }

        private async void HandleAutoscrollAsync()
        {
            while(true)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(40));
                if(autoscrollStep != 0)
                {
                    if(Math.Abs(autoscrollStep) > scrollbar.PageSize/2)
                    {
                        autoscrollStep = (int)(Math.Sign(autoscrollStep) * scrollbar.PageSize / 2);
                    }
                    SetScrollbarValue(scrollbar.Value + autoscrollStep);
                }
                await autoscrollEnabled.Task;
            }
        }

        private void PrepareLayoutParameters()
        {
            layoutParameters.Font = Font.WithFamily("Monaco").WithSize(12);
        }

        private bool RebuildHeightMap(bool continueEvenIfLongTask = true)
        {
            var oldFirstScreenRow = GetScreenRowId(0);

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

            scrollbar.PageSize = canvas.Bounds.Height; // we update it here to get new value on GetScreenRowId (it depends on height map and this value)
            var firstScreenRow = GetScreenRowId(0);
            var diff = firstScreenRow - oldFirstScreenRow;
            cursor.Position = cursor.Position.ShiftedByY(-diff);
            
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

        private int GetScreenRowId(int screenPosition)
        {
            double unused;
            return FindRowIndexAtPosition(GetMaximumScrollbarValue(), out unused) + screenPosition;
        }

        private double[] heightMap;
        private double[] unfinishedHeightMap;
        private int canvasBoundChangedGeneration;
        private int rowsGeneration;
        private Point? currentScrollStart;
        private Point lastMousePosition;
        private int autoscrollStep;
        private TaskCompletionSource<bool> autoscrollEnabled;

        private readonly List<IRow> rows;
        private readonly LayoutParameters layoutParameters;
        private readonly VScrollbar scrollbar;
        private readonly TerminalCanvas canvas;
        private readonly Cursor cursor;

        private static readonly TimeSpan HeightMapRebuildTimeout = TimeSpan.FromMilliseconds(30);
        private const int HeightMapCheckTimeoutEveryNthRow = 1000;

        internal sealed class TerminalCanvas : Canvas
        {
            public TerminalCanvas(Terminal parent)
            {
                this.parent = parent;
                Cursor = CursorType.IBeam;
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

                var heightSoFar = 0.0;

                ctx.Translate(0, -OffsetFromFirstRow);
                ctx.Save();
                var i = FirstRowToDisplay;
                var cursorRow = parent.GetScreenRowId(parent.Cursor.Position.Y);
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
                    if(i == cursorRow && parent.Cursor.BlinkState)
                    {
                        parent.rows[i].DrawCursor(ctx, parent.Cursor.Position.X);
                    }
                    ctx.Restore();

                    heightSoFar += height;
                    ctx.Translate(0, height);
                    i++;
                }
                ctx.Restore();
            }

            private readonly Terminal parent;
        }
    }
}

