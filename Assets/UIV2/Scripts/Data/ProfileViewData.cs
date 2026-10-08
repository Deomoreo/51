using UnityEngine;

namespace Project51.UIV2.Data
{
    /// <summary>
    /// Dati della pagina PROFILO. Tutti i valori arrivano dal chiamante: nessuna statistica e' scritta nella scena.
    /// </summary>
    public class ProfileViewData
    {
        public string PlayerName;
        public string PlayerId;
        public int Level;
        public Sprite Avatar;           // null = silhouette generica cotta in avatar_frame
        public string FrameId;          // id di ProfileCosmetics; vuoto = cornice iniziale
        public string BannerId;
        public int XpCurrent;
        public int XpMax;
        public int MatchesPlayed;
        public int Wins;
        public float WinRate = -1f;     // 0..1; < 0 = calcolata da Wins / MatchesPlayed
        public bool IsGuest;
        public bool HasProgress = true;
        public bool HasMatchStats = true;
        public bool CanRetry;           // B19: caricamento dell'account fallito (non in corso) -> RIPROVA
    }
}
