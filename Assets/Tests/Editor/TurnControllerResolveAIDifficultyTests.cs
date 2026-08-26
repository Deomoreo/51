#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using Project51.Core;
using UnityEngine;

namespace Project51.Tests
{
    /// <summary>
    /// Tests for TurnController.ResolveAIDifficulty(): verifica che la difficolta' bot scelta
    /// dall'utente (MatchConfig.BotDifficulty, esposta via GameSceneInitializer.ActiveConfig)
    /// venga mappata correttamente su AIDifficulty per CirullaAI.
    /// TurnController vive nell'assembly Project51.Gameplay (nessun riferimento a compile-time
    /// da questo assembly di test), quindi il tipo e il metodo privato sono risolti via reflection.
    /// </summary>
    public class TurnControllerResolveAIDifficultyTests
    {
        private static Type _turnControllerType;
        private static Type _gameSceneInitializerType;
        private static PropertyInfo _activeConfigProperty;
        private static MethodInfo _resolveAIDifficultyMethod;

        private object _previousActiveConfig;
        private GameObject _turnControllerGO;

        [SetUp]
        public void SetUp()
        {
            _turnControllerType ??= FindType("Project51.Unity.TurnController");
            _gameSceneInitializerType ??= FindType("Project51.Unity.GameSceneInitializer");

            Assert.IsNotNull(_turnControllerType, "Project51.Unity.TurnController non trovato in nessun assembly caricato.");
            Assert.IsNotNull(_gameSceneInitializerType, "Project51.Unity.GameSceneInitializer non trovato in nessun assembly caricato.");

            _activeConfigProperty ??= _gameSceneInitializerType.GetProperty("ActiveConfig", BindingFlags.Public | BindingFlags.Static);
            _resolveAIDifficultyMethod ??= _turnControllerType.GetMethod("ResolveAIDifficulty", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(_activeConfigProperty, "GameSceneInitializer.ActiveConfig non trovato.");
            Assert.IsNotNull(_resolveAIDifficultyMethod, "TurnController.ResolveAIDifficulty non trovato.");

            _previousActiveConfig = _activeConfigProperty.GetValue(null);
            _turnControllerGO = new GameObject("TurnControllerResolveAIDifficultyTest");
        }

        [TearDown]
        public void TearDown()
        {
            if (_turnControllerGO != null)
            {
                UnityEngine.Object.DestroyImmediate(_turnControllerGO);
            }

            SetActiveConfig(_previousActiveConfig as MatchConfig);
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

            var turnController = _turnControllerGO.AddComponent(_turnControllerType);
            var result = (AIDifficulty)_resolveAIDifficultyMethod.Invoke(turnController, null);

            // Default del campo [SerializeField] aiDifficulty in TurnController e' AIDifficulty.Medium.
            Assert.AreEqual(AIDifficulty.Medium, result);
        }

        private AIDifficulty ResolveFor(BotDifficulty botDifficulty)
        {
            SetActiveConfig(new MatchConfig { BotDifficulty = botDifficulty });

            var turnController = _turnControllerGO.AddComponent(_turnControllerType);
            return (AIDifficulty)_resolveAIDifficultyMethod.Invoke(turnController, null);
        }

        private static void SetActiveConfig(MatchConfig config)
        {
            _activeConfigProperty.GetSetMethod(nonPublic: true).Invoke(null, new object[] { config });
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }
    }
}
#endif
