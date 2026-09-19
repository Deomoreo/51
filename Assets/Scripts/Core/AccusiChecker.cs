using System.Collections.Generic;
using System.Linq;

namespace Project51.Core
{
    public static class AccusiChecker
    {
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

        /// <summary>
        /// Valore che la matta (7 di coppe) deve assumere perche' la mano sia un accuso: il rango della
        /// coppia per il Decino, l'asso per la Cirulla. -1 se la matta non serve (o non c'e').
        /// </summary>
        public static int MattaValueForAccuso(List<Card> hand)
        {
            if (hand == null || hand.Count != 3) return -1;
            if (hand.Count(c => c.IsMatta) != 1) return -1;
            var others = hand.Where(c => !c.IsMatta).ToList();

            if (others[0].Rank == others[1].Rank)
            {
                return others[0].Rank == 7 ? -1 : others[0].Rank;
            }

            int othersSum = others[0].Value + others[1].Value;
            bool cirullaOnlyAsAce = othersSum + 7 > 9 && othersSum + 1 <= 9;
            return cirullaOnlyAsAce ? 1 : -1;
        }

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
