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
                { Key.Home, new [] { (byte)ControlByte.Escape, (byte)ControlByte.Csi, (byte)'1', (byte)'~' } },
                { Key.Insert, new [] { (byte)ControlByte.Escape, (byte)ControlByte.Csi, (byte)'2', (byte)'~' } },
                { Key.Delete, new [] { (byte)ControlByte.Escape, (byte)ControlByte.Csi, (byte)'3', (byte)'~' } },
                { Key.End, new [] { (byte)ControlByte.Escape, (byte)ControlByte.Csi, (byte)'4', (byte)'~' } },
                { Key.PageUp, new [] { (byte)ControlByte.Escape, (byte)ControlByte.Csi, (byte)'5', (byte)'~' } },
                { Key.PageDown, new [] { (byte)ControlByte.Escape, (byte)ControlByte.Csi, (byte)'6', (byte)'~' } },

                { Key.NumPad0, new [] { (byte)'0' } },
                { Key.NumPad1, new [] { (byte)'1' } },
                { Key.NumPad2, new [] { (byte)'2' } },
                { Key.NumPad3, new [] { (byte)'3' } },
                { Key.NumPad4, new [] { (byte)'4' } },
                { Key.NumPad5, new [] { (byte)'5' } },
                { Key.NumPad6, new [] { (byte)'6' } },
                { Key.NumPad7, new [] { (byte)'7' } },
                { Key.NumPad8, new [] { (byte)'8' } },
                { Key.NumPad9, new [] { (byte)'9' } },
                { Key.NumPadAdd, new [] { (byte)'+' } },
                { Key.NumPadSubtract, new [] { (byte)'-' } },
                { Key.NumPadMultiply, new [] { (byte)'*' } },
                { Key.NumPadDivide, new [] { (byte)'/' } },
                { Key.NumPadDecimal, new [] { (byte)'.' } },
                { Key.NumPadEnter, new [] { (byte)ControlByte.CarriageReturn } },
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
            if(key >= Key.At && key <= (Key)95)
            {
                dataCallback((byte)(key - Key.At));
            }
            else
            {
                throw new NotImplementedException("Unimplemented key with control modifier.");
            }
        }

        private readonly Action<byte> dataCallback;
        private readonly Dictionary<Key, byte[]> keyHandlers;
    }
}

