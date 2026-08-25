using PromVR.MaterialAccumulation.Presentation;
using PromVR.MaterialAccumulation.Unity;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
        private const string SettingsFolder = ProjectRoot + "/Settings";
        private const string DemoScenePath = ScenesFolder + "/MaterialAccumulationDemo.unity";
        private const string SurfaceMaterialPath = MaterialsFolder + "/AccumulatedMaterial.mat";
        private const string BaseMaterialPath = MaterialsFolder + "/SurfaceBase.mat";
        private const string FrameMaterialPath = MaterialsFolder + "/SurfaceFrame.mat";
        private const string StageMaterialPath = MaterialsFolder + "/Stage.mat";
        private const string PreviewMaterialPath = MaterialsFolder + "/HemispherePreview.mat";
        private const string PostProcessProfilePath = SettingsFolder + "/DemoPostProcess.asset";
        private const string SentisAnalyticsDefine = "SENTIS_ANALYTICS_ENABLED";
        private const string AppUiEditorOnlyDefine = "APP_UI_EDITOR_ONLY";

        private static readonly Color BackgroundColor = new Color(0.012f, 0.019f, 0.033f, 1f);
        private static readonly Color PanelColor = new Color(0.026f, 0.043f, 0.068f, 0.92f);
        private static readonly Color PrimaryTextColor = new Color(0.91f, 0.95f, 1f, 1f);
        private static readonly Color SecondaryTextColor = new Color(0.62f, 0.7f, 0.81f, 1f);
        private static readonly Color IdleColor = new Color(0.12f, 0.78f, 0.95f, 1f);
        private static readonly Color ActiveColor = new Color(1f, 0.42f, 0.08f, 1f);

        [MenuItem("Tools/Material Accumulation/Rebuild Demo Scene %#g")]
        private static void RebuildDemoSceneFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BuildDemoScene();
        }

        [MenuItem("Tools/Material Accumulation/Open Demo Scene _F8")]
        private static void OpenDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        }

        public static void BuildDemoScene()
        {
            EnsureProjectFolders();

            Material accumulatedMaterial = GetOrCreateLitMaterial(
                SurfaceMaterialPath,
                new Color(0.94f, 0.29f, 0.055f, 1f),
                0.08f,
                0.48f);
            Material baseMaterial = GetOrCreateLitMaterial(
                BaseMaterialPath,
                new Color(0.045f, 0.065f, 0.095f, 1f),
                0.2f,
                0.32f);
            Material frameMaterial = GetOrCreateLitMaterial(
                FrameMaterialPath,
                new Color(0.09f, 0.15f, 0.22f, 1f),
                0.45f,
                0.72f);
            Material stageMaterial = GetOrCreateLitMaterial(
                StageMaterialPath,
                new Color(0.016f, 0.025f, 0.041f, 1f),
                0.05f,
                0.18f);
            Material previewMaterial = GetOrCreatePreviewMaterial();
            VolumeProfile postProcessProfile = GetOrCreatePostProcessProfile();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Material Accumulation Demo");

            Transform environment = CreateGroup(root.transform, "Environment");
            CreateStage(environment, baseMaterial, frameMaterial, stageMaterial, accumulatedMaterial);

            Transform simulation = CreateGroup(root.transform, "Simulation");
            AccumulationSurfaceBehaviour surface = CreateSurface(simulation, accumulatedMaterial);
            HemisphereZoneView zoneView = CreateBrushPreview(simulation, previewMaterial);
            BrushControllerBehaviour controller = CreateBrushController(simulation, surface, zoneView);

            Transform presentation = CreateGroup(root.transform, "Presentation");
            CreateLighting(presentation);
            Camera camera = CreateCamera(presentation);
            CreatePostProcessing(presentation, camera, postProcessProfile);
            CreateHud(presentation, controller);

            ConfigureRenderSettings();
            ConfigurePlayerSettings();

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

        private static Transform CreateGroup(Transform parent, string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
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

        private static void CreateStage(
            Transform parent,
            Material baseMaterial,
            Material frameMaterial,
            Material stageMaterial,
            Material accentMaterial)
        {
            CreateCube(
                parent,
                "Backdrop Floor",
                new Vector3(0f, -0.77f, 0f),
                new Vector3(32f, 0.12f, 32f),
                stageMaterial,
                ShadowCastingMode.Off);
            CreateCube(
                parent,
                "Presentation Plinth",
                new Vector3(0f, -0.53f, 0f),
                new Vector3(15.6f, 0.38f, 15.6f),
                baseMaterial,
                ShadowCastingMode.On);
            CreateCube(
                parent,
                "Surface Base",
                new Vector3(0f, -0.18f, 0f),
                new Vector3(12.7f, 0.32f, 12.7f),
                baseMaterial,
                ShadowCastingMode.On);
            CreateCube(
                parent,
                "Static Surface Cover",
                new Vector3(0f, -0.018f, 0f),
                new Vector3(12f, 0.04f, 12f),
                baseMaterial,
                ShadowCastingMode.On);

            const float railOffset = 6.24f;
            const float railLength = 12.62f;
            const float railThickness = 0.12f;
            const float railHeight = 0.07f;
            CreateCube(
                parent,
                "Frame Left",
                new Vector3(-railOffset, 0.015f, 0f),
                new Vector3(railThickness, railHeight, railLength),
                frameMaterial,
                ShadowCastingMode.On);
            CreateCube(
                parent,
                "Frame Right",
                new Vector3(railOffset, 0.015f, 0f),
                new Vector3(railThickness, railHeight, railLength),
                frameMaterial,
                ShadowCastingMode.On);
            CreateCube(
                parent,
                "Frame Back",
                new Vector3(0f, 0.015f, railOffset),
                new Vector3(railLength, railHeight, railThickness),
                frameMaterial,
                ShadowCastingMode.On);
            CreateCube(
                parent,
                "Frame Front",
                new Vector3(0f, 0.015f, -railOffset),
                new Vector3(railLength, railHeight, railThickness),
                frameMaterial,
                ShadowCastingMode.On);

            CreateCube(
                parent,
                "Front Accent Left",
                new Vector3(-4.25f, -0.315f, -7.82f),
                new Vector3(2.1f, 0.035f, 0.035f),
                accentMaterial,
                ShadowCastingMode.Off);
            CreateCube(
                parent,
                "Front Accent Right",
                new Vector3(4.25f, -0.315f, -7.82f),
                new Vector3(2.1f, 0.035f, 0.035f),
                accentMaterial,
                ShadowCastingMode.Off);
        }

        private static GameObject CreateCube(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            ShadowCastingMode shadowCastingMode)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;

            MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadowCastingMode;
            renderer.receiveShadows = true;

            Collider collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            return cube;
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

        private static BrushControllerBehaviour CreateBrushController(
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
            return controller;
        }

        private static void CreateLighting(Transform parent)
        {
            GameObject keyObject = new GameObject("Key Light", typeof(Light));
            keyObject.transform.SetParent(parent, false);
            keyObject.transform.localRotation = Quaternion.Euler(48f, -32f, 0f);

            Light key = keyObject.GetComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1f, 0.88f, 0.76f, 1f);
            key.intensity = 1.25f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.72f;

            GameObject fillObject = new GameObject("Cool Fill Light", typeof(Light));
            fillObject.transform.SetParent(parent, false);
            fillObject.transform.localPosition = new Vector3(-5.5f, 5.5f, -4.5f);

            Light fill = fillObject.GetComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(0.2f, 0.57f, 1f, 1f);
            fill.intensity = 4.2f;
            fill.range = 18f;
            fill.shadows = LightShadows.None;
        }

        private static Camera CreateCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject(
                "Main Camera",
                typeof(Camera),
                typeof(AudioListener),
                typeof(UniversalAdditionalCameraData));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = new Vector3(10.8f, 9.4f, -11.8f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.45f, 0f));

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.allowHDR = true;
            camera.allowMSAA = true;

            UniversalAdditionalCameraData cameraData =
                cameraObject.GetComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;
            return camera;
        }

        private static void CreatePostProcessing(
            Transform parent,
            Camera camera,
            VolumeProfile profile)
        {
            GameObject volumeObject = new GameObject("Global Post Processing", typeof(Volume));
            volumeObject.transform.SetParent(parent, false);
            Volume volume = volumeObject.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = profile;

            if (camera == null)
            {
                volume.enabled = false;
            }
        }

        private static void CreateHud(Transform parent, BrushControllerBehaviour controller)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new GameObject(
                "Demo HUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(DemoHudBehaviour));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.GetComponent<GraphicRaycaster>().enabled = false;

            RectTransform titlePanel = CreatePanel(
                canvasObject.transform,
                "Project Card",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(40f, -40f),
                new Vector2(520f, 178f),
                PanelColor);
            CreateImage(
                titlePanel,
                "Accent",
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(5f, 0f),
                new Vector2(5f, 130f),
                ActiveColor);
            CreateText(
                titlePanel,
                "Kicker",
                "PROMVR • TECHNICAL DEMO",
                font,
                14,
                FontStyle.Bold,
                SecondaryTextColor,
                new Vector2(30f, -24f),
                new Vector2(450f, 25f),
                TextAnchor.MiddleLeft);
            CreateText(
                titlePanel,
                "Title",
                "MATERIAL ACCUMULATION",
                font,
                30,
                FontStyle.Bold,
                PrimaryTextColor,
                new Vector2(30f, -68f),
                new Vector2(460f, 44f),
                TextAnchor.MiddleLeft);
            CreateText(
                titlePanel,
                "Subtitle",
                "Persistent CPU height field • swept hemisphere",
                font,
                17,
                FontStyle.Normal,
                SecondaryTextColor,
                new Vector2(30f, -122f),
                new Vector2(460f, 32f),
                TextAnchor.MiddleLeft);

            RectTransform statusPanel = CreatePanel(
                canvasObject.transform,
                "Live Status",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-40f, -40f),
                new Vector2(330f, 178f),
                PanelColor);
            Image stateIndicator = CreateImage(
                statusPanel,
                "State Indicator",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(22f, -30f),
                new Vector2(5f, 30f),
                IdleColor);
            CreateText(
                statusPanel,
                "Status Caption",
                "BRUSH STATUS",
                font,
                13,
                FontStyle.Bold,
                SecondaryTextColor,
                new Vector2(44f, -24f),
                new Vector2(200f, 22f),
                TextAnchor.MiddleLeft);
            Text stateLabel = CreateText(
                statusPanel,
                "Status Value",
                "READY",
                font,
                22,
                FontStyle.Bold,
                IdleColor,
                new Vector2(44f, -53f),
                new Vector2(245f, 34f),
                TextAnchor.MiddleLeft);
            CreateText(
                statusPanel,
                "Radius Caption",
                "ANIMATED RADIUS",
                font,
                13,
                FontStyle.Bold,
                SecondaryTextColor,
                new Vector2(22f, -100f),
                new Vector2(286f, 22f),
                TextAnchor.MiddleLeft);
            RectTransform radiusTrack = CreatePanel(
                statusPanel,
                "Radius Track",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(22f, -132f),
                new Vector2(286f, 8f),
                new Color(0.15f, 0.2f, 0.27f, 1f));
            Image radiusFill = CreateImage(
                radiusTrack,
                "Radius Fill",
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                Vector2.zero,
                new Vector2(286f, 8f),
                IdleColor);
            radiusFill.type = Image.Type.Filled;
            radiusFill.fillMethod = Image.FillMethod.Horizontal;
            radiusFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            CreateText(
                statusPanel,
                "Radius Min",
                "MIN",
                font,
                11,
                FontStyle.Bold,
                SecondaryTextColor,
                new Vector2(22f, -153f),
                new Vector2(60f, 18f),
                TextAnchor.MiddleLeft);
            CreateText(
                statusPanel,
                "Radius Max",
                "MAX",
                font,
                11,
                FontStyle.Bold,
                SecondaryTextColor,
                new Vector2(248f, -153f),
                new Vector2(60f, 18f),
                TextAnchor.MiddleRight);

            RectTransform controlsPanel = CreatePanel(
                canvasObject.transform,
                "Controls",
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(40f, 40f),
                new Vector2(650f, 112f),
                PanelColor);
            CreateControlHint(controlsPanel, font, "WASD", "MOVE", 22f, 104f);
            CreateControlHint(controlsPanel, font, "SPACE", "DEPOSIT", 216f, 118f);
            CreateControlHint(controlsPanel, font, "R", "CLEAR", 427f, 48f);
            CreateControlHint(controlsPanel, font, "ESC", "QUIT BUILD", 544f, 70f);

            Text holdPrompt = CreateText(
                canvasObject.GetComponent<RectTransform>(),
                "Primary Hint",
                "HOLD  SPACE  TO  ACCUMULATE",
                font,
                16,
                FontStyle.Bold,
                new Color(0.72f, 0.79f, 0.88f, 0.72f),
                new Vector2(-40f, 48f),
                new Vector2(420f, 38f),
                TextAnchor.MiddleRight,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f));

            DemoHudBehaviour hud = canvasObject.GetComponent<DemoHudBehaviour>();
            SerializedObject serializedHud = new SerializedObject(hud);
            serializedHud.FindProperty("_controller").objectReferenceValue = controller;
            serializedHud.FindProperty("_radiusFill").objectReferenceValue = radiusFill;
            serializedHud.FindProperty("_stateIndicator").objectReferenceValue = stateIndicator;
            serializedHud.FindProperty("_stateLabel").objectReferenceValue = stateLabel;
            serializedHud.FindProperty("_holdPrompt").objectReferenceValue = holdPrompt;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateControlHint(
            RectTransform parent,
            Font font,
            string key,
            string action,
            float positionX,
            float keyWidth)
        {
            RectTransform keyCap = CreatePanel(
                parent,
                key + " Key",
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(positionX, 9f),
                new Vector2(keyWidth, 42f),
                new Color(0.09f, 0.14f, 0.21f, 1f));
            CreateText(
                keyCap,
                "Key Label",
                key,
                font,
                15,
                FontStyle.Bold,
                PrimaryTextColor,
                Vector2.zero,
                new Vector2(keyWidth, 42f),
                TextAnchor.MiddleCenter);
            CreateText(
                parent,
                key + " Action",
                action,
                font,
                11,
                FontStyle.Bold,
                SecondaryTextColor,
                new Vector2(positionX, -83f),
                new Vector2(keyWidth, 18f),
                TextAnchor.MiddleCenter);
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            return CreateImage(parent, name, anchor, pivot, anchoredPosition, size, color).rectTransform;
        }

        private static Image CreateImage(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            GameObject imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(
            RectTransform parent,
            string name,
            string content,
            Font font,
            int fontSize,
            FontStyle fontStyle,
            Color color,
            Vector2 anchoredPosition,
            Vector2 size,
            TextAnchor alignment,
            Vector2? anchor = null,
            Vector2? pivot = null)
        {
            GameObject labelObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Vector2 resolvedAnchor = anchor ?? new Vector2(0f, 1f);
            rect.anchorMin = resolvedAnchor;
            rect.anchorMax = resolvedAnchor;
            rect.pivot = pivot ?? resolvedAnchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = labelObject.GetComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
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

            material.SetColor("_BaseColor", new Color(0.1f, 0.8f, 1f, 0.17f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static VolumeProfile GetOrCreatePostProcessProfile()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProcessProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, PostProcessProfilePath);
            }

            if (!profile.TryGet(out ColorAdjustments colorAdjustments))
            {
                colorAdjustments = profile.Add<ColorAdjustments>(true);
            }

            colorAdjustments.active = true;
            colorAdjustments.postExposure.Override(0.1f);
            colorAdjustments.contrast.Override(8f);
            colorAdjustments.saturation.Override(-4f);

            if (!profile.TryGet(out Vignette vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }

            vignette.active = true;
            vignette.color.Override(new Color(0.005f, 0.009f, 0.018f, 1f));
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.42f);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigureRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.23f, 0.31f, 1f);
            RenderSettings.reflectionIntensity = 0.48f;
            RenderSettings.fog = false;
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "PromVR";
            PlayerSettings.productName = "Material Accumulation";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Standalone,
                "com.promvr.materialaccumulation");
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            string defineSymbols = PlayerSettings.GetScriptingDefineSymbols(
                NamedBuildTarget.Standalone);
            defineSymbols = RemoveDefineSymbol(defineSymbols, SentisAnalyticsDefine);
            defineSymbols = RemoveDefineSymbol(defineSymbols, AppUiEditorOnlyDefine);
            PlayerSettings.SetScriptingDefineSymbols(
                NamedBuildTarget.Standalone,
                defineSymbols);
        }

        private static string RemoveDefineSymbol(string defineSymbols, string symbol)
        {
            string paddedSymbols = ";" + defineSymbols + ";";
            return paddedSymbols.Replace(";" + symbol + ";", ";").Trim(';');
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
