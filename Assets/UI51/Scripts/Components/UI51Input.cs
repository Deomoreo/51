using DG.Tweening;
using Project51.Core;
using TMPro;
using UnityEngine;

namespace Project51.UI51
{
    /// <summary>
    /// Stile dei campi di Accesso e Registrazione (SPEC §3, mockup Main): scatola r14 bianco .06 con bordo oro .3,
    /// che al focus passa a bianco .1 e bordo oro pieno in 0.15 s. Solo grafica: il testo resta del TMP_InputField.
    /// </summary>
    [AddComponentMenu("UI51/Input")]
    [DisallowMultipleComponent]
    public class UI51Input : MonoBehaviour
    {
        [SerializeField] TMP_InputField m_Field;
        [SerializeField] UI51Shape m_Box;
        [SerializeField] float m_Duration = 0.15f;
        [Tooltip("Bordo a riposo e col focus: oro .3 -> oro pieno (rosso nella conferma Elimina account).")]
        [SerializeField] Color m_Border = new Color(243f / 255f, 201f / 255f, 105f / 255f, 0.3f);
        [SerializeField] Color m_BorderFocus = new Color(243f / 255f, 201f / 255f, 105f / 255f, 1f);

        float m_T;

        void OnEnable()
        {
            if (m_Field != null)
            {
                m_Field.onSelect.AddListener(Focus);
                m_Field.onDeselect.AddListener(Blur);
            }
            Apply(m_Field != null && m_Field.isFocused ? 1f : 0f);
        }

        void OnDisable()
        {
            if (m_Field != null)
            {
                m_Field.onSelect.RemoveListener(Focus);
                m_Field.onDeselect.RemoveListener(Blur);
            }
            DOTween.Kill(this);
        }

        void Focus(string _) => AnimateTo(1f);
        void Blur(string _) => AnimateTo(0f);

        void AnimateTo(float target)
        {
            DOTween.Kill(this);
            if (!isActiveAndEnabled) { Apply(target); return; }
            DOVirtual.Float(m_T, target, GamePreferences.Scaled(m_Duration), Apply)
                .SetEase(Ease.OutQuad).SetUpdate(true).SetId(this);
        }

        void Apply(float t)
        {
            m_T = t;
            if (m_Box == null) return;
            // fill, non color: color e' una tinta moltiplicata sul riempimento (0.06 x 0.06 = invisibile).
            m_Box.color = Color.white;
            m_Box.fill = UI51Shape.Solid(UI51Tokens.WhiteA(Mathf.Lerp(0.06f, 0.1f, t)));
            m_Box.borderColor = Color.Lerp(m_Border, m_BorderFocus, t);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (m_Field == null) m_Field = GetComponent<TMP_InputField>();
            if (m_Box == null) m_Box = GetComponent<UI51Shape>();
        }
#endif
    }
}
