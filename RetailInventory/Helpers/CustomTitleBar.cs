using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace RetailInventory.Helpers;

public sealed class CustomTitleBar : Panel
{
    public event EventHandler? SettingsClicked;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HasMaximize { get; set; } = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowSettings { get; set; } = true;

    private readonly Form _owner;
    private Point _dragOffset;
    private bool _dragging;

    private Rectangle _rcClose;
    private Rectangle _rcMaximize;
    private Rectangle _rcMinimize;
    private Rectangle _rcSettings;

    private enum HoverZone { None, Settings, Minimize, Maximize, Close }
    private HoverZone _hover = HoverZone.None;
    private bool _pressing;

    private const int ButtonWidth = 28;
    private const int BarHeight = 32;

    public CustomTitleBar(Form owner, string title, bool hasMaximize = true)
    {
        _owner = owner;
        HasMaximize = hasMaximize;
        Text = title;

        Height = BarHeight;
        Dock = DockStyle.None;
        Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        Location = new Point(0, 0);
        BackColor = CyberpunkTheme.Surface;
        DoubleBuffered = true;

        MouseDown  += OnMouseDown;
        MouseMove  += OnMouseMove;
        MouseUp    += OnMouseUp;
        MouseLeave += (_, _) => { _hover = HoverZone.None; _pressing = false; Invalidate(); };
        Paint      += OnPaint;
        Resize     += (_, _) => RecalcRects();

        RecalcRects();
    }

    private void RecalcRects()
    {
        int x = Width;
        x -= ButtonWidth; _rcClose    = new Rectangle(x, 0, ButtonWidth, BarHeight);
        x -= ButtonWidth; _rcMaximize = HasMaximize ? new Rectangle(x, 0, ButtonWidth, BarHeight) : Rectangle.Empty;
        if (!HasMaximize) x += ButtonWidth; // don't advance if no maximize slot
        x -= ButtonWidth; _rcMinimize = new Rectangle(x, 0, ButtonWidth, BarHeight);
        if (ShowSettings)
        { x -= ButtonWidth; _rcSettings = new Rectangle(x, 0, ButtonWidth, BarHeight); }
        else
        { _rcSettings = Rectangle.Empty; }
    }

    private void OnPaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Bottom border
        using (var pen = new Pen(CyberpunkTheme.NeonCyan, 1f))
            g.DrawLine(pen, 0, BarHeight - 1, Width, BarHeight - 1);

