using UnityEngine;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

namespace Project51.Unity
{
    /// <summary>
    /// Main controller for a Cirulla/51 game session in Unity.
    /// Manages the core game state and turn flow.
    /// </summary>
    public class TurnController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private MoveSelectionUI moveSelectionUI;
        [SerializeField] private RoundEndPanel roundEndPanel;
        
        [Header("Managers (Optional - Auto-Find)")]
        [SerializeField] private CardViewManager cardViewManager;
        [SerializeField] private CapturedPileManager capturedPileManager;
        [SerializeField] private CardAnimationController cardAnimationController;
        
        /// <summary>
        /// Sets the CardViewManager reference at runtime. Useful for tests.
        /// </summary>
        public void SetCardViewManager(CardViewManager manager)
        {
            cardViewManager = manager;
        }

        /// <summary>
        /// Imposta lo stato di gioco ricevuto dalla rete e inizializza RoundManager per il client.
        /// Sostituisce il precedente accesso via reflection da NetworkGameController.
        /// </summary>
        public void SetNetworkGameState(GameState newGameState)
        {
            // Ricalcola l'indice locale/masterClient/bot ORA, non fidandosi di quello calcolato al
            // primissimo Awake() della scena: su un device reale (rete piu' lenta della LAN/localhost
            // dell'Editor) PhotonNetwork.PlayerList poteva non essere ancora del tutto sincronizzato
            // in quel momento, facendo risultare l'indice locale sbagliato (es. sempre 0 come il
            // Master -> stessa identica mano mostrata su piu' client). Qui l'arrivo stesso di questo
            // GameState via RPC garantisce che la connessione/room siano gia' del tutto stabilite.
            var gsi = FindObjectOfType<GameSceneInitializer>();
            gsi?.RefreshMultiplayerGameModeProvider();

            // Il Master rimanda lo stesso stato se la richiesta del client si incrocia con l'invio iniziale:
            // l'introduzione gia' in corso non deve ripartire da capo.
            var provider = GameModeService.Current;
            bool networkClient = provider.IsMultiplayer && !provider.IsMasterClient;
            bool freshSmazzata = networkClient && RoundManager.IsFreshSmazzata(newGameState);
            string stateText = freshSmazzata ? GameStateSerializer.Serialize(newGameState) : null;
            if (freshSmazzata && stateText == lastNetworkIntroState && gameState != null)
            {
                return;
            }
            lastNetworkIntroState = stateText;

            gameState = newGameState;
            pendingDealerAccuso = null;
            hiddenCapturedPlayer = -1;
            hiddenCapturedCount = 0;
            CreateRoundManager();

            // Un GameState "fresco" dal Master rende irrilevante (anzi pericolosa, se applicata
            // fuori ordine su uno stato diverso) qualunque mossa di rete accodata in precedenza:
            // scartiamo la coda e ripartiamo puliti. Azzeriamo anche i flag di animazione/redeal:
            // se questo client stava rianimando una mossa vecchia quando e' arrivato un resync
            // (es. dopo RequestNetworkResync), quello stato locale non ha piu' senso.
            pendingNetworkMoves.Clear();
            isMoveAnimationInProgress = false;
            isRedealPendingVisual = false;
            isRedealAnimationInProgress = false;
            pendingRedealVisualCopies.Clear();
            isAccusoWindowOpen = false;
            accusoWindowSecondsRemaining = 0f;
            accusoAlreadyResolvedThisHand.Clear();

            RefreshValidMoves();
            if (freshSmazzata)
            {
                // Smazzata nuova: il client vede la stessa sequenza del Master (roulette, distribuzione
                // dal mazziere, accuso del mazziere, finestra Accuso). Le mosse di rete che arrivano
                // nel frattempo vengono accodate (isRedealPendingVisual) e applicate subito dopo.
                if (RoundManager.TryGetDealerAccusoAtStart(gameState, out var dealerAccuso, out var sweptCards))
                {
                    HandleDealerAccusoDeclared(gameState.DealerIndex, dealerAccuso, sweptCards);
                }
                StageInitialDealAndStartIntro();
            }
            else
            {
                cardViewManager?.ForceRefresh();
            }
            OnMoveExecuted?.Invoke(null);
        }

        /// <summary>
        /// Chiamato da GameSceneInitializer.MarkPlayerDisconnected() quando un giocatore reale si
        /// disconnette a partita gia' avviata e il suo posto e' stato appena convertito in bot nel
        /// GameModeService.Current locale (indipendentemente su ogni client, stesso mapping di posti
        /// per tutti - vedi GameSceneInitializer._stableActorOrder).
        /// </summary>
        /// <remarks>
        /// Serve un aggancio esplicito perche' altrimenti, se il turno era GIA' fermo in attesa di
        /// quel giocatore (caso tipico: la partita "si blocca" dopo la disconnessione), nessun altro
        /// evento avrebbe mai fatto ripartire il gioco: la logica "se il prossimo giocatore e' un
        /// bot, fai giocare l'IA" in ApplyMoveInternal scatta solo in reazione a una mossa APPENA
        /// eseguita, non quando lo stato resta semplicemente fermo. Se invece la disconnessione
        /// avviene mentre e' gia' in corso l'animazione di un'altra mossa, non serve fare nulla qui:
        /// quando quell'animazione finira', ApplyMoveInternal rilevera' da solo (con IsHumanPlayerTurn
        /// ormai aggiornato) che il turno successivo va giocato da un bot.
        /// </remarks>
        public void OnPlayerConvertedToBot(int playerIndex)
        {
            RefreshValidMoves();
            cardViewManager?.ForceRefresh();

            var provider = GameModeService.Current;
            if (!provider.IsMultiplayer || !provider.IsMasterClient) return;
            if (gameState == null || gameState.RoundEnded) return;
            if (gameState.CurrentPlayerIndex != playerIndex) return;
            if (isMoveAnimationInProgress || isRedealPendingVisual || isRedealAnimationInProgress) return;

            CancelInvoke(nameof(ExecuteAITurn));
            Invoke(nameof(ExecuteAITurn), GamePreferences.Scaled(aiMoveDelay));
        }

        /// <summary>
        /// Chiamato da NetworkGameController.OnMasterClientSwitched quando QUESTO client diventa il
        /// nuovo Master Client per migrazione automatica di Photon (il master precedente si e'
        /// disconnesso). Se il turno corrente era gia' di un bot, rimasto fermo in attesa che il
        /// vecchio master lo giocasse, nessun altro evento lo rimetterebbe in moto da solo - stesso
        /// identico motivo di OnPlayerConvertedToBot qui sopra.
        /// </summary>
        /// <summary>
        /// Il giocatore di questo posto e' rientrato: il bot smette di giocare per lui. Se un turno
        /// IA era gia' programmato, ExecuteAITurn lo scarta da solo (il posto e' di nuovo umano).
        /// </summary>
        public void OnPlayerReconnected(int playerIndex)
        {
            RefreshValidMoves();
            cardViewManager?.ForceRefresh();
        }

        public void OnBecameMasterClient()
        {
            var provider = GameModeService.Current;
            if (!provider.IsMultiplayer || !provider.IsMasterClient) return;
            if (gameState == null || gameState.RoundEnded) return;
            if (isMoveAnimationInProgress || isRedealPendingVisual || isRedealAnimationInProgress) return;
            if (!provider.IsBotPlayer(gameState.CurrentPlayerIndex)) return;

            CancelInvoke(nameof(ExecuteAITurn));
            Invoke(nameof(ExecuteAITurn), GamePreferences.Scaled(aiMoveDelay));
        }

        [Header("Game Settings")]
        [SerializeField] private bool autoStartGame = false;
        
        [Header("AI Settings")]
        [SerializeField] private float aiMoveDelay = 2.0f;
        [SerializeField] private AIDifficulty aiDifficulty = AIDifficulty.Medium;

        [Header("Redeal Animation")]
        [SerializeField] private float redealStartDelay = 0.2f;

        [Header("Played Card Feedback")]
        [SerializeField] private float playedCardCrossfadeDuration = 0.08f;

        private GameState gameState;
        private List<Move> currentValidMoves;
        private RoundManager roundManager;
        private CirullaAI cirullaAI;
        private bool isMoveAnimationInProgress;
        private bool isRedealPendingVisual;
        private bool isRedealAnimationInProgress;
        private readonly List<Transform> pendingRedealVisualCopies = new List<Transform>();
        private List<CardViewManager.StagedCard> pendingInitialHandStagedCards;
        private List<CardViewManager.StagedCard> pendingInitialTableStagedCards;
        private (int dealerIndex, AccusoType type, List<Card> sweptCards)? pendingDealerAccuso;
        [SerializeField] private DealerAccusoRevealController dealerAccusoRevealController;
        private Coroutine introCoroutine;
        /// <summary>Ultimo stato "appena distribuito" ricevuto dal Master: un doppione non rifa' l'introduzione.</summary>
        private string lastNetworkIntroState;

        /// <summary>RoundManager con gli stessi agganci per il Master e per i client in rete.</summary>
        private void CreateRoundManager()
        {
            roundManager = new RoundManager(gameState);
            roundManager.OnNewHandsDealt += HandleNewHandsDealt; // redeal a meta' smazzata con distribuzione animata
            roundManager.OnDealerAccusoDeclared += HandleDealerAccusoDeclared;
        }

        private void HandleDealerAccusoDeclared(int dealerIndex, AccusoType type, List<Card> sweptCards)
        {
            pendingDealerAccuso = (dealerIndex, type, sweptCards);
            // Il mazzetto del mazziere non deve mostrare queste carte prima che volino li'.
            hiddenCapturedPlayer = dealerIndex;
            hiddenCapturedCount = sweptCards?.Count ?? 0;
        }

        private int hiddenCapturedPlayer = -1;
        private int hiddenCapturedCount;

