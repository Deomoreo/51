#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Project51.Core;
using UnityEngine;

namespace Project51.Tests
{
    public class MatchConfigStorageTests
    {
        private readonly List<Action> restore = new List<Action>();

        [SetUp]
        public void SetUp()
        {
            foreach (string key in new[] { "MatchIntent", "GameFormat", "BotDifficulty", "TargetScore",
                "MatchRules_EnableAccusi", "MatchRules_CappottoImmediate", "MatchRules_CappottoBonus" })
            {
                string capturedKey = key;
                bool exists = PlayerPrefs.HasKey(key);
                int value = PlayerPrefs.GetInt(key);
                restore.Add(() => { if (exists) PlayerPrefs.SetInt(capturedKey, value); else PlayerPrefs.DeleteKey(capturedKey); });
            }
            bool deckExists = PlayerPrefs.HasKey("DeckBackId");
            string deck = PlayerPrefs.GetString("DeckBackId");
            restore.Add(() => { if (deckExists) PlayerPrefs.SetString("DeckBackId", deck); else PlayerPrefs.DeleteKey("DeckBackId"); });
            bool multiplierExists = PlayerPrefs.HasKey("MatchRules_AccusiMultiplier");
            float multiplier = PlayerPrefs.GetFloat("MatchRules_AccusiMultiplier");
            restore.Add(() => { if (multiplierExists) PlayerPrefs.SetFloat("MatchRules_AccusiMultiplier", multiplier); else PlayerPrefs.DeleteKey("MatchRules_AccusiMultiplier"); });
            MatchConfigStorage.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var action in restore) action();
            restore.Clear();
            PlayerPrefs.Save();
        }

        [Test]
        public void TrainingSelectionKeepsDifficultyFormatAndRulesAcrossSceneStorage()
        {
            MatchConfigStorage.Save(new MatchConfig
            {
                Intent = MatchIntent.Training,
                Format = GameFormat.OneVsOne,
                BotDifficulty = BotDifficulty.Hard,
                TargetScore = 71,
                DeckBackId = "test-deck",
                Rules = new MatchRules
                {
                    EnableAccusi = false,
                    AccusiPointMultiplier = 0.5f,
                    CappottoEndsGameImmediately = false,
                    CappottoBonusPoints = 12
                }
            });
            var loaded = MatchConfigStorage.Load();
            Assert.AreEqual(MatchIntent.Training, loaded.Intent);
            Assert.AreEqual(GameFormat.OneVsOne, loaded.Format);
            Assert.AreEqual(BotDifficulty.Hard, loaded.BotDifficulty);
            Assert.AreEqual(71, loaded.TargetScore);
            Assert.AreEqual("test-deck", loaded.DeckBackId);
            Assert.IsFalse(loaded.Rules.EnableAccusi);
            Assert.AreEqual(0.5f, loaded.Rules.AccusiPointMultiplier);
            Assert.IsFalse(loaded.Rules.CappottoEndsGameImmediately);
            Assert.AreEqual(12, loaded.Rules.CappottoBonusPoints);
        }

        [Test]
        public void OlderPreferencesWithoutRulesUseIndependentDefaults()
        {
            PlayerPrefs.SetInt("GameFormat", (int)GameFormat.FourPlayers);
            var loaded = MatchConfigStorage.Load();
            Assert.AreEqual(MatchRules.Default.ToString(), loaded.Rules.ToString());
            Assert.AreNotSame(MatchRules.Default, loaded.Rules);
            loaded.Rules.EnableAccusi = !MatchRules.Default.EnableAccusi;
            Assert.AreEqual(MatchRules.Default.EnableAccusi, MatchConfigStorage.Load().Rules.EnableAccusi);
        }

        [Test]
        public void ClearRemovesRulesFromThePreviousMatch()
        {
            MatchConfigStorage.Save(new MatchConfig { Rules = new MatchRules { AccusiPointMultiplier = 0.5f, CappottoBonusPoints = 42 } });
            MatchConfigStorage.Clear();
            Assert.AreEqual(MatchRules.Default.ToString(), MatchConfigStorage.Load().Rules.ToString());
        }

        [Test]
        public void NullRulesReplacePreviousCustomRulesWithDefaults()
        {
            MatchConfigStorage.Save(new MatchConfig { Rules = new MatchRules { EnableAccusi = false } });
            MatchConfigStorage.Save(new MatchConfig { Rules = null });
            Assert.AreEqual(MatchRules.Default.ToString(), MatchConfigStorage.Load().Rules.ToString());
        }
    }
}
#endif
