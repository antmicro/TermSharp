//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;
using Xwt;

namespace Terminal
{
    public sealed class Vt100Encoder
    {
        public Vt100Encoder(Action<byte> dataCallback)
        {
            this.dataCallback = dataCallback;
            keyHandlers = new Dictionary<Key, byte[]>
            {
                { Key.Escape, new [] { (byte)ControlByte.Escape } },
                { Key.Return, new [] { (byte)ControlByte.CarriageReturn } },
                { Key.BackSpace, new [] { (byte)ControlByte.Backspace } },
                { Key.Tab, new [] { (byte)ControlByte.HorizontalTab } },
                { Key.Up, new [] { (byte)ControlByte.Escape, (byte)ControlByte.Csi, (byte)'A' } },
                { Key.Down, new [] { (byte)ControlByte.Escape, (byte)ControlByte.Csi, (byte)'B' } },
                { Key.Right, new [] { (byte)ControlByte.Escape, (byte)ControlByte.Csi, (byte)'C' } },
                { Key.Left, new [] { (byte)ControlByte.Escape, (byte)ControlByte.Csi, (byte)'D' } },
            };
        }

        public void Feed(Key key, ModifierKeys modifiers)
        {
            if((modifiers & ModifierKeys.Control) != 0)
            {
                HandleControlModifier(key);
                return;
            }
            if((key >= Key.Space && key <= (Key)126))
            {
                dataCallback((byte)key);
                return;
            }
            byte[] response;
            if(keyHandlers.TryGetValue(key, out response))
            {
                foreach(var b in response)
                {
                    dataCallback(b);
                }
            }
        }

        private void HandleControlModifier(Key key)
        {
            if(key >= Key.a && key <= Key.z)
            {
                key -= 32;
            }
            dataCallback((byte)(key - Key.At));
        }

        private readonly Action<byte> dataCallback;
        private readonly Dictionary<Key, byte[]> keyHandlers;
    }
}

