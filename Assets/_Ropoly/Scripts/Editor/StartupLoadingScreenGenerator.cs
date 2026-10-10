using System.IO;
using Ropoly.Bootstrap;
using Ropoly.Presentation.Navigation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ropoly.Editor
{
    /// <summary>
    /// Builds the first-frame loading presentation into the persistent Bootstrap scene.
    /// </summary>
    public static class StartupLoadingScreenGenerator
    {
        private const string BootstrapScenePath =
            "Assets/_Ropoly/Scenes/App/Bootstrap.unity";
        private const string FontAssetPath =
            "Assets/_Ropoly/Art/Fonts/Ropoly UI SDF.asset";

        [MenuItem("Ropoly/Development/Repair Startup Loading Screen")]
        public static void Generate()
        {
            if (UnityEngine.Application.isPlaying)
            {
                Debug.LogWarning("The startup loading screen cannot be rebuilt during Play Mode.");
                return;
            }

            Scene originalScene = SceneManager.GetActiveScene();
            string originalPath = originalScene.path;
            if (originalScene.isDirty)
            {
                Debug.LogWarning("Save the active scene before rebuilding the startup loading screen.");
                return;
            }

            Scene bootstrapScene = EditorSceneManager.OpenScene(
                BootstrapScenePath,
                OpenSceneMode.Single);
            AppRoot appRoot = Object.FindFirstObjectByType<AppRoot>();
            SceneFlowService sceneFlow = Object.FindFirstObjectByType<SceneFlowService>();
            if (!bootstrapScene.IsValid() || appRoot == null || sceneFlow == null)
            {
                throw new MissingReferenceException(
                    "The Bootstrap scene requires AppRoot and SceneFlowService before adding its loading screen.");
            }

            Configure(appRoot, sceneFlow);
            EditorSceneManager.SaveScene(bootstrapScene, BootstrapScenePath);
            AssetDatabase.SaveAssets();

            if (!UnityEngine.Application.isBatchMode &&
                !string.IsNullOrWhiteSpace(originalPath) &&
                originalPath != BootstrapScenePath)
            {
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
            }

            Debug.Log("Rebuilt the persistent first-frame loading screen.");
        }

        internal static SceneLoadingView Configure(AppRoot appRoot, SceneFlowService sceneFlow)
        {
            Transform existing = appRoot.transform.Find("Persistent Loading Screen");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (font == null)
            {
                throw new FileNotFoundException(
                    $"The startup loading screen requires the high-resolution font at '{FontAssetPath}'.");
            }

            GameObject root = new GameObject("Persistent Loading Screen");
            root.transform.SetParent(appRoot.transform, false);
            SceneLoadingView view = root.AddComponent<SceneLoadingView>();

            GameObject cameraObject = new GameObject("Loading Camera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            Camera loadingCamera = cameraObject.AddComponent<Camera>();
            loadingCamera.clearFlags = CameraClearFlags.SolidColor;
            loadingCamera.backgroundColor = new Color(0.012f, 0.008f, 0.035f, 1f);
            loadingCamera.cullingMask = 0;
            loadingCamera.depth = -100f;
            loadingCamera.allowHDR = false;
            loadingCamera.allowMSAA = false;
            loadingCamera.useOcclusionCulling = false;

            GameObject canvasObject = new GameObject("Loading Canvas", typeof(RectTransform));
            canvasObject.transform.SetParent(root.transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = short.MaxValue;
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.TexCoord2 |
                AdditionalCanvasShaderChannels.Normal |
                AdditionalCanvasShaderChannels.Tangent;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasGroup group = canvasObject.AddComponent<CanvasGroup>();

            CreateImage(
                canvas.transform,
                "Background",
                new Color(0.012f, 0.008f, 0.035f, 1f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            CreateImage(
                canvas.transform,
                "Center Glow",
                new Color(0.16f, 0.08f, 0.30f, 0.65f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(760f, 310f));
            CreateText(
                canvas.transform,
                "Brand",
                "ROPOLY",
                font,
                94,
                FontStyles.Bold,
                new Color(0.95f, 0.93f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 54f),
                new Vector2(720f, 120f));
            TextMeshProUGUI status = CreateText(
                canvas.transform,
                "Status",
                "PREPARING THE WORLD",
                font,
                21,
                FontStyles.Bold,
                new Color(0.48f, 0.92f, 0.84f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -48f),
                new Vector2(720f, 48f));

            Image track = CreateImage(
                canvas.transform,
                "Loading Track",
                new Color(0.17f, 0.13f, 0.27f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -112f),
                new Vector2(320f, 8f));
            CreateImage(
                track.transform,
                "Accent",
                new Color(0.45f, 0.28f, 0.94f, 1f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            SerializedObject viewObject = new SerializedObject(view);
            viewObject.FindProperty("_loadingCamera").objectReferenceValue = loadingCamera;
            viewObject.FindProperty("_loadingCanvas").objectReferenceValue = canvas;
            viewObject.FindProperty("_canvasGroup").objectReferenceValue = group;
            viewObject.FindProperty("_statusLabel").objectReferenceValue = status;
            viewObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject flowObject = new SerializedObject(sceneFlow);
            flowObject.FindProperty("_loadingView").objectReferenceValue = view;
            flowObject.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static Image CreateImage(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            SetRect(image.rectTransform, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            return image;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            TMP_FontAsset font,
            int fontSize,
            FontStyles style,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(14f, fontSize * 0.8f);
            text.fontSizeMax = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.extraPadding = true;
            SetRect(text.rectTransform, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            return text;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
