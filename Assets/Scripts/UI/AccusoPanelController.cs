using UnityEngine;
using UnityEngine.UI;
using System;
using Project51.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Unity
{
    /// <summary>
    /// Controls the Accuso Panel UI that displays accuso animations.
    /// Shows the cards involved in the accuso with special handling for matta (7 di coppe).
    /// </summary>
    public class AccusoPanelController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private AccusoCardSlot[] cardSlots; // 3 slots expected
        
        [Header("Animation Settings")]
        [SerializeField] private float initialDisplayDuration = 1.0f; // How long to show the "special value" representation
        [SerializeField] private float flipDuration = 0.5f; // Duration of the flip animation
        [SerializeField] private float finalDisplayDuration = 1.5f; // How long to show after flip completes
        [SerializeField] private float panelFadeDuration = 0.3f;
        
        [Header("Sprites")]
        [SerializeField] private Sprite[] cardSprites; // Same array as CardViewManager for consistency
        [SerializeField] private Sprite specialValueMarker; // Optional: a star or badge to mark special values
        
        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip accusoSound;
        [SerializeField] private AudioClip flipSound;
        [SerializeField] private float audioVolume = 0.7f;
        
        private CanvasGroup canvasGroup;
        private Dictionary<Card, Sprite> cardSpriteCache = new Dictionary<Card, Sprite>();
        private bool isAnimating = false;
        private Coroutine currentSequence;

        /// <summary>
        /// Fired when the accuso animation sequence completes or is forced closed.
        /// </summary>
        public event Action OnAccusoAnimationComplete;

        /// <summary>
        /// Readonly accessor for external systems to know if the panel is animating.
        /// </summary>
        public bool IsAnimating => isAnimating;
        
        private void Awake()
        {
            // Ensure we have a CanvasGroup for fade in/out
            canvasGroup = panelRoot?.GetComponent<CanvasGroup>();
            if (canvasGroup == null && panelRoot != null)
            {
                canvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }
            
            // Start hidden
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            
            // Build local cache from provided sprites (previous behavior)
            if (cardSprites != null && cardSprites.Length >= 40)
            {
                PopulateCardSpriteCache();
            }
        }
        
        /// <summary>
        /// Shows a Decino accuso animation.
        /// Example: hand = [5, 5, 7 di coppe] where matta is used as 5.
        /// </summary>
        /// <param name="cardsInAccuso">The 3 cards in the player's hand (including matta)</param>
        /// <param name="mattaEffectiveValue">The rank the matta is counting as for this accuso (e.g., 5)</param>
        public void ShowDecinoAccuso(List<Card> cardsInAccuso, int mattaEffectiveValue)
        {
            if (isAnimating)
            {
                Debug.LogWarning("AccusoPanel is already animating, ignoring new request.");
                return;
            }
            
            currentSequence = StartCoroutine(DecinoAccusoSequence(cardsInAccuso, mattaEffectiveValue));
        }
        
        /// <summary>
        /// Shows a Cirulla accuso animation.
        /// Example: hand = [2, 3, 7 di coppe] where matta is used as 1.
        /// </summary>
        /// <param name="cardsInAccuso">The 3 cards in the player's hand (including matta)</param>
        /// <param name="mattaEffectiveValue">Always 1 for Cirulla</param>
        public void ShowCirullaAccuso(List<Card> cardsInAccuso, int mattaEffectiveValue)
        {
            if (isAnimating)
            {
                Debug.LogWarning("AccusoPanel is already animating, ignoring new request.");
                return;
            }
            
            currentSequence = StartCoroutine(CirullaAccusoSequence(cardsInAccuso, mattaEffectiveValue));
        }
        
        private IEnumerator DecinoAccusoSequence(List<Card> cards, int mattaEffectiveValue)
        {
            isAnimating = true;
            
            // Find which card is the matta
            Card mattaCard = cards.FirstOrDefault(c => c.IsMatta);
            int mattaIndex = mattaCard != null ? cards.IndexOf(mattaCard) : -1;
            
            // Step 1: Fade in panel
            yield return StartCoroutine(FadeInPanel());
            
            // Step 2: Set title and initial description
            if (titleText != null)
            {
                titleText.text = "DECINO!";
            }
            
            if (descriptionText != null && mattaCard != null)
            {
                descriptionText.text = $"Il 7 di coppe viene contato come {mattaEffectiveValue} per questo accuso.";
            }
            
            // Play accuso sound
            PlaySound(accusoSound);
            
            // Step 3: Display cards - show matta as its effective value initially
            for (int i = 0; i < cards.Count && i < cardSlots.Length; i++)
            {
                Card cardToDisplay = cards[i];
                bool isMatta = i == mattaIndex;
                
                if (isMatta)
                {
                    // Create a temporary "special value" representation
                    // Use the sprite for the effective value, marked as special
                    Sprite effectiveSprite = GetSpriteForRank(mattaEffectiveValue, cardToDisplay.Suit);
                    cardSlots[i].SetCard(effectiveSprite, isSpecialValue: true, specialMarker: specialValueMarker);
                }
                else
                {
                    // Show normal card
                    Sprite normalSprite = GetSpriteForCard(cardToDisplay);
                    cardSlots[i].SetCard(normalSprite, isSpecialValue: false);
                }
            }
            
            // Step 4: Wait to let player read
            yield return new WaitForSeconds(initialDisplayDuration);
            
            // Step 5: Flip the matta card to reveal its true identity
            if (mattaIndex >= 0 && mattaIndex < cardSlots.Length && mattaCard != null)
            {
                // Update description
                if (descriptionText != null)
                {
                    descriptionText.text = $"Carta reale: 7 di coppe. È stata usata come {mattaEffectiveValue} per il Decino.";
                }
                
                // Play flip sound
                PlaySound(flipSound);
                
                // Animate flip
                Sprite realSprite = GetSpriteForCard(mattaCard);
                yield return StartCoroutine(cardSlots[mattaIndex].FlipToRealCard(realSprite, flipDuration));
            }
            
            // Step 6: Display final state briefly
            yield return new WaitForSeconds(finalDisplayDuration);
            
            // Step 7: Fade out and hide
            yield return StartCoroutine(FadeOutPanel());

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            // Clear all slots
            foreach (var slot in cardSlots)
            {
                if (slot != null) slot.Clear();
            }

            isAnimating = false;

            // Clear current sequence and notify listeners
            currentSequence = null;
            OnAccusoAnimationComplete?.Invoke();
        }
        
        private IEnumerator CirullaAccusoSequence(List<Card> cards, int mattaEffectiveValue)
        {
            isAnimating = true;
            
            // Find which card is the matta
            Card mattaCard = cards.FirstOrDefault(c => c.IsMatta);
            int mattaIndex = mattaCard != null ? cards.IndexOf(mattaCard) : -1;
            
            // Calculate sum for display
            int sum = cards.Sum(c => c.IsMatta ? mattaEffectiveValue : c.Value);
            
            // Step 1: Fade in panel
            yield return StartCoroutine(FadeInPanel());
            
            // Step 2: Set title and initial description
            if (titleText != null)
            {
                titleText.text = "CIRULLA!";
            }
            
            if (descriptionText != null && mattaCard != null)
            {
                descriptionText.text = $"Somma ? 9! Il 7 di coppe conta come {mattaEffectiveValue}. Totale: {sum}";
            }
            else if (descriptionText != null)
            {
                descriptionText.text = $"Somma ? 9! Totale: {sum}";
            }
            
            // Play accuso sound
            PlaySound(accusoSound);
            
            // Step 3: Display cards - show matta as Ace (1) initially
            for (int i = 0; i < cards.Count && i < cardSlots.Length; i++)
            {
                Card cardToDisplay = cards[i];
                bool isMatta = i == mattaIndex;
                
                if (isMatta)
                {
                    // Create a temporary "Ace" representation
                    Sprite aceSprite = GetSpriteForRank(1, cardToDisplay.Suit);
                    cardSlots[i].SetCard(aceSprite, isSpecialValue: true, specialMarker: specialValueMarker);
                }
                else
                {
                    // Show normal card
                    Sprite normalSprite = GetSpriteForCard(cardToDisplay);
                    cardSlots[i].SetCard(normalSprite, isSpecialValue: false);
                }
            }
            
            // Step 4: Wait to let player read
            yield return new WaitForSeconds(initialDisplayDuration);
            
            // Step 5: Flip the matta card to reveal its true identity
            if (mattaIndex >= 0 && mattaIndex < cardSlots.Length && mattaCard != null)
            {
                // Update description
                if (descriptionText != null)
                {
                    descriptionText.text = "Carta reale: 7 di coppe. È stata usata come 1 per la Cirulla.";
                }
                
                // Play flip sound
                PlaySound(flipSound);
                
                // Animate flip
                Sprite realSprite = GetSpriteForCard(mattaCard);
                yield return StartCoroutine(cardSlots[mattaIndex].FlipToRealCard(realSprite, flipDuration));
            }
            
            // Step 6: Display final state briefly
            yield return new WaitForSeconds(finalDisplayDuration);
            
            // Step 7: Fade out and hide
            yield return StartCoroutine(FadeOutPanel());

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            // Clear all slots
            foreach (var slot in cardSlots)
            {
                if (slot != null) slot.Clear();
            }

            isAnimating = false;

            // Clear current sequence and notify listeners
            currentSequence = null;
            OnAccusoAnimationComplete?.Invoke();
        }

        /// <summary>
        /// Forces the panel to close immediately. Stops any running animation sequence
        /// and cleans up the UI. Useful to recover from interrupted game flow.
        /// </summary>
        public void ForceClose()
        {
            if (currentSequence != null)
            {
                StopCoroutine(currentSequence);
                currentSequence = null;
            }

            // Stop all coroutines related to fade (defensive)
            StopAllCoroutines();

            // Hide panel immediately
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            // Clear slots
            foreach (var slot in cardSlots)
            {
                if (slot != null) slot.Clear();
            }

            bool wasAnimating = isAnimating;
            isAnimating = false;

            // Notify listeners if we were animating
            if (wasAnimating)
            {
                OnAccusoAnimationComplete?.Invoke();
            }
        }
        
        private IEnumerator FadeInPanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
            
            if (canvasGroup == null)
            {
                yield break;
            }
            
            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            
            while (elapsed < panelFadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / panelFadeDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
        
        private IEnumerator FadeOutPanel()
        {
            if (canvasGroup == null)
            {
                yield break;
            }
            
            float elapsed = 0f;
            
            while (elapsed < panelFadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / panelFadeDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
        }
        
        private void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            
            // Play at camera position or fallback
            Vector3 playPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(clip, playPos, audioVolume);
        }
        
        /// <summary>
        /// Gets the sprite for a specific card from the cache or sprite array.
        /// </summary>
        private Sprite GetSpriteForCard(Card card)
        {
            if (cardSpriteCache.TryGetValue(card, out var cached))
            {
                return cached;
            }
            
            // Fallback: calculate index from suit and rank
            // Assuming sprites are ordered: Denari 1-10, Coppe 1-10, Bastoni 1-10, Spade 1-10
            if (cardSprites != null && cardSprites.Length >= 40)
            {
                int suitIndex = (int)card.Suit; // Relies on enum order: Denari=0, Coppe=1, Bastoni=2, Spade=3
                int index = suitIndex * 10 + (card.Rank - 1);
                
                if (index >= 0 && index < cardSprites.Length)
                {
                    return cardSprites[index];
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets a sprite for a specific rank, using the same suit as the original card.
        /// Used to show the "effective value" of the matta.
        /// </summary>
        private Sprite GetSpriteForRank(int rank, Suit suit)
        {
            if (cardSprites != null && cardSprites.Length >= 40)
            {
                int suitIndex = (int)suit;
                int index = suitIndex * 10 + (rank - 1);
                
                if (index >= 0 && index < cardSprites.Length)
                {
                    return cardSprites[index];
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Populates the card sprite cache for faster lookups.
        /// Assumes sprites are ordered by suit and rank.
        /// </summary>
        private void PopulateCardSpriteCache()
        {
            cardSpriteCache.Clear();
            
            if (cardSprites == null || cardSprites.Length < 40)
            {
                return;
            }
            
            // Populate for all 40 cards (4 suits × 10 ranks)
            for (int suitIndex = 0; suitIndex < 4; suitIndex++)
            {
                Suit suit = (Suit)suitIndex;
                
                for (int rank = 1; rank <= 10; rank++)
                {
                    int index = suitIndex * 10 + (rank - 1);
                    
                    if (index >= 0 && index < cardSprites.Length && cardSprites[index] != null)
                    {
                        Card card = new Card(suit, rank);
                        cardSpriteCache[card] = cardSprites[index];
                    }
                }
            }
        }
        
        /// <summary>
        /// Public method to show accuso based on the hand and accuso type.
        /// Automatically determines matta effective value.
        /// </summary>
        public void ShowAccuso(List<Card> hand, AccusoType accusoType)
        {
            if (hand == null || hand.Count != 3)
            {
                Debug.LogWarning("Accuso requires exactly 3 cards in hand.");
                return;
            }
            
            Card mattaCard = hand.FirstOrDefault(c => c.IsMatta);
            
            if (accusoType == AccusoType.Decino)
            {
                // Determine matta effective value for Decino
                int mattaValue = 1; // Default
                
                if (mattaCard != null)
                {
                    // Find the rank of the pair
                    var nonMatta = hand.Where(c => !c.IsMatta).ToList();
                    if (nonMatta.Count == 2 && nonMatta[0].Rank == nonMatta[1].Rank)
                    {
                        mattaValue = nonMatta[0].Rank;
                    }
                }
                
                ShowDecinoAccuso(hand, mattaValue);
            }
            else if (accusoType == AccusoType.Cirulla)
            {
                // Matta always counts as 1 for Cirulla
                ShowCirullaAccuso(hand, 1);
            }
            else
            {
                Debug.LogWarning($"Accuso type {accusoType} is not supported by AccusoPanelController.");
            }
        }
    }
}
