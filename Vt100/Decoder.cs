//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TermSharp.Misc;
using TermSharp.Rows;
using Xwt.Drawing;

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
            receiveState = ReceiveState.Default;
        }

        public void Feed(string textElement)
        {
            if(receiveState == ReceiveState.IgnoreNextChar)
            {
                receiveState = ReceiveState.Default;
                return;
            }
            terminal.Cursor.StayOnForNBlinks(CharReceivedBlinkDisabledRounds);
            if(textElement.Length == 1)
            {
                var c = textElement[0];
                if(receiveState == ReceiveState.EatUpToBell)
                {
                    if((ControlByte)c == ControlByte.Bell)
                    {
                        receiveState = ReceiveState.Default;
                    }
                }
                else if(receiveState == ReceiveState.AnsiCode)
                {
                    HandleAnsiCode(c);
                }
                else if(receiveState == ReceiveState.SystemCommandNumber)
                {
                    HandleSystemCommandCode(c);
                }
                else if(receiveState == ReceiveState.Image)
                {
                    HandleReceiveImage(c);
                }
                else if(ControlByte.Backspace == (ControlByte)c)
                {
                    currentParams = new int?[] { 1 };
                    CursorLeft();
                }
                else if(ControlByte.Escape == (ControlByte)c)
                {
                    receiveState = ReceiveState.AnsiCode;
                }
                else if(ControlByte.LineFeed == (ControlByte)c)
                {
                    HandleLineFeed();
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
            var row = terminal.GetScreenRow(terminal.Cursor.Position.Y, true);
            if(row is ImageRow)
            {
                logger.Log($"Tried to insert character at the top of the image");
                return;
            }
            if(!(row is MonospaceTextRow textRow))
            {
                throw new InvalidOperationException($"MonospaceTextRow expected but {row.GetType().Name} type found.");
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
            var maximalColumn = terminal.GetScreenRow(terminal.Cursor.Position.Y).MaximalColumn;
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
            if(ControlByte.OperatingSystemCommand == (ControlByte)c)
            {
                receiveState = ReceiveState.SystemCommandNumber;
                systemCommandNumber = new StringBuilder();
                return;
            }
            if(csiCodeData == null)
            {
                if(ControlByte.ControlSequenceIntroducer != (ControlByte)c)
                {
                    HandleNonCsiCode(c);
                    receiveState = ReceiveState.Default;
                    privateModeCode = false;
                    return;
                }
                csiCodeData = new StringBuilder();
                return;
            }
            if(ControlByte.Escape == (ControlByte)c)
            {
                logger.Log("Escape character within ANSI code.");
                receiveState = ReceiveState.Default;
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
                    receiveState = ReceiveState.Default;
                    privateModeCode = false;
                    csiCodeData = null;
                }
                else
                {
                    logger.Log(string.Format("Unimplemented ANSI code {0}, data {1}.", c, csiCodeData));
                    receiveState = ReceiveState.Default;
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
                receiveState = ReceiveState.IgnoreNextChar;
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

        private void HandleLineFeed()
        {
            var oldY = cursor.Position.Y;
            cursor.Position = cursor.Position.ShiftedByY(1);
            if(oldY == cursor.Position.Y)
            {
                terminal.AppendRow(new MonospaceTextRow(string.Empty), true);
                cursor.Position = cursor.Position.ShiftedByY(1);
            }
        }

        private void HandleSystemCommandCode(char c)
        {
            if(char.IsDigit(c))
            {
                systemCommandNumber.Append(c);
                return;
            }

            if(c == ';')
            {
                if(!int.TryParse(systemCommandNumber.ToString(), out var codeNumber))
                {
                    logger.Log($"Couldn't parse the system command number: '{(systemCommandNumber.ToString())}'");
                    receiveState = ReceiveState.Default;
                    return;
                }

                if(codeNumber == InlineImageCode)
                {
                    receiveState = ReceiveState.Image;
                    base64ImageBuilder = new StringBuilder();
                }
                else
                {
                    logger.Log($"Not supported System Command Code 0x{0:X}. Ignoring the rest of the control code", codeNumber);
                    receiveState = ReceiveState.EatUpToBell;
                }
                systemCommandNumber = null;
                return;
            }

            logger.Log($"Unexpected character '{c}' in System Command Number.");
            receiveState = ReceiveState.Default;
        }

        private void HandleReceiveImage(char c)
        {
            if(ControlByte.Bell != (ControlByte)c)
            {
                base64ImageBuilder.Append(c);
            }
            else
            {
                if(!Vt100ITermFileEscapeCodeHandler.TryParse(base64ImageBuilder.ToString(), out var handler))
                {
                    logger.Log(handler.Error);
                    base64ImageBuilder = null;
                    receiveState = ReceiveState.Default;
                    return;
                }

                DrawImage(handler.Image);

                base64ImageBuilder = null;
                receiveState = ReceiveState.Default;
            }
        }

        private void DrawImage(Image image)
        {
            var imageRow = new ImageRow(image);
            terminal.AppendRow(imageRow, true);
            cursor.Position = cursor.Position.ShiftedByY(imageRow.SublineCount);
            HandleLineFeed();
        }

        private const int InlineImageCode = 1337;

        private ReceiveState receiveState;
        private GraphicRendition graphicRendition;
        private GraphicRendition savedGraphicRendition;
        private IntegerPosition savedCursorPosition;
        private int?[] currentParams;
        private bool privateModeCode;
        private bool cursorAtTheEndOfLine;
        private StringBuilder csiCodeData;
        private StringBuilder systemCommandNumber;
        private StringBuilder base64ImageBuilder;
        private readonly Terminal terminal;
        private readonly Cursor cursor;
        private readonly Action<byte> responseCallback;
        private readonly IDecoderLogger logger;

        private enum ReceiveState
        {
            Default,
            IgnoreNextChar,
            AnsiCode,
            SystemCommandNumber,
            Image,
            EatUpToBell
        }

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
                        resultY += parent.terminal.GetScreenRow(i).SublineCount;
                    }

                    // it can happen that the first row is partially hidden
                    // our vt100 cursor should be counted from the first completely displayed subrow of the first row
                    double hiddenHeight;
                    var firstRow = parent.terminal.GetFirstScreenRow(out hiddenHeight);
                    resultY -= (int)Math.Ceiling(hiddenHeight / firstRow.LineHeight);

                    // it can happen that normal cursor is not in vt100 cursor range which gives us negative result here
                    // in such case we report Y = 0
                    if(resultY < 0)
                    {
                        resultY = 0;
                    }

                    var charsInRow = parent.terminal.GetScreenRow(terminalPosition.Y).MaximalColumn + 1;
                    var resultX = terminalPosition.X % charsInRow;
                    resultY += terminalPosition.X / charsInRow;
                    return new IntegerPosition(resultX + 1, resultY + 1);
                }
                set
                {
                    parent.cursorAtTheEndOfLine = false;

                    double hiddenPart;
                    var firstRow = parent.terminal.GetFirstScreenRow(out hiddenPart);

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
                            // append dummy row to calculate proper position
                            parent.terminal.AppendRow(new MonospaceTextRow(""));
                        }

                        vt100Y -= parent.terminal.GetScreenRow(resultY).SublineCount;
                    }
                    var row = parent.terminal.GetScreenRow(resultY);
                    var resultX = (row.SublineCount - 1 + vt100Y) * (row.MaximalColumn + 1) + value.X - 1;
                    parent.terminal.Cursor.Position = new IntegerPosition(resultX, resultY);
                }
            }

            private readonly Decoder parent;
        }
    }
}

