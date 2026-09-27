using DG.Tweening;
using Project51.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Project51.UI51
{
    /// <summary>
    /// Pannello dal basso (SPEC §3; Impostazioni, Modalita', Amici): scrim + scheda larga 390 ancorata in basso,
    /// raggi 24 24 0 0, bordo superiore oro .4, maniglia 40x4, titolo Cinzel 18 e chiusura tonda 36.
    /// Apertura SheetUp .28 s con scrim FadeIn; chiusura speculare (SheetDown .22 s), poi si disattiva.
    /// Il blur dello sfondo dei mockup non c'e': lo scrim resta un velo scuro (SPEC §9, deviazione annotata).
    /// </summary>
    [AddComponentMenu("UI51/Bottom Sheet")]
    [DisallowMultipleComponent]
    public class BottomSheet : MonoBehaviour
    {
        [SerializeField] RectTransform m_Scrim;
        [SerializeField] RectTransform m_Sheet;
        [SerializeField] RectTransform m_Content;
        [SerializeField] TMP_Text m_Title;
        [SerializeField] TMP_Text m_Subtitle;
        [SerializeField] Button m_CloseButton;
        [SerializeField, Tooltip("Tocco sullo scrim = chiudi.")] bool m_CloseOnScrim = true;
        [SerializeField] UnityEvent m_OnOpened = new UnityEvent();
        [SerializeField] UnityEvent m_OnClosed = new UnityEvent();

        bool m_Closing;

        public RectTransform content => m_Content;
        public RectTransform sheet => m_Sheet;
        public UnityEvent onOpened => m_OnOpened;
        public UnityEvent onClosed => m_OnClosed;
        public bool isOpen => gameObject.activeSelf && !m_Closing;

        void Awake()
        {
            if (m_CloseButton != null) m_CloseButton.onClick.AddListener(Close);
            if (m_Scrim != null && m_Scrim.TryGetComponent(out Button scrim))
                scrim.onClick.AddListener(() => { if (m_CloseOnScrim) Close(); });
        }

        /// <summary>Titolo e sottotitolo; sottotitolo vuoto = riga nascosta.</summary>
        public void SetTitle(string title, string subtitle = null)
        {
            if (m_Title != null) m_Title.text = title ?? string.Empty;
            if (m_Subtitle != null)
            {
                m_Subtitle.text = subtitle ?? string.Empty;
                m_Subtitle.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            }
        }

        public void Open()
        {
            m_Closing = false;
            DOTween.Kill(this);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (m_Sheet != null) LayoutRebuilder.ForceRebuildLayoutImmediate(m_Sheet);
            if (m_Scrim != null) UIAnim.FadeIn(m_Scrim);
            if (m_Sheet != null) UIAnim.SheetUp(m_Sheet);
            m_OnOpened.Invoke();
        }

        public void Close()
        {
            if (!gameObject.activeInHierarchy || m_Closing) return;
            m_Closing = true;
            if (m_Scrim != null) UIAnim.FadeOut(m_Scrim);
            var t = m_Sheet != null ? UIAnim.SheetDown(m_Sheet) : null;
            float wait = t != null ? t.Duration() + t.Delay() : 0f;
            DOVirtual.DelayedCall(wait, Finish, true).SetId(this);
        }

        /// <summary>Chiusura immediata senza animazione (cambio schermata).</summary>
        public void Hide()
        {
            DOTween.Kill(this);
            if (m_Scrim != null) UIAnim.Stop(m_Scrim);
            if (m_Sheet != null) UIAnim.Stop(m_Sheet);
            bool wasOpen = gameObject.activeSelf;
            m_Closing = false;
            gameObject.SetActive(false);
            if (wasOpen) m_OnClosed.Invoke();
        }

        void Finish()
        {
            m_Closing = false;
            gameObject.SetActive(false);
            m_OnClosed.Invoke();
        }

        void OnDisable()
        {
            DOTween.Kill(this);
            m_Closing = false;
        }
    }
}
