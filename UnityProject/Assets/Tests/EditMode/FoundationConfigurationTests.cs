using CrowdCheckpoint.Unity.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CrowdCheckpoint.Unity.Tests
{
    public sealed class FoundationConfigurationTests
    {
        [Test]
        public void ConfigureRetainsAssetIdentitiesAndAppliesAllFoundationSettings()
        {
            FoundationProjectSetup.Configure();
            string renderer = AssetDatabase.AssetPathToGUID(FoundationProjectSetup.RendererPath);
            string pipeline = AssetDatabase.AssetPathToGUID(FoundationProjectSetup.PipelinePath);
            FoundationProjectSetup.Configure();
            Assert.That(renderer, Is.Not.Empty);
            Assert.That(pipeline, Is.Not.Empty);
            Assert.That(AssetDatabase.AssetPathToGUID(FoundationProjectSetup.RendererPath), Is.EqualTo(renderer));
            Assert.That(AssetDatabase.AssetPathToGUID(FoundationProjectSetup.PipelinePath), Is.EqualTo(pipeline));
            FoundationProjectSetup.ValidateConfiguration();
        }

        [Test]
        public void ValidationPreservesAnAlreadyLoadedFoundationScene()
        {
            Scene scene = SceneManager.GetSceneByPath(FoundationProjectSetup.ScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(FoundationProjectSetup.ScenePath, OpenSceneMode.Additive);
            try
            {
                FoundationProjectSetup.ValidateScene();
                Assert.That(scene.isLoaded, Is.True);
                Assert.That(SceneManager.GetSceneByPath(FoundationProjectSetup.ScenePath).handle, Is.EqualTo(scene.handle));
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
