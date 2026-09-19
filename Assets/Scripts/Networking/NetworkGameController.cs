using UnityEngine;
using Photon.Pun;
using Project51.Core;
using Project51.Unity;
using System.Collections.Generic;

namespace Project51.Networking
{
    /// <summary>
    /// Gestisce la sincronizzazione multiplayer del gioco via Photon PUN 2.
    /// Invia e riceve mosse tra i client, garantendo che tutti abbiano lo stesso GameState.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class NetworkGameController : MonoBehaviourPunCallbacks
    {
        #region Singleton

        private static NetworkGameController _instance;
        public static NetworkGameController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<NetworkGameController>();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Send accuso declaration to all clients.
        /// </summary>
        public void SendAccuso(int playerIndex, int accusoType)
        {
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("Cannot send accuso: not in a Photon room!");
                return;
            }
            photonView.RPC(nameof(RPC_ReceiveAccuso), RpcTarget.All, playerIndex, accusoType);
        }

        /// <summary>
        /// RPC to apply accuso on all clients.
        /// </summary>
        [PunRPC]
        private void RPC_ReceiveAccuso(int playerIndex, int accusoType)
        {
            // If TurnController/GameState not yet ready (scene just loaded), buffer and apply later
            if (turnController == null || turnController.GameState == null)
            {
                if (pendingAccusi != null)
                {
                    pendingAccusi.Add((playerIndex, accusoType));
                }
                return;
            }
            // Update PlayerState.AccusiPoints according to type
            var gs = turnController.GameState;
            if (playerIndex < 0 || playerIndex >= gs.NumPlayers) return;
            var player = gs.Players[playerIndex];
            // Stesse regole della partita (moltiplicatore / accusi disattivati) usate da RoundManager.
            int points = (gs.Rules ?? MatchRules.Default).AccusoPoints(accusoType == (int)AccusoType.Decino ? 10 : 3);
            int newlyAwarded = Mathf.Max(0, points - player.AccusiPoints);
            player.RoundAccusiPoints += newlyAwarded;
            player.AccusiPoints = Mathf.Max(player.AccusiPoints, points);
            if (newlyAwarded > 0) GamePresentation.ShowAccuso(playerIndex, accusoType);

            // Segna il giocatore come "gia' risolto" anche su QUESTO client: se questo e' il Master
            // e la dichiarazione era manuale (arrivata da un altro client), il fallback automatico
            // di fine finestra (CheckAndDeclareAccusiForAllPlayers) non deve ridichiararlo.
            turnController.MarkAccusoResolved(playerIndex);
            // Optional: trigger UI badges or animations via AccusoUIBridge if present
            var pileMgr = FindObjectOfType<CapturedPileManager>();
            pileMgr?.ForceRefresh();

            // CRITICO: sul Master il "giro" delle carte (face-down -> face-up) succede subito perche'
            // CheckAndDeclareAccusiForAllPlayers() chiama gia' cardViewManager.ForceRefresh() in modo
            // esplicito appena dichiara l'accuso in locale. Su TUTTI gli altri client, pero', questo
            // RPC era l'UNICO punto in cui AccusiPoints veniva aggiornato - e prima non rinfrescava
            // le CardView delle mani, solo i mucchi di prese. Il risultato: le carte dell'avversario
            // restavano visivamente coperte finche' non arrivava una mossa qualunque a fare scattare
            // un ForceRefresh per un motivo completamente diverso (da qui il "si girano solo dopo che
            // qualcuno gioca una carta", che in realta' era solo il prossimo refresh casuale).
            var cardViewMgr = FindObjectOfType<CardViewManager>();
            cardViewMgr?.ForceRefresh();
        }

