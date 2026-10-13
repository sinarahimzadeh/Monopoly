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
    public static class P11DebtResolutionGenerator
    {
        private const string PanelName = "DebtResolutionPanel";

        [MenuItem("Ropoly/Development/Generate P-11 Debt Resolution")]
        public static void Generate()
        {
            if (UnityEngine.Application.isPlaying)
            {
                Debug.LogWarning("P-11 cannot be generated while the game is running.");
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
                Debug.LogWarning("Save the current scene before generating P-11.");
                return;
            }

            Scene gameScene = EditorSceneManager.OpenScene(
                P6BoardPreviewGenerator.GameScenePath,
                OpenSceneMode.Single);
            DiceController controller = Object.FindFirstObjectByType<DiceController>();
            if (controller == null || controller.PrimaryButton == null)
            {
                throw new MissingReferenceException("P-11 requires the generated dice controls.");
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                throw new MissingReferenceException("P-11 requires the main game Canvas.");
            }

            Transform existing = canvas.transform.Find(PanelName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            TMP_Text sourceLabel = controller.PrimaryButton.GetComponentInChildren<TMP_Text>(true);
            if (sourceLabel == null)
            {
                throw new MissingReferenceException("The dice button has no TextMesh Pro label.");
            }

            GameObject panelObject = CreatePanel(canvas.transform);
            TMP_Text heading = CreateText(
                panelObject.transform,
                "Heading",
                sourceLabel,
                "RESOLVE DEBT",
                30f,
                new Vector2(0f, 138f),
                new Vector2(540f, 42f),
                new Color(0.96f, 0.32f, 0.65f, 1f));
            heading.fontStyle = FontStyles.Bold;

            TMP_Text summary = CreateText(
                panelObject.transform,
                "DebtSummary",
                sourceLabel,
                "PLAYER OWES RENT",
                19f,
                new Vector2(0f, 88f),
                new Vector2(540f, 54f),
                new Color(0.85f, 0.82f, 0.94f, 1f));

            Image assetCard = CreateImage(
                panelObject.transform,
                "AssetCard",
                new Vector2(0f, 24f),
                new Vector2(440f, 70f),
                new Color(0.11f, 0.08f, 0.18f, 0.98f));
            TMP_Text property = CreateText(
                assetCard.transform,
                "PropertyLabel",
                sourceLabel,
                "NO ASSETS AVAILABLE",
                18f,
                Vector2.zero,
                new Vector2(410f, 58f),
                Color.white);

            Button previous = CreateButton(
                controller.PrimaryButton,
                panelObject.transform,
                "PreviousAssetButton",
                "‹",
                new Vector2(-260f, 24f),
                new Vector2(54f, 54f));
            Button next = CreateButton(
                controller.PrimaryButton,
                panelObject.transform,
                "NextAssetButton",
                "›",
                new Vector2(260f, 24f),
                new Vector2(54f, 54f));
            Button mortgage = CreateButton(
                controller.PrimaryButton,
                panelObject.transform,
                "MortgageButton",
                "MORTGAGE",
                new Vector2(-125f, -58f),
                new Vector2(230f, 52f));
            Button sell = CreateButton(
                controller.PrimaryButton,
                panelObject.transform,
                "SellButton",
                "SELL TO BANK",
                new Vector2(125f, -58f),
                new Vector2(230f, 52f));
            Button bankruptcy = CreateButton(
                controller.PrimaryButton,
                panelObject.transform,
                "BankruptcyButton",
                "DECLARE BANKRUPTCY",
                new Vector2(0f, -126f),
                new Vector2(300f, 46f));
            Image bankruptcyImage = bankruptcy.GetComponent<Image>();
            if (bankruptcyImage != null)
            {
                bankruptcyImage.color = new Color(0.62f, 0.12f, 0.25f, 1f);
            }

            CreateText(
                panelObject.transform,
                "BankNotice",
                sourceLabel,
                "BANKRUPTCY RETURNS EVERY ASSET TO THE BANK",
                12f,
                new Vector2(0f, -166f),
                new Vector2(520f, 24f),
                new Color(0.62f, 0.59f, 0.72f, 1f));

            DebtResolutionView view = panelObject.AddComponent<DebtResolutionView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("_summaryLabel").objectReferenceValue = summary;
            serializedView.FindProperty("_propertyLabel").objectReferenceValue = property;
            serializedView.FindProperty("_previousButton").objectReferenceValue = previous;
            serializedView.FindProperty("_nextButton").objectReferenceValue = next;
            serializedView.FindProperty("_mortgageButton").objectReferenceValue = mortgage;
            serializedView.FindProperty("_mortgageButtonLabel").objectReferenceValue =
                mortgage.GetComponentInChildren<TMP_Text>(true);
            serializedView.FindProperty("_sellButton").objectReferenceValue = sell;
            serializedView.FindProperty("_sellButtonLabel").objectReferenceValue =
                sell.GetComponentInChildren<TMP_Text>(true);
            serializedView.FindProperty("_bankruptcyButton").objectReferenceValue = bankruptcy;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_debtResolutionView").objectReferenceValue = view;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            panelObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene, P6BoardPreviewGenerator.GameScenePath);

            if (!UnityEngine.Application.isBatchMode &&
                !string.IsNullOrEmpty(previousScenePath) &&
                previousScenePath != P6BoardPreviewGenerator.GameScenePath)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }

            Debug.Log("Generated P-11 debt resolution, liquidation, and bankruptcy interface.");
        }

        private static GameObject CreatePanel(Transform parent)
        {
            GameObject panel = new GameObject(
                PanelName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.transform.SetAsLastSibling();
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620f, 390f);
            rect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.055f, 0.035f, 0.095f, 0.985f);
            return panel;
        }

        private static Button CreateButton(
            Button source,
            Transform parent,
            string name,
            string label,
            Vector2 position,
            Vector2 size)
        {
            GameObject buttonObject = Object.Instantiate(source.gameObject, parent);
            buttonObject.name = name;
            buttonObject.SetActive(true);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TMP_Text text = buttonObject.GetComponentInChildren<TMP_Text>(true);
            text.text = label;
            text.fontSize = name.Contains("Asset") ? 28f : 15f;
            return buttonObject.GetComponent<Button>();
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            TMP_Text source,
            string value,
            float fontSize,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = source.font;
            text.fontSharedMaterial = source.fontSharedMaterial;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static Image CreateImage(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            GameObject imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }
    }
}
