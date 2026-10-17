using System;
using System.Collections.Generic;

namespace Ropoly.Core.Trading
{
    [Serializable]
    public sealed class TradeOffer
    {
        public string TradeId { get; }
        public int ProposerPlayerIndex { get; }
        public int TargetPlayerIndex { get; }
        public int OfferedCash { get; }
        public IReadOnlyList<int> OfferedPropertyTileIndices { get; }
        public int OfferedWaivers { get; }
        public int RequestedCash { get; }
        public IReadOnlyList<int> RequestedPropertyTileIndices { get; }
        public int RequestedWaivers { get; }
        public TradeOfferStatus Status { get; internal set; }

        public TradeOffer(
            string tradeId,
            int proposerPlayerIndex,
            int targetPlayerIndex,
            int offeredCash,
            IReadOnlyList<int> offeredPropertyTileIndices,
            int offeredWaivers,
            int requestedCash,
            IReadOnlyList<int> requestedPropertyTileIndices,
            int requestedWaivers)
        {
            TradeId = string.IsNullOrWhiteSpace(tradeId)
                ? Guid.NewGuid().ToString("N")
                : tradeId;
            ProposerPlayerIndex = proposerPlayerIndex;
            TargetPlayerIndex = targetPlayerIndex;
            OfferedCash = Math.Max(0, offeredCash);
            OfferedPropertyTileIndices = offeredPropertyTileIndices != null
                ? new List<int>(offeredPropertyTileIndices).AsReadOnly()
                : Array.Empty<int>();
            OfferedWaivers = Math.Max(0, offeredWaivers);
            RequestedCash = Math.Max(0, requestedCash);
            RequestedPropertyTileIndices = requestedPropertyTileIndices != null
                ? new List<int>(requestedPropertyTileIndices).AsReadOnly()
                : Array.Empty<int>();
            RequestedWaivers = Math.Max(0, requestedWaivers);
            Status = TradeOfferStatus.Pending;
        }

        public TradeOffer CreateCounterOffer(string newTradeId = null)
        {
            return new TradeOffer(
                newTradeId,
                proposerPlayerIndex: TargetPlayerIndex,
                targetPlayerIndex: ProposerPlayerIndex,
                offeredCash: RequestedCash,
                offeredPropertyTileIndices: RequestedPropertyTileIndices,
                offeredWaivers: RequestedWaivers,
                requestedCash: OfferedCash,
                requestedPropertyTileIndices: OfferedPropertyTileIndices,
                requestedWaivers: OfferedWaivers);
        }
    }
}
