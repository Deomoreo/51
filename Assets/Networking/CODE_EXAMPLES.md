# ?? Code Examples & Snippets

Esempi di codice utili per l'implementazione multiplayer.

---

## 1. GameState Serialization

### Strutture Serializzabili

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project51.Networking
{
    /// <summary>
    /// Versione serializzabile di Card
    /// </summary>
    [Serializable]
    public struct CardData
    {
        public int suit;  // 0-3 per Suit enum
        public int rank;  // 1-10
        
        public CardData(Card card)
        {
            suit = (int)card.Suit;
            rank = card.Rank;
        }
        
        public Card ToCard()
        {
            return new Card((Suit)suit, rank);
        }
    }

    /// <summary>
    /// Versione serializzabile di PlayerState
    /// </summary>
    [Serializable]
    public class PlayerStateData
    {
        public int playerId;
        public List<CardData> hand;
        public List<CardData> capturedCards;
        public List<CardData> scopaCards;
        public int scopaCount;
        public int totalScore;
        public int accusiPoints;
        
        public PlayerStateData(PlayerState player)
        {
            playerId = player.PlayerId;
            hand = ConvertCards(player.Hand);
            capturedCards = ConvertCards(player.CapturedCards);
            scopaCards = ConvertCards(player.ScopaCards);
            scopaCount = player.ScopaCount;
            totalScore = player.TotalScore;
            accusiPoints = player.AccusiPoints;
        }
        
        private List<CardData> ConvertCards(List<Card> cards)
        {
            var data = new List<CardData>();
            foreach (var card in cards)
            {
                data.Add(new CardData(card));
            }
            return data;
        }
        
        public PlayerState ToPlayerState()
        {
            var player = new PlayerState(playerId);
            
            // Converti hand
            foreach (var cardData in hand)
            {
                player.Hand.Add(cardData.ToCard());
            }
            
            // Converti captured
            foreach (var cardData in capturedCards)
            {
                player.CapturedCards.Add(cardData.ToCard());
            }
            
            // Converti scopa cards
            foreach (var cardData in scopaCards)
            {
                player.ScopaCards.Add(cardData.ToCard());
            }
            
            player.ScopaCount = scopaCount;
            player.TotalScore = totalScore;
            player.AccusiPoints = accusiPoints;
            
            return player;
        }
    }

    /// <summary>
    /// Versione serializzabile completa di GameState
    /// </summary>
    [Serializable]
    public class GameStateData
    {
        public int numPlayers;
        public int currentPlayerIndex;
        public int dealerIndex;
        public int smazzataNumber;
        public List<CardData> tableCards;
        public List<PlayerStateData> players;
        public List<CardData> remainingDeck;
        public int lastCapturePlayerIndex;
        
        public GameStateData(GameState state)
        {
            numPlayers = state.NumPlayers;
            currentPlayerIndex = state.CurrentPlayerIndex;
            dealerIndex = state.DealerIndex;
            smazzataNumber = state.SmazzataNumber;
            
            // Converti table cards
            tableCards = new List<CardData>();
            foreach (var card in state.TableCards)
            {
                tableCards.Add(new CardData(card));
            }
            
            // Converti players
            players = new List<PlayerStateData>();
            foreach (var player in state.Players)
            {
                players.Add(new PlayerStateData(player));
            }
            
            // Converti deck rimanente
            remainingDeck = new List<CardData>();
            // NOTE: Il deck potrebbe non essere accessibile direttamente
            // Potrebbe essere necessario esporlo in GameState
            
            lastCapturePlayerIndex = state.LastCapturePlayerIndex;
        }
        
        public GameState ToGameState()
        {
            var gameState = new GameState(numPlayers);
            
            // TODO: Implementare la ricostruzione completa
            // Questo richiede di poter modificare i campi privati di GameState
            // Opzioni:
            // 1. Aggiungere metodo di deserializzazione a GameState
            // 2. Usare reflection (più lento)
            // 3. Refactorare GameState per permettere full init
            
            return gameState;
        }
    }

    /// <summary>
    /// Utility per serializzare GameState
    /// </summary>
    public static class GameStateSerializer
    {
        public static string Serialize(GameState state)
        {
            var data = new GameStateData(state);
            return JsonUtility.ToJson(data);
        }
        
        public static GameState Deserialize(string json)
        {
            var data = JsonUtility.FromJson<GameStateData>(json);
            return data.ToGameState();
        }
        
        // Versione compressa (future optimization)
        public static byte[] SerializeCompressed(GameState state)
        {
            string json = Serialize(state);
            // TODO: Compressione con GZip
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
    }
}
```

---

## 2. NetworkGameController

```csharp
using UnityEngine;
using Photon.Pun;
using Project51.Core;
using System.Collections.Generic;

namespace Project51.Networking
{
    /// <summary>
    /// Controlla la sincronizzazione del GameState via Photon.
    /// Usa RPC per comunicare mosse e eventi.
    /// </summary>
    public class NetworkGameController : MonoBehaviourPunCallbacks
    {
        [Header("References")]
        [SerializeField] private TurnController turnController;
        
        private GameState gameState;
        private RoomManager roomManager;
        private bool isGameInitialized = false;
        
        private void Start()
        {
            roomManager = RoomManager.Instance;
            
            if (PhotonNetwork.IsMasterClient)
            {
                InitializeGameAsMaster();
            }
            else
            {
                RequestGameStateSync();
            }
        }
        
        #region Initialization
        
        /// <summary>
        /// Master Client inizializza il game state.
        /// </summary>
        private void InitializeGameAsMaster()
        {
            Debug.Log("NetworkGameController: Initializing as Master");
            
            // Determina numero giocatori dalla room
            int numPlayers = roomManager.FilledSlotsCount;
            
            // Crea nuovo GameState
            gameState = new GameState(numPlayers);
            
            // Inizializza round manager (se necessario)
            // ...
            
            // Sincronizza a tutti i client
            SyncGameStateToAll();
            
            isGameInitialized = true;
        }
        
        /// <summary>
        /// Client richiede sync dello stato al Master.
        /// </summary>
        private void RequestGameStateSync()
        {
            Debug.Log("NetworkGameController: Requesting sync from Master");
            photonView.RPC("RPC_RequestSync", RpcTarget.MasterClient);
        }
        
        [PunRPC]
        private void RPC_RequestSync(PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            
            // Invia stato al richiedente
            string stateJson = GameStateSerializer.Serialize(gameState);
            photonView.RPC("RPC_ReceiveGameState", info.Sender, stateJson);
        }
        
        [PunRPC]
        private void RPC_ReceiveGameState(string stateJson)
        {
            Debug.Log("NetworkGameController: Receiving game state");
            gameState = GameStateSerializer.Deserialize(stateJson);
            isGameInitialized = true;
            
            // Notifica al TurnController
            if (turnController != null)
            {
                turnController.SetGameState(gameState);
            }
        }
        
        #endregion
        
        #region Game State Sync
        
        /// <summary>
        /// Master Client sincronizza lo stato a tutti.
        /// </summary>
        private void SyncGameStateToAll()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            
            string stateJson = GameStateSerializer.Serialize(gameState);
            photonView.RPC("RPC_ReceiveGameState", RpcTarget.Others, stateJson);
        }
        
        #endregion
        
        #region Move Execution
        
        /// <summary>
        /// Esegui una mossa (chiamato localmente).
        /// Valida e propaga via RPC.
        /// </summary>
        public void ExecuteMove(Move move, int playerIndex)
        {
            // Se siamo Master, validiamo e propaghiamo
            if (PhotonNetwork.IsMasterClient)
            {
                ExecuteMoveAsMaster(move, playerIndex);
            }
            else
            {
                // Invia richiesta al Master per validazione
                RequestMoveExecution(move, playerIndex);
            }
        }
        
        private void ExecuteMoveAsMaster(Move move, int playerIndex)
        {
            // Valida la mossa
            if (!IsValidMove(move, playerIndex))
            {
                Debug.LogWarning($"Invalid move from player {playerIndex}");
                return;
            }
            
            // Applica localmente
            ApplyMove(move, playerIndex);
            
            // Propaga a tutti gli altri
            string moveJson = JsonUtility.ToJson(move);
            photonView.RPC("RPC_ExecuteMove", RpcTarget.Others, playerIndex, moveJson);
        }
        
        private void RequestMoveExecution(Move move, int playerIndex)
        {
            string moveJson = JsonUtility.ToJson(move);
            photonView.RPC("RPC_RequestMove", RpcTarget.MasterClient, playerIndex, moveJson);
        }
        
        [PunRPC]
        private void RPC_RequestMove(int playerIndex, string moveJson, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            
            Move move = JsonUtility.FromJson<Move>(moveJson);
            ExecuteMoveAsMaster(move, playerIndex);
        }
        
        [PunRPC]
        private void RPC_ExecuteMove(int playerIndex, string moveJson)
        {
            Move move = JsonUtility.FromJson<Move>(moveJson);
            ApplyMove(move, playerIndex);
        }
        
        private void ApplyMove(Move move, int playerIndex)
        {
            // Applica la mossa al GameState
            // (usa RoundManager o logica esistente)
            
            Debug.Log($"Applying move from player {playerIndex}");
            
            // Notifica al TurnController per aggiornare UI
            if (turnController != null)
            {
                turnController.OnNetworkMoveReceived(move, playerIndex);
            }
        }
        
        private bool IsValidMove(Move move, int playerIndex)
        {
            // Valida usando Rules51
            // TODO: Implementare validazione completa
            return true;
        }
        
        #endregion
        
        #region Accusi
        
        public void DeclareAccuso(int playerIndex, AccusoType accusoType)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                ApplyAccuso(playerIndex, accusoType);
                photonView.RPC("RPC_DeclareAccuso", RpcTarget.Others, playerIndex, (int)accusoType);
            }
            else
            {
                photonView.RPC("RPC_RequestAccuso", RpcTarget.MasterClient, playerIndex, (int)accusoType);
            }
        }
        
        [PunRPC]
        private void RPC_RequestAccuso(int playerIndex, int accusoType, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            
            ApplyAccuso(playerIndex, (AccusoType)accusoType);
            photonView.RPC("RPC_DeclareAccuso", RpcTarget.Others, playerIndex, accusoType);
        }
        
        [PunRPC]
        private void RPC_DeclareAccuso(int playerIndex, int accusoType)
        {
            ApplyAccuso(playerIndex, (AccusoType)accusoType);
        }
        
        private void ApplyAccuso(int playerIndex, AccusoType accusoType)
        {
            Debug.Log($"Player {playerIndex} declares {accusoType}");
            
            // Applica punti accuso
            // TODO: Usare RoundManager.TryPlayerAccuso()
            
            // Aggiorna UI
            if (turnController != null)
            {
                turnController.OnNetworkAccusoReceived(playerIndex, accusoType);
            }
        }
        
        #endregion
    }
}
```

---

## 3. Network AI Controller

```csharp
using UnityEngine;
using Photon.Pun;
using Project51.Core;
using System.Collections;
using System.Collections.Generic;

