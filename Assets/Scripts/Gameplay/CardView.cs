using UnityEngine;
using DG.Tweening;
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
        private float raiseOverride = -1f;
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
        private CardShaderEffect shaderEffect;
        private CardDropShadow dropShadow;
        private float flipFactor = 1f;

        private CardShaderEffect SurfaceEffect
        {
            get
            {
                if (shaderEffect == null)
                    shaderEffect = GetComponent<CardShaderEffect>() ?? gameObject.AddComponent<CardShaderEffect>();
                shaderEffect.Bind(CardRenderer);
                return shaderEffect;
            }
        }

        /// <summary>Explicit deck decoration hook; current decks have no rarity metadata.</summary>
        public void SetHolographic(bool enabled) => SurfaceEffect.SetHolographic(enabled);

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

        /// <summary>Tocco su una carta che non si gioca (carte accusate di un altro): lo decide chi la mette in scena.</summary>
        public Action<CardView> Tapped;

        /// <summary>
        /// Initializes this view with a specific card.
        /// </summary>
        public void Initialize(Card card, Sprite cardSprite = null, bool faceUp = true)
        {
            ClearMattaTransform();
            ClearTemporaryValue();
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
            if (dropShadow == null)
                dropShadow = GetComponent<CardDropShadow>() ?? gameObject.AddComponent<CardDropShadow>();
            dropShadow.Bind(spriteRenderer);
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

            if (!isSelected)
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
                // OnMouseDown ignora la UI: un tocco sul pannello "scegli la presa" (sopra la mano)
                // giocava anche la carta sotto, inviando una seconda mossa diversa da quella scelta.
                if (IsPointerOverUI()) return;
                GameFeedback.TryHaptic(false);

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
            else if (Tapped != null && !IsPointerOverUI())
            {
                GameFeedback.TryHaptic(false);
                Tapped(this);
            }
        }

        /// <summary>
        /// Difensivo: OnMouseExit di Unity puo' non scattare in alcuni casi limite (focus perso,
        /// GameObject riusato/nascosto mentre il mouse era sopra, frame saltati), lasciando la
        /// carta bloccata sollevata/selezionata visivamente o il valore temporaneo del Matta
        /// (7 di Coppe) non ripristinato. Ogni frame in cui isMouseOver e' vero verifichiamo che
        /// il mouse sia REALMENTE ancora sopra il collider e, se non lo e' piu', forziamo la
        /// stessa pulizia di OnMouseExit.
        /// Isteresi: l'hover solleva e ingrandisce la carta, e col suo collider la allontana dal puntatore. Con il
        /// puntatore sul bordo basso la carta usciva, riscendeva, rientrava... e pulsava "grande piccola" senza fine
        /// (utente, 01/10: la carta centrale a inizio smazzata, dove era rimasto l'ultimo tocco). Ora esce solo
        /// quando il puntatore non e' piu' ne' sulla carta ne' sul suo posto a riposo.
        /// </summary>
        private void Update()
        {
            PointerMoved();
            if (isMouseOver && !PointerStillOver())
            {
                EndHover();
            }
        }

        // Una sola carta sollevata dall'hover alla volta (con l'isteresi due carte sovrapposte potevano esserlo entrambe).
        private static CardView s_Hovered;

        /// <summary>
        /// Sui telefoni Input.mousePosition resta dove e' finito l'ultimo tocco: senza dito sullo schermo non c'e'
        /// hover, altrimenti restava sollevata la carta capitata li' sotto.
        /// </summary>
        private static bool PointerLive => Input.touchCount > 0 || !Input.touchSupported;

        private bool PointerStillOver() => PointerLive && (IsPointerActuallyOverCollider() || IsPointerOverRestPose());

        // Puntatore mosso in questo fotogramma (calcolato una volta per fotogramma, dal primo Update o OnMouseOver).
        private static Vector3 s_PrevPointer;
        private static int s_PointerFrame = -1;
        private static bool s_PointerMoved;

        /// <summary>
        /// Solleva solo un puntatore che si muove: una carta che scivola sotto un puntatore fermo (col mouse, la vicina che
        /// prende il posto di quella appena giocata; a inizio smazzata, la carta che arriva dove era l'ultimo tocco) resta giu'.
        /// Utente 01/10: "la carta che si alza quando viene giocata una carta". Il dito che tocca sposta il puntatore: si solleva.
        /// </summary>
        private static bool PointerMoved()
        {
            if (Time.frameCount != s_PointerFrame)
            {
                s_PointerFrame = Time.frameCount;
                Vector3 p = Input.mousePosition;
                s_PointerMoved = (p - s_PrevPointer).sqrMagnitude > 1f;
                s_PrevPointer = p;
            }
            return s_PointerMoved;
        }

        /// <summary>Il puntatore e' sul posto a riposo della carta (posizione e scala del layout), ovunque l'abbia portata l'hover.</summary>
        private bool IsPointerOverRestPose()
        {
            var box = GetComponent<BoxCollider2D>();
            var cam = Camera.main;
            if (box == null || cam == null) return false;
            Vector3 screen = Input.mousePosition;
            if (float.IsNaN(screen.x) || float.IsInfinity(screen.x) || float.IsNaN(screen.y) || float.IsInfinity(screen.y)) return false;
            screen.z = Mathf.Abs(cam.transform.position.z - originalPosition.z);
            return BoxContains(cam.ScreenToWorldPoint(screen), originalPosition, transform.rotation, displayScale, box.offset, box.size);
        }

        /// <summary>Il punto (mondo) cade nel BoxCollider2D di una carta messa in questa posa.</summary>
        public static bool BoxContains(Vector3 world, Vector3 position, Quaternion rotation, Vector3 scale, Vector2 offset, Vector2 size)
        {
            Vector3 local = Quaternion.Inverse(rotation) * (world - position);
            Vector2 d = new Vector2(local.x / scale.x, local.y / scale.y) - offset;
            return Mathf.Abs(d.x) <= size.x * 0.5f && Mathf.Abs(d.y) <= size.y * 0.5f;
        }

        private bool IsPointerActuallyOverCollider()
        {
            var col = GetComponent<Collider2D>();
            if (col == null) return true; // niente da verificare, non forzare un'uscita errata

            var cam = Camera.main;
            if (cam == null) return true;

            Vector3 mouseScreenPos = Input.mousePosition;
            if (float.IsNaN(mouseScreenPos.x) || float.IsInfinity(mouseScreenPos.x) ||
                float.IsNaN(mouseScreenPos.y) || float.IsInfinity(mouseScreenPos.y)) return false;
            mouseScreenPos.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
            Vector3 worldPos = cam.ScreenToWorldPoint(mouseScreenPos);
            return col.OverlapPoint(new Vector2(worldPos.x, worldPos.y));
        }

        private void OnDisable()
        {
            // Unity stops object coroutines on pooling: settle the current face and reset width/material.
            if (mattaFlip != null && shownMattaTarget != null)
                ShowTemporaryValue(shownMattaTarget, null);
            CancelMattaAnimation();
            // Stessa difesa di Update/IsPointerActuallyOverCollider ma per il caso in cui la
            // carta venga disattivata (nascosta, distrutta, riusata) mentre il mouse era ancora
            // sopra: in quel caso OnMouseExit non scatta affatto.
            if (s_Hovered == this) s_Hovered = null;
            if (isMouseOver)
            {
                isMouseOver = false;
                if (!showingTemporaryValue && temporaryFaceSprite != null && spriteRenderer != null)
                {
                    spriteRenderer.sprite = temporaryFaceSprite;
                    showingTemporaryValue = true;
                    SetMarkerVisible(markerRenderer != null && markerRenderer.sprite != null);
                }

            }
            StopPoseAnimations();
            isSelected = false;
            transform.localScale = displayScale;
            transform.position = originalPosition;
            if (spriteRenderer != null) spriteRenderer.sortingOrder = originalSortingOrder;
            if (dropShadow != null) dropShadow.SetElevation(0f);
        }

        private void OnMouseEnter() => TryBeginHover();

        // La carta e' sotto al puntatore ma non sollevata (ci e' scivolata sotto): si solleva appena il puntatore si muove.
        private void OnMouseOver()
        {
            if (!isMouseOver) TryBeginHover();
        }

        private void TryBeginHover()
        {
            if (!PointerLive || (isMouseOver && s_Hovered == this)) return; // gia' sollevata: niente seconda scia
            if (!PointerMoved()) return;
            if (s_Hovered != null && s_Hovered != this) s_Hovered.EndHover();
            s_Hovered = this;
            isMouseOver = true;

            // Simple hover effect only when enabled: slightly scale up
            if (enableHover)
            {
                SurfaceEffect.PlaySweep();
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
            // Il collider e' salito con la carta: finche' il puntatore e' sul suo posto a riposo resta sollevata (vedi Update).
            if (isMouseOver && PointerStillOver()) return;
            EndHover();
        }

        private void EndHover()
        {
            if (s_Hovered == this) s_Hovered = null;
            bool wasOver = isMouseOver;
            isMouseOver = false;
            // Mai sollevata (ingresso ignorato senza dito): nessuna posa da rimettere, e una carta in volo non va tirata a riposo.
            if (!wasOver) return;

            if (enableHover)
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

        private static readonly System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> s_UiHits =
            new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();

        private static bool IsPointerOverUI()
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null) return false;
            // Raycast nuovo nel punto del tocco: sul telefono i dati del dito arrivano all'EventSystem solo dopo OnMouseDown,
            // e un tocco su un velo (visore delle scope, scelta della presa) giocava la carta sotto. Nell'Editor era gia' cosi'.
            eventSystem.RaycastAll(new UnityEngine.EventSystems.PointerEventData(eventSystem) { position = Input.mousePosition }, s_UiHits);
            foreach (var hit in s_UiHits)
                if (hit.module is UnityEngine.UI.GraphicRaycaster) return true;
            return false;
        }

        private TurnController _turnControllerCache;

        /// <summary>
        /// True solo se e' specificamente il turno del client locale (non solo "di un umano
        /// qualsiasi"), letto da GameModeService.
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
            if (isSelected) return;
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
            StopPoseAnimations();
            isSelected = selected;
            if (selected) SurfaceEffect.PlaySweep();
            if (selectionCoroutine != null)
            {
                StopCoroutine(selectionCoroutine);
                selectionCoroutine = null;
            }

            // animate scale and raise
            selectionCoroutine = StartCoroutine(SelectionCoroutine(selected));
        }

        private float Raise => raiseOverride >= 0f ? raiseOverride : raiseAmount;

        /// <summary>Quanto sale da selezionata, in unita' mondo; negativo = quello di sempre (mano). Tavolo 1v1: 4 del mockup.</summary>
        public void SetRaiseOverride(float worldLift) => raiseOverride = worldLift;

        private System.Collections.IEnumerator SelectionCoroutine(bool select)
        {
            float elapsed = 0f;
            var startScale = GetPoseScale();
            var targetScale = select ? displayScale * 1.12f : displayScale;
            var startPos = transform.position;
            var targetPos = select ? originalPosition + Vector3.up * Raise : originalPosition;
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
                var t = PoseEase(Mathf.Clamp01(elapsed / selectionAnimDuration), select);
                transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
                transform.position = Vector3.LerpUnclamped(startPos, targetPos, t);
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
            if (!isSelected)
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

        /// <summary>
        /// Nuova posa di riposo raggiunta scivolando invece che di scatto (il tavolo che si riordina mentre la carta giocata
        /// vola). Il refresh successivo, con gli stessi valori, non la interrompe: SetPosition e SetDisplayScale escono subito.
        /// </summary>
        public void GlideTo(Vector3 position, float scale, float duration)
        {
            originalPosition = position;
            displayScale = new Vector3(Mathf.Max(0.01f, scale), Mathf.Max(0.01f, scale), 1f);
            if (isSelected) return;
            if (isMouseOver && enableHover) { AnimateHover(true); return; }
            StopPoseAnimations();
            transform.DOKill();
            transform.DOMove(position, duration).SetEase(Ease.OutCubic).SetLink(gameObject);
            transform.DOScale(displayScale, duration).SetEase(Ease.OutCubic).SetLink(gameObject);
        }

        private void AnimateHover(bool enter)
        {
            if (isSelected) return;
            if (hoverCoroutine != null)
            {
                StopCoroutine(hoverCoroutine);
            }

            hoverCoroutine = StartCoroutine(HoverCoroutine(enter));
        }

        private System.Collections.IEnumerator HoverCoroutine(bool enter)
        {
            float elapsed = 0f;
            Vector3 startScale = GetPoseScale();
            Vector3 startPosition = transform.position;
            Vector3 targetScale = enter ? displayScale * hoverScaleMultiplier : displayScale;
            Vector3 targetPosition = enter ? originalPosition + Vector3.up * hoverRaiseAmount : originalPosition;

            while (elapsed < hoverAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = PoseEase(Mathf.Clamp01(elapsed / hoverAnimDuration), enter);
                transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
                transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, t);
                yield return null;
            }

            transform.localScale = targetScale;
            transform.position = targetPosition;
            hoverCoroutine = null;
        }

        private void StopPoseAnimations()
        {
            if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
            if (selectionCoroutine != null) StopCoroutine(selectionCoroutine);
            hoverCoroutine = null;
            selectionCoroutine = null;
        }

        private Vector3 GetPoseScale()
        {
            var scale = transform.localScale;
            if (mattaFlip != null && CardRenderer != null && CardRenderer.transform == transform)
                scale.x = scale.y * displayScale.x / Mathf.Max(.0001f, displayScale.y);
            return scale;
        }

        private static float PoseEase(float t, bool lift)
        {
            if (!lift) return 1f - (1f - t) * (1f - t);
            // Quick pickup with a restrained overshoot; final pose is always exact.
            float u = t - 1f;
            return 1f + 1.8f * u * u * u + .8f * u * u;
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

        // Permanently clear any temporary value and cache.
        public void ClearTemporaryValue()
        {
            CancelMattaAnimation();
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

            CancelMattaAnimation();
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
            CancelMattaAnimation();
            if (shownMattaTarget == null && (mattaHalo == null || !mattaHalo.gameObject.activeSelf)) return;
            shownMattaTarget = null;
            if (mattaHalo != null) mattaHalo.gameObject.SetActive(false);
            ClearTemporaryValue();
        }

        private void CancelMattaAnimation()
        {
            if (mattaFlip != null)
            {
                StopCoroutine(mattaFlip);
                mattaFlip = null;
                if (CardRenderer != null) SetFlipFactor(1f);
            }
            if (shaderEffect != null) shaderEffect.ResetEffects();
        }

        private System.Collections.IEnumerator MattaFlipRoutine(Sprite target)
        {
            const float half = 0.16f;
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                SetFlipFactor(1f - t / half);
                SurfaceEffect.SetDissolve(t / half);
                yield return null;
            }

            SetFlipFactor(0f);
            ShowTemporaryValue(target, null);
            mattaHaloBurst = 1f;

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                SetFlipFactor(t / half);
                SurfaceEffect.SetDissolve(1f - t / half);
                yield return null;
            }

            SetFlipFactor(1f);
            SurfaceEffect.SetDissolve(0f);
            SurfaceEffect.PlaySweep();
            mattaFlip = null;
        }

        /// <summary>Larghezza apparente della carta durante il giro (1 = normale, 0 = di taglio).</summary>
        private void SetFlipFactor(float factor)
        {
            flipFactor = Mathf.Clamp01(factor);
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

        // ==== Carte accusate: bordo pieno dietro alla carta (mockup Partita, Partita4) ====
        private SpriteRenderer outline;
        private static Sprite outlineSprite;
        private const int OutlinePixels = 64, OutlineCornerPixels = 16; // sprite 9-slice generato, 100 px per unita'

        /// <summary>Bordo attorno alla carta spesso width volte la sua larghezza (0 = spento), angoli come quelli della carta.</summary>
        public void SetOutline(Color color, float width)
        {
            if (width <= 0f || CardRenderer == null || CardRenderer.sprite == null)
            {
                if (outline != null) outline.gameObject.SetActive(false);
                return;
            }

            if (outline == null)
            {
                var child = new GameObject("Outline");
                child.transform.SetParent(CardRenderer.transform, false);
                child.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                outline = child.AddComponent<SpriteRenderer>();
                outline.drawMode = SpriteDrawMode.Sliced;
                outline.sprite = OutlineSprite();
            }

            var bounds = CardRenderer.sprite.bounds;
            float t = bounds.size.x * width;
            // Angoli della carta circa al 9% della larghezza, piu' lo spessore. Un filo dietro alla carta (z): stesso ordine,
            // cosi' sta sopra alla sua ombra (coda 2999) e sotto alla faccia.
            float s = (0.09f * bounds.size.x + t) / (OutlineCornerPixels / 100f);
            outline.transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, 0.01f);
            outline.transform.localScale = new Vector3(s, s, 1f);
            outline.size = new Vector2(bounds.size.x + 2f * t, bounds.size.y + 2f * t) / s;
            outline.color = color;
            outline.gameObject.SetActive(true);
        }

        private static Sprite OutlineSprite()
        {
            if (outlineSprite != null) return outlineSprite;
            const int n = OutlinePixels, r = OutlineCornerPixels;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // Rettangolo bianco ad angoli tondi di raggio r, bordo sfumato di un pixel.
                    float dx = Mathf.Max(r - x - 0.5f, x + 0.5f - (n - r), 0f), dy = Mathf.Max(r - y - 0.5f, y + 0.5f - (n - r), 0f);
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01(r + 0.5f - Mathf.Sqrt(dx * dx + dy * dy))));
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            outlineSprite = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            outlineSprite.hideFlags = HideFlags.DontSave;
            return outlineSprite;
        }

        private void LateUpdate()
        {
            // Pose coroutines run before LateUpdate: keep Matta's width independent of
            // hover/selection scale, regardless of the order those coroutines resumed.
            if (mattaFlip != null && CardRenderer != null) SetFlipFactor(flipFactor);
            if (dropShadow != null)
                dropShadow.SetElevation(Mathf.Clamp01((transform.position.y - originalPosition.y) /
                    Mathf.Max(.01f, Raise)));
            if (outline != null && outline.gameObject.activeSelf && CardRenderer != null)
            {
                outline.enabled = CardRenderer.enabled;
                outline.sortingLayerID = CardRenderer.sortingLayerID;
                outline.sortingOrder = CardRenderer.sortingOrder;
            }
            bool haloOn = mattaHalo != null && mattaHalo.gameObject.activeSelf;
            if (moveHintGlow != null && moveHintGlow.gameObject.activeSelf)
            {
                // La matta trasformata ha gia' il suo alone dorato.
                moveHintGlow.enabled = !haloOn && CardRenderer != null && CardRenderer.enabled;
                // Sotto a tutte le carte della mano (ordini 40+), sopra al tavolo: il bagliore della carta
                // centrale non copre le carte ai lati.
                moveHintGlow.sortingOrder = CardRenderer.sortingOrder - 10;
                float glow = GamePreferences.ReducedGraphics ? 0.85f : 0.7f + 0.3f * (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
                moveHintGlow.color = new Color(moveHintColor.r, moveHintColor.g, moveHintColor.b, glow * moveHintColor.a);
            }

            if (!haloOn) return;
            mattaHalo.enabled = CardRenderer != null && CardRenderer.enabled;
            mattaHalo.sortingOrder = CardRenderer.sortingOrder - 1;
            mattaHaloBurst = Mathf.MoveTowards(mattaHaloBurst, 0f, Time.deltaTime * 2.5f);
            float pulse = GamePreferences.ReducedGraphics ? 0.75f : 0.6f + 0.25f * (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
            mattaHalo.color = new Color(1f, 0.8f, 0.35f, GamePreferences.ReducedGraphics ? pulse : Mathf.Clamp01(pulse + mattaHaloBurst));
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
    }
}
