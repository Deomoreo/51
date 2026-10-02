using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Ricerca della partita (mockup Matchmaking, Matchmaking2v2, MatchmakingTrovato) dentro il pannello SearchMatch di
    /// OnlineFlowV2: formato, "Cerco giocatori…" col tempo, anello che gira e cinque dorsi che ondeggiano, posti che si riempiono
    /// (1v1 una riga; 2v2 "LA TUA SQUADRA" / "AVVERSARI"; 1v3 due righe), "Annulla ricerca". Trovata: titolo, anello spento e
    /// pillola d'oro "AL TAVOLO" mentre il tavolo si carica. I dati li porta RoomFlowV2; costruita da UI51MatchBuilder.
    /// </summary>
    public sealed class UI51MatchmakingView : MonoBehaviour
    {
        [SerializeField] private TMP_Text modeLabel, title, subtitle, waitLabel;
        [SerializeField] private RectTransform spinner;
        [SerializeField] private RectTransform[] backs = new RectTransform[0];
        [SerializeField] private GameObject[] rows = new GameObject[0];
        [SerializeField] private TMP_Text[] rowLabels = new TMP_Text[0];
        [SerializeField] private UI51SeatCard[] seats = new UI51SeatCard[0]; // due per riga
        [SerializeField] private GameObject seatsBlock;
        [SerializeField] private Button cancel;
        [SerializeField] private RectTransform toTable;
        [SerializeField] private Sprite[] portraits = new Sprite[0];

        public Button Cancel => cancel;

        // ponytail: ritratto per posto (come al tavolo): l'avatar scelto da ognuno non e' pubblicato in rete.
        public Sprite Portrait(int slot) => portraits.Length > 0 ? portraits[slot % portraits.Length] : null;

        private bool found, animating;

        private void OnEnable()
        {
            // I loop si fermano da soli quando il pannello si spegne (SetLink): si riaccendono qui.
            UIAnim.Spin(spinner, 2.4f);
            UIAnim.LoadingWave(backs);
            animating = true;
        }

        private void OnDisable() => animating = found = false;

        /// <summary>Testata e fondo. found = partita trovata (anello spento, pillola AL TAVOLO al posto di Annulla).</summary>
        public void SetStatus(string mode, string titleText, string sub, string wait, bool isFound)
        {
            modeLabel.text = mode;
            title.text = titleText;
            subtitle.text = sub;
            waitLabel.text = wait;
            spinner.gameObject.SetActive(!isFound);
            cancel.gameObject.SetActive(!isFound);
            toTable.gameObject.SetActive(isFound);
            if (isFound && !found) UIAnim.Pop(toTable, 0.8f, 1.04f, 0.3f);
            found = isFound;
            if (!animating && isActiveAndEnabled) OnEnable();
        }

        /// <summary>
        /// Posti: rowCount righe da due (gli altri nascosti); labels = scritte delle righe (null = senza, come 1v1 e 1v3).
        /// seatInfo[i] null = posto vuoto "In ricerca…". showSeats false = niente posti (ingresso in stanza privata).
        /// </summary>
        public void SetSeats(bool showSeats, int rowCount, string[] labels, UI51SeatCard.Info[] seatInfo)
        {
            seatsBlock.SetActive(showSeats);
            if (!showSeats) return;
            for (int r = 0; r < rows.Length; r++)
            {
                rows[r].SetActive(r < rowCount);
                bool hasLabel = labels != null && r < labels.Length && !string.IsNullOrEmpty(labels[r]);
                rowLabels[r].gameObject.SetActive(hasLabel);
                if (!hasLabel) continue;
                rowLabels[r].text = labels[r];
                rowLabels[r].color = r == 0 ? UI51Tokens.Gold : UI51Tokens.TeamBlueText;
            }
            for (int i = 0; i < seats.Length; i++)
            {
                if (i >= rowCount * 2) continue;
                seats[i].Bind(seatInfo != null && i < seatInfo.Length ? seatInfo[i] : null);
            }
        }
    }
}
