using Ravc.Client.OglLib;

namespace Ravc.Client.OglDesktop
{
    public class ClientStatisticsRenderer : IClientStatisticsRenderer
    {
        private readonly StatisticsForm form;

        public ClientStatisticsRenderer()
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
    }
}