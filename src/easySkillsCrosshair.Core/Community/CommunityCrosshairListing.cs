using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.Core.Community;

/// <summary>A community-shared crosshair as shown in the browse/discover grid.</summary>
public sealed record CommunityCrosshairListing(
    string Id,
    string Name,
    string AuthorName,
    int Likes,
    CrosshairProfile Profile);
