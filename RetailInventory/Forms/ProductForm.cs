using RetailInventory.Helpers;
using RetailInventory.Models;
using RetailInventory.Services;

namespace RetailInventory.Forms;

public class ProductForm : Form
{
    public Product Result { get; private set; }

    private readonly InventoryService _svc;
    private TextBox _txtName = new();
    private TextBox _txtSKU = new();
    private TextBox _txtDescription = new();
    private ComboBox _cbCategory = new();
    private TextBox _txtPrice = new();
    private TextBox _txtCost = new();
    private TextBox _txtQty = new();
    private TextBox _txtReorder = new();
    private CheckBox _chkActive = new();

    public ProductForm(InventoryService svc, Product? existing = null)
    {
        _svc = svc;
        Result = existing != null
            ? new Product
            {
                Id = existing.Id, Name = existing.Name, SKU = existing.SKU,
                Description = existing.Description, CategoryId = existing.CategoryId,
                Price = existing.Price, CostPrice = existing.CostPrice,
                QuantityOnHand = existing.QuantityOnHand, ReorderPoint = existing.ReorderPoint,
                IsActive = existing.IsActive, CreatedAt = existing.CreatedAt
            }
            : new Product();

        BuildUI(existing != null);
        if (AppSettingsService.Instance.Current.BorderlessMode)
            CyberpunkTheme.ApplyBorderlessMode(this, Text, hasMaximize: false);
    }

    private void BuildUI(bool isEdit)
    {
        Text = isEdit ? "> EDIT PRODUCT" : "> NEW PRODUCT";
        Size = new Size(480, 500);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        CyberpunkTheme.ApplyToForm(this);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 11,
            ColumnCount = 2,
            BackColor = CyberpunkTheme.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 10; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        int row = 0;
        var lblTitle = CyberpunkTheme.CreateNeonLabel(isEdit ? "// EDIT PRODUCT" : "// NEW PRODUCT", CyberpunkTheme.NeonYellow);
        layout.Controls.Add(lblTitle, 0, row); layout.SetColumnSpan(lblTitle, 2); row++;

        AddRow(layout, "NAME:", _txtName, ref row, Result.Name);
        AddRow(layout, "SKU:", _txtSKU, ref row, Result.SKU);
        AddRow(layout, "DESCRIPTION:", _txtDescription, ref row, Result.Description);

        // Category combo
        var lblCat = new Label { Text = "CATEGORY:", ForeColor = CyberpunkTheme.TextSecondary, Font = CyberpunkTheme.FontBody, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        CyberpunkTheme.StyleComboBox(_cbCategory);
        _cbCategory.Dock = DockStyle.Fill;
        _cbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
        _cbCategory.Items.Add(new CategoryItem(Guid.Empty, "(None)"));
        foreach (var cat in _svc.Categories)
            _cbCategory.Items.Add(new CategoryItem(cat.Id, cat.Name));
        _cbCategory.SelectedIndex = 0;
        for (int i = 0; i < _cbCategory.Items.Count; i++)
            if (((CategoryItem)_cbCategory.Items[i]!).Id == Result.CategoryId)
            { _cbCategory.SelectedIndex = i; break; }
        layout.Controls.Add(lblCat, 0, row);
        layout.Controls.Add(_cbCategory, 1, row); row++;

        AddRow(layout, "SELL PRICE:", _txtPrice, ref row, CurrencyFormatter.FormatPlain(Result.Price));
        AddRow(layout, "COST PRICE:", _txtCost, ref row, CurrencyFormatter.FormatPlain(Result.CostPrice));
        AddRow(layout, "QTY ON HAND:", _txtQty, ref row, Result.QuantityOnHand.ToString());
        AddRow(layout, "REORDER PT:", _txtReorder, ref row, Result.ReorderPoint.ToString());

        _chkActive.Text = "ACTIVE";
        _chkActive.ForeColor = CyberpunkTheme.NeonCyan;
        _chkActive.Font = CyberpunkTheme.FontBody;
        _chkActive.BackColor = Color.Transparent;
        _chkActive.Checked = Result.IsActive;
        _chkActive.Anchor = AnchorStyles.Left;
        layout.Controls.Add(_chkActive, 1, row); row++;

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var btnSave = new Button { Text = "[ SAVE ]", Width = 100, Height = 30 };
        var btnCancel = new Button { Text = "[ CANCEL ]", Width = 100, Height = 30 };
        CyberpunkTheme.StyleButton(btnSave, CyberpunkTheme.NeonCyan);
        CyberpunkTheme.StyleButton(btnCancel, CyberpunkTheme.NeonMagenta);
        btnSave.Click += OnSave;
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        btnPanel.Controls.Add(btnSave);
        btnPanel.Controls.Add(btnCancel);
        layout.Controls.Add(btnPanel, 0, row);
        layout.SetColumnSpan(btnPanel, 2);

        Controls.Add(layout);
    }

    private void AddRow(TableLayoutPanel layout, string label, TextBox tb, ref int row, string value)
    {
        var lbl = new Label { Text = label, ForeColor = CyberpunkTheme.TextSecondary, Font = CyberpunkTheme.FontBody, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        CyberpunkTheme.StyleTextBox(tb);
        tb.Dock = DockStyle.Fill;
        tb.Text = value;
        layout.Controls.Add(lbl, 0, row);
        layout.Controls.Add(tb, 1, row);
        row++;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        { MessageBox.Show("Product name is required.", "VALIDATION ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!ValidationHelper.IsValidSKU(_txtSKU.Text))
        { MessageBox.Show("Valid SKU is required (max 50 chars).", "VALIDATION ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!ValidationHelper.IsValidPrice(_txtPrice.Text, out decimal price))
        { MessageBox.Show("Invalid sell price.", "VALIDATION ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!ValidationHelper.IsValidPrice(_txtCost.Text, out decimal cost))
        { MessageBox.Show("Invalid cost price.", "VALIDATION ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!ValidationHelper.IsValidQuantity(_txtQty.Text, out int qty))
        { MessageBox.Show("Invalid quantity.", "VALIDATION ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!ValidationHelper.IsValidQuantity(_txtReorder.Text, out int reorder))
        { MessageBox.Show("Invalid reorder point.", "VALIDATION ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        Result.Name = _txtName.Text.Trim();
        Result.SKU = _txtSKU.Text.Trim();
        Result.Description = _txtDescription.Text.Trim();
        Result.CategoryId = ((CategoryItem)_cbCategory.SelectedItem!).Id;
        Result.Price = price;
        Result.CostPrice = cost;
        Result.QuantityOnHand = qty;
        Result.ReorderPoint = reorder;
        Result.IsActive = _chkActive.Checked;
        DialogResult = DialogResult.OK;
        Close();
    }

    private record CategoryItem(Guid Id, string Name)
    {
        public override string ToString() => Name;
    }
}
