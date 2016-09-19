//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//

namespace TermSharp.Misc
{
    public struct IntegerPosition
    {
        public IntegerPosition(int x, int y) : this()
        {
            X = x;
            Y = y;
        }

        public IntegerPosition ShiftedBy(int deltaX, int deltaY)
        {
            return new IntegerPosition(X + deltaX, Y + deltaY);
        }

        public IntegerPosition ShiftedByX(int delta)
        {
            return new IntegerPosition(X + delta, Y);
        }

        public IntegerPosition ShiftedByY(int delta)
        {
            return new IntegerPosition(X, Y + delta);
        }

        public IntegerPosition WithX(int x)
        {
            return new IntegerPosition(x, Y);
        }

        public IntegerPosition WithY(int y)
        {
            return new IntegerPosition(X, y);
        }

        public override string ToString()
        {
            return string.Format("[X={0}, Y={1}]", X, Y);
        }

        public override bool Equals(object obj)
        {
            return obj is IntegerPosition && this == (IntegerPosition)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return 23 * X + Y;
            }
        }

        public static bool operator ==(IntegerPosition a, IntegerPosition b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(IntegerPosition a, IntegerPosition b)
        {
            return !(a == b);
        }

        public int X { get; private set; }
        public int Y { get; private set; }
    }
}

