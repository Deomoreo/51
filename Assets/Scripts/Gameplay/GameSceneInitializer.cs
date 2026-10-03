using UnityEngine;
using Project51.Core;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Unity
{
    /// <summary>
    /// Inizializza la scena di gioco basandosi sulla MatchConfig.
    /// Va messo nella scena di gioco.
    /// Non dipende direttamente da Networking per evitare dipendenze cicliche.
    /// 
    /// IMPORTANT: This script should execute BEFORE TurnController.
    /// Set Script Execution Order in Unity: GameSceneInitializer = -100, TurnController = 0
    /// </summary>
    [DefaultExecutionOrder(-100)] // Execute before other scripts
    public class GameSceneInitializer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurnController turnController;

        [Header("Settings")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float multiplayerStartDelay = 0.5f;

        private MatchConfig _config;

        /// <summary>
        /// Partita guidata (UI51 Fase 12): la prossima partita di allenamento parte da una distribuzione nota
        /// (TutorialSeed: mazziere il bot; tu di mano con Asso di denari, 7 e 2 di bastoni; in tavolo Asso di bastoni,
        /// Re di coppe, 4 di spade e 3 di coppe; nessun accuso, niente 15/30). La mette UI51TutorialView.Launch e la consuma StartGame.
        /// </summary>
        public static bool Tutorial { get; set; }
        public const int TutorialSeed = 759353;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTutorial() => Tutorial = false;

        /// <summary>
        /// Latest loaded match config for the active game scene.
        /// Useful for gameplay systems that need rules tweaks.
        /// </summary>
        public static MatchConfig ActiveConfig { get; private set; }

        private void Awake()
        {
            // Carica la configurazione dai PlayerPrefs usando il helper in Core
            _config = MatchConfigStorage.Load();
            ActiveConfig = _config;
            Debug.Log($"[GameSceneInitializer] Loaded config: {_config}");

            EnsureResponsiveCamera();

            // Configura il GameModeService
            SetupGameModeProvider();
        }

        private void Start()
        {
            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
            }

            if (autoStart && turnController != null)
            {
                if (_config.Intent == MatchIntent.Training)
                {
                    // Training: avvia subito
                    StartGame();
                }
                else
                {
                    // Multiplayer: solo il Master Client avvia
                    if (IsMasterClient())
                    {
                        Invoke(nameof(StartGame), multiplayerStartDelay);
                    }
                    // I client non-master riceveranno il GameState via NetworkGameController
                }
            }
        }

        /// <summary>
        /// Ricalcola il provider multiplayer (indice locale/master/bot) da capo.
        /// </summary>
        /// <remarks>
        /// SetupGameModeProvider() viene chiamato una prima volta in Awake(), prestissimo nel
        /// ciclo di vita della scena. Su un device reale, con latenza di rete piu' alta della LAN/
        /// localhost usata in Editor/ParrelSync, PhotonNetwork.PlayerList potrebbe non essere ancora
        /// completamente sincronizzato in quel preciso istante: l'indice locale calcolato allora puo'
        /// risultare sbagliato (es. sempre 0 come il Master), causando lo stesso identico stato/mano
        /// mostrato su piu' client. NetworkGameController richiama questo metodo pubblico ogni volta
        /// che applica un GameState ricevuto dalla rete: a quel punto la connessione e la room sono
        /// per forza gia' del tutto stabilite (altrimenti l'RPC stesso non sarebbe arrivato).
        /// </remarks>
        public void RefreshMultiplayerGameModeProvider()
        {
            if (_config != null && _config.Intent != MatchIntent.Training)
            {
                LockStableActorRosterIfNeeded();
                SetupGameModeProvider();
            }
        }

        /// <summary>
        /// Foto STABILE (ActorNumber, ordinata) di tutti i giocatori presenti nella room quando la
        /// partita e' realmente iniziata. Fissata una sola volta e mai piu' ricalcolata da qui in
        /// avanti: sia GetLocalPlayerIndex() sia GetPlayerIndexForActor() la usano per garantire lo
        /// STESSO indice di posto per lo STESSO giocatore per tutta la durata della partita, anche
        /// dopo che qualcuno si disconnette (PhotonNetwork.PlayerList si accorcia quando un
        /// giocatore lascia la room: ricalcolare l'ordinamento su quella lista live avrebbe fatto
        /// "scalare" gli indici di TUTTI i giocatori rimasti dopo il primo che se ne va, corrompendo
        /// silenziosamente la mappatura mano/posto per chiunque restasse in partita).
        /// </summary>
        private List<int> _stableActorOrder;

        /// <summary>
        /// Indici (posti) convertiti in bot perche' il giocatore reale si e' disconnesso A PARTITA
        /// GIA' AVVIATA. Va tenuto separato dal calcolo "posti riempiti con bot all'avvio" in
        /// SetupGameModeProvider() (quello assume che i posti mancanti siano sempre gli ULTIMI
        /// indici, cosa falsa per una disconnessione a meta' partita di un giocatore qualsiasi) e
        /// va sempre riunito ai bot calcolati li', per sopravvivere a qualunque ricostruzione del
        /// provider (nuova mano, resync).
        /// </summary>
        private readonly HashSet<int> _disconnectedPlayerIndices = new HashSet<int>();
        // Posti tolti dal master per inattivita' (NetworkGameController.RPC_SeatInactive): restano al bot anche se quel telefono rientra.
        private readonly HashSet<int> _inactiveSeats = new HashSet<int>();

        private const string RosterKey = "roster";
        private const string InactiveKey = "inattivo";

        private void LockStableActorRosterIfNeeded()
        {
            if (_stableActorOrder != null) return; // fissata una volta sola, mai risovrascritta
            if (!PhotonNetwork.InRoom) return;

            // Rientro dopo il riavvio dell'app: chi nel frattempo e' uscito non e' piu' nella lista e i posti scalerebbero.
            // Vale l'ordine fissato dal master all'inizio, salvato nella stanza (il master lo ricalcola sempre e lo riscrive).
            var room = PhotonNetwork.CurrentRoom;
            if (!PhotonNetwork.IsMasterClient && room.CustomProperties.TryGetValue(RosterKey, out object saved) && saved is int[] actors && actors.Length > 0)
            {
                _stableActorOrder = actors.ToList();
                return;
            }
            var players = new List<Photon.Realtime.Player>(PhotonNetwork.PlayerList);
            players.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));
            _stableActorOrder = players.Select(p => p.ActorNumber).ToList();
            if (PhotonNetwork.IsMasterClient)
                room.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { RosterKey, _stableActorOrder.ToArray() } });
        }

        /// <summary>
        /// Mappa l'ActorNumber di un giocatore Photon al suo indice/posto nel GameState, usando la
        /// foto stabile presa a inizio partita. Funziona anche per un giocatore che ha GIA' lasciato
        /// la room (es. da OnPlayerLeftRoom, dove PhotonNetwork.PlayerList non lo contiene piu').
        /// Ritorna -1 se non fa parte del roster iniziale di questa partita.
        /// </summary>
        public int GetPlayerIndexForActor(int actorNumber)
        {
            int joinIndex = _stableActorOrder?.IndexOf(actorNumber) ?? -1;
            return SeatLayout.SeatForJoinOrder(_config.Format, joinIndex);
        }

        /// <summary>Posti dei giocatori reali di inizio partita con il loro ActorNumber (vittoria per abbandono, MatchResultsV2).</summary>
        public IEnumerable<(int seat, int actor)> Roster() =>
            (_stableActorOrder ?? new List<int>()).Select((actor, join) => (SeatLayout.SeatForJoinOrder(_config.Format, join), actor));

        /// <summary>
        /// Dopo il NOSTRO rientro in stanza: mentre eravamo scollegati possono essere usciti o
        /// rientrati altri giocatori senza che ricevessimo gli eventi. Riallinea i posti bot con chi
        /// e' davvero presente (attivo) nella stanza.
        /// </summary>
        public void SyncSeatsWithRoom()
        {
            if (_stableActorOrder == null || !PhotonNetwork.InRoom) return;
            for (int seat = 0; seat < 4; seat++) // tolti mentre eravamo scollegati
                if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(InactiveKey + seat, out object removed) && removed is bool yes && yes)
                    _inactiveSeats.Add(seat);
            for (int join = 0; join < _stableActorOrder.Count; join++)
            {
                int seat = SeatLayout.SeatForJoinOrder(_config.Format, join);
                var player = PhotonNetwork.CurrentRoom.GetPlayer(_stableActorOrder[join]);
                if (player != null && !player.IsInactive && !_inactiveSeats.Contains(seat)) _disconnectedPlayerIndices.Remove(seat);
                else _disconnectedPlayerIndices.Add(seat);
            }
            RefreshMultiplayerGameModeProvider();
        }

        /// <summary>Posto ceduto a un bot per una disconnessione a partita avviata (icona sul banner).</summary>
        public bool IsDisconnected(int playerIndex) => _disconnectedPlayerIndices.Contains(playerIndex);

        public bool IsRemovedForInactivity(int playerIndex) => _inactiveSeats.Contains(playerIndex);

        /// <summary>3 tempi scaduti di fila: il posto passa al bot per il resto della partita (anche se quel telefono rientra).</summary>
        public void MarkPlayerInactive(int playerIndex, bool persist)
        {
            if (playerIndex < 0 || !_inactiveSeats.Add(playerIndex)) return;
            // Nella stanza per chi era scollegato quando e' successo (SyncSeatsWithRoom al rientro, anche dopo un riavvio).
            if (persist && PhotonNetwork.InRoom) // una chiave per posto: due arbitri non si sovrascrivono
                PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { InactiveKey + playerIndex, true } });
            MarkPlayerDisconnected(playerIndex);
        }

        /// <summary>
        /// Arbitro del turno di questo posto (scelta dell'utente 02/10): il master; ma se il posto lo gioca il master stesso (il suo, o un
        /// bot) e' il giocatore presente con l'ActorNumber piu' basso dopo di lui, cosi' anche un master fermo non blocca il tavolo.
        /// Senza altri giocatori presenti resta il master. -1 fuori stanza.
        /// </summary>
        public int RefereeActorFor(int seat)
        {
            var room = PhotonNetwork.CurrentRoom;
            if (room == null) return -1;
            int master = room.MasterClientId;
            bool playedByMaster = GameModeService.Current.IsBotPlayer(seat) || GetPlayerIndexForActor(master) == seat;
            if (!playedByMaster || _stableActorOrder == null) return master;
            int referee = int.MaxValue;
            for (int join = 0; join < _stableActorOrder.Count; join++)
            {
                int actor = _stableActorOrder[join];
                var player = room.GetPlayer(actor);
                if (actor == master || player == null || player.IsInactive || _inactiveSeats.Contains(SeatLayout.SeatForJoinOrder(_config.Format, join))) continue;
                referee = Mathf.Min(referee, actor);
            }
            return referee == int.MaxValue ? master : referee;
        }

        /// <summary>Il posto del master e' stato tolto per inattivita' ma il suo telefono e' ancora nella stanza (e resta master).</summary>
        public bool MasterRemoved => PhotonNetwork.InRoom && _inactiveSeats.Contains(GetPlayerIndexForActor(PhotonNetwork.CurrentRoom.MasterClientId));

        /// <summary>Questo telefono fa da arbitro per il posto (fuori stanza, nelle prove in Editor: il master).</summary>
        public bool IsLocalReferee(int seat) =>
            PhotonNetwork.InRoom ? RefereeActorFor(seat) == PhotonNetwork.LocalPlayer.ActorNumber : GameModeService.Current.IsMasterClient;

        // Tempi scaduti di fila per posto: nella stanza ("fermi" + posto, un solo arbitro scrive ogni posto), cosi' restano se cambia
        // il master. La copia locale serve fuori stanza (prove in Editor).
        // ponytail: si legge la stanza, che si aggiorna un attimo dopo la scrittura; due tempi scaduti dello stesso posto sono a turni di distanza.
        private const string StrikesKey = "fermi";
        private readonly int[] _strikes = new int[4];

        public int Strikes(int seat)
        {
            if (seat < 0 || seat >= _strikes.Length) return 0;
            var room = PhotonNetwork.CurrentRoom;
            return room != null && room.CustomProperties.TryGetValue(StrikesKey + seat, out object saved) && saved is int n ? n : _strikes[seat];
        }

        public void SetStrikes(int seat, int value)
        {
            if (seat < 0 || seat >= _strikes.Length) return;
            _strikes[seat] = value;
            if (PhotonNetwork.InRoom)
                PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { StrikesKey + seat, value } });
        }

        /// <summary>Partita nuova: tutti a zero (nella stanza lo scrive il master).</summary>
        public void ClearStrikes()
        {
            for (int seat = 0; seat < _strikes.Length; seat++)
            {
                _strikes[seat] = 0;
                if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(StrikesKey + seat))
                    PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { StrikesKey + seat, 0 } });
            }
        }

        /// <summary>
        /// Il giocatore che occupava questo posto e' rientrato nella stanza (entro PlayerTtl): il bot
        /// che lo sostituiva gli restituisce posto e carte. Chiamato su ogni client da
        /// NetworkGameController.OnPlayerEnteredRoom, come MarkPlayerDisconnected.
        /// </summary>
        public void MarkPlayerReconnected(int playerIndex)
        {
            if (_inactiveSeats.Contains(playerIndex) || !_disconnectedPlayerIndices.Remove(playerIndex)) return;

            Debug.Log($"[GameSceneInitializer] Player at seat {playerIndex} rejoined: bot released.");

            RefreshMultiplayerGameModeProvider();

            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
            }
            turnController?.OnPlayerReconnected(playerIndex);
        }

        /// <summary>
        /// Converte permanentemente un posto in bot perche' il giocatore reale che lo occupava si e'
        /// disconnesso a partita gia' avviata. Chiamato in modo indipendente (e identico) su OGNI
        /// client da NetworkGameController.OnPlayerLeftRoom (evento Photon consegnato a tutti i
        /// membri della room, non serve nessun RPC dedicato): ognuno aggiorna il proprio
        /// GameModeService.Current locale, cosi' che UI e regole di IsHumanPlayer/IsBotPlayer
        /// restino coerenti ovunque. Solo il Master Client user CirullaAI per giocare davvero le
        /// mosse di quel posto (vedi TurnController.OnPlayerConvertedToBot), esattamente come per i
        /// bot gia' presenti dall'inizio.
        /// </summary>
        public void MarkPlayerDisconnected(int playerIndex)
        {
            if (playerIndex < 0) return;
            if (!_disconnectedPlayerIndices.Add(playerIndex)) return; // gia' segnato

            Debug.LogWarning($"[GameSceneInitializer] Player at seat {playerIndex} disconnected mid-match: converting to bot.");

            RefreshMultiplayerGameModeProvider();

            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
            }
            turnController?.OnPlayerConvertedToBot(playerIndex);
        }

        private void SetupGameModeProvider()
        {
            IGameModeProvider provider;

            if (_config.Intent == MatchIntent.Training)
            {
                // Training: player 0 � umano, gli altri sono bot
                provider = SinglePlayerProvider.Instance;
                Debug.Log($"[GameSceneInitializer] Setup Training mode with {_config.PlayerCount} players, difficulty: {_config.BotDifficulty}");
            }
            else
            {
                // Multiplayer: determina l'indice locale
                int localIndex = GetLocalPlayerIndex();
                bool isMaster = IsMasterClient();

                // Se l'host ha avviato la stanza privata con meno giocatori reali di quanti
                // ne servano per il formato scelto (es. formato 4 giocatori ma stanza chiusa a 2),
                // i posti mancanti vengono riempiti con bot. I posti reali occupano sempre gli indici
                // piu' bassi (stesso ordine di PhotonNetwork.PlayerList su tutti i client), quindi
                // i bot sono semplicemente gli indici da "giocatori reali" a "PlayerCount-1".
                //
                // IMPORTANTE: questo conteggio va congelato al numero di giocatori presenti quando
                // la partita e' iniziata (_stableActorOrder.Count), NON ricalcolato da
                // GetRealPlayerCountInRoom() (PhotonNetwork.CurrentRoom.PlayerCount live) ogni volta
                // che il provider viene ricostruito (nuova mano, resync). Altrimenti, dopo che un
                // qualsiasi giocatore reale si disconnette a meta' partita, PlayerCount si riduce e
                // questo calcolo trasformerebbe in bot un ULTERIORE posto (sempre l'ultimo indice),
                // anche se il giocatore che lo occupa e' ancora presente e sta giocando - la vera
                // disconnessione va gestita SOLO tramite _disconnectedPlayerIndices qui sotto.
                int realPlayers = _stableActorOrder != null
                    ? Mathf.Clamp(_stableActorOrder.Count, 1, _config.PlayerCount)
                    : Mathf.Clamp(GetRealPlayerCountInRoom(), 1, _config.PlayerCount);
                // I posti dei giocatori reali dipendono dal formato (a coppie: vedi SeatLayout).
                var botIndices = SeatLayout.BotSeats(_config.Format, _config.PlayerCount, realPlayers);

                // Posti convertiti in bot per disconnessione REALE a partita avviata (vedi
                // MarkPlayerDisconnected): si sommano sempre, indipendentemente da quale indice sia.
                foreach (var idx in _disconnectedPlayerIndices)
                    botIndices.Add(idx);

                provider = new MultiplayerGameModeProvider(localIndex, _config.PlayerCount, isMaster, botIndices);
                Debug.Log($"[GameSceneInitializer] Setup Multiplayer mode, local index: {localIndex}, isMaster: {isMaster}, realPlayers: {realPlayers}, botSeats: {botIndices.Count}");
            }

            GameModeService.Current = provider;
        }

        /// <summary>
        /// Verifica se siamo il Master Client.
        /// </summary>
        /// <remarks>
        /// Prima usava reflection su "Photon.Pun.PhotonNetwork, PhotonUnityNetworking" "per evitare
        /// dipendenza da Photon" - ma Project51.Gameplay.asmdef referenzia gia' PhotonUnityNetworking
        /// direttamente (necessario altrove in questo stesso assembly), quindi la reflection era
        /// inutile E rischiosa: su build IL2CPP (es. APK Android) i metadati di reflection possono
        /// essere rimossi dallo stripping anche per tipi usati altrove nel programma, causando un
        /// fallimento silenzioso -> fallback "assume master" -> il client avviava una PROPRIA
        /// partita locale invece di aspettare quella dell'host (partita diversa su device reale
        /// rispetto a Editor/clone, dove la reflection funzionava per caso).
        /// </remarks>
        private bool IsMasterClient()
        {
            if (_config.Intent == MatchIntent.Training)
                return true;

            return PhotonNetwork.IsMasterClient;
        }

        /// <summary>
        /// Verifica se siamo in una room Photon.
        /// </summary>
        private bool IsInRoom()
        {
            return PhotonNetwork.InRoom;
        }

        /// <summary>
        /// Numero di giocatori reali attualmente nella room Photon.
        /// Se non siamo in una room, assume che tutti i posti siano reali (nessun bot).
        /// </summary>
        private int GetRealPlayerCountInRoom()
        {
            if (!IsInRoom())
                return _config.PlayerCount;

            return PhotonNetwork.CurrentRoom.PlayerCount;
        }

        /// <summary>
        /// Ottiene l'indice del player locale nella room Photon.
        /// </summary>
        /// <remarks>
        /// Fondamentale che TUTTI i client calcolino lo STESSO indice per lo STESSO giocatore
        /// reale (e' la base per sapere quale mano dello GameState condiviso e' "la propria").
        /// PhotonNetwork.PlayerList non garantisce esplicitamente un ordine identico su ogni
        /// client (dipende dall'ordine di iscrizione/replica locale); ActorNumber invece e'
        /// assegnato dal server, univoco e identico per tutti - ordiniamo esplicitamente su
        /// quello per avere una mappatura stabile e coerente su ogni device.
        /// </remarks>
        private int _lockedLocalSeat = -1;

        private int GetLocalPlayerIndex()
        {
            // Durante una riconnessione non siamo nella room, ma il nostro posto non cambia.
            if (!IsInRoom())
                return _lockedLocalSeat >= 0 ? _lockedLocalSeat : 0;
            int seat = ComputeLocalPlayerIndex();
            if (_stableActorOrder != null) _lockedLocalSeat = seat;
            return seat;
        }

        private int ComputeLocalPlayerIndex()
        {

            // Preferisci sempre la foto stabile, se gia' fissata: ricalcolare l'ordinamento sulla
            // PhotonNetwork.PlayerList LIVE dopo che un qualsiasi giocatore ha lasciato la room
            // farebbe "scalare" l'indice di TUTTI i giocatori rimasti con ActorNumber maggiore del
            // suo, cambiando silenziosamente il posto/mano di chi sta ancora giocando.
            if (_stableActorOrder != null)
            {
                int idx = _stableActorOrder.IndexOf(PhotonNetwork.LocalPlayer.ActorNumber);
                if (idx >= 0) return SeatLayout.SeatForJoinOrder(_config.Format, idx);
                // Fallback estremo: il locale non e' nella foto iniziale (non dovrebbe succedere
                // per chi sta gia' giocando una partita in corso) - prosegue col calcolo live sotto.
            }

            var players = new List<Photon.Realtime.Player>(PhotonNetwork.PlayerList);
            players.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].IsLocal)
                    return SeatLayout.SeatForJoinOrder(_config.Format, i);
            }

            return 0;
        }

        private void StartGame()
        {
            if (turnController == null)
            {
                Debug.LogError("[GameSceneInitializer] TurnController not found!");
                return;
            }

            Debug.Log("[GameSceneInitializer] Starting new game...");

            // Ricalcola il mapping locale subito prima di generare davvero la partita (stesso motivo
            // di TurnController.SetNetworkGameState: qui siamo il Master, quindi meno a rischio, ma
            // e' comunque un'operazione economica e mantiene i due percorsi coerenti).
            RefreshMultiplayerGameModeProvider();

            // In multiplayer e' TurnController.StartNewGame a inviare lo stato agli altri client
            // (prima veniva inviato una seconda volta anche da qui).
            bool tutorial = Tutorial && IsTrainingMode;
            Tutorial = false;
            if (tutorial) Rules51.Reseed(TutorialSeed);
            turnController.StartNewGame();
            if (tutorial) Rules51.Reseed(System.Environment.TickCount); // le smazzate dopo tornano casuali
        }

        private void EnsureResponsiveCamera()
        {
            Camera targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindObjectOfType<Camera>();
            }

            if (targetCamera == null)
            {
                Debug.LogWarning("[GameSceneInitializer] No camera found for responsive gameplay layout.");
                return;
            }

            targetCamera.orthographic = true;

            var responsiveFit = targetCamera.GetComponent("CameraResponsiveFit") as MonoBehaviour;
            if (responsiveFit == null)
            {
                var responsiveType = System.Type.GetType("Project51.Unity.CameraResponsiveFit, Project51.Gameplay");
                if (responsiveType == null)
                {
                    Debug.LogWarning("[GameSceneInitializer] CameraResponsiveFit type not available yet. A Unity/VS refresh may be required.");
                    return;
                }

                responsiveFit = targetCamera.gameObject.AddComponent(responsiveType) as MonoBehaviour;
            }

            var applyMethod = responsiveFit.GetType().GetMethod("Apply");
            applyMethod?.Invoke(responsiveFit, null);
        }

        /// <summary>
        /// Restituisce se siamo in modalit� training (vs bot).
        /// </summary>
        public bool IsTrainingMode => _config?.Intent == MatchIntent.Training;
    }
}
