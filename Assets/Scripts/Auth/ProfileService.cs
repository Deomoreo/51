using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

namespace Project51.Auth
{
    /// <summary>
    /// Servizio per la gestione del profilo utente su PlayFab.
    /// Gestisce nickname, livello, statistiche e altri dati del giocatore.
    /// 
    /// DATI SALVATI:
    /// - DisplayName: Nome visualizzato (via API dedicata)
    /// - Player Data (Title): Dati pubblici come avatar, country code
    /// - Statistics: Valori numerici come Level, Wins, TotalGames
    /// 
    /// NOTA SICUREZZA:
    /// - Player Data con permesso "Public" è visibile ad altri giocatori
    /// - Player Data con permesso "Private" è solo per il proprietario
    /// - Statistics sono automaticamente pubbliche per leaderboard
    /// </summary>
    public class ProfileService
    {
        // Statistiche
        private const string STAT_LEVEL = "Level";
        private const string STAT_WINS = "Wins";
        private const string STAT_TOTAL_GAMES = "TotalGames";
        private const string STAT_XP = "XP";
        public const string STAT_TOTAL_SCOPE = "TotalScope";
        public const string STAT_TROPHIES = "Trophies";
        
        // Player Data keys
        private const string DATA_AVATAR_ID = "AvatarId";
        private const string DATA_SELECTED_DECK = "SelectedDeck";
        public const string DATA_BANNER_ID = "BannerId";
        public const string DATA_FRAME_ID = "FrameId";
        public const string DATA_TITLE_ID = "TitleId";
        public const string DATA_BLOCKED = "Bloccati"; // PlayFab ID bloccati, separati da "|" (BlockList)
        // Aspetto come proprieta' del giocatore Photon: lo scrive AuthBootstrapper.PublishLook, lo legge ProfileCosmetics.ReadLook.
        public const string LookFrameKey = "fr", LookBannerKey = "bn", LookLevelKey = "lv";
        // Profilo rapido al tavolo (01/10): partite, vittorie, scope totali e id PlayFab (per Aggiungi amico, Segnala, Silenzia).
        public const string LookGamesKey = "gp", LookWinsKey = "gw", LookScopeKey = "sc", LookIdKey = "id";
        /// <summary>PlayFab ID di questa sessione d'ospite: per segnalarlo (le sanzioni degli ospiti restano sul loro dispositivo).</summary>
        public const string LookGuestIdKey = "og";
        
        // Cache locale
        private Dictionary<string, string> _playerDataCache = new Dictionary<string, string>();
        private Dictionary<string, int> _statisticsCache = new Dictionary<string, int>();
        
        public bool IsLoaded { get; private set; }
        
        // Proprietà di accesso rapido
        public string DisplayName { get; private set; }
        public int Level => GetStatistic(STAT_LEVEL, 1);
        public int Wins => GetStatistic(STAT_WINS, 0);
        public int TotalGames => GetStatistic(STAT_TOTAL_GAMES, 0);
        public int XP => GetStatistic(STAT_XP, 0);
        public string AvatarId => GetPlayerData(DATA_AVATAR_ID, "default");
        public string SelectedDeck => GetPlayerData(DATA_SELECTED_DECK, "default");
        public int TotalScope => GetStatistic(STAT_TOTAL_SCOPE, 0);
        public int Trophies => GetStatistic(STAT_TROPHIES, 0);
        public string BannerId => GetPlayerData(DATA_BANNER_ID, "notte");
        public string FrameId => GetPlayerData(DATA_FRAME_ID, "oro");
        public string TitleId => GetPlayerData(DATA_TITLE_ID, "");
        public string Blocked => GetPlayerData(DATA_BLOCKED, "");
        public void SetTitle(string id) => SetPlayerData(DATA_TITLE_ID, id);
        
        // Eventi
        public event Action OnProfileLoaded;
        public event Action OnProfileUpdated;
        public event Action<string> OnError;
        
        /// <summary>
        /// Carica il profilo completo del giocatore da PlayFab.
        /// </summary>
        /// <param name="onComplete">Callback al completamento.</param>
        public void LoadProfile(Action onComplete = null)
        {
            Debug.Log("[ProfileService] Loading player profile...");
            
            // Carica dati in parallelo
            int pendingRequests = 3;
            bool hasError = false;
            
            void CheckComplete()
            {
                pendingRequests--;
                if (pendingRequests <= 0)
                {
                    IsLoaded = !hasError;
                    if (IsLoaded)
                    {
                        Debug.Log("[ProfileService] Profile loaded successfully");
                        OnProfileLoaded?.Invoke();
                    }
                    onComplete?.Invoke();
                }
            }
            
            // 1. Carica profilo (display name)
            LoadDisplayName(() => CheckComplete(), () => { hasError = true; CheckComplete(); });
            
            // 2. Carica statistiche
            LoadStatistics(() => CheckComplete(), () => { hasError = true; CheckComplete(); });
            
            // 3. Carica player data
            LoadPlayerData(() => CheckComplete(), () => { hasError = true; CheckComplete(); });
        }
        
