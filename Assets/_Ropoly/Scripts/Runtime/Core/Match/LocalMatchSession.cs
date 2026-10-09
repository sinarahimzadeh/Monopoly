using System;
using System.Collections.Generic;

namespace Ropoly.Core.Match
{
    /// <summary>
    /// Owns local lobby decisions. A future host/server can call the same operations.
    /// </summary>
    public sealed class LocalMatchSession
    {
        public const int MinimumPlayers = 2;
        public const int MaximumPlayers = 4;

        private readonly HashSet<string> _availableCreatureIds;
        private readonly int _startingCash;

        public LocalMatchSession(
            int playerCount,
            int startingCash,
            IEnumerable<string> availableCreatureIds)
        {
            if (playerCount < MinimumPlayers || playerCount > MaximumPlayers)
            {
                throw new ArgumentOutOfRangeException(nameof(playerCount));
            }

            if (startingCash < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingCash));
            }

            _availableCreatureIds = new HashSet<string>(
                availableCreatureIds ?? throw new ArgumentNullException(nameof(availableCreatureIds)),
                StringComparer.Ordinal);
            if (_availableCreatureIds.Count < MaximumPlayers)
            {
                throw new ArgumentException(
                    $"At least {MaximumPlayers} unique creatures are required.",
                    nameof(availableCreatureIds));
            }

            _startingCash = startingCash;
            State = new MatchState(CreateLocalMatchId(), CreatePlayers(playerCount));
        }

        public MatchState State { get; }

        public bool CanStart
        {
            get
            {
                if (State.Phase != MatchPhase.Lobby || State.Players.Count < MinimumPlayers)
                {
                    return false;
                }

                foreach (PlayerState player in State.Players)
                {
                    if (!player.IsReady)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool TrySetPlayerCount(int playerCount)
        {
            if (State.Phase != MatchPhase.Lobby ||
                playerCount < MinimumPlayers ||
                playerCount > MaximumPlayers)
            {
                return false;
            }

            List<PlayerState> players = State.MutablePlayers;
            while (players.Count > playerCount)
            {
                players.RemoveAt(players.Count - 1);
            }

            while (players.Count < playerCount)
            {
                players.Add(CreatePlayer(players.Count));
            }

            return true;
        }

        public CreatureSelectionResult TrySelectCreature(int playerIndex, string creatureId)
        {
            if (State.Phase != MatchPhase.Lobby)
            {
                return CreatureSelectionResult.MatchAlreadyStarted;
            }

            if (playerIndex < 0 || playerIndex >= State.Players.Count)
            {
                return CreatureSelectionResult.InvalidPlayerIndex;
            }

            if (string.IsNullOrWhiteSpace(creatureId) || !_availableCreatureIds.Contains(creatureId))
            {
                return CreatureSelectionResult.UnknownCreature;
            }

            for (int index = 0; index < State.Players.Count; index++)
            {
                if (index != playerIndex &&
                    string.Equals(State.Players[index].CreatureId, creatureId, StringComparison.Ordinal))
                {
                    return CreatureSelectionResult.CreatureAlreadySelected;
                }
            }

            State.MutablePlayers[playerIndex].SelectCreature(creatureId);
            return CreatureSelectionResult.Success;
        }

        public bool TryStartMatch()
        {
            if (!CanStart)
            {
                return false;
            }

            State.Phase = MatchPhase.InProgress;
            State.Turn = new TurnState(currentPlayerIndex: 0);
            return true;
        }

        public bool TryRollDice(IDiceRollSource rollSource, out DiceRoll roll)
        {
            roll = null;
            if (rollSource == null ||
                State.Phase != MatchPhase.InProgress ||
                State.Turn == null ||
                State.Turn.Phase != TurnPhase.AwaitingRoll)
            {
                return false;
            }

            int firstValue = rollSource.NextDieValue();
            int secondValue = rollSource.NextDieValue();
            if (!DiceRoll.IsDieValueValid(firstValue) ||
                !DiceRoll.IsDieValueValid(secondValue))
            {
                return false;
            }

            roll = new DiceRoll(firstValue, secondValue);
            State.Turn.LastRoll = roll;
            State.Turn.Phase = TurnPhase.AwaitingMovement;
            return true;
        }

        public bool TryAdvanceTurn()
        {
            if (State.Phase != MatchPhase.InProgress ||
                State.Turn == null ||
                State.Turn.Phase != TurnPhase.AwaitingMovement)
            {
                return false;
            }

            State.Turn.Advance(State.Players.Count);
            return true;
        }

        private List<PlayerState> CreatePlayers(int count)
        {
            List<PlayerState> players = new List<PlayerState>(count);
            for (int index = 0; index < count; index++)
            {
                players.Add(CreatePlayer(index));
            }

            return players;
        }

        private PlayerState CreatePlayer(int index)
        {
            int number = index + 1;
            return new PlayerState($"local-player-{number}", $"PLAYER {number}", _startingCash);
        }

        private static string CreateLocalMatchId()
        {
            return $"local-{Guid.NewGuid():N}";
        }
    }
}
