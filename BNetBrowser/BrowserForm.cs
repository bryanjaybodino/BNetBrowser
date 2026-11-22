using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BNetBrowser
{
    public partial class BrowserForm : Form
    {
        protected override void OnLoad(EventArgs e)
        {
            ApplyTheme();
            base.OnLoad(e);   // raises Load -> BrowserForm_Load (unchanged)
        }

        private void ApplyTheme()
        {
            SuspendLayout();
            BackColor = Theme.Surface;
            LoadWindowIcon();

            // ---------- toolbar ----------
            toolbar.SuspendLayout();
            toolbar.Renderer = new ModernToolStripRenderer();
            toolbar.AutoSize = false;
            toolbar.Height = Theme.S(54);
            toolbar.Padding = new Padding(Theme.S(10), Theme.S(8), Theme.S(10), Theme.S(9));
            toolbar.BackColor = Theme.Surface;
            toolbar.GripStyle = ToolStripGripStyle.Hidden;

            StyleIconButton(btnBack, "\uE72B", "\u25C0");   // back
            StyleIconButton(btnForward, "\uE72A", "\u25B6");   // forward
            StyleIconButton(btnRefresh, "\uE72C", "\u27F3");   // refresh
            StyleIconButton(btnHome, "\uE80F", "\u2302");   // home
            StyleIconButton(btnNewTab, "\uE710", "+");        // new tab
            StyleIconButton(btnCloseTab, "\uE711", "\u2715");   // close tab

            sepLeft.Margin = new Padding(Theme.S(4), 0, Theme.S(4), 0);
            sepRight.Margin = new Padding(Theme.S(4), 0, Theme.S(4), 0);

            txtUrl.Font = new Font("Segoe UI", 10.5f);
            txtUrl.Margin = new Padding(Theme.S(16), 0, Theme.S(14), 0);

            btnGo.AutoSize = false;
            btnGo.Size = new Size(Theme.S(52), Theme.S(36));
            btnGo.Margin = new Padding(0, 0, Theme.S(2), 0);
            btnGo.Tag = "accent";
            btnGo.Font = new Font("Segoe UI Semibold", 9.5f);
            btnGo.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnGo.TextAlign = ContentAlignment.MiddleCenter;

            // hand cursor over every toolbar button
            foreach (ToolStripItem item in toolbar.Items)
                if (item is ToolStripButton)
                    EnableHandCursor(item);

            toolbar.ResumeLayout(true);

            // ---------- tabs ----------
            tabs.TabCloseRequested += delegate (int index)
            {
                tabs.SelectedIndex = index;
                CloseCurrentTab();
            };

            // ---------- status bar ----------
            status.Renderer = new ModernToolStripRenderer();
            status.AutoSize = false;
            status.Height = Theme.S(28);
            status.SizingGrip = false;
            status.BackColor = Theme.StatusBack;
            status.Padding = new Padding(Theme.S(12), 0, Theme.S(14), 0);

            lblStatus.Font = new Font("Segoe UI", 8.5f);
            lblStatus.ForeColor = Theme.TextMuted;

            progress.AutoSize = false;
            progress.Size = new Size(Theme.S(140), Theme.S(5));
            progress.Margin = new Padding(Theme.S(10), 0, 0, 0);

            ResumeLayout(true);
        }

        // Window / taskbar icon.
        // 1) icon.ico next to the exe (Copy to Output Directory = Copy always)
        // 2) icon.ico embedded in the exe (Build Action = Embedded Resource)
        private void LoadWindowIcon()
        {
            try
            {
                string file = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (System.IO.File.Exists(file))
                {
                    Icon = new Icon(file);
                    return;
                }

                foreach (string name in typeof(BrowserForm).Assembly.GetManifestResourceNames())
                {
                    if (name.EndsWith("icon.ico", StringComparison.OrdinalIgnoreCase))
                    {
                        using (System.IO.Stream s = typeof(BrowserForm).Assembly.GetManifestResourceStream(name))
                            Icon = new Icon(s);
                        return;
                    }
                }
            }
            catch { }
        }

        // ToolStripItems have no usable Cursor property, so we switch the
        // parent ToolStrip's cursor while the mouse is over an enabled item.
        private static void EnableHandCursor(ToolStripItem item)
        {
            item.MouseEnter += delegate
            {
                if (item.Enabled && item.Owner != null)
                    item.Owner.Cursor = Cursors.Hand;
            };
            item.MouseLeave += delegate
            {
                if (item.Owner != null)
                    item.Owner.Cursor = Cursors.Default;
            };
        }

        private static void StyleIconButton(ToolStripButton b, string glyph, string fallback)
        {
            b.AutoSize = false;
            b.Size = new Size(Theme.S(36), Theme.S(36));
            b.Margin = new Padding(Theme.S(1), 0, Theme.S(1), 0);
            b.DisplayStyle = ToolStripItemDisplayStyle.Text;
            b.TextAlign = ContentAlignment.MiddleCenter;

            if (Theme.HasIcons)
            {
                b.Font = new Font(Theme.IconFontName, 10f);
                b.Text = glyph;
            }
            else
            {
                b.Font = new Font("Segoe UI Symbol", 11f);
                b.Text = fallback;
            }
        }
        private const string AppName = "BNetBrowser";
        private readonly string homeUrl;
        private readonly bool kioskMode;

        // fullscreen toggle (F11) - normal mode only
        private bool isFullscreen = false;
        private FormBorderStyle oldBorder;
        private FormWindowState oldState;

        // kiosk mode
        private WebView2 kioskBrowser;
        private bool waiting = false;

        // WebView2 profile folder (cookies, cache). Must be writable.
        private static readonly string DataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BNetBrowser", "WebView2");

        public BrowserForm() : this("http://localhost/", false)
        {
        }

        public BrowserForm(string url) : this(url, false)
        {
        }

        public BrowserForm(string url, bool kiosk)
        {
            InitializeComponent();
            homeUrl = url;
            kioskMode = kiosk;
        }

        // ================= Form events =================

        private void BrowserForm_Load(object sender, EventArgs e)
        {
            trimTimer.Start();

            if (kioskMode)
            {
                StartKiosk();
                return;
            }

            AddTab(homeUrl);
        }

        private void trimTimer_Tick(object sender, EventArgs e)
        {
            TrimMemory();
        }

        // ================= WebView2 setup =================

        // All WebView2 controls in this process share the same profile + arguments.
        private async Task<bool> InitWebView(WebView2 wb)
        {
            try
            {
                wb.CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = DataFolder,
                    // disable swipe back/forward and pinch zoom (touch screens)
                    AdditionalBrowserArguments = "--overscroll-history-navigation=0 --disable-pinch"
                };

                await wb.EnsureCoreWebView2Async(null);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not start the WebView2 (Edge) runtime.\n\n" +
                    "Install the 'Evergreen WebView2 Runtime' from Microsoft:\n" +
                    "https://go.microsoft.com/fwlink/p/?LinkId=2124703\n\n" + ex.Message,
                    AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ================= Toolbar events =================

        private void btnBack_Click(object sender, EventArgs e)
        {
            var wb = Current();
            if (wb != null && wb.CoreWebView2 != null && wb.CoreWebView2.CanGoBack) wb.CoreWebView2.GoBack();
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            var wb = Current();
            if (wb != null && wb.CoreWebView2 != null && wb.CoreWebView2.CanGoForward) wb.CoreWebView2.GoForward();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            var wb = Current();
            if (wb != null && wb.CoreWebView2 != null) wb.CoreWebView2.Reload();
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            NavigateCurrent(homeUrl);
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            NavigateCurrent(txtUrl.Text);
        }

        private void btnNewTab_Click(object sender, EventArgs e)
        {
            AddTab(homeUrl);
        }

        private void btnCloseTab_Click(object sender, EventArgs e)
        {
            CloseCurrentTab();
        }

        private void txtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                NavigateCurrent(txtUrl.Text);
            }
        }

        private void tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            SyncUiToCurrent();
        }

        // ================= Tabs (normal mode) =================

        private WebView2 Current()
        {
            if (tabs.SelectedTab == null || tabs.SelectedTab.Controls.Count == 0) return null;
            return tabs.SelectedTab.Controls[0] as WebView2;
        }

        private async void AddTab(string url)
        {
            var page = new TabPage("New Tab");
            var wb = new WebView2
            {
                Dock = DockStyle.Fill,
                Tag = page
            };

            wb.NavigationStarting += Wb_NavigationStarting;
            wb.NavigationCompleted += Wb_NavigationCompleted;
            wb.SourceChanged += Wb_SourceChanged;

            page.Controls.Add(wb);
            tabs.TabPages.Add(page);
            tabs.SelectedTab = page;

            if (!await InitWebView(wb)) return;
            if (wb.IsDisposed) return;   // tab was closed while starting

            var core = wb.CoreWebView2;
            core.Settings.AreDevToolsEnabled = true;
            core.DocumentTitleChanged += (s, e) => UpdateTitle(wb);
            core.StatusBarTextChanged += (s, e) =>
            {
                if (wb == Current() && !string.IsNullOrEmpty(core.StatusBarText))
                    lblStatus.Text = core.StatusBarText;
            };

            // target=_blank / popups open in a new tab
            core.NewWindowRequested += (s, e) =>
            {
                e.Handled = true;
                string target = e.Uri;
                BeginInvoke((Action)(() => AddTab(target)));
            };

            if (!string.IsNullOrWhiteSpace(url))
                core.Navigate(NormalizeUrl(url));

            SyncUiToCurrent();
        }

        private void CloseCurrentTab()
        {
            var page = tabs.SelectedTab;
            if (page == null) return;

            // Always keep at least one tab
            if (tabs.TabPages.Count == 1)
            {
                NavigateCurrent(homeUrl);
                return;
            }

            var wb = Current();
            tabs.TabPages.Remove(page);
            if (wb != null) wb.Dispose();   // release memory of the closed tab
            page.Dispose();
            TrimMemory();
        }

        // ================= WebView2 events (normal mode) =================

        private void Wb_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (sender == Current())
            {
                lblStatus.Text = "Loading...";
                progress.Style = ProgressBarStyle.Marquee;
                progress.Visible = true;
            }
        }

        private void Wb_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (sender == Current())
            {
                lblStatus.Text = e.IsSuccess ? "Done" : "Failed: " + e.WebErrorStatus;
                progress.Visible = false;
                UpdateNavButtons();
            }
        }

        private void Wb_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            var wb = (WebView2)sender;
            if (wb == Current() && wb.Source != null) txtUrl.Text = wb.Source.ToString();
        }

        private void UpdateTitle(WebView2 wb)
        {
            if (wb.IsDisposed || wb.CoreWebView2 == null) return;

            string title = string.IsNullOrWhiteSpace(wb.CoreWebView2.DocumentTitle)
                ? "New Tab" : wb.CoreWebView2.DocumentTitle;
            string tabTitle = title.Length > 22 ? title.Substring(0, 22) + "…" : title;
            ((TabPage)wb.Tag).Text = tabTitle;

            if (wb == Current()) Text = title + " - " + AppName;
        }

        // ================= Navigation helpers =================

        private void NavigateCurrent(string text)
        {
            var wb = Current();
            if (wb == null || wb.CoreWebView2 == null || string.IsNullOrWhiteSpace(text)) return;
            try { wb.CoreWebView2.Navigate(NormalizeUrl(text)); } catch { }
        }

        private static string NormalizeUrl(string input)
        {
            input = input.Trim();

            // Has a space or no dot (and isn't localhost / URL) -> treat as a search
            bool looksLikeUrl = input.Contains("://") ||
                                input.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) ||
                                (input.Contains(".") && !input.Contains(" "));
            if (!looksLikeUrl)
                return "https://www.google.com/search?q=" + Uri.EscapeDataString(input);

            if (!input.Contains("://")) input = "http://" + input;
            return input;
        }

        private void SyncUiToCurrent()
        {
            var wb = Current();
            if (wb == null) return;

            txtUrl.Text = wb.Source == null ? "" : wb.Source.ToString();

            string title = wb.CoreWebView2 == null ? null : wb.CoreWebView2.DocumentTitle;
            Text = string.IsNullOrWhiteSpace(title) ? AppName : title + " - " + AppName;
            UpdateNavButtons();
        }

        private void UpdateNavButtons()
        {
            var wb = Current();
            bool ready = wb != null && wb.CoreWebView2 != null;
            btnBack.Enabled = ready && wb.CoreWebView2.CanGoBack;
            btnForward.Enabled = ready && wb.CoreWebView2.CanGoForward;
        }

        // ================= Kiosk mode =================

        // Modern CSS is fine now, so this is the same look as kiosk-loader.html
        private const string LoadingHtml =
            "<!DOCTYPE html><html><head><meta charset='utf-8'>" +
            "<style>html,body{height:100%;margin:0;background:#111;color:#fff;" +
            "font-family:Segoe UI,Arial,sans-serif;cursor:none;}" +
            ".w{height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;}" +
            ".s{width:70px;height:70px;border:7px solid #333;border-top-color:#4fc3f7;border-radius:50%;" +
            "animation:spin 1s linear infinite;}" +
            "h1{font-weight:300;margin-top:30px;}@keyframes spin{to{transform:rotate(360deg);}}</style></head>" +
            "<body><div class='w'><div class='s'></div><h1>Starting up, please wait...</h1></div></body></html>";

        private async void StartKiosk()
        {
            toolbar.Visible = false;
            status.Visible = false;
            tabs.Visible = false;

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;

            kioskBrowser = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.FromArgb(17, 17, 17)
            };
            Controls.Add(kioskBrowser);
            kioskBrowser.BringToFront();

            if (!await InitWebView(kioskBrowser)) return;

            var core = kioskBrowser.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.IsSwipeNavigationEnabled = false;
            core.Settings.IsBuiltInErrorPageEnabled = false;   // we show our own waiting screen

            // links that try to open a new window stay in this window
            core.NewWindowRequested += (s, e) =>
            {
                e.Handled = true;
                core.Navigate(e.Uri);
            };

            // server not reachable -> go back to the waiting screen
            kioskBrowser.NavigationCompleted += (s, e) =>
            {
                if (!e.IsSuccess && !waiting && IsServerDown(e.WebErrorStatus))
                    StartWaiting();
            };

            StartWaiting();
        }

        private static bool IsServerDown(CoreWebView2WebErrorStatus s)
        {
            return s == CoreWebView2WebErrorStatus.CannotConnect ||
                   s == CoreWebView2WebErrorStatus.ConnectionAborted ||
                   s == CoreWebView2WebErrorStatus.ConnectionReset ||
                   s == CoreWebView2WebErrorStatus.HostNameNotResolved ||
                   s == CoreWebView2WebErrorStatus.ServerUnreachable ||
                   s == CoreWebView2WebErrorStatus.Timeout ||
                   s == CoreWebView2WebErrorStatus.Disconnected;
        }

        // Shows the loading screen, waits until the server answers, then opens the site
        private async void StartWaiting()
        {
            if (waiting) return;
            waiting = true;

            kioskBrowser.CoreWebView2.NavigateToString(LoadingHtml);

            while (!IsDisposed)
            {
                bool up = await Task.Run(() => ServerAnswers(homeUrl));
                if (up) break;
                await Task.Delay(2000);
            }

            if (!IsDisposed) await Task.Delay(5000);   // settle time after the server first answers
            if (!IsDisposed) kioskBrowser.CoreWebView2.Navigate(homeUrl);

            waiting = false;
        }

        // true if the server replies at all (even with an HTTP error page)
        private static bool ServerAnswers(string url)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Timeout = 3000;
                req.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                using (req.GetResponse()) { return true; }
            }
            catch (WebException ex)
            {
                if (ex.Response != null) { ex.Response.Close(); return true; }
                return false;
            }
            catch { return false; }
        }

        // ================= Keyboard shortcuts =================
        // Note: when the page has focus, WebView2 handles keys itself (F5, Ctrl+R work natively).
        // These shortcuts work when the toolbar / form has focus.

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (kioskMode)
            {
                if (keyData == Keys.F5 && kioskBrowser != null && kioskBrowser.CoreWebView2 != null)
                {
                    kioskBrowser.CoreWebView2.Reload();
                    return true;
                }
                return base.ProcessCmdKey(ref msg, keyData);
            }

            switch (keyData)
            {
                case Keys.Control | Keys.T:
                    AddTab(homeUrl);
                    return true;

                case Keys.Control | Keys.W:
                    CloseCurrentTab();
                    return true;

                case Keys.Control | Keys.L:
                case Keys.Alt | Keys.D:
                    txtUrl.Focus();
                    txtUrl.SelectAll();
                    return true;

                case Keys.F5:
                    btnRefresh_Click(null, EventArgs.Empty);
                    return true;

                case Keys.Alt | Keys.Home:
                    NavigateCurrent(homeUrl);
                    return true;

                case Keys.F11:
                    ToggleFullscreen();
                    return true;

                case Keys.Escape:
                    if (isFullscreen)
                    {
                        ToggleFullscreen();
                        return true;
                    }
                    break;

                case Keys.Control | Keys.Tab:
                    if (tabs.TabCount > 1)
                        tabs.SelectedIndex = (tabs.SelectedIndex + 1) % tabs.TabCount;
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ToggleFullscreen()
        {
            if (!isFullscreen)
            {
                oldBorder = FormBorderStyle;
                oldState = WindowState;
                toolbar.Visible = false;
                status.Visible = false;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Normal;
                WindowState = FormWindowState.Maximized;
                isFullscreen = true;
            }
            else
            {
                FormBorderStyle = oldBorder;
                WindowState = oldState;
                toolbar.Visible = true;
                status.Visible = true;
                isFullscreen = false;
            }
        }

        // ================= Memory =================

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        private static void TrimMemory()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
            }
            catch { }
        }
    }
}