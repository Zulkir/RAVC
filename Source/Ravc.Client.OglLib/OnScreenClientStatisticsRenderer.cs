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

namespace Ravc.Client.OglLib
{
    public class OnScreenClientStatisticsRenderer : IClientStatisticsRenderer
    {
        private readonly Spritefont spritefont;

        public OnScreenClientStatisticsRenderer(Spritefont spritefont)
        {
            this.spritefont = spritefont;
        }

        public string Fps { private get; set; }
        public string IdleFrames { private get; set; }
        public string SkippedFrames { private get; set; }
        public string TimeLag { private get; set; }
        public string PresentTime { private get; set; }
        public string GpuUploadTime { private get; set; }
        public string BorderPassTime { private get; set; }
        public string CpuDecodingTime { private get; set; }
        public string TimeBufferingQueue { private get; set; }
        public string MainThreadQueue { private get; set; }
        public string CpuProcessingQueue { private get; set; }
        
        public void Render(IContext context)
        {
            const float size = 0.04f;
            spritefont.DrawString(context, -0.99f, 0.99f - 0 * size, size,  "FPS:                 " + Fps);
            spritefont.DrawString(context, -0.99f, 0.99f - 1 * size, size,  "Idle Frames:         " + IdleFrames);
            spritefont.DrawString(context, -0.99f, 0.99f - 2 * size, size,  "Skipped Frames:      " + SkippedFrames);
            spritefont.DrawString(context, -0.99f, 0.99f - 3 * size, size,  "Time Lag:            " + TimeLag);
            spritefont.DrawString(context, -0.99f, 0.99f - 4 * size, size,  "Present Time:        " + PresentTime);
            spritefont.DrawString(context, -0.99f, 0.99f - 5 * size, size,  "GPU Upload Time:     " + GpuUploadTime);
            spritefont.DrawString(context, -0.99f, 0.99f - 6 * size, size,  "Border Pass Time:    " + BorderPassTime);
            spritefont.DrawString(context, -0.99f, 0.99f - 7 * size, size,  "Cpu Decoding Time:   " + CpuDecodingTime);
            spritefont.DrawString(context, -0.99f, 0.99f - 8 * size, size,  "Time Buffering Queue " + TimeBufferingQueue);
            spritefont.DrawString(context, -0.99f, 0.99f - 9 * size, size,  "Main Thread Queue    " + MainThreadQueue);
            spritefont.DrawString(context, -0.99f, 0.99f - 10 * size, size, "Cpu Processing Queue " + CpuProcessingQueue);
        }
    }
}