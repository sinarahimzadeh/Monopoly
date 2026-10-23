using System;
using System.Collections.Generic;
using Ropoly.Core.Rules;
using Ropoly.Infrastructure.Content;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ropoly.Presentation.Settings
{
    [DisallowMultipleComponent]
    public sealed class GameSettingsDialogView : MonoBehaviour
    {
        private static readonly Color BackdropColor = new(0.015f, 0.01f, 0.045f, 0.94f);
        private static readonly Color PanelColor = new(0.08f, 0.05f, 0.15f, 1f);
        private static readonly Color CardColor = new(0.13f, 0.09f, 0.24f, 1f);
        private static readonly Color PillActiveColor = new(0.31f, 0.18f, 0.63f, 1f);
        private static readonly Color PillInactiveColor = new(0.14f, 0.10f, 0.24f, 1f);
        private static readonly Color MintColor = new(0.36f, 0.92f, 0.76f, 1f);
        private static readonly Color EmeraldColor = new(0.14f, 0.58f, 0.48f, 1f);
        private static readonly Color TextMutedColor = new(0.70f, 0.68f, 0.80f, 1f);
        private static readonly Color CoralColor = new(0.94f, 0.23f, 0.38f, 1f);

        private TMP_FontAsset _font;
        private GameObject _overlay;
        private GameObject _shell;
        private TMP_Text _titleText;
        private TMP_Text _subtitleText;

        // Draft state
        private bool _isReadOnly;
        private bool _auctionsEnabled;
        private int _startingCash;
        private int _turnDurationSeconds;
        private int _passStartCash;
        private int _fullSetRentMultiplier;
        private bool _mortgageEnabled;
        private bool _vacationRewardEnabled;

        private Action<GameRulesetSnapshot> _onSaveCallback;
        private GameRulesetSnapshot _baseSnapshot;

        // UI references
        private readonly List<Button> _auctionButtons = new();
        private readonly List<TMP_Text> _auctionLabels = new();

        private readonly List<Button> _startingCashButtons = new();
        private readonly List<TMP_Text> _startingCashLabels = new();
        private static readonly int[] StartingCashValues = { 1000, 1500, 2000, 2500 };

        private readonly List<Button> _turnTimerButtons = new();
        private readonly List<TMP_Text> _turnTimerLabels = new();
        private static readonly int[] TurnTimerValues = { 30, 60, 90, 0 };

        private readonly List<Button> _passStartButtons = new();
        private readonly List<TMP_Text> _passStartLabels = new();
        private static readonly int[] PassStartValues = { 100, 200, 300, 400 };

        private readonly List<Button> _multiplierButtons = new();
        private readonly List<TMP_Text> _multiplierLabels = new();
        private static readonly int[] MultiplierValues = { 2, 3 };

        private readonly List<Button> _mortgageButtons = new();
        private readonly List<TMP_Text> _mortgageLabels = new();

        private readonly List<Button> _vacationButtons = new();
        private readonly List<TMP_Text> _vacationLabels = new();

        private Button _resetButton;
        private Button _cancelButton;
        private Button _applyButton;
        private Button _closeReadOnlyButton;
        private TMP_Text _readOnlyNoticeText;

        public static GameSettingsDialogView Install(Transform parentCanvasTransform)
        {
            if (parentCanvasTransform == null)
            {
                Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    parentCanvasTransform = canvas.transform;
                }
            }

            if (parentCanvasTransform == null)
            {
                return null;
            }

            var dialogObj = new GameObject("GameSettingsDialogView", typeof(RectTransform));
            dialogObj.transform.SetParent(parentCanvasTransform, false);
            var rect = dialogObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                Stretch(rect);
            }

            var view = dialogObj.AddComponent<GameSettingsDialogView>();
            view.BuildModal();
            return view;
        }

        public void Open(
            GameRulesetSnapshot snapshot,
            bool isReadOnly,
            Action<GameRulesetSnapshot> onSave = null)
        {
            _baseSnapshot = snapshot ?? new GameRulesetSnapshot(
                GameRulesetLimits.CurrentSchemaVersion,
                "custom-ropoly",
                "Custom Rules",
                4,
                1500,
                200,
                60,
                120,
                true,
                false,
                2,
                false,
                0,
                15);

            _isReadOnly = isReadOnly;
            _onSaveCallback = onSave;

            _auctionsEnabled = _baseSnapshot.AuctionsEnabled;
            _startingCash = _baseSnapshot.StartingCash;
            _turnDurationSeconds = _baseSnapshot.TurnDurationSeconds;
            _passStartCash = _baseSnapshot.PassStartCash;
            _fullSetRentMultiplier = _baseSnapshot.FullSetRentMultiplier;
            _mortgageEnabled = _baseSnapshot.MortgageEnabled;
            _vacationRewardEnabled = _baseSnapshot.VacationRewardEnabled;

            if (_titleText != null)
            {
                _titleText.text = _isReadOnly ? "MATCH RULES (READ ONLY)" : "GAME RULES & SETTINGS";
            }

            if (_subtitleText != null)
            {
                _subtitleText.text = _isReadOnly
                    ? "HOST CONFIGURATION APPLIED TO THIS MATCH"
                    : "CUSTOMIZE GAMEPLAY, TIMERS AND PROPERTY ECONOMICS";
            }

            if (_resetButton != null) _resetButton.gameObject.SetActive(!_isReadOnly);
            if (_cancelButton != null) _cancelButton.gameObject.SetActive(!_isReadOnly);
            if (_applyButton != null) _applyButton.gameObject.SetActive(!_isReadOnly);
            if (_closeReadOnlyButton != null) _closeReadOnlyButton.gameObject.SetActive(_isReadOnly);
            if (_readOnlyNoticeText != null) _readOnlyNoticeText.gameObject.SetActive(_isReadOnly);

            RefreshPills();

            if (_overlay != null)
            {
                _overlay.SetActive(true);
                _overlay.transform.SetAsLastSibling();
            }
        }

        public void Close()
        {
            if (_overlay != null)
            {
                _overlay.SetActive(false);
            }
        }

        private void RefreshPills()
        {
            // Auctions: [OFF] [ON]
            UpdatePillPair(_auctionButtons, _auctionLabels, _auctionsEnabled);

            // Starting Cash: [1000] [1500] [2000] [2500]
            for (int i = 0; i < StartingCashValues.Length && i < _startingCashButtons.Count; i++)
            {
                bool active = _startingCash == StartingCashValues[i];
                ApplyPillStyle(_startingCashButtons[i], _startingCashLabels[i], active, !_isReadOnly);
            }

            // Turn Timer: [30s] [60s] [90s] [Unlimited]
            for (int i = 0; i < TurnTimerValues.Length && i < _turnTimerButtons.Count; i++)
            {
                bool active = _turnDurationSeconds == TurnTimerValues[i];
                ApplyPillStyle(_turnTimerButtons[i], _turnTimerLabels[i], active, !_isReadOnly);
            }

            // Pass Start: [100] [200] [300] [400]
            for (int i = 0; i < PassStartValues.Length && i < _passStartButtons.Count; i++)
            {
                bool active = _passStartCash == PassStartValues[i];
                ApplyPillStyle(_passStartButtons[i], _passStartLabels[i], active, !_isReadOnly);
            }

            // Multiplier: [2x] [3x]
            for (int i = 0; i < MultiplierValues.Length && i < _multiplierButtons.Count; i++)
            {
                bool active = _fullSetRentMultiplier == MultiplierValues[i];
                ApplyPillStyle(_multiplierButtons[i], _multiplierLabels[i], active, !_isReadOnly);
            }

            // Mortgage: [OFF] [ON]
            UpdatePillPair(_mortgageButtons, _mortgageLabels, _mortgageEnabled);

            // Vacation Pool: [OFF] [ON]
            UpdatePillPair(_vacationButtons, _vacationLabels, _vacationRewardEnabled);
        }

        private void UpdatePillPair(List<Button> buttons, List<TMP_Text> labels, bool value)
        {
            if (buttons.Count >= 2 && labels.Count >= 2)
            {
                ApplyPillStyle(buttons[0], labels[0], !value, !_isReadOnly);
                ApplyPillStyle(buttons[1], labels[1], value, !_isReadOnly);
            }
        }

        private void ApplyPillStyle(Button button, TMP_Text label, bool isSelected, bool isInteractive)
        {
            if (button == null || label == null) return;

            Color targetColor = isSelected ? PillActiveColor : PillInactiveColor;
            var colors = button.colors;
            colors.normalColor = targetColor;
            colors.highlightedColor = Color.Lerp(targetColor, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(targetColor, Color.black, 0.2f);
            colors.disabledColor = targetColor;
            button.colors = colors;
            button.interactable = isInteractive;

            label.color = isSelected ? Color.white : TextMutedColor;
            label.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
        }

        private void ResetToDefaults()
        {
            if (_isReadOnly) return;

            _auctionsEnabled = false;
            _startingCash = 1500;
            _turnDurationSeconds = 60;
            _passStartCash = 200;
            _fullSetRentMultiplier = 2;
            _mortgageEnabled = true;
            _vacationRewardEnabled = false;

            RefreshPills();
        }

        private void ApplyAndClose()
        {
            if (_isReadOnly)
            {
                Close();
                return;
            }

            var snapshot = new GameRulesetSnapshot(
                GameRulesetLimits.CurrentSchemaVersion,
                _baseSnapshot != null ? _baseSnapshot.RulesetId : "custom-ropoly",
                _baseSnapshot != null ? _baseSnapshot.DisplayName : "Custom Rules",
                _baseSnapshot != null ? _baseSnapshot.PlayerCount : 4,
                _startingCash,
                _passStartCash,
                _turnDurationSeconds,
                _baseSnapshot != null ? _baseSnapshot.DisconnectGraceSeconds : 120,
                _mortgageEnabled,
                _auctionsEnabled,
                _fullSetRentMultiplier,
                _vacationRewardEnabled,
                _vacationRewardEnabled ? 500 : 0,
                _baseSnapshot != null ? _baseSnapshot.AuctionBidTimeoutSeconds : 15);

            GameRulesetData.SaveToPlayerPrefs(snapshot);
            _onSaveCallback?.Invoke(snapshot);
            Close();
        }

        private void BuildModal()
        {
            _font = UnityEngine.Object.FindFirstObjectByType<TMP_Text>()?.font;

            _overlay = CreatePanel("SettingsOverlay", transform, BackdropColor);
            Stretch(_overlay.GetComponent<RectTransform>());

            _shell = CreatePanel("SettingsShell", _overlay.transform, PanelColor);
            SetRect(_shell.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 610f));

            // Header
            _titleText = CreateText("Title", _shell.transform, "GAME RULES & SETTINGS", 22f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 265f), new Vector2(580f, 32f), MintColor);

            _subtitleText = CreateText("Subtitle", _shell.transform, "CUSTOMIZE GAMEPLAY, TIMERS AND PROPERTY ECONOMICS", 12f,
                FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, 240f), new Vector2(580f, 22f), TextMutedColor);

            // Setting Rows
            float startY = 195f;
            float rowStep = 50f;

            // 1. Property Auctions
            BuildToggleRow("Row_Auctions", "AUCTIONS ON REJECT", "Auction unbought property",
                startY - (0 * rowStep), _auctionButtons, _auctionLabels,
                val => { if (!_isReadOnly) { _auctionsEnabled = val; RefreshPills(); } });

            // 2. Starting Cash
            BuildMultiChoiceRow("Row_StartingCash", "STARTING CASH", "Initial bankroll per player",
                startY - (1 * rowStep), new[] { "$1,000", "$1,500", "$2,000", "$2,500" },
                StartingCashValues, _startingCashButtons, _startingCashLabels,
                val => { if (!_isReadOnly) { _startingCash = val; RefreshPills(); } });

            // 3. Turn Timer
            BuildMultiChoiceRow("Row_TurnTimer", "TURN TIMER", "Inactivity timeout limit",
                startY - (2 * rowStep), new[] { "30s", "60s", "90s", "UNLIMITED" },
                TurnTimerValues, _turnTimerButtons, _turnTimerLabels,
                val => { if (!_isReadOnly) { _turnDurationSeconds = val; RefreshPills(); } });

            // 4. Pass Start Cash
            BuildMultiChoiceRow("Row_PassStart", "PASS START CASH", "Salary on Start tile",
                startY - (3 * rowStep), new[] { "$100", "$200", "$300", "$400" },
                PassStartValues, _passStartButtons, _passStartLabels,
                val => { if (!_isReadOnly) { _passStartCash = val; RefreshPills(); } });

            // 5. Full Set Rent Multiplier
            BuildMultiChoiceRow("Row_RentMultiplier", "COLOR SET RENT", "Rent for unimproved sets",
                startY - (4 * rowStep), new[] { "2x STANDARD", "3x BOOSTED" },
                MultiplierValues, _multiplierButtons, _multiplierLabels,
                val => { if (!_isReadOnly) { _fullSetRentMultiplier = val; RefreshPills(); } });

            // 6. Mortgages
            BuildToggleRow("Row_Mortgages", "MORTGAGE RULES", "Allow mortgaging properties",
                startY - (5 * rowStep), _mortgageButtons, _mortgageLabels,
                val => { if (!_isReadOnly) { _mortgageEnabled = val; RefreshPills(); } });

            // 7. Vacation Jackpot
            BuildToggleRow("Row_Vacation", "VACATION JACKPOT", "Free Parking collects fines & taxes",
                startY - (6 * rowStep), _vacationButtons, _vacationLabels,
                val => { if (!_isReadOnly) { _vacationRewardEnabled = val; RefreshPills(); } });

            // Read-Only Banner Text
            _readOnlyNoticeText = CreateText("ReadOnlyNotice", _shell.transform,
                "HOST AUTHORITY • RULES CAN ONLY BE MODIFIED BY THE HOST", 12f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, -215f), new Vector2(560f, 24f), CoralColor);
            _readOnlyNoticeText.gameObject.SetActive(false);

            // Bottom Action Buttons
            _resetButton = CreateButton("ResetButton", _shell.transform, "RESET DEFAULTS",
                new Vector2(-190f, -255f), new Vector2(170f, 44f), CardColor, 13f, out _);
            _resetButton.onClick.AddListener(ResetToDefaults);

            _cancelButton = CreateButton("CancelButton", _shell.transform, "CANCEL",
                new Vector2(0f, -255f), new Vector2(150f, 44f), PillInactiveColor, 14f, out _);
            _cancelButton.onClick.AddListener(Close);

            _applyButton = CreateButton("ApplyButton", _shell.transform, "APPLY & SAVE",
                new Vector2(190f, -255f), new Vector2(170f, 44f), EmeraldColor, 14f, out _);
            _applyButton.onClick.AddListener(ApplyAndClose);

            _closeReadOnlyButton = CreateButton("CloseReadOnlyButton", _shell.transform, "CLOSE",
                new Vector2(0f, -255f), new Vector2(220f, 44f), PillActiveColor, 14f, out _);
            _closeReadOnlyButton.onClick.AddListener(Close);
            _closeReadOnlyButton.gameObject.SetActive(false);

            _overlay.SetActive(false);
        }

        private void BuildToggleRow(
            string rowName,
            string label,
            string subtitle,
            float y,
            List<Button> buttons,
            List<TMP_Text> labels,
            Action<bool> onSelect)
        {
            GameObject row = CreatePanel(rowName, _shell.transform, Color.clear);
            SetRect(row.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(560f, 44f));

            CreateText("Label", row.transform, label, 14f, FontStyles.Bold,
                TextAlignmentOptions.Left, new Vector2(-130f, 8f), new Vector2(280f, 22f), Color.white);
            CreateText("Sub", row.transform, subtitle, 11f, FontStyles.Normal,
                TextAlignmentOptions.Left, new Vector2(-130f, -10f), new Vector2(280f, 18f), TextMutedColor);

            Button offBtn = CreateButton("OffBtn", row.transform, "OFF",
                new Vector2(165f, 0f), new Vector2(85f, 34f), PillInactiveColor, 13f, out TMP_Text offLbl);
            offBtn.onClick.AddListener(() => onSelect(false));
            buttons.Add(offBtn);
            labels.Add(offLbl);

            Button onBtn = CreateButton("OnBtn", row.transform, "ON",
                new Vector2(255f, 0f), new Vector2(85f, 34f), PillInactiveColor, 13f, out TMP_Text onLbl);
            onBtn.onClick.AddListener(() => onSelect(true));
            buttons.Add(onBtn);
            labels.Add(onLbl);
        }

        private void BuildMultiChoiceRow(
            string rowName,
            string label,
            string subtitle,
            float y,
            string[] names,
            int[] values,
            List<Button> buttons,
            List<TMP_Text> labels,
            Action<int> onSelect)
        {
            GameObject row = CreatePanel(rowName, _shell.transform, Color.clear);
            SetRect(row.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(560f, 44f));

            CreateText("Label", row.transform, label, 14f, FontStyles.Bold,
                TextAlignmentOptions.Left, new Vector2(-155f, 8f), new Vector2(230f, 22f), Color.white);
            CreateText("Sub", row.transform, subtitle, 11f, FontStyles.Normal,
                TextAlignmentOptions.Left, new Vector2(-155f, -10f), new Vector2(230f, 18f), TextMutedColor);

            float totalWidth = 310f;
            float btnWidth = totalWidth / names.Length;
            float startX = 270f - (btnWidth * (names.Length - 1) * 0.5f);

            for (int i = 0; i < names.Length; i++)
            {
                int val = values[i];
                float posX = 115f + (i * (btnWidth + 4f));
                Button btn = CreateButton($"Btn_{val}", row.transform, names[i],
                    new Vector2(posX, 0f), new Vector2(btnWidth, 34f), PillInactiveColor, 12f, out TMP_Text lbl);
                btn.onClick.AddListener(() => onSelect(val));
                buttons.Add(btn);
                labels.Add(lbl);
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
            colors.disabledColor = new Color(buttonColor.r * 0.45f, buttonColor.g * 0.45f, buttonColor.b * 0.45f, 0.45f);
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

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
