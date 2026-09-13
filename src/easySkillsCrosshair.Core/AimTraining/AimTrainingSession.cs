namespace easySkillsCrosshair.Core.AimTraining;

public enum AimTrainingMode
{
    /// <summary>One target at a time; hit it as fast as possible.</summary>
    Flick,
    /// <summary>Several small targets that expire if not hit in time.</summary>
    Precision,
    /// <summary>One moving target; keep the mouse button held while on it.</summary>
    Tracking,
}

public sealed record AimTarget(int Id, double X, double Y, double Radius, DateTime SpawnedUtc);

public readonly record struct AimSessionStats(
    int Hits,
    int Misses,
    int Expired,
    int Score,
    double AverageReactionMs,
    double TrackingOnTargetRatio)
{
    public int Shots => Hits + Misses;
    public double Accuracy => Shots == 0 ? 0 : (double)Hits / Shots;
}

/// <summary>
/// Pure, clock-driven aim-training game logic (no UI types), so scoring and timing are
/// unit-testable. The view feeds it clicks/pointer state and calls <see cref="Tick"/> per frame.
/// </summary>
public sealed class AimTrainingSession
{
    private const double FlickRadius = 22;
    private const double PrecisionRadius = 11;
    private const int PrecisionConcurrentTargets = 3;
    private const double TrackingRadius = 28;
    private const double TrackingSpeed = 260; // DIP per second
    private static readonly TimeSpan PrecisionTargetLifetime = TimeSpan.FromMilliseconds(1600);
    private static readonly TimeSpan TrackingDirectionChange = TimeSpan.FromMilliseconds(900);

    private readonly Random _random;
    private readonly List<AimTarget> _targets = [];
    private readonly List<double> _reactionTimesMs = [];

    private double _width;
    private double _height;
    private DateTime _startedUtc;
    private DateTime _lastTickUtc;
    private int _nextId;
    private int _hits;
    private int _misses;
    private int _expired;
    private int _score;

    private double _velocityX;
    private double _velocityY;
    private DateTime _nextDirectionChangeUtc;
    private double _trackingHeldSeconds;
    private double _trackingOnTargetSeconds;

    public AimTrainingSession(AimTrainingMode mode, TimeSpan duration, int? seed = null)
    {
        Mode = mode;
        Duration = duration;
        _random = seed is { } s ? new Random(s) : new Random();
    }

