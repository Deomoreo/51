// Editor unit tests for Cirulla rules
#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Project51.Tests
{
    public class CirullaRulesTests
    {
        [Test]
        public void Accusi_Cirulla_With_Matta_Is_True()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7), // matta
                new Card(Suit.Denari, 1), // ace
                new Card(Suit.Bastoni, 1) // ace
            };

            Assert.IsTrue(AccusiChecker.IsCirulla(hand));
        }

        [Test]
        public void Accusi_Decino_With_Matta_Is_True()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7), // matta
                new Card(Suit.Denari, 5),
                new Card(Suit.Spade, 5)
            };

            Assert.IsTrue(AccusiChecker.IsDecino(hand));
        }

        [Test]
        public void Rules51_GetValidMoves_Includes_Capture15()
        {
            var state = new GameState(4);
            // ensure deterministic
            state.Players[0].Hand.Add(new Card(Suit.Denari, 6)); // played value 6 -> need sum 9 on table
            state.Table.Add(new Card(Suit.Coppe, 4));
            state.Table.Add(new Card(Suit.Bastoni, 5));

            var moves = Rules51.GetValidMoves(state, 0);
            bool found = false;
            foreach (var m in moves)
            {
                if (m.PlayedCard.Rank == 6 && m.Type == MoveType.Capture15)
                {
                    found = true; break;
                }
            }
            Assert.IsTrue(found, "Expected a Capture15 move for playing 6 capturing 4+5.");
        }

        [Test]
        public void Punteggio_Primiera_Awarded_To_Highest()
        {
            var state = new GameState(2);
            // Player 0 primiera: Denari 7 (21), Coppe 6 (18), Bastoni Ace (16), Spade 5 (15) => total 70
            state.Players[0].CapturedCards.AddRange(new[] {
                new Card(Suit.Denari,7),
                new Card(Suit.Coppe,6),
                new Card(Suit.Bastoni,1),
                new Card(Suit.Spade,5)
            });
            // Player 1 primiera: lower values
            state.Players[1].CapturedCards.AddRange(new[] {
                new Card(Suit.Denari,6),
                new Card(Suit.Coppe,5),
                new Card(Suit.Bastoni,4),
                new Card(Suit.Spade,3)
            });

            var scores = PunteggioManager.CalculateSmazzataScores(state);
            // primiera point awarded to player 0 (may combine with other points such as sette bello)
            Assert.Greater(scores[0], scores[1], "Player 0 should have more points than player 1 due to primiera");
            Assert.GreaterOrEqual(scores[0], 1, "Player 0 should receive at least one point");
        }

        [Test]
        public void Punteggio_SetteBello_Awarded()
        {
            var state = new GameState(2);
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari,7)); // sette bello
            var scores = PunteggioManager.CalculateSmazzataScores(state);
            // Player should receive at least the sette bello point (may receive additional points)
            Assert.GreaterOrEqual(scores[0], 1);
        }
    }
}
#endif
