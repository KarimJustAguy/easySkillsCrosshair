using System.Globalization;
using System.Runtime.InteropServices;
using easySkillsCrosshair.Core.Licensing;
using easySkillsCrosshair.Core.Mvvm;
using easySkillsCrosshair.Core.Sensitivity;

namespace easySkillsCrosshair.App.ViewModels;

/// <summary>
/// Game-to-game sensitivity converter (360°-distance method). Trial: the core game set plus a
/// custom factor. Pro: the extended library (<see cref="Feature.ExtendedGameLibrary"/>); those
/// games are shown locked in Trial rather than hidden.
/// Inputs are strings so both "0,35" and "0.35" are accepted regardless of the UI culture;
/// results use a dot, because that is what games' settings fields expect.
/// </summary>
public sealed class SensitivityConverterViewModel : ViewModelBase
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private const string DefaultFromId = "valorant";
    private const string DefaultToId = "counter-strike-2";

    private readonly IFeatureGate _featureGate;
    private readonly string _statePath;
    private readonly SensitivityConverterState _state;

    private GameOptionViewModel _fromOption;
    private GameOptionViewModel _toOption;
    private string _sensitivityInput;
    private string _dpiFromInput;
    private string _dpiToInput;
    private string _customYawFromInput;
    private string _customYawToInput;
    private string _copyStatus = "";

    public SensitivityConverterViewModel(
        IReadOnlyList<GameSensitivityProfile> games,
        string? catalogWarning,
        string statePath,
        IFeatureGate featureGate)
    {
        _featureGate = featureGate;
        _statePath = statePath;
        _state = SensitivityConverterState.Load(statePath);
        CatalogWarning = catalogWarning;

        var custom = new GameSensitivityProfile(GameSensitivityProfile.CustomId, "Benutzerdefiniert (eigener Faktor)", 0.022, 3,
            "Eigener Yaw-Wert: Grad Drehung pro Maus-Count bei Sensitivity 1.");

        // Trial games first, then the Pro library — each block alphabetically.
        GameOptions =
        [
            .. games.Where(g => !g.IsProOnly).OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase).Select(g => new GameOptionViewModel(g)),
            new GameOptionViewModel(custom),
            .. games.Where(g => g.IsProOnly).OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase).Select(g => new GameOptionViewModel(g)),
        ];

        RefreshLocks();

        _fromOption = SelectableOrDefault(_state.FromGameId, DefaultFromId);
        _toOption = SelectableOrDefault(_state.ToGameId, DefaultToId);
        _sensitivityInput = _state.Sensitivity;
        _dpiFromInput = _state.DpiFrom;
        _dpiToInput = _state.DpiTo;
        _customYawFromInput = _state.CustomYawFrom;
        _customYawToInput = _state.CustomYawTo;

        SwapCommand = new RelayCommand(_ => Swap());
        CopyResultCommand = new RelayCommand(_ => CopyResult(), _ => HasResult);

        _featureGate.Changed += (_, _) => OnLicenseTierChanged();
    }

    public IReadOnlyList<GameOptionViewModel> GameOptions { get; }
    public RelayCommand SwapCommand { get; }
    public RelayCommand CopyResultCommand { get; }

    public string? CatalogWarning { get; }
    public bool HasCatalogWarning => CatalogWarning is not null;

    public bool IsLibraryLocked => !_featureGate.IsUnlocked(Feature.ExtendedGameLibrary);
    public int ProGameCount => GameOptions.Count(o => o.Profile.IsProOnly);

    // ---- inputs ----
    public GameOptionViewModel FromOption
    {
        get => _fromOption;
        set
        {
            if (value is { IsSelectable: true }) SetInput(ref _fromOption, value);
        }
    }

    public GameOptionViewModel ToOption
    {
        get => _toOption;
        set
        {
            if (value is { IsSelectable: true }) SetInput(ref _toOption, value);
        }
    }

    public GameSensitivityProfile FromGame => _fromOption.Profile;
    public GameSensitivityProfile ToGame => _toOption.Profile;

    public string SensitivityInput
    {
        get => _sensitivityInput;
        set => SetInput(ref _sensitivityInput, value);
    }

    public string DpiFromInput
    {
        get => _dpiFromInput;
        set => SetInput(ref _dpiFromInput, value);
    }

    public string DpiToInput
    {
        get => _dpiToInput;
        set => SetInput(ref _dpiToInput, value);
    }

    public string CustomYawFromInput
    {
        get => _customYawFromInput;
        set => SetInput(ref _customYawFromInput, value);
    }

    public string CustomYawToInput
    {
        get => _customYawToInput;
        set => SetInput(ref _customYawToInput, value);
    }

    public bool IsFromCustom => FromGame.IsCustom;
    public bool IsToCustom => ToGame.IsCustom;

    // ---- parsed values ----
    private double? Sensitivity => ParsePositive(_sensitivityInput);
    private double? DpiFrom => ParsePositive(_dpiFromInput);
    private double? DpiTo => ParsePositive(_dpiToInput);
    private double? YawFrom => FromGame.IsCustom ? ParsePositive(_customYawFromInput) : FromGame.Yaw;
    private double? YawTo => ToGame.IsCustom ? ParsePositive(_customYawToInput) : ToGame.Yaw;

    public string ValidationMessage =>
        Sensitivity is null ? "Bitte eine gültige Sensitivity größer 0 eingeben."
        : DpiFrom is null || DpiTo is null ? "Bitte eine gültige DPI größer 0 eingeben."
        : YawFrom is null || YawTo is null ? "Bitte einen gültigen benutzerdefinierten Faktor größer 0 eingeben."
        : "";

    public bool HasValidationMessage => ValidationMessage.Length > 0;
    public bool HasResult => ValidationMessage.Length == 0 && ExactResult > 0;

    private double ExactResult =>
        Sensitivity is { } s && YawFrom is { } yf && DpiFrom is { } df && YawTo is { } yt && DpiTo is { } dt
            ? SensitivityMath.Convert(s, yf, df, yt, dt)
            : 0;

    // ---- outputs ----
    public string ResultText
    {
        get
        {
            if (!HasResult) return "–";
            var rounded = Math.Round(ExactResult, ToGame.Decimals, MidpointRounding.AwayFromZero);
            return FormatSensitivity(rounded, ToGame.Decimals);
        }
    }

    /// <summary>Shown when the target game's precision can't represent the result well.</summary>
    public string PrecisionHint
    {
        get
        {
            if (!HasResult) return "";
            var rounded = Math.Round(ExactResult, ToGame.Decimals, MidpointRounding.AwayFromZero);
            if (rounded <= 0) return $"Der Wert ist kleiner als die kleinste Einstellung von {ToGame.Name} — höhere DPI im Zielspiel verwenden.";
            var deviation = Math.Abs(rounded - ExactResult) / ExactResult;
            return deviation > 0.01 ? $"Rundung auf {ToGame.Decimals} Nachkommastellen weicht um {deviation:P1} ab." : "";
        }
    }

    public bool HasPrecisionHint => PrecisionHint.Length > 0;

    /// <summary>Unverified factors are usable but flagged, so nobody relies on them blindly.</summary>
    public string VerificationHint =>
        (FromGame.IsVerified || FromGame.IsCustom) && (ToGame.IsVerified || ToGame.IsCustom)
            ? ""
            : "Mindestens ein Faktor ist noch nicht gegen eine Referenz geprüft (siehe Hinweise unten).";

    public bool HasVerificationHint => VerificationHint.Length > 0;

    public string ExactResultText => HasResult ? ExactResult.ToString("0.######", Invariant) : "–";

    public string Cm360Text => HasResult
        ? SensitivityMath.CentimetresPer360(Sensitivity!.Value, YawFrom!.Value, DpiFrom!.Value).ToString("0.00", Invariant) + " cm"
        : "–";

    public string In360Text => HasResult
        ? SensitivityMath.InchesPer360(Sensitivity!.Value, YawFrom!.Value, DpiFrom!.Value).ToString("0.00", Invariant) + " in"
        : "–";

    public string EdpiFromText => HasResult
        ? SensitivityMath.EffectiveDpi(Sensitivity!.Value, DpiFrom!.Value).ToString("0.##", Invariant)
        : "–";

    public string EdpiToText => HasResult
        ? SensitivityMath.EffectiveDpi(ExactResult, DpiTo!.Value).ToString("0.##", Invariant)
        : "–";

    public string FromNote => DescribeSource(FromGame);
    public string ToNote => DescribeSource(ToGame);

    public string CopyStatus
    {
        get => _copyStatus;
        private set => SetField(ref _copyStatus, value);
    }

    private void OnLicenseTierChanged()
    {
        RefreshLocks();

        // Pro → Trial with a Pro game selected: fall back instead of keeping a locked selection.
        if (!_fromOption.IsSelectable) _fromOption = SelectableOrDefault(DefaultFromId, DefaultFromId);
        if (!_toOption.IsSelectable) _toOption = SelectableOrDefault(DefaultToId, DefaultToId);

        OnInputsChanged();
    }

    private void RefreshLocks()
    {
        var locked = IsLibraryLocked;
        foreach (var option in GameOptions)
        {
            option.IsLocked = locked && option.Profile.IsProOnly;
        }
    }

    private GameOptionViewModel SelectableOrDefault(string preferredId, string fallbackId) =>
        FindSelectable(preferredId) ?? FindSelectable(fallbackId) ?? GameOptions.First(o => o.IsSelectable);

    private GameOptionViewModel? FindSelectable(string id) =>
        GameOptions.FirstOrDefault(o => o.IsSelectable && string.Equals(o.Profile.Id, id, StringComparison.OrdinalIgnoreCase));

    private void Swap()
    {
        // Read the result BEFORE swapping: ResultText is computed from the current from/to,
        // so reading it afterwards would convert the already-swapped setup again.
        var carriedSensitivity = HasResult ? ResultText : _sensitivityInput;

        (_fromOption, _toOption) = (_toOption, _fromOption);
        (_dpiFromInput, _dpiToInput) = (_dpiToInput, _dpiFromInput);
        (_customYawFromInput, _customYawToInput) = (_customYawToInput, _customYawFromInput);
        _sensitivityInput = carriedSensitivity;

        OnInputsChanged();
    }

    private void CopyResult()
    {
        try
        {
            System.Windows.Clipboard.SetText(ResultText);
            CopyStatus = "Kopiert";
        }
        catch (COMException)
        {
            CopyStatus = "Zwischenablage belegt";
        }
    }

    private void SetInput<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnInputsChanged();
    }

    private void OnInputsChanged()
    {
        CopyStatus = "";
        OnPropertyChanged(null);
        CopyResultCommand.RaiseCanExecuteChanged();
        PersistState();
    }

    private void PersistState()
    {
        _state.FromGameId = FromGame.Id;
        _state.ToGameId = ToGame.Id;
        _state.Sensitivity = _sensitivityInput;
        _state.DpiFrom = _dpiFromInput;
        _state.DpiTo = _dpiToInput;
        _state.CustomYawFrom = _customYawFromInput;
        _state.CustomYawTo = _customYawToInput;
        _state.Save(_statePath);
    }

    private static string DescribeSource(GameSensitivityProfile game) =>
        game.IsCustom || game.IsVerified ? game.Note : game.Note + " (ungeprüft)";

    private static double? ParsePositive(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var normalized = text.Trim().Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, Invariant, out var value) && value > 0 && double.IsFinite(value)
            ? value
            : null;
    }

    private static string FormatSensitivity(double value, int decimals) =>
        value.ToString(decimals == 0 ? "0" : "0." + new string('#', decimals), Invariant);
}
