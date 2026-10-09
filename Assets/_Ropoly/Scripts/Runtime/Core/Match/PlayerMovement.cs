using System;

namespace Ropoly.Core.Match
{
    /// <summary>
    /// Immutable authoritative result of moving one player around the board.
    /// Presentation code may animate this result but cannot change its outcome.
    /// </summary>
    [Serializable]
    public sealed class PlayerMovement
    {
        public PlayerMovement(
            int playerIndex,
            int fromIndex,
            int destinationIndex,
            int spaces,
            int completedLaps,
            int cashAward)
        {
            PlayerIndex = playerIndex;
            FromIndex = fromIndex;
            DestinationIndex = destinationIndex;
            Spaces = spaces;
            CompletedLaps = completedLaps;
            CashAward = cashAward;
        }

        public int PlayerIndex { get; }
        public int FromIndex { get; }
        public int DestinationIndex { get; }
        public int Spaces { get; }
        public int CompletedLaps { get; }
        public int CashAward { get; }
        public bool PassedStart => CompletedLaps > 0;
    }
}
