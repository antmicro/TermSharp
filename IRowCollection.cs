﻿//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;

namespace Terminal
{
    public interface IRowCollection
    {
        void AppendRow(IRow row);
        void Clear();
        int Count { get; }
    }
}

