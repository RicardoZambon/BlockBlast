#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// Settings: sound, music, haptics and reduced motion, and the version. Each change is written into the shared
    /// SettingsData and reported at once, so it's saved before anything can kill the app. Tasks 2.2 and 2.9 make the
    /// toggles do something.
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        readonly Dictionary<string, Toggle> _toggles = new Dictionary<string, Toggle>();
        Text? _version;
        Action? _changed;
        Action? _back;

        /// <param name="removeAds">Buys Remove Ads; with restore, adds the two purchase buttons (master plan 3.6).</param>
        public static SettingsScreen Create(Transform parent, SettingsData settings, Action changed, Action back,
            Action? removeAds = null, Action? restore = null)
        {
            var canvas = UiFactory.Canvas(parent, "Settings");
            var screen = canvas.gameObject.AddComponent<SettingsScreen>();
            screen._changed = changed;
            screen._back = back;
            screen._removeAds = removeAds;
            screen._restore = restore;
            var safe = SafeAreaPanel.Create(canvas);
            var top = new Vector2(0.5f, 1f);
            var bottom = new Vector2(0.5f, 0f);

            UiFactory.Text(UiFactory.Rect(safe, "Title", top, new Vector2(1000, 160), new Vector2(0, -200)), 100, Color.white).text = Strings.Get("settings.title");
            screen.AddToggle(safe, "sound", 0, settings.Sound, on => settings.Sound = on);
            screen.AddToggle(safe, "music", 1, settings.Music, on => settings.Music = on);
            screen.AddToggle(safe, "haptics", 2, settings.Haptics, on => settings.Haptics = on);
            screen.AddToggle(safe, "reduced_motion", 3, settings.ReducedMotion, on => settings.ReducedMotion = on);

            screen._version = UiFactory.Text(UiFactory.Rect(safe, "Version", bottom, new Vector2(900, 100), new Vector2(0, 320)), 44, new Color(1f, 1f, 1f, 0.6f));
            screen._version.text = Strings.Format("settings.version", Application.version);
            UiFactory.Button(safe, "Back", Strings.Get("settings.back"), bottom, new Vector2(600, 170), new Vector2(0, 160), screen.PressBack);
            if (removeAds != null && restore != null)
            {
                screen._removeAdsButton = UiFactory.Button(safe, "Remove ads", Strings.Get("settings.remove_ads"), bottom, new Vector2(700, 150), new Vector2(0, 700), screen.PressRemoveAds);
                UiFactory.Button(safe, "Restore", Strings.Get("settings.restore"), bottom, new Vector2(700, 150), new Vector2(0, 520), screen.PressRestore);
            }
            return screen;
        }

        public string VersionText => _version != null ? _version.text : "";

        /// <param name="id">"sound", "music", "haptics" or "reduced_motion": the same in every language.</param>
        public bool IsOn(string id) => _toggles[id].isOn;

        /// <summary>Sets a toggle the way a tap does, so the change is stored and reported.</summary>
        public void SetToggle(string id, bool on) => _toggles[id].isOn = on;

        /// <summary>What the Back button does; tests call it directly.</summary>
        public void PressBack() => _back?.Invoke();

        Action? _removeAds;
        Action? _restore;
        Button? _removeAdsButton;

        public string RemoveAdsLabel => _removeAdsButton != null ? _removeAdsButton.GetComponentInChildren<Text>().text : "";

        public bool RemoveAdsEnabled => _removeAdsButton != null && _removeAdsButton.interactable;

        /// <summary>"Remove ads", with the price once the store knows it, or "Ads removed", disabled, once owned.</summary>
        public void ShowRemoveAds(bool owned, string? price)
        {
            if (_removeAdsButton == null) return;
            _removeAdsButton.GetComponentInChildren<Text>().text = owned ? Strings.Get("settings.ads_removed")
                : price != null ? Strings.Format("settings.remove_ads_price", price)
                : Strings.Get("settings.remove_ads");
            _removeAdsButton.interactable = !owned;
        }

        /// <summary>What the Remove ads button does; like the button, nothing once owned.</summary>
        public void PressRemoveAds()
        {
            if (RemoveAdsEnabled) _removeAds?.Invoke();
        }

        /// <summary>What the Restore purchases button does; tests call it directly.</summary>
        public void PressRestore() => _restore?.Invoke();

        /// <summary>A row the whole width of which toggles: the label on the left, a box with a check on the right.</summary>
        void AddToggle(Transform parent, string id, int index, bool on, Action<bool> store)
        {
            var row = UiFactory.Rect(parent, id, new Vector2(0.5f, 1f), new Vector2(900, 150), new Vector2(0, -420 - index * 180));
            row.gameObject.AddComponent<Image>().color = Color.clear;   // makes the label tappable too
            var label = UiFactory.Text(UiFactory.Rect(row, "Label", new Vector2(0f, 0.5f), new Vector2(700, 150), new Vector2(350, 0)), 64, Color.white);
            label.alignment = TextAnchor.MiddleLeft;
            label.text = Strings.Get("settings." + id);

            var box = UiFactory.Rect(row, "Box", new Vector2(1f, 0.5f), new Vector2(120, 120), new Vector2(-60, 0));
            var background = box.gameObject.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.15f);
            var check = UiFactory.Rect(box, "Check", new Vector2(0.5f, 0.5f), new Vector2(80, 80), Vector2.zero).gameObject.AddComponent<Image>();
            check.color = Palette.Filled;

            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = check;
            toggle.toggleTransition = Toggle.ToggleTransition.None;
            toggle.isOn = on;
            toggle.onValueChanged.AddListener(value =>
            {
                store(value);
                _changed?.Invoke();
            });
            _toggles.Add(id, toggle);
        }
    }
}
