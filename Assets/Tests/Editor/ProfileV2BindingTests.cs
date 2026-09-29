#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Project51.Auth;
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
        public void GuestNeverShowsExperienceAndIsInvitedToRegister()
        {
            var so = new SerializedObject(screen);
            var bar = (Component)so.FindProperty("xpBar").objectReferenceValue;
            var label = (TMP_Text)so.FindProperty("xpLabel").objectReferenceValue;
            foreach (bool progress in new[] { false, true })
            {
                screen.Bind(new ProfileViewData { PlayerName = "Ospite", IsGuest = true,
                    Level = 5, XpCurrent = 250, XpMax = 500, HasProgress = progress });
                Assert.IsFalse(bar.gameObject.activeSelf);
                Assert.IsTrue(label.gameObject.activeSelf);
                Assert.AreEqual("Registrati per guadagnare XP", label.text);
            }
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

        [Test]
        public void CosmeticsFallBackToTheDefaultsAndLockWhatIsNotEarned()
        {
            Assert.AreEqual(1, ProfileCosmetics.FrameIndex(null));
            Assert.AreEqual(1, ProfileCosmetics.FrameIndex("sconosciuta"));
            Assert.AreEqual(3, ProfileCosmetics.FrameIndex("notte"));
            Assert.AreEqual(0, ProfileCosmetics.BannerIndex(""));
            Assert.AreEqual(2, ProfileCosmetics.BannerIndex("porpora"));
            Assert.IsTrue(ProfileCosmetics.BannerUnlocked(1, 1));
            Assert.IsFalse(ProfileCosmetics.BannerUnlocked(2, ProfileCosmetics.PorporaLevel - 1));
            Assert.IsTrue(ProfileCosmetics.BannerUnlocked(2, ProfileCosmetics.PorporaLevel));
            Assert.IsFalse(ProfileCosmetics.BannerUnlocked(3, 99));
            Assert.AreEqual(ProfileCosmetics.FrameIds.Length, ProfileCosmetics.FrameNames.Length);
            Assert.AreEqual(ProfileCosmetics.BannerIds.Length, ProfileCosmetics.BannerNames.Length);
            Assert.AreEqual(ProfileCosmetics.BannerIds.Length, ProfileCosmetics.BannerTags.Length);
        }

        [Test]
        public void TheLookIsSavedInOneWriteAndTheCacheOnlyAdoptsWhatTheCloudAccepted()
        {
            var service = new ProfileService();
            int writes = 0, updates = 0;
            bool accept = false, saved = false;
            string error = null;
            Dictionary<string, string> sent = null;
            service.SendUserData = (data, ok, fail) =>
            {
                writes++;
                sent = data;
                if (accept) ok(); else fail("rete");
            };
            service.OnProfileUpdated += () => updates++;

            service.SetCosmetics("avatar_03", "smeraldo", "porpora", () => saved = true, e => error = e);
            Assert.AreEqual(1, writes, "una richiesta sola");
            Assert.AreEqual(3, sent.Count, "avatar, cornice e banner insieme");
            Assert.IsFalse(saved);
            Assert.AreEqual("rete", error);
            Assert.AreEqual("default", service.AvatarId);
            Assert.AreEqual("oro", service.FrameId);
            Assert.AreEqual("notte", service.BannerId);
            Assert.AreEqual(0, updates, "la carta non cambia se il cloud rifiuta");

            accept = true;
            service.SetCosmetics("avatar_03", "smeraldo", "porpora", () => saved = true);
            Assert.IsTrue(saved);
            Assert.AreEqual(2, writes);
            Assert.AreEqual("avatar_03", service.AvatarId);
            Assert.AreEqual("smeraldo", service.FrameId);
            Assert.AreEqual("porpora", service.BannerId);
            Assert.AreEqual(1, updates, "un solo aggiornamento della carta");
        }
    }
}
#endif
