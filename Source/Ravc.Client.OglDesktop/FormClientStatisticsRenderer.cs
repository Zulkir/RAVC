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
using Ravc.Client.OglLib;

namespace Ravc.Client.OglDesktop
{
    public class FormClientStatisticsRenderer : IClientStatisticsRenderer
    {
        private readonly StatisticsForm form;

        public FormClientStatisticsRenderer()
        {
            form = new StatisticsForm();
        }

        public void ShowForm()
        {
            form.Show();
        }

        public string Fps { set { form.lbFps.Text = value; } }
        public string IdleFrames { set { form.lbIdleFrames.Text = value; } }
        public string SkippedFrames { set { form.lbSkippedFrames.Text = value; } }
        public string TimeLag { set { form.lbTimeLag.Text = value; } }
        public string PresentTime { set { form.lbPresentTime.Text = value; } }
        public string GpuUploadTime { set { form.lbGpuUploadTime.Text = value; } }
        public string BorderPassTime { set { form.lbBorderPassTime.Text = value; } }
        public string CpuDecodingTime { set { form.lbCpuDecodingTime.Text = value; } }
        public string TimeBufferingQueue { set { form.lbTimeBufferingQueue.Text = value; } }
        public string MainThreadQueue { set { form.lbMainThreadQueue.Text = value; } }
        public string CpuProcessingQueue { set { form.lbCpuProcessingQueue.Text = value; } }

        public void Render(IContext context)
        {
            
        }
    }
}