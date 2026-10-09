using System;
using System.Collections.Generic;
using Ropoly.Core.Match;
using Ropoly.Infrastructure.Content;
using Ropoly.Infrastructure.Content.Players;
using Ropoly.Presentation.Board;
using Ropoly.Presentation.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ropoly.Presentation.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LocalLobbyController : MonoBehaviour
    {
        private static readonly Vector3[] SpawnOffsets =
        {
            new Vector3(-0.34f, 0f, 0.34f),
            new Vector3(0.34f, 0f, 0.34f),
            new Vector3(-0.34f, 0f, -0.34f),
            new Vector3(0.34f, 0f, -0.34f),
        };

        [Header("Content")]
        [SerializeField]
        private GameRulesetDefinition _ruleset;

        [SerializeField]
        private CreatureRosterDefinition _creatureRoster;

        [Header("World")]
        [SerializeField]
        private BoardPreviewController _boardPreview;

        [SerializeField]
        private Transform _spawnedTokenRoot;

        [Header("Lobby interface")]
        [SerializeField]
        private GameObject _lobbyOverlay;

        [SerializeField]
        private TMP_Text _selectionPrompt;

        [SerializeField]
        private TMP_Text _readyStatus;

        [SerializeField]
        private Button _startButton;

        [SerializeField]
        private TMP_Text _startButtonLabel;

        [SerializeField]
        private LocalPlayerSlotView[] _playerSlots;

        [SerializeField]
        private CreatureChoiceView[] _creatureChoices;

        [SerializeField]
        private PlayerCountOptionView[] _playerCountOptions;

        [Header("In-match interface")]
        [SerializeField]
        private GameObject _matchHudPanel;

        [SerializeField]
        private PlayerHudRowView[] _hudRows;

        private LocalMatchSession _session;
        private int _activePlayerIndex;

        public LocalMatchSession Session => _session;
        public int ActivePlayerIndex => _activePlayerIndex;
        public GameObject LobbyOverlay => _lobbyOverlay;
        public GameObject MatchHudPanel => _matchHudPanel;
        public IReadOnlyList<CreatureChoiceView> CreatureChoices => _creatureChoices;

        public event Action<LocalMatchSession> MatchStarted;

        private void Awake()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            List<string> creatureIds = new List<string>(_creatureRoster.Creatures.Count);
            foreach (CreatureDefinition creature in _creatureRoster.Creatures)
            {
                creatureIds.Add(creature.CreatureId);
            }

            var rules = _ruleset.CreateSnapshot();
            _session = new LocalMatchSession(rules.PlayerCount, rules.StartingCash, creatureIds);
            BindInterface();
            _lobbyOverlay.SetActive(true);
            _matchHudPanel.SetActive(false);
            ClearSpawnedTokens();
            RefreshInterface();
        }

        public void SelectPlayer(int playerIndex)
        {
            if (_session == null ||
                _session.State.Phase != MatchPhase.Lobby ||
                playerIndex < 0 ||
                playerIndex >= _session.State.Players.Count)
            {
                return;
            }

            _activePlayerIndex = playerIndex;
            RefreshInterface();
        }

        public void SelectCreature(CreatureDefinition creature)
        {
            if (_session == null || creature == null)
            {
                return;
            }

            CreatureSelectionResult result = _session.TrySelectCreature(
                _activePlayerIndex,
                creature.CreatureId);
            if (result != CreatureSelectionResult.Success)
            {
                RefreshInterface();
                return;
            }

            int nextUnready = FindNextUnreadyPlayer(_activePlayerIndex);
            if (nextUnready >= 0)
            {
                _activePlayerIndex = nextUnready;
            }

            RefreshInterface();
        }

        public void SetPlayerCount(int playerCount)
        {
            if (_session == null || !_session.TrySetPlayerCount(playerCount))
            {
                return;
            }

            _activePlayerIndex = Mathf.Clamp(
                _activePlayerIndex,
                0,
                _session.State.Players.Count - 1);
            RefreshInterface();
        }

        public void StartMatch()
        {
            if (_session == null || !_session.TryStartMatch())
            {
                RefreshInterface();
                return;
            }

            SpawnSelectedCreatures();
            RefreshMatchHud();
            _lobbyOverlay.SetActive(false);
            _matchHudPanel.SetActive(true);
            _boardPreview.SetStatusText("MATCH READY  •  ROLL THE DICE");
            MatchStarted?.Invoke(_session);
        }

        private bool ValidateConfiguration()
        {
            bool valid =
                _ruleset != null &&
                _ruleset.Validate().IsValid &&
                _creatureRoster != null &&
                _creatureRoster.IsValid &&
                _boardPreview != null &&
                _spawnedTokenRoot != null &&
                _lobbyOverlay != null &&
                _selectionPrompt != null &&
                _readyStatus != null &&
                _startButton != null &&
                _startButtonLabel != null &&
                _matchHudPanel != null &&
                _playerSlots != null &&
                _playerSlots.Length == LocalMatchSession.MaximumPlayers &&
                _creatureChoices != null &&
                _creatureChoices.Length == _creatureRoster.Creatures.Count &&
                _playerCountOptions != null &&
                _playerCountOptions.Length == 3 &&
                _hudRows != null &&
                _hudRows.Length == LocalMatchSession.MaximumPlayers;

            if (!valid)
            {
                Debug.LogError("Local lobby configuration is incomplete or invalid.", this);
            }

            return valid;
        }

        private void BindInterface()
        {
            for (int index = 0; index < _playerSlots.Length; index++)
            {
                _playerSlots[index].Bind(index, SelectPlayer);
            }

            for (int index = 0; index < _creatureChoices.Length; index++)
            {
                _creatureChoices[index].Bind(_creatureRoster.Creatures[index], SelectCreature);
            }

            foreach (PlayerCountOptionView option in _playerCountOptions)
            {
                option.Bind(SetPlayerCount);
            }

            _startButton.onClick.RemoveAllListeners();
            _startButton.onClick.AddListener(StartMatch);
        }

        private void RefreshInterface()
        {
            if (_session == null)
            {
                return;
            }

            IReadOnlyList<PlayerState> players = _session.State.Players;
            for (int index = 0; index < _playerSlots.Length; index++)
            {
                bool included = index < players.Count;
                PlayerState player = included ? players[index] : null;
                CreatureDefinition creature = included ? FindCreature(player.CreatureId) : null;
                _playerSlots[index].Refresh(included, index == _activePlayerIndex, player, creature);
            }

            string activeCreatureId = players[_activePlayerIndex].CreatureId;
            foreach (CreatureChoiceView choice in _creatureChoices)
            {
                string creatureId = choice.Definition.CreatureId;
                bool selectedByActive = string.Equals(
                    activeCreatureId,
                    creatureId,
                    StringComparison.Ordinal);
                bool selectedByAnother = IsCreatureSelectedByAnotherPlayer(
                    creatureId,
                    _activePlayerIndex);
                choice.Refresh(selectedByActive, selectedByAnother);
            }

            foreach (PlayerCountOptionView option in _playerCountOptions)
            {
                option.Refresh(option.PlayerCount == players.Count);
            }

            int readyCount = CountReadyPlayers();
            _selectionPrompt.text = $"{players[_activePlayerIndex].DisplayName}  •  CHOOSE YOUR CREATURE";
            _readyStatus.text = $"{readyCount} / {players.Count} PLAYERS READY";
            _startButton.interactable = _session.CanStart;
            _startButtonLabel.text = _session.CanStart
                ? "START MATCH"
                : $"SELECT {players.Count - readyCount} MORE";
        }

        private void RefreshMatchHud()
        {
            IReadOnlyList<PlayerState> players = _session.State.Players;
            for (int index = 0; index < _hudRows.Length; index++)
            {
                bool included = index < players.Count;
                _hudRows[index].gameObject.SetActive(included);
                if (!included)
                {
                    continue;
                }

                CreatureDefinition creature = FindCreature(players[index].CreatureId);
                _hudRows[index].Refresh(players[index], creature);
            }
        }

        private void SpawnSelectedCreatures()
        {
            ClearSpawnedTokens();
            BoardPreviewTileView startTile = FindStartTile();
            if (startTile == null)
            {
                Debug.LogError("The board has no generated start tile for player spawning.", this);
                return;
            }

            Vector3 basePosition = startTile.transform.position + new Vector3(0f, 0.42f, 0f);
            IReadOnlyList<PlayerState> players = _session.State.Players;
            for (int index = 0; index < players.Count; index++)
            {
                CreatureDefinition creature = FindCreature(players[index].CreatureId);
                GameObject tokenObject = Instantiate(
                    creature.TokenPrefab,
                    basePosition + SpawnOffsets[index],
                    Quaternion.identity,
                    _spawnedTokenRoot);
                CreatureTokenView token = tokenObject.GetComponent<CreatureTokenView>();
                if (token == null)
                {
                    Debug.LogError($"Creature prefab '{creature.TokenPrefab.name}' has no token view.", creature);
                    Destroy(tokenObject);
                    continue;
                }

                token.Configure(creature, index);
            }
        }

        private BoardPreviewTileView FindStartTile()
        {
            BoardPreviewTileView[] tiles = FindObjectsByType<BoardPreviewTileView>(
                FindObjectsSortMode.None);
            foreach (BoardPreviewTileView tile in tiles)
            {
                if (tile.Index == 0)
                {
                    return tile;
                }
            }

            return null;
        }

        private CreatureDefinition FindCreature(string creatureId)
        {
            if (string.IsNullOrWhiteSpace(creatureId))
            {
                return null;
            }

            _creatureRoster.TryGetCreature(creatureId, out CreatureDefinition creature);
            return creature;
        }

        private bool IsCreatureSelectedByAnotherPlayer(string creatureId, int activePlayerIndex)
        {
            for (int index = 0; index < _session.State.Players.Count; index++)
            {
                if (index != activePlayerIndex &&
                    string.Equals(
                        _session.State.Players[index].CreatureId,
                        creatureId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private int FindNextUnreadyPlayer(int currentIndex)
        {
            int count = _session.State.Players.Count;
            for (int offset = 1; offset <= count; offset++)
            {
                int candidate = (currentIndex + offset) % count;
                if (!_session.State.Players[candidate].IsReady)
                {
                    return candidate;
                }
            }

            return -1;
        }

        private int CountReadyPlayers()
        {
            int ready = 0;
            foreach (PlayerState player in _session.State.Players)
            {
                if (player.IsReady)
                {
                    ready++;
                }
            }

            return ready;
        }

        private void ClearSpawnedTokens()
        {
            for (int index = _spawnedTokenRoot.childCount - 1; index >= 0; index--)
            {
                GameObject token = _spawnedTokenRoot.GetChild(index).gameObject;
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(token);
                }
                else
                {
                    DestroyImmediate(token);
                }
            }
        }
    }
}
