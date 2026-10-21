#nullable enable
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>The project settings the master plan requires, as ProjectSetup.ApplySettings writes them.</summary>
    public class ProjectSettingsTests
    {
        [Test]
        public void Settings_match_the_master_plan()
        {
            Assert.AreEqual("Block Puzzle", PlayerSettings.productName);
            Assert.AreEqual("yourstudio", PlayerSettings.companyName);
            Assert.AreEqual("com.yourstudio.blockpuzzle", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            Assert.AreEqual("com.yourstudio.blockpuzzle", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS));

            Assert.AreEqual(UIOrientation.Portrait, PlayerSettings.defaultInterfaceOrientation);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortraitUpsideDown);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToLandscapeRight);

            Assert.AreEqual(AndroidSdkVersions.AndroidApiLevel25, PlayerSettings.Android.minSdkVersion);   // Unity 6.3 supports nothing lower
            Assert.AreEqual((AndroidSdkVersions)36, PlayerSettings.Android.targetSdkVersion);
            Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android));
            Assert.AreEqual(AndroidArchitecture.ARM64, PlayerSettings.Android.targetArchitectures);
            Assert.AreEqual("15.0", PlayerSettings.iOS.targetOSVersionString);
            Assert.IsFalse(PlayerSettings.SplashScreen.show);

            var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            Assert.AreEqual(1, playerSettings.FindProperty("activeInputHandler").intValue, "Input System package only");

            // Nothing collects data before the consent flow exists (master plan, Privacy).
            Assert.IsFalse(playerSettings.FindProperty("submitAnalytics").boolValue, "hardware statistics");
            Assert.IsFalse(UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled, "engine diagnostics");
            StringAssert.DoesNotContain("analytics", File.ReadAllText("Packages/manifest.json"));

            CollectionAssert.AreEqual(new[] { "Assets/Game/Scenes/Game.unity" }, EditorBuildSettings.scenes.Select(scene => scene.path).ToArray());
        }

        [System.Serializable]
        sealed class NotificationSettingsFile
        {
            public NotificationSettingsBody MonoBehaviour = new NotificationSettingsBody();
        }

        [System.Serializable]
        sealed class NotificationSettingsBody
        {
            public KeyValues m_iOSNotificationSettingsValues = new KeyValues();
        }

        [System.Serializable]
        sealed class KeyValues
        {
            public string[] m_Keys = new string[0];
            public string[] m_Values = new string[0];

            public string Get(string key) => m_Values[System.Array.IndexOf(m_Keys, key)];
        }

        /// <summary>Master plan 3.7: notification permission is asked for after the first Daily, never at launch.</summary>
        [Test]
        public void Notifications_are_never_asked_for_at_launch()
        {
            var settings = UnityEngine.JsonUtility.FromJson<NotificationSettingsFile>(File.ReadAllText("ProjectSettings/NotificationsSettings.asset"));

            Assert.AreEqual("False", settings.MonoBehaviour.m_iOSNotificationSettingsValues.Get("UnityNotificationRequestAuthorizationOnAppLaunch"));
        }
    }
}
