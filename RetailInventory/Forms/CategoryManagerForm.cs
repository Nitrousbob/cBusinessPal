using RetailInventory.Helpers;
using RetailInventory.Models;
using RetailInventory.Services;

namespace RetailInventory.Forms;

public class CategoryManagerForm : Form
{
    private readonly InventoryService _svc;
    private ListBox _listBox = new();

    public CategoryManagerForm(InventoryService svc)
    {
        _svc = svc;
        BuildUI();
        if (AppSettingsService.Instance.Current.BorderlessMode)
            CyberpunkTheme.ApplyBorderlessMode(this, Text);
        RefreshList();
    }

    private void BuildUI()
    {
        Text = "> CATEGORY MANAGER";
        Size = new Size(500, 420);
        MinimumSize = new Size(400, 320);
        StartPosition = FormStartPosition.CenterParent;
        CyberpunkTheme.ApplyToForm(this);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            RowCount = 3,
            ColumnCount = 1,
            BackColor = CyberpunkTheme.Background
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        layout.Controls.Add(CyberpunkTheme.CreateNeonLabel("// CATEGORY MANAGER", CyberpunkTheme.NeonMagenta), 0, 0);

        _listBox.Dock = DockStyle.Fill;
        _listBox.BackColor = CyberpunkTheme.Surface;
        _listBox.ForeColor = CyberpunkTheme.TextPrimary;
        _listBox.Font = CyberpunkTheme.FontBody;
        _listBox.BorderStyle = BorderStyle.FixedSingle;
        layout.Controls.Add(_listBox, 0, 1);

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        var btnNew = new Button { Text = "[ + NEW ]", Width = 90, Height = 30 };
        var btnEdit = new Button { Text = "[ EDIT ]", Width = 80, Height = 30 };
        var btnDelete = new Button { Text = "[ DEL ]", Width = 80, Height = 30 };
        CyberpunkTheme.StyleButton(btnNew, CyberpunkTheme.SuccessGreen);
        CyberpunkTheme.StyleButton(btnEdit, CyberpunkTheme.NeonCyan);
        CyberpunkTheme.StyleButton(btnDelete, CyberpunkTheme.DangerRed);
        btnNew.Click += OnNew;
        btnEdit.Click += OnEdit;
        btnDelete.Click += OnDelete;
        btnPanel.Controls.AddRange([btnNew, btnEdit, btnDelete]);
        layout.Controls.Add(btnPanel, 0, 2);

        Controls.Add(layout);
    }

    private void RefreshList()
    {
        _listBox.Items.Clear();
        foreach (var cat in _svc.Categories)
            _listBox.Items.Add(new CategoryItem(cat));
    }

    private Category? GetSelected() => _listBox.SelectedItem is CategoryItem ci ? ci.Category : null;

    private void OnNew(object? sender, EventArgs e)
    {
        using var form = new CategoryForm();
        if (form.ShowDialog(this) == DialogResult.OK)
        { _svc.AddCategory(form.Result); RefreshList(); }
    }

    private void OnEdit(object? sender, EventArgs e)
    {
        var cat = GetSelected(); if (cat == null) return;
        using var form = new CategoryForm(cat);
        if (form.ShowDialog(this) == DialogResult.OK)
        { _svc.UpdateCategory(form.Result); RefreshList(); }
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        var cat = GetSelected(); if (cat == null) return;
        if (MessageBox.Show($"Delete category '{cat.Name}'?", "CONFIRM DELETE",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        { _svc.DeleteCategory(cat.Id); RefreshList(); }
    }

    private class CategoryItem(Category cat)
    {
        public Category Category { get; } = cat;
        public override string ToString() => $"{Category.Name}  —  {Category.Description}";
    }
}
