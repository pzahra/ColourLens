using System.ComponentModel;
using System.Runtime.InteropServices;
using static ColourLens.Native;

namespace ColourLens;

/// <summary>
/// A click-through, always-on-top window hosting a Windows magnifier control at 1× zoom.
/// The magnifier re-renders the screen area underneath (excluding this window) through a
/// colour matrix, so the window looks like a tinted lens.
/// </summary>
internal sealed class LensWindow : Form
{
    private static readonly float[] Identity3x3 = [1, 0, 0, 0, 1, 0, 0, 0, 1];

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private float[] _effect = ColorFilters.Effect(Deficiency.None, 0);
    private nint _magnifier;

    public LensWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        _timer.Tick += (_, _) => UpdateSource();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    public void SetEffect(float[] effect)
    {
        _effect = effect;
        if (_magnifier != 0)
        {
            MagSetColorEffect(_magnifier, effect);
            InvalidateRect(_magnifier, 0, false);
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        SetLayeredWindowAttributes(Handle, 0, 255, LWA_ALPHA);

        _magnifier = CreateWindowEx(0, WC_MAGNIFIER, "ColourLens magnifier", WS_CHILD | WS_VISIBLE,
            0, 0, ClientSize.Width, ClientSize.Height, Handle, 0, GetModuleHandle(null), 0);
        if (_magnifier == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create the magnifier control.");

        MagSetWindowTransform(_magnifier, Identity3x3);
        MagSetWindowFilterList(_magnifier, MW_FILTERMODE_EXCLUDE, 1, [Handle]);
        MagSetColorEffect(_magnifier, _effect);
        _timer.Start();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _timer.Stop();
        _magnifier = 0;
        base.OnHandleDestroyed(e);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (_magnifier != 0)
            SetWindowPos(_magnifier, 0, 0, 0, ClientSize.Width, ClientSize.Height, SWP_NOZORDER | SWP_NOACTIVATE);
        UpdateSource();
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        UpdateSource();
    }

    // The magnifier control covers the whole client area.
    protected override void OnPaintBackground(PaintEventArgs e) { }

    private void UpdateSource()
    {
        if (_magnifier == 0 || !Visible)
            return;
        var b = Bounds;
        MagSetWindowSource(_magnifier, new RECT(b.Left, b.Top, b.Right, b.Bottom));
        InvalidateRect(_magnifier, 0, false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _timer.Dispose();
        base.Dispose(disposing);
    }
}
