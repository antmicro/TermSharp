//
// Copyright (c) 2010-2021 Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;
using System.Text;
using Xwt;

namespace TermSharp.Example
{
    public partial class TerminalWidget : Widget
    {
        private Dictionary<KeyEventArgs, Action> DefineShortcuts(Terminal terminal)
        {
            return new Dictionary<KeyEventArgs, Action>
            {
                {CreateKey(Key.C, ModifierKeys.Shift | ModifierKeys.Control), CopyMarkedField},
                {CreateKey(Key.V, ModifierKeys.Shift | ModifierKeys.Control), PasteMarkedField},
                {CreateKey(Key.Insert, ModifierKeys.Shift), PasteMarkedField},
                {CreateKey(Key.PageUp, ModifierKeys.Shift), () => terminal.PageUp() },
                {CreateKey(Key.PageDown, ModifierKeys.Shift), () => terminal.PageDown() },
                {CreateKey(Key.Plus, ModifierKeys.Shift | ModifierKeys.Control), FontSizeUp},
                {CreateKey(Key.Minus, ModifierKeys.Control), FontSizeDown},
                {CreateKey(Key.K0, ModifierKeys.Control), SetDefaultFontSize},
            };
        }

        private void DefineMouseEvents(Terminal terminal)
        {
            terminal.ButtonPressed += (s, a) =>
            {
                if(a.Button == PointerButton.Middle)
                {
                    a.Handled = true;
                    PastePrimarySelection();
                }
            };
        }

        private Menu CreatePopupMenu()
        {
            var popup = new Menu();

            var copyItem = new MenuItem("Copy");
            copyItem.Clicked += delegate
            {
                CopyMarkedField();
            };
            popup.Items.Add(copyItem);

            var pasteItem = new MenuItem("Paste");
            pasteItem.Clicked += delegate
            {
                PasteMarkedField();
            };
            popup.Items.Add(pasteItem);

            return popup;
        }

        private KeyEventArgs CreateKey(Key key, ModifierKeys modifierKeys)
        {
            return new KeyEventArgs(key, modifierKeys, false, 0);
        }

        private void CopyMarkedField()
        {
            Clipboard.SetText(terminal.CollectClipboardData().Text);
        }

        private void PasteText(string text)
        {
            if(string.IsNullOrEmpty(text))
            {
                return;
            }
            var textAsBytes = Encoding.UTF8.GetBytes(text);
            foreach(var b in textAsBytes)
            {
                if(b == '\n')
                    utfDecoder.Feed((byte)'\r');
                utfDecoder.Feed(b);
            }
        }

        private void PasteMarkedField()
        {
            PasteText(Clipboard.GetText());
        }

        private void PastePrimarySelection()
        {
            PasteText(Clipboard.GetPrimaryText());
        }

        private void FontSizeUp()
        {
            var newSize = terminal.CurrentFont.Size + 1;
            terminal.CurrentFont = terminal.CurrentFont.WithSize(newSize);
        }

        private void FontSizeDown()
        {
            var newSize = Math.Max(terminal.CurrentFont.Size - 1, 1.0);
            terminal.CurrentFont = terminal.CurrentFont.WithSize(newSize);
        }

        private void SetDefaultFontSize()
        {
            var newSize = PredefinedFontSize;
            terminal.CurrentFont = terminal.CurrentFont.WithSize(newSize);
        }
    }
}