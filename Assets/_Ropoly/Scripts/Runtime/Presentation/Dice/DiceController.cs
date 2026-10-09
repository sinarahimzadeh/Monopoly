using System.Collections;
using Ropoly.Core.Match;
using Ropoly.Presentation.Board;
using Ropoly.Presentation.Lobby;
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

        [Header("Interface")]
        [SerializeField]
        private Button _primaryButton;

        [SerializeField]
        private TMP_Text _primaryButtonLabel;

        [SerializeField]
        private TMP_Text _turnLabel;

        [SerializeField]
        private TMP_Text _resultLabel;

        private readonly SystemDiceRollSource _rollSource = new SystemDiceRollSource();
        private Coroutine _rollRoutine;
        private DiceRoll _lastCompletedRoll;
        private float _firstYaw;
        private float _secondYaw;

        public bool IsRolling => _rollRoutine != null;
        public DiceRoll LastCompletedRoll => _lastCompletedRoll;
        public GameObject DiceRoot => _diceRoot;
        public DieView FirstDie => _firstDie;
        public DieView SecondDie => _secondDie;
        public Button PrimaryButton => _primaryButton;

        private void Awake()
        {
            _diceRoot.SetActive(false);
            _primaryButton.interactable = false;
            _primaryButtonLabel.text = "WAITING FOR PLAYERS";
            _turnLabel.text = "LOCAL MATCH";
            _resultLabel.text = "CHOOSE CREATURES TO BEGIN";
        }

        private void Start()
        {
            _lobby.MatchStarted += HandleMatchStarted;
            _primaryButton.onClick.RemoveAllListeners();
            _primaryButton.onClick.AddListener(HandlePrimaryAction);

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
            if (_lobby.Session == null || IsRolling)
            {
                return;
            }

            if (_lobby.Session.State.Turn.Phase == TurnPhase.AwaitingRoll)
            {
                RollDice();
            }
            else if (_lobby.Session.State.Turn.Phase == TurnPhase.AwaitingMovement)
            {
                AdvanceTurn();
            }
        }

        public void RollDice()
        {
            if (_lobby.Session == null ||
                IsRolling ||
                !_lobby.Session.TryRollDice(_rollSource, out DiceRoll roll))
            {
                return;
            }

            _rollRoutine = StartCoroutine(AnimateRoll(roll));
        }

        public void AdvanceTurn()
        {
            if (IsRolling ||
                _lobby.Session == null ||
                !_lobby.Session.TryAdvanceTurn())
            {
                return;
            }

            RefreshAwaitingRoll();
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
            _primaryButtonLabel.text = "NEXT PLAYER";
            _primaryButton.interactable = true;
            _boardPreview.SetStatusText(
                $"ROLLED {roll.Total}  •  MOVEMENT COMES NEXT");
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
            _boardPreview.SetStatusText($"{player.DisplayName}  •  ROLL THE DICE");
        }

        private Vector3 GetHomePosition(bool first)
        {
            float offset = _settings.DieSeparation * 0.5f;
            return _settings.BoardCenter + (Vector3.right * (first ? -offset : offset));
        }
    }
}
