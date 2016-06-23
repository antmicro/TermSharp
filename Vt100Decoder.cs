//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terminal
{
    public sealed partial class Vt100Decoder
    {
        public Vt100Decoder(Terminal terminal)
        {
            this.terminal = terminal;
            commands = new Dictionary<char, Action>();
            InitializeCommands();
            cursor = new Cursor(this);
            terminal.AppendRow(new TextRow(""));
        }

        public void Feed(char c)
        {
            if(inAnsiCode)
            {
                HandleAnsiCode(c);
            }
            else if(ControlByte.Escape == (ControlByte)c)
            {
                inAnsiCode = true;
                return;
            }
            else if(ControlByte.LineFeed == (ControlByte)c)
            {
                terminal.AppendRow(new TextRow(string.Empty));
                var newPosition = cursor.Position.WithX(1);
                newPosition = newPosition.ShiftedByY(1);
                cursor.Position = newPosition;
            }
            else
            {
                HandleRegularCharacter(c);
            }
        }

        public void Feed(string str)
        {
            foreach(var b in str)
            {
                Feed(b);
            }
        }

        private void InsertCharacterAt(IntegerPosition where, char what)
        {
            var textRow = terminal.GetScreenRow(where.Y - 1) as TextRow;
            if(textRow == null)
            {
                throw new InvalidOperationException(); // TODO
            }
            var builder = new StringBuilder(textRow.Content, where.X);
            for(var i = builder.Length; i < where.X; i++)
            {
                builder.Append(' ');
            }
            builder[where.X - 1] = what;
            textRow.Content = builder.ToString();
        }

        private void HandleRegularCharacter(char c)
        {
            InsertCharacterAt(cursor.Position, c);
            cursor.Position = cursor.Position.ShiftedByX(1);
            terminal.Cursor.StayOnForNBlinks(1); // TODO: value
            terminal.Redraw();
        }

        private void HandleAnsiCode(char c)
        {
            if(csiCodeData == null)
            {
                if(ControlByte.Csi != (ControlByte)c)
                {
                    throw new NotImplementedException();
                }
                csiCodeData = new StringBuilder();
                return;
            }
            if(ControlByte.Escape == (ControlByte)c)
            {
                throw new NotImplementedException();
            }
            if(char.IsLetter(c))
            {
                if(commands.ContainsKey(c))
                {
                    // let's extract parameters
                    var splitted = csiCodeData.ToString().Split(';');
                    currentParams = splitted.Select(x => string.IsNullOrEmpty(x) ? (int?)null : int.Parse(x)).ToArray();
                    commands[c]();
                    inAnsiCode = false;
                    csiCodeData = null;
                }
                else
                {
                    throw new NotImplementedException(); // TODO
                }
            }
            else
            {
                csiCodeData.Append(c);
            }
        }

        private int?[] currentParams;
        private bool inAnsiCode;
        private StringBuilder csiCodeData;
        private readonly Terminal terminal;
        private readonly Cursor cursor;

        private sealed class Cursor
        {
            public Cursor(Vt100Decoder parent)
            {
                this.parent = parent;
            }

            public int CurrentRowNumber
            {
                get
                {
                    return parent.terminal.Cursor.Position.Y + 1;
                }
            }

            public IntegerPosition Position
            {
                get
                {
                    return parent.terminal.Cursor.Position.ShiftedBy(1, 1);
                }
                set
                {
                    parent.terminal.Cursor.Position = value.ShiftedBy(-1, -1);
                }
            }

            private readonly Vt100Decoder parent;
        }
    }
}

