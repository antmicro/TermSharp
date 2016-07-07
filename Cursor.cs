//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Threading.Tasks;
using Xwt;
using Xwt.Drawing;

namespace Terminal
{
    public sealed class Cursor
    {
        internal Cursor(Terminal terminal, Terminal.TerminalCanvas canvas)
        {
            this.terminal = terminal;
            this.canvas = canvas;
            BlinkingRate = TimeSpan.FromMilliseconds(300);
            HandleBlinkingAsync();
        }

        public void StayOnForNBlinks(int n)
        {
            blinkWaitRounds = n;
            blinkState = true;
            canvas.QueueDraw();
        }

        public IntegerPosition Position
        {
            get
            {
                return position;
            }
            set
            {
                if(value.Y < 0)
                {
                    value = value.WithY(0);
                }
                if(value.Y >= terminal.ScreenRowCount)
                {
                    value = value.WithY(terminal.ScreenRowCount - 1);
                }
                if(value.X < 0)
                {
                    value = value.WithX(0);
                }
                var row = terminal.GetScreenRow(value.Y);
                if(value.X > row.MaxOffset)
                {
                    value = value.WithX(row.MaxOffset);
                }
                position = value;
            }
        }

        public IntegerPosition MaximalPosition
        {
            get
            {
                var maxY = terminal.ScreenRowCount - 1;
                var maxX = terminal.GetScreenRow(maxY).MaxOffset;
                return new IntegerPosition(maxX, maxY);
            }
        }

        public TimeSpan BlinkingRate { get; set; }

        public bool Enabled { get; set; }

        internal bool BlinkState
        {
            get
            {
                return blinkState;
            }
        }

        private async void HandleBlinkingAsync()
        {
            while(true)
            {
                await Task.Delay(BlinkingRate);
                if(blinkWaitRounds > 0)
                {
                    blinkWaitRounds--;
                    continue;
                }
                blinkState = !blinkState;
                canvas.QueueDraw(); // TODO: only if cursor is visible
            }
        }

        private int blinkWaitRounds;
        private bool blinkState;
        private IntegerPosition position;

        private readonly Terminal terminal;
        private readonly Terminal.TerminalCanvas canvas;
    }
}