        private readonly Dictionary<int, float> emoticonLastSeen = new Dictionary<int, float>();
        public void SendEmoticon(int emoticon)
        {
            if (!PhotonNetwork.InRoom || emoticon < 0 || emoticon >= 6) return;
            photonView.RPC(nameof(RPC_Emoticon), RpcTarget.All, emoticon);
        }
        [PunRPC]
        private void RPC_Emoticon(int emoticon, PhotonMessageInfo info)
        {
            if (info.Sender == null || emoticon < 0 || emoticon >= 6) return;
            var initializer = FindObjectOfType<GameSceneInitializer>();
            int player = initializer != null ? initializer.GetPlayerIndexForActor(info.Sender.ActorNumber) : -1;
            if (player < 0) return;
            float lastTime;
            if (emoticonLastSeen.TryGetValue(player, out lastTime) && Time.unscaledTime - lastTime < 1.5f) return;
            emoticonLastSeen[player] = Time.unscaledTime;
            GamePresentation.ShowEmoticon(player, emoticon);
        }
        private void FlushPendingAccusi()
        {
            if (pendingAccusi == null || pendingAccusi.Count == 0) return;
            if (turnController == null || turnController.GameState == null) return;
            foreach (var pa in pendingAccusi)
            {
                RPC_ReceiveAccuso(pa.playerIndex, pa.accusoType);
            }
            pendingAccusi.Clear();
        }

        #endregion

        #region Components

        private TurnController turnController;

        [Header("Debug")]
        [SerializeField] private bool logNetworkMoves = true;

        // Buffer for early accuso events arriving before GameState is ready
        private List<(int playerIndex, int accusoType)> pendingAccusi = new List<(int, int)>();

