using System.IO;
using Ropoly.Bootstrap;
using Ropoly.Infrastructure.Content.Board;
using Ropoly.Presentation.Board;
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
    public static class P6BoardPreviewGenerator
    {
        public const string BootstrapScenePath = "Assets/_Ropoly/Scenes/App/Bootstrap.unity";
        public const string MainMenuScenePath = "Assets/_Ropoly/Scenes/Frontend/MainMenu.unity";
        public const string GameScenePath = "Assets/_Ropoly/Scenes/Gameplay/Game.unity";
        public const string ThemePath =
            "Assets/_Ropoly/Settings/Presentation/BoardPreviewTheme.asset";
        public const string PrefabPath =
            "Assets/_Ropoly/Prefabs/Board/BoardPreviewRoot.prefab";
        public const string FontAssetPath =
            "Assets/_Ropoly/Art/Fonts/Ropoly UI SDF.asset";

        private const string LegacyBootstrapScenePath = "Assets/_Ropoly/Scenes/Bootstrap.unity";
        private const string LegacyMainMenuScenePath = "Assets/_Ropoly/Scenes/MainMenu.unity";
        private const string LegacyGameScenePath = "Assets/_Ropoly/Scenes/Game.unity";
        private static TMP_FontAsset _interfaceFont;

        [MenuItem("Ropoly/Development/Generate P-6 Board Preview")]
        public static void GenerateBoardPreview()
        {
            if (UnityEngine.Application.isPlaying)
            {
                Debug.LogWarning("The board preview cannot be generated while the game is running.");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isDirty)
            {
                Debug.LogWarning("Save the current scene before generating the board preview.");
                return;
            }

            OrganizeProjectFoldersAndScenes();
            TMP_FontAsset font = LoadFontAsset();
            UpgradeSceneTypography(MainMenuScenePath, font);

            BoardDefinition board = AssetDatabase.LoadAssetAtPath<BoardDefinition>(
                P5DefaultBoardGenerator.DefaultBoardPath);
            if (board == null)
            {
                P5DefaultBoardGenerator.CreateDefaultWorldBoard();
                board = AssetDatabase.LoadAssetAtPath<BoardDefinition>(
                    P5DefaultBoardGenerator.DefaultBoardPath);
            }

            if (board == null)
            {
                throw new FileNotFoundException("The P-5 default world board could not be loaded.");
            }

            BoardPreviewTheme theme = CreateOrLoadTheme(font);
            GameObject previewPrefab = CreateOrUpdatePreviewPrefab(board, theme);
            CreateGameScene(previewPrefab, font);
            ConfigureBuildSettings();
            ConfigurePlayModeStartScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!UnityEngine.Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            }

            Debug.Log("Generated the P-6 visible 3D board preview and organized project scenes.");
        }

        [MenuItem("Ropoly/Development/Capture P-6 Board Preview")]
        public static void CaptureBoardPreview()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            BoardPreviewController controller = Object.FindFirstObjectByType<BoardPreviewController>();
            Camera camera = Camera.main;
            if (!scene.IsValid() || controller == null || camera == null)
            {
                throw new MissingReferenceException("The generated Game scene is missing its preview controller or camera.");
            }

            controller.RebuildPreview();
            Canvas.ForceUpdateCanvases();

            const int width = 1920;
            const int height = 1080;
            RenderTexture renderTexture = new RenderTexture(width, height, 24);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();

            string outputFolder = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            string outputPath = Path.Combine(outputFolder, "P6BoardPreview.png");
            File.WriteAllBytes(outputPath, image.EncodeToPNG());

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(renderTexture);
            Debug.Log($"Captured board preview at '{outputPath}'.");
        }

        private static void OrganizeProjectFoldersAndScenes()
        {
            EnsureFolder("Assets/_Ropoly/Scenes/App");
            EnsureFolder("Assets/_Ropoly/Scenes/Frontend");
            EnsureFolder("Assets/_Ropoly/Scenes/Gameplay");
            EnsureFolder("Assets/_Ropoly/Settings/Presentation");
            EnsureFolder("Assets/_Ropoly/Prefabs/Board");

            MoveAssetIfNeeded(LegacyBootstrapScenePath, BootstrapScenePath);
            MoveAssetIfNeeded(LegacyMainMenuScenePath, MainMenuScenePath);
            MoveAssetIfNeeded(LegacyGameScenePath, GameScenePath);
        }

        private static TMP_FontAsset LoadFontAsset()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                throw new FileNotFoundException(
                    $"TextMesh Pro Essential Resources are missing. Expected '{FontAssetPath}'.");
            }

            return fontAsset;
        }

        private static void UpgradeSceneTypography(string scenePath, TMP_FontAsset font)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Text[] legacyTexts = Object.FindObjectsByType<Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (legacyTexts.Length == 0)
            {
                return;
            }

            foreach (Text legacyText in legacyTexts)
            {
                GameObject textObject = legacyText.gameObject;
                string value = legacyText.text;
                float fontSize = legacyText.fontSize;
                FontStyles fontStyle = ConvertFontStyle(legacyText.fontStyle);
                Color color = legacyText.color;
                TextAlignmentOptions alignment = ConvertAlignment(legacyText.alignment);
                bool raycastTarget = legacyText.raycastTarget;
                bool enabled = legacyText.enabled;

                Object.DestroyImmediate(legacyText, true);
                TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
                text.text = value;
                text.font = font;
                text.fontSize = fontSize;
                text.enableAutoSizing = true;
                text.fontSizeMin = Mathf.Max(12f, fontSize * 0.82f);
                text.fontSizeMax = fontSize;
                text.fontStyle = fontStyle;
                text.color = color;
                text.alignment = alignment;
                text.raycastTarget = raycastTarget;
                text.enabled = enabled;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Ellipsis;
                text.extraPadding = true;
                EditorUtility.SetDirty(text);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static FontStyles ConvertFontStyle(FontStyle style)
        {
            return style switch
            {
                FontStyle.Bold => FontStyles.Bold,
                FontStyle.Italic => FontStyles.Italic,
                FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
                _ => FontStyles.Normal,
            };
        }

        private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
        {
            return alignment switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.MidlineLeft,
                TextAnchor.MiddleRight => TextAlignmentOptions.MidlineRight,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Center,
            };
        }

        private static BoardPreviewTheme CreateOrLoadTheme(TMP_FontAsset font)
        {
            BoardPreviewTheme theme = AssetDatabase.LoadAssetAtPath<BoardPreviewTheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<BoardPreviewTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            SerializedObject serializedTheme = new SerializedObject(theme);
            serializedTheme.FindProperty("_font").objectReferenceValue = font;
            serializedTheme.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static GameObject CreateOrUpdatePreviewPrefab(
            BoardDefinition board,
            BoardPreviewTheme theme)
        {
            GameObject previewRoot = new GameObject("BoardPreviewRoot");
            BoardPreviewController controller = previewRoot.AddComponent<BoardPreviewController>();
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_boardDefinition").objectReferenceValue = board;
            serializedController.FindProperty("_theme").objectReferenceValue = theme;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(previewRoot, PrefabPath);
            Object.DestroyImmediate(previewRoot);
            if (prefab == null)
            {
                throw new IOException($"Could not save board preview prefab at '{PrefabPath}'.");
            }

            return prefab;
        }

        private static void CreateGameScene(GameObject previewPrefab, TMP_FontAsset font)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = CreateCamera();
            PrefabUtility.InstantiatePrefab(previewPrefab, scene);
            CreateEventSystem();
            CreateInterface(camera, font);
            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(
                new Vector3(0f, 24f, 0f),
                Quaternion.Euler(90f, 0f, 0f));
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.012f, 0.035f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 10.25f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.allowHDR = true;
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

        private static void CreateInterface(Camera camera, TMP_FontAsset font)
        {
            _interfaceFont = font != null
                ? font
                : throw new MissingReferenceException("The board interface requires an SDF font asset.");
            GameObject canvasObject = new GameObject("GameCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 100;
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

            Image header = CreateImage(
                canvas.transform,
                "Header",
                new Color(0.055f, 0.038f, 0.105f, 0.96f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -42f),
                new Vector2(0f, 84f));
            CreateText(
                header.transform,
                "Logo",
                "ROPOLY",
                38,
                FontStyles.Bold,
                new Color(0.96f, 0.95f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(102f, 0f),
                new Vector2(180f, 60f),
                TextAlignmentOptions.MidlineLeft);
            CreateText(
                header.transform,
                "PreviewBadge",
                "LIVE BOARD PREVIEW",
                17,
                FontStyles.Bold,
                new Color(0.50f, 0.91f, 0.84f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(300f, 44f),
                TextAlignmentOptions.Center);
            CreateNavigationButton(
                header.transform,
                "BackToMenuButton",
                "BACK TO MENU",
                AppSceneNames.MainMenu,
                new Vector2(1f, 0.5f),
                new Vector2(-145f, 0f),
                new Vector2(230f, 52f),
                new Color(0.25f, 0.16f, 0.48f),
                18);

            Image leftPanel = CreatePanel(
                canvas.transform,
                "ValueOrderPanel",
                new Vector2(0f, 0.5f),
                new Vector2(190f, -12f),
                new Vector2(320f, 680f));
            CreateText(
                leftPanel.transform,
                "Title",
                "COUNTRY VALUE ORDER",
                19,
                FontStyles.Bold,
                new Color(0.94f, 0.91f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -46f),
                new Vector2(270f, 40f),
                TextAlignmentOptions.Center);
            CreateText(
                leftPanel.transform,
                "Order",
                "1   INDIA\n\n2   BRAZIL\n\n3   CHINA\n\n4   RUSSIA\n\n" +
                "5   ITALY\n\n6   FRANCE\n\n7   UNITED KINGDOM\n\n8   UNITED STATES",
                18,
                FontStyles.Normal,
                new Color(0.73f, 0.70f, 0.84f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -8f),
                new Vector2(245f, 520f),
                TextAlignmentOptions.MidlineLeft);
            CreateText(
                leftPanel.transform,
                "Source",
                "2024 GDP PER CAPITA\nWORLD BANK SNAPSHOT",
                13,
                FontStyles.Bold,
                new Color(0.50f, 0.91f, 0.84f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 50f),
                new Vector2(250f, 54f),
                TextAlignmentOptions.Center);

            Image rightPanel = CreatePanel(
                canvas.transform,
                "BoardContentPanel",
                new Vector2(1f, 0.5f),
                new Vector2(-190f, -12f),
                new Vector2(320f, 680f));
            CreateText(
                rightPanel.transform,
                "Title",
                "BOARD CONTENT",
                19,
                FontStyles.Bold,
                new Color(0.94f, 0.91f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -46f),
                new Vector2(270f, 40f),
                TextAlignmentOptions.Center);
            CreateText(
                rightPanel.transform,
                "Counts",
                "40\nSPACES\n\n22\nCITIES\n\n8\nCOUNTRY SETS\n\n4\nAIRPORTS\n\n2\nUTILITIES",
                20,
                FontStyles.Bold,
                new Color(0.73f, 0.70f, 0.84f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 8f),
                new Vector2(250f, 535f),
                TextAlignmentOptions.Center);
            CreateText(
                rightPanel.transform,
                "Mode",
                "3D OBJECTS  •  TOP-DOWN VIEW",
                13,
                FontStyles.Bold,
                new Color(0.94f, 0.31f, 0.66f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 42f),
                new Vector2(270f, 34f),
                TextAlignmentOptions.Center);

            CreateText(
                canvas.transform,
                "Footer",
                "VISIBLE DATA PREVIEW  •  DICE, TOKENS, AND GAMEPLAY COME NEXT",
                15,
                FontStyles.Bold,
                new Color(0.62f, 0.58f, 0.75f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 27f),
                new Vector2(800f, 34f),
                TextAlignmentOptions.Center);
        }

        private static Image CreatePanel(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            return CreateImage(
                parent,
                name,
                new Color(0.075f, 0.052f, 0.135f, 0.93f),
                anchor,
                anchor,
                position,
                size);
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
            text.font = _interfaceFont;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(10f, fontSize * 0.82f);
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

            SetRect(buttonObject.GetComponent<RectTransform>(), anchor, anchor, anchoredPosition, sizeDelta);
            SceneNavigationButton navigation = buttonObject.AddComponent<SceneNavigationButton>();
            SerializedObject serializedNavigation = new SerializedObject(navigation);
            serializedNavigation.FindProperty("_destinationScene").stringValue = destinationScene;
            serializedNavigation.ApplyModifiedPropertiesWithoutUndo();
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
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            EditorSceneManager.playModeStartScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
        }

        private static void MoveAssetIfNeeded(string legacyPath, string organizedPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(legacyPath) == null ||
                AssetDatabase.LoadAssetAtPath<Object>(organizedPath) != null)
            {
                return;
            }

            string error = AssetDatabase.MoveAsset(legacyPath, organizedPath);
            if (!string.IsNullOrEmpty(error))
            {
                throw new IOException(
                    $"Could not organize '{legacyPath}' as '{organizedPath}': {error}");
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
