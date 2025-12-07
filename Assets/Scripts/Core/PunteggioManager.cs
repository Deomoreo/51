using System;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Core
{
    public static class PunteggioManager
    {
        // Calculate scores at end of smazzata. Returns list of points per player (same index order).
        // Implements: Scopa (1 each), Sette Bello (7 of Denari), Primiera (1), Denari majority (1),
        // Carte majority (1), Grande (5), Piccola (3 + extras), ties result in no point for that category.
        public static int[] CalculateSmazzataScores(GameState state)
        {
            int n = state.NumPlayers;
            var points = new int[n];

            // Scopa: already counted in PlayerState.ScopaCount per player
            for (int i = 0; i < n; i++)
            {
                points[i] += state.Players[i].ScopaCount;
            }

            // Sette Bello (7 of Denari)
            for (int i = 0; i < n; i++)
            {
                if (state.Players[i].CapturedCards.Any(c => c.IsSetteBello))
                    points[i] += 1;
            }

            // Denari majority (player with strictly most denari, and at least 6 denari)
            int[] denariCount = new int[n];
            for (int i = 0; i < n; i++)
                denariCount[i] = state.Players[i].CapturedCards.Count(c => c.Suit == Suit.Denari);
            int maxDenari = denariCount.Max();
            if (maxDenari >= 6 && denariCount.Count(x => x == maxDenari) == 1)
            {
                var winner = Array.IndexOf(denariCount, maxDenari);
                points[winner] += 1;
            }

            // Cards majority (player with strictly most captured cards, at least 21)
            int[] cardCounts = new int[n];
            for (int i = 0; i < n; i++)
                cardCounts[i] = state.Players[i].CapturedCards.Count;
            int maxCards = cardCounts.Max();
            if (maxCards >= 21 && cardCounts.Count(x => x == maxCards) == 1)
            {
                var winner = Array.IndexOf(cardCounts, maxCards);
                points[winner] += 1;
            }

            // Primiera: for each player compute best primiera sum (one per suit)
            // Primiera: compute best primiera for each player; unique highest gets +1
            int[] primieraScores = new int[n];
            for (int i = 0; i < n; i++)
            {
                primieraScores[i] = ComputePrimieraScore(state.Players[i].CapturedCards);
            }
            int maxPrimiera = primieraScores.Max();
            if (primieraScores.Count(x => x == maxPrimiera) == 1)
            {
                var winner = Array.IndexOf(primieraScores, maxPrimiera);
                points[winner] += 1;
            }

            // Grande (Re,Cavallo,Fante of denari) = +5; Piccola (Asso,2,3 of denari) = +3 plus +1 for each of 4/5/6 present
            for (int i = 0; i < n; i++)
            {
                var denari = state.Players[i].CapturedCards.Where(c => c.Suit == Suit.Denari).Select(c => c.Rank).ToHashSet();
                bool hasGrande = denari.Contains(10) && denari.Contains(9) && denari.Contains(8);
                if (hasGrande) points[i] += 5;

                bool hasPiccola = denari.Contains(1) && denari.Contains(2) && denari.Contains(3);
                if (hasPiccola)
                {
                    int add = 3;
                    // add +1 for each of 4..6 present
                    for (int r = 4; r <= 6; r++)
                        if (denari.Contains(r)) add += 1;
                    points[i] += add;
                }
            }

            return points;
        }

        private static int ComputePrimieraScore(List<Card> cards)
        {
            // Need one card per suit maximizing PrimieraValue sum
            // For each suit pick highest PrimieraValue
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