        // Title text
        using (var brush = new SolidBrush(CyberpunkTheme.NeonCyan))
        using (var font = new Font("Consolas", 9f, FontStyle.Bold))
        {
            int textRight = ShowSettings ? _rcSettings.X : _rcMinimize.X;
            var textRect = new RectangleF(8, 0, textRight - 8, BarHeight);
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter
            };
            g.DrawString(Text, font, brush, textRect, sf);
        }

        // Buttons
        DrawButton(g, _rcSettings, HoverZone.Settings, DrawMenuIcon);
        DrawButton(g, _rcMinimize, HoverZone.Minimize, DrawMinimizeIcon);
        if (HasMaximize)
            DrawButton(g, _rcMaximize, HoverZone.Maximize, DrawMaximizeIcon);
        DrawButton(g, _rcClose, HoverZone.Close, DrawCloseIcon);
    }

    private void DrawButton(Graphics g, Rectangle rc, HoverZone zone,
                            Action<Graphics, Rectangle, bool> drawIcon)
    {
        if (rc.IsEmpty) return;
        bool hot     = _hover == zone;
        bool pressed = hot && _pressing;

        if (hot || pressed)
        {
            Color tint = zone == HoverZone.Close
                ? Color.FromArgb(pressed ? 100 : 50, CyberpunkTheme.DangerRed)
                : Color.FromArgb(pressed ? 80  : 35, CyberpunkTheme.NeonCyan);
            using var hb = new SolidBrush(tint);
            g.FillRectangle(hb, rc);
        }

        drawIcon(g, rc, hot || pressed);
    }

    private static void DrawMenuIcon(Graphics g, Rectangle rc, bool lit)
    {
        Color c = lit ? CyberpunkTheme.NeonYellow
                      : Color.FromArgb(160, CyberpunkTheme.NeonYellow);
        using var pen = new Pen(c, 1.5f);
        int x1 = rc.X + 7, x2 = rc.Right - 7;
        int cy = rc.Y + rc.Height / 2;
        g.DrawLine(pen, x1, cy - 5, x2, cy - 5);
        g.DrawLine(pen, x1, cy,     x2, cy);
        g.DrawLine(pen, x1, cy + 5, x2, cy + 5);
    }

    private static void DrawMinimizeIcon(Graphics g, Rectangle rc, bool lit)
    {
        Color c = lit ? CyberpunkTheme.NeonCyan : Color.FromArgb(160, CyberpunkTheme.NeonCyan);
        using var pen = new Pen(c, 1.5f);
        int cy = rc.Y + rc.Height / 2 + 4;
        g.DrawLine(pen, rc.X + 7, cy, rc.Right - 7, cy);
    }

    private void DrawMaximizeIcon(Graphics g, Rectangle rc, bool lit)
    {
        Color c = lit ? CyberpunkTheme.NeonCyan : Color.FromArgb(160, CyberpunkTheme.NeonCyan);
        using var pen = new Pen(c, 1.5f);
        int m = 8;
        if (_owner.WindowState == FormWindowState.Maximized)
        {
            // Restore: two offset squares
            g.DrawRectangle(pen, rc.X + m + 2, rc.Y + m - 2, rc.Width - m * 2 - 2, rc.Height - m * 2 - 2);
            g.DrawRectangle(pen, rc.X + m,     rc.Y + m,     rc.Width - m * 2 - 2, rc.Height - m * 2 - 2);
        }
        else
        {
            g.DrawRectangle(pen, rc.X + m, rc.Y + m, rc.Width - m * 2, rc.Height - m * 2);
        }
    }

    private static void DrawCloseIcon(Graphics g, Rectangle rc, bool lit)
    {
        Color c = lit ? CyberpunkTheme.NeonMagenta : Color.FromArgb(180, CyberpunkTheme.DangerRed);
        using var pen = new Pen(c, 1.5f);
        int m = 9;
        g.DrawLine(pen, rc.X + m,     rc.Y + m,     rc.Right - m, rc.Bottom - m);
        g.DrawLine(pen, rc.Right - m, rc.Y + m,     rc.X + m,     rc.Bottom - m);
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;

        HoverZone z = HitTest(e.Location);
        if (z == HoverZone.None)
        {
            _dragging = true;
            Point screenPt = _owner.PointToScreen(e.Location);
            _dragOffset = new Point(screenPt.X - _owner.Left, screenPt.Y - _owner.Top);
        }
        else
        {
            _pressing = true;
            Invalidate();
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (_dragging && e.Button == MouseButtons.Left)
        {
            Point screen = _owner.PointToScreen(e.Location);
            _owner.Location = new Point(screen.X - _dragOffset.X, screen.Y - _dragOffset.Y);
            return;
        }

        HoverZone newZone = HitTest(e.Location);
        if (newZone != _hover) { _hover = newZone; Invalidate(); }
        Cursor = newZone == HoverZone.None ? Cursors.SizeAll : Cursors.Hand;
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (_dragging) { _dragging = false; return; }
        if (!_pressing || e.Button != MouseButtons.Left) return;

        _pressing = false;
        HoverZone z = HitTest(e.Location);
        Invalidate();

        switch (z)
        {
            case HoverZone.Close:    _owner.Close(); break;
            case HoverZone.Minimize: _owner.WindowState = FormWindowState.Minimized; break;
            case HoverZone.Maximize: ToggleMaximize(); break;
            case HoverZone.Settings: SettingsClicked?.Invoke(this, EventArgs.Empty); break;
        }
    }

    protected override void OnDoubleClick(EventArgs e)
    {
        base.OnDoubleClick(e);
        if (_hover == HoverZone.None && HasMaximize)
            ToggleMaximize();
    }

    private void ToggleMaximize()
    {
        _owner.WindowState = _owner.WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;
        Invalidate();
    }

    private HoverZone HitTest(Point p)
    {
        if (_rcClose.Contains(p))                    return HoverZone.Close;
        if (HasMaximize && _rcMaximize.Contains(p))  return HoverZone.Maximize;
        if (_rcMinimize.Contains(p))                 return HoverZone.Minimize;
        if (_rcSettings.Contains(p))                 return HoverZone.Settings;
        return HoverZone.None;
    }
}
