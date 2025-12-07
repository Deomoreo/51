using System.Collections.Generic;

namespace Project51.Core
{
    /// <summary>
    /// Represents the state of one player in the game.
    /// </summary>
    public class PlayerState
    {
        /// <summary>
        /// Zero-based player index.
        /// </summary>
        public int PlayerId { get; }

        /// <summary>
        /// Cards currently in hand.
        /// </summary>
        public List<Card> Hand { get; }

        /// <summary>
        /// Cards the player has captured during the current round.
        /// </summary>
        public List<Card> CapturedCards { get; }

        /// <summary>
        /// Number of Scope scored by this player so far this round.
        /// </summary>
        public int ScopaCount { get; set; }

        /// <summary>
        /// Cards that were played to make each scopa (for visual display).
        /// Index matches with scopa number (first scopa = index 0, etc.)
        /// </summary>
        public List<Card> ScopaCards { get; }

        /// <summary>
        /// Total accumulated score (across all rounds in a match).
        /// </summary>
        public int TotalScore { get; set; }
        /// <summary>
        /// Points gained from accusi declared during the smazzata (Cirulla/Decino or dealer initial accusi).
        /// These are added to the smazzata total.
        /// </summary>
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
