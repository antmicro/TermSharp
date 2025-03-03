﻿//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Threading;
using System.Threading.Tasks;
using TermSharp.Misc;

namespace TermSharp
{
    public sealed class Cursor
    {
        internal Cursor(Terminal terminal, Terminal.TerminalCanvas canvas)
        {
            this.terminal = terminal;
            this.canvas = canvas;
            BlinkingRate = TimeSpan.FromMilliseconds(300);
            tokenSource = new CancellationTokenSource();
            blinkingToken = tokenSource.Token;
            blinkingFunction = Task.Run(HandleBlinkingAsync, blinkingToken);
        }

        public void StayOnForNBlinks(int n)
        {
            blinkWaitRounds = n;
            blinkState = true;
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

        public TimeSpan BlinkingRate { get; set; }

        public bool Enabled { get; set; }

        internal bool BlinkState
        {
            get
            {
                return blinkState;
            }
        }

        private async Task HandleBlinkingAsync()
        {
            while(!blinkingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(BlinkingRate, blinkingToken);
                }
                catch(TaskCanceledException)
                {
                    break;
                }

                if(blinkWaitRounds > 0)
                {
                    blinkWaitRounds--;
                    continue;
                }
                if(Enabled)
                {
                    blinkState = !blinkState;
                    canvas.Redraw();
                }
            }
        }

        public void Dispose()
        {
            tokenSource.Cancel();
            blinkingFunction.Wait();
            tokenSource.Dispose();
        }

        private int blinkWaitRounds;
        private bool blinkState;
        private IntegerPosition position;
        private readonly Task blinkingFunction;
        private readonly CancellationTokenSource tokenSource;
        private readonly CancellationToken blinkingToken;

        private readonly Terminal terminal;
        private readonly Terminal.TerminalCanvas canvas;
    }
}

