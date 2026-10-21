#nullable enable
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>Remove Ads and Restore Purchases through App, with a fake store (master plan 3.6).</summary>
    public class PurchaseFlowTests
    {
        sealed class FakeStore : IStore
        {
            public bool? OwnedAtLaunch = false;
            public bool BuySucceeds = true;
            public bool? OwnedOnRestore = true;
            public string? Price = "US$2.99";
            public int Buys;

            public string? RemoveAdsPrice => Price;
            public void Initialize(Action<bool?> owned) => owned(OwnedAtLaunch);

            public void BuyRemoveAds(Action<bool> succeeded)
            {
                Buys++;
                succeeded(BuySucceeds);
            }

            public void Restore(Action<bool?> owned) => owned(OwnedOnRestore);
        }

        sealed class FakeAds : IAdService
        {
            public bool BannerShown;
            bool _initialized;
            public void Initialize() => _initialized = true;
            public bool IsInterstitialReady => _initialized;
            public bool IsRewardedReady => false;
            public void ShowInterstitial(Action closed) => closed();
            public void ShowRewarded(Action<bool> finished) => finished(false);
            public void ShowBanner() => BannerShown = true;
            public void HideBanner() => BannerShown = false;
        }

        string _folder = "";
        GameObject? _camera;
        App? _app;
        FakeStore _store = new FakeStore();
        FakeAds _ads = new FakeAds();

        string SavePath => Path.Combine(_folder, SaveStore.FileName);

        [SetUp]
        public void SetUp()
        {
            _store = new FakeStore();
            _ads = new FakeAds();
            _folder = Path.Combine(Path.GetTempPath(), "blockpuzzle-purchases-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _camera = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = _camera.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8;
            _camera.transform.position = new Vector3(0, 0, -10);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_app != null) Object.Destroy(_app.gameObject);
            if (_camera != null) Object.Destroy(_camera);
            yield return null;
            if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
        }

        App Boot(SaveData save)
        {
            new SaveStore(SavePath).Write(save);
            _app = new GameObject("App").AddComponent<App>();
            _app.Boot(_folder, () => new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc), _ => { }, Language.English,
                new StandInConsent(), _ads, new StandInReviewPrompt(), new StandInReminders(), new StandInAnalytics(), _store);
            return _app;
        }

        static bool SavedOwnership(string path) => new SaveStore(path).TryRead(out var saved) && saved.OwnsRemoveAds;

        [UnityTest]
        public IEnumerator Buying_remove_ads_stops_interstitials_and_the_banner()
        {
            var app = Boot(new SaveData { TutorialDone = true, NextGameIndex = 10 });
            yield return null;
            Assert.IsTrue(app.InterstitialAllowedNow(), "past the grace games");
            Assert.AreEqual("Remove ads · US$2.99", app.Settings.RemoveAdsLabel);

            app.Settings.PressRemoveAds();

            Assert.AreEqual(1, _store.Buys);
            Assert.IsTrue(SavedOwnership(SavePath));
            Assert.IsFalse(app.InterstitialAllowedNow());
            Assert.AreEqual("Ads removed", app.Settings.RemoveAdsLabel);
            Assert.IsFalse(app.Settings.RemoveAdsEnabled);
            app.Home.PressPlay();
            yield return null;
            Assert.IsFalse(_ads.BannerShown, "no banner once ads are removed");
        }

        [UnityTest]
        public IEnumerator A_failed_purchase_changes_nothing()
        {
            _store.BuySucceeds = false;
            var app = Boot(new SaveData { TutorialDone = true });
            yield return null;

            app.Settings.PressRemoveAds();

            Assert.IsFalse(SavedOwnership(SavePath));
            Assert.IsTrue(app.Settings.RemoveAdsEnabled);
        }

        [UnityTest]
        public IEnumerator Restore_brings_remove_ads_back()
        {
            var app = Boot(new SaveData { TutorialDone = true });
            yield return null;

            app.Settings.PressRestore();

            Assert.IsTrue(SavedOwnership(SavePath));
            Assert.AreEqual("Ads removed", app.Settings.RemoveAdsLabel);
        }

        [UnityTest]
        public IEnumerator Every_launch_rechecks_ownership_but_keeps_it_when_the_store_cannot_answer()
        {
            _store.OwnedAtLaunch = false;   // refunded since the last launch
            Boot(new SaveData { TutorialDone = true, OwnsRemoveAds = true });
            yield return null;
            Assert.IsFalse(SavedOwnership(SavePath));

            Object.DestroyImmediate(_app!.gameObject);
            _store.OwnedAtLaunch = null;    // offline: no answer
            Boot(new SaveData { TutorialDone = true, OwnsRemoveAds = true });
            yield return null;
            Assert.IsTrue(SavedOwnership(SavePath));
        }
    }
}
