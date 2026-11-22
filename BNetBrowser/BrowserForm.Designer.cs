namespace BNetBrowser
{
    partial class BrowserForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.ToolStrip toolbar;
        private System.Windows.Forms.ToolStripButton btnBack;
        private System.Windows.Forms.ToolStripButton btnForward;
        private System.Windows.Forms.ToolStripButton btnRefresh;
        private System.Windows.Forms.ToolStripButton btnHome;
        private System.Windows.Forms.ToolStripSeparator sepLeft;
        private ToolStripSpringTextBox txtUrl;
        private System.Windows.Forms.ToolStripButton btnGo;
        private System.Windows.Forms.ToolStripSeparator sepRight;
        private System.Windows.Forms.ToolStripButton btnNewTab;
        private System.Windows.Forms.ToolStripButton btnCloseTab;
        private ModernTabControl tabs;
        private System.Windows.Forms.StatusStrip status;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private ToolStripModernProgress progress;
        private System.Windows.Forms.Timer trimTimer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BrowserForm));
            this.toolbar = new System.Windows.Forms.ToolStrip();
            this.btnBack = new System.Windows.Forms.ToolStripButton();
            this.btnForward = new System.Windows.Forms.ToolStripButton();
            this.btnRefresh = new System.Windows.Forms.ToolStripButton();
            this.btnHome = new System.Windows.Forms.ToolStripButton();
            this.sepLeft = new System.Windows.Forms.ToolStripSeparator();
            this.txtUrl = new BNetBrowser.ToolStripSpringTextBox();
            this.btnGo = new System.Windows.Forms.ToolStripButton();
            this.sepRight = new System.Windows.Forms.ToolStripSeparator();
            this.btnNewTab = new System.Windows.Forms.ToolStripButton();
            this.btnCloseTab = new System.Windows.Forms.ToolStripButton();
            this.tabs = new BNetBrowser.ModernTabControl();
            this.status = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.progress = new BNetBrowser.ToolStripModernProgress();
            this.trimTimer = new System.Windows.Forms.Timer(this.components);
            this.toolbar.SuspendLayout();
            this.status.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolbar
            // 
            this.toolbar.AutoSize = false;
            this.toolbar.BackColor = System.Drawing.Color.White;
            this.toolbar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolbar.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnBack,
            this.btnForward,
            this.btnRefresh,
            this.btnHome,
            this.sepLeft,
            this.txtUrl,
            this.btnGo,
            this.sepRight,
            this.btnNewTab,
            this.btnCloseTab});
            this.toolbar.Location = new System.Drawing.Point(0, 0);
            this.toolbar.Name = "toolbar";
            this.toolbar.Padding = new System.Windows.Forms.Padding(10, 8, 10, 9);
            this.toolbar.Size = new System.Drawing.Size(1467, 54);
            this.toolbar.TabIndex = 0;
            // 
            // btnBack
            // 
            this.btnBack.AutoSize = false;
            this.btnBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(36, 36);
            this.btnBack.Text = "◀";
            this.btnBack.ToolTipText = "Back (Alt+Left)";
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // btnForward
            // 
            this.btnForward.AutoSize = false;
            this.btnForward.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnForward.Name = "btnForward";
            this.btnForward.Size = new System.Drawing.Size(36, 36);
            this.btnForward.Text = "▶";
            this.btnForward.ToolTipText = "Forward (Alt+Right)";
            this.btnForward.Click += new System.EventHandler(this.btnForward_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.AutoSize = false;
            this.btnRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(36, 36);
            this.btnRefresh.Text = "⟳";
            this.btnRefresh.ToolTipText = "Refresh (F5)";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnHome
            // 
            this.btnHome.AutoSize = false;
            this.btnHome.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnHome.Name = "btnHome";
            this.btnHome.Size = new System.Drawing.Size(36, 36);
            this.btnHome.Text = "⌂";
            this.btnHome.ToolTipText = "Home (Alt+Home)";
            this.btnHome.Click += new System.EventHandler(this.btnHome_Click);
            // 
            // sepLeft
            // 
            this.sepLeft.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.sepLeft.Name = "sepLeft";
            this.sepLeft.Size = new System.Drawing.Size(6, 37);
            // 
            // txtUrl
            // 
            this.txtUrl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(243)))), ((int)(((byte)(246)))));
            this.txtUrl.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtUrl.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.txtUrl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(33)))), ((int)(((byte)(36)))));
            this.txtUrl.Margin = new System.Windows.Forms.Padding(16, 0, 14, 0);
            this.txtUrl.Name = "txtUrl";
            this.txtUrl.Size = new System.Drawing.Size(1082, 37);
            this.txtUrl.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtUrl_KeyDown);
            // 
            // btnGo
            // 
            this.btnGo.AutoSize = false;
            this.btnGo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnGo.Name = "btnGo";
            this.btnGo.Size = new System.Drawing.Size(52, 36);
            this.btnGo.Tag = "accent";
            this.btnGo.Text = "Go";
            this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
            // 
            // sepRight
            // 
            this.sepRight.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.sepRight.Name = "sepRight";
            this.sepRight.Size = new System.Drawing.Size(6, 37);
            // 
            // btnNewTab
            // 
            this.btnNewTab.AutoSize = false;
            this.btnNewTab.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnNewTab.Name = "btnNewTab";
            this.btnNewTab.Size = new System.Drawing.Size(36, 36);
            this.btnNewTab.Text = "+";
            this.btnNewTab.ToolTipText = "New tab (Ctrl+T)";
            this.btnNewTab.Click += new System.EventHandler(this.btnNewTab_Click);
            // 
            // btnCloseTab
            // 
            this.btnCloseTab.AutoSize = false;
            this.btnCloseTab.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnCloseTab.Name = "btnCloseTab";
            this.btnCloseTab.Size = new System.Drawing.Size(36, 36);
            this.btnCloseTab.Text = "✕";
            this.btnCloseTab.ToolTipText = "Close tab (Ctrl+W)";
            this.btnCloseTab.Click += new System.EventHandler(this.btnCloseTab_Click);
            // 
            // tabs
            // 
            this.tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabs.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tabs.ItemSize = new System.Drawing.Size(250, 48);
            this.tabs.Location = new System.Drawing.Point(0, 54);
            this.tabs.Name = "tabs";
            this.tabs.Padding = new System.Drawing.Point(0, 0);
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(1467, 752);
            this.tabs.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabs.TabIndex = 1;
            this.tabs.SelectedIndexChanged += new System.EventHandler(this.tabs_SelectedIndexChanged);
            // 
            // status
            // 
            this.status.AutoSize = false;
            this.status.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.status.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.status.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.progress});
            this.status.Location = new System.Drawing.Point(0, 806);
            this.status.Name = "status";
            this.status.Padding = new System.Windows.Forms.Padding(12, 0, 14, 0);
            this.status.Size = new System.Drawing.Size(1467, 28);
            this.status.SizingGrip = false;
            this.status.TabIndex = 2;
            // 
            // lblStatus
            // 
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1441, 22);
            this.lblStatus.Spring = true;
            this.lblStatus.Text = "Ready";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // progress
            // 
            this.progress.AutoSize = false;
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(140, 26);
            this.progress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progress.Value = 0;
            this.progress.Visible = false;
            // 
            // trimTimer
            // 
            this.trimTimer.Interval = 60000;
            this.trimTimer.Tick += new System.EventHandler(this.trimTimer_Tick);
            // 
            // BrowserForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1467, 834);
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.status);
            this.Controls.Add(this.toolbar);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "BrowserForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BNetBrowser";
            this.Load += new System.EventHandler(this.BrowserForm_Load);
            this.toolbar.ResumeLayout(false);
            this.toolbar.PerformLayout();
            this.status.ResumeLayout(false);
            this.status.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
    }
}