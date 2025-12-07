using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Project51.Unity
{
    /// <summary>
    /// Represents a single card slot in the AccusoPanel.
    /// Handles displaying a card sprite and animating the flip reveal.
    /// </summary>
    public class AccusoCardSlot : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image cardImage;
        [SerializeField] private Image specialMarkerImage; // Optional: star or badge overlay for special values
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color specialTint = new Color(1f, 1f, 0.7f, 1f); // Slight yellow tint for special values
        
        private RectTransform rectTransform;
        private bool isSpecial = false;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            
            // Ensure card image exists
            if (cardImage == null)
            {
                cardImage = GetComponentInChildren<Image>();
            }
            
            // Hide special marker by default
            if (specialMarkerImage != null)
            {
                specialMarkerImage.enabled = false;
            }
        }
        
        /// <summary>
        /// Sets the card to display in this slot.
        /// </summary>
        /// <param name="cardSprite">The sprite to display</param>
        /// <param name="isSpecialValue">Whether this represents a special/temporary value (matta)</param>
        /// <param name="specialMarker">Optional sprite to overlay when showing special value</param>
        public void SetCard(Sprite cardSprite, bool isSpecialValue, Sprite specialMarker = null)
        {
            if (cardImage == null) return;
            
            cardImage.sprite = cardSprite;
            cardImage.enabled = cardSprite != null;
            isSpecial = isSpecialValue;
            
            // Apply visual distinction for special values
            if (isSpecialValue)
            {
                cardImage.color = specialTint;
                
                // Show special marker if available
                if (specialMarkerImage != null && specialMarker != null)
                {
                    specialMarkerImage.sprite = specialMarker;
                    specialMarkerImage.enabled = true;
                }
            }
            else
            {
                cardImage.color = normalColor;
                
                if (specialMarkerImage != null)
                {
                    specialMarkerImage.enabled = false;
                }
            }
            
            // Reset rotation
            if (rectTransform != null)
            {
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localScale = Vector3.one;
            }
        }
        
        /// <summary>
        /// Animates a flip to reveal the real card sprite.
        /// Rotates around Y-axis, swapping sprite at 90 degrees.
        /// </summary>
        /// <param name="realCardSprite">The real card sprite to show after flip</param>
        /// <param name="duration">Duration of the flip animation</param>
        public IEnumerator FlipToRealCard(Sprite realCardSprite, float duration)
        {
            if (rectTransform == null || cardImage == null)
            {
                yield break;
            }
            
            float elapsed = 0f;
            bool spriteSwapped = false;
            
            // Store initial sprite
            Sprite initialSprite = cardImage.sprite;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Rotate from 0 to 180 degrees around Y axis
                float angle = Mathf.Lerp(0f, 180f, t);
                rectTransform.localRotation = Quaternion.Euler(0f, angle, 0f);
                
                // Swap sprite at 90 degrees (midpoint)
                if (!spriteSwapped && angle >= 90f)
                {
                    cardImage.sprite = realCardSprite;
                    cardImage.color = normalColor; // Remove special tint
                    
                    // Hide special marker
                    if (specialMarkerImage != null)
                    {
                        specialMarkerImage.enabled = false;
                    }
                    
                    isSpecial = false;
                    spriteSwapped = true;
                }
                
                yield return null;
            }
            
            // Ensure final state
            rectTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            
            // Flip back to face forward (optional: or keep at 180)
            // For a cleaner look, rotate back to 0 after brief pause
            yield return new WaitForSeconds(0.1f);
            
            elapsed = 0f;
            float returnDuration = duration * 0.3f; // Faster return
            
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / returnDuration;
                
                float angle = Mathf.Lerp(180f, 360f, t);
                rectTransform.localRotation = Quaternion.Euler(0f, angle, 0f);
                
                yield return null;
            }
            
            // Final state: back to normal orientation
            rectTransform.localRotation = Quaternion.identity;
        }
        
        /// <summary>
        /// Clears the slot (hides the card).
        /// </summary>
        public void Clear()
        {
            if (cardImage != null)
            {
                cardImage.sprite = null;
                cardImage.enabled = false;
                cardImage.color = normalColor;
            }
            
            if (specialMarkerImage != null)
            {
                specialMarkerImage.enabled = false;
            }
            
            if (rectTransform != null)
            {
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localScale = Vector3.one;
            }
            
            isSpecial = false;
        }
    }
}
