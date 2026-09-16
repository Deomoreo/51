#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using UnityEditor;
using UnityEngine;

namespace Project51.Tests
{
    public class CardDeckTests
    {
        [TestCase("napoletano")]
        [TestCase("classico")]
        [TestCase("giada")]
        public void EverySuitAndRankHasTheCorrectFaceAndPlayableSize(string id)
        {
            var deck = CardDecks.Load(id);
            Assert.NotNull(deck);
            Assert.AreEqual(id, deck.Id);
            Assert.NotNull(deck.Back);
            Assert.That(deck.Back.bounds.size.y, Is.EqualTo(1.8f).Within(.01f));
            var unique = new System.Collections.Generic.HashSet<Sprite>();
            for (int suit = 0; suit < 4; suit++)
            for (int rank = 1; rank <= 10; rank++)
            {
                var card = new Card((Suit)suit, rank);
                var face = deck.GetFace(card);
                Assert.NotNull(face, card.ToString());
                Assert.IsTrue(unique.Add(face), "Duplicate face: " + card);
                string expected = id == "napoletano" ? card.Suit + "_" + rank + ".asset"
                    : (id == "giada" ? "51_GIADA_" : "51_") + card.Suit.ToString().ToUpperInvariant() + "_" + rank.ToString("00") + ".png";
                StringAssert.EndsWith(expected, AssetDatabase.GetAssetPath(face));
                Assert.That(face.bounds.size.y, Is.EqualTo(1.8f).Within(.01f));
            }
        }

        [Test]
        public void SelectionPersistsAndInvalidIdsCannotReplaceIt()
        {
            bool exists = PlayerPrefs.HasKey(CardDecks.PreferenceKey);
            string previous = PlayerPrefs.GetString(CardDecks.PreferenceKey);
            try
            {
                foreach (string id in new[] { "classico", "giada", "napoletano" })
                {
                    Assert.IsTrue(CardDecks.Select(id));
                    Assert.AreEqual(id, CardDecks.SelectedId);
                    Assert.AreEqual(id, PlayerPrefs.GetString(CardDecks.PreferenceKey));
                }
                Assert.IsFalse(CardDecks.Select("missing"));
                Assert.AreEqual("napoletano", CardDecks.SelectedId);
                PlayerPrefs.SetString(CardDecks.PreferenceKey, "removed-deck");
                Assert.AreEqual("napoletano", CardDecks.SelectedId);
                Assert.AreEqual("napoletano", CardDecks.Load("default").Id);
            }
            finally
            {
                if (exists) PlayerPrefs.SetString(CardDecks.PreferenceKey, previous);
                else PlayerPrefs.DeleteKey(CardDecks.PreferenceKey);
                PlayerPrefs.Save();
            }
        }
    }
}
#endif
