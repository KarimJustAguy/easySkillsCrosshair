using System.Text.Json;

namespace easySkillsCrosshair.Core.Sensitivity;

/// <summary>The converter's last inputs, restored on the next start.</summary>
public sealed class SensitivityConverterState
{
    public string FromGameId { get; set; } = "valorant";
    public string ToGameId { get; set; } = "counter-strike-2";
    public string Sensitivity { get; set; } = "0.35";
    public string DpiFrom { get; set; } = "800";
    public string DpiTo { get; set; } = "800";
    public string CustomYawFrom { get; set; } = "0.022";
    public string CustomYawTo { get; set; } = "0.022";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Missing or corrupt state is never an error — the defaults are used.</summary>
    public static SensitivityConverterState Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<SensitivityConverterState>(File.ReadAllText(path), JsonOptions) ?? new();
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
        }

        return new SensitivityConverterState();
    }

    /// <summary>Best effort: failing to remember the last inputs must not interrupt the user.</summary>
    public void Save(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
