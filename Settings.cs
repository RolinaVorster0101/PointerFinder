using System.IO;
using System.Text.Json;

namespace PointerFinder;

sealed class Settings
{
    public bool Enabled { get; set; } = true;
    public int Scale { get; set; } = 4;               // size of the enlarged pointer relative to a normal one
    public double ShrinkSeconds { get; set; } = 2;    // how long the slow shrink takes once the mouse is still
    public int Sensitivity { get; set; } = 1;         // 0 low, 1 medium, 2 high

    static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PointerFinder", "settings.json");

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
