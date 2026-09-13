using easySkillsCrosshair.Core.AimTraining;
using Xunit;

namespace easySkillsCrosshair.Licensing.Tests;

public class AimTrainingSessionTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Flick_Hit_Scores_And_Respawns_Miss_Penalises()
    {
        var session = new AimTrainingSession(AimTrainingMode.Flick, TimeSpan.FromMinutes(1), seed: 1);
        session.Start(800, 600, T0);
        var target = session.Targets.Single();

        Assert.True(session.Shoot(target.X, target.Y, T0.AddMilliseconds(300)));
        Assert.Single(session.Targets);
        Assert.NotEqual(target.Id, session.Targets[0].Id);

        Assert.False(session.Shoot(-50, -50, T0.AddMilliseconds(400)));

        var stats = session.Stats;
        Assert.Equal(1, stats.Hits);
        Assert.Equal(1, stats.Misses);
        Assert.Equal(0.5, stats.Accuracy);
        Assert.Equal(300, stats.AverageReactionMs, 1);
        Assert.Equal(100 + 70 - 25, stats.Score);
    }

    [Fact]
    public void Session_Finishes_After_Duration()
    {
        var session = new AimTrainingSession(AimTrainingMode.Flick, TimeSpan.FromSeconds(30), seed: 1);
        session.Start(800, 600, T0);

        session.Tick(T0.AddSeconds(31), false, 0, 0);

        Assert.True(session.IsFinished);
        Assert.False(session.IsRunning);
        Assert.Empty(session.Targets);
        Assert.Equal(TimeSpan.Zero, session.Remaining(T0.AddSeconds(31)));
    }

    [Fact]
    public void Precision_Targets_Expire()
    {
        var session = new AimTrainingSession(AimTrainingMode.Precision, TimeSpan.FromMinutes(1), seed: 2);
        session.Start(800, 600, T0);

        session.Tick(T0.AddMilliseconds(50), false, 0, 0);
        session.Tick(T0.AddSeconds(2), false, 0, 0);

        Assert.Equal(3, session.Stats.Expired);
        Assert.Equal(3, session.Targets.Count);
    }

    [Fact]
    public void Tracking_Accumulates_Time_On_Target_Only_While_Held()
    {
        var session = new AimTrainingSession(AimTrainingMode.Tracking, TimeSpan.FromMinutes(1), seed: 3);
        session.Start(800, 600, T0);

        var now = T0;
        for (var i = 0; i < 10; i++)
        {
            now = now.AddMilliseconds(50);
            var t = session.Targets[0];
            session.Tick(now, isTrackingHeld: true, t.X, t.Y);
        }

        Assert.True(session.Stats.TrackingOnTargetRatio > 0.8);
        Assert.True(session.Stats.Score > 0);
    }
}
