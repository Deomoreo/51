using System;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Core
{
    public static class PunteggioManager
    {
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

            // Denari majority (player with strictly most denari wins 1 point)
            // No points awarded if there's a tie, even if someone has >= 6
            int[] denariCount = new int[n];
            for (int i = 0; i < n; i++)
                denariCount[i] = state.Players[i].CapturedCards.Count(c => c.Suit == Suit.Denari);
            int maxDenari = denariCount.Max();
            // Only award point if there's a clear winner (no tie)
            if (denariCount.Count(x => x == maxDenari) == 1)
            {
                var winner = Array.IndexOf(denariCount, maxDenari);
                // Only award the point if the winner has at least 6 denari
                if (denariCount[winner] >= 6)
                    points[winner] += 1;
            }

            // Cards majority (player with strictly most captured cards wins 1 point)
            // No points awarded if there's a tie, even if someone has >= 21
            int[] cardCounts = new int[n];
            for (int i = 0; i < n; i++)
                cardCounts[i] = state.Players[i].CapturedCards.Count;
            int maxCards = cardCounts.Max();
            // Only award point if there's a clear winner (no tie)
            if (cardCounts.Count(x => x == maxCards) == 1)
            {
                var winner = Array.IndexOf(cardCounts, maxCards);
                // Only award the point if the winner has at least 21 cards
                if (cardCounts[winner] >= 21)
                    points[winner] += 1;
            }

            // Primiera: compute best primiera for each player; unique highest gets +1
            // Player must have at least one card of each suit to be eligible for primiera
            int[] primieraScores = new int[n];
            bool[] hasAllSuits = new bool[n];
            for (int i = 0; i < n; i++)
            {
                var playerSuits = state.Players[i].CapturedCards.Select(c => c.Suit).Distinct().Count();
                hasAllSuits[i] = (playerSuits == 4);
                if (hasAllSuits[i])
                {
                    primieraScores[i] = ComputePrimieraScore(state.Players[i].CapturedCards);
                }
                else
                {
                    primieraScores[i] = -1; // Mark as ineligible
                }
            }
            int maxPrimiera = primieraScores.Max();
            // Only award if max score is valid (>= 0) and unique
            if (maxPrimiera >= 0 && primieraScores.Count(x => x == maxPrimiera) == 1)
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
