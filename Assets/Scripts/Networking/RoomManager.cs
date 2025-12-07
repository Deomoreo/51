using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;

namespace Project51.Networking
{
    /// <summary>
    /// Gestisce lo stato della room: giocatori, slot, bot, ready status.
    /// Funziona solo quando si è in una room Photon.
    /// </summary>
    public class RoomManager : MonoBehaviourPunCallbacks
    {
        #region Components


        #endregion

        #region Singleton

        private static RoomManager _instance;
        public static RoomManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<RoomManager>();
                    
                    // NON creare GameObject se non esiste - deve essere nella scena!
                    if (_instance == null)
                    {
                        Debug.LogError("RoomManager not found in scene! Add it to a GameObject.");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Public Properties

        public NetworkPlayerInfo[] PlayerSlots { get; private set; } = new NetworkPlayerInfo[4];
        public bool IsGameStarted { get; private set; }
        public bool AllPlayersReady => PlayerSlots.All(p => p.IsEmpty || p.IsReady);
        public int HumanPlayersCount => PlayerSlots.Count(p => p.IsHuman);
        public int BotPlayersCount => PlayerSlots.Count(p => p.IsBot);
        public int FilledSlotsCount => PlayerSlots.Count(p => !p.IsEmpty);

        #endregion

        #region Events

        public event Action OnRoomStateChanged;
        public event Action<int> OnPlayerSlotChanged; // int = slot index
        public event Action<int, bool> OnPlayerReadyChanged; // slot, ready state
        public event Action OnAllPlayersReady;
        public event Action OnGameStarting;

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
            DontDestroyOnLoad(gameObject);

            // Get PhotonView component
            if (GetComponent<PhotonView>() == null)
            {
                Debug.LogError("RoomManager requires a PhotonView component!");
            }

            InitializeSlots();
        }

        #endregion

        #region Initialization

        private void InitializeSlots()
        {
            for (int i = 0; i < 4; i++)
            {
                PlayerSlots[i] = new NetworkPlayerInfo(i);
            }
        }

        /// <summary>
        /// Chiamato quando si entra in una room per sincronizzare lo stato.
        /// </summary>
        public void OnRoomJoined()
        {
            Debug.Log("RoomManager: Initializing room state");
            
            // Reset stato game
            IsGameStarted = false;

            // NON resettare gli slot se ci sono già altri giocatori!
            // Solo inizializza se siamo i primi
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                InitializeSlots();
            }
            else
            {
                // Sincronizza con gli slot esistenti
                SyncWithExistingPlayers();
            }

            // Se sei il Master Client, inizializza la room
            if (PhotonNetwork.IsMasterClient)
            {
                InitializeRoomAsMaster();
            }
            else
            {
                // Sincronizza con lo stato esistente
                SyncRoomState();
            }

            // Assegna il giocatore locale a uno slot
            AssignLocalPlayerToSlot();

            NotifyRoomStateChanged();
        }

