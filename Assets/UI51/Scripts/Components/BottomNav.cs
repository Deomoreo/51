using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Project51.UI51
{
    /// <summary>
    /// Barra di navigazione in basso (SPEC §3, mockup Home/Collezione/Negozio/Profilo): 390x72, fondo
    /// rgba(5,11,23,.96), bordo superiore oro .3, padding 0 12, quattro voci uguali (Gioca, Collezione, Negozio,
    /// Profilo). Voce attiva: trattino oro 24x2 in alto, icona piena, etichetta Nunito 11 700 #F3C969.
    /// Inattiva: icona .55, etichetta Nunito 11 600 crema .55. Cambio di stato istantaneo come nei mockup.
    /// </summary>
    [AddComponentMenu("UI51/Bottom Nav")]
    [DisallowMultipleComponent]
    public class BottomNav : MonoBehaviour
    {
        [Serializable] public class SelectEvent : UnityEvent<int> { }

        [Serializable]
        public class Item
        {
            public Button button;
            public GameObject dash;
            public Image icon;
            public TMP_Text label;
        }

        public const int Gioca = 0, Collezione = 1, Negozio = 2, Profilo = 3;

        [SerializeField] List<Item> m_Items = new List<Item>();
        [SerializeField] int m_SelectedIndex;
        [SerializeField] SelectEvent m_OnSelect = new SelectEvent();

        public SelectEvent onSelect => m_OnSelect;
        public int selectedIndex => m_SelectedIndex;
        public int count => m_Items.Count;

        void Awake()
        {
            for (int i = 0; i < m_Items.Count; i++)
            {
                int index = i;
                var b = m_Items[i].button;
                if (b != null) b.onClick.AddListener(() => Select(index));
            }
        }

        void OnEnable() => Refresh();

        /// <summary>
        /// Seleziona la voce. Il tocco sulla voce gia' attiva notifica comunque (es. torna in cima alla schermata);
        /// notify false quando la UI si sincronizza dalla schermata aperta.
        /// </summary>
        public void Select(int index, bool notify = true)
        {
            if (m_Items.Count == 0) return;
            m_SelectedIndex = Mathf.Clamp(index, 0, m_Items.Count - 1);
            Refresh();
            if (notify) m_OnSelect.Invoke(m_SelectedIndex);
        }

        void Refresh()
        {
            for (int i = 0; i < m_Items.Count; i++) Paint(m_Items[i], i == m_SelectedIndex);
        }

        static void Paint(Item item, bool active)
        {
            if (item.dash != null && item.dash.activeSelf != active) item.dash.SetActive(active);
            if (item.icon != null) item.icon.color = UI51Tokens.WhiteA(active ? 1f : 0.55f);
            if (item.label != null)
            {
                item.label.color = active ? UI51Tokens.Gold : UI51Tokens.CreamA(0.55f);
                var font = UI51Tokens.Font(active ? FontFace.NunitoBold : FontFace.NunitoSemiBold);
                if (font != null) item.label.font = font;
            }
        }
    }
}
