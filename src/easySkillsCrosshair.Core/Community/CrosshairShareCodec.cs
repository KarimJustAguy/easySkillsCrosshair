using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.Core.Community;

/// <summary>
/// Serialises crosshairs for local sharing.
/// <list type="bullet">
/// <item><b>Share code</b> (<c>ESC1:…</c>): compact, copy-pasteable; image layers travel without
/// their pixels (a reticle file would make the code unusably long).</item>
/// <item><b>Share file</b> (.escrosshair JSON): self-contained; custom images are embedded and
/// unpacked into the local media folder on import.</item>
/// </list>
/// Decoding never trusts input: size-capped, version-checked, and any failure surfaces as
/// <see cref="FormatException"/> so callers have exactly one error type to handle.
/// </summary>
public sealed class CrosshairShareCodec
{
    public const string CodePrefix = "ESC1:";
    public const string FileExtension = ".escrosshair";

    private const int CurrentVersion = 1;
    private const int MaxDecodedBytes = 32 * 1024 * 1024;
    private const int MaxLayers = 64;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private static readonly HashSet<string> AllowedImageExtensions = [".png", ".gif", ".svg"];

    private readonly string _mediaDirectory;

    public CrosshairShareCodec(string mediaDirectory)
    {
        _mediaDirectory = mediaDirectory;
    }

    public string EncodeShareCode(CrosshairProfile profile)
    {
        var package = CreatePackage(profile, embedImages: false);
        var json = JsonSerializer.SerializeToUtf8Bytes(package, JsonOptions);

        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(json);
        }

        return CodePrefix + Base64UrlEncode(output.ToArray());
    }

    public CrosshairProfile DecodeShareCode(string code)
    {
        var trimmed = code?.Trim() ?? "";
        if (!trimmed.StartsWith(CodePrefix, StringComparison.Ordinal))
        {
            throw new FormatException("Kein gültiger easySkills-Share-Code.");
        }

        try
        {
            var compressed = Base64UrlDecode(trimmed[CodePrefix.Length..]);
            using var input = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress);
            using var limited = new MemoryStream();
            CopyCapped(input, limited, MaxDecodedBytes);
            return OpenPackage(JsonSerializer.Deserialize<SharePackage>(limited.ToArray(), JsonOptions));
        }
        catch (Exception ex) when (ex is not FormatException)
        {
            throw new FormatException("Der Share-Code ist beschädigt oder unvollständig.", ex);
        }
    }

    public async Task ExportFileAsync(CrosshairProfile profile, string path, CancellationToken cancellationToken = default)
    {
        var package = CreatePackage(profile, embedImages: true);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, package, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
    }

    public async Task<CrosshairProfile> ImportFileAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            if (new FileInfo(path).Length > MaxDecodedBytes)
            {
                throw new FormatException("Die Datei ist zu groß.");
            }

            await using var stream = File.OpenRead(path);
            var package = await JsonSerializer.DeserializeAsync<SharePackage>(stream, JsonOptions, cancellationToken);
            return OpenPackage(package);
        }
        catch (Exception ex) when (ex is not FormatException and not OperationCanceledException)
        {
            throw new FormatException("Die Datei ist keine gültige Fadenkreuz-Datei.", ex);
        }
    }

    private static SharePackage CreatePackage(CrosshairProfile profile, bool embedImages)
    {
        var copy = profile.Clone();
        var images = new List<EmbeddedImage?>(copy.Layers.Count);

        foreach (var layer in copy.Layers)
        {
            EmbeddedImage? embedded = null;
            if (embedImages && layer.Type == LayerType.Image && layer.ImagePath is { } imagePath && File.Exists(imagePath))
            {
                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                if (AllowedImageExtensions.Contains(extension))
                {
                    embedded = new EmbeddedImage(extension, Convert.ToBase64String(File.ReadAllBytes(imagePath)));
                }
            }

            // Never leak local file-system paths into shared data.
            layer.ImagePath = null;
            images.Add(embedded);
        }

        return new SharePackage(CurrentVersion, copy, images);
    }

    private CrosshairProfile OpenPackage(SharePackage? package)
    {
        if (package?.Profile is null || package.Version is < 1 or > CurrentVersion)
        {
            throw new FormatException("Unbekanntes oder beschädigtes Fadenkreuz-Format.");
        }

        var profile = package.Profile;
        profile.Layers ??= [];
        profile.DynamicReactions ??= new DynamicReactionSettings();

        if (profile.Layers.Count > MaxLayers)
        {
            throw new FormatException("Zu viele Ebenen.");
        }

        for (var i = 0; i < profile.Layers.Count; i++)
        {
            var layer = profile.Layers[i];
            layer.ImagePath = null;

            if (layer.Type == LayerType.Image && package.Images is { } images && i < images.Count && images[i] is { } image)
            {
                layer.ImagePath = WriteMedia(image);
            }
        }

        return profile;
    }

    /// <summary>Content-addressed: the same image imported twice is stored once.</summary>
    private string? WriteMedia(EmbeddedImage image)
    {
        var extension = image.Extension?.ToLowerInvariant() ?? "";
        if (!AllowedImageExtensions.Contains(extension)) return null;

        var bytes = Convert.FromBase64String(image.Base64Data ?? "");
        if (bytes.Length == 0) return null;

        Directory.CreateDirectory(_mediaDirectory);
        var name = Convert.ToHexString(SHA256.HashData(bytes))[..32] + extension;
        var path = Path.Combine(_mediaDirectory, name);
        if (!File.Exists(path))
        {
            File.WriteAllBytes(path, bytes);
        }

        return path;
    }

    private static void CopyCapped(Stream source, Stream destination, int maxBytes)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            total += read;
            if (total > maxBytes) throw new FormatException("Der Share-Code ist zu groß.");
            destination.Write(buffer, 0, read);
        }
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string text)
    {
        var s = text.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        return Convert.FromBase64String(s);
    }

    private sealed record SharePackage(int Version, CrosshairProfile? Profile, List<EmbeddedImage?>? Images);

    private sealed record EmbeddedImage(string? Extension, string? Base64Data);
}
