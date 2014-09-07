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

using Beholder;
using Beholder.Platform;
using Beholder.Resources;
using Ravc.Encoding;
using Ravc.Infrastructure;
using Ravc.Utility;

namespace Ravc.Host.WinLib
{
    public class TexturePool : TexturePoolBase<ITexture2D>
    {
        private readonly IDevice device;
        private readonly int formatId;
        private readonly Usage usage;
        private readonly BindFlags bindFlags;
        private readonly MiscFlags miscFlags;

        public TexturePool(IDevice device, int formatId, Usage usage, BindFlags bindFlags, MiscFlags miscFlags)
        {
            this.device = device;
            this.usage = usage;
            this.bindFlags = bindFlags;
            this.miscFlags = miscFlags;
            this.formatId = formatId;
        }

        protected override Dimensions GetDimensions(ITexture2D mipChain)
        {
            return new Dimensions(mipChain.Width, mipChain.Height);
        }

        protected override ITexture2D CreateNew(int width, int height)
        {
            return device.Create.Texture2D(new Texture2DDescription
            {
                Width = width,
                Height = height,
                ArraySize = 1,
                MipLevels = miscFlags.HasFlag(MiscFlags.GenerateMips) ? EncodingConstants.MipLevels : 1,
                FormatID = formatId,
                Sampling = Sampling.NoMultisampling,
                Usage = usage,
                BindFlags = bindFlags,
                MiscFlags = miscFlags
            });
        }

        protected override void Delete(ITexture2D mipChain)
        {
            mipChain.Dispose();
        }
    }
}