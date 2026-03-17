using RetailInventory.Helpers;
using RetailInventory.Models;
using RetailInventory.Services;

namespace RetailInventory.Forms;

public class POSForm : Form
{
    private readonly InventoryService _svc;
    private readonly DateTime _sessionStart = DateTime.Now;
    private readonly List<CartLine> _cart = [];
    private readonly System.Windows.Forms.Timer _ticker = new();

    private TextBox _txtSearch = new();
    private DataGridView _dgvProducts = new();
    private DataGridView _dgvCart = new();
    private Label _lblSubtotal = new();
    private Label _lblTax = new();
    private Label _lblTotal = new();
    private Label _lblChange = new();
    private TextBox _txtDiscount = new();
    private TextBox _txtTender = new();
    private Label _lblOrderNum = new();
    private Label _lblSessionTime = new();
    private Label _lblItemCount = new();

    private sealed class CartLine
    {
        public Product Product { get; }
        public int Qty { get; set; }
        public decimal UnitPrice { get; }       // effective price (sale price when applicable)
        public decimal OriginalPrice { get; }   // regular list price
        public bool IsOnSale => UnitPrice != OriginalPrice;
        public CartLine(Product p, int qty, decimal price, decimal originalPrice)
            => (Product, Qty, UnitPrice, OriginalPrice) = (p, qty, price, originalPrice);
        public decimal LineTotal => Qty * UnitPrice;
    }

    public POSForm(InventoryService svc)
    {
        _svc = svc;
        BuildUI();
        if (AppSettingsService.Instance.Current.BorderlessMode)
            CyberpunkTheme.ApplyBorderlessMode(this, Text);
        FilterProducts();
        _ticker.Interval = 1000;
        _ticker.Tick += (_, _) => UpdateStatusBar();
        _ticker.Start();
    }

