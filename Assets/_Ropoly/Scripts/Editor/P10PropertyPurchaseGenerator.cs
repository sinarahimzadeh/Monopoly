using System.IO;
using Ropoly.Presentation.Dice;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ropoly.Editor
{
    public static class P10PropertyPurchaseGenerator
    {
        private const string SecondaryButtonName = "PropertyDecisionSecondaryButton";

        [MenuItem("Ropoly/Development/Generate P-10 Property Purchases")]
        public static void Generate()
        {
            if (UnityEngine.Application.isPlaying)
            {
                Debug.LogWarning("P-10 cannot be generated while the game is running.");
                return;
            }

            if (!File.Exists(P6BoardPreviewGenerator.GameScenePath))
            {
                throw new FileNotFoundException("The generated Game scene is missing.");
            }

            Scene previousScene = SceneManager.GetActiveScene();
            string previousScenePath = previousScene.path;
            if (!UnityEngine.Application.isBatchMode && previousScene.isDirty)
            {
                Debug.LogWarning("Save the current scene before generating P-10.");
                return;
            }

            Scene gameScene = EditorSceneManager.OpenScene(
                P6BoardPreviewGenerator.GameScenePath,
                OpenSceneMode.Single);
            DiceController controller = Object.FindFirstObjectByType<DiceController>();
            Button primaryButton = controller == null ? null : controller.PrimaryButton;
            if (controller == null || primaryButton == null)
            {
                throw new MissingReferenceException("P-10 requires the generated P-8 dice controls.");
            }

            Transform controls = primaryButton.transform.parent;
            Transform existing = controls.Find(SecondaryButtonName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject secondaryObject = Object.Instantiate(primaryButton.gameObject, controls);
            secondaryObject.name = SecondaryButtonName;
            secondaryObject.SetActive(true);
            Button secondaryButton = secondaryObject.GetComponent<Button>();
            TMP_Text secondaryLabel = secondaryObject.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (secondaryButton == null || secondaryLabel == null)
            {
                Object.DestroyImmediate(secondaryObject);
                throw new MissingReferenceException("The P-8 primary button is missing its expected components.");
            }

            RectTransform secondaryRect = secondaryObject.GetComponent<RectTransform>();
            secondaryRect.sizeDelta = new Vector2(120f, 54f);
            secondaryRect.anchoredPosition = new Vector2(65f, 30f);
            secondaryLabel.text = "SKIP";
            Image secondaryBackground = secondaryObject.GetComponent<Image>();
            if (secondaryBackground != null)
            {
                Color color = new Color(0.17f, 0.13f, 0.28f, 1f);
                secondaryBackground.color = color;
                ColorBlock colors = secondaryButton.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.Lerp(Color.white, color, 0.65f);
                colors.pressedColor = Color.Lerp(Color.white, color, 0.85f);
                colors.selectedColor = colors.highlightedColor;
                secondaryButton.colors = colors;
            }

            secondaryObject.SetActive(false);
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_secondaryButton").objectReferenceValue = secondaryButton;
            serializedController.FindProperty("_secondaryButtonLabel").objectReferenceValue = secondaryLabel;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene, P6BoardPreviewGenerator.GameScenePath);

            if (!UnityEngine.Application.isBatchMode &&
                !string.IsNullOrEmpty(previousScenePath) &&
                previousScenePath != P6BoardPreviewGenerator.GameScenePath)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }

            Debug.Log("Generated P-10 property purchase decisions and ownership presentation.");
        }
    }
}
