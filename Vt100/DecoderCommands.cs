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
using Xwt.Drawing;

namespace TermSharp.Vt100
{
    public partial class Decoder
    {
        // all cursor limits are already handled in terminal

        private void CursorUp()
        {
            var delta = GetParamOrDefault(0, 1);
            cursor.Position = cursor.Position.ShiftedByY(-delta);
        }

        private void CursorDown()
        {
            var delta = GetParamOrDefault(0, 1);
            cursor.Position = cursor.Position.ShiftedByY(delta);
        }

        private void CursorLeft()
        {
            var delta = GetParamOrDefault(0, 1);
            if(cursorAtTheEndOfLine)
            {
                // we ignore one cursor left here
                delta--;
            }
            cursor.Position = cursor.Position.ShiftedByX(-delta);
        }

        private void CursorRight()
        {
            var delta = GetParamOrDefault(0, 1);
            cursor.Position = cursor.Position.ShiftedByX(delta);
        }

        private void CursorNextLine()
        {
            var delta = GetParamOrDefault(0, 1);
            cursor.Position = cursor.Position.ShiftedByY(delta).WithX(1);
        }

        private void CursorPreviousLine()
        {
            var delta = GetParamOrDefault(0, 1);
            cursor.Position = cursor.Position.ShiftedByY(-delta).WithX(1);
        }

        private void CursorHorizontalAbsolute()
        {
            var column = GetParamOrDefault(0, 1);
            cursor.Position = cursor.Position.WithX(column);
        }

        private void CursorVerticalAbsolute()
        {
            var row = GetParamOrDefault(0, 1);
            cursor.Position = cursor.Position.WithY(row);
        }

        private void CursorPosition()
        {
            var row = GetParamOrDefault(0, 1);
            var column = GetParamOrDefault(1, 1);
            cursor.Position = new IntegerPosition(column, row);
        }

        private void EraseDisplay()
        {
            var type = GetParamOrDefault(0, 0);
            var currentPosition = terminal.Cursor.Position;
            IntegerPosition clearBegin, clearEnd;
            switch(type)
            {
            case 0:
                clearBegin = currentPosition;
                clearEnd = terminal.Cursor.MaximalPosition;
                break;
            case 1:
                clearBegin = new IntegerPosition();
                clearEnd = currentPosition;
                break;
            case 2:
                clearBegin = new IntegerPosition();
                clearEnd = terminal.Cursor.MaximalPosition;
                cursor.Position = new IntegerPosition(0, 0);
                break;
            default:
                logger.Log("Unimplemented erase display mode.");
                return;
            }
            terminal.EraseScreen(clearBegin, clearEnd, graphicRendition.Background);
        }

        private void EraseCharacter()
        {
            var amount = GetParamOrDefault(0, 1);
            if(amount == 0)
            {
                amount = 1;
            }
            var currentRow = (MonospaceTextRow)terminal.GetScreenRow(terminal.Cursor.Position.Y);
            currentRow.Erase(terminal.Cursor.Position.X, terminal.Cursor.Position.X + amount - 1, graphicRendition.Background);
        }

        private void EraseInLine()
        {
            var type = GetParamOrDefault(0, 0);
            var currentRow = (MonospaceTextRow)terminal.GetScreenRow(terminal.Cursor.Position.Y);

            var screenRowBegin = (terminal.Cursor.Position.X / (currentRow.MaximalColumn + 1)) * (currentRow.MaximalColumn + 1);
            var screenRowEnd = screenRowBegin + currentRow.MaximalColumn + 1;

            switch(type)
            {
            case 0:
                currentRow.Erase(terminal.Cursor.Position.X, screenRowEnd, graphicRendition.Background);
                break;
            case 1:
                currentRow.Erase(screenRowBegin, terminal.Cursor.Position.X, graphicRendition.Background);
                break;
            case 2:
                currentRow.Erase(screenRowBegin, screenRowEnd, graphicRendition.Background);
                break;
            default:
                logger.Log("Unimplemented erase line mode.");
                break;
            }
        }

        private void DeviceStatusReport()
        {
            if(GetParamOrDefault(0, 0) != 6)
            {
                logger.Log(string.Format("Unsupported device status report with params {0}.", ParamsToString()));
                return;
            }
            var response = new List<byte>();
            response.AddRange(new[] { (byte)ControlByte.Escape, (byte)ControlByte.ControlSequenceIntroducer });
            response.AddRange(Encoding.ASCII.GetBytes(string.Format("{0};{1}R", cursor.Position.Y, cursor.Position.X)));
            SendResponse(response);
        }

        private void SaveCursorPosition()
        {
            savedGraphicRendition = graphicRendition.Clone();
            savedCursorPosition = cursor.Position;
        }

        private void RestoreCursorPosition()
        {
            graphicRendition = savedGraphicRendition.Clone();
            cursor.Position = savedCursorPosition;
        }

