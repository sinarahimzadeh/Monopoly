using System;
using System.Collections.Generic;
using System.Linq;

namespace Ropoly.Core.Match
{
    [Serializable]
    public sealed class MatchResult
    {
        public int WinnerPlayerIndex { get; }
        public string WinnerDisplayName { get; }
        public string WinnerCreatureId { get; }
        public int TotalTurns { get; }
        public IReadOnlyList<PlayerStanding> Standings { get; }

        public MatchResult(
            int winnerPlayerIndex,
            string winnerDisplayName,
            string winnerCreatureId,
            int totalTurns,
            IReadOnlyList<PlayerStanding> standings)
        {
            WinnerPlayerIndex = winnerPlayerIndex;
            WinnerDisplayName = winnerDisplayName;
            WinnerCreatureId = winnerCreatureId;
            TotalTurns = totalTurns;
            Standings = standings != null
                ? new List<PlayerStanding>(standings).AsReadOnly()
                : Array.Empty<PlayerStanding>();
        }

        public PlayerStanding GetStandingForPlayer(int playerIndex)
        {
            return Standings.FirstOrDefault(s => s.PlayerIndex == playerIndex);
        }
    }
}
