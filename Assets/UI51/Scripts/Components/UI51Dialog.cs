using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51
{
    /// <summary>
    /// Dialog di conferma (SPEC §3; mockup Impostazioni "Elimina account", Profilo, Tavolo "Abbandona"):
    /// scrim .55, card a 24 px dai lati e 200 dall'alto, r22, padding 22 20 20, ombra 0 20 50 nero .6,
    /// cerchio icona 56, titolo Cinzel 19 700, testo 13 crema .65, riga di bottoni h48 r13.
    /// Variante danger: bordo e cerchio rossi, conferma #E5484D testo bianco. Variante normale: conferma oro.
    /// Entrata PopDialog .28 s + FadeIn dello scrim; uscita PopOut .18 s.
    /// </summary>
    [AddComponentMenu("UI51/Dialog")]
    [DisallowMultipleComponent]
    public class UI51Dialog : MonoBehaviour
    {
        [SerializeField] RectTransform m_Scrim;
        [SerializeField] RectTransform m_Card;
        [SerializeField] UI51Shape m_CardShape;
        [SerializeField] UI51Shape m_IconCircle;
        [SerializeField] Image m_Icon;
        [SerializeField] TMP_Text m_Title;
        [SerializeField] TMP_Text m_Text;
        [SerializeField] Button m_Confirm;
        [SerializeField] UI51Shape m_ConfirmShape;
        [SerializeField] TMP_Text m_ConfirmLabel;
        [SerializeField] Button m_Cancel;
        [SerializeField] TMP_Text m_CancelLabel;

        Action m_OnConfirm, m_OnCancel;
        bool m_Danger, m_Hiding;

        public bool isShown => gameObject.activeSelf && !m_Hiding;

        void Awake()
        {
            if (m_Confirm != null) m_Confirm.onClick.AddListener(() => Answer(true));
            if (m_Cancel != null) m_Cancel.onClick.AddListener(() => Answer(false));
        }

        /// <summary>
        /// Mostra il dialog. cancelLabel null = solo conferma. icon null = cerchio nascosto.
        /// </summary>
        public void Show(string title, string text, string confirmLabel, Action onConfirm,
            string cancelLabel = null, Action onCancel = null, Sprite icon = null, bool danger = false)
        {
            m_OnConfirm = onConfirm;
            m_OnCancel = onCancel;
            m_Danger = danger;
            m_Hiding = false;
            DOTween.Kill(this);

            if (m_Title != null) m_Title.text = title ?? string.Empty;
            if (m_Text != null)
            {
                m_Text.text = text ?? string.Empty;
                m_Text.gameObject.SetActive(!string.IsNullOrEmpty(text));
            }
            if (m_ConfirmLabel != null) m_ConfirmLabel.text = confirmLabel ?? string.Empty;
            if (m_CancelLabel != null) m_CancelLabel.text = cancelLabel ?? string.Empty;
            if (m_Cancel != null) m_Cancel.gameObject.SetActive(!string.IsNullOrEmpty(cancelLabel));
            if (m_IconCircle != null) m_IconCircle.gameObject.SetActive(icon != null);
            if (m_Icon != null)
            {
                m_Icon.sprite = icon;
                m_Icon.preserveAspect = true;
            }
            Paint();
            SetConfirmInteractable(true);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (m_Card != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Card);
                UIAnim.PopDialog(m_Card);
            }
            if (m_Scrim != null) UIAnim.FadeIn(m_Scrim, 0.2f);
        }

        /// <summary>Conferma disabilitata (es. "Elimina account" finche' non si digita la conferma).</summary>
        public void SetConfirmInteractable(bool value)
        {
            if (m_Confirm != null) m_Confirm.interactable = value;
            PaintConfirm(value);
        }

        public void Hide()
        {
            if (!gameObject.activeInHierarchy || m_Hiding) return;
            m_Hiding = true;
            if (m_Scrim != null) UIAnim.FadeOut(m_Scrim, 0.18f);
            var t = m_Card != null ? UIAnim.PopOut(m_Card) : null;
            float wait = t != null ? t.Duration() + t.Delay() : 0f;
            DOVirtual.DelayedCall(wait, () => { m_Hiding = false; gameObject.SetActive(false); }, true).SetId(this);
        }

        void Answer(bool confirmed)
        {
            if (m_Hiding) return;
            var cb = confirmed ? m_OnConfirm : m_OnCancel;
            m_OnConfirm = m_OnCancel = null;
            Hide();
            cb?.Invoke();
        }

        void Paint()
        {
            var accent = m_Danger ? UI51Tokens.Danger : UI51Tokens.Gold;
            if (m_CardShape != null) m_CardShape.borderColor = UI51Tokens.WithAlpha(accent, m_Danger ? 0.5f : 0.4f);
            if (m_IconCircle != null)
            {
                m_IconCircle.color = UI51Tokens.WithAlpha(accent, 0.12f);
                m_IconCircle.borderColor = UI51Tokens.WithAlpha(accent, 0.5f);
            }
            if (m_Icon != null) m_Icon.color = m_Danger ? UI51Tokens.DangerText : UI51Tokens.Gold;
        }

        void PaintConfirm(bool enabled)
        {
            if (m_ConfirmShape == null) return;
            if (m_Danger)
            {
                m_ConfirmShape.fill = UI51Shape.Solid(Color.white);
                m_ConfirmShape.color = enabled ? UI51Tokens.Danger : UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.25f);
                m_ConfirmShape.borderWidth = 0f;
                if (m_ConfirmLabel != null) m_ConfirmLabel.color = enabled ? Color.white : UI51Tokens.WhiteA(0.45f);
            }
            else
            {
                m_ConfirmShape.fill = UI51Tokens.GoldButtonFill();
                m_ConfirmShape.color = enabled ? Color.white : UI51Tokens.WhiteA(0.45f);
                m_ConfirmShape.borderWidth = 1f;
                m_ConfirmShape.borderColor = UI51Tokens.GoldButtonBorder;
                if (m_ConfirmLabel != null) m_ConfirmLabel.color = enabled ? UI51Tokens.OnGold : UI51Tokens.WithAlpha(UI51Tokens.OnGold, 0.6f);
            }
        }

        void OnDisable()
        {
            DOTween.Kill(this);
            m_Hiding = false;
        }
    }
}
