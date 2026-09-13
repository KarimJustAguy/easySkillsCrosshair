using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Mvvm;

namespace easySkillsCrosshair.App.ViewModels;

public sealed class ShapeOptionViewModel : ViewModelBase
{
    private bool _isActive;
    private bool _isLocked;

    public required CrosshairShape Shape { get; init; }
    public required string DisplayName { get; init; }

    /// <summary>
    /// Rendered as the tile's thumbnail instead of a text label (the label used to get
    /// truncated at tile width). Built once — the preview never changes per option.
    /// </summary>
    public required CrosshairProfile PreviewProfile { get; init; }

    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value);
    }

    /// <summary>Mutable: the tier can now be switched at runtime, so locks must re-evaluate.</summary>
    public bool IsLocked
    {
        get => _isLocked;
        set => SetField(ref _isLocked, value);
    }
}
