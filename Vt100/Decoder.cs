//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TermSharp.Misc;
using TermSharp.Rows;

namespace TermSharp.Vt100
{
    public sealed partial class Decoder
    {
        public Decoder(Terminal terminal, Action<byte> responseCallback, IDecoderLogger logger)
        {
            this.terminal = terminal;
            this.responseCallback = responseCallback;
            this.logger = logger;
            commands = new Dictionary<char, Action>();
            graphicRendition = new GraphicRendition(this);
            savedGraphicRendition = graphicRendition.Clone();
            InitializeCommands();
            cursor = new Cursor(this);
            CharReceivedBlinkDisabledRounds = 1;
        }

        public void Feed(string textElement)
        {
            if(ignoreNextChar)
            {
                ignoreNextChar = false;
                return;
            }
            terminal.Cursor.StayOnForNBlinks(CharReceivedBlinkDisabledRounds);
            if(textElement.Length == 1)
            {
                var c = textElement[0];
                if(inAnsiCode)
                {
                    HandleAnsiCode(c);
                }
                else if(ControlByte.Backspace == (ControlByte)c)
                {
                    currentParams = new int?[] { 1 };
                    CursorLeft();
                }
                else if(ControlByte.Escape == (ControlByte)c)
                {
                    inAnsiCode = true;
                }
                else if(ControlByte.LineFeed == (ControlByte)c)
                {
                    var oldY = cursor.Position.Y;
                    cursor.Position = cursor.Position.ShiftedByY(1);
                    if(oldY == cursor.Position.Y)
                    {
                        terminal.AppendRow(new MonospaceTextRow(string.Empty), true);
                        cursor.Position = cursor.Position.ShiftedByY(1);
                    }
                }
                else if(ControlByte.CarriageReturn == (ControlByte)c)
                {
                    cursor.Position = cursor.Position.WithX(0);
                }
                else if(ControlByte.Bell == (ControlByte)c)
                {
                    var bellReceived = BellReceived;
                    if(bellReceived != null)
                    {
                        bellReceived();
                    }
                }
                else if(ControlByte.HorizontalTab == (ControlByte)c)
                {
                    HandleRegularCharacter(" ");
                }
                else if(ControlByte.LockShiftG0 == (ControlByte)c || ControlByte.LockShiftG1 == (ControlByte)c)
                {
                    //ignore, as we do not support character set switching
                }
                else if(char.IsControl(c))
                {
                    if(c > 0 && c < 32)
                    {
                        Feed("^");
                        Feed(((char)(c + 64)).ToString());
                    }
                    else if(c != 0 && c != 127) // intentionally do nothing for NULL/DEL characters
                    {
                        logger.Log(string.Format("Unimplemented control character 0x{0:X}.", (int)c));
                    }
                }
                else
                {
                    HandleRegularCharacter(textElement);
                }
            }
            else
            {
                HandleRegularCharacter(textElement);
            }
            terminal.Redraw();
        }

        public int CharReceivedBlinkDisabledRounds { get; set; }

        public event Action BellReceived;

        private void InsertCharacterAtCursor(string textElement)
         {
            var textRow = terminal.GetScreenRow(terminal.Cursor.Position.Y, true) as MonospaceTextRow;
            if(textRow == null)
            {
                throw new InvalidOperationException("MonospaceTextRow expected but other type found.");
            }
            if(textRow.PutCharacterAt(terminal.Cursor.Position.X, textElement, graphicRendition.EffectiveForeground, graphicRendition.EffectiveBackground))
            {
                terminal.Refresh();
            }
        }

        private void HandleRegularCharacter(string textElement)
        {
            var oldPosition = terminal.Cursor.Position;
            if(cursorAtTheEndOfLine)
            {
                terminal.Cursor.Position = terminal.Cursor.Position.ShiftedByX(1);
                cursorAtTheEndOfLine = false;
            }
            InsertCharacterAtCursor(textElement);
            var maximalColumn = ((MonospaceTextRow)terminal.GetScreenRow(terminal.Cursor.Position.Y)).MaximalColumn;
            if(terminal.Cursor.Position.X % (maximalColumn + 1) != maximalColumn)
            {
                terminal.Cursor.Position = terminal.Cursor.Position.ShiftedByX(1);
            }
            else
            {
                cursorAtTheEndOfLine = true;
            }
        }

