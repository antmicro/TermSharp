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
            layoutParameters = new LayoutParameters(Font);
            Margins = new Rectangle();
            BackgroundColor = Colors.Black;
            PrepareLayoutParameters();
            canvas = new TerminalCanvas(this);
            PackStart(canvas, true, true);
            scrollbar = new VScrollbar();
            PackEnd(scrollbar);

            scrollbar.ValueChanged += delegate {
                Console.WriteLine(scrollbar.Value);
            };
        }

        public void AppendRow(IRow row)
        {
            rows.Add(row);
            canvas.QueueDraw();
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
            canvas.QueueDraw();
        }

        public int Count
        {
            get
            {
                return rows.Count;
            }
        }

        public Rectangle Margins { get; set; }

        private void PrepareLayoutParameters()
        {
            layoutParameters.Font = Font.WithFamily("Monaco").WithSize(12);
        }

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
                parent.layoutParameters.Width = Size.Width;
                ctx.SetColor(Colors.White);

                var rowsToDraw = new List<IRow>();
                var heights = new List<double>();
                var heightSoFar = parent.Margins.Height;
                foreach(var row in parent.GetAllRows())
                {
                    var height = row.PrepareForDrawing(parent.layoutParameters);
                    heightSoFar += height;
                    heights.Add(height);
                    rowsToDraw.Add(row);
                    if(heightSoFar > Bounds.Height)
                    {
                        // stop drawing, no more rows will be visible
                        break;
                    }
                }

                ctx.Translate(0, -parent.Margins.Height);
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

