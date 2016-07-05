//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using Xwt.Drawing;

namespace Terminal
{
    internal class LayoutParameters : ILayoutParameters
    {
        public LayoutParameters(Font font, Color defaultForeground)
        {
            Font = font;
            this.defaultForeground = defaultForeground;
        }

        public Font Font { get; set; }

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
                return defaultForeground;
            }
            set
            {
                if(value == defaultForeground)
                {
                    return;
                }
                defaultForeground = value;
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

        private int generation;
        private Color defaultForeground;
        private double width;
    }
}

