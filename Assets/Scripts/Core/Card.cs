using System;

namespace Project51.Core
{
    public class Card
    {
        public Suit Suit { get; }

        public int Rank { get; }

        public Card(Suit suit, int rank)
        {
            if (rank < 1 || rank > 10)
                throw new ArgumentException($"Invalid rank {rank}. Must be 1-10.");

            Suit = suit;
            Rank = rank;
        }

        public int Value => Rank;

        public bool IsMatta => Suit == Suit.Coppe && Rank == 7;

        public bool IsSetteBello => Suit == Suit.Denari && Rank == 7;

        public bool IsAce => Rank == 1;

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
