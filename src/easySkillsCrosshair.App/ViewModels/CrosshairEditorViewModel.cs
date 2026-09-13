using System.Collections.ObjectModel;
using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Licensing;
using easySkillsCrosshair.Core.Mvvm;
using easySkillsCrosshair.Core.Overlay;
using easySkillsCrosshair.Licensing;

namespace easySkillsCrosshair.App.ViewModels;

/// <summary>
/// Layer-based crosshair editor. Owns the profile being edited; every change is pushed straight
/// to the real <see cref="IOverlayHost"/> and signalled to the view's live canvas via
/// <see cref="PreviewChanged"/>. Both render through the same CrosshairDrawing engine.
///
/// <see cref="Layers"/> is in display order (topmost first); the profile stores render order
/// (bottom first) — <see cref="SyncProfileLayers"/> maps between them after structural edits.
/// </summary>
public sealed class CrosshairEditorViewModel : ViewModelBase
{
    private const string ImageFileFilter = "Reticle (*.png;*.gif;*.svg)|*.png;*.gif;*.svg";

    private readonly IOverlayHost _overlay;
    private readonly IFeatureGate _featureGate;
    private CrosshairProfile _profile;
    private LayerViewModel? _selectedLayer;

    public event EventHandler? PreviewChanged;

    public CrosshairEditorViewModel(IOverlayHost overlay, IFeatureGate featureGate)
    {
        _overlay = overlay;
        _featureGate = featureGate;
        _profile = CrosshairProfile.CreateDefault("Neues Fadenkreuz");

        LayerTypeOptions = Enum.GetValues<LayerType>().Select(t => new LayerTypeOptionViewModel(t)).ToArray();
        ColorSwatches = TrialCatalog.Colors.Select(c => new ColorSwatchViewModel(c)).ToArray();

        AddLayerCommand = new RelayCommand(p => AddLayer(p is LayerType t ? t : LayerType.Cross), _ => !IsMultiLayerLocked);
        AddImageLayerCommand = new RelayCommand(_ => AddImageLayer(), _ => IsCustomMediaUnlocked);
        RemoveLayerCommand = new RelayCommand(p => RemoveLayer(p as LayerViewModel ?? SelectedLayer), _ => Layers.Count > 1);
        DuplicateLayerCommand = new RelayCommand(p => DuplicateLayer(p as LayerViewModel ?? SelectedLayer), _ => !IsMultiLayerLocked);
        MoveLayerUpCommand = new RelayCommand(p => MoveLayer(p as LayerViewModel ?? SelectedLayer, -1));
        MoveLayerDownCommand = new RelayCommand(p => MoveLayer(p as LayerViewModel ?? SelectedLayer, +1));
        ToggleLayerVisibilityCommand = new RelayCommand(p =>
        {
            if (p is LayerViewModel layer) layer.IsVisible = !layer.IsVisible;
        });

        SelectLayerTypeCommand = new RelayCommand(p =>
        {
            if (p is LayerTypeOptionViewModel { IsLocked: false } option && SelectedLayer is { } layer)
            {
                ChangeLayerType(layer, option.Type);
            }
        });

        SelectColorCommand = new RelayCommand(p =>
        {
            if (p is ColorSwatchViewModel swatch && SelectedLayer is { } layer) layer.Color = swatch.Color;
        });

        BrowseImageCommand = new RelayCommand(_ => BrowseImage(), _ => IsCustomMediaUnlocked && SelectedLayer?.IsImage == true);
        ResetCommand = new RelayCommand(_ => ApplyProfile(CrosshairProfile.CreateDefault(_profile.Name)));

        _featureGate.Changed += (_, _) => OnLicenseTierChanged();

        RebuildLayers();
        RefreshLockStates();
        Publish();
    }

    public ObservableCollection<LayerViewModel> Layers { get; } = [];
    public IReadOnlyList<LayerTypeOptionViewModel> LayerTypeOptions { get; }
    public IReadOnlyList<ColorSwatchViewModel> ColorSwatches { get; }

    public RelayCommand AddLayerCommand { get; }
    public RelayCommand AddImageLayerCommand { get; }
    public RelayCommand RemoveLayerCommand { get; }
    public RelayCommand DuplicateLayerCommand { get; }
    public RelayCommand MoveLayerUpCommand { get; }
    public RelayCommand MoveLayerDownCommand { get; }
    public RelayCommand ToggleLayerVisibilityCommand { get; }
    public RelayCommand SelectLayerTypeCommand { get; }
    public RelayCommand SelectColorCommand { get; }
    public RelayCommand BrowseImageCommand { get; }
    public RelayCommand ResetCommand { get; }

