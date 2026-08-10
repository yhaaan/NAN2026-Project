using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    /// <summary>
    /// Small client-hosted lobby prototype. Gameplay synchronization is intentionally
    /// outside this class; the joined Relay/NGO connection survives the scene change.
    /// </summary>
    public sealed class PvpLobbyController : MonoBehaviour
    {
        private const string SessionType = "nan2026-pvp-prototype";
        private const string PrototypePropertyKey = "prototype";
        private const string PrototypePropertyValue = "nan2026-pvp-v1";
        private const string NicknamePropertyKey = "nickname";
        private const string MatchStatePropertyKey = "matchState";
        private const string WaitingState = "waiting";
        private const string StartedState = "started";
        private const int MaxPlayers = 2;
        private const int MaxVisibleRooms = 7;

        private TMP_FontAsset boldFont;
        private TMP_FontAsset lightFont;
        private Button pvpButton;
        private GameObject overlay;
        private GameObject browserView;
        private GameObject waitingView;
        private TMP_InputField nicknameInput;
        private TMP_Text browserStatusText;
        private RectTransform roomListRoot;
        private TMP_Text waitingRoomTitleText;
        private TMP_Text waitingRoomCodeText;
        private TMP_Text waitingPlayersText;
        private TMP_Text waitingStatusText;
        private Button createRoomButton;
        private Button refreshButton;
        private Button startGameButton;
        private Button leaveRoomButton;
        private ISession currentSession;
        private bool initialized;
        private bool busy;
        private bool startingGame;

        public void Initialize(
            Button titlePvpButton,
            GameObject lobbyOverlay,
            TMP_FontAsset titleBoldFont,
            TMP_FontAsset titleLightFont)
        {
            boldFont = titleBoldFont != null ? titleBoldFont : TMP_Settings.defaultFontAsset;
            lightFont = titleLightFont != null ? titleLightFont : boldFont;
            pvpButton = titlePvpButton;
            overlay = lobbyOverlay;

            if (pvpButton == null || overlay == null)
            {
                Debug.LogError(
                    "PvpLobbyController requires the prefab PvP button and inactive lobby overlay.",
                    this);
                return;
            }

            overlay.SetActive(false);
            pvpButton.onClick.AddListener(OpenLobby);
            CreateOverlay();
        }

        private void OnDestroy()
        {
            if (pvpButton != null)
            {
                pvpButton.onClick.RemoveListener(OpenLobby);
            }

            UnsubscribeFromSession();

            if (currentSession != null && !startingGame)
            {
                _ = currentSession.LeaveAsync();
            }
        }


        private void CreateOverlay()
        {
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            Stretch(overlayRect, Vector2.zero, Vector2.zero);
            overlay.transform.SetAsLastSibling();

            Image dim = overlay.GetComponent<Image>();
            if (dim == null)
            {
                dim = overlay.AddComponent<Image>();
            }

            dim.color = new Color(0.015f, 0.025f, 0.04f, 0.88f);

            GameObject panel = CreateUiObject("Panel", overlay.transform);
            RectTransform panelRect = (RectTransform)panel.transform;
            Center(panelRect, new Vector2(860f, 660f), Vector2.zero);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.055f, 0.09f, 0.13f, 0.99f);

            TMP_Text title = CreateText("Title", panel.transform, "PvP 대전", boldFont, 38f, TextAlignmentOptions.Center);
            SetTopRect((RectTransform)title.transform, new Vector2(0f, -22f), new Vector2(780f, 54f));

            browserView = CreateUiObject("BrowserView", panel.transform);
            Stretch((RectTransform)browserView.transform, new Vector2(34f, 34f), new Vector2(-34f, -84f));
            CreateBrowserView();

            waitingView = CreateUiObject("WaitingView", panel.transform);
            Stretch((RectTransform)waitingView.transform, new Vector2(34f, 34f), new Vector2(-34f, -84f));
            CreateWaitingView();
        }

        private void CreateBrowserView()
        {
            TMP_Text nicknameLabel = CreateText(
                "NicknameLabel",
                browserView.transform,
                "닉네임",
                boldFont,
                24f,
                TextAlignmentOptions.Left);
            SetTopRect((RectTransform)nicknameLabel.transform, new Vector2(-250f, -4f), new Vector2(180f, 44f));

            nicknameInput = CreateInputField(browserView.transform, "닉네임을 입력하세요");
            SetTopRect((RectTransform)nicknameInput.transform, new Vector2(60f, -4f), new Vector2(430f, 48f));
            nicknameInput.characterLimit = PvpNicknameUtility.MaxLength;
            nicknameInput.text = PvpNicknameUtility.CreateRandom();

            TMP_Text roomListTitle = CreateText(
                "RoomListTitle",
                browserView.transform,
                "참가 가능한 방",
                boldFont,
                26f,
                TextAlignmentOptions.Left);
            SetTopRect((RectTransform)roomListTitle.transform, new Vector2(0f, -78f), new Vector2(760f, 42f));

            GameObject roomListBackground = CreateUiObject("RoomListBackground", browserView.transform);
            RectTransform roomListBackgroundRect = (RectTransform)roomListBackground.transform;
            SetTopRect(roomListBackgroundRect, new Vector2(0f, -124f), new Vector2(760f, 350f));
            Image roomListImage = roomListBackground.AddComponent<Image>();
            roomListImage.color = new Color(0.025f, 0.045f, 0.07f, 0.96f);

            GameObject roomList = CreateUiObject("RoomList", roomListBackground.transform);
            roomListRoot = (RectTransform)roomList.transform;
            Stretch(roomListRoot, new Vector2(12f, 12f), new Vector2(-12f, -12f));

            browserStatusText = CreateText(
                "BrowserStatus",
                browserView.transform,
                "서비스 연결 대기 중",
                lightFont,
                19f,
                TextAlignmentOptions.Center);
            SetBottomRect((RectTransform)browserStatusText.transform, new Vector2(0f, 70f), new Vector2(760f, 34f));

            refreshButton = CreateButton(
                "RefreshButton",
                browserView.transform,
                "새로고침",
                new Color(0.16f, 0.25f, 0.34f, 1f),
                new Vector2(180f, 52f),
                22f);
            SetBottomRect((RectTransform)refreshButton.transform, new Vector2(-210f, 6f), new Vector2(180f, 52f));
            refreshButton.onClick.AddListener(RefreshRooms);

            createRoomButton = CreateButton(
                "CreateRoomButton",
                browserView.transform,
                "방 생성",
                new Color(0.12f, 0.42f, 0.58f, 1f),
                new Vector2(180f, 52f),
                22f);
            SetBottomRect((RectTransform)createRoomButton.transform, new Vector2(0f, 6f), new Vector2(180f, 52f));
            createRoomButton.onClick.AddListener(CreateRoom);

            Button closeButton = CreateButton(
                "CloseButton",
                browserView.transform,
                "닫기",
                new Color(0.28f, 0.22f, 0.25f, 1f),
                new Vector2(180f, 52f),
                22f);
            SetBottomRect((RectTransform)closeButton.transform, new Vector2(210f, 6f), new Vector2(180f, 52f));
            closeButton.onClick.AddListener(CloseLobby);
        }

        private void CreateWaitingView()
        {
            waitingRoomTitleText = CreateText(
                "RoomTitle",
                waitingView.transform,
                "방 대기 중",
                boldFont,
                31f,
                TextAlignmentOptions.Center);
            SetTopRect((RectTransform)waitingRoomTitleText.transform, new Vector2(0f, -24f), new Vector2(760f, 48f));

            waitingRoomCodeText = CreateText(
                "RoomCode",
                waitingView.transform,
                string.Empty,
                lightFont,
                19f,
                TextAlignmentOptions.Center);
            SetTopRect((RectTransform)waitingRoomCodeText.transform, new Vector2(0f, -78f), new Vector2(760f, 36f));

            GameObject playersBackground = CreateUiObject("PlayersBackground", waitingView.transform);
            SetTopRect((RectTransform)playersBackground.transform, new Vector2(0f, -142f), new Vector2(660f, 245f));
            Image playersImage = playersBackground.AddComponent<Image>();
            playersImage.color = new Color(0.025f, 0.045f, 0.07f, 0.96f);

            waitingPlayersText = CreateText(
                "Players",
                playersBackground.transform,
                string.Empty,
                boldFont,
                27f,
                TextAlignmentOptions.Center);
            Stretch((RectTransform)waitingPlayersText.transform, new Vector2(24f, 24f), new Vector2(-24f, -24f));

            waitingStatusText = CreateText(
                "WaitingStatus",
                waitingView.transform,
                "다른 플레이어를 기다리는 중입니다.",
                lightFont,
                21f,
                TextAlignmentOptions.Center);
            SetBottomRect((RectTransform)waitingStatusText.transform, new Vector2(0f, 100f), new Vector2(760f, 42f));

            startGameButton = CreateButton(
                "StartGameButton",
                waitingView.transform,
                "게임 시작",
                new Color(0.10f, 0.48f, 0.38f, 1f),
                new Vector2(220f, 58f),
                25f);
            SetBottomRect((RectTransform)startGameButton.transform, new Vector2(-125f, 20f), new Vector2(220f, 58f));
            startGameButton.onClick.AddListener(StartGame);

            leaveRoomButton = CreateButton(
                "LeaveRoomButton",
                waitingView.transform,
                "방 나가기",
                new Color(0.34f, 0.20f, 0.23f, 1f),
                new Vector2(220f, 58f),
                25f);
            SetBottomRect((RectTransform)leaveRoomButton.transform, new Vector2(125f, 20f), new Vector2(220f, 58f));
            leaveRoomButton.onClick.AddListener(LeaveRoom);
        }

        private async void OpenLobby()
        {
            if (overlay == null || busy)
            {
                return;
            }

            overlay.SetActive(true);
            browserView.SetActive(true);
            waitingView.SetActive(false);
            browserStatusText.text = "Unity Gaming Services에 연결 중...";

            if (!await EnsureInitializedAsync())
            {
                return;
            }

            await RefreshRoomListAsync();
        }

        private async Task<bool> EnsureInitializedAsync()
        {
            if (initialized)
            {
                return true;
            }

            SetBusy(true);
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                EnsureNetworkManager();
                initialized = true;
                browserStatusText.text = "연결되었습니다.";
                return true;
            }
            catch (Exception exception)
            {
                browserStatusText.text = "서비스 연결 실패. Unity Dashboard 설정을 확인해주세요.";
                Debug.LogException(exception, this);
                return false;
            }
            finally
            {
                SetBusy(false);
            }
        }

        private static void EnsureNetworkManager()
        {
            if (NetworkManager.Singleton != null)
            {
                return;
            }

            GameObject networkObject = new GameObject("PvpNetworkManager");
            DontDestroyOnLoad(networkObject);

            UnityTransport transport = networkObject.AddComponent<UnityTransport>();
            NetworkManager networkManager = networkObject.AddComponent<NetworkManager>();
            networkManager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false,
                ConnectionApproval = false
            };
        }

        private async void RefreshRooms()
        {
            if (!await EnsureInitializedAsync())
            {
                return;
            }

            await RefreshRoomListAsync();
        }

        private async Task RefreshRoomListAsync()
        {
            if (busy || currentSession != null)
            {
                return;
            }

            SetBusy(true);
            browserStatusText.text = "방 목록을 불러오는 중...";
            ClearRoomList();

            try
            {
                QuerySessionsOptions options = new QuerySessionsOptions
                {
                    Count = MaxVisibleRooms,
                    FilterOptions = new List<FilterOption>
                    {
                        new FilterOption(FilterField.AvailableSlots, "1", FilterOperation.GreaterOrEqual),
                        new FilterOption(FilterField.IsLocked, "false", FilterOperation.Equal),
                        new FilterOption(FilterField.StringIndex1, PrototypePropertyValue, FilterOperation.Equal)
                    }
                };

                QuerySessionsResults result = await MultiplayerService.Instance.QuerySessionsAsync(options);
                if (result.Sessions.Count == 0)
                {
                    browserStatusText.text = "참가 가능한 방이 없습니다. 새 방을 만들어보세요.";
                    return;
                }

                int count = Mathf.Min(result.Sessions.Count, MaxVisibleRooms);
                for (int index = 0; index < count; index++)
                {
                    AddRoomEntry(result.Sessions[index], index);
                }

                browserStatusText.text = $"참가 가능한 방 {count}개";
            }
            catch (Exception exception)
            {
                browserStatusText.text = "방 목록을 불러오지 못했습니다. 잠시 후 다시 시도해주세요.";
                Debug.LogException(exception, this);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void AddRoomEntry(ISessionInfo sessionInfo, int index)
        {
            int playerCount = sessionInfo.MaxPlayers - sessionInfo.AvailableSlots;
            Button roomButton = CreateButton(
                $"Room_{index}",
                roomListRoot,
                $"{sessionInfo.Name}     {playerCount}/{sessionInfo.MaxPlayers}     입장",
                new Color(0.10f, 0.18f, 0.25f, 1f),
                new Vector2(712f, 44f),
                20f);

            RectTransform rect = (RectTransform)roomButton.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -24f - index * 47f);
            string sessionId = sessionInfo.Id;
            roomButton.onClick.AddListener(() => JoinRoom(sessionId));
        }

        private void ClearRoomList()
        {
            if (roomListRoot == null)
            {
                return;
            }

            for (int index = roomListRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(roomListRoot.GetChild(index).gameObject);
            }
        }

        private async void CreateRoom()
        {
            if (!await EnsureInitializedAsync() || busy || currentSession != null)
            {
                return;
            }

            string nickname = GetNickname();
            SetBusy(true);
            browserStatusText.text = "Relay 방을 생성하는 중...";

            try
            {
                SessionOptions options = new SessionOptions
                {
                    Name = $"{nickname}의 방",
                    Type = SessionType,
                    MaxPlayers = MaxPlayers,
                    IsPrivate = false,
                    IsLocked = false,
                    PlayerProperties = CreatePlayerProperties(nickname),
                    SessionProperties = new Dictionary<string, SessionProperty>
                    {
                        {
                            PrototypePropertyKey,
                            new SessionProperty(
                                PrototypePropertyValue,
                                VisibilityPropertyOptions.Public,
                                PropertyIndex.String1)
                        },
                        {
                            MatchStatePropertyKey,
                            new SessionProperty(WaitingState, VisibilityPropertyOptions.Member)
                        }
                    }
                }.WithRelayNetwork();

                currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
                SubscribeToSession();
                ShowWaitingRoom();
            }
            catch (Exception exception)
            {
                currentSession = null;
                browserStatusText.text = "방 생성에 실패했습니다. 잠시 후 다시 시도해주세요.";
                Debug.LogException(exception, this);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void JoinRoom(string sessionId)
        {
            if (!await EnsureInitializedAsync() || busy || currentSession != null)
            {
                return;
            }

            string nickname = GetNickname();
            SetBusy(true);
            browserStatusText.text = "방에 입장하는 중...";

            bool shouldRefresh = false;

            try
            {
                JoinSessionOptions options = new JoinSessionOptions
                {
                    Type = SessionType,
                    PlayerProperties = CreatePlayerProperties(nickname)
                };

                currentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId, options);
                SubscribeToSession();
                ShowWaitingRoom();
            }
            catch (Exception exception)
            {
                currentSession = null;
                browserStatusText.text = "방에 입장하지 못했습니다. 목록을 새로고침해주세요.";
                Debug.LogException(exception, this);
                shouldRefresh = true;
            }
            finally
            {
                SetBusy(false);
            }

            if (shouldRefresh)
            {
                await RefreshRoomListAsync();
            }
        }

        private static Dictionary<string, PlayerProperty> CreatePlayerProperties(string nickname)
        {
            return new Dictionary<string, PlayerProperty>
            {
                {
                    NicknamePropertyKey,
                    new PlayerProperty(nickname, VisibilityPropertyOptions.Member)
                }
            };
        }

        private string GetNickname()
        {
            string nickname = PvpNicknameUtility.Normalize(nicknameInput.text);
            nicknameInput.SetTextWithoutNotify(nickname);
            return nickname;
        }

        private void SubscribeToSession()
        {
            if (currentSession == null)
            {
                return;
            }

            currentSession.Changed += OnSessionChanged;
            currentSession.RemovedFromSession += OnSessionRemoved;
            currentSession.Deleted += OnSessionRemoved;
        }

        private void UnsubscribeFromSession()
        {
            if (currentSession == null)
            {
                return;
            }

            currentSession.Changed -= OnSessionChanged;
            currentSession.RemovedFromSession -= OnSessionRemoved;
            currentSession.Deleted -= OnSessionRemoved;
        }

        private void OnSessionChanged()
        {
            if (currentSession == null || startingGame)
            {
                return;
            }

            UpdateWaitingRoom();
            if (IsGameStarted())
            {
                BeginGame();
            }
        }

        private async void OnSessionRemoved()
        {
            if (startingGame)
            {
                return;
            }

            UnsubscribeFromSession();
            currentSession = null;
            browserView.SetActive(true);
            waitingView.SetActive(false);
            browserStatusText.text = "방 연결이 종료되었습니다.";
            await RefreshRoomListAsync();
        }

        private void ShowWaitingRoom()
        {
            browserView.SetActive(false);
            waitingView.SetActive(true);
            UpdateWaitingRoom();
        }

        private void UpdateWaitingRoom()
        {
            if (currentSession == null)
            {
                return;
            }

            waitingRoomTitleText.text = currentSession.Name;
            waitingRoomCodeText.text = $"방 코드  {currentSession.Code}";

            List<string> playerLines = new List<string>();
            foreach (IReadOnlyPlayer player in currentSession.Players)
            {
                string nickname = player.Properties.TryGetValue(NicknamePropertyKey, out PlayerProperty property)
                    ? property.Value
                    : "이름 없는 플레이어";
                string hostMark = player.Id == currentSession.Host ? "  (방장)" : string.Empty;
                playerLines.Add($"● {nickname}{hostMark}");
            }

            while (playerLines.Count < MaxPlayers)
            {
                playerLines.Add("○ 참가자 대기 중...");
            }

            waitingPlayersText.text = string.Join("\n\n", playerLines);
            bool canStart = currentSession.IsHost && currentSession.PlayerCount == MaxPlayers && !busy;
            startGameButton.gameObject.SetActive(currentSession.IsHost);
            startGameButton.interactable = canStart;
            leaveRoomButton.interactable = !busy;

            if (currentSession.IsHost)
            {
                waitingStatusText.text = canStart
                    ? "두 명이 모였습니다. 게임을 시작할 수 있습니다."
                    : "상대 플레이어를 기다리는 중입니다.";
            }
            else
            {
                waitingStatusText.text = "방장이 게임을 시작할 때까지 기다려주세요.";
            }
        }

        private async void StartGame()
        {
            if (currentSession == null || !currentSession.IsHost || currentSession.PlayerCount != MaxPlayers || busy)
            {
                return;
            }

            SetBusy(true);
            waitingStatusText.text = "게임을 시작하는 중...";

            try
            {
                IHostSession hostSession = currentSession.AsHost();
                hostSession.IsLocked = true;
                hostSession.SetProperty(
                    MatchStatePropertyKey,
                    new SessionProperty(StartedState, VisibilityPropertyOptions.Member));
                await hostSession.SavePropertiesAsync();
                BeginGame();
            }
            catch (Exception exception)
            {
                waitingStatusText.text = "게임 시작에 실패했습니다. 다시 시도해주세요.";
                Debug.LogException(exception, this);
                SetBusy(false);
            }
        }

        private bool IsGameStarted()
        {
            return currentSession != null &&
                   currentSession.Properties.TryGetValue(MatchStatePropertyKey, out SessionProperty state) &&
                   state.Value == StartedState;
        }

        private void BeginGame()
        {
            if (startingGame)
            {
                return;
            }

            startingGame = true;
            SetBusy(true);
            SceneTransitionController.LoadMainGame();
        }

        private async void LeaveRoom()
        {
            if (currentSession == null || busy)
            {
                return;
            }

            SetBusy(true);
            waitingStatusText.text = "방에서 나가는 중...";
            ISession sessionToLeave = currentSession;
            UnsubscribeFromSession();
            currentSession = null;

            try
            {
                await sessionToLeave.LeaveAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                SetBusy(false);
                browserView.SetActive(true);
                waitingView.SetActive(false);
                await RefreshRoomListAsync();
            }
        }

        private async void CloseLobby()
        {
            if (busy)
            {
                return;
            }

            if (currentSession != null)
            {
                ISession sessionToLeave = currentSession;
                UnsubscribeFromSession();
                currentSession = null;
                try
                {
                    await sessionToLeave.LeaveAsync();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }

            overlay.SetActive(false);
        }

        private void SetBusy(bool value)
        {
            busy = value;
            if (refreshButton != null)
            {
                refreshButton.interactable = !value;
            }

            if (createRoomButton != null)
            {
                createRoomButton.interactable = !value;
            }

            if (nicknameInput != null)
            {
                nicknameInput.interactable = !value;
            }

            if (leaveRoomButton != null)
            {
                leaveRoomButton.interactable = !value;
            }

            if (currentSession != null && waitingView != null && waitingView.activeSelf)
            {
                UpdateWaitingRoom();
            }
        }

        private TMP_InputField CreateInputField(Transform parent, string placeholderText)
        {
            GameObject inputObject = CreateUiObject("NicknameInput", parent);
            Image background = inputObject.AddComponent<Image>();
            background.color = new Color(0.92f, 0.95f, 0.97f, 1f);

            TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
            input.contentType = TMP_InputField.ContentType.Standard;
            input.lineType = TMP_InputField.LineType.SingleLine;

            GameObject viewportObject = CreateUiObject("Text Area", inputObject.transform);
            RectTransform viewport = (RectTransform)viewportObject.transform;
            Stretch(viewport, new Vector2(14f, 6f), new Vector2(-14f, -6f));
            viewportObject.AddComponent<RectMask2D>();

            TMP_Text text = CreateText(
                "Text",
                viewportObject.transform,
                string.Empty,
                lightFont,
                22f,
                TextAlignmentOptions.MidlineLeft);
            Stretch((RectTransform)text.transform, Vector2.zero, Vector2.zero);
            text.color = new Color(0.05f, 0.08f, 0.12f, 1f);

            TMP_Text placeholder = CreateText(
                "Placeholder",
                viewportObject.transform,
                placeholderText,
                lightFont,
                22f,
                TextAlignmentOptions.MidlineLeft);
            Stretch((RectTransform)placeholder.transform, Vector2.zero, Vector2.zero);
            placeholder.color = new Color(0.28f, 0.34f, 0.40f, 0.65f);

            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        private Button CreateButton(
            string name,
            Transform parent,
            string label,
            Color color,
            Vector2 size,
            float fontSize)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.sizeDelta = size;

            Image image = buttonObject.AddComponent<Image>();
            image.color = color;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.7f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TMP_Text text = CreateText("Label", buttonObject.transform, label, boldFont, fontSize, TextAlignmentOptions.Center);
            Stretch((RectTransform)text.transform, new Vector2(8f, 4f), new Vector2(-8f, -4f));
            text.color = Color.white;
            text.raycastTarget = false;
            return button;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            TMP_FontAsset font,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateUiObject(name, parent);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.color = Color.white;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = 5;
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void Center(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void SetTopRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void SetBottomRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
