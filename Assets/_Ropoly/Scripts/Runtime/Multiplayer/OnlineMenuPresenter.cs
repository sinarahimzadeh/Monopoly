using System;
using System.Linq;
using Ropoly.Infrastructure.Content.Players;
using Ropoly.Multiplayer.Protocol;
using Ropoly.Presentation.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ropoly.Multiplayer
{
    /// <summary>
    /// Adds the online entry and lobby to the shipped Main Menu. The view is deliberately
    /// runtime-built so the networking package remains isolated from the scene's local flow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OnlineMenuPresenter : MonoBehaviour
    {
        private static readonly Color Backdrop = new(0.018f, 0.012f, 0.055f, 0.97f);
        private static readonly Color Panel = new(0.075f, 0.045f, 0.14f, 1f);
        private static readonly Color PanelLight = new(0.12f, 0.08f, 0.22f, 1f);
        private static readonly Color Purple = new(0.31f, 0.18f, 0.63f, 1f);
        private static readonly Color Mint = new(0.36f, 0.92f, 0.76f, 1f);
        private static readonly Color Emerald = new(0.14f, 0.58f, 0.48f, 1f);
        private static readonly Color Pink = new(0.94f, 0.23f, 0.58f, 1f);

        private OnlineGameService _service;
        private TMP_FontAsset _font;
        private GameObject _overlay;
        private GameObject _connectPanel;
        private GameObject _lobbyPanel;
        private TMP_Text _statusText;
        private TMP_Text _roomCodeText;
        private TMP_Text _playersText;
        private TMP_InputField _joinCodeInput;
        private Button _autoJoinButton;
        private Button _createButton;
        private Button _joinButton;
        private Button _readyButton;
        private TMP_Text _readyButtonLabel;
        private Button _startButton;
        private TMP_Text _startButtonLabel;
        private Button[] _creatureButtons = Array.Empty<Button>();

        public static void Install(OnlineGameService service)
        {
            if (service == null || FindFirstObjectByType<OnlineMenuPresenter>() != null)
            {
                return;
            }

            GameObject canvasObject = GameObject.Find("MainMenuCanvas");
            GameObject playButtonObject = GameObject.Find("PlayButton");
            if (canvasObject == null || playButtonObject == null)
            {
                Debug.LogError("Could not install online play: MainMenuCanvas or PlayButton is missing.");
                return;
            }

            var presenterObject = new GameObject("Online Menu Presenter");
            presenterObject.transform.SetParent(canvasObject.transform, false);
            var presenter = presenterObject.AddComponent<OnlineMenuPresenter>();
            presenter.Initialize(service, canvasObject.transform, playButtonObject);
        }

        private void Initialize(
            OnlineGameService service,
            Transform canvasTransform,
            GameObject localPlayButtonObject)
        {
            _service = service;
            _font = localPlayButtonObject.GetComponentInChildren<TMP_Text>(true)?.font;

            RectTransform localRect = localPlayButtonObject.transform as RectTransform;
            if (localRect != null)
            {
                localRect.anchoredPosition = new Vector2(localRect.anchoredPosition.x, -38f);
            }

            TMP_Text localLabel = localPlayButtonObject.GetComponentInChildren<TMP_Text>(true);
            if (localLabel != null)
            {
                localLabel.text = "LOCAL GAME";
            }

            GameObject onlineButtonObject = Instantiate(
                localPlayButtonObject,
                localPlayButtonObject.transform.parent);
            onlineButtonObject.name = "OnlineGameButton";
            RectTransform onlineRect = onlineButtonObject.transform as RectTransform;
            if (onlineRect != null)
            {
                onlineRect.anchoredPosition = new Vector2(onlineRect.anchoredPosition.x, -138f);
            }

            SceneNavigationButton navigation = onlineButtonObject.GetComponent<SceneNavigationButton>();
            if (navigation != null)
            {
                Destroy(navigation);
            }

            Button onlineButton = onlineButtonObject.GetComponent<Button>();
            onlineButton.onClick = new Button.ButtonClickedEvent();
            onlineButton.onClick.AddListener(OpenOnlineMenu);
            TMP_Text onlineLabel = onlineButtonObject.GetComponentInChildren<TMP_Text>(true);
            if (onlineLabel != null)
            {
                onlineLabel.text = "ONLINE GAME";
            }

            BuildOverlay(canvasTransform);
            _service.ConnectionStateChanged += HandleConnectionStateChanged;
            _service.LobbyStateChanged += HandleLobbyStateChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.ConnectionStateChanged -= HandleConnectionStateChanged;
                _service.LobbyStateChanged -= HandleLobbyStateChanged;
            }
        }

        private void BuildOverlay(Transform canvasTransform)
        {
            _overlay = CreatePanel("OnlineOverlay", canvasTransform, Backdrop);
            Stretch(_overlay.GetComponent<RectTransform>());
            _overlay.transform.SetAsLastSibling();

            GameObject shell = CreatePanel("OnlineShell", _overlay.transform, Panel);
            SetRect(shell.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(820f, 720f));

            CreateText("Title", shell.transform, "ROPOLY ONLINE", 42f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 306f), new Vector2(740f, 62f), Color.white);
            _statusText = CreateText("Status", shell.transform, "OFFLINE", 18f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 257f), new Vector2(720f, 34f), Mint);

            Button closeButton = CreateButton("CloseButton", shell.transform, "BACK", new Vector2(-322f, 306f),
                new Vector2(110f, 46f), PanelLight, 17f, out _);
            closeButton.onClick.AddListener(CloseOnlineMenu);

            _connectPanel = CreatePanel("ConnectPanel", shell.transform, Color.clear);
            SetRect(_connectPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -28f), new Vector2(720f, 510f));

            CreateText("CreateHeading", _connectPanel.transform, "CREATE A ROOM", 24f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 205f), new Vector2(620f, 32f), Color.white);
            CreateText("CreateHelp", _connectPanel.transform,
                "Start an open room so other players can join you directly or by code.", 15f, FontStyles.Normal,
                TextAlignmentOptions.Center, new Vector2(0f, 175f), new Vector2(650f, 26f), new Color(0.76f, 0.72f, 0.86f));
            _createButton = CreateButton("CreateRoomButton", _connectPanel.transform, "CREATE ROOM",
                new Vector2(0f, 122f), new Vector2(380f, 54f), Purple, 19f, out _);
            _createButton.onClick.AddListener(CreateRoom);

            CreateText("AutoJoinHeading", _connectPanel.transform, "AUTO JOIN", 24f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, 68f), new Vector2(620f, 28f), Mint);
            CreateText("AutoJoinHelp", _connectPanel.transform,
                "Join an open room that another player already created (no code needed):", 15f, FontStyles.Normal,
                TextAlignmentOptions.Center, new Vector2(0f, 36f), new Vector2(650f, 24f), new Color(0.76f, 0.72f, 0.86f));
            _autoJoinButton = CreateButton("AutoJoinButton", _connectPanel.transform, "AUTO JOIN OPEN ROOM",
                new Vector2(0f, -10f), new Vector2(380f, 54f), Emerald, 19f, out _);
            _autoJoinButton.onClick.AddListener(AutoJoin);

            CreateText("JoinHelp", _connectPanel.transform,
                "Or enter a friend's room code directly:", 15f, FontStyles.Normal,
                TextAlignmentOptions.Center, new Vector2(0f, -62f), new Vector2(650f, 24f), new Color(0.76f, 0.72f, 0.86f));
            _joinCodeInput = CreateInputField(_connectPanel.transform, new Vector2(-105f, -108f));
            _joinButton = CreateButton("JoinRoomButton", _connectPanel.transform, "JOIN ROOM",
                new Vector2(192f, -108f), new Vector2(190f, 52f), Purple, 18f, out _);
            _joinButton.onClick.AddListener(JoinRoom);

            CreateText("Privacy", _connectPanel.transform,
                "ONLINE RELAY MULTIPLAYER  •  2–4 PLAYERS  •  CROSS-PLATFORM", 14f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0f, -178f), new Vector2(670f, 30f), Mint);

            _lobbyPanel = CreatePanel("LobbyPanel", shell.transform, Color.clear);
            SetRect(_lobbyPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -30f), new Vector2(740f, 510f));

            _roomCodeText = CreateText("RoomCode", _lobbyPanel.transform, "ROOM CODE: ------", 25f,
                FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 208f), new Vector2(650f, 42f), Mint);
            _playersText = CreateText("Players", _lobbyPanel.transform, "WAITING FOR PLAYERS...", 18f,
                FontStyles.Normal, TextAlignmentOptions.TopLeft, new Vector2(-202f, 58f), new Vector2(310f, 240f), Color.white);

            CreateText("Choose", _lobbyPanel.transform, "CHOOSE YOUR CREATURE", 20f, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(178f, 159f), new Vector2(350f, 38f), Color.white);
            BuildCreatureButtons(_lobbyPanel.transform);

            _readyButton = CreateButton("ReadyButton", _lobbyPanel.transform, "READY",
                new Vector2(178f, -119f), new Vector2(300f, 50f), Purple, 19f, out _readyButtonLabel);
            _readyButton.onClick.AddListener(ToggleReady);
            _startButton = CreateButton("StartButton", _lobbyPanel.transform, "START MATCH",
                new Vector2(178f, -180f), new Vector2(300f, 50f), Pink, 19f, out _startButtonLabel);
            _startButton.onClick.AddListener(() => _service.RequestStartMatch());
            Button leaveButton = CreateButton("LeaveButton", _lobbyPanel.transform, "LEAVE ROOM",
                new Vector2(-202f, -213f), new Vector2(230f, 46f), PanelLight, 17f, out _);
            leaveButton.onClick.AddListener(LeaveRoom);

            _overlay.SetActive(false);
        }

        private void BuildCreatureButtons(Transform parent)
        {
            if (_service.Config?.CreatureRoster == null)
            {
                return;
            }

            CreatureRosterDefinition roster = _service.Config.CreatureRoster;
            _creatureButtons = new Button[roster.Creatures.Count];
            for (int index = 0; index < roster.Creatures.Count; index++)
            {
                CreatureDefinition creature = roster.Creatures[index];
                int row = index / 2;
                int column = index % 2;
                Vector2 position = new Vector2(94f + (column * 170f), 105f - (row * 54f));
                Button button = CreateButton($"Creature_{creature.CreatureId}", parent,
                    creature.DisplayName.ToUpperInvariant(), position, new Vector2(154f, 48f),
                    creature.BodyColor, 14f, out _);
                string creatureId = creature.CreatureId;
                button.onClick.AddListener(() => _service.SelectCreature(creatureId));
                _creatureButtons[index] = button;
            }
        }

        private void OpenOnlineMenu()
        {
            _overlay.SetActive(true);
            _overlay.transform.SetAsLastSibling();
            Refresh();
        }

        private void CloseOnlineMenu()
        {
            if (_service.IsOnlineSession)
            {
                LeaveRoom();
            }
            else
            {
                _overlay.SetActive(false);
            }
        }

        private async void AutoJoin()
        {
            SetConnectButtons(false);
            await _service.AutoJoinRoomAsync();
            Refresh();
        }

        private async void CreateRoom()
        {
            SetConnectButtons(false);
            await _service.CreateRoomAsync();
            Refresh();
        }

        private async void JoinRoom()
        {
            SetConnectButtons(false);
            await _service.JoinRoomAsync(_joinCodeInput.text);
            Refresh();
        }

        private async void LeaveRoom()
        {
            await _service.LeaveRoomAsync();
            _overlay.SetActive(false);
        }

        private void ToggleReady()
        {
            OnlineLobbyPlayerData local = GetLocalPlayer();
            _service.SetReady(local?.IsReady != true);
        }

        private void HandleConnectionStateChanged(OnlineConnectionState state, string message)
        {
            Refresh();
        }

        private void HandleLobbyStateChanged(OnlineLobbyStateData state)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_statusText == null)
            {
                return;
            }

            _statusText.text = _service.StatusMessage;
            _statusText.color = _service.State == OnlineConnectionState.Failed ? Pink : Mint;
            bool inLobby = _service.IsOnlineSession &&
                           _service.State is OnlineConnectionState.InLobby or OnlineConnectionState.StartingMatch;
            _connectPanel.SetActive(!inLobby);
            _lobbyPanel.SetActive(inLobby);
            SetConnectButtons(_service.State is OnlineConnectionState.Offline or OnlineConnectionState.Failed);
            if (!inLobby)
            {
                return;
            }

            OnlineLobbyStateData lobby = _service.LobbyState;
            string roomType = _service.IsPrivateRoom ? "PRIVATE" : "PUBLIC";
            _roomCodeText.text = $"ROOM CODE: {lobby.JoinCode}  •  {roomType}";
            OnlineLobbyPlayerData[] players = lobby.Players ?? Array.Empty<OnlineLobbyPlayerData>();
            _playersText.text = players.Length == 0
                ? "WAITING FOR PLAYERS..."
                : string.Join("\n\n", players.OrderBy(player => player.SeatIndex).Select(FormatPlayer));

            OnlineLobbyPlayerData local = GetLocalPlayer();
            _readyButton.interactable = local != null && !string.IsNullOrWhiteSpace(local.CreatureId);
            _readyButtonLabel.text = local?.IsReady == true ? "NOT READY" : "READY";

            bool canStart = _service.IsHost && players.Length >= 2 &&
                            players.All(player => player.IsReady && !string.IsNullOrWhiteSpace(player.CreatureId));
            _startButton.gameObject.SetActive(_service.IsHost);
            _startButton.interactable = canStart;
            _startButtonLabel.text = canStart ? "START MATCH" : "WAITING FOR READY";
            RefreshCreatureButtons(local, players);
        }

        private string FormatPlayer(OnlineLobbyPlayerData player)
        {
            string creature = string.IsNullOrWhiteSpace(player.CreatureId)
                ? "CHOOSING..."
                : player.CreatureId.ToUpperInvariant();
            string host = player.IsHost ? "  •  HOST" : string.Empty;
            string ready = player.IsReady ? "READY" : "NOT READY";
            return $"{player.DisplayName}{host}\n<color=#{ColorUtility.ToHtmlStringRGB(player.IsReady ? Mint : Pink)}>{creature}  •  {ready}</color>";
        }

        private void RefreshCreatureButtons(
            OnlineLobbyPlayerData local,
            OnlineLobbyPlayerData[] players)
        {
            if (_service.Config?.CreatureRoster == null)
            {
                return;
            }

            for (int index = 0; index < _creatureButtons.Length; index++)
            {
                string creatureId = _service.Config.CreatureRoster.Creatures[index].CreatureId;
                bool usedByAnother = players.Any(player =>
                    player.ClientId != local?.ClientId && player.CreatureId == creatureId);
                _creatureButtons[index].interactable = !usedByAnother && local?.IsReady != true;
                TMP_Text label = _creatureButtons[index].GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    string baseLabel = label.text.Replace("[SELECTED] ", string.Empty);
                    label.text = local?.CreatureId == creatureId
                        ? $"[SELECTED] {baseLabel}"
                        : baseLabel;
                }
            }
        }

        private OnlineLobbyPlayerData GetLocalPlayer()
        {
            int seat = _service.LocalSeatIndex;
            return (_service.LobbyState.Players ?? Array.Empty<OnlineLobbyPlayerData>())
                .FirstOrDefault(player => player.SeatIndex == seat);
        }

        private void SetConnectButtons(bool interactable)
        {
            if (_autoJoinButton != null)
            {
                _autoJoinButton.interactable = interactable;
            }

            if (_createButton != null)
            {
                _createButton.interactable = interactable;
            }

            if (_joinButton != null)
            {
                _joinButton.interactable = interactable;
            }
        }

        private GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            gameObject.GetComponent<Image>().color = color;
            return gameObject;
        }

        private TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            SetRect(gameObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), position, size);
            var text = gameObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = _font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 position,
            Vector2 size,
            Color color,
            float fontSize,
            out TMP_Text labelText)
        {
            GameObject gameObject = CreatePanel(name, parent, color);
            SetRect(gameObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), position, size);
            var button = gameObject.AddComponent<Button>();
            button.targetGraphic = gameObject.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.35f);
            button.colors = colors;
            labelText = CreateText("Label", gameObject.transform, label, fontSize, FontStyles.Bold,
                TextAlignmentOptions.Center, Vector2.zero, size, Color.white);
            return button;
        }

        private TMP_InputField CreateInputField(Transform parent, Vector2 position, Vector2 size = default)
        {
            if (size == default)
            {
                size = new Vector2(380f, 52f);
            }

            GameObject root = CreatePanel("JoinCodeInput", parent, PanelLight);
            SetRect(root.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), position, size);
            var input = root.AddComponent<TMP_InputField>();

            GameObject viewportObject = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(root.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(18f, 6f);
            viewport.offsetMax = new Vector2(-18f, -6f);

            TMP_Text text = CreateText("Text", viewportObject.transform, string.Empty, 20f, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.zero, Color.white);
            Stretch(text.rectTransform);
            TMP_Text placeholder = CreateText("Placeholder", viewportObject.transform, "ROOM CODE", 18f,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.zero,
                new Color(0.6f, 0.56f, 0.7f, 1f));
            Stretch(placeholder.rectTransform);

            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 12;
            input.contentType = TMP_InputField.ContentType.Alphanumeric;
            input.onValueChanged.AddListener(value => input.text = value.ToUpperInvariant());
            return input;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
