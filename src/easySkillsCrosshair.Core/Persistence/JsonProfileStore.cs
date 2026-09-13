using System.Text.Json;

namespace easySkillsCrosshair.Core.Persistence;

/// <summary>
/// Plain JSON-file persistence under %AppData%\easySkills\Crosshair. No framework dependency,
/// so it lives directly in Core rather than behind a separate implementation project — unlike
/// licensing/overlay, there is no second implementation to swap in later.
/// </summary>
public sealed class JsonProfileStore : IProfileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public JsonProfileStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "easySkills", "Crosshair", "profiles.json");
    }

    public IReadOnlyList<GameProfile> LoadAll()
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<GameProfile>();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<GameProfile>>(json, SerializerOptions) ?? [];
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // A hand-edited or truncated profiles.json must never take the app down on startup;
            // starting with an empty list is always recoverable (the user can re-save profiles).
            return Array.Empty<GameProfile>();
        }
    }

    public void SaveAll(IReadOnlyList<GameProfile> profiles)
    {
        var directory = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(profiles, SerializerOptions));
    }
}
