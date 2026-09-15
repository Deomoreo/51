using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UIKit
{
    [Serializable]
    public class UINavItemRefs
    {
        public Button button;
        public GameObject iconHolder;
        public TMP_Text label;
        public GameObject pill;
    }

    /// <summary>
    /// Bottom nav riutilizzabile (UI_BottomNav4): il GameObject porta un HorizontalLayoutGroup
    /// nativo con 4 LayoutElement a flexibleWidth=1 (ogni cella e' il 25% reale della larghezza
    /// disponibile, non un pixel fisso) - nessuna coordinata individuale qui. Ogni cella ha
    /// SEMPRE icona+label; quella selezionata mostra in piu' una "pill" di evidenza sovrapposta -
    /// qualunque indice puo' diventare quello selezionato, non e' piu' un caso speciale
    /// hard-coded su una singola voce come nella prima versione wireframe.
    /// </summary>
    public class UIBottomNavBar : MonoBehaviour
    {
        [SerializeField] private UINavItemRefs[] items;
        [SerializeField] private int selectedIndex;

        public event Action<int> OnItemSelected;
        public int ItemCount => items?.Length ?? 0;

        private void Awake()
        {
            for (int i = 0; i < items.Length; i++)
            {
                int idx = i;
                if (items[i].button != null)
                {
                    items[i].button.onClick.RemoveAllListeners();
                    items[i].button.onClick.AddListener(() => SelectIndex(idx));
                }
            }
            ApplyState();
        }

        public void SetLabel(int index, string text)
        {
            if (index >= 0 && index < items.Length && items[index].label != null) items[index].label.text = text;
        }

        public void SelectIndex(int index)
        {
            if (index < 0 || index >= items.Length) return;
            selectedIndex = index;
            ApplyState();
            OnItemSelected?.Invoke(index);
        }

        private void ApplyState()
        {
            for (int i = 0; i < items.Length; i++)
            {
                bool showPill = i == selectedIndex;
                if (items[i].pill != null) items[i].pill.SetActive(showPill);
                if (items[i].iconHolder != null) items[i].iconHolder.SetActive(!showPill);
                if (items[i].label != null) items[i].label.gameObject.SetActive(!showPill);
            }
        }
    }
}
