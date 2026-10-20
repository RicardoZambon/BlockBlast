#nullable enable
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BlockPuzzle.Game.EditorTools
{
    /// <summary>
    /// Project settings, the game scene and the Android build, from the "Block Puzzle" menu or in batch mode with
    /// -executeMethod BlockPuzzle.Game.EditorTools.ProjectSetup.ApplySettings (or BuildScene, BuildAndroidDevelopment).
    /// </summary>
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Game/Scenes/Game.unity";
        public const string AndroidBuildPath = "Builds/Android/blockpuzzle-dev.apk";
        const string ApplicationId = "com.yourstudio.blockpuzzle";   // until task 0.2 picks the real name

        [MenuItem("Block Puzzle/Apply project settings")]
        public static void ApplySettings()
        {
            PlayerSettings.companyName = "yourstudio";
            PlayerSettings.productName = "Block Puzzle";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, ApplicationId);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;   // Android 7.1: the lowest Unity 6.3 supports
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.SplashScreen.show = false;

            // No public API for these two: Input System package only (1), and no hardware statistics sent to Unity.
            var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            playerSettings.FindProperty("activeInputHandler").intValue = 1;
            playerSettings.FindProperty("submitAnalytics").boolValue = false;
            playerSettings.ApplyModifiedProperties();

            // Nothing collects data before the consent flow exists (master plan, Privacy).
            UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled = false;

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        /// <summary>Generates the one scene: an orthographic camera, a global 2D light and the App, which builds the rest.</summary>
        [MenuItem("Block Puzzle/Build game scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Palette.Background;
            cameraObject.transform.position = new Vector3(0, 0, -10);

            // The 2D renderer lights sprites; one global light shows their colours as they are.
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;

            new GameObject("App").AddComponent<App>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.DeleteAsset("Assets/Scenes");   // the template's SampleScene, replaced by Game.unity
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Block Puzzle/Build Android (development)")]
        public static void BuildAndroidDevelopment()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = AndroidBuildPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            });
            var summary = report.summary;
            Debug.Log($"Android build {summary.result}: {summary.totalSize / (1024f * 1024f):F1} MB in {summary.totalTime}");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
