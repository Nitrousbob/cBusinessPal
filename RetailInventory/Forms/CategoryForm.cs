using RetailInventory.Helpers;
using RetailInventory.Models;
using RetailInventory.Services;

namespace RetailInventory.Forms;

public class CategoryForm : Form
{
    public Category Result { get; private set; }

    private TextBox _txtName = new();
    private TextBox _txtDescription = new();

    public CategoryForm(Category? existing = null)
    {
        Result = existing != null
            ? new Category { Id = existing.Id, Name = existing.Name, Description = existing.Description }
            : new Category();

        BuildUI(existing != null);
        if (AppSettingsService.Instance.Current.BorderlessMode)
            CyberpunkTheme.ApplyBorderlessMode(this, Text, hasMaximize: false);
    }

    private void BuildUI(bool isEdit)
    {
        Text = isEdit ? "> EDIT CATEGORY" : "> NEW CATEGORY";
        Size = new Size(420, 260);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        CyberpunkTheme.ApplyToForm(this);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 4,
            ColumnCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.BackColor = CyberpunkTheme.Background;

        var lblTitle = CyberpunkTheme.CreateNeonLabel(isEdit ? "// EDIT CATEGORY" : "// NEW CATEGORY", CyberpunkTheme.NeonMagenta);
        layout.Controls.Add(lblTitle, 0, 0);
        layout.SetColumnSpan(lblTitle, 2);

        var lblName = new Label { Text = "NAME:", ForeColor = CyberpunkTheme.TextSecondary, Font = CyberpunkTheme.FontBody, Anchor = AnchorStyles.Left | AnchorStyles.Top, AutoSize = true };
        layout.Controls.Add(lblName, 0, 1);
        CyberpunkTheme.StyleTextBox(_txtName);
        _txtName.Dock = DockStyle.Fill;
        _txtName.Text = Result.Name;
        layout.Controls.Add(_txtName, 1, 1);

        var lblDesc = new Label { Text = "DESCRIPTION:", ForeColor = CyberpunkTheme.TextSecondary, Font = CyberpunkTheme.FontBody, Anchor = AnchorStyles.Left | AnchorStyles.Top, AutoSize = true };
        layout.Controls.Add(lblDesc, 0, 2);
        CyberpunkTheme.StyleTextBox(_txtDescription);
        _txtDescription.Dock = DockStyle.Fill;
        _txtDescription.Multiline = true;
        _txtDescription.Text = Result.Description;
        layout.Controls.Add(_txtDescription, 1, 2);

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var btnSave = new Button { Text = "[ SAVE ]", Width = 100, Height = 30 };
        var btnCancel = new Button { Text = "[ CANCEL ]", Width = 100, Height = 30 };
        CyberpunkTheme.StyleButton(btnSave, CyberpunkTheme.NeonCyan);
        CyberpunkTheme.StyleButton(btnCancel, CyberpunkTheme.NeonMagenta);
        btnSave.Click += OnSave;
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        btnPanel.Controls.Add(btnSave);
        btnPanel.Controls.Add(btnCancel);
        layout.Controls.Add(btnPanel, 0, 3);
        layout.SetColumnSpan(btnPanel, 2);

        Controls.Add(layout);
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show("Category name is required.", "VALIDATION ERROR",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Result.Name = _txtName.Text.Trim();
        Result.Description = _txtDescription.Text.Trim();
        DialogResult = DialogResult.OK;
        Close();
    }
}
