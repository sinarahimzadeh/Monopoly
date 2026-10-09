namespace Ropoly.Core.Rules
{
    /// <summary>
    /// Shared limits used by authoring tools, room validation, networking, and the referee.
    /// </summary>
    public static class GameRulesetLimits
    {
        public const int CurrentSchemaVersion = 2;

        public const int MinimumPlayers = 2;
        public const int MaximumPlayers = 4;

        public const int MinimumStartingCash = 1;
        public const int MaximumStartingCash = 1_000_000;

        public const int MinimumPassStartCash = 0;
        public const int MaximumPassStartCash = 1_000_000;

        public const int MinimumTurnDurationSeconds = 10;
        public const int MaximumTurnDurationSeconds = 600;

        public const int MinimumDisconnectGraceSeconds = 0;
        public const int MaximumDisconnectGraceSeconds = 600;

        public const int MinimumFullSetRentMultiplier = 1;
        public const int MaximumFullSetRentMultiplier = 5;

        public const int MinimumVacationReward = 0;
        public const int MaximumVacationReward = 1_000_000;

        public const int MaximumRulesetIdLength = 64;
        public const int MaximumDisplayNameLength = 64;
    }
}
