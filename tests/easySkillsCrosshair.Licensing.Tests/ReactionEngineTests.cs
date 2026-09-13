using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Reactions;
using Xunit;

namespace easySkillsCrosshair.Licensing.Tests;

public class ReactionEngineTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static DynamicReactionSettings AllOn() => new()
    {
        BloomOnFire = true,
        BloomRecoverTime = TimeSpan.FromMilliseconds(200),
        HideOnAim = true,
        TShapeOnMove = true,
    };

    [Fact]
    public void Bloom_Is_Full_While_Firing_And_Decays_After_Release()
    {
        var engine = new ReactionEngine();
        var settings = AllOn();

        engine.SetFiring(true, T0);
        Assert.Equal(1, engine.ComputeState(settings, T0.AddSeconds(5)).BloomFactor);

        engine.SetFiring(false, T0);
        Assert.Equal(0.5, engine.ComputeState(settings, T0.AddMilliseconds(100)).BloomFactor, 3);
        Assert.Equal(0, engine.ComputeState(settings, T0.AddMilliseconds(250)).BloomFactor);
    }

    [Fact]
    public void Reactions_Are_Inert_When_Disabled()
    {
        var engine = new ReactionEngine();
        engine.SetFiring(true, T0);
        engine.SetAiming(true);
        engine.SetMoveKey(0x57, true);

        Assert.Equal(CrosshairRenderState.Idle, engine.ComputeState(new DynamicReactionSettings(), T0));
    }

    [Fact]
    public void Aim_Hides_And_Movement_Forces_TStyle()
    {
        var engine = new ReactionEngine();
        var settings = AllOn();

        engine.SetAiming(true);
        engine.SetMoveKey(0x41, true);
        var state = engine.ComputeState(settings, T0);
        Assert.True(state.IsHidden);
        Assert.True(state.ForceTStyle);

        engine.SetAiming(false);
        engine.SetMoveKey(0x41, false);
        state = engine.ComputeState(settings, T0);
        Assert.False(state.IsHidden);
        Assert.False(state.ForceTStyle);
    }

    [Fact]
    public void Movement_Stays_Active_Until_All_Keys_Released()
    {
        var engine = new ReactionEngine();
        var settings = AllOn();

        engine.SetMoveKey(0x57, true);
        engine.SetMoveKey(0x44, true);
        engine.SetMoveKey(0x57, false);

        Assert.True(engine.ComputeState(settings, T0).ForceTStyle);
    }

    [Fact]
    public void Reset_When_Idle_Raises_No_Event()
    {
        var engine = new ReactionEngine();
        var raised = 0;
        engine.InputChanged += (_, _) => raised++;

        engine.Reset();

        Assert.Equal(0, raised);
    }
}
