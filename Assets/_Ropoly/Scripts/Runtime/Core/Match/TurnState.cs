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
        public bool HasPendingPropertyDecision =>
            PendingPropertyTileIndex != PropertyState.UnownedPlayerIndex;

        internal void BeginPropertyDecision(int tileIndex)
        {
            PendingPropertyTileIndex = tileIndex;
            Phase = TurnPhase.AwaitingPropertyDecision;
        }

        internal void CompleteLanding()
        {
            PendingPropertyTileIndex = PropertyState.UnownedPlayerIndex;
            Phase = TurnPhase.AwaitingTurnEnd;
        }

        internal void Advance(int playerCount)
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % playerCount;
            TurnNumber++;
            LastRoll = null;
            PendingPropertyTileIndex = PropertyState.UnownedPlayerIndex;
            Phase = TurnPhase.AwaitingRoll;
        }
    }
}
