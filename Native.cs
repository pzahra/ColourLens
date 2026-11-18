using System.Runtime.InteropServices;

namespace ColourLens;

internal static class Native
{
    public const string WC_MAGNIFIER = "Magnifier";
    public const uint MW_FILTERMODE_EXCLUDE = 0;

    public const int WS_CHILD = 0x40000000, WS_VISIBLE = 0x10000000;
    public const int WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_LAYERED = 0x80000, WS_EX_NOACTIVATE = 0x08000000;
    public const uint LWA_ALPHA = 0x2;
    public const uint SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10;

    public const int WM_NCHITTEST = 0x84, WM_NCLBUTTONDBLCLK = 0xA3, WM_NCRBUTTONUP = 0xA5;
    public const int HTCLIENT = 1, HTCAPTION = 2, HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13,
        HTTOPRIGHT = 14, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT(int left, int top, int right, int bottom)
    {
        public int Left = left, Top = top, Right = right, Bottom = bottom;
    }

    [DllImport("Magnification.dll")]
    public static extern bool MagInitialize();

    [DllImport("Magnification.dll")]
    public static extern bool MagUninitialize();

    /// <summary>Sets the screen rectangle (physical pixels) the magnifier control shows.</summary>
    [DllImport("Magnification.dll")]
    public static extern bool MagSetWindowSource(nint hwnd, RECT rect);

    /// <summary><paramref name="transform"/> is a MAGTRANSFORM: 3×3 floats, row-major.</summary>
    [DllImport("Magnification.dll")]
    public static extern bool MagSetWindowTransform(nint hwnd, float[] transform);

    /// <summary><paramref name="effect"/> is a MAGCOLOREFFECT: 5×5 floats, row = input channel, column = output channel.</summary>
    [DllImport("Magnification.dll")]
    public static extern bool MagSetColorEffect(nint hwnd, float[] effect);

    [DllImport("Magnification.dll")]
    public static extern bool MagSetWindowFilterList(nint hwnd, uint filterMode, int count, nint[] hwnds);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint CreateWindowEx(int exStyle, string className, string windowName, int style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);

    [DllImport("user32.dll")]
    public static extern bool SetLayeredWindowAttributes(nint hwnd, uint colorKey, byte alpha, uint flags);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    public static extern bool InvalidateRect(nint hwnd, nint rect, bool erase);

    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(nint icon);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern nint GetModuleHandle(string? moduleName);
}
