using NUnit.Framework;
using Project51.Core;
using Project51.Unity;
using UnityEngine;

namespace Project51.Tests
{
    public class InGameSettingsTests
    {
        private int savedFast, savedHints, savedMusic, savedEffects;
        private int savedReduced, savedQuality;
        private bool hadFast, hadReduced, hadQuality;

        [SetUp]
        public void SaveUserPreferences()
        {
            savedFast = PlayerPrefs.GetInt(GamePreferences.FastAnimationsKey, 0);
            hadFast = PlayerPrefs.HasKey(GamePreferences.FastAnimationsKey);
            hadReduced = PlayerPrefs.HasKey(GamePreferences.ReducedGraphicsKey);
            savedReduced = PlayerPrefs.GetInt(GamePreferences.ReducedGraphicsKey);
            hadQuality = PlayerPrefs.HasKey(GamePreferences.GraphicsQualityKey);
            savedQuality = PlayerPrefs.GetInt(GamePreferences.GraphicsQualityKey);
            savedHints = PlayerPrefs.GetInt(GamePreferences.MoveHintsKey, 1);
            savedMusic = PlayerPrefs.GetInt(GameAudioPreferences.MusicKey, 1);
            savedEffects = PlayerPrefs.GetInt(GameAudioPreferences.EffectsKey, 1);
        }

        [TearDown]
        public void RestoreUserPreferences()
        {
            Restore(GamePreferences.FastAnimationsKey, hadFast, savedFast);
            Restore(GamePreferences.ReducedGraphicsKey, hadReduced, savedReduced);
            Restore(GamePreferences.GraphicsQualityKey, hadQuality, savedQuality);
            foreach (var field in new[] { "graphicsQuality", "fastAnimations" })
                typeof(GamePreferences).GetField(field, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).SetValue(null, -1);
            GamePreferences.SetMoveHints(savedHints != 0);
            GameAudioPreferences.SetMusicEnabled(savedMusic != 0);
            GameAudioPreferences.SetEffectsEnabled(savedEffects != 0);
        }

        private static void Restore(string key, bool had, int value)
        {
            if (had) PlayerPrefs.SetInt(key, value);
            else PlayerPrefs.DeleteKey(key);
        }

        [Test]
        public void FastAnimationsShortenDurationsAndNotifyTheTable()
        {
            int notifications = 0;
            System.Action listener = () => notifications++;
            GamePreferences.Changed += listener;
            try
            {
                GamePreferences.SetFastAnimations(false);
                Assert.AreEqual(1f, GamePreferences.AnimationSpeed);
                Assert.AreEqual(2f, GamePreferences.Scaled(2f), 0.0001f);

                GamePreferences.SetFastAnimations(true);
                Assert.IsTrue(GamePreferences.FastAnimations);
                Assert.AreEqual(GamePreferences.FastAnimationSpeed, GamePreferences.AnimationSpeed);
                Assert.Less(GamePreferences.Scaled(2f), 2f);
                Assert.AreEqual(2, notifications);
            }
            finally
            {
                GamePreferences.Changed -= listener;
            }
        }

        [Test]
        public void MoveHintsAndAudioChannelsAreSaved()
        {
            GamePreferences.SetMoveHints(false);
            Assert.IsFalse(GamePreferences.MoveHints);
            Assert.AreEqual(0, PlayerPrefs.GetInt(GamePreferences.MoveHintsKey, 1));

            GameAudioPreferences.SetMusicEnabled(false);
            GameAudioPreferences.SetEffectsEnabled(true);
            Assert.IsFalse(GameAudioPreferences.MusicEnabled);
            Assert.AreEqual(GameAudioPreferences.Enabled, GameAudioPreferences.EffectsEnabled);
        }

        [Test]
        public void MasterAudioOffMovesToTheTwoChannels()
        {
            bool master = GameAudioPreferences.Enabled;
            try
            {
                GameAudioPreferences.SetMusicEnabled(true);
                GameAudioPreferences.SetEffectsEnabled(true);
                GameAudioPreferences.SetEnabled(false);

                GameAudioPreferences.FoldMasterIntoChannels();
                Assert.IsTrue(GameAudioPreferences.Enabled);
                Assert.IsFalse(GameAudioPreferences.MusicChoice);
                Assert.IsFalse(GameAudioPreferences.EffectsChoice);

                GameAudioPreferences.SetEffectsEnabled(true);
                GameAudioPreferences.FoldMasterIntoChannels();
                Assert.IsTrue(GameAudioPreferences.EffectsEnabled, "Con l'audio generale acceso non tocca le scelte");
                Assert.IsFalse(GameAudioPreferences.MusicEnabled);
            }
            finally
            {
                GameAudioPreferences.SetEnabled(master);
            }
        }
    }
}
