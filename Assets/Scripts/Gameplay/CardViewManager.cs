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

        // G1 - Dimensioni delle carte in pixel del mockup 09 (altezza della carta), non piu' scale fisse:
        // le scale 2,2 / 1,1 erano tarate su sprite molto piu' piccoli e con i mazzi attuali (1,8 unita'
        // di altezza) la mano copriva il 40% del tavolo. Con l'area di design di CameraResponsiveFit un
        // pixel di mockup vale sempre DesignWorldHeight / 1920 unita', su ogni schermo e con ogni mazzo.
        private const float LocalHandCardDesignHeight = 272f;
        private const float TableCardDesignHeight = 157f;
        private const float WorldPerMockupPixel = CameraResponsiveFit.DesignWorldHeight / CameraResponsiveFit.DesignHeightPx;

        // UI51 Fase 5, tavolo 1v1 (mockup Partita, largo 390): misure del mockup in unita' x MockupUnit = pixel di design.
        // Fase 6 (Partita4): la mia mano e la fascia del tavolo usano le stesse misure anche nei 4 giocatori.
        private const float MockupUnit = 1080f / 390f; // = UI51TableBuilder.Unit, che Gameplay non vede
        private const float DuelHandCardHeight = 143f * MockupUnit, DuelHandCardWidth = 92f * MockupUnit, DuelHandStep = 99f * MockupUnit;
        private const float HandBottomAboveBanner = 58f * MockupUnit; // la mano finisce 33 sopra al banner (centro + 25 + 33)
        private const float DuelTopHandBelowBanner = 74.5f * MockupUnit, DuelTopHandStep = 56f * MockupUnit, DuelOpponentCardHeight = 71f * MockupUnit;
        // Fascia del tavolo tra le due mani. Sotto: 10 oltre la carta propria selezionata (CardView: +0,35 mondo = 24,27 e x1,12 = 8,58).
        private const float DuelTableGapTop = 10f * MockupUnit, DuelTableGapBottom = 43f * MockupUnit, DuelTableAreaWidth = 340f; // 6 dal feltro: la carta scelta (x1,12) resta dentro al filo d'oro
        private const float SideTableGap = 16f * MockupUnit; // Partita4: la fascia comincia 16 sotto ai banner laterali
        private const float DuelTableCardLift = 4f * MockupUnit; // mockup: la carta del tavolo scelta sale di 4
        private const float TableRowGap = 12f; // tra le righe: due carte scelte una sopra l'altra (x1,12 e +4) non si toccano
        private const float AccusedHandShift = 6f * MockupUnit; // Partita4: mano scoperta 6 piu' fuori dal banner (ai lati a 74 invece di 68)
        private const float DuelRevealedCardHeight = 80f * MockupUnit, DuelRevealedStep = 60f * MockupUnit; // Partita: accusate 52x80 a passo 60
        // Bordo #F3C969 delle carte accusate a 4 giocatori: spessore / larghezza della carta (Partita4 1,5 su 20). Nel 1v1 niente bordo (scelta utente 01/10).
        private const float AccusedOutline = 1.5f / 20f;
        private static readonly Color AccusedGold = new Color32(0xF3, 0xC9, 0x69, 0xFF);

        /// <summary>Tre assi: giocatore la cui mano si vede scoperta col bordo d'oro, -1 nessuno (lo imposta TurnController).</summary>
        public int RevealedHandPlayer { get; set; } = -1;
        // 4 giocatori su schermi bassi (iPhone SE): sotto 150 di fascia la mano torna alta 272 come prima della Fase 6.
        private const float SmallHandMinBand = 150f, SmallHandScale = LocalHandCardDesignHeight / DuelHandCardHeight;

        private bool IsDuel => turnController != null && turnController.GameState != null && turnController.GameState.NumPlayers == 2;

        /// <summary>Altezza della mia mano rispetto ai 143 del mockup: 1, oppure SmallHandScale nei 4 giocatori se il tavolo resterebbe basso
        /// (anche senza spazio: fascia negativa). Senza stato o banner la fascia e' 0 e la mano resta intera.</summary>
        private float OwnHandScale => IsDuel || (!TryGetTableBand(1f, out _, out float band) && band == 0f) || band >= SmallHandMinBand ? 1f : SmallHandScale;

        private float EffectiveLocalPlayerCardScale => TryGetBannerRect(0, out _)
            ? ScaleForDesignBox(DuelHandCardHeight * OwnHandScale, DuelHandCardWidth * OwnHandScale)
            : ScaleForDesignHeight(LocalHandCardDesignHeight);
        private float EffectiveOpponentCardScale => ScaleForDesignHeight(opponentCardHeight);
        private float EffectiveTableCardScale => ScaleForDesignHeight(TableCardDesignHeight);

        private static float ScaleForDesignHeight(float designHeight)
        {
            var back = CardDecks.LoadForMatch()?.Back;
            float spriteHeight = back != null ? back.bounds.size.y : 1.8f;
            return Mathf.Max(0.05f, designHeight * WorldPerMockupPixel / Mathf.Max(0.01f, spriteHeight));
        }

        /// <summary>Come ScaleForDesignHeight, ma senza superare designWidth: i mazzi piu' larghi del mockup (classico, giada).</summary>
        private static float ScaleForDesignBox(float designHeight, float designWidth)
        {
            var back = CardDecks.LoadForMatch()?.Back;
            float h = back != null ? back.bounds.size.y : 1.8f, w = back != null ? back.bounds.size.x : 1.16f;
            return Mathf.Max(0.05f, Mathf.Min(designHeight / Mathf.Max(0.01f, h), designWidth / Mathf.Max(0.01f, w)) * WorldPerMockupPixel);
        }

        /// <summary>Scala delle carte del tavolo quando ce ne sono totalCards (nel 1v1 si stringono per starci).</summary>
        private float TableCardScale(int totalCards) =>
            TryTableLayout(totalCards, 0, out _, out float scale) ? scale : EffectiveTableCardScale;

        public float GetTableCardScale(int totalCards = 1) => TableCardScale(totalCards);

        /// <summary>
        /// Scala carta-in-mano per un giocatore specifico: quella del giocatore locale
        /// (piu' grande) o quella condivisa dagli avversari, a seconda di chi e' playerIndex.
        /// </summary>
        public float GetHandCardScale(int playerIndex) =>
            GameModeService.Current.IsLocalPlayer(playerIndex) ? EffectiveLocalPlayerCardScale : EffectiveOpponentCardScale;

        [Header("Sprites (Optional)")]
        [SerializeField] private Sprite[] cardSprites; // shared with CardSpriteProvider
        [SerializeField] private Sprite defaultCardBack;
        [SerializeField] private bool enableSpriteDebug = false;
        [SerializeField] private CardSpriteMapping[] explicitMappings;
        [Header("Matta")]
        [Tooltip("Alone dietro alla matta quando vale come un'altra carta (Bagliore morbido cerchio). Assegnato da Tools/UIV2/Build Accuso Window.")]
        [SerializeField] private Sprite mattaHaloSprite;
        [Header("Suggerimenti mosse")]
        [Tooltip("Bagliore dietro alle carte in mano che fanno una presa (Bagliore morbido cerchio). Assegnato da Tools/UIV2/Build In-Game Settings.")]
        [SerializeField] private Sprite moveHintGlowSprite;
        [Header("UI")]
        [SerializeField] private MoveSelectionUI moveSelectionUI;
        [Header("Feedback")]
        [SerializeField] private AudioClip playSound;
        [SerializeField] private float playSoundVolume = 0.7f;

        private Dictionary<string, Sprite> spriteLookup = new Dictionary<string, Sprite>();
        private Dictionary<(Suit suit, int rank), Sprite> explicitMapCache = new Dictionary<(Suit, int), Sprite>();

        private Dictionary<Card, CardView> activeCardViews = new Dictionary<Card, CardView>();

        /// <summary>
        /// Quando true, i render pass NON riaccendono il renderer delle carte (vedi i punti
        /// "Difensivo" in RenderTableCards/RenderHumanHand/RenderAIHandsDynamic). Serve a
        /// TurnController per tenere le carte nascoste durante l'animazione di distribuzione dal
        /// mazziere: un semplice SetAllCardRenderersVisible(false) chiamato dall'esterno non basta,
        /// perche' un QUALSIASI ForceRefresh successivo (incluso quello interno di
        /// CardViewManager.Start(), il cui ordine rispetto agli Start() altrui non e' garantito)
        /// le riaccende subito tramite quei punti difensivi, prima ancora che Unity disegni un
        /// frame - bug segnalato piu' volte ("non sono invisibili").
        /// </summary>
        private bool suppressNewCardVisibility = false;

        /// <summary>
        /// Come suppressNewCardVisibility ma per le carte sul tavolo. Separato perche' nei redeal a
        /// meta' smazzata arrivano solo carte nuove in mano: prima la sospensione valeva per tutto
        /// e il tavolo spariva dall'ultima carta del giro fino alla fine della finestra Accuso.
        /// </summary>
        private bool suppressTableCardVisibility = false;

        public void SetSuppressNewCardVisibility(bool suppress, bool includeTable = true)
        {
            suppressNewCardVisibility = suppress;
            suppressTableCardVisibility = suppress && includeTable;
        }

        /// <summary>
        /// Sposta FISICAMENTE le carte indicate sulla posizione del mazziere a scala quasi zero,
        /// catturando prima posizione/scala finale (quella appena calcolata da ForceRefresh) per
        /// poterle far rientrare in animazione. Piu' robusto di un semplice toggle di visibilita':
        /// anche se qualcos'altro dovesse riaccenderne il renderer per un motivo che non e' stato
        /// possibile isolare (bug segnalato piu' volte: "si vedono ancora"), la carta sarebbe
        /// comunque minuscola e sovrapposta dal lato del mazziere, non "gia' distribuita in mano".
        /// </summary>
        public List<StagedCard> StageCardsAtOriginForDealAnimation(IReadOnlyList<CardView> views, Vector3 originPosition)
        {
            var staged = new List<StagedCard>();
            if (views == null) return staged;

            foreach (var view in views)
            {
                if (view == null) continue;
                var t = view.transform;
                staged.Add(new StagedCard(view, t.position, t.localScale));
                t.position = originPosition;
                t.localScale = t.localScale * 0.001f;
            }
            return staged;
        }

        public readonly struct StagedCard
        {
            public readonly CardView View;
            public readonly Vector3 FinalPosition;
            public readonly Vector3 FinalScale;

            public StagedCard(CardView view, Vector3 finalPosition, Vector3 finalScale)
            {
                View = view;
                FinalPosition = finalPosition;
                FinalScale = finalScale;
            }
        }

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
        private List<Card> currentlyHighlightedCards = new List<Card>();
        private Card chooserCard; // carta della mano che ha aperto il vassoio della presa
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

        /// <summary>
        /// Renders non-local players' hands around the table (face-down by default).
        /// Dealer 15/30 accuso (1-2 points) does NOT reveal cards; Cirulla/Decino and Tre assi do.
        /// </summary>
        private void RenderAIHandsDynamic(List<PlayerState> players, int localIndex)
        {
            for (int p = 0; p < players.Count; p++)
            {
                if (p == localIndex) continue;
                var hand = players[p].Hand;

                bool accused = players[p].AccusiPoints > 0;
                bool treAssi = p == RevealedHandPlayer;
                bool hasAccuso = accused || treAssi; // mano scoperta
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

                    // La CardView per questa carta puo' gia' esistere da PRIMA che l'accuso fosse
                    // dichiarato (creata face-down quando la mano e' stata distribuita): il parametro
                    // faceUp di CreateCardView sopra si applica SOLO alla creazione, quindi senza
                    // questo controllo la carta restava coperta per sempre anche dopo un Cirulla/
                    // Decino valido, perche' il ramo "if (!activeCardViews.ContainsKey(card))" veniva
                    // saltato. Stesso identico pattern gia' usato in RenderTableCards per "flippare"
                    // una carta gia' esistente quando passa da mano a tavolo.
                    if (faceUp && !cardView.IsFaceUp)
                    {
                        var faceSprite = GetSpriteForCard(card);
                        if (faceSprite != null)
                        {
                            cardView.FlipToFaceUp(faceSprite);
                        }
                    }
                    else if (!faceUp && cardView.IsFaceUp)
                    {
                        // Vista riusata da una smazzata precedente (era in mano mia o sul tavolo):
                        // in mano a un avversario deve tornare coperta.
                        cardView.FlipToFaceDown();
                        cardView.ClearMattaTransform();
                        cardView.SetMoveHint(false, null);
                    }

                    // Difensivo: stesso motivo di RenderTableCards.
                    if (cardView.CardRenderer != null)
                    {
                        cardView.CardRenderer.enabled = !suppressNewCardVisibility;
                    }

                    cardView.SetRaiseOverride(-1f); // la vista puo' arrivare dal tavolo (resync, rivincita)
                    // Scoperte da un accuso: bordo d'oro solo a 4 giocatori (Partita4), nel 1v1 carte normali; al tocco il visore
                    // "Carte accusate da".
                    // I Tre assi hanno il bordo anche nel 1v1 (mockup TreAssi).
                    cardView.SetOutline(AccusedGold, treAssi || (accused && players.Count != 2) ? AccusedOutline : 0f);
                    cardView.SetGlow(false, null, default);
                    cardView.Tapped = accused ? new System.Action<CardView>(OnAccusedCardTapped) : null;
                    Vector3 position;
                    float baseRotation = 0f;

                    int numPlayers = players.Count;
                    int relative = (p - localIndex + numPlayers) % numPlayers;
                    int slot = SeatSlot(relative, numPlayers);
                    if (TryGetBannerHandCenter(slot, out var handCenter, out _))
                    {
                        // Mockup 09: carte piccole sotto al banner in alto, coricate sul bordo ai lati.
                        // 1v1 (mockup Partita): dorsi dritti da 71 ogni 56 sotto al banner avversario.
                        bool duel = numPlayers == 2;
                        bool revealed = duel && hasAccuso;
                        cardView.SetDisplayScale(CompactOpponentScale(cardView, revealed ? DuelRevealedCardHeight : duel ? DuelOpponentCardHeight : opponentCardHeight));
                        float k = WorldPerDesignPixel();
                        float t = FanT(hand.Count, i);
                        float centered = i - (hand.Count - 1) / 2f;
                        if (slot == 2)
                        {
                            float fan = duel ? 0f : topHandFanDegrees;
                            float drop = !duel && hasAccuso ? AccusedHandShift * k : 0f;
                            position = handCenter + Vector3.right * (centered * (revealed ? DuelRevealedStep : duel ? DuelTopHandStep : topHandStep) * k)
                                + Vector3.down * drop;
                            // Partita: le carte accusate in alto sono dritte, da leggere; i dorsi restano capovolti.
                            cardView.transform.rotation = Quaternion.Euler(0, 0, hasAccuso ? 0f : 180f + Mathf.Lerp(fan, -fan, t));
                        }
                        else
                        {
                            // Partita4: coricate, la testa verso il tavolo come quelle in alto; scoperte da un accuso
                            // escono di 6 in piu' da sotto al banner.
                            float inward = (hasAccuso ? AccusedHandShift : 0f) * k * (slot == 1 ? 1f : -1f);
                            position = handCenter + Vector3.down * (centered * sideHandStep * k) + Vector3.right * inward;
                            cardView.transform.rotation = Quaternion.Euler(0, 0, slot == 1 ? -90f : 90f);
                        }
                        cardView.SetPosition(position);
                        cardView.SetBaseSortingOrder(20 + i);
                        cardView.IsClickable = false;
                        cardView.EnableHover = accused;
                        continue;
                    }

                    cardView.SetDisplayScale(EffectiveOpponentCardScale);

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
                    cardView.EnableHover = accused;
                }

                if (hasAccuso)
                {
                    // Con 3 carte mostra la matta trasformata, dopo la prima giocata torna un 7 normale.
                    ApplyMattaSpecialVisual(hand);
                }
            }
        }

        /// <summary>Tocco sulle carte scoperte da un accuso: indice assoluto di chi le ha (PlayerBannerManager apre il visore).</summary>
        public static event System.Action<int> AccusedHandTapped;

        private void OnAccusedCardTapped(CardView view)
        {
            var players = turnController != null && turnController.GameState != null ? turnController.GameState.Players : null;
            if (players == null) return;
            // La vista puo' essere gia' passata al tavolo con il delegato ancora attaccato.
            int owner = players.FindIndex(pl => pl.AccusiPoints > 0 && pl.Hand.Contains(view.Card));
            if (owner >= 0 && owner != GameModeService.Current.LocalPlayerIndex) AccusedHandTapped?.Invoke(owner);
        }

        /// <summary>
        /// Gets the sprite for a specific card. Public for use by other UI components.
        /// </summary>
        /// <summary>
        /// Carica cardSprites da Resources/Cards se non ancora popolato (assegnato in Inspector
        /// o gia' caricato in precedenza). Prima questo accadeva SOLO dentro Start(): se un
        /// chiamante esterno (es. TurnController.StartNewGame(), chiamato direttamente da
        /// GameSceneInitializer PRIMA che Start() di questo componente sia mai girato) invocava
        /// ForceRefresh() abbastanza presto, cardSprites era ancora vuoto - le carte create in
        /// quel momento restavano bloccate con lo sprite placeholder per sempre, perche' il
        /// render delle carte in mano (a differenza di tavolo/avversari, che ri-controllano lo
        /// sprite ad ogni refresh per gestire il flip face-down->face-up) non lo ricontrolla piu'
        /// una volta creata la view (bug segnalato: "le mie carte sono rettangoli piccoli senza
        /// immagine, le altre sono normali"). GetSpriteForCard ora e' autosufficiente indipendentemente
        /// da quando/da chi viene chiamato per primo.
        /// </summary>
        private void EnsureCardSpritesLoaded()
        {
            if (cardSprites != null && cardSprites.Length > 0) return;

            var loaded = Resources.LoadAll<Sprite>("Cards");
            if (loaded != null && loaded.Length > 0)
            {
                cardSprites = loaded;
            }
        }

        private CardDeckDefinition matchDeck;
        private CardDeckDefinition MatchDeck => matchDeck != null ? matchDeck :
            (matchDeck = CardDecks.LoadForMatch());

        public Sprite GetSpriteForCard(Card card)
        {
            var selectedFace = MatchDeck != null ? MatchDeck.GetFace(card) : null;
            if (selectedFace != null) return selectedFace;
            // 1) Do NOT rely on array index ordering; many packs are unordered.
            // Prefer explicit mappings or name-based resolution.

            EnsureCardSpritesLoaded();

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
            EnsureCardSpritesLoaded();

            // Build explicit mapping cache from inspector entries
            BuildExplicitMapCache();

            GamePreferences.Changed += OnGamePreferencesChanged;

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

            // Area di design (CameraResponsiveFit), non tutto il visibile: il layout resta identico al
            // mockup su ogni proporzione, lo spazio in piu' rimane attorno.
            if (layoutCamera != null && layoutCamera.orthographic)
            {
                return Mathf.Max(1f, Mathf.Min(layoutCamera.orthographicSize * 2f * layoutCamera.aspect, CameraResponsiveFit.DesignWorldSize.x));
            }

            return 12f;
        }

        private float GetVisibleHeight()
        {
            ApplyResponsiveCameraIfAvailable();

            if (layoutCamera != null && layoutCamera.orthographic)
            {
                return Mathf.Max(1f, Mathf.Min(layoutCamera.orthographicSize * 2f, CameraResponsiveFit.DesignWorldSize.y));
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
            EnsureLayoutCameraCached();
            if (TryGetBannerHandCenter(SeatSlot(relativePlayerIndex, playerCount), out var bannerHandCenter, out _))
            {
                return bannerHandCenter;
            }

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

        #region Layout ancorato ai banner (mockup 09_tavolo_v4)

        // Tutte le misure sono pixel del mockup 1080x1920 (y verso il basso), relative al centro
        // del banner del posto. Mani e mazzetti seguono i banner veri della scena: se un banner
        // viene spostato, carte e prese lo seguono senza toccare questi numeri.
        // Questi campi valgono per le mani degli altri nei 4 giocatori (scritti da UI51TableBuilder, mockup Partita4);
        // il 1v1 usa le misure Duel* del mockup Partita, la mia mano sempre (OwnHandScale).
        [Header("Layout tavolo ancorato ai banner (mockup 09_tavolo_v4)")]
        [SerializeField] private bool anchorLayoutToBanners = true;
        [SerializeField] private float topHandBelowBanner = 128f;
        [SerializeField] private float topHandStep = 65f;
        [SerializeField] private float topHandFanDegrees = 8f;
        [SerializeField] private float sideHandBelowBanner = 182f;
        [SerializeField] private float sideHandInsetFromBannerEdge = 78f; // 2.23: 48 toccava la cornice in legno su iPhone 12
        [SerializeField] private float sideHandStep = 60f;
        [SerializeField] private float opponentCardHeight = 80f;

        private static readonly string[] BannerNamesBySlot = { "Banner_Local", "Banner_Left", "Banner_Top", "Banner_Right" };
        private static readonly Vector2[] CapturedPileOffsetsBySlot =
        {
            new Vector2(194.4f, 0f),  // locale: la pastiglia prese del banner UI51 (misurata in Play, Fase 5)
            new Vector2(0f, 85.8f),   // sinistra: la pastiglia in fondo al banner verticale (misurata in Play, Fase 6)
            new Vector2(5.5f, 24.9f), // alto: la pastiglia prese sotto al nome
            new Vector2(0f, 85.8f),   // destra: come a sinistra
        };

        private readonly RectTransform[] bannerRects = new RectTransform[4];
        private Canvas bannerCanvas;
        private float nextBannerLookupTime;

        /// <summary>Posto visivo 0=locale, 1=sinistra, 2=alto, 3=destra (in 1v1 l'avversario sta in alto).</summary>
        public static int SeatSlot(int relativePlayerIndex, int playerCount)
        {
            return playerCount == 2 && relativePlayerIndex == 1 ? 2 : relativePlayerIndex;
        }

        private bool TryGetBannerRect(int slot, out RectTransform rect)
        {
            rect = null;
            if (!anchorLayoutToBanners || slot < 0 || slot > 3) return false;
            EnsureLayoutCameraCached();

            if (bannerRects[slot] == null && Time.unscaledTime >= nextBannerLookupTime)
            {
                nextBannerLookupTime = Time.unscaledTime + 1f;
                var canvasObject = GameObject.Find("GameCanvas");
                var banners = canvasObject != null ? canvasObject.transform.Find("PlayerBanners") : null;
                if (banners != null)
                {
                    bannerCanvas = canvasObject.GetComponent<Canvas>();
                    for (int i = 0; i < BannerNamesBySlot.Length; i++)
                    {
                        bannerRects[i] = banners.Find(BannerNamesBySlot[i]) as RectTransform;
                    }
                }
            }

            rect = bannerRects[slot];
            return rect != null && bannerCanvas != null && layoutCamera != null;
        }

        /// <summary>Unita' mondo per pixel del mockup, alla risoluzione corrente.</summary>
        private float WorldPerDesignPixel()
        {
            float screenPerDesign = bannerCanvas != null ? bannerCanvas.scaleFactor : 1f;
            float worldPerScreen = layoutCamera != null && layoutCamera.orthographic
                ? layoutCamera.orthographicSize * 2f / Mathf.Max(1f, layoutCamera.pixelHeight)
                : 0.01f;
            return screenPerDesign * worldPerScreen;
        }

        private static readonly Vector3[] BannerCorners = new Vector3[4]; // tavolo e mazzo la chiamano a ogni frame

        private Vector3 BannerCenterWorld(RectTransform rect)
        {
            var corners = BannerCorners;
            rect.GetWorldCorners(corners);
            Vector3 screenCenter = (corners[0] + corners[2]) * 0.5f;
            Vector3 world = layoutCamera.ScreenToWorldPoint(screenCenter);
            world.z = 0f;
            return world;
        }

        private Vector3 BannerOffsetWorld(RectTransform rect, Vector2 designOffset)
        {
            float k = WorldPerDesignPixel();
            return BannerCenterWorld(rect) + new Vector3(designOffset.x * k, -designOffset.y * k, 0f);
        }

        /// <summary>
        /// Posizione mondo del mazzetto prese del posto (accanto al banner, come nel mockup).
        /// E' anche il punto d'arrivo delle carte catturate.
        /// </summary>
        public bool TryGetCapturedPileWorldPosition(int relativePlayerIndex, int playerCount, out Vector3 position)
        {
            EnsureLayoutCameraCached();
            int slot = SeatSlot(relativePlayerIndex, playerCount);
            position = Vector3.zero;
            if (!TryGetBannerRect(slot, out var rect)) return false;
            position = BannerOffsetWorld(rect, CapturedPileOffsetsBySlot[slot]);
            return true;
        }

        /// <summary>Offset del mazzetto prese rispetto al centro del banner, in pixel mockup (y verso il basso).</summary>
        public static Vector2 GetCapturedPileDesignOffset(int slot)
        {
            return CapturedPileOffsetsBySlot[Mathf.Clamp(slot, 0, 3)];
        }

        private bool TryGetBannerHandCenter(int slot, out Vector3 center, out RectTransform rect)
        {
            center = Vector3.zero;
            if (!TryGetBannerRect(slot, out rect)) return false;

            float k = WorldPerDesignPixel();
            Vector3 bannerCenter = BannerCenterWorld(rect);
            float screenCenterX = GetTableCenterPosition().x;
            switch (slot)
            {
                case 0:
                    center = new Vector3(screenCenterX, bannerCenter.y + (HandBottomAboveBanner + DuelHandCardHeight * OwnHandScale * 0.5f) * k, 0f);
                    break;
                case 2:
                    center = new Vector3(screenCenterX, bannerCenter.y - (IsDuel ? DuelTopHandBelowBanner : topHandBelowBanner) * k, 0f);
                    break;
                default:
                    var corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    float edgeScreenX = slot == 1 ? corners[0].x : corners[2].x;
                    float edgeWorldX = layoutCamera.ScreenToWorldPoint(new Vector3(edgeScreenX, 0f, 0f)).x;
                    float x = edgeWorldX + (slot == 1 ? 1f : -1f) * sideHandInsetFromBannerEdge * k;
                    center = new Vector3(x, bannerCenter.y - sideHandBelowBanner * k, 0f);
                    break;
            }
            return true;
        }

        private float CompactOpponentScale(CardView view, float designHeight)
        {
            var sprite = view != null && view.CardRenderer != null ? view.CardRenderer.sprite : null;
            float spriteHeight = sprite != null ? sprite.bounds.size.y : 1.8f;
            return Mathf.Max(0.05f, designHeight * WorldPerDesignPixel() / Mathf.Max(0.01f, spriteHeight));
        }

        private static float FanT(int totalCards, int cardIndex)
        {
            return totalCards > 1 ? (float)cardIndex / (totalCards - 1) : 0.5f;
        }

        /// <summary>Carta centrale davanti, poi a scalare verso i bordi (mockup: la centrale copre le laterali).</summary>
        private static int CenterFirstSortingOrder(int baseOrder, int totalCards, int cardIndex)
        {
            float center = (totalCards - 1) * 0.5f;
            // A pari distanza dal centro vince la carta piu' a destra, cosi' l'ordine non e' mai casuale.
            return baseOrder + Mathf.RoundToInt((totalCards - Mathf.Abs(cardIndex - center)) * 2f) + (cardIndex > center ? 1 : 0);
        }

        #endregion

        /// <summary>
        /// n carte in un'area areaW x areaH (unita' del mockup Partita): larghe 70 fino a 4, 60 fino a 8, poi 54; spazio 10,
        /// 8 oltre le 8 (tra le righe sempre TableRowGap); righe bilanciate (7 = 4 + 3), al massimo 4 per riga fino a 8 carte e 5 oltre. Se l'altezza non basta
        /// prova piu' colonne (fino a 8) e tiene la carta piu' larga; a pari larghezza meno colonne. Alta 1,55 volte la larghezza.
        /// fourPlayers (Partita4): larghe al massimo 62, spazio 8, si prova da una colonna (4 carte = una riga da 62).
        /// </summary>
        public static void TableGrid(int n, float areaW, float areaH, out int columns, out int rows, out float cardW, out float gap,
            bool fourPlayers = false)
        {
            columns = rows = 0;
            cardW = 0f;
            gap = fourPlayers || n > 8 ? 8f : 10f;
            if (n <= 0) return;
            float wMax = fourPlayers ? 62f : n <= 4 ? 70f : n <= 8 ? 60f : 54f, best = float.NegativeInfinity;
            for (int c = fourPlayers ? 1 : Mathf.Min(n, n <= 8 ? 4 : 5); c <= Mathf.Min(n, 8); c++)
            {
                int r = (n + c - 1) / c, cols = (n + r - 1) / r; // righe bilanciate: mai una carta sola sotto una riga piena
                float w = Mathf.Min(wMax, (areaW - (cols - 1) * gap) / cols, (areaH - (r - 1) * TableRowGap) / (r * 1.55f));
                if (w > best + 0.01f) { best = w; columns = cols; rows = r; }
                if (w >= wMax) break;
            }
            cardW = Mathf.Max(1f, Mathf.Floor(best));
        }

        /// <summary>
        /// Fascia delle carte in tavola con la mia mano alta DuelHandCardHeight x handScale: centro in mondo, altezza in unita'
        /// del mockup. Sotto: 43 sopra alla mia mano. Sopra: 1v1 10 sotto la mano avversaria, 4 giocatori 16 sotto ai banner laterali.
        /// </summary>
        private bool TryGetTableBand(float handScale, out Vector3 center, out float heightU)
        {
            center = Vector3.zero;
            heightU = 0f;
            var state = turnController != null ? turnController.GameState : null;
            if (state == null || !TryGetBannerRect(0, out var own)) return false;
            float k = WorldPerDesignPixel();
            float bandTop;
            if (state.NumPlayers == 2)
            {
                if (!TryGetBannerHandCenter(2, out var top, out _)) return false;
                bandTop = top.y - (DuelOpponentCardHeight * 0.5f + DuelTableGapTop) * k;
            }
            else
            {
                if (!TryGetBannerRect(1, out var side)) return false;
                side.GetWorldCorners(BannerCorners);
                bandTop = layoutCamera.ScreenToWorldPoint(BannerCorners[0]).y - SideTableGap * k;
            }
            float bandBottom = BannerCenterWorld(own).y + (HandBottomAboveBanner + DuelHandCardHeight * handScale + DuelTableGapBottom) * k;
            heightU = (bandTop - bandBottom) / (MockupUnit * k);
            center = new Vector3(GetTableCenterPosition().x, (bandTop + bandBottom) * 0.5f, 0f);
            return heightU > 0f;
        }

        /// <summary>
        /// Bordo esterno del tavolo (mockup Partita: x 10..380, 17 sopra il centro del banner in alto, 97 sotto quello proprio).
        /// Mondo con y in su: yMax e' il bordo alto. unit = mondo per unita' del mockup.
        /// </summary>
        public static Rect TableRim(float centerX, float topBannerY, float ownBannerY, float unit) =>
            Rect.MinMaxRect(centerX - 185f * unit, ownBannerY + 97f * unit, centerX + 185f * unit, topBannerY - 17f * unit);

        /// <summary>Centro del mazzo: 42 dentro il bordo sinistro, 32 sopra quello alto (il cuscino poggia sul bordo).</summary>
        public static Vector2 DeckCenter(Rect rim, float unit) => new Vector2(rim.xMin + 42f * unit, rim.yMax + 32f * unit);

        /// <summary>Bordo del tavolo dai banner mio e in alto (1v1 e 4 giocatori). False senza banner.</summary>
        public bool TryGetTableRim(out Rect rim, out float unit)
        {
            rim = default;
            unit = 0f;
            if (!TryGetBannerRect(0, out var own) || !TryGetBannerRect(2, out var top)) return false;
            unit = MockupUnit * WorldPerDesignPixel();
            rim = TableRim(GetTableCenterPosition().x, BannerCenterWorld(top).y, BannerCenterWorld(own).y, unit);
            return unit > 0f && rim.height > 0f;
        }

        /// <summary>Centro del mazzo sul cuscino (mondo): da qui partono le carte distribuite.</summary>
        public bool TryGetDeckPosition(out Vector3 position, out float unit)
        {
            position = Vector3.zero;
            if (!TryGetTableRim(out var rim, out unit)) return false;
            position = DeckCenter(rim, unit);
            return true;
        }

        /// <summary>Posto e scala della carta index su total nel tavolo (TableGrid nella fascia). False senza banner.</summary>
        private bool TryTableLayout(int total, int index, out Vector3 position, out float scale)
        {
            position = Vector3.zero;
            scale = 0f;
            if (total <= 0 || !TryGetTableBand(OwnHandScale, out var center, out float heightU)) return false;
            TableGrid(total, DuelTableAreaWidth, heightU, out _, out int rows, out float w, out float gap, !IsDuel);
            int small = total / rows, extra = total % rows; // le prime "extra" righe hanno una carta in piu'
            int row = 0, start = 0, inRow = small + (extra > 0 ? 1 : 0);
            while (index >= start + inRow && row < rows - 1)
            {
                start += inRow;
                row++;
                inRow = small + (row < extra ? 1 : 0);
            }
            float u = MockupUnit * WorldPerDesignPixel();
            float x = (index - start - (inRow - 1) * 0.5f) * (w + gap) * u;
            float y = ((rows - 1) * 0.5f - row) * (w * 1.55f + TableRowGap) * u;
            position = center + new Vector3(x, y, 0f);
            scale = ScaleForDesignBox(w * 1.55f * MockupUnit, w * MockupUnit);
            return true;
        }

        private Vector3 CalculateTableCardPosition(int totalCards, int cardIndex)
        {
            if (TryTableLayout(totalCards, cardIndex, out var gridPosition, out _)) return gridPosition;
            Vector3 center = GetTableCenterPosition();
            float spacing = GetResponsiveHorizontalSpacing(totalCards, minTableCardSpacing, tableWidthUsage) * EffectiveTableCardScale;
            float offset = (cardIndex - (totalCards - 1) / 2f) * spacing;
            return center + Vector3.right * offset;
        }

        /// <summary>
        /// Wrapper pubblico di CalculateTableCardPosition. Serve a chi deve posizionare carte
        /// "fantasma" (non presenti nel vero gameState.Table) sulle stesse coordinate delle carte
        /// tavolo reali - es. il reveal dell'accuso del dealer, che mostra carte gia' rimosse da
        /// gameState.Table (RoundManager le processa prima che qualunque render esista).
        /// </summary>
        public Vector3 GetTableCardPosition(int totalCards, int cardIndex)
        {
            return CalculateTableCardPosition(totalCards, cardIndex);
        }

        /// <summary>
        /// Crea una CardView "fantasma": stesso identico visual delle carte tavolo reali, ma NON
        /// registrata in activeCardViews e non interattiva - solo per animazioni una tantum (es.
        /// il reveal dell'accuso del dealer). Il chiamante e' responsabile di distruggerla con
        /// view.DestroyView() quando ha finito.
        /// </summary>
        public CardView SpawnGhostCardView(Card card, Vector3 position, int totalCards = 1)
        {
            var view = CreateCardView(card, faceUp: true, clickable: false);
            if (view == null) return null;

            view.SetDisplayScale(TableCardScale(totalCards));
            view.SetBaseSortingOrder(10); // come le carte del tavolo: sopra al mazzo (5) da cui partono
            view.SetPosition(position);
            view.transform.rotation = Quaternion.identity;
            // Nascosta finche' parte il suo volo (PlayDealtCardsFromOrigin la accende a 0,12x): ferma sul mazzo si vedrebbe intera.
            if (view.CardRenderer != null) view.CardRenderer.enabled = false;
            return view;
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
        /// Le carte del tavolo scivolano nei posti che avranno dopo la mossa (cards = quelle che restano, in ordine;
        /// totalCards = quante saranno), mentre la mossa e' ancora in volo. Prima restavano ferme e scattavano al refresh:
        /// la carta giocata atterrava a meta' su quella centrale e, con l'ultima carta della mano, ci restava fino alla
        /// distribuzione seguente (utente, 01/10).
        /// </summary>
        public void GlideTableCards(IReadOnlyList<Card> cards, int totalCards, float duration)
        {
            for (int i = 0; i < cards.Count; i++)
                if (activeCardViews.TryGetValue(cards[i], out var view) && view != null)
                    view.GlideTo(CalculateTableCardPosition(totalCards, i), TableCardScale(totalCards), duration);
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

            // Il pannello di scelta presa non deve restare aperto se il turno e' passato.
            if (moveSelectionUI != null && moveSelectionUI.IsVisible && !IsMyTurnToPlay)
            {
                moveSelectionUI.Cancel(); // anche le carte sollevate tornano giu'
            }

            // Refresh card views

            // Clear old views that are no longer in the game
            CleanupOldViews(state);

            // Render table cards
            RenderTableCards(state.Table);

            // Determina l'indice del player locale tramite GameModeService (fonte di verita' reale,
            // popolata da GameSceneInitializer sia in training che in multiplayer). In precedenza
            // usava reflection su "Project51.Unity.GameManager, Project51.Networking": quell'assembly
            // non esiste piu' (asmdef rimosso), quindi la lookup falliva sempre e localIndex restava
            // sempre 0 - ogni client renderizzava la mano del player 0 come "la propria mano",
            // motivo per cui in multiplayer tutti i client vedevano le stesse identiche carte.
            int localIndex = GameModeService.Current.LocalPlayerIndex;
            if (localIndex < 0 || localIndex >= state.Players.Count)
                localIndex = 0;

            // Render local human player's hand at bottom UI using their actual hand
            if (state.Players.Count > localIndex)
            {
                RenderHumanHand(state.Players[localIndex].Hand, RevealedHandPlayer == localIndex);
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
        /// Nasconde/mostra il renderer di TUTTE le CardView attive, senza toccare posizione o
        /// scala. Usato da TurnController per tenere le carte invisibili tra ForceRefresh() (che
        /// le crea e posiziona subito, sincrono) e l'animazione di distribuzione dal mazziere -
        /// altrimenti le carte si vedevano gia' ferme un attimo prima che partisse l'animazione
        /// (bug segnalato: "le carte appaiono gia' distribuite, poi dichiara il dealer").
        /// </summary>
        public void SetAllCardRenderersVisible(bool visible)
        {
            foreach (var view in activeCardViews.Values)
            {
                if (view == null) continue;
                var renderer = view.CardRenderer;
                if (renderer != null) renderer.enabled = visible;
            }
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
                    cardView.SetMoveHint(false, null);
                    cardView.SetOutline(default, 0f); // carta accusata appena giocata
                    cardView.Tapped = null;
                    if (card.IsMatta) cardView.ClearMattaTransform();

                    // Difensivo: una carta riposizionata sul tavolo deve sempre essere visibile,
                    // anche se un'animazione precedente aveva disabilitato il renderer (a meno che
                    // non sia sospeso apposta per l'animazione di distribuzione dal mazziere).
                    if (cardView.CardRenderer != null)
                    {
                        cardView.CardRenderer.enabled = !suppressTableCardVisibility;
                    }
                }

                Vector3 position = CalculateTableCardPosition(tableCards.Count, i);

                cardView.SetDisplayScale(TableCardScale(tableCards.Count));
                // Scelta sale di 4 come nel mockup (Partita e Partita4), non di 24 come in mano: toccherebbe la mano avversaria
                // su iPhone SE, e con la scelta della presa salgono insieme tutte le carte delle prese.
                cardView.SetRaiseOverride(DuelTableCardLift * WorldPerDesignPixel());
                cardView.SetPosition(position);
                cardView.SetBaseSortingOrder(10 + i);

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
            if (handCards == null) return;
            var matta = handCards.FirstOrDefault(c => c.IsMatta);
            if (matta == null || !activeCardViews.TryGetValue(matta, out var mattaView)) return;

            // La matta diventa un'altra carta solo quando serve per l'accuso (coppia per il Decino,
            // asso per la Cirulla): si gira con l'alone dorato. Prima c'erano vecchie immagini gialle a
            // bassa risoluzione con un quadratino bianco sopra.
            int specialRank = AccusiChecker.MattaValueForAccuso(handCards);
            if (specialRank <= 0)
            {
                mattaView.ClearMattaTransform();
                return;
            }

            var targetSprite = GetSpriteForCard(new Card(matta.Suit, specialRank));
            if (targetSprite != null)
            {
                mattaView.ShowMattaTransform(targetSprite, mattaHaloSprite);
            }
        }

        private void OnTableCardClicked(CardView tableCardView)
        {
            if (!IsMyTurnToPlay) return;

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
                    if (GamePreferences.MoveHints && !helpShownForCurrentSelection)
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

            // Niente piu' quadratini gialli sopra le carte: restavano a schermo se il pannello si
            // chiudeva senza PointerExit (tocco su mobile). Basta il sollevamento delle carte.
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

        // CreateTooltip method removed - tooltips caused memory leaks and visual clutter
        // We now rely on card bounce animations for visual feedback

        private void ClearArrowsAndHighlights()
        {
            foreach (var c in currentlyHighlightedCards)
            {
                if (activeCardViews.TryGetValue(c, out var v)) v.SetSelected(false);
            }
            currentlyHighlightedCards.Clear();
        }

        /// <summary>
        /// True solo se e' il turno del giocatore umano E quel giocatore e' il client locale.
        /// </summary>
        /// <remarks>
        /// turnController.IsHumanPlayerTurn da solo dice solo "il player di turno non e' un bot":
        /// in single-player e' equivalente a "e' il mio turno" (unico umano = seat locale), ma in
        /// multiplayer con 2+ giocatori umani reali NON lo e' - ogni client renderizzava comunque
        /// la propria mano come cliccabile ogni volta che era il turno di UN QUALSIASI giocatore
        /// umano (non necessariamente il proprio), permettendo di giocare carte fuori turno e
        /// desincronizzando la partita fra i client ("quando qualcuno gioca non si vede").
        /// </remarks>
        private bool IsMyTurnToPlay =>
            turnController != null
            && turnController.IsHumanPlayerTurn
            && GameModeService.Current.IsLocalPlayer(turnController.CurrentPlayerIndex);

        /// <summary>
        /// Renders the human player's hand with fan layout.
        /// </summary>
        private void RenderHumanHand(List<Card> handCards, bool treAssi)
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
                    var view = CreateCardView(card, faceUp: true, clickable: IsMyTurnToPlay);
                    if (view != null)
                    {
                        // Hook UI events
                        view.OnCardClicked += OnHumanCardClicked;
                        view.OnCardDoubleClicked += OnHumanCardDoubleClicked;
                        // Do NOT subscribe to OnDragReleased for human cards: drag-to-play UI disabled for human

                        // Human cards: clickable and show hover overlay
                        view.IsClickable = IsMyTurnToPlay;
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
                cardView.IsClickable = IsMyTurnToPlay;
                // ALWAYS enable hover (for Matta visual hints to work)
                cardView.EnableHover = true;

                // Difensivo: stesso motivo di RenderTableCards.
                if (cardView.CardRenderer != null)
                {
                    cardView.CardRenderer.enabled = !suppressNewCardVisibility;
                }

                cardView.SetBaseSortingOrder(CenterFirstSortingOrder(40, handCards.Count, i));
                cardView.SetRaiseOverride(-1f); // la vista puo' arrivare dal tavolo (smazzata precedente)
                // Bordo d'oro solo per i miei Tre assi; altrimenti lo toglie a una vista arrivata dal tavolo
                // o dalla mano accusata di un altro (rivincita, resync).
                cardView.SetOutline(AccusedGold, treAssi ? AccusedOutline : 0f);
                cardView.Tapped = null;

                if (TryGetBannerHandCenter(0, out var handCenter, out _))
                {
                    // Mockup Partita e Partita4: in fila, ogni 99 (in scala con la mano piccola dei 4 giocatori su schermi bassi).
                    float centered = i - (handCards.Count - 1) / 2f;
                    cardView.SetPosition(handCenter + Vector3.right * (centered * DuelHandStep * OwnHandScale * WorldPerDesignPixel()));
                    cardView.transform.rotation = Quaternion.identity;
                    continue;
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
            ApplyMoveHints(handCards);
        }

        /// <summary>
        /// Impostazioni in partita, "Suggerimenti mosse": nel proprio turno le carte in mano che fanno
        /// una presa hanno un bagliore dietro. Fuori turno, o con l'opzione spenta, nessun bagliore.
        /// Le altre carte in mano, nel proprio turno, hanno un alone oro tenue che pulsa (non con la grafica ridotta).
        /// </summary>
        private void ApplyMoveHints(List<Card> handCards)
        {
            if (handCards == null) return;
            bool myTurn = IsMyTurnToPlay && !suppressNewCardVisibility
                && turnController.GameState != null && !turnController.GameState.RoundEnded;
            bool active = GamePreferences.MoveHints && myTurn;
            bool turnGlow = myTurn && !GamePreferences.ReducedGraphics;
            // Calcolate qui dalle regole: il refresh arriva prima che TurnController aggiorni le sue
            // mosse valide per il nuovo turno.
            var captures = active
                ? Rules51.GetValidMoves(turnController.GameState, turnController.CurrentPlayerIndex)
                    .Where(m => m.Type != MoveType.PlayOnly).Select(m => m.PlayedCard).ToList()
                : new List<Card>();
            foreach (var card in handCards)
            {
                if (!activeCardViews.TryGetValue(card, out var view) || view == null) continue;
                if (captures.Contains(card)) view.SetMoveHint(true, moveHintGlowSprite);
                else view.SetGlow(turnGlow, moveHintGlowSprite, TurnGlowColor);
            }
        }

        // Alfa = intensita' massima dell'alone (CardView.LateUpdate): piu' tenue del suggerimento.
        private static readonly Color TurnGlowColor = new Color(1f, 0.85f, 0.45f, 0.5f);

        /// <summary>Alone oro attorno alle carte che il mazziere sta per prendere (accuso 15/30).</summary>
        public void SetDealerAccusoGlow(IReadOnlyList<CardView> views, bool on)
        {
            if (views == null) return;
            foreach (var view in views)
            {
                if (view != null) view.SetGlow(on, moveHintGlowSprite, DealerAccusoGlowColor);
            }
        }

        private static readonly Color DealerAccusoGlowColor = new Color(1f, 0.8f, 0.35f);

        private void OnGamePreferencesChanged()
        {
            var state = turnController != null ? turnController.GameState : null;
            if (state == null) return;
            int localIndex = GameModeService.Current.LocalPlayerIndex;
            if (localIndex >= 0 && localIndex < state.Players.Count) ApplyMoveHints(state.Players[localIndex].Hand);
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
            if (!IsMyTurnToPlay) return;
            turnController.OnPlayerDoubleClick(clickedCardView.Card);
        }

        private void OnHumanCardDragReleased(CardView cardView, Vector3 worldPos)
        {
            if (!IsMyTurnToPlay) return;
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
            var selectedBack = MatchDeck != null ? MatchDeck.Back : defaultCardBack;
            if (selectedBack != null)
            {
                view.SetDefaultBack(selectedBack);
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
        /// Gets the local player index from GameModeService (fonte di verita' reale).
        /// </summary>
        private int GetLocalPlayerIndex()
        {
            return GameModeService.Current.LocalPlayerIndex;
        }

        private void OnDestroy()
        {
            GamePreferences.Changed -= OnGamePreferencesChanged;
        }

        /// <summary>
        /// Handles human player clicking a card in their hand.
        /// Uses unified logic for all cards.
        /// </summary>
        private void OnHumanCardClicked(CardView clickedCardView)
        {
            if (!IsMyTurnToPlay)
            {
                // silent: not local player's turn
                return;
            }

            // Vassoio della presa aperto: la stessa carta lo lascia com'e', un'altra lo chiude e fa la sua strada.
            if (moveSelectionUI != null && moveSelectionUI.IsVisible)
            {
                if (clickedCardView.Card.Equals(chooserCard)) return;
                moveSelectionUI.Cancel();
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

            // Nessuna mossa valida per questa carta (c'e' una presa obbligata con un'altra):
            // prima si forzava uno scarto col giocatore 0, che l'altro client rifiutava -> partite divergenti.
            if (movesForCard.Count == 0)
            {
                clickedCardView.SetSelected(false);
                moveSelectionUI?.ShowInvalid("Devi prendere con un'altra carta");
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
                var gameState = turnController != null ? turnController.GameState : null;
                var choices = uniqueMoves.Select(m => new MoveSelectionUI.CaptureChoice
                {
                    Title = FormatMoveTitle(m),
                    Detail = FormatMoveDetail(m),
                    Cards = (m.CapturedCards ?? new List<Card>()).Select(GetSpriteForCard).Where(sprite => sprite != null).ToList(),
                    Denari = (m.CapturedCards ?? new List<Card>()).Count(c => c.Suit == Suit.Denari),
                    Scopa = TurnController.IsScopaCapture(gameState, m),
                    TableCards = (m.CapturedCards ?? new List<Card>())
                        .Select(c => activeCardViews.TryGetValue(c, out var v) && v != null ? (Renderer)v.CardRenderer : null)
                        .Where(r => r != null).ToList(),
                }).ToList();

                // Mockup: salgono insieme le carte di tutte le prese; anelli colorati e numeri li mette il vassoio.
                ClearArrowsAndHighlights();
                currentlyHighlightedCards = uniqueMoves.SelectMany(m => m.CapturedCards ?? new List<Card>()).Distinct().ToList();
                foreach (var c in currentlyHighlightedCards)
                    if (activeCardViews.TryGetValue(c, out var v)) v.SetSelected(true);
                chooserCard = playedCard;

                var back = MatchDeck != null ? MatchDeck.Back : defaultCardBack;
                moveSelectionUI.ShowCaptureChoices(choices,
                    idx =>
                    {
                        ClearArrowsAndHighlights();
                        foreach (var kv in activeCardViews)
                            kv.Value?.SetSelected(false);
                        if (idx >= 0 && idx < uniqueMoves.Count)
                        {
                            turnController.ExecuteMove(uniqueMoves[idx]);
                        }
                    },
                    hoveredIndex => HighlightAlternative(uniqueMoves, hoveredIndex, null),
                    () =>
                    {
                        // Annulla: si puo' scegliere un'altra carta dalla mano.
                        ClearArrowsAndHighlights();
                        foreach (var kv in activeCardViews)
                            kv.Value?.SetSelected(false);
                    },
                    playedCard: GetSpriteForCard(playedCard), cardBack: back);
            }
            else
            {
                // No UI available, execute first move
                turnController.ExecuteMove(uniqueMoves[0]);
                foreach (var kv in activeCardViews)
                    kv.Value?.SetSelected(false);
            }
        }

        private static string FormatMoveTitle(Move move)
        {
            switch (move.Type)
            {
                case MoveType.CaptureEqual: return "Carta uguale";
                case MoveType.Capture15: return "Somma 15";
                case MoveType.CaptureSum: return "Somma";
                case MoveType.AceCapture: return "Asso piglia tutto";
                default: return "Gioca la carta";
            }
        }

        private string FormatMoveDetail(Move move)
        {
            int count = move.CapturedCards?.Count ?? 0;
            if (count == 0) return "Nessuna presa";
            var tableCount = turnController != null && turnController.GameState != null ? turnController.GameState.Table.Count : -1;
            string cards = count == 1 ? "Prendi 1 carta" : $"Prendi {count} carte";
            return count == tableCount ? cards + " - pulisci il tavolo" : cards;
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
                if (GamePreferences.MoveHints && !helpShownForCurrentSelection && allMatches.Count > 0)
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
