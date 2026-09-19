using System.Collections.Generic;

namespace Project51.Core
{
    /// <summary>
    /// Posto al tavolo dei giocatori reali in una partita online, in base all'ordine di ingresso
    /// nella stanza (ActorNumber crescente, identico su ogni client).
    /// A coppie i compagni siedono di fronte (0+2 contro 1+3): i primi due entrati (host e primo
    /// amico) giocano insieme, il terzo e il quarto formano l'altra coppia. I posti rimasti liberi
    /// vanno ai bot.
    /// </summary>
    public static class SeatLayout
    {
        private static readonly int[] TeamSeats = { 0, 2, 1, 3 };

        public static int SeatForJoinOrder(GameFormat format, int joinIndex)
        {
            if (joinIndex < 0) return -1;
            return format == GameFormat.TwoVsTwo && joinIndex < TeamSeats.Length ? TeamSeats[joinIndex] : joinIndex;
        }

        /// <summary>Posti occupati da bot all'avvio, dati i giocatori reali presenti.</summary>
        public static HashSet<int> BotSeats(GameFormat format, int playerCount, int humanCount)
        {
            var bots = new HashSet<int>();
            for (int seat = 0; seat < playerCount; seat++) bots.Add(seat);
            for (int join = 0; join < humanCount && join < playerCount; join++) bots.Remove(SeatForJoinOrder(format, join));
            return bots;
        }
    }
}
