using System.Text.Json;
using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.Core.Community;

/// <summary>
/// Offline community backend: built-in presets plus a persisted library of crosshairs imported
/// via share code or share file. No network access; a Steam Workshop service can replace this
/// behind <see cref="ICommunityService"/> later.
/// </summary>
public sealed class LocalCommunityService : ICommunityService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly CrosshairShareCodec _codec;
    private readonly string _libraryPath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<LibraryEntry>? _library;

    public LocalCommunityService(CrosshairShareCodec codec, string libraryPath)
    {
        _codec = codec;
        _libraryPath = libraryPath;
    }

    public async Task<IReadOnlyList<CommunityCrosshairListing>> SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        var library = await LoadLibraryAsync(cancellationToken);

        var all = library
            .OrderByDescending(e => e.ImportedAtUtc)
            .Select(ToListing)
            .Concat(CrosshairPresets.All);

        if (!string.IsNullOrWhiteSpace(query))
        {
            all = all.Where(l =>
                l.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || l.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return all.ToArray();
    }

    public string CreateShareCode(CrosshairProfile profile) => _codec.EncodeShareCode(profile);

    public async Task<CommunityCrosshairListing> ImportShareCodeAsync(string code, CancellationToken cancellationToken = default) =>
        await AddAsync(_codec.DecodeShareCode(code), cancellationToken);

    public Task ExportFileAsync(CrosshairProfile profile, string path, CancellationToken cancellationToken = default) =>
        _codec.ExportFileAsync(profile, path, cancellationToken);

    public async Task<CommunityCrosshairListing> ImportFileAsync(string path, CancellationToken cancellationToken = default) =>
        await AddAsync(await _codec.ImportFileAsync(path, cancellationToken), cancellationToken);

    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var library = await LoadLibraryCoreAsync(cancellationToken);
            if (library.RemoveAll(e => e.Id == id) > 0)
            {
                await SaveLibraryCoreAsync(library, cancellationToken);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<CommunityCrosshairListing> AddAsync(CrosshairProfile profile, CancellationToken cancellationToken)
    {
        var entry = new LibraryEntry(Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, profile);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var library = await LoadLibraryCoreAsync(cancellationToken);
            library.Add(entry);
            await SaveLibraryCoreAsync(library, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }

        return ToListing(entry);
    }

    private async Task<List<LibraryEntry>> LoadLibraryAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return [.. await LoadLibraryCoreAsync(cancellationToken)];
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<LibraryEntry>> LoadLibraryCoreAsync(CancellationToken cancellationToken)
    {
        if (_library is not null) return _library;

        _library = [];
        if (!File.Exists(_libraryPath)) return _library;

        try
        {
            await using var stream = File.OpenRead(_libraryPath);
            var loaded = await JsonSerializer.DeserializeAsync<List<LibraryEntry>>(stream, JsonOptions, cancellationToken);
            _library = loaded?.Where(e => e?.Profile is not null).ToList() ?? [];
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // A corrupt library must not break the Community view; start empty.
        }

        return _library;
    }

    private async Task SaveLibraryCoreAsync(List<LibraryEntry> library, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_libraryPath)!);
        await using var stream = File.Create(_libraryPath);
        await JsonSerializer.SerializeAsync(stream, library, JsonOptions, cancellationToken);
    }

    private static CommunityCrosshairListing ToListing(LibraryEntry entry) =>
        new(entry.Id, entry.Profile.Name, "Importiert", $"Importiert am {entry.ImportedAtUtc.ToLocalTime():dd.MM.yyyy}", entry.Profile, IsImported: true);

    private sealed record LibraryEntry(string Id, DateTimeOffset ImportedAtUtc, CrosshairProfile Profile);
}
