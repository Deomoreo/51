using UnityEngine;

namespace Project51.UIV2.Data
{
    public class PlayerSummaryViewData
    {
        public string PlayerId;
        public string DisplayName;
        public Sprite Avatar;
        public int Level;
        public bool IsLocalPlayer;

        // Barra XP della testata: XpMax <= 0 la nasconde (ospiti).
        public int XpCurrent;
        public int XpMax;
    }
}
