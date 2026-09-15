using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Scheletro geometrico (wireframe) della schermata Emoticon: rettangoli placeholder colorati
    /// + etichette di debug, nessun asset finale (niente emoji/icone/sprite decorativi).
    ///
    /// Fase 5 (refactor responsive): Canvas/Background (ignora la Safe Area, copre notch e bordi
    /// fisici) + Canvas/SafeArea (SafeAreaFitter) con dentro HeaderArea, MainContent
    /// (Tabs/EquippedSection/CollectionSection) e BottomNav. Le righe/griglie ripetitive (Tabs,
    /// EquippedSection, la griglia Collection, BottomNav) NON usano piu' coordinate X individuali
    /// per figlio: usano HorizontalLayoutGroup/GridLayoutGroup con padding/spacing/cellSize, cosi'
    /// la larghezza dei container guida i figli invece del contrario. Le coordinate Y dei
    /// container principali restano invariate rispetto alla Fase 4 (nessun cambiamento verticale
    /// in questo giro). Ogni RectTransform: localScale=(1,1,1). Vedi EmoticonScreenExactBuilder
    /// per la versione con asset veri (rimane intatta, disattivata qui sotto).
    /// </summary>
    public static class EmoticonWireframeBuilder
    {
        private const string ScenePath = "Assets/Scenes/HomeScreen.unity";
        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;

        private static readonly Color BgColor = new Color(0.04f, 0.05f, 0.10f, 1f);       // blu molto scuro uniforme
        private static readonly Color TabColor = new Color(0.20f, 0.24f, 0.34f, 1f);
        private static readonly Color TabSelectedColor = new Color(0.10f, 0.55f, 0.50f, 1f);
        private static readonly Color EquippedColor = new Color(0.16f, 0.30f, 0.42f, 1f);
        private static readonly Color PlusColor = new Color(0.30f, 0.55f, 0.45f, 1f);
        private static readonly Color BarBgColor = new Color(0.10f, 0.10f, 0.14f, 1f);
        private static readonly Color BarFillColor = new Color(0.75f, 0.60f, 0.20f, 1f);
        private static readonly Color CardColor = new Color(0.22f, 0.26f, 0.36f, 1f);
        private static readonly Color CardBorder = new Color(0.55f, 0.60f, 0.70f, 1f);
        private static readonly Color NavColor = new Color(0.08f, 0.09f, 0.15f, 1f);
        private static readonly Color PillColor = new Color(0.75f, 0.60f, 0.20f, 1f);
        private static readonly Color LabelWhite = Color.white;
        private static readonly Color EquippedBorder = new Color(0.85f, 0.65f, 0.20f, 1f); // bordo oro semplice per equipped/card01-02
        private static readonly Color CheckGreen = new Color(0.30f, 0.80f, 0.45f, 1f);
        private static readonly Color RemoveBg = new Color(0.55f, 0.20f, 0.20f, 1f);
        private static readonly Color LockColor = new Color(0.80f, 0.75f, 0.55f, 1f);

        // Fase 2 - contenuti reali per le 6 emoticon sbloccate (colore placeholder al posto
        // dell'emoji finale, come richiesto: "elementi interni semplici", non asset definitivi).
        private static readonly string[] EmotionNames = { "Risata", "Arrabbiato", "Sorpreso", "Pensieroso", "Triste", "Furbo" };
        private static readonly Color[] EmotionColors =
        {
            new Color(1.00f, 0.85f, 0.20f, 1f), // Risata - giallo
            new Color(0.85f, 0.25f, 0.20f, 1f), // Arrabbiato - rosso
            new Color(0.95f, 0.60f, 0.25f, 1f), // Sorpreso - arancio
            new Color(0.55f, 0.60f, 0.70f, 1f), // Pensieroso - grigio-blu
            new Color(0.35f, 0.55f, 0.85f, 1f), // Triste - blu
            new Color(0.35f, 0.75f, 0.45f, 1f), // Furbo - verde
        };

        private static Sprite _knobSprite;
        private static Sprite _checkmarkSprite;

        // Fase 2.5 - Header: colori placeholder, nessun asset finale.
        private static readonly Color AvatarFrameColor = new Color(0.85f, 0.65f, 0.20f, 1f);
        private static readonly Color AvatarFillColor = new Color(0.30f, 0.40f, 0.55f, 1f);
        private static readonly Color LevelBadgeColor = new Color(0.85f, 0.65f, 0.20f, 1f);
        private static readonly Color NamePlateColor = new Color(0.16f, 0.20f, 0.30f, 1f);
        private static readonly Color CoinColor = new Color(1.00f, 0.85f, 0.20f, 1f);
        private static readonly Color AddButtonColor = new Color(0.30f, 0.75f, 0.40f, 1f);
        private static readonly Color EnergyIconColor = new Color(0.40f, 0.85f, 0.35f, 1f);
        private static readonly Color EnergyBarBgColor = new Color(0.10f, 0.10f, 0.14f, 1f);
        private static readonly Color EnergyBarFillColor = new Color(0.40f, 0.85f, 0.35f, 1f);
        private static readonly Color ProgressPillColor = new Color(0.10f, 0.14f, 0.22f, 1f);
        private static readonly Color ProgressPillBorder = new Color(0.40f, 0.55f, 0.75f, 1f);

        // Fase 4: shift verticale cumulativo tra sezioni (INVARIATO in questo giro - "VERTICALE:
        // per ora NON cambiare il look attuale").
        private const float TabsYShift = 70f;
        private const float EquippedYShift = 140f;
        private const float CollectionYShift = 210f;

        private static readonly Dictionary<string, Vector4> Targets = new Dictionary<string, Vector4>();
        private static readonly Dictionary<string, RectTransform> Built = new Dictionary<string, RectTransform>();
        private static readonly Dictionary<string, Vector2> ExtraOffsets = new Dictionary<string, Vector2>();

        private static readonly Dictionary<string, Vector4> HeaderTargets = new Dictionary<string, Vector4>();
        private static readonly Dictionary<string, RectTransform> HeaderBuilt = new Dictionary<string, RectTransform>();

        private static readonly Dictionary<string, Vector4> BottomNavTargets = new Dictionary<string, Vector4>();
        private static readonly Dictionary<string, RectTransform> BottomNavBuilt = new Dictionary<string, RectTransform>();

        [MenuItem("Tools/Dragons Hoard/Build Emoticon WIREFRAME")]
        private static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[EmoticonWireframeBuilder] Nessun Canvas in HomeScreen.unity.");
                return;
            }
            var canvasRect = (RectTransform)canvas.transform;

            var previous = canvasRect.Find("EmoticonScreenExact");
            if (previous != null) previous.gameObject.SetActive(false);

            foreach (var childName in new[] { "Background", "SafeArea" })
            {
                var old = canvasRect.Find(childName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            Targets.Clear();
            Built.Clear();
            HeaderTargets.Clear();
            HeaderBuilt.Clear();
            BottomNavTargets.Clear();
            BottomNavBuilt.Clear();
            ExtraOffsets.Clear();

            // --- Background: figlio diretto di Canvas, full screen, IGNORA la Safe Area. ---
            var background = CreateUIObject("Background", canvasRect);
            SetTopLeft(background, 0f, 0f, RefWidth, RefHeight);
            AddFlat(background, BgColor);

            // --- SafeArea (SafeAreaFitter, invariato dalla Fase 3) ---
            var safeArea = CreateUIObject("SafeArea", canvasRect);
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.pivot = new Vector2(0.5f, 0.5f);
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
            var safeAreaFitter = safeArea.gameObject.AddComponent<SafeAreaFitter>();
            var safeAreaSo = new SerializedObject(safeAreaFitter);
            safeAreaSo.FindProperty("debugLog").boolValue = true;
            safeAreaSo.ApplyModifiedPropertiesWithoutUndo();

            // --- HeaderArea (rinominato da "Header" - stessa posizione/contenuto interno) ---
            var headerArea = CreateUIObject("HeaderArea", safeArea);
            headerArea.anchorMin = headerArea.anchorMax = new Vector2(0f, 1f);
            headerArea.pivot = new Vector2(0f, 1f);
            BuildHeader(headerArea);

            // --- MainContent: NUOVO contenitore richiesto dalla gerarchia target, raggruppa
            // Tabs/EquippedSection/CollectionSection. Pure container alla stessa origine (0,0)
            // di SafeArea -> le Y assolute delle 3 sotto-sezioni restano identiche alla Fase 4.
            var mainContent = CreateUIObject("MainContent", safeArea);
            mainContent.anchorMin = mainContent.anchorMax = new Vector2(0f, 1f);
            mainContent.pivot = new Vector2(0f, 1f);

            BuildTabs(mainContent);
            BuildEquippedSection(mainContent);
            BuildCollectionSection(mainContent);
            BuildBottomNav(safeArea);

            EnforceUniformScale(canvasRect.Find("Background"));
            EnforceUniformScale(safeArea);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            RunDebugVerification();
            RunHeaderVerification();
            RunBottomNavVerification();

            Debug.Log("[EmoticonWireframeBuilder] Wireframe ricostruito (Fase 5, responsive) in HomeScreen.unity: Canvas/Background + Canvas/SafeArea/HeaderArea,MainContent(Tabs,EquippedSection,CollectionSection),BottomNav. Salvato.");
        }

        // ------------------------------------------------------------------
        // Fase 5 - TABS: HorizontalLayoutGroup, niente X individuali per tab.
        // ------------------------------------------------------------------

        private static void BuildTabs(RectTransform mainContent)
        {
            float tabsTop = 217f + TabsYShift;
            const float tabsHeight = 69f; // altezza massima (EMOTICON selected) - definisce l'altezza della riga

            var tabsRow = CreateUIObject("Tabs", mainContent);
            SetTopLeft(tabsRow, 0f, tabsTop, RefWidth, tabsHeight);
            var layout = tabsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            // Padding/spacing ESATTI derivati dal reference design (60 sx, 1080-708-310=62 dx,
            // 384-(60+310)=14 di spacing) - ora sono parametri del container, non piu'
            // ricalcolati a mano per ogni tab.
            layout.padding = new RectOffset(60, 62, 0, 0);
            layout.spacing = 14f;
            // MiddleLeft sull'asse trasversale (verticale): centra i tab piu' bassi (56) dentro
            // la riga alta 69 - riproduce l'effetto "EMOTICON spunta piu' in alto" senza bisogno
            // di una Y individuale per tab (differenza vs l'originale: <1px, invisibile).
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var tabMazzi = CreateFixedSizeChild(tabsRow, "Tab_Mazzi", 310f, 56f);
            AddPlaceholderVisual(tabMazzi, TabColor, "MAZZI", labelFontSize: 26f, labelStyle: FontStyles.Bold);

            var tabEmoticon = CreateFixedSizeChild(tabsRow, "Tab_Emoticon_Selected", 310f, 69f);
            AddPlaceholderVisual(tabEmoticon, TabSelectedColor, "EMOTICON", labelFontSize: 27f, labelStyle: FontStyles.Bold);

            var tabAccusi = CreateFixedSizeChild(tabsRow, "Tab_Accusi", 310f, 56f);
            AddPlaceholderVisual(tabAccusi, TabColor, "ACCUSI", labelFontSize: 26f, labelStyle: FontStyles.Bold);

            LayoutRebuilder.ForceRebuildLayoutImmediate(tabsRow);

            var rowOffset = new Vector2(0f, tabsTop);
            RegisterTarget("Tab_Mazzi", 60f, 224f + TabsYShift, 310f, 56f, tabMazzi, rowOffset);
            RegisterTarget("Tab_Emoticon_Selected", 384f, 217f + TabsYShift, 310f, 69f, tabEmoticon, rowOffset);
            RegisterTarget("Tab_Accusi", 708f, 224f + TabsYShift, 310f, 56f, tabAccusi, rowOffset);
        }

        // ------------------------------------------------------------------
        // Fase 5 - EQUIPPED SECTION: HorizontalLayoutGroup per la riga dei 3 slot.
        // ------------------------------------------------------------------

        private static void BuildEquippedSection(RectTransform mainContent)
        {
            var equippedSection = CreateUIObject("EquippedSection", mainContent);
            equippedSection.anchorMin = equippedSection.anchorMax = new Vector2(0f, 1f);
            equippedSection.pivot = new Vector2(0f, 1f);

            var equippedHeader = CreateUIObject("EquippedHeader", equippedSection);
            SetTopLeft(equippedHeader, 59f, 322f + EquippedYShift, 959f, 35f);
            AddText(equippedHeader, "EQUIPAGGIATE · 3 max", 26f, TextAlignmentOptions.Left, LabelWhite);

            float rowTop = 365f + EquippedYShift;
            const float rowHeight = 249f;
            var equippedRow = CreateUIObject("EquippedRow", equippedSection);
            SetTopLeft(equippedRow, 0f, rowTop, RefWidth, rowHeight);
            var layout = equippedRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            // 59 sx, 1080-715-307=58 dx, 387-(59+307)=21 di spacing - dal reference design.
            layout.padding = new RectOffset(59, 58, 0, 0);
            layout.spacing = 21f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var slot1 = CreateFixedSizeChild(equippedRow, "EquippedSlot1", 307f, rowHeight);
            AddPlaceholderVisual(slot1, EquippedColor, null, border: EquippedBorder);
            AddEmojiCircle(slot1, EmotionColors[0], 128f, 10f);
            AddBottomLabel(slot1, "Risata", 307f, 22f, FontStyles.Normal);
            AddRemoveButton(slot1);

            var slot2 = CreateFixedSizeChild(equippedRow, "EquippedSlot2", 307f, rowHeight);
            AddPlaceholderVisual(slot2, EquippedColor, null, border: EquippedBorder);
            AddEmojiCircle(slot2, EmotionColors[1], 128f, 10f);
            AddBottomLabel(slot2, "Arrabbiato", 307f, 22f, FontStyles.Normal);
            AddRemoveButton(slot2);

            var slot3 = CreateFixedSizeChild(equippedRow, "EquippedSlot3", 307f, rowHeight);
            AddPlaceholderVisual(slot3, EquippedColor, null);

            // FreeSlotPlus: bbox LOCALE allo slot3 (114,86) invariata - lo slot stesso e'
            // posizionato dal layout group, questo figlio resta un semplice offset interno.
            var freeSlotPlus = CreateUIObject("FreeSlotPlus", slot3);
            SetTopLeft(freeSlotPlus, 114f, 86f, 78f, 78f);
            AddFlat(freeSlotPlus, PlusColor);
            var freeSlotPlusLabel = CreateUIObject("Label", freeSlotPlus);
            StretchFill(freeSlotPlusLabel);
            AddText(freeSlotPlusLabel, "+", 40f, TextAlignmentOptions.Center, LabelWhite);
            AddBottomLabel(slot3, "Slot libero", 307f, 22f, FontStyles.Normal);

            LayoutRebuilder.ForceRebuildLayoutImmediate(equippedRow);

            var rowOffset = new Vector2(0f, rowTop);
            RegisterTarget("EquippedSlot1", 59f, 365f + EquippedYShift, 307f, rowHeight, slot1, rowOffset);
            RegisterTarget("EquippedSlot2", 387f, 365f + EquippedYShift, 307f, rowHeight, slot2, rowOffset);
            RegisterTarget("EquippedSlot3", 715f, 365f + EquippedYShift, 307f, rowHeight, slot3, rowOffset);
            // FreeSlotPlus: assoluto = slot3(715,505) + locale(114,86) = (829,591) - verificato
            // rispetto alla posizione REALE dello slot3 dopo il layout (letta qui a valle del
            // ForceRebuildLayoutImmediate), non a un valore presunto.
            var slot3Abs = new Vector2(rowOffset.x + slot3.anchoredPosition.x, rowTop - slot3.anchoredPosition.y);
            RegisterTarget("FreeSlotPlus", 829f, 451f + EquippedYShift, 78f, 78f, freeSlotPlus, slot3Abs);
        }

        // ------------------------------------------------------------------
        // Fase 5 - COLLECTION: GridLayoutGroup per la griglia 4x3 (PRIORITARIA).
        // ------------------------------------------------------------------

        private static void BuildCollectionSection(RectTransform mainContent)
        {
            var collectionSection = CreateUIObject("CollectionSection", mainContent);
            collectionSection.anchorMin = collectionSection.anchorMax = new Vector2(0f, 1f);
            collectionSection.pivot = new Vector2(0f, 1f);

            var collectionHeader = CreateUIObject("CollectionHeader", collectionSection);
            SetTopLeft(collectionHeader, 59f, 677f + CollectionYShift, 959f, 35f);
            AddText(collectionHeader, "COLLEZIONE", 26f, TextAlignmentOptions.Left, LabelWhite);

            var counterLabelRt = CreateUIObject("CounterLabel", collectionSection);
            SetTopLeft(counterLabelRt, 59f, 677f + CollectionYShift, 959f, 35f);
            AddText(counterLabelRt, "6 / 12", 26f, TextAlignmentOptions.Right, LabelWhite);

            var barBg = AddPlaceholder(collectionSection, "ProgressBar_BG", 59f, 721f + CollectionYShift, 959f, 35f, BarBgColor, null);
            var barFill = CreateUIObject("ProgressBar_Fill", collectionSection);
            SetTopLeft(barFill, 59f, 721f + CollectionYShift, 488f, 35f);
            AddFlat(barFill, BarFillColor);
            RegisterTarget("ProgressBar_Fill", 59f, 721f + CollectionYShift, 488f, 35f, barFill);

            // GridLayoutGroup: 4 colonne fisse, cellSize/spacing/padding.left ESATTI dal reference
            // design. Il padding destro non e' un parametro di GridLayoutGroup (viene "quello che
            // resta"): 59 + 4*228 + 3*17 = 1022, su 1080 restano 58 - combacia esattamente col
            // margine destro richiesto, senza doverlo specificare a parte.
            float gridTop = 789f + CollectionYShift;
            const float cellW = 228f, cellH = 211f, gapX = 17f, gapY = 19f;
            const float gridHeight = cellH * 3f + gapY * 2f;
            var grid = CreateUIObject("CollectionGrid", collectionSection);
            SetTopLeft(grid, 0f, gridTop, RefWidth, gridHeight);
            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(59, 58, 0, 0);
            gridLayout.cellSize = new Vector2(cellW, cellH);
            gridLayout.spacing = new Vector2(gapX, gapY);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;

            var cards = new RectTransform[12];
            for (int i = 0; i < 12; i++)
            {
                string name = "Card" + (i + 1).ToString("00");
                bool unlocked = i < EmotionNames.Length;
                bool equipped = i == 0 || i == 1;

                var card = CreateUIObject(name, grid);
                AddPlaceholderVisual(card, CardColor, null, border: equipped ? EquippedBorder : CardBorder);
                cards[i] = card;

                if (unlocked)
                {
                    AddEmojiCircle(card, EmotionColors[i], 100f, 12f);
                    AddBottomLabel(card, EmotionNames[i], cellW, 18f, FontStyles.Normal);
                    if (equipped) AddCheckBadge(card);
                }
                else
                {
                    AddLockGlyph(card);
                    AddBottomLabel(card, "Bloccata", cellW, 18f, FontStyles.Normal);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(grid);

            float[] colX = { 59f, 304f, 549f, 794f };
            float[] rowYRef = { 789f + CollectionYShift, 1019f + CollectionYShift, 1249f + CollectionYShift };
            var gridOffset = new Vector2(0f, gridTop);
            for (int i = 0; i < 12; i++)
            {
                int row = i / 4;
                int col = i % 4;
                string name = "Card" + (i + 1).ToString("00");
                RegisterTarget(name, colX[col], rowYRef[row], cellW, cellH, cards[i], gridOffset);
            }
        }

        // ------------------------------------------------------------------
        // Fase 5 - BOTTOM NAV: 4 celle uguali (25% ciascuna) via HorizontalLayoutGroup, pill
        // centrata nella cella "Carte" invece di coordinate assolute.
        // ------------------------------------------------------------------

        private static void BuildBottomNav(RectTransform safeArea)
        {
            var bottomNav = CreateUIObject("BottomNav", safeArea);
            StretchFill(bottomNav);

            const float navBarBottomOffset = 0f;
            const float pillBottomOffset = 72f;
            const float navHeight = 190f;

            var navBar = CreateUIObject("NavBar", bottomNav);
            SetBottomLeft(navBar, 0f, navBarBottomOffset, RefWidth, navHeight);
            AddFlat(navBar, NavColor);
            RegisterBottomNavTarget("NavBar", 0f, navBarBottomOffset, RefWidth, navHeight, navBar);

            // NavItemsRow: stretch pieno su NavBar (stesso rect) - necessario perche' il layout
            // group calcola "sinistra/cima" rispetto al PROPRIO rect: se il contenitore avesse
            // dimensione diversa da NavBar i figli non coinciderebbero col fondo/lati reali.
            var navItemsRow = CreateUIObject("NavItemsRow", navBar);
            StretchFill(navItemsRow);
            var layout = navItemsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            // childControlWidth=true + flexibleWidth uguale su tutti i 4 figli = "ogni cella e'
            // il 25% della larghezza disponibile", non piu' 270px fissi calcolati a mano.
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            string[] labels = { "Gioca", "Carte", "Negozio", "Profilo" };
            var cells = new RectTransform[4];
            for (int i = 0; i < 4; i++)
            {
                var navItem = CreateUIObject("NavItem_" + labels[i], navItemsRow);
                var le = navItem.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                cells[i] = navItem;

                if (labels[i] == "Carte")
                {
                    // Pill CENTRATA nella cella (anchor 0.5,0 con width fissa) invece di
                    // un'anchoredPosition assoluta rispetto a tutta la BottomNav - resta centrata
                    // anche se la cella non e' esattamente 270px (es. safe area piu' larga/stretta).
                    var pill = CreateUIObject("BottomSelectedPill", navItem);
                    SetAnchoredRect(pill, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(243f, 100f), new Vector2(0f, pillBottomOffset));
                    AddFlat(pill, PillColor);
                    AddNavIcon(pill, "Carte", 40f, 12f);
                    var pillLabel = CreateUIObject("Label", pill);
                    SetAnchoredRect(pillLabel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(220f, 26f), new Vector2(0f, 8f));
                    AddText(pillLabel, "CARTE", 22f, TextAlignmentOptions.Center, Color.black, FontStyles.Bold);
                }
                else
                {
                    AddNavIcon(navItem, labels[i], 60f, 18f);
                    var navLabelRt = CreateUIObject("Label", navItem);
                    SetAnchoredRect(navLabelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(262f, 28f), new Vector2(0f, 20f));
                    AddText(navLabelRt, labels[i], 18f, TextAlignmentOptions.Center, LabelWhite);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(navItemsRow);

            for (int i = 0; i < 4; i++)
            {
                RegisterBottomNavTarget("NavItem_" + labels[i], cells[i].anchoredPosition.x, navBarBottomOffset, cells[i].rect.width, navHeight, cells[i]);
            }
            var carteCell = cells[1];
            var pillRt = (RectTransform)carteCell.Find("BottomSelectedPill");
            // Pill anchorata a (0.5,0) dentro la cella "Carte": anchoredPosition.x=0 significa
            // GIA' "centrata, zero scarto dal centro" - non un offset assoluto da convertire.
            BottomNavTargets["BottomSelectedPill"] = new Vector4(0f, pillBottomOffset, 243f, 100f);
            BottomNavBuilt["BottomSelectedPill"] = pillRt;
        }

        // ------------------------------------------------------------------
        // Fase 2.5 - HEADER (invariato: sotto-container Profile/Currency/Energy/Progress gia'
        // presenti dalla Fase 2.5, riusati cosi' come sono per il riuso richiesto in questa fase)
        // ------------------------------------------------------------------

        private static void BuildHeader(RectTransform header)
        {
            var profileArea = CreateUIObject("ProfileArea", header);
            SetTopLeft(profileArea, 55f, 25f, 180f, 150f);
            RegisterHeaderTarget("ProfileArea", 55f, 25f, 180f, 150f, profileArea);

            var avatarFrame = CreateUIObject("AvatarFrame", profileArea);
            SetTopLeft(avatarFrame, 0f, 0f, 110f, 110f);
            RegisterHeaderTarget("AvatarFrame", 55f, 25f, 110f, 110f, avatarFrame, new Vector2(55f, 25f));
            var avatarFrameImg = avatarFrame.gameObject.AddComponent<Image>();
            avatarFrameImg.sprite = KnobSprite();
            avatarFrameImg.color = AvatarFrameColor;
            avatarFrameImg.raycastTarget = false;

            var avatarPlaceholder = CreateUIObject("AvatarPlaceholder", avatarFrame);
            SetTopLeft(avatarPlaceholder, 6f, 6f, 98f, 98f);
            RegisterHeaderTarget("AvatarPlaceholder", 61f, 31f, 98f, 98f, avatarPlaceholder, new Vector2(55f, 25f));
            var avatarFillImg = avatarPlaceholder.gameObject.AddComponent<Image>();
            avatarFillImg.sprite = KnobSprite();
            avatarFillImg.color = AvatarFillColor;
            avatarFillImg.raycastTarget = false;

            var levelBadge = CreateUIObject("LevelBadge", profileArea);
            SetTopLeft(levelBadge, -15f, -10f, 40f, 40f);
            RegisterHeaderTarget("LevelBadge", 40f, 15f, 40f, 40f, levelBadge, new Vector2(55f, 25f));
            var levelBg = levelBadge.gameObject.AddComponent<Image>();
            levelBg.sprite = KnobSprite();
            levelBg.color = LevelBadgeColor;
            levelBg.raycastTarget = false;
            var levelLabelRt = CreateUIObject("Label", levelBadge);
            StretchFill(levelLabelRt);
            AddText(levelLabelRt, "1", 20f, TextAlignmentOptions.Center, Color.black, FontStyles.Bold);

            var playerName = CreateUIObject("PlayerName", profileArea);
            SetTopLeft(playerName, 0f, 115f, 150f, 32f);
            RegisterHeaderTarget("PlayerName", 55f, 140f, 150f, 32f, playerName, new Vector2(55f, 25f));
            AddFlat(playerName, NamePlateColor);
            var nameLabelRt = CreateUIObject("Label", playerName);
            StretchFill(nameLabelRt);
            AddText(nameLabelRt, "Giocatore", 16f, TextAlignmentOptions.Center, LabelWhite);

            var currencyArea = CreateUIObject("CurrencyArea", header);
            SetTopLeft(currencyArea, 250f, 45f, 260f, 50f);
            RegisterHeaderTarget("CurrencyArea", 250f, 45f, 260f, 50f, currencyArea);

            var coinIcon = CreateUIObject("CoinIcon", currencyArea);
            SetTopLeft(coinIcon, 0f, 7f, 36f, 36f);
            coinIcon.gameObject.AddComponent<Image>().sprite = KnobSprite();
            coinIcon.GetComponent<Image>().color = CoinColor;
            RegisterHeaderTarget("CoinIcon", 250f, 52f, 36f, 36f, coinIcon, new Vector2(250f, 45f));

            var coinValue = CreateUIObject("CoinValue", currencyArea);
            SetTopLeft(coinValue, 46f, 10f, 110f, 32f);
            AddText(coinValue, "1.250", 24f, TextAlignmentOptions.Left, LabelWhite, FontStyles.Bold);
            RegisterHeaderTarget("CoinValue", 296f, 55f, 110f, 32f, coinValue, new Vector2(250f, 45f));

            var addCurrencyButton = CreateUIObject("AddCurrencyButton", currencyArea);
            SetTopLeft(addCurrencyButton, 166f, 7f, 36f, 36f);
            AddFlat(addCurrencyButton, AddButtonColor);
            var addLabelRt = CreateUIObject("Label", addCurrencyButton);
            StretchFill(addLabelRt);
            AddText(addLabelRt, "+", 22f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            RegisterHeaderTarget("AddCurrencyButton", 416f, 52f, 36f, 36f, addCurrencyButton, new Vector2(250f, 45f));

            var energyArea = CreateUIObject("EnergyArea", header);
            SetTopLeft(energyArea, 530f, 45f, 220f, 50f);
            RegisterHeaderTarget("EnergyArea", 530f, 45f, 220f, 50f, energyArea);

            var energyIcon = CreateUIObject("EnergyIcon", energyArea);
            SetTopLeft(energyIcon, 0f, 10f, 30f, 30f);
            AddFlat(energyIcon, EnergyIconColor);
            RegisterHeaderTarget("EnergyIcon", 530f, 55f, 30f, 30f, energyIcon, new Vector2(530f, 45f));

            var energyBarBg = CreateUIObject("EnergyBarBG", energyArea);
            SetTopLeft(energyBarBg, 40f, 13f, 140f, 24f);
            AddFlat(energyBarBg, EnergyBarBgColor);
            RegisterHeaderTarget("EnergyBarBG", 570f, 58f, 140f, 24f, energyBarBg, new Vector2(530f, 45f));

            var energyBarFill = CreateUIObject("EnergyBarFill", energyBarBg);
            SetTopLeft(energyBarFill, 0f, 0f, 84f, 24f);
            AddFlat(energyBarFill, EnergyBarFillColor);
            RegisterHeaderTarget("EnergyBarFill", 570f, 58f, 84f, 24f, energyBarFill, new Vector2(570f, 58f));

            var energyValue = CreateUIObject("EnergyValue", energyArea);
            SetTopLeft(energyValue, 184f, 10f, 36f, 30f);
            AddText(energyValue, "48", 20f, TextAlignmentOptions.Center, LabelWhite, FontStyles.Bold);
            RegisterHeaderTarget("EnergyValue", 714f, 55f, 36f, 30f, energyValue, new Vector2(530f, 45f));

            var progressArea = CreateUIObject("ProgressArea", header);
            SetTopLeft(progressArea, 800f, 55f, 225f, 44f);
            RegisterHeaderTarget("ProgressArea", 800f, 55f, 225f, 44f, progressArea);

            var progressBarBg = CreateUIObject("ProgressBarBG", progressArea);
            SetTopLeft(progressBarBg, 0f, 0f, 225f, 44f);
            AddFlat(progressBarBg, ProgressPillBorder);
            var innerFill = CreateUIObject("Fill", progressBarBg);
            innerFill.anchorMin = Vector2.zero;
            innerFill.anchorMax = Vector2.one;
            innerFill.pivot = new Vector2(0.5f, 0.5f);
            innerFill.offsetMin = new Vector2(2f, 2f);
            innerFill.offsetMax = new Vector2(-2f, -2f);
            AddFlat(innerFill, ProgressPillColor);
            RegisterHeaderTarget("ProgressBarBG", 800f, 55f, 225f, 44f, progressBarBg, new Vector2(800f, 55f));

            var progressText = CreateUIObject("ProgressText", progressArea);
            SetTopLeft(progressText, 0f, 0f, 225f, 44f);
            AddText(progressText, "320 / 500", 22f, TextAlignmentOptions.Center, LabelWhite, FontStyles.Bold);
            RegisterHeaderTarget("ProgressText", 800f, 55f, 225f, 44f, progressText, new Vector2(800f, 55f));
        }

        private static void RegisterHeaderTarget(string name, float x, float y, float w, float h, RectTransform built, Vector2? parentOffset = null)
        {
            HeaderTargets[name] = new Vector4(x, y, w, h);
            HeaderBuilt[name] = built;
            if (parentOffset.HasValue) ExtraOffsets[name] = parentOffset.Value;
        }

        private static void RunHeaderVerification()
        {
            const float tolerance = 2f;
            int pass = 0, fail = 0;
            var report = new StringBuilder();
            report.AppendLine("[EmoticonWireframeBuilder] Header - coordinate applicate e verifica (tolleranza " + tolerance + "px):");

            foreach (var kvp in HeaderTargets)
            {
                string name = kvp.Key;
                Vector4 target = kvp.Value;
                if (!HeaderBuilt.TryGetValue(name, out var rt) || rt == null)
                {
                    report.AppendLine($"  [SKIP] {name}: RectTransform non trovato");
                    continue;
                }

                Vector2 extra = ExtraOffsets.TryGetValue(name, out var off) ? off : Vector2.zero;
                float actualX = rt.anchoredPosition.x + extra.x;
                float actualY = -rt.anchoredPosition.y + extra.y;
                float actualW = rt.sizeDelta.x;
                float actualH = rt.sizeDelta.y;

                float dx = actualX - target.x;
                float dy = actualY - target.y;
                float dw = actualW - target.z;
                float dh = actualH - target.w;
                bool ok = Mathf.Abs(dx) <= tolerance && Mathf.Abs(dy) <= tolerance && Mathf.Abs(dw) <= tolerance && Mathf.Abs(dh) <= tolerance;
                if (ok) pass++; else fail++;

                report.AppendLine($"  [{(ok ? "PASS" : "FAIL")}] {name}: anchoredPosition={rt.anchoredPosition} sizeDelta={rt.sizeDelta} scale={rt.localScale} | target=({target.x},{target.y},{target.z},{target.w}) diff=({dx:F1},{dy:F1},{dw:F1},{dh:F1})");
            }

            report.AppendLine($"[EmoticonWireframeBuilder] Header totale: {pass} PASS, {fail} FAIL su {HeaderTargets.Count} elementi.");
            if (fail > 0) Debug.LogError(report.ToString());
            else Debug.Log(report.ToString());
        }

        private static void RegisterBottomNavTarget(string name, float x, float yFromBottom, float w, float h, RectTransform built)
        {
            BottomNavTargets[name] = new Vector4(x, yFromBottom, w, h);
            BottomNavBuilt[name] = built;
        }

        private static void RunBottomNavVerification()
        {
            const float tolerance = 2f;
            int pass = 0, fail = 0;
            var report = new StringBuilder();
            report.AppendLine("[EmoticonWireframeBuilder] BottomNav (celle 25% responsive, ancorato dal fondo SafeArea) - verifica (tolleranza " + tolerance + "px):");

            foreach (var kvp in BottomNavTargets)
            {
                string name = kvp.Key;
                Vector4 target = kvp.Value;
                if (!BottomNavBuilt.TryGetValue(name, out var rt) || rt == null)
                {
                    report.AppendLine($"  [SKIP] {name}: RectTransform non trovato");
                    continue;
                }

                float actualX = rt.anchoredPosition.x;
                float actualYFromBottom = rt.anchoredPosition.y;
                float actualW = rt.rect.width;
                float actualH = rt.rect.height;

                float dx = actualX - target.x;
                float dy = actualYFromBottom - target.y;
                float dw = actualW - target.z;
                float dh = actualH - target.w;
                bool ok = Mathf.Abs(dx) <= tolerance && Mathf.Abs(dy) <= tolerance && Mathf.Abs(dw) <= tolerance && Mathf.Abs(dh) <= tolerance;
                if (ok) pass++; else fail++;

                report.AppendLine($"  [{(ok ? "PASS" : "FAIL")}] {name}: anchoredPosition={rt.anchoredPosition} rect={rt.rect} scale={rt.localScale} | target(x,yFromBottom,w,h)=({target.x},{target.y},{target.z},{target.w}) diff=({dx:F1},{dy:F1},{dw:F1},{dh:F1})");
            }

            report.AppendLine($"[EmoticonWireframeBuilder] BottomNav totale: {pass} PASS, {fail} FAIL su {BottomNavTargets.Count} elementi (su reference width 1080 - su altre larghezze le celle sono comunque 25% esatto, e' la larghezza reference stessa a variare).");
            if (fail > 0) Debug.LogError(report.ToString());
            else Debug.Log(report.ToString());
        }

        // ------------------------------------------------------------------
        // Helper - contenuti interni (placeholder semplici, nessun asset finale)
        // ------------------------------------------------------------------

        private static RectTransform AddPlaceholder(RectTransform parent, string name, float x, float y, float w, float h,
            Color color, string label, Color? border = null, float labelFontSize = 22f, FontStyles labelStyle = FontStyles.Normal)
        {
            var rt = CreateUIObject(name, parent);
            SetTopLeft(rt, x, y, w, h);
            RegisterTarget(name, x, y, w, h, rt);
            AddPlaceholderVisual(rt, color, label, border, labelFontSize, labelStyle);
            return rt;
        }

        /// <summary>Crea SOLO il contenuto visivo (fill/bordo/label) su un RectTransform GIA'
        /// posizionato altrove (da un layout group, o da SetTopLeft/SetBottomLeft a monte) -
        /// separato da AddPlaceholder cosi' i figli di un LayoutGroup non ricevono mai una
        /// posizione assoluta in conflitto con quella calcolata dal container.</summary>
        private static void AddPlaceholderVisual(RectTransform rt, Color color, string label, Color? border = null,
            float labelFontSize = 22f, FontStyles labelStyle = FontStyles.Normal)
        {
            if (border.HasValue)
            {
                var borderRt = CreateUIObject("Border", rt);
                StretchFill(borderRt);
                AddFlat(borderRt, border.Value);
                var innerRt = CreateUIObject("Fill", rt);
                innerRt.anchorMin = Vector2.zero;
                innerRt.anchorMax = Vector2.one;
                innerRt.pivot = new Vector2(0.5f, 0.5f);
                innerRt.offsetMin = new Vector2(2f, 2f);
                innerRt.offsetMax = new Vector2(-2f, -2f);
                AddFlat(innerRt, color);
            }
            else
            {
                AddFlat(rt, color);
            }

            if (!string.IsNullOrEmpty(label))
            {
                var labelRt = CreateUIObject("Label", rt);
                StretchFill(labelRt);
                AddText(labelRt, label, labelFontSize, TextAlignmentOptions.Center, LabelWhite, labelStyle);
            }
        }

        /// <summary>Figlio con dimensione FISSA (sizeDelta) ma SENZA posizione impostata - la
        /// posizione la calcola il LayoutGroup del genitore (childControlWidth/Height=false usa
        /// comunque questa sizeDelta per la spaziatura, senza sovrascriverla).</summary>
        private static RectTransform CreateFixedSizeChild(RectTransform parent, string name, float width, float height)
        {
            var rt = CreateUIObject(name, parent);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        private static void AddEmojiCircle(RectTransform parent, Color color, float size, float topOffset)
        {
            var circleRt = CreateUIObject("EmojiPlaceholder", parent);
            SetAnchoredRect(circleRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(size, size), new Vector2(0f, -topOffset));
            var img = circleRt.gameObject.AddComponent<Image>();
            img.sprite = KnobSprite();
            img.color = color;
            img.raycastTarget = false;
        }

        private static void AddBottomLabel(RectTransform parent, string text, float width, float fontSize, FontStyles style)
        {
            var labelRt = CreateUIObject("NameText", parent);
            SetAnchoredRect(labelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(width - 12f, 24f), new Vector2(0f, 8f));
            AddText(labelRt, text, fontSize, TextAlignmentOptions.Center, LabelWhite, style);
        }

        private static void AddLockGlyph(RectTransform parent)
        {
            var holder = CreateUIObject("LockIcon", parent);
            SetAnchoredRect(holder, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(52f, 52f), new Vector2(0f, -26f));

            var body = CreateUIObject("Body", holder);
            SetAnchoredRect(body, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(52f, 30f), Vector2.zero);
            AddFlat(body, LockColor);

            var shackleLeft = CreateUIObject("ShackleLeft", holder);
            SetAnchoredRect(shackleLeft, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(6f, 28f), new Vector2(-13f, 26f));
            AddFlat(shackleLeft, LockColor);

            var shackleRight = CreateUIObject("ShackleRight", holder);
            SetAnchoredRect(shackleRight, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(6f, 28f), new Vector2(13f, 26f));
            AddFlat(shackleRight, LockColor);

            var shackleTop = CreateUIObject("ShackleTop", holder);
            SetAnchoredRect(shackleTop, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(26f, 6f), new Vector2(0f, 52f));
            AddFlat(shackleTop, LockColor);
        }

        private static void AddCheckBadge(RectTransform parent)
        {
            var badgeRt = CreateUIObject("CheckBadge", parent);
            SetAnchoredRect(badgeRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(28f, 28f), new Vector2(-4f, -4f));
            var bg = badgeRt.gameObject.AddComponent<Image>();
            bg.sprite = KnobSprite();
            bg.color = CheckGreen;
            bg.raycastTarget = false;

            var checkRt = CreateUIObject("Check", badgeRt);
            SetAnchoredRect(checkRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(16f, 16f), Vector2.zero);
            var check = checkRt.gameObject.AddComponent<Image>();
            check.sprite = CheckmarkSprite();
            check.color = Color.white;
            check.raycastTarget = false;
        }

        private static void AddRemoveButton(RectTransform parent)
        {
            var btnRt = CreateUIObject("RemoveButton", parent);
            SetAnchoredRect(btnRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(32f, 32f), new Vector2(-4f, -4f));
            var bg = btnRt.gameObject.AddComponent<Image>();
            bg.sprite = KnobSprite();
            bg.color = RemoveBg;
            bg.raycastTarget = false;

            var xLabelRt = CreateUIObject("Label", btnRt);
            StretchFill(xLabelRt);
            AddText(xLabelRt, "X", 16f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        }

        private static void AddNavIcon(RectTransform parent, string kind, float size, float topOffset)
        {
            var holder = CreateUIObject("Icon", parent);
            SetAnchoredRect(holder, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(size, size), new Vector2(0f, -topOffset));

            switch (kind)
            {
                case "Gioca":
                    var body = CreateUIObject("Body", holder);
                    SetAnchoredRect(body, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size, size * 0.55f), Vector2.zero);
                    AddFlat(body, LabelWhite);
                    var btn1 = CreateUIObject("Btn1", holder);
                    SetAnchoredRect(btn1, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size * 0.22f, size * 0.22f), new Vector2(size * 0.18f, 0f));
                    btn1.gameObject.AddComponent<Image>().sprite = KnobSprite();
                    btn1.GetComponent<Image>().color = NavColor;
                    break;
                case "Carte":
                    var card = CreateUIObject("Card", holder);
                    SetAnchoredRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size * 0.62f, size * 0.86f), Vector2.zero);
                    AddFlat(card, LabelWhite);
                    break;
                case "Negozio":
                    var basket = CreateUIObject("Basket", holder);
                    SetAnchoredRect(basket, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(size * 0.9f, size * 0.55f), new Vector2(0f, -size * 0.15f));
                    AddFlat(basket, LabelWhite);
                    var wheelL = CreateUIObject("WheelL", holder);
                    SetAnchoredRect(wheelL, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(size * 0.2f, size * 0.2f), new Vector2(-size * 0.2f, 0f));
                    wheelL.gameObject.AddComponent<Image>().sprite = KnobSprite();
                    wheelL.GetComponent<Image>().color = LabelWhite;
                    var wheelR = CreateUIObject("WheelR", holder);
                    SetAnchoredRect(wheelR, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(size * 0.2f, size * 0.2f), new Vector2(size * 0.2f, 0f));
                    wheelR.gameObject.AddComponent<Image>().sprite = KnobSprite();
                    wheelR.GetComponent<Image>().color = LabelWhite;
                    break;
                default: // Profilo
                    var head = CreateUIObject("Head", holder);
                    SetAnchoredRect(head, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(size * 0.42f, size * 0.42f), new Vector2(0f, 0f));
                    head.gameObject.AddComponent<Image>().sprite = KnobSprite();
                    head.GetComponent<Image>().color = LabelWhite;
                    var shoulders = CreateUIObject("Shoulders", holder);
                    SetAnchoredRect(shoulders, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(size * 0.8f, size * 0.4f), Vector2.zero);
                    AddFlat(shoulders, LabelWhite);
                    break;
            }
        }

        private static Sprite KnobSprite()
        {
            if (_knobSprite == null) _knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            return _knobSprite;
        }

        private static Sprite CheckmarkSprite()
        {
            if (_checkmarkSprite == null) _checkmarkSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            return _checkmarkSprite;
        }

        private static void RegisterTarget(string name, float x, float y, float w, float h, RectTransform built, Vector2? parentOffset = null)
        {
            Targets[name] = new Vector4(x, y, w, h);
            Built[name] = built;
            if (parentOffset.HasValue) ExtraOffsets[name] = parentOffset.Value;
        }

        private static void RunDebugVerification()
        {
            const float tolerance = 2f;
            int pass = 0, fail = 0;
            var report = new StringBuilder();
            report.AppendLine("[EmoticonWireframeBuilder] Coordinate applicate e verifica (tolleranza " + tolerance + "px) - X/width ora calcolate da HorizontalLayoutGroup/GridLayoutGroup, non piu' letteralmente scritte per elemento:");

            foreach (var kvp in Targets)
            {
                string name = kvp.Key;
                Vector4 target = kvp.Value;
                if (!Built.TryGetValue(name, out var rt) || rt == null)
                {
                    report.AppendLine($"  [SKIP] {name}: RectTransform non trovato");
                    continue;
                }

                Vector2 extra = ExtraOffsets.TryGetValue(name, out var off) ? off : Vector2.zero;
                float actualX = rt.anchoredPosition.x + extra.x;
                float actualY = -rt.anchoredPosition.y + extra.y;
                float actualW = rt.rect.width;
                float actualH = rt.rect.height;

                float dx = actualX - target.x;
                float dy = actualY - target.y;
                float dw = actualW - target.z;
                float dh = actualH - target.w;
                bool ok = Mathf.Abs(dx) <= tolerance && Mathf.Abs(dy) <= tolerance && Mathf.Abs(dw) <= tolerance && Mathf.Abs(dh) <= tolerance;
                if (ok) pass++; else fail++;

                report.AppendLine($"  [{(ok ? "PASS" : "FAIL")}] {name}: anchoredPosition={rt.anchoredPosition} rect={rt.rect} scale={rt.localScale} | target=({target.x},{target.y},{target.z},{target.w}) diff=({dx:F1},{dy:F1},{dw:F1},{dh:F1})");
            }

            report.AppendLine($"[EmoticonWireframeBuilder] Totale: {pass} PASS, {fail} FAIL su {Targets.Count} elementi.");
            if (fail > 0) Debug.LogError(report.ToString());
            else Debug.Log(report.ToString());
        }

        private static void EnforceUniformScale(Transform root)
        {
            if (root == null) return;
            if (root is RectTransform rt) rt.localScale = Vector3.one;
            for (int i = 0; i < root.childCount; i++) EnforceUniformScale(root.GetChild(i));
        }

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            // Bug reale trovato in Fase 5: il pivot di default di un RectTransform appena creato
            // e' (0.5,0.5) - i figli di un LayoutGroup (childControlWidth/Height=false) lo
            // mantengono, quindi il loro anchoredPosition finisce per rappresentare il CENTRO
            // invece dell'angolo top-left, rompendo la formula di verifica (che assume
            // top-left ovunque). Default qui a (0,1): armonico con SetTopLeft/tutto il resto del
            // file, e i chiamanti che vogliono un'altra convenzione la sovrascrivono comunque
            // subito dopo (StretchFill, SetBottomLeft, SetAnchoredRect).
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            return rt;
        }

        private static void SetTopLeft(RectTransform rt, float x0, float y0, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x0, -y0);
        }

        private static void SetBottomLeft(RectTransform rt, float x0, float yFromBottom, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x0, yFromBottom);
        }

        private static void SetAnchoredRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
        }

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Image AddFlat(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI AddText(RectTransform rt, string content, float fontSize, TextAlignmentOptions alignment, Color color, FontStyles style = FontStyles.Normal)
        {
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }
    }
}
