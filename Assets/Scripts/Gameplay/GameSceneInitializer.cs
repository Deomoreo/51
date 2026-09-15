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

        // Cache per evitare FindObjectOfType ripetuti
        private MonoBehaviour _networkController;
        private bool _networkControllerSearched;

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

        private void LockStableActorRosterIfNeeded()
        {
            if (_stableActorOrder != null) return; // fissata una volta sola, mai risovrascritta
            if (!PhotonNetwork.InRoom) return;

            var players = new List<Photon.Realtime.Player>(PhotonNetwork.PlayerList);
            players.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));
            _stableActorOrder = players.Select(p => p.ActorNumber).ToList();
        }

        /// <summary>
        /// Mappa l'ActorNumber di un giocatore Photon al suo indice/posto nel GameState, usando la
        /// foto stabile presa a inizio partita. Funziona anche per un giocatore che ha GIA' lasciato
        /// la room (es. da OnPlayerLeftRoom, dove PhotonNetwork.PlayerList non lo contiene piu').
        /// Ritorna -1 se non fa parte del roster iniziale di questa partita.
        /// </summary>
        public int GetPlayerIndexForActor(int actorNumber)
        {
            return _stableActorOrder?.IndexOf(actorNumber) ?? -1;
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
                provider = new TrainingGameModeProvider(_config.PlayerCount, _config.BotDifficulty);
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
                var botIndices = new HashSet<int>();
                int realPlayers = _stableActorOrder != null
                    ? Mathf.Clamp(_stableActorOrder.Count, 1, _config.PlayerCount)
                    : Mathf.Clamp(GetRealPlayerCountInRoom(), 1, _config.PlayerCount);
                for (int i = realPlayers; i < _config.PlayerCount; i++)
                    botIndices.Add(i);

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
        private int GetLocalPlayerIndex()
        {
            if (!IsInRoom())
                return 0;

            // Preferisci sempre la foto stabile, se gia' fissata: ricalcolare l'ordinamento sulla
            // PhotonNetwork.PlayerList LIVE dopo che un qualsiasi giocatore ha lasciato la room
            // farebbe "scalare" l'indice di TUTTI i giocatori rimasti con ActorNumber maggiore del
            // suo, cambiando silenziosamente il posto/mano di chi sta ancora giocando.
            if (_stableActorOrder != null)
            {
                int idx = _stableActorOrder.IndexOf(PhotonNetwork.LocalPlayer.ActorNumber);
                if (idx >= 0) return idx;
                // Fallback estremo: il locale non e' nella foto iniziale (non dovrebbe succedere
                // per chi sta gia' giocando una partita in corso) - prosegue col calcolo live sotto.
            }

            var players = new List<Photon.Realtime.Player>(PhotonNetwork.PlayerList);
            players.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].IsLocal)
                    return i;
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

            turnController.StartNewGame();

            // In multiplayer, invia il GameState agli altri client
            if (_config.Intent != MatchIntent.Training && IsMasterClient())
            {
                SendInitialGameStateToClients();
            }
        }

        /// <summary>
        /// Invia il GameState ai client usando reflection per evitare dipendenza ciclica.
        /// </summary>
        private void SendInitialGameStateToClients()
        {
            if (turnController == null || turnController.GameState == null)
                return;

            // Cerca NetworkGameController senza import diretto
            if (!_networkControllerSearched)
            {
                _networkControllerSearched = true;
                
                // Cerca il tipo NetworkGameController. NOTA: l'assembly "Project51.Networking" non
                // esiste piu' (l'asmdef dedicato e' stato rimosso in precedenza) - gli script in
                // Assets/Scripts/Networking compilano ora nell'assembly di default "Assembly-CSharp".
                // Con il nome vecchio Type.GetType tornava sempre null: l'host non mandava MAI lo
                // stato iniziale della partita agli altri client, che restavano con il tavolo vuoto.
                var ngcType = System.Type.GetType("Project51.Networking.NetworkGameController, Assembly-CSharp");
                if (ngcType != null)
                {
                    _networkController = FindObjectOfType(ngcType) as MonoBehaviour;
                }
            }

            if (_networkController != null)
            {
                // Chiama SendInitialGameState via reflection
                var method = _networkController.GetType().GetMethod("SendInitialGameState");
                if (method != null)
                {
                    method.Invoke(_networkController, new object[] { turnController.GameState });
                    Debug.Log("[GameSceneInitializer] Sent initial GameState to clients");
                }
            }
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
        /// Restituisce la configurazione corrente del match.
        /// </summary>
        public MatchConfig GetConfig() => _config;

        /// <summary>
        /// Restituisce se siamo in modalit� training (vs bot).
        /// </summary>
        public bool IsTrainingMode => _config?.Intent == MatchIntent.Training;

        /// <summary>
        /// Restituisce se siamo in modalit� multiplayer.
        /// </summary>
        public bool IsMultiplayerMode => _config?.Intent != MatchIntent.Training;
    }
}
