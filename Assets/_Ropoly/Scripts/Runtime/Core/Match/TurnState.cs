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
        }

        public int CurrentPlayerIndex { get; private set; }
        public int TurnNumber { get; private set; }
        public TurnPhase Phase { get; internal set; }
        public DiceRoll LastRoll { get; internal set; }

        internal void Advance(int playerCount)
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % playerCount;
            TurnNumber++;
            LastRoll = null;
            Phase = TurnPhase.AwaitingRoll;
        }
    }
}
