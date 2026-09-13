namespace easySkillsCrosshair.Core.Licensing;

/// <summary>Pro-gated capabilities that aren't expressible as a simple "allowed value" check.</summary>
public enum Feature
{
    MultipleLayers,
    DynamicReactions,
    CustomMediaUpload,
    FullAimTrainer,
    CommunitySharing,
    /// <summary>Sensitivity converter: games beyond the Trial set in games.json.</summary>
    ExtendedGameLibrary,
}
