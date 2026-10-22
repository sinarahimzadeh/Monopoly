namespace Ropoly.Core.Auctions
{
    public enum AuctionBidResult
    {
        Success,
        NoActiveAuction,
        PlayerEliminated,
        PlayerDroppedOut,
        AlreadyHighestBidder,
        BidTooLow,
        InsufficientCash
    }
}
