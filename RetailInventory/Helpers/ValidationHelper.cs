namespace RetailInventory.Helpers;

public static class ValidationHelper
{
    public static bool IsValidSKU(string sku) =>
        !string.IsNullOrWhiteSpace(sku) && sku.Length <= 50;

    public static bool IsValidPrice(string input, out decimal value) =>
        decimal.TryParse(input, out value) && value >= 0;

    public static bool IsValidQuantity(string input, out int value) =>
        int.TryParse(input, out value) && value >= 0;
}
