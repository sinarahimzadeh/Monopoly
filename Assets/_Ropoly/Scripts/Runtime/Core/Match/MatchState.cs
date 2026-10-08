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

        internal MatchState(string matchId, List<PlayerState> players)
        {
            MatchId = matchId;
            _players = players;
            Phase = MatchPhase.Lobby;
        }

        public string MatchId { get; }

        public MatchPhase Phase { get; internal set; }

        public IReadOnlyList<PlayerState> Players => _players;

        internal List<PlayerState> MutablePlayers => _players;
    }
}
