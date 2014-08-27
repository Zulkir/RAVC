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

using Android.Content.Res;
using Android.Graphics;
using Android.Opengl;
using ObjectGL.Api;
using ObjectGL.Api.Objects.Resources;
using Ravc.Client.OglLib;
using Format = ObjectGL.Api.Objects.Resources.Format;

namespace Ravc.Client.Android
{
    public class TextureLoader : ITextureLoader
    {
        private readonly AssetManager assetManager;

        public TextureLoader(AssetManager assetManager)
        {
            this.assetManager = assetManager;
        }

        public ITexture2D LoadTexture(IContext context, string name)
        {
            Bitmap bitmap;
            using (var stream = assetManager.Open("DebugFont.png"))
            {
                bitmap = BitmapFactory.DecodeStream(stream);
            }
            var texture = context.Create.Texture2D(bitmap.Width, bitmap.Height, 1, Format.Rgba8);
            GLUtils.TexSubImage2D((int)TextureTarget.Texture2D, 0, 0, 0, bitmap);
            return texture;
        }
    }
}