using System.Collections.Generic;
using UnityEngine;

namespace Project51.UIV2.Data
{
    /// <summary>
    /// Dati della pagina PROFILO V2. Tutti i valori arrivano dal chiamante: nessuna statistica o
    /// trofeo e' hardcoded nel prefab. Trofei = CollectionItemViewData (Icon, Unlocked).
    /// </summary>
    public class ProfileViewData
    {
        public string PlayerName;
        public string PlayerId;
        public int Level;
        public Sprite Avatar;           // null = silhouette generica cotta in avatar_frame
        public int XpCurrent;
        public int XpMax;
        public int MatchesPlayed;
        public int Wins;
        public float WinRate = -1f;     // 0..1; < 0 = calcolata da Wins / MatchesPlayed
        public int TotalScopas;
        public int SettebelloCount;
        public int PointRecord;
        public List<CollectionItemViewData> Trophies;
        public bool IsGuest;
    }
}