        private void InitializeRoomAsMaster()
        {
            Debug.Log("RoomManager: Initializing as Master Client");
            
            // Imposta proprietà room iniziali
            var props = new Hashtable
            {
                { NetworkConstants.ROOM_GAME_STARTED, false }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            // Salva stato slot iniziale
            SaveRoomSlots();
        }

        private void SyncRoomState()
        {
            if (PhotonNetwork.CurrentRoom?.CustomProperties == null)
                return;

            var props = PhotonNetwork.CurrentRoom.CustomProperties;
            
            if (props.ContainsKey(NetworkConstants.ROOM_GAME_STARTED))
            {
                IsGameStarted = (bool)props[NetworkConstants.ROOM_GAME_STARTED];
            }

            // TODO: Deserializzare e applicare lo stato degli slot salvato
        }

        /// <summary>
        /// Sincronizza gli slot con i giocatori già presenti nella room.
        /// </summary>
        private void SyncWithExistingPlayers()
        {
            Debug.Log("RoomManager: Syncing with existing players");

            // Inizializza slot vuoti
            for (int i = 0; i < 4; i++)
            {
                if (PlayerSlots[i] == null)
                    PlayerSlots[i] = new NetworkPlayerInfo(i);
            }

            // Sincronizza con i giocatori già nella room
            foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                // Controlla se il player ha già uno slot assegnato
                if (player.CustomProperties.ContainsKey(NetworkConstants.PLAYER_SLOT))
                {
                    int slot = (int)player.CustomProperties[NetworkConstants.PLAYER_SLOT];
                    
                    if (slot >= 0 && slot < PlayerSlots.Length)
                    {
                        Debug.Log($"Syncing player {player.NickName} to slot {slot}");
                        PlayerSlots[slot].SetAsHuman(player);

                        // Sync ready state
                        if (player.CustomProperties.ContainsKey(NetworkConstants.PLAYER_IS_READY))
                        {
                            PlayerSlots[slot].IsReady = (bool)player.CustomProperties[NetworkConstants.PLAYER_IS_READY];
                        }
                    }
                }
            }
        }

        #endregion

        #region Player Slot Management

        /// <summary>
        /// Assegna il giocatore locale al primo slot libero.
        /// </summary>
        private void AssignLocalPlayerToSlot()
        {
            if (!PhotonNetwork.InRoom)
                return;

            // Verifica se il giocatore è già assegnato
            int existingSlot = GetLocalPlayerSlot();
            if (existingSlot >= 0)
            {
                Debug.Log($"Local player already in slot {existingSlot}");
                return;
            }

            // Trova il primo slot libero
            int freeSlot = FindFreeSlot();
            if (freeSlot >= 0)
            {
                AssignPlayerToSlot(PhotonNetwork.LocalPlayer, freeSlot);
            }
            else
            {
                Debug.LogWarning("No free slots available!");
            }
        }

        /// <summary>
        /// Assegna un giocatore Photon a uno slot specifico.
        /// </summary>
        public void AssignPlayerToSlot(Player player, int slot)
        {
            if (slot < 0 || slot >= PlayerSlots.Length)
            {
                Debug.LogError($"Invalid slot index: {slot}");
                return;
            }

            if (!PlayerSlots[slot].IsEmpty)
            {
                Debug.LogWarning($"Slot {slot} is already occupied");
                return;
            }

            Debug.Log($"Assigning {player.NickName} to slot {slot}");
            PlayerSlots[slot].SetAsHuman(player);

            // Salva nelle proprietà del giocatore
            var props = new Hashtable
            {
                { NetworkConstants.PLAYER_SLOT, slot },
                { NetworkConstants.PLAYER_IS_READY, false }
            };
            player.SetCustomProperties(props);

            // Salva stato room
            if (PhotonNetwork.IsMasterClient)
            {
                SaveRoomSlots();
            }

            OnPlayerSlotChanged?.Invoke(slot);
            NotifyRoomStateChanged();
        }

