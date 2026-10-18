using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrowdCheckpoint.Unity.Editor
{
    /// <summary>Actual Editor receipt for standalone validation or CI; not yet executed in Unity.</summary>
    public static class FoundationValidation
    {
        public static void ConfigureAndReport()
        {
            string destination = ReadResultsDirectory();
            FoundationProjectSetup.Configure();
            string rendererGuid = AssetDatabase.AssetPathToGUID(FoundationProjectSetup.RendererPath);
            string pipelineGuid = AssetDatabase.AssetPathToGUID(FoundationProjectSetup.PipelinePath);
            FoundationProjectSetup.Configure();
            if (rendererGuid != AssetDatabase.AssetPathToGUID(FoundationProjectSetup.RendererPath) ||
                pipelineGuid != AssetDatabase.AssetPathToGUID(FoundationProjectSetup.PipelinePath))
                throw new InvalidOperationException("Repeated configuration changed existing asset identities.");

            FoundationProjectSetup.ValidateConfiguration();
            bool mono = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) == ScriptingImplementation.Mono2x;
            bool standard = PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.Standalone) == ApiCompatibilityLevel.NET_Standard;
            bool pipelineAssigned = AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline) == FoundationProjectSetup.PipelinePath;
            bool lockPresent = File.Exists("Packages/packages-lock.json");
            if (!mono || !standard || !pipelineAssigned || !lockPresent ||
                string.IsNullOrEmpty(rendererGuid) || string.IsNullOrEmpty(pipelineGuid))
                throw new InvalidOperationException("Foundation settings or the actual UPM lockfile are incomplete.");

            Directory.CreateDirectory(destination);
            File.WriteAllText(Path.Combine(destination, "configuration-receipt.json"), JsonUtility.ToJson(new Receipt
            {
                schemaVersion = 1,
                editorVersion = Application.unityVersion,
                configurationPasses = 2,
                rendererGuid = rendererGuid,
                pipelineGuid = pipelineGuid,
                sceneValidated = true,
                monoBackend = mono,
                dotNetStandard = standard,
                pipelineAssigned = pipelineAssigned,
                packageLockPresent = lockPresent,
                applicationParity = false
            }, true));
            Debug.Log("Foundation configuration receipt written. Application parity is not validated.");
        }

        private static string ReadResultsDirectory()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index] != "-migrationResultsPath")
                    continue;
                string path = Path.GetFullPath(arguments[index + 1]);
                StringComparison comparison = Path.DirectorySeparatorChar == '\\'
                    ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                string cloudRoot = Path.GetFullPath("Artifacts/UnityCloud") + Path.DirectorySeparatorChar;
                string standaloneRoot = Path.GetFullPath("Artifacts/UnityValidation") + Path.DirectorySeparatorChar;
                if (!path.StartsWith(cloudRoot, comparison) && !path.StartsWith(standaloneRoot, comparison))
                    throw new InvalidOperationException("Result files must stay under Artifacts/UnityCloud or Artifacts/UnityValidation.");
                return path;
            }
            throw new ArgumentException("Missing -migrationResultsPath argument.");
        }

        [Serializable]
        private sealed class Receipt
        {
            public int schemaVersion;
            public string editorVersion;
            public int configurationPasses;
            public string rendererGuid;
            public string pipelineGuid;
            public bool sceneValidated;
            public bool monoBackend;
            public bool dotNetStandard;
            public bool pipelineAssigned;
            public bool packageLockPresent;
            public bool applicationParity;
        }
    }
}
