//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using Xwt;
using Xwt.Drawing;
using System.Collections.Generic;

namespace Terminal
{
    public sealed class Terminal : Canvas
    {
        public Terminal()
        {
            rows = new RowCollection();
            rows.ContentChanged += OnRowsChanged;
            layoutParameters = new LayoutParameters(Font);
            Margins = new Rectangle();
            BackgroundColor = Colors.Black;
            PrepareLayoutParameters();
        }

        public Rectangle Margins { get; set; }

        public IRowCollection Rows
        {
            get
            {
                return rows;
            }
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            layoutParameters.Width = Size.Width;
            ctx.SetColor(Colors.White);

            var rowsToDraw = new List<IRow>();
            var heights = new List<double>();
            var heightSoFar = Margins.Height;
            foreach(var row in rows.GetAllRows())
            {
                var height = row.PrepareForDrawing(layoutParameters);
                heightSoFar += height;
                heights.Add(height);
                rowsToDraw.Add(row);
                if(heightSoFar > Bounds.Height)
                {
                    // stop drawing, no more rows will be visible
                    break;
                }
            }

            ctx.Translate(0, -Margins.Height);
            ctx.Save();
            ctx.Translate(0, Math.Min(heightSoFar, Bounds.Height));
            for(var i = 0; i < rowsToDraw.Count; i++)
            {
                ctx.Translate(0, -heights[i]);
                rowsToDraw[i].Draw (ctx);
            }
            ctx.Restore();
        }

        private void PrepareLayoutParameters()
        {
            layoutParameters.Font = Font.WithFamily("Monaco").WithSize(12);
        }

        private void OnRowsChanged()
        {
            QueueDraw();
        }

        private readonly RowCollection rows;
        private readonly LayoutParameters layoutParameters;
    }
}

