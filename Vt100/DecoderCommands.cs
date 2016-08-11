//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Misc;
using Terminal.Rows;
using Xwt.Drawing;

namespace Terminal.Vt100
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

        private void CursorPosition()
        {
            var row = GetParamOrDefault(0, 1);
            var column = GetParamOrDefault(1, 1);
            cursor.Position = cursor.Position.WithY(row).WithX(column);
        }

        private void SelectGraphicRendition()
        {
            var onlyZeros = true;

            var bright = false;
            var foregroundColorIndex = -1;
            var backgroundColorIndex = -1;
            var swapColors = false;
            for(var i = 0; i < currentParams.Length; i++)
            {
                var parameter = currentParams[i];
                if(!parameter.HasValue)
                {
                    continue;
                }
                var value = parameter.Value;
                if(value != 0)
                {
                    onlyZeros = false;
                }
                if(value == 1)
                {
                    bright = true;
                }
                else if(value >= 30 && value <= 37)
                {
                    foregroundColorIndex = value - 30;
                }
                else if(value >= 40 && value <= 47)
                {
                    backgroundColorIndex = value - 40;
                }
                else if(value == 7)
                {
                    swapColors = true;
                }
                else if(value == 38)
                {
                    i++;
                    CurrentForeground = GetExtendedColor(ref i);
                }
                else if(value == 48)
                {
                    i++;
                    CurrentBackground = GetExtendedColor(ref i);
                }
                else if(value != 0)
                {
                    logger.Log(string.Format("Unimplemented SGR code {0}.", value));
                }
            }
            if(onlyZeros)
            {
                CurrentForeground = null;
                CurrentBackground = null;
                return;
            }
            if(foregroundColorIndex != -1)
            {
                CurrentForeground = GetConsolePaletteColor(!bright, foregroundColorIndex);
            }
            if(backgroundColorIndex != -1)
            {
                CurrentBackground = GetConsolePaletteColor(true, backgroundColorIndex);
            }
            if(swapColors)
            {
                var oldBackground = CurrentBackground ?? terminal.DefaultBackground;
                CurrentBackground = CurrentForeground ?? terminal.DefaultForeground;
                CurrentForeground = oldBackground;
            }
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
                break;
            default:
                logger.Log("Unimplemented erase display mode.");
                return;
            }
            terminal.EraseScreen(clearBegin, clearEnd, CurrentBackground);
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
                currentRow.Erase(terminal.Cursor.Position.X, screenRowEnd, CurrentBackground);
                break;
            case 1:
                currentRow.Erase(screenRowBegin, terminal.Cursor.Position.X, CurrentBackground);
                break;
            case 2:
                currentRow.Erase(screenRowBegin, screenRowEnd, CurrentBackground);
                break;
            default:
                logger.Log("Uimplemented erase line mode.");
                break;
            }
        }

        private void DeviceStatusReport()
        {
            if(GetParamOrDefault(0, 0) != 6)
            {
                return;
            }
            var response = new List<byte>();
            response.AddRange(new[] { (byte)ControlByte.Escape, (byte)ControlByte.Csi });
            response.AddRange(Encoding.ASCII.GetBytes(cursor.Position.Y.ToString() + ';' + cursor.Position.X + 'R'));
            SendResponse(response);
        }

        private void SaveCursorPosition()
        {
            savedCursorPosition = cursor.Position;
            savedForeground = CurrentForeground;
            savedBackground = CurrentBackground;
        }

        private void RestoreCursorPosition()
        {
            cursor.Position = savedCursorPosition;
            CurrentForeground = savedForeground;
            CurrentBackground = savedBackground;
        }

        private void DeviceAttributes()
        {
            if(privateModeCode || GetParamOrDefault(0, 0) != 0)
            {
                return;
            }
            SendResponse(new[] { (byte)ControlByte.LineFeed, (byte)ControlByte.Escape, (byte)ControlByte.Csi }.Union(Encoding.ASCII.GetBytes("?1;2c")));
        }

        private void SetMode()
        {
            if(privateModeCode && GetParamOrDefault(0, 0) == 25)
            {
                terminal.Cursor.Enabled = true;
                return;
            }
            logger.Log(string.Format("Unimplemented mode set with params {0}.", ParamsToString()));
        }

        private void ResetMode()
        {
            if(privateModeCode && GetParamOrDefault(0, 0) == 25)
            {
                terminal.Cursor.Enabled = false;
                return;
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

        private Color GetConsolePaletteColor(bool dark, int number)
        {
            switch(number)
            {
            case 0:
                return dark ? Colors.Black : Colors.DarkGray;
            case 1:
                return dark ? Colors.DarkRed : Colors.Red;
            case 2:
                return dark ? Colors.DarkGreen : Colors.Green;
            case 3:
                return dark ? Colors.Brown : Colors.Yellow;
            case 4:
                return dark ? Colors.DarkBlue : Colors.Blue;
            case 5:
                return dark ? Colors.DarkMagenta : Colors.Magenta;
            case 6:
                return dark ? Colors.DarkCyan : Colors.Cyan;
            case 7:
                return dark ? Colors.LightGray : Colors.White;
            }
            logger.Log("Unexpected palette color number.");
            return Colors.Black;
        }

        private Color GetExtendedColor(ref int baseParameterIndex)
        {
            switch(GetParamOrDefault(baseParameterIndex, 0))
            {
            case 2:
                baseParameterIndex += 3;
                return new Color(GetParamOrDefault(baseParameterIndex - 2, 0)/255.0, GetParamOrDefault(baseParameterIndex - 1, 0)/255.0, GetParamOrDefault(baseParameterIndex, 0)/255.0);
            case 5:
                baseParameterIndex++;
                var index = GetParamOrDefault(baseParameterIndex, 0);
                if(index >= 0 && index <= 15)
                {
                    return GetConsolePaletteColor(index > 7, index % 8);
                }
                else if(index < 0xE7)
                {
                    index -= 16;
                    return new Color(((index / 36) % 6) / 5.0, ((index / 6) % 6) / 5.0, (index % 6) / 5.0);
                }
                else
                {
                    index -= 0xE7;
                    return new Color(index / 24.0, index / 24.0, index / 24.0);
                }
            default:
                logger.Log("Unimplemented extended color mode.");
                return Colors.Black;
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

            commands.Add('m', SelectGraphicRendition);
            commands.Add('n', DeviceStatusReport);
            commands.Add('s', SaveCursorPosition);
            commands.Add('u', RestoreCursorPosition);

            commands.Add('h', SetMode);
            commands.Add('l', ResetMode);
            commands.Add('c', DeviceAttributes);
        }

        private readonly Dictionary<char, Action> commands;
    }
}

