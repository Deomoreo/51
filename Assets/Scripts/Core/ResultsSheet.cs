using System;
using System.Linq;

namespace Project51.Core
{
    /// <summary>
    /// Righe del fine smazzata (mockup FineSmazzata): punti per concorrente e riga di dettaglio, classifica a pari merito
    /// condivisi (1, 2, 2, 4) e nota della corsa al traguardo. Solo dati: la grafica sta in UI51ResultsView.
    /// </summary>
    public static class ResultsSheet
    {
        public static readonly string[] Labels = { "Carte", "Denari", "Settebello", "Primiera", "Grande", "Piccola", "Scope", "Accusi" };

        /// <summary>Punti della riga row per il concorrente s (0 = la riga non gli ha dato nulla).</summary>
        public static int Points(SmazzataScore s, int row)
        {
            switch (row)
            {
                case 0: return s.WonCards ? 1 : 0;
                case 1: return s.WonDenari ? 1 : 0;
                case 2: return s.HasSetteBello ? 1 : 0;
                case 3: return s.WonPrimiera ? 1 : 0;
                case 4: return s.HasGrande ? 5 : 0;
                case 5: return s.HasPiccola ? 3 + s.PiccolaExtras : 0;
                case 6: return s.ScopaCount;
                default: return s.AccusiPoints;
            }
        }

        /// <summary>
        /// Riga piccola sotto l'etichetta. Conteggi "22 – 18" a due, "14 · 9 · 10 · 7" a quattro; settebello "preso da te/X";
        /// primiera a quattro "X 79"; Grande e Piccola le carte; accusi chi li ha dichiarati. player(i) = nome del giocatore i,
        /// entry(e) = nome del concorrente e, local = il mio posto.
        /// </summary>
        public static string Detail(GameState state, SmazzataScore[] b, int row, Func<int, string> player, Func<int, string> entry, int local)
        {
            const string none = "nessuno";
            switch (row)
            {
                case 0: return Joined(b.Select(x => x.CardCount.ToString()).ToArray());
                case 1: return Joined(b.Select(x => x.DenariCount.ToString()).ToArray());
                case 2:
                    for (int p = 0; p < state.NumPlayers; p++)
                        if (state.Players[p].CapturedCards.Any(c => c.IsSetteBello)) return "preso da " + (p == local ? "te" : player(p));
                    return none;
                case 3:
                    if (b.Length == 2) return Joined(b.Select(x => x.PrimieraScore < 0 ? "-" : x.PrimieraScore.ToString()).ToArray());
                    int w = Array.FindIndex(b, x => x.WonPrimiera);
                    return w < 0 ? none : entry(w) + " " + b[w].PrimieraScore;
                case 4: return b.Any(x => x.HasGrande) ? "Re, Cavallo e Fante di denari" : none;
                case 5:
                    var piccola = b.FirstOrDefault(x => x.HasPiccola);
                    return piccola == null ? none : Series(new[] { "Asso", "2", "3", "4", "5", "6" }.Take(3 + piccola.PiccolaExtras).ToArray()) + " di denari";
                case 6: return Joined(b.Select(x => x.ScopaCount.ToString()).ToArray());
                default:
                    var who = Enumerable.Range(0, state.NumPlayers)
                        .Where(p => Math.Max(state.Players[p].RoundAccusiPoints, state.Players[p].AccusiPoints) > 0).Select(player).ToArray();
                    return who.Length == 0 ? none : string.Join(", ", who);
            }
        }

        /// <summary>Due valori "a – b" (mockup a due colonne), altrimenti "a · b · c · d".</summary>
        public static string Joined(string[] values) => values.Length == 2 ? values[0] + " – " + values[1] : string.Join(" · ", values);

        /// <summary>"Asso, 2 e 3".</summary>
        public static string Series(string[] items) =>
            items.Length < 2 ? string.Join("", items) : string.Join(", ", items.Take(items.Length - 1)) + " e " + items[items.Length - 1];

        /// <summary>Posizioni in classifica, pari merito condivisi: 52, 44, 44, 30 -> 1, 2, 2, 4 (scelta utente 01/10).</summary>
        public static int[] Ranks(int[] totals) => totals.Select(t => 1 + totals.Count(o => o > t)).ToArray();

        /// <summary>
        /// Nota in alto a destra della corsa: a due quanto manca a me, a quattro a chi e' in testa. Si vince superando il
        /// traguardo; chi ci arriva esatto torna a 0 (B30).
        /// </summary>
        public static string RaceNote(int[] totals, int localEntry, int target)
        {
            bool four = totals.Length > 2;
            int score = four ? totals.Max() : totals[localEntry];
            if (score == target) return target + " esatti: si riparte da 0";
            int left = target + 1 - score;
            if (left <= 0) return "pari: si gioca ancora"; // a smazzata finita col traguardo superato resta solo il pari in testa
            return (four ? "in testa: " : "") + left + " punti alla vittoria"; // mai 1: a 51 esatti si torna a 0
        }
    }
}
