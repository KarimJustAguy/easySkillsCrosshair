using easySkillsCrosshair.Core.Community;
using easySkillsCrosshair.Core.Crosshair;
using Xunit;

namespace easySkillsCrosshair.Licensing.Tests;

public sealed class CrosshairShareCodecTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "esc-codec-" + Guid.NewGuid().ToString("N"));

    public CrosshairShareCodecTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private CrosshairShareCodec Codec() => new(Path.Combine(_root, "media"));

    private static CrosshairProfile Sample() => new()
    {
        Name = "Sample",
        RotationDegrees = 12,
        Layers =
        [
            new CrosshairLayer { Type = LayerType.Cross, Length = 7, Gap = 4, TStyle = true, OutlineThickness = 1 },
            new CrosshairLayer { Type = LayerType.Dot, Color = RgbaColor.FromHex("#FF1744"), Length = 3 },
        ],
        DynamicReactions = new DynamicReactionSettings { BloomOnFire = true, BloomAmount = 9 },
    };

    [Fact]
    public void ShareCode_RoundTrips_Layers_And_Reactions()
    {
        var codec = Codec();

        var code = codec.EncodeShareCode(Sample());
        var decoded = codec.DecodeShareCode(code);

        Assert.StartsWith(CrosshairShareCodec.CodePrefix, code);
        Assert.Equal("Sample", decoded.Name);
        Assert.Equal(2, decoded.Layers.Count);
        Assert.True(decoded.Layers[0].TStyle);
        Assert.Equal(RgbaColor.FromHex("#FF1744"), decoded.Layers[1].Color);
        Assert.Equal(9, decoded.DynamicReactions.BloomAmount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("ESC1:not-base64!!")]
    [InlineData("ESC1:AAAA")]
    public void Invalid_Codes_Throw_FormatException_Only(string code)
    {
        Assert.Throws<FormatException>(() => Codec().DecodeShareCode(code));
    }

    [Fact]
    public void ShareCode_Never_Contains_Local_Image_Paths()
    {
        var codec = Codec();
        var profile = Sample();
        profile.Layers.Add(new CrosshairLayer { Type = LayerType.Image, ImagePath = @"C:\Users\secret\reticle.png" });

        var decoded = codec.DecodeShareCode(codec.EncodeShareCode(profile));

        Assert.All(decoded.Layers, l => Assert.Null(l.ImagePath));
    }

    [Fact]
    public async Task File_Export_Embeds_Image_And_Import_Restores_It()
    {
        var codec = Codec();
        var imagePath = Path.Combine(_root, "reticle.png");
        byte[] pixels = [0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4];
        await File.WriteAllBytesAsync(imagePath, pixels);

        var profile = Sample();
        profile.Layers.Add(new CrosshairLayer { Type = LayerType.Image, ImagePath = imagePath });
        var sharePath = Path.Combine(_root, "shared" + CrosshairShareCodec.FileExtension);

        await codec.ExportFileAsync(profile, sharePath);
        var imported = await codec.ImportFileAsync(sharePath);

        var restored = imported.Layers[2].ImagePath;
        Assert.NotNull(restored);
        Assert.NotEqual(imagePath, restored);
        Assert.Equal(pixels, await File.ReadAllBytesAsync(restored!));
        Assert.DoesNotContain(_root.Replace("\\", "\\\\"), await File.ReadAllTextAsync(sharePath));
    }
}
