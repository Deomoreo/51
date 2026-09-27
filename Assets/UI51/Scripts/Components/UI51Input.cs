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
            m_Box.color = UI51Tokens.WhiteA(Mathf.Lerp(0.06f, 0.1f, t));
            m_Box.borderColor = Color.Lerp(UI51Tokens.GoldA(0.3f), UI51Tokens.Gold, t);
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
