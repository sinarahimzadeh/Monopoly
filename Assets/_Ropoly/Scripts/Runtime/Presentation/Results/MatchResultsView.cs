using System;
using System.Collections.Generic;
using Ropoly.Core.Match;
using Ropoly.Presentation.Board;
using Ropoly.Presentation.Dice;
using Ropoly.Presentation.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ropoly.Presentation.Results
{
    [DisallowMultipleComponent]
    public sealed class MatchResultsView : MonoBehaviour
    {
        private static readonly Color BackdropColor = new(0.018f, 0.012f, 0.055f, 0.96f);
        private static readonly Color PanelColor = new(0.075f, 0.045f, 0.14f, 1f);
        private static readonly Color CardColor = new(0.12f, 0.08f, 0.22f, 1f);
        private static readonly Color GoldColor = new(1f, 0.84f, 0.2f, 1f);
        private static readonly Color SilverColor = new(0.78f, 0.82f, 0.9f, 1f);
        private static readonly Color BronzeColor = new(0.85f, 0.55f, 0.35f, 1f);
        private static readonly Color MintColor = new(0.36f, 0.92f, 0.76f, 1f);
        private static readonly Color CoralColor = new(0.94f, 0.23f, 0.38f, 1f);
        private static readonly Color PurpleColor = new(0.31f, 0.18f, 0.63f, 1f);
        private static readonly Color GreenColor = new(0.22f, 0.82f, 0.44f, 1f);

        private LocalLobbyController _lobby;
        private DiceController _diceController;
        private BoardPreviewController _boardPreview;
        private Action _onRematch;
        private Action _onReturnToLobby;
        private Action _onReturnToMainMenu;
        private TMP_FontAsset _font;

        private GameObject _overlay;
        private TMP_Text _winnerText;
        private TMP_Text _matchSubText;
        private Transform _standingsContainer;

        public static MatchResultsView Install(
            LocalLobbyController lobby,
            DiceController diceController,
            BoardPreviewController boardPreview,
            Action onRematch,
            Action onReturnToLobby,
            Action onReturnToMainMenu)
        {
            if (lobby == null || diceController == null)
            {
                return null;
            }

            Canvas canvas = diceController.PrimaryButton != null
                ? diceController.PrimaryButton.GetComponentInParent<Canvas>()
                : UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            var dialogObj = new GameObject("MatchResultsView", typeof(RectTransform));
            dialogObj.transform.SetParent(canvas.transform, false);
            var rect = dialogObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                Stretch(rect);
            }

            var view = dialogObj.AddComponent<MatchResultsView>();
            view.Initialize(lobby, diceController, boardPreview, onRematch, onReturnToLobby, onReturnToMainMenu);
            return view;
        }

        private void Initialize(
            LocalLobbyController lobby,
            DiceController diceController,
            BoardPreviewController boardPreview,
            Action onRematch,
            Action onReturnToLobby,
            Action onReturnToMainMenu)
        {
            _lobby = lobby;
            _diceController = diceController;
            _boardPreview = boardPreview;
            _onRematch = onRematch;
            _onReturnToLobby = onReturnToLobby;
            _onReturnToMainMenu = onReturnToMainMenu;
            _font = diceController.PrimaryButton.GetComponentInChildren<TMP_Text>(true)?.font;

            BuildModal();
            _overlay.SetActive(false);
        }

        private void BuildModal()
        {
            _overlay = CreatePanel("ResultsOverlay", transform, BackdropColor);
            Stretch(_overlay.GetComponent<RectTransform>());
            _overlay.transform.SetAsLastSibling();

            GameObject shell = CreatePanel("ResultsShell", _overlay.transform, PanelColor);
            SetRect(shell.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 640f));

            // Header Banner
            CreateText("Title", shell.transform, "MATCH COMPLETED", 32f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 275f), new Vector2(800f, 44f), GoldColor);

            // Winner Showcase Banner
            GameObject winnerBox = CreatePanel("WinnerBox", shell.transform, CardColor);
            SetRect(winnerBox.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, 195f), new Vector2(860f, 85f));

            _winnerText = CreateText("WinnerText", winnerBox.transform, "WINNER: PLAYER 1", 26f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 14f), new Vector2(840f, 36f), MintColor);

            _matchSubText = CreateText("SubText", winnerBox.transform, "Champion of the Board", 16f, FontStyles.Normal,
                TextAlignmentOptions.Center, new Vector2(0f, -18f), new Vector2(840f, 26f), new Color(0.8f, 0.8f, 0.9f));

            // Standings Column Headers
            GameObject headerRow = CreatePanel("HeaderRow", shell.transform, new Color(0.16f, 0.11f, 0.28f, 1f));
            SetRect(headerRow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, 115f), new Vector2(860f, 32f));

            CreateText("H_Rank", headerRow.transform, "RANK", 13f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(-380f, 0f), new Vector2(70f, 24f), MintColor);
            CreateText("H_Player", headerRow.transform, "PLAYER", 13f, FontStyles.Bold,
                TextAlignmentOptions.Left, new Vector2(-220f, 0f), new Vector2(180f, 24f), MintColor);
            CreateText("H_Props", headerRow.transform, "PROPERTIES", 13f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(-40f, 0f), new Vector2(120f, 24f), MintColor);
            CreateText("H_Cash", headerRow.transform, "CASH", 13f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(100f, 0f), new Vector2(100f, 24f), MintColor);
            CreateText("H_NetWorth", headerRow.transform, "NET WORTH", 13f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(230f, 0f), new Vector2(120f, 24f), MintColor);
            CreateText("H_Status", headerRow.transform, "STATUS", 13f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(365f, 0f), new Vector2(110f, 24f), MintColor);

            // Standings Container
            GameObject standingsObj = CreatePanel("StandingsContainer", shell.transform, Color.clear);
            SetRect(standingsObj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(860f, 250f));
            _standingsContainer = standingsObj.transform;

            // Bottom action buttons
            Button rematchBtn = CreateButton("RematchBtn", shell.transform, "PLAY AGAIN",
                new Vector2(-260f, -260f), new Vector2(220f, 50f), GreenColor, 18f, out _);
            rematchBtn.onClick.AddListener(() =>
            {
                Hide();
                _onRematch?.Invoke();
            });

            Button lobbyBtn = CreateButton("LobbyBtn", shell.transform, "CHANGE PLAYERS",
                new Vector2(0f, -260f), new Vector2(220f, 50f), PurpleColor, 16f, out _);
            lobbyBtn.onClick.AddListener(() =>
            {
                Hide();
                _onReturnToLobby?.Invoke();
            });

            Button menuBtn = CreateButton("MenuBtn", shell.transform, "MAIN MENU",
                new Vector2(260f, -260f), new Vector2(220f, 50f), CardColor, 16f, out _);
            menuBtn.onClick.AddListener(() =>
            {
                Hide();
                _onReturnToMainMenu?.Invoke();
            });
        }

        public void Show(MatchResult result)
        {
            if (result == null)
            {
                return;
            }

            _overlay.SetActive(true);

            _winnerText.text = $"🏆 {result.WinnerDisplayName.ToUpperInvariant()} WINS THE MATCH!";
            _matchSubText.text = $"Total Rounds: {result.TotalTurns}  •  Victory by Elimination";

            foreach (Transform child in _standingsContainer)
            {
                Destroy(child.gameObject);
            }

            float startY = 90f;
            float stepY = 54f;

            for (int i = 0; i < result.Standings.Count; i++)
            {
                PlayerStanding standing = result.Standings[i];
                float y = startY - (i * stepY);
                BuildStandingRow(_standingsContainer, standing, y);
            }
        }

        private void BuildStandingRow(Transform container, PlayerStanding standing, float yPos)
        {
            Color rowColor = standing.IsWinner ? new Color(0.18f, 0.14f, 0.28f, 1f) : CardColor;
            GameObject row = CreatePanel($"Row_{standing.Rank}", container, rowColor);
            SetRect(row.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, yPos), new Vector2(860f, 48f));

            // Rank
            string rankStr = standing.Rank switch
            {
                1 => "1st 👑",
                2 => "2nd",
                3 => "3rd",
                _ => $"{standing.Rank}th"
            };
            Color rankColor = standing.Rank switch
            {
                1 => GoldColor,
                2 => SilverColor,
                3 => BronzeColor,
                _ => Color.white
            };
            CreateText("Rank", row.transform, rankStr, 16f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(-380f, 0f), new Vector2(70f, 30f), rankColor);

            // Player Name + Creature
            string creaturePart = !string.IsNullOrEmpty(standing.CreatureId) ? $" ({standing.CreatureId.ToUpperInvariant()})" : "";
            CreateText("Player", row.transform, $"{standing.DisplayName}{creaturePart}", 16f, FontStyles.Bold,
                TextAlignmentOptions.Left, new Vector2(-220f, 0f), new Vector2(180f, 30f), Color.white);

            // Properties
            CreateText("Props", row.transform, $"{standing.PropertiesCount}", 16f, FontStyles.Normal,
                TextAlignmentOptions.Center, new Vector2(-40f, 0f), new Vector2(120f, 30f), new Color(0.85f, 0.85f, 0.95f));

            // Cash
            CreateText("Cash", row.transform, $"${standing.Cash:N0}", 16f, FontStyles.Normal,
                TextAlignmentOptions.Center, new Vector2(100f, 0f), new Vector2(100f, 30f), MintColor);

            // Net Worth
            CreateText("NetWorth", row.transform, $"${standing.NetWorth:N0}", 17f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(230f, 0f), new Vector2(120f, 30f), GoldColor);

            // Status Badge
            string statusStr;
            Color statusColor;
            if (standing.IsWinner)
            {
                statusStr = "WINNER";
                statusColor = GreenColor;
            }
            else
            {
                statusStr = standing.EliminationReason switch
                {
                    EliminationReason.Bankruptcy => "BANKRUPT",
                    EliminationReason.DebtTimeout => "DEBT TIMEOUT",
                    EliminationReason.DisconnectTimeout => "DISCONNECTED",
                    EliminationReason.InactivityTimeout => "TIMED OUT",
                    _ => "ELIMINATED"
                };
                if (standing.EliminationTurn > 0)
                {
                    statusStr += $" (T{standing.EliminationTurn})";
                }
                statusColor = CoralColor;
            }

            CreateText("Status", row.transform, statusStr, 13f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(365f, 0f), new Vector2(110f, 30f), statusColor);
        }

        public void Hide()
        {
            if (_overlay != null)
            {
                _overlay.SetActive(false);
            }
        }

        private GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return go;
        }

        private TMP_Text CreateText(
            string name,
            Transform parent,
            string content,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 position,
            Vector2 size,
            Color buttonColor,
            float fontSize,
            out TMP_Text labelComponent)
        {
            GameObject go = CreatePanel(name, parent, buttonColor);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = Color.Lerp(buttonColor, Color.white, 0.25f);
            colors.pressedColor = Color.Lerp(buttonColor, Color.black, 0.25f);
            colors.disabledColor = new Color(buttonColor.r * 0.5f, buttonColor.g * 0.5f, buttonColor.b * 0.5f, 0.5f);
            button.colors = colors;

            labelComponent = CreateText("Label", go.transform, label, fontSize, FontStyles.Bold,
                TextAlignmentOptions.Center, Vector2.zero, size, Color.white);
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 pivotAndAnchors, Vector2 position, Vector2 size)
        {
            rect.pivot = pivotAndAnchors;
            rect.anchorMin = pivotAndAnchors;
            rect.anchorMax = pivotAndAnchors;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
