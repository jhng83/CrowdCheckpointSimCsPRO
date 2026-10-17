using CrowdCheckpoint.Unity.Editor;
using NUnit.Framework;
using UnityEditor;

namespace CrowdCheckpoint.Unity.Tests
{
    public sealed class FoundationSceneTests
    {
        [Test]
        public void SerializedSceneHasOneBootstrapAndItsOrthographicCamera()
        {
            FoundationProjectSetup.ValidateScene();
        }

        [Test]
        public void FoundationSceneIsEnabledInBuildSettings()
        {
            Assert.That(EditorBuildSettings.scenes.Length, Is.EqualTo(1));
            Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(FoundationProjectSetup.ScenePath));
            Assert.That(EditorBuildSettings.scenes[0].enabled, Is.True);
        }
    }
}
