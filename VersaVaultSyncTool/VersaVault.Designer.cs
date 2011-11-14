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
            this.PnlAuthentication = new System.Windows.Forms.Panel();
            this.LblError = new System.Windows.Forms.Label();
            this.BtnAuthenticate = new System.Windows.Forms.Button();
            this.TxtPassword = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.TxtUsername = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.VersaVaultNotifications = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.startToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startSyncToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._watchFolder = new System.IO.FileSystemWatcher();
            this.PnlAuthentication.SuspendLayout();
            this.contextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._watchFolder)).BeginInit();
            this.SuspendLayout();
            // 
            // PnlAuthentication
            // 
            this.PnlAuthentication.Controls.Add(this.LblError);
            this.PnlAuthentication.Controls.Add(this.BtnAuthenticate);
            this.PnlAuthentication.Controls.Add(this.TxtPassword);
            this.PnlAuthentication.Controls.Add(this.label2);
            this.PnlAuthentication.Controls.Add(this.TxtUsername);
            this.PnlAuthentication.Controls.Add(this.label1);
            this.PnlAuthentication.Dock = System.Windows.Forms.DockStyle.Left;
            this.PnlAuthentication.Location = new System.Drawing.Point(0, 0);
            this.PnlAuthentication.Name = "PnlAuthentication";
            this.PnlAuthentication.Size = new System.Drawing.Size(385, 144);
            this.PnlAuthentication.TabIndex = 0;
            // 
            // LblError
            // 
            this.LblError.ForeColor = System.Drawing.Color.OrangeRed;
            this.LblError.Location = new System.Drawing.Point(8, 97);
            this.LblError.Name = "LblError";
            this.LblError.Size = new System.Drawing.Size(262, 38);
            this.LblError.TabIndex = 5;
            // 
            // BtnAuthenticate
            // 
            this.BtnAuthenticate.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.BtnAuthenticate.Location = new System.Drawing.Point(276, 104);
            this.BtnAuthenticate.Name = "BtnAuthenticate";
            this.BtnAuthenticate.Size = new System.Drawing.Size(99, 31);
            this.BtnAuthenticate.TabIndex = 4;
            this.BtnAuthenticate.Text = "Authenticate";
            this.BtnAuthenticate.UseVisualStyleBackColor = true;
            this.BtnAuthenticate.Click += new System.EventHandler(this.BtnAuthenticate_Click);
            // 
            // TxtPassword
            // 
            this.TxtPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TxtPassword.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TxtPassword.Location = new System.Drawing.Point(11, 68);
            this.TxtPassword.Name = "TxtPassword";
            this.TxtPassword.PasswordChar = '*';
            this.TxtPassword.Size = new System.Drawing.Size(363, 21);
            this.TxtPassword.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 50);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "Password";
            // 
            // TxtUsername
            // 
            this.TxtUsername.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TxtUsername.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TxtUsername.Location = new System.Drawing.Point(11, 26);
            this.TxtUsername.Name = "TxtUsername";
            this.TxtUsername.Size = new System.Drawing.Size(363, 21);
            this.TxtUsername.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Username";
            // 
            // VersaVaultNotifications
            // 
            this.VersaVaultNotifications.ContextMenuStrip = this.contextMenuStrip;
            this.VersaVaultNotifications.Icon = ((System.Drawing.Icon)(resources.GetObject("VersaVaultNotifications.Icon")));
            this.VersaVaultNotifications.Text = "VersaVault";
            this.VersaVaultNotifications.Visible = true;
            this.VersaVaultNotifications.DoubleClick += new System.EventHandler(this.VersaVaultNotifications_DoubleClick);
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
            this.startToolStripMenuItem.Click += new System.EventHandler(this.startToolStripMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(261, 22);
            this.settingsToolStripMenuItem.Text = "Se&ttings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // startSyncToolStripMenuItem
            // 
            this.startSyncToolStripMenuItem.Name = "startSyncToolStripMenuItem";
            this.startSyncToolStripMenuItem.Size = new System.Drawing.Size(261, 22);
            this.startSyncToolStripMenuItem.Text = "Start Sync";
            this.startSyncToolStripMenuItem.Click += new System.EventHandler(this.startSyncToolStripMenuItem_Click);
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
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // _watchFolder
            // 
            this._watchFolder.EnableRaisingEvents = true;
            this._watchFolder.SynchronizingObject = this;
            // 
            // VersaVault
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(385, 144);
            this.Controls.Add(this.PnlAuthentication);
            this.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "VersaVault";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "VersaVault";
            this.Load += new System.EventHandler(this.VersaVault_Load);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.VersaVault_FormClosed);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VersaVault_FormClosing);
            this.PnlAuthentication.ResumeLayout(false);
            this.PnlAuthentication.PerformLayout();
            this.contextMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._watchFolder)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel PnlAuthentication;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox TxtUsername;
        private System.Windows.Forms.TextBox TxtPassword;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button BtnAuthenticate;
        private System.Windows.Forms.NotifyIcon VersaVaultNotifications;
        private System.IO.FileSystemWatcher _watchFolder;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem startToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startSyncToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Label LblError;
    }
}