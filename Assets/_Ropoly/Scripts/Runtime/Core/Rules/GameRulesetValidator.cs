namespace Ropoly.Core.Rules
{
    public static class GameRulesetValidator
    {
        public static GameRulesetValidationResult Validate(GameRulesetSnapshot snapshot)
        {
            GameRulesetValidationResult result = new GameRulesetValidationResult();
            if (snapshot == null)
            {
                result.Add(RulesetValidationErrorCode.MissingSnapshot, "Ruleset snapshot is required.");
                return result;
            }

            if (snapshot.SchemaVersion != GameRulesetLimits.CurrentSchemaVersion)
            {
                result.Add(
                    RulesetValidationErrorCode.UnsupportedSchemaVersion,
                    $"Schema version must be {GameRulesetLimits.CurrentSchemaVersion}.");
            }

            if (!IsValidStableId(snapshot.RulesetId))
            {
                result.Add(
                    RulesetValidationErrorCode.InvalidRulesetId,
                    "Ruleset ID must be a lowercase stable ID containing letters, numbers, or hyphens.");
            }

            if (string.IsNullOrWhiteSpace(snapshot.DisplayName) ||
                snapshot.DisplayName.Length > GameRulesetLimits.MaximumDisplayNameLength)
            {
                result.Add(
                    RulesetValidationErrorCode.InvalidDisplayName,
                    $"Display name is required and cannot exceed {GameRulesetLimits.MaximumDisplayNameLength} characters.");
            }

            AddRangeErrorIfNeeded(
                result,
                snapshot.PlayerCount,
                GameRulesetLimits.MinimumPlayers,
                GameRulesetLimits.MaximumPlayers,
                RulesetValidationErrorCode.PlayerCountOutOfRange,
                "Player count");

            AddRangeErrorIfNeeded(
                result,
                snapshot.StartingCash,
                GameRulesetLimits.MinimumStartingCash,
                GameRulesetLimits.MaximumStartingCash,
                RulesetValidationErrorCode.StartingCashOutOfRange,
                "Starting cash");

            AddRangeErrorIfNeeded(
                result,
                snapshot.PassStartCash,
                GameRulesetLimits.MinimumPassStartCash,
                GameRulesetLimits.MaximumPassStartCash,
                RulesetValidationErrorCode.PassStartCashOutOfRange,
                "Pass-Start cash");

            AddRangeErrorIfNeeded(
                result,
                snapshot.TurnDurationSeconds,
                GameRulesetLimits.MinimumTurnDurationSeconds,
                GameRulesetLimits.MaximumTurnDurationSeconds,
                RulesetValidationErrorCode.TurnDurationOutOfRange,
                "Turn duration");

            AddRangeErrorIfNeeded(
                result,
                snapshot.DisconnectGraceSeconds,
                GameRulesetLimits.MinimumDisconnectGraceSeconds,
                GameRulesetLimits.MaximumDisconnectGraceSeconds,
                RulesetValidationErrorCode.DisconnectGraceOutOfRange,
                "Disconnect grace duration");

            AddRangeErrorIfNeeded(
                result,
                snapshot.FullSetRentMultiplier,
                GameRulesetLimits.MinimumFullSetRentMultiplier,
                GameRulesetLimits.MaximumFullSetRentMultiplier,
                RulesetValidationErrorCode.FullSetRentMultiplierOutOfRange,
                "Full-set rent multiplier");

            AddRangeErrorIfNeeded(
                result,
                snapshot.VacationReward,
                GameRulesetLimits.MinimumVacationReward,
                GameRulesetLimits.MaximumVacationReward,
                RulesetValidationErrorCode.VacationRewardOutOfRange,
                "Vacation reward");

            return result;
        }

        private static bool IsValidStableId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Length > GameRulesetLimits.MaximumRulesetIdLength ||
                value[0] == '-' ||
                value[value.Length - 1] == '-')
            {
                return false;
            }

            foreach (char character in value)
            {
                bool isLowercaseLetter = character >= 'a' && character <= 'z';
                bool isNumber = character >= '0' && character <= '9';
                if (!isLowercaseLetter && !isNumber && character != '-')
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddRangeErrorIfNeeded(
            GameRulesetValidationResult result,
            int value,
            int minimum,
            int maximum,
            RulesetValidationErrorCode code,
            string fieldName)
        {
            if (value < minimum || value > maximum)
            {
                result.Add(code, $"{fieldName} must be between {minimum} and {maximum}.");
            }
        }
    }
}
