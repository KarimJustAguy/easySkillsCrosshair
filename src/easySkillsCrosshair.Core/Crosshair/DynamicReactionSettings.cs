namespace easySkillsCrosshair.Core.Crosshair;

/// <summary>Pro-only behaviour: bloom on fire, hide on ADS, T-shape while moving.</summary>
public sealed class DynamicReactionSettings
{
    public bool BloomOnFire { get; set; }
    /// <summary>Extra gap (px) added to Cross/XCross layers at full bloom.</summary>
    public double BloomAmount { get; set; } = 6;
    public TimeSpan BloomRecoverTime { get; set; } = TimeSpan.FromMilliseconds(180);

    public bool HideOnAim { get; set; }
    public bool TShapeOnMove { get; set; }

    public bool IsActive => BloomOnFire || HideOnAim || TShapeOnMove;

    public DynamicReactionSettings Clone() => (DynamicReactionSettings)MemberwiseClone();
}
