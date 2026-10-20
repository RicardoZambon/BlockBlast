#nullable enable
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

            CollectionAssert.AreEqual(new[] { "Assets/Game/Scenes/Game.unity" }, EditorBuildSettings.scenes.Select(scene => scene.path).ToArray());
        }
    }
}
