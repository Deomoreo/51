using UnityEngine;
using UnityEngine.UI;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Unity
{
    // Manages the visual representation of all cards in the game.
    public class CardViewManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurnController turnController;
        [SerializeField] private GameObject cardViewPrefab;
        
        /// <summary>
        /// Sets the card view prefab at runtime. Useful for tests.
        /// </summary>
        public void SetCardViewPrefab(GameObject prefab)
        {
            cardViewPrefab = prefab;
            // Set prefab at runtime
        }
        
        /// <summary>
        /// Sets the turn controller reference at runtime. Useful for tests.
        /// </summary>
        public void SetTurnController(TurnController controller)
        {
            turnController = controller;
            // Set controller at runtime
        }

        [Header("Layout Settings")]
        [SerializeField] private Transform tableCardContainer;
        [SerializeField] private Transform humanHandContainer;
        [SerializeField] private float cardSpacing = 1.5f;
        [SerializeField] private bool useFanLayout = true; // Layout carte a ventaglio
        [SerializeField] private float fanAngle = 15f; // Angolo massimo del ventaglio (gradi)
        [SerializeField] private float fanRadius = 0.3f; // Raggio del ventaglio (curvatura)

        [Header("Responsive Layout")]
        [SerializeField] private bool useResponsiveLayout = true;
        [SerializeField, Range(0.4f, 0.95f)] private float tableWidthUsage = 0.72f;
        [SerializeField, Range(0.25f, 0.8f)] private float handWidthUsage = 0.42f;
        [SerializeField, Range(0.25f, 0.8f)] private float sideHandHeightUsage = 0.42f;
        [SerializeField, Range(0f, 2f)] private float layoutScreenPadding = 0.75f;
        [SerializeField] private float minTableCardSpacing = 0.8f;
        [SerializeField] private float minHandCardSpacing = 0.75f;
        [SerializeField] private float minSideHandCardSpacing = 0.75f;
        [SerializeField, Range(0.35f, 0.49f)] private float localPlayerBottomOffsetRatio = 0.46f;
        [SerializeField, Range(0.2f, 0.48f)] private float topOpponentOffsetRatio = 0.4f;
        [SerializeField, Range(0.2f, 0.4f)] private float topOpponentOffsetRatioFourPlayers = 0.33f;
        [SerializeField, Range(0.2f, 0.48f)] private float sideOpponentOffsetRatio = 0.38f;
        [SerializeField, Range(0f, 0.1f)] private float opponentHandScreenDownOffsetRatio = 0.04f;
        [SerializeField, Range(0f, 0.15f)] private float sideOpponentAdditionalScreenDownOffsetRatio = 0.05f;
        [SerializeField, Range(0f, 0.1f)] private float capturedPileScreenDownOffsetRatio = 0.035f;

        [Header("Player Hierarchy")]
        [SerializeField, Range(1f, 2f)] private float localPlayerCardScale = 1.7f;
        [SerializeField, Range(0.5f, 1.4f)] private float opponentCardScale = 1.12f;
        [SerializeField, Range(0.5f, 1.4f)] private float tableCardScale = 1.1f;

        private const float MinimumLocalPlayerCardScale = 2.2f;
        private const float MinimumOpponentCardScale = 1.12f;
        private const float MinimumTableCardScale = 1.1f;

        private float EffectiveLocalPlayerCardScale => Mathf.Max(localPlayerCardScale, MinimumLocalPlayerCardScale);
        private float EffectiveOpponentCardScale => Mathf.Max(opponentCardScale, MinimumOpponentCardScale);
        private float EffectiveTableCardScale => Mathf.Max(tableCardScale, MinimumTableCardScale);

        public float GetTableCardScale() => EffectiveTableCardScale;

        [Header("Sprites (Optional)")]
        [SerializeField] private Sprite[] cardSprites; // shared with CardSpriteProvider
        [SerializeField] private Sprite defaultCardBack;
        [SerializeField] private bool enableSpriteDebug = false;
        [SerializeField] private CardSpriteMapping[] explicitMappings;
        [Header("Matta Special Sprites")]
        [SerializeField] private Sprite[] mattaSpecialSprites; // Special sprites for Matta transformations (Matta_1, Matta_2, etc.)
        [Header("UI")]
        [SerializeField] private MoveSelectionUI moveSelectionUI;
        [Header("Feedback")]
        [SerializeField] private AudioClip playSound;
        [SerializeField] private float playSoundVolume = 0.7f;

        private Dictionary<string, Sprite> spriteLookup = new Dictionary<string, Sprite>();
        private Dictionary<(Suit suit, int rank), Sprite> explicitMapCache = new Dictionary<(Suit, int), Sprite>();

        private Dictionary<Card, CardView> activeCardViews = new Dictionary<Card, CardView>();
        private Camera layoutCamera;

        /// <summary>
        /// Gets all currently active CardViews. Useful for tests.
        /// </summary>
        public IEnumerable<CardView> GetActiveCardViews()
        {
            return activeCardViews.Values.Where(v => v != null);
        }

        // Selection mode state: when human selects a card to play and chooses table cards
        private bool isSelecting = false;
        private Card selectionPlayedCard = null;
        private List<Card> selectionTableCards = new List<Card>();
        // Visual helpers for alternative highlighting
        private List<GameObject> currentMarkers = new List<GameObject>();
        private List<Card> currentlyHighlightedCards = new List<Card>();
        private bool helpShownForCurrentSelection = false;

        // Helper: expected order of suits in cardSprites should match this enum ordering
        private int SuitToIndex(Suit suit)
        {
            switch (suit)
            {
                case Suit.Denari: return 0;
                case Suit.Coppe: return 1;
                case Suit.Bastoni: return 2;
                case Suit.Spade: return 3;
                default: return 0;
            }
        }

        private void BuildExplicitMapCache()
        {
            explicitMapCache.Clear();
            if (explicitMappings == null) return;
            foreach (var m in explicitMappings)
            {
                if (m.Sprite == null) continue;
                var key = (m.Suit, m.Rank);
                explicitMapCache[key] = m.Sprite;
            }
        }

        private Sprite markerSpriteCache = null;
        private Sprite GetMarkerSprite()
        {
            if (markerSpriteCache != null) return markerSpriteCache;
            // create a 8x8 white texture sprite at runtime
            var tex = new Texture2D(8, 8);
            var cols = new Color[8 * 8];
            for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
            tex.SetPixels(cols);
            tex.Apply();
            markerSpriteCache = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            return markerSpriteCache;
        }
        /// <summary>
        /// Renders non-local players' hands around the table (face-down by default).
        /// Dealer 15/30 accuso (1-2 points) does NOT reveal cards; Cirulla/Decino do.
        /// </summary>
        private void RenderAIHandsDynamic(List<PlayerState> players, int localIndex)
        {
            for (int p = 0; p < players.Count; p++)
            {
                if (p == localIndex) continue;
                var hand = players[p].Hand;

                bool hasAccuso = players[p].AccusiPoints >= 3;
                bool faceUp = hasAccuso;

                for (int i = 0; i < hand.Count; i++)
                {
                    var card = hand[i];
                    if (!activeCardViews.ContainsKey(card))
                    {
                        var view = CreateCardView(card, faceUp: faceUp, clickable: false);
                        if (view != null)
                        {
                            activeCardViews[card] = view;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var cardView = activeCardViews[card];
                    cardView.SetDisplayScale(EffectiveOpponentCardScale);

                    // Difensivo: stesso motivo di RenderTableCards.
                    if (cardView.CardRenderer != null)
                    {
                        cardView.CardRenderer.enabled = true;
                    }

                    Vector3 position;
                    float baseRotation = 0f;

                    int numPlayers = players.Count;

                    // Layout 1v1: avversario in alto capovolto
                    if (numPlayers == 2)
                    {
                        // Player 1 (avversario) in alto, ruotato 180�
                        position = CalculateFanPosition(GetTopOpponentBasePosition(true), hand.Count, i, 180f, EffectiveOpponentCardScale);
                        baseRotation = 180f;
                    }
                    // Layout 4P: distribuzione classica (sinistra, alto, destra)
                    else if (p == ((localIndex + 1) % numPlayers))
                    {
                        position = CalculateFanPositionVertical(GetSideOpponentBasePosition(true), hand.Count, i, true, EffectiveOpponentCardScale);
                        baseRotation = 90f;
                    }
                    else if (p == ((localIndex + 2) % numPlayers))
                    {
                        position = CalculateFanPosition(GetTopOpponentBasePosition(false), hand.Count, i, 180f, EffectiveOpponentCardScale);
                        baseRotation = 180f;
                    }
                    else
                    {
                        position = CalculateFanPositionVertical(GetSideOpponentBasePosition(false), hand.Count, i, false, EffectiveOpponentCardScale);
                        baseRotation = -90f;
                    }

                    cardView.SetPosition(position);

                    if (useFanLayout)
                    {
                        float fanRotation = 0f;

                        // 1v1: solo rotazione fan orizzontale
                        if (numPlayers == 2)
                        {
                            fanRotation = CalculateFanRotation(hand.Count, i);
                        }
                        // 4P: rotazione fan verticale per sinistra/destra, orizzontale per alto
                        else if (p == ((localIndex + 1) % numPlayers) || p == ((localIndex + 3) % numPlayers))
                        {
                            fanRotation = CalculateFanRotationVertical(hand.Count, i);
                            if (p == ((localIndex + 3) % numPlayers))
                            {
                                fanRotation = -fanRotation;
                            }
                        }
                        else
                        {
                            fanRotation = CalculateFanRotation(hand.Count, i);
                        }
                        cardView.transform.rotation = Quaternion.Euler(0, 0, baseRotation + fanRotation);
                    }
                    else
                    {
                        cardView.transform.rotation = Quaternion.Euler(0, 0, baseRotation);
                    }

                    cardView.IsClickable = false;
                    cardView.EnableHover = hasAccuso;
                }

                if (hasAccuso && hand.Count == 3)
                {
                    ApplyMattaSpecialVisual(hand);
                }
            }
        }

        /// <summary>
        /// Gets the sprite for a specific card. Public for use by other UI components.
        /// </summary>
        public Sprite GetSpriteForCard(Card card)
        {
            // 1) Do NOT rely on array index ordering; many packs are unordered.
            // Prefer explicit mappings or name-based resolution.

            // 2) Name-based fallback for renamed assets (e.g., Bastoni_1, Coppe_7, Spade_Re, Denari_Asso)
            if (spriteLookup == null || spriteLookup.Count == 0)
            {
                PopulateSpriteLookup();
            }
            string suitName = card.Suit.ToString().ToLowerInvariant();
            string rankNum = card.Rank.ToString();
            // Common keys
            var candidates = new List<string>
            {
                $"{suitName}_{rankNum}",
                $"{suitName}{rankNum}"
            };
            // Figure names
            switch (card.Rank)
            {
                case 1: candidates.AddRange(new[]{ $"{suitName}_asso", $"{suitName}_ace" }); break;
                case 8: candidates.AddRange(new[]{ $"{suitName}_fante", $"{suitName}_jack" }); break;
                case 9: candidates.AddRange(new[]{ $"{suitName}_cavallo", $"{suitName}_queen" }); break;
                case 10: candidates.AddRange(new[]{ $"{suitName}_re", $"{suitName}_king" }); break;
            }
            foreach (var key in candidates)
            {
                var k = key.ToLowerInvariant();
                if (spriteLookup.TryGetValue(k, out var s)) return s;
            }

            return null;
        }

        [System.Serializable]
        private struct CardSpriteMapping
        {
            public Suit Suit;
            public int Rank;
            public Sprite Sprite;
        }

        private void PopulateSpriteLookup()
        {
            spriteLookup.Clear();
            if (cardSprites == null) return;
            // Known suit and rank synonyms to help parse file names
            var suitCanonical = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "denari", "denari" }, { "diamonds", "denari" }, { "d", "denari" },
                { "coppe", "coppe" }, { "hearts", "coppe" }, { "c", "coppe" },
                { "bastoni", "bastoni" }, { "clubs", "bastoni" }, { "b", "bastoni" },
                { "spade", "spade" }, { "spades", "spade" }, { "s", "spade" }
            };

            var rankSynonyms = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "1", 1 }, { "01", 1 }, { "ace", 1 }, { "asso", 1 }, { "a", 1 },
                { "2", 2 }, { "3", 3 }, { "4", 4 }, { "5", 5 }, { "6", 6 }, { "7", 7 },
                { "8", 8 }, { "jack", 8 }, { "fante", 8 }, { "j", 8 },
                { "9", 9 }, { "horse", 9 }, { "cavallo", 9 },
                { "10", 10 }, { "king", 10 }, { "re", 10 }, { "k", 10 }
            };

            foreach (var s in cardSprites)
            {
                if (s == null) continue;
                var raw = s.name ?? string.Empty;
                var name = raw.ToLowerInvariant();

                // add the raw name and some normalized variants
                void AddKey(string k)
                {
                    var kk = k.ToLowerInvariant();
                    if (!spriteLookup.ContainsKey(kk))
                        spriteLookup[kk] = s;
                }

                AddKey(name);
                AddKey(name.Replace(" ", ""));
                AddKey(name.Replace("_", ""));
                AddKey(name.Replace("-", ""));

                // Tokenize by non-alphanumeric to try to find suit and rank
                var tokens = System.Text.RegularExpressions.Regex.Split(name, "[^a-z0-9]+");
                string foundSuit = null;
                string foundRank = null;

                foreach (var t in tokens)
                {
                    if (string.IsNullOrWhiteSpace(t)) continue;
                    if (foundSuit == null && suitCanonical.TryGetValue(t, out var canonicalSuit))
                    {
                        foundSuit = canonicalSuit;
                    }

                    if (foundRank == null && rankSynonyms.TryGetValue(t, out var rank))
                    {
                        foundRank = rank.ToString();
                    }
                }

                if (foundSuit != null && foundRank != null)
                {
                    AddKey($"{foundSuit}_{foundRank}");
                    AddKey($"{foundSuit}{foundRank}");
                    AddKey($"{foundRank}_{foundSuit}");
                    AddKey($"{foundRank}{foundSuit}");
                }
                // skip per-card cache population; rely on provider
            }
        }

        private void Start()
        {
            // Cache TurnController reference if not assigned in Inspector
            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
                if (turnController == null)
                {
                    Debug.LogWarning("CardViewManager: TurnController not found. Please assign it in the Inspector or ensure TurnController exists in the scene.");
                }
            }
            
            CacheLayoutCamera();

            // reset name lookup cache on scene start
            spriteLookup.Clear();

            // Auto-load sprites if not assigned
            if (cardSprites == null || cardSprites.Length == 0)
            {
                var loaded = Resources.LoadAll<Sprite>("Cards");
                if (loaded != null && loaded.Length > 0)
                {
                    cardSprites = loaded;
                    // sprites loaded
                }
                else
                {
                    // no sprites found in Resources/Cards
                }
            }

            // Auto-load Matta special sprites if not assigned
            if (mattaSpecialSprites == null || mattaSpecialSprites.Length == 0)
            {
                LoadMattaSpecialSprites();
            }

            // Build explicit mapping cache from inspector entries
            BuildExplicitMapCache();

            // No global provider usage; mapping handled locally

            // Log prefab status for debugging
            if (cardViewPrefab == null)
            {
                Debug.LogWarning("CardViewManager.Start: cardViewPrefab is null after Start(). Cards will not be rendered.");
            }

            ForceRefresh();
        }

        private void CacheLayoutCamera()
        {
            layoutCamera = Camera.main;
            if (layoutCamera == null)
            {
                layoutCamera = FindObjectOfType<Camera>();
            }
        }

        private void EnsureLayoutCameraCached()
        {
            if (layoutCamera == null)
            {
                CacheLayoutCamera();
            }
        }

        private void ApplyResponsiveCameraIfAvailable()
        {
            EnsureLayoutCameraCached();
            if (layoutCamera == null)
            {
                return;
            }

            var responsiveFit = layoutCamera.GetComponent("CameraResponsiveFit") as MonoBehaviour;
            if (responsiveFit == null)
            {
                return;
            }

            var applyMethod = responsiveFit.GetType().GetMethod("Apply");
            applyMethod?.Invoke(responsiveFit, null);
        }

        private float GetVisibleWidth()
        {
            ApplyResponsiveCameraIfAvailable();

            if (layoutCamera != null && layoutCamera.orthographic)
            {
                return Mathf.Max(1f, layoutCamera.orthographicSize * 2f * layoutCamera.aspect);
            }

            return 12f;
        }

        private float GetVisibleHeight()
        {
            ApplyResponsiveCameraIfAvailable();

            if (layoutCamera != null && layoutCamera.orthographic)
            {
                return Mathf.Max(1f, layoutCamera.orthographicSize * 2f);
            }

            return 8f;
        }

        private float GetResponsiveHorizontalSpacing(int totalCards, float minSpacing, float widthUsage)
        {
            if (!useResponsiveLayout || totalCards <= 1)
            {
                return cardSpacing;
            }

            float availableWidth = Mathf.Max(minSpacing, (GetVisibleWidth() * widthUsage) - (layoutScreenPadding * 2f));
            float spacing = availableWidth / Mathf.Max(1, totalCards - 1);
            return Mathf.Clamp(spacing, minSpacing, cardSpacing);
        }

        private float GetResponsiveVerticalSpacing(int totalCards, float minSpacing, float heightUsage)
        {
            if (!useResponsiveLayout || totalCards <= 1)
            {
                return cardSpacing;
            }

            float availableHeight = Mathf.Max(minSpacing, (GetVisibleHeight() * heightUsage) - (layoutScreenPadding * 2f));
            float spacing = availableHeight / Mathf.Max(1, totalCards - 1);
            return Mathf.Clamp(spacing, minSpacing, cardSpacing);
        }

        private Vector3 GetTableCenterPosition()
        {
            return tableCardContainer != null ? tableCardContainer.position : Vector3.zero;
        }

        private Vector3 GetHumanHandBasePosition()
        {
            Vector3 tableCenter = GetTableCenterPosition();
            Vector3 position = tableCenter + Vector3.down * (GetVisibleHeight() * localPlayerBottomOffsetRatio);

            if (humanHandContainer != null)
            {
                position.x = humanHandContainer.position.x;
                position.z = humanHandContainer.position.z;
            }

            return position;
        }

        private Vector3 GetTopOpponentBasePosition(bool isTwoPlayers)
        {
            float ratio = isTwoPlayers ? topOpponentOffsetRatio : topOpponentOffsetRatioFourPlayers;
            float visibleHeight = GetVisibleHeight();
            return GetTableCenterPosition() + Vector3.up * (visibleHeight * ratio) + Vector3.down * (visibleHeight * opponentHandScreenDownOffsetRatio);
        }

        private Vector3 GetSideOpponentBasePosition(bool isLeft)
        {
            float direction = isLeft ? -1f : 1f;
            float visibleHeight = GetVisibleHeight();
            float downOffset = opponentHandScreenDownOffsetRatio + sideOpponentAdditionalScreenDownOffsetRatio;
            return GetTableCenterPosition() + Vector3.right * (GetVisibleWidth() * sideOpponentOffsetRatio * direction) + Vector3.down * (visibleHeight * downOffset);
        }

        /// <summary>
        /// Restituisce l'ancora della mano per una seduta relativa al giocatore locale.
        /// Indici: 0 = locale, 1 = sinistra, 2 = fronte, 3 = destra.
        /// </summary>
        public Vector3 GetPlayerHandAnchor(int relativePlayerIndex, int playerCount)
        {
            if (relativePlayerIndex == 0)
            {
                return GetHumanHandBasePosition();
            }

            if (playerCount == 2)
            {
                return GetTopOpponentBasePosition(true);
            }

            switch (relativePlayerIndex)
            {
                case 1:
                    return GetSideOpponentBasePosition(true);
                case 2:
                    return GetTopOpponentBasePosition(false);
                default:
                    return GetSideOpponentBasePosition(false);
            }
        }

        /// <summary>
        /// Restituisce la destra relativa del giocatore nella sua posizione al tavolo.
        /// </summary>
        public Vector3 GetPlayerRightDirection(int relativePlayerIndex, int playerCount)
        {
            if (relativePlayerIndex == 0)
            {
                return Vector3.right;
            }

            if (playerCount == 2 || relativePlayerIndex == 2)
            {
                return Vector3.left;
            }

            return relativePlayerIndex == 1 ? Vector3.down : Vector3.up;
        }

        /// <summary>
        /// Distanza responsive per posizionare prese e scope accanto alla mano.
        /// </summary>
        public float GetCapturedPileDistance()
        {
            return Mathf.Max(1.8f, Mathf.Min(GetVisibleWidth(), GetVisibleHeight()) * 0.22f);
        }

        /// <summary>
        /// Offset verso il basso per separare prese e scope dalla mano senza usare coordinate fisse.
        /// </summary>
        public Vector3 GetCapturedPileScreenDownOffset()
        {
            return Vector3.down * (GetVisibleHeight() * capturedPileScreenDownOffsetRatio);
        }

        private Vector3 CalculateTableCardPosition(int totalCards, int cardIndex)
        {
            Vector3 center = GetTableCenterPosition();
            float spacing = GetResponsiveHorizontalSpacing(totalCards, minTableCardSpacing, tableWidthUsage) * EffectiveTableCardScale;
            float offset = (cardIndex - (totalCards - 1) / 2f) * spacing;
            return center + Vector3.right * offset;
        }

        /// <summary>
        /// Calcola la posizione che avr� una carta aggiunta al tavolo senza modificare lo stato.
        /// </summary>
        public Vector3 GetNextTableCardPosition(int currentTableCardCount)
        {
            int totalCards = Mathf.Max(1, currentTableCardCount + 1);
            return CalculateTableCardPosition(totalCards, totalCards - 1);
        }

        /// <summary>
        /// Loads the special Matta sprites from Resources/Cards.
        /// Expected naming: Matta_1, Matta_2, ..., Matta_10
        /// </summary>
        private void LoadMattaSpecialSprites()
        {
            var mattaSprites = new List<Sprite>();
            
            // Try to load all Matta special sprites (ranks 1-10)
            for (int rank = 1; rank <= 10; rank++)
            {
                string spriteName = $"Matta_{rank}";
                var sprite = Resources.Load<Sprite>($"Cards/{spriteName}");
                
                if (sprite != null)
                {
                    mattaSprites.Add(sprite);
                }
                else
                {
                    // Sprite not found, log warning if debug enabled
                    if (enableSpriteDebug)
                    {
                        Debug.LogWarning($"Matta special sprite not found: Cards/{spriteName}");
                    }
                    // Add null to maintain index alignment
                    mattaSprites.Add(null);
                }
            }
            
            mattaSpecialSprites = mattaSprites.ToArray();
        }

        /// <summary>
        /// Gets the special Matta sprite for a specific rank.
        /// Returns null if not found.
        /// </summary>
        private Sprite GetMattaSpecialSprite(int rank)
        {
            if (mattaSpecialSprites == null || mattaSpecialSprites.Length == 0)
            {
                return null;
            }
            
            // Rank 1-10 maps to index 0-9
            int index = rank - 1;
            
            if (index >= 0 && index < mattaSpecialSprites.Length)
            {
                return mattaSpecialSprites[index];
            }
            
            return null;
        }

        /// <summary>
        /// Refreshes all card views to match the current game state.
        /// </summary>
        private void RefreshCardViews()
        {
            if (turnController == null)
            {
                Debug.LogWarning("CardViewManager.RefreshCardViews: turnController is null");
                return;
            }
            
            if (turnController.GameState == null)
            {
                // GameState not yet initialized - this is normal before StartNewGame
                return;
            }

            var state = turnController.GameState;
            
            // Refresh card views

            // Clear old views that are no longer in the game
            CleanupOldViews(state);

            // Render table cards
            RenderTableCards(state.Table);

            // Determine local player index via GameManager (reflection to avoid assembly ref)
            int localIndex = 0;
            var gmType = System.Type.GetType("Project51.Unity.GameManager, Project51.Networking");
            if (gmType != null)
            {
                var isMpProp = gmType.GetProperty("IsMultiplayerSafe", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                bool isMp = false;
                if (isMpProp != null)
                {
                    var val = isMpProp.GetValue(null);
                    if (val is bool b) isMp = b;
                }

                var instanceProp = gmType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var gm = instanceProp?.GetValue(null);
                if (gm != null)
                {
                    var localIdxProp = gmType.GetProperty("LocalPlayerIndex", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (localIdxProp != null)
                    {
                        var idxVal = localIdxProp.GetValue(gm);
                        if (idxVal is int i && i >= 0 && i < state.Players.Count)
                        {
                            localIndex = i;
                        }
                    }
                }
            }

            // Render local human player's hand at bottom UI using their actual hand
            if (state.Players.Count > localIndex)
            {
                RenderHumanHand(state.Players[localIndex].Hand);
            }

            // Render other players' hands (face-down)
            if (state.Players.Count > 1)
            {
                RenderAIHandsDynamic(state.Players, localIndex);
            }
        }

        // Allow external callers (e.g., context menus) to force an immediate UI refresh
        public void ForceRefresh()
        {
            // Force refresh
            RefreshCardViews();
        }

        /// <summary>
        /// Restituisce la vista gi� presente per una carta, senza crearla o modificarla.
        /// Usato dal controller di animazione prima del commit della mossa.
        /// </summary>
        public bool TryGetCardView(Card card, out CardView cardView)
        {
            if (card != null && activeCardViews.TryGetValue(card, out var resolvedView) && resolvedView != null)
            {
                cardView = resolvedView;
                return true;
            }

            cardView = null;
            return false;
        }

        /// <summary>
        /// Removes card views for cards that are no longer in play.
        /// </summary>
        private void CleanupOldViews(GameState state)
        {
            var currentCards = new HashSet<Card>();

            // Collect all cards currently in play
            currentCards.UnionWith(state.Table);
            foreach (var player in state.Players)
            {
                currentCards.UnionWith(player.Hand);
            }

            // Remove views for cards no longer present
            var viewsToRemove = activeCardViews.Keys.Where(c => !currentCards.Contains(c)).ToList();
            foreach (var card in viewsToRemove)
            {
                if (activeCardViews.TryGetValue(card, out var view))
                {
                    // removing CardView for card
                    view.DestroyView();
                    activeCardViews.Remove(card);
                }
            }
        }

        /// <summary>
        /// Renders the cards on the table - always face-up and STRAIGHT (rotation 0).
        /// </summary>
        private void RenderTableCards(List<Card> tableCards)
        {
            for (int i = 0; i < tableCards.Count; i++)
            {
                var card = tableCards[i];
                CardView cardView;

                if (!activeCardViews.ContainsKey(card))
                {
                    // Cards on table are ALWAYS face-up (scoperte) - even if played by bots
                    var view = CreateCardView(card, faceUp: true, clickable: false);
                    if (view != null)
                    {
                        // Hook clicks for table cards so they can be selected during manual selection mode
                        view.OnCardClicked += OnTableCardClicked;
                        // Ensure table cards are not clickable unless selection mode is active
                        view.IsClickable = false;
                        // Disable hover by default - only enabled during selection mode
                        view.EnableHover = false;
                        activeCardViews[card] = view;
                        cardView = view;
                    }
                    else
                    {
                        // silent: failed to create view for table card
                        continue;
                    }
                }
                else
                {
                    // Card already exists (was in bot hand, now on table)
                    // FLIP IT TO FACE-UP!
                    cardView = activeCardViews[card];
                    var faceSprite = GetSpriteForCard(card);
                    if (faceSprite != null)
                    {
                        cardView.FlipToFaceUp(faceSprite);
                    }

                    // Disable hover when card moves from hand to table
                    cardView.EnableHover = false;

                    // Difensivo: una carta riposizionata sul tavolo deve sempre essere visibile,
                    // anche se un'animazione precedente aveva disabilitato il renderer.
                    if (cardView.CardRenderer != null)
                    {
                        cardView.CardRenderer.enabled = true;
                    }
                }

                Vector3 position = CalculateTableCardPosition(tableCards.Count, i);

                cardView.SetDisplayScale(EffectiveTableCardScale);
                cardView.SetPosition(position);
                
                // IMPORTANT: Table cards are ALWAYS straight (rotation 0) - no fan layout
                cardView.transform.rotation = Quaternion.Euler(0, 0, 0);
            }

            // Table render does not apply Matta visual
        }

        /// <summary>
        /// Updates interactivity (clickable and hover) for all table cards based on selection mode.
        /// Called when entering/exiting selection mode.
        /// </summary>
        private void UpdateTableCardsInteractivity()
        {
            if (turnController == null || turnController.GameState == null) return;
            
            var tableCards = turnController.GameState.Table ?? new List<Card>();
            foreach (var c in tableCards)
            {
                if (activeCardViews.TryGetValue(c, out var view))
                {
                    // Only enable interactivity during selection mode
                    view.IsClickable = isSelecting;
                    view.EnableHover = isSelecting;
                }
            }
        }

        private void ApplyMattaSpecialVisual(List<Card> handCards)
        {
            // Only aid when human has exactly 3 cards in hand
            if (handCards == null || handCards.Count != 3) return;
            var matta = handCards.FirstOrDefault(c => c.IsMatta);
            if (matta == null) return;

            // Evaluate the other two cards
            var others = handCards.Where(c => !c.IsMatta).ToList();
            if (others.Count != 2) return;

            // Decino aid: the other two ranks are equal
            bool isDecino = others[0].Value == others[1].Value;
            int decinoRank = isDecino ? others[0].Value : -1;

            // Accuso aid: show Ace visual only if the sum of all 3 cards, counting Matta as 1, is <= 9
            // i.e., others[0].Value + others[1].Value + 1 <= 9
            int othersSum = (others[0].Value + others[1].Value);
            bool showAceForAccuso = !isDecino && (othersSum + 1) <= 9;
            int specialRank = isDecino ? decinoRank : (showAceForAccuso ? 1 : -1);

            if (activeCardViews.TryGetValue(matta, out var mattaView))
            {
                if (specialRank > 0)
                {
                    // Try to get the special Matta sprite first
                    var tempSprite = GetMattaSpecialSprite(specialRank);
                    
                    // Fallback to normal card sprite if special sprite not found
                    if (tempSprite == null)
                    {
                        tempSprite = GetSpriteForRank(specialRank, matta.Suit);
                        if (enableSpriteDebug)
                        {
                            Debug.LogWarning($"Using fallback sprite for Matta rank {specialRank}");
                        }
                    }
                    
                    var marker = GetMarkerSprite();
                    if (tempSprite != null)
                    {
                        mattaView.ShowTemporaryValue(tempSprite, marker);
                    }
                }
                else
                {
                    // No special visual needed; ensure Matta shows its real face
                    mattaView.ClearTemporaryValue();
                }
            }
        }

        private void OnTableCardClicked(CardView tableCardView)
        {
            if (!turnController.IsHumanPlayerTurn) return;

            if (!isSelecting)
            {
                // ignore table clicks when not selecting
                return;
            }

            var card = tableCardView.Card;
            if (selectionTableCards.Contains(card))
            {
                selectionTableCards.Remove(card);
                tableCardView.SetSelected(false);
            }
            else
            {
                selectionTableCards.Add(card);
                tableCardView.SetSelected(true);
            }

            // update message text in UI if available
            if (moveSelectionUI != null)
            {
                // If selection is invalid (not matching any valid capture subset), show suggestion arrows instead of message
                var validMoves = turnController.GetMovesForCard(selectionPlayedCard);
                var matching = Rules51.GetMatchingMovesFromSelection(turnController.GameState, 0, selectionPlayedCard, selectionTableCards);
                if (matching.Count == 0)
                {
                    // show suggestion: highlight all possible captures for the played card
                    // only show help on the first wrong selection
                    if (!helpShownForCurrentSelection)
                    {
                        var allMatches = Rules51.GetMatchingMovesFromSelection(turnController.GameState, 0, selectionPlayedCard, null);
                        if (allMatches.Count > 0)
                        {
                            helpShownForCurrentSelection = true;
                            // highlight first alternative
                            HighlightAlternative(allMatches, 0, null);
                        }
                    }
                    else
                    {
                        var names = selectionTableCards.Select(c => c.ToString()).ToList();
                        var msg = names.Count == 0 ? "Selected: (none)" : "Selected: " + string.Join(", ", names);
                        moveSelectionUI.ShowInvalid(msg, 0.9f);
                    }
                }
                else
                {
                    var names = selectionTableCards.Select(c => c.ToString()).ToList();
                    var msg = names.Count == 0 ? "Selected: (none)" : "Selected: " + string.Join(", ", names);
                    moveSelectionUI.ShowInvalid(msg, 0.9f);
                }
            }
            
            // Update hover state for all table cards based on selection mode
            UpdateTableCardsInteractivity();
        }

        private void HighlightAlternative(List<Move> moves, int hoveredIndex, CardView contextPlayedCard)
        {
            ClearArrowsAndHighlights();
            if (hoveredIndex < 0 || hoveredIndex >= moves.Count) return;
            var m = moves[hoveredIndex];
            if (m == null) return;

            // Highlight captured cards for this move
            currentlyHighlightedCards = new List<Card>(m.CapturedCards ?? new List<Card>());
            foreach (var c in currentlyHighlightedCards)
            {
                if (activeCardViews.TryGetValue(c, out var view))
                {
                    view.SetSelected(true);
                }
            }

            // Create markers above each captured card (do not draw lines from hand)
            foreach (var c in currentlyHighlightedCards)
            {
                if (!activeCardViews.TryGetValue(c, out var toView)) continue;
                var marker = CreateMarkerAt(toView.transform.position);
                if (marker != null) currentMarkers.Add(marker);
            }

            // Tooltips removed - we rely on bounce animation for visual feedback
        }

        /// <summary>
        /// Shows sequential bounce animation on all valid capture options.
        /// Used when player makes an invalid selection to show them what cards can be captured.
        /// </summary>
        /// <param name="moves">List of valid moves to highlight</param>
        /// <param name="delayBetweenMoves">Delay between showing each move option</param>
        public void ShowSequentialCaptureHints(List<Move> moves, float delayBetweenMoves = 0.6f)
        {
            if (moves == null || moves.Count == 0) return;

            // Stop any existing hint animations
            StopAllHintAnimations();

            // Get unique capture sets
            var seenSets = new HashSet<string>();
            var uniqueMoves = new List<Move>();
            
            foreach (var move in moves.Where(m => m.Type != MoveType.PlayOnly))
            {
                var setKey = string.Join("|", (move.CapturedCards ?? new List<Card>())
                    .Select(c => c.ToString()).OrderBy(s => s));
                
                if (!seenSets.Contains(setKey))
                {
                    seenSets.Add(setKey);
                    uniqueMoves.Add(move);
                }
            }

            // Play bounce animations with sequential delays
            float currentDelay = 0f;
            foreach (var move in uniqueMoves)
            {
                if (move.CapturedCards == null) continue;
                
                foreach (var card in move.CapturedCards)
                {
                    if (activeCardViews.TryGetValue(card, out var view))
                    {
                        view.PlayHintBounce(currentDelay, 2);
                    }
                }
                
                currentDelay += delayBetweenMoves;
            }
        }

        /// <summary>
        /// Stops all hint bounce animations on table cards.
        /// </summary>
        public void StopAllHintAnimations()
        {
            if (turnController?.GameState?.Table == null) return;
            
            foreach (var card in turnController.GameState.Table)
            {
                if (activeCardViews.TryGetValue(card, out var view))
                {
                    view.StopHintBounce();
                }
            }
        }

        private GameObject CreateMarkerAt(Vector3 pos)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null && Camera.main != null)
            {
                var ui = new GameObject("MarkerUI");
                ui.transform.SetParent(canvas.transform, false);
                var rt = ui.AddComponent<RectTransform>();
                Vector2 screen = Camera.main.WorldToScreenPoint(pos + Vector3.up * 0.6f);
                // convert screen point to canvas local point
                Vector2 localPoint;
                var canvasRect = canvas.GetComponent<RectTransform>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out localPoint);
                rt.anchoredPosition = localPoint;
                rt.sizeDelta = new Vector2(28, 28);
                var img = ui.AddComponent<UnityEngine.UI.Image>();
                // assign a simple generated white sprite if builtin not available
                img.sprite = GetMarkerSprite();
                img.color = Color.yellow;
                img.raycastTarget = false;
                return ui;
            }

            // fallback: small sphere in world
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Marker";
            go.transform.position = pos + Vector3.up * 0.6f + (Camera.main != null ? (Camera.main.transform.forward * -0.01f) : Vector3.back * 0.01f);
            go.transform.localScale = Vector3.one * 0.25f;
            var mr = go.GetComponent<Renderer>();
            if (mr != null)
            {
                mr.material = new Material(Shader.Find("Sprites/Default")) { color = Color.yellow };
            }
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            return go;
        }

        // CreateTooltip method removed - tooltips caused memory leaks and visual clutter
        // We now rely on card bounce animations for visual feedback

        private void ClearArrowsAndHighlights()
        {
            foreach (var m in currentMarkers)
            {
                if (m == null) continue;
                try { Destroy(m); } catch { }
            }
            currentMarkers.Clear();
            foreach (var c in currentlyHighlightedCards)
            {
                if (activeCardViews.TryGetValue(c, out var v)) v.SetSelected(false);
            }
            currentlyHighlightedCards.Clear();
        }

        /// <summary>
        /// Renders the human player's hand with fan layout.
        /// </summary>
        private void RenderHumanHand(List<Card> handCards)
        {
            // Empty hand is normal when waiting for network GameState or between deals
            if (handCards == null || handCards.Count == 0)
            {
                return;
            }
            
            for (int i = 0; i < handCards.Count; i++)
            {
                var card = handCards[i];
                if (!activeCardViews.ContainsKey(card))
                {
                    var view = CreateCardView(card, faceUp: true, clickable: turnController.IsHumanPlayerTurn);
                    if (view != null)
                    {
                        // Hook UI events
                        view.OnCardClicked += OnHumanCardClicked;
                        view.OnCardDoubleClicked += OnHumanCardDoubleClicked;
                        // Do NOT subscribe to OnDragReleased for human cards: drag-to-play UI disabled for human

                        // Human cards: clickable and show hover overlay
                        view.IsClickable = turnController.IsHumanPlayerTurn;
                        // ALWAYS enable hover for player cards (even when not their turn - for Matta visual)
                        view.EnableHover = true;
                        // Ensure selection state cleared
                        view.SetSelected(false);
                        activeCardViews[card] = view;
                    }
                    else
                    {
                        // CreateCardView returned null - prefab might be missing
                        continue;
                    }
                }

                var cardView = activeCardViews[card];
                cardView.SetDisplayScale(EffectiveLocalPlayerCardScale);
                // Ensure interactivity reflects current turn
                cardView.IsClickable = turnController.IsHumanPlayerTurn;
                // ALWAYS enable hover (for Matta visual hints to work)
                cardView.EnableHover = true;

                // Difensivo: stesso motivo di RenderTableCards.
                if (cardView.CardRenderer != null)
                {
                    cardView.CardRenderer.enabled = true;
                }

                // Calculate position with fan layout
                Vector3 position = CalculateFanPosition(GetHumanHandBasePosition(), handCards.Count, i, 0f, EffectiveLocalPlayerCardScale);
                cardView.SetPosition(position);
                
                // Apply rotation for fan effect - INVERTED for bottom player
                if (useFanLayout)
                {
                    float angle = CalculateFanRotation(handCards.Count, i);
                    // Invert angle for bottom player (opposite of top)
                    angle = -angle;
                    cardView.transform.rotation = Quaternion.Euler(0, 0, angle);
                }
            }

            // After positioning human hand, apply Matta special visual if applicable
            ApplyMattaSpecialVisual(handCards);
        }

        /// <summary>
        /// Calculates the position for a card in a fan layout.
        /// </summary>
        private Vector3 CalculateFanPosition(Vector3 centerPos, int totalCards, int cardIndex, float baseRotation, float spacingMultiplier = 1f)
        {
            float effectiveSpacing = GetResponsiveHorizontalSpacing(totalCards, minHandCardSpacing, handWidthUsage) * spacingMultiplier;
            float effectiveFanRadius = fanRadius * Mathf.Sqrt(spacingMultiplier);

            if (!useFanLayout || totalCards <= 1)
            {
                float offset = (cardIndex - (totalCards - 1) / 2.0f) * effectiveSpacing * 0.6f;
                return centerPos + Vector3.right * offset;
            }

            float normalizedIndex = totalCards > 1 ? (float)cardIndex / (totalCards - 1) : 0.5f;
            float angle = Mathf.Lerp(-fanAngle, fanAngle, normalizedIndex);
            float angleRad = angle * Mathf.Deg2Rad;

            float x = Mathf.Sin(angleRad) * effectiveFanRadius;
            float y = (1f - Mathf.Cos(angleRad)) * effectiveFanRadius * 0.5f;
            float horizontalOffset = (normalizedIndex - 0.5f) * (totalCards - 1) * effectiveSpacing * 0.5f;

            return centerPos + new Vector3(horizontalOffset + x, y, 0);
        }

        /// <summary>
        /// Calculates the position for a card in a vertical fan layout (for left/right players).
        /// </summary>
        private Vector3 CalculateFanPositionVertical(Vector3 centerPos, int totalCards, int cardIndex, bool isLeft, float spacingMultiplier = 1f)
        {
            float effectiveSpacing = GetResponsiveVerticalSpacing(totalCards, minSideHandCardSpacing, sideHandHeightUsage) * spacingMultiplier;
            float effectiveFanRadius = fanRadius * Mathf.Sqrt(spacingMultiplier);

            if (!useFanLayout || totalCards <= 1)
            {
                float offset = (cardIndex - (totalCards - 1) / 2.0f) * effectiveSpacing * 0.6f;
                return centerPos + Vector3.up * offset;
            }

            float normalizedIndex = totalCards > 1 ? (float)cardIndex / (totalCards - 1) : 0.5f;
            float angle = Mathf.Lerp(-fanAngle, fanAngle, normalizedIndex);
            float angleRad = angle * Mathf.Deg2Rad;

            float ySpread = Mathf.Sin(angleRad) * effectiveFanRadius;
            float xCurve = (1f - Mathf.Cos(angleRad)) * effectiveFanRadius * 0.8f;
            if (!isLeft)
            {
                xCurve = -xCurve;
            }

            float verticalOffset = (normalizedIndex - 0.5f) * (totalCards - 1) * effectiveSpacing * 0.5f;

            return centerPos + new Vector3(xCurve, verticalOffset + ySpread, 0);
        }

        /// <summary>
        /// Calculates the rotation angle for a card in a fan layout.
        /// </summary>
        private float CalculateFanRotation(int totalCards, int cardIndex)
        {
            if (totalCards <= 1) return 0f;
            
            float normalizedIndex = (float)cardIndex / (totalCards - 1);
            return Mathf.Lerp(-fanAngle, fanAngle, normalizedIndex);
        }

        /// <summary>
        /// Calculates the rotation angle for a card in a vertical fan layout.
        /// </summary>
        private float CalculateFanRotationVertical(int totalCards, int cardIndex)
        {
            if (totalCards <= 1) return 0f;
            
            float normalizedIndex = (float)cardIndex / (totalCards - 1);
            // For vertical fans, rotation should be perpendicular
            return Mathf.Lerp(-fanAngle, fanAngle, normalizedIndex);
        }

        private void OnHumanCardDoubleClicked(CardView clickedCardView)
        {
            if (!turnController.IsHumanPlayerTurn) return;
            turnController.OnPlayerDoubleClick(clickedCardView.Card);
        }

        private void OnHumanCardDragReleased(CardView cardView, Vector3 worldPos)
        {
            if (!turnController.IsHumanPlayerTurn) return;
            // Use a Physics2D overlap to detect table card colliders under the release point.
            var targets = new List<Card>();
            Vector2 point = new Vector2(worldPos.x, worldPos.y);
            const float overlapRadius = 0.35f; // tighter radius for precise drops
            var hits = Physics2D.OverlapCircleAll(point, overlapRadius);
            if (hits != null && hits.Length > 0)
            {
                foreach (var hit in hits)
                {
                    if (hit == null) continue;
                    var view = hit.GetComponent<CardView>();
                    if (view == null) continue;
                    // Only consider cards that are currently on the table
                    if (turnController.GameState.Table.Contains(view.Card))
                        targets.Add(view.Card);
                }
            }

            // Fallback: if no collider hit, pick nearest table card within a larger radius
            if (targets.Count == 0)
            {
                float fallbackRadius = 0.6f;
                var tableCards = turnController.GameState.Table;
                Card nearest = null;
                float bestDist = fallbackRadius;
                foreach (var kv in activeCardViews)
                {
                    var v = kv.Value;
                    if (v == null) continue;
                    if (!tableCards.Contains(v.Card)) continue;
                    float d = Vector3.Distance(v.transform.position, worldPos);
                    if (d <= bestDist)
                    {
                        bestDist = d;
                        nearest = v.Card;
                    }
                }
                if (nearest != null) targets.Add(nearest);
            }

            turnController.OnPlayerDragPlay(cardView.Card, targets);
        }

        /// <summary>
        /// Creates a new CardView instance.
        /// </summary>
        private CardView CreateCardView(Card card, bool faceUp, bool clickable)
        {
            if (cardViewPrefab == null)
            {
                Debug.LogWarning($"CardViewManager.CreateCardView: cardViewPrefab is null, cannot create CardView for {card}");
                return null;
            }

            var viewObj = Instantiate(cardViewPrefab, transform);
            // Hide runtime objects from Hierarchy/selection to avoid editor inspector errors when destroyed
            // But don't hide completely to allow FindObjectsOfType to work in tests
            viewObj.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            viewObj.SetActive(true);
            viewObj.transform.localScale = Vector3.one;
            // Name the instance for easier identification in Hierarchy using human-readable rank
            string rankLabel = card.Rank switch { 1 => "Asso", 8 => "Fante", 9 => "Cavallo", 10 => "Re", _ => card.Rank.ToString() };
            viewObj.name = $"Card_{card.Suit}_{rankLabel}";
            var view = viewObj.GetComponent<CardView>();

            if (view == null)
            {
                view = viewObj.AddComponent<CardView>();
            }

            // If manager has a default back assigned use it
            if (defaultCardBack != null)
            {
                view.SetDefaultBack(defaultCardBack);
            }

            // Get correct sprite for this card if cardSprites has been populated
            Sprite cardSprite = GetSpriteForCard(card);

            view.Initialize(card, cardSprite, faceUp);

            // If created face-up and we didn't have the sprite at init time, try to set it now
            if (faceUp && cardSprite == null)
            {
                var resolved = GetSpriteForCard(card);
                if (resolved != null)
                {
                    view.FlipToFaceUp(resolved);
                }
            }
            view.IsClickable = clickable;

            return view;
        }

        /// <summary>
        /// Gets the local player index, with fallback for when GameManager is not available.
        /// </summary>
        private int GetLocalPlayerIndex()
        {
            var gmType = System.Type.GetType("Project51.Unity.GameManager, Project51.Networking");
            if (gmType != null)
            {
                var instanceProp = gmType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var gm = instanceProp?.GetValue(null);
                if (gm != null)
                {
                    var localIdxProp = gmType.GetProperty("LocalPlayerIndex", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (localIdxProp != null)
                    {
                        var idxVal = localIdxProp.GetValue(gm);
                        if (idxVal is int i) return i;
                    }
                }
            }
            return 0;
        }

        private void OnDestroy()
        {
        }

        /// <summary>
        /// Handles human player clicking a card in their hand.
        /// Uses unified logic for all cards.
        /// </summary>
        private void OnHumanCardClicked(CardView clickedCardView)
        {
            if (!turnController.IsHumanPlayerTurn)
            {
                // silent: not human turn
                return;
            }

            // Visual feedback: select this card and deselect others (works without separate UI)
            foreach (var kv in activeCardViews)
            {
                var v = kv.Value;
                if (v == null) continue;
                v.SetSelected(v == clickedCardView);
            }

            var validMoves = turnController.GetCurrentValidMoves();
            var movesForCard = validMoves.Where(m => m.PlayedCard.Equals(clickedCardView.Card)).ToList();

            // If there are no valid moves for this specific card, allow a PlayOnly move
            if (movesForCard.Count == 0)
            {
                var playOnlyMove = new Move(0, clickedCardView.Card, MoveType.PlayOnly, new List<Card>());
                turnController.ExecuteMove(playOnlyMove);
                foreach (var kv in activeCardViews)
                    kv.Value?.SetSelected(false);
                return;
            }

            // If any capture moves exist, decide whether to auto-execute or show options
            var captureMoves = movesForCard.Where(m => m.Type != MoveType.PlayOnly).ToList();
            if (captureMoves.Count > 0)
            {
                // Group captures by the set of cards they capture (order-independent)
                var setKeys = captureMoves.Select(m =>
                {
                    var keys = (m.CapturedCards ?? new List<Card>()).Select(c => c.ToString()).OrderBy(s => s);
                    return string.Join("|", keys);
                }).Distinct().ToList();

                // ONLY auto-execute if there is exactly ONE distinct capture set
                // This ensures player ALWAYS chooses when multiple different captures exist
                // (e.g., CaptureSum {7,3} vs Capture15 {5} - player must choose!)
                if (setKeys.Count == 1)
                {
                    Move chosen = null;
                    var priority = new[] { MoveType.CaptureEqual, MoveType.Capture15, MoveType.CaptureSum, MoveType.AceCapture };
                    foreach (var p in priority)
                    {
                        chosen = captureMoves.FirstOrDefault(m => m.Type == p && 
                            ((m.CapturedCards ?? new List<Card>()).Count == 0 ? "" : 
                            string.Join("|", (m.CapturedCards ?? new List<Card>()).Select(c => c.ToString()).OrderBy(s => s))) == setKeys[0]);
                        if (chosen != null) break;
                    }
                    if (chosen == null) chosen = captureMoves[0];

                    turnController.ExecuteMove(chosen);
                    foreach (var kv in activeCardViews)
                        kv.Value?.SetSelected(false);
                    return;
                }

                // Multiple distinct capture options exist - let the player choose
                // NO superset logic - player must ALWAYS choose when different card sets are available
                ShowCaptureOptions(clickedCardView.Card, captureMoves);
                return;
            }

            // Otherwise (only PlayOnly moves), execute the first
            turnController.ExecuteMove(movesForCard[0]);
            foreach (var kv in activeCardViews)
                kv.Value?.SetSelected(false);
        }

        /// <summary>
        /// Shows capture options to the player when multiple distinct captures are available.
        /// Does not use Confirm/Cancel - player simply clicks on the desired option.
        /// </summary>
        private void ShowCaptureOptions(Card playedCard, List<Move> captureMoves)
        {
            // Group moves by their captured card set to avoid duplicates
            var uniqueMoves = new List<Move>();
            var seenSets = new HashSet<string>();
            
            // Priority order for move types
            var priority = new[] { MoveType.CaptureEqual, MoveType.Capture15, MoveType.CaptureSum, MoveType.AceCapture };
            
            foreach (var moveType in priority)
            {
                foreach (var move in captureMoves.Where(m => m.Type == moveType))
                {
                    var setKey = string.Join("|", (move.CapturedCards ?? new List<Card>())
                        .Select(c => c.ToString()).OrderBy(s => s));
                    
                    if (!seenSets.Contains(setKey))
                    {
                        seenSets.Add(setKey);
                        uniqueMoves.Add(move);
                    }
                }
            }

            if (uniqueMoves.Count == 0)
            {
                uniqueMoves = captureMoves;
            }

            // Show UI with move options
            if (moveSelectionUI != null)
            {
                var descriptions = uniqueMoves.Select(m => FormatMoveDescription(m)).ToList();
                moveSelectionUI.ShowMoves(descriptions, idx =>
                {
                    if (idx >= 0 && idx < uniqueMoves.Count)
                    {
                        turnController.ExecuteMove(uniqueMoves[idx]);
                    }
                    // Clear selection state
                    foreach (var kv in activeCardViews)
                        kv.Value?.SetSelected(false);
                }, autoHideOnChoose: true, onHover: hoveredIndex => HighlightAlternative(uniqueMoves, hoveredIndex, null));
            }
            else
            {
                // No UI available, execute first move
                turnController.ExecuteMove(uniqueMoves[0]);
                foreach (var kv in activeCardViews)
                    kv.Value?.SetSelected(false);
            }
        }

        /// <summary>
        /// Formats a move description for display in the UI.
        /// </summary>
        private string FormatMoveDescription(Move move)
        {
            if (move.CapturedCards == null || move.CapturedCards.Count == 0)
            {
                return "Play only";
            }

            var cardNames = move.CapturedCards.Select(c => 
            {
                string rankName = c.Rank switch
                {
                    1 => "A",
                    8 => "J",
                    9 => "Q", 
                    10 => "K",
                    _ => c.Rank.ToString()
                };
                string suitName = c.Suit switch
                {
                    Suit.Denari => "?",
                    Suit.Coppe => "?",
                    Suit.Bastoni => "?",
                    Suit.Spade => "?",
                    _ => c.Suit.ToString()
                };
                return $"{rankName}{suitName}";
            }).ToList();

            string captureDesc = string.Join(" + ", cardNames);
            
            string typeDesc = move.Type switch
            {
                MoveType.CaptureEqual => "=",
                MoveType.Capture15 => "?15",
                MoveType.CaptureSum => "?",
                MoveType.AceCapture => "A?",
                _ => ""
            };

            return $"{captureDesc} {typeDesc}";
        }

        private void EnterSelectionMode(CardView clickedCardView)
        {
            // begin selection: store played card and enable table cards selection
            isSelecting = true;
            // reset help state for this new selection
            helpShownForCurrentSelection = false;
            ClearArrowsAndHighlights();
            selectionPlayedCard = clickedCardView.Card;
            selectionTableCards.Clear();

            // visually select the played card
            foreach (var kv in activeCardViews)
            {
                if (kv.Value == null) continue;
                kv.Value.SetSelected(kv.Value == clickedCardView);
            }

            // Enable interactivity for table cards (clickable and hover)
            UpdateTableCardsInteractivity();

            // show confirm/cancel UI
            if (moveSelectionUI != null)
            {
                // do not auto-hide on choose so Confirm/Cancel keep the panel visible for invalid selections
                moveSelectionUI.ShowMoves(new List<string> { "Confirm", "Cancel" }, idx =>
                {
                    if (idx == 0)
                    {
                        // confirm
                        TryConfirmSelection();
                    }
                    else
                    {
                        // cancel
                        CancelSelection();
                    }
                }, false);
            }
        }

        private void TryConfirmSelection()
        {
            // validate with rules engine
            var matches = Rules51.GetMatchingMovesFromSelection(turnController.GameState, 0, selectionPlayedCard, selectionTableCards);
            if (matches.Count == 1)
            {
                // execute move (only now play animation / apply)
                turnController.ExecuteMove(matches[0]);
                // clear visuals after successful execution
                ClearArrowsAndHighlights();
                CancelSelection();
                return;
            }
            else if (matches.Count > 1)
            {
                // present alternatives
                if (moveSelectionUI != null)
                {
                    var desc = matches.Select(m => m.ToString()).ToList();
                    moveSelectionUI.ShowMoves(desc, idx =>
                    {
                        if (idx >= 0 && idx < matches.Count)
                        {
                            turnController.ExecuteMove(matches[idx]);
                        }
                        CancelSelection();
                    }, false, hoveredIndex => HighlightAlternative(matches, hoveredIndex, activeCardViews.ContainsKey(selectionPlayedCard) ? activeCardViews[selectionPlayedCard] : null));
                }
                else
                {
                    turnController.ExecuteMove(matches[0]);
                    CancelSelection();
                }
                return;
            }

            // no matches
            if (moveSelectionUI != null)
            {
                // Deselect currently selected table cards and keep selection mode active so player can retry
                foreach (var c in selectionTableCards.ToList())
                {
                    if (activeCardViews.TryGetValue(c, out var v)) v.SetSelected(false);
                }
                selectionTableCards.Clear();

                // Show help markers on first invalid confirm, otherwise show an invalid message
                var allMatches = Rules51.GetMatchingMovesFromSelection(turnController.GameState, 0, selectionPlayedCard, null);
                if (!helpShownForCurrentSelection && allMatches.Count > 0)
                {
                    helpShownForCurrentSelection = true;
                    HighlightAlternative(allMatches, 0, null);
                }
                else
                {
                    moveSelectionUI.ShowInvalid("Invalid selection");
                }
            }
        }

        private void CancelSelection()
        {
            isSelecting = false;
            selectionPlayedCard = null;
            selectionTableCards.Clear();

            // reset visuals and interactivity
            foreach (var kv in activeCardViews)
            {
                var v = kv.Value;
                if (v == null) continue;
                v.SetSelected(false);
            }
            
            // Disable interactivity for table cards when exiting selection mode
            UpdateTableCardsInteractivity();

            if (moveSelectionUI != null)
                moveSelectionUI.Hide();
        }

        // Helper to retrieve a sprite for a suit/rank, used by Matta special visual.
        private Sprite GetSpriteForRank(int rank, Suit suit)
        {
            var tmp = new Card(suit, rank);
            return GetSpriteForCard(tmp);
        }
    }
}
