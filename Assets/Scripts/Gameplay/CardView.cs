using UnityEngine;
using Project51.Core;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project51.Unity
{
    /// <summary>
    /// Represents a single card visually in Unity.
    /// Handles rendering and user interaction for a card.
    /// </summary>
    public class CardView : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite defaultCardBack;

        private Card card;
        private bool isClickable = false;
        private bool enableHover = false;
        [SerializeField] private float raiseAmount = 0.35f;
        [SerializeField] private int selectionSortingBoost = 100;
        [SerializeField] private float selectionAnimDuration = 0.12f;

        private bool isSelected = false;
        private Vector3 originalPosition;
        private int originalSortingOrder = 0;
        private Coroutine selectionCoroutine;

        public Card Card => card;
        public bool IsClickable
        {
            get => isClickable;
            set => isClickable = value;
        }

        /// <summary>
        /// Whether this card should show hover effects (scale) when the mouse is over it.
        /// AI cards will typically have this disabled.
        /// </summary>
        public bool EnableHover
        {
            get => enableHover;
            set => enableHover = value;
        }

        /// <summary>
        /// True se la carta è attualmente scoperta (face-up), false se mostra il dorso.
        /// </summary>
        public bool IsFaceUp
        {
            get
            {
                if (spriteRenderer == null) return false;
                // Considera la carta scoperta se la sprite NON è il dorso
                return spriteRenderer.sprite != null && spriteRenderer.sprite != defaultCardBack;
            }
        }

        public event Action<CardView> OnCardClicked;
        public event Action<CardView> OnCardDoubleClicked;
        public event Action<CardView, Vector3> OnDragReleased;

        /// <summary>
        /// Initializes this view with a specific card.
        /// </summary>
        public void Initialize(Card card, Sprite cardSprite = null, bool faceUp = true)
        {
            this.card = card;

            if (spriteRenderer == null)
            {
                // Try to find a SpriteRenderer on this object or its children.
                spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
                // If still null, add one so the card can be seen at runtime even when prefab was misconfigured.
                if (spriteRenderer == null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (spriteRenderer != null)
            {
                // Face-up cards must NEVER show the back. If a face sprite is missing,
                // use a placeholder so the player can still see the card.
                if (faceUp)
                {
                    spriteRenderer.sprite = cardSprite ?? GetPlaceholderSprite();
                }
                else
                {
                    // Face-down cards: prefer back, then fallback to any available sprite
                    spriteRenderer.sprite = defaultCardBack ?? cardSprite ?? GetPlaceholderSprite();
                }
                spriteRenderer.enabled = true;

                // If for any reason no sprite was assigned, force a placeholder so the card is visible.
                if (spriteRenderer.sprite == null)
                {
                    spriteRenderer.sprite = GetPlaceholderSprite();
                }

                // IMPORTANT: Store the original sprite BEFORE any ShowTemporaryValue is called
                // This ensures originalFaceSprite always contains the real card sprite (7 di Coppe for Matta)
                originalFaceSprite = spriteRenderer.sprite;

                // Ensure visible color and sorting order
                spriteRenderer.color = Color.white;
                if (string.IsNullOrEmpty(spriteRenderer.sortingLayerName))
                    spriteRenderer.sortingLayerName = "Default";
                // elevate order so UI/camera overlays don't hide them
                spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 0);

                // Diagnostic log to help track visibility issues at runtime
                var spriteName = spriteRenderer.sprite != null ? spriteRenderer.sprite.name : "<none>";
                // remember original values for selection animation
                originalSortingOrder = spriteRenderer.sortingOrder;
            }

            // Ensure there is a Collider2D so OnMouse* callbacks fire for 2D sprites
            InitCollider();

            // store original position for selection animation
            originalPosition = transform.position;

            string rankLabel = card.Rank switch { 1 => "Asso", 8 => "Fante", 9 => "Cavallo", 10 => "Re", _ => card.Rank.ToString() };
            gameObject.name = $"CardView_{card.Suit}_{rankLabel}";
        }

        private void InitCollider()
        {
            var col = GetComponent<Collider2D>();
            if (col == null)
            {
                var sr = GetComponent<SpriteRenderer>();
                var box = gameObject.AddComponent<BoxCollider2D>();
                if (sr != null && sr.sprite != null)
                {
                    box.size = sr.sprite.bounds.size;
                }
                box.isTrigger = false;
            }
        }

        /// <summary>
        /// Allows external code to set the default back sprite on this view.
        /// </summary>
        public void SetDefaultBack(Sprite back)
        {
            defaultCardBack = back;
        }

        private void OnMouseDown()
        {
            if (isClickable)
            {
                // Prevent interactions when not local player's turn
                if (!IsLocalPlayersTurn()) return;
                var cam = Camera.main;
                if (cam != null)
                {
                    var mp = Input.mousePosition;
                    mp.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
                    var world = cam.ScreenToWorldPoint(mp);
                    dragOffset = world - transform.position;
                }

                // detect double click
                float now = Time.time;
                if (now - lastClickTime <= doubleClickThreshold)
                {
                    OnCardDoubleClicked?.Invoke(this);
                    lastClickTime = 0f;
                }
                else
                {
                    lastClickTime = now;
                    OnCardClicked?.Invoke(this);
                }
            }
        }

        private Vector3 dragOffset;
        private bool isDragging = false;
        [SerializeField] private bool allowDrag = false; // default: disable drag for human players

        private void OnMouseEnter()
        {
            isMouseOver = true;
            
            // Simple hover effect only when enabled: slightly scale up
            if (enableHover)
            {
                // if selected, keep selection animation; otherwise scale smoothly
                if (!isSelected)
                    transform.localScale = Vector3.one * 1.05f;

                // Show original 7 di Coppe when hovering (hide Matta temporary value)
                if (showingTemporaryValue && originalFaceSprite != null)
                {
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.sprite = originalFaceSprite;
                        showingTemporaryValue = false;
                        SetMarkerVisible(false);
                    }
                }
            }
        }

        private void OnMouseExit()
        {
            isMouseOver = false;
            
            if (!isDragging && enableHover)
            {
                if (!isSelected)
                    transform.localScale = Vector3.one;

                // Restore Matta temporary value when leaving hover
                if (!showingTemporaryValue && temporaryFaceSprite != null)
                {
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.sprite = temporaryFaceSprite;
                        showingTemporaryValue = true;
                        SetMarkerVisible(markerRenderer != null && markerRenderer.sprite != null);
                    }
                }
            }
        }

        private void OnMouseDrag()
        {
            if (!isClickable) return;
            if (!IsLocalPlayersTurn()) return;
            if (!allowDrag) return;
            var cam = Camera.main;
            if (cam == null) return;
            var mp = Input.mousePosition;
            mp.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
            var world = cam.ScreenToWorldPoint(mp);
            transform.position = new Vector3(world.x - dragOffset.x, world.y - dragOffset.y, transform.position.z);
            isDragging = true;
        }

        private void OnMouseUp()
        {
            if (isDragging)
            {
                isDragging = false;
                transform.localScale = Vector3.one;
                // notify listeners about drag release only if dragging allowed
                if (allowDrag)
                {
                    OnDragReleased?.Invoke(this, transform.position);
                }
            }
        }

        private bool IsLocalPlayersTurn()
        {
            // Check via GameManager reflection to avoid hard assembly refs
            try
            {
                var gmType = System.Type.GetType("Project51.Unity.GameManager, Project51.Networking");
                if (gmType != null)
                {
                    var instanceProp = gmType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var gm = instanceProp?.GetValue(null);
                    if (gm != null)
                    {
                        var isLocalTurnMethod = gmType.GetMethod("IsLocalPlayerTurn", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (isLocalTurnMethod != null)
                        {
                            var res = isLocalTurnMethod.Invoke(gm, null);
                            if (res is bool b) return b;
                        }
                    }
                }
            }
            catch
            {
                // ignore reflection errors in editor
            }
            // Default to true in single-player or if GM not found
            return true;
        }

        private float lastClickTime = 0f;
        private const float doubleClickThreshold = 0.35f;

        // Temporary value overlay support (for Matta during accusi)
        private Sprite originalFaceSprite;
        private Sprite temporaryFaceSprite;
        private bool showingTemporaryValue = false;
        private bool isMouseOver = false; // Track if mouse is currently over this card
        private SpriteRenderer markerRenderer; // small overlay marker

        // When clicked we toggle selection elevation animation
        public void SetSelected(bool selected)
        {
            if (isSelected == selected) return;
            isSelected = selected;
            if (selectionCoroutine != null)
            {
                StopCoroutine(selectionCoroutine);
                selectionCoroutine = null;
            }

            // animate scale and raise
            selectionCoroutine = StartCoroutine(SelectionCoroutine(selected));
        }

        private System.Collections.IEnumerator SelectionCoroutine(bool select)
        {
            float elapsed = 0f;
            var startScale = transform.localScale;
            var targetScale = select ? Vector3.one * 1.12f : Vector3.one;
            var startPos = transform.position;
            var targetPos = select ? originalPosition + Vector3.up * raiseAmount : originalPosition;
            if (spriteRenderer != null)
            {
                if (select)
                    spriteRenderer.sortingOrder = originalSortingOrder + selectionSortingBoost;
                else
                    spriteRenderer.sortingOrder = originalSortingOrder;
            }

            while (elapsed < selectionAnimDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / selectionAnimDuration);
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            transform.localScale = targetScale;
            transform.position = targetPos;
            selectionCoroutine = null;
        }

        /// <summary>
        /// Creates or returns a cached placeholder sprite so cards are visible even when
        /// no art is assigned in the prefab. The placeholder is a 32x48 white texture.
        /// </summary>
        private static Sprite placeholderSprite;
        private Sprite GetPlaceholderSprite()
        {
            if (placeholderSprite != null)
                return placeholderSprite;

            var tex = new Texture2D(32, 48, TextureFormat.ARGB32, false);
            var cols = new Color32[32 * 48];
            for (int i = 0; i < cols.Length; i++) cols[i] = new Color32(200, 200, 200, 255);
            tex.SetPixels32(cols);
            tex.Apply();

            placeholderSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return placeholderSprite;
        }

        /// <summary>
        /// Sets the position of this card view.
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        /// <summary>
        /// Destroys this card view game object.
        /// </summary>
        public void DestroyView()
        {
#if UNITY_EDITOR
            EnsureDeselected(gameObject);
#endif
            // Use DestroyImmediate in edit mode to avoid Editor holding null targets
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

#if UNITY_EDITOR
        private static void EnsureDeselected(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            bool selectionChanged = false;

            if (Selection.activeObject != null)
            {
                if (Selection.activeObject == target)
                {
                    Selection.activeObject = null;
                    selectionChanged = true;
                }
                else if (Selection.activeObject is Component activeComponent && activeComponent != null && activeComponent.gameObject == target)
                {
                    Selection.activeObject = null;
                    selectionChanged = true;
                }
            }

            var currentSelection = Selection.objects;
            if (currentSelection == null || currentSelection.Length == 0)
            {
                return;
            }

            var trimmed = new System.Collections.Generic.List<UnityEngine.Object>(currentSelection.Length);
            foreach (var obj in currentSelection)
            {
                if (obj == null)
                {
                    selectionChanged = true;
                    continue;
                }

                if (obj == target)
                {
                    selectionChanged = true;
                    continue;
                }

                if (obj is Component component && component != null && component.gameObject == target)
                {
                    selectionChanged = true;
                    continue;
                }

                trimmed.Add(obj);
            }

            if (selectionChanged)
            {
                Selection.objects = trimmed.ToArray();
            }
        }
#endif
        private void EnsureMarkerRenderer()
        {
            if (markerRenderer != null) return;
            var child = new GameObject("CardMarkerOverlay");
            child.transform.SetParent(transform, false);
            child.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.DontUnloadUnusedAsset;
            child.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            markerRenderer = child.AddComponent<SpriteRenderer>();
            markerRenderer.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Default";
            markerRenderer.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 0) + 5;
            markerRenderer.color = Color.white;
            markerRenderer.enabled = false;
        }

        private void SetMarkerVisible(bool visible)
        {
            if (markerRenderer != null)
            {
                markerRenderer.enabled = visible && markerRenderer.sprite != null;
            }
        }

        // Show a temporary face sprite and optional marker until cleared.
        // The temp sprite is displayed immediately and hidden only on hover (OnMouseEnter).
        public void ShowTemporaryValue(Sprite tempSprite, Sprite marker)
        {
            if (spriteRenderer == null) return;
            if (tempSprite == null) return;
            
            // Cache the temporary sprite for display
            temporaryFaceSprite = tempSprite;
            
            // NOTE: DO NOT overwrite originalFaceSprite here!
            // It was already set in Initialize() to preserve the real card sprite (7 di Coppe)
            // If we overwrote it here, we would lose the reference to the original sprite
            
            // IMPORTANT: If mouse is currently over the card, don't change the sprite!
            // The user is viewing the original 7 di Coppe and we don't want to interrupt that.
            if (isMouseOver)
            {
                // Just update the marker sprite, but keep it hidden
                EnsureMarkerRenderer();
                markerRenderer.sprite = marker;
                SetMarkerVisible(false);
                // Keep showingTemporaryValue = false so OnMouseExit will restore the temp sprite
                return;
            }
            
            // Show the temporary sprite immediately (not the original 7 di Coppe)
            spriteRenderer.sprite = tempSprite;
            showingTemporaryValue = true;
            
            // Set marker and show it
            EnsureMarkerRenderer();
            markerRenderer.sprite = marker;
            SetMarkerVisible(marker != null);
        }

        // Clear only the visual overlay (used on hover). Keeps the temp sprite cached for restore on exit.
        public void ClearTemporaryOverlay()
        {
            showingTemporaryValue = false;
            if (spriteRenderer != null && originalFaceSprite != null)
            {
                spriteRenderer.sprite = originalFaceSprite;
            }
            SetMarkerVisible(false);
        }

        // Permanently clear any temporary value and cache.
        public void ClearTemporaryValue()
        {
            showingTemporaryValue = false;
            temporaryFaceSprite = null;
            if (spriteRenderer != null && originalFaceSprite != null)
            {
                spriteRenderer.sprite = originalFaceSprite;
            }
            SetMarkerVisible(false);
        }

        /// <summary>
        /// Flips this card to face-up, showing the real card sprite.
        /// Used when bot cards are played on the table.
        /// </summary>
        public void FlipToFaceUp(Sprite faceSprite)
        {
            if (spriteRenderer == null) return;
            if (faceSprite == null) return;
            
            // Update to face-up sprite
            spriteRenderer.sprite = faceSprite;
            originalFaceSprite = faceSprite;
        }

        /// <summary>
        /// Plays a bounce animation to indicate this card can be captured.
        /// Used when player makes an invalid selection to suggest valid captures.
        /// </summary>
        /// <param name="delay">Delay before starting the animation (for sequential hints)</param>
        /// <param name="bounceCount">Number of bounces</param>
        public void PlayHintBounce(float delay = 0f, int bounceCount = 2)
        {
            if (hintBounceCoroutine != null)
            {
                StopCoroutine(hintBounceCoroutine);
            }
            hintBounceCoroutine = StartCoroutine(HintBounceCoroutine(delay, bounceCount));
        }

        /// <summary>
        /// Stops any active hint bounce animation.
        /// </summary>
        public void StopHintBounce()
        {
            if (hintBounceCoroutine != null)
            {
                StopCoroutine(hintBounceCoroutine);
                hintBounceCoroutine = null;
            }
            // Reset to original position if not selected
            if (!isSelected)
            {
                transform.position = originalPosition;
                transform.localScale = Vector3.one;
            }
        }

        private Coroutine hintBounceCoroutine;

        private System.Collections.IEnumerator HintBounceCoroutine(float delay, int bounceCount)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            float bounceHeight = 0.15f;
            float bounceDuration = 0.15f;
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;

            for (int i = 0; i < bounceCount; i++)
            {
                // Bounce up
                float elapsed = 0f;
                while (elapsed < bounceDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / bounceDuration;
                    float easeOut = 1f - (1f - t) * (1f - t);
                    transform.position = startPos + Vector3.up * bounceHeight * easeOut;
                    transform.localScale = Vector3.Lerp(startScale, startScale * 1.1f, easeOut);
                    yield return null;
                }

                // Bounce down
                elapsed = 0f;
                while (elapsed < bounceDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / bounceDuration;
                    float easeIn = t * t;
                    transform.position = startPos + Vector3.up * bounceHeight * (1f - easeIn);
                    transform.localScale = Vector3.Lerp(startScale * 1.1f, startScale, easeIn);
                    yield return null;
                }

                transform.position = startPos;
                transform.localScale = startScale;

                // Small pause between bounces
                if (i < bounceCount - 1)
                {
                    yield return new WaitForSeconds(0.05f);
                }
            }

            hintBounceCoroutine = null;
        }
    }
}
