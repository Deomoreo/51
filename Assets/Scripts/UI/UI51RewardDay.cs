using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>Riferimenti di una tessera dei Premi giornalieri (giorni 1-6), impostati da UI51SocialBuilder. Lo stato lo decide UI51RewardsView.</summary>
    public sealed class UI51RewardDay : MonoBehaviour
    {
        public Button button;
        public UI51Shape face, pulse;
        public TMP_Text caption, label;
        public Image icon;
        public GameObject claimPill, check;
    }
}