        // Client-side: richiede lo stato iniziale al Master finche' non lo riceve.
        private Coroutine _requestInitialStateCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Ensure PhotonView exists
            if (GetComponent<PhotonView>() == null)
            {
                Debug.LogError("NetworkGameController requires a PhotonView component!");
            }
        }

        private void Start()
        {
            // Find TurnController
            turnController = FindObjectOfType<TurnController>();
            if (turnController == null)
            {
                Debug.LogError("TurnController not found! NetworkGameController requires it.");
                return;
            }

            // Subscribe to TurnController events
            turnController.OnLocalPlayerMoveRequested += SendMove;
            
            // If we're in multiplayer and we're the Master Client, we'll send the initial GameState
            // after TurnController.StartNewGame() is called
            if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            {
                Debug.Log("<color=cyan>[NET] Master Client: Will send initial GameState after game starts</color>");
            }
            else if (PhotonNetwork.InRoom)
            {
                // Il Master manda lo stato iniziale una volta sola, con un delay fisso
                // (GameSceneInitializer.multiplayerStartDelay), subito dopo aver avviato la partita.
                // Se questo client (device reale, caricamento piu' lento dell'Editor) non ha ancora
                // istanziato la propria PhotonView/scena in quel preciso istante, quell'RPC va
                // semplicemente perso per lui (non e' bufferizzato, e lui non e' un "nuovo" membro
                // della room per Photon). Prima restava per sempre con GameState=null / uno stato
                // sbagliato. Con questa richiesta attiva, il client stesso chiede allo stato al
                // Master finche' non lo riceve, indipendentemente dai tempi di caricamento di ciascuno.
                _requestInitialStateCoroutine = StartCoroutine(RequestInitialGameStateUntilReceived());
            }

            // Try flush any pending accusi if received before init
            FlushPendingAccusi();
        }

        /// <summary>
        /// Callback Photon consegnato IDENTICAMENTE a ogni client rimasto nella room quando un
        /// giocatore se ne va (disconnessione, crash, uscita volontaria). Non serve nessun RPC
        /// dedicato: essendo un evento nativo di Photon, ogni client lo riceve gia' da solo e puo'
        /// aggiornare il proprio stato locale in modo indipendente ma coerente con tutti gli altri
        /// (stessa foto stabile dei posti, vedi GameSceneInitializer._stableActorOrder).
        /// </summary>
        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            if (turnController == null || turnController.GameState == null)
            {
                // La partita non e' nemmeno iniziata da questa parte: niente da convertire in bot.
                return;
            }

            var gsi = FindObjectOfType<GameSceneInitializer>();
            if (gsi == null || otherPlayer == null)
            {
                return;
            }

            int leftPlayerIndex = gsi.GetPlayerIndexForActor(otherPlayer.ActorNumber);
            if (leftPlayerIndex < 0)
            {
                if (logNetworkMoves)
                    Debug.LogWarning($"[NET] Player {otherPlayer.NickName} left but is not part of this match's starting roster - ignoring.");
                return;
            }

            if (logNetworkMoves)
                Debug.Log($"<color=orange>[NET] Player {otherPlayer.NickName} (seat {leftPlayerIndex}) left mid-match - converting to bot.</color>");

            gsi.MarkPlayerDisconnected(leftPlayerIndex);
            // IsInactive: disconnessione (puo' rientrare entro PlayerTtl); altrimenti ha lasciato la partita.
            GamePresentation.ShowConnectionNotice(otherPlayer.IsInactive
                ? $"{otherPlayer.NickName} si è disconnesso: gioca un bot finché non rientra"
                : $"{otherPlayer.NickName} ha lasciato la partita: gioca un bot", 4f);
        }

        /// <summary>
        /// Un giocatore del roster iniziale rientra entro PlayerTtl (stesso ActorNumber): riprende il
        /// suo posto. Lo stato aggiornato lo chiede lui stesso al Master (RequestResync in OnJoinedRoom).
        /// </summary>
        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            if (turnController == null || turnController.GameState == null || newPlayer == null) return;

            var gsi = FindObjectOfType<GameSceneInitializer>();
            int seat = gsi != null ? gsi.GetPlayerIndexForActor(newPlayer.ActorNumber) : -1;
            if (seat < 0) return;

            if (logNetworkMoves)
                Debug.Log($"<color=green>[NET] Player {newPlayer.NickName} rejoined seat {seat}.</color>");
            gsi.MarkPlayerReconnected(seat);
            GamePresentation.ShowConnectionNotice($"{newPlayer.NickName} è rientrato in partita", 3f);
        }

        #region Reconnection

        /// <summary>Tempo concesso per rientrare: deve coincidere con il PlayerTtl della stanza.</summary>
        public const float RejoinWindowSeconds = 60f;

        private Coroutine _reconnectCoroutine;

        public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
        {
            // Uscita volontaria (menu) o partita non ancora iniziata: niente riconnessione.
            if (cause == Photon.Realtime.DisconnectCause.DisconnectByClientLogic) return;
            if (turnController == null || turnController.GameState == null || !GameModeService.Current.IsMultiplayer) return;
            if (_reconnectCoroutine != null) return;

            Debug.LogWarning($"[NET] Disconnected mid-match ({cause}): trying to rejoin for {RejoinWindowSeconds}s.");
            _reconnectCoroutine = StartCoroutine(ReconnectAndRejoinLoop());
        }

        private System.Collections.IEnumerator ReconnectAndRejoinLoop()
        {
            float deadline = Time.unscaledTime + RejoinWindowSeconds;
            float nextAttempt = 0f;
            while (Time.unscaledTime < deadline && !PhotonNetwork.InRoom)
            {
                int secondsLeft = Mathf.CeilToInt(deadline - Time.unscaledTime);
                GamePresentation.ShowConnectionNotice($"Connessione persa. Riconnessione in corso… {secondsLeft}s");
                if (Time.unscaledTime >= nextAttempt && PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Disconnected)
                {
                    nextAttempt = Time.unscaledTime + 3f;
                    PhotonNetwork.ReconnectAndRejoin();
                }
                yield return new WaitForSecondsRealtime(0.25f);
            }

            _reconnectCoroutine = null;
            if (PhotonNetwork.InRoom) yield break; // OnJoinedRoom ha gia' gestito il rientro

            GamePresentation.ShowConnectionNotice("Impossibile rientrare nella partita. Ritorno al menu…");
            yield return new WaitForSecondsRealtime(2.5f);
            AppFlowManager.GoToMainMenu();
        }

        public override void OnJoinedRoom()
        {
            // In GameScene l'unico ingresso in stanza possibile e' il rientro dopo una disconnessione.
            if (turnController == null || turnController.GameState == null) return;

            if (_reconnectCoroutine != null)
            {
                StopCoroutine(_reconnectCoroutine);
                _reconnectCoroutine = null;
            }
            Debug.Log("<color=green>[NET] Rejoined the match room.</color>");
            GamePresentation.ShowConnectionNotice("Sei di nuovo in partita!", 2.5f);

            FindObjectOfType<GameSceneInitializer>()?.SyncSeatsWithRoom();
            if (PhotonNetwork.IsMasterClient)
            {
                // Tutti gli altri sono usciti: questo client e' l'autorita', riparte da dove era.
                turnController.OnBecameMasterClient();
            }
            else
            {
                RequestResync();
            }
        }

        #endregion

        /// <summary>
        /// Photon migra automaticamente il ruolo di Master Client quando quello attuale si
        /// disconnette. Senza questo hook, il NUOVO master non ricalcolava mai IsMasterClient nel
        /// proprio GameModeService.Current (costruito una volta sola in
        /// GameSceneInitializer.SetupGameModeProvider e mai piu' aggiornato per questo evento):
        /// restava "non master" per sempre, quindi non avrebbe mai piu' fatto giocare i bot ne'
        /// rimandato lo stato - la partita si sarebbe bloccata esattamente come nel caso (gia'
        /// risolto) della disconnessione di un giocatore normale. Il posto occupato dal vecchio
        /// master viene comunque convertito in bot separatamente da OnPlayerLeftRoom sopra (Photon
        /// consegna entrambi gli eventi): questo hook si occupa solo del ruolo di autorita', non
        /// del posto giocatore.
        /// </summary>
        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            if (turnController == null || turnController.GameState == null)
            {
                return; // Partita non ancora iniziata da questa parte: nulla da riprendere.
            }

            var gsi = FindObjectOfType<GameSceneInitializer>();
            gsi?.RefreshMultiplayerGameModeProvider();

            if (!PhotonNetwork.IsMasterClient)
            {
                return; // Non sono io il nuovo master: nient'altro da fare da questo lato.
            }

            if (logNetworkMoves)
                Debug.Log($"<color=orange>[NET] Master Client migrated to me ({(newMasterClient != null ? newMasterClient.NickName : "?")}) - resuming authoritative duties.</color>");

            // Rimanda lo stato corrente a tutti: un client potrebbe essere rimasto indietro
            // proprio nell'istante della migrazione (vecchio master disconnesso a meta' di un invio).
            SendInitialGameState(turnController.GameState);

            // Se il turno corrente era di un bot rimasto fermo in attesa che il vecchio master lo
            // giocasse, nessun altro evento lo farebbe ripartire da solo (stesso motivo di
            // TurnController.OnPlayerConvertedToBot).
            turnController.OnBecameMasterClient();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (turnController != null)
            {
                turnController.OnLocalPlayerMoveRequested -= SendMove;
            }

            if (_requestInitialStateCoroutine != null)
            {
                StopCoroutine(_requestInitialStateCoroutine);
                _requestInitialStateCoroutine = null;
            }
        }

        /// <summary>
        /// Richiede periodicamente lo stato iniziale della partita al Master Client, finche'
        /// il TurnController locale non ha uno GameState valido. Si auto-cancella quando
        /// RPC_ReceiveInitialGameState applica con successo lo stato ricevuto.
        /// </summary>
        private System.Collections.IEnumerator RequestInitialGameStateUntilReceived()
        {
            // Piccolo margine per dare tempo al proprio PhotonView di essere pronto.
            yield return new WaitForSeconds(0.3f);

            while (turnController != null && turnController.GameState == null)
            {
                if (logNetworkMoves)
                    Debug.Log("<color=cyan>[NET] Requesting initial GameState from Master Client...</color>");

                photonView.RPC(nameof(RPC_RequestInitialGameState), RpcTarget.MasterClient);

                yield return new WaitForSeconds(1.5f);
            }

            _requestInitialStateCoroutine = null;
        }

        /// <summary>
        /// Chiede al Master Client di reinviare l'intero GameState per riallineare questo client.
        /// Usata da TurnController quando rileva una mossa di rete incompatibile con il proprio
        /// stato locale (segnale di disallineamento, non un evento normale da ignorare). Riusa
        /// esattamente lo stesso meccanismo dell'RPC dello stato iniziale.
        /// </summary>
        public void RequestResync()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
            {
                return;
            }

            if (logNetworkMoves)
                Debug.Log("<color=orange>[NET] Local GameState diverged - requesting full resync from Master Client...</color>");

            photonView.RPC(nameof(RPC_RequestInitialGameState), RpcTarget.MasterClient);
        }

        /// <summary>
        /// Eseguita sul Master Client quando un altro client richiede lo stato iniziale
        /// (perche' non l'ha ricevuto in tempo, es. caricamento scena piu' lento su device reale,
        /// o perche' RequestResync() lo ha invocato dopo aver rilevato un disallineamento).
        /// </summary>
        [PunRPC]
        private void RPC_RequestInitialGameState(PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            if (turnController == null || turnController.GameState == null)
            {
                // La partita non e' ancora partita da questa parte: il richiedente ritentera'.
                return;
            }

            if (logNetworkMoves)
                Debug.Log($"<color=cyan>[NET] Resending initial GameState to {(info.Sender != null ? info.Sender.NickName : "?")} on request</color>");

            string gameStateJson = SerializeGameState(turnController.GameState);
            photonView.RPC(nameof(RPC_ReceiveInitialGameState), info.Sender, gameStateJson);
        }

        #endregion

        #region Move Sending

        /// <summary>
        /// Invia una mossa a tutti i client via RPC.
        /// Chiamato automaticamente quando TurnController triggera OnLocalPlayerMoveRequested.
        /// </summary>
        private void SendMove(Move move)
        {
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("Cannot send move: not in a Photon room!");
                return;
            }

            if (move == null)
            {
                Debug.LogError("Cannot send null move!");
                return;
            }

            // Serialize move to JSON
            string moveJson = SerializeMove(move);

            if (logNetworkMoves)
            {
                Debug.Log($"<color=cyan>[NET] Sending move: {move}</color>");
            }

            // Send to all clients (including self)
            photonView.RPC(nameof(RPC_ExecuteMove), RpcTarget.All, moveJson);
        }

        #endregion

        #region GameState Sync

        /// <summary>
        /// Invia il GameState iniziale a tutti i client.
        /// Chiamato dal Master Client dopo aver creato la partita.
        /// </summary>
        public void SendInitialGameState(GameState gameState)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("Only Master Client can send initial GameState!");
                return;
            }

            if (gameState == null)
            {
                Debug.LogError("Cannot send null GameState!");
                return;
            }

            string gameStateJson = SerializeGameState(gameState);
            
            Debug.Log($"<color=cyan>[NET] Master sending initial GameState ({gameStateJson.Length} chars)</color>");
            
            // Send to all OTHER clients (not self - Master already has it)
            photonView.RPC(nameof(RPC_ReceiveInitialGameState), RpcTarget.Others, gameStateJson);
        }

        // Formato dello stato: vedi Project51.Core.GameStateSerializer (testato in EditMode).
        private string SerializeGameState(GameState gs)
        {
            return GameStateSerializer.Serialize(gs);
        }

        private GameState DeserializeGameState(string data)
        {
            try
            {
                return GameStateSerializer.Deserialize(data);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error deserializing GameState: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        #endregion

        #region RPC Methods

        /// <summary>
        /// RPC ricevuta da tutti i client quando un player fa una mossa.
        /// Esegue la mossa localmente per mantenere il GameState sincronizzato.
        /// </summary>
        [PunRPC]
        private void RPC_ExecuteMove(string moveJson, PhotonMessageInfo info)
        {
            if (turnController == null)
            {
                Debug.LogError("TurnController is null! Cannot execute move.");
                return;
            }

            // Deserialize move
            Move move = DeserializeMove(moveJson);
            if (move == null)
            {
                Debug.LogError("Failed to deserialize move!");
                return;
            }

            if (logNetworkMoves)
            {
                string senderName = info.Sender != null ? info.Sender.NickName : "Unknown";
                Debug.Log($"<color=yellow>[NET] Received move from {senderName}: {move}</color>");
            }

            // Execute move locally with fromNetwork=true to prevent re-broadcasting
            turnController.ExecuteMove(move, fromNetwork: true);
        }

        /// <summary>
        /// RPC ricevuta dai client (non-Master) per sincronizzare il GameState iniziale.
        /// </summary>
        [PunRPC]
        private void RPC_ReceiveInitialGameState(string gameStateJson, PhotonMessageInfo info)
        {
            Debug.Log($"<color=yellow>[NET] Receiving initial GameState from Master ({gameStateJson.Length} chars)</color>");
            
            if (turnController == null)
            {
                Debug.LogError("TurnController is null! Cannot set GameState.");
                return;
            }

            GameState gameState = DeserializeGameState(gameStateJson);
            if (gameState == null)
            {
                Debug.LogError("Failed to deserialize GameState!");
                return;
            }

            Debug.Log($"<color=green>[NET] GameState received! Players: {gameState.NumPlayers}, Dealer: {gameState.DealerIndex}, Current: {gameState.CurrentPlayerIndex}</color>");
            Debug.Log($"<color=green>[NET] Deck cards: {gameState.Deck.Count}, Table cards: {gameState.Table.Count}</color>");
            
            // Log player hands for debugging
            for (int i = 0; i < gameState.NumPlayers; i++)
            {
                Debug.Log($"<color=green>[NET] Player {i} hand: {gameState.Players[i].Hand.Count} cards</color>");
            }

            turnController.SetNetworkGameState(gameState);
            Debug.Log("<color=green>[NET] GameState applied to TurnController!</color>");

            // Non serve piu' richiedere lo stato: fermiamo eventuale retry in corso.
            if (_requestInitialStateCoroutine != null)
            {
                StopCoroutine(_requestInitialStateCoroutine);
                _requestInitialStateCoroutine = null;
            }

            // Apply any pending accusi received before GameState was ready
            FlushPendingAccusi();
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Serializza una mossa in formato JSON.
        /// Formato: playerIndex|playedCard|moveType|capturedCards
        /// </summary>
        private string SerializeMove(Move move)
        {
            // Format: playerIndex|suit:rank|moveType|suit1:rank1,suit2:rank2,...
            string result = $"{move.PlayerIndex}|{move.PlayedCard.Suit}:{move.PlayedCard.Rank}|{(int)move.Type}";

            if (move.CapturedCards != null && move.CapturedCards.Count > 0)
            {
                var capturedParts = new List<string>();
                foreach (var card in move.CapturedCards)
                {
                    capturedParts.Add($"{card.Suit}:{card.Rank}");
                }
                result += "|" + string.Join(",", capturedParts);
            }

            return result;
        }

        /// <summary>
        /// Deserializza una mossa da formato JSON.
        /// </summary>
        private Move DeserializeMove(string moveJson)
        {
            try
            {
                string[] parts = moveJson.Split('|');
                if (parts.Length < 3)
                {
                    Debug.LogError($"Invalid move format: {moveJson}");
                    return null;
                }

                // Parse player index
                int playerIndex = int.Parse(parts[0]);

                // Parse played card
                string[] cardParts = parts[1].Split(':');
                Suit suit = (Suit)System.Enum.Parse(typeof(Suit), cardParts[0]);
                int rank = int.Parse(cardParts[1]);
                Card playedCard = new Card(suit, rank);

                // Parse move type
                MoveType moveType = (MoveType)int.Parse(parts[2]);

                // Parse captured cards (if any)
                List<Card> capturedCards = new List<Card>();
                if (parts.Length > 3 && !string.IsNullOrEmpty(parts[3]))
                {
                    string[] capturedParts = parts[3].Split(',');
                    foreach (var capturedPart in capturedParts)
                    {
                        string[] capCardParts = capturedPart.Split(':');
                        Suit capSuit = (Suit)System.Enum.Parse(typeof(Suit), capCardParts[0]);
                        int capRank = int.Parse(capCardParts[1]);
                        capturedCards.Add(new Card(capSuit, capRank));
                    }
                }

                return new Move(playerIndex, playedCard, moveType, capturedCards);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error deserializing move: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
