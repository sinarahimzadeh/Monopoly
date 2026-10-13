using System;
using System.Collections.Generic;
using System.IO;
using Ropoly.Presentation.Dice;
using Ropoly.Presentation.Players;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Ropoly.Editor
{
    /// <summary>
    /// Gives the procedural placeholder pieces a cohesive Ropoly visual language
    /// without coupling gameplay code to final art assets.
    /// </summary>
    public static class PlaceholderPieceStyleGenerator
    {
        private const string ToonShaderName = "Ropoly/Soft Toon Surface";
        private const string ShadowShaderName = "Ropoly/Soft Contact Shadow";

        private const string DiceMaterialFolder =
            "Assets/_Ropoly/Art/Materials/Dice";
        private const string DiceBodyMaterialPath = DiceMaterialFolder + "/DiceBody.mat";
        private const string DicePipMaterialPath = DiceMaterialFolder + "/DicePips.mat";
        private const string DiceShadowMaterialPath = DiceMaterialFolder + "/DiceShadow.mat";

        [MenuItem("Ropoly/Development/Refresh Placeholder Piece Style")]
        public static void Generate()
        {
            if (UnityEngine.Application.isPlaying)
            {
                Debug.LogWarning("Placeholder piece styling cannot run during Play Mode.");
                return;
            }

            ApplyToExistingAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Refreshed Ropoly soft-toon dice and 2D portrait token styling.");
        }

        internal static void ApplyToExistingAssets()
        {
            Shader toonShader = Shader.Find(ToonShaderName);
            Shader shadowShader = Shader.Find(ShadowShaderName);
            if (toonShader == null || shadowShader == null)
            {
                throw new InvalidOperationException(
                    "The Ropoly placeholder shaders have not finished importing.");
            }

            EnsureFolder(DiceMaterialFolder);
            Material diceBody = ConfigureToonMaterial(
                DiceBodyMaterialPath,
                toonShader,
                new Color(0.98f, 0.965f, 1f, 1f),
                new Color(0.74f, 0.68f, 0.9f, 1f),
                new Color(0.43f, 1f, 0.88f, 1f),
                rimStrength: 0.62f,
                rimPower: 2.6f,
                smoothness: 0.62f,
                emissionColor: new Color(0.32f, 0.16f, 0.52f, 1f),
                emissionStrength: 0.08f);
            Material dicePips = ConfigureToonMaterial(
                DicePipMaterialPath,
                toonShader,
                new Color(0.055f, 0.025f, 0.11f, 1f),
                new Color(0.012f, 0.006f, 0.03f, 1f),
                new Color(0.52f, 1f, 0.91f, 1f),
                rimStrength: 0.84f,
                rimPower: 2f,
                smoothness: 0.42f,
                emissionColor: new Color(0.52f, 1f, 0.91f, 1f),
                emissionStrength: 0.9f);
            ConfigureShadowMaterial(
                DiceShadowMaterialPath,
                shadowShader,
                new Color(0.018f, 0.008f, 0.05f, 0.38f),
                edgeSoftness: 0.58f);

            ConfigureDiceSettings();
            ConfigureCreaturePrefab();
            ConfigureDiePrefab(diceBody, dicePips);
        }

        private static Material ConfigureToonMaterial(
            string path,
            Shader shader,
            Color baseColor,
            Color shadeColor,
            Color rimColor,
            float rimStrength,
            float rimPower,
            float smoothness,
            Color emissionColor,
            float emissionStrength)
        {
            Material material = LoadOrCreateMaterial(path, shader);
            material.shader = shader;
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_ShadeColor", shadeColor);
            material.SetColor("_RimColor", rimColor);
            material.SetFloat("_RimStrength", rimStrength);
            material.SetFloat("_RimPower", rimPower);
            material.SetFloat("_ShadowThreshold", 0.08f);
            material.SetFloat("_ShadowSoftness", 0.14f);
            material.SetFloat("_AmbientStrength", 0.78f);
            material.SetFloat("_Smoothness", smoothness);
            material.SetColor("_EmissionColor", emissionColor);
            material.SetFloat("_EmissionStrength", emissionStrength);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material ConfigureShadowMaterial(
            string path,
            Shader shader,
            Color color,
            float edgeSoftness)
        {
            Material material = LoadOrCreateMaterial(path, shader);
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_EdgeSoftness", edgeSoftness);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(path),
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ConfigureDiceSettings()
        {
            DicePresentationSettings settings =
                AssetDatabase.LoadAssetAtPath<DicePresentationSettings>(P8DiceGenerator.SettingsPath);
            if (settings == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(settings);
            serialized.FindProperty("_dieSize").floatValue = 1.28f;
            serialized.FindProperty("_bodyColor").colorValue =
                new Color(0.98f, 0.965f, 1f, 1f);
            serialized.FindProperty("_pipColor").colorValue =
                new Color(0.055f, 0.025f, 0.11f, 1f);
            serialized.FindProperty("_bodyShadeColor").colorValue =
                new Color(0.74f, 0.68f, 0.9f, 1f);
            serialized.FindProperty("_rimColor").colorValue =
                new Color(0.43f, 1f, 0.88f, 1f);
            serialized.FindProperty("_pipGlowColor").colorValue =
                new Color(0.52f, 1f, 0.91f, 1f);
            serialized.FindProperty("_rimStrength").floatValue = 0.62f;
            serialized.FindProperty("_pipGlowStrength").floatValue = 0.9f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void ConfigureCreaturePrefab()
        {
            if (!File.Exists(P7LocalLobbyGenerator.TokenPrefabPath))
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(P7LocalLobbyGenerator.TokenPrefabPath);
            try
            {
                CreatureTokenView view = root.GetComponent<CreatureTokenView>();
                if (view == null)
                {
                    throw new MissingComponentException("Creature token prefab has no CreatureTokenView.");
                }

                for (int childIndex = root.transform.childCount - 1; childIndex >= 0; childIndex--)
                {
                    Object.DestroyImmediate(root.transform.GetChild(childIndex).gameObject);
                }

                Sprite roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                if (roundSprite == null)
                {
                    throw new FileNotFoundException("Unity's built-in round UI sprite is missing.");
                }

                GameObject visualsObject = new GameObject("2D Portrait Visuals");
                visualsObject.transform.SetParent(root.transform, false);
                visualsObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                Transform visuals = visualsObject.transform;

                SpriteRenderer body = CreateSpriteRenderer(
                    visuals,
                    "Round Body",
                    roundSprite,
                    Color.white,
                    Vector3.zero,
                    new Vector2(0.78f, 0.78f),
                    0);
                SpriteRenderer leftEye = CreateSpriteRenderer(
                    visuals,
                    "Left Eye",
                    roundSprite,
                    Color.white,
                    new Vector3(-0.145f, 0.075f, 0f),
                    new Vector2(0.22f, 0.27f),
                    1);
                SpriteRenderer rightEye = CreateSpriteRenderer(
                    visuals,
                    "Right Eye",
                    roundSprite,
                    Color.white,
                    new Vector3(0.145f, 0.075f, 0f),
                    new Vector2(0.22f, 0.27f),
                    1);
                SpriteRenderer leftPupil = CreateSpriteRenderer(
                    visuals,
                    "Left Pupil",
                    roundSprite,
                    new Color(0.025f, 0.02f, 0.04f, 1f),
                    new Vector3(-0.145f, 0.06f, 0f),
                    new Vector2(0.09f, 0.11f),
                    2);
                SpriteRenderer rightPupil = CreateSpriteRenderer(
                    visuals,
                    "Right Pupil",
                    roundSprite,
                    new Color(0.025f, 0.02f, 0.04f, 1f),
                    new Vector3(0.145f, 0.06f, 0f),
                    new Vector2(0.09f, 0.11f),
                    2);
                SpriteRenderer leftHighlight = CreateSpriteRenderer(
                    visuals,
                    "Left Eye Glint",
                    roundSprite,
                    Color.white,
                    new Vector3(-0.165f, 0.095f, 0f),
                    new Vector2(0.028f, 0.032f),
                    3);
                SpriteRenderer rightHighlight = CreateSpriteRenderer(
                    visuals,
                    "Right Eye Glint",
                    roundSprite,
                    Color.white,
                    new Vector3(0.125f, 0.095f, 0f),
                    new Vector2(0.028f, 0.032f),
                    3);

                SphereCollider collider = root.GetComponent<SphereCollider>();
                collider.center = new Vector3(0f, 0.46f, 0f);
                collider.radius = 0.4f;

                SerializedObject serialized = new SerializedObject(view);
                serialized.FindProperty("_visualRoot").objectReferenceValue = visuals;
                serialized.FindProperty("_bodySprite").objectReferenceValue = body;
                SetObjectArray(
                    serialized.FindProperty("_eyeSprites"),
                    new[] { leftEye, rightEye });
                SetObjectArray(
                    serialized.FindProperty("_pupilSprites"),
                    new[] { leftPupil, rightPupil });
                SetObjectArray(
                    serialized.FindProperty("_pupilHighlights"),
                    new[] { leftHighlight, rightHighlight });
                serialized.FindProperty("_bodyCollider").objectReferenceValue = collider;
                serialized.FindProperty("_breathAmount").floatValue = 0.065f;
                serialized.FindProperty("_breathSpeed").floatValue = 2f;
                serialized.FindProperty("_breathLift").floatValue = 0.03f;
                serialized.FindProperty("_lookDistance").floatValue = 0.04f;
                serialized.FindProperty("_lookResponsiveness").floatValue = 16f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, P7LocalLobbyGenerator.TokenPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static SpriteRenderer CreateSpriteRenderer(
            Transform parent,
            string name,
            Sprite sprite,
            Color color,
            Vector3 localPosition,
            Vector2 size,
            int sortingOrder)
        {
            GameObject spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = localPosition;
            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            Vector2 sourceSize = sprite.bounds.size;
            spriteObject.transform.localScale = new Vector3(
                size.x / sourceSize.x,
                size.y / sourceSize.y,
                1f);
            return renderer;
        }

        private static void ConfigureDiePrefab(Material bodyMaterial, Material pipMaterial)
        {
            if (!File.Exists(P8DiceGenerator.PrefabPath))
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(P8DiceGenerator.PrefabPath);
            try
            {
                Transform body = FindChild(root.transform, "Rounded Body");
                if (body == null || !body.TryGetComponent(out MeshRenderer bodyRenderer))
                {
                    throw new MissingComponentException("Die prefab has no rounded body renderer.");
                }

                SetRendererMaterial(bodyRenderer, bodyMaterial);
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!child.name.StartsWith("Pip ", StringComparison.Ordinal) ||
                        !child.TryGetComponent(out MeshRenderer pipRenderer))
                    {
                        continue;
                    }

                    SetRendererMaterial(pipRenderer, pipMaterial);
                    // Keep the pips just proud of the rounded mesh. Pushing them inside
                    // looks attractive in a prefab close-up but makes them disappear at
                    // the actual top-down gameplay camera distance.
                    child.localPosition = MoveDominantAxis(child.localPosition, 0.515f);
                    child.localScale = new Vector3(0.115f, 0.015f, 0.115f);
                }

                PrefabUtility.SaveAsPrefabAsset(root, P8DiceGenerator.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Vector3 MoveDominantAxis(Vector3 position, float distance)
        {
            Vector3 absolute = new Vector3(
                Mathf.Abs(position.x),
                Mathf.Abs(position.y),
                Mathf.Abs(position.z));
            if (absolute.x >= absolute.y && absolute.x >= absolute.z)
            {
                position.x = Mathf.Sign(position.x) * distance;
            }
            else if (absolute.y >= absolute.z)
            {
                position.y = Mathf.Sign(position.y) * distance;
            }
            else
            {
                position.z = Mathf.Sign(position.z) * distance;
            }

            return position;
        }

        private static Transform FindChild(Transform root, string childName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static void SetRendererMaterial(Renderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
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
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }
}
