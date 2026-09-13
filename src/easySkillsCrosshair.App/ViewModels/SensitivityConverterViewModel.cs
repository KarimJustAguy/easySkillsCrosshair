using System.Globalization;
using System.Runtime.InteropServices;
using easySkillsCrosshair.Core.Mvvm;
using easySkillsCrosshair.Core.Sensitivity;

namespace easySkillsCrosshair.App.ViewModels;

/// <summary>
/// Game-to-game sensitivity converter (360°-distance method). Unrestricted in every tier.
/// Inputs are strings so both "0,35" and "0.35" are accepted regardless of the UI culture;
/// results use a dot, because that is what games' settings fields expect.
/// </summary>
public sealed class SensitivityConverterViewModel : ViewModelBase
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private readonly string _statePath;
    private readonly SensitivityConverterState _state;

    private GameSensitivityProfile _fromGame;
    private GameSensitivityProfile _toGame;
    private string _sensitivityInput;
    private string _dpiFromInput;
    private string _dpiToInput;
    private string _customYawFromInput;
    private string _customYawToInput;
    private string _copyStatus = "";

    public SensitivityConverterViewModel(IReadOnlyList<GameSensitivityProfile> games, string? catalogWarning, string statePath)
    {
        _statePath = statePath;
        _state = SensitivityConverterState.Load(statePath);
        CatalogWarning = catalogWarning;

        Games =
        [
            .. games,
            new GameSensitivityProfile(GameSensitivityProfile.CustomId, "Benutzerdefiniert (eigener Faktor)", 0.022, 3,
                "Eigener Yaw-Wert: Grad Drehung pro Maus-Count bei Sensitivity 1."),
        ];

        _fromGame = FindGame(_state.FromGameId) ?? FindGame("valorant") ?? Games[0];
        _toGame = FindGame(_state.ToGameId) ?? FindGame("counter-strike-2") ?? Games[^1];
        _sensitivityInput = _state.Sensitivity;
        _dpiFromInput = _state.DpiFrom;
        _dpiToInput = _state.DpiTo;
        _customYawFromInput = _state.CustomYawFrom;
        _customYawToInput = _state.CustomYawTo;

        SwapCommand = new RelayCommand(_ => Swap());
        CopyResultCommand = new RelayCommand(_ => CopyResult(), _ => HasResult);
    }

    public IReadOnlyList<GameSensitivityProfile> Games { get; }
    public RelayCommand SwapCommand { get; }
    public RelayCommand CopyResultCommand { get; }

    public string? CatalogWarning { get; }
    public bool HasCatalogWarning => CatalogWarning is not null;

    // ---- inputs ----
    public GameSensitivityProfile FromGame
    {
        get => _fromGame;
        set => SetInput(ref _fromGame, value);
    }

    public GameSensitivityProfile ToGame
    {
        get => _toGame;
        set => SetInput(ref _toGame, value);
    }

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

    public bool IsFromCustom => _fromGame.IsCustom;
    public bool IsToCustom => _toGame.IsCustom;

    // ---- parsed values ----
    private double? Sensitivity => ParsePositive(_sensitivityInput);
    private double? DpiFrom => ParsePositive(_dpiFromInput);
    private double? DpiTo => ParsePositive(_dpiToInput);
    private double? YawFrom => _fromGame.IsCustom ? ParsePositive(_customYawFromInput) : _fromGame.Yaw;
    private double? YawTo => _toGame.IsCustom ? ParsePositive(_customYawToInput) : _toGame.Yaw;

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
            var rounded = Math.Round(ExactResult, _toGame.Decimals, MidpointRounding.AwayFromZero);
            return FormatSensitivity(rounded, _toGame.Decimals);
        }
    }

    /// <summary>Shown when the target game's precision can't represent the result well.</summary>
    public string PrecisionHint
    {
        get
        {
            if (!HasResult) return "";
            var rounded = Math.Round(ExactResult, _toGame.Decimals, MidpointRounding.AwayFromZero);
            if (rounded <= 0) return $"Der Wert ist kleiner als die kleinste Einstellung von {_toGame.Name} — höhere DPI im Zielspiel verwenden.";
            var deviation = Math.Abs(rounded - ExactResult) / ExactResult;
            return deviation > 0.01 ? $"Rundung auf {_toGame.Decimals} Nachkommastellen weicht um {deviation:P1} ab." : "";
        }
    }

    public bool HasPrecisionHint => PrecisionHint.Length > 0;

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

    public string FromNote => _fromGame.Note;
    public string ToNote => _toGame.Note;

    public string CopyStatus
    {
        get => _copyStatus;
        private set => SetField(ref _copyStatus, value);
    }

    private void Swap()
    {
        // Read the result BEFORE swapping: ResultText is computed from the current from/to,
        // so reading it afterwards would convert the already-swapped setup again.
        var carriedSensitivity = HasResult ? ResultText : _sensitivityInput;

        (_fromGame, _toGame) = (_toGame, _fromGame);
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
        _state.FromGameId = _fromGame.Id;
        _state.ToGameId = _toGame.Id;
        _state.Sensitivity = _sensitivityInput;
        _state.DpiFrom = _dpiFromInput;
        _state.DpiTo = _dpiToInput;
        _state.CustomYawFrom = _customYawFromInput;
        _state.CustomYawTo = _customYawToInput;
        _state.Save(_statePath);
    }

    private GameSensitivityProfile? FindGame(string id) =>
        Games.FirstOrDefault(g => string.Equals(g.Id, id, StringComparison.OrdinalIgnoreCase));

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
