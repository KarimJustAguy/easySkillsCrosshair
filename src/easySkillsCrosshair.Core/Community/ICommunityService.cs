namespace easySkillsCrosshair.Core.Community;

/// <summary>
/// Abstracts the community/sharing backend. Today only <see cref="PlaceholderCommunityService"/>
/// exists (static sample data, no network); a future server-backed implementation drops in here
/// without the Community view or its ViewModel changing.
/// </summary>
public interface ICommunityService
{
    Task<IReadOnlyList<CommunityCrosshairListing>> GetFeaturedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommunityCrosshairListing>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
