using System.Collections.Generic;

namespace Project51.Core
{
    public class GameState
    {
        public int NumPlayers { get; }

        public int DealerIndex { get; set; }

        public int CurrentPlayerIndex { get; set; }

        public int LastCapturePlayerIndex { get; set; }

        public List<Card> Deck { get; }

        public List<Card> Table { get; }

        public List<PlayerState> Players { get; }

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
