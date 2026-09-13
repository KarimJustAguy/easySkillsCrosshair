using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace easySkillsCrosshair.Overlay.Rendering;

/// <summary>
/// Decodes custom reticles once and caches them by path + last-write time, so re-rendering
/// (every slider tick, every animation frame) never touches the disk again. Unreadable or
/// corrupt files yield null and the layer simply draws nothing — never an exception mid-render.
/// </summary>
public static class ImageAssetCache
{
    private static readonly ConcurrentDictionary<(string Path, DateTime Stamp), ImageAsset?> Cache = new();

    private static readonly TimeSpan MinimumGifFrameDelay = TimeSpan.FromMilliseconds(20);
    private static readonly TimeSpan DefaultGifFrameDelay = TimeSpan.FromMilliseconds(100);

    public static ImageAsset? Get(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        var key = (path, File.GetLastWriteTimeUtc(path));
        return Cache.GetOrAdd(key, static k => Load(k.Path));
    }

    private static ImageAsset? Load(string path)
    {
        try
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".svg" => LoadSvg(path),
                ".gif" => LoadGif(path),
                _ => LoadStill(path),
            };
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or FileFormatException
                                       or UnauthorizedAccessException or ArgumentException
                                       or InvalidOperationException)
        {
            return null;
        }
    }

    private static ImageAsset LoadStill(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad; // releases the file handle immediately
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return new ImageAsset([bitmap], [TimeSpan.Zero]);
    }

    private static ImageAsset? LoadSvg(string path)
    {
        var settings = new WpfDrawingSettings { IncludeRuntime = false, TextAsGeometry = true };
        var drawing = new FileSvgReader(settings).Read(path);
        if (drawing is null) return null;
        drawing.Freeze();
        return new ImageAsset(drawing);
    }

    /// <summary>
    /// GIF frames are deltas: each has its own offset and a disposal method. Compositing them
    /// onto a full logical-screen canvas is required, otherwise optimised GIFs flicker/tear.
    /// </summary>
    private static ImageAsset LoadGif(string path)
    {
        BitmapDecoder decoder;
        using (var stream = File.OpenRead(path))
        {
            decoder = new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        }

        if (decoder.Frames.Count <= 1)
        {
            var single = decoder.Frames[0];
            single.Freeze();
            return new ImageAsset([single], [TimeSpan.Zero]);
        }

        var width = ReadUShort(decoder.Metadata as BitmapMetadata, "/logscrdesc/Width") ?? decoder.Frames[0].PixelWidth;
        var height = ReadUShort(decoder.Metadata as BitmapMetadata, "/logscrdesc/Height") ?? decoder.Frames[0].PixelHeight;

        var frames = new List<ImageSource>(decoder.Frames.Count);
        var delays = new List<TimeSpan>(decoder.Frames.Count);
        BitmapSource? previous = null;

        foreach (var frame in decoder.Frames)
        {
            var metadata = frame.Metadata as BitmapMetadata;
            var left = ReadUShort(metadata, "/imgdesc/Left") ?? 0;
            var top = ReadUShort(metadata, "/imgdesc/Top") ?? 0;
            var delayCs = ReadUShort(metadata, "/grctlext/Delay") ?? 0;
            var disposal = ReadByte(metadata, "/grctlext/Disposal") ?? 0;

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                if (previous is not null)
                {
                    dc.DrawImage(previous, new Rect(0, 0, width, height));
                }

                dc.DrawImage(frame, new Rect(left, top, frame.PixelWidth, frame.PixelHeight));
            }

            var composed = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            composed.Render(visual);
            composed.Freeze();

            frames.Add(composed);
            var delay = TimeSpan.FromMilliseconds(delayCs * 10);
            delays.Add(delay < MinimumGifFrameDelay ? DefaultGifFrameDelay : delay);

            // Disposal 2 = restore to background: the next frame starts from a clear canvas.
            previous = disposal == 2 ? null : composed;
        }

        return new ImageAsset(frames, delays);
    }

    private static int? ReadUShort(BitmapMetadata? metadata, string query) =>
        metadata?.GetQuery(query) is ushort value ? value : null;

    private static int? ReadByte(BitmapMetadata? metadata, string query) =>
        metadata?.GetQuery(query) is byte value ? value : null;
}
