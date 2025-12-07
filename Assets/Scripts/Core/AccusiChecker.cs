using System.Collections.Generic;
using System.Linq;

namespace Project51.Core
{
    public static class AccusiChecker
    {
        // Cirulla: if sum of 3 hand cards <= 9 -> +3
        // Matta (7 di Coppe) counts automatically as Ace (1) for this evaluation
        public static bool IsCirulla(List<Card> hand)
        {
            if (hand == null || hand.Count != 3) return false;
            int sum = 0;
            foreach (var c in hand)
            {
                sum += c.IsMatta ? 1 : c.Value;
            }
            return sum <= 9;
        }

        // Decino: three of a kind -> +10
        // Matta (7 di Coppe) automatically takes the value of the existing pair (X,X,7C -> trio di X)
        public static bool IsDecino(List<Card> hand)
        {
            if (hand == null || hand.Count != 3) return false;

            int mattaCount = hand.Count(c => c.IsMatta);
            var nonMatta = hand.Where(c => !c.IsMatta).ToList();

            if (mattaCount == 0)
            {
                // All three equal ranks
                return nonMatta.Count == 3 && nonMatta.All(c => c.Rank == nonMatta[0].Rank);
            }

            if (mattaCount == 1)
            {
                // Exactly one matta and two non-matta: must be a pair
                if (nonMatta.Count == 2 && nonMatta[0].Rank == nonMatta[1].Rank)
                    return true;
                return false;
            }

            // In a standard deck there is only one matta; any other case is not a Decino by rules
            return false;
        }
    }
}
