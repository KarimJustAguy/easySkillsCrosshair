using System.Windows.Media;

namespace easySkillsCrosshair.Overlay.Rendering;

/// <summary>
/// A decoded, frozen custom reticle. Raster images carry one frame (PNG) or a fully composited
/// frame sequence (GIF); SVGs carry a vector <see cref="Drawing"/> that scales without blur.
/// </summary>
public sealed class ImageAsset
{
    public ImageAsset(IReadOnlyList<ImageSource> frames, IReadOnlyList<TimeSpan> frameDelays)
    {
        Frames = frames;
        FrameDelays = frameDelays;
        TotalDuration = TimeSpan.FromTicks(frameDelays.Sum(d => d.Ticks));
    }

    public ImageAsset(Drawing vector)
    {
        Vector = vector;
        Frames = [];
        FrameDelays = [];
    }

    public IReadOnlyList<ImageSource> Frames { get; }
    public IReadOnlyList<TimeSpan> FrameDelays { get; }
    public TimeSpan TotalDuration { get; }
    public Drawing? Vector { get; }

    public bool IsAnimated => Frames.Count > 1 && TotalDuration > TimeSpan.Zero;

    public ImageSource? FrameAt(TimeSpan elapsed)
    {
        if (Frames.Count == 0) return null;
        if (!IsAnimated) return Frames[0];

        var position = TimeSpan.FromTicks(elapsed.Ticks % TotalDuration.Ticks);
        for (var i = 0; i < FrameDelays.Count; i++)
        {
            if (position < FrameDelays[i]) return Frames[i];
            position -= FrameDelays[i];
        }

        return Frames[^1];
    }
}
