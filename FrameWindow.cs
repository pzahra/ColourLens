using System.Drawing.Drawing2D;
using static ColourLens.Native;

namespace ColourLens;

/// <summary>
/// The visible, draggable, resizable frame with the controls. Its window region has a hole in
/// the middle where the click-through <see cref="LensWindow"/> sits, so clicks inside the lens
/// reach whatever is underneath.
/// </summary>
internal sealed class FrameWindow : Form
{
    // Layout in 96-DPI pixels.
    private const int BorderPx = 6, BarPx = 30, InsetPx = 4, CornerPx = 16, SliderPx = 110, CloseButtonPx = 30;
    private const float SeverityStep = 0.05f;

    private static readonly Color FrameColor = Color.FromArgb(32, 32, 32);
    private static readonly Color OutlineColor = Color.FromArgb(85, 85, 85);
    private static readonly Color HoverColor = Color.FromArgb(62, 62, 62);
    private static readonly Color TextColor = Color.FromArgb(240, 240, 240);
    private static readonly Color DimTextColor = Color.FromArgb(165, 165, 165);
    private static readonly Color TrackColor = Color.FromArgb(95, 95, 95);
    private static readonly Color AccentColor = Color.FromArgb(76, 194, 255);
    private static readonly Color CloseHoverColor = Color.FromArgb(196, 43, 28);

    private enum Part { None, Mode, Slider, Close }

    private readonly LensWindow _lens = new();
    private readonly ContextMenuStrip _menu = new();
    private Font _font = new("Segoe UI", 12f, GraphicsUnit.Pixel);

    private Deficiency _mode;
    private Deficiency _lastMode;
    private float _severity;
    private Rectangle? _restoreBounds;

    private Rectangle _inner, _modeButton, _slider, _sliderHit, _closeButton;
    private Part _hover;
    private bool _draggingSlider;

    public FrameWindow()
    {
        var settings = Settings.Load();
        _mode = settings.Mode;
        _lastMode = _mode == Deficiency.None ? Deficiency.Deutan : _mode;
        _severity = Math.Clamp(settings.Severity, 0f, 1f);

        Text = "ColourLens";
        Icon = AppIcon.Create();
        FormBorderStyle = FormBorderStyle.None;
        AutoScaleMode = AutoScaleMode.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = settings.GetBounds();
        TopMost = true;
        DoubleBuffered = true;
        BackColor = FrameColor;
        BuildMenu();
    }

    private float DpiScale => DeviceDpi / 96f;

    private int S(int px) => (int)MathF.Round(px * DpiScale);

    // ---- lifecycle -------------------------------------------------------------------------

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyDpi();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        ApplyDpi();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _lens.Owner = this;
        ApplyEffect();
        PositionLens();
        _lens.Show();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        var settings = new Settings { Mode = _mode, Severity = _severity };
        settings.SetBounds(_restoreBounds ?? (WindowState == FormWindowState.Normal ? Bounds : RestoreBounds));
        settings.Save();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lens.Dispose();
            _menu.Dispose();
            _font.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ApplyDpi()
    {
        _font.Dispose();
        _font = new Font("Segoe UI", 12f * DpiScale, GraphicsUnit.Pixel);
        MinimumSize = new Size(S(160), S(100));
        Relayout();
    }

