using System;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Core
{
    /// <summary>
    /// Dettaglio del punteggio di una smazzata per un concorrente: un giocatore oppure, a coppie,
    /// una squadra (prese, scope e accusi dei due compagni uniti).
    /// </summary>
    public sealed class SmazzataScore
    {
        public int CardCount;
        public int DenariCount;
        public int ScopaCount;
        /// <summary>-1 se non ha carte di tutti e quattro i semi (niente primiera).</summary>
        public int PrimieraScore = -1;
        public bool HasSetteBello;
        public bool WonCards;
        public bool WonDenari;
        public bool WonPrimiera;
        public bool HasGrande;
        public bool HasPiccola;
        public int PiccolaExtras;
        public int AccusiPoints;

        /// <summary>Punti della smazzata SENZA accusi (li somma RoundManager a fine smazzata).</summary>
        public int Points =>
            ScopaCount
            + (HasSetteBello ? 1 : 0)
            + (WonDenari ? 1 : 0)
            + (WonCards ? 1 : 0)
            + (WonPrimiera ? 1 : 0)
            + (HasGrande ? 5 : 0)
            + (HasPiccola ? 3 + PiccolaExtras : 0);
    }

    public static class PunteggioManager
    {
        /// <summary>
        /// Punti della smazzata (senza accusi) per ciascun giocatore. A coppie ogni compagno riceve
        /// il punteggio della propria squadra.
        /// </summary>
        public static int[] CalculateSmazzataScores(GameState state)
        {
            var entries = CalculateBreakdown(state);
            var points = new int[state.NumPlayers];
            for (int i = 0; i < state.NumPlayers; i++)
                points[i] = entries[MatchScore.EntryOf(state, i)].Points;
            return points;
        }

        /// <summary>
        /// Dettaglio per concorrente (vedi MatchScore.EntryCount): categorie vinte, scope, accusi.
        /// </summary>
        public static SmazzataScore[] CalculateBreakdown(GameState state)
        {
            int count = MatchScore.EntryCount(state);
            var entries = new SmazzataScore[count];
            for (int e = 0; e < count; e++)
            {
                var members = MatchScore.MembersOf(state, e);
                var captured = members.SelectMany(i => state.Players[i].CapturedCards).ToList();
                var denari = captured.Where(c => c.Suit == Suit.Denari).Select(c => c.Rank).ToHashSet();
                var entry = new SmazzataScore
                {
                    CardCount = captured.Count,
                    DenariCount = captured.Count(c => c.Suit == Suit.Denari),
                    ScopaCount = members.Sum(i => state.Players[i].ScopaCount),
                    HasSetteBello = captured.Any(c => c.IsSetteBello),
                    // Grande (Re, Cavallo, Fante di denari) = +5
                    HasGrande = denari.Contains(10) && denari.Contains(9) && denari.Contains(8),
                    // Piccola (Asso, 2, 3 di denari) = +3, piu' 1 per ciascun 4/5/6 consecutivo presente
                    HasPiccola = denari.Contains(1) && denari.Contains(2) && denari.Contains(3),
                    AccusiPoints = members.Sum(i => Math.Max(state.Players[i].RoundAccusiPoints, state.Players[i].AccusiPoints))
                };
                if (entry.HasPiccola)
                {
                    for (int r = 4; r <= 6; r++)
                        if (denari.Contains(r)) entry.PiccolaExtras++;
                }
                // Primiera: serve almeno una carta per seme
                if (captured.Select(c => c.Suit).Distinct().Count() == 4)
                    entry.PrimieraScore = ComputePrimieraScore(captured);
                entries[e] = entry;
            }

            // Maggioranze: il punto va solo a un vincitore unico (a pari merito nessuno),
            // con le soglie minime di 6 denari e 21 carte.
            AwardUniqueMaximum(entries, x => x.DenariCount, 6, x => x.WonDenari = true);
            AwardUniqueMaximum(entries, x => x.CardCount, 21, x => x.WonCards = true);
            AwardUniqueMaximum(entries, x => x.PrimieraScore, 0, x => x.WonPrimiera = true);
            return entries;
        }

        private static void AwardUniqueMaximum(SmazzataScore[] entries, Func<SmazzataScore, int> value, int minimum, Action<SmazzataScore> award)
        {
            int max = entries.Max(value);
            if (max < minimum || entries.Count(x => value(x) == max) != 1) return;
            award(entries.First(x => value(x) == max));
        }

        private static int ComputePrimieraScore(List<Card> cards)
        {
            var suits = System.Enum.GetValues(typeof(Suit)).Cast<Suit>();
            int total = 0;
            foreach (var s in suits)
            {
                var best = cards.Where(c => c.Suit == s).Select(c => c.PrimieraValue).DefaultIfEmpty(0).Max();
                total += best;
            }
            return total;
        }
    }
}
