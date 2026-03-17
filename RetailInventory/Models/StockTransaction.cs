namespace RetailInventory.Models;

public enum TransactionType
{
    Receive,
    Sale,
    Adjustment,
    Return
}

public class StockTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public TransactionType Type { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
