using DG.Tweening;
using Project51.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project51.UI51
{
    /// <summary>Feedback di pressione dei pulsanti UI51 (SPEC §3): scala 0.97 e riempimento un po' piu' chiaro.</summary>
    [AddComponentMenu("UI51/Press")]
    [DisallowMultipleComponent]
    public class UI51Press : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] float m_Scale = 0.97f;
        [SerializeField] float m_Brightness = 1.08f;
        [SerializeField] float m_Duration = 0.08f;
        [Tooltip("Opzionale: CanvasGroup portato a 0.5 quando il pulsante non e' interattivo (mockup: disabilitato = opacita' .5).")]
        [SerializeField] CanvasGroup m_DisabledGroup;

        Selectable m_Selectable;
        UI51Shape m_Shape;
        Vector3 m_BaseScale = Vector3.one;
        bool m_Pressed;

        void Awake()
        {
            m_Selectable = GetComponent<Selectable>();
            m_Shape = GetComponent<UI51Shape>();
            m_BaseScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (m_Selectable != null && !m_Selectable.IsInteractable()) return;
            m_Pressed = true;
            Animate(m_Scale, m_Brightness);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!m_Pressed) return;
            m_Pressed = false;
            Animate(1f, 1f);
        }

        void Animate(float scale, float brightness)
        {
            DOTween.Kill(this);
            float t = GamePreferences.Scaled(m_Duration);
            transform.DOScale(m_BaseScale * scale, t).SetUpdate(true).SetId(this);
            if (m_Shape != null)
                DOTween.To(() => m_Shape.brightness, v => m_Shape.brightness = v, brightness, t).SetUpdate(true).SetId(this);
        }

        void LateUpdate()
        {
            if (m_DisabledGroup != null) m_DisabledGroup.alpha = m_Selectable == null || m_Selectable.IsInteractable() ? 1f : 0.5f;
        }

        void OnDisable()
        {
            DOTween.Kill(this);
            m_Pressed = false;
            transform.localScale = m_BaseScale;
            if (m_Shape != null) m_Shape.brightness = 1f;
        }
    }
}
