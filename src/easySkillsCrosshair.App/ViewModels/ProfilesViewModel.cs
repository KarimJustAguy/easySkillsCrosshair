using System.Collections.ObjectModel;
using System.Windows.Input;
using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Mvvm;
using easySkillsCrosshair.Core.Persistence;

namespace easySkillsCrosshair.App.ViewModels;

public sealed class ProfilesViewModel : ViewModelBase
{
    private readonly IProfileStore _profileStore;
    private readonly CrosshairEditorViewModel _editor;
    private GameProfile? _selectedProfile;
    private string _newGameName = "";

    public ProfilesViewModel(IProfileStore profileStore, CrosshairEditorViewModel editor)
    {
        _profileStore = profileStore;
        _editor = editor;

        Profiles = new ObservableCollection<GameProfile>(_profileStore.LoadAll());

        SaveCurrentAsCommand = new RelayCommand(_ => SaveCurrentAs(), _ => !string.IsNullOrWhiteSpace(NewGameName));
        ActivateCommand = new RelayCommand(p => Activate(p as GameProfile ?? SelectedProfile));
        DeleteCommand = new RelayCommand(p => Delete(p as GameProfile ?? SelectedProfile));
    }

    public ObservableCollection<GameProfile> Profiles { get; }

    public GameProfile? SelectedProfile
    {
        get => _selectedProfile;
        set => SetField(ref _selectedProfile, value);
    }

    public string NewGameName
    {
        get => _newGameName;
        set
        {
            if (SetField(ref _newGameName, value))
            {
                // RelayCommand deliberately doesn't hook CommandManager.RequerySuggested, so
                // without this the Save button would stay disabled forever while typing.
                SaveCurrentAsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public RelayCommand SaveCurrentAsCommand { get; }
    public ICommand ActivateCommand { get; }
    public ICommand DeleteCommand { get; }

    private void SaveCurrentAs()
    {
        var profile = new GameProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            GameName = NewGameName,
            Crosshair = Clone(_editor.Profile),
        };

        Profiles.Add(profile);
        Persist();
        NewGameName = "";
    }

    private void Activate(GameProfile? profile)
    {
        if (profile is null)
        {
            return;
        }

        _editor.ApplyProfile(Clone(profile.Crosshair));
    }

    private void Delete(GameProfile? profile)
    {
        if (profile is null)
        {
            return;
        }

        Profiles.Remove(profile);
        if (ReferenceEquals(SelectedProfile, profile))
        {
            SelectedProfile = null;
        }

        Persist();
    }

    private void Persist() => _profileStore.SaveAll(Profiles.ToArray());

    private static CrosshairProfile Clone(CrosshairProfile source) => new()
    {
        Name = source.Name,
        Shape = source.Shape,
        Color = source.Color,
        Size = source.Size,
        Thickness = source.Thickness,
        RotationDegrees = source.RotationDegrees,
        Opacity = source.Opacity,
        OffsetX = source.OffsetX,
        OffsetY = source.OffsetY,
        CustomMediaPath = source.CustomMediaPath,
        DynamicReactions = new DynamicReactionSettings
        {
            BloomOnFire = source.DynamicReactions.BloomOnFire,
            BloomAmount = source.DynamicReactions.BloomAmount,
            BloomRecoverTime = source.DynamicReactions.BloomRecoverTime,
            HideOnAim = source.DynamicReactions.HideOnAim,
            TShapeOnMove = source.DynamicReactions.TShapeOnMove,
        },
    };
}