        /// <summary>Carte prese da mostrare nel mazzetto, escluse quelle ancora in animazione.</summary>
        public int GetDisplayedCapturedCount(int playerIndex)
        {
            if (gameState == null || playerIndex < 0 || playerIndex >= gameState.Players.Count) return 0;
            int count = gameState.Players[playerIndex].CapturedCards.Count;
            return playerIndex == hiddenCapturedPlayer ? Mathf.Max(0, count - hiddenCapturedCount) : count;
        }

        [Header("Accuso manuale")]
        [Tooltip("Durata fissa della finestra per dichiarare Accuso manualmente prima che scatti l'automatico. Sempre la stessa, anche se nessuno ha un accuso disponibile: una durata variabile rivelerebbe implicitamente chi ce l'ha (vedi discussione UI_SPEC_Tavolo.md sezione 9).")]
        [SerializeField] private float manualAccusoWindowSeconds = 5f;

        [Header("Animazione dealer (inizio smazzata) - provvisorio, verra' rifatto con una vera grafica")]
        [Tooltip("Quanto resta visibile la chip 'MAZZIERE' prima che inizi la distribuzione animata.")]
        [SerializeField] private float dealerDeclareHoldSeconds = 2.2f;
        [Tooltip("Ritmo con cui arrivano le carte tavolo (una alla volta, dopo le mani): piu' lento dello stagger di default delle mani, richiesto esplicitamente.")]
        [SerializeField] private float tableCardStagger = 0.35f;

        private bool isAccusoWindowOpen;
        private float accusoWindowSecondsRemaining;
        // Indici gia' risolti (con o senza accuso) in questa mano: evita la doppia dichiarazione
        // quando un giocatore ha gia' dichiarato manualmente durante la finestra e poi il fallback
        // automatico di fine finestra ripasserebbe su tutti. Popolato anche dall'RPC in arrivo per
        // le dichiarazioni manuali di ALTRI client (vedi MarkAccusoResolved).
        private readonly HashSet<int> accusoAlreadyResolvedThisHand = new HashSet<int>();

        public bool IsAccusoWindowOpen => isAccusoWindowOpen;
        public float ManualAccusoWindowSeconds => manualAccusoWindowSeconds;
        public float AccusoWindowSecondsRemaining => accusoWindowSecondsRemaining;

        /// <summary>
        /// Mosse arrivate dalla rete (fromNetwork=true) mentre questo client era occupato ad
        /// animare una mossa/redeal precedente. PRIMA venivano scartate silenziosamente dal guard
        /// iniziale di ExecuteMove: sugli screenshot di questo bug (soprattutto su device reali,
        /// dove le animazioni durano piu' a lungo che in Editor/ParrelSync) bastava che due mosse
        /// arrivassero ravvicinate perche' la seconda sparisse per un solo client, mandando quel
        /// client fuori sincrono per sempre (ogni RPC successiva viene validata contro un
        /// GameState ormai diverso da quello degli altri client, e fallisce a sua volta). Ora le
        /// mosse di rete non vengono MAI scartate per questo motivo: si accodano ed entrano in
        /// gioco in ordine non appena il client torna libero (vedi TryProcessNextQueuedNetworkMove).
        /// </summary>
        private readonly Queue<Move> pendingNetworkMoves = new Queue<Move>();

        /// <summary>Throttle per RequestNetworkResync, per non spammare il Master di richieste.</summary>
        private float lastResyncRequestTime = -999f;
        
        /// <summary>
        /// Event fired quando un player esegue una mossa.
        /// NetworkGameController si subscribe a questo evento per sincronizzare via RPC.
        /// </summary>
        public event System.Action<Move> OnMoveExecuted;
        
        /// <summary>
        /// Event fired quando un player (umano locale) vuole eseguire una mossa.
        /// NetworkGameController si subscribe per inviare la mossa via RPC.
        /// </summary>
        public event System.Action<Move> OnLocalPlayerMoveRequested;

        public GameState GameState => gameState;
        public RoundManager RoundManager => roundManager;
        public int CurrentPlayerIndex => gameState?.CurrentPlayerIndex ?? -1;
        
        /// <summary>
        /// Check if current player is a human player using GameModeService.
        /// </summary>
        public bool IsHumanPlayerTurn
        {
            get
            {
                if (CurrentPlayerIndex < 0) return false;
                return GameModeService.Current.IsHumanPlayer(CurrentPlayerIndex);
            }
        }

        private void Start()
        {
            // Cache manager references if not assigned
            if (cardViewManager == null)
            {
                cardViewManager = FindObjectOfType<CardViewManager>();
            }
            
            if (capturedPileManager == null)
            {
                capturedPileManager = FindObjectOfType<CapturedPileManager>();
            }

            if (cardAnimationController == null)
            {
                cardAnimationController = FindObjectOfType<CardAnimationController>();
                if (cardAnimationController == null)
                {
                    cardAnimationController = gameObject.AddComponent<CardAnimationController>();
                }
            }
            
            // Initialize AI
            if (cirullaAI == null)
            {
                cirullaAI = new CirullaAI(ResolveAIDifficulty());
            }

            // Check if GameSceneInitializer exists OR if ActiveConfig is already set
            // If GameSceneInitializer exists, it will call StartNewGame() when ready
            var gameSceneInitializer = FindObjectOfType<GameSceneInitializer>();
            if (gameSceneInitializer != null)
            {
                Debug.Log("[TurnController] GameSceneInitializer found - deferring game start to it.");
                return;
            }

            // Also check if ActiveConfig is set (means GameSceneInitializer.Awake already ran)
            if (GameSceneInitializer.ActiveConfig != null)
            {
                Debug.Log("[TurnController] ActiveConfig already set by GameSceneInitializer - deferring to it.");
                return;
            }
            
            var provider = GameModeService.Current;
            
            if (provider.IsMultiplayer)
            {
                if (provider.IsMasterClient)
                {
                    Debug.Log("<color=cyan>[MP] Master Client starting game...</color>");
                    StartNewGame();
                }
                else
                {
                    Debug.Log("<color=cyan>[MP] Waiting for Master Client to start game...</color>");
                }
                return;
            }
            
            // Single-player or no GameManager found - only auto-start if explicitly enabled
            // AND we're not expecting a GameSceneInitializer
            if (autoStartGame)
            {
                Debug.Log("[TurnController] No GameSceneInitializer and autoStartGame=true - starting with defaults.");
                StartNewGame();
            }
        }

