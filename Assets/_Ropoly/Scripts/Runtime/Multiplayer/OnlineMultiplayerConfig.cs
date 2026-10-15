using Ropoly.Infrastructure.Content.Players;
using UnityEngine;

namespace Ropoly.Multiplayer
{
    [CreateAssetMenu(
        fileName = "OnlineMultiplayerConfig",
        menuName = "Ropoly/Multiplayer/Online Multiplayer Config")]
    public sealed class OnlineMultiplayerConfig : ScriptableObject
    {
        [SerializeField]
        private CreatureRosterDefinition _creatureRoster;

        [SerializeField]
        [Range(2, 4)]
        private int _maximumPlayers = 4;

        [SerializeField]
        private string _sessionType = "ropoly-v1";

        [SerializeField]
        private string _roomName = "Ropoly Room";

        [SerializeField]
        private string _authenticationProfilePrefix = "ropoly";

        public CreatureRosterDefinition CreatureRoster => _creatureRoster;
        public int MaximumPlayers => Mathf.Clamp(_maximumPlayers, 2, 4);
        public string SessionType => string.IsNullOrWhiteSpace(_sessionType) ? "ropoly-v1" : _sessionType;
        public string RoomName => string.IsNullOrWhiteSpace(_roomName) ? "Ropoly Room" : _roomName;
        public string AuthenticationProfilePrefix =>
            string.IsNullOrWhiteSpace(_authenticationProfilePrefix)
                ? "ropoly"
                : _authenticationProfilePrefix;
    }
}
