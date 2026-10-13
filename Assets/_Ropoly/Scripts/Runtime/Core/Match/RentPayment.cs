using System;

namespace Ropoly.Core.Match
{
    /// <summary>
    /// Authoritative record of one rent charge. Pending records block the turn
    /// until a later asset-management step raises enough cash or bankrupts the payer.
    /// </summary>
    [Serializable]
    public sealed class RentPayment
    {
        internal RentPayment(
            int tileIndex,
            string propertyId,
            string propertyDisplayName,
            int payerPlayerIndex,
            int ownerPlayerIndex,
            int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            TileIndex = tileIndex;
            PropertyId = propertyId;
            PropertyDisplayName = propertyDisplayName;
            PayerPlayerIndex = payerPlayerIndex;
            OwnerPlayerIndex = ownerPlayerIndex;
            Amount = amount;
            Status = RentPaymentStatus.AwaitingFunds;
        }

        public int TileIndex { get; }
        public string PropertyId { get; }
        public string PropertyDisplayName { get; }
        public int PayerPlayerIndex { get; }
        public int OwnerPlayerIndex { get; }
        public int Amount { get; }
        public RentPaymentStatus Status { get; private set; }
        public bool IsPaid => Status == RentPaymentStatus.Paid;
        public bool IsResolved => Status != RentPaymentStatus.AwaitingFunds;

        internal void MarkPaid()
        {
            Status = RentPaymentStatus.Paid;
        }

        internal void CancelByBankruptcy()
        {
            Status = RentPaymentStatus.CancelledByBankruptcy;
        }
    }
}
