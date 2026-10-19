using System;

namespace Ropoly.Core.Match
{
    [Serializable]
    public sealed class PlayerStanding
    {
        public int Rank { get; }
        public int PlayerIndex { get; }
        public string PlayerId { get; }
        public string DisplayName { get; }
        public string CreatureId { get; }
        public int Cash { get; }
        public int NetWorth { get; }
        public int PropertiesCount { get; }
        public bool IsWinner { get; }
        public EliminationReason? EliminationReason { get; }
        public int EliminationTurn { get; }

        public PlayerStanding(
            int rank,
            int playerIndex,
            string playerId,
            string displayName,
            string creatureId,
            int cash,
            int netWorth,
            int propertiesCount,
            bool isWinner,
            EliminationReason? eliminationReason = null,
            int eliminationTurn = 0)
        {
            Rank = rank;
            PlayerIndex = playerIndex;
            PlayerId = playerId;
            DisplayName = displayName;
            CreatureId = creatureId;
            Cash = cash;
            NetWorth = netWorth;
            PropertiesCount = propertiesCount;
            IsWinner = isWinner;
            EliminationReason = eliminationReason;
            EliminationTurn = eliminationTurn;
        }
    }
}