        private void HandleAnsiCode(char c)
        {
            if(csiCodeData == null)
            {
                if(ControlByte.ControlSequenceIntroducer != (ControlByte)c)
                {
                    HandleNonCsiCode(c);
                    inAnsiCode = false;
                    privateModeCode = false;
                    return;
                }
                csiCodeData = new StringBuilder();
                return;
            }
            if(ControlByte.Escape == (ControlByte)c)
            {
                logger.Log("Escape character within ANSI code.");
                inAnsiCode = false;
                privateModeCode = false;
                csiCodeData = null;
                return;
            }
            if(char.IsLetter(c))
            {
                if(commands.ContainsKey(c))
                {
                    // let's extract parameters
                    var splitted = csiCodeData.ToString().Split(';');
                    var parsed = new List<int?>();
                    foreach(var s in splitted)
                    {
                        if(string.IsNullOrEmpty(s))
                        {
                            parsed.Add(null);
                        }
                        else if(int.TryParse(s, out var i))
                        {
                            parsed.Add(i);
                        }
                        else
                        {
                            logger.Log($"Broken ANSI code data for command '{c}': '{csiCodeData}'");
                            parsed = null;
                            break;
                        }
                    }
                    // parsed is set to null when broken ANSI code is detected
                    if(parsed != null)
                    {
                        currentParams = parsed.ToArray();
                        commands[c]();
                    }
                    inAnsiCode = false;
                    privateModeCode = false;
                    csiCodeData = null;
                }
                else
                {
                    logger.Log(string.Format("Unimplemented ANSI code {0}, data {1}.", c, csiCodeData));
                    inAnsiCode = false;
                    csiCodeData = null;
                    privateModeCode = false;
                }
            }
            else
            {
                if(c != '?')
                {
                    csiCodeData.Append(c);
                }
                else
                {
                    privateModeCode = true;
                }
            }
        }

        private void HandleNonCsiCode(char c)
        {
            switch(c)
            {
            case 'c':
                HandleTerminalReset();
                break;
            case '(':
            case ')':
            case '*':
            case '+':
                // G0-G3 character set, we ignore this, at least for now
                ignoreNextChar = true;
                break;
            case '7':
                SaveCursorPosition();
                break;
            case '8':
                RestoreCursorPosition();
                break;
            default:
                logger.Log(string.Format("Unimplemented non-CSI code '{0}'.", c));
                break;
            }
        }

        private void HandleTerminalReset()
        {
            terminal.Cursor.Enabled = true;
            graphicRendition.Reset();
            var screenRows = terminal.ScreenRowCount;
            for(var i = 0; i < screenRows; i++)
            {
                terminal.AppendRow(new MonospaceTextRow(string.Empty));
            }
            terminal.Cursor.Position = new IntegerPosition();
        }

        private bool ignoreNextChar;
        private GraphicRendition graphicRendition;
        private GraphicRendition savedGraphicRendition;
        private IntegerPosition savedCursorPosition;
        private int?[] currentParams;
        private bool inAnsiCode;
        private bool privateModeCode;
        private bool cursorAtTheEndOfLine;
        private StringBuilder csiCodeData;
        private readonly Terminal terminal;
        private readonly Cursor cursor;
        private readonly Action<byte> responseCallback;
        private readonly IDecoderLogger logger;

        private sealed class Cursor
        {
            public Cursor(Decoder parent)
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
                    var terminalPosition = parent.terminal.Cursor.Position;
                    var resultY = 0;
                    for(var i = 0; i < terminalPosition.Y; i++)
                    {
                        resultY += ((MonospaceTextRow)parent.terminal.GetScreenRow(i)).SublineCount;
                    }

                    // it can happen that the first row is partially hidden
                    // our vt100 cursor should be counted from the first completely displayed subrow of the first row
                    double hiddenHeight;
                    var firstRow = (MonospaceTextRow)parent.terminal.GetFirstScreenRow(out hiddenHeight);
                    resultY -= (int)Math.Ceiling(hiddenHeight / firstRow.LineHeight);

                    // it can happen that normal cursor is not in vt100 cursor range which gives us negative result here
                    // in such case we report Y = 0
                    if(resultY < 0)
                    {
                        resultY = 0;
                    }

                    var charsInRow = ((MonospaceTextRow)parent.terminal.GetScreenRow(terminalPosition.Y)).MaximalColumn + 1;
                    var resultX = terminalPosition.X % charsInRow;
                    resultY += terminalPosition.X / charsInRow;
                    return new IntegerPosition(resultX + 1, resultY + 1);
                }
                set
                {
                    parent.cursorAtTheEndOfLine = false;

                    double hiddenPart;
                    var firstRow = (MonospaceTextRow)parent.terminal.GetFirstScreenRow(out hiddenPart);

                    var maxX = firstRow.MaximalColumn + 1;
                    var maxY = (int)Math.Floor(parent.terminal.ScreenSize / firstRow.LineHeight);
                    value = new IntegerPosition(Math.Min(value.X, maxX), Math.Min(value.Y, maxY));
                    value = new IntegerPosition(Math.Max(value.X, 1), Math.Max(value.Y, 1));

                    var resultY = 0;
                    var vt100Y = value.Y;

                    // in the case of first row we only count its visible part
                    vt100Y -= firstRow.SublineCount - (int)Math.Ceiling(hiddenPart / firstRow.LineHeight);
                    while(vt100Y > 0)
                    {
                        resultY++;
                        if(resultY >= parent.terminal.ScreenRowCount)
                        {
                            parent.terminal.AppendRow(new MonospaceTextRow(""));
                        }

                        vt100Y -= ((MonospaceTextRow)parent.terminal.GetScreenRow(resultY)).SublineCount;
                    }
                    var row = (MonospaceTextRow)parent.terminal.GetScreenRow(resultY);
                    var resultX = (row.SublineCount - 1 + vt100Y) * (row.MaximalColumn + 1) + value.X - 1;
                    parent.terminal.Cursor.Position = new IntegerPosition(resultX, resultY);
                }
            }

            private readonly Decoder parent;
        }
    }
}

