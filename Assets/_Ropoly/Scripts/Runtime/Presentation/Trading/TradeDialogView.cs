using System;
using System.Collections.Generic;
using System.Linq;
using Ropoly.Core.Board;
using Ropoly.Core.Match;
using Ropoly.Core.Trading;
using Ropoly.Presentation.Board;
using Ropoly.Presentation.Dice;
using Ropoly.Presentation.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ropoly.Presentation.Trading
{
    [DisallowMultipleComponent]
    public sealed class TradeDialogView : MonoBehaviour
    {
        private static readonly Color BackdropColor = new(0.018f, 0.012f, 0.055f, 0.96f);
        private static readonly Color PanelColor = new(0.075f, 0.045f, 0.14f, 1f);
        private static readonly Color CardColor = new(0.12f, 0.08f, 0.22f, 1f);
        private static readonly Color CardSelectedColor = new(0.22f, 0.16f, 0.38f, 1f);
        private static readonly Color PurpleColor = new(0.31f, 0.18f, 0.63f, 1f);
        private static readonly Color MintColor = new(0.36f, 0.92f, 0.76f, 1f);
        private static readonly Color CoralColor = new(0.94f, 0.23f, 0.38f, 1f);
        private static readonly Color GreenColor = new(0.22f, 0.82f, 0.44f, 1f);

        private LocalMatchSession _session;
        private LocalLobbyController _lobby;
        private BoardPreviewController _boardPreview;
        private DiceController _diceController;
        private TMP_FontAsset _font;

        private GameObject _overlay;
        private GameObject _proposalPanel;
        private GameObject _incomingPanel;
        private GameObject _waitingPanel;

        // Proposal state
        private int _proposerSeat;
        private int _targetSeat = -1;
        private int _offeredCash;
        private int _requestedCash;
        private int _offeredWaivers;
        private int _requestedWaivers;
        private readonly HashSet<int> _offeredProperties = new();
        private readonly HashSet<int> _requestedProperties = new();

        // UI references - Proposal
        private Transform _targetButtonsContainer;
        private TMP_Text _offeredCashText;
        private TMP_Text _requestedCashText;
        private TMP_Text _offeredWaiversText;
        private TMP_Text _requestedWaiversText;
        private Transform _offeredPropertiesContainer;
        private Transform _requestedPropertiesContainer;
        private TMP_Text _validationText;
        private Button _proposeButton;

        // UI references - Incoming
        private TMP_Text _incomingHeadingText;
        private TMP_Text _incomingOfferedSummary;
        private TMP_Text _incomingRequestedSummary;
        private Button _acceptButton;
        private Button _rejectButton;
        private Button _counterButton;

        // UI references - Waiting
        private TMP_Text _waitingText;
        private Button _cancelButton;

        // HUD Trade Button
        private Button _hudTradeButton;
        private TMP_Text _hudTradeButtonLabel;

        // Action dispatchers
        private Action<TradeOffer> _onProposeAction;
        private Action<int> _onAcceptAction;
        private Action<int> _onRejectAction;
        private Action<int> _onCancelAction;

        public static TradeDialogView Install(
            LocalLobbyController lobby,
            DiceController diceController,
            BoardPreviewController boardPreview,
            Action<TradeOffer> onPropose,
            Action<int> onAccept,
            Action<int> onReject,
            Action<int> onCancel)
        {
            if (lobby == null || diceController == null)
            {
                return null;
            }

            Canvas canvas = diceController.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            var dialogObj = new GameObject("TradeDialogView");
            dialogObj.transform.SetParent(canvas.transform, false);
            var view = dialogObj.AddComponent<TradeDialogView>();
            view.Initialize(lobby, diceController, boardPreview, onPropose, onAccept, onReject, onCancel);
            return view;
        }

        private void Initialize(
            LocalLobbyController lobby,
            DiceController diceController,
            BoardPreviewController boardPreview,
            Action<TradeOffer> onPropose,
            Action<int> onAccept,
            Action<int> onReject,
            Action<int> onCancel)
        {
            _lobby = lobby;
            _diceController = diceController;
            _boardPreview = boardPreview;
            _session = lobby.Session;
            _onProposeAction = onPropose;
            _onAcceptAction = onAccept;
            _onRejectAction = onReject;
            _onCancelAction = onCancel;
            _font = diceController.PrimaryButton.GetComponentInChildren<TMP_Text>(true)?.font;

            BuildHudButton();
            BuildModals();

            if (_session != null)
            {
                SubscribeSessionEvents(_session);
            }

            _lobby.MatchStarted += HandleMatchStarted;
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

            CloseAllModals();
            RefreshHudButton();
        }

        private void SubscribeSessionEvents(LocalMatchSession session)
        {
            session.TradeProposed += HandleTradeProposed;
            session.TradeCompleted += HandleTradeCompleted;
            session.TradeRejected += HandleTradeRejected;
            session.TradeCancelled += HandleTradeCancelled;
            session.PlayerEliminated += HandlePlayerEliminated;
        }

        private void UnsubscribeSessionEvents(LocalMatchSession session)
        {
            session.TradeProposed -= HandleTradeProposed;
            session.TradeCompleted -= HandleTradeCompleted;
            session.TradeRejected -= HandleTradeRejected;
            session.TradeCancelled -= HandleTradeCancelled;
            session.PlayerEliminated -= HandlePlayerEliminated;
        }

        private void BuildHudButton()
        {
            Transform parent = _diceController.PrimaryButton.transform.parent;
            _hudTradeButton = CreateButton("TradeButton", parent, "TRADE",
                new Vector2(0f, 130f), new Vector2(160f, 44f), PurpleColor, 17f, out _hudTradeButtonLabel);
            _hudTradeButton.onClick.AddListener(OnHudTradeClicked);
            _hudTradeButton.gameObject.SetActive(true);
        }

        private void BuildModals()
        {
            _overlay = CreatePanel("TradeOverlay", transform, BackdropColor);
            Stretch(_overlay.GetComponent<RectTransform>());
            _overlay.transform.SetAsLastSibling();

            GameObject shell = CreatePanel("TradeShell", _overlay.transform, PanelColor);
            SetRect(shell.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 680f));

            BuildProposalPanel(shell.transform);
            BuildIncomingPanel(shell.transform);
            BuildWaitingPanel(shell.transform);

            _overlay.SetActive(false);
        }

        private void BuildProposalPanel(Transform parent)
        {
            _proposalPanel = CreatePanel("ProposalPanel", parent, Color.clear);
            Stretch(_proposalPanel.GetComponent<RectTransform>());

            CreateText("Title", _proposalPanel.transform, "PROPOSE A DEAL", 32f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 305f), new Vector2(600f, 44f), Color.white);

            Button closeButton = CreateButton("CloseButton", _proposalPanel.transform, "CANCEL",
                new Vector2(-410f, 305f), new Vector2(100f, 38f), CardColor, 15f, out _);
            closeButton.onClick.AddListener(CloseAllModals);

            // Target selector row
            CreateText("TargetLabel", _proposalPanel.transform, "TRADE WITH:", 16f, FontStyles.Bold,
                TextAlignmentOptions.Left, new Vector2(-410f, 255f), new Vector2(140f, 30f), MintColor);
            GameObject targetContainer = CreatePanel("TargetButtons", _proposalPanel.transform, Color.clear);
            SetRect(targetContainer.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
                new Vector2(40f, 255f), new Vector2(650f, 36f));
            _targetButtonsContainer = targetContainer.transform;

            // Two columns: Left (Offer), Right (Receive)
            BuildOfferColumn(_proposalPanel.transform, isOffered: true, xPos: -230f);
            BuildOfferColumn(_proposalPanel.transform, isOffered: false, xPos: 230f);

            // Bottom bar
            _validationText = CreateText("Validation", _proposalPanel.transform, "SELECT ITEMS TO TRADE", 16f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, -270f), new Vector2(500f, 30f), MintColor);

            _proposeButton = CreateButton("ProposeButton", _proposalPanel.transform, "SEND OFFER",
                new Vector2(0f, -308f), new Vector2(260f, 46f), GreenColor, 18f, out _);
            _proposeButton.onClick.AddListener(SubmitProposal);
        }

        private void BuildOfferColumn(Transform parent, bool isOffered, float xPos)
        {
            string prefix = isOffered ? "Offer" : "Request";
            string title = isOffered ? "YOU GIVE" : "YOU RECEIVE";
            Color titleColor = isOffered ? CoralColor : MintColor;

            GameObject col = CreatePanel($"{prefix}Col", parent, CardColor);
            SetRect(col.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
                new Vector2(xPos, -5f), new Vector2(440f, 470f));

            CreateText($"{prefix}Title", col.transform, title, 22f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 215f), new Vector2(400f, 32f), titleColor);

            // Cash row
            CreateText($"{prefix}CashLabel", col.transform, "CASH:", 16f, FontStyles.Bold,
                TextAlignmentOptions.Left, new Vector2(-160f, 175f), new Vector2(80f, 28f), Color.white);

            TMP_Text cashText = CreateText($"{prefix}CashVal", col.transform, "$0", 18f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(20f, 175f), new Vector2(100f, 28f), MintColor);
            if (isOffered) _offeredCashText = cashText;
            else _requestedCashText = cashText;

            Button minus50 = CreateButton($"{prefix}Minus50", col.transform, "-50", new Vector2(-80f, 175f), new Vector2(50f, 28f), PurpleColor, 13f, out _);
            Button minus10 = CreateButton($"{prefix}Minus10", col.transform, "-10", new Vector2(-25f, 175f), new Vector2(50f, 28f), PurpleColor, 13f, out _);
            Button plus10 = CreateButton($"{prefix}Plus10", col.transform, "+10", new Vector2(80f, 175f), new Vector2(50f, 28f), PurpleColor, 13f, out _);
            Button plus50 = CreateButton($"{prefix}Plus50", col.transform, "+50", new Vector2(135f, 175f), new Vector2(50f, 28f), PurpleColor, 13f, out _);

            minus50.onClick.AddListener(() => AdjustCash(isOffered, -50));
            minus10.onClick.AddListener(() => AdjustCash(isOffered, -10));
            plus10.onClick.AddListener(() => AdjustCash(isOffered, 10));
            plus50.onClick.AddListener(() => AdjustCash(isOffered, 50));

            // Waivers row
            CreateText($"{prefix}WaiverLabel", col.transform, "WAIVERS:", 15f, FontStyles.Bold,
                TextAlignmentOptions.Left, new Vector2(-160f, 135f), new Vector2(100f, 26f), Color.white);
            TMP_Text waiverText = CreateText($"{prefix}WaiverVal", col.transform, "0", 17f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(20f, 135f), new Vector2(60f, 26f), MintColor);
            if (isOffered) _offeredWaiversText = waiverText;
            else _requestedWaiversText = waiverText;

            Button minusW = CreateButton($"{prefix}MinusW", col.transform, "-", new Vector2(-40f, 135f), new Vector2(36f, 26f), PurpleColor, 15f, out _);
            Button plusW = CreateButton($"{prefix}PlusW", col.transform, "+", new Vector2(75f, 135f), new Vector2(36f, 26f), PurpleColor, 15f, out _);
            minusW.onClick.AddListener(() => AdjustWaivers(isOffered, -1));
            plusW.onClick.AddListener(() => AdjustWaivers(isOffered, 1));

            // Properties label & container
            CreateText($"{prefix}PropLabel", col.transform, "PROPERTIES (CLICK TO SELECT):", 14f, FontStyles.Bold,
                TextAlignmentOptions.Left, new Vector2(-190f, 95f), new Vector2(380f, 24f), new Color(0.76f, 0.72f, 0.86f));

            GameObject propContainer = CreatePanel($"{prefix}Props", col.transform, Color.clear);
            SetRect(propContainer.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -80f), new Vector2(410f, 310f));

            if (isOffered) _offeredPropertiesContainer = propContainer.transform;
            else _requestedPropertiesContainer = propContainer.transform;
        }

        private void BuildIncomingPanel(Transform parent)
        {
            _incomingPanel = CreatePanel("IncomingPanel", parent, Color.clear);
            Stretch(_incomingPanel.GetComponent<RectTransform>());

            _incomingHeadingText = CreateText("IncomingTitle", _incomingPanel.transform, "INCOMING TRADE OFFER", 32f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 280f), new Vector2(700f, 44f), MintColor);

            GameObject offerBox = CreatePanel("TheyOfferBox", _incomingPanel.transform, CardColor);
            SetRect(offerBox.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-220f, 20f), new Vector2(400f, 440f));
            CreateText("TheyOfferTitle", offerBox.transform, "THEY OFFER YOU:", 20f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 195f), new Vector2(360f, 30f), GreenColor);
            _incomingOfferedSummary = CreateText("TheyOfferSummary", offerBox.transform, "", 16f, FontStyles.Normal,
                TextAlignmentOptions.TopLeft, new Vector2(0f, -20f), new Vector2(360f, 370f), Color.white);

            GameObject requestBox = CreatePanel("TheyWantBox", _incomingPanel.transform, CardColor);
            SetRect(requestBox.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(220f, 20f), new Vector2(400f, 440f));
            CreateText("TheyWantTitle", requestBox.transform, "THEY REQUEST FROM YOU:", 20f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 195f), new Vector2(360f, 30f), CoralColor);
            _incomingRequestedSummary = CreateText("TheyWantSummary", requestBox.transform, "", 16f, FontStyles.Normal,
                TextAlignmentOptions.TopLeft, new Vector2(0f, -20f), new Vector2(360f, 370f), Color.white);

            _acceptButton = CreateButton("AcceptBtn", _incomingPanel.transform, "ACCEPT DEAL",
                new Vector2(-200f, -280f), new Vector2(220f, 50f), GreenColor, 18f, out _);
            _acceptButton.onClick.AddListener(AcceptIncomingTrade);

            _counterButton = CreateButton("CounterBtn", _incomingPanel.transform, "COUNTER-OFFER",
                new Vector2(40f, -280f), new Vector2(220f, 50f), PurpleColor, 17f, out _);
            _counterButton.onClick.AddListener(CounterIncomingTrade);

            _rejectButton = CreateButton("RejectBtn", _incomingPanel.transform, "REJECT DEAL",
                new Vector2(280f, -280f), new Vector2(220f, 50f), CoralColor, 18f, out _);
            _rejectButton.onClick.AddListener(RejectIncomingTrade);

            _incomingPanel.SetActive(false);
        }

        private void BuildWaitingPanel(Transform parent)
        {
            _waitingPanel = CreatePanel("WaitingPanel", parent, Color.clear);
            Stretch(_waitingPanel.GetComponent<RectTransform>());

            CreateText("WaitTitle", _waitingPanel.transform, "OFFER SENT", 34f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 120f), new Vector2(700f, 50f), MintColor);

            _waitingText = CreateText("WaitSub", _waitingPanel.transform, "Waiting for other player to review the offer...", 20f, FontStyles.Normal,
                TextAlignmentOptions.Center, new Vector2(0f, 40f), new Vector2(700f, 60f), Color.white);

            _cancelButton = CreateButton("CancelOfferBtn", _waitingPanel.transform, "CANCEL OFFER",
                new Vector2(0f, -80f), new Vector2(240f, 50f), CoralColor, 18f, out _);
            _cancelButton.onClick.AddListener(CancelActiveTrade);

            _waitingPanel.SetActive(false);
        }

        public void OpenTradeDialog(int currentLocalPlayerSeat)
        {
            if (_session == null || _session.State.Phase != MatchPhase.InProgress)
            {
                return;
            }

            // Check if there is an active trade offer
            TradeOffer active = _session.ActiveTradeOffer;
            if (active != null && active.Status == TradeOfferStatus.Pending)
            {
                if (active.TargetPlayerIndex == currentLocalPlayerSeat)
                {
                    ShowIncomingTrade(active);
                    return;
                }

                if (active.ProposerPlayerIndex == currentLocalPlayerSeat)
                {
                    ShowWaitingModal(active);
                    return;
                }
            }

            _proposerSeat = currentLocalPlayerSeat;
            _offeredCash = 0;
            _requestedCash = 0;
            _offeredWaivers = 0;
            _requestedWaivers = 0;
            _offeredProperties.Clear();
            _requestedProperties.Clear();

            // Default target: first available active opponent
            _targetSeat = -1;
            for (int i = 0; i < _session.State.Players.Count; i++)
            {
                if (i != _proposerSeat && !_session.State.Players[i].IsEliminated)
                {
                    _targetSeat = i;
                    break;
                }
            }

            if (_targetSeat < 0)
            {
                _diceController.BoardPreview.SetStatusText("NO OPPONENTS AVAILABLE TO TRADE");
                return;
            }

            ShowProposalModal();
        }

        private void ShowProposalModal()
        {
            _overlay.SetActive(true);
            _proposalPanel.SetActive(true);
            _incomingPanel.SetActive(false);
            _waitingPanel.SetActive(false);

            RefreshTargetButtons();
            RefreshProposalUI();
        }

        private void ShowIncomingTrade(TradeOffer offer)
        {
            _overlay.SetActive(true);
            _proposalPanel.SetActive(false);
            _incomingPanel.SetActive(true);
            _waitingPanel.SetActive(false);

            string proposerName = _session.State.Players[offer.ProposerPlayerIndex].DisplayName;
            _incomingHeadingText.text = $"TRADE OFFER FROM {proposerName.ToUpperInvariant()}";
            _incomingOfferedSummary.text = BuildAssetListString(offer.OfferedCash, offer.OfferedPropertyTileIndices, offer.OfferedWaivers);
            _incomingRequestedSummary.text = BuildAssetListString(offer.RequestedCash, offer.RequestedPropertyTileIndices, offer.RequestedWaivers);
        }

        private void ShowWaitingModal(TradeOffer offer)
        {
            _overlay.SetActive(true);
            _proposalPanel.SetActive(false);
            _incomingPanel.SetActive(false);
            _waitingPanel.SetActive(true);

            string targetName = _session.State.Players[offer.TargetPlayerIndex].DisplayName;
            _waitingText.text = $"Offer sent to {targetName}.\nWaiting for their response...";
        }

        public void CloseAllModals()
        {
            if (_overlay != null)
            {
                _overlay.SetActive(false);
            }
        }

        private void RefreshTargetButtons()
        {
            foreach (Transform child in _targetButtonsContainer)
            {
                Destroy(child.gameObject);
            }

            float buttonWidth = 140f;
            float gap = 10f;
            int count = 0;
            for (int i = 0; i < _session.State.Players.Count; i++)
            {
                if (i == _proposerSeat || _session.State.Players[i].IsEliminated)
                {
                    continue;
                }

                int seatIndex = i;
                PlayerState player = _session.State.Players[i];
                Color btnColor = (seatIndex == _targetSeat) ? MintColor : CardColor;
                float x = count * (buttonWidth + gap) - 180f;

                Button btn = CreateButton($"TargetBtn_{seatIndex}", _targetButtonsContainer, player.DisplayName.ToUpperInvariant(),
                    new Vector2(x, 0f), new Vector2(buttonWidth, 34f), btnColor, 14f, out TMP_Text lbl);
                if (seatIndex == _targetSeat)
                {
                    lbl.color = Color.black;
                }

                btn.onClick.AddListener(() =>
                {
                    _targetSeat = seatIndex;
                    _requestedProperties.Clear();
                    _requestedCash = 0;
                    _requestedWaivers = 0;
                    RefreshTargetButtons();
                    RefreshProposalUI();
                });
                count++;
            }
        }

        private void RefreshProposalUI()
        {
            if (_session == null || _proposerSeat < 0 || _targetSeat < 0)
            {
                return;
            }

            PlayerState proposer = _session.State.Players[_proposerSeat];
            PlayerState target = _session.State.Players[_targetSeat];

            _offeredCash = Mathf.Clamp(_offeredCash, 0, proposer.Cash);
            _requestedCash = Mathf.Clamp(_requestedCash, 0, target.Cash);
            _offeredWaivers = Mathf.Clamp(_offeredWaivers, 0, proposer.HeldDetentionWaivers);
            _requestedWaivers = Mathf.Clamp(_requestedWaivers, 0, target.HeldDetentionWaivers);

            _offeredCashText.text = $"${_offeredCash:N0}";
            _requestedCashText.text = $"${_requestedCash:N0}";
            _offeredWaiversText.text = $"{_offeredWaivers}";
            _requestedWaiversText.text = $"{_requestedWaivers}";

            RebuildPropertyList(_offeredPropertiesContainer, _proposerSeat, _offeredProperties, isOffered: true);
            RebuildPropertyList(_requestedPropertiesContainer, _targetSeat, _requestedProperties, isOffered: false);

            TradeOffer candidate = new TradeOffer(
                null,
                _proposerSeat,
                _targetSeat,
                _offeredCash,
                _offeredProperties.ToList(),
                _offeredWaivers,
                _requestedCash,
                _requestedProperties.ToList(),
                _requestedWaivers);

            bool canPropose = _session.CanProposeTrade(candidate, out TradeOfferResult error);
            _proposeButton.interactable = canPropose;
            _validationText.text = canPropose
                ? "VALID DEAL  •  READY TO PROPOSE"
                : FormatTradeError(error);
            _validationText.color = canPropose ? MintColor : CoralColor;
        }

        private void RebuildPropertyList(Transform container, int playerIndex, HashSet<int> selectedSet, bool isOffered)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

            IReadOnlyList<PropertyState> owned = _session.GetPropertiesOwnedBy(playerIndex);
            float startY = 110f;
            float stepY = 36f;
            int rowIndex = 0;

            foreach (PropertyState prop in owned)
            {
                if (prop.UpgradeLevel > 0 ||
                    (!string.IsNullOrEmpty(prop.CountryId) && _session.HasUpgradesInCountrySet(prop.CountryId)))
                {
                    continue; // cannot trade built properties
                }

                int tileIndex = prop.TileIndex;
                bool isSelected = selectedSet.Contains(tileIndex);
                string tileName = _boardPreview != null ? _boardPreview.GetTileDisplayName(tileIndex) : prop.DisplayName;
                string label = isSelected ? $"✓ {tileName.ToUpperInvariant()}" : tileName.ToUpperInvariant();
                if (prop.IsMortgaged) label += " (MORTGAGED)";

                Color btnColor = isSelected ? CardSelectedColor : PanelColor;
                float y = startY - (rowIndex * stepY);
                if (rowIndex >= 7) break; // visible limit per column

                Button btn = CreateButton($"Prop_{tileIndex}", container, label,
                    new Vector2(0f, y), new Vector2(380f, 32f), btnColor, 13f, out TMP_Text text);
                if (isSelected)
                {
                    text.color = MintColor;
                }

                btn.onClick.AddListener(() =>
                {
                    if (selectedSet.Contains(tileIndex)) selectedSet.Remove(tileIndex);
                    else selectedSet.Add(tileIndex);
                    RefreshProposalUI();
                });
                rowIndex++;
            }

            if (rowIndex == 0)
            {
                CreateText("NoProps", container, "NO UNIMPROVED PROPERTIES", 14f, FontStyles.Normal,
                    TextAlignmentOptions.Center, new Vector2(0f, 40f), new Vector2(360f, 30f), new Color(0.6f, 0.6f, 0.7f));
            }
        }

        private void AdjustCash(bool isOffered, int delta)
        {
            if (isOffered) _offeredCash = Mathf.Max(0, _offeredCash + delta);
            else _requestedCash = Mathf.Max(0, _requestedCash + delta);
            RefreshProposalUI();
        }

        private void AdjustWaivers(bool isOffered, int delta)
        {
            if (isOffered) _offeredWaivers = Mathf.Max(0, _offeredWaivers + delta);
            else _requestedWaivers = Mathf.Max(0, _requestedWaivers + delta);
            RefreshProposalUI();
        }

        private void SubmitProposal()
        {
            TradeOffer offer = new TradeOffer(
                Guid.NewGuid().ToString("N"),
                _proposerSeat,
                _targetSeat,
                _offeredCash,
                _offeredProperties.ToList(),
                _offeredWaivers,
                _requestedCash,
                _requestedProperties.ToList(),
                _requestedWaivers);

            _onProposeAction?.Invoke(offer);
            ShowWaitingModal(offer);
        }

        private void AcceptIncomingTrade()
        {
            TradeOffer active = _session.ActiveTradeOffer;
            if (active != null)
            {
                _onAcceptAction?.Invoke(active.TargetPlayerIndex);
            }

            CloseAllModals();
        }

        private void RejectIncomingTrade()
        {
            TradeOffer active = _session.ActiveTradeOffer;
            if (active != null)
            {
                _onRejectAction?.Invoke(active.TargetPlayerIndex);
            }

            CloseAllModals();
        }

        private void CounterIncomingTrade()
        {
            TradeOffer active = _session.ActiveTradeOffer;
            if (active == null)
            {
                CloseAllModals();
                return;
            }

            TradeOffer counter = active.CreateCounterOffer();
            _onRejectAction?.Invoke(active.TargetPlayerIndex);

            // Open proposal pre-filled with swapped offer
            _proposerSeat = counter.ProposerPlayerIndex;
            _targetSeat = counter.TargetPlayerIndex;
            _offeredCash = counter.OfferedCash;
            _requestedCash = counter.RequestedCash;
            _offeredWaivers = counter.OfferedWaivers;
            _requestedWaivers = counter.RequestedWaivers;
            _offeredProperties.Clear();
            foreach (int t in counter.OfferedPropertyTileIndices) _offeredProperties.Add(t);
            _requestedProperties.Clear();
            foreach (int t in counter.RequestedPropertyTileIndices) _requestedProperties.Add(t);

            ShowProposalModal();
        }

        private void CancelActiveTrade()
        {
            TradeOffer active = _session.ActiveTradeOffer;
            if (active != null)
            {
                _onCancelAction?.Invoke(active.ProposerPlayerIndex);
            }

            CloseAllModals();
        }

        private void OnHudTradeClicked()
        {
            int localSeat = _diceController.GetCurrentLocalPlayerSeat();
            OpenTradeDialog(localSeat);
        }

        public void RefreshHudButton()
        {
            if (_hudTradeButton == null || _session == null)
            {
                return;
            }

            bool inGame = _session.State.Phase == MatchPhase.InProgress;
            _hudTradeButton.gameObject.SetActive(inGame);
        }

        private void HandleTradeProposed(TradeOffer offer)
        {
            int localSeat = _diceController.GetCurrentLocalPlayerSeat();
            if (offer.TargetPlayerIndex == localSeat)
            {
                ShowIncomingTrade(offer);
            }
            else if (offer.ProposerPlayerIndex == localSeat)
            {
                ShowWaitingModal(offer);
            }

            _diceController.BoardPreview.SetStatusText(
                $"{_session.State.Players[offer.ProposerPlayerIndex].DisplayName} PROPOSED A TRADE TO {_session.State.Players[offer.TargetPlayerIndex].DisplayName}");
        }

        private void HandleTradeCompleted(TradeOffer offer)
        {
            CloseAllModals();
            _diceController.RefreshMatchHud();
            _diceController.BoardPreview.SetStatusText(
                $"TRADE COMPLETED: {_session.State.Players[offer.ProposerPlayerIndex].DisplayName} & {_session.State.Players[offer.TargetPlayerIndex].DisplayName}");
        }

        private void HandleTradeRejected(TradeOffer offer)
        {
            CloseAllModals();
            _diceController.BoardPreview.SetStatusText(
                $"{_session.State.Players[offer.TargetPlayerIndex].DisplayName} REJECTED THE TRADE OFFER");
        }

        private void HandleTradeCancelled(TradeOffer offer)
        {
            CloseAllModals();
            _diceController.BoardPreview.SetStatusText("TRADE OFFER WAS CANCELLED");
        }

        private void HandlePlayerEliminated(int playerIndex, EliminationReason reason)
        {
            CloseAllModals();
            RefreshHudButton();
        }

        private string BuildAssetListString(int cash, IReadOnlyList<int> tileIndices, int waivers)
        {
            var lines = new List<string>();
            if (cash > 0)
            {
                lines.Add($"•  Cash:  ${cash:N0}");
            }

            if (waivers > 0)
            {
                lines.Add($"•  Detention Waiver:  {waivers}");
            }

            if (tileIndices != null && tileIndices.Count > 0)
            {
                foreach (int tile in tileIndices)
                {
                    string name = _boardPreview != null ? _boardPreview.GetTileDisplayName(tile) : $"Property {tile}";
                    lines.Add($"•  {name}");
                }
            }

            return lines.Count > 0 ? string.Join("\n\n", lines) : "•  Nothing";
        }

        private static string FormatTradeError(TradeOfferResult error)
        {
            return error switch
            {
                TradeOfferResult.EmptyTrade => "SELECT AT LEAST ONE ASSET TO TRADE",
                TradeOfferResult.InsufficientCash => "INSUFFICIENT CASH",
                TradeOfferResult.InsufficientWaivers => "INSUFFICIENT WAIVERS",
                TradeOfferResult.PropertyNotOwned => "PROPERTY NOT OWNED",
                TradeOfferResult.PropertyHasBuildings => "PROPERTIES WITH BUILDINGS CANNOT BE TRADED",
                TradeOfferResult.PlayerEliminated => "PLAYER IS ELIMINATED",
                TradeOfferResult.TradeAlreadyActive => "ANOTHER TRADE IS ALREADY PENDING",
                _ => "CANNOT PROPOSE TRADE"
            };
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
