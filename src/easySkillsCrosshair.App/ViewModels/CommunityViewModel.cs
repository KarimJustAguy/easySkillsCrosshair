using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using easySkillsCrosshair.Core.Community;
using easySkillsCrosshair.Core.Licensing;
using easySkillsCrosshair.Core.Mvvm;

namespace easySkillsCrosshair.App.ViewModels;

/// <summary>
/// Browse built-in/imported crosshairs and share locally via share codes or share files.
/// Using a crosshair works in every tier (it is clamped to the license); creating and importing
/// shares is Pro-only (<see cref="Feature.CommunitySharing"/>).
/// </summary>
public sealed class CommunityViewModel : ViewModelBase
{
    private readonly ICommunityService _communityService;
    private readonly CrosshairEditorViewModel _editor;
    private readonly IFeatureGate _featureGate;

    private string _searchText = "";
    private string _shareCodeInput = "";
    private string _statusMessage = "";
    private bool _isStatusError;
    private bool _isBusy;

    public CommunityViewModel(ICommunityService communityService, CrosshairEditorViewModel editor, IFeatureGate featureGate)
    {
        _communityService = communityService;
        _editor = editor;
        _featureGate = featureGate;

        SearchCommand = new RelayCommand(async _ => await RefreshAsync());
        UseCommand = new RelayCommand(p =>
        {
            if (p is CommunityCrosshairListing listing)
            {
                _editor.ApplyProfile(listing.Profile);
                SetStatus($"„{listing.Name}“ ist jetzt aktiv.");
            }
        });

        CopyShareCodeCommand = new RelayCommand(_ => CopyShareCode(), _ => !IsSharingLocked);
        ImportShareCodeCommand = new RelayCommand(async _ => await ImportShareCodeAsync(), _ => !IsSharingLocked && !string.IsNullOrWhiteSpace(ShareCodeInput));
        ExportFileCommand = new RelayCommand(async _ => await ExportFileAsync(), _ => !IsSharingLocked);
        ImportFileCommand = new RelayCommand(async _ => await ImportFileAsync(), _ => !IsSharingLocked);
        RemoveCommand = new RelayCommand(async p =>
        {
            if (p is CommunityCrosshairListing { IsImported: true } listing)
            {
                await RunAsync(async () =>
                {
                    await _communityService.RemoveAsync(listing.Id);
                    await RefreshAsync();
                    SetStatus($"„{listing.Name}“ wurde entfernt.");
                });
            }
        });

        _featureGate.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(IsSharingLocked));
            RaiseCanExecute();
        };

        _ = RefreshAsync();
    }

    public ObservableCollection<CommunityCrosshairListing> Listings { get; } = [];

    public bool IsSharingLocked => !_featureGate.IsUnlocked(Feature.CommunitySharing);

    public string SearchText
    {
        get => _searchText;
        set => SetField(ref _searchText, value);
    }

    public string ShareCodeInput
    {
        get => _shareCodeInput;
        set
        {
            if (SetField(ref _shareCodeInput, value)) ImportShareCodeCommand.RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetField(ref _statusMessage, value)) OnPropertyChanged(nameof(HasStatus));
        }
    }

    public bool HasStatus => !string.IsNullOrEmpty(_statusMessage);

    public bool IsStatusError
    {
        get => _isStatusError;
        private set => SetField(ref _isStatusError, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    public RelayCommand SearchCommand { get; }
    public RelayCommand UseCommand { get; }
    public RelayCommand CopyShareCodeCommand { get; }
    public RelayCommand ImportShareCodeCommand { get; }
    public RelayCommand ExportFileCommand { get; }
    public RelayCommand ImportFileCommand { get; }
    public RelayCommand RemoveCommand { get; }

    private async Task RefreshAsync()
    {
        var results = await _communityService.SearchAsync(SearchText);
        Listings.Clear();
        foreach (var listing in results)
        {
            Listings.Add(listing);
        }
    }

    private void CopyShareCode()
    {
        var code = _communityService.CreateShareCode(_editor.Profile);
        try
        {
            System.Windows.Clipboard.SetText(code);
            var note = _editor.Profile.Layers.Any(l => l.ImagePath is not null)
                ? " Bild-Ebenen sind im Code nicht enthalten — dafür „Als Datei exportieren“ nutzen."
                : "";
            SetStatus("Share-Code in die Zwischenablage kopiert." + note);
        }
        catch (COMException)
        {
            // Another process holds the clipboard open.
            SetStatus("Zwischenablage ist gerade belegt — bitte erneut versuchen.", isError: true);
        }
    }

    private Task ImportShareCodeAsync() => RunAsync(async () =>
    {
        var listing = await _communityService.ImportShareCodeAsync(ShareCodeInput);
        ShareCodeInput = "";
        await RefreshAsync();
        SetStatus($"„{listing.Name}“ importiert.");
    });

    private Task ExportFileAsync() => RunAsync(async () =>
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = SanitizeFileName(_editor.Profile.Name) + CrosshairShareCodec.FileExtension,
            Filter = $"easySkills Fadenkreuz (*{CrosshairShareCodec.FileExtension})|*{CrosshairShareCodec.FileExtension}",
        };

        if (dialog.ShowDialog() != true) return;

        await _communityService.ExportFileAsync(_editor.Profile, dialog.FileName);
        SetStatus($"Exportiert nach {System.IO.Path.GetFileName(dialog.FileName)}.");
    });

    private Task ImportFileAsync() => RunAsync(async () =>
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = $"easySkills Fadenkreuz (*{CrosshairShareCodec.FileExtension})|*{CrosshairShareCodec.FileExtension}",
        };

        if (dialog.ShowDialog() != true) return;

        var listing = await _communityService.ImportFileAsync(dialog.FileName);
        await RefreshAsync();
        SetStatus($"„{listing.Name}“ importiert.");
    });

    /// <summary>Single place that turns expected failures into a visible message instead of a crash.</summary>
    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await action();
        }
        catch (FormatException ex)
        {
            SetStatus(ex.Message, isError: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus("Datei konnte nicht gelesen oder geschrieben werden: " + ex.Message, isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetStatus(string message, bool isError = false)
    {
        IsStatusError = isError;
        StatusMessage = message;
    }

    private void RaiseCanExecute()
    {
        CopyShareCodeCommand.RaiseCanExecuteChanged();
        ImportShareCodeCommand.RaiseCanExecuteChanged();
        ExportFileCommand.RaiseCanExecuteChanged();
        ImportFileCommand.RaiseCanExecuteChanged();
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrEmpty(cleaned) ? "fadenkreuz" : cleaned;
    }
}
