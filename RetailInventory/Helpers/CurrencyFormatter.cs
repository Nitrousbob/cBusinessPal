namespace RetailInventory.Helpers;

public static class CurrencyFormatter
{
    public static string Format(decimal amount) => amount.ToString("C2");
    public static string FormatPlain(decimal amount) => amount.ToString("F2");
}
