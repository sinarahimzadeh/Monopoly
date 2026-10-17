using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Ropoly.Multiplayer.Protocol;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ropoly.Multiplayer
{
    [DisallowMultipleComponent]
    public sealed class OnlineGameService : MonoBehaviour
    {
        private const string ConfigResourcePath = "Multiplayer/OnlineMultiplayerConfig";
        private const string LobbyCommandMessage = "ropoly/lobby/command/v1";
        private const string LobbyStateMessage = "ropoly/lobby/state/v1";
        private const string MatchCommandMessage = "ropoly/match/command/v1";
        private const string MatchEventMessage = "ropoly/match/event/v1";

        private readonly List<OnlineLobbyPlayerData> _hostPlayers = new();
        private readonly List<PendingMatchCommand> _pendingMatchCommands = new();
        private NetworkManager _networkManager;
        private UnityTransport _transport;
        private OnlineMultiplayerConfig _config;
        private ISession _session;
        private OnlineLobbyStateData _lobbyState = new();
        private bool _messageHandlersRegistered;
        private bool _leaving;

        public static OnlineGameService Instance { get; private set; }

        public OnlineConnectionState State { get; private set; } = OnlineConnectionState.Offline;
        public string StatusMessage { get; private set; } = "OFFLINE";
        public string LastError { get; private set; } = string.Empty;
        public bool IsOnlineSession => _session != null;
        public bool IsHost => _session?.IsHost == true;
        public string JoinCode => _session?.Code ?? string.Empty;
        public OnlineLobbyStateData LobbyState => _lobbyState;
        public OnlineMultiplayerConfig Config => _config;
        public int LocalSeatIndex { get; private set; } = -1;

        public event Action<OnlineConnectionState, string> ConnectionStateChanged;
        public event Action<OnlineLobbyStateData> LobbyStateChanged;
        public event Action<ulong, OnlineMatchCommandData> MatchCommandReceived;
        public event Action<OnlineMatchEventData> MatchEventReceived;
        public event Action<ulong> OnlineClientDisconnected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateService()
        {
            if (Instance != null)
            {
                return;
            }

            var serviceObject = new GameObject("Ropoly Online Services");
            serviceObject.AddComponent<OnlineGameService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _config = Resources.Load<OnlineMultiplayerConfig>(ConfigResourcePath);
            if (_config == null)
            {
                Debug.LogError($"Missing multiplayer config at Resources/{ConfigResourcePath}.", this);
            }

            _transport = gameObject.AddComponent<UnityTransport>();
            _networkManager = gameObject.AddComponent<NetworkManager>();
            // A NetworkManager added at runtime does not receive the serialized
            // NetworkConfig that Unity creates for a scene component. Build it
            // explicitly before touching any of its settings.
            _networkManager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = _transport,
                EnableSceneManagement = false,
                ConnectionApproval = false,
                ProtocolVersion = 1
            };
            _networkManager.OnClientConnectedCallback += HandleClientConnected;
            _networkManager.OnClientDisconnectCallback += HandleClientDisconnected;

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (_networkManager != null)
            {
                _networkManager.OnClientConnectedCallback -= HandleClientConnected;
                _networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }

            UnregisterMessageHandlers();
            Instance = null;
        }

        private async void OnApplicationQuit()
        {
            if (_session != null)
            {
                try
                {
                    await _session.LeaveAsync();
                }
                catch
                {
                    // The process is already shutting down.
                }
            }
        }

        public async Task CreateRoomAsync()
        {
            if (!CanBeginConnection())
            {
                return;
            }

            try
            {
                await EnsureServicesReadyAsync();
                SetState(OnlineConnectionState.CreatingRoom, "CREATING ROOM...");
                PrepareNetworkManager();

                var options = new SessionOptions
                {
                    Type = _config.SessionType,
                    Name = _config.RoomName,
                    MaxPlayers = _config.MaximumPlayers,
                    IsPrivate = true,
                    IsLocked = false
                }.WithRelayNetwork();

                _session = await MultiplayerService.Instance.CreateSessionAsync(options);
                RegisterMessageHandlers();
                EnsureHostPlayer();
                SetState(OnlineConnectionState.InLobby, "ROOM READY");
                BroadcastLobbyState();
            }
            catch (Exception exception)
            {
                await FailConnectionAsync("COULD NOT CREATE ROOM", exception);
            }
        }

        public async Task JoinRoomAsync(string joinCode)
        {
            if (!CanBeginConnection())
            {
                return;
            }

            string normalizedCode = (joinCode ?? string.Empty).Trim().ToUpperInvariant();
            if (normalizedCode.Length == 0)
            {
                SetFailure("ENTER A ROOM CODE");
                return;
            }

            try
            {
                await EnsureServicesReadyAsync();
                SetState(OnlineConnectionState.JoiningRoom, "JOINING ROOM...");
                PrepareNetworkManager();
                var options = new JoinSessionOptions { Type = _config.SessionType };
                _session = await MultiplayerService.Instance.JoinSessionByCodeAsync(normalizedCode, options);
                RegisterMessageHandlers();
                SetState(OnlineConnectionState.InLobby, "CONNECTED");
                SendLobbyCommand(new OnlineLobbyCommandData
                {
                    Kind = OnlineLobbyCommandKind.RequestState
                });
            }
            catch (Exception exception)
            {
                await FailConnectionAsync("COULD NOT JOIN ROOM", exception);
            }
        }

        public async Task LeaveRoomAsync()
        {
            if (_leaving)
            {
                return;
            }

            _leaving = true;
            SetState(OnlineConnectionState.Leaving, "LEAVING ROOM...");
            try
            {
                if (_session != null)
                {
                    await _session.LeaveAsync();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Leaving online room failed: {exception.Message}", this);
            }
            finally
            {
                ResetOnlineState();
                _leaving = false;
                SetState(OnlineConnectionState.Offline, "OFFLINE");
            }
        }

        public void SelectCreature(string creatureId)
        {
            SendLobbyCommand(new OnlineLobbyCommandData
            {
                Kind = OnlineLobbyCommandKind.SelectCreature,
                StringValue = creatureId ?? string.Empty
            });
        }

        public void SetReady(bool ready)
        {
            SendLobbyCommand(new OnlineLobbyCommandData
            {
                Kind = OnlineLobbyCommandKind.SetReady,
                BoolValue = ready
            });
        }

        public void RequestStartMatch()
        {
            SendLobbyCommand(new OnlineLobbyCommandData
            {
                Kind = OnlineLobbyCommandKind.StartMatch
            });
        }

        public void SendMatchCommand(OnlineMatchCommandKind commandKind, int intValue = -1, string stringValue = null)
        {
            var command = new OnlineMatchCommandData
            {
                Kind = commandKind,
                IntValue = intValue,
                StringValue = stringValue
            };
            if (IsHost)
            {
                DispatchMatchCommand(_networkManager.LocalClientId, command);
                return;
            }

            SendMessage(MatchCommandMessage, NetworkManager.ServerClientId, command);
        }

        public void BroadcastMatchEvent(OnlineMatchEventData matchEvent)
        {
            if (!IsHost || matchEvent == null)
            {
                return;
            }

            foreach (ulong clientId in _networkManager.ConnectedClientsIds)
            {
                if (clientId != _networkManager.LocalClientId)
                {
                    SendMessage(MatchEventMessage, clientId, matchEvent);
                }
            }
        }

        public bool TryGetSeatIndex(ulong clientId, out int seatIndex)
        {
            string id = clientId.ToString();
            OnlineLobbyPlayerData player = _hostPlayers.FirstOrDefault(item => item.ClientId == id);
            seatIndex = player?.SeatIndex ?? -1;
            return seatIndex >= 0;
        }

        public void MarkMatchActive()
        {
            if (_session != null)
            {
                SetState(OnlineConnectionState.InMatch, "ONLINE MATCH");
            }
        }

        public void FlushPendingMatchCommands()
        {
            if (MatchCommandReceived == null || _pendingMatchCommands.Count == 0)
            {
                return;
            }

            PendingMatchCommand[] pending = _pendingMatchCommands.ToArray();
            _pendingMatchCommands.Clear();
            foreach (PendingMatchCommand item in pending)
            {
                MatchCommandReceived?.Invoke(item.SenderClientId, item.Command);
            }
        }

        private bool CanBeginConnection()
        {
            if (_config == null)
            {
                SetFailure("MULTIPLAYER CONFIGURATION IS MISSING");
                return false;
            }

            return State is OnlineConnectionState.Offline or OnlineConnectionState.Failed;
        }

        private async Task EnsureServicesReadyAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                SetState(OnlineConnectionState.Initializing, "INITIALIZING UNITY SERVICES...");
                var options = new InitializationOptions();
                options.SetProfile(BuildAuthenticationProfile());
                await UnityServices.InitializeAsync(options);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                SetState(OnlineConnectionState.Authenticating, "SIGNING IN...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        private string BuildAuthenticationProfile()
        {
            string commandLineProfile = ReadCommandLineValue("-ropolyProfile");
            if (!string.IsNullOrWhiteSpace(commandLineProfile))
            {
                return SanitizeProfile(commandLineProfile);
            }

            string identity = Path.GetFullPath(UnityEngine.Application.dataPath).ToLowerInvariant();
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(identity));
            string suffix = BitConverter.ToString(hash)
                .Replace("-", string.Empty)
                .Substring(0, 12)
                .ToLowerInvariant();
            int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            return SanitizeProfile(
                $"{_config.AuthenticationProfilePrefix}-{suffix}-{processId}");
        }

        private static string ReadCommandLineValue(string key)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], key, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }

            return string.Empty;
        }

        private static string SanitizeProfile(string value)
        {
            var builder = new StringBuilder(30);
            foreach (char character in value ?? string.Empty)
            {
                if (char.IsLetterOrDigit(character) || character is '-' or '_')
                {
                    builder.Append(character);
                    if (builder.Length == 30)
                    {
                        break;
                    }
                }
            }

            return builder.Length == 0 ? "ropoly-player" : builder.ToString();
        }

        private void PrepareNetworkManager()
        {
            if (_networkManager.IsListening)
            {
                _networkManager.Shutdown();
            }

            _networkManager.NetworkConfig.NetworkTransport = _transport;
            _networkManager.NetworkConfig.EnableSceneManagement = false;
            _networkManager.NetworkConfig.ProtocolVersion = 1;
        }

        private void RegisterMessageHandlers()
        {
            if (_messageHandlersRegistered || _networkManager.CustomMessagingManager == null)
            {
                return;
            }

            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(
                LobbyCommandMessage,
                ReceiveLobbyCommand);
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(
                LobbyStateMessage,
                ReceiveLobbyState);
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(
                MatchCommandMessage,
                ReceiveMatchCommand);
            _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(
                MatchEventMessage,
                ReceiveMatchEvent);
            _messageHandlersRegistered = true;
        }

        private void UnregisterMessageHandlers()
        {
            if (!_messageHandlersRegistered || _networkManager?.CustomMessagingManager == null)
            {
                return;
            }

            _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(LobbyCommandMessage);
            _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(LobbyStateMessage);
            _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(MatchCommandMessage);
            _networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(MatchEventMessage);
            _messageHandlersRegistered = false;
        }

        private void HandleClientConnected(ulong clientId)
        {
            RegisterMessageHandlers();
            if (!IsHost)
            {
                return;
            }

            AddHostPlayer(clientId);
            BroadcastLobbyState();
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!IsHost)
            {
                return;
            }

            if (State == OnlineConnectionState.InMatch)
            {
                OnlineClientDisconnected?.Invoke(clientId);
                return;
            }

            _hostPlayers.RemoveAll(player => player.ClientId == clientId.ToString());
            ReindexHostPlayers();
            BroadcastLobbyState();
        }

        private void EnsureHostPlayer()
        {
            AddHostPlayer(_networkManager.LocalClientId);
        }

        private void AddHostPlayer(ulong clientId)
        {
            string id = clientId.ToString();
            if (_hostPlayers.Any(player => player.ClientId == id) ||
                _hostPlayers.Count >= _config.MaximumPlayers)
            {
                return;
            }

            _hostPlayers.Add(new OnlineLobbyPlayerData
            {
                ClientId = id,
                SeatIndex = _hostPlayers.Count,
                DisplayName = $"PLAYER {_hostPlayers.Count + 1}",
                CreatureId = string.Empty,
                IsReady = false,
                IsHost = clientId == NetworkManager.ServerClientId
            });
        }

        private void ReindexHostPlayers()
        {
            for (int index = 0; index < _hostPlayers.Count; index++)
            {
                _hostPlayers[index].SeatIndex = index;
                _hostPlayers[index].DisplayName = $"PLAYER {index + 1}";
            }
        }

        private void ReceiveLobbyCommand(ulong senderClientId, FastBufferReader reader)
        {
            if (!IsHost || !TryReadJson(reader, out OnlineLobbyCommandData command))
            {
                return;
            }

            ApplyLobbyCommand(senderClientId, command);
        }

        private void SendLobbyCommand(OnlineLobbyCommandData command)
        {
            if (_session == null || command == null)
            {
                return;
            }

            if (IsHost)
            {
                ApplyLobbyCommand(_networkManager.LocalClientId, command);
            }
            else
            {
                SendMessage(LobbyCommandMessage, NetworkManager.ServerClientId, command);
            }
        }

        private async void ApplyLobbyCommand(ulong senderClientId, OnlineLobbyCommandData command)
        {
            if (command.Kind == OnlineLobbyCommandKind.RequestState)
            {
                AddHostPlayer(senderClientId);
                BroadcastLobbyState();
                return;
            }

            OnlineLobbyPlayerData player = _hostPlayers.FirstOrDefault(
                item => item.ClientId == senderClientId.ToString());
            if (player == null || _lobbyState.MatchStarted)
            {
                return;
            }

            switch (command.Kind)
            {
                case OnlineLobbyCommandKind.SelectCreature:
                    if (IsValidCreature(command.StringValue) &&
                        !_hostPlayers.Any(item => item != player && item.CreatureId == command.StringValue))
                    {
                        player.CreatureId = command.StringValue;
                        player.IsReady = false;
                    }
                    break;

                case OnlineLobbyCommandKind.SetReady:
                    if (!string.IsNullOrWhiteSpace(player.CreatureId))
                    {
                        player.IsReady = command.BoolValue;
                    }
                    break;

                case OnlineLobbyCommandKind.StartMatch:
                    if (senderClientId == NetworkManager.ServerClientId && CanStartMatch())
                    {
                        _lobbyState.MatchStarted = true;
                        SetState(OnlineConnectionState.StartingMatch, "STARTING MATCH...");
                        if (_session is IHostSession hostSession)
                        {
                            hostSession.IsLocked = true;
                            try
                            {
                                await hostSession.SavePropertiesAsync();
                            }
                            catch (Exception exception)
                            {
                                Debug.LogWarning($"Could not lock online room: {exception.Message}", this);
                            }
                        }

                        BroadcastLobbyState();
                        LoadOnlineGameScene();
                        return;
                    }
                    break;
            }

            BroadcastLobbyState();
        }

        private bool IsValidCreature(string creatureId)
        {
            return _config.CreatureRoster != null &&
                   _config.CreatureRoster.Creatures.Any(
                       creature => string.Equals(
                           creature.CreatureId,
                           creatureId,
                           StringComparison.Ordinal));
        }

        private bool CanStartMatch()
        {
            return _hostPlayers.Count >= 2 &&
                   _hostPlayers.All(player =>
                       player.IsReady && !string.IsNullOrWhiteSpace(player.CreatureId));
        }

        private void BroadcastLobbyState()
        {
            if (!IsHost)
            {
                return;
            }

            _lobbyState.JoinCode = JoinCode;
            _lobbyState.Players = _hostPlayers
                .OrderBy(player => player.SeatIndex)
                .Select(ClonePlayer)
                .ToArray();
            ApplyLobbyState(_lobbyState);

            foreach (ulong clientId in _networkManager.ConnectedClientsIds)
            {
                if (clientId != _networkManager.LocalClientId)
                {
                    SendMessage(LobbyStateMessage, clientId, _lobbyState);
                }
            }
        }

        private static OnlineLobbyPlayerData ClonePlayer(OnlineLobbyPlayerData player)
        {
            return new OnlineLobbyPlayerData
            {
                ClientId = player.ClientId,
                SeatIndex = player.SeatIndex,
                DisplayName = player.DisplayName,
                CreatureId = player.CreatureId,
                IsReady = player.IsReady,
                IsHost = player.IsHost
            };
        }

        private void ReceiveLobbyState(ulong senderClientId, FastBufferReader reader)
        {
            if (senderClientId != NetworkManager.ServerClientId ||
                !TryReadJson(reader, out OnlineLobbyStateData state))
            {
                return;
            }

            ApplyLobbyState(state);
            if (state.MatchStarted && SceneManager.GetActiveScene().name != "Game")
            {
                LoadOnlineGameScene();
            }
        }

        private void ApplyLobbyState(OnlineLobbyStateData state)
        {
            _lobbyState = state ?? new OnlineLobbyStateData();
            string localClientId = _networkManager.LocalClientId.ToString();
            OnlineLobbyPlayerData localPlayer = _lobbyState.Players?.FirstOrDefault(
                player => player.ClientId == localClientId);
            LocalSeatIndex = localPlayer?.SeatIndex ?? -1;
            LobbyStateChanged?.Invoke(_lobbyState);
        }

        private void ReceiveMatchCommand(ulong senderClientId, FastBufferReader reader)
        {
            if (IsHost && TryReadJson(reader, out OnlineMatchCommandData command))
            {
                DispatchMatchCommand(senderClientId, command);
            }
        }

        private void DispatchMatchCommand(ulong senderClientId, OnlineMatchCommandData command)
        {
            if (MatchCommandReceived != null)
            {
                MatchCommandReceived.Invoke(senderClientId, command);
            }
            else
            {
                _pendingMatchCommands.Add(new PendingMatchCommand(senderClientId, command));
            }
        }

        private void ReceiveMatchEvent(ulong senderClientId, FastBufferReader reader)
        {
            if (senderClientId == NetworkManager.ServerClientId &&
                TryReadJson(reader, out OnlineMatchEventData matchEvent))
            {
                MatchEventReceived?.Invoke(matchEvent);
            }
        }

        private void SendMessage<T>(string messageName, ulong clientId, T payload)
        {
            if (!_networkManager.IsListening || payload == null)
            {
                return;
            }

            string json = JsonUtility.ToJson(payload);
            // WriteValueSafe(string) serializes UTF-16 characters by default.
            // UTF-8 byte counts under-allocate this buffer and make even a small
            // lobby command overflow before it can reach the host.
            int capacity = FastBufferWriter.GetWriteSize(json);
            using var writer = new FastBufferWriter(capacity, Allocator.Temp);
            writer.WriteValueSafe(json);
            _networkManager.CustomMessagingManager.SendNamedMessage(
                messageName,
                clientId,
                writer,
                NetworkDelivery.ReliableSequenced);
        }

        private static bool TryReadJson<T>(FastBufferReader reader, out T value)
        {
            value = default;
            try
            {
                reader.ReadValueSafe(out string json);
                value = JsonUtility.FromJson<T>(json);
                return value != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Rejected malformed multiplayer message: {exception.Message}");
                return false;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu")
            {
                if (State is OnlineConnectionState.StartingMatch or OnlineConnectionState.InMatch)
                {
                    _ = LeaveRoomAsync();
                }

                OnlineMenuPresenter.Install(this);
            }
            else if (scene.name == "Game" && IsOnlineSession)
            {
                OnlineGameSceneController.Install(this);
            }
        }

        private void LoadOnlineGameScene()
        {
            if (SceneManager.GetActiveScene().name == "Game")
            {
                return;
            }

            SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }

        private async Task FailConnectionAsync(string summary, Exception exception)
        {
            Debug.LogError($"{summary}: {exception}", this);
            LastError = $"{summary}\n{exception.Message}";
            if (_session != null)
            {
                try
                {
                    await _session.LeaveAsync();
                }
                catch
                {
                    // Preserve the original connection error.
                }
            }

            ResetOnlineState();
            SetState(OnlineConnectionState.Failed, LastError);
        }

        private void SetFailure(string message)
        {
            LastError = message;
            SetState(OnlineConnectionState.Failed, message);
        }

        private void ResetOnlineState()
        {
            UnregisterMessageHandlers();
            if (_networkManager != null && _networkManager.IsListening)
            {
                _networkManager.Shutdown();
            }

            _session = null;
            _hostPlayers.Clear();
            _pendingMatchCommands.Clear();
            _lobbyState = new OnlineLobbyStateData();
            LocalSeatIndex = -1;
            LastError = string.Empty;
        }

        private void SetState(OnlineConnectionState state, string message)
        {
            State = state;
            StatusMessage = message ?? string.Empty;
            ConnectionStateChanged?.Invoke(State, StatusMessage);
        }

        private readonly struct PendingMatchCommand
        {
            public PendingMatchCommand(ulong senderClientId, OnlineMatchCommandData command)
            {
                SenderClientId = senderClientId;
                Command = command;
            }

            public ulong SenderClientId { get; }
            public OnlineMatchCommandData Command { get; }
        }
    }
}
