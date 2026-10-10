using System;

namespace Ropoly.Core.Match
{
    /// <summary>
    /// Provider-neutral player state. It contains no Unity or transport-specific values.
    /// </summary>
    [Serializable]
    public sealed class PlayerState
    {
        public PlayerState(string playerId, string displayName, int cash)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            Cash = cash;
            BoardPosition = 0;
            ConnectionStatus = PlayerConnectionStatus.Connected;
        }

        public string PlayerId { get; private set; }

        public string DisplayName { get; private set; }

        public string CreatureId { get; private set; }

        public int Cash { get; private set; }

        public int BoardPosition { get; private set; }

        public PlayerConnectionStatus ConnectionStatus { get; private set; }

        public bool IsReady => !string.IsNullOrWhiteSpace(CreatureId);

        internal void SelectCreature(string creatureId)
        {
            CreatureId = creatureId;
        }

        internal void MoveTo(int boardPosition)
        {
            BoardPosition = boardPosition;
        }

        internal void AddCash(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Cash = checked(Cash + amount);
        }

        internal bool TrySpendCash(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (Cash < amount)
            {
                return false;
            }

            Cash -= amount;
            return true;
        }
    }
}
