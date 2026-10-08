using System.Linq;
using NUnit.Framework;
using Project51.Core;
using Project51.UIV2.Core;
using Project51.UIV2.Screens;
using UnityEngine;

namespace Project51.Tests
{
    public class FrontendExpansionTests
    {
        [Test]
        public void CodesNormalizeAndRequireExactlyFiveCharacters()
        {
            Assert.AreEqual("AB2C7",RoomFlowV2.NormalizeCode(" ab-2c7 "));
            Assert.IsTrue(RoomFlowV2.IsValidCode("AB2C7"));
            Assert.IsFalse(RoomFlowV2.IsValidCode("AB2"));Assert.IsFalse(RoomFlowV2.IsValidCode("AB2C7D"));Assert.IsFalse(RoomFlowV2.IsValidCode("A!2C7"));
        }
        [Test]
        public void EmoticonLoadoutIsLimitedAndCanBeReplacedWithoutDuplicates()
        {
            string original=CollectionCosmeticsV2.Stored;
            try
            {
                CollectionCosmeticsV2.Stored="0,1,2";Assert.IsFalse(CollectionCosmeticsV2.Equip(3));
                CollectionCosmeticsV2.Remove(1);Assert.IsTrue(CollectionCosmeticsV2.Equip(3));Assert.IsFalse(CollectionCosmeticsV2.Equip(3));
                CollectionAssert.AreEqual(new[]{0,2,3},CollectionCosmeticsV2.Equipped);
                CollectionCosmeticsV2.Stored="0,0,99,invalid,2,3,4";CollectionAssert.AreEqual(new[]{0,2,3},CollectionCosmeticsV2.Equipped);
            }
            finally{CollectionCosmeticsV2.Stored=original;}
        }
        [Test]
        public void GuestMuteLastsTheSessionAndWritesNothingToThePhone()
        {
            string id="test-"+System.Guid.NewGuid().ToString("N");
            int before=PlayerPrefs.GetString(Project51.Auth.EmoticonMute.Key+id,"").Length;
            Project51.Auth.EmoticonMute.SetMuted(id,true); // nessun login vero (nessun AuthBootstrapper): come un ospite
            Assert.IsTrue(Project51.Auth.EmoticonMute.IsMuted(id));
            Assert.IsFalse(PlayerPrefs.HasKey(Project51.Auth.EmoticonMute.Key+id),"B12 (E2): a guest's mute stays in memory.");
            Assert.AreEqual(0,before);
            Project51.Auth.EmoticonMute.SetMuted(id,false);
            Assert.IsFalse(Project51.Auth.EmoticonMute.IsMuted(id));
        }
        [Test]
        public void EmoticonToggleRemovesOneInUseAndAppendsAFreeOne()
        {
            string original=CollectionCosmeticsV2.Stored;
            try
            {
                CollectionCosmeticsV2.Stored="0,1,2";Assert.IsFalse(CollectionCosmeticsV2.Toggle(3));CollectionAssert.AreEqual(new[]{0,1,2},CollectionCosmeticsV2.Equipped);
                Assert.IsTrue(CollectionCosmeticsV2.Toggle(1));CollectionAssert.AreEqual(new[]{0,2},CollectionCosmeticsV2.Equipped);
                Assert.IsTrue(CollectionCosmeticsV2.Toggle(1));CollectionAssert.AreEqual(new[]{0,2,1},CollectionCosmeticsV2.Equipped);
                CollectionCosmeticsV2.Toggle(0);CollectionCosmeticsV2.Toggle(2);CollectionCosmeticsV2.Toggle(1);CollectionAssert.IsEmpty(CollectionCosmeticsV2.Equipped);
            }
            finally{CollectionCosmeticsV2.Stored=original;}
        }
        [Test]
        public void CollectionTabLabelShowsCountOnlyWhenKnown()
        {
            Assert.AreEqual("Mazzi",CollectionScreenV2.TabLabel("Mazzi",null,true));Assert.AreEqual("Mazzi",CollectionScreenV2.TabLabel("Mazzi","",false));
            string on=CollectionScreenV2.TabLabel("Emoticon","2/3",true),off=CollectionScreenV2.TabLabel("Emoticon","2/3",false);
            StringAssert.StartsWith("Emoticon<",on);StringAssert.Contains(">2/3<",on);StringAssert.Contains(">2/3<",off);Assert.AreNotEqual(on,off);
        }
        [Test]
        public void AccusiSurviveRedealAndScoreOnlyOnceAtEnd()
        {
            var s=new GameState(2);s.Players[0].Hand.AddRange(new[]{new Card(Suit.Coppe,1),new Card(Suit.Denari,1),new Card(Suit.Spade,1)});
            s.Players[1].Hand.AddRange(new[]{new Card(Suit.Coppe,8),new Card(Suit.Denari,9),new Card(Suit.Spade,10)});
            s.Deck.AddRange(new[]{new Card(Suit.Bastoni,2),new Card(Suit.Bastoni,3),new Card(Suit.Bastoni,4),new Card(Suit.Bastoni,5),new Card(Suit.Bastoni,6),new Card(Suit.Bastoni,7)});
            var rm=new RoundManager(s);Assert.IsTrue(rm.TryPlayerAccuso(0,AccusoType.Decino));
            for(int i=0;i<6;i++)rm.ApplyMove(Rules51.GetValidMoves(s,s.CurrentPlayerIndex).First());
            Assert.AreEqual(0,s.Players[0].AccusiPoints);Assert.AreEqual(10,s.Players[0].RoundAccusiPoints);
            int expected=PunteggioManager.CalculateSmazzataScores(s)[0]+10;rm.EndSmazzata();
            // Remaining table cards may award further capture-category points.
            Assert.AreEqual(PunteggioManager.CalculateSmazzataScores(s)[0]+10,s.Players[0].TotalScore);
            int total=s.Players[0].TotalScore;rm.EndSmazzata();Assert.AreEqual(total,s.Players[0].TotalScore);
        }
    }
}