        private void DeviceAttributes()
        {
            if(privateModeCode || GetParamOrDefault(0, 0) != 0)
            {
                logger.Log(string.Format("Unsupported device attributes query with params {0}.", ParamsToString()));
                return;
            }
            SendResponse(new[] { (byte)ControlByte.LineFeed, (byte)ControlByte.Escape, (byte)ControlByte.ControlSequenceIntroducer }.Union(Encoding.ASCII.GetBytes("?1;2c")));
        }

        private void SetMode()
        {
            if(privateModeCode)
            {
                switch(GetParamOrDefault(0, 0))
                {
                    case 5:
                        graphicRendition.Negative = true;
                        return;
                    case 7:
                        // Enable wraparound mode - we do it by default, have no option to turn it off
                        // This is effectively a no-op.
                        return;
                    case 25:
                        terminal.Cursor.Enabled = true;
                        return;
                }
            }
            logger.Log(string.Format("Unimplemented mode set with params {0}.", ParamsToString()));
        }

        private void ResetMode()
        {
            if(privateModeCode)
            {
                switch(GetParamOrDefault(0, 0))
                {
                    case 4:
                        // Disable insert mode. We do not handle this mode, so this is a no-op
                        return;
                    case 5:
                        graphicRendition.Negative = false;
                        return;
                    case 25:
                        terminal.Cursor.Enabled = false;
                        return;
                }
            }
            logger.Log(string.Format("Unimplemented mode reset with params {0}.", ParamsToString()));
        }

        private int GetParamOrDefault(int index, int defaultValue)
        {
            if(currentParams.Length <= index)
            {
                return defaultValue;
            }
            return currentParams[index] ?? defaultValue;
        }

        private string ParamsToString()
        {
            return currentParams.Select(x => x == null ? "(default)" : x.Value.ToString()).Aggregate((x, y) => x + "; " + y);
        }

        private void SendResponse(IEnumerable<byte> response)
        {
            foreach(var b in response)
            {
                responseCallback(b);
            }
        }

        private void InitializeCommands()
        {
            commands.Add('A', CursorUp);
            commands.Add('B', CursorDown);
            commands.Add('C', CursorRight);
            commands.Add('D', CursorLeft);
            commands.Add('E', CursorNextLine);
            commands.Add('F', CursorPreviousLine);
            commands.Add('G', CursorHorizontalAbsolute);
            commands.Add('H', CursorPosition);
            commands.Add('f', CursorPosition);
            commands.Add('J', EraseDisplay);
            commands.Add('K', EraseInLine);
            commands.Add('X', EraseCharacter);

            commands.Add('m', () => graphicRendition.HandleSgr());
            commands.Add('n', DeviceStatusReport);
            commands.Add('s', SaveCursorPosition);
            commands.Add('u', RestoreCursorPosition);

            commands.Add('h', SetMode);
            commands.Add('l', ResetMode);
            commands.Add('c', DeviceAttributes);
            commands.Add('d', CursorVerticalAbsolute);

            commands.Add('`', CursorHorizontalAbsolute);
        }

        private readonly Dictionary<char, Action> commands;

        private sealed class GraphicRendition
        {
            public GraphicRendition(Decoder parent)
            {
                this.parent = parent;
            }

            public void Reset()
            {
                Foreground = null;
                Background = null;
                Negative = false;
                Bright = false;
            }

            public GraphicRendition Clone()
            {
                return (GraphicRendition)MemberwiseClone();
            }

            // Codes based on https://en.wikipedia.org/wiki/ANSI_escape_code
            public void HandleSgr()
            {
                for(var i = 0; i < parent.currentParams.Length; i++)
                {
                    var parameter = parent.currentParams[i];
                    if(!parameter.HasValue)
                    {
                        // no parameter is interpreted as 0
                        // todo: analyze if this change can be made more general, in all ANSI codes
                        parameter = 0;
                    }
                    var value = parameter.Value;
                    Action<GraphicRendition> handler;
                    if(SgrHandlers.TryGetValue(value, out handler))
                    {
                        handler(this);
                    }
                    else if(value >= 30 && value <= 37)
                    {
                        Foreground = GetConsolePaletteColor(value - 30);
                    }
                    else if(value >= 40 && value <= 47)
                    {
                        Background = GetConsolePaletteColor(value - 40);
                    }
                    else if(value == 38)
                    {
                        i++;
                        Foreground = GetExtendedColor(ref i);
                    }
                    else if(value == 48)
                    {
                        i++;
                        Background = GetExtendedColor(ref i);
                    }
                    else
                    {
                        parent.logger.Log(string.Format("Unimplemented SGR code {0}.", value));
                    }
                }
            }

            public Color? Foreground
            {
                get
                {
                    return foreground;
                }
                set
                {
                    foreground = value;
                    Refresh();
                }
            }

