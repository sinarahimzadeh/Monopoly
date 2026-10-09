using System;
using System.Collections.Generic;
using System.IO;
using Ropoly.Presentation.Board;
using Ropoly.Presentation.Dice;
using Ropoly.Presentation.Lobby;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ropoly.Editor
{
    public static class P8DiceGenerator
    {
        public const string SettingsPath =
            "Assets/_Ropoly/Settings/Presentation/DicePresentationSettings.asset";
        public const string MeshPath =
            "Assets/_Ropoly/Art/Models/Dice/RoundedDie.asset";
        public const string PrefabPath =
            "Assets/_Ropoly/Prefabs/Gameplay/Dice/Die.prefab";

        private const string MaterialFolder = "Assets/_Ropoly/Art/Materials/Dice";
        private const string BodyMaterialPath = MaterialFolder + "/DiceBody.mat";
        private const string PipMaterialPath = MaterialFolder + "/DicePips.mat";
        private const string ShadowMaterialPath = MaterialFolder + "/DiceShadow.mat";
        private static TMP_FontAsset _font;

        [MenuItem("Ropoly/Development/Generate P-8 3D Dice")]
        public static void GenerateDice()
        {
            if (UnityEngine.Application.isPlaying)
            {
                Debug.LogWarning("The dice cannot be generated while the game is running.");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isDirty)
            {
                Debug.LogWarning("Save the current scene before generating the dice.");
                return;
            }

            EnsureFolders();
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                P6BoardPreviewGenerator.FontAssetPath);
            if (_font == null)
            {
                throw new FileNotFoundException("The TextMesh Pro SDF font asset is missing.");
            }

            DicePresentationSettings settings = CreateOrLoadSettings();
            Material bodyMaterial = CreateOrLoadMaterial(
                BodyMaterialPath,
                settings.BodyColor,
                smoothness: 0.72f);
            Material pipMaterial = CreateOrLoadMaterial(
                PipMaterialPath,
                settings.PipColor,
                smoothness: 0.35f);
            Material shadowMaterial = CreateOrLoadMaterial(
                ShadowMaterialPath,
                new Color(0.025f, 0.016f, 0.045f, 1f),
                smoothness: 0.05f);
            Mesh roundedDie = CreateOrUpdateRoundedDieMesh();
            GameObject diePrefab = CreateOrUpdateDiePrefab(
                roundedDie,
                bodyMaterial,
                pipMaterial);
            AddDiceToGameScene(settings, diePrefab, shadowMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!UnityEngine.Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(P6BoardPreviewGenerator.GameScenePath, OpenSceneMode.Single);
            }

            Debug.Log("Generated P-8 rounded 3D dice, authoritative roll controls, and turn UI.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Ropoly/Art/Models/Dice");
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/_Ropoly/Prefabs/Gameplay");
            EnsureFolder("Assets/_Ropoly/Prefabs/Gameplay/Dice");
            EnsureFolder("Assets/_Ropoly/Settings/Presentation");
        }

        private static DicePresentationSettings CreateOrLoadSettings()
        {
            DicePresentationSettings settings =
                AssetDatabase.LoadAssetAtPath<DicePresentationSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<DicePresentationSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static Material CreateOrLoadMaterial(
            string path,
            Color color,
            float smoothness)
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
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh CreateOrUpdateRoundedDieMesh()
        {
            Mesh generated = BuildRoundedCubeMesh(halfSize: 0.5f, cornerRadius: 0.16f, segments: 7);
            generated.name = "RoundedDie";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (mesh == null)
            {
                AssetDatabase.CreateAsset(generated, MeshPath);
                mesh = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, mesh);
                Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static Mesh BuildRoundedCubeMesh(
            float halfSize,
            float cornerRadius,
            int segments)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            AddRoundedFace(vertices, normals, uvs, triangles, Vector3.right, Vector3.up, Vector3.forward, halfSize, cornerRadius, segments);
            AddRoundedFace(vertices, normals, uvs, triangles, Vector3.left, Vector3.up, Vector3.back, halfSize, cornerRadius, segments);
            AddRoundedFace(vertices, normals, uvs, triangles, Vector3.up, Vector3.forward, Vector3.right, halfSize, cornerRadius, segments);
            AddRoundedFace(vertices, normals, uvs, triangles, Vector3.down, Vector3.forward, Vector3.left, halfSize, cornerRadius, segments);
            AddRoundedFace(vertices, normals, uvs, triangles, Vector3.forward, Vector3.right, Vector3.up, halfSize, cornerRadius, segments);
            AddRoundedFace(vertices, normals, uvs, triangles, Vector3.back, Vector3.left, Vector3.up, halfSize, cornerRadius, segments);

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static void AddRoundedFace(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles,
            Vector3 faceNormal,
            Vector3 axisU,
            Vector3 axisV,
            float halfSize,
            float radius,
            int segments)
        {
            int startIndex = vertices.Count;
            float innerLimit = halfSize - radius;
            for (int vIndex = 0; vIndex <= segments; vIndex++)
            {
                float vNormalized = vIndex / (float)segments;
                float v = Mathf.Lerp(-halfSize, halfSize, vNormalized);
                for (int uIndex = 0; uIndex <= segments; uIndex++)
                {
                    float uNormalized = uIndex / (float)segments;
                    float u = Mathf.Lerp(-halfSize, halfSize, uNormalized);
                    Vector3 cubePoint =
                        (faceNormal * halfSize) +
                        (axisU * u) +
                        (axisV * v);
                    Vector3 inner = new Vector3(
                        Mathf.Clamp(cubePoint.x, -innerLimit, innerLimit),
                        Mathf.Clamp(cubePoint.y, -innerLimit, innerLimit),
                        Mathf.Clamp(cubePoint.z, -innerLimit, innerLimit));
                    Vector3 normal = (cubePoint - inner).normalized;
                    vertices.Add(inner + (normal * radius));
                    normals.Add(normal);
                    uvs.Add(new Vector2(uNormalized, vNormalized));
                }
            }

            int rowSize = segments + 1;
            for (int row = 0; row < segments; row++)
            {
                for (int column = 0; column < segments; column++)
                {
                    int a = startIndex + (row * rowSize) + column;
                    int b = a + 1;
                    int d = a + rowSize;
                    int c = d + 1;
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }
        }

        private static GameObject CreateOrUpdateDiePrefab(
            Mesh roundedDie,
            Material bodyMaterial,
            Material pipMaterial)
        {
            GameObject root = new GameObject("Die");
            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.mass = 1f;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.96f, 0.96f, 0.96f);

            GameObject body = new GameObject("Rounded Body");
            body.transform.SetParent(root.transform, false);
            MeshFilter meshFilter = body.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = roundedDie;
            MeshRenderer bodyRenderer = body.AddComponent<MeshRenderer>();
            bodyRenderer.sharedMaterial = bodyMaterial;

            List<MeshRenderer> pips = new List<MeshRenderer>(21);
            AddPipFace(root.transform, "Top 1", Vector3.up, Vector3.right, Vector3.forward, PipPattern(1), pipMaterial, pips);
            AddPipFace(root.transform, "Bottom 6", Vector3.down, Vector3.right, Vector3.back, PipPattern(6), pipMaterial, pips);
            AddPipFace(root.transform, "Front 2", Vector3.forward, Vector3.right, Vector3.up, PipPattern(2), pipMaterial, pips);
            AddPipFace(root.transform, "Back 5", Vector3.back, Vector3.left, Vector3.up, PipPattern(5), pipMaterial, pips);
            AddPipFace(root.transform, "Right 3", Vector3.right, Vector3.back, Vector3.up, PipPattern(3), pipMaterial, pips);
            AddPipFace(root.transform, "Left 4", Vector3.left, Vector3.forward, Vector3.up, PipPattern(4), pipMaterial, pips);

            DieView view = root.AddComponent<DieView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("_bodyRenderer").objectReferenceValue = bodyRenderer;
            SetObjectArray(serializedView.FindProperty("_pipRenderers"), pips);
            serializedView.FindProperty("_rigidbody").objectReferenceValue = rigidbody;
            serializedView.FindProperty("_bodyCollider").objectReferenceValue = collider;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            if (prefab == null)
            {
                throw new IOException($"Could not save die prefab at '{PrefabPath}'.");
            }

            return prefab;
        }

        private static void AddPipFace(
            Transform parent,
            string faceName,
            Vector3 normal,
            Vector3 axisU,
            Vector3 axisV,
            IReadOnlyList<Vector2> pattern,
            Material material,
            ICollection<MeshRenderer> renderers)
        {
            GameObject face = new GameObject(faceName);
            face.transform.SetParent(parent, false);
            for (int index = 0; index < pattern.Count; index++)
            {
                Vector2 coordinate = pattern[index];
                GameObject pip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pip.name = $"Pip {index + 1}";
                pip.transform.SetParent(face.transform, false);
                pip.transform.localPosition =
                    (normal * 0.496f) +
                    (axisU * coordinate.x) +
                    (axisV * coordinate.y);
                pip.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal);
                pip.transform.localScale = new Vector3(0.078f, 0.012f, 0.078f);
                Object.DestroyImmediate(pip.GetComponent<Collider>());
                MeshRenderer renderer = pip.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderers.Add(renderer);
            }
        }

        private static IReadOnlyList<Vector2> PipPattern(int value)
        {
            const float offset = 0.205f;
            Vector2 center = Vector2.zero;
            Vector2 topLeft = new Vector2(-offset, offset);
            Vector2 topRight = new Vector2(offset, offset);
            Vector2 middleLeft = new Vector2(-offset, 0f);
            Vector2 middleRight = new Vector2(offset, 0f);
            Vector2 bottomLeft = new Vector2(-offset, -offset);
            Vector2 bottomRight = new Vector2(offset, -offset);

            return value switch
            {
                1 => new[] { center },
                2 => new[] { topLeft, bottomRight },
                3 => new[] { topLeft, center, bottomRight },
                4 => new[] { topLeft, topRight, bottomLeft, bottomRight },
                5 => new[] { topLeft, topRight, center, bottomLeft, bottomRight },
                6 => new[] { topLeft, middleLeft, bottomLeft, topRight, middleRight, bottomRight },
                _ => throw new ArgumentOutOfRangeException(nameof(value)),
            };
        }

        private static void AddDiceToGameScene(
            DicePresentationSettings settings,
            GameObject diePrefab,
            Material shadowMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(
                P6BoardPreviewGenerator.GameScenePath,
                OpenSceneMode.Single);
            LocalLobbyController lobby = Object.FindFirstObjectByType<LocalLobbyController>();
            BoardPreviewController board = Object.FindFirstObjectByType<BoardPreviewController>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject matchHud = FindGameObjectIncludingInactive("LocalMatchHud");
            if (!scene.IsValid() || lobby == null || board == null || canvas == null || matchHud == null)
            {
                throw new MissingReferenceException(
                    "P-8 requires the generated P-7 Game scene and local match HUD.");
            }

            DestroyIfPresent("Dice System");
            Transform oldControls = matchHud.transform.Find("DiceControls");
            if (oldControls != null)
            {
                Object.DestroyImmediate(oldControls.gameObject);
            }

            Transform oldNextStep = matchHud.transform.Find("NextStep");
            if (oldNextStep != null)
            {
                Object.DestroyImmediate(oldNextStep.gameObject);
            }

            GameObject systemObject = new GameObject("Dice System");
            DiceController controller = systemObject.AddComponent<DiceController>();
            GameObject diceRoot = new GameObject("3D Dice");
            diceRoot.transform.SetParent(systemObject.transform, false);

            DieView firstDie = CreateDieInstance(
                diePrefab,
                diceRoot.transform,
                "First Die");
            DieView secondDie = CreateDieInstance(
                diePrefab,
                diceRoot.transform,
                "Second Die");
            CreateShadow(diceRoot.transform, "First Die Shadow", settings, first: true, shadowMaterial);
            CreateShadow(diceRoot.transform, "Second Die Shadow", settings, first: false, shadowMaterial);

            GameObject controls = new GameObject("DiceControls", typeof(RectTransform));
            controls.transform.SetParent(matchHud.transform, false);
            SetRect(
                controls.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 96f),
                new Vector2(280f, 160f));
            TMP_Text turnLabel = CreateText(
                controls.transform,
                "TurnLabel",
                "LOCAL MATCH",
                13,
                FontStyles.Bold,
                Color.white,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -16f),
                new Vector2(255f, 24f));
            TMP_Text resultLabel = CreateText(
                controls.transform,
                "ResultLabel",
                "CHOOSE CREATURES TO BEGIN",
                11,
                FontStyles.Bold,
                new Color(0.50f, 0.91f, 0.84f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -45f),
                new Vector2(255f, 24f));
            Button rollButton = CreateButton(
                controls.transform,
                "PrimaryDiceButton",
                new Vector2(0f, -40f),
                new Vector2(250f, 54f),
                new Color(0.31f, 0.19f, 0.57f, 1f));
            RectTransform rollButtonRect = rollButton.GetComponent<RectTransform>();
            rollButtonRect.anchorMin = new Vector2(0.5f, 0f);
            rollButtonRect.anchorMax = new Vector2(0.5f, 0f);
            rollButtonRect.anchoredPosition = new Vector2(0f, 30f);
            TMP_Text rollButtonLabel = CreateText(
                rollButton.transform,
                "Label",
                "WAITING FOR PLAYERS",
                15,
                FontStyles.Bold,
                Color.white,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_lobby").objectReferenceValue = lobby;
            serializedController.FindProperty("_boardPreview").objectReferenceValue = board;
            serializedController.FindProperty("_settings").objectReferenceValue = settings;
            serializedController.FindProperty("_diceRoot").objectReferenceValue = diceRoot;
            serializedController.FindProperty("_firstDie").objectReferenceValue = firstDie;
            serializedController.FindProperty("_secondDie").objectReferenceValue = secondDie;
            serializedController.FindProperty("_primaryButton").objectReferenceValue = rollButton;
            serializedController.FindProperty("_primaryButtonLabel").objectReferenceValue = rollButtonLabel;
            serializedController.FindProperty("_turnLabel").objectReferenceValue = turnLabel;
            serializedController.FindProperty("_resultLabel").objectReferenceValue = resultLabel;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            TMP_Text hudStatus = matchHud.transform.Find("Status")?.GetComponent<TMP_Text>();
            if (hudStatus != null)
            {
                hudStatus.text = "AUTHORITATIVE LOCAL DICE";
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, P6BoardPreviewGenerator.GameScenePath);
        }

        private static DieView CreateDieInstance(
            GameObject prefab,
            Transform parent,
            string name)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            return instance.GetComponent<DieView>();
        }

        private static void CreateShadow(
            Transform parent,
            string name,
            DicePresentationSettings settings,
            bool first,
            Material material)
        {
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = name;
            shadow.transform.SetParent(parent, false);
            float offset = settings.DieSeparation * 0.5f;
            Vector3 position = settings.BoardCenter +
                               (Vector3.right * (first ? -offset : offset));
            position.y = 0.34f;
            shadow.transform.position = position;
            shadow.transform.localScale = new Vector3(
                settings.DieSize * 0.62f,
                0.015f,
                settings.DieSize * 0.62f);
            Object.DestroyImmediate(shadow.GetComponent<Collider>());
            shadow.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            Image background = buttonObject.AddComponent<Image>();
            background.color = color;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 0.82f);
            button.colors = colors;
            SetRect(
                buttonObject.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                position,
                size);
            return button;
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
            Vector2 size)
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
            text.alignment = TextAlignmentOptions.Center;
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

        private static GameObject FindGameObjectIncludingInactive(string name)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == name)
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private static void DestroyIfPresent(string name)
        {
            GameObject existing = FindGameObjectIncludingInactive(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
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
    }
}
