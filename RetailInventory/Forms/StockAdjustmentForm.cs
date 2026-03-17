using RetailInventory.Helpers;
using RetailInventory.Models;
using RetailInventory.Services;

namespace RetailInventory.Forms;

public class StockAdjustmentForm : Form
{
    public StockTransaction? Result { get; private set; }

    private readonly Product _product;
    private ComboBox _cbType = new();
    private TextBox _txtQty = new();
    private TextBox _txtPrice = new();
    private TextBox _txtNotes = new();

    public StockAdjustmentForm(Product product)
    {
        _product = product;
        BuildUI();
        if (AppSettingsService.Instance.Current.BorderlessMode)
            CyberpunkTheme.ApplyBorderlessMode(this, Text, hasMaximize: false);
    }

    private void BuildUI()
    {
        Text = $"> STOCK ADJUSTMENT // {_product.Name}";
        Size = new Size(420, 360);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        CyberpunkTheme.ApplyToForm(this);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 8,
            ColumnCount = 2,
            BackColor = CyberpunkTheme.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 8; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        int row = 0;
        var lblTitle = CyberpunkTheme.CreateNeonLabel("// STOCK ADJUSTMENT", CyberpunkTheme.NeonOrange);
        layout.Controls.Add(lblTitle, 0, row); layout.SetColumnSpan(lblTitle, 2); row++;

        var lblProduct = new Label
        {
            Text = $"PRODUCT: {_product.SKU} — {_product.Name}",
            ForeColor = CyberpunkTheme.TextSecondary,
            Font = CyberpunkTheme.FontSmall,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        layout.Controls.Add(lblProduct, 0, row); layout.SetColumnSpan(lblProduct, 2); row++;

        var lblCurrent = new Label
        {
            Text = $"CURRENT QTY: {_product.QuantityOnHand}",
            ForeColor = CyberpunkTheme.NeonYellow,
            Font = CyberpunkTheme.FontBody,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        layout.Controls.Add(lblCurrent, 0, row); layout.SetColumnSpan(lblCurrent, 2); row++;

        var lblType = new Label { Text = "TYPE:", ForeColor = CyberpunkTheme.TextSecondary, Font = CyberpunkTheme.FontBody, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        CyberpunkTheme.StyleComboBox(_cbType);
        _cbType.Dock = DockStyle.Fill;
        _cbType.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (var t in Enum.GetValues<TransactionType>()) _cbType.Items.Add(t);
        _cbType.SelectedIndex = 0;
        layout.Controls.Add(lblType, 0, row); layout.Controls.Add(_cbType, 1, row); row++;

        AddRow(layout, "QUANTITY:", _txtQty, ref row, "0");
        AddRow(layout, "UNIT PRICE:", _txtPrice, ref row, CurrencyFormatter.FormatPlain(_product.Price));

        var lblNotes = new Label { Text = "NOTES:", ForeColor = CyberpunkTheme.TextSecondary, Font = CyberpunkTheme.FontBody, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        CyberpunkTheme.StyleTextBox(_txtNotes);
        _txtNotes.Dock = DockStyle.Fill;
        layout.Controls.Add(lblNotes, 0, row); layout.Controls.Add(_txtNotes, 1, row); row++;

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var btnConfirm = new Button { Text = "[ CONFIRM ]", Width = 110, Height = 30 };
        var btnCancel = new Button { Text = "[ CANCEL ]", Width = 100, Height = 30 };
        CyberpunkTheme.StyleButton(btnConfirm, CyberpunkTheme.NeonOrange);
        CyberpunkTheme.StyleButton(btnCancel, CyberpunkTheme.NeonMagenta);
        btnConfirm.Click += OnConfirm;
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        btnPanel.Controls.Add(btnConfirm);
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

    private void OnConfirm(object? sender, EventArgs e)
    {
        if (!ValidationHelper.IsValidQuantity(_txtQty.Text, out int qty) || qty == 0)
        { MessageBox.Show("Quantity must be a non-zero integer.", "VALIDATION ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!ValidationHelper.IsValidPrice(_txtPrice.Text, out decimal price))
        { MessageBox.Show("Invalid unit price.", "VALIDATION ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        Result = new StockTransaction
        {
            ProductId = _product.Id,
            Type = (TransactionType)_cbType.SelectedItem!,
            Quantity = qty,
            UnitPrice = price,
            Notes = _txtNotes.Text.Trim()
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}
