using System;
using Project51.Core;
using UnityEngine;

namespace Project51.Auth
{
    /// <summary>
    /// Copia locale dei progressi del giocatore (EXP, partite, vittorie) in PlayerPrefs.
    ///
    /// REGOLE EXP:
    /// - Solo gli account registrati guadagnano EXP (i chiamanti passano 0 agli ospiti); curva e livello da Project51.Core.PlayerXp
    ///
    /// USO:
    /// - PlayerProgressLocal.Instance.RecordGameResult(vinto, xp);
    /// - PlayerProgressLocal.Instance.OnExpChanged += (totale, guadagnati) => ...;
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
        private const string KEY_TOTAL_WINS = "progress_wins";
        private const string KEY_TOTAL_GAMES = "progress_totalGames";
        
        #endregion
        
        #region Configuration
        
        // Curva e livello massimo: PlayerXp (unica per locale, cloud e Home).
        
        #endregion
        
        #region Public Properties
        
        public int Exp => PlayerPrefs.GetInt(KEY_EXP, 0);
        public int Level => PlayerXp.LevelOf(Exp);
        public int TotalWins => PlayerPrefs.GetInt(KEY_TOTAL_WINS, 0);
        public int TotalGames => PlayerPrefs.GetInt(KEY_TOTAL_GAMES, 0);
        
        #endregion
        
        #region Events
        
        /// <summary>Invocato quando l'EXP cambia. Parametri: exp totale, exp guadagnato.</summary>
        public event Action<int, int> OnExpChanged;
        
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
                AddExpInternal(xpGained);
                Debug.Log($"[PlayerProgress] Added {xpGained} EXP. Total: {Exp}, Level: {Level}");
            }
            
            Debug.Log($"[PlayerProgress] Game recorded. Wins: {TotalWins}/{TotalGames}, Win: {isWin}");
        }
        
        #endregion
        
        #region Private Methods
        
        private void AddExpInternal(int amount)
        {
            int newExp = Exp + amount;

            PlayerPrefs.SetInt(KEY_EXP, newExp);
            PlayerPrefs.Save();
            OnExpChanged?.Invoke(newExp, amount);
        }
        
        #endregion
    }
}
