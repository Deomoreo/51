using System;
using UnityEngine;

namespace Project51.Auth
{
    /// <summary>
    /// Gestione locale dei progressi del giocatore (EXP, Level, pendingExp).
    /// I dati sono salvati in PlayerPrefs.
    /// 
    /// REGOLE EXP:
    /// - Utenti registrati: EXP aggiunto normalmente
    /// - Utenti guest (non registrati): EXP accumulato in pendingExp
    /// - Al momento della registrazione: pendingExp viene convertito in EXP reale
    /// 
    /// USO:
    /// - PlayerProgressLocal.Instance.TryAddExp(25);
    /// - PlayerProgressLocal.Instance.OnLevelUp += (newLevel) => Debug.Log("Level up!");
    /// </summary>
    public class PlayerProgressLocal : MonoBehaviour
    {
        public static PlayerProgressLocal Instance { get; private set; }
        
        #region PlayerPrefs Keys
        
        private const string KEY_EXP = "progress_exp";
        private const string KEY_LEVEL = "progress_level";
        private const string KEY_PENDING_EXP = "progress_pendingExp";
        private const string KEY_TOTAL_WINS = "progress_wins";
        private const string KEY_TOTAL_GAMES = "progress_totalGames";

        // Stato Auth (salvato da PlayFabAuthService)
        private const string KEY_IS_REGISTERED = "Project51_IsRegistered";
        
        #endregion
        
        #region Configuration
        
        [Header("Leveling Configuration")]
        [Tooltip("EXP base richiesta per il primo livello")]
        [SerializeField] private int baseExpPerLevel = 100;
        
        [Tooltip("Moltiplicatore EXP per ogni livello (es: 1.2 = +20% per livello)")]
        [SerializeField] private float expMultiplierPerLevel = 1.15f;
        
        [Tooltip("Livello massimo raggiungibile")]
        [SerializeField] private int maxLevel = 100;
        
        #endregion
        
        #region Public Properties
        
        public int Exp => PlayerPrefs.GetInt(KEY_EXP, 0);
        public int Level => PlayerPrefs.GetInt(KEY_LEVEL, 1);
        public int PendingExp => PlayerPrefs.GetInt(KEY_PENDING_EXP, 0);
        public int TotalWins => PlayerPrefs.GetInt(KEY_TOTAL_WINS, 0);
        public int TotalGames => PlayerPrefs.GetInt(KEY_TOTAL_GAMES, 0);
        
        /// <summary>
        /// EXP necessari per raggiungere il prossimo livello.
        /// </summary>
        public int ExpToNextLevel => CalculateExpForLevel(Level + 1) - CalculateExpForLevel(Level);
        
        /// <summary>
        /// Progressione nel livello corrente (0.0 - 1.0).
        /// </summary>
        public float LevelProgress
        {
            get
            {
                int expForCurrentLevel = CalculateExpForLevel(Level);
                int expForNextLevel = CalculateExpForLevel(Level + 1);
                int expInCurrentLevel = Exp - expForCurrentLevel;
                int expNeeded = expForNextLevel - expForCurrentLevel;
                return expNeeded > 0 ? (float)expInCurrentLevel / expNeeded : 1f;
            }
        }
        
        /// <summary>
        /// True se l'utente ha EXP in pending (guadagnato come guest).
        /// </summary>
        public bool HasPendingExp => PendingExp > 0;
        
        #endregion
        
        #region Events
        
        /// <summary>Invocato quando l'utente sale di livello. Parametro: nuovo livello.</summary>
        public event Action<int> OnLevelUp;
        
        /// <summary>Invocato quando l'EXP cambia. Parametri: exp totale, exp guadagnato.</summary>
        public event Action<int, int> OnExpChanged;
        
        /// <summary>Invocato quando pendingExp cambia (per utenti guest).</summary>
        public event Action<int> OnPendingExpChanged;
        
        /// <summary>Invocato quando pendingExp viene riscattato dopo registrazione.</summary>
        public event Action<int> OnPendingExpClaimed;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Inizializza livello se primo avvio
            if (!PlayerPrefs.HasKey(KEY_LEVEL))
            {
                PlayerPrefs.SetInt(KEY_LEVEL, 1);
                PlayerPrefs.Save();
            }
        }
        
