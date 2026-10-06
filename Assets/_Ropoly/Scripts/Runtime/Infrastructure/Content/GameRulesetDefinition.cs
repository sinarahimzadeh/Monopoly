using Ropoly.Core.Rules;
using UnityEngine;

namespace Ropoly.Infrastructure.Content
{
    [CreateAssetMenu(
        fileName = "GameRuleset",
        menuName = "Ropoly/Content/Game Ruleset",
        order = 10)]
    public sealed class GameRulesetDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("Stable lowercase ID used by networking and saved data.")]
        private string _rulesetId = "classic-ropoly";

        [SerializeField]
        private string _displayName = "Classic Ropoly";

        [Header("Players and timing")]
        [SerializeField]
        [Range(GameRulesetLimits.MinimumPlayers, GameRulesetLimits.MaximumPlayers)]
        private int _playerCount = 4;

        [SerializeField]
        [Min(GameRulesetLimits.MinimumStartingCash)]
        private int _startingCash = 1500;

        [SerializeField]
        [Range(
            GameRulesetLimits.MinimumTurnDurationSeconds,
            GameRulesetLimits.MaximumTurnDurationSeconds)]
        private int _turnDurationSeconds = 60;

        [SerializeField]
        [Range(
            GameRulesetLimits.MinimumDisconnectGraceSeconds,
            GameRulesetLimits.MaximumDisconnectGraceSeconds)]
        private int _disconnectGraceSeconds = 120;

        [Header("Property rules")]
        [SerializeField]
        private bool _mortgageEnabled = true;

        [SerializeField]
        private bool _auctionsEnabled = true;

        [SerializeField]
        [Range(
            GameRulesetLimits.MinimumFullSetRentMultiplier,
            GameRulesetLimits.MaximumFullSetRentMultiplier)]
        private int _fullSetRentMultiplier = 2;

        [Header("Vacation reward")]
        [SerializeField]
        private bool _vacationRewardEnabled;

        [SerializeField]
        [Min(GameRulesetLimits.MinimumVacationReward)]
        private int _vacationReward;

        public GameRulesetSnapshot CreateSnapshot()
        {
            return new GameRulesetSnapshot(
                GameRulesetLimits.CurrentSchemaVersion,
                _rulesetId,
                _displayName,
                _playerCount,
                _startingCash,
                _turnDurationSeconds,
                _disconnectGraceSeconds,
                _mortgageEnabled,
                _auctionsEnabled,
                _fullSetRentMultiplier,
                _vacationRewardEnabled,
                _vacationReward);
        }

        public GameRulesetValidationResult Validate()
        {
            return GameRulesetValidator.Validate(CreateSnapshot());
        }

        private void OnValidate()
        {
            _playerCount = Mathf.Clamp(
                _playerCount,
                GameRulesetLimits.MinimumPlayers,
                GameRulesetLimits.MaximumPlayers);
            _startingCash = Mathf.Clamp(
                _startingCash,
                GameRulesetLimits.MinimumStartingCash,
                GameRulesetLimits.MaximumStartingCash);
            _turnDurationSeconds = Mathf.Clamp(
                _turnDurationSeconds,
                GameRulesetLimits.MinimumTurnDurationSeconds,
                GameRulesetLimits.MaximumTurnDurationSeconds);
            _disconnectGraceSeconds = Mathf.Clamp(
                _disconnectGraceSeconds,
                GameRulesetLimits.MinimumDisconnectGraceSeconds,
                GameRulesetLimits.MaximumDisconnectGraceSeconds);
            _fullSetRentMultiplier = Mathf.Clamp(
                _fullSetRentMultiplier,
                GameRulesetLimits.MinimumFullSetRentMultiplier,
                GameRulesetLimits.MaximumFullSetRentMultiplier);
            _vacationReward = Mathf.Clamp(
                _vacationReward,
                GameRulesetLimits.MinimumVacationReward,
                GameRulesetLimits.MaximumVacationReward);
        }
    }
}
