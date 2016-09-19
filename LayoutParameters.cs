//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using Xwt.Drawing;

namespace TermSharp
{
    internal class LayoutParameters : ILayoutParameters
    {
        public LayoutParameters(Font font, Color defaultForegroundColor, Color selectionColor)
        {
            this.font = font;
            this.defaultForegroundColor = defaultForegroundColor;
            this.selectionColor = selectionColor;
        }

        public Font Font
        {
            get
            {
                return font;
            }
            set
            {
                if(font == value)
                {
                    return;
                }
                font = value;
                UpdateGeneration();
            }
        }

        public double Width
        {
            get
            {
                return width;
            }
            set
            {
                if(value == width)
                {
                    return;
                }
                width = value;
                UpdateGeneration();
            }
        }

        public Color DefaultForeground
        {
            get
            {
                return defaultForegroundColor;
            }
            set
            {
                if(value == defaultForegroundColor)
                {
                    return;
                }
                defaultForegroundColor = value;
                UpdateGeneration();
            }
        }

        public Color SelectionColor
        {
            get
            {
                return selectionColor;
            }
            set
            {
                if(value == selectionColor)
                {
                    return;
                }
                selectionColor = value;
                UpdateGeneration();
            }
        }

        public int Generation
        {
            get
            {
                return generation;
            }
        }

        private void UpdateGeneration()
        {
            generation++;
        }

        private Font font;
        private int generation;
        private Color defaultForegroundColor;
        private Color selectionColor;
        private double width;
    }
}

