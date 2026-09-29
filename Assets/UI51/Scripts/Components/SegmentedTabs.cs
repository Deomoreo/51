using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Project51.UI51
{
    /// <summary>
    /// Schede a segmenti (SPEC §3; Collezione, Amici, Premi): contenitore r12 bianco .04 con bordo oro .18,
    /// padding e spazio 4. Scheda h36 r9: attiva oro .16 con bordo oro .7 e testo #F3C969 Nunito 12 800,
    /// inattiva trasparente con testo crema .65 Nunito 12 700. Cambio di stato istantaneo come nei mockup.
    /// </summary>
    [AddComponentMenu("UI51/Segmented Tabs")]
    [DisallowMultipleComponent]
    public class SegmentedTabs : MonoBehaviour
    {
        [Serializable] public class TabEvent : UnityEvent<int> { }

        [Serializable]
        public class Tab
        {
            public UI51Shape shape;
            public Button button;
            public TMP_Text label;
        }

        [SerializeField] List<Tab> m_Tabs = new List<Tab>();
        [SerializeField] int m_SelectedIndex;
        [SerializeField] TabEvent m_OnTabChanged = new TabEvent();

        public TabEvent onTabChanged => m_OnTabChanged;
        public int selectedIndex => m_SelectedIndex;
        public int count => m_Tabs.Count;

        void Awake()
        {
            for (int i = 0; i < m_Tabs.Count; i++) Wire(i);
        }

        void OnEnable() => Refresh();

        /// <summary>Seleziona la scheda; notify false quando la UI si sincronizza dai dati.</summary>
        public void Select(int index, bool notify = true)
        {
            if (m_Tabs.Count == 0) return;
            index = Mathf.Clamp(index, 0, m_Tabs.Count - 1);
            bool changed = index != m_SelectedIndex;
            m_SelectedIndex = index;
            Refresh();
            if (changed && notify) m_OnTabChanged.Invoke(index);
        }

        /// <summary>
        /// Imposta le etichette: clona la prima scheda se ne servono di piu', nasconde quelle in eccesso.
        /// </summary>
        public void SetLabels(IList<string> labels)
        {
            if (labels == null || m_Tabs.Count == 0) return;
            var template = m_Tabs[0];
            while (m_Tabs.Count < labels.Count && template.shape != null)
            {
                var go = Instantiate(template.shape.gameObject, template.shape.transform.parent);
                go.name = "Tab" + m_Tabs.Count;
                var tab = new Tab
                {
                    shape = go.GetComponent<UI51Shape>(),
                    button = go.GetComponent<Button>(),
                    label = go.GetComponentInChildren<TMP_Text>(true),
                };
                if (tab.button != null) tab.button.onClick.RemoveAllListeners();
                m_Tabs.Add(tab);
                Wire(m_Tabs.Count - 1);
            }
            for (int i = 0; i < m_Tabs.Count; i++)
            {
                var root = m_Tabs[i].shape != null ? m_Tabs[i].shape.gameObject : null;
                if (root != null) root.SetActive(i < labels.Count);
                if (i < labels.Count && m_Tabs[i].label != null) m_Tabs[i].label.text = labels[i];
            }
            if (m_SelectedIndex >= labels.Count) m_SelectedIndex = 0;
            Refresh();
        }

        void Wire(int index)
        {
            var b = m_Tabs[index].button;
            if (b != null) b.onClick.AddListener(() => Select(index));
        }

        void Refresh()
        {
            for (int i = 0; i < m_Tabs.Count; i++) Paint(m_Tabs[i], i == m_SelectedIndex);
        }

        static void Paint(Tab tab, bool active)
        {
            if (tab.shape != null)
            {
                // color e' una tinta su tutta la forma, bordo compreso: il colore va nel riempimento.
                tab.shape.fill = UI51Shape.Solid(UI51Tokens.GoldA(0.16f));
                tab.shape.borderColor = UI51Tokens.GoldA(0.7f);
                tab.shape.color = active ? Color.white : Color.clear;
            }
            if (tab.label != null)
            {
                tab.label.color = active ? UI51Tokens.Gold : UI51Tokens.CreamA(0.65f);
                // Un asset per peso (800 / 700), niente grassetto sintetico.
                var font = UI51Tokens.Font(active ? FontFace.NunitoExtraBold : FontFace.NunitoBold);
                if (font != null) tab.label.font = font;
            }
        }
    }
}
