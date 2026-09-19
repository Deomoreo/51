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
        [SerializeField] private float hoverRaiseAmount = 0.12f;
        [SerializeField] private float hoverScaleMultiplier = 1.08f;
        [SerializeField] private float hoverAnimDuration = 0.1f;
        [SerializeField] private int selectionSortingBoost = 100;
        [SerializeField] private float selectionAnimDuration = 0.12f;

        private bool isSelected = false;
        private Vector3 originalPosition;
        private Vector3 displayScale = Vector3.one;
        private int originalSortingOrder = 0;
        private Coroutine selectionCoroutine;
        private Coroutine hoverCoroutine;

        public Card Card => card;

        /// <summary>
        /// Renderer visivo della carta, risolto anche quando si trova in un figlio del prefab.
        /// </summary>
        public SpriteRenderer CardRenderer
        {
            get
            {
                if (spriteRenderer == null)
                {
                    spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
                }

                return spriteRenderer;
            }
        }

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
        /// True se la carta � attualmente scoperta (face-up), false se mostra il dorso.
        /// </summary>
        public bool IsFaceUp
        {
            get
            {
                if (spriteRenderer == null) return false;
                // Considera la carta scoperta se la sprite NON � il dorso
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

        /// <summary>
        /// Imposta la scala visiva di riposo della carta.
        /// Hover, selezione e hint vengono calcolati a partire da questa scala.
        /// </summary>
        public void SetDisplayScale(float scale)
        {
            float clampedScale = Mathf.Max(0.01f, scale);
            Vector3 newDisplayScale = new Vector3(clampedScale, clampedScale, 1f);
            if ((displayScale - newDisplayScale).sqrMagnitude <= 0.000001f)
            {
                return;
            }

            displayScale = newDisplayScale;

            if (!isSelected && !isDragging)
            {
                if (isMouseOver && enableHover)
                {
                    AnimateHover(true);
                }
                else
                {
                    transform.localScale = displayScale;
                }
            }
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

        /// <summary>
        /// Difensivo: OnMouseExit di Unity puo' non scattare in alcuni casi limite (focus perso,
        /// GameObject riusato/nascosto mentre il mouse era sopra, frame saltati), lasciando la
        /// carta bloccata sollevata/selezionata visivamente o il valore temporaneo del Matta
        /// (7 di Coppe) non ripristinato. Ogni frame in cui isMouseOver e' vero verifichiamo che
        /// il mouse sia REALMENTE ancora sopra il collider e, se non lo e' piu', forziamo la
        /// stessa pulizia di OnMouseExit.
        /// </summary>
        private void Update()
        {
            if (isMouseOver && !IsPointerActuallyOverCollider())
            {
                OnMouseExit();
            }
        }

        private bool IsPointerActuallyOverCollider()
        {
            var col = GetComponent<Collider2D>();
            if (col == null) return true; // niente da verificare, non forzare un'uscita errata

            var cam = Camera.main;
            if (cam == null) return true;

            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
            Vector3 worldPos = cam.ScreenToWorldPoint(mouseScreenPos);
            return col.OverlapPoint(new Vector2(worldPos.x, worldPos.y));
        }

        private void OnDisable()
        {
            // Stessa difesa di Update/IsPointerActuallyOverCollider ma per il caso in cui la
            // carta venga disattivata (nascosta, distrutta, riusata) mentre il mouse era ancora
            // sopra: in quel caso OnMouseExit non scatta affatto.
            if (isMouseOver)
            {
                isMouseOver = false;
                if (!showingTemporaryValue && temporaryFaceSprite != null && spriteRenderer != null)
                {
                    spriteRenderer.sprite = temporaryFaceSprite;
                    showingTemporaryValue = true;
                    SetMarkerVisible(markerRenderer != null && markerRenderer.sprite != null);
                }

                if (!isSelected)
                {
                    if (hoverCoroutine != null)
                    {
                        StopCoroutine(hoverCoroutine);
                        hoverCoroutine = null;
                    }
                    transform.localScale = displayScale;
                    transform.position = originalPosition;
                }
            }
        }

        private void OnMouseEnter()
        {
            isMouseOver = true;
            
            // Simple hover effect only when enabled: slightly scale up
            if (enableHover)
            {
                // if selected, keep selection animation; otherwise scale smoothly
                if (!isSelected)
                    AnimateHover(true);

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
                    AnimateHover(false);

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
                transform.localScale = displayScale;
                // notify listeners about drag release only if dragging allowed
                if (allowDrag)
                {
                    OnDragReleased?.Invoke(this, transform.position);
                }
            }
        }

        private TurnController _turnControllerCache;

        /// <summary>
        /// True solo se e' specificamente il turno del client locale (non solo "di un umano
        /// qualsiasi"). Prima usava reflection su "Project51.Unity.GameManager, Project51.Networking",
        /// un assembly che non esiste piu': tornava sempre null -> fallback "return true" sempre,
        /// per chiunque, in qualunque momento - qualsiasi client poteva trascinare/giocare carte
        /// fuori dal proprio turno, desincronizzando la partita in multiplayer.
        /// </summary>
        private bool IsLocalPlayersTurn()
        {
            if (_turnControllerCache == null)
                _turnControllerCache = FindObjectOfType<TurnController>();

            if (_turnControllerCache == null || _turnControllerCache.CurrentPlayerIndex < 0)
                return true; // fallback: nessun TurnController trovato ancora (es. scena in caricamento)

            return _turnControllerCache.IsHumanPlayerTurn
                && GameModeService.Current.IsLocalPlayer(_turnControllerCache.CurrentPlayerIndex);
        }

        private float lastClickTime = 0f;
        private const float doubleClickThreshold = 0.35f;

        // Temporary value overlay support (for Matta during accusi)
        private Sprite originalFaceSprite;
        private Sprite temporaryFaceSprite;
        private bool showingTemporaryValue = false;
        private bool isMouseOver = false; // Track if mouse is currently over this card
        private SpriteRenderer markerRenderer; // small overlay marker

        /// <summary>
        /// Rimette la carta esattamente nella posa di riposo decisa dal layout (dopo animazioni
        /// "decorative" come il salto del pugno, che non devono spostare le carte).
        /// </summary>
        public void SnapToRestPose()
        {
            if (isSelected || isDragging) return;
            transform.position = originalPosition;
            transform.localScale = displayScale;
        }

        /// <summary>
        /// Ordine di disegno a riposo, deciso dal layout (es. carta centrale del ventaglio davanti).
        /// Prima tutte le carte avevano ordine 0 e la sovrapposizione era casuale.
        /// </summary>
        public void SetBaseSortingOrder(int order)
        {
            if (originalSortingOrder == order) return;
            originalSortingOrder = order;
            // Oltre 400 la carta e' in volo (CardAnimationController): l'ordine a riposo verra'
            // ripristinato a fine animazione, non va abbassata a meta' volo.
            if (spriteRenderer != null && spriteRenderer.sortingOrder < 400)
            {
                spriteRenderer.sortingOrder = isSelected ? order + selectionSortingBoost : order;
            }
        }

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
            var targetScale = select ? displayScale * 1.12f : displayScale;
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
            if ((originalPosition - position).sqrMagnitude <= 0.000001f)
            {
                return;
            }

            originalPosition = position;
            if (!isSelected && !isDragging)
            {
                if (isMouseOver && enableHover)
                {
                    AnimateHover(true);
                }
                else
                {
                    transform.position = position;
                }
            }
        }

        private void AnimateHover(bool enter)
        {
            if (hoverCoroutine != null)
            {
                StopCoroutine(hoverCoroutine);
            }

            hoverCoroutine = StartCoroutine(HoverCoroutine(enter));
        }

        private System.Collections.IEnumerator HoverCoroutine(bool enter)
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 startPosition = transform.position;
            Vector3 targetScale = enter ? displayScale * hoverScaleMultiplier : displayScale;
            Vector3 targetPosition = enter ? originalPosition + Vector3.up * hoverRaiseAmount : originalPosition;

            while (elapsed < hoverAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / hoverAnimDuration));
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            transform.localScale = targetScale;
            transform.position = targetPosition;
            hoverCoroutine = null;
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

        // ==== Matta (7 di coppe) usata come jolly per l'accuso ====
        private SpriteRenderer mattaHalo;
        private Sprite shownMattaTarget;
        private Coroutine mattaFlip;
        private float mattaHaloBurst;

        /// <summary>
        /// La matta diventa targetSprite per l'accuso: la carta si gira e cambia faccia, dietro resta un
        /// alone dorato che pulsa. Passandoci sopra si vede ancora il 7 di coppe vero.
        /// Richiamabile a ogni refresh: l'animazione parte solo quando il valore cambia.
        /// </summary>
        public void ShowMattaTransform(Sprite targetSprite, Sprite haloSprite)
        {
            if (targetSprite == null || CardRenderer == null) return;
            EnsureMattaHalo(haloSprite);
            if (shownMattaTarget == targetSprite) return;
            shownMattaTarget = targetSprite;

            if (mattaFlip != null) StopCoroutine(mattaFlip);
            if (gameObject.activeInHierarchy)
            {
                mattaFlip = StartCoroutine(MattaFlipRoutine(targetSprite));
            }
            else
            {
                ShowTemporaryValue(targetSprite, null);
            }
        }

        public void ClearMattaTransform()
        {
            if (shownMattaTarget == null && (mattaHalo == null || !mattaHalo.gameObject.activeSelf)) return;
            shownMattaTarget = null;
            if (mattaFlip != null)
            {
                StopCoroutine(mattaFlip);
                mattaFlip = null;
                SetFlipFactor(1f);
            }
            if (mattaHalo != null) mattaHalo.gameObject.SetActive(false);
            ClearTemporaryValue();
        }

        private System.Collections.IEnumerator MattaFlipRoutine(Sprite target)
        {
            const float half = 0.16f;
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                SetFlipFactor(1f - t / half);
                yield return null;
            }

            SetFlipFactor(0f);
            ShowTemporaryValue(target, null);
            mattaHaloBurst = 1f;

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                SetFlipFactor(t / half);
                yield return null;
            }

            SetFlipFactor(1f);
            mattaFlip = null;
        }

        /// <summary>Larghezza apparente della carta durante il giro (1 = normale, 0 = di taglio).</summary>
        private void SetFlipFactor(float factor)
        {
            var target = CardRenderer.transform;
            if (target == transform)
            {
                var scale = transform.localScale;
                float restX = scale.y * (displayScale.x / Mathf.Max(0.0001f, displayScale.y));
                transform.localScale = new Vector3(restX * Mathf.Clamp01(factor), scale.y, scale.z);
            }
            else
            {
                var scale = target.localScale;
                target.localScale = new Vector3(Mathf.Abs(scale.y) * Mathf.Clamp01(factor), scale.y, scale.z);
            }
        }

        private void EnsureMattaHalo(Sprite haloSprite)
        {
            if (haloSprite == null || CardRenderer == null) return;
            if (mattaHalo == null)
            {
                var child = new GameObject("MattaHalo");
                child.transform.SetParent(CardRenderer.transform, false);
                child.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                mattaHalo = child.AddComponent<SpriteRenderer>();
                mattaHalo.sortingLayerID = CardRenderer.sortingLayerID;
            }

            mattaHalo.sprite = haloSprite;
            var cardSize = CardRenderer.sprite != null ? CardRenderer.sprite.bounds.size : Vector3.one;
            var haloSize = haloSprite.bounds.size;
            mattaHalo.transform.localScale = new Vector3(cardSize.x * 2f / haloSize.x, cardSize.y * 1.7f / haloSize.y, 1f);
            mattaHalo.gameObject.SetActive(true);
        }

        // ==== Suggerimenti mosse: bagliore dietro alle carte in mano che fanno una presa ====
        private SpriteRenderer moveHintGlow;
        private Color moveHintColor = MoveHintColor;
        private static readonly Color MoveHintColor = new Color(0.45f, 0.92f, 1f);

        /// <summary>Impostazioni in partita, "Suggerimenti mosse". Senza sprite il bagliore non compare.</summary>
        public void SetMoveHint(bool on, Sprite glowSprite) => SetGlow(on, glowSprite, MoveHintColor);

        /// <summary>Alone colorato dietro alla carta (suggerimenti, accuso del mazziere).</summary>
        public void SetGlow(bool on, Sprite glowSprite, Color color)
        {
            moveHintColor = color;
            if (!on || glowSprite == null || CardRenderer == null)
            {
                if (moveHintGlow != null) moveHintGlow.gameObject.SetActive(false);
                return;
            }

            if (moveHintGlow == null)
            {
                var child = new GameObject("MoveHintGlow");
                child.transform.SetParent(CardRenderer.transform, false);
                child.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                moveHintGlow = child.AddComponent<SpriteRenderer>();
                moveHintGlow.sortingLayerID = CardRenderer.sortingLayerID;
            }

            moveHintGlow.sprite = glowSprite;
            var cardSize = CardRenderer.sprite != null ? CardRenderer.sprite.bounds.size : Vector3.one;
            var glowSize = glowSprite.bounds.size;
            moveHintGlow.transform.localScale = new Vector3(cardSize.x * 2.1f / glowSize.x, cardSize.y * 1.6f / glowSize.y, 1f);
            moveHintGlow.gameObject.SetActive(true);
        }

        public bool HasMoveHint => moveHintGlow != null && moveHintGlow.gameObject.activeSelf;

        private void LateUpdate()
        {
            bool haloOn = mattaHalo != null && mattaHalo.gameObject.activeSelf;
            if (moveHintGlow != null && moveHintGlow.gameObject.activeSelf)
            {
                // La matta trasformata ha gia' il suo alone dorato.
                moveHintGlow.enabled = !haloOn && CardRenderer != null && CardRenderer.enabled;
                // Sotto a tutte le carte della mano (ordini 40+), sopra al tavolo: il bagliore della carta
                // centrale non copre le carte ai lati.
                moveHintGlow.sortingOrder = CardRenderer.sortingOrder - 10;
                float glow = 0.7f + 0.3f * (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
                moveHintGlow.color = new Color(moveHintColor.r, moveHintColor.g, moveHintColor.b, glow);
            }

            if (!haloOn) return;
            mattaHalo.enabled = CardRenderer != null && CardRenderer.enabled;
            mattaHalo.sortingOrder = CardRenderer.sortingOrder - 1;
            mattaHaloBurst = Mathf.MoveTowards(mattaHaloBurst, 0f, Time.deltaTime * 2.5f);
            float pulse = 0.6f + 0.25f * (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
            mattaHalo.color = new Color(1f, 0.8f, 0.35f, Mathf.Clamp01(pulse + mattaHaloBurst));
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
        /// Rigira la carta a faccia in giu'. Serve quando una vista gia' scoperta (mia mano o tavolo)
        /// viene riusata per la mano di un avversario nella smazzata successiva: senza questo, le sue
        /// carte restavano visibili.
        /// </summary>
        public void FlipToFaceDown()
        {
            if (spriteRenderer == null || defaultCardBack == null) return;
            ClearTemporaryValue();
            spriteRenderer.sprite = defaultCardBack;
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
                transform.localScale = displayScale;
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
