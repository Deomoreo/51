using DG.Tweening;
using Project51.Core;
using Project51.UI51;
using Project51.UIV2.Core;
using TMPro;
using UnityEngine;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 5 (S10, mockup Sorteggio) e Fase 6 (Sorteggio4): ruota del mazziere a 2 o 4 spicchi in ordine di posto
    /// (io, poi sinistra, alto, destra; in 1 contro 1 io e l'avversario). Parte da sola, si ferma col mazziere sotto la
    /// lancetta, poi scheda MAZZIERE e conto alla rovescia. Solo presentazione: chi distribuisce lo decide il master
    /// (DealerRouletteController.WheelDealer), qui sono locali solo lo scarto casuale e i tempi, gli stessi del
    /// controller (DealerRouletteController.Timing). Acceso e spento da DealerRouletteController.PlayWheel.
    /// Costruita da Tools/UI51/Build Fase 5 (Tavolo 1v1).
    /// </summary>
    public sealed class SorteggioView : MonoBehaviour
    {
        [SerializeField] DealerRouletteController m_Roulette;
        [SerializeField] PlayerBannerManager m_Banners;
        [SerializeField] TMP_Text m_Mode;
        [SerializeField] RectTransform m_Face;
        [Tooltip("A 4: i quattro spicchi colorati e la riga d'oro orizzontale (in 1 contro 1 bastano faccia e riga verticale).")]
        [SerializeField] GameObject m_Slices4;
        [SerializeField] GameObject m_Cross;
        [SerializeField] RectTransform m_Pointer;
        [Tooltip("Spicchio i: 0 = io, poi in ordine di posto.")]
        [SerializeField] RectTransform[] m_Seats = new RectTransform[4];
        [SerializeField] AvatarFrame[] m_SeatAvatars = new AvatarFrame[4];
        [SerializeField] TMP_Text[] m_SeatNames = new TMP_Text[4];
        [SerializeField] TMP_Text m_Status;
        [SerializeField] RectTransform m_Card;
        [SerializeField] AvatarFrame m_WinAvatar;
        [SerializeField] UI51Shape m_WinPulse;
        [SerializeField] TMP_Text m_WinName;
        [SerializeField] TMP_Text m_WinNote;
        [SerializeField] RectTransform m_Footer;
        [SerializeField] TMP_Text m_Count;
        [SerializeField] GameObject m_Continue;

        public const string Waiting = "LA RUOTA STA PER GIRARE…", Spinning = "SORTEGGIO IN CORSO…";

        void OnEnable()
        {
            if (m_Roulette == null || m_Face == null) return;
            bool online = m_Roulette.WheelOnline;
            // Gioca per primo chi sta alla destra del mazziere: lo spicchio prima del suo.
            int n = m_Roulette.WheelPlayers, dealer = m_Roulette.WheelDealer, first = (dealer + n - 1) % n;
            int local = GameModeService.Current.LocalPlayerIndex;
            float T(float s) => DealerRouletteController.Timing(s, online);

            // Ogni volta da capo: alla rivincita scheda e conto non devono essere gia' li'.
            m_Face.localEulerAngles = Vector3.zero;
            m_Card.gameObject.SetActive(false);
            m_Footer.gameObject.SetActive(false);
            m_Status.gameObject.SetActive(true);
            m_Status.text = Waiting;
            if (m_Mode != null) m_Mode.text = ModeLabel(n, m_Roulette.WheelTeams);
            if (m_Slices4 != null) m_Slices4.SetActive(n == 4);
            if (m_Cross != null) m_Cross.SetActive(n == 4);
            for (int i = 0; i < m_Seats.Length; i++)
            {
                m_Seats[i].gameObject.SetActive(i < n);
                if (i >= n) continue;
                // Come il mockup: spicchio i centrato a (i + 0,5) * 360 / n gradi CSS, orari da mezzogiorno.
                m_Seats[i].localEulerAngles = new Vector3(0f, 0f, -(i + 0.5f) * 360f / n);
                int p = (local + i) % n;
                m_SeatNames[i].text = GameSocialV2.PlayerName(p);
                if (m_Banners != null) m_SeatAvatars[i].SetAvatar(m_Banners.SeatAvatar(p));
            }

            var root = (RectTransform)transform;
            UIAnim.FadeIn(root, 0.25f);
            // DealerWheel riscala da se' i tempi con le animazioni veloci: qui si compensa, cosi' vale la timeline del controller.
            float speed = GamePreferences.AnimationSpeed;
            UIAnim.DealerWheel(m_Face, m_Pointer, UIAnim.WheelTarget(dealer, n),
                T(DealerRouletteController.WheelSpinAt) * speed, T(DealerRouletteController.WheelSpinSeconds) * speed);
            DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .InsertCallback(T(DealerRouletteController.WheelSpinAt), () => m_Status.text = Spinning)
                .InsertCallback(T(DealerRouletteController.WheelResultAt), () => ShowResult(n, dealer, first, local, online))
                .InsertCallback(T(DealerRouletteController.WheelResultAt + 1f), () => m_Count.text = "2")
                .InsertCallback(T(DealerRouletteController.WheelResultAt + 2f), () => m_Count.text = "1")
                // FadeOut riscala da se' la durata: parte in modo da finire proprio alla consegna, anche online con le animazioni veloci.
                .InsertCallback(T(DealerRouletteController.WheelEndAt) - GamePreferences.Scaled(0.25f), () => UIAnim.FadeOut(root, 0.25f));
        }

        void LateUpdate()
        {
            // Avatar e nomi sempre dritti mentre la ruota gira (scelta dell'utente 30/09; il mockup li fa girare con la ruota).
            for (int i = 0; i < m_SeatAvatars.Length; i++) m_SeatAvatars[i].transform.parent.rotation = transform.rotation;
        }

        void OnDisable()
        {
            if (m_Face != null) DOTween.Kill(m_Face); // la ruota e' legata solo alla distruzione
        }

        void ShowResult(int n, int dealer, int first, int local, bool online)
        {
            int dealerPlayer = (local + dealer) % n, firstPlayer = (local + first) % n;
            // I nomi si rileggono qui: all'apertura il roster poteva non essere pronto.
            Texts(n, dealer == 0, GameSocialV2.PlayerName(dealerPlayer), first == 0, GameSocialV2.PlayerName(firstPlayer),
                out string name, out string note);
            m_WinName.text = name;
            m_WinNote.text = note;
            if (m_Banners != null) m_WinAvatar.SetAvatar(m_Banners.SeatAvatar(dealerPlayer));
            m_Status.gameObject.SetActive(false);
            m_Count.text = "3";
            m_Continue.SetActive(!online); // online la consegna e' uguale per tutti
            m_Card.gameObject.SetActive(true);
            m_Footer.gameObject.SetActive(true);
            UIAnim.Pop(m_Card, 0.5f, 1.06f, 0.4f);
            UIAnim.Rise(m_Footer);
            UIAnim.Pulse(m_WinPulse, 12f, 1.4f, 0.7f);
        }

        /// <summary>
        /// Nome e nota della scheda MAZZIERE (mockup). first = chi gioca per primo: in 1 contro 1 "gli altri" del mockup
        /// diventa il nome dell'avversario, a 4 se non sei tu si dice chi inizia.
        /// </summary>
        public static void Texts(int players, bool localDealer, string dealer, bool localFirst, string first, out string name, out string note)
        {
            name = localDealer ? "Sei tu!" : dealer;
            note = localDealer
                ? "Distribuisci tu: " + (players == 2 ? first + " gioca per primo" : "gli altri giocano prima di te")
                : "Distribuisce " + dealer + ": " + (localFirst ? "giochi tu per primo" : "inizia " + first);
        }

        /// <summary>Riga sopra al titolo: mockup Sorteggio e Sorteggio4, "TUTTI CONTRO TUTTI" scelto dall'utente il 01/10.</summary>
        public static string ModeLabel(int players, bool teams) =>
            players == 2 ? "PARTITA 1 VS 1" : teams ? "PARTITA 2 VS 2" : "TUTTI CONTRO TUTTI";
    }
}
