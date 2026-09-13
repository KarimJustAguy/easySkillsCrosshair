using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.Core.Community;

/// <summary>
/// Abstracts community sharing. Today: <see cref="LocalCommunityService"/> (built-in presets,
/// share codes, share files — no server). A Steam Workshop implementation can replace it later
/// without the Community view or its ViewModel changing.
/// </summary>
public interface ICommunityService
{
    Task<IReadOnlyList<CommunityCrosshairListing>> SearchAsync(string? query, CancellationToken cancellationToken = default);

    string CreateShareCode(CrosshairProfile profile);

    /// <exception cref="FormatException">The code is invalid or corrupted.</exception>
    Task<CommunityCrosshairListing> ImportShareCodeAsync(string code, CancellationToken cancellationToken = default);

    Task ExportFileAsync(CrosshairProfile profile, string path, CancellationToken cancellationToken = default);

    /// <exception cref="FormatException">The file is not a valid crosshair file.</exception>
    Task<CommunityCrosshairListing> ImportFileAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Removes a previously imported crosshair. Built-in presets can't be removed.</summary>
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);
}
