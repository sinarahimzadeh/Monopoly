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

        public bool IsEliminated => ConnectionStatus == PlayerConnectionStatus.Eliminated;

        public bool IsConnected => ConnectionStatus == PlayerConnectionStatus.Connected;

        public bool IsDisconnected => ConnectionStatus == PlayerConnectionStatus.Disconnected;

        public DateTimeOffset? DisconnectDeadline { get; private set; }
        public bool IsInDetention { get; private set; }
        public int DetentionTurns { get; private set; }
        public int HeldDetentionWaivers { get; private set; }

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

        internal bool TryDisconnect(DateTimeOffset deadline)
        {
            if (IsEliminated)
            {
                return false;
            }

            ConnectionStatus = PlayerConnectionStatus.Disconnected;
            DisconnectDeadline = deadline;
            return true;
        }

        internal bool TryReconnect()
        {
            if (IsEliminated || ConnectionStatus == PlayerConnectionStatus.Connected)
            {
                return false;
            }

            ConnectionStatus = PlayerConnectionStatus.Connected;
            DisconnectDeadline = null;
            return true;
        }

        internal void SendToDetention(int detentionTileIndex = 10)
        {
            IsInDetention = true;
            DetentionTurns = 0;
            BoardPosition = detentionTileIndex;
        }

        internal void ReleaseFromDetention()
        {
            IsInDetention = false;
            DetentionTurns = 0;
        }

        internal void IncrementDetentionTurns()
        {
            DetentionTurns++;
        }

        internal void AddDetentionWaiver()
        {
            HeldDetentionWaivers++;
        }

        internal void AddDetentionWaivers(int count)
        {
            if (count > 0)
            {
                HeldDetentionWaivers += count;
            }
        }

        internal bool TryUseDetentionWaiver()
        {
            if (HeldDetentionWaivers <= 0)
            {
                return false;
            }

            HeldDetentionWaivers--;
            return true;
        }

        internal bool TryRemoveDetentionWaivers(int count)
        {
            if (count <= 0)
            {
                return true;
            }

            if (HeldDetentionWaivers < count)
            {
                return false;
            }

            HeldDetentionWaivers -= count;
            return true;
        }

        internal void Eliminate()
        {
            Cash = 0;
            ConnectionStatus = PlayerConnectionStatus.Eliminated;
            DisconnectDeadline = null;
            IsInDetention = false;
            DetentionTurns = 0;
            HeldDetentionWaivers = 0;
        }
    }
}