    // ---- layout ----------------------------------------------------------------------------

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        Relayout();
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        PositionLens();
    }

    private void Relayout()
    {
        if (!IsHandleCreated)
            return;
        ArrangeParts();
        UpdateRegion();
        PositionLens();
        Invalidate();
    }

    private void ArrangeParts()
    {
        int w = ClientSize.Width, h = ClientSize.Height;
        int border = S(BorderPx), bar = S(BarPx), inset = S(InsetPx);
        int partHeight = bar - 2 * inset;

        _inner = Rectangle.FromLTRB(border, bar, Math.Max(border, w - border), Math.Max(bar, h - border));
        _closeButton = new Rectangle(w - border - S(CloseButtonPx), inset, S(CloseButtonPx), partHeight);

        // Fixed width from the longest possible label, so the slider doesn't jump while dragging.
        int labelWidth = Enum.GetValues<Deficiency>()
            .SelectMany(kind => new[] { ColorFilters.Name(kind, 1f), ColorFilters.Name(kind, 0.5f) })
            .Max(name => TextRenderer.MeasureText(name + "  ▾", _font).Width);
        int modeRight = Math.Min(border + labelWidth + S(16), _closeButton.Left - S(4));
        _modeButton = Rectangle.FromLTRB(border, inset, Math.Max(border, modeRight), inset + partHeight);

        int sliderLeft = _modeButton.Right + S(14);
        int percentWidth = TextRenderer.MeasureText("100%", _font).Width;
        int sliderHitRight = sliderLeft + S(SliderPx) + S(10) + percentWidth;
        if (_mode != Deficiency.None && sliderHitRight < _closeButton.Left - S(8))
        {
            _slider = new Rectangle(sliderLeft, inset, S(SliderPx), partHeight);
            _sliderHit = Rectangle.FromLTRB(sliderLeft - S(8), inset, sliderHitRight, inset + partHeight);
        }
        else
        {
            _slider = _sliderHit = Rectangle.Empty;
        }
    }

    private void UpdateRegion()
    {
        var region = new Region(new Rectangle(Point.Empty, Size));
        if (_inner.Width > 0 && _inner.Height > 0)
            region.Exclude(_inner);
        var old = Region;
        Region = region;
        old?.Dispose();
    }

    private void PositionLens()
    {
        if (!IsHandleCreated || WindowState == FormWindowState.Minimized || _inner.Width <= 0 || _inner.Height <= 0)
            return;
        // Borderless, so client and window origins coincide.
        _lens.Bounds = new Rectangle(Left + _inner.X, Top + _inner.Y, _inner.Width, _inner.Height);
    }

    // ---- state -----------------------------------------------------------------------------

    private void SetMode(Deficiency mode)
    {
        if (mode != Deficiency.None)
        {
            _lastMode = mode;
            if (_severity <= 0f)
                _severity = 1f;
        }
        _mode = mode;
        ApplyEffect();
    }

    private void ToggleOriginal() => SetMode(_mode == Deficiency.None ? _lastMode : Deficiency.None);

    private void SetSeverity(float value)
    {
        value = Math.Clamp(MathF.Round(value / SeverityStep) * SeverityStep, 0f, 1f);
        if (value == _severity)
            return;
        _severity = value;
        ApplyEffect();
    }

    private void NudgeSeverity(float delta)
    {
        if (_mode != Deficiency.None)
            SetSeverity(_severity + delta);
    }

    private void ApplyEffect()
    {
        _lens.SetEffect(ColorFilters.Effect(_mode, _severity));
        Text = $"ColourLens – {ColorFilters.Name(_mode, _severity)}";
        if (IsHandleCreated)
        {
            ArrangeParts();
            Invalidate();
        }
    }

    private void ToggleFill()
    {
        if (_restoreBounds is Rectangle restore)
        {
            _restoreBounds = null;
            Bounds = restore;
        }
        else
        {
            _restoreBounds = Bounds;
            Bounds = Screen.FromHandle(Handle).WorkingArea;
        }
    }

    private void BuildMenu()
    {
        foreach (var kind in Enum.GetValues<Deficiency>())
        {
            var item = new ToolStripMenuItem(ColorFilters.MenuText(kind))
            {
                Tag = kind,
                ShortcutKeyDisplayString = ((int)kind).ToString(),
            };
            item.Click += (_, _) => SetMode(kind);
            _menu.Items.Add(item);
        }

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem("Compare with original", null, (_, _) => ToggleOriginal())
        {
            ShortcutKeyDisplayString = "Space",
        });
        var fill = new ToolStripMenuItem("Fill screen", null, (_, _) => ToggleFill())
        {
            ShortcutKeyDisplayString = "Double-click bar",
        };
        _menu.Items.Add(fill);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => Close()) { ShortcutKeyDisplayString = "Alt+F4" });

        _menu.Opening += (_, _) =>
        {
            foreach (var item in _menu.Items.OfType<ToolStripMenuItem>())
                if (item.Tag is Deficiency kind)
                    item.Checked = kind == _mode;
            fill.Checked = _restoreBounds is not null;
        };
    }

    // ---- input -----------------------------------------------------------------------------

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case WM_NCHITTEST:
                m.Result = HitTest(PointToClient(PointFromLParam(m.LParam)));
                return;
            case WM_NCLBUTTONDBLCLK when m.WParam == HTCAPTION:
                ToggleFill();
                return;
            case WM_NCRBUTTONUP when m.WParam == HTCAPTION:
                _menu.Show(Cursor.Position);
                return;
        }
        base.WndProc(ref m);
    }

    private static Point PointFromLParam(nint lParam) => new((short)(lParam & 0xFFFF), (short)((lParam >> 16) & 0xFFFF));

    private nint HitTest(Point p)
    {
        int w = ClientSize.Width, h = ClientSize.Height;
        if (_restoreBounds is null)
        {
            int grip = S(BorderPx), topGrip = S(InsetPx), corner = S(CornerPx);
            bool left = p.X < grip, right = p.X >= w - grip, top = p.Y < topGrip, bottom = p.Y >= h - grip;
            bool nearLeft = p.X < corner, nearRight = p.X >= w - corner, nearTop = p.Y < corner, nearBottom = p.Y >= h - corner;

            if ((top && nearLeft) || (left && nearTop)) return HTTOPLEFT;
            if ((top && nearRight) || (right && nearTop)) return HTTOPRIGHT;
            if ((bottom && nearLeft) || (left && nearBottom)) return HTBOTTOMLEFT;
            if ((bottom && nearRight) || (right && nearBottom)) return HTBOTTOMRIGHT;
            if (left) return HTLEFT;
            if (right) return HTRIGHT;
            if (top) return HTTOP;
            if (bottom) return HTBOTTOM;
        }
        return PartAt(p) == Part.None ? HTCAPTION : HTCLIENT;
    }

    private Part PartAt(Point p) =>
        _modeButton.Contains(p) ? Part.Mode :
        _sliderHit.Contains(p) ? Part.Slider :
        _closeButton.Contains(p) ? Part.Close :
        Part.None;

    private void SetHover(Part part)
    {
        if (_hover == part)
            return;
        _hover = part;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_draggingSlider)
            SetSeverity((e.X - _slider.Left) / (float)Math.Max(1, _slider.Width));
        else
            SetHover(PartAt(e.Location));
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (!_draggingSlider)
            SetHover(Part.None);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Right)
        {
            _menu.Show(this, e.Location);
            return;
        }
        if (e.Button != MouseButtons.Left)
            return;

        switch (PartAt(e.Location))
        {
            case Part.Mode:
                _menu.Show(this, new Point(_modeButton.Left, _modeButton.Bottom));
                break;
            case Part.Slider:
                _draggingSlider = true;
                Capture = true;
                SetSeverity((e.X - _slider.Left) / (float)Math.Max(1, _slider.Width));
                Invalidate();
                break;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_draggingSlider)
        {
            _draggingSlider = false;
            Capture = false;
            SetHover(PartAt(e.Location));
            Invalidate();
        }
        else if (e.Button == MouseButtons.Left && PartAt(e.Location) == Part.Close)
        {
            Close();
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        NudgeSeverity(Math.Sign(e.Delta) * SeverityStep);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case >= Keys.D0 and <= Keys.D4:
                SetMode((Deficiency)(keyData - Keys.D0));
                return true;
            case >= Keys.NumPad0 and <= Keys.NumPad4:
                SetMode((Deficiency)(keyData - Keys.NumPad0));
                return true;
            case Keys.Up or Keys.Right:
                NudgeSeverity(2 * SeverityStep);
                return true;
            case Keys.Down or Keys.Left:
                NudgeSeverity(-2 * SeverityStep);
                return true;
            case Keys.Space:
                ToggleOriginal();
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    // ---- painting --------------------------------------------------------------------------

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;

        using (var outline = new Pen(OutlineColor))
            g.DrawRectangle(outline, _inner.X - 1, _inner.Y - 1, _inner.Width + 1, _inner.Height + 1);

        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (_hover == Part.Mode)
            FillRounded(g, _modeButton, HoverColor);
        var label = Rectangle.FromLTRB(_modeButton.Left + S(8), _modeButton.Top, _modeButton.Right - S(4), _modeButton.Bottom);
        TextRenderer.DrawText(g, ColorFilters.Name(_mode, _severity) + "  ▾", _font, label, TextColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        if (!_slider.IsEmpty)
            PaintSlider(g);

        if (_hover == Part.Close)
            FillRounded(g, _closeButton, CloseHoverColor);
        int cx = _closeButton.Left + _closeButton.Width / 2, cy = _closeButton.Top + _closeButton.Height / 2, arm = S(5);
        using var cross = new Pen(TextColor, Math.Max(1f, DpiScale));
        g.DrawLine(cross, cx - arm, cy - arm, cx + arm, cy + arm);
        g.DrawLine(cross, cx - arm, cy + arm, cx + arm, cy - arm);
    }

    private void PaintSlider(Graphics g)
    {
        int cy = _slider.Top + _slider.Height / 2;
        int x = _slider.Left + (int)MathF.Round(_severity * _slider.Width);

        using var track = new Pen(TrackColor, S(2)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var filled = new Pen(AccentColor, S(2)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(track, _slider.Left, cy, _slider.Right, cy);
        if (x > _slider.Left)
            g.DrawLine(filled, _slider.Left, cy, x, cy);

        int r = S(_hover == Part.Slider || _draggingSlider ? 7 : 6);
        using var thumb = new SolidBrush(AccentColor);
        g.FillEllipse(thumb, x - r, cy - r, 2 * r, 2 * r);

        var percent = Rectangle.FromLTRB(_slider.Right + S(10), _slider.Top, _sliderHit.Right, _slider.Bottom);
        TextRenderer.DrawText(g, $"{MathF.Round(_severity * 100)}%", _font, percent, DimTextColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
    }

    private void FillRounded(Graphics g, Rectangle r, Color color)
    {
        int d = S(8);
        using var path = new GraphicsPath();
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }
}
