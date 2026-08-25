using System.Collections;
using NUnit.Framework;
using PromVR.MaterialAccumulation.Core;
using PromVR.MaterialAccumulation.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PromVR.MaterialAccumulation.Tests.PlayMode
{
    public sealed class DemoSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator DemoScene_InitializesAppliesSweepAndResets()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                "MaterialAccumulationDemo",
                LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);
            yield return loadOperation;
            yield return null;

            AccumulationSurfaceBehaviour surface =
                Object.FindFirstObjectByType<AccumulationSurfaceBehaviour>();
            HemisphereZoneView zoneView = Object.FindFirstObjectByType<HemisphereZoneView>();
            BrushControllerBehaviour controller = Object.FindFirstObjectByType<BrushControllerBehaviour>();

            Assert.That(surface, Is.Not.Null);
            Assert.That(zoneView, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);

            MeshFilter meshFilter = surface.GetComponent<MeshFilter>();
            Assert.That(meshFilter.sharedMesh, Is.Not.Null);
            Assert.That(meshFilter.sharedMesh.vertexCount, Is.EqualTo(129 * 129));

            Assert.DoesNotThrow(() =>
                surface.ApplySweep(new Sweep(0f, 0f, 1f, 0f, 1.25f, 1.25f, 0.2f)));
            yield return null;

            Assert.DoesNotThrow(surface.ResetSurface);
            yield return null;
        }
    }
}
