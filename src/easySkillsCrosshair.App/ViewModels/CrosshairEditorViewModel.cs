using System.Windows.Input;
using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Licensing;
using easySkillsCrosshair.Core.Mvvm;
using easySkillsCrosshair.Core.Overlay;
using easySkillsCrosshair.Licensing;

namespace easySkillsCrosshair.App.ViewModels;

/// <summary>
/// Owns the crosshair currently being edited. Every mutation immediately (a) re-renders the
/// editor's own live-preview canvas via <see cref="PreviewChanged"/> and (b) pushes the same
/// profile into the real <see cref="IOverlayHost"/> — both paths render through the identical
/// <c>SimpleCrosshairRenderer</c>, so the preview can never drift from what's actually on screen.
/// </summary>
public sealed class CrosshairEditorViewModel : ViewModelBase
{
    private readonly IOverlayHost _overlay;
    private readonly IFeatureGate _featureGate;
    private CrosshairProfile _profile;

    public event EventHandler? PreviewChanged;

    public CrosshairEditorViewModel(IOverlayHost overlay, IFeatureGate featureGate)
    {
        _overlay = overlay;
        _featureGate = featureGate;
        _profile = new CrosshairProfile { Name = "Neues Fadenkreuz" };

        ShapeOptions = Enum.GetValues<CrosshairShape>()
            .Select(shape => new ShapeOptionViewModel
            {
                Shape = shape,
                DisplayName = shape.ToString(),
                IsLocked = !_featureGate.IsShapeAllowed(shape),
                PreviewProfile = new CrosshairProfile
                {
                    Name = shape.ToString(),
                    Shape = shape,
                    Color = RgbaColor.FromHex("#D4AF37"),
                    Size = 7,
                    Thickness = 1.6,
                },
            })
            .ToArray();

        ColorSwatches = TrialCatalog.Colors
            .Select(color => new ColorSwatchViewModel(color))
            .ToArray();

        SizePresets = TrialCatalog.Sizes.ToArray();

        SelectShapeCommand = new RelayCommand(p =>
        {
            if (p is ShapeOptionViewModel { IsLocked: false } option)
            {
                Shape = option.Shape;
            }
        });

        SelectColorCommand = new RelayCommand(p =>
        {
            if (p is ColorSwatchViewModel swatch)
            {
                _profile.Color = swatch.Color;
                Publish();
            }
        });

        SelectSizeCommand = new RelayCommand(p =>
        {
            if (p is double size)
            {
                Size = size;
            }
        });

        BrowseCustomMediaCommand = new RelayCommand(_ => BrowseCustomMedia(), _ => IsCustomMediaUnlocked);

        // The tier can change at runtime (dev Trial/Pro toggle), so every lock state has to be
        // re-evaluated instead of staying as computed here in the constructor.
        _featureGate.Changed += OnLicenseTierChanged;

        Publish();
    }

    public IReadOnlyList<ShapeOptionViewModel> ShapeOptions { get; }
    public IReadOnlyList<ColorSwatchViewModel> ColorSwatches { get; }
    public IReadOnlyList<double> SizePresets { get; }

    public ICommand SelectShapeCommand { get; }
    public ICommand SelectColorCommand { get; }
    public ICommand SelectSizeCommand { get; }
    public RelayCommand BrowseCustomMediaCommand { get; }

    public CrosshairProfile Profile => _profile;

    public bool IsProUser => _featureGate.Tier == LicenseTier.Pro;
    public bool IsCustomMediaUnlocked => _featureGate.IsUnlocked(Feature.CustomMediaUpload);
    public bool IsDynamicReactionsUnlocked => _featureGate.IsUnlocked(Feature.DynamicReactions);

    /// <summary>Trial's swatches/presets always work; the free-form pickers are Pro-only.</summary>
    public bool IsColorPickerLocked => !IsProUser;
    public bool IsSizeSliderLocked => !IsProUser;
    public bool IsDynamicReactionsLocked => !IsDynamicReactionsUnlocked;
    public bool IsCustomMediaLocked => !IsCustomMediaUnlocked;

    public string Name
    {
        get => _profile.Name;
        set { _profile.Name = value; Publish(); }
    }

    public CrosshairShape Shape
    {
        get => _profile.Shape;
        set { _profile.Shape = value; Publish(); }
    }

    public double Size
    {
        get => _profile.Size;
        set { _profile.Size = Math.Clamp(value, 1, 64); Publish(); }
    }

    public double Thickness
    {
        get => _profile.Thickness;
        set { _profile.Thickness = Math.Clamp(value, 0.5, 12); Publish(); }
    }

    public double Rotation
    {
        get => _profile.RotationDegrees;
        set { _profile.RotationDegrees = value; Publish(); }
    }

    public double Opacity
    {
        get => _profile.Opacity;
        set { _profile.Opacity = Math.Clamp(value, 0, 1); Publish(); }
    }

    public int OffsetX
    {
        get => _profile.OffsetX;
        set { _profile.OffsetX = value; Publish(); }
    }

