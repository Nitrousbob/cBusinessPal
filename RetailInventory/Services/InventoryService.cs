using RetailInventory.Models;

namespace RetailInventory.Services;

public class InventoryService
{
    private readonly PersistenceService _persistence = new();
    private InventoryData _data;

    public InventoryService()
    {
        _data = _persistence.Load();
    }

    public IReadOnlyList<Category> Categories => _data.Categories.AsReadOnly();
    public IReadOnlyList<Product> Products => _data.Products.AsReadOnly();
    public IReadOnlyList<StockTransaction> Transactions => _data.Transactions.AsReadOnly();
    public IReadOnlyList<SaleRule> SaleRules => _data.SaleRules.AsReadOnly();

    // Categories
    public void AddCategory(Category category)
    {
        _data.Categories.Add(category);
        Save();
    }

    public void UpdateCategory(Category category)
    {
        int idx = _data.Categories.FindIndex(c => c.Id == category.Id);
        if (idx >= 0) { _data.Categories[idx] = category; Save(); }
    }

    public void DeleteCategory(Guid id)
    {
        _data.Categories.RemoveAll(c => c.Id == id);
        Save();
    }

    // Products
    public void AddProduct(Product product)
    {
        _data.Products.Add(product);
        Save();
    }

    public void UpdateProduct(Product product)
    {
        int idx = _data.Products.FindIndex(p => p.Id == product.Id);
        if (idx >= 0) { _data.Products[idx] = product; Save(); }
    }

    public void DeleteProduct(Guid id)
    {
        _data.Products.RemoveAll(p => p.Id == id);
        Save();
    }

    public Category? GetCategory(Guid id) => _data.Categories.FirstOrDefault(c => c.Id == id);

    public List<Product> GetLowStockProducts() =>
        _data.Products.Where(p => p.IsActive && p.QuantityOnHand <= p.ReorderPoint).ToList();

    // Stock transactions
    public void RecordTransaction(StockTransaction tx)
    {
        var product = _data.Products.FirstOrDefault(p => p.Id == tx.ProductId)
            ?? throw new InvalidOperationException("Product not found.");

        product.QuantityOnHand += tx.Type switch
        {
            TransactionType.Receive => tx.Quantity,
            TransactionType.Sale => -tx.Quantity,
            TransactionType.Return => tx.Quantity,
            TransactionType.Adjustment => tx.Quantity,
            _ => 0
        };

        _data.Transactions.Add(tx);
        Save();
    }

    public List<StockTransaction> GetTransactionsForProduct(Guid productId) =>
        _data.Transactions.Where(t => t.ProductId == productId).OrderByDescending(t => t.Timestamp).ToList();

    // Sale rules
    public void AddSaleRule(SaleRule rule) { _data.SaleRules.Add(rule); Save(); }

    public void UpdateSaleRule(SaleRule rule)
    {
        int idx = _data.SaleRules.FindIndex(r => r.Id == rule.Id);
        if (idx >= 0) { _data.SaleRules[idx] = rule; Save(); }
    }

    public void DeleteSaleRule(Guid id) { _data.SaleRules.RemoveAll(r => r.Id == id); Save(); }

    // Returns effective sale price for a product (null = no active sale).
    // Product-specific rules take priority over category rules.
    public decimal? GetSalePrice(Product product)
    {
        var rule = _data.SaleRules.FirstOrDefault(r =>
                       r.IsActive && r.TargetType == SaleTargetType.Product && r.TargetId == product.Id)
                   ?? _data.SaleRules.FirstOrDefault(r =>
                       r.IsActive && r.TargetType == SaleTargetType.Category && r.TargetId == product.CategoryId);
        if (rule == null) return null;
        return rule.DiscountType == SaleDiscountType.FixedPrice
            ? rule.DiscountValue
            : Math.Round(product.Price * (1 - rule.DiscountValue / 100m), 2);
    }

    public void ForceSave() => _persistence.Save(_data);
    private void Save() => _persistence.Save(_data);

    public void Reload() => _data = _persistence.Load();
}
