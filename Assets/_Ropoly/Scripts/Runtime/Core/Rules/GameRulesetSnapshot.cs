using System;

namespace Ropoly.Core.Rules
{
    /// <summary>
    /// Provider-neutral, versioned values used to start a match.
    /// It deliberately contains no Unity object references.
    /// </summary>
    [Serializable]
    public sealed class GameRulesetSnapshot
    {
        public GameRulesetSnapshot(
            int schemaVersion,
            string rulesetId,
            string displayName,
            int playerCount,
            int startingCash,
            int passStartCash,
            int turnDurationSeconds,
            int disconnectGraceSeconds,
            bool mortgageEnabled,
            bool auctionsEnabled,
            int fullSetRentMultiplier,
            bool vacationRewardEnabled,
            int vacationReward)
        {
            SchemaVersion = schemaVersion;
            RulesetId = rulesetId;
            DisplayName = displayName;
            PlayerCount = playerCount;
            StartingCash = startingCash;
            PassStartCash = passStartCash;
            TurnDurationSeconds = turnDurationSeconds;
            DisconnectGraceSeconds = disconnectGraceSeconds;
            MortgageEnabled = mortgageEnabled;
            AuctionsEnabled = auctionsEnabled;
            FullSetRentMultiplier = fullSetRentMultiplier;
            VacationRewardEnabled = vacationRewardEnabled;
            VacationReward = vacationReward;
        }

        public int SchemaVersion { get; }

        public string RulesetId { get; }

        public string DisplayName { get; }

        public int PlayerCount { get; }

        public int StartingCash { get; }

        public int PassStartCash { get; }

        public int TurnDurationSeconds { get; }

        public int DisconnectGraceSeconds { get; }

        public bool MortgageEnabled { get; }

        public bool AuctionsEnabled { get; }

        public int FullSetRentMultiplier { get; }

        public bool VacationRewardEnabled { get; }

        public int VacationReward { get; }
    }
}
