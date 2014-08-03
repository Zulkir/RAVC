namespace Ravc.WinformsOglClient
{
    partial class StatisticsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.lbFps = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lbIdleFrames = new System.Windows.Forms.Label();
            this.lbSkippedFrames = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lbTimeBufferingQueue = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lbMainThreadQueue = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.lbCpuProcessingQueue = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.lbTimeLag = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.lbGpuUploadTime = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(27, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "FPS";
            // 
            // lbFps
            // 
            this.lbFps.AutoSize = true;
            this.lbFps.Location = new System.Drawing.Point(141, 13);
            this.lbFps.Name = "lbFps";
            this.lbFps.Size = new System.Drawing.Size(35, 13);
            this.lbFps.TabIndex = 1;
            this.lbFps.Text = "label2";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Idle Frames";
            // 
            // lbIdleFrames
            // 
            this.lbIdleFrames.AutoSize = true;
            this.lbIdleFrames.Location = new System.Drawing.Point(141, 37);
            this.lbIdleFrames.Name = "lbIdleFrames";
            this.lbIdleFrames.Size = new System.Drawing.Size(35, 13);
            this.lbIdleFrames.TabIndex = 3;
            this.lbIdleFrames.Text = "label3";
            // 
            // lbSkippedFrames
            // 
            this.lbSkippedFrames.AutoSize = true;
            this.lbSkippedFrames.Location = new System.Drawing.Point(141, 61);
            this.lbSkippedFrames.Name = "lbSkippedFrames";
            this.lbSkippedFrames.Size = new System.Drawing.Size(35, 13);
            this.lbSkippedFrames.TabIndex = 5;
            this.lbSkippedFrames.Text = "label3";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 61);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(83, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Skipped Frames";
            // 
            // lbTimeBufferingQueue
            // 
            this.lbTimeBufferingQueue.AutoSize = true;
            this.lbTimeBufferingQueue.Location = new System.Drawing.Point(141, 133);
            this.lbTimeBufferingQueue.Name = "lbTimeBufferingQueue";
            this.lbTimeBufferingQueue.Size = new System.Drawing.Size(35, 13);
            this.lbTimeBufferingQueue.TabIndex = 7;
            this.lbTimeBufferingQueue.Text = "label3";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 133);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(110, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Time Buffering Queue";
            // 
            // lbMainThreadQueue
            // 
            this.lbMainThreadQueue.AutoSize = true;
            this.lbMainThreadQueue.Location = new System.Drawing.Point(141, 157);
            this.lbMainThreadQueue.Name = "lbMainThreadQueue";
            this.lbMainThreadQueue.Size = new System.Drawing.Size(35, 13);
            this.lbMainThreadQueue.TabIndex = 9;
            this.lbMainThreadQueue.Text = "label6";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(13, 157);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(102, 13);
            this.label7.TabIndex = 8;
            this.label7.Text = "Main Thread Queue";
            // 
            // lbCpuProcessingQueue
            // 
            this.lbCpuProcessingQueue.AutoSize = true;
            this.lbCpuProcessingQueue.Location = new System.Drawing.Point(141, 181);
            this.lbCpuProcessingQueue.Name = "lbCpuProcessingQueue";
            this.lbCpuProcessingQueue.Size = new System.Drawing.Size(35, 13);
            this.lbCpuProcessingQueue.TabIndex = 11;
            this.lbCpuProcessingQueue.Text = "label6";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(13, 181);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(116, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Cpu Processing Queue";
            // 
            // lbTimeLag
            // 
            this.lbTimeLag.AutoSize = true;
            this.lbTimeLag.Location = new System.Drawing.Point(141, 85);
            this.lbTimeLag.Name = "lbTimeLag";
            this.lbTimeLag.Size = new System.Drawing.Size(35, 13);
            this.lbTimeLag.TabIndex = 13;
            this.lbTimeLag.Text = "label3";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(13, 85);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(51, 13);
            this.label8.TabIndex = 12;
            this.label8.Text = "Time Lag";
            // 
            // lbGpuUploadTime
            // 
            this.lbGpuUploadTime.AutoSize = true;
            this.lbGpuUploadTime.Location = new System.Drawing.Point(141, 109);
            this.lbGpuUploadTime.Name = "lbGpuUploadTime";
            this.lbGpuUploadTime.Size = new System.Drawing.Size(35, 13);
            this.lbGpuUploadTime.TabIndex = 15;
            this.lbGpuUploadTime.Text = "label3";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(13, 109);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(93, 13);
            this.label9.TabIndex = 14;
            this.label9.Text = "GPU Upload Time";
            // 
            // StatisticsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(244, 211);
            this.Controls.Add(this.lbGpuUploadTime);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.lbTimeLag);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.lbCpuProcessingQueue);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.lbMainThreadQueue);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.lbTimeBufferingQueue);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lbSkippedFrames);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lbIdleFrames);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lbFps);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.Name = "StatisticsForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "StatisticsForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.Label lbFps;
        public System.Windows.Forms.Label label2;
        public System.Windows.Forms.Label lbIdleFrames;
        public System.Windows.Forms.Label lbSkippedFrames;
        public System.Windows.Forms.Label label4;
        public System.Windows.Forms.Label lbTimeBufferingQueue;
        public System.Windows.Forms.Label label5;
        public System.Windows.Forms.Label lbMainThreadQueue;
        public System.Windows.Forms.Label label7;
        public System.Windows.Forms.Label lbCpuProcessingQueue;
        public System.Windows.Forms.Label label6;
        public System.Windows.Forms.Label lbTimeLag;
        public System.Windows.Forms.Label label8;
        public System.Windows.Forms.Label lbGpuUploadTime;
        public System.Windows.Forms.Label label9;
    }
}