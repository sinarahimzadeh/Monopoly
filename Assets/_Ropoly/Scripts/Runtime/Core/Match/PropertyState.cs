using System;
using Ropoly.Core.Board;

namespace Ropoly.Core.Match
{
    /// <summary>
    /// Authoritative ownership state for one purchasable board tile.
    /// </summary>
    [Serializable]
    public sealed class PropertyState
    {
        public const int UnownedPlayerIndex = -1;

        internal PropertyState(
            int tileIndex,
            BoardTileKind kind,
            string propertyId,
            string displayName,
            int purchasePrice)
        {
            TileIndex = tileIndex;
            Kind = kind;
            PropertyId = propertyId;
            DisplayName = displayName;
            PurchasePrice = purchasePrice;
            OwnerPlayerIndex = UnownedPlayerIndex;
        }

        public int TileIndex { get; }
        public BoardTileKind Kind { get; }
        public string PropertyId { get; }
        public string DisplayName { get; }
        public int PurchasePrice { get; }
        public int OwnerPlayerIndex { get; private set; }
        public bool IsOwned => OwnerPlayerIndex != UnownedPlayerIndex;

        internal bool TryAssignOwner(int playerIndex)
        {
            if (playerIndex < 0 || IsOwned)
            {
                return false;
            }

            OwnerPlayerIndex = playerIndex;
            return true;
        }
    }
}
