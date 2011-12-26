namespace VersaVaultUpdater
{
    partial class Notification
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Notification));
            this.LblStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // LblStatus
            // 
            this.LblStatus.AutoSize = true;
            this.LblStatus.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LblStatus.Location = new System.Drawing.Point(0, 7);
            this.LblStatus.Name = "LblStatus";
            this.LblStatus.Size = new System.Drawing.Size(0, 15);
            this.LblStatus.TabIndex = 0;
            this.LblStatus.UseWaitCursor = true;
            // 
            // Notification
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(196, 30);
            this.Controls.Add(this.LblStatus);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Notification";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "VersaVault";
            this.TopMost = true;
            this.UseWaitCursor = true;
            this.Load += new System.EventHandler(this.NotificationLoad);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion Windows Form Designer generated code

        private System.Windows.Forms.Label LblStatus;
    }
}