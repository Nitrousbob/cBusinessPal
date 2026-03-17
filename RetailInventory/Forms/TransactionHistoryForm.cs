using RetailInventory.Helpers;
using RetailInventory.Models;
using RetailInventory.Services;

namespace RetailInventory.Forms;

public class TransactionHistoryForm : Form
{
    private readonly InventoryService _svc;
    private readonly Product? _filterProduct;
    private DataGridView _grid = new();

    public TransactionHistoryForm(InventoryService svc, Product? filterProduct = null)
    {
        _svc = svc;
        _filterProduct = filterProduct;
        BuildUI();
        if (AppSettingsService.Instance.Current.BorderlessMode)
            CyberpunkTheme.ApplyBorderlessMode(this, Text);
        LoadData();
    }

    private void BuildUI()
    {
        Text = _filterProduct != null
            ? $"> TRANSACTION HISTORY // {_filterProduct.Name}"
            : "> TRANSACTION HISTORY // ALL";
        Size = new Size(800, 500);
        MinimumSize = new Size(600, 400);
        StartPosition = FormStartPosition.CenterParent;
        CyberpunkTheme.ApplyToForm(this);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 2,
            ColumnCount = 1,
            BackColor = CyberpunkTheme.Background
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lblTitle = CyberpunkTheme.CreateNeonLabel("// TRANSACTION HISTORY", CyberpunkTheme.NeonCyan);
        layout.Controls.Add(lblTitle, 0, 0);

        CyberpunkTheme.StyleDataGridView(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Timestamp", HeaderText = "TIMESTAMP", Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Product", HeaderText = "PRODUCT" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "TYPE", Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "QTY", Width = 70 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "UNIT $", Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "NOTES" });
        layout.Controls.Add(_grid, 0, 1);

        Controls.Add(layout);
    }

    private void LoadData()
    {
        _grid.Rows.Clear();
        var transactions = _filterProduct != null
            ? _svc.GetTransactionsForProduct(_filterProduct.Id)
            : _svc.Transactions.OrderByDescending(t => t.Timestamp).ToList();

        foreach (var tx in transactions)
        {
            var product = _svc.Products.FirstOrDefault(p => p.Id == tx.ProductId);
            var row = _grid.Rows.Add(
                tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                product?.Name ?? "UNKNOWN",
                tx.Type.ToString().ToUpper(),
                tx.Quantity,
                CurrencyFormatter.Format(tx.UnitPrice),
                tx.Notes
            );
            var txType = tx.Type;
            _grid.Rows[row].DefaultCellStyle.ForeColor = txType switch
            {
                TransactionType.Sale => CyberpunkTheme.NeonMagenta,
                TransactionType.Receive => CyberpunkTheme.SuccessGreen,
                TransactionType.Return => CyberpunkTheme.NeonYellow,
                TransactionType.Adjustment => CyberpunkTheme.NeonOrange,
                _ => CyberpunkTheme.TextPrimary
            };
        }
    }
}
