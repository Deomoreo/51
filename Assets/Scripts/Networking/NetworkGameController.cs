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
            // AccusoType: assume Cirulla=3, Decino=10
            int points = accusoType == (int)AccusoType.Decino ? 10 : 3;
            player.AccusiPoints = Mathf.Max(player.AccusiPoints, points);
            // Optional: trigger UI badges or animations via AccusoUIBridge if present
            var pileMgr = FindObjectOfType<CapturedPileManager>();
            pileMgr?.ForceRefresh();
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

            // Try flush any pending accusi if received before init
            FlushPendingAccusi();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (turnController != null)
            {
                turnController.OnLocalPlayerMoveRequested -= SendMove;
            }
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

        /// <summary>
        /// Serializza il GameState in formato JSON compatto.
        /// </summary>
        private string SerializeGameState(GameState gs)
        {
            // Simple format: numPlayers|dealerIndex|currentPlayerIndex|deck|table|players
            var parts = new List<string>();
            
            parts.Add(gs.NumPlayers.ToString());
            parts.Add(gs.DealerIndex.ToString());
            parts.Add(gs.CurrentPlayerIndex.ToString());
            
            // Serialize deck
            parts.Add(SerializeCardList(gs.Deck));
            
            // Serialize table
            parts.Add(SerializeCardList(gs.Table));
            
            // Serialize players
            for (int i = 0; i < gs.NumPlayers; i++)
            {
                parts.Add(SerializePlayer(gs.Players[i]));
            }
            
            return string.Join("||", parts);
        }

        /// <summary>
        /// Serializza una lista di carte.
        /// </summary>
        private string SerializeCardList(List<Card> cards)
        {
            if (cards == null || cards.Count == 0)
                return "";
            
            var cardStrings = new List<string>();
            foreach (var card in cards)
            {
                cardStrings.Add($"{card.Suit}:{card.Rank}");
            }
            return string.Join(",", cardStrings);
        }

        /// <summary>
        /// Serializza un PlayerState.
        /// </summary>
        private string SerializePlayer(PlayerState player)
        {
            // Format: hand|capturedCards
            var parts = new List<string>();
            parts.Add(SerializeCardList(player.Hand));
            parts.Add(SerializeCardList(player.CapturedCards));
            return string.Join(";", parts);
        }

        /// <summary>
        /// Deserializza il GameState da JSON.
        /// </summary>
        private GameState DeserializeGameState(string json)
        {
            try
            {
                string[] parts = json.Split(new[] { "||" }, System.StringSplitOptions.None);
                if (parts.Length < 5)
                {
                    Debug.LogError($"Invalid GameState format: not enough parts ({parts.Length})");
                    return null;
                }

                int numPlayers = int.Parse(parts[0]);
                int dealerIndex = int.Parse(parts[1]);
                int currentPlayerIndex = int.Parse(parts[2]);
                
                var deckCards = DeserializeCardList(parts[3]);
                var tableCards = DeserializeCardList(parts[4]);
                
                var playerDataList = new List<(List<Card> hand, List<Card> captured)>();
                for (int i = 0; i < numPlayers; i++)
                {
                    if (parts.Length <= 5 + i)
                    {
                        Debug.LogError($"Missing player {i} in GameState");
                        return null;
                    }
                    var playerData = DeserializePlayerData(parts[5 + i]);
                    playerDataList.Add(playerData);
                }

                // Create GameState (constructor creates empty lists)
                var gameState = new GameState(numPlayers)
                {
                    DealerIndex = dealerIndex,
                    CurrentPlayerIndex = currentPlayerIndex
                };

                // Populate Deck (it's a get-only property but we can add to the list)
                gameState.Deck.Clear();
                gameState.Deck.AddRange(deckCards);
                
                // Populate Table
                gameState.Table.Clear();
                gameState.Table.AddRange(tableCards);

                // Populate Players
                for (int i = 0; i < numPlayers; i++)
                {
                    gameState.Players[i].Hand.Clear();
                    gameState.Players[i].Hand.AddRange(playerDataList[i].hand);
                    
                    gameState.Players[i].CapturedCards.Clear();
                    gameState.Players[i].CapturedCards.AddRange(playerDataList[i].captured);
                }

                return gameState;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error deserializing GameState: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Deserializza una lista di carte.
        /// </summary>
        private List<Card> DeserializeCardList(string data)
        {
            var cards = new List<Card>();
            if (string.IsNullOrEmpty(data))
                return cards;

            string[] cardStrings = data.Split(',');
            foreach (var cardString in cardStrings)
            {
                if (string.IsNullOrEmpty(cardString))
                    continue;

                string[] cardParts = cardString.Split(':');
                Suit suit = (Suit)System.Enum.Parse(typeof(Suit), cardParts[0]);
                int rank = int.Parse(cardParts[1]);
                cards.Add(new Card(suit, rank));
            }
            return cards;
        }

        /// <summary>
        /// Deserializza dati di un player.
        /// Returns: (hand, capturedCards)
        /// </summary>
        private (List<Card> hand, List<Card> captured) DeserializePlayerData(string data)
        {
            string[] parts = data.Split(';');
            var hand = DeserializeCardList(parts[0]);
            var captured = DeserializeCardList(parts.Length > 1 ? parts[1] : "");
            return (hand, captured);
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

            // Set the GameState in TurnController using reflection
            // (TurnController doesn't have a public SetGameState method)
            var tcType = typeof(TurnController);
            var gameStateField = tcType.GetField("gameState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (gameStateField != null)
            {
                gameStateField.SetValue(turnController, gameState);
                Debug.Log("<color=green>[NET] GameState applied to TurnController!</color>");
                
                // Force UI refresh
                var onMoveExecuted = tcType.GetField("OnMoveExecuted", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (onMoveExecuted != null)
                {
                    var evt = onMoveExecuted.GetValue(turnController) as System.Action<Move>;
                    evt?.Invoke(null); // Trigger UI refresh
                }

                // Apply any pending accusi received before GameState was ready
                FlushPendingAccusi();
            }
            else
            {
                Debug.LogError("Could not find gameState field in TurnController!");
            }
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
