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
                return rows.Skip(1).Aggregate(rows[0], (x, y) => x + Environment.NewLine + y);
            }
        }

        private readonly List<string> rows;
    }
}

