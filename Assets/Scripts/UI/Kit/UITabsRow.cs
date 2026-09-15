using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UIKit
{
    [Serializable]
    public class UITabEntry
    {
        public Button button;
        public RectTransform rect;
        public Image background;
        public TMP_Text label;
    }

    /// <summary>
    /// Riga di N tab riutilizzabile (UI_Tabs3): il GameObject porta gia' un
    /// HorizontalLayoutGroup nativo di Unity - padding/spacing restano configurabili nel suo
    /// Inspector standard, non duplicati qui. Il componente gestisce solo IL COMPORTAMENTO
    /// (quale tab e' selezionato, colori, altezza normale/selezionata) - zero coordinate X
    /// individuali: la posizione di ogni tab la calcola sempre il LayoutGroup.
    /// </summary>
    public class UITabsRow : MonoBehaviour
    {
        [SerializeField] private UITabEntry[] tabs;
        [SerializeField] private float normalHeight = 56f;
        [SerializeField] private float selectedHeight = 69f;
        [SerializeField] private Color normalColor = new Color(0.20f, 0.24f, 0.34f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.10f, 0.55f, 0.50f, 1f);
        [SerializeField] private int selectedIndex;

        public event Action<int> OnTabSelected;
        public int TabCount => tabs?.Length ?? 0;
        public int SelectedIndex => selectedIndex;

        private void Awake()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int idx = i;
                if (tabs[i].button != null)
                {
                    tabs[i].button.onClick.RemoveAllListeners();
                    tabs[i].button.onClick.AddListener(() => SelectTab(idx));
                }
            }
            ApplyState();
        }

        public void SetLabel(int index, string text)
        {
            if (index >= 0 && index < tabs.Length && tabs[index].label != null) tabs[index].label.text = text;
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= tabs.Length) return;
            selectedIndex = index;
            ApplyState();
            OnTabSelected?.Invoke(index);
        }

        private void ApplyState()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                bool isSelected = i == selectedIndex;
                var t = tabs[i];
                if (t.rect != null) t.rect.sizeDelta = new Vector2(t.rect.sizeDelta.x, isSelected ? selectedHeight : normalHeight);
                if (t.background != null) t.background.color = isSelected ? selectedColor : normalColor;
            }
        }
    }
}
