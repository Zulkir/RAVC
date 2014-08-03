#region License
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

using System;
using System.Collections.Concurrent;
using Ravc.Infrastructure;

namespace Ravc.Utility
{
    public abstract class TexturePoolBase<T> : IDisposable
    {
        private readonly ConcurrentStack<PooledTexture<T>> stack;

        protected TexturePoolBase()
        {
            stack = new ConcurrentStack<PooledTexture<T>>();
        }

        protected abstract Dimensions GetDimensions(T texture);
        protected abstract T CreateNew(int width, int height);
        protected abstract void Delete(T texture);

        public IPooled<T> Extract(int width, int height)
        {
            var dimensions = new Dimensions(width, height);
            PooledTexture<T> pooledTexture;
            while (stack.TryPop(out pooledTexture))
            {
                if (GetDimensions(pooledTexture.Item) == dimensions)
                    return pooledTexture;
                Delete(pooledTexture.Item);
            }
            var newTexture = CreateNew(width, height);
            return new PooledTexture<T>(newTexture, this);
        }

        public void Dispose()
        {
            PooledTexture<T> pooledTexture;
            while (stack.TryPop(out pooledTexture))
                Delete(pooledTexture.Item);
        }

        internal void Return(PooledTexture<T> pooledTexture)
        {
            stack.Push(pooledTexture);
        }
    }
}