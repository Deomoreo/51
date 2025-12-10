using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Project51.Core;
using Project51.Unity;
using System.Collections;
using System.Linq;

public class CardAccusoRevealPlayModeTests
{
    [UnityTest]
    public IEnumerator CardViewManager_RevealsHandOnAccusoDeclaration_CirullaOrDecino()
    {
        // Crea GameManager fittizio
        var goGM = new GameObject("GameManager");
        goGM.AddComponent<Project51.Unity.GameManager>();

        // Crea CardView prefab fittizio
        var prefab = new GameObject("CardViewPrefab");
        prefab.AddComponent<Project51.Unity.CardView>();
        prefab.AddComponent<SpriteRenderer>();

        // Setup scene objects
        var goController = new GameObject("TurnController");
        var turnController = goController.AddComponent<TurnController>();
        var goCardViewManager = new GameObject("CardViewManager");
        var cardViewManager = goCardViewManager.AddComponent<CardViewManager>();
        turnController.SetCardViewManager(cardViewManager);

        // Assegna il prefab tramite reflection
        typeof(CardViewManager)
            .GetField("cardViewPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(cardViewManager, prefab);

        // Setup una mano con Decino (pair + Matta)
        var hand = new System.Collections.Generic.List<Card>
        {
            new Card(Suit.Coppe, 7), // Matta
            new Card(Suit.Bastoni, 6),
            new Card(Suit.Denari, 6), // Pair -> Decino
        };
        var table = new System.Collections.Generic.List<Card>();
        turnController.StartNewGame();
        yield return null;
        turnController.SetupScenarioForCurrentPlayer(hand, table);
        yield return null;

        // Forza dichiarazione accuso
        var gameState = turnController.GameState;
        var roundManagerField = typeof(TurnController).GetField("roundManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var roundManager = (RoundManager)roundManagerField.GetValue(turnController);
        roundManager.TryPlayerAccuso(0, AccusoType.Decino);
        cardViewManager.ForceRefresh();
        yield return null;

        // Verifica che le carte del player siano mostrate scoperte
        var cardViews = cardViewManager.GetActiveCardViews().ToList();
        Assert.IsTrue(cardViews.Count >= 3, "Devono esserci almeno 3 CardView attivi per la mano del player.");
        foreach (var cv in cardViews)
        {
            if (hand.Any(h => h.Equals(cv.Card)))
            {
                Assert.IsTrue(cv.IsFaceUp, $"La carta {cv.Card} dovrebbe essere scoperta (face-up) dopo accuso Decino.");
            }
        }
    }
}
