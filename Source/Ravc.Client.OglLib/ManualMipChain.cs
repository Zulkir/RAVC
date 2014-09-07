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
using System.Collections.Generic;
using ObjectGL.Api;
using ObjectGL.Api.Objects.Resources;
using Ravc.Encoding;

namespace Ravc.Client.OglLib
{
    public class ManualMipChain : IDisposable
    {
        private readonly ITexture2D[] textures;

        public ITexture2D this[int level] { get { return textures[level]; } }
        public IReadOnlyList<ITexture2D> Levels { get { return textures; } }
        public int Width { get { return textures[0].Width; } }
        public int Height { get { return textures[0].Height; } }

        public ManualMipChain(IContext context, int width, int height, Format format)
        {
            textures = new ITexture2D[EncodingConstants.MipLevels];
            for (int i = 0; i < textures.Length; i++)
                textures[i] = context.Create.Texture2D(width >> i, height >> i, 1, format);
        }

        public void Dispose()
        {
            for (int i = 0; i < textures.Length; i++)
            {
                textures[i].Dispose();
                textures[i] = null;
            }
        }
    }
}