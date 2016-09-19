//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;

namespace TermSharp.Misc
{
    internal sealed class SimpleCache<TFrom, TTo> where TFrom : IGenerationAware
    {
        public SimpleCache(Func<TFrom, TTo> factory)
        {
            generation = -1;
            this.factory = factory;
        }

        public TTo GetValue(TFrom from)
        {
            if(generation != from.Generation)
            {
                var resultAsDisposable = lastResult as IDisposable;
                if(resultAsDisposable != null)
                {
                    resultAsDisposable.Dispose();
                }
                lastResult = factory(from);
                generation = from.Generation;
            }
            return lastResult;
        }

        private TTo lastResult;
        private Func<TFrom, TTo> factory;
        private int generation;
    }
}
