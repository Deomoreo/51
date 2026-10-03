using System.Reflection;
using NUnit.Framework;
using Project51.Core;
using Project51.Unity;
using UnityEngine;

namespace Project51.Tests
{
    /// <summary>
    /// Tests for TurnController.ResolveAIDifficulty(): verifica che la difficolta' bot scelta
    /// dall'utente (MatchConfig.BotDifficulty, esposta via GameSceneInitializer.ActiveConfig)
    /// venga mappata correttamente su AIDifficulty per CirullaAI.
    /// ResolveAIDifficulty e il setter di ActiveConfig sono privati: solo loro passano per reflection.
    /// </summary>
    public class TurnControllerResolveAIDifficultyTests
    {
        private static readonly PropertyInfo _activeConfigProperty =
            typeof(GameSceneInitializer).GetProperty(nameof(GameSceneInitializer.ActiveConfig), BindingFlags.Public | BindingFlags.Static);
        private static readonly MethodInfo _resolveAIDifficultyMethod =
            typeof(TurnController).GetMethod("ResolveAIDifficulty", BindingFlags.NonPublic | BindingFlags.Instance);

        private MatchConfig _previousActiveConfig;
        private GameObject _turnControllerGO;

        [SetUp]
        public void SetUp()
        {
            Assert.IsNotNull(_resolveAIDifficultyMethod, "TurnController.ResolveAIDifficulty non trovato.");

            _previousActiveConfig = GameSceneInitializer.ActiveConfig;
            _turnControllerGO = new GameObject("TurnControllerResolveAIDifficultyTest");
        }

        [TearDown]
        public void TearDown()
        {
            if (_turnControllerGO != null)
            {
                UnityEngine.Object.DestroyImmediate(_turnControllerGO);
            }

            SetActiveConfig(_previousActiveConfig);
        }

        [Test]
        public void Easy_Maps_To_AIDifficulty_Easy()
        {
            Assert.AreEqual(AIDifficulty.Easy, ResolveFor(BotDifficulty.Easy));
        }

        [Test]
        public void Medium_Maps_To_AIDifficulty_Medium()
        {
            Assert.AreEqual(AIDifficulty.Medium, ResolveFor(BotDifficulty.Medium));
        }

        [Test]
        public void Hard_Maps_To_AIDifficulty_Hard()
        {
            Assert.AreEqual(AIDifficulty.Hard, ResolveFor(BotDifficulty.Hard));
        }

        [Test]
        public void Expert_Maps_To_AIDifficulty_Hard()
        {
            // CirullaAI non ha ancora un livello Expert distinto (vedi TODO in TurnController.ResolveAIDifficulty).
            Assert.AreEqual(AIDifficulty.Hard, ResolveFor(BotDifficulty.Expert));
        }

        [Test]
        public void Null_ActiveConfig_Falls_Back_To_Inspector_Value()
        {
            SetActiveConfig(null);

            var turnController = _turnControllerGO.AddComponent<TurnController>();
            var result = (AIDifficulty)_resolveAIDifficultyMethod.Invoke(turnController, null);

            // Default del campo [SerializeField] aiDifficulty in TurnController e' AIDifficulty.Medium.
            Assert.AreEqual(AIDifficulty.Medium, result);
        }

        private AIDifficulty ResolveFor(BotDifficulty botDifficulty)
        {
            SetActiveConfig(new MatchConfig { BotDifficulty = botDifficulty });

            var turnController = _turnControllerGO.AddComponent<TurnController>();
            return (AIDifficulty)_resolveAIDifficultyMethod.Invoke(turnController, null);
        }

        private static void SetActiveConfig(MatchConfig config)
        {
            _activeConfigProperty.GetSetMethod(nonPublic: true).Invoke(null, new object[] { config });
        }
    }
}
