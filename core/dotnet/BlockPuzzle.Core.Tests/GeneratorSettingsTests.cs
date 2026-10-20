using System.Text.Json;

namespace BlockPuzzle.Core.Tests;

public class GeneratorSettingsTests
{
    public static TheoryData<string> ProfileNames => new(GeneratorSettings.ProfileNames);

    [Theory]
    [MemberData(nameof(ProfileNames))]
    public void Presets_are_valid(string name)
    {
        var settings = GeneratorSettings.ForProfile(name);

        settings.Validate();
        Assert.Equal(name, settings.Name);
    }

    [Fact]
    public void ProfileNames_lists_the_presets_from_easiest_to_hardest()
    {
        Assert.Equal(new[] { "gentle", "baseline", "hard" }, GeneratorSettings.ProfileNames);
    }

    [Fact]
    public void ProfileNames_cannot_be_changed_through_a_cast()
    {
        Assert.Throws<NotSupportedException>(() => ((IList<string>)GeneratorSettings.ProfileNames)[0] = "gentle");
    }

    [Fact]
    public void An_unknown_profile_throws()
    {
        Assert.Throws<ArgumentException>(() => GeneratorSettings.ForProfile("nope"));
    }

    [Fact]
    public void BandFor_picks_the_last_band_that_has_started()
    {
        var settings = new GeneratorSettings
        {
            Bands = new[]
            {
                new DifficultyBand(0, 0, 1, 0, 0),
                new DifficultyBand(30, 0, 0, 1, 0),
                new DifficultyBand(90, 0, 0, 0, 1),
            },
        };

        Assert.Same(settings.Bands[0], settings.BandFor(0));
        Assert.Same(settings.Bands[0], settings.BandFor(29));
        Assert.Same(settings.Bands[1], settings.BandFor(30));
        Assert.Same(settings.Bands[2], settings.BandFor(500));
    }

    [Fact]
    public void Validate_rejects_settings_that_cannot_work()
    {
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { Bands = Array.Empty<DifficultyBand>() }.Validate());
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { Bands = new[] { new DifficultyBand(5, 0, 1, 1, 1) } }.Validate());
        Assert.Throws<ArgumentException>(() => new GeneratorSettings
        {
            Bands = new[] { new DifficultyBand(0, 0, 1, 1, 1), new DifficultyBand(0, 0, 1, 1, 1) },
        }.Validate());
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { Bands = new[] { new DifficultyBand(0, 0, -1, 1, 1) } }.Validate());
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { Bands = new[] { new DifficultyBand(0, 0, 0, 0, 0) } }.Validate());
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { HelpfulChancePercent = 101 }.Validate());
    }

    [Fact]
    public void Validate_rejects_a_missing_band()
    {
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { Bands = new DifficultyBand[] { null! } }.Validate());
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { Bands = new[] { new DifficultyBand(0, 0, 1, 1, 1), null! } }.Validate());
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { Mercy = null! }.Validate());
    }

    [Fact]
    public void Validate_rejects_weights_too_large_to_add_up()
    {
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { Bands = new[] { new DifficultyBand(0, 0, int.MaxValue, 1, 0) } }.Validate());
    }

    [Fact]
    public void Validate_rejects_an_undefined_fit_guarantee()
    {
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { FitGuarantee = (FitGuarantee)22 }.Validate());
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { FitGuarantee = (FitGuarantee)(-1) }.Validate());
    }

    [Fact]
    public void New_settings_default_to_the_baseline_profile()
    {
        var json = new JsonSerializerOptions { IncludeFields = true };

        Assert.Equal(JsonSerializer.Serialize(GeneratorSettings.Baseline(), json), JsonSerializer.Serialize(new GeneratorSettings(), json));
    }

    [Fact]
    public void Validate_caps_the_whole_tray_attempts()
    {
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { MaxWholeTrayAttempts = 101 }.Validate());
        new GeneratorSettings { MaxWholeTrayAttempts = 100 }.Validate();
    }
}
