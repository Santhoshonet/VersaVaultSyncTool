namespace VersaVaultSyncTool
{
    partial class Control
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Lblstatus = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.Lblfilename = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Lblstatus
            // 
            this.Lblstatus.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.Lblstatus.Font = new System.Drawing.Font("Arial Narrow", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Lblstatus.Location = new System.Drawing.Point(227, 2);
            this.Lblstatus.Name = "Lblstatus";
            this.Lblstatus.Size = new System.Drawing.Size(71, 16);
            this.Lblstatus.TabIndex = 5;
            this.Lblstatus.Text = "Downloading";
            this.Lblstatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // progressBar1
            // 
            this.progressBar1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.progressBar1.Location = new System.Drawing.Point(9, 18);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(289, 14);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 4;
            // 
            // Lblfilename
            // 
            this.Lblfilename.AutoSize = true;
            this.Lblfilename.Font = new System.Drawing.Font("Arial Narrow", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Lblfilename.Location = new System.Drawing.Point(6, 33);
            this.Lblfilename.Name = "Lblfilename";
            this.Lblfilename.Size = new System.Drawing.Size(80, 16);
            this.Lblfilename.TabIndex = 3;
            this.Lblfilename.Text = "ACBDEFGH.jpg";
            // 
            // Control
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.Controls.Add(this.Lblstatus);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.Lblfilename);
            this.Name = "Control";
            this.Size = new System.Drawing.Size(300, 50);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Lblstatus;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label Lblfilename;
    }
}
