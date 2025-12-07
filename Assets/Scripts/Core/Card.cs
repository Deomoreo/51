using System;

namespace Project51.Core
{
    /// <summary>
    /// Represents a card in the Italian 40-card Cirulla deck.
    /// </summary>
    public class Card
    {
        /// <summary>
        /// Suit of the card (Denari, Coppe, Bastoni, Spade).
        /// </summary>
        public Suit Suit { get; }

        /// <summary>
        /// Rank or nominal value of the card. 
        /// 1 = Ace, 2-7 = numeric cards, 8 = Jack, 9 = Horse (Queen), 10 = King.
        /// </summary>
        public int Rank { get; }

        public Card(Suit suit, int rank)
        {
            if (rank < 1 || rank > 10)
                throw new ArgumentException($"Invalid rank {rank}. Must be 1-10.");

            Suit = suit;
            Rank = rank;
        }

        /// <summary>
        /// The in-game value for capture purposes (used in sums).
        /// Ace = 1, 2-7 = rank, Jack = 8, Horse = 9, King = 10.
        /// </summary>
        public int Value => Rank;

        /// <summary>
        /// Whether this card is the "Matta" (7 of Coppe), which can act as a wild for accusi.
        /// </summary>
        public bool IsMatta => Suit == Suit.Coppe && Rank == 7;

        /// <summary>
        /// Whether this card is the Sette Bello (7 of Denari).
        /// </summary>
        public bool IsSetteBello => Suit == Suit.Denari && Rank == 7;

        /// <summary>
        /// Whether this card is an Ace.
        /// </summary>
        public bool IsAce => Rank == 1;

        /// <summary>
        /// Value used in Primiera scoring (not the same as capture value).
        /// 7 = 21, 6 = 18, Ace = 16, 5 = 15, 4 = 14, 3 = 13, 2 = 12, K/Q/J = 10.
        /// </summary>
        public int PrimieraValue
        {
            get
            {
                switch (Rank)
                {
                    case 7: return 21;
                    case 6: return 18;
                    case 1: return 16; // Ace
                    case 5: return 15;
                    case 4: return 14;
                    case 3: return 13;
                    case 2: return 12;
                    case 8:  // Jack
                    case 9:  // Horse
                    case 10: // King
                        return 10;
                    default:
                        return 0;
                }
            }
        }

        public override string ToString()
        {
            string rankName;
            switch (Rank)
            {
                case 1: rankName = "Ace"; break;
                case 8: rankName = "Jack"; break;
                case 9: rankName = "Horse"; break;
                case 10: rankName = "King"; break;
                default: rankName = Rank.ToString(); break;
            }
            return $"{rankName} of {Suit}";
        }

        public override bool Equals(object obj)
        {
            return obj is Card card && card.Suit == Suit && card.Rank == Rank;
        }

        public override int GetHashCode()
        {
            return ((int)Suit << 8) | Rank;
        }
    }
}
