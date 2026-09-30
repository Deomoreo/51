using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51
{
    /// <summary>
    /// Banner del giocatore al tavolo (SPEC §3.5, mockup Partita / Partita4): pillola r25 con banner scelto (§7),
    /// bordo 1px, ombra 0 6 14 nero .45, avatar con cornice, nome Nunito 800 crema, livello crema .55 e chip
    /// "carte prese" (bianco .06, mini dorso + numero 800 crema .8).
    /// Varianti (misure nel builder): proprio 186x50 (avatar 38, nome 12, livello 10, chip h22),
    /// avversario 120x50 (avatar 36, nome 11, livello 9 + chip h16 sulla stessa riga), verticale 64x100 r20
    /// (avatar 40, nome 10, chip h16, senza livello).
    /// Turno: bordo oro .85 + anello che pulsa (1.8 s, 7 px, .55) su una forma dietro al fondo.
    /// </summary>
    [AddComponentMenu("UI51/Player Banner")]
    [DisallowMultipleComponent]
    public class PlayerBanner : MonoBehaviour
    {
        public const string LevelFormat = "Liv. {0}";

        [SerializeField] UI51Shape m_PulseRing;
        [SerializeField] UI51Shape m_Background;
        [SerializeField] AvatarFrame m_Avatar;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text m_Level;
        [SerializeField] GameObject m_Chip;
        [SerializeField] Image m_CardBack;
        [SerializeField] TMP_Text m_Captures;
        [SerializeField] BannerStyle m_Style = BannerStyle.Notte;

        [Header("Tavolo (opzionali, fuori dalla pillola): gettone del mazziere e scope dietro al banner")]
        [SerializeField] GameObject m_Dealer;
        [SerializeField] Image[] m_Scope = new Image[0];
        [SerializeField] UI51Badge m_ScopeMore;

        bool m_IsTurn;
        bool m_Picking;
        bool m_ChipOn = true;

        public AvatarFrame avatar => m_Avatar;
        public BannerStyle style => m_Style;
        public bool isTurn => m_IsTurn;

        void OnEnable()
        {
            // Il link KillOnDisable ferma l'anello quando il banner si spegne: si riaccende qui.
            if (m_IsTurn) UIAnim.Pulse(m_PulseRing);
        }

        void OnDisable()
        {
            if (m_PulseRing != null) UIAnim.Stop(m_PulseRing);
        }

        /// <summary>Dati del giocatore. level o captures negativi = elemento nascosto (dato non disponibile).</summary>
        public void Bind(string playerName, int level, int captures, BannerStyle bannerStyle, Sprite avatarSprite)
        {
            SetName(playerName);
            SetLevel(level);
            SetCaptures(captures);
            SetStyle(bannerStyle);
            if (m_Avatar != null) m_Avatar.SetAvatar(avatarSprite);
        }

        public void SetName(string playerName)
        {
            if (nameText != null) nameText.text = playerName ?? string.Empty;
        }

        public void SetLevel(int level) => SetInfo(level >= 0 ? string.Format(LevelFormat, level) : null);

        /// <summary>Riga sotto il nome (di solito il livello); vuoto = nascosta.</summary>
        public void SetInfo(string text)
        {
            if (m_Level == null) return;
            bool on = !string.IsNullOrEmpty(text);
            if (on) m_Level.text = text;
            if (m_Level.gameObject.activeSelf != on) m_Level.gameObject.SetActive(on);
        }

        /// <summary>Carte prese nel chip; negativo = chip nascosto.</summary>
        public void SetCaptures(int count)
        {
            m_ChipOn = count >= 0;
            if (m_ChipOn && m_Captures != null) m_Captures.text = count.ToString();
            ShowInfo();
        }

        /// <summary>
        /// Scelta delle emoticon aperta nel banner (mockup Partita): nome, livello e chip lasciano il posto alla fila di
        /// emoticon, l'avatar resta. Tiene anche se nel frattempo arrivano nuove carte prese.
        /// </summary>
        public void SetPicking(bool on)
        {
            m_Picking = on;
            ShowInfo();
        }

        void ShowInfo()
        {
            bool chip = m_ChipOn && !m_Picking;
            if (m_Chip != null && m_Chip.activeSelf != chip) m_Chip.SetActive(chip);
            // Colonna nome + livello del banner proprio; nelle varianti senza colonna il nome sta accanto all'avatar e resta.
            var info = nameText != null ? nameText.transform.parent : null;
            if (info != null && info.name == "Info" && info.gameObject.activeSelf == m_Picking) info.gameObject.SetActive(!m_Picking);
        }

        /// <summary>Dorso del mazzo in uso (mini carta del chip).</summary>
        public void SetCardBack(Sprite sprite)
        {
            if (m_CardBack == null || sprite == null) return;
            m_CardBack.sprite = sprite;
            m_CardBack.preserveAspect = true;
        }

        public void SetStyle(BannerStyle bannerStyle)
        {
            m_Style = bannerStyle;
            UI51Banners.Apply(m_Background, bannerStyle);
            PaintBorder();
        }

        public void SetFrame(FrameStyle frame)
        {
            if (m_Avatar != null) m_Avatar.SetFrame(frame);
        }

        /// <summary>Tempo del proprio turno 0..1 attorno all'avatar; negativo = nascosto.</summary>
        public void SetTimer(float fraction)
        {
            if (m_Avatar != null) m_Avatar.SetTimer(fraction);
        }

        /// <summary>Emoticon ricevuta (indice di rete 0..5) al posto dell'avatar.</summary>
        public void ShowEmoticon(int index)
        {
            if (m_Avatar != null) m_Avatar.ShowEmoticon(index);
        }

        /// <summary>Tocca a questo giocatore: bordo oro .85 e anello che pulsa.</summary>
        public void SetTurn(bool on)
        {
            if (m_IsTurn == on) return;
            m_IsTurn = on;
            PaintBorder();
            if (m_PulseRing == null) return;
            if (on && isActiveAndEnabled) UIAnim.Pulse(m_PulseRing);
            else if (!on) UIAnim.Stop(m_PulseRing);
        }

        /// <summary>Gettone "M": questo giocatore e' il mazziere della smazzata.</summary>
        public void SetDealer(bool on)
        {
            if (m_Dealer == null || m_Dealer.activeSelf == on) return;
            m_Dealer.SetActive(on);
            if (on) UIAnim.Pop((RectTransform)m_Dealer.transform);
        }

        /// <summary>Scope dietro al banner (SPEC §5): una carta per posto, oltre i posti il badge "+N".</summary>
        public void SetScope(IReadOnlyList<Sprite> cards)
        {
            int count = cards != null ? cards.Count : 0;
            for (int i = 0; i < m_Scope.Length; i++)
            {
                if (m_Scope[i] == null) continue;
                bool on = i < count;
                if (on) m_Scope[i].sprite = cards[i];
                if (m_Scope[i].gameObject.activeSelf != on) m_Scope[i].gameObject.SetActive(on);
            }
            if (m_ScopeMore != null) m_ScopeMore.SetCount(count - m_Scope.Length);
        }

        void PaintBorder()
        {
            if (m_Background != null)
                m_Background.borderColor = m_IsTurn ? UI51Tokens.GoldA(0.85f) : UI51Banners.BorderColor(m_Style);
        }
    }
}
