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
            directlyPassedKeys = new HashSet<Key>
            {
                Key.K1, Key.K2, Key.K3, Key.K4, Key.K5, Key.K6, Key.K7, Key.K8, Key.K9, Key.K0,
                Key.Space
            };
            keyHandlers = new Dictionary<Key, byte[]>
            {
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
            if((key >= Key.A && key <= Key.Z) || (key >= Key.a && key <= Key.z) || directlyPassedKeys.Contains(key))
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

        private readonly HashSet<Key> directlyPassedKeys;
        private readonly Action<byte> dataCallback;
        private readonly Dictionary<Key, byte[]> keyHandlers;
    }
}

