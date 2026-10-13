using System;
using System.Collections.Generic;
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

        private readonly int[] _rentValues;

        internal PropertyState(
            int tileIndex,
            BoardTileKind kind,
            string propertyId,
            string displayName,
            int purchasePrice,
            int mortgageValue,
            IReadOnlyList<int> rentValues)
        {
            TileIndex = tileIndex;
            Kind = kind;
            PropertyId = propertyId;
            DisplayName = displayName;
            PurchasePrice = purchasePrice;
            MortgageValue = mortgageValue;
            _rentValues = Copy(rentValues);
            OwnerPlayerIndex = UnownedPlayerIndex;
        }

        public int TileIndex { get; }
        public BoardTileKind Kind { get; }
        public string PropertyId { get; }
        public string DisplayName { get; }
        public int PurchasePrice { get; }
        public int MortgageValue { get; }
        public IReadOnlyList<int> RentValues => _rentValues;
        public int OwnerPlayerIndex { get; private set; }
        public int UpgradeLevel { get; private set; }
        public bool IsMortgaged { get; private set; }
        public bool IsOwned => OwnerPlayerIndex != UnownedPlayerIndex;
        public int BankSaleValue => IsMortgaged ? 0 : MortgageValue;

        public int CalculateRent(int sameKindOwnedCount, int diceTotal)
        {
            if (!IsOwned || IsMortgaged || _rentValues.Length == 0)
            {
                return 0;
            }

            switch (Kind)
            {
                case BoardTileKind.City:
                    return _rentValues[Math.Min(UpgradeLevel, _rentValues.Length - 1)];
                case BoardTileKind.Airport:
                    return _rentValues[GetOwnedCountIndex(sameKindOwnedCount)];
                case BoardTileKind.Utility:
                    if (diceTotal < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(diceTotal));
                    }

                    return checked(
                        _rentValues[GetOwnedCountIndex(sameKindOwnedCount)] * diceTotal);
                default:
                    return 0;
            }
        }

        internal bool TryAssignOwner(int playerIndex)
        {
            if (playerIndex < 0 || IsOwned)
            {
                return false;
            }

            OwnerPlayerIndex = playerIndex;
            return true;
        }

        internal bool TryMortgage(int playerIndex)
        {
            if (OwnerPlayerIndex != playerIndex || IsMortgaged || MortgageValue <= 0)
            {
                return false;
            }

            IsMortgaged = true;
            return true;
        }

        internal bool TrySellToBank(int playerIndex, out int proceeds)
        {
            proceeds = 0;
            if (OwnerPlayerIndex != playerIndex || BankSaleValue <= 0)
            {
                return false;
            }

            proceeds = BankSaleValue;
            ReleaseToBank();
            return true;
        }

        internal void ReleaseToBank()
        {
            OwnerPlayerIndex = UnownedPlayerIndex;
            UpgradeLevel = 0;
            IsMortgaged = false;
        }

        private int GetOwnedCountIndex(int ownedCount)
        {
            if (ownedCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ownedCount));
            }

            return Math.Min(ownedCount, _rentValues.Length) - 1;
        }

        private static int[] Copy(IReadOnlyList<int> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<int>();
            }

            int[] copy = new int[values.Count];
            for (int index = 0; index < values.Count; index++)
            {
                copy[index] = values[index];
            }

            return copy;
        }
    }
}