        /// <summary>
        /// Aggiunge un bot a uno slot libero.
        /// Solo il Master Client può farlo.
        /// </summary>
        public void AddBotToSlot(int slot = -1)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("Only Master Client can add bots");
                return;
            }

            if (slot < 0)
            {
                slot = FindFreeSlot();
            }

            if (slot < 0 || slot >= PlayerSlots.Length)
            {
                Debug.LogWarning("No free slots for bot");
                return;
            }

            int botIndex = BotPlayersCount + 1;
            PlayerSlots[slot].SetAsBot(botIndex);

            Debug.Log($"Added Bot {botIndex} to slot {slot}");

            // Notifica agli altri client via RPC
            photonView.RPC(nameof(RPC_BotAdded), RpcTarget.Others, slot, botIndex);

            SaveRoomSlots();
            OnPlayerSlotChanged?.Invoke(slot);
            NotifyRoomStateChanged();
        }

        /// <summary>
        /// Rimuove un bot da uno slot.
        /// Solo il Master Client può farlo.
        /// </summary>
        public void RemoveBotFromSlot(int slot)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("Only Master Client can remove bots");
                return;
            }

            if (slot < 0 || slot >= PlayerSlots.Length)
                return;

            if (!PlayerSlots[slot].IsBot)
            {
                Debug.LogWarning($"Slot {slot} doesn't contain a bot");
                return;
            }

            Debug.Log($"Removing bot from slot {slot}");
            PlayerSlots[slot].ClearSlot();

            // Notifica agli altri client via RPC
            photonView.RPC(nameof(RPC_BotRemoved), RpcTarget.Others, slot);

            SaveRoomSlots();
            OnPlayerSlotChanged?.Invoke(slot);
            NotifyRoomStateChanged();
        }

        /// <summary>
        /// Trova il primo slot libero.
        /// </summary>
        private int FindFreeSlot()
        {
            for (int i = 0; i < PlayerSlots.Length; i++)
            {
                if (PlayerSlots[i].IsEmpty)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Trova lo slot del giocatore locale.
        /// </summary>
        public int GetLocalPlayerSlot()
        {
            if (!PhotonNetwork.InRoom)
                return -1;

            int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            
            for (int i = 0; i < PlayerSlots.Length; i++)
            {
                if (PlayerSlots[i].IsHuman && PlayerSlots[i].PhotonActorNumber == actorNumber)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Trova lo slot di un giocatore Photon specifico.
        /// </summary>
        public int GetPlayerSlot(Player player)
        {
            for (int i = 0; i < PlayerSlots.Length; i++)
            {
                if (PlayerSlots[i].IsHuman && PlayerSlots[i].PhotonActorNumber == player.ActorNumber)
                    return i;
            }
            return -1;
        }

        #endregion

        #region Ready System

        /// <summary>
        /// Imposta lo stato "ready" del giocatore locale.
        /// </summary>
        public void SetLocalPlayerReady(bool ready)
        {
            int slot = GetLocalPlayerSlot();
            if (slot < 0)
            {
                Debug.LogWarning("Local player not assigned to a slot");
                return;
            }

            SetPlayerReady(slot, ready);
        }

        /// <summary>
        /// Imposta lo stato "ready" di un giocatore.
        /// </summary>
        public void SetPlayerReady(int slot, bool ready)
        {
            if (slot < 0 || slot >= PlayerSlots.Length)
                return;

            var playerInfo = PlayerSlots[slot];
            if (playerInfo.IsEmpty)
                return;

            // I bot sono sempre ready
            if (playerInfo.IsBot)
            {
                playerInfo.IsReady = true;
                return;
            }

            Debug.Log($"Setting slot {slot} ready: {ready}");
            playerInfo.IsReady = ready;

            // Aggiorna proprietà Photon
            var props = new Hashtable
            {
                { NetworkConstants.PLAYER_IS_READY, ready }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            OnPlayerReadyChanged?.Invoke(slot, ready);
            NotifyRoomStateChanged();

            // Controlla se tutti sono pronti
            CheckAllPlayersReady();
        }

        /// <summary>
        /// Controlla se tutti i player sono ready e notifica se sì.
        /// </summary>
        private void CheckAllPlayersReady()
        {
            bool allReady = AllPlayersReady;
            int filledSlots = FilledSlotsCount;
            
            Debug.Log($"<color=cyan>CheckAllPlayersReady: AllReady={allReady}, FilledSlots={filledSlots}</color>");
            
            if (allReady && filledSlots >= 2)
            {
                Debug.Log("<color=yellow>? Triggering OnAllPlayersReady event!</color>");
                OnAllPlayersReady?.Invoke();
            }
        }

        #endregion

        #region Game Start

        /// <summary>
        /// Avvia la partita (solo Master Client).
        /// </summary>
        public void StartGame()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("Only Master Client can start the game");
                return;
            }

            if (FilledSlotsCount < 2)
            {
                Debug.LogWarning("Need at least 2 players to start");
                return;
            }

            if (!AllPlayersReady)
            {
                Debug.LogWarning("Not all players are ready");
                return;
            }

            Debug.Log("<color=green>=== STARTING GAME ===</color>");
            IsGameStarted = true;

            // Imposta proprietà room
            var props = new Hashtable
            {
                { NetworkConstants.ROOM_GAME_STARTED, true }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            // Chiudi la room (no nuovi player durante la partita)
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;

            OnGameStarting?.Invoke();

            // Carica la scena di gioco tramite SceneLoadManager
            if (SceneLoadManager.Instance != null)
            {
                SceneLoadManager.Instance.LoadGameScene();
            }
            else
            {
                Debug.LogError("SceneLoadManager not found! Cannot load game scene.");
            }
        }

        #endregion

        #region Room Properties Sync

        /// <summary>
        /// Salva lo stato degli slot nelle proprietà della room.
        /// </summary>
        private void SaveRoomSlots()
        {
            if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
                return;

            // TODO: Serializza PlayerSlots in JSON e salva in custom properties
            // Per ora usiamo solo le proprietà dei singoli player
        }

        #endregion

        #region Disconnect/Reconnect Handling

        /// <summary>
        /// Gestisce la disconnessione di un giocatore.
        /// </summary>
        public void OnPlayerDisconnected(Player disconnectedPlayer)
        {
            int slot = GetPlayerSlot(disconnectedPlayer);
            if (slot < 0)
            {
                Debug.LogWarning($"Player {disconnectedPlayer.NickName} not found in any slot!");
                return;
            }

            Debug.Log($"<color=orange>Player {disconnectedPlayer.NickName} disconnected from slot {slot}</color>");

            // Se la partita è iniziata, sostituisci con un bot
            if (IsGameStarted && PhotonNetwork.IsMasterClient)
            {
                ReplaceDisconnectedPlayerWithBot(slot);
            }
            else
            {
                // Altrimenti libera lo slot COMPLETAMENTE
                Debug.Log($"Clearing slot {slot} (was: {PlayerSlots[slot].NickName})");
                PlayerSlots[slot].ClearSlot();
                
                // IMPORTANTE: Notifica il cambio stato per aggiornare AllPlayersReady
                OnPlayerSlotChanged?.Invoke(slot);
                
                // Verifica se tutti i rimanenti sono ancora ready
                Debug.Log($"Checking all players ready after disconnect. Filled slots: {FilledSlotsCount}, All ready: {AllPlayersReady}");
                CheckAllPlayersReady();
            }

            NotifyRoomStateChanged();
        }

        /// <summary>
        /// Sostituisce un giocatore disconnesso con un bot.
        /// </summary>
        private void ReplaceDisconnectedPlayerWithBot(int slot)
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            Debug.Log($"Replacing disconnected player in slot {slot} with bot");
            
            int botIndex = BotPlayersCount + 1;
            PlayerSlots[slot].SetAsBot(botIndex);

            // Notifica agli altri client via RPC
            photonView.RPC(NetworkConstants.RPC_BOT_REPLACED_PLAYER, RpcTarget.Others, slot, botIndex);

            OnPlayerSlotChanged?.Invoke(slot);
            NotifyRoomStateChanged();
        }

        #endregion

        #region Photon Callbacks

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"RoomManager: Player entered - {newPlayer.NickName}");
            
            // Sincronizza gli slot con il nuovo player
            // Se il player ha già uno slot assegnato (reconnect), usalo
            if (newPlayer.CustomProperties.ContainsKey(NetworkConstants.PLAYER_SLOT))
            {
                int slot = (int)newPlayer.CustomProperties[NetworkConstants.PLAYER_SLOT];
                if (slot >= 0 && slot < PlayerSlots.Length && PlayerSlots[slot].IsEmpty)
                {
                    PlayerSlots[slot].SetAsHuman(newPlayer);
                    OnPlayerSlotChanged?.Invoke(slot);
                }
            }
            else
            {
                // Nuovo player: assegna a uno slot libero
                int freeSlot = FindFreeSlot();
                if (freeSlot >= 0)
                {
                    AssignPlayerToSlot(newPlayer, freeSlot);
                }
            }

            NotifyRoomStateChanged();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"RoomManager: Player left - {otherPlayer.NickName}");
            OnPlayerDisconnected(otherPlayer);
            
            // IMPORTANTE: Forza un refresh dello stato UI per tutti
            NotifyRoomStateChanged();
            
            // Notifica cambio ready status per aggiornare UI
            OnPlayerReadyChanged?.Invoke(-1, false);
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // Aggiorna stato ready se cambiato
            if (changedProps.ContainsKey(NetworkConstants.PLAYER_IS_READY))
            {
                int slot = GetPlayerSlot(targetPlayer);
                if (slot >= 0)
                {
                    bool ready = (bool)changedProps[NetworkConstants.PLAYER_IS_READY];
                    PlayerSlots[slot].IsReady = ready;
                    OnPlayerReadyChanged?.Invoke(slot, ready);
                }
            }
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey(NetworkConstants.ROOM_GAME_STARTED))
            {
                IsGameStarted = (bool)propertiesThatChanged[NetworkConstants.ROOM_GAME_STARTED];
                
                if (IsGameStarted)
                {
                    OnGameStarting?.Invoke();
                }
            }

            NotifyRoomStateChanged();
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.Log($"Master Client switched to {newMasterClient.NickName}");
            
            if (PhotonNetwork.IsMasterClient)
            {
                // Nuovo Master Client: assume il controllo dei bot
                Debug.Log("Taking over bot control as new Master Client");
            }
        }

        #endregion

        #region RPCs

        [PunRPC]
        private void RPC_BotAdded(int slot, int botIndex)
        {
            Debug.Log($"Received RPC: Bot added to slot {slot}");
            PlayerSlots[slot].SetAsBot(botIndex);
            OnPlayerSlotChanged?.Invoke(slot);
            NotifyRoomStateChanged();
        }

        [PunRPC]
        private void RPC_BotRemoved(int slot)
        {
            Debug.Log($"Received RPC: Bot removed from slot {slot}");
            PlayerSlots[slot].ClearSlot();
            OnPlayerSlotChanged?.Invoke(slot);
            NotifyRoomStateChanged();
        }

        [PunRPC]
        private void RPC_BotReplacedPlayer(int slot, int botIndex)
        {
            Debug.Log($"Received RPC: Bot replaced player in slot {slot}");
            PlayerSlots[slot].SetAsBot(botIndex);
            OnPlayerSlotChanged?.Invoke(slot);
            NotifyRoomStateChanged();
        }

        #endregion

        #region Helper Methods

        private void NotifyRoomStateChanged()
        {
            OnRoomStateChanged?.Invoke();
        }

        /// <summary>
        /// Ottiene informazioni debug sulla room.
        /// </summary>
        public string GetRoomDebugInfo()
        {
            if (!PhotonNetwork.InRoom)
                return "Not in room";

            string info = $"Room: {PhotonNetwork.CurrentRoom.Name}\n";
            info += $"Players: {HumanPlayersCount} humans, {BotPlayersCount} bots\n";
            info += $"Game Started: {IsGameStarted}\n";
            info += $"All Ready: {AllPlayersReady}\n\n";

            for (int i = 0; i < PlayerSlots.Length; i++)
            {
                var slot = PlayerSlots[i];
                info += $"Slot {i}: {slot.NickName} ({slot.Type}) - Ready: {slot.IsReady}\n";
            }

            return info;
        }

        #endregion
    }
}
