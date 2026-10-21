#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>
    /// The app side of consent (master plan 3.2), through App with fakes: what the flow found reaches analytics before
    /// its first event and goes into consent_result, and Settings offers Privacy Settings where the region requires it.
    /// </summary>
    public class ConsentFlowTests
    {
        sealed class FakeConsent : IConsentFlow
        {
            public ConsentResult Result = new ConsentResult(true, "denied", analyticsStorage: true, adStorage: false, adUserData: false, adPersonalization: false);
            public ConsentResult ChangedResult = new ConsentResult(true, "denied", analyticsStorage: false, adStorage: false, adUserData: false, adPersonalization: false);
            public bool Required = true;
            public int OptionsShown;

            public bool PrivacyOptionsRequired => Required;

            public void Request(Action<ConsentResult> finished) => finished(Result);

            public void ShowPrivacyOptions(Action<ConsentResult> closed)
            {
                OptionsShown++;
                closed(ChangedResult);
            }
        }

        /// <summary>Records consent changes and events in one list, in the order they arrive.</summary>
        sealed class FakeAnalytics : IAnalytics
        {
            public readonly List<object> Calls = new List<object>();

            public void SetConsent(ConsentResult consent) => Calls.Add(consent);

            public void Log(string name, IReadOnlyDictionary<string, object>? parameters = null) =>
                Calls.Add((name, parameters ?? new Dictionary<string, object>()));

            public IReadOnlyDictionary<string, object> Last(string name) =>
                Calls.OfType<(string name, IReadOnlyDictionary<string, object> parameters)>().Last(e => e.name == name).parameters;

            public int Count(string name) => Calls.OfType<(string name, IReadOnlyDictionary<string, object> parameters)>().Count(e => e.name == name);
        }

        string _folder = "";
        GameObject? _camera;
        App? _app;
        FakeConsent _consent = new FakeConsent();
        FakeAnalytics _analytics = new FakeAnalytics();

        [SetUp]
        public void SetUp()
        {
            _consent = new FakeConsent();
            _analytics = new FakeAnalytics();
            _folder = Path.Combine(Path.GetTempPath(), "blockpuzzle-consent-" + Guid.NewGuid().ToString("N"));
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

        App Boot()
        {
            _app = new GameObject("App").AddComponent<App>();
            _app.Boot(_folder, null, _ => { }, Language.English, _consent, new StandInAds(), new StandInReviewPrompt(), new StandInReminders(), _analytics);
            return _app;
        }

        [UnityTest]
        public IEnumerator The_consent_choices_reach_analytics_before_its_first_event()
        {
            Boot();
            yield return null;

            Assert.AreSame(_consent.Result, _analytics.Calls[0]);
            Assert.AreEqual("consent_result", (((string name, IReadOnlyDictionary<string, object> parameters))_analytics.Calls[1]).name);
        }

        [UnityTest]
        public IEnumerator Consent_result_says_whether_GDPR_applies_and_what_the_player_told_Apple()
        {
            Boot();
            yield return null;

            var result = _analytics.Last("consent_result");
            Assert.AreEqual("true", result["gdpr_applies"]);
            Assert.AreEqual("denied", result["att_status"]);
        }

        [UnityTest]
        public IEnumerator Settings_offers_privacy_settings_where_the_region_requires_it()
        {
            var app = Boot();
            yield return null;

            Assert.IsTrue(app.Settings.PrivacySettingsShown);
        }

        [UnityTest]
        public IEnumerator Settings_hides_privacy_settings_elsewhere()
        {
            _consent.Required = false;
            var app = Boot();
            yield return null;

            Assert.IsFalse(app.Settings.PrivacySettingsShown);
            app.Settings.PressPrivacySettings();
            Assert.AreEqual(0, _consent.OptionsShown);
        }

        [UnityTest]
        public IEnumerator Privacy_settings_reopens_the_form_and_passes_on_the_new_choices()
        {
            var app = Boot();
            yield return null;

            app.Settings.PressPrivacySettings();

            Assert.AreEqual(1, _consent.OptionsShown);
            Assert.AreSame(_consent.ChangedResult, _analytics.Calls.OfType<ConsentResult>().Last());
            Assert.AreEqual(2, _analytics.Count("consent_result"));
        }
    }
}
