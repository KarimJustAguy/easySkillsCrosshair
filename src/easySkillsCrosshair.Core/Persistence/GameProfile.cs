using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.Core.Persistence;

/// <summary>A saved, per-game crosshair configuration ("Per-Game-Profile").</summary>
public sealed class GameProfile
{
    public required string Id { get; init; }
    public required string GameName { get; set; }
    public required CrosshairProfile Crosshair { get; set; }
    public DateTimeOffset LastModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
}
