using TMPro;
using UnityEngine;

namespace Project51.UI51
{
    /// <summary>
    /// Pallino / contatore (SPEC §3.11). I mockup ne hanno solo due forme:
    /// Dot = pallino rosso #E5484D 9 px con bordo scuro #0B1626 1.5 px (Home, pulsanti laterali; 11/2 px sulle icone);
    /// More = pillola oro "+N" h22 min 22, padding 0 5, r11, bordo #0B1626 2 px, Nunito 10 800 #25160A
    /// (scope oltre le 4 visibili). Nessun contatore numerico rosso nei mockup: non aggiunto.
    /// Comparsa con Pop (SPEC §6: 0.25 s, scala .6 -> 1).
    /// </summary>
    [AddComponentMenu("UI51/Badge")]
    [DisallowMultipleComponent]
    public class UI51Badge : MonoBehaviour
    {
        public enum Mode { Dot, More }

        [SerializeField] Mode m_Mode = Mode.Dot;
        [SerializeField] UI51Shape m_Shape;
        [SerializeField] TMP_Text m_Label;
        [SerializeField, Tooltip("Pop alla comparsa.")] bool m_Animate = true;

        public Mode mode => m_Mode;
        public bool isVisible => gameObject.activeSelf;

        /// <summary>Dot: acceso se count > 0. More: "+count", nascosto se count <= 0.</summary>
        public void SetCount(int count)
        {
            if (m_Mode == Mode.More && m_Label != null && count > 0)
                m_Label.text = "+" + (count > 99 ? "99" : count.ToString());
            SetVisible(count > 0);
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf == visible) return;
            gameObject.SetActive(visible);
            if (visible && m_Animate) UIAnim.Pop((RectTransform)transform, 0.6f, 1.08f, 0.25f);
        }

        void OnDisable() => UIAnim.Stop(transform);

#if UNITY_EDITOR
        void OnValidate()
        {
            if (m_Shape == null) m_Shape = GetComponent<UI51Shape>();
        }
#endif
    }
}
