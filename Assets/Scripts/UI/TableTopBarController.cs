using UnityEngine;
using TMPro;
using Project51.Core;
using Project51.Unity;
using Project51.UIV2.Core;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Testi dinamici della barra superiore del tavolo. Aspetto UI51 (Fase 5): pillola del punteggio
    /// "TU 34 - A 51 - MARCO 29" con i totali di partita; tutti contro tutti a 4 (Fase 6) "TU - A 51" e i tre
    /// rivali in ordine di posto, con i segmenti piu' stretti. I vecchi testi "Mano X di Y" e
    /// "Carte rimaste N" restano collegati ma spenti in scena.
    /// </summary>
    public class TableTopBarController : MonoBehaviour
    {
        [SerializeField] private TMP_Text handText;
        [SerializeField] private TMP_Text cardsLeftText;

        [Header("UI51 (opzionali): pillola del punteggio")]
        [SerializeField] private TMP_Text myLabel;
        [SerializeField] private TMP_Text myScore;
        [SerializeField] private TMP_Text targetScore;
        [SerializeField] private TMP_Text rivalLabel;
        [SerializeField] private TMP_Text rivalScore;
        [Tooltip("Tutti contro tutti: secondo e terzo rivale (posto in alto e a destra); spenti negli altri modi.")]
        [SerializeField] private TMP_Text[] moreLabels = new TMP_Text[0];
        [SerializeField] private TMP_Text[] moreScores = new TMP_Text[0];

        /// <summary>Lunghezza massima del nome del rivale nella pillola.</summary>
        public const int MaxNameLength = 12;
        /// <summary>Con tre rivali nella pillola i nomi si tagliano qui ("BOT 2", "MARCO").</summary>
        public const int ShortNameLength = 5;
        /// <summary>Margine ai lati di ogni segmento: mockup 14 (11 il traguardo), 9 (6) con tre rivali.</summary>
        public const int WidePad = 14, NarrowPad = 9;

        private TurnController turnController;
        private readonly string[] labels = new string[3];
        private readonly string[] scores = new string[3];
        private int padNow = -1;

        private void Start()
        {
            turnController = FindObjectOfType<TurnController>();
            InvokeRepeating(nameof(Refresh), 0.2f, 0.2f);
        }

        private void Refresh()
        {
            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
            }
            if (turnController == null) return;

            var roundManager = turnController.RoundManager;
            if (handText != null && roundManager != null)
            {
                handText.text = $"Mano {roundManager.CurrentHandNumber} di {roundManager.TotalHands}";
            }

            var state = turnController.GameState;
            if (cardsLeftText != null && state != null)
            {
                cardsLeftText.text = $"Carte rimaste {state.Deck.Count}";
            }

            if (state == null) return;
            // Tre assi: il punteggio cambia appena distribuito; resta com'era finche' le carte volano, poi lo svela TRE ASSI!.
            if (turnController.IsDealInProgress && state.RoundEnded && RoundManager.TreAssiHolder(state) >= 0) return;
            int rivals = ScorePill(state, GameModeService.Current.LocalPlayerIndex, GameSocialV2.PlayerName,
                out string me, out string mine, labels, scores);
            Set(myLabel, me);
            Set(myScore, mine);
            Set(rivalLabel, labels[0]);
            Set(rivalScore, scores[0]);
            for (int i = 0; i < moreLabels.Length && i < moreScores.Length; i++)
            {
                if (moreLabels[i] == null) continue;
                bool on = i + 1 < rivals;
                var segment = moreLabels[i].transform.parent.gameObject;
                if (segment.activeSelf != on) segment.SetActive(on);
                if (!on) continue;
                Set(moreLabels[i], labels[i + 1]);
                Set(moreScores[i], scores[i + 1]);
            }
            Pad(rivals > 1 ? NarrowPad : WidePad);
            Set(targetScore, (GameSceneInitializer.ActiveConfig ?? new MatchConfig()).TargetScore.ToString());
        }

        private static void Set(TMP_Text text, string value)
        {
            if (text != null) text.text = value;
        }

        /// <summary>Margini ai lati dei segmenti (il traguardo 3 in meno), solo quando cambiano.</summary>
        private void Pad(int pad)
        {
            if (pad == padNow || myLabel == null) return;
            padNow = pad;
            var pill = myLabel.transform.parent.parent;
            foreach (Transform segment in pill)
            {
                var group = segment.GetComponent<UnityEngine.UI.HorizontalOrVerticalLayoutGroup>();
                if (group == null) continue;
                int side = targetScore != null && segment == targetScore.transform.parent ? pad - 3 : pad;
                group.padding.left = group.padding.right = side;
            }
            UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild((RectTransform)pill);
        }

        /// <summary>
        /// Contenuto della pillola: 1 contro 1 "TU" e il nome dell'avversario, a coppie "NOI" e "LORO", tutti contro
        /// tutti "TU" e ogni rivale in ordine di posto (sinistra, alto, destra), nomi corti con tre rivali.
        /// I punteggi sono i totali di partita (MatchScore.Totals). Riempie rivalLabels/rivalScores
        /// (lunghi almeno 3) e restituisce quanti rivali ci sono.
        /// </summary>
        public static int ScorePill(GameState state, int localPlayer, System.Func<int, string> nameOf,
            out string myLabel, out string myScore, string[] rivalLabels, string[] rivalScores)
        {
            var totals = MatchScore.Totals(state);
            int mine = MatchScore.EntryOf(state, localPlayer);
            int rivals = totals.Length - 1;
            bool narrow = rivals > 1;
            bool treAssi = state.RoundEnded && RoundManager.TreAssiHolder(state) >= 0;

            myLabel = state.TeamMode ? "NOI" : "TU";
            myScore = Score(totals[mine], narrow, treAssi);
            for (int i = 1; i < totals.Length; i++)
            {
                int entry = (mine + i) % totals.Length; // posto relativo i: 1 sinistra, 2 alto, 3 destra
                rivalLabels[i - 1] = state.TeamMode ? "LORO" : ShortName(nameOf(entry), narrow ? ShortNameLength : MaxNameLength);
                rivalScores[i - 1] = Score(totals[entry], narrow, treAssi);
            }
            return rivals;
        }

        // Cappotto e tre assi chiudono la partita: resta scritto per un attimo, prima dei risultati.
        private static string Score(int total, bool narrow, bool treAssi) =>
            !MatchScore.IsCappotto(total) ? total.ToString() : treAssi ? narrow ? "3 ASSI" : "TRE ASSI" : narrow ? "CAPP." : "CAPPOTTO";

        private static string ShortName(string name, int length)
        {
            name = (name ?? "").Trim().ToUpperInvariant();
            return name.Length > length ? name.Substring(0, length).TrimEnd() : name;
        }
    }
}
