#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using Project51.Unity;
using UnityEngine;

namespace Project51.Tests
{
    public class InGameSettingsTests
    {
        private int savedFast, savedHints, savedMusic, savedEffects;

        [SetUp]
        public void SaveUserPreferences()
        {
            savedFast = PlayerPrefs.GetInt(GamePreferences.FastAnimationsKey, 0);
            savedHints = PlayerPrefs.GetInt(GamePreferences.MoveHintsKey, 1);
            savedMusic = PlayerPrefs.GetInt(GameAudioPreferences.MusicKey, 1);
            savedEffects = PlayerPrefs.GetInt(GameAudioPreferences.EffectsKey, 1);
        }

        [TearDown]
        public void RestoreUserPreferences()
        {
            GamePreferences.SetFastAnimations(savedFast != 0);
            GamePreferences.SetMoveHints(savedHints != 0);
            GameAudioPreferences.SetMusicEnabled(savedMusic != 0);
            GameAudioPreferences.SetEffectsEnabled(savedEffects != 0);
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
        public void BoxBlurKeepsFlatImagesAndSpreadsABrightPoint()
        {
            const int size = 9;
            var flat = new Color32[size * size];
            for (int i = 0; i < flat.Length; i++) flat[i] = new Color32(40, 80, 120, 255);
            BackdropBlur.BoxBlur(flat, size, size, 2, 3);
            foreach (var c in flat) Assert.AreEqual(new Color32(40, 80, 120, 255), c);

            var point = new Color32[size * size];
            for (int i = 0; i < point.Length; i++) point[i] = new Color32(0, 0, 0, 255);
            point[4 * size + 4] = new Color32(255, 255, 255, 255);
            BackdropBlur.BoxBlur(point, size, size, 1, 1);

            Assert.Less(point[4 * size + 4].r, 255, "il punto luminoso si attenua");
            Assert.Greater(point[4 * size + 5].r, 0, "e si spande ai vicini");
            Assert.AreEqual(point[4 * size + 3].r, point[4 * size + 5].r, "in modo simmetrico");
            Assert.AreEqual(point[3 * size + 4].r, point[5 * size + 4].r);
            Assert.AreEqual(0, point[0].r, "senza toccare gli angoli lontani");
        }
    }
}
#endif
