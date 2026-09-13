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
            Crosshair = _editor.Profile.Clone(),
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

        _editor.ApplyProfile(profile.Crosshair);
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
}
