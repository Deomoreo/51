using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51
{
    /// <summary>
    /// Fase 1, solo scena UI51_Gallery: collega i bottoni di prova ai componenti (bottom sheet, dialog,
    /// emoticon, turno con timer, stile dei banner). Nessuna logica di gioco; i listener si aggiungono in Awake.
    /// </summary>
    public class UI51GalleryDemo : MonoBehaviour
    {
        const float TurnSeconds = 15f;

        [SerializeField] BottomSheet m_Sheet;
        [SerializeField] UI51Dialog m_Dialog;
        [SerializeField] PlayerBanner[] m_Banners;
        [SerializeField] AvatarFrame m_Avatar;
        [SerializeField] Button m_SheetButton;
        [SerializeField] Button m_SheetConfirmButton;
        [SerializeField] Button m_DialogButton;
        [SerializeField] Button m_EmoticonButton;
        [SerializeField] Button m_TurnButton;
        [SerializeField] Button m_StyleButton;

        int m_Emoticon = -1;
        int m_Style;
        bool m_Turn;
        float m_TimeLeft;

        void Awake()
        {
            if (m_SheetButton != null) m_SheetButton.onClick.AddListener(OpenSheet);
            if (m_SheetConfirmButton != null && m_Sheet != null) m_SheetConfirmButton.onClick.AddListener(m_Sheet.Close);
            if (m_DialogButton != null) m_DialogButton.onClick.AddListener(ShowDialog);
            if (m_EmoticonButton != null) m_EmoticonButton.onClick.AddListener(NextEmoticon);
            if (m_TurnButton != null) m_TurnButton.onClick.AddListener(ToggleTurn);
            if (m_StyleButton != null) m_StyleButton.onClick.AddListener(NextStyle);
        }

        void Update()
        {
            if (!m_Turn || m_Banners == null || m_Banners.Length == 0 || m_Banners[0] == null) return;
            m_TimeLeft -= Time.deltaTime;
            if (m_TimeLeft <= 0f) m_TimeLeft = TurnSeconds;
            m_Banners[0].SetTimer(m_TimeLeft / TurnSeconds);
        }

        void OpenSheet()
        {
            if (m_Sheet == null) return;
            m_Sheet.SetTitle("Bottom sheet", "Scorri o tocca fuori per chiudere");
            m_Sheet.Open();
        }

        void ShowDialog()
        {
            if (m_Dialog == null) return;
            m_Dialog.Show("Abbandonare la partita?", "Perderai la partita in corso.", "Abbandona", null,
                "Annulla", null, null, true);
        }

        /// <summary>Indici di rete 0..5 a rotazione su tutti i banner e sulla cornice singola.</summary>
        void NextEmoticon()
        {
            m_Emoticon = (m_Emoticon + 1) % UI51EmoticonSet.Order.Length;
            if (m_Banners != null)
                foreach (var banner in m_Banners)
                    if (banner != null) banner.ShowEmoticon(m_Emoticon);
            if (m_Avatar != null) m_Avatar.ShowEmoticon(m_Emoticon);
        }

        /// <summary>Turno del proprio banner: anello che pulsa e timer di 15 s in loop.</summary>
        void ToggleTurn()
        {
            if (m_Banners == null || m_Banners.Length == 0 || m_Banners[0] == null) return;
            m_Turn = !m_Turn;
            m_TimeLeft = TurnSeconds;
            m_Banners[0].SetTurn(m_Turn);
            m_Banners[0].SetTimer(m_Turn ? 1f : -1f);
        }

        /// <summary>I sei banner SPEC sez. 7 a rotazione (gli ultimi tre animati dallo shader).</summary>
        void NextStyle()
        {
            m_Style = (m_Style + 1) % System.Enum.GetValues(typeof(BannerStyle)).Length;
            if (m_Banners == null) return;
            foreach (var banner in m_Banners)
                if (banner != null) banner.SetStyle((BannerStyle)m_Style);
        }
    }
}
