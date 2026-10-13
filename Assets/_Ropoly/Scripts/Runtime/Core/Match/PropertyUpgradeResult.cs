namespace Ropoly.Core.Match
{
    public enum PropertyUpgradeResult
    {
        Success = 0,
        NotPlayerTurn = 1,
        NotAllowedInCurrentPhase = 2,
        PropertyNotFound = 3,
        NotACity = 4,
        NotOwnedByPlayer = 5,
        IsMortgaged = 6,
        CountrySetIncomplete = 7,
        HasMortgagedPropertyInSet = 8,
        MaximumLevelReached = 9,
        EvenBuildRuleViolation = 10,
        InsufficientCash = 11,
    }
}
