using System.IO;
using Ropoly.Bootstrap;
using Ropoly.Presentation.Navigation;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ropoly.Editor
{
    /// <summary>
    /// Generates the first playable scene flow from source so the scenes stay reproducible.
    /// It only creates missing scenes and never overwrites authored scene work.
    /// </summary>
    [InitializeOnLoad]
    public static class P1SceneGenerator
    {
        private const string SceneFolder = "Assets/_Ropoly/Scenes";
        private const string AppSceneFolder = SceneFolder + "/App";
        private const string FrontendSceneFolder = SceneFolder + "/Frontend";
        private const string GameplaySceneFolder = SceneFolder + "/Gameplay";
        private const string BootstrapScenePath = AppSceneFolder + "/Bootstrap.unity";
        private const string MainMenuScenePath = FrontendSceneFolder + "/MainMenu.unity";
        private const string GameScenePath = GameplaySceneFolder + "/Game.unity";
        private const string LegacyBootstrapScenePath = SceneFolder + "/Bootstrap.unity";
        private const string LegacyMainMenuScenePath = SceneFolder + "/MainMenu.unity";
        private const string LegacyGameScenePath = SceneFolder + "/Game.unity";
        private const string MenuBackgroundPath =
            "Assets/_Ropoly/Art/Placeholder/ropoly-menu-background-placeholder.png";
        private const string FontAssetPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        static P1SceneGenerator()
        {
            EditorApplication.delayCall += GenerateAutomaticallyIfRequired;
        }

        [MenuItem("Ropoly/Development/Generate Missing P-1 Scenes")]
        public static void GenerateMissingScenes()
        {
            if (UnityEngine.Application.isPlaying)
            {
                Debug.LogWarning("P-1 scenes cannot be generated while the game is running.");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isDirty)
            {
                Debug.LogWarning("Save the current scene before generating the P-1 scenes.");
                return;
            }

            EnsureSceneFoldersAndMigrate();

            bool createdAnyScene = false;
            if (!File.Exists(BootstrapScenePath))
            {
                CreateBootstrapScene();
                createdAnyScene = true;
            }

            if (!File.Exists(MainMenuScenePath))
            {
                CreateMainMenuScene();
                createdAnyScene = true;
            }

            if (!File.Exists(GameScenePath))
            {
                CreateGameScene();
                createdAnyScene = true;
            }

            ConfigureBuildSettings();
            ConfigurePlayModeStartScene();

            if (createdAnyScene)
            {
                EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
                Debug.Log("Ropoly P-1 scenes generated. MainMenu is open; Play Mode starts from Bootstrap.");
            }
        }

        private static void GenerateAutomaticallyIfRequired()
        {
            if (UnityEngine.Application.isPlaying)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isDirty)
            {
                return;
            }

            EnsureSceneFoldersAndMigrate();

            if (File.Exists(BootstrapScenePath) &&
                File.Exists(MainMenuScenePath) &&
                File.Exists(GameScenePath))
            {
                ConfigureBuildSettings();
                ConfigurePlayModeStartScene();
                return;
            }

            GenerateMissingScenes();
        }

        private static void EnsureSceneFoldersAndMigrate()
        {
            EnsureFolderExists(AppSceneFolder);
            EnsureFolderExists(FrontendSceneFolder);
            EnsureFolderExists(GameplaySceneFolder);
            MigrateLegacySceneIfRequired(LegacyBootstrapScenePath, BootstrapScenePath);
            MigrateLegacySceneIfRequired(LegacyMainMenuScenePath, MainMenuScenePath);
            MigrateLegacySceneIfRequired(LegacyGameScenePath, GameScenePath);
            AssetDatabase.SaveAssets();
        }

        private static void MigrateLegacySceneIfRequired(string legacyPath, string organizedPath)
        {
            if (!File.Exists(legacyPath) || File.Exists(organizedPath))
            {
                return;
            }

            string error = AssetDatabase.MoveAsset(legacyPath, organizedPath);
            if (!string.IsNullOrEmpty(error))
            {
                throw new IOException(
                    $"Could not organize scene '{legacyPath}' as '{organizedPath}': {error}");
            }
        }

        private static void CreateBootstrapScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject bootstrap = new GameObject("AppRoot");
            SceneFlowService sceneFlow = bootstrap.AddComponent<SceneFlowService>();
            AppRoot appRoot = bootstrap.AddComponent<AppRoot>();
            BootstrapSceneLoader sceneLoader = bootstrap.AddComponent<BootstrapSceneLoader>();

            SerializedObject appRootObject = new SerializedObject(appRoot);
            appRootObject.FindProperty("_sceneFlowService").objectReferenceValue = sceneFlow;
            appRootObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject sceneLoaderObject = new SerializedObject(sceneLoader);
            sceneLoaderObject.FindProperty("_appRoot").objectReferenceValue = appRoot;
            sceneLoaderObject.ApplyModifiedPropertiesWithoutUndo();
            StartupLoadingScreenGenerator.Configure(appRoot, sceneFlow);
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera("Main Camera", new Vector3(0f, 0f, -10f), Quaternion.identity, false);
            CreateEventSystem();

            Canvas canvas = CreateCanvas("MainMenuCanvas");
            CreateMenuBackground(canvas.transform);
            CreateImage(
                canvas.transform,
                "AtmosphereOverlay",
                new Color(0.025f, 0.018f, 0.075f, 0.34f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            Image panel = CreateImage(
                canvas.transform,
                "MenuPanel",
                new Color(0.055f, 0.04f, 0.13f, 0.93f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 15f),
                new Vector2(610f, 590f));

            CreateText(
                panel.transform,
                "Title",
                "ROPOLY",
                82,
                FontStyles.Bold,
                new Color(0.93f, 0.91f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -112f),
                new Vector2(540f, 110f),
                TextAlignmentOptions.Center);

            CreateText(
                panel.transform,
                "Subtitle",
                "BUILD CITIES  •  MAKE DEALS  •  OWN THE MAP",
                18,
                FontStyles.Normal,
                new Color(0.47f, 0.86f, 0.91f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -196f),
                new Vector2(530f, 52f),
                TextAlignmentOptions.Center);

            CreateText(
                panel.transform,
                "Welcome",
                "A world of cities, countries, and clever trades awaits.",
                22,
                FontStyles.Normal,
                new Color(0.82f, 0.8f, 0.9f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 55f),
                new Vector2(490f, 90f),
                TextAlignmentOptions.Center);

            CreateNavigationButton(
                panel.transform,
                "PlayButton",
                "PLAY",
                AppSceneNames.Game,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -82f),
                new Vector2(350f, 82f),
                new Color(0.37f, 0.22f, 0.88f),
                30);

            CreateText(
                panel.transform,
                "MilestoneLabel",
                "P-1  •  FOUNDATION BUILD",
                14,
                FontStyles.Normal,
                new Color(0.53f, 0.5f, 0.66f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 38f),
                new Vector2(420f, 32f),
                TextAlignmentOptions.Center);

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void CreateGameScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // The board will live on the XZ plane. The camera is a standard 3D Camera
            // viewed from above; no 2D physics types are used anywhere in this setup.
            Camera camera = CreateCamera(
                "Main Camera",
                new Vector3(0f, 20f, 0f),
                Quaternion.Euler(90f, 0f, 0f),
                true);
            camera.orthographicSize = 9f;

            GameplayLightingGenerator.Configure(scene);

            GameObject worldRoot = new GameObject("WorldRoot");
            GameObject boardRoot = new GameObject("BoardRoot");
            boardRoot.transform.SetParent(worldRoot.transform, false);

            GameObject presentationRoot = new GameObject("GamePresentationRoot");
            presentationRoot.transform.SetParent(worldRoot.transform, false);

            CreateEventSystem();
            Canvas canvas = CreateCanvas("GameCanvas");
            CreateImage(
                canvas.transform,
                "Backdrop",
                new Color(0.025f, 0.02f, 0.07f, 1f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            CreateText(
                canvas.transform,
                "GameReadyTitle",
                "GAME SCENE READY",
                58,
                FontStyles.Bold,
                new Color(0.93f, 0.91f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 80f),
                new Vector2(900f, 100f),
                TextAlignmentOptions.Center);

            CreateText(
                canvas.transform,
                "EnvironmentLabel",
                "P-1  •  3D WORLD, 2D PRESENTATION",
                20,
                FontStyles.Normal,
                new Color(0.47f, 0.86f, 0.91f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 5f),
                new Vector2(650f, 50f),
                TextAlignmentOptions.Center);

            CreateNavigationButton(
                canvas.transform,
                "BackToMenuButton",
                "BACK TO MAIN MENU",
                AppSceneNames.MainMenu,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -115f),
                new Vector2(380f, 74f),
                new Color(0.22f, 0.16f, 0.43f),
                23);

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        private static Camera CreateCamera(
            string name,
            Vector3 position,
            Quaternion rotation,
            bool orthographic)
        {
            GameObject cameraObject = new GameObject(name);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(position, rotation);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.014f, 0.05f);
            camera.orthographic = orthographic;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject canvasObject = new GameObject(name, typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
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
            return canvas;
        }

        private static void CreateMenuBackground(Transform parent)
        {
            GameObject backgroundObject = new GameObject("GeneratedPlaceholderBackground", typeof(RectTransform));
            backgroundObject.transform.SetParent(parent, false);
            RawImage background = backgroundObject.AddComponent<RawImage>();
            background.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MenuBackgroundPath);
            background.color = background.texture == null
                ? new Color(0.06f, 0.035f, 0.14f, 1f)
                : Color.white;
            background.raycastTarget = false;
            SetRect(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
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
            int fontSize,
            FontStyles fontStyle,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (text.font == null)
            {
                throw new FileNotFoundException(
                    $"TextMesh Pro Essential Resources are missing. Expected '{FontAssetPath}'.");
            }

            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(12f, fontSize * 0.82f);
            text.fontSizeMax = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.extraPadding = true;
            SetRect(text.rectTransform, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            return text;
        }

        private static void CreateNavigationButton(
            Transform parent,
            string name,
            string label,
            string destinationScene,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color,
            int fontSize)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            Image background = buttonObject.AddComponent<Image>();
            background.color = color;

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetRect(rect, anchor, anchor, anchoredPosition, sizeDelta);

            SceneNavigationButton navigation = buttonObject.AddComponent<SceneNavigationButton>();
            SerializedObject navigationObject = new SerializedObject(navigation);
            navigationObject.FindProperty("_destinationScene").stringValue = destinationScene;
            navigationObject.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(button.onClick, navigation.LoadDestination);

            CreateText(
                buttonObject.transform,
                "Label",
                label,
                fontSize,
                FontStyles.Bold,
                Color.white,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                TextAlignmentOptions.Center);
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

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };
        }

        private static void ConfigurePlayModeStartScene()
        {
            if (UnityEngine.Application.isBatchMode)
            {
                // The Unity Test Framework needs its generated runner scene in batch mode.
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            SceneAsset bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
            if (bootstrapScene != null)
            {
                EditorSceneManager.playModeStartScene = bootstrapScene;
            }
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
