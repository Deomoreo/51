using System.Collections.Generic;
using NUnit.Framework;
using Project51.Core;

namespace Project51.Tests
{
    // Formato di rete di una mossa (RPC_ExecuteMove, replay al rientro): deve restare identico byte per byte.
    public class MoveSerializerTests
    {
        static string Ser(Move m) => GameStateSerializer.SerializeMove(m);
        static Move De(string s) => GameStateSerializer.DeserializeMove(s);

        [Test]
        public void CaptureMove_ExactString_AndRoundTrip()
        {
            var m = new Move(0, new Card(Suit.Denari, 7), MoveType.CaptureEqual,
                new List<Card> { new Card(Suit.Coppe, 3), new Card(Suit.Spade, 4) });
            Assert.AreEqual("0|Denari:7|1|Coppe:3,Spade:4", Ser(m));
            var back = De(Ser(m));
            Assert.AreEqual(m, back);
            Assert.AreEqual(Ser(m), Ser(back));
        }

        [Test]
        public void PlayOnlyMove_HasNoTrailingSeparator()
        {
            var m = new Move(3, new Card(Suit.Bastoni, 10), MoveType.PlayOnly);
            Assert.AreEqual("3|Bastoni:10|0", Ser(m));
            Assert.AreEqual(m, De("3|Bastoni:10|0"));
            Assert.AreEqual(0, De("3|Bastoni:10|0").CapturedCards.Count);
        }

        [Test]
        public void Malformed_ReturnsNull()
        {
            Assert.IsNull(De("0|Denari:7"));
            Assert.IsNull(De("x|Denari:7|1"));
        }

        // #151, falla nota e NON risolta: lo stato mandato dal Master Client a tutti (avvio, rientro, cambio Master) contiene le
        // mani degli avversari e il mazzo, quindi un client modificato li legge. Questa prova fotografa la falla: quando #151
        // filtra lo stato per destinatario deve fallire e va rovesciata (il destinatario vede solo la propria mano).
        [Test]
        public void HiddenCards_KnownLeak151_StateCarriesOpponentHandAndDeck()
        {
            var gs = new GameState(2);
            gs.Deck.Add(new Card(Suit.Spade, 9));
            gs.Players[0].Hand.Add(new Card(Suit.Denari, 1));
            gs.Players[1].Hand.Add(new Card(Suit.Coppe, 5));
            string wire = GameStateSerializer.Serialize(gs);
            var seenByPlayer0 = GameStateSerializer.Deserialize(wire);
            Assert.AreEqual(new Card(Suit.Coppe, 5), seenByPlayer0.Players[1].Hand[0], "mano dell'avversario in chiaro");
            Assert.AreEqual(new Card(Suit.Spade, 9), seenByPlayer0.Deck[0], "mazzo in chiaro");
        }
    }
}
