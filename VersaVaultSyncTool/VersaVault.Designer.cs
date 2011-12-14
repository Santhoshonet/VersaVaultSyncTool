namespace VersaVaultSyncTool
{
    partial class VersaVault
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VersaVault));
            this.LblError = new System.Windows.Forms.Label();
            this.VersaVaultNotifications = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.startToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startSyncToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._watchFolder = new System.IO.FileSystemWatcher();
            this.timer_status_update = new System.Windows.Forms.Timer(this.components);
            this.connectBtn = new System.Windows.Forms.PictureBox();
            this.TxtPassword = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.passwordLbl = new System.Windows.Forms.Label();
            this.TxtUsername = new System.Windows.Forms.TextBox();
            this.fieldDescriptionLbl = new System.Windows.Forms.Label();
            this.emailIdLbl = new System.Windows.Forms.Label();
            this.closeWindow = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.contextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._watchFolder)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.connectBtn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.closeWindow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // LblError
            // 
            this.LblError.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LblError.ForeColor = System.Drawing.Color.Tomato;
            this.LblError.Location = new System.Drawing.Point(5, 240);
            this.LblError.Name = "LblError";
            this.LblError.Size = new System.Drawing.Size(208, 45);
            this.LblError.TabIndex = 5;
            this.LblError.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // VersaVaultNotifications
            // 
            this.VersaVaultNotifications.ContextMenuStrip = this.contextMenuStrip;
            this.VersaVaultNotifications.Icon = ((System.Drawing.Icon)(resources.GetObject("VersaVaultNotifications.Icon")));
            this.VersaVaultNotifications.Text = "VersaVault";
            this.VersaVaultNotifications.Visible = true;
            this.VersaVaultNotifications.DoubleClick += new System.EventHandler(this.VersaVaultNotificationsDoubleClick);
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.BackColor = System.Drawing.Color.WhiteSmoke;
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.startSyncToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.contextMenuStrip.Size = new System.Drawing.Size(262, 98);
            // 
            // startToolStripMenuItem
            // 
            this.startToolStripMenuItem.AutoToolTip = true;
            this.startToolStripMenuItem.Checked = true;
            this.startToolStripMenuItem.CheckOnClick = true;
            this.startToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.startToolStripMenuItem.Name = "startToolStripMenuItem";
            this.startToolStripMenuItem.Size = new System.Drawing.Size(261, 22);
            this.startToolStripMenuItem.Text = "Start AmazonSyn on system startup";
            this.startToolStripMenuItem.TextDirection = System.Windows.Forms.ToolStripTextDirection.Horizontal;
            this.startToolStripMenuItem.ToolTipText = "Start sync automatically when system started up.";
            this.startToolStripMenuItem.Click += new System.EventHandler(this.StartToolStripMenuItemClick);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(261, 22);
            this.settingsToolStripMenuItem.Text = "Se&ttings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.SettingsToolStripMenuItemClick);
            // 
            // startSyncToolStripMenuItem
            // 
            this.startSyncToolStripMenuItem.Name = "startSyncToolStripMenuItem";
            this.startSyncToolStripMenuItem.Size = new System.Drawing.Size(261, 22);
            this.startSyncToolStripMenuItem.Text = "Start Sync";
            this.startSyncToolStripMenuItem.Click += new System.EventHandler(this.StartSyncToolStripMenuItemClick);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(258, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(261, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItemClick);
            // 
            // _watchFolder
            // 
            this._watchFolder.EnableRaisingEvents = true;
            this._watchFolder.SynchronizingObject = this;
            // 
            // timer_status_update
            // 
            this.timer_status_update.Enabled = true;
            this.timer_status_update.Interval = 500;
            this.timer_status_update.Tick += new System.EventHandler(this.TimerStatusUpdateTick);
            // 
            // connectBtn
            // 
            this.connectBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.connectBtn.Image = global::VersaVaultSyncTool.Properties.Resources.connectButton_Normal;
            this.connectBtn.Location = new System.Drawing.Point(213, 240);
            this.connectBtn.Name = "connectBtn";
            this.connectBtn.Size = new System.Drawing.Size(177, 45);
            this.connectBtn.TabIndex = 19;
            this.connectBtn.TabStop = false;
            this.connectBtn.MouseLeave += new System.EventHandler(this.connectBtn_MouseLeave);
            this.connectBtn.Click += new System.EventHandler(this.BtnAuthenticateClick);
            this.connectBtn.MouseDown += new System.Windows.Forms.MouseEventHandler(this.connectBtn_MouseDown);
            this.connectBtn.MouseUp += new System.Windows.Forms.MouseEventHandler(this.connectBtn_MouseUp);
            this.connectBtn.MouseEnter += new System.EventHandler(this.connectBtn_MouseEnter);
            // 
            // TxtPassword
            // 
            this.TxtPassword.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TxtPassword.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.TxtPassword.Location = new System.Drawing.Point(7, 186);
            this.TxtPassword.Name = "TxtPassword";
            this.TxtPassword.PasswordChar = '*';
            this.TxtPassword.Size = new System.Drawing.Size(381, 29);
            this.TxtPassword.TabIndex = 18;
            this.TxtPassword.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtPasswordKeyPress);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Silver;
            this.label1.Location = new System.Drawing.Point(3, 160);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(314, 21);
            this.label1.TabIndex = 17;
            this.label1.Text = "Enter the password you signed up with here";
            // 
            // passwordLbl
            // 
            this.passwordLbl.AutoSize = true;
            this.passwordLbl.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.passwordLbl.ForeColor = System.Drawing.Color.Gray;
            this.passwordLbl.Location = new System.Drawing.Point(3, 139);
            this.passwordLbl.Name = "passwordLbl";
            this.passwordLbl.Size = new System.Drawing.Size(82, 21);
            this.passwordLbl.TabIndex = 16;
            this.passwordLbl.Text = "Password";
            // 
            // TxtUsername
            // 
            this.TxtUsername.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TxtUsername.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.TxtUsername.Location = new System.Drawing.Point(8, 92);
            this.TxtUsername.Name = "TxtUsername";
            this.TxtUsername.Size = new System.Drawing.Size(382, 29);
            this.TxtUsername.TabIndex = 15;
            // 
            // fieldDescriptionLbl
            // 
            this.fieldDescriptionLbl.AutoSize = true;
            this.fieldDescriptionLbl.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.fieldDescriptionLbl.ForeColor = System.Drawing.Color.Silver;
            this.fieldDescriptionLbl.Location = new System.Drawing.Point(4, 66);
            this.fieldDescriptionLbl.Name = "fieldDescriptionLbl";
            this.fieldDescriptionLbl.Size = new System.Drawing.Size(343, 21);
            this.fieldDescriptionLbl.TabIndex = 14;
            this.fieldDescriptionLbl.Text = "Enter the email address you signed up with here";
            // 
            // emailIdLbl
            // 
            this.emailIdLbl.AutoSize = true;
            this.emailIdLbl.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.emailIdLbl.ForeColor = System.Drawing.Color.Gray;
            this.emailIdLbl.Location = new System.Drawing.Point(4, 45);
            this.emailIdLbl.Name = "emailIdLbl";
            this.emailIdLbl.Size = new System.Drawing.Size(53, 21);
            this.emailIdLbl.TabIndex = 13;
            this.emailIdLbl.Text = "Email";
            // 
            // closeWindow
            // 
            this.closeWindow.Cursor = System.Windows.Forms.Cursors.Hand;
            this.closeWindow.Image = global::VersaVaultSyncTool.Properties.Resources.closeDefault;
            this.closeWindow.Location = new System.Drawing.Point(377, 12);
            this.closeWindow.Name = "closeWindow";
            this.closeWindow.Size = new System.Drawing.Size(12, 12);
            this.closeWindow.TabIndex = 12;
            this.closeWindow.TabStop = false;
            this.closeWindow.MouseLeave += new System.EventHandler(this.closeWindow_MouseLeave);
            this.closeWindow.Click += new System.EventHandler(this.closeWindow_Click);
            this.closeWindow.MouseEnter += new System.EventHandler(this.closeWindow_MouseEnter);
            // 
            // pictureBox2
            // 
            this.pictureBox2.Cursor = System.Windows.Forms.Cursors.SizeAll;
            this.pictureBox2.Image = global::VersaVaultSyncTool.Properties.Resources.topStrip;
            this.pictureBox2.Location = new System.Drawing.Point(13, 0);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(400, 4);
            this.pictureBox2.TabIndex = 11;
            this.pictureBox2.TabStop = false;
            this.pictureBox2.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Cursor = System.Windows.Forms.Cursors.SizeAll;
            this.pictureBox1.Image = global::VersaVaultSyncTool.Properties.Resources.windowsAppLogo;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(100, 30);
            this.pictureBox1.TabIndex = 10;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
            // 
            // VersaVault
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(407, 304);
            this.Controls.Add(this.connectBtn);
            this.Controls.Add(this.TxtPassword);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.passwordLbl);
            this.Controls.Add(this.TxtUsername);
            this.Controls.Add(this.fieldDescriptionLbl);
            this.Controls.Add(this.emailIdLbl);
            this.Controls.Add(this.closeWindow);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.LblError);
            this.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "VersaVault";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "VersaVault";
            this.Load += new System.EventHandler(this.VersaVaultLoad);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.VersaVaultFormClosed);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VersaVaultFormClosing);
            this.contextMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._watchFolder)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.connectBtn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.closeWindow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion Windows Form Designer generated code

        private System.Windows.Forms.NotifyIcon VersaVaultNotifications;
        private System.IO.FileSystemWatcher _watchFolder;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem startToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startSyncToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Label LblError;
        private System.Windows.Forms.Timer timer_status_update;
        private System.Windows.Forms.PictureBox connectBtn;
        private System.Windows.Forms.TextBox TxtPassword;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label passwordLbl;
        private System.Windows.Forms.TextBox TxtUsername;
        private System.Windows.Forms.Label fieldDescriptionLbl;
        private System.Windows.Forms.Label emailIdLbl;
        private System.Windows.Forms.PictureBox closeWindow;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}