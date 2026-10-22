using System;
using System.Collections.Generic;

namespace Ropoly.Core.Auctions
{
    [Serializable]
    public sealed class PropertyAuctionState
    {
        public PropertyAuctionState(
            int tileIndex,
            string propertyId,
            string propertyDisplayName,
            int basePrice,
            int timeoutDurationSeconds,
            DateTimeOffset deadline)
        {
            TileIndex = tileIndex;
            PropertyId = propertyId ?? string.Empty;
            PropertyDisplayName = propertyDisplayName ?? string.Empty;
            BasePrice = basePrice;
            TimeoutDurationSeconds = timeoutDurationSeconds;
            Deadline = deadline;
            CurrentBid = 0;
            HighestBidderPlayerIndex = -1;
            WinnerPlayerIndex = -1;
            WinningBid = 0;
            IsActive = true;
            DroppedOutPlayerIndices = new HashSet<int>();
        }

        public int TileIndex { get; }
        public string PropertyId { get; }
        public string PropertyDisplayName { get; }
        public int BasePrice { get; }
        public int TimeoutDurationSeconds { get; }
        public DateTimeOffset Deadline { get; internal set; }
        public int CurrentBid { get; internal set; }
        public int HighestBidderPlayerIndex { get; internal set; }
        public bool IsActive { get; internal set; }
        public int WinnerPlayerIndex { get; internal set; }
        public int WinningBid { get; internal set; }
        public HashSet<int> DroppedOutPlayerIndices { get; }

        public TimeSpan GetRemainingTime(DateTimeOffset now)
        {
            TimeSpan remaining = Deadline - now;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        internal void SetResult(int winnerPlayerIndex, int winningBid)
        {
            WinnerPlayerIndex = winnerPlayerIndex;
            WinningBid = winningBid;
            IsActive = false;
        }
    }
}