        // ==== QA Helpers (Context Menu) ====
        [ContextMenu("Setup Matta: Decino in mano (coppia)")]
        private void SetupMattaDecino_Context()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7), // Matta
                new Card(Suit.Bastoni, 6),
                new Card(Suit.Denari, 6), // coppia -> decino
            };
            var table = new List<Card>();
            SetupScenarioForCurrentPlayer(hand, table);
        }

        [ContextMenu("Setup Matta: Accuso in mano (somma<=9)")]
        private void SetupMattaAceCapture_Context()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7), // Matta
                new Card(Suit.Bastoni, 5),
                new Card(Suit.Denari, 3), // 5+3+1=9 -> accuso
            };
            var table = new List<Card>();
            SetupScenarioForCurrentPlayer(hand, table);
        }

        [ContextMenu("Setup Matta: Nessun hint (no decino/accuso)")]
        private void SetupMattaCapture15_Context()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7), // Matta
                new Card(Suit.Bastoni, 6),
                new Card(Suit.Denari, 3), // 6+3+1=10 -> nessun accuso e non è coppia
            };
            var table = new List<Card>();
            SetupScenarioForCurrentPlayer(hand, table);
        }

    /// <summary>
    /// Starts a new game of Cirulla/51.
    /// </summary>
    public void StartNewGame()
    {
        // GUARDIA CRITICA: in multiplayer SOLO il Master Client puo' generare/rimescolare un
        // nuovo mazzo. Rules51.CreateNewGame() usa un Random locale non condiviso: se un client
        // non-Master arrivasse qui (es. in passato OnRoundEndContinue() lo chiamava SENZA
        // controllare chi fosse il Master, quindi bastava che un giocatore qualsiasi premesse
        // "Continua" a fine mano perche' il SUO client si costruisse in locale un mazzo/mano
        // completamente diversi da quelli di tutti gli altri) lo stato locale diverge per sempre
        // da quello reale della partita, esattamente come nei sintomi riportati ("la partita va
        // avanti come se fosse un'altra sessione"). I client non-Master devono solo aspettare il
        // GameState del Master via RPC (RPC_ReceiveInitialGameState -> SetNetworkGameState).
        var providerGuard = GameModeService.Current;
        if (providerGuard.IsMultiplayer && !providerGuard.IsMasterClient)
        {
            Debug.LogError("[TurnController] StartNewGame() chiamato su un client non-Master in multiplayer: ignorato per evitare un mazzo/mano divergenti. Questo client aspettera' il GameState dal Master.");
            return;
        }

        // GameSceneInitializer.Start() chiama StartNewGame() DIRETTAMENTE, come normale metodo -
        // non tramite il ciclo di vita Unity - e questo puo' succedere PRIMA che
        // TurnController.Start() sia mai girato (confermato dai log: "deferring game start" arriva
        // DOPO "GameState created"). cardViewManager/capturedPileManager/cardAnimationController
        // non sono assegnati in Inspector su questo prefab (fileID: 0), quindi dipendono
        // interamente dal fallback FindObjectOfType di Start() - se non e' ancora girato, erano
        // null qui, e ogni blocco "if (cardViewManager != null)" piu' sotto veniva saltato in
        // silenzio (nessun errore, nessuna soppressione, nessuna animazione - bug segnalato:
        // "non fa neanche piu' l'animazione del dealer"). Stessa risoluzione di Start(), ripetuta
        // qui per essere sicuri che questi riferimenti esistano indipendentemente da chi chiama
        // StartNewGame() per primo.
        if (cardViewManager == null) cardViewManager = FindObjectOfType<CardViewManager>();
        if (capturedPileManager == null) capturedPileManager = FindObjectOfType<CapturedPileManager>();
        if (cardAnimationController == null)
        {
            cardAnimationController = FindObjectOfType<CardAnimationController>();
            if (cardAnimationController == null)
            {
                cardAnimationController = gameObject.AddComponent<CardAnimationController>();
            }
        }

        // La smazzata appena chiusa serve a MatchScore.ContinueMatch (totali, mazziere, numero smazzata).
        var previousGameState = gameState;
        gameState = null;

        // Scarta qualunque mossa di rete accodata da una mano/partita precedente: non ha piu'
        // senso applicarla al nuovo GameState che stiamo per creare.
        pendingNetworkMoves.Clear();

        // Initialize AI if not already done
        if (cirullaAI == null)
        {
            cirullaAI = new CirullaAI(ResolveAIDifficulty());
        }

        // Determine player count from MatchConfig (loaded by GameSceneInitializer)
        int numPlayers = 4;
        var cfg = GameSceneInitializer.ActiveConfig;
        if (cfg != null)
        {
            numPlayers = cfg.PlayerCount;
            Debug.Log($"[TurnController] Starting game with {numPlayers} players (from config: {cfg.Format})");
        }
        else
        {
            Debug.LogWarning("[TurnController] GameSceneInitializer.ActiveConfig is null! Defaulting to 4 players.");
        }

        // starting new game
        gameState = Rules51.CreateNewGame(numPlayers);
        gameState.TeamMode = cfg != null && cfg.Format == GameFormat.TwoVsTwo && numPlayers == 4;
        gameState.Rules = (cfg?.Rules ?? MatchRules.Default).Clone();
        MatchScore.ContinueMatch(previousGameState, gameState, cfg != null ? cfg.TargetScore : 51);
        Debug.Log($"[TurnController] GameState created: {gameState.NumPlayers} players, dealer={gameState.DealerIndex}, current={gameState.CurrentPlayerIndex}");
        
        // Cattura l'eventuale accuso del dealer (StartSmazzata lo processa in modo sincrono, PRIMA
        // che qualunque render esista) per poterlo rivelare piu' avanti nella sequenza dealer.
        pendingDealerAccuso = null;
        CreateRoundManager();
        // Do NOT declare on initial hands immediately; we'll handle it after a short visual delay

        roundManager.StartSmazzata();

        StageInitialDealAndStartIntro();

        // Compute valid moves for the current player
        RefreshValidMoves();

        // MULTIPLAYER: If we're Master Client, send GameState to all clients
        var provider = GameModeService.Current;
        if (provider.IsMultiplayer && provider.IsMasterClient)
        {
            // Find NetworkGameController and send GameState via reflection (to avoid circular dependency).
            // L'assembly si chiama "Assembly-CSharp" (il vecchio asmdef "Project51.Networking" non
            // esiste piu'): con il nome vecchio questa lookup falliva sempre silenziosamente e lo
            // stato iniziale non arrivava mai agli altri client (tavolo vuoto per loro).
            var netControllerType = System.Type.GetType("Project51.Networking.NetworkGameController, Assembly-CSharp");
            if (netControllerType != null)
            {
                var netInstanceProp = netControllerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var netController = netInstanceProp?.GetValue(null);
                if (netController != null)
                {
                    var sendMethod = netControllerType.GetMethod("SendInitialGameState");
                    sendMethod?.Invoke(netController, new object[] { gameState });
                    Debug.Log("<color=cyan>[MP] Master sent initial GameState to all clients</color>");
                }
            }
        }

        // NON invochiamo piu' l'IA qui direttamente: DeclareInitialAccusiWithDelay se ne occupa
        // alla fine della finestra Accuso (isRedealPendingVisual la tiene comunque ferma fino ad
        // allora tramite gli stessi guard usati per il redeal a meta' partita).
    }

    /// <summary>
    /// Inizio smazzata visibile: carte sul mazziere, poi roulette, distribuzione, eventuale accuso del
    /// mazziere e finestra Accuso (DeclareInitialAccusiWithDelay). Usata dal Master dopo aver
    /// distribuito e dai client quando ricevono dal Master uno stato appena distribuito.
    /// </summary>
    private void StageInitialDealAndStartIntro()
    {
        // Sospende la visibilita' PRIMA del primo ForceRefresh (difesa di base) e, subito dopo,
        // sposta FISICAMENTE ogni carta appena distribuita sulla posizione del mazziere a scala
        // quasi zero (StageCardsAtOriginForDealAnimation) - cosi' anche se qualcosa dovesse
        // riaccendere il renderer per un motivo che non e' stato possibile isolare (bug segnalato
        // piu' volte: "si vedono ancora"), la carta sarebbe comunque minuscola e sovrapposta dal
        // lato del mazziere, non "gia' distribuita in mano".
        pendingInitialHandStagedCards = null;
        pendingInitialTableStagedCards = null;
        if (cardViewManager != null)
        {
            // IMPORTANT: Force immediate visual refresh AFTER StartSmazzata
            // This ensures dealer 15/30 accuso (which removes table cards) happens BEFORE rendering
            // Otherwise cards might be visible for 0.5 seconds before disappearing.
            cardViewManager.SetSuppressNewCardVisibility(true);
            cardViewManager.ForceRefresh();

            // try/catch difensivo: se qualunque cosa qui sotto lancia un'eccezione (es. camera non
            // ancora pronta per GetDealerSeatPosition), NON deve impedire a StartCoroutine() poco
            // sotto di partire - altrimenti l'intera sequenza dealer/distribuzione/accuso salta,
            // non solo lo staging (bug segnalato: "non fa neanche piu' l'animazione del dealer").
            try
            {
                // Separate in due liste (non piu' una sola): la sequenza ora distribuisce prima
                // le mani, poi le carte tavolo a parte con un ritmo piu' lento (richiesto
                // esplicitamente). Se il dealer ha fatto un accuso, gameState.Table e' gia' vuoto
                // a questo punto (RoundManager l'ha gia' processato dentro StartSmazzata) - niente
                // da mettere in pendingInitialTableStagedCards in quel caso, se ne occupa il reveal.
                var handViews = new List<CardView>();
                for (int i = 0; i < gameState.NumPlayers; i++)
                {
                    foreach (var card in gameState.Players[i].Hand)
                    {
                        if (cardViewManager.TryGetCardView(card, out var cv) && cv != null) handViews.Add(cv);
                    }
                }
                if (handViews.Count > 0)
                {
                    pendingInitialHandStagedCards = cardViewManager.StageCardsAtOriginForDealAnimation(handViews, GetDealerSeatPosition());
                }

                var tableViews = new List<CardView>();
                if (gameState.Table != null)
                {
                    foreach (var card in gameState.Table)
                    {
                        if (cardViewManager.TryGetCardView(card, out var cv) && cv != null) tableViews.Add(cv);
                    }
                }
                if (tableViews.Count > 0)
                {
                    pendingInitialTableStagedCards = cardViewManager.StageCardsAtOriginForDealAnimation(tableViews, GetDealerSeatPosition());
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TurnController] Staging carte per animazione dealer fallito, proseguo senza: {ex}");
                cardViewManager.SetSuppressNewCardVisibility(false);
                pendingInitialHandStagedCards = null;
                pendingInitialTableStagedCards = null;
            }
        }

        // Delay the initial accusi declaration slightly so all clients see stable visuals first,
        // poi apre la finestra Accuso manuale. In multiplayer, solo il Master Client dichiara
        // automaticamente allo scadere e sincronizza via RPC (le dichiarazioni manuali restano
        // per-client, ognuno dichiara solo se stesso - vedi TryDeclareLocalManualAccuso).
        if (introCoroutine != null) StopCoroutine(introCoroutine);
        introCoroutine = StartCoroutine(DeclareInitialAccusiWithDelay());
    }

        /// <summary>
        /// Dopo un breve delay per la stabilizzazione visiva, apre la finestra Accuso manuale
        /// (sempre la stessa durata, con o senza accusi disponibili) e infine dichiara in automatico
        /// chi non ha dichiarato da se'. In multiplayer, solo il Master Client esegue il fallback
        /// automatico e sincronizza via RPC.
        /// </summary>
        private System.Collections.IEnumerator DeclareInitialAccusiWithDelay()
        {
            isRedealPendingVisual = true; // stesso guard usato per il redeal: tiene ferma l'IA/le mosse

            try
            {
                // La visibilita' e' gia' sospesa "alla fonte" da StartNewGame (vedi
                // CardViewManager.SetSuppressNewCardVisibility), quindi qui basta il delay normale.
                // Small delay for UI stabilization
                yield return new UnityEngine.WaitForSeconds(0.25f);

                // Il tavolo deve essere visibile prima che cominci l'intro: finche' la schermata di
                // caricamento copre tutto si aspetta, poi si lascia un respiro perche' il giocatore
                // veda dove si trova (bug segnalato: la roulette del mazziere partiva, con i suoi
                // suoni, mentre il caricamento era ancora a schermo).
                yield return WaitForLoadingCurtain();
                yield return new WaitForSeconds(GamePreferences.Scaled(dealerIntroLeadSeconds));

                // 1) Dichiarazione dealer: chip "MAZZIERE" sul banner giusto (resta accesa per
                // tutta la smazzata, non si spegne piu' da sola) - da qui il giocatore capisce
                // chi sta distribuendo PRIMA che si vedano le carte muoversi.
                yield return PlayDealerDeclareSequence();

                // 2) Le mani arrivano per prime (ritmo normale) dalla posizione del dealer.
                if (cardAnimationController != null && pendingInitialHandStagedCards != null && pendingInitialHandStagedCards.Count > 0)
                {
                    Vector3 dealerOrigin = GetDealerSeatPosition();
                    yield return cardAnimationController.PlayDealtCardsFromOrigin(pendingInitialHandStagedCards, dealerOrigin).WaitForCompletion();
                }

                yield return new WaitForSeconds(0.4f);

                // 3) Poi le carte tavolo: se il dealer NON ha fatto accuso, arrivano una alla
                // volta e piu' lentamente delle mani (richiesto esplicitamente). Se invece ha
                // fatto 15/30, il tavolo e' gia' vuoto (RoundManager l'ha gia' processato dentro
                // StartSmazzata): al posto della distribuzione normale si vede il reveal - prima
                // queste carte sparivano in silenzio, senza che il giocatore potesse mai saperlo.
                if (pendingDealerAccuso != null)
                {
                    // PlayDealerAccusoRevealIfAny si occupa gia' di posare le carte sul tavolo e
                    // aspettare dopo l'ultima prima di rivelare l'esito - nessun'altra pausa qui.
                    yield return PlayDealerAccusoRevealIfAny();
                }
                else if (cardAnimationController != null && pendingInitialTableStagedCards != null && pendingInitialTableStagedCards.Count > 0)
                {
                    Vector3 dealerOrigin = GetDealerSeatPosition();
                    yield return cardAnimationController.PlayDealtCardsFromOrigin(pendingInitialTableStagedCards, dealerOrigin, tableCardStagger).WaitForCompletion();
                }

                // 4) Finestra Accuso, come sempre.
                yield return RunAccusoWindowCoroutine(refreshVisualsOnAutoDeclare: true);
            }
            finally
            {
                // Rete di sicurezza: le carte sono state SPOSTATE FISICAMENTE sul mazziere da
                // StartNewGame. Se per qualunque motivo l'animazione sopra non parte/fallisce
                // (cardAnimationController nullo, eccezione), non devono restare li' minuscole per
                // sempre - ForceRefresh ricalcola e riapplica la posizione/scala corretta di ognuna,
                // esattamente come la sospensione della visibilita'.
                if (cardViewManager != null)
                {
                    cardViewManager.SetSuppressNewCardVisibility(false);
                    cardViewManager.ForceRefresh();
                    cardViewManager.SetAllCardRenderersVisible(true);
                }
                pendingInitialHandStagedCards = null;
                pendingInitialTableStagedCards = null;
                isRedealPendingVisual = false;
            }

            RefreshValidMoves();
            PlayYourTurnCue();

            if (!IsHumanPlayerTurn)
            {
                var provider = GameModeService.Current;
                bool shouldExecuteAI = !provider.IsMultiplayer || provider.IsMasterClient;
                if (shouldExecuteAI)
                {
                    CancelInvoke(nameof(ExecuteAITurn));
                    Invoke(nameof(ExecuteAITurn), GamePreferences.Scaled(aiMoveDelay));
                }
            }
        }

        [SerializeField] private DealerRouletteController dealerRouletteController;

        [Tooltip("Respiro fra la fine del caricamento e l'inizio della scelta del mazziere.")]
        [SerializeField] private float dealerIntroLeadSeconds = 1.1f;

        /// <summary>
        /// Aspetta che la schermata di caricamento sia sparita davvero. Se non c'e' nessuna
        /// schermata di caricamento (avvio diretto della scena nell'Editor) esce subito, e in ogni
        /// caso non aspetta all'infinito: la partita non deve restare bloccata per un velo rimasto su.
        /// </summary>
        private System.Collections.IEnumerator WaitForLoadingCurtain()
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (Project51.Core.AppLoading.IsCovering && Time.realtimeSinceStartup < deadline) yield return null;
        }

        /// <summary>
        /// Roulette (icone/nomi dei 4 giocatori, evidenziatore che rallenta e si ferma sul
        /// mazziere) seguita dalla chip "MAZZIERE" persistente sul banner - richiesta esplicita
        /// dell'utente al posto della semplice chip statica. Un no-op silenzioso se qualcosa non
        /// e' pronto - la distribuzione prosegue comunque, e' tutto puramente cosmetico.
        /// </summary>
        private System.Collections.IEnumerator PlayDealerDeclareSequence()
        {
            if (gameState == null) yield break;

            if (dealerRouletteController == null)
            {
                // true = includeInactive: il pannello roulette parte SetActive(false) (spento
                // finche' non serve), e FindObjectOfType di default IGNORA gli oggetti disattivati
                // - senza questo parametro la ricerca falliva sempre in silenzio, saltando
                // l'intera roulette (bug segnalato: "non si vede nulla, fa tutto come prima").
                dealerRouletteController = FindObjectOfType<DealerRouletteController>(true);
            }

            if (dealerRouletteController != null)
            {
                var names = new string[4];
                for (int p = 0; p < gameState.NumPlayers && p < 4; p++)
                {
                    int relative = GetRelativeSlot(p);
                    if (relative >= 0 && relative < 4) names[relative] = GetSimpleDisplayName(p);
                }
                for (int i = 0; i < 4; i++)
                {
                    if (string.IsNullOrEmpty(names[i])) names[i] = "-";
                }

                yield return dealerRouletteController.PlayRoulette(names, GetDealerRelativeSlot(), gameState.NumPlayers);

                // La fanfara d'inizio partita sta qui, non prima della roulette: suonata all'inizio
                // arrivava mentre lo schermo era ancora coperto dal caricamento, prima che ci fosse
                // qualcosa da vedere. Ora accompagna la prima distribuzione.
                if (gameState.RoundIndex <= 1) GameAudio.Play(SoundId.MatchStart);
            }

            // Resta accesa per tutta la smazzata (non si spegne piu' da sola): richiesta esplicita
            // dell'utente. PlayerBannerManager.SetDealerIndicatorForPlayer spegne automaticamente
            // quella del dealer precedente quando il ruolo passa a qualcun altro.
            SetDealerIndicatorViaReflection(true);
            yield return new WaitForSeconds(GamePreferences.Scaled(dealerDeclareHoldSeconds));
        }

        /// <summary>
        /// Rivela l'accuso del dealer (Dealer15/Dealer30), se e' successo (catturato in
        /// pendingDealerAccuso da HandleDealerAccusoDeclared). Le carte spazzate via da
        /// RoundManager (gia' rimosse da gameState.Table prima che qualunque render esistesse)
        /// vengono ricreate come CardView "fantasma" e posate sul tavolo VERO, una alla volta,
        /// nelle stesse posizioni/ritmo delle carte tavolo normali - richiesto esplicitamente
        /// dall'utente ("l'animazione deve partire 1-2 secondi dopo l'ultima carta poggiata sul
        /// tavolo", non un pannello popup separato con carte finte). Poi una pausa leggibile,
        /// poi volano verso il mazziere e vengono distrutte. No-op silenzioso se non c'e' nulla.
        /// </summary>
        private System.Collections.IEnumerator PlayDealerAccusoRevealIfAny()
        {
            if (pendingDealerAccuso == null) yield break;

            var (dealerIndex, type, sweptCards) = pendingDealerAccuso.Value;
            pendingDealerAccuso = null;

            try
            {
                if (cardViewManager == null || cardAnimationController == null || sweptCards == null || sweptCards.Count == 0) yield break;

                Vector3 dealerOrigin = GetDealerSeatPosition();

                // 1) Ricrea le carte spazzate via come "fantasmi" e le posa sul tavolo vero, una alla
                // volta, con lo stesso ritmo delle carte tavolo normali - non un popup con carte finte.
                var ghosts = new List<CardView>();
                var staged = new List<CardViewManager.StagedCard>();
                for (int i = 0; i < sweptCards.Count; i++)
                {
                    Vector3 tablePos = cardViewManager.GetTableCardPosition(sweptCards.Count, i);
                    var ghost = cardViewManager.SpawnGhostCardView(sweptCards[i], dealerOrigin);
                    if (ghost == null) continue;

                    ghosts.Add(ghost);
                    staged.Add(new CardViewManager.StagedCard(ghost, tablePos, ghost.transform.localScale));
                }

                if (ghosts.Count == 0) yield break;

                yield return cardAnimationController.PlayDealtCardsFromOrigin(staged, dealerOrigin, tableCardStagger).WaitForCompletion();

                // 2) Pausa leggibile dopo che l'ultima carta e' atterrata - richiesta esplicita
                // dell'utente ("1-2 secondi dall'ultima carta poggiata sul tavolo").
                yield return new WaitForSeconds(GamePreferences.Scaled(1.2f));

                // 3) Messaggio esito (banner piccolo, non un pannello a schermo intero: le carte vere
                // restano visibili sul tavolo mentre il testo compare sopra).
                if (dealerAccusoRevealController == null)
                {
                    // true = includeInactive, stesso motivo della roulette dealer.
                    dealerAccusoRevealController = FindObjectOfType<DealerAccusoRevealController>(true);
                }
                if (dealerAccusoRevealController != null)
                {
                    // Niente numero di punti: RoundManager applica un moltiplicatore (regole match)
                    // che non e' passato attraverso l'evento - un valore base fisso rischierebbe di
                    // essere sbagliato. Solo il fatto ("ha fatto scopa da 15/30") e' garantito corretto.
                    int points = type == AccusoType.Dealer30 ? 30 : 15;
                    string detail = GameModeService.Current.IsLocalPlayer(dealerIndex)
                        ? "Prendi le carte del tavolo"
                        : $"{GetSimpleDisplayName(dealerIndex)} prende le carte del tavolo";
                    GameAudio.Play(SoundId.Scopa);
                    cardViewManager.SetDealerAccusoGlow(ghosts, true);
                    yield return dealerAccusoRevealController.Show($"SCOPA DA {points}!", detail);
                    // Il cartello sfuma mentre le carte volano via: nessuna pausa morta.
                    dealerAccusoRevealController.HideAnimated();
                    cardViewManager.SetDealerAccusoGlow(ghosts, false);
                }

                // 4) Le carte volano nel mazzetto prese del mazziere e vengono distrutte (erano solo
                // fantasmi per questa animazione, non fanno parte di activeCardViews).
                var transforms = new List<Transform>();
                var renderers = new List<SpriteRenderer>();
                foreach (var ghost in ghosts)
                {
                    if (ghost == null) continue;
                    transforms.Add(ghost.transform);
                    renderers.Add(ghost.CardRenderer);
                }

                int dealerRelative = (dealerIndex - GameModeService.Current.LocalPlayerIndex + gameState.NumPlayers) % gameState.NumPlayers;
                if (!cardViewManager.TryGetCapturedPileWorldPosition(dealerRelative, gameState.NumPlayers, out Vector3 pileTarget))
                {
                    pileTarget = dealerOrigin;
                }
                yield return cardAnimationController.PlaySweepToPile(transforms, renderers, pileTarget).WaitForCompletion();

                foreach (var ghost in ghosts)
                {
                    ghost?.DestroyView();
                }
            }
            finally
            {
                hiddenCapturedPlayer = -1;
                hiddenCapturedCount = 0;
            }
        }

        /// <summary>
        /// Nome semplice per la roulette (niente lookup PlayFab: Project51.Auth vive
        /// nell'assembly di default come Project51.Unity.UI, stesso motivo di
        /// SetDealerIndicatorViaReflection - non vale la pena una reflection in piu' solo per
        /// questo, "Tu"/"Giocatore N"/"Bot N" e' gia' chiaro per una rivelazione di un istante).
        /// </summary>
        private string GetSimpleDisplayName(int playerIndex)
        {
            var provider = GameModeService.Current;
            if (provider.IsHumanPlayer(playerIndex))
            {
                return provider.IsLocalPlayer(playerIndex) ? "Tu" : $"Giocatore {playerIndex + 1}";
            }
            return $"Bot {playerIndex + 1}";
        }

        /// <summary>
        /// Project51.Unity.UI (PlayerBanner/PlayerBannerManager) vive nell'assembly di default
        /// (nessun .asmdef in Assets/Scripts/UI): Project51.Gameplay e' un assembly separato e
        /// non puo' referenziarlo direttamente (dipendenza circolare, CS0234/CS0246 a compile
        /// time) - stesso identico motivo/stesso pattern gia' usato per NetworkGameController
        /// (vedi TrySendAccusoSync).
        /// </summary>
        private void SetDealerIndicatorViaReflection(bool active)
        {
            var managerType = System.Type.GetType("Project51.Unity.UI.PlayerBannerManager, Assembly-CSharp");
            if (managerType == null) return;

            var manager = FindObjectOfType(managerType);
            if (manager == null) return;

            var method = managerType.GetMethod("SetDealerIndicatorForPlayer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            method?.Invoke(manager, new object[] { gameState.DealerIndex, active });
        }

        /// <summary>
        /// Indice relativo (0=Locale/1=Sinistra/2=Alto/3=Destra) del giocatore assoluto dato,
        /// stessa convenzione usata in tutto il progetto (PlayerBannerManager.ResolveRelativeSlot,
        /// CardViewManager.RenderAIHandsDynamic, AccusoPanelController, ecc. - duplicata ovunque,
        /// nessun helper condiviso esiste nel progetto per questo calcolo).
        /// </summary>
        private int GetRelativeSlot(int playerIndex)
        {
            if (gameState == null || gameState.NumPlayers <= 0) return 0;

            int localIndex = GameModeService.Current.LocalPlayerIndex;
            int numPlayers = gameState.NumPlayers;
            int relative = ((playerIndex - localIndex) % numPlayers + numPlayers) % numPlayers;
            if (numPlayers == 2 && relative == 1) relative = 2;
            return relative;
        }

        private int GetDealerRelativeSlot()
        {
            return gameState != null ? GetRelativeSlot(gameState.DealerIndex) : 0;
        }

        /// <summary>
        /// Posizione mondo della "seduta" del dealer - vedi CardViewManager.GetPlayerHandAnchor,
        /// che e' l'unica fonte di verita' per queste posizioni (responsive, non pixel fissi).
        /// </summary>
        private Vector3 GetDealerSeatPosition()
        {
            if (cardViewManager == null || gameState == null) return Vector3.zero;
            return cardViewManager.GetPlayerHandAnchor(GetDealerRelativeSlot(), gameState.NumPlayers);
        }

        /// <summary>
        /// Finestra Accuso manuale condivisa da inizio-smazzata (DeclareInitialAccusiWithDelay) e
        /// redeal a meta' partita (HandleNewHandsRevealSequence): stessa durata fissa sempre,
        /// poi fallback automatico (solo Master in multiplayer) su chi non ha gia' risolto.
        /// </summary>
        private System.Collections.IEnumerator RunAccusoWindowCoroutine(bool refreshVisualsOnAutoDeclare)
        {
            accusoAlreadyResolvedThisHand.Clear();
            isAccusoWindowOpen = true;
            accusoWindowSecondsRemaining = manualAccusoWindowSeconds;

            try
            {
                while (accusoWindowSecondsRemaining > 0f)
                {
                    accusoWindowSecondsRemaining -= Time.deltaTime;
                    yield return null;
                }
            }
            finally
            {
                isAccusoWindowOpen = false;
                accusoWindowSecondsRemaining = 0f;
            }

            var provider = GameModeService.Current;

            // In multiplayer, ensure only Master executes initial accusi
            if (!provider.IsMultiplayer || provider.IsMasterClient)
            {
                CheckAndDeclareAccusiForAllPlayers(refreshVisuals: refreshVisualsOnAutoDeclare);
            }

            // Si riprende a giocare solo a fine animazione del pugno (anche se l'accuso e' stato
            // dichiarato durante la finestra o arriva dalla rete).
            yield return null;
            while (GamePresentation.IsBusy)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Refreshes the list of valid moves for the current player.
        /// </summary>
        private void RefreshValidMoves()
        {
            if (gameState == null || gameState.RoundEnded)
            {
                currentValidMoves = new List<Move>();
                return;
            }

            currentValidMoves = Rules51.GetValidMoves(gameState, gameState.CurrentPlayerIndex);
            // player valid moves count
        }

        /// <summary>
        /// Executes a move. Called by human player UI or AI logic.
        /// In multiplayer, local player moves trigger OnLocalPlayerMoveRequested event.
        /// </summary>
        /// <param name="move">The move to execute</param>
        /// <param name="fromNetwork">True if this call originated from a network RPC (prevents re-broadcasting)</param>
        public void ExecuteMove(Move move, bool fromNetwork = false)
        {
            if (move == null || gameState == null || gameState.RoundEnded)
            {
                return;
            }

            bool localAnimationBusy = isMoveAnimationInProgress || isRedealPendingVisual || isRedealAnimationInProgress || GamePresentation.IsBusy;
            if (localAnimationBusy)
            {
                if (fromNetwork)
                {
                    // Una mossa gia' avvenuta per davvero (su chi l'ha giocata e su tutti gli altri
                    // client) NON puo' essere semplicemente ignorata solo perche' qui stiamo ancora
                    // animando la precedente: accodiamola e applichiamola in ordine appena liberi
                    // (vedi TryProcessNextQueuedNetworkMove). Scartarla qui significava perdere per
                    // sempre quella singola mossa SOLO su questo client, con ogni RPC successiva
                    // che falliva a sua volta il controllo di validita' contro uno stato ormai
                    // diverso da quello reale.
                    pendingNetworkMoves.Enqueue(move);
                }
                // Se invece e' un tentativo locale (umano o bot) mentre siamo occupati, va bene
                // ignorarlo: non e' ancora stato inviato in rete, quindi non causa nessun disallineamento.
                return;
            }

            var provider = GameModeService.Current;

            // Multiplayer: check if this move needs to be sent via network
            // SKIP if this call is already from network to prevent infinite loop!
            if (!fromNetwork && provider.IsMultiplayer)
            {
                bool isLocalPlayer = provider.IsLocalPlayer(move.PlayerIndex);
                bool isHuman = provider.IsHumanPlayer(move.PlayerIndex);
                bool isBot = provider.IsBotPlayer(move.PlayerIndex);

                if (isLocalPlayer && isHuman)
                {
                    OnLocalPlayerMoveRequested?.Invoke(move);
                    return;
                }

                if (isBot)
                {
                    if (!provider.IsMasterClient)
                    {
                        Debug.LogWarning("Non-master client tried to execute bot move - ignoring.");
                        return;
                    }
                    
                    OnLocalPlayerMoveRequested?.Invoke(move);
                    return;
                }
            }

            // Ensure valid moves list is initialized
            if (currentValidMoves == null)
            {
                RefreshValidMoves();
                if (currentValidMoves == null)
                {
                    currentValidMoves = new List<Move>();
                }
            }

            // If move is not in precomputed valid moves, allow a human PlayOnly to be forced
            if (!currentValidMoves.Contains(move))
            {
                bool allowForcedHumanPlayOnly = move.Type == MoveType.PlayOnly
                    && move.PlayerIndex == provider.LocalPlayerIndex
                    && gameState.Players[move.PlayerIndex].Hand.Contains(move.PlayedCard);

                if (!allowForcedHumanPlayOnly)
                {
                    if (fromNetwork)
                    {
                        // Una mossa arrivata dalla rete che non torna con lo stato locale non e'
                        // un evento "normale" da ignorare in silenzio: significa che questo client
                        // e' gia' divergente rispetto al resto della partita (es. per una mossa
                        // persa in passato, prima di questo fix, o per qualunque altra causa non
                        // ancora prevista). L'unica cosa sicura da fare e' richiedere al Master lo
                        // stato completo e ripartire da li', invece di restare bloccati per sempre.
                        Debug.LogError($"[TurnController] Mossa di rete non valida contro lo stato locale (probabile disallineamento): {move}. Richiedo un resync completo al Master.");
                        RequestNetworkResync();
                    }
                    return;
                }
            }

            StartCoroutine(ExecuteMoveWithAnimation(move, fromNetwork));
        }

        /// <summary>
        /// Applica in ordine le mosse di rete accodate mentre eravamo occupati ad animare una
        /// mossa/redeal precedente (vedi pendingNetworkMoves). Va richiamato ogni volta che TUTTI
        /// i flag di occupazione (animazione mossa, redeal in corso) tornano a false.
        /// </summary>
        private void Update()
        {
            // Mosse di rete accodate durante un'animazione a tutto tavolo (GamePresentation.IsBusy):
            // nessun altro punto le riprende quando l'animazione finisce.
            if (pendingNetworkMoves.Count > 0 && !GamePresentation.IsBusy)
            {
                TryProcessNextQueuedNetworkMove();
            }
        }

        private void TryProcessNextQueuedNetworkMove()
        {
            if (isMoveAnimationInProgress || isRedealPendingVisual || isRedealAnimationInProgress || GamePresentation.IsBusy)
            {
                return;
            }

            if (pendingNetworkMoves.Count == 0)
            {
                return;
            }

            var nextMove = pendingNetworkMoves.Dequeue();
            ExecuteMove(nextMove, fromNetwork: true);
        }

        /// <summary>
        /// Chiede al Master Client di reinviare l'intero GameState, per riallineare questo client
        /// dopo aver rilevato una mossa di rete incompatibile con lo stato locale. Riusa lo stesso
        /// meccanismo gia' usato per lo stato iniziale (RPC_RequestInitialGameState /
        /// RPC_ReceiveInitialGameState -> SetNetworkGameState), tramite reflection per evitare la
        /// dipendenza circolare Gameplay -> Networking (stesso pattern di TrySendAccusoSync).
        /// </summary>
        private void RequestNetworkResync()
        {
            var provider = GameModeService.Current;
            if (!provider.IsMultiplayer || provider.IsMasterClient)
            {
                return;
            }

            if (Time.time - lastResyncRequestTime < 2f)
            {
                return; // Evita di spammare il Master se arrivano piu' mosse invalide di fila.
            }
            lastResyncRequestTime = Time.time;

            var netControllerType = System.Type.GetType("Project51.Networking.NetworkGameController, Assembly-CSharp");
            if (netControllerType == null)
            {
                return;
            }

            var netInstanceProp = netControllerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var netController = netInstanceProp?.GetValue(null);
            if (netController == null)
            {
                return;
            }

            var requestMethod = netControllerType.GetMethod("RequestResync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            requestMethod?.Invoke(netController, null);
        }

        /// <summary>
        /// Animates the played card to the table, optionally previews the capture, then commits the move once.
        /// </summary>
        private System.Collections.IEnumerator ExecuteMoveWithAnimation(Move move, bool fromNetwork)
        {
            isMoveAnimationInProgress = true;

            var hiddenRenderers = new List<SpriteRenderer>();
            var visualCopies = new List<Transform>();
            Transform playedVisualForCrossfade = null;
            SpriteRenderer playedVisualRendererForCrossfade = null;

            try
            {
                if (cardAnimationController != null && cardViewManager != null &&
                    cardViewManager.TryGetCardView(move.PlayedCard, out var playedCardView) &&
                    cardAnimationController.TryCreateVisualCopy(
                        playedCardView.transform,
                        playedCardView.CardRenderer,
                        $"PlayedCardAnimation_{move.PlayedCard.Suit}_{move.PlayedCard.Rank}",
                        out var playedVisual,
                        out var playedVisualRenderer))
                {
                    visualCopies.Add(playedVisual);
                    Sprite faceSprite = cardViewManager.GetSpriteForCard(move.PlayedCard);
                    if (faceSprite != null)
                    {
                        playedVisualRenderer.sprite = faceSprite;
                    }

                    playedCardView.CardRenderer.enabled = false;
                    hiddenRenderers.Add(playedCardView.CardRenderer);

                    Vector3 tableTarget = cardViewManager.GetNextTableCardPosition(gameState.Table.Count);
                    float tableScale = cardViewManager.GetTableCardScale();
                    Sequence playSequence = cardAnimationController.PlayCardToTable(
                        playedVisual,
                        playedVisualRenderer,
                        tableTarget,
                        0f,
                        tableScale);
                    yield return playSequence.WaitForCompletion();

                    bool isCapture = move.Type != MoveType.PlayOnly && move.CapturedCards != null && move.CapturedCards.Count > 0;
                    if (isCapture)
                    {
                        var capturedTransforms = new List<Transform>();
                        var capturedRenderers = new List<SpriteRenderer>();
                        bool canAnimateCapture = true;

                        foreach (var capturedCard in move.CapturedCards)
                        {
                            if (!cardViewManager.TryGetCardView(capturedCard, out var capturedCardView) ||
                                !cardAnimationController.TryCreateVisualCopy(
                                    capturedCardView.transform,
                                    capturedCardView.CardRenderer,
                                    $"CapturedCardAnimation_{capturedCard.Suit}_{capturedCard.Rank}",
                                    out var capturedVisual,
                                    out var capturedVisualRenderer))
                            {
                                canAnimateCapture = false;
                                break;
                            }

                            visualCopies.Add(capturedVisual);
                            capturedCardView.CardRenderer.enabled = false;
                            hiddenRenderers.Add(capturedCardView.CardRenderer);
                            capturedTransforms.Add(capturedVisual);
                            capturedRenderers.Add(capturedVisualRenderer);
                        }

                        if (canAnimateCapture)
                        {
                            yield return cardAnimationController.CreateCapturePreview().WaitForCompletion();

                            int playerCount = gameState.NumPlayers;
                            int localPlayerIndex = GameModeService.Current.LocalPlayerIndex;
                            int relativePlayerIndex = (move.PlayerIndex - localPlayerIndex + playerCount) % playerCount;
                            // Le prese volano nel mazzetto accanto al banner del giocatore.
                            if (!cardViewManager.TryGetCapturedPileWorldPosition(relativePlayerIndex, playerCount, out Vector3 pileTarget))
                            {
                                pileTarget = cardViewManager.GetPlayerHandAnchor(relativePlayerIndex, playerCount)
                                    + cardViewManager.GetPlayerRightDirection(relativePlayerIndex, playerCount) * cardViewManager.GetCapturedPileDistance()
                                    + cardViewManager.GetCapturedPileScreenDownOffset();
                            }

                            if (IsScopaCapture(move)) GameAudio.Play(SoundId.Scopa);
                            Sequence captureSequence = cardAnimationController.CaptureSequence(
                                playedVisual,
                                playedVisualRenderer,
                                capturedTransforms,
                                capturedRenderers,
                                pileTarget);
                            yield return captureSequence.WaitForCompletion();
                        }
                    }
                    else
                    {
                        playedVisualForCrossfade = playedVisual;
                        playedVisualRendererForCrossfade = playedVisualRenderer;
                    }
                }

                ApplyMoveInternal(move, fromNetwork);

                if (playedVisualRendererForCrossfade != null && !isRedealPendingVisual)
                {
                    yield return DOTween.ToAlpha(
                        () => playedVisualRendererForCrossfade.color,
                        c => playedVisualRendererForCrossfade.color = c,
                        0f,
                        playedCardCrossfadeDuration).WaitForCompletion();
                }
            }
            finally
            {
                bool shouldRestoreHiddenRenderers = !isRedealPendingVisual;
                if (shouldRestoreHiddenRenderers)
                {
                    foreach (var renderer in hiddenRenderers)
                    {
                        if (renderer != null)
                        {
                            renderer.enabled = true;
                        }
                    }
                }

                if (isRedealPendingVisual)
                {
                    pendingRedealVisualCopies.AddRange(visualCopies);
                }
                else
                {
                    foreach (var visualCopy in visualCopies)
                    {
                        cardAnimationController?.DestroyVisualCopy(visualCopy);
                    }
                }

                isMoveAnimationInProgress = false;

                // Se nel frattempo e' arrivata (ed e' stata accodata) un'altra mossa di rete,
                // applicala ora. Se un redeal e' stato appena innescato da ApplyMoveInternal
                // (isRedealPendingVisual=true, impostato in modo sincrono prima che questa
                // coroutine arrivi qui), il metodo e' un no-op e sara' invece
                // HandleNewHandsRevealSequence a occuparsene alla fine della sua animazione.
                TryProcessNextQueuedNetworkMove();
            }
        }

        /// <summary>
        /// Internal method that applies a move to the game state.
        /// Called either directly (for PlayOnly) or after visual delay (for captures).
        /// </summary>
        private void ApplyMoveInternal(Move move, bool fromNetwork)
        {
            // Use RoundManager to apply move (handles scopa, dealing, end of smazzata)
            if (roundManager == null && gameState != null)
            {
                CreateRoundManager();
            }
            roundManager.ApplyMove(move);

            if (!isRedealPendingVisual && cardViewManager != null)
            {
                cardViewManager.ForceRefresh();
            }
            
            // Refresh captured piles visuals after move
            if (capturedPileManager != null)
            {
                capturedPileManager.ForceRefresh();
            }
            
            // Notify listeners that a move was executed so they can animate
            OnMoveExecuted?.Invoke(move);

            // If RoundManager set RoundEnded, handle end
            if (gameState.RoundEnded) 
            { 
                ShowRoundEndPanel();
                return; 
            }

            // Refresh valid moves for the next player
            RefreshValidMoves();
            if (!isRedealPendingVisual) PlayYourTurnCue();

            // If next player is AI, execute their turn
            var provider = GameModeService.Current;
            if (!IsHumanPlayerTurn && !isRedealPendingVisual && !isRedealAnimationInProgress)
            {
                bool shouldExecuteAI = !provider.IsMultiplayer || provider.IsMasterClient;

                if (shouldExecuteAI)
                {
                    Invoke(nameof(ExecuteAITurn), GamePreferences.Scaled(aiMoveDelay));
                }
            }
        }

        /// <summary>
        /// Deals 3 new cards to each player from the deck (no new table cards).
        /// Legacy helper retained for QA scenarios.
        /// </summary>
        private void DealNewHands()
        {
            int firstPlayerIndex = (gameState.DealerIndex - 1 + gameState.NumPlayers) % gameState.NumPlayers;
            for (int round = 0; round < 3; round++)
            {
                for (int offset = 0; offset < gameState.NumPlayers; offset++)
                {
                    int playerIndex = (firstPlayerIndex + offset) % gameState.NumPlayers;
                    if (gameState.Deck.Count > 0)
                    {
                        var card = gameState.Deck[0];
                        gameState.Deck.RemoveAt(0);
                        gameState.Players[playerIndex].Hand.Add(card);
                    }
                }
            }

            HandleNewHandsDealt();
        }

        private void HandleNewHandsDealt()
        {
            if (isRedealAnimationInProgress)
            {
                return;
            }

            isRedealPendingVisual = true;
            StartCoroutine(HandleNewHandsRevealSequence());
        }

        private System.Collections.IEnumerator HandleNewHandsRevealSequence()
        {
            isRedealAnimationInProgress = true;

            try
            {
                while (isMoveAnimationInProgress)
                {
                    yield return null;
                }

                if (redealStartDelay > 0f)
                {
                    yield return new WaitForSeconds(GamePreferences.Scaled(redealStartDelay));
                }

                // Come per la primissima mano (StartNewGame): sposta FISICAMENTE le nuove carte sul
                // mazziere subito dopo ForceRefresh, non solo un toggle di visibilita' (richiesto:
                // la distribuzione animata ad ogni mano, non solo all'inizio della smazzata - vedi
                // CardViewManager.StageCardsAtOriginForDealAnimation per il motivo).
                List<CardViewManager.StagedCard> staged = null;
                if (cardViewManager != null)
                {
                    // Solo le mani: le carte gia' sul tavolo (compresa quella appena giocata) restano
                    // visibili durante distribuzione e finestra Accuso.
                    cardViewManager.SetSuppressNewCardVisibility(true, includeTable: false);
                    cardViewManager.ForceRefresh();

                    try
                    {
                        var handViews = new List<CardView>();
                        for (int i = 0; i < gameState.NumPlayers; i++)
                        {
                            foreach (var card in gameState.Players[i].Hand)
                            {
                                if (cardViewManager.TryGetCardView(card, out var cardView) && cardView != null)
                                {
                                    handViews.Add(cardView);
                                }
                            }
                        }

                        if (handViews.Count > 0)
                        {
                            staged = cardViewManager.StageCardsAtOriginForDealAnimation(handViews, GetDealerSeatPosition());
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[TurnController] Staging carte per redeal fallito, proseguo senza: {ex}");
                        cardViewManager.SetSuppressNewCardVisibility(false);
                        staged = null;
                    }
                }

                if (cardAnimationController != null && pendingRedealVisualCopies.Count > 0)
                {
                    foreach (var visualCopy in pendingRedealVisualCopies)
                    {
                        cardAnimationController.DestroyVisualCopy(visualCopy);
                    }
                    pendingRedealVisualCopies.Clear();
                }

                if (cardAnimationController != null && staged != null && staged.Count > 0)
                {
                    Vector3 dealerOrigin = GetDealerSeatPosition();
                    yield return cardAnimationController.PlayDealtCardsFromOrigin(staged, dealerOrigin).WaitForCompletion();
                }

                // La finestra Accuso parte SOLO ora, a distribuzione visivamente completata: prima
                // partiva ancor prima che le carte finissero di arrivare in mano (bug segnalato -
                // il countdown scadeva senza che si potesse nemmeno vedere la mano nuova).
                yield return RunAccusoWindowCoroutine(refreshVisualsOnAutoDeclare: false);

                if (capturedPileManager != null)
                {
                    capturedPileManager.ForceRefresh();
                }

                if (cardViewManager != null)
                {
                    cardViewManager.ForceRefresh();
                }
            }
            finally
            {
                // Rete di sicurezza (stesso motivo di DeclareInitialAccusiWithDelay): le carte sono
                // state spostate fisicamente sul mazziere, ForceRefresh le riporta alla posizione
                // corretta se l'animazione sopra non e' partita/e' fallita.
                if (cardViewManager != null)
                {
                    cardViewManager.SetSuppressNewCardVisibility(false);
                    cardViewManager.ForceRefresh();
                    cardViewManager.SetAllCardRenderersVisible(true);
                }

                isRedealPendingVisual = false;
                isRedealAnimationInProgress = false;

                // Stesso motivo del blocco analogo in ExecuteMoveWithAnimation: eventuali mosse
                // di rete arrivate ed accodate durante il redeal vanno applicate ora che il
                // client torna libero.
                TryProcessNextQueuedNetworkMove();
            }

            RefreshValidMoves();
            PlayYourTurnCue();

            if (!IsHumanPlayerTurn)
            {
                var provider = GameModeService.Current;
                bool shouldExecuteAI = !provider.IsMultiplayer || provider.IsMasterClient;
                if (shouldExecuteAI)
                {
                    CancelInvoke(nameof(ExecuteAITurn));
                    Invoke(nameof(ExecuteAITurn), GamePreferences.Scaled(aiMoveDelay));
                }
            }
        }

        /// <summary>
        /// Ends the current round, assigns remaining table cards, and computes scores.
        /// </summary>
        private void EndRound()
        {
            // round ended, computing scores

            // Assign remaining table cards to last capture player
            if (gameState.LastCapturePlayerIndex >= 0 && gameState.Table.Count > 0)
            {
                var lastCapturePlayer = gameState.Players[gameState.LastCapturePlayerIndex];
                foreach (var card in gameState.Table)
                {
                    lastCapturePlayer.CapturedCards.Add(card);
                }
                gameState.Table.Clear();

                // remaining table cards assigned
            }

            // TODO: Implement full scoring (Scopa, Sette Bello, Primiera, Denari, Cards, Grande, Piccola, Cappotto)
            // For now, just log captured cards and Scopa counts
            for (int i = 0; i < gameState.NumPlayers; i++)
            {
                var player = gameState.Players[i];
                // player capture/scopa summary
            }

            gameState.RoundEnded = true;
        }

        /// <summary>
        /// AI uses CirullaAI for strategic move selection.
        /// Animation is handled locally by ExecuteMoveWithAnimation/CardAnimationController;
        /// in multiplayer the move is broadcast via OnLocalPlayerMoveRequested and replayed
        /// on each client through ExecuteMove(move, fromNetwork: true).
        /// </summary>
        private void ExecuteAITurn()
        {
            if (currentValidMoves == null || currentValidMoves.Count == 0)
            {
                return;
            }

            // Durante il pugno dell'accuso il bot aspetta invece di perdere il turno.
            if (GamePresentation.IsBusy)
            {
                Invoke(nameof(ExecuteAITurn), 0.3f);
                return;
            }

            // Online un posto puo' tornare umano mentre il turno IA era in attesa (giocatore rientrato).
            if (GameModeService.Current.IsMultiplayer && IsHumanPlayerTurn)
            {
                return;
            }

            Move chosenMove = cirullaAI?.ChooseMove(gameState, gameState.CurrentPlayerIndex, currentValidMoves);
            
            if (chosenMove == null)
            {
                var captureMoves = currentValidMoves.Where(m => m.Type != MoveType.PlayOnly).ToList();
                chosenMove = captureMoves.Count > 0 
                    ? captureMoves[Random.Range(0, captureMoves.Count)]
                    : currentValidMoves[Random.Range(0, currentValidMoves.Count)];
            }

            // Execute move: in singleplayer this runs ExecuteMoveWithAnimation directly;
            // in multiplayer it raises OnLocalPlayerMoveRequested so NetworkGameController
            // can broadcast the move via RPC, which each client then applies locally.
            ExecuteMove(chosenMove);
        }

        /// <summary>
        /// Legge la difficolta' bot scelta dall'utente da GameSceneInitializer.ActiveConfig
        /// (impostata da MatchConfig in lobby) e la converte in AIDifficulty per CirullaAI.
        /// Fallback al valore Inspector se non c'e' una config attiva (es. contesto di test).
        /// </summary>
        private AIDifficulty ResolveAIDifficulty()
        {
            var activeConfig = GameSceneInitializer.ActiveConfig;
            if (activeConfig == null)
                return aiDifficulty;

            switch (activeConfig.BotDifficulty)
            {
                case BotDifficulty.Easy:
                    return AIDifficulty.Easy;
                case BotDifficulty.Medium:
                    return AIDifficulty.Medium;
                case BotDifficulty.Hard:
                case BotDifficulty.Expert:
                    // CirullaAI non ha ancora un livello Expert distinto: per ora mappa su Hard.
                    // TODO: valutare se serve un comportamento IA piu' difficile per Expert -
                    // decisione di design separata da questo fix di wiring.
                    return AIDifficulty.Hard;
                default:
                    return AIDifficulty.Medium;
            }
        }

        /// <summary>
        /// Dichiara in automatico Decino/Cirulla per chi non ha gia' dichiarato manualmente durante
        /// la finestra Accuso (accusoAlreadyResolvedThisHand), in ordine di turno dal primo giocatore
        /// di mano - non piu' per indice fisso 0..N: se piu' giocatori hanno un accuso nella stessa
        /// mano, si rivelano in un ordine deterministico coerente col tavolo.
        /// </summary>
        private void CheckAndDeclareAccusiForAllPlayers(bool refreshVisuals)
        {
            if (roundManager == null || gameState == null) return;

            int firstPlayer = (gameState.DealerIndex - 1 + gameState.NumPlayers) % gameState.NumPlayers;
            for (int offset = 0; offset < gameState.NumPlayers; offset++)
            {
                int i = (firstPlayer + offset) % gameState.NumPlayers;
                DeclareAccusoForPlayer(i);
            }

            if (!refreshVisuals)
            {
                return;
            }

            // Refresh piles to show accusi badges/updates
            if (capturedPileManager != null)
            {
                capturedPileManager.ForceRefresh();
            }
            // Force card view refresh so hands with accuso are shown face-up
            if (cardViewManager != null)
            {
                cardViewManager.ForceRefresh();
            }
        }

        /// <summary>
        /// Chiamato dal bottone Accuso (TableActionButtonsController) per il giocatore locale,
        /// SOLO mentre la finestra e' aperta. TryPlayerAccuso e' comunque auto-validante (non fa
        /// nulla se la mano non e' davvero Cirulla/Decino), quindi e' sicuro chiamarlo alla cieca.
        /// </summary>
        public bool TryDeclareLocalManualAccuso()
        {
            if (!isAccusoWindowOpen || roundManager == null || gameState == null) return false;

            int localIndex = GameModeService.Current.LocalPlayerIndex;
            return DeclareAccusoForPlayer(localIndex);
        }

        /// <summary>
        /// Dichiara Decino/Cirulla per un giocatore se non ancora risolto in questa mano (che sia
        /// stato dichiarato manualmente o dal fallback automatico non importa: idempotente).
        /// </summary>
        private bool DeclareAccusoForPlayer(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= gameState.NumPlayers) return false;
            if (!accusoAlreadyResolvedThisHand.Add(playerIndex)) return false; // gia' risolto

            var hand = gameState.Players[playerIndex].Hand;

            // Decino ha priorita' (10 punti) su Cirulla (3 punti) - stessa mano non puo' essere entrambi.
            if (AccusiChecker.IsDecino(hand))
            {
                bool declared = roundManager.TryPlayerAccuso(playerIndex, AccusoType.Decino);
                if (declared)
                {
                    Debug.Log($"Player {playerIndex} declared DECINO!");
                    TrySendAccusoSync(playerIndex, AccusoType.Decino);
                }
                return declared;
            }

            if (AccusiChecker.IsCirulla(hand))
            {
                bool declared = roundManager.TryPlayerAccuso(playerIndex, AccusoType.Cirulla);
                if (declared)
                {
                    Debug.Log($"Player {playerIndex} declared CIRULLA!");
                    TrySendAccusoSync(playerIndex, AccusoType.Cirulla);
                }
                return declared;
            }

            return false;
        }

        /// <summary>
        /// Chiamato da NetworkGameController.RPC_ReceiveAccuso quando arriva la dichiarazione
        /// manuale di UN ALTRO client: senza questo, il fallback automatico di questo client (se
        /// e' Master) non saprebbe che quel giocatore ha gia' dichiarato da se' e lo ridichiarerebbe
        /// (doppi punti, doppia animazione).
        /// </summary>
        public void MarkAccusoResolved(int playerIndex)
        {
            accusoAlreadyResolvedThisHand.Add(playerIndex);
        }

        private void TrySendAccusoSync(int playerIndex, AccusoType type)
        {
            var provider = GameModeService.Current;
            if (!provider.IsMultiplayer) return;

            // Use reflection to call NetworkGameController.SendAccuso (to avoid circular dependency).
            // Stesso fix del nome assembly di StartNewGame()/SendInitialGameStateToClients: senza
            // questo, gli accusi (Cirulla/Decino) non venivano mai sincronizzati agli altri client.
            var netControllerType = System.Type.GetType("Project51.Networking.NetworkGameController, Assembly-CSharp");
            if (netControllerType != null)
            {
                var netInstanceProp = netControllerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var netController = netInstanceProp?.GetValue(null);
                if (netController != null)
                {
                    var sendMethod = netControllerType.GetMethod("SendAccuso", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    sendMethod?.Invoke(netController, new object[] { playerIndex, (int)type });
                }
            }
        }

        /// <summary>
        /// Returns valid moves for the current player (used by UI to show options).
        /// </summary>
        public List<Move> GetCurrentValidMoves()
        {
            return currentValidMoves ?? new List<Move>();
        }

        /// <summary>
        /// Shows the round end panel with scores.
        /// </summary>
        private void ShowRoundEndPanel()
        {
            if (roundEndPanel != null)
            {
                roundEndPanel.OnContinueClicked += OnRoundEndContinue;
                roundEndPanel.OnMainMenuClicked += OnRoundEndMainMenu;
                roundEndPanel.Show(gameState);
            }
            else
            {
                // Fallback: log scores to console
                var scores = PunteggioManager.CalculateSmazzataScores(gameState);
                for (int i = 0; i < gameState.NumPlayers; i++)
                {
                    Debug.Log($"Player {i}: {scores[i]} points (Scope: {gameState.Players[i].ScopaCount})");
                }
            }
        }

        private void OnRoundEndContinue()
        {
            if (roundEndPanel != null)
            {
                roundEndPanel.OnContinueClicked -= OnRoundEndContinue;
                roundEndPanel.OnMainMenuClicked -= OnRoundEndMainMenu;
                roundEndPanel.Hide();
            }

            // In multiplayer, il bottone "Continua" e' cliccabile su OGNI client (RoundEndPanel
            // non ha idea di chi sia il Master). Solo il Master deve davvero far partire la
            // mano successiva (mazzo nuovo + broadcast): gli altri client si limitano a chiudere
            // il pannello e aspettano il GameState che arrivera' via RPC quando il Master premera'
            // a sua volta Continua - esattamente come gia' avviene per la primissima mano.
            // StartNewGame() ha comunque una guardia equivalente, questo check evita solo di
            // richiamarlo inutilmente da qui.
            var provider = GameModeService.Current;
            if (provider.IsMultiplayer && !provider.IsMasterClient)
            {
                return;
            }

            // Start a new round (new smazzata)
            StartNewGame();
        }

        private void OnRoundEndMainMenu()
        {
            if (roundEndPanel != null)
            {
                roundEndPanel.OnContinueClicked -= OnRoundEndContinue;
                roundEndPanel.OnMainMenuClicked -= OnRoundEndMainMenu;
                roundEndPanel.Hide();
            }
            
            // Use AppFlowManager for centralized navigation
                    AppFlowManager.GoToMainMenu();
        }

        /// <summary>Avviso sonoro leggero quando tocca al giocatore di questo dispositivo.</summary>
        private void PlayYourTurnCue()
        {
            if (gameState == null || gameState.RoundEnded || !IsHumanPlayerTurn) return;
            if (!GameModeService.Current.IsLocalPlayer(CurrentPlayerIndex)) return;
            GameAudio.Play(SoundId.YourTurn);
        }

        /// <summary>
        /// La presa svuota il tavolo e non e' l'ultima giocata della smazzata (stessa regola di
        /// Rules51.ApplyMove). Va chiamata prima di applicare la mossa.
        /// </summary>
        private bool IsScopaCapture(Move move)
        {
            if (gameState == null || move == null || move.Type == MoveType.PlayOnly || move.CapturedCards == null) return false;
            if (move.CapturedCards.Count == 0 || move.CapturedCards.Count != gameState.Table.Count) return false;
            bool isLastPlay = gameState.Deck.Count == 0 && gameState.Players.Sum(p => p.Hand.Count) <= 1;
            return !isLastPlay;
        }

        /// <summary>
        /// Returns the valid moves for the current player that use the specified card.
        /// Useful for UI to present options when the player selects a card.
        /// </summary>
        public List<Move> GetMovesForCard(Card card)
        {
            if (currentValidMoves == null) return new List<Move>();
            return currentValidMoves.Where(m => m.PlayedCard.Equals(card)).ToList();
        }

        /// <summary>
        /// Debug/QA helper: set a deterministic scenario for the current player.
        /// Use this to verify Matta behavior without restarting the game.
        /// Provide exact cards for the current player's hand and the table.
        /// </summary>
        public void SetupScenarioForCurrentPlayer(List<Card> handCards, List<Card> tableCards)
        {
            if (gameState == null) return;
            // Force human player's turn for testing scenarios
            gameState.CurrentPlayerIndex = 0;
            var player = gameState.Players[0];
            player.Hand.Clear();
            if (handCards != null) player.Hand.AddRange(handCards);

            gameState.Table.Clear();
            if (tableCards != null) gameState.Table.AddRange(tableCards);

            // Clear moves and recompute
            currentValidMoves = Rules51.GetValidMoves(gameState, 0);

            // Cancel any pending AI invokes
            CancelInvoke();
            // Ensure normal time scale
            Time.timeScale = 1f;

            // Notify UI to refresh views immediately
            if (cardViewManager != null)
            {
                cardViewManager.ForceRefresh();
            }
            else
            {
                OnMoveExecuted?.Invoke(null);
            }
        }

        /// <summary>
        /// Called by UI when the player double-clicks a card: if no capture is possible for this card,
        /// perform a forced PlayOnly (discard). If captures exist, ignores the double-click.
        /// </summary>
        public void OnPlayerDoubleClick(Card card)
        {
            var moves = GetMovesForCard(card);
            // If any capture move exists, ignore double-click (must choose capture)
            if (moves.Any(m => m.Type != MoveType.PlayOnly))
            {
                // double-click ignored: capture available
                return;
            }

            // Otherwise perform a forced PlayOnly
            var playOnly = new Move(0, card, MoveType.PlayOnly);
            ExecuteMove(playOnly);
        }

        /// <summary>
        /// Called by UI when the player confirms a specific move for a selected card (index into moves list).
        /// </summary>
        public void OnPlayerConfirmMove(Card card, int moveIndex)
        {
            var moves = GetMovesForCard(card);
            if (moveIndex < 0 || moveIndex >= moves.Count) { return; }
            ExecuteMove(moves[moveIndex]);
        }

        /// <summary>
        /// Called by UI when the player drags a card onto table and releases over a set of table cards.
        /// The UI should provide the list of table cards targeted for capture (may be empty).
        /// </summary>
        public void OnPlayerDragPlay(Card playedCard, List<Card> targetTableCards)
        {
            var moves = GetMovesForCard(playedCard);
            if (moves == null || moves.Count == 0) { return; }
            // Let the rules engine validate the manual selection (handles matta, forced-capture rules, ace rules, etc.)
            var matches = Rules51.GetMatchingMovesFromSelection(gameState, gameState.CurrentPlayerIndex, playedCard, targetTableCards);
            if (matches.Count == 1)
            {
                ExecuteMove(matches[0]);
                return;
            }
            else if (matches.Count > 1)
            {
                // Multiple equivalent moves (e.g., matta assignments). Let the player choose.
                if (moveSelectionUI != null)
                {
                    var desc = matches.Select(m => m.ToString()).ToList();
                    moveSelectionUI.ShowMoves(desc, idx =>
                    {
                        if (idx >= 0 && idx < matches.Count)
                        {
                            ExecuteMove(matches[idx]);
                        }
                    });
                    return;
                }
                else
                {
                    // fallback: choose first
                    ExecuteMove(matches[0]);
                    return;
                }
            }

            // No matches -> show invalid feedback
            if (moveSelectionUI != null)
            {
                moveSelectionUI.ShowInvalid("Invalid selection");
            }
            else
            {
                // dragged play did not match any valid move
            }
        }
    }
}

