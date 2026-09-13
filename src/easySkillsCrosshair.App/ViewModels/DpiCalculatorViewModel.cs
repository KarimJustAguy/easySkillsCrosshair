using easySkillsCrosshair.Core.Mvvm;

namespace easySkillsCrosshair.App.ViewModels;

public sealed record SensitivityGamePreset(string Name, double YawPerCount);

/// <summary>
/// Unrestricted regardless of license tier, per spec. Yaw-per-count values for the built-in
/// presets are widely-cited community constants, not verified against each game's current
/// build — treat them as indicative and let players fall back to "Benutzerdefiniert" with a
/// yaw they trust.
/// </summary>
public sealed class DpiCalculatorViewModel : ViewModelBase
{
    private const string CustomPresetName = "Benutzerdefiniert";

    private SensitivityGamePreset _selectedPreset;
    private double _dpi = 800;
    private double _sensitivity = 1.0;
    private double _customYaw = 0.022;

    public DpiCalculatorViewModel()
    {
        _selectedPreset = Presets[0];
    }

    public IReadOnlyList<SensitivityGamePreset> Presets { get; } =
    [
        new("CS2 / CS:GO", 0.022),
        new("Valorant", 0.07),
        new("Apex Legends", 0.022),
        new(CustomPresetName, 0.022),
    ];

    public SensitivityGamePreset SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (SetField(ref _selectedPreset, value))
            {
                OnPropertyChanged(nameof(IsCustomYawEditable));
                OnPropertyChanged(nameof(EffectiveDpi));
                OnPropertyChanged(nameof(CmPer360));
            }
        }
    }

    public bool IsCustomYawEditable => _selectedPreset.Name == CustomPresetName;

    public double Dpi
    {
        get => _dpi;
        set
        {
            if (SetField(ref _dpi, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(EffectiveDpi));
                OnPropertyChanged(nameof(CmPer360));
            }
        }
    }

    public double Sensitivity
    {
        get => _sensitivity;
        set
        {
            if (SetField(ref _sensitivity, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(EffectiveDpi));
                OnPropertyChanged(nameof(CmPer360));
            }
        }
    }

    public double CustomYaw
    {
        get => _customYaw;
        set
        {
            if (SetField(ref _customYaw, value))
            {
                OnPropertyChanged(nameof(CmPer360));
            }
        }
    }

    public double EffectiveDpi => _dpi * _sensitivity;

    public double CmPer360
    {
        get
        {
            var yaw = IsCustomYawEditable ? _customYaw : _selectedPreset.YawPerCount;
            return EffectiveDpi <= 0 || yaw <= 0 ? 0 : 2.54 * 360 / (EffectiveDpi * yaw);
        }
    }
}
