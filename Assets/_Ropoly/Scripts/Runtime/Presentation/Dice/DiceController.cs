using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ropoly.Core.Board;
using Ropoly.Core.Cards;
using Ropoly.Core.Match;
using Ropoly.Core.Trading;
using Ropoly.Presentation.Board;
using Ropoly.Presentation.Lobby;
using Ropoly.Presentation.Navigation;
using Ropoly.Presentation.Players;
using Ropoly.Presentation.Results;
using Ropoly.Presentation.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Ropoly.Presentation.Dice
{
    public enum ExternalTurnAction
    {
        RollDice,
        AdvanceTurn,
        PurchaseProperty,
        DeclineProperty,
        UpgradeProperty,
        UseDetentionWaiver,
        PayDetentionBail,
        MortgageProperty,
        SellProperty,
        DeclareBankruptcy,
        ProposeTrade,
        AcceptTrade,
        RejectTrade,
        CancelTrade,
        RequestRematch,
        ReturnToLobby
    }

    [DisallowMultipleComponent]
    public sealed class DiceController : MonoBehaviour
    {
        [Header("Match")]
        [SerializeField]
        private LocalLobbyController _lobby;

        [SerializeField]
        private BoardPreviewController _boardPreview;

        [Header("Dice")]
        [SerializeField]
        private DicePresentationSettings _settings;

        [SerializeField]
        private GameObject _diceRoot;

        [SerializeField]
        private DieView _firstDie;

        [SerializeField]
        private DieView _secondDie;

        [Header("Player movement")]
        [SerializeField]
        [Range(0.05f, 1f)]
        private float _moveStepDuration = 0.18f;

        [SerializeField]
        [Range(0f, 2f)]
        private float _moveHopHeight = 0.48f;

        [Header("Interface")]
        [SerializeField]
        private Button _primaryButton;

        [SerializeField]
        private TMP_Text _primaryButtonLabel;

        [SerializeField]
        private Button _secondaryButton;

        [SerializeField]
        private TMP_Text _secondaryButtonLabel;

        [SerializeField]
        [Min(1f)]
        private float _singleButtonWidth = 250f;

        [SerializeField]
        [Min(1f)]
        private float _decisionButtonWidth = 120f;

        [SerializeField]
        [Min(0f)]
        private float _decisionButtonGap = 10f;

        [SerializeField]
        private TMP_Text _turnLabel;

        [SerializeField]
        private TMP_Text _resultLabel;

        [SerializeField]
        private DebtResolutionView _debtResolutionView;

        [SerializeField]
        private TradeDialogView _tradeDialogView;

        [SerializeField]
        private MatchResultsView _matchResultsView;

        private readonly SystemDiceRollSource _rollSource = new SystemDiceRollSource();
        private Coroutine _rollRoutine;
        private Coroutine _movementRoutine;
        private DiceRoll _lastCompletedRoll;
        private float _firstYaw;
        private float _secondYaw;
        private PropertyPurchaseOffer _pendingOffer;
        private int _pendingUpgradeTileIndex = -1;
        private Action<ExternalTurnAction, int, string> _externalCommandHandler;
        private bool _externalInputAllowed = true;
        private bool _externalAuthorityConfigured;
        private bool _externalSimulationAuthority;
        private int _localSeatIndex = -1;

        public bool IsRolling => _rollRoutine != null;
        public bool IsMoving => _movementRoutine != null;
        public bool IsBusy => IsRolling || IsMoving;
        public DiceRoll LastCompletedRoll => _lastCompletedRoll;
        public GameObject DiceRoot => _diceRoot;
        public DieView FirstDie => _firstDie;
        public DieView SecondDie => _secondDie;
        public Button PrimaryButton => _primaryButton;
        public Button SecondaryButton => _secondaryButton;
        public BoardPreviewController BoardPreview => _boardPreview;
        public DebtResolutionView DebtResolutionView => _debtResolutionView;
        public TradeDialogView TradeDialogView => _tradeDialogView;
        public MatchResultsView MatchResultsView => _matchResultsView;

        private void Awake()
        {
            _diceRoot.SetActive(false);
            _primaryButton.interactable = false;
            SetDecisionMode(false);
            _primaryButtonLabel.text = "WAITING FOR PLAYERS";
            _turnLabel.text = "LOCAL MATCH";
            _resultLabel.text = "CHOOSE CREATURES TO BEGIN";
        }

        private void Start()
        {
            _lobby.MatchStarted += HandleMatchStarted;
            _primaryButton.onClick.RemoveAllListeners();
            _primaryButton.onClick.AddListener(HandlePrimaryAction);
            _secondaryButton.onClick.RemoveAllListeners();
            _secondaryButton.onClick.AddListener(HandleSecondaryAction);

            if (_tradeDialogView == null)
            {
                _tradeDialogView = GetComponentInChildren<TradeDialogView>(true);
            }

            if (_tradeDialogView == null)
            {
                _tradeDialogView = TradeDialogView.Install(
                    _lobby,
                    this,
                    _boardPreview,
                    onPropose: HandleTradeProposedByLocal,
                    onAccept: HandleTradeAcceptedByLocal,
                    onReject: HandleTradeRejectedByLocal,
                    onCancel: HandleTradeCancelledByLocal);
            }

            if (_matchResultsView == null)
            {
                _matchResultsView = GetComponentInChildren<MatchResultsView>(true);
            }

            if (_matchResultsView == null)
            {
                _matchResultsView = MatchResultsView.Install(
                    _lobby,
                    this,
                    _boardPreview,
                    onRematch: HandleRematchAction,
                    onReturnToLobby: HandleReturnToLobbyAction,
                    onReturnToMainMenu: HandleReturnToMainMenuAction);
            }

            if (_lobby.Session != null && _lobby.Session.State.Phase == MatchPhase.InProgress)
            {
                HandleMatchStarted(_lobby.Session);
            }
        }

        private void Update()
        {
            if (_lobby == null || _lobby.Session == null || _lobby.Session.State.Phase != MatchPhase.InProgress)
            {
                return;
            }

            if (!_externalAuthorityConfigured || _externalSimulationAuthority)
            {
                _lobby.Session.Tick();
            }

            if (_lobby.Session.State.Phase == MatchPhase.Finished || IsBusy)
            {
                return;
            }

            UpdateTurnTimerDisplay();
        }

        private void UpdateTurnTimerDisplay()
        {
            TimeSpan? remaining = _lobby.Session.CurrentTurnRemainingTime;
            if (!remaining.HasValue)
            {
                return;
            }

            int remainingSeconds = Mathf.CeilToInt((float)remaining.Value.TotalSeconds);
            TurnState turn = _lobby.Session.State.Turn;
            if (turn == null)
            {
                return;
            }

            PlayerState player = _lobby.Session.State.Players[turn.CurrentPlayerIndex];
            if (turn.Phase == TurnPhase.DebtResolution)
            {
                _debtResolutionView?.UpdateCountdown(remainingSeconds);
            }
            else if (turn.Phase == TurnPhase.AwaitingRoll ||
                     turn.Phase == TurnPhase.AwaitingPropertyDecision ||
                     turn.Phase == TurnPhase.AwaitingTurnEnd)
            {
                _turnLabel.text = $"TURN {turn.TurnNumber}  •  {player.DisplayName}  ({remainingSeconds}s)";
            }
        }

        private void OnDestroy()
        {
            if (_lobby != null)
            {
                _lobby.MatchStarted -= HandleMatchStarted;
                if (_lobby.Session != null)
                {
                    _lobby.Session.PlayerEliminated -= HandlePlayerEliminated;
                    _lobby.Session.MatchEnded -= HandleMatchEnded;
                }
            }
        }

        public void HandlePrimaryAction()
        {
            if (_lobby.Session == null || IsBusy || !_externalInputAllowed)
            {
                return;
            }

            TurnState turn = _lobby.Session.State.Turn;
            if (turn == null)
            {
                return;
            }

            if (turn.Phase == TurnPhase.AwaitingRoll)
            {
                if (TrySendExternalCommand(ExternalTurnAction.RollDice))
                {
                    return;
                }

                RollDice();
            }
            else if (turn.Phase == TurnPhase.AwaitingTurnEnd)
            {
                if (_externalCommandHandler != null)
                {
                    if (_pendingUpgradeTileIndex >= 0)
                    {
                        TrySendExternalCommand(
                            ExternalTurnAction.UpgradeProperty,
                            _pendingUpgradeTileIndex);
                    }
                    else
                    {
                        TrySendExternalCommand(
                            turn.CanRollAgain
                                ? ExternalTurnAction.RollDice
                                : ExternalTurnAction.AdvanceTurn);
                    }
                }
                else if (_pendingUpgradeTileIndex >= 0)
                {
                    UpgradePendingProperty();
                }
                else if (turn.CanRollAgain)
                {
                    RollDice();
                }
                else
                {
                    AdvanceTurn();
                }
            }
            else if (turn.Phase == TurnPhase.AwaitingPropertyDecision)
            {
                if (TrySendExternalCommand(ExternalTurnAction.PurchaseProperty))
                {
                    return;
                }

                PurchaseCurrentProperty();
            }
        }

        public void HandleSecondaryAction()
        {
            if (_lobby.Session == null || IsBusy || !_externalInputAllowed)
            {
                return;
            }

            TurnState turn = _lobby.Session.State.Turn;
            if (_externalCommandHandler != null)
            {
                if (turn != null && turn.Phase == TurnPhase.AwaitingRoll)
                {
                    PlayerState player = _lobby.Session.State.Players[turn.CurrentPlayerIndex];
                    if (player.IsInDetention && player.HeldDetentionWaivers > 0)
                    {
                        TrySendExternalCommand(ExternalTurnAction.UseDetentionWaiver);
                    }
                    else if (player.IsInDetention && player.Cash >= 50)
                    {
                        TrySendExternalCommand(ExternalTurnAction.PayDetentionBail);
                    }
                }
                else if (turn != null && turn.Phase == TurnPhase.AwaitingPropertyDecision)
                {
                    TrySendExternalCommand(ExternalTurnAction.DeclineProperty);
                }
                else if (turn != null && turn.Phase == TurnPhase.AwaitingTurnEnd)
                {
                    TrySendExternalCommand(ExternalTurnAction.AdvanceTurn);
                }

                return;
            }

            if (turn != null && turn.Phase == TurnPhase.AwaitingRoll)
            {
                PlayerState player = _lobby.Session.State.Players[turn.CurrentPlayerIndex];
                if (player.IsInDetention)
                {
                    if (player.HeldDetentionWaivers > 0 && _lobby.Session.TryUseDetentionWaiver())
                    {
                        _lobby.RefreshMatchHud();
                        RefreshAwaitingRoll();
                        _resultLabel.text = "USED WAIVER • READY TO ROLL";
                        _boardPreview.SetStatusText($"{player.DisplayName} USED WAIVER  •  ROLL THE DICE");
                        return;
                    }

                    if (player.Cash >= 50 && _lobby.Session.TryPayDetentionBail())
                    {
                        _lobby.RefreshMatchHud();
                        RefreshAwaitingRoll();
                        _resultLabel.text = "PAID $50 BAIL • READY TO ROLL";
                        _boardPreview.SetStatusText($"{player.DisplayName} PAID $50 BAIL  •  ROLL THE DICE");
                        return;
                    }
                }
            }

            if (_pendingUpgradeTileIndex >= 0)
            {
                _pendingUpgradeTileIndex = -1;
                AdvanceTurn();
                return;
            }

            PropertyPurchaseOffer offer = _pendingOffer;
            if (!_lobby.Session.TryDeclineCurrentProperty())
            {
                return;
            }

            _pendingOffer = null;
            string propertyName = offer == null ? "PROPERTY" : offer.DisplayName.ToUpperInvariant();
            FinishPropertyDecision($"SKIPPED {propertyName}");
        }

        public void RollDice()
        {
            if (_lobby.Session == null ||
                IsBusy ||
                !_lobby.Session.TryRollDice(_rollSource, out DiceRoll roll))
            {
                return;
            }

            _rollRoutine = StartCoroutine(AnimateRoll(roll));
        }

        public void AdvanceTurn()
        {
            if (IsBusy ||
                _lobby.Session == null ||
                !_lobby.Session.TryAdvanceTurn())
            {
                return;
            }

            RefreshAwaitingRoll();
        }

        public void ConfigureExternalAuthority(
            Action<ExternalTurnAction, int, string> commandHandler,
            bool simulationAuthority)
        {
            _externalCommandHandler = commandHandler;
            _externalAuthorityConfigured = commandHandler != null;
            _externalSimulationAuthority = simulationAuthority;
            _externalInputAllowed = false;
            RefreshExternalButtonAvailability();
        }

        public void ConfigureExternalAuthority(
            Action<ExternalTurnAction, int> commandHandler,
            bool simulationAuthority)
        {
            ConfigureExternalAuthority(
                commandHandler != null ? (action, intVal, _) => commandHandler(action, intVal) : null,
                simulationAuthority);
        }

        public void ClearExternalAuthority()
        {
            _externalCommandHandler = null;
            _externalAuthorityConfigured = false;
            _externalSimulationAuthority = false;
            _externalInputAllowed = true;
            RefreshExternalButtonAvailability();
        }

        public void SetExternalInputAllowed(bool allowed)
        {
            if (_externalCommandHandler == null || _externalInputAllowed == allowed)
            {
                return;
            }

            _externalInputAllowed = allowed;
            RefreshExternalButtonAvailability();
        }

        public bool ApplyExternalDiceRoll(int firstDie, int secondDie)
        {
            if (_lobby.Session == null || IsBusy)
            {
                return false;
            }

            var source = new FixedDiceRollSource(firstDie, secondDie);
            if (!_lobby.Session.TryRollDice(source, out DiceRoll roll))
            {
                return false;
            }

            _rollRoutine = StartCoroutine(AnimateRoll(roll));
            return true;
        }

        public bool ApplyExternalAdvanceTurn()
        {
            if (IsBusy || _lobby.Session == null || !_lobby.Session.TryAdvanceTurn())
            {
                return false;
            }

            _pendingUpgradeTileIndex = -1;
            RefreshAwaitingRoll();
            return true;
        }

        public bool ApplyExternalPropertyPurchase()
        {
            return PurchaseCurrentProperty();
        }

        public bool ApplyExternalPropertyDecline()
        {
            PropertyPurchaseOffer offer = _pendingOffer;
            if (_lobby.Session == null || !_lobby.Session.TryDeclineCurrentProperty())
            {
                return false;
            }

            _pendingOffer = null;
            string propertyName = offer == null ? "PROPERTY" : offer.DisplayName.ToUpperInvariant();
            FinishPropertyDecision($"SKIPPED {propertyName}");
            return true;
        }

        public bool ApplyExternalPropertyUpgrade(int tileIndex)
        {
            PropertyUpgradeResult result = _lobby.Session.TryUpgradeProperty(tileIndex);
            if (result != PropertyUpgradeResult.Success)
            {
                _resultLabel.text = $"UPGRADE FAILED ({result})";
                _boardPreview.SetStatusText($"CANNOT UPGRADE  •  {result}");
                return false;
            }

            if (_lobby.Session.TryGetPropertyAt(tileIndex, out PropertyState property))
            {
                _boardPreview.SetTileUpgradeLevel(tileIndex, property.UpgradeLevel);
                _lobby.RefreshMatchHud();
                string tileName = _boardPreview.GetTileDisplayName(tileIndex);
                string buildingName = property.UpgradeLevel >= 5
                    ? "HOTEL"
                    : $"HOUSE {property.UpgradeLevel}";
                _resultLabel.text = $"{tileName.ToUpperInvariant()}  •  {buildingName}";

                if (_lobby.Session.CanUpgradeProperty(tileIndex, out _))
                {
                    _pendingUpgradeTileIndex = tileIndex;
                    _primaryButtonLabel.text = $"UPGRADE ${property.UpgradeCost:N0}";
                    _boardPreview.SetStatusText(
                        $"BUILT {buildingName} ON {tileName.ToUpperInvariant()}  •  UPGRADE AGAIN OR FINISH TURN");
                    return true;
                }
            }

            _pendingUpgradeTileIndex = -1;
            SetDecisionMode(false);
            _primaryButtonLabel.text = "NEXT PLAYER";
            _primaryButton.interactable = true;
            _boardPreview.SetStatusText("UPGRADE COMPLETE  •  END TURN");
            return true;
        }

        public bool ApplyExternalDetentionWaiver()
        {
            if (!_lobby.Session.TryUseDetentionWaiver())
            {
                return false;
            }

            PlayerState player = _lobby.Session.State.Players[
                _lobby.Session.State.Turn.CurrentPlayerIndex];
            _lobby.RefreshMatchHud();
            RefreshAwaitingRoll();
            _resultLabel.text = "USED WAIVER • READY TO ROLL";
            _boardPreview.SetStatusText($"{player.DisplayName} USED WAIVER  •  ROLL THE DICE");
            return true;
        }

        public bool ApplyExternalDetentionBail()
        {
            if (!_lobby.Session.TryPayDetentionBail())
            {
                return false;
            }

            PlayerState player = _lobby.Session.State.Players[
                _lobby.Session.State.Turn.CurrentPlayerIndex];
            _lobby.RefreshMatchHud();
            RefreshAwaitingRoll();
            _resultLabel.text = "PAID $50 BAIL • READY TO ROLL";
            _boardPreview.SetStatusText($"{player.DisplayName} PAID $50 BAIL  •  ROLL THE DICE");
            return true;
        }

        private bool PurchaseCurrentProperty()
        {
            PropertyPurchaseOffer offer = _pendingOffer;
            if (offer == null)
            {
                return false;
            }

            PropertyPurchaseResult result = _lobby.Session.TryPurchaseCurrentProperty();
            if (result == PropertyPurchaseResult.InsufficientCash)
            {
                _resultLabel.text = "NOT ENOUGH CASH";
                _boardPreview.SetStatusText("NOT ENOUGH CASH  •  SKIP THIS PROPERTY");
                return false;
            }

            if (result != PropertyPurchaseResult.Success)
            {
                _resultLabel.text = "PURCHASE ERROR";
                return false;
            }

            _pendingOffer = null;
            var creature = _lobby.GetPlayerCreature(offer.PlayerIndex);
            if (creature != null)
            {
                _boardPreview.SetTileOwnerColor(offer.TileIndex, creature.BodyColor);
            }

            _lobby.RefreshMatchHud();
            FinishPropertyDecision($"BOUGHT {offer.DisplayName.ToUpperInvariant()}");
            return true;
        }

        private bool TrySendExternalCommand(ExternalTurnAction action, int intValue = -1, string stringValue = null)
        {
            if (_externalCommandHandler == null)
            {
                return false;
            }

            _primaryButton.interactable = false;
            _secondaryButton.interactable = false;
            _externalCommandHandler.Invoke(action, intValue, stringValue);
            return true;
        }

        private void RefreshExternalButtonAvailability()
        {
            if (_externalCommandHandler == null || _lobby?.Session?.State?.Turn == null || IsBusy)
            {
                return;
            }

            if (!_externalInputAllowed)
            {
                _primaryButton.interactable = false;
                _secondaryButton.interactable = false;
                return;
            }

            TurnState turn = _lobby.Session.State.Turn;
            _primaryButton.interactable = turn.Phase switch
            {
                TurnPhase.AwaitingRoll => true,
                TurnPhase.AwaitingPropertyDecision => _pendingOffer?.CanAfford == true,
                TurnPhase.AwaitingTurnEnd => true,
                _ => false
            };
            _secondaryButton.interactable = _secondaryButton.gameObject.activeSelf &&
                                            (turn.Phase == TurnPhase.AwaitingRoll ||
                                             turn.Phase == TurnPhase.AwaitingPropertyDecision ||
                                             turn.Phase == TurnPhase.AwaitingTurnEnd);
        }

        private sealed class FixedDiceRollSource : IDiceRollSource
        {
            private readonly int _first;
            private readonly int _second;
            private int _readCount;

            public FixedDiceRollSource(int first, int second)
            {
                _first = Mathf.Clamp(first, 1, 6);
                _second = Mathf.Clamp(second, 1, 6);
            }

            public int NextDieValue()
            {
                return _readCount++ == 0 ? _first : _second;
            }
        }

        private void HandleMatchStarted(LocalMatchSession session)
        {
            if (session != null)
            {
                session.PlayerEliminated -= HandlePlayerEliminated;
                session.PlayerEliminated += HandlePlayerEliminated;
                session.MatchEnded -= HandleMatchEnded;
                session.MatchEnded += HandleMatchEnded;
            }

            _debtResolutionView?.Hide();
            _matchResultsView?.Hide();
            _diceRoot.SetActive(true);
            _firstDie.ApplySettings(_settings);
            _secondDie.ApplySettings(_settings);
            _firstYaw = 0f;
            _secondYaw = 90f;
            _firstDie.CompleteRoll(1, GetHomePosition(first: true), _firstYaw);
            _secondDie.CompleteRoll(6, GetHomePosition(first: false), _secondYaw);
            _lastCompletedRoll = null;
            RefreshAwaitingRoll();
        }

        private IEnumerator AnimateRoll(DiceRoll roll)
        {
            _primaryButton.interactable = false;
            _primaryButtonLabel.text = "ROLLING...";
            _resultLabel.text = "DICE IN THE AIR";
            _boardPreview.SetStatusText("ROLLING THE DICE...");

            Vector3 firstStartPosition = _firstDie.transform.position;
            Vector3 secondStartPosition = _secondDie.transform.position;
            Quaternion firstStartRotation = _firstDie.transform.rotation;
            Quaternion secondStartRotation = _secondDie.transform.rotation;
            Vector3 firstTargetPosition = GetHomePosition(first: true);
            Vector3 secondTargetPosition = GetHomePosition(first: false);
            _firstYaw = Random.Range(0, 4) * 90f;
            _secondYaw = Random.Range(0, 4) * 90f;
            Quaternion firstTargetRotation = DieView.GetRotationForValue(roll.FirstDie, _firstYaw);
            Quaternion secondTargetRotation = DieView.GetRotationForValue(roll.SecondDie, _secondYaw);
            int firstSpin = Random.Range(_settings.MinimumSpinTurns, _settings.MaximumSpinTurns + 1);
            int secondSpin = Random.Range(_settings.MinimumSpinTurns, _settings.MaximumSpinTurns + 1);

            float elapsed = 0f;
            while (elapsed < _settings.RollDuration)
            {
                elapsed += Time.deltaTime;
                float time = Mathf.Clamp01(elapsed / _settings.RollDuration);
                float eased = 1f - Mathf.Pow(1f - time, 3f);
                float arc = Mathf.Sin(time * Mathf.PI) * _settings.ThrowHeight;

                UpdateAnimatedDie(
                    _firstDie,
                    firstStartPosition,
                    firstTargetPosition,
                    firstStartRotation,
                    firstTargetRotation,
                    time,
                    eased,
                    arc,
                    firstSpin,
                    direction: 1f);
                UpdateAnimatedDie(
                    _secondDie,
                    secondStartPosition,
                    secondTargetPosition,
                    secondStartRotation,
                    secondTargetRotation,
                    time,
                    eased,
                    arc * 0.92f,
                    secondSpin,
                    direction: -1f);
                yield return null;
            }

            _firstDie.CompleteRoll(roll.FirstDie, firstTargetPosition, _firstYaw);
            _secondDie.CompleteRoll(roll.SecondDie, secondTargetPosition, _secondYaw);
            _lastCompletedRoll = roll;
            _rollRoutine = null;
            _resultLabel.text = roll.IsDouble
                ? $"DOUBLE {roll.FirstDie}  •  TOTAL {roll.Total}"
                : $"{roll.FirstDie} + {roll.SecondDie}  •  TOTAL {roll.Total}";

            TurnState turn = _lobby.Session.State.Turn;
            PlayerState currentPlayer = _lobby.Session.State.Players[turn.CurrentPlayerIndex];

            if (turn.Phase == TurnPhase.AwaitingTurnEnd && currentPlayer.IsInDetention && roll.IsDouble)
            {
                _resultLabel.text = "3 CONSECUTIVE DOUBLES • TO DETENTION";
                _boardPreview.SetStatusText($"{currentPlayer.DisplayName} ROLLED 3 DOUBLES  •  SENT DIRECTLY TO DETENTION");
                _movementRoutine = StartCoroutine(AnimateFlyToDetention(turn.CurrentPlayerIndex, currentPlayer.BoardPosition));
                yield break;
            }

            if (turn.Phase == TurnPhase.AwaitingTurnEnd && currentPlayer.IsInDetention && !roll.IsDouble)
            {
                _resultLabel.text = $"NO DOUBLES ({currentPlayer.DetentionTurns}/3)";
                _boardPreview.SetStatusText($"{currentPlayer.DisplayName} REMAINS IN DETENTION (TURN {currentPlayer.DetentionTurns}/3)");
                _primaryButtonLabel.text = "NEXT PLAYER";
                _primaryButton.interactable = true;
                SetDecisionMode(false);
                _lobby.RefreshMatchHud();
                yield break;
            }

            if (turn.Phase == TurnPhase.DebtResolution)
            {
                RentPayment payment = turn.RentPayment;
                if (payment != null)
                {
                    PresentRentPayment(payment, string.Empty);
                    yield break;
                }
            }

            BeginMovement();
        }

        private IEnumerator AnimateFlyToDetention(int playerIndex, int detentionTileIndex)
        {
            _primaryButton.interactable = false;
            _primaryButtonLabel.text = "TO DETENTION...";

            CreatureTokenView token = _lobby.GetToken(playerIndex);
            if (token != null &&
                _boardPreview.TryGetTokenPosition(detentionTileIndex, playerIndex, out Vector3 targetPos))
            {
                Vector3 startPos = token.transform.position;
                token.LookTowards(targetPos - startPos);
                float elapsed = 0f;
                const float flyDuration = 0.85f;
                while (elapsed < flyDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / flyDuration);
                    float eased = t * t * (3f - (2f * t));
                    float arc = Mathf.Sin(t * Mathf.PI) * 1.6f;
                    token.transform.position = Vector3.Lerp(startPos, targetPos, eased) + (Vector3.up * arc);
                    yield return null;
                }

                token.transform.position = targetPos;
            }

            _movementRoutine = null;
            _lobby.RefreshMatchHud();
            _resultLabel.text = "IN DETENTION";
            _primaryButtonLabel.text = "NEXT PLAYER";
            _primaryButton.interactable = true;
            SetDecisionMode(false);
        }

        private void BeginMovement()
        {
            if (_lobby.Session == null ||
                !_lobby.Session.TryBeginMovement(out PlayerMovement movement))
            {
                _primaryButtonLabel.text = "MOVEMENT ERROR";
                _boardPreview.SetStatusText("COULD NOT MOVE THE CURRENT PLAYER");
                return;
            }

            _primaryButton.interactable = false;
            _primaryButtonLabel.text = "MOVING...";
            PlayerState player = _lobby.Session.State.Players[movement.PlayerIndex];
            _boardPreview.SetStatusText(
                $"{player.DisplayName}  •  MOVING {movement.Spaces} SPACES");
            _movementRoutine = StartCoroutine(AnimateMovement(movement));
        }

        private IEnumerator AnimateMovement(PlayerMovement movement)
        {
            // Ensure StartCoroutine has assigned the handle before any early completion path.
            yield return null;
            CreatureTokenView token = _lobby.GetToken(movement.PlayerIndex);
            if (token == null)
            {
                Debug.LogError($"Player {movement.PlayerIndex + 1} has no spawned creature token.", this);
                CompleteMovement(movement);
                yield break;
            }

            int tileCount = _boardPreview.TileCount;
            for (int step = 1; step <= movement.Spaces; step++)
            {
                int fromTile = (movement.FromIndex + step - 1) % tileCount;
                int toTile = (movement.FromIndex + step) % tileCount;
                if (!_boardPreview.TryGetTokenPosition(
                        fromTile,
                        movement.PlayerIndex,
                        out Vector3 fromPosition) ||
                    !_boardPreview.TryGetTokenPosition(
                        toTile,
                        movement.PlayerIndex,
                        out Vector3 toPosition))
                {
                    Debug.LogError("The board could not provide a token movement path.", _boardPreview);
                    CompleteMovement(movement);
                    yield break;
                }

                token.LookTowards(toPosition - fromPosition);

                float elapsed = 0f;
                while (elapsed < _moveStepDuration)
                {
                    elapsed += Time.deltaTime;
                    float time = Mathf.Clamp01(elapsed / _moveStepDuration);
                    float eased = time * time * (3f - (2f * time));
                    float hop = Mathf.Sin(time * Mathf.PI) * _moveHopHeight;
                    token.transform.position =
                        Vector3.Lerp(fromPosition, toPosition, eased) + (Vector3.up * hop);
                    yield return null;
                }

                token.transform.position = toPosition;
            }

            CompleteMovement(movement);
        }

        private void CompleteMovement(PlayerMovement movement)
        {
            _movementRoutine = null;
            if (!_lobby.Session.TryCompleteMovement())
            {
                _primaryButtonLabel.text = "MOVEMENT ERROR";
                _boardPreview.SetStatusText("COULD NOT COMPLETE PLAYER MOVEMENT");
                return;
            }

            _lobby.RefreshMatchHud();
            PlayerState player = _lobby.Session.State.Players[movement.PlayerIndex];
            int currentTile = player.BoardPosition;
            int initialDestination = movement.DestinationIndex;
            if (currentTile != initialDestination)
            {
                _movementRoutine = StartCoroutine(AnimateRelocation(movement, initialDestination, currentTile));
                return;
            }

            PresentLandingOutcome(movement.PlayerIndex, currentTile, movement.PassedStart, movement.CashAward, wasRelocated: false);
        }

        private IEnumerator AnimateRelocation(
            PlayerMovement originalMovement,
            int initialTile,
            int targetTile)
        {
            _primaryButton.interactable = false;
            _primaryButtonLabel.text = "RELOCATING...";
            SetDecisionMode(false);
            _pendingOffer = null;
            _pendingUpgradeTileIndex = -1;

            PlayerState player = _lobby.Session.State.Players[originalMovement.PlayerIndex];
            CardSnapshot card = _lobby.Session.State.Turn.DrawnCard;
            if (card != null)
            {
                _resultLabel.text = card.Title.ToUpperInvariant();
                _boardPreview.SetStatusText($"{player.DisplayName}  •  {card.Description}");
            }
            else if (player.IsInDetention)
            {
                _resultLabel.text = "GO TO DETENTION";
                _boardPreview.SetStatusText($"{player.DisplayName}  •  GO DIRECTLY TO DETENTION");
            }

            yield return new WaitForSeconds(0.75f);

            CreatureTokenView token = _lobby.GetToken(originalMovement.PlayerIndex);
            int tileCount = _boardPreview.TileCount;

            if (player.IsInDetention || (card != null && card.EffectKind == CardEffectKind.GoToDetention))
            {
                if (token != null &&
                    _boardPreview.TryGetTokenPosition(initialTile, originalMovement.PlayerIndex, out Vector3 startPos) &&
                    _boardPreview.TryGetTokenPosition(targetTile, originalMovement.PlayerIndex, out Vector3 targetPos))
                {
                    token.LookTowards(targetPos - startPos);
                    float elapsed = 0f;
                    const float flyDuration = 0.85f;
                    while (elapsed < flyDuration)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / flyDuration);
                        float eased = t * t * (3f - (2f * t));
                        float arc = Mathf.Sin(t * Mathf.PI) * 1.6f;
                        token.transform.position = Vector3.Lerp(startPos, targetPos, eased) + (Vector3.up * arc);
                        yield return null;
                    }

                    token.transform.position = targetPos;
                }
            }
            else if (card != null && card.EffectKind == CardEffectKind.MoveRelative && card.RelativeSteps < 0)
            {
                int stepsBack = Mathf.Abs(card.RelativeSteps);
                for (int step = 1; step <= stepsBack; step++)
                {
                    int fromTile = (initialTile - step + 1 + tileCount) % tileCount;
                    int toTile = (initialTile - step + tileCount) % tileCount;
                    if (_boardPreview.TryGetTokenPosition(fromTile, originalMovement.PlayerIndex, out Vector3 fromPos) &&
                        _boardPreview.TryGetTokenPosition(toTile, originalMovement.PlayerIndex, out Vector3 toPos))
                    {
                        if (token != null)
                        {
                            token.LookTowards(toPos - fromPos);
                            float elapsed = 0f;
                            float duration = _moveStepDuration * 0.9f;
                            while (elapsed < duration)
                            {
                                elapsed += Time.deltaTime;
                                float t = Mathf.Clamp01(elapsed / duration);
                                float eased = t * t * (3f - (2f * t));
                                float hop = Mathf.Sin(t * Mathf.PI) * _moveHopHeight;
                                token.transform.position = Vector3.Lerp(fromPos, toPos, eased) + (Vector3.up * hop);
                                yield return null;
                            }

                            token.transform.position = toPos;
                        }
                    }
                }
            }
            else
            {
                int forwardSteps = (targetTile - initialTile + tileCount) % tileCount;
                if (forwardSteps == 0)
                {
                    forwardSteps = tileCount;
                }

                float stepDuration = Mathf.Clamp(1.6f / forwardSteps, 0.04f, 0.09f);
                for (int step = 1; step <= forwardSteps; step++)
                {
                    int fromTile = (initialTile + step - 1) % tileCount;
                    int toTile = (initialTile + step) % tileCount;
                    if (_boardPreview.TryGetTokenPosition(fromTile, originalMovement.PlayerIndex, out Vector3 fromPos) &&
                        _boardPreview.TryGetTokenPosition(toTile, originalMovement.PlayerIndex, out Vector3 toPos))
                    {
                        if (token != null)
                        {
                            token.LookTowards(toPos - fromPos);
                            float elapsed = 0f;
                            while (elapsed < stepDuration)
                            {
                                elapsed += Time.deltaTime;
                                float t = Mathf.Clamp01(elapsed / stepDuration);
                                float eased = t * t * (3f - (2f * t));
                                float hop = Mathf.Sin(t * Mathf.PI) * _moveHopHeight;
                                token.transform.position = Vector3.Lerp(fromPos, toPos, eased) + (Vector3.up * hop);
                                yield return null;
                            }

                            token.transform.position = toPos;
                        }
                    }
                }
            }

            _movementRoutine = null;
            _lobby.RefreshMatchHud();
            PresentLandingOutcome(originalMovement.PlayerIndex, targetTile, originalMovement.PassedStart, originalMovement.CashAward, wasRelocated: true);
        }

        private void PresentLandingOutcome(int playerIndex, int currentTile, bool passedStart, int cashAward, bool wasRelocated)
        {
            PlayerState player = _lobby.Session.State.Players[playerIndex];
            string tileName = _boardPreview.GetTileDisplayName(currentTile);
            string startReward = (passedStart && !wasRelocated)
                ? $"  •  PASSED START +${cashAward}"
                : string.Empty;

            RentPayment rentPayment = _lobby.Session.State.Turn.RentPayment;
            if (rentPayment != null)
            {
                PresentRentPayment(rentPayment, startReward);
                return;
            }

            PropertyPurchaseOffer offer = _lobby.Session.CurrentPurchaseOffer;
            if (offer != null)
            {
                _pendingOffer = offer;
                SetDecisionMode(true);
                _resultLabel.text = $"{offer.DisplayName.ToUpperInvariant()}  •  ${offer.PurchasePrice:N0}";
                _primaryButtonLabel.text = offer.CanAfford
                    ? $"BUY ${offer.PurchasePrice:N0}"
                    : "CAN'T AFFORD";
                _primaryButton.interactable = offer.CanAfford;
                _secondaryButtonLabel.text = "SKIP";
                _secondaryButton.interactable = true;
                _boardPreview.SetStatusText(
                    $"UNOWNED {offer.DisplayName.ToUpperInvariant()}  •  BUY OR SKIP{startReward}");
                return;
            }

            _resultLabel.text = $"LANDED ON {tileName.ToUpperInvariant()}";
            _primaryButtonLabel.text = "NEXT PLAYER";
            _primaryButton.interactable = true;
            SetDecisionMode(false);

            if (_lobby.Session.TryGetPropertyAt(
                    currentTile,
                    out PropertyState property) &&
                property.IsOwned)
            {
                string ownerName = _lobby.Session.State.Players[property.OwnerPlayerIndex].DisplayName;
                _resultLabel.text = $"{tileName.ToUpperInvariant()}  •  {ownerName}";

                if (property.OwnerPlayerIndex == playerIndex &&
                    property.Kind == BoardTileKind.City &&
                    _lobby.Session.CanUpgradeProperty(currentTile, out _))
                {
                    _pendingUpgradeTileIndex = currentTile;
                    string buildingInfo = property.UpgradeLevel > 0
                        ? $"LEVEL {property.UpgradeLevel}"
                        : "UNIMPROVED";
                    _resultLabel.text = $"{tileName.ToUpperInvariant()}  •  {buildingInfo}";
                    _primaryButtonLabel.text = $"UPGRADE ${property.UpgradeCost:N0}";
                    _primaryButton.interactable = true;
                    _secondaryButtonLabel.text = "NEXT PLAYER";
                    _secondaryButton.interactable = true;
                    SetDecisionMode(true);
                    _boardPreview.SetStatusText(
                        $"{ownerName}  •  UPGRADE {tileName.ToUpperInvariant()} FOR ${property.UpgradeCost:N0} OR FINISH TURN{startReward}");
                    return;
                }
            }

            if (player.IsInDetention)
            {
                _resultLabel.text = "IN DETENTION";
                _boardPreview.SetStatusText($"{player.DisplayName} IS NOW IN DETENTION");
            }
            else if (_lobby.Session.State.Turn.DrawnCard != null)
            {
                CardSnapshot drawn = _lobby.Session.State.Turn.DrawnCard;
                _resultLabel.text = $"{drawn.Title.ToUpperInvariant()}";
                _boardPreview.SetStatusText($"{player.DisplayName}  •  {drawn.Description}{startReward}");
            }
            else if (_lobby.Session.State.Turn.VacationRewardAwarded > 0)
            {
                _resultLabel.text = $"VACATION REWARD +${_lobby.Session.State.Turn.VacationRewardAwarded:N0}";
                _boardPreview.SetStatusText($"{player.DisplayName}  •  COLLECTED VACATION REWARD +${_lobby.Session.State.Turn.VacationRewardAwarded:N0}{startReward}");
            }
            else
            {
                _boardPreview.SetStatusText(
                    $"LANDED ON {tileName.ToUpperInvariant()}{startReward}");
            }

            if (_lobby.Session.State.Turn.CanRollAgain)
            {
                _primaryButtonLabel.text = "ROLL AGAIN";
            }
        }

        private void UpgradePendingProperty()
        {
            if (_pendingUpgradeTileIndex < 0)
            {
                return;
            }

            ApplyExternalPropertyUpgrade(_pendingUpgradeTileIndex);
        }

        private void PresentRentPayment(RentPayment payment, string startReward)
        {
            PlayerState payer = _lobby.Session.State.Players[payment.PayerPlayerIndex];
            string ownerName = payment.OwnerPlayerIndex >= 0 && payment.OwnerPlayerIndex < _lobby.Session.State.Players.Count
                ? _lobby.Session.State.Players[payment.OwnerPlayerIndex].DisplayName
                : "THE BANK";
            SetDecisionMode(false);

            if (!payment.IsPaid)
            {
                _resultLabel.text = $"OWES {ownerName}  •  ${payment.Amount:N0}";
                _primaryButtonLabel.text = "RESOLVE DEBT";
                _primaryButton.interactable = false;
                _boardPreview.SetStatusText(
                    $"{payer.DisplayName} NEEDS ${payment.Amount:N0} FOR " +
                    $"{payment.PropertyDisplayName.ToUpperInvariant()}{startReward}");
                _debtResolutionView?.Show(
                    _lobby.Session,
                    MortgageProperty,
                    SellProperty,
                    DeclareBankruptcy);
                return;
            }

            _debtResolutionView?.Hide();
            _resultLabel.text = payment.OwnerPlayerIndex >= 0
                ? $"PAID ${payment.Amount:N0} RENT"
                : $"PAID ${payment.Amount:N0} FEE";
            _primaryButtonLabel.text = _lobby.Session.State.Turn.CanRollAgain ? "ROLL AGAIN" : "NEXT PLAYER";
            _primaryButton.interactable = true;
            _boardPreview.SetStatusText(
                $"{payer.DisplayName} PAID {ownerName} ${payment.Amount:N0}" +
                $" FOR {payment.PropertyDisplayName.ToUpperInvariant()}{startReward}");
        }

        private void FinishPropertyDecision(string message)
        {
            _debtResolutionView?.Hide();
            SetDecisionMode(false);
            _resultLabel.text = message;
            _primaryButtonLabel.text = "NEXT PLAYER";
            _primaryButton.interactable = true;
            _boardPreview.SetStatusText(message);
        }

        private void UpdateAnimatedDie(
            DieView die,
            Vector3 startPosition,
            Vector3 targetPosition,
            Quaternion startRotation,
            Quaternion targetRotation,
            float time,
            float eased,
            float arc,
            int spinTurns,
            float direction)
        {
            Vector3 lateral = new Vector3(
                Mathf.Sin(time * Mathf.PI * 2f) * _settings.SidewaysMotion * direction,
                0f,
                Mathf.Sin(time * Mathf.PI) * _settings.SidewaysMotion * 0.55f);
            Vector3 position = Vector3.Lerp(startPosition, targetPosition, eased) +
                               (Vector3.up * arc) +
                               (lateral * (1f - time));
            Quaternion settledRotation = Quaternion.Slerp(startRotation, targetRotation, eased);
            Quaternion spin = Quaternion.Euler(
                spinTurns * 360f * time,
                (spinTurns + 1) * 360f * time * direction,
                (spinTurns + 2) * 360f * time);
            die.SetPose(position, settledRotation * spin);
        }

        private void RefreshAwaitingRoll()
        {
            _debtResolutionView?.Hide();
            TurnState turn = _lobby.Session.State.Turn;
            PlayerState player = _lobby.Session.State.Players[turn.CurrentPlayerIndex];
            _turnLabel.text = $"TURN {turn.TurnNumber}  •  {player.DisplayName}";
            _pendingOffer = null;
            _pendingUpgradeTileIndex = -1;

            if (player.IsInDetention)
            {
                _resultLabel.text = $"IN DETENTION ({player.DetentionTurns + 1}/3)";
                _primaryButtonLabel.text = "ROLL DOUBLES";
                _primaryButton.interactable = true;

                if (player.HeldDetentionWaivers > 0)
                {
                    _secondaryButtonLabel.text = "USE WAIVER";
                    _secondaryButton.interactable = true;
                    SetDecisionMode(true);
                    _boardPreview.SetStatusText($"{player.DisplayName}  •  IN DETENTION  •  ROLL DOUBLES OR USE WAIVER");
                }
                else if (player.Cash >= 50)
                {
                    _secondaryButtonLabel.text = "PAY $50 BAIL";
                    _secondaryButton.interactable = true;
                    SetDecisionMode(true);
                    _boardPreview.SetStatusText($"{player.DisplayName}  •  IN DETENTION  •  ROLL DOUBLES OR PAY $50 BAIL");
                }
                else
                {
                    SetDecisionMode(false);
                    _boardPreview.SetStatusText($"{player.DisplayName}  •  IN DETENTION  •  ROLL DOUBLES TO ESCAPE");
                }
            }
            else
            {
                _resultLabel.text = "READY TO ROLL";
                _primaryButtonLabel.text = "ROLL DICE";
                _primaryButton.interactable = true;
                SetDecisionMode(false);
                _boardPreview.SetStatusText($"{player.DisplayName}  •  ROLL THE DICE");
            }
        }

        private void MortgageProperty(int tileIndex)
        {
            if (TrySendExternalCommand(ExternalTurnAction.MortgageProperty, tileIndex))
            {
                return;
            }

            ApplyExternalMortgageProperty(tileIndex);
        }

        public bool ApplyExternalMortgageProperty(int tileIndex)
        {
            AssetLiquidationResult result = _lobby.Session.TryMortgageProperty(tileIndex);
            if (result != AssetLiquidationResult.Success)
            {
                _boardPreview.SetStatusText($"MORTGAGE FAILED  •  {result}");
                _debtResolutionView?.Refresh();
                return false;
            }

            _lobby.RefreshMatchHud();
            TryFinishDebtAfterAssetAction("PROPERTY MORTGAGED");
            return true;
        }

        private void SellProperty(int tileIndex)
        {
            if (TrySendExternalCommand(ExternalTurnAction.SellProperty, tileIndex))
            {
                return;
            }

            ApplyExternalSellProperty(tileIndex);
        }

        public bool ApplyExternalSellProperty(int tileIndex)
        {
            if (_lobby.Session.TryGetPropertyAt(tileIndex, out PropertyState property) && property.UpgradeLevel > 0)
            {
                PropertyDowngradeResult downgradeResult = _lobby.Session.TryDowngradeProperty(tileIndex, out int refund);
                if (downgradeResult != PropertyDowngradeResult.Success)
                {
                    _boardPreview.SetStatusText($"DOWNGRADE FAILED  •  {downgradeResult}");
                    _debtResolutionView?.Refresh();
                    return false;
                }

                _boardPreview.SetTileUpgradeLevel(tileIndex, property.UpgradeLevel);
                _lobby.RefreshMatchHud();
                TryFinishDebtAfterAssetAction($"BUILDING SOLD (+${refund:N0})");
                return true;
            }

            AssetLiquidationResult result = _lobby.Session.TrySellPropertyToBank(tileIndex);
            if (result != AssetLiquidationResult.Success)
            {
                _boardPreview.SetStatusText($"SALE FAILED  •  {result}");
                _debtResolutionView?.Refresh();
                return false;
            }

            _boardPreview.ClearTileOwner(tileIndex);
            _lobby.RefreshMatchHud();
            TryFinishDebtAfterAssetAction("PROPERTY SOLD TO BANK");
            return true;
        }

        private void TryFinishDebtAfterAssetAction(string actionMessage)
        {
            RentPaymentResult paymentResult = _lobby.Session.TryPayCurrentDebt();
            if (paymentResult == RentPaymentResult.Success)
            {
                _lobby.RefreshMatchHud();
                PresentRentPayment(_lobby.Session.State.Turn.RentPayment, string.Empty);
                return;
            }

            _resultLabel.text = actionMessage;
            _boardPreview.SetStatusText($"{actionMessage}  •  MORE CASH IS STILL NEEDED");
            _debtResolutionView?.Refresh();
        }

        private void DeclareBankruptcy()
        {
            if (TrySendExternalCommand(ExternalTurnAction.DeclareBankruptcy))
            {
                return;
            }

            ApplyExternalBankruptcy();
        }

        public bool ApplyExternalBankruptcy()
        {
            return _lobby.Session?.TryDeclareCurrentPlayerBankruptcy() == true;
        }

        private void HandlePlayerEliminated(int playerIndex, EliminationReason reason)
        {
            for (int tileIndex = 0; tileIndex < _boardPreview.TileCount; tileIndex++)
            {
                if (_lobby.Session.TryGetPropertyAt(tileIndex, out PropertyState prop) && !prop.IsOwned)
                {
                    _boardPreview.ClearTileOwner(tileIndex);
                }
            }

            _lobby.HidePlayerToken(playerIndex);
            _lobby.RefreshMatchHud();
            _debtResolutionView?.Hide();
            SetDecisionMode(false);

            if (_lobby.Session.State.Phase == MatchPhase.Finished)
            {
                HandleMatchEnded(_lobby.Session.GetMatchResult());
                return;
            }

            string reasonText = reason switch
            {
                EliminationReason.DebtTimeout => "DEBT TIMEOUT",
                EliminationReason.InactivityTimeout => "INACTIVITY",
                EliminationReason.DisconnectTimeout => "DISCONNECTED",
                _ => "BANKRUPTCY"
            };

            PlayerState player = _lobby.Session.State.Players[playerIndex];
            _boardPreview.SetStatusText($"{player.DisplayName} ELIMINATED  •  {reasonText}");

            if (!IsBusy)
            {
                RefreshAwaitingRoll();
            }
        }

        private void SetDecisionMode(bool decisionMode)
        {
            if (_secondaryButton == null)
            {
                return;
            }

            RectTransform primaryRect = _primaryButton.transform as RectTransform;
            if (primaryRect != null)
            {
                primaryRect.sizeDelta = new Vector2(
                    decisionMode ? _decisionButtonWidth : _singleButtonWidth,
                    primaryRect.sizeDelta.y);
                primaryRect.anchoredPosition = new Vector2(
                    decisionMode ? -((_decisionButtonWidth + _decisionButtonGap) * 0.5f) : 0f,
                    primaryRect.anchoredPosition.y);
            }

            RectTransform secondaryRect = _secondaryButton.transform as RectTransform;
            if (secondaryRect != null)
            {
                secondaryRect.sizeDelta = new Vector2(
                    _decisionButtonWidth,
                    secondaryRect.sizeDelta.y);
                secondaryRect.anchoredPosition = new Vector2(
                    (_decisionButtonWidth + _decisionButtonGap) * 0.5f,
                    secondaryRect.anchoredPosition.y);
            }

            _secondaryButton.gameObject.SetActive(decisionMode);
        }

        public int GetCurrentLocalPlayerSeat()
        {
            if (_localSeatIndex >= 0)
            {
                return _localSeatIndex;
            }

            if (_lobby?.Session?.State?.Turn != null)
            {
                return _lobby.Session.State.Turn.CurrentPlayerIndex;
            }

            return 0;
        }

        public void SetLocalSeatIndex(int seatIndex)
        {
            _localSeatIndex = seatIndex;
        }

        public void RefreshMatchHud()
        {
            _lobby?.RefreshMatchHud();
        }

        [Serializable]
        private sealed class TradePayloadData
        {
            public string TradeId;
            public int ProposerSeat;
            public int TargetSeat;
            public int OfferedCash;
            public int[] OfferedPropertyTileIndices;
            public int OfferedWaivers;
            public int RequestedCash;
            public int[] RequestedPropertyTileIndices;
            public int RequestedWaivers;
        }

        private void HandleTradeProposedByLocal(TradeOffer offer)
        {
            if (_externalCommandHandler != null)
            {
                var data = new TradePayloadData
                {
                    TradeId = offer.TradeId,
                    ProposerSeat = offer.ProposerPlayerIndex,
                    TargetSeat = offer.TargetPlayerIndex,
                    OfferedCash = offer.OfferedCash,
                    OfferedPropertyTileIndices = offer.OfferedPropertyTileIndices != null
                        ? offer.OfferedPropertyTileIndices.ToArray()
                        : Array.Empty<int>(),
                    OfferedWaivers = offer.OfferedWaivers,
                    RequestedCash = offer.RequestedCash,
                    RequestedPropertyTileIndices = offer.RequestedPropertyTileIndices != null
                        ? offer.RequestedPropertyTileIndices.ToArray()
                        : Array.Empty<int>(),
                    RequestedWaivers = offer.RequestedWaivers
                };
                string json = JsonUtility.ToJson(data);
                TrySendExternalCommand(ExternalTurnAction.ProposeTrade, -1, json);
            }
            else
            {
                ApplyExternalProposeTrade(offer);
            }
        }

        private void HandleTradeAcceptedByLocal(int playerIndex)
        {
            if (_externalCommandHandler != null)
            {
                TrySendExternalCommand(ExternalTurnAction.AcceptTrade, playerIndex);
            }
            else
            {
                ApplyExternalAcceptTrade(playerIndex);
            }
        }

        private void HandleTradeRejectedByLocal(int playerIndex)
        {
            if (_externalCommandHandler != null)
            {
                TrySendExternalCommand(ExternalTurnAction.RejectTrade, playerIndex);
            }
            else
            {
                ApplyExternalRejectTrade(playerIndex);
            }
        }

        private void HandleTradeCancelledByLocal(int playerIndex)
        {
            if (_externalCommandHandler != null)
            {
                TrySendExternalCommand(ExternalTurnAction.CancelTrade, playerIndex);
            }
            else
            {
                ApplyExternalCancelTrade(playerIndex);
            }
        }

        public bool ApplyExternalProposeTrade(TradeOffer offer)
        {
            if (_lobby?.Session == null || offer == null)
            {
                return false;
            }

            return _lobby.Session.TryProposeTrade(offer, out _);
        }

        public bool ApplyExternalAcceptTrade(int playerIndex)
        {
            if (_lobby?.Session == null)
            {
                return false;
            }

            TradeOffer active = _lobby.Session.ActiveTradeOffer;
            bool success = _lobby.Session.TryAcceptActiveTrade(playerIndex, out _);
            if (success && active != null)
            {
                RefreshBoardPropertyOwnersAfterTrade(active);
                _lobby.RefreshMatchHud();
            }

            return success;
        }

        public bool ApplyExternalRejectTrade(int playerIndex)
        {
            if (_lobby?.Session == null)
            {
                return false;
            }

            return _lobby.Session.TryRejectActiveTrade(playerIndex, out _);
        }

        public bool ApplyExternalCancelTrade(int playerIndex)
        {
            if (_lobby?.Session == null)
            {
                return false;
            }

            return _lobby.Session.TryCancelActiveTrade(playerIndex, out _);
        }

        private void RefreshBoardPropertyOwnersAfterTrade(TradeOffer offer)
        {
            var targetCreature = _lobby.GetPlayerCreature(offer.TargetPlayerIndex);
            if (targetCreature != null && offer.OfferedPropertyTileIndices != null)
            {
                foreach (int tile in offer.OfferedPropertyTileIndices)
                {
                    _boardPreview.SetTileOwnerColor(tile, targetCreature.BodyColor);
                }
            }

            var proposerCreature = _lobby.GetPlayerCreature(offer.ProposerPlayerIndex);
            if (proposerCreature != null && offer.RequestedPropertyTileIndices != null)
            {
                foreach (int tile in offer.RequestedPropertyTileIndices)
                {
                    _boardPreview.SetTileOwnerColor(tile, proposerCreature.BodyColor);
                }
            }
        }

        private void HandleMatchEnded(MatchResult result)
        {
            if (result == null)
            {
                return;
            }

            _turnLabel.text = "MATCH COMPLETE";
            _resultLabel.text = $"{result.WinnerDisplayName} WINS";
            _primaryButtonLabel.text = "GAME OVER";
            _primaryButton.interactable = false;
            _secondaryButton.gameObject.SetActive(false);
            _boardPreview.SetStatusText($"{result.WinnerDisplayName} WINS THE MATCH");
            _tradeDialogView?.CloseAllModals();
            _debtResolutionView?.Hide();
            _matchResultsView?.Show(result);
        }

        private void HandleRematchAction()
        {
            if (_externalCommandHandler != null)
            {
                TrySendExternalCommand(ExternalTurnAction.RequestRematch);
            }
            else
            {
                _matchResultsView?.Hide();
                _lobby.Rematch();
            }
        }

        private void HandleReturnToLobbyAction()
        {
            if (_externalCommandHandler != null)
            {
                TrySendExternalCommand(ExternalTurnAction.ReturnToLobby);
            }
            else
            {
                _matchResultsView?.Hide();
                _lobby.ReturnToLobby();
            }
        }

        private void HandleReturnToMainMenuAction()
        {
            _matchResultsView?.Hide();
            UnityEngine.SceneManagement.SceneManager.LoadScene(AppSceneNames.MainMenu);
        }

        public void ApplyExternalRematch()
        {
            _matchResultsView?.Hide();
            _lobby.Rematch();
        }

        public void ApplyExternalReturnToLobby()
        {
            _matchResultsView?.Hide();
            _lobby.ReturnToLobby();
        }

        private Vector3 GetHomePosition(bool first)
        {
            float offset = _settings.DieSeparation * 0.5f;
            return _settings.BoardCenter + (Vector3.right * (first ? -offset : offset));
        }
    }
}
