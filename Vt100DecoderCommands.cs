//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;

namespace Terminal
{
    public partial class Vt100Decoder
    {
        // all cursor limits are already handled in terminal

        private void CursorUp()
        {
            var delta = currentParams[0] ?? 1;
            cursor.Position = cursor.Position.ShiftedByY(-delta);
        }

        private void CursorDown()
        {
            var delta = currentParams[0] ?? 1;
            cursor.Position = cursor.Position.ShiftedByY(delta);
        }

        private void CursorLeft()
        {
            var delta = currentParams[0] ?? 1;
            cursor.Position = cursor.Position.ShiftedByX(-delta);
        }

        private void CursorRight()
        {
            var delta = currentParams[0] ?? 1;
            cursor.Position = cursor.Position.ShiftedByX(delta);
        }

        private void CursorNextLine()
        {
            var delta = currentParams[0] ?? 1;
            cursor.Position = cursor.Position.ShiftedByY(delta).WithX(1);
        }

        private void CursorPreviousLine()
        {
            var delta = currentParams[0] ?? 1;
            cursor.Position = cursor.Position.ShiftedByY(-delta).WithX(1);
        }

        private void CursorHorizontalAbsolute()
        {
            var column = currentParams[0] ?? 1;
            cursor.Position = cursor.Position.WithX(column);
        }

        private void CursorPosition()
        {
            var row = currentParams[0] ?? 1;
            var column = currentParams[1] ?? 1;
            cursor.Position = cursor.Position.WithY(row).WithX(column);
        }

        private void SelectGraphicRendition()
        {
            Console.WriteLine("SGR"); // TODO
        }

        private void EraseDisplay()
        {
            var type = currentParams[0] ?? 0;
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

            commands.Add('m', SelectGraphicRendition);
        }

        private readonly Dictionary<char, Action> commands;
    }
}

