using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UIKit
{
    /// <summary>
    /// Singola card riutilizzabile per una griglia di collezione (UI_CollectionCard): stati
    /// unlocked / equipped / locked. Nessuna coordinata di schermata - dimensione e posizione le
    /// impone il GridLayoutGroup del genitore (UI_CollectionGrid4), questo componente scrive solo
    /// contenuto e colori di stato.
    /// </summary>
    public class UICollectionCard : MonoBehaviour
    {
        [SerializeField] private Image border;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private GameObject checkBadge;
        [SerializeField] private Color unlockedBorderColor = new Color(0.55f, 0.60f, 0.70f, 1f);
        [SerializeField] private Color equippedBorderColor = new Color(0.85f, 0.65f, 0.20f, 1f);

        public void SetUnlocked(Color iconColor, string label, bool equipped)
        {
            if (icon != null)
            {
                icon.gameObject.SetActive(true);
                icon.color = iconColor;
            }
            if (lockIcon != null) lockIcon.SetActive(false);
            if (nameLabel != null) nameLabel.text = label;
            if (border != null) border.color = equipped ? equippedBorderColor : unlockedBorderColor;
            if (checkBadge != null) checkBadge.SetActive(equipped);
        }

        public void SetLocked(string label = "Bloccata")
        {
            if (icon != null) icon.gameObject.SetActive(false);
            if (lockIcon != null) lockIcon.SetActive(true);
            if (nameLabel != null) nameLabel.text = label;
            if (border != null) border.color = unlockedBorderColor;
            if (checkBadge != null) checkBadge.SetActive(false);
        }
    }
}