namespace Project51.Networking
{
    /// <summary>
    /// Controlla i bot in multiplayer.
    /// Solo il Master Client esegue la logica AI.
    /// </summary>
    public class NetworkAIController : MonoBehaviourPunCallbacks
    {
        [Header("Settings")]
        [SerializeField] private float aiThinkingTime = 2f;
        
        private GameState gameState;
        private NetworkGameController networkController;
        private List<AIPlayer> aiPlayers = new List<AIPlayer>();
        
        private void Start()
        {
            networkController = GetComponent<NetworkGameController>();
            
            if (PhotonNetwork.IsMasterClient)
            {
                InitializeBots();
            }
        }
        
        private void InitializeBots()
        {
            var roomManager = RoomManager.Instance;
            
            for (int i = 0; i < roomManager.PlayerSlots.Length; i++)
            {
                if (roomManager.PlayerSlots[i].IsBot)
                {
                    var aiPlayer = new AIPlayer(i, gameState);
                    aiPlayers.Add(aiPlayer);
                    Debug.Log($"Initialized AI for slot {i}");
                }
            }
        }
        
        public void OnBotTurn(int botSlot)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            
            var aiPlayer = aiPlayers.Find(ai => ai.PlayerIndex == botSlot);
            if (aiPlayer == null)
            {
                Debug.LogError($"No AI found for slot {botSlot}");
                return;
            }
            
