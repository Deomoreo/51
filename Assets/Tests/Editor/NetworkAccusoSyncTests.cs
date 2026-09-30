#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using Project51.Networking;
using System.Reflection;

namespace Project51.Tests
{
    /// <summary>
    /// Accusi in rete (NetworkGameController.AlignAccusoTotal): chi riceve l'RPC deve finire con gli
    /// stessi punti del mittente anche quando il dichiarante e' il mazziere con lo 15/30 gia' preso.
    /// </summary>
    public class NetworkAccusoSyncTests
    {
        // Mazziere 0 con tavolo da 30 e Cirulla in mano; x1.5: Dealer30 = 3 punti, Cirulla = 5.
        private static (GameState state, RoundManager rm) DealerThirtyWithCirulla()
        {
            var s = new GameState(2) { DealerIndex = 0, Rules = new MatchRules { AccusiPointMultiplier = 1.5f } };
            s.Table.AddRange(new[] { new Card(Suit.Coppe, 10), new Card(Suit.Spade, 10), new Card(Suit.Denari, 5), new Card(Suit.Bastoni, 5) });
            s.Players[0].Hand.AddRange(new[] { new Card(Suit.Coppe, 1), new Card(Suit.Denari, 2), new Card(Suit.Spade, 3) });
            var rm = new RoundManager(s);
            typeof(RoundManager).GetMethod("ProcessDealerInitialAccuso", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(rm, null);
            return (s, rm);
        }

        [Test]
        public void Receiver_Matches_Sender_When_Dealer30_Dealer_Declares_Cirulla()
        {
            var (sender, senderRm) = DealerThirtyWithCirulla();
            var (receiver, _) = DealerThirtyWithCirulla();
            Assert.AreEqual(3, sender.Players[0].RoundAccusiPoints);
            Assert.AreEqual(0, sender.Players[0].AccusiPoints, "Dealer30 alone must not reveal the dealer's hand");

            Assert.IsTrue(senderRm.TryPlayerAccuso(0, AccusoType.Cirulla));
            int sent = sender.Players[0].RoundAccusiPoints; // what SendAccuso puts on the wire

            Assert.AreEqual(5, NetworkGameController.AlignAccusoTotal(receiver.Players[0], sent));
            Assert.AreEqual(8, receiver.Players[0].RoundAccusiPoints);
            Assert.AreEqual(sender.Players[0].RoundAccusiPoints, receiver.Players[0].RoundAccusiPoints);
            Assert.AreEqual(sender.Players[0].AccusiPoints, receiver.Players[0].AccusiPoints);
            Assert.AreEqual(2, receiver.Players[0].RoundAccusiCount, "Dealer30 + Cirulla: XP bonus counts on the receiver too");
            Assert.AreEqual(sender.Players[0].RoundAccusiCount, receiver.Players[0].RoundAccusiCount);
        }

        [Test]
        public void One_Accuso_Per_Hand_Even_After_A_Full_State_Resync()
        {
            var (sender, senderRm) = DealerThirtyWithCirulla();
            Assert.IsTrue(senderRm.TryPlayerAccuso(0, AccusoType.Cirulla));
            Assert.IsFalse(senderRm.TryPlayerAccuso(0, AccusoType.Cirulla), "second tap in the same hand");

            // Rientro online: lo stato completo riapre la finestra con i "gia' risolti" azzerati.
            var resynced = GameStateSerializer.Deserialize(GameStateSerializer.Serialize(sender));
            Assert.IsFalse(new RoundManager(resynced).TryPlayerAccuso(0, AccusoType.Cirulla));
            Assert.AreEqual(8, resynced.Players[0].RoundAccusiPoints);
            Assert.AreEqual(2, resynced.Players[0].RoundAccusiCount);
        }

        // Ultima carta della mano: 1 gioca il 3 (gia' fatto dal mittente), poi 0 gioca il 9 e si distribuisce la
        // mano dopo, in cui 1 riceve Asso, 2, 3 (Cirulla). Stesso stato di partenza per mittente e ricevente.
        private static (GameState state, RoundManager rm) LastCardsOfHand()
        {
            var s = new GameState(2) { DealerIndex = 0, CurrentPlayerIndex = 1, RoundIndex = 2 };
            s.Table.Add(new Card(Suit.Coppe, 10));
            s.Players[1].Hand.Add(new Card(Suit.Spade, 3));
            s.Players[0].Hand.Add(new Card(Suit.Denari, 9));
            s.Deck.AddRange(new[] { new Card(Suit.Spade, 1), new Card(Suit.Coppe, 6), new Card(Suit.Bastoni, 2),
                new Card(Suit.Coppe, 8), new Card(Suit.Denari, 3), new Card(Suit.Bastoni, 9) });
            var rm = new RoundManager(s);
            rm.ApplyMove(new Move(1, s.Players[1].Hand[0], MoveType.PlayOnly));
            return (s, rm);
        }

        private static void PlayLastCard(GameState s, RoundManager rm) =>
            rm.ApplyMove(new Move(0, s.Players[0].Hand[0], MoveType.PlayOnly));

        [Test]
        public void Accuso_For_The_Next_Hand_Waits_For_The_Receivers_Last_Move()
        {
            var (sender, senderRm) = LastCardsOfHand();
            PlayLastCard(sender, senderRm);
            Assert.IsTrue(senderRm.TryPlayerAccuso(1, AccusoType.Cirulla));
            int total = sender.Players[1].RoundAccusiPoints, round = sender.RoundIndex, deck = sender.Deck.Count;

            // Ricevente ancora alla mano prima: va tenuto da parte.
            var (receiver, receiverRm) = LastCardsOfHand();
            Assert.Less(NetworkGameController.CompareHand(receiver, round, deck), 0);
            PlayLastCard(receiver, receiverRm);
            Assert.AreEqual(0, NetworkGameController.CompareHand(receiver, round, deck));
            Assert.AreEqual(3, NetworkGameController.AlignAccusoTotal(receiver.Players[1], total));
            Assert.AreEqual(sender.Players[1].AccusiPoints, receiver.Players[1].AccusiPoints);
            Assert.IsFalse(receiverRm.TryPlayerAccuso(1, AccusoType.Cirulla), "Master fallback must not re-declare");

            // Applicato subito (vecchio comportamento): il reset di fine mano lo cancella e il fallback lo ripaga.
            var (early, earlyRm) = LastCardsOfHand();
            NetworkGameController.AlignAccusoTotal(early.Players[1], total);
            PlayLastCard(early, earlyRm);
            Assert.AreEqual(0, early.Players[1].AccusiPoints);
            Assert.IsTrue(earlyRm.TryPlayerAccuso(1, AccusoType.Cirulla), "the double award the buffer prevents");
        }

        [Test]
        public void CompareHand_Holds_An_Accuso_Until_The_Receiver_Reaches_That_Hand()
        {
            var s = new GameState(2) { RoundIndex = 2 };
            s.Deck.AddRange(new[] { new Card(Suit.Coppe, 4), new Card(Suit.Spade, 4), new Card(Suit.Denari, 6),
                new Card(Suit.Bastoni, 6), new Card(Suit.Coppe, 9), new Card(Suit.Spade, 9) });

            Assert.AreEqual(0, NetworkGameController.CompareHand(s, 2, 6), "same hand");
            Assert.Less(NetworkGameController.CompareHand(s, 2, 0), 0, "sender already dealt the next hand");
            Assert.Less(NetworkGameController.CompareHand(s, 3, 30), 0, "sender is in the next smazzata");
            Assert.Greater(NetworkGameController.CompareHand(s, 2, 12), 0, "hand already passed here");
            Assert.Greater(NetworkGameController.CompareHand(s, 1, 0), 0, "past smazzata");
        }

        [Test]
        public void Align_Is_Idempotent_For_Echo_Crossing_And_Flush_After_Full_State()
        {
            var (sender, senderRm) = DealerThirtyWithCirulla();
            var (receiver, _) = DealerThirtyWithCirulla();
            senderRm.TryPlayerAccuso(0, AccusoType.Cirulla);
            int sent = sender.Players[0].RoundAccusiPoints;

            // (a) eco dell'RPC al mittente
            Assert.AreEqual(0, NetworkGameController.AlignAccusoTotal(sender.Players[0], sent));
            Assert.AreEqual(8, sender.Players[0].RoundAccusiPoints);
            Assert.AreEqual(2, sender.Players[0].RoundAccusiCount, "echo must not count twice");

            // (c) stesso accuso inviato due volte (giocatore + fallback del Master che si incrociano)
            NetworkGameController.AlignAccusoTotal(receiver.Players[0], sent);
            Assert.AreEqual(0, NetworkGameController.AlignAccusoTotal(receiver.Players[0], sent));
            Assert.AreEqual(5, receiver.Players[0].AccusiPoints);
            Assert.AreEqual(2, receiver.Players[0].RoundAccusiCount, "crossing must not count twice");

            // (b) RPC bufferizzato svuotato dopo uno stato completo che lo include gia', o piu' vecchio
            var resynced = GameStateSerializer.Deserialize(GameStateSerializer.Serialize(sender));
            Assert.AreEqual(0, NetworkGameController.AlignAccusoTotal(resynced.Players[0], sent));
            Assert.AreEqual(0, NetworkGameController.AlignAccusoTotal(resynced.Players[0], sent - 5));
            Assert.AreEqual(8, resynced.Players[0].RoundAccusiPoints);
            Assert.AreEqual(5, resynced.Players[0].AccusiPoints);
        }
    }
}
#endif
