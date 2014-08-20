namespace Ravc.WinHost
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
            this.btnReset = new System.Windows.Forms.Button();
            this.lbGpuCallsTime = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.lbCpuProcessingQueue = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.lbPresentTime = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.lbReadbackTime = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lbSize = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lbFps = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lbBitrate = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lbCompressionTime = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.lbCapture = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(90, 259);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(75, 23);
            this.btnReset.TabIndex = 0;
            this.btnReset.Text = "Reset";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // lbGpuCallsTime
            // 
            this.lbGpuCallsTime.AutoSize = true;
            this.lbGpuCallsTime.Location = new System.Drawing.Point(140, 81);
            this.lbGpuCallsTime.Name = "lbGpuCallsTime";
            this.lbGpuCallsTime.Size = new System.Drawing.Size(35, 13);
            this.lbGpuCallsTime.TabIndex = 31;
            this.lbGpuCallsTime.Text = "label3";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(12, 81);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(78, 13);
            this.label9.TabIndex = 30;
            this.label9.Text = "Gpu Calls Time";
            // 
            // lbCpuProcessingQueue
            // 
            this.lbCpuProcessingQueue.AutoSize = true;
            this.lbCpuProcessingQueue.Location = new System.Drawing.Point(140, 153);
            this.lbCpuProcessingQueue.Name = "lbCpuProcessingQueue";
            this.lbCpuProcessingQueue.Size = new System.Drawing.Size(35, 13);
            this.lbCpuProcessingQueue.TabIndex = 27;
            this.lbCpuProcessingQueue.Text = "label6";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 153);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(116, 13);
            this.label6.TabIndex = 26;
            this.label6.Text = "Cpu Processing Queue";
            // 
            // lbPresentTime
            // 
            this.lbPresentTime.AutoSize = true;
            this.lbPresentTime.Location = new System.Drawing.Point(140, 129);
            this.lbPresentTime.Name = "lbPresentTime";
            this.lbPresentTime.Size = new System.Drawing.Size(35, 13);
            this.lbPresentTime.TabIndex = 25;
            this.lbPresentTime.Text = "label6";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 129);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(69, 13);
            this.label7.TabIndex = 24;
            this.label7.Text = "Present Time";
            // 
            // lbReadbackTime
            // 
            this.lbReadbackTime.AutoSize = true;
            this.lbReadbackTime.Location = new System.Drawing.Point(140, 105);
            this.lbReadbackTime.Name = "lbReadbackTime";
            this.lbReadbackTime.Size = new System.Drawing.Size(35, 13);
            this.lbReadbackTime.TabIndex = 23;
            this.lbReadbackTime.Text = "label3";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 105);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(83, 13);
            this.label5.TabIndex = 22;
            this.label5.Text = "Readback Time";
            // 
            // lbSize
            // 
            this.lbSize.AutoSize = true;
            this.lbSize.Location = new System.Drawing.Point(140, 33);
            this.lbSize.Name = "lbSize";
            this.lbSize.Size = new System.Drawing.Size(35, 13);
            this.lbSize.TabIndex = 19;
            this.lbSize.Text = "label3";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 33);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(27, 13);
            this.label2.TabIndex = 18;
            this.label2.Text = "Size";
            // 
            // lbFps
            // 
            this.lbFps.AutoSize = true;
            this.lbFps.Location = new System.Drawing.Point(140, 9);
            this.lbFps.Name = "lbFps";
            this.lbFps.Size = new System.Drawing.Size(35, 13);
            this.lbFps.TabIndex = 17;
            this.lbFps.Text = "label2";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(27, 13);
            this.label1.TabIndex = 16;
            this.label1.Text = "FPS";
            // 
            // lbBitrate
            // 
            this.lbBitrate.AutoSize = true;
            this.lbBitrate.Location = new System.Drawing.Point(140, 201);
            this.lbBitrate.Name = "lbBitrate";
            this.lbBitrate.Size = new System.Drawing.Size(35, 13);
            this.lbBitrate.TabIndex = 33;
            this.lbBitrate.Text = "label6";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 201);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(37, 13);
            this.label4.TabIndex = 32;
            this.label4.Text = "Bitrate";
            // 
            // lbCompressionTime
            // 
            this.lbCompressionTime.AutoSize = true;
            this.lbCompressionTime.Location = new System.Drawing.Point(140, 177);
            this.lbCompressionTime.Name = "lbCompressionTime";
            this.lbCompressionTime.Size = new System.Drawing.Size(35, 13);
            this.lbCompressionTime.TabIndex = 35;
            this.lbCompressionTime.Text = "label6";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(12, 177);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(93, 13);
            this.label8.TabIndex = 34;
            this.label8.Text = "Compression Time";
            // 
            // lbCapture
            // 
            this.lbCapture.AutoSize = true;
            this.lbCapture.Location = new System.Drawing.Point(140, 57);
            this.lbCapture.Name = "lbCapture";
            this.lbCapture.Size = new System.Drawing.Size(35, 13);
            this.lbCapture.TabIndex = 37;
            this.lbCapture.Text = "label3";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(12, 57);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(70, 13);
            this.label10.TabIndex = 36;
            this.label10.Text = "Capture Time";
            // 
            // StatisticsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(257, 294);
            this.Controls.Add(this.lbCapture);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.lbCompressionTime);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.lbBitrate);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lbGpuCallsTime);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.lbCpuProcessingQueue);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.lbPresentTime);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.lbReadbackTime);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lbSize);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lbFps);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnReset);
            this.Name = "StatisticsForm";
            this.Text = "StatisticsForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Button btnReset;
        public System.Windows.Forms.Label lbGpuCallsTime;
        public System.Windows.Forms.Label label9;
        public System.Windows.Forms.Label lbCpuProcessingQueue;
        public System.Windows.Forms.Label label6;
        public System.Windows.Forms.Label lbPresentTime;
        public System.Windows.Forms.Label label7;
        public System.Windows.Forms.Label lbReadbackTime;
        public System.Windows.Forms.Label label5;
        public System.Windows.Forms.Label lbSize;
        public System.Windows.Forms.Label label2;
        public System.Windows.Forms.Label lbFps;
        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.Label lbBitrate;
        public System.Windows.Forms.Label label4;
        public System.Windows.Forms.Label lbCompressionTime;
        public System.Windows.Forms.Label label8;
        public System.Windows.Forms.Label lbCapture;
        public System.Windows.Forms.Label label10;
    }
}