    private void BuildUI()
    {
        Text = "> POS TERMINAL // CYBERPUNK RETAIL";
        Size = new Size(1200, 750);
        MinimumSize = new Size(900, 620);
        StartPosition = FormStartPosition.CenterScreen;
        CyberpunkTheme.ApplyToForm(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            BackColor = CyberpunkTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 4));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(12, 12, 28) };
        header.Paint += DrawHeader;
        root.Controls.Add(header, 0, 0);

        var glowBar = new Panel { Dock = DockStyle.Fill, BackColor = CyberpunkTheme.NeonCyan };
        root.Controls.Add(glowBar, 0, 1);

        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = CyberpunkTheme.Background,
            Padding = new Padding(8)
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        split.Controls.Add(BuildLeftPanel(), 0, 0);
        split.Controls.Add(BuildRightPanel(), 1, 0);
        root.Controls.Add(split, 0, 2);

        root.Controls.Add(BuildStatusBar(), 0, 3);
        Controls.Add(root);
    }

    private Panel BuildLeftPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            BackColor = CyberpunkTheme.Background,
            Padding = new Padding(0, 0, 6, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(
            CyberpunkTheme.CreateNeonLabel("// PRODUCT FINDER", CyberpunkTheme.NeonCyan), 0, 0);

        CyberpunkTheme.StyleTextBox(_txtSearch);
        _txtSearch.Dock = DockStyle.Fill;
        _txtSearch.PlaceholderText = "Search by name or SKU...";
        _txtSearch.TextChanged += (_, _) => FilterProducts();
        _txtSearch.KeyDown += OnSearchKeyDown;
        panel.Controls.Add(_txtSearch, 0, 1);

        var lblHint = new Label
        {
            Text = "Click row to add  |  Low stock = orange  |  Out of stock = red",
            ForeColor = CyberpunkTheme.TextSecondary,
            Font = CyberpunkTheme.FontSmall,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        panel.Controls.Add(lblHint, 0, 2);

        _dgvProducts.Dock = DockStyle.Fill;
        CyberpunkTheme.StyleDataGridView(_dgvProducts);
        _dgvProducts.ReadOnly = true;
        _dgvProducts.MultiSelect = false;
        _dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "SKU", HeaderText = "SKU", Width = 80 });
        _dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Name", HeaderText = "NAME", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Price", HeaderText = "PRICE", Width = 80 });
        _dgvProducts.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Qty", HeaderText = "QTY", Width = 50 });
        _dgvProducts.CellClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && _dgvProducts.Rows[e.RowIndex].Tag is Product p)
                AddToCart(p);
        };
        _dgvProducts.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && _dgvProducts.CurrentRow?.Tag is Product p)
                AddToCart(p);
        };
        panel.Controls.Add(_dgvProducts, 0, 3);

        return panel;
    }

    private Panel BuildRightPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            BackColor = CyberpunkTheme.Background,
            Padding = new Padding(6, 0, 0, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 162));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        panel.Controls.Add(
            CyberpunkTheme.CreateNeonLabel("// ORDER CART  [DEL = remove row]", CyberpunkTheme.NeonYellow), 0, 0);

        _dgvCart.Dock = DockStyle.Fill;
        CyberpunkTheme.StyleDataGridView(_dgvCart);
        _dgvCart.MultiSelect = false;
        _dgvCart.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _dgvCart.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        _dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Item", HeaderText = "ITEM", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Qty", HeaderText = "QTY", Width = 55 });
        _dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "UnitPrice", HeaderText = "UNIT PRICE", Width = 95, ReadOnly = true });
        _dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "LineTotal", HeaderText = "LINE TOTAL", Width = 100, ReadOnly = true });
        _dgvCart.CellEndEdit += OnCartCellEndEdit;
        _dgvCart.KeyDown += OnCartKeyDown;
        panel.Controls.Add(_dgvCart, 0, 1);

        panel.Controls.Add(BuildTotalsPanel(), 0, 2);
        panel.Controls.Add(BuildButtonBar(), 0, 3);

        return panel;
    }

    private Panel BuildTotalsPanel()
    {
        var pnl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            BackColor = CyberpunkTheme.Surface,
            Padding = new Padding(10, 4, 10, 4)
        };
        pnl.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(
                60, CyberpunkTheme.NeonYellow.R, CyberpunkTheme.NeonYellow.G, CyberpunkTheme.NeonYellow.B));
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
        };
        pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        for (int i = 0; i < 6; i++) pnl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6));

        _lblSubtotal.ForeColor = CyberpunkTheme.TextPrimary;
        _lblSubtotal.Font = CyberpunkTheme.FontBody;
        _lblSubtotal.Text = "$0.00";

        _lblTax.ForeColor = CyberpunkTheme.NeonMagenta;
        _lblTax.Font = CyberpunkTheme.FontBody;
        _lblTax.Text = "$0.00";

        _lblTotal.ForeColor = CyberpunkTheme.NeonCyan;
        _lblTotal.Font = new Font("Consolas", 13f, FontStyle.Bold);
        _lblTotal.Text = "$0.00";

        _lblChange.ForeColor = CyberpunkTheme.SuccessGreen;
        _lblChange.Font = CyberpunkTheme.FontBody;
        _lblChange.Text = "$0.00";

        CyberpunkTheme.StyleTextBox(_txtDiscount);
        _txtDiscount.Text = "0";
        _txtDiscount.Dock = DockStyle.Fill;
        _txtDiscount.TextChanged += (_, _) => RecalcTotals();

        CyberpunkTheme.StyleTextBox(_txtTender);
        _txtTender.Text = "0.00";
        _txtTender.Dock = DockStyle.Fill;
        _txtTender.TextChanged += (_, _) => RecalcTotals();

        int r = 0;
        AddTotalRow(pnl, "SUBTOTAL:", _lblSubtotal, r++);
        AddTotalInput(pnl, "DISCOUNT %:", _txtDiscount, r++);
        AddTotalRow(pnl, "TAX:", _lblTax, r++);
        AddTotalRow(pnl, "TOTAL:", _lblTotal, r++);
        AddTotalInput(pnl, "TENDER $:", _txtTender, r++);
        AddTotalRow(pnl, "CHANGE:", _lblChange, r++);

        return pnl;
    }

    private static void AddTotalRow(TableLayoutPanel pnl, string text, Label valueLabel, int row)
    {
        pnl.Controls.Add(MakeRowLabel(text), 0, row);
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.TextAlign = ContentAlignment.MiddleRight;
        valueLabel.AutoSize = false;
        pnl.Controls.Add(valueLabel, 1, row);
    }

    private static void AddTotalInput(TableLayoutPanel pnl, string text, Control ctrl, int row)
    {
        pnl.Controls.Add(MakeRowLabel(text), 0, row);
        pnl.Controls.Add(ctrl, 1, row);
    }

    private static Label MakeRowLabel(string text) => new()
    {
        Text = text,
        ForeColor = CyberpunkTheme.TextSecondary,
        Font = CyberpunkTheme.FontBody,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private Panel BuildButtonBar()
    {
        var pnl = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = CyberpunkTheme.Background,
            Padding = new Padding(0, 4, 0, 0)
        };

        var btnProcess = new Button { Text = "[ PROCESS SALE ]", Width = 160, Height = 38 };
        CyberpunkTheme.StyleButton(btnProcess, CyberpunkTheme.NeonCyan);
        btnProcess.Click += (_, _) => ProcessSale();

        var btnClear = new Button { Text = "[ CLEAR CART ]", Width = 130, Height = 38 };
        CyberpunkTheme.StyleButton(btnClear, CyberpunkTheme.DangerRed);
        btnClear.Click += (_, _) => ClearCart();

        var btnReturn = new Button { Text = "[ RETURN/EXCHANGE ]", Width = 178, Height = 38 };
        CyberpunkTheme.StyleButton(btnReturn, CyberpunkTheme.NeonMagenta);
        btnReturn.Click += (_, _) => OpenReturn();

        pnl.Controls.AddRange([btnProcess, btnClear, btnReturn]);
        return pnl;
    }

    private Panel BuildStatusBar()
    {
        var bar = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(8, 8, 18) };

        _lblOrderNum.ForeColor = CyberpunkTheme.TextSecondary;
        _lblOrderNum.Font = CyberpunkTheme.FontSmall;
        _lblOrderNum.AutoSize = true;
        _lblOrderNum.Location = new Point(12, 8);

        _lblSessionTime.ForeColor = CyberpunkTheme.TextSecondary;
        _lblSessionTime.Font = CyberpunkTheme.FontSmall;
        _lblSessionTime.AutoSize = true;
        _lblSessionTime.Location = new Point(310, 8);

        _lblItemCount.ForeColor = CyberpunkTheme.TextSecondary;
        _lblItemCount.Font = CyberpunkTheme.FontSmall;
        _lblItemCount.AutoSize = true;
        _lblItemCount.Location = new Point(570, 8);

        UpdateStatusBar();
        bar.Controls.AddRange([_lblOrderNum, _lblSessionTime, _lblItemCount]);
        return bar;
    }

    private void DrawHeader(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        var panel = (Panel)sender!;
        g.Clear(Color.FromArgb(12, 12, 28));
        using var scanPen = new Pen(Color.FromArgb(6, 0, 255, 200));
        for (int y = 0; y < panel.Height; y += 3)
            g.DrawLine(scanPen, 0, y, panel.Width, y);
        using var titleFont = new Font("Consolas", 16f, FontStyle.Bold);
        using var subFont = new Font("Consolas", 9f);
        using var brush = new SolidBrush(CyberpunkTheme.NeonCyan);
        using var subBrush = new SolidBrush(CyberpunkTheme.TextSecondary);
        g.DrawString("POS TERMINAL", titleFont, brush, new PointF(16, 8));
        g.DrawString("// POINT OF SALE // CYBERPUNK RETAIL SYSTEM", subFont, subBrush, new PointF(18, 38));
        using var pen = new Pen(CyberpunkTheme.NeonMagenta, 1);
        g.DrawLines(pen, new Point[] { new(panel.Width - 60, 8), new(panel.Width - 8, 8), new(panel.Width - 8, 52) });
    }

    private void FilterProducts()
    {
        var term = _txtSearch.Text.Trim().ToLowerInvariant();
        var filtered = _svc.Products
            .Where(p => p.IsActive && (string.IsNullOrEmpty(term) ||
                p.Name.ToLowerInvariant().Contains(term) ||
                p.SKU.ToLowerInvariant().Contains(term)))
            .OrderBy(p => p.Name)
            .ToList();

        _dgvProducts.Rows.Clear();
        foreach (var p in filtered)
        {
            decimal? salePrice = _svc.GetSalePrice(p);
            string priceDisplay = salePrice.HasValue
                ? $"SALE:{CurrencyFormatter.Format(salePrice.Value)}"
                : CurrencyFormatter.Format(p.Price);
            int idx = _dgvProducts.Rows.Add(p.SKU, p.Name, priceDisplay, p.QuantityOnHand);
            var row = _dgvProducts.Rows[idx];
            row.Tag = p;
            if (p.QuantityOnHand == 0)
            {
                row.DefaultCellStyle.ForeColor = CyberpunkTheme.DangerRed;
                row.DefaultCellStyle.BackColor = Color.FromArgb(30, 10, 10);
            }
            else if (salePrice.HasValue)
            {
                row.DefaultCellStyle.ForeColor = CyberpunkTheme.NeonOrange;
            }
            else if (p.QuantityOnHand <= p.ReorderPoint)
            {
                row.DefaultCellStyle.ForeColor = CyberpunkTheme.NeonYellow;
            }
        }
    }

    private void AddToCart(Product p)
    {
        if (p.QuantityOnHand <= 0)
        {
            MessageBox.Show($"{p.Name} is out of stock.", "OUT OF STOCK",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var line = _cart.FirstOrDefault(c => c.Product.Id == p.Id);
        if (line != null)
        {
            if (line.Qty >= p.QuantityOnHand)
            {
                MessageBox.Show($"Cannot exceed available stock ({p.QuantityOnHand}).", "STOCK LIMIT",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            line.Qty++;
        }
        else
        {
            decimal effectivePrice = _svc.GetSalePrice(p) ?? p.Price;
            _cart.Add(new CartLine(p, 1, effectivePrice, p.Price));
        }
        RefreshCartGrid();
    }

    private void RefreshCartGrid()
    {
        _dgvCart.Rows.Clear();
        foreach (var line in _cart)
        {
            string itemName = line.IsOnSale ? $"[SALE] {line.Product.Name}" : line.Product.Name;
            int idx = _dgvCart.Rows.Add(
                itemName,
                line.Qty.ToString(),
                CurrencyFormatter.Format(line.UnitPrice),
                CurrencyFormatter.Format(line.LineTotal));
            if (line.IsOnSale)
                _dgvCart.Rows[idx].DefaultCellStyle.ForeColor = CyberpunkTheme.NeonOrange;
        }
        RecalcTotals();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && _dgvProducts.Rows.Count > 0
            && _dgvProducts.Rows[0].Tag is Product p)
        {
            AddToCart(p);
            e.Handled = true;
        }
    }

    private void OnCartCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (_dgvCart.Columns["Qty"] is not DataGridViewColumn qtyCol
            || e.ColumnIndex != qtyCol.Index) return;
        if (e.RowIndex < 0 || e.RowIndex >= _cart.Count) return;

        var cellVal = _dgvCart.Rows[e.RowIndex].Cells["Qty"].Value?.ToString() ?? "";
        var line = _cart[e.RowIndex];

        if (!ValidationHelper.IsValidQuantity(cellVal, out int qty) || qty <= 0)
        {
            _dgvCart.Rows[e.RowIndex].Cells["Qty"].Value = line.Qty.ToString();
            return;
        }

        var available = _svc.Products.FirstOrDefault(p => p.Id == line.Product.Id)?.QuantityOnHand ?? 0;
        if (qty > available)
        {
            MessageBox.Show($"Only {available} units available.", "STOCK LIMIT",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _dgvCart.Rows[e.RowIndex].Cells["Qty"].Value = line.Qty.ToString();
            return;
        }

        line.Qty = qty;
        _dgvCart.Rows[e.RowIndex].Cells["LineTotal"].Value = CurrencyFormatter.Format(line.LineTotal);
        RecalcTotals();
    }

    private void OnCartKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete && !_dgvCart.IsCurrentCellInEditMode
            && _dgvCart.CurrentRow != null)
        {
            int idx = _dgvCart.CurrentRow.Index;
            if (idx >= 0 && idx < _cart.Count)
            {
                _cart.RemoveAt(idx);
                RefreshCartGrid();
                e.Handled = true;
            }
        }
    }

    private void RecalcTotals()
    {
        decimal subtotal = _cart.Sum(c => c.LineTotal);
        decimal discPct = decimal.TryParse(_txtDiscount.Text, out decimal d) ? Math.Clamp(d, 0, 100) : 0;
        decimal discounted = subtotal * (1 - discPct / 100);

        var cfg = AppSettingsService.Instance.Current;
        decimal totalTaxPct = cfg.StateTaxRate + cfg.CountyTaxRate + cfg.CityTaxRate;
        decimal tax = Math.Round(discounted * totalTaxPct / 100m, 2);
        decimal total = discounted + tax;

        decimal tender = decimal.TryParse(_txtTender.Text, out decimal t) ? t : 0;
        decimal change = tender - total;

        _lblSubtotal.Text = CurrencyFormatter.Format(subtotal);
        _lblTax.Text = totalTaxPct > 0
            ? $"{CurrencyFormatter.Format(tax)} ({totalTaxPct:F2}%)"
            : "$0.00";
        _lblTotal.Text = CurrencyFormatter.Format(total);
        _lblChange.Text = change >= 0
            ? CurrencyFormatter.Format(change)
            : $"({CurrencyFormatter.Format(-change)})";
        _lblChange.ForeColor = change >= 0 ? CyberpunkTheme.SuccessGreen : CyberpunkTheme.DangerRed;

        UpdateStatusBar();
    }

    private void ProcessSale()
    {
        if (_cart.Count == 0)
        {
            MessageBox.Show("Cart is empty.", "POS ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var errors = _cart
            .Select(line => (line, avail: _svc.Products.FirstOrDefault(p => p.Id == line.Product.Id)?.QuantityOnHand ?? 0))
            .Where(x => x.avail < x.line.Qty)
            .Select(x => $"  {x.line.Product.Name}: want {x.line.Qty}, have {x.avail}")
            .ToList();

        if (errors.Count > 0)
        {
            MessageBox.Show("Insufficient stock:\n" + string.Join("\n", errors),
                "STOCK ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        decimal discPct = decimal.TryParse(_txtDiscount.Text, out decimal d) ? Math.Clamp(d, 0, 100) : 0;
        decimal discounted = _cart.Sum(c => c.LineTotal) * (1 - discPct / 100);
        var cfg = AppSettingsService.Instance.Current;
        decimal totalTaxPct = cfg.StateTaxRate + cfg.CountyTaxRate + cfg.CityTaxRate;
        decimal tax = Math.Round(discounted * totalTaxPct / 100m, 2);
        decimal total = discounted + tax;

        var confirm = MessageBox.Show(
            $"Process sale of {_cart.Sum(c => c.Qty)} item(s)\nTotal: {CurrencyFormatter.Format(total)}",
            "CONFIRM SALE", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        var orderNum = $"ORD-{DateTime.Now:yyyyMMdd-HHmmss}";
        var receipt = new System.Text.StringBuilder();
        receipt.AppendLine($"ORDER: {orderNum}");
        receipt.AppendLine(new string('\u2500', 44));

        foreach (var line in _cart)
        {
            _svc.RecordTransaction(new StockTransaction
            {
                Id = Guid.NewGuid(),
                ProductId = line.Product.Id,
                Type = TransactionType.Sale,
                Quantity = line.Qty,
                UnitPrice = line.UnitPrice,
                Notes = $"POS Sale {orderNum}",
                Timestamp = DateTime.Now
            });
            var name = line.Product.Name.Length > 24
                ? line.Product.Name[..21] + "..."
                : line.Product.Name.PadRight(24);
            string saleTag = line.IsOnSale ? " *SALE*" : "";
            receipt.AppendLine($"{name}  x{line.Qty}  {CurrencyFormatter.Format(line.LineTotal)}{saleTag}");
        }

        receipt.AppendLine(new string('\u2500', 44));
        if (discPct > 0) receipt.AppendLine($"Discount:  {discPct:F1}%");
        if (totalTaxPct > 0) receipt.AppendLine($"Tax ({totalTaxPct:F2}%): {CurrencyFormatter.Format(tax)}");
        receipt.AppendLine($"TOTAL:     {CurrencyFormatter.Format(total)}");
        decimal tender = decimal.TryParse(_txtTender.Text, out decimal t) ? t : 0;
        if (tender > 0)
            receipt.AppendLine($"TENDER:    {CurrencyFormatter.Format(tender)}  CHANGE: {CurrencyFormatter.Format(tender - total)}");

        MessageBox.Show(receipt.ToString(), "SALE COMPLETE", MessageBoxButtons.OK, MessageBoxIcon.Information);

        ClearCart();
        FilterProducts();
    }

    private void ClearCart()
    {
        _cart.Clear();
        RefreshCartGrid();
    }

    private void OpenReturn()
    {
        using var picker = new ProductListForm(_svc);
        picker.Text = "> SELECT PRODUCT FOR RETURN/EXCHANGE";
        picker.ShowDialog(this);
    }

    private void UpdateStatusBar()
    {
        _lblOrderNum.Text = $"ORDER#: {DateTime.Now:yyyyMMdd-HHmm}";
        _lblSessionTime.Text = $"SESSION: {(DateTime.Now - _sessionStart):hh\\:mm\\:ss}";
        _lblItemCount.Text = $"ITEMS IN CART: {_cart.Sum(c => c.Qty)}";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _ticker.Stop();
        base.OnFormClosed(e);
    }
}
