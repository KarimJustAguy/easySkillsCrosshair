using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.Core.Community;

/// <summary>A browsable crosshair in the Community view — a built-in preset or a local import.</summary>
public sealed record CommunityCrosshairListing(
    string Id,
    string Name,
    string AuthorName,
    string Description,
    CrosshairProfile Profile,
    bool IsImported);
