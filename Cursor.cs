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
                if(value.Y >= terminal.ScreenRowsCount)
                {
                    value = value.WithY(terminal.ScreenRowsCount - 1);
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

        public TimeSpan BlinkingRate { get; set; }

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

