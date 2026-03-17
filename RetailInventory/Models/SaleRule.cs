namespace RetailInventory.Models;

public enum SaleTargetType { Category, Product }
public enum SaleDiscountType { PercentOff, FixedPrice }

public class SaleRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public SaleTargetType TargetType { get; set; }
    public Guid TargetId { get; set; } // Category.Id or Product.Id
    public SaleDiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; } // % off OR fixed sale price
    public bool IsActive { get; set; } = true;
}
