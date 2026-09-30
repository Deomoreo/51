using UnityEngine;
using TMPro;
using Project51.Core;
using Project51.Unity;
using Project51.UIV2.Core;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Testi dinamici della barra superiore del tavolo. Aspetto UI51 (Fase 5): pillola del punteggio
    /// "TU 34 - A 51 - MARCO 29" con i totali di partita. I vecchi testi "Mano X di Y" e
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

        /// <summary>Lunghezza massima del nome del rivale nella pillola.</summary>
        public const int MaxNameLength = 12;

        private TurnController turnController;

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
            ScorePill(state, GameModeService.Current.LocalPlayerIndex, GameSocialV2.PlayerName,
                out string me, out string mine, out string rival, out string theirs);
            Set(myLabel, me);
            Set(myScore, mine);
            Set(rivalLabel, rival);
            Set(rivalScore, theirs);
            Set(targetScore, (GameSceneInitializer.ActiveConfig ?? new MatchConfig()).TargetScore.ToString());
        }

        private static void Set(TMP_Text text, string value)
        {
            if (text != null) text.text = value;
        }

        /// <summary>
        /// Contenuto della pillola: 1 contro 1 "TU" e il nome dell'avversario, a coppie "NOI" e "LORO".
        /// I punteggi sono i totali di partita (MatchScore.Totals).
        /// </summary>
        public static void ScorePill(GameState state, int localPlayer, System.Func<int, string> nameOf,
            out string myLabel, out string myScore, out string rivalLabel, out string rivalScore)
        {
            var totals = MatchScore.Totals(state);
            int mine = MatchScore.EntryOf(state, localPlayer);
            // ponytail: tutti contro tutti in 3 o 4 c'e' un solo posto per i rivali, si mostra quello in testa
            // (a pari punti il primo dopo di me nel giro). Una pillola per rivale se la Fase 6 la chiede.
            int rival = -1;
            for (int i = 1; i < totals.Length; i++)
            {
                int entry = (mine + i) % totals.Length;
                if (rival < 0 || totals[entry] > totals[rival]) rival = entry;
            }

            myLabel = state.TeamMode ? "NOI" : "TU";
            myScore = Score(totals[mine]);
            rivalLabel = rival < 0 ? "" : state.TeamMode ? "LORO" : ShortName(nameOf(rival));
            rivalScore = rival < 0 ? "" : Score(totals[rival]);
        }

        private static string Score(int total) => MatchScore.IsCappotto(total) ? "CAPPOTTO" : total.ToString();

        private static string ShortName(string name)
        {
            name = (name ?? "").Trim().ToUpperInvariant();
            return name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name;
        }
    }
}
