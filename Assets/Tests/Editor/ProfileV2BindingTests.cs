#if UNITY_EDITOR
using NUnit.Framework;
using Project51.UIV2.Data;
using Project51.UIV2.Screens;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Project51.Tests
{
    public class ProfileV2BindingTests
    {
        private GameObject instance;
        private ProfileScreenV2 screen;

        [SetUp]
        public void SetUp()
        {
            instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/UIV2/Prefabs/Screens/ProfileScreenV2.prefab"));
            screen = instance.GetComponent<ProfileScreenV2>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(instance);

        private string TileValue(string field)
        {
            var tile = (Component)new SerializedObject(screen).FindProperty(field).objectReferenceValue;
            return tile.transform.Find("ValueLabel").GetComponent<TMP_Text>().text;
        }

        [Test]
        public void UnavailableStatisticsAreDistinctFromARealZero()
        {
            screen.Bind(new ProfileViewData { PlayerName = "Ospite", HasProgress = false,
                HasMatchStats = false, HasAdvancedStats = false, IsGuest = true });
            Assert.AreEqual("—", TileValue("matchesTile"));
            Assert.AreEqual("—", TileValue("winsTile"));
            Assert.AreEqual("—", TileValue("winRateTile"));
            var xp = (Component)new SerializedObject(screen).FindProperty("xpBar").objectReferenceValue;
            Assert.IsFalse(xp.gameObject.activeSelf);
            screen.Bind(new ProfileViewData { PlayerName = "Ospite", Level = 1, XpMax = 100,
                HasMatchStats = true, HasAdvancedStats = false });
            Assert.AreEqual("0", TileValue("matchesTile"));
            Assert.AreEqual("0%", TileValue("winRateTile"));
            Assert.AreEqual("—", TileValue("scopasTile"));
            Assert.IsTrue(xp.gameObject.activeSelf);
        }

        [Test]
        public void GuestDoesNotShowExperienceEvenIfDataHasXp()
        {
            screen.Bind(new ProfileViewData { PlayerName = "Ospite", IsGuest = true,
                Level = 5, XpCurrent = 250, XpMax = 500, HasProgress = false });
            var so = new SerializedObject(screen);
            var bar = (Component)so.FindProperty("xpBar").objectReferenceValue;
            var label = (Component)so.FindProperty("xpLabel").objectReferenceValue;
            Assert.IsFalse(bar.gameObject.activeSelf);
            Assert.IsFalse(label.gameObject.activeSelf);
        }

        [Test]
        public void LoadedAccountShowsItsCountsAndHidesGuestRegistration()
        {
            screen.Bind(new ProfileViewData { PlayerName = "Test", PlayerId = "test-id", Level = 2,
                XpCurrent = 25, XpMax = 200, MatchesPlayed = 8, Wins = 3,
                IsGuest = false, HasAdvancedStats = false });
            Assert.AreEqual("8", TileValue("matchesTile"));
            Assert.AreEqual("3", TileValue("winsTile"));
            Assert.AreEqual("38%", TileValue("winRateTile"));
            var register = (Component)new SerializedObject(screen).FindProperty("registerButton").objectReferenceValue;
            Assert.IsFalse(register.gameObject.activeSelf);
        }
    }
}
#endif
