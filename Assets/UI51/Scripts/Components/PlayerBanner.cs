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
        [SerializeField] TMP_Text m_Name;
        [SerializeField] TMP_Text m_Level;
        [SerializeField] GameObject m_Chip;
        [SerializeField] Image m_CardBack;
        [SerializeField] TMP_Text m_Captures;
        [SerializeField] BannerStyle m_Style = BannerStyle.Notte;

        bool m_IsTurn;

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
            if (m_Name != null) m_Name.text = playerName ?? string.Empty;
        }

        public void SetLevel(int level)
        {
            if (m_Level == null) return;
            bool on = level >= 0;
            if (on) m_Level.text = string.Format(LevelFormat, level);
            if (m_Level.gameObject.activeSelf != on) m_Level.gameObject.SetActive(on);
        }

        /// <summary>Carte prese nel chip; negativo = chip nascosto.</summary>
        public void SetCaptures(int count)
        {
            bool on = count >= 0;
            if (on && m_Captures != null) m_Captures.text = count.ToString();
            if (m_Chip != null && m_Chip.activeSelf != on) m_Chip.SetActive(on);
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

        void PaintBorder()
        {
            if (m_Background != null)
                m_Background.borderColor = m_IsTurn ? UI51Tokens.GoldA(0.85f) : UI51Banners.BorderColor(m_Style);
        }
    }
}
