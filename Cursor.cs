//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using TermSharp.Misc;
using Xwt;

namespace TermSharp
{
    public sealed class Cursor : IDisposable
    {
        internal Cursor(Terminal terminal, Terminal.TerminalCanvas canvas)
        {
            this.terminal = terminal;
            this.canvas = canvas;
            BlinkingRate = TimeSpan.FromMilliseconds(300);
            blinkHandle = Application.TimeoutInvoke(BlinkingRate, UpdateBlinkState);
        }

        public void Dispose()
        {
            blinkHandle.Dispose();
        }

        public void StayOnForNBlinks(int n)
        {
            blinkWaitRounds = n;
            BlinkState = true;
            canvas.Redraw();
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
                position = value;
            }
        }

        public IntegerPosition MaximalPosition
        {
            get
            {
                var maxY = terminal.ScreenRowCount - 1;
                var maxX = terminal.GetScreenRow(maxY).CurrentMaximalCursorPosition;
                return new IntegerPosition(maxX, maxY);
            }
        }

        public TimeSpan BlinkingRate
        {
            get => blinkingRate;
            set
            {
                blinkingRate = value;
                if(blinkHandle != null)
                {
                    blinkHandle.Dispose();
                    blinkHandle = Application.TimeoutInvoke(BlinkingRate, UpdateBlinkState);
                }
            }
        }

        public bool Enabled { get; set; }

        internal bool BlinkState { get; private set; }

        private bool UpdateBlinkState()
        {
            if(blinkWaitRounds > 0)
            {
                blinkWaitRounds--;
            }
            else if(Enabled)
            {
                BlinkState = !BlinkState;
                canvas.Redraw();
            }
            return true;
        }

        private TimeSpan blinkingRate;
        private int blinkWaitRounds;
        private IntegerPosition position;
        private IDisposable blinkHandle;

        private readonly Terminal terminal;
        private readonly Terminal.TerminalCanvas canvas;
    }
}
