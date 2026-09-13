namespace easySkillsCrosshair.Core.Reactions;

/// <summary>Transient, input-driven modifiers applied on top of a profile at render time.</summary>
public readonly record struct CrosshairRenderState(double BloomFactor, bool IsHidden, bool ForceTStyle)
{
    public static readonly CrosshairRenderState Idle = new(0, false, false);
}
