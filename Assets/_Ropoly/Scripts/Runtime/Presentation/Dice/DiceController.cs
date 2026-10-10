using System.Collections;
using Ropoly.Core.Match;
using Ropoly.Presentation.Board;
using Ropoly.Presentation.Lobby;
using Ropoly.Presentation.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ropoly.Presentation.Dice
{
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

        private readonly SystemDiceRollSource _rollSource = new SystemDiceRollSource();
        private Coroutine _rollRoutine;
        private Coroutine _movementRoutine;
        private DiceRoll _lastCompletedRoll;
        private float _firstYaw;
        private float _secondYaw;
        private PropertyPurchaseOffer _pendingOffer;

        public bool IsRolling => _rollRoutine != null;
        public bool IsMoving => _movementRoutine != null;
        public bool IsBusy => IsRolling || IsMoving;
        public DiceRoll LastCompletedRoll => _lastCompletedRoll;
        public GameObject DiceRoot => _diceRoot;
        public DieView FirstDie => _firstDie;
        public DieView SecondDie => _secondDie;
        public Button PrimaryButton => _primaryButton;
        public Button SecondaryButton => _secondaryButton;

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

            if (_lobby.Session != null && _lobby.Session.State.Phase == MatchPhase.InProgress)
            {
                HandleMatchStarted(_lobby.Session);
            }
        }

        private void OnDestroy()
        {
            if (_lobby != null)
            {
                _lobby.MatchStarted -= HandleMatchStarted;
            }
        }

        public void HandlePrimaryAction()
        {
            if (_lobby.Session == null || IsBusy)
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
                RollDice();
            }
            else if (turn.Phase == TurnPhase.AwaitingTurnEnd)
            {
                AdvanceTurn();
            }
            else if (turn.Phase == TurnPhase.AwaitingPropertyDecision)
            {
                PurchaseCurrentProperty();
            }
        }

        public void HandleSecondaryAction()
        {
            if (_lobby.Session == null || IsBusy)
            {
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

        private void PurchaseCurrentProperty()
        {
            PropertyPurchaseOffer offer = _pendingOffer;
            if (offer == null)
            {
                return;
            }

            PropertyPurchaseResult result = _lobby.Session.TryPurchaseCurrentProperty();
            if (result == PropertyPurchaseResult.InsufficientCash)
            {
                _resultLabel.text = "NOT ENOUGH CASH";
                _boardPreview.SetStatusText("NOT ENOUGH CASH  •  SKIP THIS PROPERTY");
                return;
            }

            if (result != PropertyPurchaseResult.Success)
            {
                _resultLabel.text = "PURCHASE ERROR";
                return;
            }

            _pendingOffer = null;
            var creature = _lobby.GetPlayerCreature(offer.PlayerIndex);
            if (creature != null)
            {
                _boardPreview.SetTileOwnerColor(offer.TileIndex, creature.BodyColor);
            }

            _lobby.RefreshMatchHud();
            FinishPropertyDecision($"BOUGHT {offer.DisplayName.ToUpperInvariant()}");
        }

        private void HandleMatchStarted(LocalMatchSession session)
        {
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
            BeginMovement();
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
            string tileName = _boardPreview.GetTileDisplayName(movement.DestinationIndex);
            string startReward = movement.PassedStart
                ? $"  •  PASSED START +${movement.CashAward}"
                : string.Empty;

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
                    movement.DestinationIndex,
                    out PropertyState property) &&
                property.IsOwned)
            {
                string ownerName = _lobby.Session.State.Players[property.OwnerPlayerIndex].DisplayName;
                _resultLabel.text = $"{tileName.ToUpperInvariant()}  •  {ownerName}";
            }

            _boardPreview.SetStatusText(
                $"LANDED ON {tileName.ToUpperInvariant()}{startReward}");
        }

        private void FinishPropertyDecision(string message)
        {
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
            TurnState turn = _lobby.Session.State.Turn;
            PlayerState player = _lobby.Session.State.Players[turn.CurrentPlayerIndex];
            _turnLabel.text = $"TURN {turn.TurnNumber}  •  {player.DisplayName}";
            _resultLabel.text = "READY TO ROLL";
            _primaryButtonLabel.text = "ROLL DICE";
            _primaryButton.interactable = true;
            _pendingOffer = null;
            SetDecisionMode(false);
            _boardPreview.SetStatusText($"{player.DisplayName}  •  ROLL THE DICE");
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

        private Vector3 GetHomePosition(bool first)
        {
            float offset = _settings.DieSeparation * 0.5f;
            return _settings.BoardCenter + (Vector3.right * (first ? -offset : offset));
        }
    }
}
