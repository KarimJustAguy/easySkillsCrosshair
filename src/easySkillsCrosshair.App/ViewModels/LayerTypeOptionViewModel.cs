using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Mvvm;

namespace easySkillsCrosshair.App.ViewModels;

/// <summary>One tile in the layer-type picker, rendered as a live mini crosshair.</summary>
public sealed class LayerTypeOptionViewModel : ViewModelBase
{
    private bool _isActive;
    private bool _isLocked;

    public LayerTypeOptionViewModel(LayerType type)
    {
        Type = type;
        DisplayName = LayerTypeNames.Get(type);
        PreviewProfile = new CrosshairProfile
        {
            Name = DisplayName,
            Layers =
            [
                type == LayerType.Image
                    ? new CrosshairLayer { Type = LayerType.Box, Filled = false, Length = 8, Thickness = 1.5, Color = RgbaColor.FromHex("#D4AF37") }
                    : new CrosshairLayer { Type = type, Length = type == LayerType.Dot ? 6 : 7, Thickness = 1.6, Gap = 2, Color = RgbaColor.FromHex("#D4AF37") },
            ],
        };
    }

    public LayerType Type { get; }
    public string DisplayName { get; }
    public CrosshairProfile PreviewProfile { get; }

    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value);
    }

    public bool IsLocked
    {
        get => _isLocked;
        set => SetField(ref _isLocked, value);
    }
}
