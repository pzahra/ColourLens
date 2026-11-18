using System.Text.Json;
using System.Text.Json.Serialization;

namespace ColourLens;

internal sealed class Settings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ColourLens", "settings.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public Deficiency Mode { get; set; } = Deficiency.Deutan;
    public float Severity { get; set; } = 1f;

    /// <summary>x, y, width, height of the frame in physical pixels.</summary>
    public int[]? Bounds { get; set; }

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath), Json) ?? new();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
        }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>The saved bounds, or a centred default when missing or no longer on any monitor.</summary>
    public Rectangle GetBounds()
    {
        if (Bounds is [var x, var y, var w, var h] && w > 0 && h > 0)
        {
            var titleBar = new Rectangle(x, y, w, Math.Min(h, 30));
            if (Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(titleBar)))
                return new Rectangle(x, y, w, h);
        }

        var area = Screen.PrimaryScreen!.WorkingArea;
        int width = area.Width * 2 / 5, height = area.Height / 2;
        return new Rectangle(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2, width, height);
    }

    public void SetBounds(Rectangle r) => Bounds = [r.X, r.Y, r.Width, r.Height];
}
