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
using Terminal.Misc;
using Terminal.Rows;
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
            layoutParameters = new LayoutParameters(Font, Colors.White, Colors.LightSlateGray);
            defaultBackground = Colors.Black;

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

            scrollbar.ValueChanged += OnScrollbarValueChanged;
            autoscrollEnabled = new TaskCompletionSource<bool>();
            HandleAutoscrollAsync();
            canvas.CanGetFocus = true;
            AppendRow(new MonospaceTextRow(""));
            scrollbar.StepIncrement = MonospaceTextRow.GetLineSizeFromLayoutParams(layoutParameters).Height;
        }

        public void AppendRow(IRow row)
        {
            var weWereAtEnd = scrollbar.Value == GetMaximumScrollbarValue();
            rows.Add(row);
            row.PrepareForDrawing(layoutParameters);
            AddToHeightMap(row.PrepareForDrawing(layoutParameters));
            RefreshInner(weWereAtEnd);
        }

        public new void Clear()
        {
            rows.Clear();
            RebuildHeightMap(true);
            RefreshInner(true); // it does not matter whether we actually were at the end
        }

        public void Refresh()
        {
            var weWereAtEnd = scrollbar.Value == GetMaximumScrollbarValue();
            RebuildHeightMap(true);
            RefreshInner(weWereAtEnd);
        }

        public void Redraw()
        {
            canvas.Redraw();
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
            return rows[GetScreenRowIndex(screenPosition)];
        }

        public IRow GetFirstScreenRow(out double hiddenHeight)
        {
            double rowStart;
            var result = rows[FindRowIndexAtPosition(GetMaximumScrollbarValue(), out rowStart)];
            hiddenHeight = GetMaximumScrollbarValue() - rowStart;
            return result;
        }

        public void EraseScreen(IntegerPosition from, IntegerPosition to, Color? background)
        {
            for(var rowScreenPosition = from.Y; rowScreenPosition <= to.Y; rowScreenPosition++)
            {
                var row = GetScreenRow(rowScreenPosition);
                row.Erase(rowScreenPosition == from.Y ? from.X : 0, rowScreenPosition == to.Y ? to.X : row.CurrentMaximalCursorPosition, background);
            }
            canvas.Redraw();
        }

        public void MoveScrollbarToEnd()
        {
            SetScrollbarValue(scrollbar.UpperValue - canvas.Bounds.Height);
        }

        public int RowCount
        {
            get
            {
                return rows.Count;
            }
        }

        public int ScreenRowCount
        {
            get
            {
                double unused;
                var firstRowNo = FindRowIndexAtPosition(GetMaximumScrollbarValue(), out unused);
                return RowCount - firstRowNo;
            }
        }

        public double ScreenSize
        {
            get
            {
                return canvas.Bounds.Height;
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

        public new Cursor Cursor
        {
            get
            {
                return cursor;
            }
        }

        public Color DefaultForeground
        {
            get
            {
                return layoutParameters.DefaultForeground;
            }
            set
            {
                layoutParameters.DefaultForeground = value;
            }
        }

        public Color DefaultBackground
        {
            get
            {
                return defaultBackground;
            }
            set
            {
                BackgroundColor = value;
                defaultBackground = value;
            }
        }

        public Color SelectionColor
        {
            get
            {
                return layoutParameters.SelectionColor;
            }
            set
            {
                layoutParameters.SelectionColor = value;
            }
        }

        public WidgetSpacing InnerMargin
        {
            get
            {
                return canvas.Margin;
            }
            set
            {
                canvas.Margin = value;
            }
        }

        public new event EventHandler<KeyEventArgs> KeyPressed
        {
            add
            {
                canvas.KeyPressed += value;
            }
            remove
            {
                canvas.KeyPressed -= value;
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
            canvas.SetFocus();
            var position = e.Position;
            position.Y += scrollbar.Value;
            currentScrollStart = position;
            foreach(var row in rows)
            {
                row.ResetSelection();
            }
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

#if DEBUG
        private async void OnCanvasBoundsChanged(object sender, EventArgs e)
#else
        private void OnCanvasBoundsChanged(object sender, EventArgs e)
#endif
        {
            canvas.SelectedArea = default(Rectangle);

            double oldPosition;
            var firstDisplayedRowIndex = FindRowIndexAtPosition(scrollbar.Value, out oldPosition);
            var oldScrollbarValue = scrollbar.Value;

            layoutParameters.Width = canvas.Size.Width;

#if DEBUG
            if(!RebuildHeightMap(false))
            {
                var boundChangedGeneration = ++canvasBoundChangedGeneration;

                await Task.Delay(TimeSpan.FromMilliseconds(200));
                if(boundChangedGeneration != canvasBoundChangedGeneration)
                {
                    return;
                }
                RebuildHeightMap(true);
            }
#else
            RebuildHeightMap(true);
#endif

            scrollbar.UpperValue = GetMaximumHeight();

            // difference between old and new position of the first displayed row:
            var diff = GetPositionOfTheRow(firstDisplayedRowIndex) - oldPosition;
            SetScrollbarValue(oldScrollbarValue + diff);
            canvas.Redraw();
        }

        private void RefreshInner(bool weWereAtEnd)
        {
            canvas.Redraw();
            scrollbar.Sensitive = GetMaximumHeight() > canvas.Bounds.Height;

            scrollbar.UpperValue = GetMaximumHeight();
            if(weWereAtEnd)
            {
                MoveScrollbarToEnd();
            }
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
            canvas.Redraw();
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
            if(rows.Count == 0)
            {
                return true;
            }

            var oldFirstScreenRow = GetScreenRowIndex(0);

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
            var firstScreenRow = GetScreenRowIndex(0);
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

        private double GetPositionOfTheRow(int rowIndex)
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
            rowStart = GetPositionOfTheRow(result);
            return result;
        }

        private int GetScreenRowIndex(int screenPosition)
        {
            double unused;
            return FindRowIndexAtPosition(GetMaximumScrollbarValue(), out unused) + screenPosition;
        }

        private double[] heightMap;
        private double[] unfinishedHeightMap;
#if DEBUG
        private int canvasBoundChangedGeneration;
#endif
        private Point? currentScrollStart;
        private Point lastMousePosition;
        private int autoscrollStep;
        private TaskCompletionSource<bool> autoscrollEnabled;
        private Color defaultBackground;

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

            public void Redraw()
            {
                if(drawn)
                {
                    QueueDraw();
                    drawn = false;
                }
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

                ctx.Save();
                ctx.SetColor(parent.DefaultBackground);
                ctx.Rectangle(new Rectangle(0, 0, Bounds.Width, Bounds.Height));
                ctx.Fill();
                ctx.Restore();

                ctx.Translate(0, -OffsetFromFirstRow);
                ctx.Save();
                var i = FirstRowToDisplay;
                var cursorRow = parent.GetScreenRowIndex(parent.Cursor.Position.Y);
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
                    parent.rows[i].Draw(ctx, selectedAreaInRow, selectionDirection, parent.SelectionMode);
                    if(parent.Cursor.Enabled && i == cursorRow && (parent.Cursor.BlinkState || !HasFocus))
                    {
                        parent.rows[i].DrawCursor(ctx, parent.Cursor.Position.X, HasFocus);
                    }
                    ctx.Restore();

                    heightSoFar += height;
                    ctx.Translate(0, height);
                    i++;
                }
                ctx.Restore();
                drawn = true;
            }

            private bool drawn;
            private readonly Terminal parent;
        }
    }
}