    public AimTrainingMode Mode { get; }
    public TimeSpan Duration { get; }
    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }
    public IReadOnlyList<AimTarget> Targets => _targets;

    public TimeSpan Remaining(DateTime nowUtc) =>
        !IsRunning && !IsFinished ? Duration
        : IsFinished ? TimeSpan.Zero
        : TimeSpan.FromTicks(Math.Max(0, (Duration - (nowUtc - _startedUtc)).Ticks));

    public AimSessionStats Stats => new(
        _hits,
        _misses,
        _expired,
        _score,
        _reactionTimesMs.Count == 0 ? 0 : _reactionTimesMs.Average(),
        _trackingHeldSeconds <= 0 ? 0 : _trackingOnTargetSeconds / _trackingHeldSeconds);

    public void Start(double arenaWidth, double arenaHeight, DateTime nowUtc)
    {
        _width = Math.Max(120, arenaWidth);
        _height = Math.Max(120, arenaHeight);
        _startedUtc = nowUtc;
        _lastTickUtc = nowUtc;
        IsRunning = true;
        IsFinished = false;

        switch (Mode)
        {
            case AimTrainingMode.Flick:
                Spawn(FlickRadius, nowUtc);
                break;
            case AimTrainingMode.Precision:
                for (var i = 0; i < PrecisionConcurrentTargets; i++) Spawn(PrecisionRadius, nowUtc);
                break;
            case AimTrainingMode.Tracking:
                _targets.Add(new AimTarget(_nextId++, _width / 2, _height / 2, TrackingRadius, nowUtc));
                PickDirection(nowUtc);
                break;
        }
    }

    /// <summary>Keeps the playfield in sync if the window is resized mid-session.</summary>
    public void Resize(double arenaWidth, double arenaHeight)
    {
        _width = Math.Max(120, arenaWidth);
        _height = Math.Max(120, arenaHeight);
    }

    public void Tick(DateTime nowUtc, bool isTrackingHeld, double pointerX, double pointerY)
    {
        if (!IsRunning) return;

        var dt = Math.Clamp((nowUtc - _lastTickUtc).TotalSeconds, 0, 0.1);
        _lastTickUtc = nowUtc;

        if (nowUtc - _startedUtc >= Duration)
        {
            IsRunning = false;
            IsFinished = true;
            _targets.Clear();
            return;
        }

        if (Mode == AimTrainingMode.Precision)
        {
            for (var i = _targets.Count - 1; i >= 0; i--)
            {
                if (nowUtc - _targets[i].SpawnedUtc >= PrecisionTargetLifetime)
                {
                    _targets.RemoveAt(i);
                    _expired++;
                    Spawn(PrecisionRadius, nowUtc);
                }
            }
        }
        else if (Mode == AimTrainingMode.Tracking && _targets.Count == 1)
        {
            MoveTrackingTarget(dt, nowUtc);

            if (isTrackingHeld)
            {
                _trackingHeldSeconds += dt;
                if (IsInside(_targets[0], pointerX, pointerY))
                {
                    _trackingOnTargetSeconds += dt;
                    _score = (int)Math.Round(_trackingOnTargetSeconds * 100);
                }
            }
        }
    }

    /// <returns>True if a target was hit.</returns>
    public bool Shoot(double x, double y, DateTime nowUtc)
    {
        if (!IsRunning || Mode == AimTrainingMode.Tracking) return false;

        var target = _targets.FirstOrDefault(t => IsInside(t, x, y));
        if (target is null)
        {
            _misses++;
            _score = Math.Max(0, _score - 25);
            return false;
        }

        _targets.Remove(target);
        _hits++;

        var reactionMs = (nowUtc - target.SpawnedUtc).TotalMilliseconds;
        _reactionTimesMs.Add(reactionMs);
        _score += 100 + (int)Math.Max(0, 100 - reactionMs / 10);

        Spawn(Mode == AimTrainingMode.Flick ? FlickRadius : PrecisionRadius, nowUtc);
        return true;
    }

    public void Stop()
    {
        IsRunning = false;
        IsFinished = true;
        _targets.Clear();
    }

    private void Spawn(double radius, DateTime nowUtc)
    {
        var margin = radius + 8;
        var x = margin + _random.NextDouble() * Math.Max(1, _width - margin * 2);
        var y = margin + _random.NextDouble() * Math.Max(1, _height - margin * 2);
        _targets.Add(new AimTarget(_nextId++, x, y, radius, nowUtc));
    }

    private void MoveTrackingTarget(double dt, DateTime nowUtc)
    {
        if (nowUtc >= _nextDirectionChangeUtc) PickDirection(nowUtc);

        var t = _targets[0];
        var x = t.X + _velocityX * dt;
        var y = t.Y + _velocityY * dt;

        if (x < t.Radius || x > _width - t.Radius) { _velocityX = -_velocityX; x = Math.Clamp(x, t.Radius, _width - t.Radius); }
        if (y < t.Radius || y > _height - t.Radius) { _velocityY = -_velocityY; y = Math.Clamp(y, t.Radius, _height - t.Radius); }

        _targets[0] = t with { X = x, Y = y };
    }

    private void PickDirection(DateTime nowUtc)
    {
        var angle = _random.NextDouble() * Math.PI * 2;
        _velocityX = Math.Cos(angle) * TrackingSpeed;
        _velocityY = Math.Sin(angle) * TrackingSpeed;
        _nextDirectionChangeUtc = nowUtc + TrackingDirectionChange;
    }

    private static bool IsInside(AimTarget target, double x, double y)
    {
        var dx = x - target.X;
        var dy = y - target.Y;
        return dx * dx + dy * dy <= target.Radius * target.Radius;
    }
}
