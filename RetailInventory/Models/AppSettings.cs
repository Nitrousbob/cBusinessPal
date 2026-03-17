namespace RetailInventory.Models;

public class AppSettings
{
    public bool BorderlessMode { get; set; } = false;
    public decimal StateTaxRate { get; set; } = 0;    // e.g. 6.5 = 6.5%
    public decimal CountyTaxRate { get; set; } = 0;
    public decimal CityTaxRate { get; set; } = 0;
}
