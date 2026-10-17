namespace Ropoly.Core.Trading
{
    public enum TradeOfferStatus
    {
        Pending,
        Accepted,
        Rejected,
        Cancelled
    }

    public enum TradeOfferResult
    {
        Success,
        NotAwaitingTrade,
        TradeAlreadyActive,
        InvalidPlayers,
        PlayerEliminated,
        EmptyTrade,
        InsufficientCash,
        PropertyNotOwned,
        PropertyHasBuildings,
        InsufficientWaivers,
        MatchFinished,
        Unauthorized
    }
}
