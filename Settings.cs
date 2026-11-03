using System.IO;
using System.Text.Json;

namespace PointerFinder;

sealed class Settings
{
    public bool Enabled { get; set; } = true;
    public int Scale { get; set; } = 4;               // size of the enlarged pointer relative to a normal one
    public double ShrinkSeconds { get; set; } = 2;    // how long the slow shrink takes once the mouse is still
    public int Sensitivity { get; set; } = 1;         // 0 low, 1 medium, 2 high

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PointerFinder", "settings.json");

        public static Settings Load() => TryLoad() ?? new Settings();

    /// <summary>Null when the file is missing or half-written (another program may be saving it at that moment).</summary>
    public static Settings? TryLoad()
    {
        try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)); }
        catch { return null; }
    }

    public void CopyFrom(Settings other)
    {
        Enabled = other.Enabled;
        Scale = other.Scale;
        ShrinkSeconds = other.ShrinkSeconds;
        Sensitivity = other.Sensitivity;
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
