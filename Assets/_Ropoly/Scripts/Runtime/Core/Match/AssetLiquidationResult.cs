namespace Ropoly.Core.Match
{
    public enum AssetLiquidationResult
    {
        Success = 0,
        NotResolvingDebt = 1,
        MortgageDisabled = 2,
        PropertyNotFound = 3,
        NotOwnedByCurrentPlayer = 4,
        AlreadyMortgaged = 5,
        NoLiquidationValue = 6,
    }
}
