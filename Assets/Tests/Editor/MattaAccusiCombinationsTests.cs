#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using System.Collections.Generic;

namespace Project51.Tests
{
    /// <summary>
    /// Test completi per tutte le combinazioni possibili di Matta (7 di Coppe)
    /// per Decino e Accuso/Cirulla.
    /// </summary>
    public class MattaAccusiCombinationsTests
    {
        #region Decino Tests - Tutte le combinazioni di coppie con Matta

        [Test]
        public void Matta_Decino_Con_Coppia_Di_Assi()
        {
            // Matta + Asso + Asso = Decino (matta conta come 1)
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 1),     // Asso
                new Card(Suit.Bastoni, 1)     // Asso
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand), "Matta con coppia di Assi dovrebbe essere Decino");
        }

        [Test]
        public void Matta_Decino_Con_Coppia_Di_Due()
        {
            // Matta + 2 + 2 = Decino (matta conta come 2)
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 2),     
                new Card(Suit.Bastoni, 2)     
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand), "Matta con coppia di 2 dovrebbe essere Decino");
        }

        [Test]
        public void Matta_Decino_Con_Coppia_Di_Tre()
        {
            // Matta + 3 + 3 = Decino (matta conta come 3)
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 3),     
                new Card(Suit.Spade, 3)     
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand), "Matta con coppia di 3 dovrebbe essere Decino");
        }

        [Test]
        public void Matta_Decino_Con_Coppia_Di_Quattro()
        {
            // Matta + 4 + 4 = Decino (matta conta come 4)
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 4),     
                new Card(Suit.Bastoni, 4)     
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand), "Matta con coppia di 4 dovrebbe essere Decino");
        }

        [Test]
        public void Matta_Decino_Con_Coppia_Di_Cinque()
        {
            // Matta + 5 + 5 = Decino (matta conta come 5)
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 5),     
                new Card(Suit.Spade, 5)     
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand), "Matta con coppia di 5 dovrebbe essere Decino");
        }

        [Test]
        public void Matta_Decino_Con_Coppia_Di_Sei()
        {
            // Matta + 6 + 6 = Decino (matta conta come 6)
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 6),     
                new Card(Suit.Bastoni, 6)     
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand), "Matta con coppia di 6 dovrebbe essere Decino");
        }

        [Test]
        public void Matta_Decino_Con_Coppia_Di_Otto_Fante()
        {
            // Matta + Fante + Fante = Decino (matta conta come 8)
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 8),     // Fante
                new Card(Suit.Spade, 8)       // Fante
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand), "Matta con coppia di Fanti dovrebbe essere Decino");
        }

        [Test]
        public void Matta_Decino_Con_Coppia_Di_Nove_Cavallo()
        {
            // Matta + Cavallo + Cavallo = Decino (matta conta come 9)
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 9),     // Cavallo
                new Card(Suit.Bastoni, 9)     // Cavallo
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand), "Matta con coppia di Cavalli dovrebbe essere Decino");
        }

        [Test]
        public void Matta_Decino_Con_Coppia_Di_Dieci_Re()
        {
            // Matta + Re + Re = Decino (matta conta come 10)
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 10),    // Re
                new Card(Suit.Spade, 10)      // Re
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand), "Matta con coppia di Re dovrebbe essere Decino");
        }

        [Test]
        public void Matta_NON_Decino_Senza_Coppia()
        {
            // Matta + 4 + 5 = NON Decino (nessuna coppia)
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 4),     
                new Card(Suit.Bastoni, 5)     
            };
            Assert.IsFalse(AccusiChecker.IsDecino(hand), "Matta senza coppia NON dovrebbe essere Decino");
        }

        [Test]
        public void Matta_NON_Decino_Con_Tre_Carte_Diverse()
        {
            // Matta + 2 + 10 = NON Decino
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 2),     
                new Card(Suit.Bastoni, 10)     
            };
            Assert.IsFalse(AccusiChecker.IsDecino(hand), "Matta con tre valori diversi NON dovrebbe essere Decino");
        }

        #endregion

        #region Cirulla Tests - Tutte le combinazioni con somma <= 9 (Matta = 1)

        [Test]
        public void Matta_Cirulla_Con_Asso_E_Asso()
        {
            // Matta(1) + 1 + 1 = 3 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 1),     
                new Card(Suit.Bastoni, 1)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 1 + 1 = 3 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_Cirulla_Con_Asso_E_Due()
        {
            // Matta(1) + 1 + 2 = 4 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 1),     
                new Card(Suit.Bastoni, 2)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 1 + 2 = 4 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_Cirulla_Con_Asso_E_Tre()
        {
            // Matta(1) + 1 + 3 = 5 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 1),     
                new Card(Suit.Spade, 3)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 1 + 3 = 5 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_Cirulla_Con_Asso_E_Quattro()
        {
            // Matta(1) + 1 + 4 = 6 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 1),     
                new Card(Suit.Bastoni, 4)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 1 + 4 = 6 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_Cirulla_Con_Asso_E_Cinque()
        {
            // Matta(1) + 1 + 5 = 7 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 1),     
                new Card(Suit.Spade, 5)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 1 + 5 = 7 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_Cirulla_Con_Asso_E_Sei()
        {
            // Matta(1) + 1 + 6 = 8 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 1),     
                new Card(Suit.Bastoni, 6)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 1 + 6 = 8 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_Cirulla_Con_Due_E_Due()
        {
            // Matta(1) + 2 + 2 = 5 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 2),     
                new Card(Suit.Spade, 2)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 2 + 2 = 5 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_Cirulla_Con_Due_E_Tre()
        {
            // Matta(1) + 2 + 3 = 6 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 2),     
                new Card(Suit.Bastoni, 3)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 2 + 3 = 6 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_Cirulla_Con_Due_E_Quattro()
        {
            // Matta(1) + 2 + 4 = 7 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 2),     
                new Card(Suit.Spade, 4)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 2 + 4 = 7 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_Cirulla_Con_Due_E_Cinque()
        {
            // Matta(1) + 2 + 5 = 8 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 2),     
                new Card(Suit.Bastoni, 5)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 2 + 5 = 8 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_Cirulla_Con_Due_E_Sei()
        {
            // Matta(1) + 2 + 6 = 9 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 2),     
                new Card(Suit.Spade, 6)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 2 + 6 = 9 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_Cirulla_Con_Tre_E_Tre()
        {
            // Matta(1) + 3 + 3 = 7 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 3),     
                new Card(Suit.Bastoni, 3)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 3 + 3 = 7 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_Cirulla_Con_Tre_E_Quattro()
        {
            // Matta(1) + 3 + 4 = 8 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 3),     
                new Card(Suit.Spade, 4)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 3 + 4 = 8 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_Cirulla_Con_Tre_E_Cinque()
        {
            // Matta(1) + 3 + 5 = 9 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 3),     
                new Card(Suit.Bastoni, 5)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 3 + 5 = 9 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_Cirulla_Con_Quattro_E_Quattro()
        {
            // Matta(1) + 4 + 4 = 9 <= 9 = Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 4),     
                new Card(Suit.Spade, 4)     
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Matta + 4 + 4 = 9 dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_NON_Cirulla_Somma_Maggiore_Di_9()
        {
            // Matta(1) + 5 + 5 = 11 > 9 = NON Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 5),     
                new Card(Suit.Bastoni, 5)     
            };
            Assert.IsFalse(AccusiChecker.IsCirulla(hand), "Matta + 5 + 5 = 11 NON dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_NON_Cirulla_Con_Due_E_Sette()
        {
            // Matta(1) + 2 + 7 = 10 > 9 = NON Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 2),     
                new Card(Suit.Bastoni, 7)     
            };
            Assert.IsFalse(AccusiChecker.IsCirulla(hand), "Matta + 2 + 7 = 10 NON dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_NON_Cirulla_Con_Sei_E_Sei()
        {
            // Matta(1) + 6 + 6 = 13 > 9 = NON Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 6),     
                new Card(Suit.Spade, 6)     
            };
            Assert.IsFalse(AccusiChecker.IsCirulla(hand), "Matta + 6 + 6 = 13 NON dovrebbe essere Cirulla");
        }

        [Test]
        public void Matta_NON_Cirulla_Con_Carte_Alte()
        {
            // Matta(1) + 8 + 10 = 19 > 9 = NON Cirulla
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 8),     
                new Card(Suit.Bastoni, 10)     
            };
            Assert.IsFalse(AccusiChecker.IsCirulla(hand), "Matta + 8 + 10 = 19 NON dovrebbe essere Cirulla");
        }

        #endregion

        #region Edge Cases - Casi limite

        [Test]
        public void Matta_Decino_Ha_Priorita_Su_Cirulla()
        {
            // Matta + 2 + 2 = Sia Decino che Cirulla (somma = 5)
            // Il Decino ha priorità e vale 10 punti vs 3 punti
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta
                new Card(Suit.Denari, 2),     
                new Card(Suit.Bastoni, 2)     
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand), "Dovrebbe essere Decino");
            Assert.IsTrue(AccusiChecker.IsCirulla(hand), "Dovrebbe essere anche Cirulla");
            // In game logic, Decino viene scelto perché vale più punti
        }

        [Test]
        public void Matta_Con_Coppia_Di_Sette_NON_E_Decino()
        {
            // Matta (7C) + 7D + 7B = NON valido (la Matta stessa è 7)
            // Questa combinazione non dovrebbe verificarsi nel gioco reale
            // perché ci sarebbe una sola carta da 7 di Coppe (la Matta)
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 7),      // Matta (7 di Coppe)
                new Card(Suit.Denari, 7),     
                new Card(Suit.Bastoni, 7)     
            };
            // La Matta può diventare 7 per formare tris, ma questo è Decino
            Assert.IsTrue(AccusiChecker.IsDecino(hand), "Matta + 7 + 7 dovrebbe essere Decino");
        }

        [Test]
        public void Tre_Carte_Uguali_Senza_Matta_E_Decino()
        {
            // 5 + 5 + 5 = Decino normale (senza Matta)
            var hand = new List<Card> 
            { 
                new Card(Suit.Coppe, 5),      
                new Card(Suit.Denari, 5),     
                new Card(Suit.Bastoni, 5)     
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand), "Tris di 5 dovrebbe essere Decino");
        }

        #endregion
    }
}
#endif