        /// <summary>
        /// Ottiene il profilo pubblico di un altro giocatore.
        /// </summary>
        /// <param name="playFabId">PlayFab ID del giocatore.</param>
        /// <param name="onSuccess">Callback con i dati del profilo.</param>
        /// <param name="onError">Callback in caso di errore.</param>
        public void GetPublicProfile(string playFabId, Action<PublicPlayerProfile> onSuccess, Action<string> onError = null)
        {
            var request = new GetPlayerProfileRequest
            {
                PlayFabId = playFabId,
                ProfileConstraints = new PlayerProfileViewConstraints
                {
                    ShowDisplayName = true,
                    ShowStatistics = true,
                    ShowAvatarUrl = true
                }
            };
            
            PlayFabClientAPI.GetPlayerProfile(request,
                result =>
                {
                    var profile = new PublicPlayerProfile
                    {
                        PlayFabId = playFabId,
                        DisplayName = result.PlayerProfile?.DisplayName ?? "Unknown",
                        AvatarUrl = result.PlayerProfile?.AvatarUrl
                    };
                    
                    // Estrai statistiche
                    if (result.PlayerProfile?.Statistics != null)
                    {
                        foreach (var stat in result.PlayerProfile.Statistics)
                        {
                            switch (stat.Name)
                            {
                                case STAT_LEVEL: profile.Level = stat.Value; break;
                                case STAT_WINS: profile.Wins = stat.Value; break;
                                case STAT_TOTAL_GAMES: profile.TotalGames = stat.Value; break;
                            }
                        }
                    }
                    
                    onSuccess?.Invoke(profile);
                },
                error =>
                {
                    Debug.LogWarning($"[ProfileService] Failed to get profile for {playFabId}: {error.ErrorMessage}");
                    onError?.Invoke(error.ErrorMessage);
                }
            );
        }
        
        /// <summary>
        /// Aggiorna il nickname del giocatore.
        /// </summary>
        /// <param name="newNickname">Nuovo nickname (3-25 caratteri).</param>
        /// <param name="onSuccess">Callback su successo.</param>
        /// <param name="onError">Callback con messaggio errore.</param>
        public void UpdateNickname(string newNickname, Action onSuccess = null, Action<string> onError = null)
        {
            // Validazione locale
            if (string.IsNullOrWhiteSpace(newNickname))
            {
                onError?.Invoke("Nickname cannot be empty");
                return;
            }
            
            newNickname = newNickname.Trim();
            
            if (newNickname.Length < 3 || newNickname.Length > 25)
            {
                onError?.Invoke("Nickname must be 3-25 characters");
                return;
            }
            
            var request = new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = newNickname
            };
            
            PlayFabClientAPI.UpdateUserTitleDisplayName(request,
                result =>
                {
                    DisplayName = result.DisplayName;
                    Debug.Log($"[ProfileService] Nickname updated to: {DisplayName}");
                    OnProfileUpdated?.Invoke();
                    onSuccess?.Invoke();
                },
                error =>
                {
                    string errorMsg = error.ErrorMessage;
                    
                    // Errori comuni
                    if (error.Error == PlayFabErrorCode.NameNotAvailable)
                    {
                        errorMsg = "This nickname is already taken";
                    }
                    else if (error.Error == PlayFabErrorCode.ProfaneDisplayName)
                    {
                        errorMsg = "This nickname contains inappropriate content";
                    }
                    
                    Debug.LogWarning($"[ProfileService] Failed to update nickname: {errorMsg}");
                    OnError?.Invoke(errorMsg);
                    onError?.Invoke(errorMsg);
                }
            );
        }
        
        /// <summary>
        /// Incrementa una statistica (es. dopo una vittoria).
        /// </summary>
        public void IncrementStatistic(string statName, int value = 1, Action onSuccess = null, Action<string> onError = null)
        {
            UpdateStatistic(statName, GetStatistic(statName, 0) + value, onSuccess, onError);
        }
        
