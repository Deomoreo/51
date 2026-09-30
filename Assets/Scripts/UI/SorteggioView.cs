using DG.Tweening;
using Project51.Core;
using Project51.UI51;
using Project51.UIV2.Core;
using TMPro;
using UnityEngine;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 5 (S10, mockup Sorteggio): ruota del mazziere in 1 contro 1. Parte da sola, si ferma col mazziere sotto
    /// la lancetta, poi scheda MAZZIERE e conto alla rovescia. Solo presentazione: chi distribuisce lo decide il master
    /// (DealerRouletteController.WheelLocalDealer), qui sono locali solo lo scarto casuale e i tempi, gli stessi del
    /// controller (DealerRouletteController.Timing). Acceso e spento da DealerRouletteController.PlayWheel.
    /// Costruita da Tools/UI51/Build Fase 5 (Tavolo 1v1).
    /// </summary>
    public sealed class SorteggioView : MonoBehaviour
    {
        [SerializeField] DealerRouletteController m_Roulette;
        [SerializeField] PlayerBannerManager m_Banners;
        [SerializeField] RectTransform m_Face;
        [SerializeField] RectTransform m_Pointer;
        [Tooltip("0 = io (a destra a ruota ferma), 1 = avversario.")]
        [SerializeField] AvatarFrame[] m_SeatAvatars = new AvatarFrame[2];
        [SerializeField] TMP_Text[] m_SeatNames = new TMP_Text[2];
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
            bool online = m_Roulette.WheelOnline, mine = m_Roulette.WheelLocalDealer;
            int local = GameModeService.Current.LocalPlayerIndex, rival = 1 - local;
            float T(float s) => DealerRouletteController.Timing(s, online);

            // Ogni volta da capo: alla rivincita scheda e conto non devono essere gia' li'.
            m_Face.localEulerAngles = Vector3.zero;
            m_Card.gameObject.SetActive(false);
            m_Footer.gameObject.SetActive(false);
            m_Status.gameObject.SetActive(true);
            m_Status.text = Waiting;
            int[] seats = { local, rival };
            for (int i = 0; i < 2; i++)
            {
                m_SeatNames[i].text = GameSocialV2.PlayerName(seats[i]);
                if (m_Banners != null) m_SeatAvatars[i].SetAvatar(m_Banners.SeatAvatar(seats[i]));
            }

            var root = (RectTransform)transform;
            UIAnim.FadeIn(root, 0.25f);
            // DealerWheel riscala da se' i tempi con le animazioni veloci: qui si compensa, cosi' vale la timeline del controller.
            float speed = GamePreferences.AnimationSpeed;
            UIAnim.DealerWheel(m_Face, m_Pointer, UIAnim.WheelTarget(mine ? 0 : 1, 2),
                T(DealerRouletteController.WheelSpinAt) * speed, T(DealerRouletteController.WheelSpinSeconds) * speed);
            DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .InsertCallback(T(DealerRouletteController.WheelSpinAt), () => m_Status.text = Spinning)
                .InsertCallback(T(DealerRouletteController.WheelResultAt), () => ShowResult(mine, mine ? local : rival, rival, online))
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

        void ShowResult(bool mine, int dealer, int rival, bool online)
        {
            // Il nome si rilegge qui: all'apertura il roster poteva non essere pronto.
            Texts(mine, GameSocialV2.PlayerName(rival), out string name, out string note);
            m_WinName.text = name;
            m_WinNote.text = note;
            if (m_Banners != null) m_WinAvatar.SetAvatar(m_Banners.SeatAvatar(dealer));
            m_Status.gameObject.SetActive(false);
            m_Count.text = "3";
            m_Continue.SetActive(!online); // online la consegna e' uguale per tutti
            m_Card.gameObject.SetActive(true);
            m_Footer.gameObject.SetActive(true);
            UIAnim.Pop(m_Card, 0.5f, 1.06f, 0.4f);
            UIAnim.Rise(m_Footer);
            UIAnim.Pulse(m_WinPulse, 12f, 1.4f, 0.7f);
        }

        /// <summary>Nome e nota della scheda MAZZIERE (mockup; in 1 contro 1 "gli altri" diventa il nome dell'avversario).</summary>
        public static void Texts(bool localDealer, string rival, out string name, out string note)
        {
            name = localDealer ? "Sei tu!" : rival;
            note = localDealer ? "Distribuisci tu: " + rival + " gioca per primo" : "Distribuisce " + rival + ": giochi tu per primo";
        }
    }
}
