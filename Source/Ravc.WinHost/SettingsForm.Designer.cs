namespace Ravc.WinHost
{
    partial class SettingsForm
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
            this.lbAverageBitrate = new System.Windows.Forms.Label();
            this.lbTotalBytes = new System.Windows.Forms.Label();
            this.lbFps = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lbSize = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(100, 227);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(75, 23);
            this.btnReset.TabIndex = 0;
            this.btnReset.Text = "Reset";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // lbAverageBitrate
            // 
            this.lbAverageBitrate.AutoSize = true;
            this.lbAverageBitrate.Location = new System.Drawing.Point(62, 77);
            this.lbAverageBitrate.Name = "lbAverageBitrate";
            this.lbAverageBitrate.Size = new System.Drawing.Size(35, 13);
            this.lbAverageBitrate.TabIndex = 1;
            this.lbAverageBitrate.Text = "label1";
            // 
            // lbTotalBytes
            // 
            this.lbTotalBytes.AutoSize = true;
            this.lbTotalBytes.Location = new System.Drawing.Point(62, 113);
            this.lbTotalBytes.Name = "lbTotalBytes";
            this.lbTotalBytes.Size = new System.Drawing.Size(35, 13);
            this.lbTotalBytes.TabIndex = 2;
            this.lbTotalBytes.Text = "label1";
            // 
            // lbFps
            // 
            this.lbFps.AutoSize = true;
            this.lbFps.Location = new System.Drawing.Point(62, 9);
            this.lbFps.Name = "lbFps";
            this.lbFps.Size = new System.Drawing.Size(35, 13);
            this.lbFps.TabIndex = 3;
            this.lbFps.Text = "label1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "FPS:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 77);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Bitrate";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 113);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(31, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Total";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 43);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(27, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Size";
            // 
            // lbSize
            // 
            this.lbSize.AutoSize = true;
            this.lbSize.Location = new System.Drawing.Point(62, 43);
            this.lbSize.Name = "lbSize";
            this.lbSize.Size = new System.Drawing.Size(35, 13);
            this.lbSize.TabIndex = 7;
            this.lbSize.Text = "label1";
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lbSize);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lbFps);
            this.Controls.Add(this.lbTotalBytes);
            this.Controls.Add(this.lbAverageBitrate);
            this.Controls.Add(this.btnReset);
            this.Name = "SettingsForm";
            this.Text = "SettingsForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Label lbAverageBitrate;
        private System.Windows.Forms.Label lbTotalBytes;
        private System.Windows.Forms.Label lbFps;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lbSize;
    }
}

