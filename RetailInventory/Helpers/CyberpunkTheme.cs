namespace RetailInventory.Helpers;

public static class CyberpunkTheme
{
    // Core palette
    public static readonly Color Background    = Color.FromArgb(10, 10, 20);
    public static readonly Color Surface       = Color.FromArgb(18, 18, 36);
    public static readonly Color SurfaceAlt    = Color.FromArgb(25, 25, 50);
    public static readonly Color Border        = Color.FromArgb(0, 255, 200);
    public static readonly Color NeonCyan      = Color.FromArgb(0, 255, 200);
    public static readonly Color NeonMagenta   = Color.FromArgb(255, 0, 180);
    public static readonly Color NeonYellow    = Color.FromArgb(255, 220, 0);
    public static readonly Color NeonOrange    = Color.FromArgb(255, 100, 0);
    public static readonly Color TextPrimary   = Color.FromArgb(220, 240, 255);
    public static readonly Color TextSecondary = Color.FromArgb(120, 160, 200);
    public static readonly Color TextAccent    = Color.FromArgb(0, 255, 200);
    public static readonly Color DangerRed     = Color.FromArgb(255, 50, 80);
    public static readonly Color SuccessGreen  = Color.FromArgb(0, 230, 100);

    public static readonly Font FontHeader  = new("Consolas", 14f, FontStyle.Bold);
    public static readonly Font FontTitle   = new("Consolas", 11f, FontStyle.Bold);
    public static readonly Font FontBody    = new("Consolas", 9f);
    public static readonly Font FontSmall   = new("Consolas", 8f);
    public static readonly Font FontMono    = new("Courier New", 9f);

    public static void ApplyToForm(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = TextPrimary;
        form.Font = FontBody;
    }

