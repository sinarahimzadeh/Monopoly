using System;

namespace Ropoly.Core.Match
{
    [Serializable]
    public sealed class PlayerEliminationRecord
    {
        public int PlayerIndex { get; }
        public EliminationReason Reason { get; }
        public int TurnNumber { get; }
        public int CashAtElimination { get; }
        public int PropertiesCountAtElimination { get; }
        public int NetWorthAtElimination { get; }

        public PlayerEliminationRecord(
            int playerIndex,
            EliminationReason reason,
            int turnNumber,
            int cashAtElimination,
            int propertiesCountAtElimination,
            int netWorthAtElimination)
        {
            PlayerIndex = playerIndex;
            Reason = reason;
            TurnNumber = turnNumber;
            CashAtElimination = cashAtElimination;
            PropertiesCountAtElimination = propertiesCountAtElimination;
            NetWorthAtElimination = netWorthAtElimination;
        }
    }
}
