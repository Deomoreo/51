using System;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Components
{
    [Serializable]
    public class UIV2NavItemData
    {
        public string Id;
        public string Label;
        public Sprite Icon;
    }

    [Serializable]
    public class UIV2NavItemRefs
    {
        public Button Button;
        public Image Icon;
        public LayoutElement IconLayoutElement;
        public TMP_Text Label;
        public GameObject SelectedIndicator;
    }

    /// <summary>
    /// Bottom nav data-driven (Home/Cards/Shop/Profile): le celle sono slot gia' presenti
    /// nel prefab (stessa idea del vecchio UIBottomNavBar), ma etichette/icone vengono
    /// scritte da SetItems invece di essere testo statico nel prefab, cosi' l'ordine o il
    /// numero di tab puo' cambiare senza toccare la UI a mano.
    /// </summary>
    public class UIV2BottomNav : MonoBehaviour
    {
        [SerializeField] private UIV2NavItemRefs[] slots;

        // Visual calibration 2026-09-13: la tab selezionata deve "emergere" col mockup, non
        // solo cambiare sfondo - l'icona selezionata e' visibilmente piu' grande delle altre.
        // Rivisto 2026-09-13 (pass fedelta' home_B2): nel mockup le icone hanno quasi la stessa
        // taglia, la tab attiva emerge per la linguetta oro + label oro, non per l'icona gigante.
        private const float NormalIconSize = 62f;
        private const float SelectedIconSize = 72f;

        [SerializeField] private Color normalLabelColor = new Color32(148, 172, 202, 255);
        [SerializeField] private Color selectedLabelColor = new Color32(255, 224, 140, 255);

        private int _selectedIndex;

        public event Action<int> OnItemSelected;

        public void SetItems(IReadOnlyList<UIV2NavItemData> items)
        {
            if (slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.Button == null) continue;

                slot.Button.onClick.RemoveAllListeners();

                if (items != null && i < items.Count)
                {
                    var data = items[i];
                    slot.Button.gameObject.SetActive(true);
                    // Se il chiamante non passa un'icona, resta quella di default gia' nel
                    // prefab (stesso pattern di UIV2ResourcePill/UIV2StatTile) invece di
                    // cancellarla.
                    if (slot.Icon != null && data.Icon != null) slot.Icon.sprite = data.Icon;
                    if (slot.Label != null) slot.Label.text = data.Label;

                    int idx = i;
                    slot.Button.onClick.AddListener(() => SelectIndex(idx));
                }
                else
                {
                    slot.Button.gameObject.SetActive(false);
                }
            }

            ApplyVisualState();
        }

        public void SelectIndex(int index, bool notify = true)
        {
            if (slots == null || index < 0 || index >= slots.Length) return;
            _selectedIndex = index;
            ApplyVisualState();
            if (notify) OnItemSelected?.Invoke(index);
        }

        private void ApplyVisualState()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;

                bool selected = i == _selectedIndex;
                if (slot.SelectedIndicator != null) slot.SelectedIndicator.SetActive(selected);

                float size = selected ? SelectedIconSize : NormalIconSize;
                if (slot.IconLayoutElement != null)
                {
                    slot.IconLayoutElement.preferredWidth = NormalIconSize;
                    slot.IconLayoutElement.preferredHeight = NormalIconSize;
                }
                if (slot.Icon != null)
                {
                    slot.Icon.rectTransform.sizeDelta = Vector2.one * NormalIconSize;
                    slot.Icon.rectTransform.DOKill();
                    if (Application.isPlaying) slot.Icon.rectTransform.DOScale(size / NormalIconSize, .2f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(slot.Icon.gameObject);
                    else slot.Icon.rectTransform.localScale = Vector3.one * (size / NormalIconSize);
                }
                if (slot.Label != null) slot.Label.color = selected ? selectedLabelColor : normalLabelColor;
            }
        }
    }
}
