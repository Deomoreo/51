using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce il contenuto vero della pagina CARTE (DeckPage, dentro PagesViewport di
    /// HomeScreen, navigata via bottom nav bar/swipe - NON il pannello modale del
    /// DeckSelector in Home, quello resta gestito da PanelMazzoBuilder ed e' solo-mazzo).
    ///
    /// RISCRITTURA (2026): rifatta per matchare 1:1 i 3 mockup forniti dall'utente
    /// (tab Mazzi/Emoticon/Accusi) - box "IN USO" in evidenza in alto per tab, sezione
    /// "COLLEZIONE X/Y" con barra di progresso, griglia con stati sbloccato/equipaggiato/
    /// bloccato (lucchetto+prezzo). Tab strip ora a pillole (MAZZI/EMOTICON/ACCUSI, in
    /// quest'ordine, diverso dal vecchio MAZZO/ACCUSO/EMOTICON). Tutto entra senza
    /// ScrollRect (stessa lezione del pannello modale sul click-eating, e l'utente ha
    /// chiesto esplicitamente "piu' spazio, non in finestra, piu' fluidita'").
    /// </summary>
    public static class DeckPageBuilder
    {
        private const string ScenePath = "Assets/Scenes/HomeScreen.unity";
        private const string ThemePath = "Assets/Resources/DragonsHoardTheme.asset";

        private const float RefWidth = 1080f;
        private const float ContentWidth = 980f;
        private const float TabStripY0 = 160f;
        private const float TabStripHeight = 84f;
        private const float ContentAreaY0 = 254f;
        // NOTA: l'altezza REALE utilizzabile di DeckPage (misurata a runtime via
        // RectTransform.rect, non la vecchia ipotesi "reference 1920") e' risultata solo
        // ~1352 unita' locali (CanvasScaler con match=0.5 su schermi non esattamente
        // 1080x1920 non da' mai esattamente 1920 di altezza locale) - un valore piu' alto qui
        // faceva sborare la griglia Mazzo/Emoticon sotto la NavBar (verificato con uno
        // screenshot Scene View via Unity MCP). 1080 lascia margine di sicurezza.
        private const float ContentAreaHeight = 1080f;

        private const float CardAspect = 151f / 219f; // aspect reale di card_back_green (151x219)

        // Colori specifici di questo screen, presi dai mockup forniti dall'utente (non tutti
        // coincidono con UITheme, che resta comunque la fonte per gold/cream generici).
        private static readonly Color MutedBlue = new Color(0.545f, 0.588f, 0.686f, 1f); // #8B96AF
        private static readonly Color GreenCheck = new Color(0.290f, 0.839f, 0.545f, 1f); // #4AD68B
        private static readonly Color PanelDark = new Color(0.043f, 0.078f, 0.125f, 1f); // #0B1420
        // Palette esatta da ASSET_MAPPING_Collezione.md per lo sfondo FILL+RING di ogni
        // cella/riga/slot delle tre schede Collezione (RGBA 0-255 del documento, convertiti
        // 1:1 tramite Rgba() - "alla lettera", non piu' approssimati via MutedBlue/tinte
        // UITheme come prima). Sostituiscono BorderBlue/GlowGold, rimossi perche' non piu'
        // referenziati da nessuna cella/riga/slot dopo questo refactor.
        private static readonly Color GoldRingColor = Rgba(232, 178, 74, 255); // bordo/ring oro "equipaggiato/in uso" ovunque
        private static readonly Color LineRingColor = Rgba(70, 102, 142, 255); // bordo "normale" di celle/righe/slot sbloccati-non-equipaggiati o bloccati
        private static readonly Color PriceTextColor = Rgba(255, 250, 235, 255); // testo sul bottone prezzo (btn_blue_long)

        // Riquadri "IN USO" featured §2.1/§4.1
        private static readonly Color MazzoFeaturedGlow = Rgba(255, 196, 70, 70);
        private static readonly Color MazzoFeaturedFill = Rgba(18, 32, 52, 252);
        private static readonly Color MazzoFeaturedName = Rgba(255, 236, 190, 255);
        private static readonly Color MazzoFeaturedSubtitle = Rgba(185, 200, 222, 255);
        private static readonly Color AccusoFeaturedFill = Rgba(20, 36, 58, 252);

        // Mazzi §2.2/2.3/2.4
        private static readonly Color DeckEquippedGlow = Rgba(255, 196, 70, 95);
        private static readonly Color DeckCellFill = Rgba(26, 44, 68, 252);
        private static readonly Color DeckUnlockedName = Rgba(255, 236, 190, 255);
        private static readonly Color DeckLockedFill = Rgba(14, 24, 40, 246);
        private static readonly Color DeckLockedVeil = Rgba(6, 14, 26, 150);
        private static readonly Color DeckLockedName = Rgba(124, 146, 176, 255);

        // Emoticon §3.1/3.2/3.3/3.4/3.5
        // (EmoEquippedGlow rimosso: bagliore giallo sfocato dietro slot/celle equipaggiate,
        // segnalato esplicitamente dall'utente come inutile/brutto - vedi CreateEquippedSlot)
        private static readonly Color EmoSlotFill = Rgba(26, 44, 68, 252);
        private static readonly Color EmoEquippedName = Rgba(185, 200, 222, 255);
        private static readonly Color EmoEmptyFill = Rgba(14, 24, 40, 220);
        private static readonly Color EmoEmptyDashColor = Rgba(70, 100, 140, 255);
        private static readonly Color EmoEmptyPlusColor = Rgba(90, 124, 166, 255);
        private static readonly Color EmoEmptyText = Rgba(120, 146, 178, 255);
        private static readonly Color EmoGridFill = Rgba(26, 44, 68, 250);
        private static readonly Color EmoGridName = Rgba(255, 236, 190, 255); // "identico a §2.2" nel documento
        private static readonly Color EmoLockedFill = Rgba(14, 24, 40, 240);
        private static readonly Color EmoLockedText = Rgba(110, 132, 162, 255);

        // Accusi §4.2/4.3
        private static readonly Color AccusoEquippedFill = Rgba(26, 44, 68, 250);
        private static readonly Color AccusoEquippedName = Rgba(255, 250, 235, 255);
        private static readonly Color AccusoEquippedSubtitle = Rgba(185, 200, 222, 255);
        private static readonly Color AccusoInUsoText = Rgba(255, 228, 156, 255);
        private static readonly Color AccusoBadgeRed = Rgba(196, 54, 44, 255);
        private static readonly Color AccusoLockedFill = Rgba(16, 28, 46, 244);
        private static readonly Color AccusoLockedVeil = Rgba(10, 18, 30, 150);
        private static readonly Color AccusoLockedName = Rgba(140, 162, 192, 255);
        private static readonly Color AccusoLockedSubtitle = Rgba(110, 132, 162, 255);

        // Dimensioni misurate a pixel sul mockup reale (Assets/Mockup/20_pagina_carte (1).png,
        // canvas 1080x1920): riga griglia Y906-1206 e Y1240-1540 = altezza cella 300, passo tra
        // righe 334. Larghezza/passo colonna erano gia' corretti (misurato: celle 305 wide,
        // passo 327 - entro il 2% di quanto gia' in uso, non toccati). La tab Mazzo scrolla ora
        // (vedi BuildMazzoTab) invece di stare tutta senza scroll: a queste dimensioni reali il
        // contenuto non ci sta nei ~1080 px utili della pagina.
        private const float DeckCellWidth = 300f;
        private const float DeckCellHeight = 300f;
        private const float DeckColStep = 330f;
        private const float DeckRowStep = 334f;

        // Dimensione card ESATTA fornita dall'utente (era 225x171, misura precedente approssimata
        // dal mockup - ora sostituita dai valori source-of-truth).
        private const float EmoCellWidth = 228f;
        private const float EmoCellHeight = 211f;

        private struct DeckInfo
        {
            public string Name;
            public bool Unlocked;
            public string PriceLabel; // usato solo se !Unlocked
        }

        // Esattamente i 6 mazzi mostrati nel mockup reale (2 righe x 3) - Notturno/Oro/Rubino
        // erano stati inventati in una sessione precedente per riempire un contatore "2/9" che
        // il mockup non giustifica (mostra solo questi 6, contatore corretto in "2/6" sotto).
        private static readonly DeckInfo[] Decks =
        {
            new DeckInfo { Name = "Napoletano", Unlocked = true },
            new DeckInfo { Name = "Classico", Unlocked = true },
            new DeckInfo { Name = "Reale", Unlocked = false, PriceLabel = "1.500" },
            new DeckInfo { Name = "Smeraldo", Unlocked = false, PriceLabel = "250 gemme" },
            new DeckInfo { Name = "Antico", Unlocked = false, PriceLabel = "3.000" },
            new DeckInfo { Name = "Drago", Unlocked = false, PriceLabel = "Evento" },
        };

        private static readonly string[] EmoticonNames = { "Risata", "Arrabbiato", "Sorpreso", "Pensieroso", "Triste", "Furbo" };
        private static readonly string[] EmoticonSprites = { "emo_risata", "emo_arrabbiato", "emo_sorpreso", "emo_pensieroso", "emo_triste", "emo_furbo" };
        private const int EmoticonLockedCount = 6; // celle "Bloccata" generiche, per arrivare a 12 totali come nel mockup
        private const int MaxEquippedEmoticons = 3;

        private struct AccusoInfo
        {
            public string Name;
            public string Subtitle;
            public string Icon;
            public bool Unlocked;
            public string PriceLabel;
        }

        private static readonly AccusoInfo[] AccusoList =
        {
            new AccusoInfo { Name = "Pugno sul tavolo", Subtitle = "Le carte saltano in aria", Icon = "ic_trophy", Unlocked = true },
            new AccusoInfo { Name = "Tuono", Subtitle = "Un lampo illumina il tavolo", Icon = "ic_warn", Unlocked = false, PriceLabel = "2.000" },
            new AccusoInfo { Name = "Pioggia d'oro", Subtitle = "Monete cadono sul tavolo", Icon = "ic_coin_clover", Unlocked = false, PriceLabel = "300 gemme" },
            new AccusoInfo { Name = "Vortice", Subtitle = "Le carte turbinano", Icon = "ic_fastfwd", Unlocked = false, PriceLabel = "Livello 15" },
        };
        private const string AccusoFeaturedDescription = "Batti il pugno e fai saltare tutte le carte sul tavolo.";

        private static UITheme _theme;

        [MenuItem("Tools/Dragons Hoard/Build Deck Page (CARTE)")]
        private static void Build()
        {
            if (!LoadContext())
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[DeckPageBuilder] Nessun Canvas trovato in HomeScreen.unity.");
                return;
            }

            var canvasRect = (RectTransform)canvas.transform;

            var deckPage = FindDeepChild(canvasRect, "DeckPage") as RectTransform;
            if (deckPage == null)
            {
                Debug.LogError("[DeckPageBuilder] Nessun 'DeckPage' trovato sotto PagesViewport: esegui prima Tools/Dragons Hoard/Build Home Screen.");
                return;
            }

            // Vedi nota storica: un componente TMP creato su un GameObject inattivo non passa
            // mai da OnEnable, quindi il fontSharedMaterial resta null e un tocco successivo
            // (outlineWidth, ecc.) lancia ArgumentNullException. Riattivato qui, rimesso
            // disattivato in fondo prima del salvataggio.
            deckPage.gameObject.SetActive(true);

            // Idempotente: ripulisce completamente il contenuto esistente prima di ricostruire.
            var existingController = deckPage.GetComponent<DeckPageController>();
            if (existingController != null)
            {
                UnityEngine.Object.DestroyImmediate(existingController);
            }
            for (int i = deckPage.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(deckPage.GetChild(i).gameObject);
            }

            var bg = deckPage.GetComponent<Image>();
            if (bg == null) bg = deckPage.gameObject.AddComponent<Image>();
            bg.color = PanelDark;
            bg.raycastTarget = true;

            var tabButtons = new Button[3];
            var tabSelectedBgs = new GameObject[3];
            var tabLabelTexts = new TMP_Text[3];
            var tabRects = new RectTransform[3];
            CreateTabStrip(deckPage, tabButtons, tabSelectedBgs, tabLabelTexts, tabRects);

            // Ancorato al CENTRO orizzontale di DeckPage: PanelSwipeController allarga ogni
            // pagina di pageHorizontalOverflow per lato a runtime, quindi il bordo sinistro
            // REALE di DeckPage non coincide col bordo sinistro dello schermo - un anchor
            // top-left con offset fisso finirebbe fuori centro. Usata ancora da Mazzo/Accuso
            // (nessuna nuova geometria assoluta fornita per quei due), NON piu' da Emoticon
            // (vedi emoticonContent sotto: quello ha coordinate ESATTE misurate dal top-left
            // assoluto del canvas, fornite dall'utente come source of truth - nidificarlo dentro
            // questa ContentArea centrata a 980 le avrebbe rese tutte sbagliate).
            var contentArea = CreateUIObject("ContentArea", deckPage);
            SetTopCenter(contentArea, ContentAreaY0, ContentWidth, ContentAreaHeight);

            // Ordine Mazzi(0) / Emoticon(1) / Accusi(2) - come nei mockup, diverso dal vecchio
            // Mazzo/Accuso/Emoticon.
            var mazzoContent = CreateUIObject("MazzoContent", contentArea);
            StretchFill(mazzoContent);
            var deckItems = new List<SelectableToggleItem>();
            var deckLabels = new List<string>();
            var fanCardArt = BuildMazzoTab(mazzoContent, deckItems, deckLabels, out var deckNameText, out var deckSubtitleText, out _);

            // Sibling diretto di deckPage (NON figlio di contentArea): le coordinate esatte
            // fornite dall'utente sono misurate dal top-left assoluto del canvas 1080x1920, e
            // deckPage stesso e' gia' allineato 1:1 al canvas (pageHorizontalOverflow=0,
            // verificato via Unity MCP) - quindi qui bastano SetTopLeft dirette, zero conversioni.
            var emoticonContent = CreateUIObject("EmoticonContent", deckPage);
            SetTopLeft(emoticonContent, 0f, 0f, RefWidth, 1920f);
            var emoticonItems = new List<SelectableToggleItem>();
            BuildEmoticonTab(emoticonContent, emoticonItems, out var equippedSlots);
            emoticonContent.gameObject.SetActive(false);

            var accusoContent = CreateUIObject("AccusoContent", contentArea);
            StretchFill(accusoContent);
            var accusoItems = new List<SelectableToggleItem>();
            BuildAccusoTab(accusoContent, accusoItems);
            accusoContent.gameObject.SetActive(false);

            var tabContents = new[] { mazzoContent, emoticonContent, accusoContent };

            var deckGroup = WireSingleGroup(deckPage, "DeckSelectionGroup", deckItems, defaultSelectedIndex: 0);
            var emoticonGroup = WireMultiGroup(deckPage, "EmoticonSelectionGroup", emoticonItems, maxSelected: MaxEquippedEmoticons, defaultIndices: new List<int> { 0, 1 });
            var accusoGroup = WireSingleGroup(deckPage, "AccusoSelectionGroup", accusoItems, defaultSelectedIndex: 0);

            CreateController(canvasRect, deckPage, tabButtons, tabSelectedBgs, tabLabelTexts, tabRects, tabContents,
                deckGroup, fanCardArt, deckNameText, deckSubtitleText, deckLabels,
                emoticonGroup, equippedSlots, accusoGroup);

            deckPage.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[DeckPageBuilder] Pagina CARTE (DeckPage) ricostruita in HomeScreen.unity. Salvato.");
        }

        // ------------------------------------------------------------------
        // Tab strip: 3 pillole (MAZZI / EMOTICON / ACCUSI)
        // ------------------------------------------------------------------

        private static void CreateTabStrip(RectTransform deckPage, Button[] tabButtons, GameObject[] tabSelectedBgs, TMP_Text[] tabLabelTexts, RectTransform[] tabRects)
        {
            // Geometria ESATTA fornita dall'utente (source of truth, canvas 1080x1920, misurata
            // dal top-left assoluto - NON piu' 3 colonne a frazione uguale dentro una strip
            // centrata). x/larghezza uguali in stato normale e selezionato, solo y/altezza
            // cambiano (il tab attivo "spunta" piu' in alto e piu' alto - vedi DeckPageController.
            // SelectTab, che applica questi due stati a runtime in base al tab selezionato).
            var strip = CreateUIObject("TabStrip", deckPage);
            SetTopLeft(strip, 0f, 0f, RefWidth, 320f); // contenitore puramente organizzativo, 0 offset: i figli usano coordinate assolute canvas

            string[] labels = { "MAZZI", "EMOTICON", "ACCUSI" };
            float[] tabX = { 60f, 384f, 708f };
            const float tabWidth = 310f;
            const float tabNormalY = 224f;
            const float tabNormalHeight = 56f;

            for (int i = 0; i < 3; i++)
            {
                var tab = CreateUIObject("Tab_" + labels[i], strip);
                SetTopLeft(tab, tabX[i], tabNormalY, tabWidth, tabNormalHeight);
                tabRects[i] = tab;

                var hit = tab.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
                hit.raycastTarget = true;
                var button = tab.gameObject.AddComponent<Button>();
                button.targetGraphic = hit;

                var bgRt = CreateUIObject("Bg", tab);
                StretchFill(bgRt);
                bgRt.offsetMin = new Vector2(10f, 4f);
                bgRt.offsetMax = new Vector2(-10f, -4f);
                // btn_gray_small per il pill inattivo (spec §3): prima era btn_blue_mid, mai
                // corretto nonostante segnalato in precedenza - qui il fix vero.
                AddSpriteImage(bgRt, "btn_gray_small", raycastTarget: false, sliced: true);

                var selectedRt = CreateUIObject("SelectedBg", tab);
                StretchFill(selectedRt);
                selectedRt.offsetMin = new Vector2(10f, 4f);
                selectedRt.offsetMax = new Vector2(-10f, -4f);
                AddSpriteImage(selectedRt, "btn_teal", raycastTarget: false, sliced: true);

                var labelRt = CreateUIObject("Label", tab);
                StretchFill(labelRt);
                // Stile "cartoon" del mockup: riempimento crema chiaro + contorno nero, MAI un
                // colore pieno scuro/nero (bug reale segnalato dall'utente: la label del tab
                // attivo appariva "full nera" perche' DeckPageController.activeColor era quasi
                // nero - vedi fix li'). Uguale su tab attivo e inattivo, cambia solo lo sfondo pillola.
                var labelText = AddText(labelRt, labels[i], 27f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
                labelText.fontMaterial = new Material(labelText.fontMaterial);
                labelText.outlineWidth = 0.2f;
                labelText.outlineColor = Color.black;

                tabButtons[i] = button;
                tabSelectedBgs[i] = selectedRt.gameObject;
                tabLabelTexts[i] = labelText;

                selectedRt.gameObject.SetActive(i == 0);
            }
        }

        // ------------------------------------------------------------------
        // Helper condivisi: badge "IN USO", header "COLLEZIONE X/Y", barra progresso
        // ------------------------------------------------------------------

        private static readonly Color DarkBrownText = new Color(0.227f, 0.086f, 0.031f, 1f); // #3A1608

        /// <summary>
        /// Reintrodotto dopo il fix del bordo 9-slice (vedi Icons.png.meta): prima usava
        /// sq_gold, pensato per pannelli quadrati con bordo alto (44px), che su una fascia
        /// sottile da 52px si rompeva visibilmente - da qui la rimozione temporanea. btn_gold_long
        /// e' gia' una pillola orizzontale con bordo verticale sottile (7px), adatta.
        /// </summary>
        private static void CreateBadge(RectTransform parent, float y0, string text, float width)
        {
            var badge = CreateUIObject("InUsoBadge", parent);
            SetTopCenter(badge, y0, width, 52f);
            AddSpriteImage(badge, "btn_gold_long", raycastTarget: false, sliced: true);

            var textRt = CreateUIObject("Text", badge);
            StretchFill(textRt);
            AddText(textRt, text, 26f, FontStyles.Bold, DarkBrownText, TextAlignmentOptions.Center);
        }

        private static void CreateCollectionHeader(RectTransform parent, float y0, int unlockedCount, int totalCount, float x0 = 0f, float width = ContentWidth)
        {
            var header = CreateUIObject("CollectionHeader", parent);
            SetTopLeft(header, x0, y0, width, 40f);

            var labelRt = CreateUIObject("Label", header);
            SetAnchoredRect(labelRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(260f, 40f), Vector2.zero);
            AddText(labelRt, "COLLEZIONE", 26f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.Left);

            var counterRt = CreateUIObject("Counter", header);
            SetAnchoredRect(counterRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(140f, 40f), Vector2.zero);
            AddText(counterRt, $"{unlockedCount} / {totalCount}", 26f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.Right);

            var lineRt = CreateUIObject("Line", header);
            SetAnchoredRect(lineRt, new Vector2(260f / width, 0.5f), new Vector2(1f - 140f / width, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), Vector2.zero);
            var lineImg = lineRt.gameObject.AddComponent<Image>();
            lineImg.color = new Color(_theme.Gold.r, _theme.Gold.g, _theme.Gold.b, 0.5f);
            lineImg.raycastTarget = false;
        }

        private static void CreateProgressBar(RectTransform parent, float y0, int unlockedCount, int totalCount, float x0 = 0f, float width = ContentWidth, float height = 28f)
        {
            // Bug reale segnalato dall'utente ("sprite tutto stretchato"): bar_empty ha un bordo
            // 9-slice asimmetrico (27h/7v su un sorgente 217x63) pensato per un cerino/capsula -
            // sliceato su una barra larga ~980 e alta 28 la sua curva d'angolo si stira in modo
            // visibile (non e' una vera "capsula"). Fix: stessa tecnica gia' usata per i bordi
            // arrotondati delle celle (AddFillLine) - panel_fill_r24 Sliced come "pillola" per
            // track E riempimento (il suo raggio 9-slice e' fisso in pixel sorgente, si scala
            // gracefully anche su una barra sottile). Il riempimento NON usa Image.Type.Filled
            // (incompatibile con Sliced in Unity - avrebbe un taglio netto invece di un bordo
            // arrotondato) ma un RectTransform ridimensionato via anchorMax.x, cosi' resta
            // Sliced/arrotondato su tutta la sua larghezza. Sopra, un velo bianco semitrasparente
            // nella meta' superiore da' l'effetto "lucido" richiesto, come nel mockup.
            var bar = CreateUIObject("ProgressBar", parent);
            SetTopLeft(bar, x0, y0, width, height);
            AddSpriteImage(bar, "panel_fill_r24", sliced: true).color = new Color(0.086f, 0.129f, 0.192f, 1f);

            float fraction = totalCount > 0 ? Mathf.Clamp01((float)unlockedCount / totalCount) : 0f;

            var fill = CreateUIObject("Fill", bar);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(fraction, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            var fillImg = AddSpriteImage(fill, "panel_fill_r24", sliced: true);
            fillImg.color = _theme.Gold;
            fillImg.raycastTarget = false;

            var gloss = CreateUIObject("Gloss", fill);
            gloss.anchorMin = new Vector2(0f, 0.5f);
            gloss.anchorMax = new Vector2(1f, 1f);
            gloss.pivot = new Vector2(0.5f, 1f);
            gloss.offsetMin = new Vector2(4f, 0f);
            gloss.offsetMax = new Vector2(-4f, -3f);
            var glossImg = AddSpriteImage(gloss, "panel_fill_r24", sliced: true);
            glossImg.color = new Color(1f, 1f, 1f, 0.28f);
            glossImg.raycastTarget = false;
        }

        // ------------------------------------------------------------------
        // Tab Mazzo: badge + box "IN USO" (ventaglio) + collezione + griglia 3x3
        // ------------------------------------------------------------------

        // Altezza box misurata a pixel sul mockup reale (Y272-740 nel canvas 1080x1920 di
        // Assets/Mockup/20_pagina_carte (1).png) - prima era 263 (quasi la meta'), causa
        // principale dell'effetto "tutto schiacciato" segnalato confrontando con lo screenshot.
        private const float MazzoBoxHeight = 468f;

        // Layout verticale della tab, misurato sullo stesso mockup (origine 0 = inizio del
        // badge "IN USO", che nel mockup e' a Y254 - inizio ContentArea): badge 0, bordo
        // superiore box a 26 cosi' il badge (alto 52) resta centrato sul bordo del box come
        // nel mockup; header/barra/griglia/footer sotto usando le Y assolute del mockup meno
        // 254 (inizio ContentArea) piu' lo stesso +8 dell'offset badge/box.
        private const float BadgeY0 = 0f;
        private const float BoxY0 = 26f;
        private const float CollectionHeaderY0 = 534f;
        private const float ProgressBarY0 = 594f;
        private const float GridY0 = 660f;
        private const float FooterY0 = 1540f;
        private const int DeckGridRows = 2; // 6 mazzi nel mockup reale, non 9 (vedi Decks)

        private static Image[] BuildMazzoTab(RectTransform mazzoContent, List<SelectableToggleItem> deckItems,
            List<string> deckLabels, out TMP_Text nameText, out TMP_Text subtitleText, out ScrollRect scrollRect)
        {
            // Il box "IN USO" a piena altezza mockup (468) + griglia (2 righe da 300, misurate)
            // non entrano nei ~1080px utili della pagina: serve scroll (stessa tecnica di
            // NegozioBuilder - RectMask2D + catcher trasparente, non Mask+Image).
            var content = CreateScrollWrapper(mazzoContent, out scrollRect);

            var box = CreateUIObject("FeaturedBox", content);
            SetTopLeft(box, 0f, BoxY0, ContentWidth, MazzoBoxHeight);
            // ASSET_MAPPING_Collezione.md §2.1: bagliore + FILL+RING r30, non sq_gold (bordo
            // sorgente 44px, il piu' pesante del set - causa principale dell'effetto "frame di
            // gioco" segnalato dall'utente confrontando con lo screenshot del mockup).
            AddGlow(box, "panel_ring_r30", MazzoFeaturedGlow, ContentWidth, MazzoBoxHeight, 30f);
            AddFillLine(box, "panel_fill_r30", MazzoFeaturedFill, GoldRingColor, 6f);

            // Badge creato DOPO il box (stesso parent "content", ma sibling successivo = sopra
            // nel render order): prima veniva creato per primo e il bagliore del box - che
            // sborda ~30px oltre il bordo superiore - lo copriva quasi del tutto (bug reale
            // trovato via screenshot live, "IN USO" appariva spento/tagliato a meta').
            CreateBadge(content, BadgeY0, "IN USO", 200f);

            var fanCardArt = CreateCardFan(box);

            var nameRt = CreateUIObject("NameText", box);
            SetTopCenter(nameRt, 313f, 800f, 40f);
            nameText = AddText(nameRt, "", 34f, FontStyles.Bold, MazzoFeaturedName, TextAlignmentOptions.Center);

            var subtitleRt = CreateUIObject("SubtitleText", box);
            SetTopCenter(subtitleRt, 395f, 800f, 30f);
            subtitleText = AddText(subtitleRt, "", 23f, FontStyles.Normal, MazzoFeaturedSubtitle, TextAlignmentOptions.Center);

            int unlockedCount = Decks.Count(d => d.Unlocked);
            CreateCollectionHeader(content, CollectionHeaderY0, unlockedCount, Decks.Length);
            CreateProgressBar(content, ProgressBarY0, unlockedCount, Decks.Length);

            var gridContainer = CreateUIObject("GridContainer", content);
            float gridWidth = DeckColStep * 2f + DeckCellWidth;
            float gridOffsetX = (ContentWidth - gridWidth) * 0.5f;
            SetTopLeft(gridContainer, gridOffsetX, GridY0, gridWidth, DeckRowStep * (DeckGridRows - 1) + DeckCellHeight);

            for (int row = 0; row < DeckGridRows; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    float x = col * DeckColStep;
                    float y = row * DeckRowStep;
                    int deckIndex = row * 3 + col;
                    var info = Decks[deckIndex];
                    bool selected = deckIndex == 0; // Napoletano equipaggiato di default

                    var item = CreateDeckCell(gridContainer, "DeckCell_" + info.Name, info, x, y, selected);
                    if (item != null)
                    {
                        deckItems.Add(item);
                        deckLabels.Add(info.Name);
                    }
                }
            }

            var footerRt = CreateUIObject("Footer", content);
            SetTopCenter(footerRt, FooterY0, 900f, 34f);
            AddText(footerRt, "Nuovi mazzi in arrivo con gli eventi stagionali", 22f, FontStyles.Italic, MutedBlue, TextAlignmentOptions.Center);

            content.sizeDelta = new Vector2(0f, FooterY0 + 34f + 40f);

            return fanCardArt;
        }

        /// <summary>
        /// Avvolge il contenuto della tab Mazzo in uno ScrollRect (RectMask2D + catcher
        /// trasparente per l'input, stessa tecnica di NegozioBuilder - non Mask+Image, bug di
        /// culling alpha gia' visto altrove in questo progetto). Ritorna il Content dentro cui
        /// costruire il resto della tab.
        /// </summary>
        private static RectTransform CreateScrollWrapper(RectTransform parent, out ScrollRect scrollRect)
        {
            var scrollViewRt = CreateUIObject("ScrollView", parent);
            StretchFill(scrollViewRt);
            scrollRect = scrollViewRt.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            var viewport = CreateUIObject("Viewport", scrollViewRt);
            StretchFill(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            catcher.raycastTarget = true;

            var content = CreateUIObject("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            scrollRect.viewport = viewport;
            scrollRect.content = content;

            return content;
        }

        private static Image[] CreateCardFan(RectTransform box)
        {
            // Dimensioni scalate proporzionalmente al nuovo MazzoBoxHeight (468, prima 263 -
            // fattore 1.78x) sulle stesse proporzioni gia' in uso, non ri-misurate pixel per
            // pixel dal mockup (il ventaglio carte e' un blocco visivo continuo, difficile da
            // isolare con precisione da uno scan di colore a singola colonna).
            float cardHeight = 254f;
            float cardWidth = cardHeight * CardAspect;
            float centerX = ContentWidth * 0.5f;
            float centerY = 171f;

            var left = CreateFanCard(box, "CardLeft", centerX - 196f, centerY, 16f, cardWidth, cardHeight);
            var right = CreateFanCard(box, "CardRight", centerX + 196f, centerY, -16f, cardWidth, cardHeight);
            var center = CreateFanCard(box, "CardCenter", centerX, centerY, 0f, cardWidth, cardHeight);

            return new[] { left, center, right };
        }

        private static Image CreateFanCard(RectTransform box, string name, float centerX, float centerY,
            float rotationZ, float width, float height)
        {
            var slot = CreateUIObject(name, box);
            SetAnchoredRect(slot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(width, height), new Vector2(centerX, -centerY));
            slot.localEulerAngles = new Vector3(0f, 0f, rotationZ);

            var shadow = CreateUIObject("Shadow", slot);
            SetAnchoredRect(shadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(width, height), new Vector2(7f, -10f));
            var shadowImg = AddSpriteImage(shadow, "card_back_green");
            shadowImg.color = new Color(0f, 0f, 0f, 0.35f);

            var art = CreateUIObject("CardArt", slot);
            SetAnchoredRect(art, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(width, height), Vector2.zero);
            var artImg = AddSpriteImage(art, "card_back_green");

            return artImg;
        }

        /// <summary>
        /// Cella mazzo a 3 stati: equipaggiato (bordo oro + check + "In uso"), sbloccato ma
        /// non equipaggiato (bordo normale + pulsante "USA"), bloccato (arte scurita +
        /// lucchetto + prezzo, nessuna interazione).
        /// </summary>
        private static SelectableToggleItem CreateDeckCell(RectTransform grid, string name, DeckInfo info,
            float x, float y, bool selected)
        {
            var cell = CreateUIObject(name, grid);
            SetTopLeft(cell, x, y, DeckCellWidth, DeckCellHeight);

            AddFillLine(cell, "panel_fill_r24", info.Unlocked ? DeckCellFill : DeckLockedFill, LineRingColor);

            GameObject selectionIndicator = null;
            if (info.Unlocked)
            {
                selectionIndicator = CreateSelectionIndicator(cell, DeckCellWidth, DeckCellHeight, true, DeckEquippedGlow, 22f, selected, DeckCellFill);
            }

            float cardArtHeight = 105f;
            float cardArtWidth = cardArtHeight * CardAspect;
            // Le celle bloccate restano identiche a prima (bordo -8, poi price pill sotto -
            // l'utente le trova gia' bene cosi'). Le sbloccate invece non hanno quella riga in
            // piu' sotto al nome: con lo stesso -8 il blocco carta+nome restava "appeso" in
            // alto con un vuoto sotto - qui lo si centra davvero nella cella.
            float topGap = info.Unlocked ? (DeckCellHeight - (cardArtHeight + 4f + 22f)) * 0.5f : 8f;
            var cardArt = CreateUIObject("CardArt", cell);
            SetAnchoredRect(cardArt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(cardArtWidth, cardArtHeight), new Vector2(0f, -topGap));
            var cardImg = AddSpriteImage(cardArt, info.Unlocked ? "card_back_green" : "card_frame_dark");
            cardImg.color = Color.white; // nessuna tinta sul dorso - il "velo scuro" per le bloccate e' un overlay separato (§2.4)

            if (!info.Unlocked)
            {
                var veilRt = CreateUIObject("Veil", cardArt);
                StretchFill(veilRt);
                var veil = veilRt.gameObject.AddComponent<Image>();
                veil.color = DeckLockedVeil;
                veil.raycastTarget = false;

                var lockRt = CreateUIObject("LockIcon", cardArt);
                SetAnchoredRect(lockRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(34f, 34f), Vector2.zero);
                AddSpriteImage(lockRt, "ic_lock", preserveAspect: true);
            }

            var nameRt = CreateUIObject("NameText", cell);
            SetAnchoredRect(nameRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(DeckCellWidth - 12f, 22f), new Vector2(0f, -topGap - cardArtHeight - 4f));
            AddText(nameRt, info.Name, 19f, FontStyles.Normal, info.Unlocked ? DeckUnlockedName : DeckLockedName, TextAlignmentOptions.Center);

            if (!info.Unlocked)
            {
                float bottomRowY = -topGap - cardArtHeight - 4f - 26f;
                var priceRt = CreateUIObject("PricePill", cell);
                SetAnchoredRect(priceRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(150f, 30f), new Vector2(0f, bottomRowY));
                AddSpriteImage(priceRt, "btn_blue_long", raycastTarget: false, sliced: true);
                var priceTextRt = CreateUIObject("Text", priceRt);
                StretchFill(priceTextRt);
                AddText(priceTextRt, info.PriceLabel, 17f, FontStyles.Bold, PriceTextColor, TextAlignmentOptions.Center);
                return null;
            }

            // Niente bottone "USA" ne' testo "In uso": il click sulla cella equipaggia gia'
            // direttamente (bordo dorato + check bastano come indicazione, richiesta esplicita
            // dell'utente - il bottone/etichetta era ridondante con l'anteprima gia' avvenuta).
            var checkRt = CreateUIObject("CheckIcon", cell);
            SetAnchoredRect(checkRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(36f, 36f), new Vector2(-5f, -5f));
            var checkImg = AddSpriteImage(checkRt, "ic_check", preserveAspect: true);
            checkImg.color = GreenCheck;
            checkRt.gameObject.SetActive(selected);

            var hit = cell.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;
            cell.gameObject.AddComponent<Button>().targetGraphic = hit;

            var toggle = cell.gameObject.AddComponent<SelectableToggleItem>();
            var so = new SerializedObject(toggle);
            so.FindProperty("checkIcon").objectReferenceValue = checkRt.gameObject;
            so.FindProperty("glowObject").objectReferenceValue = selectionIndicator;
            so.ApplyModifiedPropertiesWithoutUndo();

            return toggle;
        }

        // ------------------------------------------------------------------
        // Tab Emoticon: riga "EQUIPAGGIATE" (max 3, con rimozione) + collezione + griglia 4x3
        // ------------------------------------------------------------------

        // Geometria ESATTA fornita dall'utente per la schermata Emoticon (canvas 1080x1920,
        // top-left assoluto - source of truth, NON ricalcolata/centrata come le altre due tab).
        // Margine sinistro 59, margine destro 58 (1080-59-958... vedi ContentW sotto).
        private const float EmoMarginLeft = 59f;
        private const float EmoContentW = 959f; // 1080 - 59 (sx) - 58 (dx), verificato contro griglia/barra/slot

        private const float EmoEquippedY = 365f;
        private const float EmoEquippedWidth = 307f;
        private const float EmoEquippedHeight = 249f;
        private static readonly float[] EmoEquippedX = { 59f, 387f, 715f };

        private const float EmoProgressBarY = 721f;
        private const float EmoProgressBarWidth = 959f;
        private const float EmoProgressBarHeight = 35f;

        private static readonly float[] EmoGridColX = { 59f, 304f, 549f, 794f };
        private static readonly float[] EmoGridRowY = { 789f, 1019f, 1249f };

        private static void BuildEmoticonTab(RectTransform emoticonContent, List<SelectableToggleItem> emoticonItems,
            out DeckPageController.EquippedSlotRefs[] equippedSlots)
        {
            // Non misurato esplicitamente dall'utente (solo gli slot sotto lo sono): posizionato
            // con lo stesso margine sinistro/larghezza (59/959) appena sopra la riga slot (y=365).
            var headerRt = CreateUIObject("EquippedHeader", emoticonContent);
            SetTopLeft(headerRt, EmoMarginLeft, 322f, EmoContentW, 35f);
            AddText(headerRt, $"EQUIPAGGIATE · {MaxEquippedEmoticons} max", 26f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.Left);

            equippedSlots = new DeckPageController.EquippedSlotRefs[MaxEquippedEmoticons];
            for (int i = 0; i < MaxEquippedEmoticons; i++)
            {
                equippedSlots[i] = CreateEquippedSlot(emoticonContent, "EquippedSlot_" + i,
                    EmoEquippedX[i], EmoEquippedY, EmoEquippedWidth, EmoEquippedHeight);
            }

            int unlockedCount = EmoticonNames.Length;
            int totalCount = EmoticonNames.Length + EmoticonLockedCount;
            // Header "COLLEZIONE X/Y" non misurato esplicitamente: stessi margini, appena sopra
            // la barra (y=721) con lo stesso ritmo verticale gia' in uso altrove nel file.
            CreateCollectionHeader(emoticonContent, 677f, unlockedCount, totalCount, EmoMarginLeft, EmoContentW);
            CreateProgressBar(emoticonContent, EmoProgressBarY, unlockedCount, totalCount, EmoMarginLeft, EmoProgressBarWidth, EmoProgressBarHeight);

            int totalCells = EmoticonNames.Length + EmoticonLockedCount;
            for (int i = 0; i < totalCells; i++)
            {
                int row = i / 4;
                int col = i % 4;
                float x = EmoGridColX[col];
                float y = EmoGridRowY[row];

                if (i < EmoticonNames.Length)
                {
                    bool selected = i == 0 || i == 1; // Risata + Arrabbiato equipaggiate di default
                    var item = CreateEmoticonUnlockedCell(emoticonContent, "EmoCell_" + EmoticonNames[i], EmoticonNames[i], EmoticonSprites[i], x, y, selected);
                    emoticonItems.Add(item);
                }
                else
                {
                    CreateLockedCell(emoticonContent, "EmoCell_Locked_" + i, x, y, EmoCellWidth, EmoCellHeight);
                }
            }
        }

        private static DeckPageController.EquippedSlotRefs CreateEquippedSlot(RectTransform parent, string name, float x, float y, float width, float height)
        {
            var slot = CreateUIObject(name, parent);
            SetTopLeft(slot, x, y, width, height);

            // --- Stato "pieno" (icona + nome + pulsante rimuovi) - §3.1 ---
            // Niente piu' AddGlow qui: bagliore giallo sfocato dietro lo slot segnalato
            // esplicitamente dall'utente come inutile/brutto ("fascio giallo che fa vomitare") -
            // il bordo oro (AddFillLine) basta gia' da solo a indicare lo stato equipaggiato.
            var filled = CreateUIObject("Filled", slot);
            StretchFill(filled);
            AddFillLine(filled, "panel_fill_r24", EmoSlotFill, GoldRingColor, 6f);

            var iconRt = CreateUIObject("Icon", filled);
            SetAnchoredRect(iconRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(128f, 128f), new Vector2(0f, -14f));
            var icon = AddSpriteImage(iconRt, "emo_risata", preserveAspect: true);

            var nameRt = CreateUIObject("NameText", filled);
            SetAnchoredRect(nameRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(width - 16f, 32f), new Vector2(0f, 16f));
            var nameText = AddText(nameRt, "", 22f, FontStyles.Normal, EmoEquippedName, TextAlignmentOptions.Center);

            var removeRt = CreateUIObject("RemoveButton", filled);
            SetAnchoredRect(removeRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(52f, 52f), new Vector2(-2f, -2f));
            var removeHit = removeRt.gameObject.AddComponent<Image>();
            removeHit.color = new Color(0f, 0f, 0f, 0f);
            removeHit.raycastTarget = true;
            var removeButton = removeRt.gameObject.AddComponent<Button>();
            removeButton.targetGraphic = removeHit;
            // Cerchio rosso con bordo chiaro dietro la X (era la sola icona nuda, minuscola e
            // senza contrasto - segnalato esplicitamente dall'utente): frame_round e' lo stesso
            // sprite circolare gia' usato per le icone Accuso in questo file, qui tinto rosso
            // sotto e l'icona X bianca sopra, cosi' il bottone si vede chiaramente sul bordo
            // della cella invece di sparire.
            var removeBgRt = CreateUIObject("Bg", removeRt);
            SetAnchoredRect(removeBgRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 40f), Vector2.zero);
            var removeBgImg = AddSpriteImage(removeBgRt, "frame_round", preserveAspect: true);
            removeBgImg.color = AccusoBadgeRed;
            var removeIconRt = CreateUIObject("Icon", removeRt);
            SetAnchoredRect(removeIconRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(20f, 20f), Vector2.zero);
            var removeIconImg = AddSpriteImage(removeIconRt, "ic_x", preserveAspect: true);
            removeIconImg.color = Color.white;

            // --- Stato "vuoto" (slot libero) - §3.2: FILL scuro + bordo TRATTEGGIATO, mai il
            // RING pieno usato per lo stato equipaggiato (visivamente distinti, non lo stesso
            // riquadro con solo l'icona cambiata). Icona "+" neutra (ic_plus_circle, non
            // btn_plus_green che ha un verde gia' cotto nel PNG e non va tintato).
            var empty = CreateUIObject("Empty", slot);
            StretchFill(empty);
            var emptyFillRt = CreateUIObject("Fill", empty);
            StretchFill(emptyFillRt);
            var emptyFill = AddSpriteImage(emptyFillRt, "panel_fill_r24", sliced: true);
            emptyFill.color = EmoEmptyFill;
            CreateDashedBorder(empty, width, height, EmoEmptyDashColor);

            // Bounding box ESATTA fornita dall'utente per lo slot vuoto (x=829,y=451,w=78,h=78 in
            // coordinate assolute canvas, slot3 a x=715 => offset locale 114,86 dal top-left dello
            // slot - generalizzato qui perche' "Empty" puo' comparire su un qualunque slot).
            var plusRt = CreateUIObject("PlusIcon", empty);
            SetTopLeft(plusRt, 114f, 86f, 78f, 78f);
            var plusImg = AddSpriteImage(plusRt, "ic_plus_circle", preserveAspect: true);
            plusImg.color = new Color(0.706f, 0.816f, 0.918f, 1f); // #B4D0EA, chiaro e ben visibile su EmoEmptyFill
            var emptyTextRt = CreateUIObject("Text", empty);
            SetAnchoredRect(emptyTextRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(width - 16f, 32f), new Vector2(0f, 16f));
            AddText(emptyTextRt, "Slot libero", 21f, FontStyles.Normal, EmoEmptyText, TextAlignmentOptions.Center);

            return new DeckPageController.EquippedSlotRefs
            {
                Icon = icon,
                NameText = nameText,
                RemoveButton = removeButton,
                FilledGroup = filled.gameObject,
                EmptyGroup = empty.gameObject,
            };
        }

        private static SelectableToggleItem CreateEmoticonUnlockedCell(RectTransform grid, string name, string label,
            string spriteName, float x, float y, bool selected)
        {
            var cell = CreateUIObject(name, grid);
            SetTopLeft(cell, x, y, EmoCellWidth, EmoCellHeight);

            AddFillLine(cell, "panel_fill_r24", EmoGridFill, LineRingColor);
            // includeGlow=false: stesso bagliore giallo sfocato segnalato dall'utente come
            // inutile/brutto sugli slot equipaggiati, tolto anche qui per coerenza - il bordo
            // oro (dentro CreateSelectionIndicator) basta gia' da solo.
            var selectionIndicator = CreateSelectionIndicator(cell, EmoCellWidth, EmoCellHeight, false, DeckEquippedGlow, 0f, selected, EmoGridFill);

            // Icona ingrandita (era 96, poi 124, sproporzionata rispetto al mockup) e ricentrata:
            // con la nuova EmoCellHeight=211 (era 171) c'e' piu' spazio verticale - label a y=8
            // alta 24 => bordo superiore label a 32; icona 148 con offset -10 arriva a
            // 211-10-148=53, gap di 21px verso la label, ancora comodo.
            var iconRt = CreateUIObject("Icon", cell);
            SetAnchoredRect(iconRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(148f, 148f), new Vector2(0f, -10f));
            AddSpriteImage(iconRt, spriteName, preserveAspect: true);

            var nameRt = CreateUIObject("NameText", cell);
            SetAnchoredRect(nameRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(EmoCellWidth - 12f, 24f), new Vector2(0f, 8f));
            AddText(nameRt, label, 18f, FontStyles.Normal, EmoGridName, TextAlignmentOptions.Center);

            var checkRt = CreateUIObject("CheckIcon", cell);
            SetAnchoredRect(checkRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(40f, 40f), new Vector2(-4f, -4f));
            var checkImg = AddSpriteImage(checkRt, "ic_check", preserveAspect: true);
            checkImg.color = GreenCheck;
            checkRt.gameObject.SetActive(selected);

            var hit = cell.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;
            cell.gameObject.AddComponent<Button>().targetGraphic = hit;

            var toggle = cell.gameObject.AddComponent<SelectableToggleItem>();
            var so = new SerializedObject(toggle);
            so.FindProperty("checkIcon").objectReferenceValue = checkRt.gameObject;
            so.FindProperty("glowObject").objectReferenceValue = selectionIndicator;
            so.ApplyModifiedPropertiesWithoutUndo();

            return toggle;
        }

        private static void CreateLockedCell(RectTransform grid, string name, float x, float y, float width, float height)
        {
            var cell = CreateUIObject(name, grid);
            SetTopLeft(cell, x, y, width, height);

            AddFillLine(cell, "panel_fill_r24", EmoLockedFill, LineRingColor);

            var lockRt = CreateUIObject("LockIcon", cell);
            SetAnchoredRect(lockRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(52f, 52f), new Vector2(0f, -18f - 30f));
            AddSpriteImage(lockRt, "ic_lock", preserveAspect: true);

            var nameRt = CreateUIObject("NameText", cell);
            SetAnchoredRect(nameRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(width - 12f, 30f), new Vector2(0f, 12f));
            AddText(nameRt, "Bloccata", 19f, FontStyles.Normal, EmoLockedText, TextAlignmentOptions.Center);
        }

        // ------------------------------------------------------------------
        // Tab Accuso: badge + box "IN USO" (icona+testo orizzontale) + collezione + lista
        // ------------------------------------------------------------------

        private static void BuildAccusoTab(RectTransform accusoContent, List<SelectableToggleItem> accusoItems)
        {
            CreateBadge(accusoContent, 0f, "IN USO", 200f);

            var featured = AccusoList[0];
            const float accusoBoxHeight = 282f;
            var box = CreateUIObject("FeaturedBox", accusoContent);
            SetTopLeft(box, 0f, 58f, ContentWidth, accusoBoxHeight);
            // ASSET_MAPPING_Collezione.md §4.1: "stessa struttura di §2.1" - bagliore + FILL+RING
            // r30, non sq_gold (vedi nota identica in BuildMazzoTab).
            AddGlow(box, "panel_ring_r30", MazzoFeaturedGlow, ContentWidth, accusoBoxHeight, 30f);
            AddFillLine(box, "panel_fill_r30", AccusoFeaturedFill, GoldRingColor, 6f);

            var iconFrameRt = CreateUIObject("IconFrame", box);
            SetAnchoredRect(iconFrameRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(160f, 176f), new Vector2(30f, 0f));
            var iconFrameImg = AddSpriteImage(iconFrameRt, "frame_round", preserveAspect: true);
            iconFrameImg.color = new Color(0.72f, 0.24f, 0.20f, 1f);
            var iconRt = CreateUIObject("Icon", iconFrameRt);
            SetAnchoredRect(iconRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(76f, 76f), Vector2.zero);
            AddSpriteImage(iconRt, featured.Icon, preserveAspect: true);

            var textX = 210f;
            var textWidth = ContentWidth - textX - 24f;

            var titleRt = CreateUIObject("Title", box);
            SetTopLeft(titleRt, textX, 36f, textWidth, 40f);
            AddText(titleRt, featured.Name.ToUpperInvariant(), 30f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Left);

            var subtitleRt = CreateUIObject("Subtitle", box);
            SetTopLeft(subtitleRt, textX, 80f, textWidth, 32f);
            AddText(subtitleRt, "Standard · sbloccata", 21f, FontStyles.Normal, MutedBlue, TextAlignmentOptions.Left);

            var descRt = CreateUIObject("Description", box);
            SetTopLeft(descRt, textX, 120f, textWidth, 72f);
            var descText = AddText(descRt, AccusoFeaturedDescription, 22f, FontStyles.Normal, _theme.Cream, TextAlignmentOptions.Left);
            descText.enableWordWrapping = true;

            var previewRt = CreateUIObject("PreviewButton", box);
            SetTopLeft(previewRt, textX, 198f, 220f, 56f);
            var previewBg = AddSpriteImage(previewRt, "btn_blue_long", raycastTarget: true, sliced: true); // ASSET_MAPPING_Collezione.md §4.1/§5: era btn_blue_mid, mai un caso approvato
            var previewTextRt = CreateUIObject("Text", previewRt);
            StretchFill(previewTextRt);
            AddText(previewTextRt, "ANTEPRIMA", 20f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
            var previewButton = previewRt.gameObject.AddComponent<Button>();
            previewButton.targetGraphic = previewBg;
            previewButton.onClick.AddListener(() => Debug.Log("[DeckPageController] Anteprima Accuso non ancora implementata."));

            int unlockedCount = AccusoList.Count(a => a.Unlocked);
            CreateCollectionHeader(accusoContent, 360f, unlockedCount, AccusoList.Length);
            CreateProgressBar(accusoContent, 404f, unlockedCount, AccusoList.Length);

            float rowY = 438f;
            const float rowHeight = 140f;
            const float rowGap = 20f;
            for (int i = 0; i < AccusoList.Length; i++)
            {
                var info = AccusoList[i];
                bool selected = i == 0;
                var item = CreateAccusoRow(accusoContent, "AccusoRow_" + info.Name, info, rowY, rowHeight, selected);
                if (item != null)
                {
                    accusoItems.Add(item);
                }
                rowY += rowHeight + rowGap;
            }
        }

        private static SelectableToggleItem CreateAccusoRow(RectTransform parent, string name, AccusoInfo info,
            float y0, float height, bool selected)
        {
            var row = CreateUIObject(name, parent);
            SetTopLeft(row, 0f, y0, ContentWidth, height);

            AddFillLine(row, "panel_fill_r24", info.Unlocked ? AccusoEquippedFill : AccusoLockedFill, LineRingColor);

            GameObject selectionIndicator = null;
            if (info.Unlocked)
            {
                // §4.2 non elenca un bagliore sfocato per le righe (a differenza di celle mazzo/
                // emoticon in §2.2/§3.3): solo il RING oro sovrapposto, niente glow.
                selectionIndicator = CreateSelectionIndicator(row, ContentWidth, height, false, default, 0f, selected, AccusoEquippedFill);
            }

            var iconFrameRt = CreateUIObject("IconFrame", row);
            SetAnchoredRect(iconFrameRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(96f, 106f), new Vector2(22f, 0f));
            var iconFrameImg = AddSpriteImage(iconFrameRt, "frame_round", preserveAspect: true);
            iconFrameImg.color = AccusoBadgeRed;
            if (!info.Unlocked)
            {
                var veilRt = CreateUIObject("Veil", iconFrameRt);
                StretchFill(veilRt);
                var veil = veilRt.gameObject.AddComponent<Image>();
                veil.color = AccusoLockedVeil;
                veil.raycastTarget = false;
            }
            var iconRt = CreateUIObject("Icon", iconFrameRt);
            SetAnchoredRect(iconRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46f, 46f), Vector2.zero);
            var iconImg = AddSpriteImage(iconRt, info.Unlocked ? info.Icon : "ic_lock", preserveAspect: true);
            if (!info.Unlocked) iconImg.color = _theme.Gold;

            float textX = 140f;
            float textWidth = ContentWidth - textX - 190f;

            var nameRt = CreateUIObject("NameText", row);
            SetAnchoredRect(nameRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(textWidth, 36f), new Vector2(textX, 18f));
            AddText(nameRt, info.Name, 24f, FontStyles.Bold, info.Unlocked ? AccusoEquippedName : AccusoLockedName, TextAlignmentOptions.Left);

            var subtitleRt = CreateUIObject("SubtitleText", row);
            SetAnchoredRect(subtitleRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(textWidth, 30f), new Vector2(textX, -20f));
            AddText(subtitleRt, info.Subtitle, 19f, FontStyles.Normal, info.Unlocked ? AccusoEquippedSubtitle : AccusoLockedSubtitle, TextAlignmentOptions.Left);

            if (!info.Unlocked)
            {
                var priceRt = CreateUIObject("PricePill", row);
                SetAnchoredRect(priceRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(160f, 48f), new Vector2(-20f, 0f));
                AddSpriteImage(priceRt, "btn_blue_long", raycastTarget: false, sliced: true);
                var priceTextRt = CreateUIObject("Text", priceRt);
                StretchFill(priceTextRt);
                AddText(priceTextRt, info.PriceLabel, 20f, FontStyles.Bold, PriceTextColor, TextAlignmentOptions.Center);
                return null;
            }

            var equippedRt = CreateUIObject("EquippedText", row);
            SetAnchoredRect(equippedRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(160f, 40f), new Vector2(-20f, 0f));
            AddText(equippedRt, "IN USO", 22f, FontStyles.Bold, AccusoInUsoText, TextAlignmentOptions.Right);
            equippedRt.gameObject.SetActive(selected);

            var hit = row.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;
            row.gameObject.AddComponent<Button>().targetGraphic = hit;

            var toggle = row.gameObject.AddComponent<SelectableToggleItem>();
            var so = new SerializedObject(toggle);
            so.FindProperty("glowObject").objectReferenceValue = selectionIndicator;
            so.FindProperty("equippedOnlyObject").objectReferenceValue = equippedRt.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            return toggle;
        }

        // ------------------------------------------------------------------
        // Wiring gruppi + controller
        // ------------------------------------------------------------------

        private static SelectableToggleGroup WireSingleGroup(RectTransform deckPage, string name,
            List<SelectableToggleItem> items, int defaultSelectedIndex)
        {
            var groupGO = CreateUIObject(name, deckPage).gameObject;
            var group = groupGO.AddComponent<SelectableToggleGroup>();
            var so = new SerializedObject(group);
            var prop = so.FindProperty("items");
            prop.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
            so.FindProperty("defaultSelectedIndex").intValue = defaultSelectedIndex;
            so.ApplyModifiedPropertiesWithoutUndo();
            return group;
        }

        private static SelectableMultiToggleGroup WireMultiGroup(RectTransform deckPage, string name,
            List<SelectableToggleItem> items, int maxSelected, List<int> defaultIndices)
        {
            var groupGO = CreateUIObject(name, deckPage).gameObject;
            var group = groupGO.AddComponent<SelectableMultiToggleGroup>();
            var so = new SerializedObject(group);
            var prop = so.FindProperty("items");
            prop.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
            so.FindProperty("maxSelected").intValue = maxSelected;
            var idxProp = so.FindProperty("defaultSelectedIndices");
            idxProp.arraySize = defaultIndices.Count;
            for (int i = 0; i < defaultIndices.Count; i++)
            {
                idxProp.GetArrayElementAtIndex(i).intValue = defaultIndices[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return group;
        }

        private static void CreateController(RectTransform canvasRect, RectTransform deckPage,
            Button[] tabButtons, GameObject[] tabSelectedBgs, TMP_Text[] tabLabelTexts, RectTransform[] tabRects, RectTransform[] tabContents,
            SelectableToggleGroup deckGroup, Image[] fanCardArt, TMP_Text deckNameText, TMP_Text deckSubtitleText, List<string> deckLabels,
            SelectableMultiToggleGroup emoticonGroup, DeckPageController.EquippedSlotRefs[] equippedSlots,
            SelectableToggleGroup accusoGroup)
        {
            var deckSelectorTransform = FindDeepChild(canvasRect, "DeckSelector");
            var homeDeckSelectorValueText = deckSelectorTransform?.Find("ValueText")?.GetComponent<TMP_Text>();
            if (homeDeckSelectorValueText == null)
            {
                Debug.LogWarning("[DeckPageBuilder] Non ho trovato DeckSelector/ValueText in Home: la scelta del mazzo su questa pagina non si sincronizzera' con la scorciatoia rapida.");
            }

            var pagesViewportTransform = FindDeepChild(canvasRect, "PagesViewport");
            var swipeController = pagesViewportTransform?.GetComponent<PanelSwipeController>();
            int pageIndex = -1;
            if (swipeController != null)
            {
                var soSwipe = new SerializedObject(swipeController);
                var pagesProp = soSwipe.FindProperty("pages");
                for (int i = 0; i < pagesProp.arraySize; i++)
                {
                    if (pagesProp.GetArrayElementAtIndex(i).objectReferenceValue == deckPage)
                    {
                        pageIndex = i;
                        break;
                    }
                }
            }
            if (swipeController == null || pageIndex < 0)
            {
                Debug.LogWarning("[DeckPageBuilder] Non ho trovato PanelSwipeController/l'indice di DeckPage: al ritorno sulla pagina il tab attivo non verra' resettato su Mazzo.");
            }

            var controller = deckPage.gameObject.AddComponent<DeckPageController>();
            var so = new SerializedObject(controller);

            AssignButtons(so, "tabButtons", tabButtons);
            AssignObjects(so, "tabSelectedBgs", tabSelectedBgs);
            AssignTexts(so, "tabLabels", tabLabelTexts);
            AssignRects(so, "tabContents", tabContents);
            AssignRects(so, "tabRects", tabRects);
            so.FindProperty("tabNormalY").floatValue = 224f;
            so.FindProperty("tabNormalHeight").floatValue = 56f;
            so.FindProperty("tabSelectedY").floatValue = 217f;
            so.FindProperty("tabSelectedHeight").floatValue = 69f;

            so.FindProperty("deckGroup").objectReferenceValue = deckGroup;
            var fanProp = so.FindProperty("deckPreviewCardArt");
            fanProp.arraySize = fanCardArt.Length;
            for (int i = 0; i < fanCardArt.Length; i++)
            {
                fanProp.GetArrayElementAtIndex(i).objectReferenceValue = fanCardArt[i];
            }
            so.FindProperty("deckNameText").objectReferenceValue = deckNameText;
            so.FindProperty("deckSubtitleText").objectReferenceValue = deckSubtitleText;
            var labelsProp = so.FindProperty("deckLabels");
            labelsProp.arraySize = deckLabels.Count;
            for (int i = 0; i < deckLabels.Count; i++)
            {
                labelsProp.GetArrayElementAtIndex(i).stringValue = deckLabels[i];
            }

            var cardBackSprite = LoadSprite("card_back_green");
            var spritesProp = so.FindProperty("deckCardBackSprites");
            spritesProp.arraySize = deckLabels.Count;
            for (int i = 0; i < deckLabels.Count; i++)
            {
                spritesProp.GetArrayElementAtIndex(i).objectReferenceValue = cardBackSprite;
            }

            so.FindProperty("homeDeckSelectorValueText").objectReferenceValue = homeDeckSelectorValueText;

            so.FindProperty("emoticonGroup").objectReferenceValue = emoticonGroup;
            var namesProp = so.FindProperty("emoticonNames");
            namesProp.arraySize = EmoticonNames.Length;
            for (int i = 0; i < EmoticonNames.Length; i++)
            {
                namesProp.GetArrayElementAtIndex(i).stringValue = EmoticonNames[i];
            }
            var emoSpritesProp = so.FindProperty("emoticonSprites");
            emoSpritesProp.arraySize = EmoticonSprites.Length;
            for (int i = 0; i < EmoticonSprites.Length; i++)
            {
                emoSpritesProp.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite(EmoticonSprites[i]);
            }

            var slotIconsProp = so.FindProperty("equippedSlotIcons");
            var slotNamesProp = so.FindProperty("equippedSlotNames");
            var slotRemoveProp = so.FindProperty("equippedSlotRemoveButtons");
            var slotFilledProp = so.FindProperty("equippedSlotFilledGroup");
            var slotEmptyProp = so.FindProperty("equippedSlotEmptyGroup");
            slotIconsProp.arraySize = equippedSlots.Length;
            slotNamesProp.arraySize = equippedSlots.Length;
            slotRemoveProp.arraySize = equippedSlots.Length;
            slotFilledProp.arraySize = equippedSlots.Length;
            slotEmptyProp.arraySize = equippedSlots.Length;
            for (int i = 0; i < equippedSlots.Length; i++)
            {
                slotIconsProp.GetArrayElementAtIndex(i).objectReferenceValue = equippedSlots[i].Icon;
                slotNamesProp.GetArrayElementAtIndex(i).objectReferenceValue = equippedSlots[i].NameText;
                slotRemoveProp.GetArrayElementAtIndex(i).objectReferenceValue = equippedSlots[i].RemoveButton;
                slotFilledProp.GetArrayElementAtIndex(i).objectReferenceValue = equippedSlots[i].FilledGroup;
                slotEmptyProp.GetArrayElementAtIndex(i).objectReferenceValue = equippedSlots[i].EmptyGroup;
            }

            so.FindProperty("accusoGroup").objectReferenceValue = accusoGroup;
            so.FindProperty("swipeController").objectReferenceValue = swipeController;
            so.FindProperty("pageIndex").intValue = pageIndex;

            so.ApplyModifiedPropertiesWithoutUndo();

            if (deckNameText != null && deckLabels.Count > 0)
            {
                deckNameText.text = deckLabels[0];
                deckSubtitleText.text = "Mazzo standard · 40 carte";
            }
        }

        private static void AssignButtons(SerializedObject so, string propName, Button[] values)
        {
            var prop = so.FindProperty(propName);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void AssignObjects(SerializedObject so, string propName, GameObject[] values)
        {
            var prop = so.FindProperty(propName);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void AssignTexts(SerializedObject so, string propName, TMP_Text[] values)
        {
            var prop = so.FindProperty(propName);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void AssignRects(SerializedObject so, string propName, RectTransform[] values)
        {
            var prop = so.FindProperty(propName);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t.name == name)
                {
                    return t;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------
        // Helper generici
        // ------------------------------------------------------------------

        /// <summary>
        /// Placeholder piatto (tinta unita + bordo sottile) per riquadri/pillole troppo piccoli
        /// per uno degli sprite 9-sliced del set attuale senza stirarsi in modo visibile.
        /// Non essendo un'immagine non puo' mai "stretchare" - da sostituire quando ci sara'
        /// uno sprite dedicato per quella dimensione.
        /// </summary>
        private static Image AddFlatPlaceholder(RectTransform rt, Color fill, Color border, float borderThickness = 1.5f)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = fill;
            img.raycastTarget = false;
            if (borderThickness > 0f)
            {
                CreateThinBorder(rt, border, borderThickness);
            }
            return img;
        }

        private static void CreateThinBorder(RectTransform parent, Color color, float thickness)
        {
            var top = CreateUIObject("BorderTop", parent);
            SetAnchoredRect(top, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, thickness), Vector2.zero);
            var topImg = top.gameObject.AddComponent<Image>();
            topImg.color = color;
            topImg.raycastTarget = false;

            var bottom = CreateUIObject("BorderBottom", parent);
            SetAnchoredRect(bottom, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, thickness), Vector2.zero);
            var bottomImg = bottom.gameObject.AddComponent<Image>();
            bottomImg.color = color;
            bottomImg.raycastTarget = false;

            var left = CreateUIObject("BorderLeft", parent);
            SetAnchoredRect(left, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(thickness, 0f), Vector2.zero);
            var leftImg = left.gameObject.AddComponent<Image>();
            leftImg.color = color;
            leftImg.raycastTarget = false;

            var right = CreateUIObject("BorderRight", parent);
            SetAnchoredRect(right, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(thickness, 0f), Vector2.zero);
            var rightImg = right.gameObject.AddComponent<Image>();
            rightImg.color = color;
            rightImg.raycastTarget = false;
        }

        private static bool LoadContext()
        {
            _theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (_theme == null)
            {
                Debug.LogError($"[DeckPageBuilder] UITheme non trovato in {ThemePath}.");
                return false;
            }

            return true;
        }

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchoredRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
        }

        private static void SetTopLeft(RectTransform rt, float x0, float y0, float w, float h)
        {
            SetAnchoredRect(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(w, h), new Vector2(x0, -y0));
        }

        private static void SetTopCenter(RectTransform rt, float y0, float w, float h)
        {
            SetAnchoredRect(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(w, h), new Vector2(0f, -y0));
        }

        private static Sprite LoadSprite(string fileNameNoExt)
        {
            return DragonsHoardSprites.Find(fileNameNoExt);
        }

        private static Image AddSpriteImage(RectTransform rt, string fileNameNoExt,
            bool raycastTarget = false, bool preserveAspect = false, bool sliced = false, bool fillCenter = true)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(fileNameNoExt);
            image.raycastTarget = raycastTarget;
            image.preserveAspect = preserveAspect;
            if (sliced)
            {
                image.type = Image.Type.Sliced;
                image.fillCenter = fillCenter;
            }

            return image;
        }

        /// <summary>
        /// Sfondo standard di ogni cella/riga/slot della Collezione, per ASSET_MAPPING_Collezione.md
        /// (regola generale, sez. 5): SEMPRE FILL (panel_fill_rXX, Sliced, tinto) + RING sovrapposto
        /// sopra (panel_ring_rXX, Sliced, tinto, fillCenter=false), stessa dimensione esatta - mai
        /// uno sprite bottone (sq_blue/sq_gold/btn_*) come sfondo. Sostituisce il vecchio
        /// AddCardBackground (bordo-sprite + tinta piatta CardDark).
        /// </summary>
        /// <summary>
        /// FILL + linea piatta sottile (CreateThinBorder), MAI panel_ring_rXX: misurato pixel per
        /// pixel sul mockup originale (non a occhio) - OGNI bordo, oro incluso, e' una linea sottile
        /// di 3-7px raw (~3-6 unita' canvas alla scala 1:1080 del mockup), mai un frame decorativo
        /// spesso. panel_ring_r24/r30 hanno un bordo sorgente importato di 24-30px: essendo Sliced,
        /// quel bordo si disegna SEMPRE a quella dimensione fissa indipendentemente dalla cella -
        /// nessuna tinta puo' "assottigliarlo". Per questo va bandito ovunque in favore di una linea
        /// piatta regolabile, sia per lo stato normale/bloccato (LineRingColor) sia per quello
        /// oro/equipaggiato (GoldRingColor, leggermente piu' spessa - vedi CreateSelectionIndicator).
        /// </summary>
        private static void AddFillLine(RectTransform target, string fillSprite, Color fillColor, Color lineColor, float thickness = 3f)
        {
            // Bug reale segnalato dall'utente ("quadrati stretti e non arrotondati"): CreateThinBorder
            // disegnava 4 barre piatte ad angolo retto proprio sul bordo esterno della cella - nei
            // corner le due barre perpendicolari si sovrapponevano formando un blocco pieno che
            // squadrava visivamente l'angolo, anche se il Fill sottostante (panel_fill_r24, Sliced)
            // era gia' arrotondato. Fix: stesso identico sprite Sliced disegnato due volte - una
            // copia piu' grande (colore linea) dietro, una piu' piccola di 'thickness' per lato
            // (colore fill) sopra. Il raggio del 9-slice e' fisso in pixel sorgente indipendentemente
            // dalla dimensione del RectTransform, quindi il "gap" fra i due bordi resta uniforme
            // anche lungo la curva - bordo sottile MA arrotondato, niente piu' angoli vivi.
            var lineRt = CreateUIObject("Line", target);
            StretchFill(lineRt);
            var line = AddSpriteImage(lineRt, fillSprite, sliced: true);
            line.color = lineColor;

            var fillRt = CreateUIObject("Fill", target);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.offsetMin = new Vector2(thickness, thickness);
            fillRt.offsetMax = new Vector2(-thickness, -thickness);
            var fill = AddSpriteImage(fillRt, fillSprite, sliced: true);
            fill.color = fillColor;
        }

        /// <summary>
        /// Bagliore dietro una cella/slot equipaggiato: stesso RING ingrandito di 'expand' px
        /// per lato e tinto a bassa opacita' (nessun blur reale, stesso trucco gia' usato per il
        /// bottone GIOCA in Home) - va sempre PRIMA di Fill/Ring nella gerarchia cosi' resta dietro.
        /// </summary>
        private static GameObject AddGlow(RectTransform target, string ringSprite, Color glowColor, float width, float height, float expand)
        {
            var glowRt = CreateUIObject("Glow", target);
            SetAnchoredRect(glowRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(width + expand, height + expand), Vector2.zero);
            var glow = AddSpriteImage(glowRt, ringSprite, sliced: true);
            glow.color = glowColor;
            glowRt.SetAsFirstSibling();
            return glowRt.gameObject;
        }

        /// <summary>
        /// Indicatore "equipaggiato/in uso" di una cella/riga selezionabile: un contenitore
        /// "Selected" (bagliore opzionale + RING oro esatto sovrapposto al bordo normale della
        /// cella) mostrato/nascosto in blocco - va sul campo serializzato "glowObject" gia'
        /// esistente in SelectableToggleItem, cosi' SelectableToggleGroup lo gestisce gratis
        /// senza toccare quello script. Usato identicamente da celle mazzo, celle emoticon e
        /// righe accuso (l'unica differenza fra loro e' se includere il bagliore sfocato: le
        /// righe accuso in ASSET_MAPPING_Collezione.md §4.2 non ce l'hanno, solo il RING).
        /// </summary>
        private static GameObject CreateSelectionIndicator(RectTransform target, float width, float height,
            bool includeGlow, Color glowColor, float glowExpand, bool selected, Color innerFillColor)
        {
            var indicator = CreateUIObject("Selected", target);
            StretchFill(indicator);
            if (includeGlow)
            {
                AddGlow(indicator, "panel_ring_r24", glowColor, width, height, glowExpand);
            }
            // Bug reale (trovato via screenshot live): un'Image con alpha=0 non "buca" quella
            // sotto, resta solo invisibile - il livello Line pieno restava quindi visibile
            // ovunque, tingendo l'intera cella di oro pieno invece di un anello sottile. Fix:
            // stessa tecnica "doppio sliced" di AddFillLine (Line grande colore oro dietro, Fill
            // piu' piccolo di 'thickness' per lato SOPRA con il vero colore di riempimento della
            // cella) - qui l'indicatore sta sopra la cella normale, quindi il centro deve
            // ridipingere lo stesso colore di sfondo della cella, non lasciarlo trasparente.
            AddFillLine(indicator, "panel_fill_r24", innerFillColor, GoldRingColor, 6f);

            indicator.gameObject.SetActive(selected);
            return indicator.gameObject;
        }

        /// <summary>
        /// Bordo tratteggiato per lo slot emoticon vuoto (ASSET_MAPPING_Collezione.md §3.2):
        /// nessuno sprite tratteggiato fornito (vedi §6, "da creare quando serve"), quindi lo si
        /// disegna con tacche piatte (Image) lungo i 4 lati - dimensioni note a build time, niente
        /// da ricalcolare a runtime. Deliberatamente diverso dal RING pieno usato per lo slot
        /// equipaggiato (richiesta esplicita: non lo stesso riquadro con solo l'icona cambiata).
        /// </summary>
        private static void CreateDashedBorder(RectTransform parent, float width, float height, Color color,
            float thickness = 3f, float dash = 12f, float gap = 8f)
        {
            CreateDashRow(parent, color, thickness, dash, gap, width, new Vector2(0f, 1f), new Vector2(0f, 1f), horizontal: true);   // top
            CreateDashRow(parent, color, thickness, dash, gap, width, new Vector2(0f, 0f), new Vector2(0f, 0f), horizontal: true);   // bottom
            CreateDashRow(parent, color, thickness, dash, gap, height, new Vector2(0f, 0f), new Vector2(0f, 0f), horizontal: false); // left
            CreateDashRow(parent, color, thickness, dash, gap, height, new Vector2(1f, 0f), new Vector2(1f, 0f), horizontal: false); // right
        }

        private static void CreateDashRow(RectTransform parent, Color color, float thickness, float dash, float gap,
            float length, Vector2 anchor, Vector2 pivot, bool horizontal)
        {
            float step = dash + gap;
            int count = Mathf.Max(1, Mathf.FloorToInt((length + gap) / step));
            float used = count * step - gap;
            float offset = (length - used) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float pos = offset + i * step;
                var seg = CreateUIObject("Dash", parent);
                var size = horizontal ? new Vector2(dash, thickness) : new Vector2(thickness, dash);
                var anchoredPos = horizontal ? new Vector2(pos, 0f) : new Vector2(0f, pos);
                SetAnchoredRect(seg, anchor, anchor, pivot, size, anchoredPos);
                var img = seg.gameObject.AddComponent<Image>();
                img.color = color;
                img.raycastTarget = false;
            }
        }

        private static TextMeshProUGUI AddText(RectTransform rt, string content, float fontSize,
            FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;
        }

        private static Color Rgba(int r, int g, int b, int a)
        {
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }
    }
}
