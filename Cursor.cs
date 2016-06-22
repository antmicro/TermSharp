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
        internal Cursor(Terminal.TerminalCanvas canvas, Func<int, double> rowHeightCallback)
        {
            this.rowHeightCallback = rowHeightCallback;
            this.canvas = canvas;
            BlinkingRate = TimeSpan.FromMilliseconds(300);
            HandleBlinkingAsync();
        }

        public void Draw(Context ctx, ILayoutParameters layoutParams)
        {
            if(!blinkState)
            {
                return;
            }
            ctx.Save();
            var charSize = CharSizeCache.GetValue(layoutParams);
            ctx.SetColor(Colors.White);
            ctx.MoveTo(0, 0);
            ctx.Rectangle(new Rectangle(Position.X * charSize.Width, rowHeightCallback(Position.Y), charSize.Width, charSize.Height));
            ctx.Fill();
            ctx.Restore();
        }

        public void StayOnForNBlinks(int n)
        {
            blinkWaitRounds = n;
            blinkState = true;
            canvas.QueueDraw();
        }

        public IntegerPosition Position { get; set; }

        public TimeSpan BlinkingRate { get; set; }

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
                canvas.QueueDraw();
            }
        }

        private int blinkWaitRounds;
        private bool blinkState;

        private readonly Func<int, double> rowHeightCallback;
        private readonly Terminal.TerminalCanvas canvas;

        private static readonly SimpleCache<ILayoutParameters, Size> CharSizeCache = new SimpleCache<ILayoutParameters, Size>(Utilities.GetCharSizeFromLayoutParams);
    }
}