        /// <summary>
        /// Imposta il valore di una statistica.
        /// </summary>
        public void UpdateStatistic(string statName, int value, Action onSuccess = null, Action<string> onError = null)
        {
            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = statName,
                        Value = value
                    }
                }
            };
            
            PlayFabClientAPI.UpdatePlayerStatistics(request,
                result =>
                {
                    _statisticsCache[statName] = value;
                    Debug.Log($"[ProfileService] Statistic {statName} updated to {value}");
                    OnProfileUpdated?.Invoke();
                    onSuccess?.Invoke();
                },
                error =>
                {
                    Debug.LogWarning($"[ProfileService] Failed to update statistic {statName}: {error.ErrorMessage}");
                    OnError?.Invoke(error.ErrorMessage);
                    onError?.Invoke(error.ErrorMessage);
                }
            );
        }
        
        /// <summary>
        /// Salva dati personalizzati del giocatore.
        /// </summary>
        /// <param name="key">Chiave del dato.</param>
        /// <param name="value">Valore (null per eliminare).</param>
        /// <param name="permission">Permesso (Public o Private).</param>
        public void SetPlayerData(string key, string value, UserDataPermission permission = UserDataPermission.Public, 
            Action onSuccess = null, Action<string> onError = null)
        {
            var data = new Dictionary<string, string>();
            
            if (value != null)
            {
                data[key] = value;
            }
            
            var request = new UpdateUserDataRequest
            {
                Data = data,
                Permission = permission
            };
            
            if (value == null)
            {
                request.KeysToRemove = new List<string> { key };
                request.Data = null;
            }
            
            PlayFabClientAPI.UpdateUserData(request,
                result =>
                {
                    if (value != null)
                    {
                        _playerDataCache[key] = value;
                    }
                    else
                    {
                        _playerDataCache.Remove(key);
                    }
                    
                    Debug.Log($"[ProfileService] Player data '{key}' updated");
                    OnProfileUpdated?.Invoke();
                    onSuccess?.Invoke();
                },
                error =>
                {
                    Debug.LogWarning($"[ProfileService] Failed to update player data: {error.ErrorMessage}");
                    OnError?.Invoke(error.ErrorMessage);
                    onError?.Invoke(error.ErrorMessage);
                }
            );
        }

        /// <summary>Invio a PlayFab di piu' chiavi in UNA richiesta. Sostituibile nei test.</summary>
        public Action<Dictionary<string, string>, Action, Action<string>> SendUserData = SendToPlayFab;

        /// <summary>
        /// Avatar, cornice e banner in una richiesta sola, con un solo esito.
        /// La cache cambia solo dopo il si' di PlayFab.
        /// </summary>
        public void SetCosmetics(string avatarId, string frameId, string bannerId,
            Action onSuccess = null, Action<string> onError = null)
        {
            var data = new Dictionary<string, string>
            {
                { DATA_AVATAR_ID, avatarId }, { DATA_FRAME_ID, frameId }, { DATA_BANNER_ID, bannerId }
            };
            SendUserData(data,
                () =>
                {
                    foreach (var pair in data) _playerDataCache[pair.Key] = pair.Value;
                    Debug.Log("[ProfileService] Cosmetics updated");
                    OnProfileUpdated?.Invoke();
                    onSuccess?.Invoke();
                },
                error =>
                {
                    Debug.LogWarning($"[ProfileService] Failed to update cosmetics: {error}");
                    OnError?.Invoke(error);
                    onError?.Invoke(error);
                });
        }

        private static void SendToPlayFab(Dictionary<string, string> data, Action onSuccess, Action<string> onError)
        {
            try
            {
                PlayFabClientAPI.UpdateUserData(
                    new UpdateUserDataRequest { Data = data, Permission = UserDataPermission.Public },
                    result => onSuccess(), error => onError(error.ErrorMessage));
            }
            catch (PlayFabException e) // sessione PlayFab assente: l'SDK lancia invece di rispondere
            {
                onError(e.Message);
            }
        }
        
        /// <summary>
        /// Salita di livello dalle partite giocate dall'ultimo ritorno in Home (0 = nessuna): la mostra LivelloSu (UI51 Fase 15).
        /// Piu' partite di fila (rivincita) allargano l'intervallo; prima che le statistiche siano caricate non si sa da dove si parte.
        /// </summary>
        public static int LevelUpFrom, LevelUpTo;

        public static void NoteLevelUp(int before, int after)
        {
            if (before <= 0 || after <= before) return;
            if (LevelUpTo <= 0) LevelUpFrom = before;
            LevelUpTo = after;
        }

        /// <summary>
        /// Statistiche dopo una partita: le scrive solo il server (premioPartita, 2.62), qui si aggiorna la copia locale.
        /// </summary>
        public void ApplyServerStats(ServerReward r)
        {
            if (r == null || !r.statistiche) return;
            NoteLevelUp(_statisticsCache.TryGetValue(STAT_LEVEL, out int before) ? before : 0, r.livello);
            _statisticsCache[STAT_TOTAL_GAMES] = r.partite;
            _statisticsCache[STAT_WINS] = r.vittorie;
            _statisticsCache[STAT_XP] = r.esperienza;
            _statisticsCache[STAT_TOTAL_SCOPE] = r.scopeTotali;
            _statisticsCache[STAT_LEVEL] = r.livello;
            OnProfileUpdated?.Invoke();
        }

        #region Private Methods
        
        private void LoadDisplayName(Action onSuccess, Action onError)
        {
            var request = new GetPlayerProfileRequest
            {
                ProfileConstraints = new PlayerProfileViewConstraints
                {
                    ShowDisplayName = true
                }
            };
            
            PlayFabClientAPI.GetPlayerProfile(request,
                result =>
                {
                    DisplayName = result.PlayerProfile?.DisplayName ?? "Player";
                    onSuccess?.Invoke();
                },
                error =>
                {
                    Debug.LogWarning($"[ProfileService] Failed to load display name: {error.ErrorMessage}");
                    OnError?.Invoke(error.ErrorMessage);
                    onError?.Invoke();
                }
            );
        }
        
        private void LoadStatistics(Action onSuccess, Action onError)
        {
            var request = new GetPlayerStatisticsRequest
            {
                StatisticNames = new List<string> { STAT_LEVEL, STAT_WINS, STAT_TOTAL_GAMES, STAT_XP, STAT_TOTAL_SCOPE, STAT_TROPHIES }
            };
            
            PlayFabClientAPI.GetPlayerStatistics(request,
                result =>
                {
                    _statisticsCache.Clear();
                    
                    foreach (var stat in result.Statistics)
                    {
                        _statisticsCache[stat.StatisticName] = stat.Value;
                    }
                    
                    // Imposta default per statistiche mancanti (nuovo utente)
                    if (!_statisticsCache.ContainsKey(STAT_LEVEL))
                    {
                        _statisticsCache[STAT_LEVEL] = 1;
                    }
                    
                    Debug.Log($"[ProfileService] Statistics loaded: Level={Level}, Wins={Wins}");
                    onSuccess?.Invoke();
                },
                error =>
                {
                    Debug.LogWarning($"[ProfileService] Failed to load statistics: {error.ErrorMessage}");
                    OnError?.Invoke(error.ErrorMessage);
                    onError?.Invoke();
                }
            );
        }
        
        private void LoadPlayerData(Action onSuccess, Action onError)
        {
            var request = new GetUserDataRequest
            {
                Keys = new List<string> { DATA_AVATAR_ID, DATA_SELECTED_DECK, DATA_BANNER_ID, DATA_FRAME_ID, DATA_TITLE_ID, DATA_BLOCKED }
            };
            
            PlayFabClientAPI.GetUserData(request,
                result =>
                {
                    _playerDataCache.Clear();
                    
                    if (result.Data != null)
                    {
                        foreach (var kvp in result.Data)
                        {
                            _playerDataCache[kvp.Key] = kvp.Value.Value;
                        }
                    }
                    
                    Debug.Log($"[ProfileService] Player data loaded: {_playerDataCache.Count} keys");
                    onSuccess?.Invoke();
                },
                error =>
                {
                    Debug.LogWarning($"[ProfileService] Failed to load player data: {error.ErrorMessage}");
                    OnError?.Invoke(error.ErrorMessage);
                    onError?.Invoke();
                }
            );
        }
        
        private int GetStatistic(string name, int defaultValue)
        {
            return _statisticsCache.TryGetValue(name, out int value) ? value : defaultValue;
        }
        
        private string GetPlayerData(string key, string defaultValue)
        {
            return _playerDataCache.TryGetValue(key, out string value) ? value : defaultValue;
        }
        
        private static int CalculateLevelFromXP(int xp) => Project51.Core.PlayerXp.LevelOf(xp);
        
        #endregion
    }
    
    /// <summary>
    /// Profilo pubblico di un giocatore (visibile da altri).
    /// </summary>
    public class PublicPlayerProfile
    {
        public string PlayFabId { get; set; }
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; }
        public int Level { get; set; } = 1;
        public int Wins { get; set; }
        public int TotalGames { get; set; }
    }
}
