using System;
using Project51.Core;
using UnityEngine;

namespace Project51.Auth
{
    /// <summary>
    /// Gestione locale dei progressi del giocatore (EXP, Level, pendingExp).
    /// I dati sono salvati in PlayerPrefs.
    /// 
    /// REGOLE EXP:
    /// - Solo gli account registrati guadagnano EXP (i chiamanti passano 0 agli ospiti); curva e livello da Project51.Core.PlayerXp
    /// - pendingExp resta solo per i salvataggi delle versioni precedenti e si riscatta alla registrazione
    /// 
    /// USO:
    /// - PlayerProgressLocal.Instance.TryAddExp(25);
    /// - PlayerProgressLocal.Instance.OnLevelUp += (newLevel) => Debug.Log("Level up!");
    /// </summary>
    public class PlayerProgressLocal : MonoBehaviour
    {
        public static PlayerProgressLocal Instance { get; private set; }

        // Non e' in nessuna scena: senza questo l'XP ospite non veniva mai salvata ne' mostrata.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            if (Instance == null) new GameObject(nameof(PlayerProgressLocal)).AddComponent<PlayerProgressLocal>();
        }
        
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
        
        // Curva e livello massimo: PlayerXp (unica per locale, cloud e Home).
        
        #endregion
        
        #region Public Properties
        
        public int Exp => PlayerPrefs.GetInt(KEY_EXP, 0);
        public int Level => PlayerXp.LevelOf(Exp);
        public int PendingExp => PlayerPrefs.GetInt(KEY_PENDING_EXP, 0);
        public int TotalWins => PlayerPrefs.GetInt(KEY_TOTAL_WINS, 0);
        public int TotalGames => PlayerPrefs.GetInt(KEY_TOTAL_GAMES, 0);
        
        /// <summary>
        /// EXP necessari per raggiungere il prossimo livello.
        /// </summary>
        public int ExpToNextLevel => PlayerXp.XpToNext(Level);
        
        /// <summary>
        /// Progressione nel livello corrente (0.0 - 1.0).
        /// </summary>
        public float LevelProgress => (float)PlayerXp.XpInLevel(Exp) / ExpToNextLevel;
        
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
        /// Aggiunge EXP al giocatore (anche guest, cosi' la barra XP si riempie) e controlla il level up.
        /// </summary>
        /// <returns>False se amount non e' positivo.</returns>
        public bool TryAddExp(int amount)
        {
            if (amount <= 0) return false;
            AddExpInternal(amount);
            Debug.Log($"[PlayerProgress] Added {amount} EXP. Total: {Exp}, Level: {Level}");
            return true;
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
        
        #endregion
        
        #region Private Methods
        
        private void AddExpInternal(int amount)
        {
            int oldLevel = Level;
            int newExp = Exp + amount;
            
            PlayerPrefs.SetInt(KEY_EXP, newExp);
            
            // Controlla level up (il livello si ricava sempre dagli EXP, vedi Level)
            int newLevel = PlayerXp.LevelOf(newExp);
            if (newLevel > oldLevel)
            {
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
        
        private bool IsRegisteredLocal()
        {
            return PlayerPrefs.GetInt(KEY_IS_REGISTERED, 0) == 1;
        }
        
        #endregion
    }
}
