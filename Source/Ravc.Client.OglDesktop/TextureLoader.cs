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

using System.Drawing;
using System.Drawing.Imaging;
using ObjectGL.Api;
using ObjectGL.Api.Objects.Resources;
using Ravc.Client.OglLib;

namespace Ravc.Client.OglDesktop
{
    public class TextureLoader : ITextureLoader
    {
        public ITexture2D LoadTexture(IContext context, string name)
        {
            var bitmap = new Bitmap(@"../../Resources/Textures/" + name);
            var texture = context.Create.Texture2D(bitmap.Width, bitmap.Height, 1, Format.Rgba8);
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            {
                // todo: swap R-B and calculate mips ... if someone ever needs that here
                texture.SetData(0, 0, 0, bitmap.Width, bitmap.Height, data.Scan0, FormatColor.Rgba, FormatType.UnsignedByte);
            }
            bitmap.UnlockBits(data);
            return texture;
        }
    }
}