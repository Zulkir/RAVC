using System.Globalization;
using System.Windows.Forms;
using Beholder;

namespace Ravc.WinHost
{
    public partial class SettingsForm : Form
    {
        private readonly IEye eye;
        private readonly IHostStatistics statistics;

        public SettingsForm(IHostStatistics statistics, IEye eye)
        {
            InitializeComponent();
            this.statistics = statistics;
            this.eye = eye;
            eye.NewFrame += time =>
            {
                lbFps.Text = string.Format("{0}", statistics.FramesPerSecond);
                lbSize.Text = string.Format("{0}x{1} ({2:0.##e+0})", statistics.Width, statistics.Height, statistics.Width * statistics.Height);
                lbAverageBitrate.Text = string.Format(CultureInfo.InvariantCulture, "{0:F3} Mbit/s", statistics.AverageBitrate / 1024.0 / 1024);
                lbTotalBytes.Text = string.Format(CultureInfo.InvariantCulture, "{0:F3} MBytes", statistics.TotalBytes / 1024.0 / 1024);
            };
        }

        private void btnReset_Click(object sender, System.EventArgs e)
        {
            statistics.Reset();
        }
    }
}
