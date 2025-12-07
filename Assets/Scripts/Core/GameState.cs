using System.Collections.Generic;

namespace Project51.Core
{
    /// <summary>
    /// Represents the state of a Cirulla/51 game at any point in time.
    /// </summary>
    public class GameState
    {
        /// <summary>
        /// Number of players in the game (always 4 for Cirulla).
        /// </summary>
        public int NumPlayers { get; }

        /// <summary>
        /// Zero-based index of the current dealer.
        /// </summary>
        public int DealerIndex { get; set; }

        /// <summary>
        /// Zero-based index of the player whose turn it currently is.
        /// </summary>
        public int CurrentPlayerIndex { get; set; }

        /// <summary>
        /// Index of the player who made the last capture (to receive remaining table cards at round end).
        /// </summary>
        public int LastCapturePlayerIndex { get; set; }

        /// <summary>
        /// The draw pile (deck) of cards not yet dealt.
        /// </summary>
        public List<Card> Deck { get; }

        /// <summary>
        /// Cards currently face-up on the table.
        /// </summary>
        public List<Card> Table { get; }

        /// <summary>
        /// State of each player.
        /// </summary>
        public List<PlayerState> Players { get; }

        /// <summary>
        /// Whether the current round (smazzata) has ended.
        /// </summary>
        public bool RoundEnded { get; set; }

        public GameState(int numPlayers)
        {
            NumPlayers = numPlayers;
            DealerIndex = 0;
            CurrentPlayerIndex = 0;
            LastCapturePlayerIndex = -1; // No captures yet

            Deck = new List<Card>();
            Table = new List<Card>();
            Players = new List<PlayerState>();

            for (int i = 0; i < numPlayers; i++)
            {
                Players.Add(new PlayerState(i));
            }

            RoundEnded = false;
        }
    }
}
