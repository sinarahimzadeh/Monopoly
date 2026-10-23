using System;
using System.Collections.Generic;
using Ropoly.Core.Match;
using Ropoly.Core.Rules;
using Ropoly.Infrastructure.Content;
using Ropoly.Infrastructure.Content.Players;
using Ropoly.Presentation.Board;
using Ropoly.Presentation.Players;
using Ropoly.Presentation.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ropoly.Presentation.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LocalLobbyController : MonoBehaviour
    {
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
        private readonly CreatureTokenView[] _spawnedTokens =
            new CreatureTokenView[LocalMatchSession.MaximumPlayers];

        private GameRulesetSnapshot _activeRules;
        private Button _rulesButton;
        private GameSettingsDialogView _settingsDialog;

        public GameRulesetSnapshot ActiveRules
        {
            get
            {
                if (_activeRules == null)
                {
                    _activeRules = GameRulesetData.LoadFromPlayerPrefs(
                        _ruleset != null ? _ruleset.CreateSnapshot() : null);
                }
                return _activeRules;
            }
            set
            {
                _activeRules = value;
                if (_activeRules != null)
                {
                    GameRulesetData.SaveToPlayerPrefs(_activeRules);
                }
            }
        }

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

            var rules = ActiveRules;
            var board = _boardPreview.BoardDefinition.CreateSnapshot();
            _session = new LocalMatchSession(
                rules.PlayerCount,
                rules.StartingCash,
                creatureIds,
                board,
                rules.PassStartCash,
                rules.MortgageEnabled,
                rules.TurnDurationSeconds,
                rules.DisconnectGraceSeconds,
                clock: null,
                rules.FullSetRentMultiplier,
                rules.VacationRewardEnabled,
                rules.VacationReward,
                cardDecks: null,
                rules.AuctionsEnabled,
                rules.AuctionBidTimeoutSeconds);
            BindInterface();
            EnsureRulesButton();
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

        /// <summary>
        /// Builds the same match used by local play from an authoritative online lobby snapshot.
        /// The multiplayer layer supplies the ordered creature ids; all rules and visuals remain
        /// owned by this scene controller.
        /// </summary>
        public bool StartOnlineMatch(
            IReadOnlyList<string> creatureIds,
            GameRulesetSnapshot authoritativeRules = null)
        {
            if (creatureIds == null ||
                creatureIds.Count < LocalMatchSession.MinimumPlayers ||
                creatureIds.Count > LocalMatchSession.MaximumPlayers)
            {
                return false;
            }

            if (authoritativeRules != null)
            {
                _activeRules = authoritativeRules;
            }

            var rules = ActiveRules;
            var board = _boardPreview.BoardDefinition.CreateSnapshot();
            List<string> rosterIds = new List<string>(_creatureRoster.Creatures.Count);
            foreach (CreatureDefinition creature in _creatureRoster.Creatures)
            {
                rosterIds.Add(creature.CreatureId);
            }

            _session = new LocalMatchSession(
                creatureIds.Count,
                rules.StartingCash,
                rosterIds,
                board,
                rules.PassStartCash,
                rules.MortgageEnabled,
                rules.TurnDurationSeconds,
                rules.DisconnectGraceSeconds,
                clock: null,
                rules.FullSetRentMultiplier,
                rules.VacationRewardEnabled,
                rules.VacationReward,
                cardDecks: null,
                rules.AuctionsEnabled,
                rules.AuctionBidTimeoutSeconds);
            BindInterface();

            for (int index = 0; index < creatureIds.Count; index++)
            {
                if (_session.TrySelectCreature(index, creatureIds[index]) !=
                    CreatureSelectionResult.Success)
                {
                    Debug.LogError($"Could not apply online creature selection for player {index + 1}.", this);
                    return false;
                }
            }

            _activePlayerIndex = 0;
            StartMatch();
            return _session.State.Phase == MatchPhase.InProgress;
        }

        private void EnsureRulesButton()
        {
            if (_rulesButton != null || _startButton == null)
            {
                return;
            }

            GameObject rulesObj = Instantiate(_startButton.gameObject, _startButton.transform.parent);
            rulesObj.name = "RulesSettingsButton";
            _rulesButton = rulesObj.GetComponent<Button>();
            _rulesButton.onClick.RemoveAllListeners();
            _rulesButton.onClick.AddListener(OpenSettingsDialog);

            TMP_Text label = rulesObj.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "⚙ RULES";
            }

            RectTransform startRect = _startButton.transform as RectTransform;
            RectTransform rulesRect = rulesObj.transform as RectTransform;
            if (startRect != null && rulesRect != null)
            {
                rulesRect.sizeDelta = new Vector2(160f, startRect.sizeDelta.y);
                rulesRect.anchoredPosition = new Vector2(
                    startRect.anchoredPosition.x,
                    startRect.anchoredPosition.y + startRect.sizeDelta.y + 12f);
            }
        }

        public void OpenSettingsDialog()
        {
            if (_settingsDialog == null)
            {
                _settingsDialog = GameSettingsDialogView.Install(_lobbyOverlay.transform.parent);
            }

            _settingsDialog.Open(ActiveRules, isReadOnly: false, onSave: updatedRules =>
            {
                ActiveRules = updatedRules;
                RebuildLobbySession(updatedRules);
            });
        }

        private void RebuildLobbySession(GameRulesetSnapshot rules)
        {
            if (_session == null)
            {
                return;
            }

            int currentCount = _session.State.Players.Count;
            string[] currentCreatures = new string[currentCount];
            for (int i = 0; i < currentCount; i++)
            {
                currentCreatures[i] = _session.State.Players[i].CreatureId;
            }

            var board = _boardPreview.BoardDefinition.CreateSnapshot();
            List<string> rosterIds = new List<string>(_creatureRoster.Creatures.Count);
            foreach (CreatureDefinition creature in _creatureRoster.Creatures)
            {
                rosterIds.Add(creature.CreatureId);
            }

            _session = new LocalMatchSession(
                currentCount,
                rules.StartingCash,
                rosterIds,
                board,
                rules.PassStartCash,
                rules.MortgageEnabled,
                rules.TurnDurationSeconds,
                rules.DisconnectGraceSeconds,
                clock: null,
                rules.FullSetRentMultiplier,
                rules.VacationRewardEnabled,
                rules.VacationReward,
                cardDecks: null,
                rules.AuctionsEnabled,
                rules.AuctionBidTimeoutSeconds);
            BindInterface();

            for (int i = 0; i < currentCount; i++)
            {
                if (!string.IsNullOrEmpty(currentCreatures[i]))
                {
                    _session.TrySelectCreature(i, currentCreatures[i]);
                }
            }

            RefreshInterface();
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

        public void RefreshMatchHud()
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
                _hudRows[index].BindClick(index, HandlePlayerRowClicked);
            }
        }

        private void HandlePlayerRowClicked(int targetPlayerIndex)
        {
            if (_session == null || _session.State.Phase != MatchPhase.InProgress)
            {
                return;
            }

            var dice = FindFirstObjectByType<Dice.DiceController>();
            if (dice != null && dice.TradeDialogView != null)
            {
                int localSeat = dice.GetCurrentLocalPlayerSeat();
                if (targetPlayerIndex != localSeat)
                {
                    dice.TradeDialogView.OpenTradeDialog(localSeat, targetPlayerIndex);
                }
            }
        }

        private void SpawnSelectedCreatures()
        {
            ClearSpawnedTokens();
            if (!_boardPreview.TryGetTokenPosition(0, 0, out _))
            {
                Debug.LogError("The board has no generated start tile for player spawning.", this);
                return;
            }

            IReadOnlyList<PlayerState> players = _session.State.Players;
            for (int index = 0; index < players.Count; index++)
            {
                CreatureDefinition creature = FindCreature(players[index].CreatureId);
                _boardPreview.TryGetTokenPosition(0, index, out Vector3 spawnPosition);
                GameObject tokenObject = Instantiate(
                    creature.TokenPrefab,
                    spawnPosition,
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
                _spawnedTokens[index] = token;
            }
        }

        public CreatureTokenView GetToken(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _spawnedTokens.Length)
            {
                return null;
            }

            return _spawnedTokens[playerIndex];
        }

        public void HidePlayerToken(int playerIndex)
        {
            CreatureTokenView token = GetToken(playerIndex);
            if (token != null)
            {
                token.gameObject.SetActive(false);
            }
        }

        public CreatureDefinition GetPlayerCreature(int playerIndex)
        {
            if (_session == null ||
                playerIndex < 0 ||
                playerIndex >= _session.State.Players.Count)
            {
                return null;
            }

            return FindCreature(_session.State.Players[playerIndex].CreatureId);
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
            Array.Clear(_spawnedTokens, 0, _spawnedTokens.Length);
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

        public void Rematch()
        {
            if (_session == null)
            {
                return;
            }

            string[] chosenCreatures = new string[_session.State.Players.Count];
            for (int i = 0; i < _session.State.Players.Count; i++)
            {
                chosenCreatures[i] = _session.State.Players[i].CreatureId;
            }

            for (int i = 0; i < _boardPreview.TileCount; i++)
            {
                _boardPreview.ClearTileOwner(i);
                _boardPreview.SetTileUpgradeLevel(i, 0);
            }

            ClearSpawnedTokens();

            var rules = ActiveRules;
            var board = _boardPreview.BoardDefinition.CreateSnapshot();
            List<string> rosterIds = new List<string>(_creatureRoster.Creatures.Count);
            foreach (CreatureDefinition creature in _creatureRoster.Creatures)
            {
                rosterIds.Add(creature.CreatureId);
            }

            _session = new LocalMatchSession(
                chosenCreatures.Length,
                rules.StartingCash,
                rosterIds,
                board,
                rules.PassStartCash,
                rules.MortgageEnabled,
                rules.TurnDurationSeconds,
                rules.DisconnectGraceSeconds,
                clock: null,
                rules.FullSetRentMultiplier,
                rules.VacationRewardEnabled,
                rules.VacationReward,
                cardDecks: null,
                rules.AuctionsEnabled,
                rules.AuctionBidTimeoutSeconds);

            for (int i = 0; i < chosenCreatures.Length; i++)
            {
                if (!string.IsNullOrEmpty(chosenCreatures[i]))
                {
                    _session.TrySelectCreature(i, chosenCreatures[i]);
                }
            }

            _session.TryStartMatch();

            SpawnSelectedCreatures();
            RefreshMatchHud();
            _lobbyOverlay.SetActive(false);
            _matchHudPanel.SetActive(true);
            _boardPreview.SetStatusText("MATCH READY  •  ROLL THE DICE");
            MatchStarted?.Invoke(_session);
        }

        public void ReturnToLobby()
        {
            if (_session == null)
            {
                return;
            }

            for (int i = 0; i < _boardPreview.TileCount; i++)
            {
                _boardPreview.ClearTileOwner(i);
                _boardPreview.SetTileUpgradeLevel(i, 0);
            }

            ClearSpawnedTokens();

            var rules = ActiveRules;
            var board = _boardPreview.BoardDefinition.CreateSnapshot();
            List<string> rosterIds = new List<string>(_creatureRoster.Creatures.Count);
            foreach (CreatureDefinition creature in _creatureRoster.Creatures)
            {
                rosterIds.Add(creature.CreatureId);
            }

            _session = new LocalMatchSession(
                rules.PlayerCount,
                rules.StartingCash,
                rosterIds,
                board,
                rules.PassStartCash,
                rules.MortgageEnabled,
                rules.TurnDurationSeconds,
                rules.DisconnectGraceSeconds,
                clock: null,
                rules.FullSetRentMultiplier,
                rules.VacationRewardEnabled,
                rules.VacationReward,
                cardDecks: null,
                rules.AuctionsEnabled,
                rules.AuctionBidTimeoutSeconds);

            _lobbyOverlay.SetActive(true);
            _matchHudPanel.SetActive(false);
            _activePlayerIndex = 0;
            RefreshInterface();
        }
    }
}
