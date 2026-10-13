namespace Ropoly.Core.Match
{
    public enum PropertyDowngradeResult
    {
        Success = 0,
        NotPlayerTurn = 1,
        NotAllowedInCurrentPhase = 2,
        PropertyNotFound = 3,
        NotACity = 4,
        NotOwnedByPlayer = 5,
        NoUpgradesToSell = 6,
        EvenDowngradeRuleViolation = 7,
    }
}
