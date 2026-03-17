using RetailInventory.Helpers;
using RetailInventory.Models;
using RetailInventory.Services;

namespace RetailInventory.Forms;

public class MainForm : Form
{
    private readonly InventoryService _svc = new();
    private Label _lblProductCount = new();
    private Label _lblLowStock = new();
    private Label _lblCategories = new();
    private Label _lblTxCount = new();
    private Label _lblLastSaved = new();
    private Panel _glowBar = new();

    public MainForm()
    {
        BuildUI();
        RefreshStats();
        var ticker = new System.Windows.Forms.Timer { Interval = 2000 };
        ticker.Tick += (_, _) => AnimateGlow();
        ticker.Start();
    }

    private void BuildUI()
    {
        Text = "RETAIL INVENTORY SYSTEM v1.0 // CYBERPUNK EDITION";
        Size = new Size(900, 600);
        MinimumSize = new Size(700, 500);
        StartPosition = FormStartPosition.CenterScreen;
        CyberpunkTheme.ApplyToForm(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            BackColor = CyberpunkTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));   // header
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 4));    // glow bar
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // content
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));   // status bar

        // Header
        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(12, 12, 28) };
        header.Paint += DrawHeader;
        root.Controls.Add(header, 0, 0);

        // Neon glow bar
        _glowBar.Dock = DockStyle.Fill;
        _glowBar.BackColor = CyberpunkTheme.NeonCyan;
        root.Controls.Add(_glowBar, 0, 1);

        // Content area
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 12),
            RowCount = 2,
            ColumnCount = 1,
            BackColor = CyberpunkTheme.Background
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Stat cards
        var statsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            WrapContents = false
        };
        statsPanel.Controls.Add(CreateStatCard("PRODUCTS", ref _lblProductCount, CyberpunkTheme.NeonCyan));
        statsPanel.Controls.Add(CreateStatCard("LOW STOCK", ref _lblLowStock, CyberpunkTheme.NeonOrange));
        statsPanel.Controls.Add(CreateStatCard("CATEGORIES", ref _lblCategories, CyberpunkTheme.NeonMagenta));
        statsPanel.Controls.Add(CreateStatCard("TRANSACTIONS", ref _lblTxCount, CyberpunkTheme.NeonYellow));
        content.Controls.Add(statsPanel, 0, 0);

        // Nav buttons
        var navPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };
        navPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        navPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        navPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        navPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        navPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

        navPanel.Controls.Add(CreateNavButton("[ PRODUCTS ]", "Manage product catalog and inventory levels",
            CyberpunkTheme.NeonCyan, OnProducts), 0, 0);
        navPanel.Controls.Add(CreateNavButton("[ CATEGORIES ]", "Organize products into categories",
            CyberpunkTheme.NeonMagenta, OnCategories), 1, 0);
        navPanel.Controls.Add(CreateNavButton("[ STOCK ADJUSTMENT ]", "Record receipts, sales, returns, and adjustments",
            CyberpunkTheme.NeonOrange, OnStock), 0, 1);
        navPanel.Controls.Add(CreateNavButton("[ TRANSACTION LOG ]", "Full history of all stock movements",
            CyberpunkTheme.NeonYellow, OnHistory), 1, 1);

        var posBtn = CreateNavButton("[ POS TERMINAL ]", "Open point-of-sale checkout — ring up sales, manage cart, process payments",
            CyberpunkTheme.NeonCyan, OnPOS);
        navPanel.Controls.Add(posBtn, 0, 2);
        navPanel.SetColumnSpan(posBtn, 2);

        content.Controls.Add(navPanel, 0, 1);

        root.Controls.Add(content, 0, 2);

        // Status bar
        var statusBar = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(8, 8, 18) };
        _lblLastSaved.ForeColor = CyberpunkTheme.TextSecondary;
        _lblLastSaved.Font = CyberpunkTheme.FontSmall;
        _lblLastSaved.Location = new Point(12, 6);
        _lblLastSaved.AutoSize = true;
        statusBar.Controls.Add(_lblLastSaved);

        var lblPowered = new Label
        {
            Text = "POWERED BY JCODEMUNCH // NET10.0-WINDOWS",
            ForeColor = Color.FromArgb(40, 80, 80),
            Font = CyberpunkTheme.FontSmall,
            AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.Top
        };
        lblPowered.Location = new Point(600, 6);
        statusBar.Controls.Add(lblPowered);
        root.Controls.Add(statusBar, 0, 3);

        Controls.Add(root);
    }

    private void DrawHeader(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        var panel = (Panel)sender!;
        g.Clear(Color.FromArgb(12, 12, 28));

        // Scanlines
        for (int y = 0; y < panel.Height; y += 3)
            g.DrawLine(new Pen(Color.FromArgb(6, 0, 255, 200)), 0, y, panel.Width, y);

        // Title
        using var titleFont = new Font("Consolas", 20f, FontStyle.Bold);
        using var subFont = new Font("Consolas", 9f);
        using var brush = new SolidBrush(CyberpunkTheme.NeonCyan);
        using var subBrush = new SolidBrush(CyberpunkTheme.TextSecondary);
        g.DrawString("RETAIL INVENTORY SYSTEM", titleFont, brush, new PointF(16, 8));
        g.DrawString("// LOCAL DATA STORAGE // CYBERPUNK EDITION // v1.0", subFont, subBrush, new PointF(18, 46));

        // Corner decoration
        using var pen = new Pen(CyberpunkTheme.NeonMagenta, 1);
        g.DrawLines(pen, new Point[] { new(panel.Width - 60, 10), new(panel.Width - 10, 10), new(panel.Width - 10, 60) });
    }

    private Panel CreateStatCard(string label, ref Label valueLabel, Color accent)
    {
        var card = new Panel
        {
            Width = 160,
            Height = 110,
            BackColor = CyberpunkTheme.Surface,
            Margin = new Padding(0, 0, 12, 0)
        };
        card.Paint += (s, e) =>
        {
            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(60, accent.R, accent.G, accent.B)), 0, 0, card.Width - 1, card.Height - 1);
        };

        var lblHeader = new Label
        {
            Text = label,
            ForeColor = accent,
            Font = CyberpunkTheme.FontSmall,
            AutoSize = false,
            Width = 140,
            Location = new Point(10, 8),
            TextAlign = ContentAlignment.MiddleLeft
        };

        valueLabel = new Label
        {
            Text = "0",
            ForeColor = CyberpunkTheme.TextPrimary,
            Font = new Font("Consolas", 28f, FontStyle.Bold),
            AutoSize = false,
            Width = 140,
            Height = 50,
            Location = new Point(10, 28),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var divider = new Panel
        {
            Height = 1,
            Width = 140,
            Location = new Point(10, 82),
            BackColor = Color.FromArgb(40, accent.R, accent.G, accent.B)
        };

        card.Controls.AddRange([lblHeader, valueLabel, divider]);
        return card;
    }

    private Panel CreateNavButton(string title, string description, Color accent, EventHandler handler)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CyberpunkTheme.Surface,
            Margin = new Padding(4),
            Cursor = Cursors.Hand
        };
        card.Paint += (s, e) =>
        {
            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(80, accent.R, accent.G, accent.B)),
                0, 0, card.Width - 1, card.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = title,
            ForeColor = accent,
            Font = CyberpunkTheme.FontTitle,
            AutoSize = true,
            Location = new Point(12, 12),
            BackColor = Color.Transparent
        };
        var lblDesc = new Label
        {
            Text = description,
            ForeColor = CyberpunkTheme.TextSecondary,
            Font = CyberpunkTheme.FontSmall,
            AutoSize = false,
            Width = 380,
            Height = 30,
            Location = new Point(12, 38),
            BackColor = Color.Transparent
        };

        card.Controls.AddRange([lblTitle, lblDesc]);
        card.Click += handler;
        lblTitle.Click += handler;
        lblDesc.Click += handler;

        card.MouseEnter += (_, _) => card.BackColor = Color.FromArgb(25, 30, 50);
        card.MouseLeave += (_, _) => card.BackColor = CyberpunkTheme.Surface;

        return card;
    }

    private void RefreshStats()
    {
        _lblProductCount.Text = _svc.Products.Count.ToString();
        _lblLowStock.Text = _svc.GetLowStockProducts().Count.ToString();
        _lblCategories.Text = _svc.Categories.Count.ToString();
        _lblTxCount.Text = _svc.Transactions.Count.ToString();
        _lblLastSaved.Text = $"DATA: %APPDATA%\\RetailInventory\\inventory.json";
    }

    private int _glowStep = 0;
    private void AnimateGlow()
    {
        Color[] colors = [CyberpunkTheme.NeonCyan, CyberpunkTheme.NeonMagenta, CyberpunkTheme.NeonYellow, CyberpunkTheme.NeonOrange];
        _glowBar.BackColor = colors[_glowStep % colors.Length];
        _glowStep++;
    }

    private void OnProducts(object? sender, EventArgs e)
    {
        using var form = new ProductListForm(_svc);
        form.ShowDialog(this);
        RefreshStats();
    }

    private void OnCategories(object? sender, EventArgs e)
    {
        using var form = new CategoryManagerForm(_svc);
        form.ShowDialog(this);
        RefreshStats();
    }

    private void OnStock(object? sender, EventArgs e)
    {
        // Quick stock adjustment — pick a product first
        using var picker = new ProductListForm(_svc);
        picker.Text = "> SELECT PRODUCT FOR STOCK ADJUSTMENT";
        picker.ShowDialog(this);
        RefreshStats();
    }

    private void OnHistory(object? sender, EventArgs e)
    {
        using var form = new TransactionHistoryForm(_svc);
        form.ShowDialog(this);
    }

    private void OnPOS(object? sender, EventArgs e)
    {
        using var form = new POSForm(_svc);
        form.ShowDialog(this);
        RefreshStats();
    }
}
