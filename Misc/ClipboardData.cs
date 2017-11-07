//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Collections.Generic;
using System.Linq;

namespace TermSharp.Misc
{
    public sealed class ClipboardData
    {
        public ClipboardData()
        {
            rows = new List<string>();
        }

        public void AppendText(string text)
        {
            rows.Add(text);
        }

        public string Text
        {
            get
            {
                if(rows.Count == 0)
                {
                    return string.Empty;
                }
                return string.Join(Environment.NewLine, rows.Select(x => x.TrimEnd('\n').TrimEnd('\r')));
            }
        }

        private readonly List<string> rows;
    }
}

