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
    public partial class Vt100Decoder
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
            Console.WriteLine("SGR with params {0}.", ParamsToString()); // TODO
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
                throw new NotImplementedException("Unimplemented erase display mode.");
            }
            terminal.EraseScreen(clearBegin, clearEnd);
        }

        private void EraseInLine()
        {
            var type = GetParamOrDefault(0, 0);
            var currentRow = terminal.GetScreenRow(terminal.Cursor.Position.Y);
            var textRow = currentRow as TextRow;
            if(textRow == null)
            {
                throw new InvalidOperationException(); // TODO
            }
            switch(type)
            {
            case 0:
                textRow.Erase(terminal.Cursor.Position.X, int.MaxValue);
                break;
            case 1:
                textRow.Erase(0, terminal.Cursor.Position.X);
                break;
            case 2:
                textRow.Erase(0, int.MaxValue);
                break;
            default:
                throw new NotImplementedException("Uimplemented erase line mode.");
            }
        }

        private void DeviceStatusReport()
        {
            if(currentParams[0] == null || currentParams[0].Value != 6)
            {
                return;
            }
            var response = new List<byte>();
            response.AddRange(new[] { (byte)ControlByte.Escape, (byte)ControlByte.Csi });
            response.AddRange(Encoding.ASCII.GetBytes(cursor.Position.Y.ToString() + ';' + cursor.Position.X.ToString()));
            SendResponse(response);
        }

        private void SetMode()
        {
            Console.WriteLine("Set mode with {0}.", ParamsToString()); // TODO
        }

        private void ResetMode()
        {
            Console.WriteLine("Reset mode with {0}.", ParamsToString()); // TODO
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

            commands.Add('m', SelectGraphicRendition);
            commands.Add('n', DeviceStatusReport);

            commands.Add('h', SetMode);
            commands.Add('l', ResetMode);
        }

        private readonly Dictionary<char, Action> commands;
    }
}