    public CrosshairProfile Profile => _profile;

    public LayerViewModel? SelectedLayer
    {
        get => _selectedLayer;
        set
        {
            if (!SetField(ref _selectedLayer, value)) return;
            OnPropertyChanged(nameof(HasSelectedLayer));
            RefreshActiveTypeOption();
            BrowseImageCommand.RaiseCanExecuteChanged();
        }
    }

    public bool HasSelectedLayer => _selectedLayer is not null;

    // ---- license state ----
    public bool IsProUser => _featureGate.Tier == LicenseTier.Pro;
    public bool IsCustomMediaUnlocked => _featureGate.IsUnlocked(Feature.CustomMediaUpload);
    public bool IsMultiLayerLocked => !_featureGate.IsUnlocked(Feature.MultipleLayers);
    public bool IsColorPickerLocked => !IsProUser;
    public bool IsSizeSliderLocked => !IsProUser;
    public bool IsOutlineLocked => !IsProUser;
    public bool IsDynamicReactionsLocked => !_featureGate.IsUnlocked(Feature.DynamicReactions);
    public bool IsCustomMediaLocked => !IsCustomMediaUnlocked;

    // ---- whole-crosshair properties ----
    public string Name
    {
        get => _profile.Name;
        set => SetProfile(() => _profile.Name = value);
    }

    public double Rotation
    {
        get => _profile.RotationDegrees;
        set => SetProfile(() => _profile.RotationDegrees = Math.Clamp(value, 0, 360));
    }

    public double Opacity
    {
        get => _profile.Opacity;
        set => SetProfile(() => _profile.Opacity = Math.Clamp(value, 0, 1));
    }

    public int OffsetX
    {
        get => _profile.OffsetX;
        set => SetProfile(() => _profile.OffsetX = Math.Clamp(value, -500, 500));
    }

    public int OffsetY
    {
        get => _profile.OffsetY;
        set => SetProfile(() => _profile.OffsetY = Math.Clamp(value, -500, 500));
    }

    // ---- dynamic reactions ----
    public bool BloomOnFire
    {
        get => _profile.DynamicReactions.BloomOnFire;
        set => SetProfile(() => _profile.DynamicReactions.BloomOnFire = value);
    }

    public double BloomAmount
    {
        get => _profile.DynamicReactions.BloomAmount;
        set => SetProfile(() => _profile.DynamicReactions.BloomAmount = Math.Clamp(value, 0, 40));
    }

    public double BloomRecoverMilliseconds
    {
        get => _profile.DynamicReactions.BloomRecoverTime.TotalMilliseconds;
        set => SetProfile(() => _profile.DynamicReactions.BloomRecoverTime = TimeSpan.FromMilliseconds(Math.Clamp(value, 20, 2000)));
    }

    public bool HideOnAim
    {
        get => _profile.DynamicReactions.HideOnAim;
        set => SetProfile(() => _profile.DynamicReactions.HideOnAim = value);
    }

    public bool TShapeOnMove
    {
        get => _profile.DynamicReactions.TShapeOnMove;
        set => SetProfile(() => _profile.DynamicReactions.TShapeOnMove = value);
    }

    /// <summary>
    /// Loads a profile (from Profiles/Community) as an independent copy, clamped to what the
    /// current tier permits — a Pro crosshair can never silently activate in a Trial session.
    /// </summary>
    public void ApplyProfile(CrosshairProfile profile)
    {
        _profile = _featureGate.ClampToLicense(profile);
        RebuildLayers();
        OnPropertyChanged(null);
        Publish();
    }

    private void AddLayer(LayerType type)
    {
        if (IsMultiLayerLocked || !_featureGate.IsLayerTypeAllowed(type)) return;

        var layer = new CrosshairLayer
        {
            Name = NextLayerName(type),
            Type = type,
            Length = type == LayerType.Image ? 16 : type == LayerType.Dot ? 3 : 6,
        };
        InsertAboveSelection(layer);
    }

