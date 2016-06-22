//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
namespace Terminal
{
    public struct IntegerPosition
    {
        public IntegerPosition(int x, int y) : this()
        {
            X = x;
            Y = y;
        }

        public IntegerPosition ShiftedBy(int x, int y)
        {
            return new IntegerPosition(X + x, Y + y);
        }

        public IntegerPosition ShiftedByX(int delta)
        {
            return new IntegerPosition(X + delta, Y);
        }

        public IntegerPosition ShiftedByY(int delta)
        {
            return new IntegerPosition(X, Y + delta);
        }

        public int X { get; private set; }
        public int Y { get; private set; }
    }
}

