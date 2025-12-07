using UnityEngine;
using System;

namespace Project51.Networking
{
    /// <summary>
    /// Statistiche di un giocatore salvate localmente.
    /// In futuro: sincronizzazione con cloud/database.
    /// </summary>
    [Serializable]
    public class PlayerStats
    {
        public string PlayerId;          // ID univoco (generato alla prima apertura)
        public string Nickname;
        public int TotalGames;
        public int Wins;
        public int Losses;
        public int TotalScore;           // Punti totali segnati in tutte le partite
        public int HighestScore;         // Punteggio più alto in una singola partita
        public int TotalScopa;           // Scope totali fatte
        public int TotalDecino;          // Decini totali dichiarati
        public int TotalCirulla;         // Cirulle totali dichiarate
        public float WinRate => TotalGames > 0 ? (float)Wins / TotalGames * 100f : 0f;
        public DateTime LastPlayed;
        public DateTime FirstPlayed;
        
        // Statistiche per ranking (futuro)
        public int EloRating;            // Rating ELO (default 1000)
        public int RankedGames;
        public int RankedWins;

        public PlayerStats()
        {
            PlayerId = Guid.NewGuid().ToString();
            Nickname = GenerateDefaultNickname(); // ? Usa metodo statico
            TotalGames = 0;
            Wins = 0;
            Losses = 0;
            TotalScore = 0;
            HighestScore = 0;
            TotalScopa = 0;
            TotalDecino = 0;
            TotalCirulla = 0;
            EloRating = 1000;
            RankedGames = 0;
            RankedWins = 0;
            FirstPlayed = DateTime.Now;
            LastPlayed = DateTime.Now;
        }

        /// <summary>
        /// Genera un nickname default senza usare Random (serialization-safe).
        /// </summary>
        private static string GenerateDefaultNickname()
        {
            // Usa timestamp invece di Random per evitare serialization issues
            int suffix = (int)(DateTime.Now.Ticks % 10000);
            return $"Player{suffix}";
        }

        public void RecordGamePlayed(bool won, int finalScore, int scopaMade, int decinoMade, int cirullaMade)
        {
            TotalGames++;
            if (won)
                Wins++;
            else
                Losses++;

            TotalScore += finalScore;
            if (finalScore > HighestScore)
                HighestScore = finalScore;

            TotalScopa += scopaMade;
            TotalDecino += decinoMade;
            TotalCirulla += cirullaMade;

            LastPlayed = DateTime.Now;
        }
    }

    /// <summary>
    /// Gestisce il salvataggio e caricamento delle statistiche del giocatore.
    /// Usa PlayerPrefs per ora, in futuro può essere esteso con cloud save.
    /// </summary>
    public class PlayerDataManager : MonoBehaviour
    {
        #region Singleton

        private static PlayerDataManager _instance;
        public static PlayerDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PlayerDataManager>();
                    
                    // NON creare GameObject se non esiste - deve essere nella scena!
                    if (_instance == null)
                    {
                        Debug.LogError("PlayerDataManager not found in scene! Add it to a GameObject.");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Constants

        private const string PLAYER_DATA_KEY = "PlayerData_V1";
        private const string NICKNAME_KEY = "PlayerNickname";
        private const string PLAYER_ID_KEY = "PlayerId";

        #endregion

        #region Public Properties

        public PlayerStats CurrentStats { get; private set; }
        public bool IsDataLoaded { get; private set; }

        #endregion

        #region Events

        public event Action<PlayerStats> OnStatsUpdated;
        public event Action<PlayerStats> OnStatsLoaded;

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

            LoadPlayerData();
        }

        #endregion

        #region Load/Save

        /// <summary>
        /// Carica i dati del giocatore salvati.
        /// </summary>
        public void LoadPlayerData()
        {
            Debug.Log("Loading player data...");

            if (PlayerPrefs.HasKey(PLAYER_DATA_KEY))
            {
                try
                {
                    string json = PlayerPrefs.GetString(PLAYER_DATA_KEY);
                    CurrentStats = JsonUtility.FromJson<PlayerStats>(json);
                    Debug.Log($"Player data loaded: {CurrentStats.Nickname} - {CurrentStats.TotalGames} games");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load player data: {e.Message}");
                    CreateNewPlayerData();
                }
            }
            else
            {
                CreateNewPlayerData();
            }

            IsDataLoaded = true;
            OnStatsLoaded?.Invoke(CurrentStats);
        }

        /// <summary>
        /// Salva i dati del giocatore.
        /// </summary>
        public void SavePlayerData()
        {
            if (CurrentStats == null)
            {
                Debug.LogWarning("No player data to save");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(CurrentStats, true);
                PlayerPrefs.SetString(PLAYER_DATA_KEY, json);
                PlayerPrefs.SetString(NICKNAME_KEY, CurrentStats.Nickname);
                PlayerPrefs.SetString(PLAYER_ID_KEY, CurrentStats.PlayerId);
                PlayerPrefs.Save();

                Debug.Log("Player data saved successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save player data: {e.Message}");
            }
        }

