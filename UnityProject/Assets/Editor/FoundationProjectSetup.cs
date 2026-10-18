using System;
using System.IO;
using CrowdCheckpoint.Unity.Foundation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace CrowdCheckpoint.Unity.Editor
{
    /// <summary>S1 setup/build entry points; these have not been executed in Unity yet.</summary>
    public static class FoundationProjectSetup
    {
        public const string ExpectedEditorVersion = "6000.3.26f1";
        public const string ScenePath = "Assets/Scenes/MigrationFoundation.unity";
        public const string PipelinePath = "Assets/Settings/FoundationPipeline.asset";
        public const string RendererPath = "Assets/Settings/FoundationRenderer.asset";

        [MenuItem("Tools/Crowd Checkpoint/Migration/Configure S1 Foundation")]
        public static void Configure()
        {
            RequirePinnedEditor();
            if (!File.Exists(ScenePath))
                throw new InvalidOperationException("The tracked foundation scene is missing.");

            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");

            var renderer = LoadOrRejectDifferentAsset<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = LoadOrRejectDifferentAsset<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int previousQuality = QualitySettings.GetQualityLevel();
            try
            {
                for (int index = 0; index < QualitySettings.names.Length; index++)
                {
                    QualitySettings.SetQualityLevel(index, false);
                    QualitySettings.renderPipeline = pipeline;
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(previousQuality, false);
            }

            PlayerSettings.companyName = "CrowdCheckpointSim";
            PlayerSettings.productName = "Crowd Checkpoint - S1 Foundation";
            PlayerSettings.defaultScreenWidth = 1500;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorSettings.enterPlayModeOptionsEnabled = false;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("S1 foundation configured. Review and commit generated Settings assets and ProjectSettings; this is not a completed simulator.");
        }

        [MenuItem("Tools/Crowd Checkpoint/Migration/Build S1 Windows Foundation")]
        public static void BuildWindows()
        {
            try
            {
                Configure();
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                    throw new InvalidOperationException("Windows x86-64 build support is not installed for the pinned Editor.");

                ValidateScene();
                const string output = "Artifacts/WindowsFoundation/CrowdCheckpointFoundation.exe";
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Foundation Windows build failed: " + report.summary.result);
                Debug.Log("S1 foundation player built at " + Path.GetFullPath(output));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                else
                    throw;
            }
        }

        public static void ValidateScene()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                FoundationBootstrap bootstrap = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    var found = root.GetComponentsInChildren<FoundationBootstrap>(true);
                    foreach (FoundationBootstrap candidate in found)
                    {
                        if (bootstrap != null)
                            throw new InvalidOperationException("Foundation scene has multiple bootstraps.");
                        bootstrap = candidate;
                    }
                }
                if (bootstrap == null || bootstrap.FoundationCamera == null || !bootstrap.FoundationCamera.orthographic)
                    throw new InvalidOperationException("Foundation scene requires one wired bootstrap and an orthographic camera.");
            }
            finally
            {
                if (openedForValidation)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void ValidateConfiguration()
        {
            RequirePinnedEditor();
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (pipeline == null || renderer == null || GraphicsSettings.defaultRenderPipeline != pipeline)
                throw new InvalidOperationException("Foundation URP assets/global assignment are incomplete.");

            int previousQuality = QualitySettings.GetQualityLevel();
            try
            {
                for (int index = 0; index < QualitySettings.names.Length; index++)
                {
                    QualitySettings.SetQualityLevel(index, false);
                    if (QualitySettings.renderPipeline != pipeline)
                        throw new InvalidOperationException("A quality level does not use the foundation pipeline.");
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(previousQuality, false);
            }

            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) != ScriptingImplementation.Mono2x ||
                PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.Standalone) != ApiCompatibilityLevel.NET_Standard ||
                PlayerSettings.defaultScreenWidth != 1500 || PlayerSettings.defaultScreenHeight != 900 ||
                PlayerSettings.fullScreenMode != FullScreenMode.Windowed || !PlayerSettings.resizableWindow ||
                EditorSettings.serializationMode != SerializationMode.ForceText || EditorSettings.enterPlayModeOptionsEnabled)
                throw new InvalidOperationException("Foundation player/editor settings do not match Configure.");

            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length != 1 || scenes[0].path != ScenePath || !scenes[0].enabled)
                throw new InvalidOperationException("Foundation build scene is not configured.");
            ValidateScene();
        }

        private static T LoadOrRejectDifferentAsset<T>(string path) where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null && !(existing is T))
                throw new InvalidOperationException("Refusing to overwrite an unrelated asset: " + path);
            return existing as T;
        }

        private static void RequirePinnedEditor()
        {
            if (Application.unityVersion != ExpectedEditorVersion)
                throw new InvalidOperationException("Use the pinned Unity " + ExpectedEditorVersion + "; actual " + Application.unityVersion);
        }
    }
}