    private void AddImageLayer()
    {
        if (IsCustomMediaLocked || PickImageFile() is not { } path) return;

        InsertAboveSelection(new CrosshairLayer
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(path),
            Type = LayerType.Image,
            ImagePath = path,
            Length = 16,
        });
    }

    private void DuplicateLayer(LayerViewModel? source)
    {
        if (source is null || IsMultiLayerLocked) return;

        var copy = source.Layer.Clone();
        copy.Name = source.Name + " Kopie";
        InsertAboveSelection(copy);
    }

    private void InsertAboveSelection(CrosshairLayer layer)
    {
        var vm = CreateLayerViewModel(layer);
        var index = SelectedLayer is { } selected ? Layers.IndexOf(selected) : 0;
        Layers.Insert(Math.Max(0, index), vm);
        SelectedLayer = vm;
        OnStructureChanged();
    }

    private void RemoveLayer(LayerViewModel? layer)
    {
        if (layer is null || Layers.Count <= 1) return;

        var index = Layers.IndexOf(layer);
        Layers.Remove(layer);
        SelectedLayer = Layers[Math.Clamp(index, 0, Layers.Count - 1)];
        OnStructureChanged();
    }

    private void MoveLayer(LayerViewModel? layer, int direction)
    {
        if (layer is null) return;

        var from = Layers.IndexOf(layer);
        var to = from + direction;
        if (from < 0 || to < 0 || to >= Layers.Count) return;

        Layers.Move(from, to);
        OnStructureChanged();
    }

    private void ChangeLayerType(LayerViewModel layer, LayerType type)
    {
        if (layer.Type == type) return;

        if (type == LayerType.Image && string.IsNullOrEmpty(layer.ImagePath))
        {
            if (PickImageFile() is not { } path) return;
            layer.Layer.ImagePath = path;
            layer.Layer.Length = Math.Max(layer.Layer.Length, 16);
        }

        layer.Type = type; // notifies + publishes
        RefreshActiveTypeOption();
        BrowseImageCommand.RaiseCanExecuteChanged();
    }

    private void BrowseImage()
    {
        if (SelectedLayer is { IsImage: true } layer && PickImageFile() is { } path)
        {
            layer.ImagePath = path;
        }
    }

    private static string? PickImageFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = ImageFileFilter };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private string NextLayerName(LayerType type)
    {
        var baseName = LayerTypeNames.Get(type);
        var n = Layers.Count(l => l.Name.StartsWith(baseName, StringComparison.Ordinal)) + 1;
        return n == 1 ? baseName : $"{baseName} {n}";
    }

    private void OnLicenseTierChanged()
    {
        RefreshLockStates();
        ApplyProfile(_profile); // clamps down on Pro → Trial, no-op copy on Trial → Pro
    }

    private void RefreshLockStates()
    {
        foreach (var option in LayerTypeOptions)
        {
            option.IsLocked = !_featureGate.IsLayerTypeAllowed(option.Type);
        }

        AddLayerCommand.RaiseCanExecuteChanged();
        AddImageLayerCommand.RaiseCanExecuteChanged();
        DuplicateLayerCommand.RaiseCanExecuteChanged();
        BrowseImageCommand.RaiseCanExecuteChanged();
    }

    private void RebuildLayers()
    {
        var previouslySelectedIndex = SelectedLayer is { } s ? Layers.IndexOf(s) : 0;

        Layers.Clear();
        for (var i = _profile.Layers.Count - 1; i >= 0; i--)
        {
            Layers.Add(CreateLayerViewModel(_profile.Layers[i]));
        }

        SelectedLayer = Layers.Count == 0 ? null : Layers[Math.Clamp(previouslySelectedIndex, 0, Layers.Count - 1)];
        RemoveLayerCommand.RaiseCanExecuteChanged();
    }

    private LayerViewModel CreateLayerViewModel(CrosshairLayer layer) => new(layer, onChanged: Publish);

    private void OnStructureChanged()
    {
        SyncProfileLayers();
        RemoveLayerCommand.RaiseCanExecuteChanged();
        Publish();
    }

    private void SyncProfileLayers() =>
        _profile.Layers = Layers.Reverse().Select(vm => vm.Layer).ToList();

    private void RefreshActiveTypeOption()
    {
        foreach (var option in LayerTypeOptions)
        {
            option.IsActive = SelectedLayer?.Type == option.Type;
        }
    }

    private void SetProfile(Action mutate, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        mutate();
        OnPropertyChanged(propertyName);
        Publish();
    }

    private void Publish()
    {
        _overlay.SetOffset(_profile.OffsetX, _profile.OffsetY);
        _overlay.UpdateContent(_profile);
        PreviewChanged?.Invoke(this, EventArgs.Empty);
    }
}