            StartCoroutine(ExecuteBotTurnWithDelay(aiPlayer));
        }
        
        private IEnumerator ExecuteBotTurnWithDelay(AIPlayer aiPlayer)
        {
            Debug.Log($"Bot {aiPlayer.PlayerIndex} thinking...");
            yield return new WaitForSeconds(aiThinkingTime);
            
            // Get valid moves
            var validMoves = Rules51.GetValidMoves(
                gameState.Players[aiPlayer.PlayerIndex].Hand,
                gameState.TableCards
            );
            
            if (validMoves.Count == 0)
            {
                Debug.LogError($"Bot {aiPlayer.PlayerIndex} has no valid moves!");
                yield break;
            }
            
            // AI selects move
            Move selectedMove = aiPlayer.SelectMove(validMoves);
            
            Debug.Log($"Bot {aiPlayer.PlayerIndex} plays: {selectedMove}");
            
            // Execute via network controller
            networkController.ExecuteMove(selectedMove, aiPlayer.PlayerIndex);
        }
    }
    
    /// <summary>
    /// Classe AI separata (da Core)
    /// </summary>
    public class AIPlayer
    {
        public int PlayerIndex { get; private set; }
        private GameState gameState;
        
        public AIPlayer(int playerIndex, GameState state)
        {
            PlayerIndex = playerIndex;
            gameState = state;
        }
        
        public Move SelectMove(List<Move> validMoves)
        {
            // Priorità: Scopa > Capture con più carte > PlayOnly
            
            // 1. Cerca Scopa
            var scopaMoves = validMoves.FindAll(m => 
                m.Type == MoveType.Capture && 
                gameState.TableCards.Count == m.CapturedCards.Count
            );
            
            if (scopaMoves.Count > 0)
            {
                return scopaMoves[Random.Range(0, scopaMoves.Count)];
            }
            
            // 2. Cerca capture con più carte
            var captureMoves = validMoves.FindAll(m => m.Type == MoveType.Capture);
            if (captureMoves.Count > 0)
            {
                captureMoves.Sort((a, b) => b.CapturedCards.Count.CompareTo(a.CapturedCards.Count));
                return captureMoves[0];
            }
            
            // 3. PlayOnly come fallback
            var playOnlyMoves = validMoves.FindAll(m => m.Type == MoveType.PlayOnly);
            if (playOnlyMoves.Count > 0)
            {
                return playOnlyMoves[Random.Range(0, playOnlyMoves.Count)];
            }
            
            // Fallback: prima mossa valida
            return validMoves[0];
        }
        
        public AccusoType? CheckForAccuso(List<Card> hand)
        {
            // Controlla Decino (priorità)
            if (AccusiChecker.IsDecino(hand))
            {
                return AccusoType.Decino;
            }
            
            // Controlla Cirulla
            if (AccusiChecker.IsCirulla(hand))
            {
                return AccusoType.Cirulla;
            }
            
            return null;
        }
    }
}
```

---

## 4. Esempi di Utilizzo

### A. Inizializzare Partita Multiplayer

```csharp
public class GameInitializer : MonoBehaviour
{
    private void Start()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("Not in a Photon room!");
            return;
        }
        
        // Attendi che tutti i giocatori siano pronti
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(WaitForPlayersReady());
        }
    }
    
    private IEnumerator WaitForPlayersReady()
    {
        while (!RoomManager.Instance.AllPlayersReady)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.Log("All players ready! Starting game...");
        
        // Inizializza NetworkGameController
        var controller = FindObjectOfType<NetworkGameController>();
        if (controller == null)
        {
            Debug.LogError("NetworkGameController not found!");
        }
    }
}
```

### B. UI per Mostrare Turno Corrente

```csharp
public class TurnIndicatorUI : MonoBehaviour
{
    [SerializeField] private TMP_Text turnText;
    
