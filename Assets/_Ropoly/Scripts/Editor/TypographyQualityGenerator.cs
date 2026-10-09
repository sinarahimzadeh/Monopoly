using System;
using System.IO;
using Ropoly.Presentation.Board;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ropoly.Editor
{
    public static class TypographyQualityGenerator
    {
        public const string SourceFontPath =
            "Assets/_Ropoly/Art/Fonts/LiberationSans.ttf";
        public const string FontAssetPath =
            "Assets/_Ropoly/Art/Fonts/Ropoly UI SDF.asset";

        private const string SceneFolder = "Assets/_Ropoly/Scenes";
        private const string PrefabFolder = "Assets/_Ropoly/Prefabs";
        private const string ThemePath =
            "Assets/_Ropoly/Settings/Presentation/BoardPreviewTheme.asset";

        [MenuItem("Ropoly/Development/Repair Typography Quality")]
        public static void RepairTypographyQuality()
        {
            if (UnityEngine.Application.isPlaying)
            {
                Debug.LogWarning("Typography cannot be repaired while the game is running.");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isDirty)
            {
                Debug.LogWarning("Save the current scene before repairing typography.");
                return;
            }

            TMP_FontAsset font = CreateOrUpdateFontAsset();
            UpdateBoardTheme(font);
            UpdatePrefabs(font);
            UpdateScenes(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!UnityEngine.Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(
                    P6BoardPreviewGenerator.GameScenePath,
                    OpenSceneMode.Single);
            }

            Debug.Log("Repaired Ropoly typography with a high-resolution SDF font and pixel-perfect canvases.");
        }

        private static TMP_FontAsset CreateOrUpdateFontAsset()
        {
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                throw new FileNotFoundException($"The source font is missing at '{SourceFontPath}'.");
            }

            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            bool created = fontAsset == null;
            if (fontAsset == null)
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(
                    sourceFont,
                    180,
                    12,
                    GlyphRenderMode.SDFAA,
                    2048,
                    2048,
                    AtlasPopulationMode.Dynamic,
                    true);
                if (fontAsset == null)
                {
                    throw new InvalidOperationException("TextMesh Pro could not create the Ropoly font asset.");
                }

                fontAsset.name = "Ropoly UI SDF";
                fontAsset.atlasTexture.name = "Ropoly UI SDF Atlas";
                fontAsset.material.name = "Ropoly UI SDF Material";
                AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            fontAsset.isMultiAtlasTexturesEnabled = true;
            fontAsset.normalStyle = 0f;
            fontAsset.boldStyle = 0.48f;
            SerializedObject serializedFont = new SerializedObject(fontAsset);
            serializedFont.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
            serializedFont.ApplyModifiedPropertiesWithoutUndo();
            string missingCharacters = string.Empty;
            if (created)
            {
                fontAsset.TryAddCharacters(BuildCharacterSet(), out missingCharacters, true);
            }

            Material material = fontAsset.material;
            if (material != null && material.HasProperty("_Sharpness"))
            {
                material.SetFloat("_Sharpness", 0.32f);
                EditorUtility.SetDirty(material);
            }

            if (!string.IsNullOrEmpty(missingCharacters))
            {
                Debug.LogWarning($"The Ropoly font is missing these optional characters: {missingCharacters}");
            }

            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        private static string BuildCharacterSet()
        {
            const string extras = "…•–—×©®™€£¥₩°";
            char[] characters = new char[(126 - 32 + 1) + extras.Length];
            int index = 0;
            for (char character = (char)32; character <= (char)126; character++)
            {
                characters[index++] = character;
            }

            extras.CopyTo(0, characters, index, extras.Length);
            return new string(characters);
        }

        private static void UpdateBoardTheme(TMP_FontAsset font)
        {
            BoardPreviewTheme theme = AssetDatabase.LoadAssetAtPath<BoardPreviewTheme>(ThemePath);
            if (theme == null)
            {
                throw new FileNotFoundException($"The board preview theme is missing at '{ThemePath}'.");
            }

            SerializedObject serializedTheme = new SerializedObject(theme);
            SerializedProperty fontProperty = serializedTheme.FindProperty("_font");
            fontProperty.objectReferenceValue = font;
            serializedTheme.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
        }

        private static void UpdatePrefabs(TMP_FontAsset font)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool changed = ApplyTypography(root, font);
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }

                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpdateScenes(TMP_FontAsset font)
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { SceneFolder });
            Array.Sort(sceneGuids, StringComparer.Ordinal);
            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    changed |= ApplyTypography(root, font);
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
        }

        private static bool ApplyTypography(GameObject root, TMP_FontAsset font)
        {
            bool changed = false;
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font != font)
                {
                    text.font = font;
                    changed = true;
                }

                if (text.fontSharedMaterial != font.material)
                {
                    text.fontSharedMaterial = font.material;
                    changed = true;
                }

                text.extraPadding = false;
                EditorUtility.SetDirty(text);
            }

            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (!canvas.pixelPerfect)
                {
                    canvas.pixelPerfect = true;
                    changed = true;
                    EditorUtility.SetDirty(canvas);
                }
            }

            foreach (CanvasScaler scaler in root.GetComponentsInChildren<CanvasScaler>(true))
            {
                if (!Mathf.Approximately(scaler.dynamicPixelsPerUnit, 4f))
                {
                    scaler.dynamicPixelsPerUnit = 4f;
                    changed = true;
                    EditorUtility.SetDirty(scaler);
                }
            }

            return changed;
        }
    }
}
