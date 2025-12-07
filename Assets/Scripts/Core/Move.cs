using System.Collections.Generic;

namespace Project51.Core
{
    /// <summary>
    /// Represents a single move a player can make during their turn.
    /// </summary>
    public class Move
    {
        /// <summary>
        /// Index of the player making the move.
        /// </summary>
        public int PlayerIndex { get; }

        /// <summary>
        /// The card the player plays from their hand.
        /// </summary>
        public Card PlayedCard { get; }

        /// <summary>
        /// The type of move.
        /// </summary>
        public MoveType Type { get; }

        /// <summary>
        /// Cards captured from the table (empty if PlayOnly).
        /// </summary>
        public List<Card> CapturedCards { get; }

        public Move(int playerIndex, Card playedCard, MoveType type, List<Card> capturedCards = null)
        {
            PlayerIndex = playerIndex;
            PlayedCard = playedCard;
            Type = type;
            CapturedCards = capturedCards ?? new List<Card>();
        }

        public override bool Equals(object obj)
        {
            if (obj is not Move other) return false;
            if (PlayerIndex != other.PlayerIndex) return false;
            if (Type != other.Type) return false;
            if (!PlayedCard.Equals(other.PlayedCard)) return false;

            // Compare captured cards as multisets (order-insensitive)
            if ((CapturedCards == null || CapturedCards.Count == 0) && (other.CapturedCards == null || other.CapturedCards.Count == 0))
                return true;

            if (CapturedCards == null || other.CapturedCards == null) return false;
            if (CapturedCards.Count != other.CapturedCards.Count) return false;

            // Use counts by card
            var counts = new Dictionary<Card, int>();
            foreach (var c in CapturedCards)
            {
                if (!counts.ContainsKey(c)) counts[c] = 0;
                counts[c]++;
            }
            foreach (var c in other.CapturedCards)
            {
                if (!counts.ContainsKey(c)) return false;
                counts[c]--;
                if (counts[c] < 0) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + PlayerIndex.GetHashCode();
                hash = hash * 31 + Type.GetHashCode();
                hash = hash * 31 + (PlayedCard != null ? PlayedCard.GetHashCode() : 0);
                if (CapturedCards != null)
                {
                    // order-insensitive hash for captured cards: sum of individual hashes
                    int sum = 0;
                    foreach (var c in CapturedCards) sum += c.GetHashCode();
                    hash = hash * 31 + sum;
                }
                return hash;
            }
        }

        public override string ToString()
        {
            if (Type == MoveType.PlayOnly)
                return $"Player {PlayerIndex} plays {PlayedCard} (no capture)";
            else
                return $"Player {PlayerIndex} plays {PlayedCard} and captures {CapturedCards.Count} card(s) [{Type}]";
        }
    }
}
