using System.Collections;
using CrowdCheckpoint.Unity.Foundation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CrowdCheckpoint.Unity.Tests
{
    public sealed class FoundationSmokeTests
    {
        [UnityTest]
        public IEnumerator TrackedSceneLoadsAndRunsWithItsSerializedCamera()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/MigrationFoundation.unity", LoadSceneMode.Single);
            yield return null;
            var bootstrap = UnityEngine.Object.FindFirstObjectByType<FoundationBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.enabled, Is.True);
            Assert.That(bootstrap.FoundationCamera, Is.Not.Null);
            Assert.That(bootstrap.FoundationCamera.isActiveAndEnabled, Is.True);
            Assert.That(bootstrap.FoundationCamera.orthographic, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
