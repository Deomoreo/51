using System.Collections.Generic;
using System.Linq;

namespace Project51.Core
{
    /// <summary>
    /// B33 (scelte dell'utente 07/10): partita guidata corta, due mini smazzate 1v1 con mazzo ridotto e mosse fissate.
    /// Prima (16 carte, mazziere il bot): uguale, somma, 15, scopa e settebello del bot, Accuso, l'asso piglia tutto; il bot
    /// chiude a 51 esatti e torna a 0. Seconda (10 carte, mazziere tu): scopa col 15 e settebello, poi due turni liberi; superi 51 e vinci.
    /// Le verifica TutorialScriptTests rigiocandole su RoundManager.
    /// </summary>
    public static class TutorialScript
    {
        /// <summary>Totali di partenza (tu, bot): +5 e +2 nella prima smazzata fanno 50 e 51 esatti.</summary>
        public static readonly int[] StartTotals = { 45, 49 };

        /// <summary>Una mossa del copione: chi gioca, la carta e le carte prese (null = la posa).</summary>
        public sealed class Step
        {
            public readonly int Player;
            public readonly Card Card;
            public readonly Card[] Takes;
            public Step(int player, Card card, params Card[] takes) { Player = player; Card = card; Takes = takes.Length > 0 ? takes : null; }

            public bool Matches(Move move) =>
                move.PlayerIndex == Player && move.PlayedCard.Equals(Card)
                && (Takes == null ? move.Type == MoveType.PlayOnly
                    : move.Type != MoveType.PlayOnly && move.CapturedCards.Count == Takes.Length && Takes.All(move.CapturedCards.Contains));
        }

        static Card D(int r) => new Card(Suit.Denari, r);
        static Card C(int r) => new Card(Suit.Coppe, r);
        static Card B(int r) => new Card(Suit.Bastoni, r);
        static Card S(int r) => new Card(Suit.Spade, r);

        /// <summary>Ordine di pesca: tre giri a partire da chi e' di mano, quattro in tavolo, poi la mano dopo.</summary>
        public static List<Card> Deck1() => new List<Card>
        {
            D(1), D(6), B(7), C(2), S(5), D(7), B(1), C(3), S(4), C(10),
            S(3), B(9), S(2), D(8), C(1), S(10),
        };

        public static List<Card> Deck2() => new List<Card> { B(3), S(5), B(6), C(6), C(9), B(4), D(7), B(1), S(2), C(3) };

        /// <summary>Prima smazzata, tutte le mosse. Prima della seconda mano il giocatore accusa (2 + Asso + 3 = 6).</summary>
        public static readonly Step[] Moves1 =
        {
            new Step(0, D(1), B(1)), new Step(1, D(6)), new Step(0, B(7), C(3), S(4)), new Step(1, C(2)),
            new Step(0, S(5), C(10)), new Step(1, D(7), D(6), C(2)),
            new Step(0, S(3)), new Step(1, B(9)), new Step(0, S(2)), new Step(1, D(8)),
            new Step(0, C(1), S(3), B(9), S(2), D(8)), new Step(1, S(10)),
        };

        /// <summary>Seconda smazzata: la presa guidata, poi solo il bot (le ultime due carte del giocatore sono libere).</summary>
        public static readonly Step[] Moves2 =
        {
            new Step(1, B(3), C(3)), new Step(0, S(5), D(7), B(1), S(2)), new Step(1, B(6)), new Step(1, C(9)),
        };

        /// <summary>Mossa del bot: la carta del copione, con la presa del copione se c'e' (dopo i turni liberi puo' cambiare).</summary>
        public static Move BotMove(Step[] script, List<Move> valid)
        {
            foreach (var step in script)
            {
                var mine = valid.Where(m => m.PlayerIndex == step.Player && m.PlayedCard.Equals(step.Card)).ToList();
                if (mine.Count > 0) return mine.FirstOrDefault(step.Matches) ?? mine[0];
            }
            return null;
        }
    }
}
