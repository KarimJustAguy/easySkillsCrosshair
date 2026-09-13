using easySkillsCrosshair.App.Icons;
using easySkillsCrosshair.Core.Mvvm;

namespace easySkillsCrosshair.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private NavigationItemViewModel _selectedItem;

    public MainViewModel(
        CrosshairEditorViewModel crosshairEditor,
        AimTrainingViewModel aimTraining,
        DpiCalculatorViewModel dpiCalculator,
        ProfilesViewModel profiles,
        CommunityViewModel community,
        SettingsViewModel settings)
    {
        NavigationItems =
        [
            new NavigationItemViewModel("Crosshair", IconKind.Crosshair, crosshairEditor),
            new NavigationItemViewModel("Aim Training", IconKind.Target, aimTraining),
            new NavigationItemViewModel("DPI Calculator", IconKind.Calculator, dpiCalculator),
            new NavigationItemViewModel("Profile", IconKind.Layers, profiles),
            new NavigationItemViewModel("Community", IconKind.Globe, community),
            new NavigationItemViewModel("Settings", IconKind.Settings, settings),
        ];

        _selectedItem = NavigationItems[0];
    }

    public IReadOnlyList<NavigationItemViewModel> NavigationItems { get; }

    public NavigationItemViewModel SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetField(ref _selectedItem, value))
            {
                OnPropertyChanged(nameof(CurrentView));
            }
        }
    }

    public object CurrentView => _selectedItem.Content;
}
