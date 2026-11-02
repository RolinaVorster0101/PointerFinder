using System.IO;
using System.Text.Json;

namespace PointerFinder;

sealed class Settings
{
    public bool Enabled { get; set; } = true;
    public int Scale { get; set; } = 4;               // size of the enlarged pointer relative to a normal one
    public double HoldSeconds { get; set; } = 2;      // how long it stays enlarged after the last shake
    public int Sensitivity { get; set; } = 1;         // 0 low, 1 medium, 2 high

    static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RosewoodPointerFinder", "settings.json");

    public static Settings Load()
    {
        try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings(); }
        catch { return new Settings(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* settings are a convenience; never crash over them */ }
    }
}
