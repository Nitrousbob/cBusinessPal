using System.Text.Json;
using RetailInventory.Models;

namespace RetailInventory.Services;

public class AppSettingsService
{
    private static AppSettingsService? _instance;
    public static AppSettingsService Instance => _instance ??= new AppSettingsService();

    private static readonly string DataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "RetailInventory");
    private static readonly string SettingsFile = Path.Combine(DataDir, "settings.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppSettings Current { get; private set; } = new();

    private AppSettingsService() { Load(); }

    public void Load()
    {
        if (!File.Exists(SettingsFile)) { Current = new AppSettings(); return; }
        try
        {
            string json = File.ReadAllText(SettingsFile);
            Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch { Current = new AppSettings(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(DataDir);
        string json = JsonSerializer.Serialize(Current, JsonOptions);
        string tmp = SettingsFile + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, SettingsFile, overwrite: true);
    }
}
