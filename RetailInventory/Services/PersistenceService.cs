using System.Text.Json;
using RetailInventory.Models;

namespace RetailInventory.Services;

public class PersistenceService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RetailInventory");

    private static readonly string DataFile = Path.Combine(DataDir, "inventory.json");
    private static readonly string BackupFile = Path.Combine(DataDir, "inventory.bak");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public InventoryData Load()
    {
        if (!File.Exists(DataFile))
            return new InventoryData();

        try
        {
            string json = File.ReadAllText(DataFile);
            return JsonSerializer.Deserialize<InventoryData>(json, JsonOptions) ?? new InventoryData();
        }
        catch
        {
            if (File.Exists(BackupFile))
            {
                string json = File.ReadAllText(BackupFile);
                return JsonSerializer.Deserialize<InventoryData>(json, JsonOptions) ?? new InventoryData();
            }
            return new InventoryData();
        }
    }

    public void Save(InventoryData data)
    {
        Directory.CreateDirectory(DataDir);
        data.LastSaved = DateTime.Now;
        string json = JsonSerializer.Serialize(data, JsonOptions);
        string tempFile = DataFile + ".tmp";
        File.WriteAllText(tempFile, json);
        if (File.Exists(DataFile))
            File.Copy(DataFile, BackupFile, overwrite: true);
        File.Move(tempFile, DataFile, overwrite: true);
    }
}
