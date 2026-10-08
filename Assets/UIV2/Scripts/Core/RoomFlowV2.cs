using System.Linq;
using DG.Tweening;
using Project51.UIV2.Animations;
using Photon.Pun;
using Project51.Core;
using Project51.Networking;
using Project51.UIV2.Components;
using Project51.Unity;
using Project51.Unity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Flusso online: crea stanza ed entra con codice (dalla scheda Stanza privata di Modalita'), ricerca partita, sala d'attesa
    /// (host e ospite). Ricerca = UI51 Matchmaking, sala = UI51 SalaPrivata (Fase 13).
    /// La logica di rete resta in MatchmakingManager / GameLaunchController.
    /// </summary>
    public sealed class RoomFlowV2 : MonoBehaviour
    {
        public GameLaunchController Launcher;

        [Header("Pannelli")]
        public GameObject SearchPanel;
        public GameObject LobbyHostPanel;
        public GameObject LobbyGuestPanel;
        public Button[] CloseButtons;

        [Header("Ricerca partita (UI51 Matchmaking)")]
        public UI51MatchmakingView SearchView;

        [Header("Sala d'attesa (UI51 SalaPrivata)")]
        public UI51PrivateRoomView HostView;
        public UI51PrivateRoomView GuestView;

        [Header("Ingresso fallito (UI51 StanzaErrore)")]
        public UI51RoomErrorView RoomError;

        public bool IsOpen => Panels.Any(p => p != null && p.activeSelf);

        /// <summary>Formato della stanza da creare (scheda Stanza privata della Home); vale anche per "Gioca online invece".</summary>
        public GameFormat Format { get => format; set => format = value; }

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
        private GameFormat format = GameFormat.OneVsOne;
        private short joinFailure; // codice di Photon dell'ultimo ingresso fallito (0 = nessuno)
        private bool joining;
        private bool busy;
        private float started;
        private float nextRefresh;
        // Photon aggiorna le proprieta' della stanza solo quando il server risponde: due tocchi rapidi
        // leggevano la stessa maschera e il secondo annullava il primo. Per un attimo vale quella locale.
        private int localBotMask;
        private float localBotMaskTime = -10f;

        private GameObject[] Panels => new[] { SearchPanel, LobbyHostPanel, LobbyGuestPanel };

        private void Awake()
        {
            foreach (var view in new[] { HostView, GuestView })
            {
                var v = view;
                v.Copy.onClick.AddListener(() => CopyCode(v));
                v.Share.onClick.AddListener(ShareCode);
            }
            HostView.StartButton.onClick.AddListener(StartMatch);
            foreach (var button in CloseButtons) if (button != null) button.onClick.AddListener(Cancel);
            RoomError.Alt.onClick.AddListener(PlayOnline);
            for (int i = 0; i < HostView.Seats.Length; i++)
            {
                int index = i;
                HostView.Seats[i].Button.onClick.AddListener(() => ToggleBot(index));
            }
            HideAll();
        }

        public static string NormalizeCode(string code) =>
            new string((code ?? "").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).Take(MatchmakingManager.RoomCodeLength).ToArray());

        public static bool IsValidCode(string code) =>
            code != null && code.Length == MatchmakingManager.RoomCodeLength && code.All(c => c >= 'A' && c <= 'Z' || c >= '0' && c <= '9');

        /// <summary>
        /// Entra subito con questo codice (scheda Stanza privata della Home, invito di un amico): si passa dritti a "Ingresso nella
        /// stanza…"; se Photon rifiuta, la finestra StanzaErrore.
        /// </summary>
        public void JoinCode(string code)
        {
            joining = true;
            busy = false;
            JoinRoom(NormalizeCode(code));
        }

        /// <summary>CREA STANZA della scheda Stanza privata: crea subito nel Format scelto li'.</summary>
        public void CreateNow()
        {
            joining = false;
            busy = false;
            CreateRoom();
        }

        /// <summary>Nome del formato della stanza, per l'invito agli amici ("1 vs 1", "2 vs 2", "1 vs 3").</summary>
        public static string FormatName(GameFormat f)
        {
            int i = System.Array.IndexOf(FormatOrder, f);
            return i >= 0 ? FormatNames[i] : "";
        }

        private MatchConfig Config()
        {
            return new MatchConfig
            {
                Intent = MatchIntent.PrivateRoom,
                Format = format,
                Rules = MatchRules.ForFormat(format)
            };
        }

        private void CreateRoom()
        {
            if (busy || Suspended()) return;
            busy = true;
            joining = false;
            started = Time.unscaledTime;
            Launcher.CreatePrivateRoom(Config(), Unblock); // stato fresco della sospensione prima di creare
        }

        private void JoinRoom(string code)
        {
            if (busy || Suspended()) return;
            if (!IsValidCode(code)) { ShowRoomError(0); return; } // la scheda lo controlla gia' (scossa); qui solo un invito malformato
            busy = true;
            joining = true;
            joinFailure = 0;
            started = Time.unscaledTime;
            Launcher.JoinPrivateRoom(code, Config(), Unblock); // stato fresco della sospensione prima di entrare
        }

        // Sospensione letta dal server mentre si partiva: la schermata Sospensione e' aperta, qui si torna liberi (niente ritorno a Modalita').
        private void Unblock()
        {
            if (this == null) return;
            busy = false;
            joining = false;
            ReturnOnCancel = null;
        }

        /// <summary>Moderazione: gioco online sospeso -> la schermata Sospensione al posto della stanza (e niente ritorno a Modalita').</summary>
        private bool Suspended()
        {
            if (!UI51SuspensionView.BlocksOnline()) return false;
            ReturnOnCancel = null;
            return true;
        }

        private void Subscribe()
        {
            if (manager != null || MatchmakingManager.Instance == null) return;
            manager = MatchmakingManager.Instance;
            manager.OnStateChanged += State;
            manager.OnJoinFailed += code => joinFailure = code;
            manager.OnError += Error;
            manager.OnRoomCreated += Created;
            manager.OnRoomJoined += Joined;
        }

        private void State(MatchmakingState state)
        {
            if (state == MatchmakingState.Idle) { busy = false; return; }
            if (state == MatchmakingState.InWaitingRoom) { Joined(); return; }
            if (state == MatchmakingState.Connecting) { started = Time.unscaledTime; busy = true; }
            searchState = state;
            Show(SearchPanel);
            RefreshSearch();
        }

        private MatchmakingState searchState;

        private void Created(string code) => Joined();

        private void Joined()
        {
            busy = false;
            ShowLobby();
            RefreshPlayers();
        }

        // Host e ospite hanno layout diversi; se l'host esce, Photon passa il ruolo a un ospite.
        private void ShowLobby() => Show(PhotonNetwork.IsMasterClient ? LobbyHostPanel : LobbyGuestPanel);

        /// <summary>Codice rifiutato da Photon: StanzaErrore; il resto (rete, tempo scaduto) nella ricerca, come per la creazione.</summary>
        public void Error(string error)
        {
            busy = false;
            if (joining && joinFailure != 0)
            {
                ShowRoomError(joinFailure);
                joinFailure = 0;
                return;
            }
            Show(SearchPanel);
            SearchView.SetStatus(ModeLabel(manager != null ? manager.CurrentConfig : null), "Connessione non riuscita", error, "", false);
            SearchView.SetSeats(false, 0, null, null);
        }

        // Mockup StanzaErrore: sotto si riapre Modalita' sulla scheda Stanza privata (il codice e' ancora scritto, RIPROVA lo lascia
        // correggere); "Gioca online invece" parte con una partita veloce nel formato della scheda.
        private void ShowRoomError(short code)
        {
            HideAll();
            var back = ReturnOnCancel;
            ReturnOnCancel = null;
            back?.Invoke();
            RoomError.Show(UI51RoomErrorView.ForCode(code));
        }

        private void PlayOnline()
        {
            var config = Config();
            config.Intent = MatchIntent.QuickMatch;
            Launcher.Launch(config);
        }

        public void Cancel()
        {
            if (Launcher != null) Launcher.CancelPendingLaunch(); // creazione o ingresso ancora in attesa del controllo della sospensione
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
            group.alpha = 0;
            // DesignCanvasFit owns the frame position and scale: fade the page only.
            panelFade = group.DOFade(1, UIV2Motion.Enter).SetUpdate(true);
        }

        private Tween panelFade;

        private void HideAll()
        {
            UIV2Motion.Cancel(ref panelFade);
            foreach (var panel in Panels)
            {
                if (panel == null) continue;
                panel.SetActive(false);
            }
        }

        private string RoomCode => PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "";

        private void CopyCode(UI51PrivateRoomView view)
        {
            GUIUtility.systemCopyBuffer = RoomCode;
            view.ShowCopied();
        }

        private void ShareCode()
        {
            string text = "Gioca a 51 con me! Apri il gioco, tocca \"Entra in stanza\" e inserisci il codice " + RoomCode;
            if (!NativeShare.ShareText(text)) UI51Toast.Show("Invito copiato: incollalo in chat ai tuoi amici", UI51Toast.Kind.Success);
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
            HostView.StartButton.interactable = false;
            manager.StartGame();
        }

        private static GameFormat RoomFormat =>
            PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MatchmakingManager.PropFormat, out var value)
                ? (GameFormat)(int)value : GameFormat.FourPlayers;

        // A coppie i primi due entrati sono compagni (SeatLayout): il posto i della griglia e' l'ordine di ingresso.
        private static bool SameTeam(GameFormat roomFormat, int joinIndex, int me) =>
            roomFormat == GameFormat.TwoVsTwo
                ? SeatLayout.SeatForJoinOrder(roomFormat, joinIndex) % 2 == SeatLayout.SeatForJoinOrder(roomFormat, me) % 2
                : joinIndex == me;

        private static string RoomModeLabel(GameFormat f) =>
            f == GameFormat.TwoVsTwo ? "2 VS 2" : f == GameFormat.FourPlayers ? "TUTTI CONTRO TUTTI" : "1 VS 1";

        /// <summary>
        /// Mockup SalaPrivata: posti in ordine di ingresso (io "Tu", HOST, bot), anello oro per la mia squadra e blu per gli altri,
        /// AVVIA PARTITA quando tutti i posti sono presi (giocatori o bot). Ogni 0,2 s mentre la sala e' aperta.
        /// </summary>
        public void RefreshPlayers()
        {
            // 2.22: finche' Photon non ha la stanza niente dati del builder (codice e posti vuoti).
            if (!PhotonNetwork.InRoom)
            {
                var config = manager != null ? manager.CurrentConfig : null;
                var f = config != null ? config.Format : GameFormat.OneVsOne;
                foreach (var v in new[] { HostView, GuestView })
                    v.Bind(RoomModeLabel(f), "", null, config != null ? config.PlayerCount : 2, false, "", FormatName(f), new string[0]);
                return;
            }
            if (PhotonNetwork.IsMasterClient != LobbyHostPanel.activeSelf) ShowLobby();

            var room = PhotonNetwork.CurrentRoom;
            var players = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
            var roomFormat = RoomFormat;
            var view = PhotonNetwork.IsMasterClient ? HostView : GuestView;
            int me = System.Array.FindIndex(players, p => p.IsLocal);
            var seats = new UI51SeatCard.Info[room.MaxPlayers];
            for (int i = 0; i < seats.Length; i++)
            {
                bool ally = SameTeam(roomFormat, i, me);
                if (i < players.Length)
                    seats[i] = new UI51SeatCard.Info
                    {
                        Name = players[i].IsLocal ? "Tu" : string.IsNullOrEmpty(players[i].NickName) ? "Giocatore" : players[i].NickName,
                        Tag = players[i].IsMasterClient ? "HOST" : null,
                        Portrait = players[i].IsLocal && HomeV2Integration.LocalAvatar != null ? HomeV2Integration.LocalAvatar
                            : HomeV2Integration.AvatarOf(players[i]) ?? view.Portrait(i),
                        Ally = ally,
                    };
                else if ((BotMask & (1 << i)) != 0)
                    seats[i] = new UI51SeatCard.Info { Name = "Bot " + (i + 1), Tag = "BOT", Portrait = view.Portrait(i), Ally = ally };
            }
            var hostPlayer = players.FirstOrDefault(p => p.IsMasterClient);
            view.Bind(RoomModeLabel(roomFormat), room.Name, seats, room.MaxPlayers, CanStart(), hostPlayer?.NickName, FormatName(roomFormat),
                players.Select(p => p.NickName).ToList());
        }

        private static string ModeLabel(MatchConfig config)
        {
            if (config != null && config.Intent == MatchIntent.PrivateRoom) return "STANZA PRIVATA";
            var f = config != null ? config.Format : GameFormat.OneVsOne;
            return (f == GameFormat.TwoVsTwo ? "PARTITA 2 VS 2" : f == GameFormat.FourPlayers ? "TUTTI CONTRO TUTTI" : "PARTITA 1 VS 1") + " · ONLINE";
        }

        /// <summary>
        /// Mockup Matchmaking: testata col tempo, posti e fondo. Partita veloce: io e chi e' entrato (in ordine di ingresso), a
        /// coppie per squadra nel 2v2 (SeatLayout); trovata, i posti rimasti vuoti sono i bot del riempimento. Stanza privata:
        /// solo la testata (i posti li mostra la sala d'attesa).
        /// </summary>
        private void RefreshSearch()
        {
            var config = manager != null ? manager.CurrentConfig : null;
            bool quick = config == null || config.Intent == MatchIntent.QuickMatch;
            bool found = searchState == MatchmakingState.Starting;
            int seconds = (int)(Time.unscaledTime - started);
            float botsIn = manager != null ? manager.QuickMatchSecondsLeft : -1f;

            string title = found ? (quick ? "Partita trovata!" : "Si parte!")
                : searchState == MatchmakingState.CreatingRoom ? "Creazione stanza…"
                : searchState == MatchmakingState.JoiningRoom ? "Ingresso nella stanza…"
                : searchState == MatchmakingState.Connecting ? "Connessione…"
                : "Cerco giocatori…";
            string sub = found ? "Il mazziere viene sorteggiato al tavolo"
                : quick ? "Tempo di attesa " + seconds / 60 + ":" + (seconds % 60).ToString("00") : "";
            string wait = found ? "Si parte da soli tra qualche secondo"
                : !quick ? ""
                : botsIn >= 0f ? "Se non arriva nessuno, tra " + Mathf.CeilToInt(botsIn) + " s si gioca coi bot"
                : "Tempo stimato circa 20 secondi";
            SearchView.SetStatus(ModeLabel(config), title, sub, wait, found);
            if (!quick) { SearchView.SetSeats(false, 0, null, null); return; }

            var f = config != null ? config.Format : GameFormat.OneVsOne;
            int max = config != null ? config.PlayerCount : 2;
            var players = PhotonNetwork.InRoom ? PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray() : new Photon.Realtime.Player[0];
            int me = System.Array.FindIndex(players, p => p.IsLocal);
            if (me < 0) { players = new[] { PhotonNetwork.LocalPlayer }; me = 0; }

            // Posto i della ricerca (righe da due): 0 sono io; nel 2v2 1 = compagno, 2-3 avversari.
            var seats = new UI51SeatCard.Info[4];
            int myTeam = f == GameFormat.TwoVsTwo ? SeatLayout.SeatForJoinOrder(f, me) % 2 : 0;
            int ally = 1, foe = f == GameFormat.TwoVsTwo ? 2 : 1;
            for (int slot = 0; slot < max; slot++)
            {
                bool bot = slot >= players.Length;
                if (bot && !found) continue;
                bool mine = f == GameFormat.TwoVsTwo ? SeatLayout.SeatForJoinOrder(f, slot) % 2 == myTeam : slot == me;
                int index = slot == me ? 0 : mine ? ally++ : foe++;
                if (index >= seats.Length) continue;
                seats[index] = bot ? SeatInfo("Bot " + (slot + 1), "Computer", slot, mine)
                    : SeatInfo(players[slot].IsLocal ? "Tu" : players[slot].NickName, LookDetail(players[slot]), slot, mine, players[slot]);
            }
            bool twoRows = f != GameFormat.OneVsOne;
            SearchView.SetSeats(true, twoRows ? 2 : 1, f == GameFormat.TwoVsTwo ? new[] { "LA TUA SQUADRA", "AVVERSARI" } : null, seats);
        }

        private UI51SeatCard.Info SeatInfo(string name, string detail, int slot, bool ally, Photon.Realtime.Player player = null) => new UI51SeatCard.Info
        {
            Name = string.IsNullOrEmpty(name) ? "Giocatore" : name,
            Detail = detail,
            Portrait = player != null && player.IsLocal && HomeV2Integration.LocalAvatar != null ? HomeV2Integration.LocalAvatar
                : HomeV2Integration.AvatarOf(player) ?? SearchView.Portrait(slot),
            Ally = ally,
        };

        // Livello dall'aspetto pubblicato in rete (ProfileCosmetics.ReadLook); senza (ospite): "Ospite".
        private static string LookDetail(Photon.Realtime.Player player) =>
            Project51.UIV2.Data.ProfileCosmetics.ReadLook(player.CustomProperties, out _, out _, out int level) ? "Liv. " + level : "Ospite";

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
            UIV2Motion.Cancel(ref panelFade);
            if (manager == null) return;
            manager.OnStateChanged -= State;
            manager.OnError -= Error;
            manager.OnRoomCreated -= Created;
            manager.OnRoomJoined -= Joined;
        }
    }
}
