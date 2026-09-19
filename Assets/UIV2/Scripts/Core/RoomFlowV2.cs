using System.Linq;
using DG.Tweening;
using Photon.Pun;
using Project51.Core;
using Project51.Networking;
using Project51.UIV2.Components;
using Project51.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Flusso online V2: crea stanza, entra con codice, ricerca partita, sala d'attesa (host e ospite).
    /// Mockup: screen_3_entra_codice, screen_1_ricerca_partita, 03_crea_stanza_con_bot, 04_lobby_non_host.
    /// La logica di rete resta in MatchmakingManager / GameLaunchController.
    /// </summary>
    public sealed class RoomFlowV2 : MonoBehaviour
    {
        public GameLaunchController Launcher;

        [Header("Pannelli")]
        public GameObject CreatePanel;
        public GameObject JoinPanel;
        public GameObject SearchPanel;
        public GameObject LobbyHostPanel;
        public GameObject LobbyGuestPanel;
        public Button[] CloseButtons;

        [Header("Crea stanza")]
        public Button[] Formats;
        public Sprite FormatSelectedSprite;
        public Sprite FormatNormalSprite;
        public TMP_Text CreateFormat;
        public Button Create;

        [Header("Entra con codice")]
        public TMP_InputField CodeInput;
        public CodeCellsV2 JoinCells;
        public GameObject JoinErrorRow;
        public TMP_Text JoinError;
        public Button Paste;
        public Button Join;

        [Header("Ricerca partita")]
        public TMP_Text SearchStatus;
        public TMP_Text SearchDetail;
        public TMP_Text SearchCount;
        public LobbySlotRowV2[] SearchRows;
        public RectTransform SearchProgressFill;

        [Header("Sala d'attesa - host")]
        public CodeCellsV2 HostCells;
        public Button HostCopy;
        public TMP_Text HostFeedback;
        public Button ShareButton;
        public TMP_Text HostCount;
        public LobbySlotRowV2[] HostRows;
        public TMP_Text HostHint;
        public Button StartGameButton;

        [Header("Sala d'attesa - ospite")]
        public TMP_Text GuestSubtitle;
        public CodeCellsV2 GuestCells;
        public Button GuestCopy;
        public TMP_Text GuestFeedback;
        public TMP_Text GuestCount;
        public LobbySlotRowV2[] GuestRows;
        public TMP_Text GuestStatus;

        public bool IsOpen => Panels.Any(p => p != null && p.activeSelf);

        /// <summary>
        /// Dove tornare annullando. Chi apre il flusso lo imposta (il pannello Modalita' si
        /// riapre da solo): senza questo, "Annulla" chiudeva tutto e lasciava la Home nuda,
        /// costringendo a ripartire da capo per cambiare una sola opzione.
        /// </summary>
        public System.Action ReturnOnCancel;

        private static readonly GameFormat[] FormatOrder = { GameFormat.OneVsOne, GameFormat.TwoVsTwo, GameFormat.FourPlayers };
        private static readonly string[] FormatNames = { "1 vs 1", "2 vs 2", "1 vs 3" };
        private const float ConnectTimeoutSeconds = 45f;

        private MatchmakingManager manager;
        private GameFormat format = GameFormat.FourPlayers;
        private bool joining;
        private bool busy;
        private float started;
        private float nextRefresh;
        // Photon aggiorna le proprieta' della stanza solo quando il server risponde: due tocchi rapidi
        // leggevano la stessa maschera e il secondo annullava il primo. Per un attimo vale quella locale.
        private int localBotMask;
        private float localBotMaskTime = -10f;

        private GameObject[] Panels => new[] { CreatePanel, JoinPanel, SearchPanel, LobbyHostPanel, LobbyGuestPanel };

        private void Awake()
        {
            Create.onClick.AddListener(CreateRoom);
            Join.onClick.AddListener(JoinRoom);
            Paste.onClick.AddListener(() => CodeInput.text = NormalizeCode(GUIUtility.systemCopyBuffer));
            HostCopy.onClick.AddListener(() => CopyCode(HostFeedback));
            GuestCopy.onClick.AddListener(() => CopyCode(GuestFeedback));
            ShareButton.onClick.AddListener(ShareCode);
            StartGameButton.onClick.AddListener(StartMatch);
            foreach (var button in CloseButtons) button.onClick.AddListener(Cancel);
            for (int i = 0; i < Formats.Length; i++)
            {
                int index = i;
                Formats[i].onClick.AddListener(() => SelectFormat(index));
            }
            for (int i = 0; i < HostRows.Length; i++)
            {
                int index = i;
                HostRows[i].BotButton.onClick.AddListener(() => ToggleBot(index));
            }
            CodeInput.onValueChanged.AddListener(EditCode);
            CodeInput.onSubmit.AddListener(_ => JoinRoom());
            HideAll();
        }

        public static string NormalizeCode(string code) =>
            new string((code ?? "").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).Take(5).ToArray());

        public static bool IsValidCode(string code) =>
            code != null && code.Length == 5 && code.All(c => c >= 'A' && c <= 'Z' || c >= '0' && c <= '9');

        public void OpenCreate()
        {
            joining = false;
            busy = false;
            SelectFormat(0);
            Show(CreatePanel);
        }

        public void OpenJoin()
        {
            joining = true;
            busy = false;
            Show(JoinPanel);
            CodeInput.text = "";
            EditCode("");
            CodeInput.ActivateInputField();
        }

        private void SelectFormat(int index)
        {
            format = FormatOrder[index];
            CreateFormat.text = "Formato: " + FormatNames[index];
            for (int i = 0; i < Formats.Length; i++)
                Formats[i].image.sprite = i == index ? FormatSelectedSprite : FormatNormalSprite;
        }

        private MatchConfig Config()
        {
            var config = new MatchConfig
            {
                Intent = MatchIntent.PrivateRoom,
                Format = format,
                DeckBackId = CardDecks.SelectedId,
                Rules = MatchRules.Default.Clone()
            };
            if (format == GameFormat.OneVsOne)
            {
                config.Rules.CappottoEndsGameImmediately = false;
                config.Rules.CappottoBonusPoints = 0;
            }
            return config;
        }

        private void CreateRoom()
        {
            if (busy) return;
            busy = true;
            joining = false;
            started = Time.unscaledTime;
            Launcher.CreatePrivateRoom(Config());
        }

        private void JoinRoom()
        {
            if (busy) return;
            string code = NormalizeCode(CodeInput.text);
            if (!IsValidCode(code))
            {
                ShowJoinError("Inserisci tutti i 5 caratteri del codice.");
                return;
            }
            busy = true;
            joining = true;
            started = Time.unscaledTime;
            Launcher.JoinPrivateRoom(code, Config());
        }

        private void EditCode(string value)
        {
            string code = NormalizeCode(value);
            if (code != value) CodeInput.SetTextWithoutNotify(code);
            JoinCells.SetCode(code);
            Join.interactable = IsValidCode(code) && !busy;
            JoinErrorRow.SetActive(false);
        }

        private void ShowJoinError(string message)
        {
            JoinError.text = message;
            JoinErrorRow.SetActive(true);
        }

        private void Subscribe()
        {
            if (manager != null || MatchmakingManager.Instance == null) return;
            manager = MatchmakingManager.Instance;
            manager.OnStateChanged += State;
            manager.OnError += Error;
            manager.OnRoomCreated += Created;
            manager.OnRoomJoined += Joined;
        }

        private void State(MatchmakingState state)
        {
            if (state == MatchmakingState.Idle) { busy = false; return; }
            if (state == MatchmakingState.InWaitingRoom) { Joined(); return; }
            if (state == MatchmakingState.Connecting) { started = Time.unscaledTime; busy = true; }
            Show(SearchPanel);
            SearchStatus.text = state == MatchmakingState.Starting ? "Partita trovata!"
                : state == MatchmakingState.CreatingRoom ? "Creazione stanza…"
                : state == MatchmakingState.JoiningRoom ? "Ingresso nella stanza…"
                : state == MatchmakingState.Connecting ? "Connessione…"
                : "Ricerca giocatori…";
        }

        private void Created(string code) => Joined();

        private void Joined()
        {
            busy = false;
            HostFeedback.text = "";
            GuestFeedback.text = "";
            ShowLobby();
            RefreshPlayers();
        }

        // Host e ospite hanno layout diversi; se l'host esce, Photon passa il ruolo a un ospite.
        private void ShowLobby() => Show(PhotonNetwork.IsMasterClient ? LobbyHostPanel : LobbyGuestPanel);

        public void Error(string error)
        {
            busy = false;
            if (joining)
            {
                Show(JoinPanel);
                ShowJoinError(error);
                Join.interactable = IsValidCode(CodeInput.text);
            }
            else
            {
                Show(SearchPanel);
                SearchStatus.text = "Connessione non riuscita";
                SearchDetail.text = error;
            }
        }

        public void Cancel()
        {
            if (manager != null && (busy || PhotonNetwork.InRoom)) manager.Cancel();
            busy = false;
            HideAll();
            var back = ReturnOnCancel;
            ReturnOnCancel = null;
            if (back != null) back();
        }

        private void Show(GameObject panel)
        {
            if (panel.activeSelf) return;
            HideAll();
            panel.SetActive(true);
            var group = panel.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.DOFade(1f, 0.2f).SetUpdate(true).SetLink(panel);
        }

        private void HideAll()
        {
            foreach (var panel in Panels)
            {
                if (panel == null) continue;
                panel.GetComponent<CanvasGroup>().DOKill();
                panel.SetActive(false);
            }
        }

        private string RoomCode => PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "";

        private void CopyCode(TMP_Text feedback)
        {
            GUIUtility.systemCopyBuffer = RoomCode;
            feedback.text = "Codice copiato!";
        }

        private void ShareCode()
        {
            string text = "Gioca a 51 con me! Apri il gioco, tocca \"Entra in stanza\" e inserisci il codice " + RoomCode;
            if (!NativeShare.ShareText(text)) HostFeedback.text = "Invito copiato: incollalo in chat ai tuoi amici";
        }

        private int BotMask
        {
            get
            {
                if (Time.unscaledTime - localBotMaskTime < 2f) return localBotMask;
                return PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("ui_bots", out var value) ? (int)value : 0;
            }
        }

        private void ToggleBot(int index)
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || index < PhotonNetwork.CurrentRoom.PlayerCount) return;
            int mask = BotMask ^ (1 << index);
            localBotMask = mask;
            localBotMaskTime = Time.unscaledTime;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "ui_bots", mask } });
            RefreshPlayers();
        }

        private bool CanStart()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return false;
            for (int i = PhotonNetwork.CurrentRoom.PlayerCount; i < PhotonNetwork.CurrentRoom.MaxPlayers; i++)
                if ((BotMask & (1 << i)) == 0) return false;
            return true;
        }

        private void StartMatch()
        {
            if (!CanStart()) return;
            // La partita parte davvero: uscendo dal tavolo si torna alla Home, non al pannello
            // Modalita' di mezz'ora prima.
            ReturnOnCancel = null;
            StartGameButton.interactable = false;
            manager.StartGame();
        }

        private static GameFormat RoomFormat =>
            PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("format", out var value)
                ? (GameFormat)(int)value : GameFormat.FourPlayers;

        // A coppie i primi due entrati sono compagni (SeatLayout): la riga i e' l'ordine di ingresso.
        private static string TeamLabel(GameFormat roomFormat, int joinIndex) =>
            roomFormat != GameFormat.TwoVsTwo ? ""
                : SeatLayout.SeatForJoinOrder(GameFormat.TwoVsTwo, joinIndex) % 2 == 0 ? " · SQUADRA A" : " · SQUADRA B";

        public void RefreshPlayers()
        {
            if (!PhotonNetwork.InRoom) return;
            if (PhotonNetwork.IsMasterClient != LobbyHostPanel.activeSelf) ShowLobby();

            var room = PhotonNetwork.CurrentRoom;
            var players = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
            var roomFormat = RoomFormat;
            bool host = PhotonNetwork.IsMasterClient;
            var rows = host ? HostRows : GuestRows;
            int occupied = 0;

            for (int i = 0; i < rows.Length; i++)
            {
                if (i >= room.MaxPlayers) { rows[i].Hide(); continue; }
                string team = TeamLabel(roomFormat, i);
                if (i < players.Length)
                {
                    occupied++;
                    var player = players[i];
                    string name = player.IsLocal && !host ? "Tu" : player.NickName;
                    rows[i].ShowPlayer(name, (player.IsMasterClient ? "HOST" : "PRONTO") + team,
                        player.IsMasterClient ? LobbySlotRowV2.HostColor : LobbySlotRowV2.ReadyColor);
                }
                else if ((BotMask & (1 << i)) != 0)
                {
                    occupied++;
                    rows[i].ShowPlayer("Bot " + (i + 1), "BOT" + team, LobbySlotRowV2.BotColor, isBot: true, canRemoveBot: host);
                }
                else
                {
                    rows[i].ShowEmpty(host ? "Slot libero" : "In attesa…", canAddBot: host);
                }
            }

            string count = "GIOCATORI " + occupied + "/" + room.MaxPlayers;
            if (host)
            {
                HostCells.SetCode(room.Name);
                HostCount.text = count;
                bool ready = CanStart();
                StartGameButton.interactable = ready;
                HostHint.text = ready ? "Tavolo pronto: puoi avviare la partita" : "Solo l'host può aggiungere bot o avviare";
            }
            else
            {
                var hostPlayer = players.FirstOrDefault(p => p.IsMasterClient);
                GuestSubtitle.text = hostPlayer != null ? "Stanza di " + hostPlayer.NickName : "Sala d'attesa";
                GuestCells.SetCode(room.Name);
                GuestCount.text = count;
                GuestStatus.text = "In attesa che l'host avvii la partita…";
            }
        }

        private void RefreshSearch()
        {
            int seconds = (int)(Time.unscaledTime - started);
            var players = PhotonNetwork.InRoom ? PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray() : new Photon.Realtime.Player[0];
            var config = manager != null ? manager.CurrentConfig : null;
            int max = config != null ? config.PlayerCount : 4;
            int formatIndex = config != null ? System.Array.IndexOf(FormatOrder, config.Format) : -1;

            float botsIn = manager != null ? manager.QuickMatchSecondsLeft : -1f;
            SearchDetail.text = (formatIndex >= 0 ? "Modalità: " + FormatNames[formatIndex] + "  ·  " : "")
                + "tempo " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00")
                + (botsIn >= 0f ? "  ·  bot tra " + Mathf.CeilToInt(botsIn) + "s" : "");
            SearchCount.text = "GIOCATORI " + players.Length + "/" + max;

            for (int i = 0; i < SearchRows.Length; i++)
            {
                if (i >= max) { SearchRows[i].Hide(); continue; }
                if (i < players.Length)
                    SearchRows[i].ShowPlayer(players[i].IsLocal ? "Tu" : players[i].NickName,
                        players[i].IsMasterClient ? "HOST" : "PRONTO",
                        players[i].IsMasterClient ? LobbySlotRowV2.HostColor : LobbySlotRowV2.ReadyColor);
                else
                    SearchRows[i].ShowEmpty("In attesa…", canAddBot: false);
            }

            // Barra: avanzamento verso il riempimento con bot della partita veloce.
            float progress = botsIn >= 0f ? 1f - botsIn / MatchmakingManager.QuickMatchBotFillSeconds : 0f;
            SearchProgressFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        }

        private void Update()
        {
            Subscribe();
            if (Input.GetKeyDown(KeyCode.Escape) && IsOpen) Cancel();

            if (SearchPanel.activeSelf && busy)
            {
                RefreshSearch();
                if (Time.unscaledTime - started > ConnectTimeoutSeconds && !PhotonNetwork.InRoom)
                {
                    manager?.Cancel();
                    Error("La connessione sta impiegando troppo tempo. Riprova dalla Home.");
                }
            }

            bool inLobby = LobbyHostPanel.activeSelf || LobbyGuestPanel.activeSelf;
            if (inLobby && Time.unscaledTime > nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 0.2f;
                RefreshPlayers();
            }
        }

        private void OnDestroy()
        {
            if (manager == null) return;
            manager.OnStateChanged -= State;
            manager.OnError -= Error;
            manager.OnRoomCreated -= Created;
            manager.OnRoomJoined -= Joined;
        }
    }
}
