//
// Copyright (c) 2010-2021 Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using TermSharp.Rows;
using TermSharp.Vt100;
using Xwt;

namespace TermSharp.Example
{
    public partial class TerminalWidget : Widget
    {
        public TerminalWidget(Action<string> writer)
        {
            terminal = new Terminal(null);
            inputDecoder = new ByteUtf8Decoder(writer);

            vt100decoder = new TermSharp.Vt100.Decoder(terminal, b =>
                    {
                        //your logic here
                        inputDecoder.Feed(b);
                    }, new TerminalLogger(terminal));

            utfDecoder = new ByteUtf8Decoder(x => Application.Invoke(() => vt100decoder.Feed(x)));

            terminal.InnerMargin = new WidgetSpacing(5, 5, 5, 5);
            terminal.Cursor.Enabled = true;
            terminal.ContextMenu = CreatePopupMenu();

            terminal.CurrentFont = Xwt.Drawing.Font.SystemMonospaceFont.WithSize(PredefinedFontSize);

            // this empty dummy row is required as this is where first
            // characters will be displayed
            terminal.AppendRow(new MonospaceTextRow(""), true);

            var encoder = new TermSharp.Vt100.Encoder(x =>
            {
                terminal.ClearSelection();
                terminal.MoveScrollbarToEnd();
                //your logic here
                inputDecoder.Feed(x);
            });

            var shortcutDictionary = DefineShortcuts(terminal);
            terminal.KeyPressed += (s, a) =>
            {
                a.Handled = true;

                var modifiers = a.Modifiers;
                modifiers &= ~(ModifierKeys.Command);

                foreach(var entry in shortcutDictionary)
                {
                    if(modifiers == entry.Key.Modifiers)
                    {
                        if(a.Key == entry.Key.Key)
                        {
                            entry.Value();
                            return;
                        }
                    }
                }
                encoder.Feed(a.Key, modifiers);
            };

            DefineMouseEvents(terminal);
            Content = terminal;
        }

        public void Dispose()
        {
            terminal.Close();
        }

        public void Feed(byte b)
        {
            utfDecoder.Feed(b);
        }

        private Terminal terminal;

        private TermSharp.Vt100.Decoder vt100decoder;
        private ByteUtf8Decoder utfDecoder;
        private ByteUtf8Decoder inputDecoder;

        private const double PredefinedFontSize = 20.0;

        private class TerminalLogger : IDecoderLogger
        {
            public TerminalLogger(Terminal t) { }

            public void Log(string format, params object[] args)
            {
                Console.WriteLine(format, args);
            }
        }
    }
}
