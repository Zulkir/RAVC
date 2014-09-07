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

using ObjectGL.Api;
using ObjectGL.Api.Objects.Resources;
using Ravc.Infrastructure;
using Ravc.Utility;

namespace Ravc.Client.OglLib
{
    public class TexturePool : TexturePoolBase<ManualMipChain>
    {
        private readonly IContext context;
        private readonly ITextureInitializer textureInitializer;

        public TexturePool(IContext context, ITextureInitializer textureInitializer)
        {
            this.context = context;
            this.textureInitializer = textureInitializer;
        }

        protected override Dimensions GetDimensions(ManualMipChain mipChain)
        {
            return new Dimensions(mipChain.Width, mipChain.Height);
        }

        protected override ManualMipChain CreateNew(int width, int height)
        {
            var mipChain = new ManualMipChain(context, width, height, Format.Rgba8);
            foreach (var level in mipChain.Levels)
                textureInitializer.InitializeTexture(level);
            return mipChain;
        }

        protected override void Delete(ManualMipChain mipChain)
        {
            mipChain.Dispose();
        }
    }
}