        /// <summary>
        /// Crea nuovi dati giocatore.
        /// </summary>
        private void CreateNewPlayerData()
        {
            Debug.Log("Creating new player data");
            CurrentStats = new PlayerStats();

            // Carica nickname salvato se esiste
            if (PlayerPrefs.HasKey(NICKNAME_KEY))
            {
                CurrentStats.Nickname = PlayerPrefs.GetString(NICKNAME_KEY);
            }

            SavePlayerData();
        }

        #endregion

        #region Update Stats

        /// <summary>
        /// Registra il risultato di una partita.
        /// </summary>
        public void RecordGameResult(bool won, int finalScore, int scopaMade, int decinoMade, int cirullaMade)
        {
            if (CurrentStats == null)
            {
                Debug.LogError("Cannot record game: player data not loaded");
                return;
            }

            Debug.Log($"Recording game result: Won={won}, Score={finalScore}");
            CurrentStats.RecordGamePlayed(won, finalScore, scopaMade, decinoMade, cirullaMade);

            SavePlayerData();
            OnStatsUpdated?.Invoke(CurrentStats);
        }

        /// <summary>
        /// Aggiorna il nickname del giocatore.
        /// </summary>
        public void UpdateNickname(string newNickname)
        {
            if (string.IsNullOrWhiteSpace(newNickname))
            {
                Debug.LogWarning("Invalid nickname");
                return;
            }

            CurrentStats.Nickname = newNickname;
            SavePlayerData();
            OnStatsUpdated?.Invoke(CurrentStats);
        }

        /// <summary>
        /// Aggiorna il rating ELO (per partite ranked - futuro).
        /// </summary>
        public void UpdateEloRating(int newRating, bool won)
        {
            CurrentStats.EloRating = newRating;
            CurrentStats.RankedGames++;
            if (won)
                CurrentStats.RankedWins++;

            SavePlayerData();
            OnStatsUpdated?.Invoke(CurrentStats);
        }

        #endregion

        #region Getters

        /// <summary>
        /// Ottiene il nickname corrente.
        /// </summary>
        public string GetNickname()
        {
            return CurrentStats?.Nickname ?? "Player";
        }

        /// <summary>
        /// Ottiene l'ID giocatore univoco.
        /// </summary>
        public string GetPlayerId()
        {
            return CurrentStats?.PlayerId ?? "";
        }

        /// <summary>
        /// Formatta le statistiche per visualizzazione.
        /// </summary>
        public string GetStatsDisplay()
        {
            if (CurrentStats == null)
                return "No data";

            return $"Games: {CurrentStats.TotalGames}\n" +
                   $"Wins: {CurrentStats.Wins} ({CurrentStats.WinRate:F1}%)\n" +
                   $"Total Score: {CurrentStats.TotalScore}\n" +
                   $"Best Score: {CurrentStats.HighestScore}\n" +
                   $"Scopa: {CurrentStats.TotalScopa}\n" +
                   $"Decino: {CurrentStats.TotalDecino}\n" +
                   $"Cirulla: {CurrentStats.TotalCirulla}";
        }

        #endregion

        #region Reset/Debug

        /// <summary>
        /// Reset completo delle statistiche (per debug o opzione utente).
        /// </summary>
        [ContextMenu("Reset Player Data")]
        public void ResetPlayerData()
        {
            Debug.LogWarning("Resetting all player data!");
            PlayerPrefs.DeleteKey(PLAYER_DATA_KEY);
            PlayerPrefs.DeleteKey(NICKNAME_KEY);
            PlayerPrefs.DeleteKey(PLAYER_ID_KEY);
            PlayerPrefs.Save();

            CreateNewPlayerData();
            OnStatsUpdated?.Invoke(CurrentStats);
        }

        /// <summary>
        /// Esporta i dati in formato leggibile (per debug).
        /// </summary>
        [ContextMenu("Export Player Data")]
        public void ExportPlayerDataToLog()
        {
            if (CurrentStats == null)
            {
                Debug.Log("No player data to export");
                return;
            }

            string json = JsonUtility.ToJson(CurrentStats, true);
            Debug.Log($"Player Data:\n{json}");
        }

        #endregion

        #region Cloud Sync (Future)

        // TODO: Implementare sincronizzazione cloud
        // - Google Play Games per Android
        // - Game Center per iOS
        // - Firebase/PlayFab per cross-platform

        /// <summary>
        /// Sincronizza dati con cloud (placeholder per futuro).
        /// </summary>
        public void SyncWithCloud()
        {
            Debug.Log("Cloud sync not yet implemented");
            // TODO: Implementare
        }

        #endregion
    }
}
