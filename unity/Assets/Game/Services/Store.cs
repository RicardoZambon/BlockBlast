#nullable enable
using System;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// The one purchase, Remove Ads (remove_ads_299, non-consumable, master plan 3.6). A null answer means the store
    /// couldn't say (offline, or not ready), and the app keeps what it saved.
    /// </summary>
    public interface IStore
    {
        /// <summary>The price as the store shows it in the player's currency, once known.</summary>
        string? RemoveAdsPrice { get; }

        /// <summary>Starts the store at launch and re-checks ownership.</summary>
        void Initialize(Action<bool?> owned);

        void BuyRemoveAds(Action<bool> succeeded);

        /// <summary>Restore Purchases, which Apple requires for non-consumables.</summary>
        void Restore(Action<bool?> owned);
    }

    /// <summary>Until Unity IAP and the store products exist (3.6, with your consoles): no answers, and buying fails.</summary>
    public sealed class StandInStore : IStore
    {
        public string? RemoveAdsPrice => null;

        public void Initialize(Action<bool?> owned) => owned(null);

        public void BuyRemoveAds(Action<bool> succeeded) => succeeded(false);

        public void Restore(Action<bool?> owned) => owned(null);
    }
}
