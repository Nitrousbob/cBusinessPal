namespace RetailInventory.Models;

public class InventoryData
{
    public List<Category> Categories { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public List<StockTransaction> Transactions { get; set; } = [];
    public List<SaleRule> SaleRules { get; set; } = [];
    public DateTime LastSaved { get; set; } = DateTime.Now;
}
