using PromVR.MaterialAccumulation.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PromVR.MaterialAccumulation.Editor
{
    public static class MaterialAccumulationDemoBuilder
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenesFolder = ProjectRoot + "/Scenes";
        private const string MaterialsFolder = ProjectRoot + "/Materials";
        private const string DemoScenePath = ScenesFolder + "/MaterialAccumulationDemo.unity";
        private const string SurfaceMaterialPath = MaterialsFolder + "/AccumulatedMaterial.mat";
        private const string BaseMaterialPath = MaterialsFolder + "/SurfaceBase.mat";
        private const string PreviewMaterialPath = MaterialsFolder + "/HemispherePreview.mat";

        [MenuItem("Tools/Material Accumulation/Rebuild Demo Scene %#g")]
        private static void RebuildDemoSceneFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BuildDemoScene();
        }

        public static void BuildDemoScene()
        {
            EnsureProjectFolders();

            Material accumulatedMaterial = GetOrCreateLitMaterial(
                SurfaceMaterialPath,
                new Color(0.92f, 0.31f, 0.085f, 1f),
                0.05f,
                0.42f);
            Material baseMaterial = GetOrCreateLitMaterial(
                BaseMaterialPath,
                new Color(0.055f, 0.075f, 0.105f, 1f),
                0.15f,
                0.28f);
            Material previewMaterial = GetOrCreatePreviewMaterial();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Material Accumulation Demo");

            AccumulationSurfaceBehaviour surface = CreateSurface(root.transform, accumulatedMaterial);
            CreateBase(root.transform, baseMaterial);
            HemisphereZoneView zoneView = CreateBrushPreview(root.transform, previewMaterial);
            CreateBrushController(root.transform, surface, zoneView);
            CreateLighting(root.transform);
            CreateCamera(root.transform);
            CreateInstructions(root.transform);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.32f, 0.4f, 1f);
            RenderSettings.reflectionIntensity = 0.55f;

            EditorSceneManager.SaveScene(scene, DemoScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(DemoScenePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = root;
            Debug.Log($"Material Accumulation demo created at {DemoScenePath}");
        }

        private static AccumulationSurfaceBehaviour CreateSurface(
            Transform parent,
            Material accumulatedMaterial)
        {
            GameObject surfaceObject = new GameObject(
                "Accumulation Surface",
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(AccumulationSurfaceBehaviour));
            surfaceObject.transform.SetParent(parent, false);
            MeshRenderer renderer = surfaceObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = accumulatedMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return surfaceObject.GetComponent<AccumulationSurfaceBehaviour>();
        }

        private static void CreateBase(Transform parent, Material baseMaterial)
        {
            GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObject.name = "Static Surface Base";
            baseObject.transform.SetParent(parent, false);
            baseObject.transform.localPosition = new Vector3(0f, -0.16f, 0f);
            baseObject.transform.localScale = new Vector3(12.8f, 0.3f, 12.8f);
            baseObject.GetComponent<MeshRenderer>().sharedMaterial = baseMaterial;

            Collider collider = baseObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static HemisphereZoneView CreateBrushPreview(Transform parent, Material previewMaterial)
        {
            GameObject previewObject = new GameObject(
                "Hemisphere Zone Preview",
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(HemisphereZoneView));
            previewObject.transform.SetParent(parent, false);

            MeshRenderer renderer = previewObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = previewMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return previewObject.GetComponent<HemisphereZoneView>();
        }

        private static void CreateBrushController(
            Transform parent,
            AccumulationSurfaceBehaviour surface,
            HemisphereZoneView zoneView)
        {
            GameObject controllerObject = new GameObject("Brush Controller");
            controllerObject.transform.SetParent(parent, false);
            BrushControllerBehaviour controller = controllerObject.AddComponent<BrushControllerBehaviour>();

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_surface").objectReferenceValue = surface;
            serializedController.FindProperty("_zoneView").objectReferenceValue = zoneView;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateLighting(Transform parent)
        {
            GameObject lightObject = new GameObject("Key Light", typeof(Light));
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localRotation = Quaternion.Euler(48f, -32f, 0f);

            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.9f, 0.8f, 1f);
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
        }

        private static void CreateCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = new Vector3(9.8f, 8.4f, -10.6f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.55f, 0f));

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.026f, 0.042f, 1f);
            camera.fieldOfView = 43f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
        }

        private static void CreateInstructions(Transform parent)
        {
            GameObject canvasObject = new GameObject(
                "Demo UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.GetComponent<GraphicRaycaster>().enabled = false;

            GameObject panelObject = new GameObject(
                "Controls Panel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.SetParent(canvasObject.transform, false);
            panel.anchorMin = new Vector2(0.025f, 0.75f);
            panel.anchorMax = new Vector2(0.335f, 0.955f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.025f, 0.04f, 0.065f, 0.88f);
            panelImage.raycastTarget = false;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateLabel(
                panel,
                "Title",
                "MATERIAL ACCUMULATION",
                font,
                29,
                FontStyle.Bold,
                new Color(1f, 0.46f, 0.12f, 1f),
                new Vector2(0.06f, 0.62f),
                new Vector2(0.94f, 0.92f));
            CreateLabel(
                panel,
                "Controls",
                "WASD   Move hemisphere\nSPACE  Accumulate material\nR      Reset surface",
                font,
                20,
                FontStyle.Normal,
                new Color(0.86f, 0.91f, 0.98f, 1f),
                new Vector2(0.06f, 0.08f),
                new Vector2(0.94f, 0.62f));
        }

        private static void CreateLabel(
            RectTransform parent,
            string name,
            string content,
            Font font,
            int fontSize,
            FontStyle fontStyle,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject labelObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = labelObject.GetComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }

        private static Material GetOrCreateLitMaterial(
            string assetPath,
            Color color,
            float metallic,
            float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new System.InvalidOperationException("URP Lit shader was not found.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreatePreviewMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                throw new System.InvalidOperationException("URP Unlit shader was not found.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(PreviewMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, PreviewMaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", new Color(0.1f, 0.8f, 1f, 0.18f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureProjectFolders()
        {
            EnsureFolder("Assets", "_Project");
            EnsureFolder(ProjectRoot, "Scenes");
            EnsureFolder(ProjectRoot, "Materials");
            EnsureFolder(ProjectRoot, "Prefabs");
            EnsureFolder(ProjectRoot, "Art");
            EnsureFolder(ProjectRoot, "Settings");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
