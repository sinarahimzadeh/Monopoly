using System;
using System.Collections;
using System.Collections.Generic;
using Ropoly.Core.Auctions;
using Ropoly.Core.Match;
using Ropoly.Presentation.Board;
using Ropoly.Presentation.Dice;
using Ropoly.Presentation.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ropoly.Presentation.Auctions
{
    [DisallowMultipleComponent]
    public sealed class AuctionDialogView : MonoBehaviour
    {
        private static readonly Color BackdropColor = new(0.015f, 0.01f, 0.045f, 0.94f);
        private static readonly Color PanelColor = new(0.08f, 0.05f, 0.15f, 1f);
        private static readonly Color CardColor = new(0.13f, 0.09f, 0.24f, 1f);
        private static readonly Color PurpleColor = new(0.31f, 0.18f, 0.63f, 1f);
        private static readonly Color MintColor = new(0.36f, 0.92f, 0.76f, 1f);
        private static readonly Color CoralColor = new(0.94f, 0.23f, 0.38f, 1f);
        private static readonly Color GreenColor = new(0.22f, 0.82f, 0.44f, 1f);
        private static readonly Color GoldColor = new(1f, 0.82f, 0.2f, 1f);

        private LocalLobbyController _lobby;
        private DiceController _diceController;
        private BoardPreviewController _boardPreview;
        private LocalMatchSession _session;
        private TMP_FontAsset _font;

        private GameObject _overlay;
        private GameObject _shell;
        private TMP_Text _titleText;
        private TMP_Text _propertyDetailsText;
        private TMP_Text _currentBidText;
        private TMP_Text _highestBidderText;
        private TMP_Text _timerText;
        private TMP_Text _statusMessageText;

        // Bidding buttons
        private Button _bid1Button;
        private TMP_Text _bid1ButtonLabel;
        private Button _bid10Button;
        private TMP_Text _bid10ButtonLabel;
        private Button _bid100Button;
        private TMP_Text _bid100ButtonLabel;
        private Button _passButton;
        private TMP_Text _passButtonLabel;

        // Player tabs container (for local hotseat seat selection)
        private Transform _playerTabsContainer;
        private readonly List<Button> _playerTabButtons = new();
        private readonly List<TMP_Text> _playerTabLabels = new();

        private Action<int, int> _onBidAction;
        private Action<int> _onPassAction;
        private int _localSeatIndex = -1; // -1 for hotseat (allows selecting active bidder)
        private int _selectedBidderSeat = -1;
        private PropertyAuctionState _activeAuction;
        private Coroutine _autoCloseRoutine;

        public static AuctionDialogView Install(
            LocalLobbyController lobby,
            DiceController diceController,
            BoardPreviewController boardPreview,
            Action<int, int> onBid,
            Action<int> onPass)
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

            var dialogObj = new GameObject("AuctionDialogView", typeof(RectTransform));
            dialogObj.transform.SetParent(canvas.transform, false);
            var rect = dialogObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                Stretch(rect);
            }

            var view = dialogObj.AddComponent<AuctionDialogView>();
            view.Initialize(lobby, diceController, boardPreview, onBid, onPass);
            return view;
        }

        public void Initialize(
            LocalLobbyController lobby,
            DiceController diceController,
            BoardPreviewController boardPreview,
            Action<int, int> onBid,
            Action<int> onPass)
        {
            _lobby = lobby;
            _diceController = diceController;
            _boardPreview = boardPreview;
            _session = lobby.Session;
            _onBidAction = onBid;
            _onPassAction = onPass;
            _font = diceController.PrimaryButton.GetComponentInChildren<TMP_Text>(true)?.font;

            BuildModal();

            if (_session != null)
            {
                SubscribeSessionEvents(_session);
            }

            _lobby.MatchStarted += HandleMatchStarted;
            _overlay.SetActive(false);
        }

        public void SetLocalSeatIndex(int seatIndex)
        {
            _localSeatIndex = seatIndex;
            if (_localSeatIndex >= 0)
            {
                _selectedBidderSeat = _localSeatIndex;
            }
            RefreshView();
        }

        private void OnDestroy()
        {
            if (_lobby != null)
            {
                _lobby.MatchStarted -= HandleMatchStarted;
            }

            if (_session != null)
            {
                UnsubscribeSessionEvents(_session);
            }
        }

        private void HandleMatchStarted(LocalMatchSession session)
        {
            if (_session != null)
            {
                UnsubscribeSessionEvents(_session);
            }

            _session = session;
            if (_session != null)
            {
                SubscribeSessionEvents(_session);
            }

            Close();
        }

        private void SubscribeSessionEvents(LocalMatchSession session)
        {
            session.AuctionStarted += HandleAuctionStarted;
            session.AuctionBidPlaced += HandleAuctionBidPlaced;
            session.AuctionPlayerPassed += HandleAuctionPlayerPassed;
            session.AuctionCompleted += HandleAuctionCompleted;
        }

        private void UnsubscribeSessionEvents(LocalMatchSession session)
        {
            session.AuctionStarted -= HandleAuctionStarted;
            session.AuctionBidPlaced -= HandleAuctionBidPlaced;
            session.AuctionPlayerPassed -= HandleAuctionPlayerPassed;
            session.AuctionCompleted -= HandleAuctionCompleted;
        }

        private void HandleAuctionStarted(PropertyAuctionState auction)
        {
            OpenAuction(auction);
        }

        private void HandleAuctionBidPlaced(PropertyAuctionState auction, int bidderIndex, int amount)
        {
            _activeAuction = auction;
            RefreshView();
        }

        private void HandleAuctionPlayerPassed(PropertyAuctionState auction, int playerIndex)
        {
            _activeAuction = auction;
            RefreshView();
        }

        private void HandleAuctionCompleted(PropertyAuctionState auction)
        {
            _activeAuction = auction;
            ShowOutcome(auction);
        }

        public void OpenAuction(PropertyAuctionState auction)
        {
            if (auction == null)
            {
                return;
            }

            if (_autoCloseRoutine != null)
            {
                StopCoroutine(_autoCloseRoutine);
                _autoCloseRoutine = null;
            }

            _activeAuction = auction;
            if (_localSeatIndex >= 0)
            {
                _selectedBidderSeat = _localSeatIndex;
            }
            else
            {
                // In local hotseat: pick first active player
                _selectedBidderSeat = FindFirstActiveBidder();
            }

            _overlay.SetActive(true);
            _overlay.transform.SetAsLastSibling();
            RefreshView();
        }

        public void Close()
        {
            if (_autoCloseRoutine != null)
            {
                StopCoroutine(_autoCloseRoutine);
                _autoCloseRoutine = null;
            }

            _activeAuction = null;
            if (_overlay != null)
            {
                _overlay.SetActive(false);
            }
        }

        private void Update()
        {
            if (_activeAuction == null || !_activeAuction.IsActive || _session == null)
            {
                return;
            }

            TimeSpan remaining = _activeAuction.GetRemainingTime(_session.Clock.UtcNow);
            int seconds = Mathf.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
            _timerText.text = $"CLOSING IN: {seconds}s";
            _timerText.color = seconds <= 3 ? CoralColor : GoldColor;
        }

        private void RefreshView()
        {
            if (_activeAuction == null)
            {
                return;
            }

            _titleText.text = "PROPERTY AUCTION";
            _propertyDetailsText.text = $"{_activeAuction.PropertyDisplayName.ToUpperInvariant()}  •  VALUE: ${_activeAuction.BasePrice:N0}";

            if (_activeAuction.CurrentBid > 0 && _activeAuction.HighestBidderPlayerIndex >= 0)
            {
                _currentBidText.text = $"${_activeAuction.CurrentBid:N0}";
                string leaderName = _session != null && _activeAuction.HighestBidderPlayerIndex < _session.State.Players.Count
                    ? _session.State.Players[_activeAuction.HighestBidderPlayerIndex].DisplayName
                    : $"PLAYER {_activeAuction.HighestBidderPlayerIndex + 1}";
                _highestBidderText.text = $"HIGH BIDDER: {leaderName.ToUpperInvariant()}";
            }
            else
            {
                _currentBidText.text = "NO BIDS YET";
                _highestBidderText.text = "STARTING BID: $1";
            }

            RefreshPlayerTabs();
            RefreshActionButtons();
        }

        private void RefreshPlayerTabs()
        {
            if (_session == null)
            {
                return;
            }

            int count = _session.State.Players.Count;
            // If online multiplayer, tabs are not needed (player only controls their own seat)
            if (_localSeatIndex >= 0)
            {
                _playerTabsContainer.gameObject.SetActive(false);
                return;
            }

            _playerTabsContainer.gameObject.SetActive(true);

            while (_playerTabButtons.Count < count)
            {
                int index = _playerTabButtons.Count;
                Button btn = CreateButton($"PlayerTab_{index}", _playerTabsContainer, $"P{index + 1}",
                    Vector2.zero, new Vector2(105f, 36f), CardColor, 12f, out TMP_Text lbl);
                btn.onClick.AddListener(() => OnSelectPlayerTab(index));
                _playerTabButtons.Add(btn);
                _playerTabLabels.Add(lbl);
            }

            for (int i = 0; i < _playerTabButtons.Count; i++)
            {
                if (i >= count)
                {
                    _playerTabButtons[i].gameObject.SetActive(false);
                    continue;
                }

                _playerTabButtons[i].gameObject.SetActive(true);
                PlayerState player = _session.State.Players[i];
                bool isElim = player.IsEliminated;
                bool hasPassed = _activeAuction.DroppedOutPlayerIndices.Contains(i);
                bool isLeader = _activeAuction.HighestBidderPlayerIndex == i;
                bool isSelected = _selectedBidderSeat == i;

                string status = isElim ? "OUT" : (hasPassed ? "PASSED" : (isLeader ? "LEAD" : $"${player.Cash:N0}"));
                _playerTabLabels[i].text = $"{player.DisplayName}\n<size=10>{status}</size>";

                Color tabColor = isSelected ? PurpleColor : (hasPassed || isElim ? new Color(0.1f, 0.08f, 0.14f) : CardColor);
                var colors = _playerTabButtons[i].colors;
                colors.normalColor = tabColor;
                _playerTabButtons[i].colors = colors;
                _playerTabButtons[i].interactable = !isElim && !hasPassed;
            }
        }

        private void RefreshActionButtons()
        {
            if (_session == null || _activeAuction == null || !_activeAuction.IsActive)
            {
                SetButtonsInteractable(false);
                return;
            }

            int actingSeat = _localSeatIndex >= 0 ? _localSeatIndex : _selectedBidderSeat;
            if (actingSeat < 0 || actingSeat >= _session.State.Players.Count)
            {
                SetButtonsInteractable(false);
                _statusMessageText.text = "SELECT AN ACTIVE BIDDER";
                return;
            }

            PlayerState player = _session.State.Players[actingSeat];
            if (player.IsEliminated)
            {
                SetButtonsInteractable(false);
                _statusMessageText.text = "PLAYER IS ELIMINATED";
                return;
            }

            if (_activeAuction.DroppedOutPlayerIndices.Contains(actingSeat))
            {
                SetButtonsInteractable(false);
                _statusMessageText.text = "YOU HAVE PASSED THIS AUCTION";
                return;
            }

            bool isHighBidder = _activeAuction.HighestBidderPlayerIndex == actingSeat;
            if (isHighBidder)
            {
                SetButtonsInteractable(false);
                _statusMessageText.text = "YOU ARE THE CURRENT HIGHEST BIDDER";
                return;
            }

            int cur = _activeAuction.CurrentBid;
            int bid1 = cur == 0 ? 1 : cur + 1;
            int bid10 = cur == 0 ? 10 : cur + 10;
            int bid100 = cur == 0 ? 100 : cur + 100;

            _bid1ButtonLabel.text = cur == 0 ? "BID $1" : $"+$1\n<size=12>${bid1:N0}</size>";
            _bid10ButtonLabel.text = cur == 0 ? "BID $10" : $"+$10\n<size=12>${bid10:N0}</size>";
            _bid100ButtonLabel.text = cur == 0 ? "BID $100" : $"+$100\n<size=12>${bid100:N0}</size>";
            _passButtonLabel.text = "PASS";

            _bid1Button.interactable = player.Cash >= bid1;
            _bid10Button.interactable = player.Cash >= bid10;
            _bid100Button.interactable = player.Cash >= bid100;
            _passButton.interactable = true;

            _statusMessageText.text = $"{player.DisplayName} CASH: ${player.Cash:N0}";
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_bid1Button != null) _bid1Button.interactable = interactable;
            if (_bid10Button != null) _bid10Button.interactable = interactable;
            if (_bid100Button != null) _bid100Button.interactable = interactable;
            if (_passButton != null) _passButton.interactable = interactable;
        }

        private int FindFirstActiveBidder()
        {
            if (_session == null || _activeAuction == null)
            {
                return 0;
            }

            for (int i = 0; i < _session.State.Players.Count; i++)
            {
                if (!_session.State.Players[i].IsEliminated &&
                    !_activeAuction.DroppedOutPlayerIndices.Contains(i))
                {
                    return i;
                }
            }

            return 0;
        }

        private void OnSelectPlayerTab(int index)
        {
            _selectedBidderSeat = index;
            RefreshView();
        }

        private void OnBidClicked(int increment)
        {
            if (_activeAuction == null || !_activeAuction.IsActive)
            {
                return;
            }

            int actingSeat = _localSeatIndex >= 0 ? _localSeatIndex : _selectedBidderSeat;
            int cur = _activeAuction.CurrentBid;
            int amount = cur == 0 ? increment : cur + increment;

            _onBidAction?.Invoke(actingSeat, amount);
        }

        private void OnPassClicked()
        {
            if (_activeAuction == null || !_activeAuction.IsActive)
            {
                return;
            }

            int actingSeat = _localSeatIndex >= 0 ? _localSeatIndex : _selectedBidderSeat;
            _onPassAction?.Invoke(actingSeat);

            // In local hotseat: advance selected tab to next active player
            if (_localSeatIndex < 0)
            {
                _selectedBidderSeat = FindFirstActiveBidder();
                RefreshView();
            }
        }

        private void ShowOutcome(PropertyAuctionState auction)
        {
            SetButtonsInteractable(false);
            if (auction.WinnerPlayerIndex >= 0 && _session != null &&
                auction.WinnerPlayerIndex < _session.State.Players.Count)
            {
                PlayerState winner = _session.State.Players[auction.WinnerPlayerIndex];
                _statusMessageText.text = $"SOLD TO {winner.DisplayName.ToUpperInvariant()} FOR ${auction.WinningBid:N0}!";
                _statusMessageText.color = MintColor;
            }
            else
            {
                _statusMessageText.text = "AUCTION ENDED • PROPERTY REMAINS UNOWNED";
                _statusMessageText.color = CoralColor;
            }

            _timerText.text = "CONCLUDED";
            _timerText.color = Color.white;

            if (_autoCloseRoutine != null)
            {
                StopCoroutine(_autoCloseRoutine);
            }
            _autoCloseRoutine = StartCoroutine(AutoCloseRoutine());
        }

        private IEnumerator AutoCloseRoutine()
        {
            yield return new WaitForSeconds(1.8f);
            Close();
            _autoCloseRoutine = null;
        }

        private void BuildModal()
        {
            _overlay = CreatePanel("AuctionOverlay", transform, BackdropColor);
            Stretch(_overlay.GetComponent<RectTransform>());

            _shell = CreatePanel("AuctionShell", _overlay.transform, PanelColor);
            SetRect(_shell.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 480f));

            // Title
            _titleText = CreateText("Title", _shell.transform, "PROPERTY AUCTION", 22f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 205f), new Vector2(520f, 34f), MintColor);

            // Property details
            _propertyDetailsText = CreateText("PropertyDetails", _shell.transform, "PROPERTY • VALUE: $0", 14f,
                FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, 175f), new Vector2(520f, 26f), Color.white);

            // Center Bid Card
            GameObject bidCard = CreatePanel("BidCard", _shell.transform, CardColor);
            SetRect(bidCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, 65f), new Vector2(500f, 135f));

            _currentBidText = CreateText("CurrentBid", bidCard.transform, "$0", 38f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 25f), new Vector2(480f, 50f), GoldColor);

            _highestBidderText = CreateText("HighestBidder", bidCard.transform, "NO BIDS YET", 14f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, -18f), new Vector2(480f, 25f), MintColor);

            _timerText = CreateText("Timer", bidCard.transform, "CLOSING IN: 15s", 13f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, -44f), new Vector2(480f, 22f), GoldColor);

            // Player tabs row (local hotseat selector)
            GameObject tabsObj = new GameObject("PlayerTabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabsObj.transform.SetParent(_shell.transform, false);
            var tabsRect = tabsObj.GetComponent<RectTransform>();
            SetRect(tabsRect, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(500f, 42f));
            var hlg = tabsObj.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 10f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            _playerTabsContainer = tabsObj.transform;

            // Status message
            _statusMessageText = CreateText("StatusMessage", _shell.transform, "", 13f, FontStyles.Normal,
                TextAlignmentOptions.Center, new Vector2(0f, -70f), new Vector2(500f, 25f), Color.white);

            // Action Buttons Row: +$1, +$10, +$100, PASS
            _bid1Button = CreateButton("Bid1Button", _shell.transform, "+$1",
                new Vector2(-180f, -145f), new Vector2(105f, 54f), PurpleColor, 14f, out _bid1ButtonLabel);
            _bid1Button.onClick.AddListener(() => OnBidClicked(1));

            _bid10Button = CreateButton("Bid10Button", _shell.transform, "+$10",
                new Vector2(-60f, -145f), new Vector2(105f, 54f), PurpleColor, 14f, out _bid10ButtonLabel);
            _bid10Button.onClick.AddListener(() => OnBidClicked(10));

            _bid100Button = CreateButton("Bid100Button", _shell.transform, "+$100",
                new Vector2(60f, -145f), new Vector2(105f, 54f), PurpleColor, 14f, out _bid100ButtonLabel);
            _bid100Button.onClick.AddListener(() => OnBidClicked(100));

            _passButton = CreateButton("PassButton", _shell.transform, "PASS",
                new Vector2(180f, -145f), new Vector2(105f, 54f), CoralColor, 15f, out _passButtonLabel);
            _passButton.onClick.AddListener(OnPassClicked);
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
