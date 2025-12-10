using System.Collections.Generic;

namespace Project51.Core
{
    public class PlayerState
    {
        public int PlayerId { get; }

        public List<Card> Hand { get; }

        public List<Card> CapturedCards { get; }

        public int ScopaCount { get; set; }

        public List<Card> ScopaCards { get; }

        public int TotalScore { get; set; }
        public int AccusiPoints { get; set; }

        public PlayerState(int playerId)
        {
            PlayerId = playerId;
            Hand = new List<Card>();
            CapturedCards = new List<Card>();
            ScopaCards = new List<Card>();
            ScopaCount = 0;
            TotalScore = 0;
        }
    }
}
