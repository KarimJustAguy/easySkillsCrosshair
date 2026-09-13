using System.Text.Json;
using System.Text.Json.Serialization;

namespace easySkillsCrosshair.Core.Sensitivity;

/// <summary>
/// Loads the game list from games.json. The editable copy next to the executable is preferred
/// (so values can be corrected after game patches without a rebuild); if it is missing or invalid,
/// the copy embedded in this assembly is used and a warning is reported instead of failing.
/// </summary>
public static class GameCatalog
{
    public const string EmbeddedResourceName = "easySkillsCrosshair.Core.Sensitivity.games.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public sealed record LoadResult(IReadOnlyList<GameSensitivityProfile> Games, string? Warning);

    public static LoadResult Load(string? editableFilePath)
    {
        if (editableFilePath is not null && File.Exists(editableFilePath))
        {
            try
            {
                using var stream = File.OpenRead(editableFilePath);
                return new LoadResult(Parse(stream), null);
            }
            catch (Exception ex) when (ex is FormatException or IOException or UnauthorizedAccessException)
            {
                return new LoadResult(LoadEmbedded(), $"games.json konnte nicht gelesen werden ({ex.Message}) — Standardwerte werden verwendet.");
            }
        }

        return new LoadResult(LoadEmbedded(), null);
    }

    public static IReadOnlyList<GameSensitivityProfile> LoadEmbedded()
    {
        using var stream = typeof(GameCatalog).Assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{EmbeddedResourceName}' is missing.");
        return Parse(stream);
    }

    /// <exception cref="FormatException">Malformed JSON or invalid game entries.</exception>
    public static IReadOnlyList<GameSensitivityProfile> Parse(Stream json)
    {
        CatalogFile? file;
        try
        {
            file = JsonSerializer.Deserialize<CatalogFile>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Ungültiges JSON: {ex.Message}", ex);
        }

        if (file?.Games is not { Count: > 0 } entries)
        {
            throw new FormatException("Die Datei enthält keine Spiele.");
        }

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var games = new List<GameSensitivityProfile>(entries.Count);

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.Name))
                throw new FormatException("Jedes Spiel braucht \"id\" und \"name\".");
            if (entry.Id == GameSensitivityProfile.CustomId)
                throw new FormatException($"Die id \"{GameSensitivityProfile.CustomId}\" ist reserviert.");
            if (!ids.Add(entry.Id))
                throw new FormatException($"Doppelte id \"{entry.Id}\".");
            if (entry.Yaw is not > 0 || !double.IsFinite(entry.Yaw.Value))
                throw new FormatException($"\"{entry.Name}\": yaw muss eine positive Zahl sein.");

            // A missing tier counts as Pro: entries added by hand never widen the Trial by accident.
            var isProOnly = (entry.Tier?.Trim().ToLowerInvariant()) switch
            {
                "trial" => false,
                "pro" or null or "" => true,
                _ => throw new FormatException($"\"{entry.Name}\": tier muss \"trial\" oder \"pro\" sein."),
            };

            games.Add(new GameSensitivityProfile(
                entry.Id.Trim(),
                entry.Name.Trim(),
                entry.Yaw.Value,
                Math.Clamp(entry.Decimals ?? 2, 0, 6),
                entry.Note?.Trim() ?? "",
                isProOnly,
                entry.Verified ?? false));
        }

        return games;
    }

    private sealed class CatalogFile
    {
        [JsonPropertyName("games")]
        public List<CatalogEntry>? Games { get; set; }
    }

    private sealed class CatalogEntry
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public double? Yaw { get; set; }
        public int? Decimals { get; set; }
        public string? Note { get; set; }
        public string? Tier { get; set; }
        public bool? Verified { get; set; }
    }
}
