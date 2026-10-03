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
    }
}
