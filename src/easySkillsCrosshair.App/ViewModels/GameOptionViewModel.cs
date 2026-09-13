using easySkillsCrosshair.Core.Mvvm;
using easySkillsCrosshair.Core.Sensitivity;

namespace easySkillsCrosshair.App.ViewModels;

/// <summary>A game in the converter's drop-downs. Pro-only games stay visible but unselectable in Trial.</summary>
public sealed class GameOptionViewModel : ViewModelBase
{
    private bool _isLocked;

    public GameOptionViewModel(GameSensitivityProfile profile)
    {
        Profile = profile;
    }

    public GameSensitivityProfile Profile { get; }
    public string Name => Profile.Name;
    public bool ShowUnverifiedTag => !Profile.IsVerified && !Profile.IsCustom;

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (SetField(ref _isLocked, value)) OnPropertyChanged(nameof(IsSelectable));
        }
    }

    public bool IsSelectable => !_isLocked;
}
