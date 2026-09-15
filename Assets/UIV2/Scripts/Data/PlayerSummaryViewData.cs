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

        // Letti da UIV2_TopBar per l'energy/XP widget (visual calibration 2026-09-13) -
        // opzionali: se Max <= 0 il rispettivo widget resta a 0 senza generare errori.
        public int EnergyCurrent;
        public int EnergyMax;
        public int XpCurrent;
        public int XpMax;
    }
}
