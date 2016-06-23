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
        internal Cursor(Terminal.TerminalCanvas canvas)
        {
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

        public IntegerPosition Position { get; set; }

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

        private readonly Terminal.TerminalCanvas canvas;
    }
}

