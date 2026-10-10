using System;
using Ropoly.Core.Board;

namespace Ropoly.Core.Match
{
    /// <summary>
    /// Immutable decision data presented to the current player after landing.
    /// </summary>
    [Serializable]
    public sealed class PropertyPurchaseOffer
    {
        internal PropertyPurchaseOffer(PropertyState property, int playerIndex, int playerCash)
        {
            TileIndex = property.TileIndex;
            Kind = property.Kind;
            PropertyId = property.PropertyId;
            DisplayName = property.DisplayName;
            PurchasePrice = property.PurchasePrice;
            PlayerIndex = playerIndex;
            PlayerCash = playerCash;
        }

        public int TileIndex { get; }
        public BoardTileKind Kind { get; }
        public string PropertyId { get; }
        public string DisplayName { get; }
        public int PurchasePrice { get; }
        public int PlayerIndex { get; }
        public int PlayerCash { get; }
        public bool CanAfford => PlayerCash >= PurchasePrice;
    }
}
