using RetailInventory.Helpers;
using RetailInventory.Models;
using RetailInventory.Services;

namespace RetailInventory.Forms;

public class SaleManagerForm : Form
{
    private readonly InventoryService _svc;
    private DataGridView _dgv = new();
    private ComboBox _cboTargetType = new();
    private ComboBox _cboTarget = new();
    private ComboBox _cboDiscountType = new();
    private TextBox _txtName = new();
    private TextBox _txtValue = new();
    private CheckBox _chkActive = new();
    private SaleRule? _editing;

    public SaleManagerForm(InventoryService svc)
    {
        _svc = svc;
        BuildUI();
        RefreshGrid();
        if (AppSettingsService.Instance.Current.BorderlessMode)
            CyberpunkTheme.ApplyBorderlessMode(this, Text);
    }

    private void BuildUI()
    {
        Text = "> SALE MANAGER // CYBERPUNK RETAIL";
        Size = new Size(920, 620);
        MinimumSize = new Size(720, 500);
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(12, 12, 28) };
        header.Paint += DrawHeader;
        root.Controls.Add(header, 0, 0);

        var glowBar = new Panel { Dock = DockStyle.Fill, BackColor = CyberpunkTheme.NeonOrange };
        root.Controls.Add(glowBar, 0, 1);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = CyberpunkTheme.Background,
            Padding = new Padding(12)
        };
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 196));

        _dgv.Dock = DockStyle.Fill;
        CyberpunkTheme.StyleDataGridView(_dgv);
        _dgv.ReadOnly = true;
        _dgv.MultiSelect = false;
        _dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _dgv.Columns.Add(new DataGridViewCheckBoxColumn
            { Name = "Active", HeaderText = "ON", Width = 38, ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Name", HeaderText = "RULE NAME", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "TargetType", HeaderText = "APPLIES TO", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Target", HeaderText = "TARGET", Width = 180, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "DiscountType", HeaderText = "TYPE", Width = 90, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Value", HeaderText = "VALUE", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        _dgv.SelectionChanged += (_, _) => LoadSelectedRule();
        content.Controls.Add(_dgv, 0, 0);

        content.Controls.Add(BuildEditorPanel(), 0, 1);
        root.Controls.Add(content, 0, 2);

        var status = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(8, 8, 18) };
        status.Controls.Add(new Label
        {
            Text = "Select a rule to edit  |  [ NEW ] to create  |  [ DEL ] to remove",
            ForeColor = CyberpunkTheme.TextSecondary,
            Font = CyberpunkTheme.FontSmall,
            Location = new Point(12, 6),
            AutoSize = true
        });
        root.Controls.Add(status, 0, 3);

        Controls.Add(root);
    }

    private Panel BuildEditorPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 2,
            BackColor = CyberpunkTheme.Surface,
            Padding = new Padding(10, 6, 10, 6)
        };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(70, CyberpunkTheme.NeonOrange.R,
                CyberpunkTheme.NeonOrange.G, CyberpunkTheme.NeonOrange.B));
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190)); // name
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); // target type
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // target
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); // discount type
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));  // value
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230)); // active + buttons
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 56));

        // Labels row
        panel.Controls.Add(MakeLabel("RULE NAME"), 0, 0);
        panel.Controls.Add(MakeLabel("APPLIES TO"), 1, 0);
        panel.Controls.Add(MakeLabel("TARGET"), 2, 0);
        panel.Controls.Add(MakeLabel("DISCOUNT TYPE"), 3, 0);
        panel.Controls.Add(MakeLabel("VALUE"), 4, 0);
        panel.Controls.Add(MakeLabel(""), 5, 0);

        // Inputs row
        CyberpunkTheme.StyleTextBox(_txtName);
        _txtName.Dock = DockStyle.Fill;
        panel.Controls.Add(_txtName, 0, 1);

        _cboTargetType.Dock = DockStyle.Fill;
        _cboTargetType.Items.AddRange(["Category", "Product"]);
        _cboTargetType.SelectedIndex = 0;
        CyberpunkTheme.StyleComboBox(_cboTargetType);
        _cboTargetType.SelectedIndexChanged += (_, _) => PopulateTargets();
        panel.Controls.Add(_cboTargetType, 1, 1);

        _cboTarget.Dock = DockStyle.Fill;
        CyberpunkTheme.StyleComboBox(_cboTarget);
        panel.Controls.Add(_cboTarget, 2, 1);

        _cboDiscountType.Dock = DockStyle.Fill;
        _cboDiscountType.Items.AddRange(["% Off", "Fixed Price $"]);
        _cboDiscountType.SelectedIndex = 0;
        CyberpunkTheme.StyleComboBox(_cboDiscountType);
        panel.Controls.Add(_cboDiscountType, 3, 1);

        CyberpunkTheme.StyleTextBox(_txtValue);
        _txtValue.Dock = DockStyle.Fill;
        _txtValue.Text = "10";
        panel.Controls.Add(_txtValue, 4, 1);

        // Active + buttons
        var btnArea = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            WrapContents = false
        };

        _chkActive.Text = "Active";
        _chkActive.ForeColor = CyberpunkTheme.NeonOrange;
        _chkActive.Font = CyberpunkTheme.FontBody;
        _chkActive.Checked = true;
        _chkActive.AutoSize = true;
        _chkActive.Margin = new Padding(0, 6, 8, 0);
        btnArea.Controls.Add(_chkActive);

        var btnSave = new Button { Text = "[ SAVE ]", Width = 76, Height = 30 };
        CyberpunkTheme.StyleButton(btnSave, CyberpunkTheme.NeonOrange);
        btnSave.Click += OnSave;
        btnArea.Controls.Add(btnSave);

        var btnNew = new Button { Text = "[ NEW ]", Width = 68, Height = 30 };
        CyberpunkTheme.StyleButton(btnNew, CyberpunkTheme.NeonCyan);
        btnNew.Click += (_, _) => ClearEditor();
        btnArea.Controls.Add(btnNew);

        var btnDelete = new Button { Text = "[ DEL ]", Width = 68, Height = 30 };
        CyberpunkTheme.StyleButton(btnDelete, CyberpunkTheme.DangerRed);
        btnDelete.Click += OnDelete;
        btnArea.Controls.Add(btnDelete);

        panel.Controls.Add(btnArea, 5, 1);

        PopulateTargets();
        return panel;
    }

    private void PopulateTargets()
    {
        _cboTarget.Items.Clear();
        if (_cboTargetType.SelectedIndex == 0) // Category
        {
            foreach (var cat in _svc.Categories.OrderBy(c => c.Name))
                _cboTarget.Items.Add(new TargetItem(cat.Id, cat.Name));
        }
        else // Product
        {
            foreach (var p in _svc.Products.Where(p => p.IsActive).OrderBy(p => p.Name))
                _cboTarget.Items.Add(new TargetItem(p.Id, p.Name));
        }
        if (_cboTarget.Items.Count > 0) _cboTarget.SelectedIndex = 0;
    }

    private void RefreshGrid()
    {
        _dgv.Rows.Clear();
        foreach (var rule in _svc.SaleRules)
        {
            string targetName = GetTargetName(rule);
            string valueDisplay = rule.DiscountType == SaleDiscountType.PercentOff
                ? $"{rule.DiscountValue:F1}% off"
                : $"${rule.DiscountValue:F2} fixed";
            int idx = _dgv.Rows.Add(
                rule.IsActive,
                rule.Name,
                rule.TargetType == SaleTargetType.Category ? "Category" : "Product",
                targetName,
                rule.DiscountType == SaleDiscountType.PercentOff ? "% Off" : "Fixed $",
                valueDisplay);
            _dgv.Rows[idx].Tag = rule;
            _dgv.Rows[idx].DefaultCellStyle.ForeColor = rule.IsActive
                ? CyberpunkTheme.NeonOrange
                : CyberpunkTheme.TextSecondary;
        }
    }

    private string GetTargetName(SaleRule rule) =>
        rule.TargetType == SaleTargetType.Category
            ? _svc.Categories.FirstOrDefault(c => c.Id == rule.TargetId)?.Name ?? "?"
            : _svc.Products.FirstOrDefault(p => p.Id == rule.TargetId)?.Name ?? "?";

    private void LoadSelectedRule()
    {
        if (_dgv.CurrentRow?.Tag is not SaleRule rule) return;
        _editing = rule;
        _txtName.Text = rule.Name;
        _cboTargetType.SelectedIndex = rule.TargetType == SaleTargetType.Category ? 0 : 1;
        PopulateTargets();
        for (int i = 0; i < _cboTarget.Items.Count; i++)
        {
            if (_cboTarget.Items[i] is TargetItem ti && ti.Id == rule.TargetId)
            { _cboTarget.SelectedIndex = i; break; }
        }
        _cboDiscountType.SelectedIndex = rule.DiscountType == SaleDiscountType.PercentOff ? 0 : 1;
        _txtValue.Text = rule.DiscountValue.ToString("F2");
        _chkActive.Checked = rule.IsActive;
    }

    private void ClearEditor()
    {
        _editing = null;
        _txtName.Text = "";
        _cboTargetType.SelectedIndex = 0;
        PopulateTargets();
        _cboDiscountType.SelectedIndex = 0;
        _txtValue.Text = "10";
        _chkActive.Checked = true;
        _dgv.ClearSelection();
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        { MessageBox.Show("Rule name required.", "VALIDATION", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (_cboTarget.SelectedItem is not TargetItem target)
        { MessageBox.Show("Select a target.", "VALIDATION", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!decimal.TryParse(_txtValue.Text, out decimal val) || val < 0)
        { MessageBox.Show("Enter a valid positive value.", "VALIDATION", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var rule = _editing ?? new SaleRule();
        rule.Name = _txtName.Text.Trim();
        rule.TargetType = _cboTargetType.SelectedIndex == 0 ? SaleTargetType.Category : SaleTargetType.Product;
        rule.TargetId = target.Id;
        rule.DiscountType = _cboDiscountType.SelectedIndex == 0 ? SaleDiscountType.PercentOff : SaleDiscountType.FixedPrice;
        rule.DiscountValue = val;
        rule.IsActive = _chkActive.Checked;

        if (_editing == null) _svc.AddSaleRule(rule);
        else _svc.UpdateSaleRule(rule);

        RefreshGrid();
        ClearEditor();
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        if (_editing == null)
        { MessageBox.Show("Select a rule to delete.", "INFO", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (MessageBox.Show($"Delete rule '{_editing.Name}'?", "CONFIRM DELETE",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _svc.DeleteSaleRule(_editing.Id);
        RefreshGrid();
        ClearEditor();
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        ForeColor = CyberpunkTheme.TextSecondary,
        Font = CyberpunkTheme.FontSmall,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.BottomLeft
    };

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
        using var brush = new SolidBrush(CyberpunkTheme.NeonOrange);
        using var subBrush = new SolidBrush(CyberpunkTheme.TextSecondary);
        g.DrawString("SALE MANAGER", titleFont, brush, new PointF(16, 8));
        g.DrawString("// CONFIGURE DISCOUNTS AND PROMOTIONS BY CATEGORY OR PRODUCT", subFont, subBrush, new PointF(18, 38));
        using var pen = new Pen(CyberpunkTheme.NeonOrange, 1);
        g.DrawLines(pen, new Point[] { new(panel.Width - 60, 8), new(panel.Width - 8, 8), new(panel.Width - 8, 52) });
    }

    private sealed record TargetItem(Guid Id, string Name)
    {
        public override string ToString() => Name;
    }
}
