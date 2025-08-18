using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using LPR381_Assignment.UI.Helpers;

namespace LPR381_Assignment.UI.Controls
{
    // Custom panel that displays a styled title and rounded borders
    internal class StyledGroupPanel : Panel
    {
        public string Title { get; set; } = "";
        public Font TitleFont { get; set; } = SystemFonts.DefaultFont;
        public Color TitleColor { get; set; } = Color.Black;
        public Color BorderColor { get; set; } = Color.Gray;
        public Color BackgroundFill { get; set; } = Color.White;
        public int CornerRadius { get; set; } = 8;

        // Custom paint for styled border and title
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var titleSize = TextRenderer.MeasureText(Title, TitleFont);
            int titlePadX = 20;
            int titlePadY = 8;
            int borderTop = titlePadY + (titleSize.Height / 2);
            var bodyRect = new Rectangle(1, borderTop, Width - 2, Height - borderTop - 1);

            using (var path = GraphicsHelper.CreateRoundRectPath(bodyRect, CornerRadius))
            {
                using (var fill = new SolidBrush(BackgroundFill))
                    e.Graphics.FillPath(fill, path);
                using (var pen = new Pen(BorderColor, 1.5f))
                    e.Graphics.DrawPath(pen, path);
            }

            // Draw title background and text
            var titleBgRect = new Rectangle(titlePadX - 4, titlePadY - 2, titleSize.Width + 8, titleSize.Height + 4);
            using (var bg = new SolidBrush(Parent?.BackColor ?? BackgroundFill))
                e.Graphics.FillRectangle(bg, titleBgRect);
            TextRenderer.DrawText(e.Graphics, Title, TitleFont,
                new Point(titlePadX, titlePadY), TitleColor,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPadding);
        }
    }
}