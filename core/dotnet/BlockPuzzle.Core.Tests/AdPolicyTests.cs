namespace BlockPuzzle.Core.Tests;

public class AdPolicyTests
{
    static readonly AdPolicySettings Defaults = new();

    [Fact]
    public void No_interstitials_during_the_first_three_games()
    {
        Assert.False(AdPolicy.CanShowInterstitial(Defaults, ownsRemoveAds: false, gamesCompleted: 2, secondsSinceLastFullScreenAd: null, betweenGames: true));
        Assert.True(AdPolicy.CanShowInterstitial(Defaults, ownsRemoveAds: false, gamesCompleted: 3, secondsSinceLastFullScreenAd: null, betweenGames: true));
    }

    [Fact]
    public void The_cooldown_is_respected()
    {
        Assert.False(AdPolicy.CanShowInterstitial(Defaults, false, 10, secondsSinceLastFullScreenAd: 89.9, betweenGames: true));
        Assert.True(AdPolicy.CanShowInterstitial(Defaults, false, 10, secondsSinceLastFullScreenAd: 90, betweenGames: true));
    }

    [Fact]
    public void Never_during_a_game()
    {
        Assert.False(AdPolicy.CanShowInterstitial(Defaults, false, 10, null, betweenGames: false));
    }

    [Fact]
    public void Remove_Ads_and_the_kill_switch_block_interstitials()
    {
        Assert.False(AdPolicy.CanShowInterstitial(Defaults, ownsRemoveAds: true, 10, null, true));
        Assert.False(AdPolicy.CanShowInterstitial(new AdPolicySettings { InterstitialsEnabled = false }, false, 10, null, true));
    }
}
