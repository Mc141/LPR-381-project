using System.Drawing;
using System.Drawing.Drawing2D;

namespace LPR381_Assignment.UI.Helpers
{
    // Helper for graphics operations, e.g. rounded rectangles
    internal static class GraphicsHelper
    {
        // Creates a rounded rectangle GraphicsPath for the given bounds and radius
        public static GraphicsPath CreateRoundRectPath(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}