        private void Start()
        {
            // Se l'utente risulta già registrato (flag locale), riscatta subito pendingExp.
            if (IsRegisteredLocal() && HasPendingExp)
            {
                ClaimPendingExp();
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Tenta di aggiungere EXP al giocatore.
        /// - Se registrato: aggiunge EXP e controlla level up
        /// - Se guest: accumula in pendingExp
        /// </summary>
        /// <param name="amount">Quantità di EXP da aggiungere.</param>
        /// <returns>True se EXP aggiunto normalmente, false se accumulato come pending.</returns>
        public bool TryAddExp(int amount)
        {
            if (amount <= 0) return false;
            
            bool isRegistered = IsRegisteredLocal();
            
            if (isRegistered)
            {
                AddExpInternal(amount);
                Debug.Log($"[PlayerProgress] Added {amount} EXP. Total: {Exp}, Level: {Level}");
                return true;
            }
            else
            {
                // Utente guest: accumula EXP in pending
                int newPending = PendingExp + amount;
                PlayerPrefs.SetInt(KEY_PENDING_EXP, newPending);
                PlayerPrefs.Save();
                
                OnPendingExpChanged?.Invoke(newPending);
                Debug.Log($"[PlayerProgress] Guest user - {amount} EXP added to pending. Total pending: {newPending}");
                return false;
            }
        }
        
        /// <summary>
        /// Registra il risultato di una partita.
        /// </summary>
        /// <param name="isWin">True se ha vinto.</param>
        /// <param name="xpGained">EXP guadagnato dalla partita.</param>
        public void RecordGameResult(bool isWin, int xpGained = 0)
        {
            // Incrementa statistiche (sempre, anche per guest)
            int totalGames = TotalGames + 1;
            PlayerPrefs.SetInt(KEY_TOTAL_GAMES, totalGames);
            
            if (isWin)
            {
                int totalWins = TotalWins + 1;
                PlayerPrefs.SetInt(KEY_TOTAL_WINS, totalWins);
            }
            
            PlayerPrefs.Save();
            
            // Aggiungi EXP
            if (xpGained > 0)
            {
                TryAddExp(xpGained);
            }
            
            Debug.Log($"[PlayerProgress] Game recorded. Wins: {TotalWins}/{TotalGames}, Win: {isWin}");
        }
        
        /// <summary>
        /// Riscatta l'EXP in pending (chiamato dopo registrazione).
        /// </summary>
        public void ClaimPendingExp()
        {
            int pending = PendingExp;
            
            if (pending <= 0)
            {
                Debug.Log("[PlayerProgress] No pending EXP to claim");
                return;
            }
            
            // Resetta pending
            PlayerPrefs.SetInt(KEY_PENDING_EXP, 0);
            
            // Aggiungi EXP reale
            AddExpInternal(pending);
            PlayerPrefs.Save();
            
            OnPendingExpClaimed?.Invoke(pending);
            Debug.Log($"[PlayerProgress] Claimed {pending} pending EXP! New total: {Exp}, Level: {Level}");
        }
        
        /// <summary>
        /// Resetta tutti i progressi locali (per debug/test).
        /// </summary>
        public void ResetAllProgress()
        {
            PlayerPrefs.DeleteKey(KEY_EXP);
            PlayerPrefs.DeleteKey(KEY_LEVEL);
            PlayerPrefs.DeleteKey(KEY_PENDING_EXP);
            PlayerPrefs.DeleteKey(KEY_TOTAL_WINS);
            PlayerPrefs.DeleteKey(KEY_TOTAL_GAMES);
            PlayerPrefs.Save();
            
            Debug.Log("[PlayerProgress] All progress reset");
        }
        
        /// <summary>
        /// Calcola l'EXP totale necessario per raggiungere un certo livello.
        /// </summary>
        public int CalculateExpForLevel(int level)
        {
            if (level <= 1) return 0;
            
            int totalExp = 0;
            for (int i = 1; i < level; i++)
            {
                totalExp += Mathf.RoundToInt(baseExpPerLevel * Mathf.Pow(expMultiplierPerLevel, i - 1));
            }
            return totalExp;
        }
        
        #endregion
        
        #region Private Methods
        
        private void AddExpInternal(int amount)
        {
            int oldLevel = Level;
            int newExp = Exp + amount;
            
            PlayerPrefs.SetInt(KEY_EXP, newExp);
            
            // Controlla level up
            int newLevel = CalculateLevelFromExp(newExp);
            if (newLevel > oldLevel && newLevel <= maxLevel)
            {
                PlayerPrefs.SetInt(KEY_LEVEL, newLevel);
                
                // Notifica tutti i level up intermedi
                for (int lvl = oldLevel + 1; lvl <= newLevel; lvl++)
                {
                    OnLevelUp?.Invoke(lvl);
                    Debug.Log($"[PlayerProgress] LEVEL UP! Now level {lvl}");
                }
            }
            
            PlayerPrefs.Save();
            OnExpChanged?.Invoke(newExp, amount);
        }
        
        private int CalculateLevelFromExp(int exp)
        {
            int level = 1;
            while (level < maxLevel && exp >= CalculateExpForLevel(level + 1))
            {
                level++;
            }
            return level;
        }
        
        private bool IsRegisteredLocal()
        {
            return PlayerPrefs.GetInt(KEY_IS_REGISTERED, 0) == 1;
        }
        
        #endregion
    }
}
