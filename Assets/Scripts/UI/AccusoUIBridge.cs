using UnityEngine;
using Project51.Core;
using System.Collections.Generic;

namespace Project51.Unity
{
    /// <summary>
    /// Bridges the game logic and AccusoPanel UI.
    /// Listens for accuso events and triggers the appropriate animations.
    /// </summary>
    public class AccusoUIBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AccusoPanelController accusoPanelController;
        [SerializeField] private TurnController turnController;
        
        [Header("Settings")]
        [SerializeField] private bool autoShowAccusi = true; // Automatically show accuso animations
        
        private RoundManager roundManager;
        private AccusoType lastAccusoType = AccusoType.None;
        private int lastAccusoPlayer = -1;
        
        private void Start()
        {
            if (accusoPanelController == null)
            {
                accusoPanelController = FindObjectOfType<AccusoPanelController>();
            }
            
            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
            }
            
            // Try to get RoundManager from TurnController if it has one
            if (turnController != null)
            {
                // You may need to expose RoundManager from TurnController or get it another way
                // For now, this is a placeholder for the integration point
                Debug.Log("AccusoUIBridge: TurnController found. You may need to expose RoundManager for event subscription.");
            }
        }
        
        /// <summary>
        /// Call this to subscribe to a RoundManager's accuso events.
        /// Should be called when a new game/round starts.
        /// </summary>
        public void SubscribeToRoundManager(RoundManager manager)
        {
            // Unsubscribe from previous manager if any
            if (roundManager != null)
            {
                roundManager.OnAccusoDeclared -= HandleAccusoDeclared;
            }
            
            roundManager = manager;
            
            if (roundManager != null)
            {
                roundManager.OnAccusoDeclared += HandleAccusoDeclared;
            }
        }
        
        private void OnDestroy()
        {
            // Clean up event subscription
            if (roundManager != null)
            {
                roundManager.OnAccusoDeclared -= HandleAccusoDeclared;
            }
        }
        
        private void HandleAccusoDeclared(int playerIndex, AccusoType accusoType, List<Card> hand)
        {
            if (!autoShowAccusi || accusoPanelController == null)
            {
                return;
            }
            
            Debug.Log($"AccusoUIBridge: Player {playerIndex} declared {accusoType}");
            
            // Only show animation for Cirulla and Decino
            if (accusoType == AccusoType.Cirulla || accusoType == AccusoType.Decino)
            {
                // Check if the accuso contains matta - only animate if it does
                bool hasMatta = hand != null && hand.Exists(c => c.IsMatta);
                
                if (hasMatta)
                {
                    // In-hand temporary overlay for matta to communicate effective value
                    ApplyTemporaryMattaOverlay(playerIndex, accusoType, hand);

                    // Show the accuso panel with animation
                    accusoPanelController.ShowAccuso(hand, accusoType);

                    // Register to clear overlay when animation completes
                    accusoPanelController.OnAccusoAnimationComplete -= HandleAccusoAnimationComplete;
                    accusoPanelController.OnAccusoAnimationComplete += HandleAccusoAnimationComplete;
                    lastAccusoType = accusoType;
                    lastAccusoPlayer = playerIndex;
                }
                else
                {
                    // Optional: Show a simpler notification for non-matta accusi
                    Debug.Log($"Player {playerIndex} declared {accusoType} (no matta - no special animation).");
                }
            }
        }

        private void ApplyTemporaryMattaOverlay(int playerIndex, AccusoType accusoType, List<Card> hand)
        {
            // This method is now handled by CardViewManager's ApplyMattaSpecialVisual
            // which is called automatically during RefreshCardViews.
            // No need to manually apply overlay here - the CardViewManager will detect
            // the matta and apply the special sprite automatically.
            
            // If you need to force an immediate refresh, you can call:
            var cvManager = FindObjectOfType<CardViewManager>();
            if (cvManager != null)
            {
                cvManager.ForceRefresh();
            }
        }

        private int GetDecinoPairRank(List<Card> hand)
        {
            var nonMatta = hand.FindAll(c => !c.IsMatta);
            if (nonMatta.Count == 2 && nonMatta[0].Rank == nonMatta[1].Rank)
                return nonMatta[0].Rank;
            return 7; // fallback shouldn't happen
        }

        private Sprite GetSpecialMarkerSprite()
        {
            // Try to reuse panel's special marker
            var field = accusoPanelController.GetType().GetField("specialValueMarker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var marker = field.GetValue(accusoPanelController) as Sprite;
                return marker;
            }
            return null;
        }

        private void HandleAccusoAnimationComplete()
        {
            // Clear temporary overlays after animation
            var cvManager = FindObjectOfType<CardViewManager>();
            if (cvManager == null) return;
            var field = cvManager.GetType().GetField("activeCardViews", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null) return;
            var dict = field.GetValue(cvManager) as System.Collections.IDictionary;
            if (dict == null) return;
            foreach (System.Collections.DictionaryEntry de in dict)
            {
                if (de.Value is CardView view)
                {
                    view.ClearTemporaryValue();
                }
            }
            // Unsubscribe after handling
            accusoPanelController.OnAccusoAnimationComplete -= HandleAccusoAnimationComplete;
            lastAccusoType = AccusoType.None;
            lastAccusoPlayer = -1;
        }
        
        /// <summary>
        /// Call this method when a player declares an accuso.
        /// This should be integrated into your TurnController or game flow.
        /// </summary>
        /// <param name="playerIndex">Index of the player declaring the accuso</param>
        /// <param name="accusoType">Type of accuso (Cirulla or Decino)</param>
        /// <param name="hand">The player's hand cards</param>
        public void OnPlayerAccuso(int playerIndex, AccusoType accusoType, List<Card> hand)
        {
            HandleAccusoDeclared(playerIndex, accusoType, hand);
        }
        
        /// <summary>
        /// Manual trigger for testing in the Unity Editor.
        /// </summary>
        [ContextMenu("Test Decino Accuso (5-5-Matta)")]
        private void TestDecinoAccuso()
        {
            if (accusoPanelController == null)
            {
                Debug.LogError("AccusoPanelController not assigned!");
                return;
            }
            
            // Create test hand: 5, 5, 7 di coppe (matta)
            var testHand = new List<Card>
            {
                new Card(Suit.Denari, 5),
                new Card(Suit.Spade, 5),
                new Card(Suit.Coppe, 7) // Matta
            };
            
            accusoPanelController.ShowAccuso(testHand, AccusoType.Decino);
        }
        
        /// <summary>
        /// Manual trigger for testing in the Unity Editor.
        /// </summary>
        [ContextMenu("Test Cirulla Accuso (2-3-Matta)")]
        private void TestCirullaAccuso()
        {
            if (accusoPanelController == null)
            {
                Debug.LogError("AccusoPanelController not assigned!");
                return;
            }
            
            // Create test hand: 2, 3, 7 di coppe (matta)
            var testHand = new List<Card>
            {
                new Card(Suit.Denari, 2),
                new Card(Suit.Bastoni, 3),
                new Card(Suit.Coppe, 7) // Matta
            };
            
            accusoPanelController.ShowAccuso(testHand, AccusoType.Cirulla);
        }
    }
}
