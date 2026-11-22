using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
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

        // Folder for the fallback browsers' kiosk profiles (kept apart from the user's own profiles)
        private static readonly string FallbackProfileRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BNetBrowser");

        // optional bundled WebView2 runtime next to the exe
        private static string FixedRuntimeFolder
        {
            get
            {
                string f = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebView2Runtime");
                return Directory.Exists(f) ? f : null;
            }
        }

        // true once we handed over to another browser (no WebView2 / Edge runtime)
        private bool browserFallbackActive = false;

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
                    BrowserExecutableFolder = FixedRuntimeFolder,
                    AdditionalBrowserArguments = "--overscroll-history-navigation=0 --disable-pinch " + LightArgsCommon + ExtraArgs()
                };

                await wb.EnsureCoreWebView2Async(null);
                return true;
            }
            catch (Exception ex)
            {
                // WebView2 failed to start -> another installed browser is the alternative
                LogFallback(ex);
                if (UseBrowserFallback()) return false;

                MessageBox.Show(
                    "Could not start the WebView2 (Edge) runtime, and no supported browser " +
                    "(Chrome, Brave, Edge, Firefox, Vivaldi) was found.\n\n" +
                    "Install the 'Evergreen WebView2 Runtime' from Microsoft:\n" +
                    "https://go.microsoft.com/fwlink/p/?LinkId=2124703\n\n" + ex.Message,
                    AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ================= Low resource mode =================
        // The pages only use Serial Port, WebSocket and SSE, so everything else
        // (sync, updates, telemetry, translate, autofill, phishing checks, extra
        // renderer processes ...) is switched off. Nothing here touches networking
        // sockets, SSE or Web Serial.
        //
        // Optional: put extra command-line flags on the first line of extra-args.txt
        // next to the exe (for example --disable-gpu or --js-flags=--max-old-space-size=256).

        private const string LightArgsCommon =
            "--renderer-process-limit=1 --in-process-gpu --enable-low-end-device-mode " +
            "--disable-background-networking --disable-sync --disable-default-apps " +
            "--disable-component-update --disable-breakpad --disable-domain-reliability " +
            "--disable-client-side-phishing-detection --metrics-recording-only --no-pings " +
            "--disable-features=Translate,MediaRouter,OptimizationHints,AutofillServerCommunication," +
            "InterestFeedContentSuggestions,msSmartScreenProtection," +
            "IsolateOrigins,site-per-process,NetworkServiceInProcess,NetworkServiceInProcess2";

        private static string ExtraArgs()
        {
            try
            {
                string f = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "extra-args.txt");
                if (File.Exists(f))
                {
                    string[] lines = File.ReadAllLines(f);
                    if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]))
                        return " " + lines[0].Trim();
                }
            }
            catch { }
            return "";
        }

        // Runtime settings for a WebView2 that is already created.
        private static void ApplyLightSettings(CoreWebView2 core)
        {
            try
            {
                core.Settings.IsPasswordAutosaveEnabled = false;
                core.Settings.IsGeneralAutofillEnabled = false;
            }
            catch { }

            // Ask the engine to use less memory (needs a newer WebView2 SDK/runtime).
            // Done with reflection so older SDK versions still compile and just skip it.
            try
            {
                var prop = typeof(CoreWebView2).GetProperty("MemoryUsageTargetLevel");
                if (prop != null && prop.CanWrite)
                    prop.SetValue(core, Enum.ToObject(prop.PropertyType, 1), null);   // 1 = Low
            }
            catch { }
        }

        // Writes why WebView2 failed to %LocalAppData%\BNetBrowser\fallback.log
        private static void LogFallback(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(FallbackProfileRoot);
                File.AppendAllText(Path.Combine(FallbackProfileRoot, "fallback.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  WebView2 failed: " + ex + "\r\n\r\n");
            }
            catch { }
        }

        // ================= Browser fallback =================
        // Used when the WebView2 (Edge) runtime is missing. Supports Chrome, Brave,
        // Edge, Firefox and Vivaldi. Optional: put a browser name (chrome / brave /
        // edge / firefox / vivaldi) on the first line of browser.txt next to the exe
        // to choose which one is tried first.

        private sealed class ExternalBrowser
        {
            public string Name;
            public string Exe;
            public bool IsFirefox;
        }

        private sealed class Candidate
        {
            public string Name;
            public string ExeName;      // for the registry "App Paths" lookup
            public string RelPath;      // under Program Files / Program Files (x86) / LocalAppData
            public bool IsFirefox;
            public Candidate(string name, string exeName, string relPath, bool firefox)
            {
                Name = name; ExeName = exeName; RelPath = relPath; IsFirefox = firefox;
            }
        }

        // default order = order of preference
        private static readonly Candidate[] Candidates =
        {
            new Candidate("Chrome",  "chrome.exe",  @"Google\Chrome\Application\chrome.exe", false),
            new Candidate("Brave",   "brave.exe",   @"BraveSoftware\Brave-Browser\Application\brave.exe", false),
            new Candidate("Edge",    "msedge.exe",  @"Microsoft\Edge\Application\msedge.exe", false),
            new Candidate("Firefox", "firefox.exe", @"Mozilla Firefox\firefox.exe", true),
            new Candidate("Vivaldi", "vivaldi.exe", @"Vivaldi\Application\vivaldi.exe", false)
        };

        private static ExternalBrowser Locate(Candidate c)
        {
            // 1) registry App Paths
            try
            {
                string[] keys =
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + c.ExeName,
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\" + c.ExeName
                };
                RegistryKey[] roots = { Registry.LocalMachine, Registry.CurrentUser };

                foreach (RegistryKey root in roots)
                    foreach (string k in keys)
                        using (RegistryKey key = root.OpenSubKey(k))
                        {
                            string p = key == null ? null : key.GetValue(null) as string;
                            if (!string.IsNullOrEmpty(p))
                            {
                                p = p.Trim('"');
                                if (File.Exists(p))
                                    return new ExternalBrowser { Name = c.Name, Exe = p, IsFirefox = c.IsFirefox };
                            }
                        }
            }
            catch { }

            // 2) usual install folders
            string[] bases =
            {
                Environment.GetEnvironmentVariable("ProgramFiles"),
                Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
                Environment.GetEnvironmentVariable("LocalAppData")
            };

            foreach (string b in bases)
            {
                if (string.IsNullOrEmpty(b)) continue;
                string p = Path.Combine(b, c.RelPath);
                if (File.Exists(p))
                    return new ExternalBrowser { Name = c.Name, Exe = p, IsFirefox = c.IsFirefox };
            }
            return null;
        }

        private static ExternalBrowser FindBrowser()
        {
            string preferred = null;
            try
            {
                string cfg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "browser.txt");
                if (File.Exists(cfg))
                {
                    string[] lines = File.ReadAllLines(cfg);
                    if (lines.Length > 0) preferred = lines[0].Trim();
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(preferred))
            {
                foreach (Candidate c in Candidates)
                {
                    if (!c.Name.Equals(preferred, StringComparison.OrdinalIgnoreCase)) continue;
                    ExternalBrowser pb = Locate(c);
                    if (pb != null) return pb;
                }
            }

            foreach (Candidate c in Candidates)
            {
                ExternalBrowser b = Locate(c);
                if (b != null) return b;
            }
            return null;
        }

        // Hands over to another installed browser. Returns false if none found / can't start.
        private bool UseBrowserFallback()
        {
            if (browserFallbackActive) return true;

            ExternalBrowser browser = FindBrowser();
            if (browser == null) return false;

            browserFallbackActive = true;

            if (kioskMode)
            {
                StartExternalKiosk(browser);
                return true;
            }

            try
            {
                string url = NormalizeUrl(homeUrl).Replace("\"", "%22");
                string args = browser.IsFirefox
                    ? "-new-window \"" + url + "\""
                    : "--new-window " + LightArgsCommon + ExtraArgs() + " \"" + url + "\"";

                Process.Start(new ProcessStartInfo(browser.Exe, args) { UseShellExecute = false });
            }
            catch
            {
                browserFallbackActive = false;
                return false;
            }

            // the browser is open - this window has nothing left to do
            Opacity = 0;
            ShowInTaskbar = false;
            BeginInvoke((Action)Close);
            return true;
        }

        private static string BuildKioskArgs(ExternalBrowser browser, string url)
        {
            string profile = Path.Combine(FallbackProfileRoot, "Kiosk_" + browser.Name);
            Directory.CreateDirectory(profile);

            if (browser.IsFirefox)
            {
                // keep the first-run / welcome pages out of the kiosk
                try
                {
                    File.WriteAllText(Path.Combine(profile, "user.js"),
                        "user_pref(\"browser.aboutwelcome.enabled\", false);\r\n" +
                        "user_pref(\"startup.homepage_welcome_url\", \"\");\r\n" +
                        "user_pref(\"startup.homepage_welcome_url.additional\", \"\");\r\n" +
                        "user_pref(\"browser.startup.homepage_override.mstone\", \"ignore\");\r\n" +
                        "user_pref(\"browser.shell.checkDefaultBrowser\", false);\r\n" +
                        "user_pref(\"datareporting.policy.dataSubmissionPolicyBypassNotification\", true);\r\n" +
                        "user_pref(\"browser.sessionstore.resume_from_crash\", false);\r\n" +
                        // low resource mode
                        "user_pref(\"dom.ipc.processCount\", 1);\r\n" +
                        "user_pref(\"browser.sessionhistory.max_total_viewers\", 0);\r\n" +
                        "user_pref(\"browser.cache.disk.enable\", false);\r\n" +
                        "user_pref(\"browser.cache.memory.capacity\", 32768);\r\n" +
                        "user_pref(\"browser.tabs.unloadOnLowMemory\", true);\r\n" +
                        "user_pref(\"browser.safebrowsing.malware.enabled\", false);\r\n" +
                        "user_pref(\"browser.safebrowsing.phishing.enabled\", false);\r\n" +
                        "user_pref(\"network.prefetch-next\", false);\r\n" +
                        "user_pref(\"network.dns.disablePrefetch\", true);\r\n" +
                        "user_pref(\"extensions.pocket.enabled\", false);\r\n" +
                        "user_pref(\"app.update.auto\", false);\r\n" +
                        "user_pref(\"app.update.enabled\", false);\r\n" +
                        "user_pref(\"toolkit.telemetry.enabled\", false);\r\n" +
                        "user_pref(\"datareporting.healthreport.uploadEnabled\", false);\r\n" +
                        "user_pref(\"browser.newtabpage.activity-stream.feeds.telemetry\", false);\r\n");
                }
                catch { }

                return "-no-remote -profile \"" + profile + "\" -kiosk \"" + url + "\"";
            }

            return "--kiosk --no-first-run --no-default-browser-check " +
                   "--disable-pinch --overscroll-history-navigation=0 " +
                   "--disable-infobars --noerrdialogs --disable-session-crashed-bubble " +
                   "--disable-extensions " + LightArgsCommon + ExtraArgs() + " " +
                   "--user-data-dir=\"" + profile + "\" \"" + url + "\"";
        }

        // Kiosk via an external browser: show the waiting screen until the server answers,
        // start the browser in kiosk mode, and exit when it is closed.
        private async void StartExternalKiosk(ExternalBrowser browser)
        {
            try
            {
                toolbar.Visible = false;
                status.Visible = false;
                tabs.Visible = false;

                if (kioskBrowser != null)
                {
                    Controls.Remove(kioskBrowser);
                    kioskBrowser.Dispose();
                    kioskBrowser = null;
                }

                BackColor = Color.FromArgb(17, 17, 17);
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;

                var lbl = new Label
                {
                    Dock = DockStyle.Fill,
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(17, 17, 17),
                    Font = new Font("Segoe UI Light", 28f),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text = "Starting up, please wait...",
                    Cursor = Cursors.WaitCursor
                };
                Controls.Add(lbl);
                lbl.BringToFront();

                string url = NormalizeUrl(homeUrl).Replace("\"", "%22");

                while (!IsDisposed)
                {
                    bool up = await Task.Run(() => ServerAnswers(homeUrl));
                    if (up) break;
                    await Task.Delay(2000);
                }
                if (IsDisposed) return;

                await Task.Delay(5000);   // settle time after the server first answers
                if (IsDisposed) return;

                Process p = Process.Start(new ProcessStartInfo(browser.Exe, BuildKioskArgs(browser, url))
                {
                    UseShellExecute = false
                });

                Hide();   // let the browser own the screen

                DateTime started = DateTime.UtcNow;
                if (p != null) await Task.Run(() => p.WaitForExit());

                // Some browsers (Firefox, Edge) start through a small launcher process that
                // exits right away while the real browser keeps running. If that happened,
                // keep waiting until no process of that browser is left, otherwise the .bat
                // would relaunch the kiosk again and again.
                if ((DateTime.UtcNow - started).TotalSeconds < 15)
                {
                    string procName = Path.GetFileNameWithoutExtension(browser.Exe);
                    await Task.Delay(3000);
                    while (!IsDisposed && Process.GetProcessesByName(procName).Length > 0)
                        await Task.Delay(2000);
                }

                if (!IsDisposed) Close();
            }
            catch (Exception ex)
            {
                if (IsDisposed) return;
                MessageBox.Show("Could not start " + browser.Name + ".\n\n" + ex.Message,
                    AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
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
            ApplyLightSettings(core);
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
            ApplyLightSettings(core);
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

        // ---- trim the browser child processes (msedgewebview2, chrome, brave, firefox ...) ----

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint pid);

        [DllImport("kernel32.dll", EntryPoint = "Process32FirstW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Process32First(IntPtr snapshot, ref PROCESSENTRY32 entry);

        [DllImport("kernel32.dll", EntryPoint = "Process32NextW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Process32Next(IntPtr snapshot, ref PROCESSENTRY32 entry);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EmptyWorkingSet(IntPtr process);

        // Empties the working set of every process that was started by this app
        // (all descendants). RAM comes back only for what the page really uses.
        private static void TrimChildProcesses()
        {
            IntPtr snap = CreateToolhelp32Snapshot(0x00000002, 0);   // TH32CS_SNAPPROCESS
            if (snap == IntPtr.Zero || snap == new IntPtr(-1)) return;

            var parentOf = new Dictionary<uint, uint>();
            try
            {
                var e = new PROCESSENTRY32();
                e.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));
                if (Process32First(snap, ref e))
                {
                    do { parentOf[e.th32ProcessID] = e.th32ParentProcessID; }
                    while (Process32Next(snap, ref e));
                }
            }
            finally { CloseHandle(snap); }

            uint me = (uint)Process.GetCurrentProcess().Id;
            var family = new HashSet<uint> { me };
            bool added = true;
            while (added)
            {
                added = false;
                foreach (KeyValuePair<uint, uint> kv in parentOf)
                {
                    if (!family.Contains(kv.Key) && family.Contains(kv.Value))
                    {
                        family.Add(kv.Key);
                        added = true;
                    }
                }
            }
            family.Remove(me);

            foreach (uint pid in family)
            {
                // PROCESS_QUERY_INFORMATION | PROCESS_SET_QUOTA
                IntPtr h = OpenProcess(0x0400 | 0x0100, false, pid);
                if (h == IntPtr.Zero) continue;
                try { EmptyWorkingSet(h); }
                finally { CloseHandle(h); }
            }
        }

        private static void TrimMemory()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
                TrimChildProcesses();   // WebView2 / Chrome / Brave / Firefox processes we started
            }
            catch { }
        }
    }
}