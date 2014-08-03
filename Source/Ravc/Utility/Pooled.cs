﻿#region License
/*
Copyright (c) 2014 RAVC Project - Daniil Rodin

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

using System.Threading;

namespace Ravc.Utility
{
    public class Pooled<T> : IPooled<T>
    {
        private readonly T item;
        private readonly Pool<T> pool;
        private int refCount;

        public T Item { get { return item; } }

        public Pooled(Pool<T> pool, T item)
        {
            this.pool = pool;
            this.item = item;
            refCount = 1;
        }

        public void IncRefCount()
        {
            Interlocked.Increment(ref refCount);
        }

        public void Release()
        {
            Interlocked.Decrement(ref refCount);
            if (refCount > 0) 
                return;
            refCount = 1;
            pool.Return(this);
        }
    }
}