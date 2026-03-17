using RetailInventory.Helpers;
using RetailInventory.Services;

namespace RetailInventory.Forms;

public class TaxSettingsDialog : Form
{
    private TextBox _txtState = new();
    private TextBox _txtCounty = new();
    private TextBox _txtCity = new();

    public TaxSettingsDialog()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        Text = "> TAX SETTINGS";
        Size = new Size(360, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        CyberpunkTheme.ApplyToForm(this);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(16),
            BackColor = CyberpunkTheme.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int i = 0; i < 4; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var settings = AppSettingsService.Instance.Current;

        AddRow(layout, "STATE TAX %:", _txtState, settings.StateTaxRate.ToString("F2"), 0);
        AddRow(layout, "COUNTY TAX %:", _txtCounty, settings.CountyTaxRate.ToString("F2"), 1);
        AddRow(layout, "CITY TAX %:", _txtCity, settings.CityTaxRate.ToString("F2"), 2);

        // Total rate preview
        var lblPreviewLabel = new Label
        {
            Text = "TOTAL RATE:",
            ForeColor = CyberpunkTheme.TextSecondary,
            Font = CyberpunkTheme.FontBody,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var lblPreview = new Label
        {
            ForeColor = CyberpunkTheme.NeonCyan,
            Font = CyberpunkTheme.FontBody,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight
        };
        UpdatePreview(lblPreview);
        _txtState.TextChanged += (_, _) => UpdatePreview(lblPreview);
        _txtCounty.TextChanged += (_, _) => UpdatePreview(lblPreview);
        _txtCity.TextChanged += (_, _) => UpdatePreview(lblPreview);
        layout.Controls.Add(lblPreviewLabel, 0, 3);
        layout.Controls.Add(lblPreview, 1, 3);

        // Buttons
        var btnArea = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent
        };
        var btnSave = new Button { Text = "[ SAVE ]", Width = 90, Height = 30 };
        CyberpunkTheme.StyleButton(btnSave, CyberpunkTheme.NeonCyan);
        btnSave.Click += OnSave;
        var btnCancel = new Button { Text = "[ CANCEL ]", Width = 100, Height = 30 };
        CyberpunkTheme.StyleButton(btnCancel, CyberpunkTheme.TextSecondary);
        btnCancel.Click += (_, _) => Close();
        btnArea.Controls.AddRange([btnSave, btnCancel]);
        layout.Controls.Add(btnArea, 0, 4);
        layout.SetColumnSpan(btnArea, 2);

        Controls.Add(layout);
    }

    private void AddRow(TableLayoutPanel layout, string labelText, TextBox tb, string value, int row)
    {
        layout.Controls.Add(new Label
        {
            Text = labelText,
            ForeColor = CyberpunkTheme.TextSecondary,
            Font = CyberpunkTheme.FontBody,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);

        CyberpunkTheme.StyleTextBox(tb);
        tb.Text = value;
        tb.Dock = DockStyle.Fill;
        layout.Controls.Add(tb, 1, row);
    }

    private void UpdatePreview(Label lbl)
    {
        decimal s = decimal.TryParse(_txtState.Text, out decimal sv) ? sv : 0;
        decimal co = decimal.TryParse(_txtCounty.Text, out decimal cv) ? cv : 0;
        decimal ci = decimal.TryParse(_txtCity.Text, out decimal citv) ? citv : 0;
        lbl.Text = $"{s + co + ci:F2}%";
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (!decimal.TryParse(_txtState.Text, out decimal state) || state < 0
            || !decimal.TryParse(_txtCounty.Text, out decimal county) || county < 0
            || !decimal.TryParse(_txtCity.Text, out decimal city) || city < 0)
        {
            MessageBox.Show("Enter valid non-negative rates.", "VALIDATION",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var s = AppSettingsService.Instance.Current;
        s.StateTaxRate = state;
        s.CountyTaxRate = county;
        s.CityTaxRate = city;
        AppSettingsService.Instance.Save();
        Close();
    }
}