    private void Update()
    {
        if (gameState == null) return;
        
        int currentPlayer = gameState.CurrentPlayerIndex;
        var slot = RoomManager.Instance.PlayerSlots[currentPlayer];
        
        if (slot.IsHuman && slot.PhotonActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            turnText.text = "YOUR TURN";
            turnText.color = Color.green;
        }
        else if (slot.IsBot)
        {
            turnText.text = $"{slot.NickName}'s turn (Bot thinking...)";
            turnText.color = Color.yellow;
        }
        else
        {
            turnText.text = $"{slot.NickName}'s turn";
            turnText.color = Color.white;
        }
    }
}
```

---

## 5. Testing Utilities

### Debug RPC Logger

```csharp
public class RPCDebugLogger : MonoBehaviourPunCallbacks
{
    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += OnEventReceived;
    }
    
    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnEventReceived;
    }
    
    private void OnEventReceived(ExitGames.Client.Photon.EventData photonEvent)
    {
        Debug.Log($"[RPC] Event {photonEvent.Code} received");
    }
}
```

### Network Stats Display

```csharp
public class NetworkStatsUI : MonoBehaviour
{
    [SerializeField] private TMP_Text statsText;
    
    private void Update()
    {
        if (!PhotonNetwork.IsConnected)
        {
            statsText.text = "Not connected";
            return;
        }
        
        string stats = $"Ping: {PhotonNetwork.GetPing()}ms\n";
        stats += $"Region: {PhotonNetwork.CloudRegion}\n";
        stats += $"Server: {PhotonNetwork.ServerAddress}\n";
        stats += $"Players in Room: {PhotonNetwork.CurrentRoom?.PlayerCount ?? 0}\n";
        
        statsText.text = stats;
    }
}
```

---

Questi esempi ti daranno una base solida per implementare il resto del sistema multiplayer! ??