    public int OffsetY
    {
        get => _profile.OffsetY;
        set { _profile.OffsetY = value; Publish(); }
    }

    public string ColorHex
    {
        get => _profile.Color.ToHex();
        set
        {
            if (RgbaColor.TryFromHex(value, out var color))
            {
                _profile.Color = color;
                Publish();
            }
        }
    }

    public double ColorR
    {
        get => _profile.Color.R;
        set { _profile.Color = _profile.Color with { R = (byte)Math.Clamp(value, 0, 255) }; Publish(); }
    }

    public double ColorG
    {
        get => _profile.Color.G;
        set { _profile.Color = _profile.Color with { G = (byte)Math.Clamp(value, 0, 255) }; Publish(); }
    }

    public double ColorB
    {
        get => _profile.Color.B;
        set { _profile.Color = _profile.Color with { B = (byte)Math.Clamp(value, 0, 255) }; Publish(); }
    }

    public double Hue
    {
        get => _profile.Color.ToHsv().Hue;
        set
        {
            var (_, s, v) = _profile.Color.ToHsv();
            _profile.Color = RgbaColor.FromHsv(value, s, v, _profile.Color.A);
            Publish();
        }
    }

    public double Saturation
    {
        get => _profile.Color.ToHsv().Saturation * 100;
        set
        {
            var (h, _, v) = _profile.Color.ToHsv();
            _profile.Color = RgbaColor.FromHsv(h, value / 100.0, v, _profile.Color.A);
            Publish();
        }
    }

    public double Brightness
    {
        get => _profile.Color.ToHsv().Value * 100;
        set
        {
            var (h, s, _) = _profile.Color.ToHsv();
            _profile.Color = RgbaColor.FromHsv(h, s, value / 100.0, _profile.Color.A);
            Publish();
        }
    }

    public string? CustomMediaPath
    {
        get => _profile.CustomMediaPath;
        set { _profile.CustomMediaPath = value; Publish(); }
    }

    public bool BloomOnFire
    {
        get => _profile.DynamicReactions.BloomOnFire;
        set { _profile.DynamicReactions.BloomOnFire = value; Publish(); }
    }

    public bool HideOnAim
    {
        get => _profile.DynamicReactions.HideOnAim;
        set { _profile.DynamicReactions.HideOnAim = value; Publish(); }
    }

    public bool TShapeOnMove
    {
        get => _profile.DynamicReactions.TShapeOnMove;
        set { _profile.DynamicReactions.TShapeOnMove = value; Publish(); }
    }

    /// <summary>
    /// Loads an externally-supplied profile (from Profiles/Community) into the editor,
    /// clamping anything the current license tier doesn't allow rather than silently
    /// applying a Pro-only configuration to a Trial session.
    /// </summary>
    public void ApplyProfile(CrosshairProfile profile)
    {
        _profile = new CrosshairProfile
        {
            Name = profile.Name,
            Shape = _featureGate.IsShapeAllowed(profile.Shape) ? profile.Shape : CrosshairShape.Cross,
            Color = _featureGate.IsColorAllowed(profile.Color) ? profile.Color : TrialCatalog.Colors[0],
            Size = _featureGate.IsSizeAllowed(profile.Size) ? profile.Size : TrialCatalog.Sizes[0],
            Thickness = profile.Thickness,
            RotationDegrees = profile.RotationDegrees,
            Opacity = profile.Opacity,
            OffsetX = profile.OffsetX,
            OffsetY = profile.OffsetY,
            CustomMediaPath = IsCustomMediaUnlocked ? profile.CustomMediaPath : null,
            DynamicReactions = IsDynamicReactionsUnlocked ? profile.DynamicReactions : new DynamicReactionSettings(),
        };
        Publish();
    }

    /// <summary>
    /// Re-evaluates every lock and clamps the currently edited profile down to the new tier —
    /// switching Pro → Trial while a Pro-only shape/colour is active must not leave the editor
    /// in a state the license no longer permits.
    /// </summary>
    private void OnLicenseTierChanged(object? sender, EventArgs e)
    {
        foreach (var option in ShapeOptions)
        {
            option.IsLocked = !_featureGate.IsShapeAllowed(option.Shape);
        }

        BrowseCustomMediaCommand.RaiseCanExecuteChanged();
        ApplyProfile(_profile);
    }

    private void BrowseCustomMedia()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Reticle (*.png;*.svg;*.gif)|*.png;*.svg;*.gif",
        };

        if (dialog.ShowDialog() == true)
        {
            _profile.CustomMediaPath = dialog.FileName;
            _profile.Shape = CrosshairShape.Custom;
            Publish();
        }
    }

    private void Publish()
    {
        foreach (var option in ShapeOptions)
        {
            option.IsActive = option.Shape == _profile.Shape;
        }

        OnPropertyChanged(null);
        _overlay.SetOffset(_profile.OffsetX, _profile.OffsetY);
        _overlay.UpdateContent(_profile);
        PreviewChanged?.Invoke(this, EventArgs.Empty);
    }
}
