using System;
using System.Collections.Generic;
using System.IO;
using Ropoly.Infrastructure.Content;
using Ropoly.Infrastructure.Content.Players;
using Ropoly.Presentation.Board;
using Ropoly.Presentation.Lobby;
using Ropoly.Presentation.Players;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ropoly.Editor
{
    public static class P7LocalLobbyGenerator
    {
        public const string RosterPath =
            "Assets/_Ropoly/Content/Players/DefaultCreatureRoster.asset";
        public const string TokenPrefabPath =
            "Assets/_Ropoly/Prefabs/Players/CreatureToken.prefab";

        private const string CreatureFolder =
            "Assets/_Ropoly/Content/Players/Creatures/Default";
        private const string MaterialFolder =
            "Assets/_Ropoly/Art/Materials/Creatures";
        private const string RulesetPath =
            "Assets/_Ropoly/Content/Rulesets/ClassicRopoly.asset";

        private static readonly CreatureSeed[] CreatureSeeds =
        {
            new CreatureSeed("pip", "Pip", "#55D68D"),
            new CreatureSeed("bubu", "Bubu", "#56A8FF"),
            new CreatureSeed("sunny", "Sunny", "#FFD35A"),
            new CreatureSeed("peach", "Peach", "#FF727E"),
            new CreatureSeed("mochi", "Mochi", "#A77CFF"),
            new CreatureSeed("nibi", "Nibi", "#35D5C5"),
            new CreatureSeed("poppy", "Poppy", "#F77CCB"),
            new CreatureSeed("bean", "Bean", "#FF9C50"),
        };

        private static TMP_FontAsset _font;
        private static Sprite _roundSprite;

        [MenuItem("Ropoly/Development/Generate P-7 Local Lobby")]
        public static void GenerateLocalLobby()
        {
            if (UnityEngine.Application.isPlaying)
            {
                Debug.LogWarning("The local lobby cannot be generated while the game is running.");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isDirty)
            {
                Debug.LogWarning("Save the current scene before generating the local lobby.");
                return;
            }

            P6BoardPreviewGenerator.GenerateBoardPreview();
            EnsureFolders();
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(P6BoardPreviewGenerator.FontAssetPath);
            if (_font == null)
            {
                throw new FileNotFoundException("The TextMesh Pro SDF font asset is missing.");
            }

            _roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Material bodyMaterial = CreateOrLoadMaterial(
                $"{MaterialFolder}/CreatureBodyBase.mat",
                Color.white);
            Material eyeMaterial = CreateOrLoadMaterial(
                $"{MaterialFolder}/CreatureEye.mat",
                Color.white);
            Material pupilMaterial = CreateOrLoadMaterial(
                $"{MaterialFolder}/CreaturePupil.mat",
                new Color(0.025f, 0.02f, 0.04f, 1f));

            GameObject tokenPrefab = CreateTokenPrefab(bodyMaterial, eyeMaterial, pupilMaterial);
            List<CreatureDefinition> creatures = CreateCreatureDefinitions(tokenPrefab);
            CreatureRosterDefinition roster = CreateRoster(creatures);
            CreateGameLobby(roster);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!UnityEngine.Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(P6BoardPreviewGenerator.GameScenePath, OpenSceneMode.Single);
            }

            Debug.Log("Generated P-7 local match setup, creature selection, and 3D player spawning.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Ropoly/Content/Players");
            EnsureFolder("Assets/_Ropoly/Content/Players/Creatures");
            EnsureFolder(CreatureFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/_Ropoly/Prefabs/Players");
        }

        private static Material CreateOrLoadMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                if (shader == null)
                {
                    throw new InvalidOperationException("No supported 3D material shader was found.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateTokenPrefab(
            Material bodyMaterial,
            Material eyeMaterial,
            Material pupilMaterial)
        {
            GameObject root = new GameObject("CreatureToken");
            SphereCollider bodyCollider = root.AddComponent<SphereCollider>();
            bodyCollider.center = new Vector3(0f, 0.34f, 0f);
            bodyCollider.radius = 0.37f;

            Renderer body = CreateSphere(
                root.transform,
                "Round Body",
                new Vector3(0f, 0.34f, 0f),
                new Vector3(0.72f, 0.62f, 0.72f),
                bodyMaterial);
            Renderer leftEye = CreateSphere(
                root.transform,
                "Left Eye",
                new Vector3(-0.17f, 0.67f, -0.20f),
                new Vector3(0.19f, 0.09f, 0.23f),
                eyeMaterial);
            Renderer rightEye = CreateSphere(
                root.transform,
                "Right Eye",
                new Vector3(0.17f, 0.67f, -0.20f),
                new Vector3(0.19f, 0.09f, 0.23f),
                eyeMaterial);
            Renderer leftPupil = CreateSphere(
                root.transform,
                "Left Pupil",
                new Vector3(-0.17f, 0.73f, -0.24f),
                new Vector3(0.075f, 0.045f, 0.10f),
                pupilMaterial);
            Renderer rightPupil = CreateSphere(
                root.transform,
                "Right Pupil",
                new Vector3(0.17f, 0.73f, -0.24f),
                new Vector3(0.075f, 0.045f, 0.10f),
                pupilMaterial);

            CreatureTokenView view = root.AddComponent<CreatureTokenView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("_bodyRenderer").objectReferenceValue = body;
            SetObjectArray(serializedView.FindProperty("_eyeRenderers"), new[] { leftEye, rightEye });
            SetObjectArray(
                serializedView.FindProperty("_pupilRenderers"),
                new[] { leftPupil, rightPupil });
            serializedView.FindProperty("_bodyCollider").objectReferenceValue = bodyCollider;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, TokenPrefabPath);
            Object.DestroyImmediate(root);
            if (prefab == null)
            {
                throw new IOException($"Could not save creature prefab at '{TokenPrefabPath}'.");
            }

            return prefab;
        }

        private static Renderer CreateSphere(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localScale = localScale;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            Renderer renderer = sphere.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return renderer;
        }

        private static List<CreatureDefinition> CreateCreatureDefinitions(GameObject tokenPrefab)
        {
            List<CreatureDefinition> creatures = new List<CreatureDefinition>(CreatureSeeds.Length);
            foreach (CreatureSeed seed in CreatureSeeds)
            {
                string path = $"{CreatureFolder}/{seed.DisplayName}.asset";
                CreatureDefinition definition = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(path);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<CreatureDefinition>();
                    AssetDatabase.CreateAsset(definition, path);
                }

                if (!ColorUtility.TryParseHtmlString(seed.ColorHex, out Color bodyColor))
                {
                    throw new InvalidDataException($"Invalid creature color '{seed.ColorHex}'.");
                }

                SerializedObject serializedDefinition = new SerializedObject(definition);
                serializedDefinition.FindProperty("_creatureId").stringValue = seed.Id;
                serializedDefinition.FindProperty("_displayName").stringValue = seed.DisplayName;
                serializedDefinition.FindProperty("_bodyColor").colorValue = bodyColor;
                serializedDefinition.FindProperty("_eyeColor").colorValue = Color.white;
                serializedDefinition.FindProperty("_pupilColor").colorValue =
                    new Color(0.025f, 0.02f, 0.04f, 1f);
                serializedDefinition.FindProperty("_tokenPrefab").objectReferenceValue = tokenPrefab;
                serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);
                creatures.Add(definition);
            }

            return creatures;
        }

        private static CreatureRosterDefinition CreateRoster(IReadOnlyList<CreatureDefinition> creatures)
        {
            CreatureRosterDefinition roster =
                AssetDatabase.LoadAssetAtPath<CreatureRosterDefinition>(RosterPath);
            if (roster == null)
            {
                roster = ScriptableObject.CreateInstance<CreatureRosterDefinition>();
                AssetDatabase.CreateAsset(roster, RosterPath);
            }

            SerializedObject serializedRoster = new SerializedObject(roster);
            SetObjectArray(serializedRoster.FindProperty("_creatures"), creatures);
            serializedRoster.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(roster);
            return roster;
        }

        private static void CreateGameLobby(CreatureRosterDefinition roster)
        {
            Scene scene = EditorSceneManager.OpenScene(
                P6BoardPreviewGenerator.GameScenePath,
                OpenSceneMode.Single);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            BoardPreviewController board = Object.FindFirstObjectByType<BoardPreviewController>();
            GameRulesetDefinition ruleset =
                AssetDatabase.LoadAssetAtPath<GameRulesetDefinition>(RulesetPath);
            if (!scene.IsValid() || canvas == null || board == null || ruleset == null)
            {
                throw new MissingReferenceException(
                    "The generated Game scene, board preview, canvas, or default ruleset is missing.");
            }

            TMP_Text footer = canvas.transform.Find("Footer")?.GetComponent<TMP_Text>();
            if (footer != null)
            {
                footer.text = "LOCAL MATCH SETUP  •  ONLINE SYNCHRONIZATION COMES NEXT";
            }

            TMP_Text headerBadge =
                canvas.transform.Find("Header/PreviewBadge")?.GetComponent<TMP_Text>();
            if (headerBadge != null)
            {
                headerBadge.text = "LOCAL MATCH SETUP";
            }

            GameObject tokenRootObject = new GameObject("Spawned Player Tokens");
            tokenRootObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            GameObject systemObject = new GameObject("Local Match System");
            LocalLobbyController controller = systemObject.AddComponent<LocalLobbyController>();

            GameObject matchHud;
            List<PlayerHudRowView> hudRows;
            CreateMatchHud(canvas.transform, out matchHud, out hudRows);

            LobbyInterface lobby = CreateLobbyInterface(canvas.transform, roster.Creatures);

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_ruleset").objectReferenceValue = ruleset;
            serializedController.FindProperty("_creatureRoster").objectReferenceValue = roster;
            serializedController.FindProperty("_boardPreview").objectReferenceValue = board;
            serializedController.FindProperty("_spawnedTokenRoot").objectReferenceValue =
                tokenRootObject.transform;
            serializedController.FindProperty("_lobbyOverlay").objectReferenceValue = lobby.Root;
            serializedController.FindProperty("_selectionPrompt").objectReferenceValue = lobby.Prompt;
            serializedController.FindProperty("_readyStatus").objectReferenceValue = lobby.ReadyStatus;
            serializedController.FindProperty("_startButton").objectReferenceValue = lobby.StartButton;
            serializedController.FindProperty("_startButtonLabel").objectReferenceValue =
                lobby.StartButtonLabel;
            SetObjectArray(serializedController.FindProperty("_playerSlots"), lobby.PlayerSlots);
            SetObjectArray(
                serializedController.FindProperty("_creatureChoices"),
                lobby.CreatureChoices);
            SetObjectArray(
                serializedController.FindProperty("_playerCountOptions"),
                lobby.PlayerCountOptions);
            serializedController.FindProperty("_matchHudPanel").objectReferenceValue = matchHud;
            SetObjectArray(serializedController.FindProperty("_hudRows"), hudRows);
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            matchHud.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, P6BoardPreviewGenerator.GameScenePath);
        }

        private static LobbyInterface CreateLobbyInterface(
            Transform canvas,
            IReadOnlyList<CreatureDefinition> creatures)
        {
            Image overlay = CreateImage(
                canvas,
                "CreatureSelectionLobby",
                new Color(0.016f, 0.010f, 0.030f, 0.94f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                null,
                true);
            RectTransform overlayRect = overlay.rectTransform;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = new Vector2(0f, -84f);

            Image modal = CreateImage(
                overlay.transform,
                "LobbyPanel",
                new Color(0.060f, 0.040f, 0.110f, 0.985f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f),
                new Vector2(1180f, 850f),
                null,
                true);
            Outline modalOutline = modal.gameObject.AddComponent<Outline>();
            modalOutline.effectColor = new Color(0.36f, 0.24f, 0.61f, 0.85f);
            modalOutline.effectDistance = new Vector2(2f, -2f);

            CreateText(
                modal.transform,
                "Title",
                "CHOOSE YOUR CREATURES",
                36,
                FontStyles.Bold,
                new Color(0.97f, 0.95f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 378f),
                new Vector2(700f, 52f),
                TextAlignmentOptions.Center);
            CreateText(
                modal.transform,
                "Subtitle",
                "EVERY PLAYER PICKS A DIFFERENT ROUND FRIEND",
                15,
                FontStyles.Bold,
                new Color(0.50f, 0.91f, 0.84f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 340f),
                new Vector2(700f, 30f),
                TextAlignmentOptions.Center);

            CreateText(
                modal.transform,
                "PlayerCountLabel",
                "PLAYERS",
                15,
                FontStyles.Bold,
                new Color(0.68f, 0.64f, 0.79f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-180f, 298f),
                new Vector2(130f, 34f),
                TextAlignmentOptions.MidlineRight);

            List<PlayerCountOptionView> countOptions = new List<PlayerCountOptionView>();
            for (int count = 2; count <= 4; count++)
            {
                Button button = CreateButton(
                    modal.transform,
                    $"{count}Players",
                    new Vector2(-70f + ((count - 2) * 72f), 298f),
                    new Vector2(58f, 42f),
                    new Color(0.11f, 0.08f, 0.19f, 1f));
                TMP_Text label = CreateText(
                    button.transform,
                    "Label",
                    count.ToString(),
                    20,
                    FontStyles.Bold,
                    Color.white,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero,
                    TextAlignmentOptions.Center);
                PlayerCountOptionView view = button.gameObject.AddComponent<PlayerCountOptionView>();
                SerializedObject serializedView = new SerializedObject(view);
                serializedView.FindProperty("_playerCount").intValue = count;
                serializedView.FindProperty("_button").objectReferenceValue = button;
                serializedView.FindProperty("_background").objectReferenceValue =
                    button.GetComponent<Image>();
                serializedView.FindProperty("_label").objectReferenceValue = label;
                serializedView.ApplyModifiedPropertiesWithoutUndo();
                countOptions.Add(view);
            }

            List<LocalPlayerSlotView> slots = new List<LocalPlayerSlotView>();
            float[] slotX = { -405f, -135f, 135f, 405f };
            for (int index = 0; index < 4; index++)
            {
                slots.Add(CreatePlayerSlot(modal.transform, index, new Vector2(slotX[index], 225f)));
            }

            TMP_Text prompt = CreateText(
                modal.transform,
                "SelectionPrompt",
                "PLAYER 1  •  CHOOSE YOUR CREATURE",
                19,
                FontStyles.Bold,
                new Color(0.96f, 0.95f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 150f),
                new Vector2(700f, 36f),
                TextAlignmentOptions.Center);

            List<CreatureChoiceView> choices = new List<CreatureChoiceView>();
            float[] cardX = { -405f, -135f, 135f, 405f };
            float[] cardY = { 40f, -145f };
            for (int index = 0; index < creatures.Count; index++)
            {
                int row = index / 4;
                int column = index % 4;
                choices.Add(CreateCreatureChoice(
                    modal.transform,
                    creatures[index],
                    new Vector2(cardX[column], cardY[row])));
            }

            TMP_Text readyStatus = CreateText(
                modal.transform,
                "ReadyStatus",
                "0 / 4 PLAYERS READY",
                16,
                FontStyles.Bold,
                new Color(0.50f, 0.91f, 0.84f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-330f, -316f),
                new Vector2(360f, 44f),
                TextAlignmentOptions.MidlineLeft);

            Button startButton = CreateButton(
                modal.transform,
                "StartMatchButton",
                new Vector2(350f, -316f),
                new Vector2(360f, 62f),
                new Color(0.31f, 0.19f, 0.57f, 1f));
            TMP_Text startLabel = CreateText(
                startButton.transform,
                "Label",
                "SELECT 4 MORE",
                18,
                FontStyles.Bold,
                Color.white,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                TextAlignmentOptions.Center);

            CreateText(
                modal.transform,
                "Footnote",
                "LOCAL FOUNDATION  •  THE HOST WILL AUTHORIZE THESE CHOICES ONLINE",
                12,
                FontStyles.Bold,
                new Color(0.52f, 0.48f, 0.64f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -382f),
                new Vector2(720f, 28f),
                TextAlignmentOptions.Center);

            return new LobbyInterface(
                overlay.gameObject,
                prompt,
                readyStatus,
                startButton,
                startLabel,
                slots,
                choices,
                countOptions);
        }

        private static LocalPlayerSlotView CreatePlayerSlot(
            Transform parent,
            int index,
            Vector2 position)
        {
            Button button = CreateButton(
                parent,
                $"PlayerSlot{index + 1}",
                position,
                new Vector2(250f, 76f),
                new Color(0.11f, 0.08f, 0.19f, 1f));
            Image body = CreateRoundImage(
                button.transform,
                "CreatureBody",
                Color.white,
                new Vector2(-88f, 0f),
                new Vector2(50f, 50f));
            List<Image> pupils = CreateFace(body.transform, 0.40f);
            body.gameObject.SetActive(false);

            TMP_Text name = CreateText(
                button.transform,
                "PlayerName",
                $"PLAYER {index + 1}",
                15,
                FontStyles.Bold,
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(28f, 13f),
                new Vector2(160f, 25f),
                TextAlignmentOptions.MidlineLeft);
            TMP_Text selection = CreateText(
                button.transform,
                "Selection",
                "CHOOSE A CREATURE",
                11,
                FontStyles.Bold,
                new Color(0.58f, 0.54f, 0.69f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(28f, -14f),
                new Vector2(160f, 22f),
                TextAlignmentOptions.MidlineLeft);

            LocalPlayerSlotView view = button.gameObject.AddComponent<LocalPlayerSlotView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("_button").objectReferenceValue = button;
            serializedView.FindProperty("_background").objectReferenceValue =
                button.GetComponent<Image>();
            serializedView.FindProperty("_creatureBody").objectReferenceValue = body;
            SetObjectArray(serializedView.FindProperty("_creatureEyes"), pupils);
            serializedView.FindProperty("_playerName").objectReferenceValue = name;
            serializedView.FindProperty("_selectionName").objectReferenceValue = selection;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static CreatureChoiceView CreateCreatureChoice(
            Transform parent,
            CreatureDefinition definition,
            Vector2 position)
        {
            Button button = CreateButton(
                parent,
                $"Creature-{definition.DisplayName}",
                position,
                new Vector2(250f, 155f),
                new Color(0.105f, 0.075f, 0.18f, 1f));
            Image body = CreateRoundImage(
                button.transform,
                "RoundBody",
                definition.BodyColor,
                new Vector2(0f, 18f),
                new Vector2(78f, 78f));
            List<Image> pupils = CreateFace(body.transform, 0.28f);
            TMP_Text name = CreateText(
                button.transform,
                "Name",
                definition.DisplayName.ToUpperInvariant(),
                16,
                FontStyles.Bold,
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -42f),
                new Vector2(210f, 26f),
                TextAlignmentOptions.Center);
            TMP_Text state = CreateText(
                button.transform,
                "State",
                "AVAILABLE",
                10,
                FontStyles.Bold,
                new Color(0.50f, 0.91f, 0.84f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -63f),
                new Vector2(210f, 20f),
                TextAlignmentOptions.Center);

            CreatureChoiceView view = button.gameObject.AddComponent<CreatureChoiceView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("_button").objectReferenceValue = button;
            serializedView.FindProperty("_background").objectReferenceValue =
                button.GetComponent<Image>();
            serializedView.FindProperty("_body").objectReferenceValue = body;
            SetObjectArray(serializedView.FindProperty("_eyes"), pupils);
            serializedView.FindProperty("_nameLabel").objectReferenceValue = name;
            serializedView.FindProperty("_stateLabel").objectReferenceValue = state;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void CreateMatchHud(
            Transform canvas,
            out GameObject panelObject,
            out List<PlayerHudRowView> rows)
        {
            Image panel = CreateImage(
                canvas,
                "LocalMatchHud",
                new Color(0.075f, 0.052f, 0.135f, 0.97f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-190f, -12f),
                new Vector2(320f, 680f),
                null,
                true);
            panelObject = panel.gameObject;
            CreateText(
                panel.transform,
                "Title",
                "LOCAL MATCH",
                21,
                FontStyles.Bold,
                Color.white,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -46f),
                new Vector2(270f, 38f),
                TextAlignmentOptions.Center);
            CreateText(
                panel.transform,
                "Status",
                "CREATURES ON THE START SPACE",
                11,
                FontStyles.Bold,
                new Color(0.50f, 0.91f, 0.84f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -80f),
                new Vector2(270f, 24f),
                TextAlignmentOptions.Center);

            rows = new List<PlayerHudRowView>();
            for (int index = 0; index < 4; index++)
            {
                Image row = CreateImage(
                    panel.transform,
                    $"Player{index + 1}",
                    new Color(0.11f, 0.08f, 0.19f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -145f - (index * 108f)),
                    new Vector2(270f, 88f),
                    null,
                    false);
                Image body = CreateRoundImage(
                    row.transform,
                    "CreatureBody",
                    Color.white,
                    new Vector2(-92f, 0f),
                    new Vector2(58f, 58f));
                List<Image> pupils = CreateFace(body.transform, 0.36f);
                TMP_Text playerName = CreateText(
                    row.transform,
                    "PlayerName",
                    $"PLAYER {index + 1}",
                    15,
                    FontStyles.Bold,
                    Color.white,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(34f, 14f),
                    new Vector2(150f, 27f),
                    TextAlignmentOptions.MidlineLeft);
                TMP_Text cash = CreateText(
                    row.transform,
                    "Cash",
                    "$1,500",
                    14,
                    FontStyles.Bold,
                    new Color(0.50f, 0.91f, 0.84f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(34f, -16f),
                    new Vector2(150f, 25f),
                    TextAlignmentOptions.MidlineLeft);
                PlayerHudRowView view = row.gameObject.AddComponent<PlayerHudRowView>();
                SerializedObject serializedView = new SerializedObject(view);
                serializedView.FindProperty("_creatureBody").objectReferenceValue = body;
                SetObjectArray(serializedView.FindProperty("_creatureEyes"), pupils);
                serializedView.FindProperty("_playerName").objectReferenceValue = playerName;
                serializedView.FindProperty("_cashLabel").objectReferenceValue = cash;
                serializedView.ApplyModifiedPropertiesWithoutUndo();
                rows.Add(view);
            }

            CreateText(
                panel.transform,
                "NextStep",
                "NEXT: DICE + MOVEMENT",
                12,
                FontStyles.Bold,
                new Color(0.94f, 0.31f, 0.66f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 40f),
                new Vector2(270f, 28f),
                TextAlignmentOptions.Center);
        }

        private static List<Image> CreateFace(Transform body, float eyeScale)
        {
            Image leftWhite = CreateRoundImage(
                body,
                "LeftEyeWhite",
                Color.white,
                new Vector2(-0.19f * body.GetComponent<RectTransform>().sizeDelta.x, 0.10f * body.GetComponent<RectTransform>().sizeDelta.y),
                body.GetComponent<RectTransform>().sizeDelta * new Vector2(eyeScale, eyeScale * 1.18f));
            Image rightWhite = CreateRoundImage(
                body,
                "RightEyeWhite",
                Color.white,
                new Vector2(0.19f * body.GetComponent<RectTransform>().sizeDelta.x, 0.10f * body.GetComponent<RectTransform>().sizeDelta.y),
                body.GetComponent<RectTransform>().sizeDelta * new Vector2(eyeScale, eyeScale * 1.18f));
            Image leftPupil = CreateRoundImage(
                leftWhite.transform,
                "Pupil",
                new Color(0.025f, 0.02f, 0.04f, 1f),
                new Vector2(0f, -1f),
                leftWhite.rectTransform.sizeDelta * 0.42f);
            Image rightPupil = CreateRoundImage(
                rightWhite.transform,
                "Pupil",
                new Color(0.025f, 0.02f, 0.04f, 1f),
                new Vector2(0f, -1f),
                rightWhite.rectTransform.sizeDelta * 0.42f);
            return new List<Image> { leftPupil, rightPupil };
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            Image background = CreateImage(
                parent,
                name,
                color,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                position,
                size,
                null,
                true);
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 0.78f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            return button;
        }

        private static Image CreateRoundImage(
            Transform parent,
            string name,
            Color color,
            Vector2 position,
            Vector2 size)
        {
            Image image = CreateImage(
                parent,
                name,
                color,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                position,
                size,
                _roundSprite,
                false);
            image.preserveAspect = true;
            return image;
        }

        private static Image CreateImage(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Sprite sprite,
            bool raycastTarget)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = sprite == null ? Image.Type.Simple : Image.Type.Sliced;
            image.raycastTarget = raycastTarget;
            SetRect(image.rectTransform, anchorMin, anchorMax, position, size);
            return image;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            FontStyles style,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = _font;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(9f, fontSize * 0.78f);
            text.fontSizeMax = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.extraPadding = true;
            SetRect(text.rectTransform, anchorMin, anchorMax, position, size);
            return text;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetObjectArray<T>(SerializedProperty property, IReadOnlyList<T> values)
            where T : Object
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
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

        private readonly struct CreatureSeed
        {
            public CreatureSeed(string id, string displayName, string colorHex)
            {
                Id = id;
                DisplayName = displayName;
                ColorHex = colorHex;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string ColorHex { get; }
        }

        private sealed class LobbyInterface
        {
            public LobbyInterface(
                GameObject root,
                TMP_Text prompt,
                TMP_Text readyStatus,
                Button startButton,
                TMP_Text startButtonLabel,
                List<LocalPlayerSlotView> playerSlots,
                List<CreatureChoiceView> creatureChoices,
                List<PlayerCountOptionView> playerCountOptions)
            {
                Root = root;
                Prompt = prompt;
                ReadyStatus = readyStatus;
                StartButton = startButton;
                StartButtonLabel = startButtonLabel;
                PlayerSlots = playerSlots;
                CreatureChoices = creatureChoices;
                PlayerCountOptions = playerCountOptions;
            }

            public GameObject Root { get; }
            public TMP_Text Prompt { get; }
            public TMP_Text ReadyStatus { get; }
            public Button StartButton { get; }
            public TMP_Text StartButtonLabel { get; }
            public List<LocalPlayerSlotView> PlayerSlots { get; }
            public List<CreatureChoiceView> CreatureChoices { get; }
            public List<PlayerCountOptionView> PlayerCountOptions { get; }
        }
    }
}
