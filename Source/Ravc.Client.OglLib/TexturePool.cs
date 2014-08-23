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

using ObjectGL;
using ObjectGL.Api;
using ObjectGL.Api.Objects.Resources;
using Ravc.Infrastructure;
using Ravc.Utility;

namespace Ravc.Client.OglLib
{
    public class TexturePool : TexturePoolBase<ITexture2D>
    {
        private readonly IContext context;
        private readonly ITextureInitializer textureInitializer;
        private readonly bool withMipChain;

        public TexturePool(IContext context, ITextureInitializer textureInitializer, bool withMipChain)
        {
            this.context = context;
            this.textureInitializer = textureInitializer;
            this.withMipChain = withMipChain;
        }

        protected override Dimensions GetDimensions(ITexture2D texture)
        {
            return new Dimensions(texture.Width, texture.Height);
        }

        protected override ITexture2D CreateNew(int width, int height)
        {
            var texture = context.Create.Texture2D(width, height, withMipChain ? TextureHelper.CalculateMipCount(width, height, 1) : 1, Format.Rgba8);
            textureInitializer.InitializeTexture(texture);
            return texture;
        }

        protected override void Delete(ITexture2D texture)
        {
            texture.Dispose();
        }
    }
}