using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace BNetBrowser
{
    // ======================================================================
    //  Colors, fonts, helpers
    // ======================================================================
    internal static class Theme
    {
        public static readonly Color Accent = Color.FromArgb(26, 115, 232);
        public static readonly Color AccentHover = Color.FromArgb(23, 101, 204);
        public static readonly Color AccentPressed = Color.FromArgb(21, 87, 176);

        public static readonly Color Surface = Color.White;
        public static readonly Color StripBack = Color.FromArgb(233, 236, 241);
        public static readonly Color TabHover = Color.FromArgb(220, 224, 230);
        public static readonly Color StatusBack = Color.FromArgb(248, 249, 250);
        public static readonly Color Border = Color.FromArgb(227, 230, 234);

        public static readonly Color Hover = Color.FromArgb(235, 237, 240);
        public static readonly Color Pressed = Color.FromArgb(220, 223, 228);

        public static readonly Color Field = Color.FromArgb(241, 243, 246);

        public static readonly Color TextPrimary = Color.FromArgb(32, 33, 36);
        public static readonly Color TextMuted = Color.FromArgb(95, 99, 104);
        public static readonly Color TextDisabled = Color.FromArgb(173, 177, 183);

        private static float scale = 0f;

        // scale a pixel value for the current screen DPI
        public static int S(int px)
        {
            if (scale <= 0f)
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                    scale = g.DpiX / 96f;
            }
            return (int)Math.Round(px * scale);
        }

        // Windows 11: Segoe Fluent Icons, Windows 10: Segoe MDL2 Assets
        public static readonly string IconFontName = DetectIconFont();

        public static bool HasIcons { get { return IconFontName != null; } }

        private static string DetectIconFont()
        {
            string found = null;
            using (InstalledFontCollection c = new InstalledFontCollection())
            {
                foreach (FontFamily f in c.Families)
                {
                    if (f.Name == "Segoe Fluent Icons") return f.Name;
                    if (f.Name == "Segoe MDL2 Assets") found = f.Name;
                }
            }
            return found;
        }

        public static GraphicsPath Rounded(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            if (d > r.Height) d = r.Height;
            if (d > r.Width) d = r.Width;
            if (d <= 0) { p.AddRectangle(r); return p; }

            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d - 1, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d - 1, r.Bottom - d - 1, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d - 1, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static GraphicsPath TopRounded(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d - 1, r.Y, d, d, 270, 90);
            p.AddLine(r.Right - 1, r.Bottom, r.X, r.Bottom);
            p.CloseFigure();
            return p;
        }
    }

    // ======================================================================
    //  ToolStrip / StatusStrip renderer (flat, rounded, light)
    // ======================================================================
    internal class ModernToolStripRenderer : ToolStripProfessionalRenderer
    {
        public ModernToolStripRenderer() : base(new ProfessionalColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            Graphics g = e.Graphics;
            bool isStatus = e.ToolStrip is StatusStrip;

            using (SolidBrush b = new SolidBrush(isStatus ? Theme.StatusBack : Theme.Surface))
                g.FillRectangle(b, e.AffectedBounds);

            if (isStatus) return;

            // rounded "pill" behind the address box
            g.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (ToolStripItem item in e.ToolStrip.Items)
            {
                ToolStripSpringTextBox box = item as ToolStripSpringTextBox;
                if (box == null || !box.Visible || box.IsOnOverflow) continue;

                int h = Theme.S(34);
                Rectangle r = box.Bounds;
                Rectangle pill = new Rectangle(
                    r.X - Theme.S(12),
                    r.Y + (r.Height - h) / 2,
                    r.Width + Theme.S(24),
                    h);

                bool focused = box.TextBox.Focused;
                using (GraphicsPath path = Theme.Rounded(pill, h / 2))
                {
                    using (SolidBrush fill = new SolidBrush(focused ? Theme.Surface : Theme.Field))
                        g.FillPath(fill, path);

                    if (focused)
                    {
                        using (Pen pen = new Pen(Theme.Accent, 2f))
                            g.DrawPath(pen, path);
                    }
                }
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen p = new Pen(Theme.Border))
            {
                if (e.ToolStrip is StatusStrip)
                    e.Graphics.DrawLine(p, 0, 0, e.ToolStrip.Width, 0);
                else
                    e.Graphics.DrawLine(p, 0, e.ToolStrip.Height - 1, e.ToolStrip.Width, e.ToolStrip.Height - 1);
            }
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            ToolStripItem item = e.Item;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool accent = "accent".Equals(item.Tag);
            Color? fill = null;

            if (accent)
            {
                fill = item.Pressed ? Theme.AccentPressed : (item.Selected ? Theme.AccentHover : Theme.Accent);
            }
            else if (item.Enabled)
            {
                if (item.Pressed) fill = Theme.Pressed;
                else if (item.Selected) fill = Theme.Hover;
            }

            if (fill.HasValue)
            {
                Rectangle r = new Rectangle(0, 0, item.Width, item.Height);
                using (GraphicsPath path = Theme.Rounded(r, accent ? item.Height / 2 : Theme.S(9)))
                using (SolidBrush b = new SolidBrush(fill.Value))
                    g.FillPath(b, path);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if ("accent".Equals(e.Item.Tag)) e.TextColor = Color.White;
            else if (e.Item is ToolStripStatusLabel) e.TextColor = Theme.TextMuted;
            else e.TextColor = e.Item.Enabled ? Theme.TextPrimary : Theme.TextDisabled;

            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (Pen p = new Pen(Theme.Border))
            {
                if (e.Vertical)
                {
                    int h = Theme.S(20);
                    int x = e.Item.Width / 2;
                    int y = (e.Item.Height - h) / 2;
                    e.Graphics.DrawLine(p, x, y, x, y + h);
                }
                else
                {
                    int y = e.Item.Height / 2;
                    e.Graphics.DrawLine(p, 0, y, e.Item.Width, y);
                }
            }
        }
    }

    // ======================================================================
    //  Tab control: flat, rounded, with close button per tab
    // ======================================================================
    internal class ModernTabControl : TabControl
    {
        public event Action<int> TabCloseRequested;

        private int hoverTab = -1;
        private bool hoverClose = false;

        public ModernTabControl()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            DrawMode = TabDrawMode.OwnerDrawFixed;
            SizeMode = TabSizeMode.Fixed;
            Multiline = false;
            Padding = new Point(0, 0);
            ItemSize = new Size(Theme.S(200), Theme.S(38));
        }

        private int StripHeight { get { return ItemSize.Height + Theme.S(6); } }

        // pages start right below the strip, no borders
        public override Rectangle DisplayRectangle
        {
            get
            {
                int top = StripHeight;
                return new Rectangle(0, top, Math.Max(0, Width), Math.Max(0, Height - top));
            }
        }

        private Rectangle TabBounds(int i)
        {
            Rectangle r = GetTabRect(i);
            int top = Theme.S(8);
            return new Rectangle(r.X + 1, top, Math.Max(1, r.Width - 3), StripHeight - top);
        }

        private Rectangle CloseRect(int i)
        {
            Rectangle t = TabBounds(i);
            int sz = Theme.S(18);
            return new Rectangle(t.Right - sz - Theme.S(8), t.Y + (t.Height - sz) / 2 - Theme.S(1), sz, sz);
        }

        private void UpdateItemWidth()
        {
            if (TabPages.Count == 0) return;
            int w = (Width - Theme.S(8)) / TabPages.Count;
            w = Math.Max(Theme.S(90), Math.Min(Theme.S(220), w));
            if (ItemSize.Width != w) ItemSize = new Size(w, ItemSize.Height);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            TabPage page = e.Control as TabPage;
            if (page != null)
            {
                page.UseVisualStyleBackColor = false;
                page.BackColor = Theme.Surface;
                page.Padding = new Padding(0);
            }
            base.OnControlAdded(e);
            UpdateItemWidth();
            Invalidate();
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);
            hoverTab = -1;
            UpdateItemWidth();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateItemWidth();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            g.Clear(Theme.Surface);
            using (SolidBrush strip = new SolidBrush(Theme.StripBack))
                g.FillRectangle(strip, 0, 0, Width, StripHeight);

            // non-selected first, selected last (on top)
            for (int i = 0; i < TabCount; i++)
                if (i != SelectedIndex) DrawTab(g, i);
            if (SelectedIndex >= 0 && SelectedIndex < TabCount) DrawTab(g, SelectedIndex);
        }

        private void DrawTab(Graphics g, int i)
        {
            bool selected = i == SelectedIndex;
            bool hover = i == hoverTab;
            Rectangle r = TabBounds(i);

            if (selected)
            {
                using (GraphicsPath p = Theme.TopRounded(r, Theme.S(10)))
                using (SolidBrush b = new SolidBrush(Theme.Surface))
                    g.FillPath(b, p);
            }
            else if (hover)
            {
                Rectangle h = new Rectangle(r.X, r.Y + Theme.S(3), r.Width, r.Height - Theme.S(8));
                using (GraphicsPath p = Theme.Rounded(h, Theme.S(8)))
                using (SolidBrush b = new SolidBrush(Theme.TabHover))
                    g.FillPath(b, p);
            }

            // title
            Rectangle textRect = new Rectangle(
                r.X + Theme.S(14), r.Y,
                r.Width - Theme.S(14) - Theme.S(34), r.Height - Theme.S(4));
            using (Font f = new Font("Segoe UI", 9f))
            {
                TextRenderer.DrawText(g, TabPages[i].Text, f, textRect,
                    selected ? Theme.TextPrimary : Theme.TextMuted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            // close button
            if (selected || hover)
            {
                Rectangle c = CloseRect(i);
                if (hover && hoverClose)
                {
                    using (SolidBrush b = new SolidBrush(selected ? Theme.Hover : Theme.Pressed))
                        g.FillEllipse(b, c);
                }

                int m = Theme.S(6);
                using (Pen pen = new Pen(Theme.TextMuted, 1.4f))
                {
                    g.DrawLine(pen, c.X + m, c.Y + m, c.Right - m - 1, c.Bottom - m - 1);
                    g.DrawLine(pen, c.Right - m - 1, c.Y + m, c.X + m, c.Bottom - m - 1);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int idx = -1;
            bool close = false;
            for (int i = 0; i < TabCount; i++)
            {
                if (TabBounds(i).Contains(e.Location))
                {
                    idx = i;
                    close = CloseRect(i).Contains(e.Location);
                    break;
                }
            }

            Cursor = idx >= 0 ? Cursors.Hand : Cursors.Default;

            if (idx != hoverTab || close != hoverClose)
            {
                hoverTab = idx;
                hoverClose = close;
                Invalidate(new Rectangle(0, 0, Width, StripHeight));
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor = Cursors.Default;
            if (hoverTab != -1)
            {
                hoverTab = -1;
                hoverClose = false;
                Invalidate(new Rectangle(0, 0, Width, StripHeight));
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            for (int i = 0; i < TabCount; i++)
            {
                bool onClose = e.Button == MouseButtons.Left && CloseRect(i).Contains(e.Location);
                bool middle = e.Button == MouseButtons.Middle && TabBounds(i).Contains(e.Location);
                if (onClose || middle)
                {
                    hoverTab = -1;
                    hoverClose = false;
                    if (TabCloseRequested != null) TabCloseRequested(i);
                    return;
                }
            }
            base.OnMouseDown(e);
        }
    }

    // ======================================================================
    //  Thin rounded progress bar for the status strip
    // ======================================================================
    internal class ModernProgressBar : Control
    {
        private readonly Timer timer = new Timer();
        private float pos = 0f;
        private ProgressBarStyle style = ProgressBarStyle.Marquee;
        private int value = 0;

        public ModernProgressBar()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            timer.Interval = 16;
            timer.Tick += delegate
            {
                pos += 0.012f;
                if (pos > 1f) pos = 0f;
                Invalidate();
            };
        }

        protected override Size DefaultSize { get { return new Size(160, 6); } }

        public ProgressBarStyle Style
        {
            get { return style; }
            set { style = value; UpdateTimer(); Invalidate(); }
        }

        // 0..100 (used when Style is not Marquee)
        public int Value
        {
            get { return value; }
            set { this.value = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }

        private void UpdateTimer()
        {
            if (Visible && style == ProgressBarStyle.Marquee) timer.Start();
            else timer.Stop();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateTimer();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.StatusBack);

            Rectangle track = new Rectangle(0, 0, Width, Height);
            using (GraphicsPath path = Theme.Rounded(track, Height / 2))
            {
                using (SolidBrush b = new SolidBrush(Theme.Border))
                    g.FillPath(b, path);

                g.SetClip(path);
                using (SolidBrush b = new SolidBrush(Theme.Accent))
                {
                    if (style == ProgressBarStyle.Marquee)
                    {
                        int segW = (int)(Width * 0.35f);
                        int x = (int)(pos * (Width + segW)) - segW;
                        g.FillRectangle(b, x, 0, segW, Height);
                    }
                    else
                    {
                        g.FillRectangle(b, 0, 0, (int)(Width * value / 100f), Height);
                    }
                }
                g.ResetClip();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }
    }

    // ToolStrip host so it can sit in the StatusStrip (same API the form uses: Style, Visible)
    internal class ToolStripModernProgress : ToolStripControlHost
    {
        public ToolStripModernProgress() : base(new ModernProgressBar())
        {
        }

        public ProgressBarStyle Style
        {
            get { return ((ModernProgressBar)Control).Style; }
            set { ((ModernProgressBar)Control).Style = value; }
        }

        public int Value
        {
            get { return ((ModernProgressBar)Control).Value; }
            set { ((ModernProgressBar)Control).Value = value; }
        }
    }
}