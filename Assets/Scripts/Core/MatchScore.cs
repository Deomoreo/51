using System.Linq;

namespace Project51.Core
{
    /// <summary>
    /// Punteggio di una partita a piu' smazzate, calcolato SOLO dallo GameState: il totale viaggia
    /// con lo stato (GameState.MatchTotals) ed e' quindi identico su ogni client.
    /// Un "concorrente" (entry) e' un giocatore, oppure a coppie una squadra (0 = posti 0+2, 1 = posti 1+3).
    /// </summary>
    public static class MatchScore
    {
        /// <summary>Punteggio simbolico assegnato dal cappotto che chiude subito la partita.</summary>
        public const int CappottoScore = 1000;

        public static int EntryCount(GameState state) => state.TeamMode ? 2 : state.NumPlayers;

        public static int EntryOf(GameState state, int player) => state.TeamMode ? player % 2 : player;

        public static int[] MembersOf(GameState state, int entry) => state.TeamMode ? new[] { entry, entry + 2 } : new[] { entry };

        public static bool IsCappotto(int score) => score >= CappottoScore;

        /// <summary>Punti di questa smazzata per concorrente (significativi a smazzata conclusa).</summary>
        public static int[] RoundScores(GameState state)
        {
            // A coppie i due compagni ricevono lo stesso punteggio: basta leggere il primo membro.
            return Enumerable.Range(0, EntryCount(state)).Select(e => state.Players[e].TotalScore).ToArray();
        }

        /// <summary>Totale di partita per concorrente: smazzate precedenti + smazzata corrente.</summary>
        public static int[] Totals(GameState state)
        {
            var totals = RoundScores(state);
            var previous = state.MatchTotals;
            for (int e = 0; previous != null && e < totals.Length && e < previous.Length; e++)
                totals[e] += previous[e];
            return totals;
        }

        /// <summary>
        /// La partita finisce quando la smazzata e' conclusa e un solo concorrente e' in testa con
        /// almeno il traguardo. A pari merito sopra il traguardo si gioca un'altra smazzata.
        /// </summary>
        public static bool IsFinished(GameState state, int target)
        {
            if (state == null || !state.RoundEnded) return false;
            var totals = Totals(state);
            int best = totals.Max();
            return best >= target && totals.Count(t => t == best) == 1;
        }

        /// <summary>Concorrenti in testa (piu' di uno a pari merito).</summary>
        public static int[] Leaders(GameState state)
        {
            var totals = Totals(state);
            int best = totals.Max();
            return Enumerable.Range(0, totals.Length).Where(e => totals[e] == best).ToArray();
        }

        /// <summary>
        /// Prepara la nuova smazzata: se la precedente e' conclusa e non ha chiuso la partita, porta
        /// avanti i totali, passa il mazzo al giocatore di mano precedente e incrementa il numero di
        /// smazzata. Altrimenti (prima smazzata, partita finita, formato diverso) riparte da zero.
        /// Va chiamato PRIMA di RoundManager.StartSmazzata, che distribuisce in base al mazziere.
        /// </summary>
        public static void ContinueMatch(GameState previous, GameState next, int target)
        {
            bool continues = previous != null
                && previous.RoundEnded
                && previous.NumPlayers == next.NumPlayers
                && previous.TeamMode == next.TeamMode
                && !IsFinished(previous, target);
            if (!continues)
            {
                next.MatchTotals = new int[EntryCount(next)];
                next.RoundIndex = 1;
                return;
            }
            next.MatchTotals = Totals(previous);
            next.RoundIndex = previous.RoundIndex + 1;
            next.DealerIndex = (previous.DealerIndex - 1 + next.NumPlayers) % next.NumPlayers;
        }
    }
}
