using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

namespace Project51.Networking
{
    /// <summary>
    /// Enumerazione per identificare il tipo di giocatore nella stanza.
    /// </summary>
    public enum PlayerType
    {
        Human,  // Giocatore umano online
        Bot,    // Bot AI (controllato dal Master Client)
        Empty   // Slot vuoto
    }

    /// <summary>
    /// Rappresenta un giocatore in una room multiplayer, sia umano che bot.
    /// </summary>
    public class NetworkPlayerInfo
    {
        public int Slot { get; set; }               // 0-3
        public PlayerType Type { get; set; }
        public string NickName { get; set; }
        public int PhotonActorNumber { get; set; }  // -1 per bot
        public bool IsReady { get; set; }
        public int AvatarId { get; set; }           // Per futuri avatar

        public bool IsBot => Type == PlayerType.Bot;
        public bool IsHuman => Type == PlayerType.Human;
        public bool IsEmpty => Type == PlayerType.Empty;

        public NetworkPlayerInfo(int slot)
        {
            Slot = slot;
            Type = PlayerType.Empty;
            NickName = $"Empty Slot {slot + 1}";
            PhotonActorNumber = -1;
            IsReady = false;
            AvatarId = 0;
        }

        public void SetAsBot(int botIndex)
        {
            Type = PlayerType.Bot;
            NickName = $"Bot {botIndex}";
            PhotonActorNumber = -1;
            IsReady = true; // I bot sono sempre pronti
        }

        public void SetAsHuman(Player photonPlayer)
        {
            Type = PlayerType.Human;
            NickName = photonPlayer.NickName;
            PhotonActorNumber = photonPlayer.ActorNumber;
            IsReady = false;
        }

        public void ClearSlot()
        {
            Type = PlayerType.Empty;
            NickName = $"Empty Slot {Slot + 1}";
            PhotonActorNumber = -1;
            IsReady = false;
        }
    }

    /// <summary>
    /// Modalità di gioco disponibili.
    /// </summary>
    public enum GameMode
    {
        Solo_vs_3Bots,      // 1 giocatore vs 3 bot
        Duo_vs_2Bots,       // 2 giocatori vs 2 bot
        Online_4Players,    // 4 giocatori umani online
        QuickMatch          // Matchmaking rapido (misto umani/bot)
    }

    /// <summary>
    /// Configurazione di una partita.
    /// </summary>
    public class GameConfiguration
    {
        public GameMode Mode { get; set; }
        public int MaxPlayers { get; set; } = 4;
        public bool AllowBots { get; set; } = true;
        public bool IsRanked { get; set; } = false;
        public int WinningScore { get; set; } = 51; // Punti per vincere la partita
        public string RoomCode { get; set; }        // Codice per amici (6 caratteri)

        public static GameConfiguration CreateQuickMatch()
        {
            return new GameConfiguration
            {
                Mode = GameMode.QuickMatch,
                MaxPlayers = 4,
                AllowBots = true,
                IsRanked = false
            };
        }

        public static GameConfiguration CreatePrivateRoom(string roomCode)
        {
            return new GameConfiguration
            {
                Mode = GameMode.Online_4Players,
                MaxPlayers = 4,
                AllowBots = true,
                IsRanked = false,
                RoomCode = roomCode
            };
        }

        public static GameConfiguration CreateSoloVsBots()
        {
            return new GameConfiguration
            {
                Mode = GameMode.Solo_vs_3Bots,
                MaxPlayers = 4,
                AllowBots = true,
                IsRanked = false
            };
        }
    }

    /// <summary>
    /// Costanti per le Custom Properties di Photon.
    /// </summary>
    public static class NetworkConstants
    {
        // Room Properties
        public const string ROOM_GAME_MODE = "GameMode";
        public const string ROOM_ALLOW_BOTS = "AllowBots";
        public const string ROOM_WINNING_SCORE = "WinScore";
        public const string ROOM_GAME_STARTED = "Started";
        public const string ROOM_CODE = "RoomCode";
        public const string ROOM_PLAYER_SLOTS = "PlayerSlots"; // JSON serialized

        // Player Properties
        public const string PLAYER_IS_READY = "IsReady";
        public const string PLAYER_SLOT = "Slot";
        public const string PLAYER_AVATAR_ID = "Avatar";
        public const string PLAYER_STATS_WINS = "Wins";
        public const string PLAYER_STATS_LOSSES = "Losses";
        public const string PLAYER_STATS_GAMES = "Games";

        // RPC Names
        public const string RPC_SYNC_GAME_STATE = "RPC_SyncGameState";
        public const string RPC_EXECUTE_MOVE = "RPC_ExecuteMove";
        public const string RPC_DECLARE_ACCUSO = "RPC_DeclareAccuso";
        public const string RPC_DEAL_CARDS = "RPC_DealCards";
        public const string RPC_END_ROUND = "RPC_EndRound";
        public const string RPC_GAME_OVER = "RPC_GameOver";
        public const string RPC_PLAYER_DISCONNECTED = "RPC_PlayerDisconnected";
        public const string RPC_BOT_REPLACED_PLAYER = "RPC_BotReplacedPlayer";

        // Photon Settings
        public const int MAX_PLAYERS_PER_ROOM = 4;
        public const float SEND_RATE = 20f; // Updates per secondo
        public const float SERIALIZATION_RATE = 10f;
        
        // Timeouts
        public const float PLAYER_TURN_TIMEOUT = 30f; // 30 secondi per mossa
        public const float RECONNECT_GRACE_PERIOD = 60f; // 1 minuto per riconnessione
    }

    /// <summary>
    /// Stati di connessione per il NetworkManager.
    /// </summary>
    public enum NetworkState
    {
        Disconnected,
        Connecting,
        ConnectedToMaster,
        JoiningLobby,
        InLobby,
        CreatingRoom,
        JoiningRoom,
        InRoom,
        InGame
    }
}
