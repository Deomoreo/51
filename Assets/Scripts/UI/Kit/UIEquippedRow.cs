using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UIKit
{
    [Serializable]
    public class UIEquippedSlotRefs
    {
        public GameObject filledGroup;
        public GameObject emptyGroup;
        public Image icon;
        public TMP_Text nameLabel;
        public Button removeButton;
    }

    /// <summary>
    /// Riga di N slot "equipaggiabili" riutilizzabile (UI_EquippedRow3): il GameObject porta un
    /// HorizontalLayoutGroup nativo - padding/spacing configurabili li' come sempre, mai
    /// duplicati qui. Ogni slot ha 2 stati (filled/empty) gia' presenti nel prefab come
    /// GameObject separati, questo componente si limita ad attivare/disattivare e a scrivere
    /// icona/nome - nessuna coordinata specifica di una schermata.
    /// </summary>
    public class UIEquippedRow : MonoBehaviour
    {
        [SerializeField] private UIEquippedSlotRefs[] slots;

        public int SlotCount => slots?.Length ?? 0;

        public void SetEquipped(int index, Color iconColor, string label, Action onRemove = null)
        {
            if (index < 0 || index >= slots.Length) return;
            var s = slots[index];
            if (s.filledGroup != null) s.filledGroup.SetActive(true);
            if (s.emptyGroup != null) s.emptyGroup.SetActive(false);
            if (s.icon != null) s.icon.color = iconColor;
            if (s.nameLabel != null) s.nameLabel.text = label;
            if (s.removeButton != null)
            {
                s.removeButton.onClick.RemoveAllListeners();
                if (onRemove != null) s.removeButton.onClick.AddListener(() => onRemove());
            }
        }

        public void SetEmpty(int index)
        {
            if (index < 0 || index >= slots.Length) return;
            var s = slots[index];
            if (s.filledGroup != null) s.filledGroup.SetActive(false);
            if (s.emptyGroup != null) s.emptyGroup.SetActive(true);
        }
    }
}
