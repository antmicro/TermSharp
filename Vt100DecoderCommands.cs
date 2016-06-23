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

        private void InitializeCommands()
        {
            commands.Add('A', CursorUp);
            commands.Add('B', CursorDown);
            commands.Add('C', CursorRight);
            commands.Add('D', CursorLeft);
        }

        private readonly Dictionary<char, Action> commands;
    }
}

