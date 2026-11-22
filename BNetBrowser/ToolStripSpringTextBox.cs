using System.Drawing;
using System.Windows.Forms;

namespace BNetBrowser
{
    // Text box that stretches to fill the remaining toolbar width.
    // Borderless - the rounded "pill" is drawn by ModernToolStripRenderer.
    public class ToolStripSpringTextBox : ToolStripTextBox
    {
        public ToolStripSpringTextBox()
        {
            BorderStyle = BorderStyle.None;
            TextBox.BackColor = Theme.Field;
            TextBox.ForeColor = Theme.TextPrimary;

            TextBox.Enter += delegate
            {
                TextBox.BackColor = Theme.Surface;
                if (Owner != null) Owner.Invalidate();
            };
            TextBox.Leave += delegate
            {
                TextBox.BackColor = Theme.Field;
                if (Owner != null) Owner.Invalidate();
            };
        }

        public override Size GetPreferredSize(Size constrainingSize)
        {
            if (IsOnOverflow || Owner.Orientation == Orientation.Vertical)
                return DefaultSize;

            int width = Owner.DisplayRectangle.Width;
            if (Owner.OverflowButton.Visible)
                width -= Owner.OverflowButton.Width + Owner.OverflowButton.Margin.Horizontal;

            int springCount = 0;
            foreach (ToolStripItem item in Owner.Items)
            {
                if (item.IsOnOverflow) continue;

                if (item is ToolStripSpringTextBox)
                {
                    springCount++;
                    width -= item.Margin.Horizontal;
                }
                else
                {
                    width -= item.Width + item.Margin.Horizontal;
                }
            }

            if (springCount > 1) width /= springCount;
            if (width < DefaultSize.Width) width = DefaultSize.Width;

            Size size = base.GetPreferredSize(constrainingSize);
            size.Width = width;
            return size;
        }
    }
}