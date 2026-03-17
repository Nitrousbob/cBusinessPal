using RetailInventory.Helpers;
using RetailInventory.Models;
using RetailInventory.Services;

namespace RetailInventory.Forms;

public class ProductListForm : Form
{
    private readonly InventoryService _svc;
    private DataGridView _grid = new();
    private TextBox _txtSearch = new();
    private ComboBox _cbCategory = new();
    private CheckBox _chkLowStock = new();
    private Label _lblStatus = new();

    public ProductListForm(InventoryService svc)
    {
        _svc = svc;
        BuildUI();
        if (AppSettingsService.Instance.Current.BorderlessMode)
            CyberpunkTheme.ApplyBorderlessMode(this, Text);
        RefreshGrid();
    }

    private void BuildUI()
    {
        Text = "> PRODUCT CATALOG";
        Size = new Size(1000, 600);
        MinimumSize = new Size(800, 450);
        StartPosition = FormStartPosition.CenterParent;
        CyberpunkTheme.ApplyToForm(this);

        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 3,
            ColumnCount = 1,
            BackColor = CyberpunkTheme.Background
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Title bar
        var titlePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        titlePanel.Controls.Add(CyberpunkTheme.CreateNeonLabel("// PRODUCT CATALOG", CyberpunkTheme.NeonYellow));
        outer.Controls.Add(titlePanel, 0, 0);

        // Toolbar
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = CyberpunkTheme.SurfaceAlt, Padding = new Padding(4) };

        CyberpunkTheme.StyleTextBox(_txtSearch);
        _txtSearch.Width = 180;
        _txtSearch.PlaceholderText = "SEARCH...";
        _txtSearch.TextChanged += (_, _) => RefreshGrid();
        toolbar.Controls.Add(_txtSearch);

        CyberpunkTheme.StyleComboBox(_cbCategory);
        _cbCategory.Width = 150;
        _cbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
        _cbCategory.Items.Add(new CategoryItem(Guid.Empty, "ALL CATEGORIES"));
        foreach (var cat in _svc.Categories) _cbCategory.Items.Add(new CategoryItem(cat.Id, cat.Name));
        _cbCategory.SelectedIndex = 0;
        _cbCategory.SelectedIndexChanged += (_, _) => RefreshGrid();
        toolbar.Controls.Add(_cbCategory);

        _chkLowStock.Text = "LOW STOCK ONLY";
        _chkLowStock.ForeColor = CyberpunkTheme.NeonOrange;
        _chkLowStock.Font = CyberpunkTheme.FontBody;
        _chkLowStock.BackColor = Color.Transparent;
        _chkLowStock.CheckedChanged += (_, _) => RefreshGrid();
        toolbar.Controls.Add(_chkLowStock);

        var btnNew = new Button { Text = "[ + NEW ]", Width = 90, Height = 28 };
        var btnEdit = new Button { Text = "[ EDIT ]", Width = 80, Height = 28 };
        var btnDelete = new Button { Text = "[ DEL ]", Width = 80, Height = 28 };
        var btnStock = new Button { Text = "[ STOCK ]", Width = 90, Height = 28 };
        var btnHistory = new Button { Text = "[ HISTORY ]", Width = 100, Height = 28 };
        CyberpunkTheme.StyleButton(btnNew, CyberpunkTheme.SuccessGreen);
        CyberpunkTheme.StyleButton(btnEdit, CyberpunkTheme.NeonCyan);
        CyberpunkTheme.StyleButton(btnDelete, CyberpunkTheme.DangerRed);
        CyberpunkTheme.StyleButton(btnStock, CyberpunkTheme.NeonOrange);
        CyberpunkTheme.StyleButton(btnHistory, CyberpunkTheme.NeonMagenta);
        btnNew.Click += OnNew;
        btnEdit.Click += OnEdit;
        btnDelete.Click += OnDelete;
        btnStock.Click += OnStock;
        btnHistory.Click += OnHistory;
        toolbar.Controls.AddRange([btnNew, btnEdit, btnDelete, btnStock, btnHistory]);
        outer.Controls.Add(toolbar, 0, 1);

        CyberpunkTheme.StyleDataGridView(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SKU", HeaderText = "SKU", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "NAME" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "CATEGORY", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "PRICE", Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cost", HeaderText = "COST", Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "QTY", Width = 70 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reorder", HeaderText = "REORDER", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "STATUS", Width = 80 });
        _grid.Tag = new List<Product>();
        outer.Controls.Add(_grid, 0, 2);

        Controls.Add(outer);
    }

    public void RefreshGrid()
    {
        _grid.Rows.Clear();
        var filter = _txtSearch.Text.Trim().ToLower();
        var catFilter = _cbCategory.SelectedItem is CategoryItem ci ? ci.Id : Guid.Empty;

        var products = _svc.Products
            .Where(p => string.IsNullOrEmpty(filter) ||
                        p.Name.ToLower().Contains(filter) ||
                        p.SKU.ToLower().Contains(filter))
            .Where(p => catFilter == Guid.Empty || p.CategoryId == catFilter)
            .Where(p => !_chkLowStock.Checked || p.QuantityOnHand <= p.ReorderPoint)
            .ToList();

        _grid.Tag = products;
        foreach (var p in products)
        {
            var cat = _svc.GetCategory(p.CategoryId);
            bool isLow = p.QuantityOnHand <= p.ReorderPoint;
            int rowIdx = _grid.Rows.Add(
                p.SKU, p.Name, cat?.Name ?? "-",
                CurrencyFormatter.Format(p.Price),
                CurrencyFormatter.Format(p.CostPrice),
                p.QuantityOnHand, p.ReorderPoint,
                p.IsActive ? (isLow ? "LOW" : "OK") : "INACTIVE"
            );
            if (!p.IsActive)
                _grid.Rows[rowIdx].DefaultCellStyle.ForeColor = CyberpunkTheme.TextSecondary;
            else if (isLow)
                _grid.Rows[rowIdx].DefaultCellStyle.ForeColor = CyberpunkTheme.NeonOrange;
        }
    }

    private Product? GetSelected()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        var products = (List<Product>)_grid.Tag!;
        int idx = _grid.SelectedRows[0].Index;
        return idx < products.Count ? products[idx] : null;
    }

    private void OnNew(object? sender, EventArgs e)
    {
        using var form = new ProductForm(_svc);
        if (form.ShowDialog(this) == DialogResult.OK)
        { _svc.AddProduct(form.Result); RefreshGrid(); }
    }

    private void OnEdit(object? sender, EventArgs e)
    {
        var p = GetSelected(); if (p == null) return;
        using var form = new ProductForm(_svc, p);
        if (form.ShowDialog(this) == DialogResult.OK)
        { _svc.UpdateProduct(form.Result); RefreshGrid(); }
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        var p = GetSelected(); if (p == null) return;
        if (MessageBox.Show($"Delete '{p.Name}'?", "CONFIRM DELETE", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        { _svc.DeleteProduct(p.Id); RefreshGrid(); }
    }

    private void OnStock(object? sender, EventArgs e)
    {
        var p = GetSelected(); if (p == null) return;
        using var form = new StockAdjustmentForm(p);
        if (form.ShowDialog(this) == DialogResult.OK && form.Result != null)
        { _svc.RecordTransaction(form.Result); RefreshGrid(); }
    }

    private void OnHistory(object? sender, EventArgs e)
    {
        var p = GetSelected(); if (p == null) return;
        using var form = new TransactionHistoryForm(_svc, p);
        form.ShowDialog(this);
    }

    private record CategoryItem(Guid Id, string Name)
    {
        public override string ToString() => Name;
    }
}
