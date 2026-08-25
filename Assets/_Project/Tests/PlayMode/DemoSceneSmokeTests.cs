using System.Collections;
using NUnit.Framework;
using PromVR.MaterialAccumulation.Core;
using PromVR.MaterialAccumulation.Presentation;
using PromVR.MaterialAccumulation.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
            DemoHudBehaviour hud = Object.FindFirstObjectByType<DemoHudBehaviour>();

            Assert.That(surface, Is.Not.Null);
            Assert.That(zoneView, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(controller.CurrentRadius, Is.GreaterThan(0f));
            Assert.That(controller.NormalizedRadius, Is.InRange(0f, 1f));

            GameObject languageButtonObject = GameObject.Find("Language Toggle");
            GameObject languageLabelObject = GameObject.Find("Language Label");
            GameObject titleObject = GameObject.Find("Title");
            Assert.That(languageButtonObject, Is.Not.Null);
            Assert.That(languageLabelObject, Is.Not.Null);
            Assert.That(titleObject, Is.Not.Null);

            Button languageButton = languageButtonObject.GetComponent<Button>();
            Text languageLabel = languageLabelObject.GetComponent<Text>();
            Text titleLabel = titleObject.GetComponent<Text>();
            Assert.That(languageButton, Is.Not.Null);
            Assert.That(languageLabel, Is.Not.Null);
            Assert.That(titleLabel, Is.Not.Null);

            string initialLanguage = languageLabel.text;
            string initialTitle = titleLabel.text;
            languageButton.onClick.Invoke();
            Assert.That(languageLabel.text, Is.Not.EqualTo(initialLanguage));
            Assert.That(titleLabel.text, Is.Not.EqualTo(initialTitle));
            languageButton.onClick.Invoke();
            Assert.That(languageLabel.text, Is.EqualTo(initialLanguage));
            Assert.That(titleLabel.text, Is.EqualTo(initialTitle));

            MeshFilter meshFilter = surface.GetComponent<MeshFilter>();
            Assert.That(meshFilter.sharedMesh, Is.Not.Null);
            Assert.That(meshFilter.sharedMesh.vertexCount, Is.EqualTo(surface.VertexCount));

            int transformCountBefore = Object.FindObjectsByType<Transform>(
                FindObjectsSortMode.None).Length;
            int runtimeMeshCountBefore = CountRuntimeMeshes();

            for (int i = 0; i < 24; i++)
            {
                float startX = -3f + (i * 0.25f);
                surface.ApplySweep(new Sweep(
                    startX,
                    -1f,
                    startX + 0.25f,
                    1f,
                    1.1f,
                    1.35f,
                    1f / 60f));
            }

            Assert.That(
                Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length,
                Is.EqualTo(transformCountBefore),
                "Accumulation must not create GameObjects for material portions.");
            Assert.That(
                CountRuntimeMeshes(),
                Is.EqualTo(runtimeMeshCountBefore),
                "Accumulation must reuse the two runtime meshes.");
            yield return null;

            Assert.DoesNotThrow(surface.ResetSurface);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SweptAccumulation_AfterWarmup_DoesNotAllocateManagedMemory()
        {
            yield return LoadDemoScene();

            AccumulationSurfaceBehaviour surface =
                Object.FindFirstObjectByType<AccumulationSurfaceBehaviour>();
            Assert.That(surface, Is.Not.Null);

            ApplyMeasuredPath(surface, 12);
            surface.ResetSurface();

            long allocatedBefore = System.GC.GetAllocatedBytesForCurrentThread();
            ApplyMeasuredPath(surface, 90);
            long allocatedBytes = System.GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            Assert.That(
                allocatedBytes,
                Is.Zero,
                "Core accumulation and dirty Mesh synchronization must stay allocation-free after warmup.");
        }

        private static IEnumerator LoadDemoScene()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                "MaterialAccumulationDemo",
                LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);
            yield return loadOperation;
            yield return null;
        }

        private static void ApplyMeasuredPath(AccumulationSurfaceBehaviour surface, int stepCount)
        {
            float previousX = -4.5f;
            float previousZ = 0f;
            float previousRadius = 1.05f;

            for (int i = 0; i < stepCount; i++)
            {
                float t = (i + 1f) / stepCount;
                float currentX = Mathf.Lerp(-4.5f, 4.5f, t);
                float currentZ = Mathf.Sin(t * Mathf.PI * 2f) * 2.8f;
                float currentRadius = Mathf.Lerp(1.05f, 1.5f, t);
                surface.ApplySweep(new Sweep(
                    previousX,
                    previousZ,
                    currentX,
                    currentZ,
                    previousRadius,
                    currentRadius,
                    1f / 60f));
                previousX = currentX;
                previousZ = currentZ;
                previousRadius = currentRadius;
            }
        }

        private static int CountRuntimeMeshes()
        {
            Mesh[] meshes = Resources.FindObjectsOfTypeAll<Mesh>();
            int count = 0;

            for (int i = 0; i < meshes.Length; i++)
            {
                string meshName = meshes[i].name;
                if (meshName == "Material Accumulation Surface (Runtime)" ||
                    meshName == "Hemisphere Zone Preview (Runtime)")
                {
                    count++;
                }
            }

            return count;
        }
    }
}
