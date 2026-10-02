using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Ingresso in stanza fallito (mockup StanzaErrore, StanzaPiena, StanzaIniziata): velo, scheda col bordo rosso, icona nel
    /// cerchio, titolo, testo, pulsante d'oro (RIPROVA / HO CAPITO) e "Gioca online invece". Sta sul canvas di OnlineFlowV2,
    /// sopra la Home; i due pulsanti la chiudono, RoomFlowV2 decide il resto. Costruita da UI51MatchBuilder.
    /// </summary>
    public sealed class UI51RoomErrorView : MonoBehaviour
    {
        public enum Kind { Code, Full, Started }

        static readonly string[] Titles = { "Codice non valido", "Stanza piena", "Partita già iniziata" };
        static readonly string[] Texts =
        {
            "Nessuna stanza corrisponde a questo codice. Controlla le lettere e riprova.",
            "Tutti i posti di questa stanza sono già occupati. Chiedi all’host di crearne un’altra.",
            "In questa stanza si sta già giocando. Potrai entrare nella prossima partita.",
        };

        [SerializeField] private RectTransform card;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite[] icons = new Sprite[0]; // warn, lock, cards
        [SerializeField] private TMP_Text title, text, ctaLabel;
        [SerializeField] private Button cta, alt;

        public Button Cta => cta;
        public Button Alt => alt;

        /// <summary>Codice di Photon dell'ingresso fallito: 32765 piena, 32764 chiusa (si gioca gia'), il resto codice sbagliato.</summary>
        public static Kind ForCode(short code) => code == 32765 ? Kind.Full : code == 32764 ? Kind.Started : Kind.Code;

        private void Awake()
        {
            cta.onClick.AddListener(Hide);
            alt.onClick.AddListener(Hide);
        }

        public void Show(Kind kind)
        {
            int i = (int)kind;
            icon.sprite = icons[i];
            title.text = Titles[i];
            text.text = Texts[i];
            ctaLabel.text = kind == Kind.Code ? "RIPROVA" : "HO CAPITO";
            gameObject.SetActive(true);
            UIAnim.PopDialog(card);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
