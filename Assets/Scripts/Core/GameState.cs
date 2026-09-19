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
        public int RoundIndex { get; set; }

        /// <summary>
        /// 2v2 a coppie: i compagni siedono di fronte (posti 0+2 contro 1+3) e i punti si calcolano
        /// sulle prese unite della coppia. Vedi MatchScore.EntryOf.
        /// </summary>
        public bool TeamMode { get; set; }

        /// <summary>
        /// Regole della partita. Viaggiano con lo stato (anche in rete) cosi' ogni client applica
        /// esattamente le regole scelte dall'host. Null = MatchRules.Default.
        /// </summary>
        public MatchRules Rules { get; set; }

        /// <summary>
        /// Punteggio di partita accumulato PRIMA di questa smazzata, uno per giocatore o, a coppie,
        /// uno per squadra. Null = prima smazzata della partita.
        /// </summary>
        public int[] MatchTotals { get; set; }

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
