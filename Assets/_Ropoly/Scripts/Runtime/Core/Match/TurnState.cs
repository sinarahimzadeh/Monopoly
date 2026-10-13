using System;

namespace Ropoly.Core.Match
{
    [Serializable]
    public sealed class TurnState
    {
        internal TurnState(int currentPlayerIndex)
        {
            CurrentPlayerIndex = currentPlayerIndex;
            TurnNumber = 1;
            Phase = TurnPhase.AwaitingRoll;
            PendingPropertyTileIndex = PropertyState.UnownedPlayerIndex;
        }

        public int CurrentPlayerIndex { get; private set; }
        public int TurnNumber { get; private set; }
        public TurnPhase Phase { get; internal set; }
        public DiceRoll LastRoll { get; internal set; }
        public int PendingPropertyTileIndex { get; private set; }
        public RentPayment RentPayment { get; private set; }
        public DateTimeOffset? Deadline { get; internal set; }
        public bool HasPendingPropertyDecision =>
            PendingPropertyTileIndex != PropertyState.UnownedPlayerIndex;

        public TimeSpan? GetRemainingTime(DateTimeOffset now)
        {
            if (!Deadline.HasValue)
            {
                return null;
            }

            TimeSpan remaining = Deadline.Value - now;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        internal void BeginPropertyDecision(int tileIndex)
        {
            PendingPropertyTileIndex = tileIndex;
            RentPayment = null;
            Phase = TurnPhase.AwaitingPropertyDecision;
        }

        internal void CompleteLanding()
        {
            PendingPropertyTileIndex = PropertyState.UnownedPlayerIndex;
            RentPayment = null;
            Phase = TurnPhase.AwaitingTurnEnd;
        }

        internal void RecordRentPayment(RentPayment payment)
        {
            RentPayment = payment ?? throw new ArgumentNullException(nameof(payment));
            PendingPropertyTileIndex = PropertyState.UnownedPlayerIndex;
            Phase = payment.IsPaid
                ? TurnPhase.AwaitingTurnEnd
                : TurnPhase.DebtResolution;
        }

        internal void CompleteDebtResolution()
        {
            if (RentPayment == null || !RentPayment.IsPaid)
            {
                throw new InvalidOperationException("Debt cannot finish before the rent is paid.");
            }

            Phase = TurnPhase.AwaitingTurnEnd;
        }

        internal void Advance(int playerCount)
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % playerCount;
            ResetForNextTurn();
        }

        internal void AdvanceTo(int playerIndex)
        {
            CurrentPlayerIndex = playerIndex;
            ResetForNextTurn();
        }

        private void ResetForNextTurn()
        {
            TurnNumber++;
            LastRoll = null;
            PendingPropertyTileIndex = PropertyState.UnownedPlayerIndex;
            RentPayment = null;
            Deadline = null;
            Phase = TurnPhase.AwaitingRoll;
        }
    }
}
