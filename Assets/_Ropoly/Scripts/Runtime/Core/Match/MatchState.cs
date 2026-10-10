using System;
using System.Collections.Generic;

namespace Ropoly.Core.Match
{
    /// <summary>
    /// Serializable authoritative state that can later be synchronized by a network host.
    /// </summary>
    [Serializable]
    public sealed class MatchState
    {
        private readonly List<PlayerState> _players;
        private readonly List<PropertyState> _properties;

        internal MatchState(
            string matchId,
            List<PlayerState> players,
            List<PropertyState> properties)
        {
            MatchId = matchId;
            _players = players;
            _properties = properties;
            Phase = MatchPhase.Lobby;
        }

        public string MatchId { get; }

        public MatchPhase Phase { get; internal set; }

        public TurnState Turn { get; internal set; }

        public IReadOnlyList<PlayerState> Players => _players;

        public IReadOnlyList<PropertyState> Properties => _properties;

        internal List<PlayerState> MutablePlayers => _players;
    }
}
