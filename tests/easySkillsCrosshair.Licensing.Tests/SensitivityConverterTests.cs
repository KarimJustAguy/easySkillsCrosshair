using System.Text;
using easySkillsCrosshair.Core.Sensitivity;
using Xunit;

namespace easySkillsCrosshair.Licensing.Tests;

public class SensitivityConverterTests
{
    private const double CsYaw = 0.022;
    private const double ValorantYaw = 0.07;
    private const double OverwatchYaw = 0.0066;

    [Fact]
    public void Cm360_Matches_Known_Reference_Value()
    {
        // CS 1.0 @ 800 DPI: 360 / 0.022 = 16363.6 counts → / 800 · 2.54 ≈ 51.95 cm
        Assert.Equal(51.95, SensitivityMath.CentimetresPer360(1.0, CsYaw, 800), 2);
        Assert.Equal(20.45, SensitivityMath.InchesPer360(1.0, CsYaw, 800), 2);
    }

    [Fact]
    public void Valorant_To_CS_Uses_Established_Ratio()
    {
        // Community rule of thumb: CS = Valorant × 3.18
        Assert.Equal(0.35 * 3.1818, SensitivityMath.Convert(0.35, ValorantYaw, 800, CsYaw, 800), 3);
        Assert.Equal(3.333, SensitivityMath.Convert(1.0, CsYaw, 800, OverwatchYaw, 800), 3);
    }

    [Fact]
    public void Conversion_Preserves_360_Distance_Including_Dpi_Change()
    {
        var converted = SensitivityMath.Convert(0.4, ValorantYaw, 1600, CsYaw, 800);

        Assert.Equal(
            SensitivityMath.CentimetresPer360(0.4, ValorantYaw, 1600),
            SensitivityMath.CentimetresPer360(converted, CsYaw, 800),
            6);
    }

    [Theory]
    [InlineData(0, 0.022, 800)]
    [InlineData(-1, 0.022, 800)]
    [InlineData(1, 0, 800)]
    [InlineData(1, 0.022, 0)]
    [InlineData(double.NaN, 0.022, 800)]
    public void Invalid_Inputs_Yield_Zero_Instead_Of_Infinity(double sens, double yaw, double dpi)
    {
        Assert.Equal(0, SensitivityMath.CentimetresPer360(sens, yaw, dpi));
        Assert.Equal(0, SensitivityMath.Convert(sens, yaw, dpi, CsYaw, 800));
    }

    [Fact]
    public void Embedded_Catalog_Loads_With_Expected_Games()
    {
        var games = GameCatalog.LoadEmbedded();

        Assert.Contains(games, g => g.Id == "valorant" && g.Yaw == ValorantYaw);
        Assert.Contains(games, g => g.Id == "counter-strike-2" && g.Yaw == CsYaw);
        Assert.Contains(games, g => g.Id == "overwatch-2" && g.Yaw == OverwatchYaw);
        Assert.Equal(games.Count, games.Select(g => g.Id).Distinct().Count());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"games\": []}")]
    [InlineData("{\"games\": [{\"id\":\"a\",\"name\":\"A\",\"yaw\":0}]}")]
    [InlineData("{\"games\": [{\"id\":\"a\",\"name\":\"A\",\"yaw\":1},{\"id\":\"A\",\"name\":\"B\",\"yaw\":1}]}")]
    [InlineData("{\"games\": [{\"id\":\"custom\",\"name\":\"X\",\"yaw\":1}]}")]
    public void Invalid_Catalog_Throws_FormatException(string json)
    {
        Assert.Throws<FormatException>(() => GameCatalog.Parse(new MemoryStream(Encoding.UTF8.GetBytes(json))));
    }

    [Fact]
    public void Trial_Set_Is_The_Core_Games_And_Pro_Extends_It()
    {
        var games = GameCatalog.LoadEmbedded();
        var trialIds = games.Where(g => !g.IsProOnly).Select(g => g.Id).ToHashSet();

        Assert.Equal(12, trialIds.Count);
        Assert.Contains("valorant", trialIds);
        Assert.Contains("counter-strike-2", trialIds);
        Assert.Contains("fortnite", trialIds);
        Assert.Contains(games, g => g.IsProOnly);
    }

    [Fact]
    public void Missing_Tier_Defaults_To_Pro_So_Manual_Entries_Never_Widen_Trial()
    {
        var json = "{\"games\": [{\"id\":\"a\",\"name\":\"A\",\"yaw\":1},{\"id\":\"b\",\"name\":\"B\",\"yaw\":1,\"tier\":\"trial\",\"verified\":true}]}";

        var games = GameCatalog.Parse(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        Assert.True(games.Single(g => g.Id == "a").IsProOnly);
        Assert.False(games.Single(g => g.Id == "b").IsProOnly);
        Assert.True(games.Single(g => g.Id == "b").IsVerified);
    }

    [Fact]
    public void Unknown_Tier_Is_Rejected()
    {
        var json = "{\"games\": [{\"id\":\"a\",\"name\":\"A\",\"yaw\":1,\"tier\":\"gold\"}]}";

        Assert.Throws<FormatException>(() => GameCatalog.Parse(new MemoryStream(Encoding.UTF8.GetBytes(json))));
    }

    [Fact]
    public void Broken_Editable_File_Falls_Back_To_Embedded_With_Warning()
    {
        var path = Path.Combine(Path.GetTempPath(), $"games-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ broken");
        try
        {
            var result = GameCatalog.Load(path);

            Assert.NotNull(result.Warning);
            Assert.Contains(result.Games, g => g.Id == "valorant");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
