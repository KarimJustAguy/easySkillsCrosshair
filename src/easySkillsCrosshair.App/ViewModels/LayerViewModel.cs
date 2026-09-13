using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Mvvm;

namespace easySkillsCrosshair.App.ViewModels;

/// <summary>
/// Editable facade over one <see cref="CrosshairLayer"/>. Mutates the layer in place and
/// notifies the owning editor, which pushes the change to the preview and the real overlay.
/// </summary>
public sealed class LayerViewModel : ViewModelBase
{
    private readonly Action _onChanged;

    public LayerViewModel(CrosshairLayer layer, Action onChanged)
    {
        Layer = layer;
        _onChanged = onChanged;
    }

    public CrosshairLayer Layer { get; }

    public string Name
    {
        get => Layer.Name;
        set => Set(() => Layer.Name = value);
    }

    public LayerType Type
    {
        get => Layer.Type;
        set => Set(() => Layer.Type = value);
    }

    public string TypeDisplayName => LayerTypeNames.Get(Layer.Type);

    public bool IsVisible
    {
        get => Layer.IsVisible;
        set => Set(() => Layer.IsVisible = value);
    }

    // ---- which inspector sections apply to this layer type ----
    public bool UsesGap => Layer.Type is LayerType.Cross or LayerType.XCross;
    public bool UsesThickness => Layer.Type is not (LayerType.Dot or LayerType.Image);
    public bool UsesTStyle => Layer.Type == LayerType.Cross;
    public bool UsesFilled => Layer.Type == LayerType.Box;
    public bool HasShapeParameters => UsesThickness || UsesGap || UsesFilled;
    public bool UsesOutline => Layer.Type != LayerType.Image;
    public bool UsesColor => Layer.Type != LayerType.Image;
    public bool IsImage => Layer.Type == LayerType.Image;

    public string LengthLabel => Layer.Type switch
    {
        LayerType.Dot => "Durchmesser",
        LayerType.Circle => "Radius",
        LayerType.Box => "Kantenlänge",
        LayerType.Image => "Größe",
        _ => "Länge",
    };

    // ---- geometry ----
    public double Length
    {
        get => Layer.Length;
        set => Set(() => Layer.Length = Math.Clamp(value, 0, 120));
    }

    public double Thickness
    {
        get => Layer.Thickness;
        set => Set(() => Layer.Thickness = Math.Clamp(value, 0.5, 20));
    }

    public double Gap
    {
        get => Layer.Gap;
        set => Set(() => Layer.Gap = Math.Clamp(value, -20, 60));
    }

    public bool TStyle
    {
        get => Layer.TStyle;
        set => Set(() => Layer.TStyle = value);
    }

    public bool Filled
    {
        get => Layer.Filled;
        set => Set(() => Layer.Filled = value);
    }

    // ---- transform ----
    public double OffsetX
    {
        get => Layer.OffsetX;
        set => Set(() => Layer.OffsetX = Math.Round(Math.Clamp(value, -120, 120)));
    }

    public double OffsetY
    {
        get => Layer.OffsetY;
        set => Set(() => Layer.OffsetY = Math.Round(Math.Clamp(value, -120, 120)));
    }

    public double Rotation
    {
        get => Layer.RotationDegrees;
        set => Set(() => Layer.RotationDegrees = Math.Clamp(value, 0, 360));
    }

    // ---- color (Hex / RGB edit the same value) ----
    public RgbaColor Color
    {
        get => Layer.Color;
        set => Set(() => Layer.Color = value);
    }

    public string ColorHex
    {
        get => Layer.Color.ToHex();
        set
        {
            if (RgbaColor.TryFromHex(value, out var color) && color != Layer.Color)
            {
                Set(() => Layer.Color = color);
            }
        }
    }

    public double ColorR
    {
        get => Layer.Color.R;
        set => Set(() => Layer.Color = Layer.Color with { R = ToByte(value) });
    }

    public double ColorG
    {
        get => Layer.Color.G;
        set => Set(() => Layer.Color = Layer.Color with { G = ToByte(value) });
    }

    public double ColorB
    {
        get => Layer.Color.B;
        set => Set(() => Layer.Color = Layer.Color with { B = ToByte(value) });
    }

    // ---- outline ----
    public double OutlineThickness
    {
        get => Layer.OutlineThickness;
        set => Set(() => Layer.OutlineThickness = Math.Clamp(value, 0, 6));
    }

    public string OutlineColorHex
    {
        get => Layer.OutlineColor.ToHex();
        set
        {
            if (RgbaColor.TryFromHex(value, out var color) && color != Layer.OutlineColor)
            {
                Set(() => Layer.OutlineColor = color);
            }
        }
    }

    // ---- image ----
    public string? ImagePath
    {
        get => Layer.ImagePath;
        set => Set(() => Layer.ImagePath = value);
    }

    public string ImageFileName => string.IsNullOrEmpty(Layer.ImagePath)
        ? "Keine Datei gewählt"
        : System.IO.Path.GetFileName(Layer.ImagePath);

    /// <summary>Re-reads every property, e.g. after the editor clamped the layer to a license tier.</summary>
    public void Refresh() => OnPropertyChanged(null);

    private void Set(Action mutate)
    {
        mutate();
        // One blanket notification: Hex/RGB/HSV and the per-type flags are all derived values.
        OnPropertyChanged(null);
        _onChanged();
    }

    private static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value), 0, 255);
}
