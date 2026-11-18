using System.Drawing.Drawing2D;

namespace ColourLens;

internal static class AppIcon
{
    public static Icon Create()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var disc = new Rectangle(2, 2, 27, 27);
            using var red = new SolidBrush(Color.FromArgb(230, 60, 50));
            using var green = new SolidBrush(Color.FromArgb(60, 180, 75));
            using var blue = new SolidBrush(Color.FromArgb(40, 110, 230));
            using var ring = new Pen(Color.FromArgb(40, 40, 40), 2);
            g.FillPie(red, disc, -90, 120);
            g.FillPie(green, disc, 30, 120);
            g.FillPie(blue, disc, 150, 120);
            g.DrawEllipse(ring, disc);
        }

        nint handle = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            Native.DestroyIcon(handle);
        }
    }
}
