using easySkillsCrosshair.Core.AimTraining;
using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Licensing;
using easySkillsCrosshair.Core.Mvvm;

namespace easySkillsCrosshair.App.ViewModels;

public sealed record AimModeOption(AimTrainingMode Mode, string DisplayName, string Description);

/// <summary>
/// Drives the 2D shooting range. The view forwards pointer input and calls <see cref="Tick"/>
/// once per rendered frame while a session runs; all game rules live in
/// <see cref="AimTrainingSession"/>. Trial = 30-second Flick preview; Pro = every mode, custom minutes.
/// </summary>
public sealed class AimTrainingViewModel : ViewModelBase
{
    private static readonly TimeSpan TrialPreviewDuration = TimeSpan.FromSeconds(30);

    private readonly IFeatureGate _featureGate;
    private readonly CrosshairEditorViewModel _editor;

    private AimModeOption _selectedMode;
    private double _durationMinutes = 1;
    private AimTrainingSession? _session;

    public AimTrainingViewModel(IFeatureGate featureGate, CrosshairEditorViewModel editor)
    {
        _featureGate = featureGate;
        _editor = editor;

        Modes =
        [
            new(AimTrainingMode.Flick, "Flick", "Ein Ziel nach dem anderen — so schnell wie möglich treffen."),
            new(AimTrainingMode.Precision, "Präzision", "Kleine Ziele, die nach kurzer Zeit verschwinden."),
            new(AimTrainingMode.Tracking, "Tracking", "Bewegtes Ziel verfolgen — linke Maustaste gedrückt halten."),
        ];
        _selectedMode = Modes[0];

        StartCommand = new RelayCommand(_ => SessionStartRequested?.Invoke(this, EventArgs.Empty), _ => !IsRunning);
        StopCommand = new RelayCommand(_ => Stop(), _ => IsRunning);
        SelectModeCommand = new RelayCommand(p =>
        {
            if (p is AimModeOption option && !IsRunning) SelectedMode = option;
        });

        _featureGate.Changed += (_, _) =>
        {
            if (IsTrialPreview && _selectedMode.Mode != AimTrainingMode.Flick) SelectedMode = Modes[0];
            OnPropertyChanged(null);
        };
    }

    /// <summary>Raised so the view can supply the arena size and start the frame loop.</summary>
    public event EventHandler? SessionStartRequested;

    /// <summary>Raised whenever the arena needs to be redrawn.</summary>
    public event EventHandler? FrameUpdated;

    public IReadOnlyList<AimModeOption> Modes { get; }
    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand SelectModeCommand { get; }

    public bool IsFullAccess => _featureGate.IsUnlocked(Feature.FullAimTrainer);
    public bool IsTrialPreview => !IsFullAccess;
    public string AccessLevelText => IsFullAccess
        ? "Pro — alle Modi, freie Dauer"
        : "Trial-Vorschau — 30 Sekunden, nur Flick";

    public AimModeOption SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (IsTrialPreview && value.Mode != AimTrainingMode.Flick) return;
            SetField(ref _selectedMode, value);
        }
    }

    public double DurationMinutes
    {
        get => _durationMinutes;
        set
        {
            if (SetField(ref _durationMinutes, Math.Round(Math.Clamp(value, 0.5, 30) * 2) / 2))
            {
                OnPropertyChanged(nameof(RemainingText));
            }
        }
    }

    public CrosshairProfile CursorProfile => _editor.Profile;
    public AimTrainingSession? Session => _session;
    public bool IsRunning => _session?.IsRunning == true;
    public bool IsIdle => !IsRunning;
    public bool HasResult => _session?.IsFinished == true;

    public string RemainingText
    {
        get
        {
            var remaining = _session?.Remaining(DateTime.UtcNow) ?? EffectiveDuration;
            return $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
        }
    }

    public int Hits => _session?.Stats.Hits ?? 0;
    public int Score => _session?.Stats.Score ?? 0;

    public string AccuracyText => _session is null
        ? "–"
        : _session.Mode == AimTrainingMode.Tracking
            ? $"{_session.Stats.TrackingOnTargetRatio:P0}"
            : $"{_session.Stats.Accuracy:P0}";

    public string ReactionText => _session is null || _session.Stats.AverageReactionMs <= 0
        ? "–"
        : $"{_session.Stats.AverageReactionMs:0} ms";

    public string ResultSummary => _session is not { IsFinished: true } s
        ? ""
        : s.Mode == AimTrainingMode.Tracking
            ? $"Ergebnis: {s.Stats.Score} Punkte · {s.Stats.TrackingOnTargetRatio:P0} auf dem Ziel"
            : $"Ergebnis: {s.Stats.Score} Punkte · {s.Stats.Hits} Treffer · {s.Stats.Accuracy:P0} Präzision · Ø {s.Stats.AverageReactionMs:0} ms"
              + (s.Stats.Expired > 0 ? $" · {s.Stats.Expired} verpasst" : "");

    private TimeSpan EffectiveDuration => IsTrialPreview ? TrialPreviewDuration : TimeSpan.FromMinutes(_durationMinutes);

    public void Start(double arenaWidth, double arenaHeight)
    {
        var mode = IsTrialPreview ? AimTrainingMode.Flick : _selectedMode.Mode;
        _session = new AimTrainingSession(mode, EffectiveDuration);
        _session.Start(arenaWidth, arenaHeight, DateTime.UtcNow);
        NotifyAll();
    }

    public void Tick(bool isPrimaryHeld, double pointerX, double pointerY)
    {
        if (_session is null) return;

        var wasRunning = _session.IsRunning;
        _session.Tick(DateTime.UtcNow, isPrimaryHeld, pointerX, pointerY);

        if (wasRunning != _session.IsRunning)
        {
            NotifyAll();
        }
        else
        {
            OnPropertyChanged(nameof(RemainingText));
            if (_session.Mode == AimTrainingMode.Tracking)
            {
                OnPropertyChanged(nameof(Score));
                OnPropertyChanged(nameof(AccuracyText));
            }
        }

        FrameUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void Shoot(double x, double y)
    {
        if (_session is not { IsRunning: true }) return;

        _session.Shoot(x, y, DateTime.UtcNow);
        OnPropertyChanged(nameof(Hits));
        OnPropertyChanged(nameof(Score));
        OnPropertyChanged(nameof(AccuracyText));
        OnPropertyChanged(nameof(ReactionText));
        FrameUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void Resize(double width, double height) => _session?.Resize(width, height);

    public void Stop()
    {
        _session?.Stop();
        NotifyAll();
    }

    private void NotifyAll()
    {
        OnPropertyChanged(null);
        StartCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
        FrameUpdated?.Invoke(this, EventArgs.Empty);
    }
}