            public Color? Background
            {
                get
                {
                    return background;
                }
                set
                {
                    background = value;
                    Refresh();
                }
            }

            public bool Bright
            {
                get
                {
                    return bright;
                }
                set
                {
                    bright = value;
                    Refresh();
                }
            }

            public bool Negative
            {
                get
                {
                    return negative;
                }
                set
                {
                    negative = value;
                    Refresh();
                }
            }

            public Color? EffectiveForeground { get; private set; }

            public Color? EffectiveBackground { get; private set; }

            private Color GetConsolePaletteColor(int number)
            {
                switch(number)
                {
                case 0:
                    return Colors.Black;
                case 1:
                    return Colors.DarkRed;
                case 2:
                    return Colors.DarkGreen;
                case 3:
                    return Colors.Brown;
                case 4:
                    return Colors.DarkBlue;
                case 5:
                    return Colors.DarkMagenta;
                case 6:
                    return Colors.DarkCyan;
                case 7:
                    return Terminal.DefaultGray;
                }
                parent.logger.Log("Unexpected palette color number.");
                return Colors.Black;
            }

            private Color GetExtendedColor(ref int baseParameterIndex)
            {
                switch(parent.GetParamOrDefault(baseParameterIndex, 0))
                {
                case 2:
                    baseParameterIndex += 3;
                    return new Color(parent.GetParamOrDefault(baseParameterIndex - 2, 0) / 255.0, parent.GetParamOrDefault(baseParameterIndex - 1, 0) / 255.0, parent.GetParamOrDefault(baseParameterIndex, 0) / 255.0);
                case 5:
                    baseParameterIndex++;
                    var index = parent.GetParamOrDefault(baseParameterIndex, 0);
                    if(index >= 0 && index <= 15)
                    {
                        var color = GetConsolePaletteColor(index % 8);
                        if(index > 7)
                        {
                            color = Brighten(color);
                        }
                        return color;
                    }
                    else if(index < 0xE7)
                    {
                        index -= 16;
                        var r = ToColorTable((index / 36) % 6);
                        var g = ToColorTable((index / 6) % 6);
                        var b = ToColorTable(index % 6);
                        return new Color(r / 255.0, g / 255.0, b / 255.0);
                    }
                    else
                    {
                        // colors taken from https://jonasjacek.github.io/colors/
                        var intensity = 0x8 + (index - 0xE8) * 0xA;
                        return new Color(intensity / 255.0, intensity / 255.0, intensity / 255.0);
                    }
                default:
                    parent.logger.Log("Unimplemented extended color mode.");
                    return Colors.Black;
                }
            }

            private void Refresh()
            {
                var currentForeground = foreground ?? parent.terminal.DefaultForeground;
                var currentBackground = background ?? parent.terminal.DefaultBackground;
                if(Bright)
                {
                    currentForeground = Brighten(currentForeground);
                }
                if(Negative)
                {
                    var temporary = currentForeground;
                    currentForeground = currentBackground;
                    currentBackground = temporary;
                }
                EffectiveForeground = currentForeground == parent.terminal.DefaultForeground ? default(Color?) : currentForeground;
                EffectiveBackground = currentBackground == parent.terminal.DefaultBackground ? default(Color?) : currentBackground;
            }

            private static Color Brighten(Color value)
            {
                Color result;
                if(!DefinedBrightColors.TryGetValue(value, out result))
                {
                    result = value.WithIncreasedLight(0.3);
                }
                return result;
            }

            private static int ToColorTable(int index)
            {
                if(index > 0)
                {
                    index = 0x5f + (index - 1) * 0x28;
                }
                return index;
            }

            private Color? foreground;
            private Color? background;
            private bool bright;
            private bool negative;

            private readonly Decoder parent;

            private static readonly Dictionary<Color, Color> DefinedBrightColors = new Dictionary<Color, Color>
            {
                { Colors.Black, Colors.DarkGray },
                { Colors.DarkRed, Colors.Red },
                { Colors.DarkGreen, Colors.Green },
                { Colors.Brown, Colors.Yellow },
                { Colors.DarkBlue, Colors.Blue },
                { Colors.DarkMagenta, Colors.Magenta },
                { Colors.DarkCyan, Colors.Cyan },
                { Terminal.DefaultGray, Colors.White }
            };

            private static readonly Dictionary<int, Action<GraphicRendition>> SgrHandlers = new Dictionary<int, Action<GraphicRendition>>
            {
                { 0, x => x.Reset() },
                { 1, x => x.Bright = true },
                { 7, x => x.Negative = true },
                { 10, x => { /* Select primary font. We don't support font switching, so this is always no-op */ }},
                { 21, x => x.Bright = false },
                { 24, x => { /* Underline off. We don't handle underline yet, so this is always no-op */ }},
                { 27, x => x.Negative = false },
                { 39, x => x.Foreground = null },
                { 49, x => x.Background = null }
            };
        }
    }
}
