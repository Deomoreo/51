using System;
using DG.Tweening;
using Project51.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Project51.UI51
{
    /// <summary>
    /// Interruttore delle Impostazioni (SPEC §3, mockup Impostazioni): 46x26 r13.
    /// Acceso: pista #F3C969 senza bordo, pomello 20 #25160A centrato a x 33. Spento: pista bianco .08 con bordo
    /// oro .4, pomello 18 crema .6 centrato a x 13. Transizione 0.15 s.
    /// </summary>
    [AddComponentMenu("UI51/Toggle")]
    [DisallowMultipleComponent]
    public class UI51Toggle : MonoBehaviour, IPointerClickHandler
    {
        [Serializable] public class ToggleEvent : UnityEvent<bool> { }

        [SerializeField] bool m_IsOn;
        [SerializeField] bool m_Interactable = true;
        [SerializeField] UI51Shape m_Track;
        [SerializeField] UI51Shape m_Knob;
        [SerializeField] float m_Duration = 0.15f;
        [SerializeField] ToggleEvent m_OnValueChanged = new ToggleEvent();

        float m_T;

        public ToggleEvent onValueChanged => m_OnValueChanged;
        public bool isOn { get => m_IsOn; set => SetIsOn(value, true); }
        public bool interactable { get => m_Interactable; set => m_Interactable = value; }

        /// <summary>Cambia stato; notify false per sincronizzare la UI dai dati senza richiamare il listener.</summary>
        public void SetIsOn(bool value, bool notify = true, bool animate = true)
        {
            bool changed = m_IsOn != value;
            m_IsOn = value;
            float target = value ? 1f : 0f;
            DOTween.Kill(this);
            if (animate && isActiveAndEnabled && !Mathf.Approximately(m_T, target))
                DOVirtual.Float(m_T, target, GamePreferences.Scaled(m_Duration), Apply)
                    .SetEase(Ease.OutQuad).SetUpdate(true).SetId(this);
            else Apply(target);
            if (changed && notify) m_OnValueChanged.Invoke(value);
        }

        public void Toggle() => SetIsOn(!m_IsOn);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !m_Interactable) return;
            Toggle();
        }

        void Apply(float t)
        {
            m_T = t;
            if (m_Track != null)
            {
                // fill, non color: color e' una tinta che spegne anche il bordo (oro .4 x .08 = invisibile).
                m_Track.color = Color.white;
                m_Track.fill = UI51Shape.Solid(Color.Lerp(UI51Tokens.WhiteA(0.08f), UI51Tokens.Gold, t));
                m_Track.borderColor = UI51Tokens.GoldA(Mathf.Lerp(0.4f, 0f, t));
            }
            if (m_Knob != null)
            {
                float size = Mathf.Lerp(18f, 20f, t);
                var rt = m_Knob.rectTransform;
                rt.sizeDelta = new Vector2(size, size);
                rt.anchoredPosition = new Vector2(Mathf.Lerp(13f, 33f, t), 0f);
                m_Knob.radius = size * 0.5f;
                m_Knob.color = Color.Lerp(UI51Tokens.CreamA(0.6f), UI51Tokens.OnGold, t);
            }
        }

        void OnEnable() => Apply(m_IsOn ? 1f : 0f);

        void OnDisable() => DOTween.Kill(this);

#if UNITY_EDITOR
        void OnValidate()
        {
            // Solo riferimenti: toccare le RectTransform in OnValidate genera avvisi SendMessage.
            if (m_Track == null) m_Track = GetComponent<UI51Shape>();
        }
#endif
    }
}