    public static void StyleButton(Button btn, Color? accentColor = null)
    {
        Color accent = accentColor ?? NeonCyan;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderColor = accent;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, accent.R, accent.G, accent.B);
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, accent.R, accent.G, accent.B);
        btn.BackColor = Surface;
        btn.ForeColor = accent;
        btn.Font = FontBody;
        btn.Cursor = Cursors.Hand;
    }

    public static void StyleDataGridView(DataGridView dgv)
    {
        dgv.BackgroundColor = Background;
        dgv.BorderStyle = BorderStyle.None;
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgv.GridColor = Color.FromArgb(30, 50, 80);
        dgv.DefaultCellStyle.BackColor = Surface;
        dgv.DefaultCellStyle.ForeColor = TextPrimary;
        dgv.DefaultCellStyle.Font = FontSmall;
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 80, 80);
        dgv.DefaultCellStyle.SelectionForeColor = NeonCyan;
        dgv.AlternatingRowsDefaultCellStyle.BackColor = SurfaceAlt;
        dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 30, 40);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = NeonCyan;
        dgv.ColumnHeadersDefaultCellStyle.Font = FontSmall;
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        dgv.EnableHeadersVisualStyles = false;
        dgv.RowHeadersVisible = false;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.ReadOnly = true;
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToDeleteRows = false;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }

    public static void StyleTextBox(TextBox tb)
    {
        tb.BackColor = SurfaceAlt;
        tb.ForeColor = TextPrimary;
        tb.BorderStyle = BorderStyle.FixedSingle;
        tb.Font = FontBody;
    }

    public static void StyleComboBox(ComboBox cb)
    {
        cb.BackColor = SurfaceAlt;
        cb.ForeColor = TextPrimary;
        cb.FlatStyle = FlatStyle.Flat;
        cb.Font = FontBody;
    }

    public static void StyleLabel(Label lbl, bool accent = false)
    {
        lbl.ForeColor = accent ? TextAccent : TextSecondary;
        lbl.Font = FontBody;
    }

    public static Panel CreateScanlinePanel()
    {
        var panel = new Panel
        {
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill
        };
        panel.Paint += (s, e) =>
        {
            for (int y = 0; y < panel.Height; y += 4)
                e.Graphics.DrawLine(new Pen(Color.FromArgb(8, 0, 255, 200)), 0, y, panel.Width, y);
        };
        return panel;
    }

    public static Label CreateNeonLabel(string text, Color? color = null)
    {
        return new Label
        {
            Text = text,
            ForeColor = color ?? NeonCyan,
            Font = FontTitle,
            AutoSize = true,
            BackColor = Color.Transparent
        };
    }

    public static Panel CreateDivider()
    {
        return new Panel
        {
            Height = 1,
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(0, 100, 90),
            Margin = new Padding(0, 4, 0, 4)
        };
    }

    // ── Borderless window support ────────────────────────────────────────

    public static CustomTitleBar ApplyBorderlessMode(Form form, string title, bool hasMaximize = true)
    {
        form.SuspendLayout();
        form.FormBorderStyle = FormBorderStyle.None;

        // Push root content down by padding rather than relying on Dock sibling ordering,
        // which is unreliable when the form is already shown.
        var rootFill = form.Controls.Cast<Control>().FirstOrDefault(c => c.Dock == DockStyle.Fill);
        if (rootFill != null)
            rootFill.Padding = new Padding(rootFill.Padding.Left,
                                           rootFill.Padding.Top + BarHeight,
                                           rootFill.Padding.Right,
                                           rootFill.Padding.Bottom);

        var bar = new CustomTitleBar(form, title, hasMaximize)
        {
            Size = new Size(form.ClientSize.Width, BarHeight)
        };
        form.Controls.Add(bar);
        bar.BringToFront();
        form.ResumeLayout(true);

        if (hasMaximize)
            EnableBorderlessResize(form);
        return bar;
    }

    public static void RemoveBorderlessMode(Form form, CustomTitleBar bar, FormBorderStyle restoreStyle)
    {
        form.SuspendLayout();
        form.Controls.Remove(bar);
        bar.Dispose();

        var rootFill = form.Controls.Cast<Control>().FirstOrDefault(c => c.Dock == DockStyle.Fill);
        if (rootFill != null)
            rootFill.Padding = new Padding(rootFill.Padding.Left,
                                           Math.Max(0, rootFill.Padding.Top - BarHeight),
                                           rootFill.Padding.Right,
                                           rootFill.Padding.Bottom);

        form.FormBorderStyle = restoreStyle;
        form.ResumeLayout(true);
    }

    private const int BarHeight = 32;

    public static void EnableBorderlessResize(Form form, int gripSize = 6)
    {
        var helper = new BorderlessResizeHelper(form, gripSize);
        if (form.IsHandleCreated)
            helper.AssignHandle(form.Handle);
    }
}

internal sealed class BorderlessResizeHelper : NativeWindow
{
    private readonly Form _form;
    private readonly int _grip;

    public BorderlessResizeHelper(Form form, int grip)
    {
        _form = form;
        _grip = grip;
        form.HandleCreated   += (_, _) => AssignHandle(form.Handle);
        form.HandleDestroyed += (_, _) => ReleaseHandle();
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x0084;
        if (m.Msg == WM_NCHITTEST && _form.WindowState == FormWindowState.Normal)
        {
            int lp = m.LParam.ToInt32();
            var pt = _form.PointToClient(new Point(lp & 0xFFFF, (lp >> 16) & 0xFFFF));
            int w = _form.ClientSize.Width, h = _form.ClientSize.Height;
            bool l = pt.X < _grip, r = pt.X > w - _grip;
            bool t = pt.Y < _grip, b = pt.Y > h - _grip;

            if      (t && l) { m.Result = (IntPtr)13; return; } // HTTOPLEFT
            else if (t && r) { m.Result = (IntPtr)14; return; } // HTTOPRIGHT
            else if (b && l) { m.Result = (IntPtr)16; return; } // HTBOTTOMLEFT
            else if (b && r) { m.Result = (IntPtr)17; return; } // HTBOTTOMRIGHT
            else if (t)      { m.Result = (IntPtr)12; return; } // HTTOP
            else if (b)      { m.Result = (IntPtr)15; return; } // HTBOTTOM
            else if (l)      { m.Result = (IntPtr)10; return; } // HTLEFT
            else if (r)      { m.Result = (IntPtr)11; return; } // HTRIGHT
        }
        base.WndProc(ref m);
    }
}
