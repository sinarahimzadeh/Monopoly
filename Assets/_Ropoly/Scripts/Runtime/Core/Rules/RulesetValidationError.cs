namespace Ropoly.Core.Rules
{
    public enum RulesetValidationErrorCode
    {
        MissingSnapshot,
        UnsupportedSchemaVersion,
        InvalidRulesetId,
        InvalidDisplayName,
        PlayerCountOutOfRange,
        StartingCashOutOfRange,
        PassStartCashOutOfRange,
        TurnDurationOutOfRange,
        DisconnectGraceOutOfRange,
        FullSetRentMultiplierOutOfRange,
        VacationRewardOutOfRange,
    }

    public readonly struct RulesetValidationError
    {
        public RulesetValidationError(RulesetValidationErrorCode code, string message)
        {
            Code = code;
            Message = message;
        }

        public RulesetValidationErrorCode Code { get; }

        public string Message { get; }

        public override string ToString()
        {
            return $"{Code}: {Message}";
        }
    }
}
