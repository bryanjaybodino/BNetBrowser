using System;
using System.IO;
using System.Windows.Forms;

namespace BNetBrowser
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Usage: BNetBrowser.exe [--kiosk] [--no-webview2] [--browser=name] [--lowmem] [url]
            bool kiosk = false;
            string argUrl = null;
            foreach (string a in args)
            {
                if (a.Equals("--kiosk", StringComparison.OrdinalIgnoreCase)) kiosk = true;
                else if (a.Equals("--lowmem", StringComparison.OrdinalIgnoreCase))
                    BrowserForm.LowMemory = true;
                else if (a.Equals("--no-webview2", StringComparison.OrdinalIgnoreCase))
                    BrowserForm.ForceFallback = true;
                else if (a.StartsWith("--browser=", StringComparison.OrdinalIgnoreCase))
                {
                    BrowserForm.PreferredBrowser = a.Substring("--browser=".Length).Trim();
                    BrowserForm.ForceFallback = true;
                }
                else if (argUrl == null && !string.IsNullOrWhiteSpace(a)) argUrl = a.Trim();
            }

            // Home page priority: command line -> home.txt (first line) -> default
            string home = "http://localhost/";
            string cfg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "home.txt");

            if (argUrl != null)
                home = argUrl;
            else if (File.Exists(cfg))
            {
                string[] lines = File.ReadAllLines(cfg);
                if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]))
                    home = lines[0].Trim();
            }

            Application.Run(new BrowserForm(home, kiosk));
        }
    }
}