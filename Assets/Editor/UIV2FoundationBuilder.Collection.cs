using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Components;
using Project51.UIV2.Screens;

namespace Project51.EditorTools
{
    /// <summary>
    /// COLLEZIONE V2 (Tools/UIV2/Build Collection Screen V2): shell con tab bar MAZZI/EMOTICON/
    /// ACCUSI + pannello MAZZI da 20_collezione_carte.png; tab bar dal linguaggio visivo di
    /// 21_collezione_emoticon (1).png. Pannelli MAZZI (mockup 20) ed EMOTICON (mockup 21) costruiti; ACCUSI (mockup 22) costruito.
    /// Coordinate di riferimento: mockup 1080x1920, ScreenHost da y=262 a y=1750. Il mockup Mazzi
    /// non aveva la tab bar: il suo contenuto e' traslato di +130px sotto le tab.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        // ---- Token campionati da 20_collezione_carte.png / 21_collezione_emoticon (1).png ----
        private static readonly Color CollectionGold = new Color32(232, 178, 74, 255);
        private static readonly Color CollectionHeaderGold = new Color32(255, 221, 140, 255);
        private static readonly Color CollectionHeaderLine = new Color32(69, 99, 136, 255);
        private static readonly Color CollectionHeroFill = new Color32(19, 33, 52, 255);
        private static readonly Color CollectionCardFill = new Color32(26, 44, 68, 255);
        private static readonly Color CollectionCardBorder = new Color32(70, 102, 142, 255);
        private static readonly Color CollectionNameCream = new Color32(255, 236, 190, 255);
        private static readonly Color CollectionSubtitle = new Color32(186, 205, 228, 255);
        private static readonly Color CollectionFooter = new Color32(128, 152, 184, 255);
        private static readonly Color CollectionPillText = new Color32(58, 34, 8, 255);
        private static readonly Color CollectionTabText = new Color32(196, 208, 224, 255);
        private static readonly Color CollectionProgressHighlight = new Color32(157, 149, 121, 255);

        // Tab: parte visibile 310x62. btn_gray_small (alpha 107x74 su 112x84) e btn_teal (203x90 su
        // 206x102) portati alla stessa altezza visibile con pixelsPerUnitMultiplier diversi.
        private const float CollectionTabRectHeight = 70.4f;

        [MenuItem("Tools/UIV2/Build Collection Screen V2")]
        private static void BuildCollectionScreenV2Entry()
        {
            var progressBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ComponentsPrefabDir}/UIV2_ProgressBar.prefab");
            if (progressBarPrefab == null)
            {
                Debug.LogError("[UIV2FoundationBuilder] UIV2_ProgressBar.prefab non trovato - esegui prima 'Tools/UIV2/Build Foundation'.");
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var deckCardPrefab = BuildDeckCardPrefab();
            var emoticonCardPrefab = BuildEmoticonCardPrefab();
            var accusoRowPrefab = BuildAccusoRowPrefab();
            BuildCollectionScreenV2Prefab(deckCardPrefab, emoticonCardPrefab, accusoRowPrefab, progressBarPrefab);
            AssetDatabase.SaveAssets();

            Debug.Log("[UIV2FoundationBuilder] CollectionScreenV2 costruita in " +
                      $"{ScreensPrefabDir}/CollectionScreenV2.prefab. Scena scratch NON salvata.");
        }

        // ------------------------------------------------------------------
        // Deck card (griglia MAZZI) - 305x300
        // ------------------------------------------------------------------

        private static DeckCardView BuildDeckCardPrefab()
        {
            var go = new GameObject("DeckCard", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(305f, 300f);

            var border = AddRoundedPanel(rect, "panel_fill_r24", 48f, 22f, CollectionCardBorder, 3f, CollectionCardFill,
                out var fillRect, out var fill);

            // Arte a aspect nativo: card_back_green (alpha 145x210) -> 98x142 visibili come nel mockup.
            var artRect = CreateUIObject("Art", rect);
            Place(artRect, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(156.5f, -107f), new Vector2(117f, 166f));
            var art = artRect.gameObject.AddComponent<Image>();
            art.sprite = LoadSprite(IconsPath, "card_back_green");
            art.preserveAspect = true;
            art.raycastTarget = false;

            var lockRect = CreateUIObject("LockIcon", rect);
            Place(lockRect, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(152f, -104f), new Vector2(40f, 46f));
            var lockImage = lockRect.gameObject.AddComponent<Image>();
            lockImage.sprite = LoadSprite(IconsPath, "ic_lock");
            lockImage.preserveAspect = true;
            lockImage.raycastTarget = false;
            lockRect.gameObject.SetActive(false);

            var nameRect = CreateUIObject("NameLabel", rect);
            Place(nameRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -215f), new Vector2(280f, 36f));
            var nameLabel = AddText(nameRect, "Mazzo", 24f, FontStyles.Bold, CollectionNameCream, TextAlignmentOptions.Center);
            nameLabel.enableWordWrapping = false;
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMin = 16f;
            nameLabel.fontSizeMax = 24f;

            var statusRect = CreateUIObject("StatusLabel", rect);
            Place(statusRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(280f, 30f));
            var statusLabel = AddText(statusRect, "In uso", 20f, FontStyles.Bold, CollectionHeaderGold, TextAlignmentOptions.Center);
            statusLabel.enableWordWrapping = false;
            statusRect.gameObject.SetActive(false);

            // Bottone azione: parte visibile 260x37 (btn_teal / btn_blue_long hanno 90 righe visibili su
            // 102 -> rect alto 42, 9-slice a scala uniforme).
            var actionRect = CreateUIObject("ActionButton", rect);
            Place(actionRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.6f, -260.5f), new Vector2(261.5f, 42f));
            var actionImage = AddSlicedImage(actionRect, LoadSprite(IconsPath, "btn_teal"), 42f);
            var actionButton = actionRect.gameObject.AddComponent<Button>();
            actionButton.targetGraphic = actionImage;
            var actionLabelRect = CreateUIObject("Label", actionRect);
            StretchFill(actionLabelRect);
            var actionLabel = AddText(actionLabelRect, "USA", 20f, FontStyles.Bold, HomeTextLight, TextAlignmentOptions.Center);
            actionLabel.enableWordWrapping = false;
            ApplyOutline(actionLabel, NavyOutlineMaterial());

            // Check verde (ic_check, alpha 80x83) centrato 20px dentro l'angolo in alto a destra.
            var badgeRect = CreateUIObject("EquippedBadge", rect);
            Place(badgeRect, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-20f, -20f), new Vector2(36f, 40f));
            var badgeImage = badgeRect.gameObject.AddComponent<Image>();
            badgeImage.sprite = LoadSprite(IconsPath, "ic_check");
            badgeImage.preserveAspect = true;
            badgeImage.raycastTarget = false;
            badgeRect.gameObject.SetActive(false);

            var comp = go.AddComponent<DeckCardView>();
            SetPrivateField(comp, "border", border);
            SetPrivateField(comp, "fillRect", fillRect);
            SetPrivateField(comp, "fill", fill);
            SetPrivateField(comp, "art", art);
            SetPrivateField(comp, "lockIcon", lockRect.gameObject);
            SetPrivateField(comp, "equippedBadge", badgeRect.gameObject);
            SetPrivateField(comp, "nameLabel", nameLabel);
            SetPrivateField(comp, "statusLabel", statusLabel);
            SetPrivateField(comp, "actionButton", actionButton);
            SetPrivateField(comp, "actionBackground", actionImage);
            SetPrivateField(comp, "actionLabel", actionLabel);
            SetPrivateField(comp, "lockedArtSprite", LoadSprite(IconsPath, "card_frame_dark"));
            SetPrivateField(comp, "ownedActionSprite", LoadSprite(IconsPath, "btn_teal"));
            SetPrivateField(comp, "lockedActionSprite", LoadSprite(IconsPath, "btn_blue_long"));

            return SaveAsPrefab(go, $"{ScreensPrefabDir}/DeckCard.prefab").GetComponent<DeckCardView>();
        }

        // ------------------------------------------------------------------
        // CollectionScreenV2
        // ------------------------------------------------------------------

        private static void BuildCollectionScreenV2Prefab(DeckCardView deckCardPrefab, UIV2CollectionCard emoticonCardPrefab,
            AccusoRowView accusoRowPrefab, GameObject progressBarPrefab)
        {
            var go = new GameObject("CollectionScreenV2", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            StretchFill(rect);

            // ---- TabsRow: sempre presente, parte visibile y 268..330 ----
            var tabsRect = CreateUIObject("TabsRow", rect);
            tabsRect.anchorMin = new Vector2(0f, 1f);
            tabsRect.anchorMax = new Vector2(1f, 1f);
            tabsRect.pivot = new Vector2(0.5f, 1f);
            tabsRect.anchoredPosition = Vector2.zero;
            tabsRect.sizeDelta = new Vector2(0f, CollectionTabRectHeight);
            var tabsLayout = tabsRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.padding = new RectOffset(58, 58, 0, 0);
            tabsLayout.spacing = 10f;
            tabsLayout.childAlignment = TextAnchor.MiddleCenter;
            tabsLayout.childControlWidth = true;
            tabsLayout.childControlHeight = true;
            tabsLayout.childForceExpandWidth = true;
            tabsLayout.childForceExpandHeight = true;

            var graySprite = LoadSprite(IconsPath, "btn_gray_small");
            var tealSprite = LoadSprite(IconsPath, "btn_teal");
            float grayPpu = 84f / CollectionTabRectHeight;
            float tealPpu = 102f / CollectionTabRectHeight;

            // ---- ContentHost: sotto le tab, un pannello attivo alla volta ----
            var contentHost = CreateUIObject("ContentHost", rect);
            contentHost.anchorMin = Vector2.zero;
            contentHost.anchorMax = Vector2.one;
            contentHost.offsetMin = Vector2.zero;
            contentHost.offsetMax = new Vector2(0f, -80f);

            var decksPanelRect = CreateUIObject("DecksPanel", contentHost);
            StretchFill(decksPanelRect);
            var emoticonsPanelRect = CreateUIObject("EmoticonsPanel", contentHost);
            StretchFill(emoticonsPanelRect);
            // Contenuto costruito PRIMA di disattivare il pannello (TMP su GameObject inattivi = rischio eccezioni).
            var emoticonsPanel = BuildEmoticonsPanelContent(emoticonsPanelRect, emoticonCardPrefab, progressBarPrefab);
            emoticonsPanelRect.gameObject.SetActive(false);
            var accusiPanelRect = CreateUIObject("AccusiPanel", contentHost);
            StretchFill(accusiPanelRect);
            var accusiPanel = BuildAccusiPanelContent(accusiPanelRect, accusoRowPrefab, progressBarPrefab);
            accusiPanelRect.gameObject.SetActive(false);

            string[] tabObjectNames = { "MazziTab", "EmoticonTab", "AccusiTab" };
            string[] tabLabels = { "MAZZI", "EMOTICON", "ACCUSI" };
            GameObject[] panels = { decksPanelRect.gameObject, emoticonsPanelRect.gameObject, accusiPanelRect.gameObject };
            var tabRefs = new CollectionScreenV2.TabRefs[tabLabels.Length];
            for (int i = 0; i < tabLabels.Length; i++)
            {
                bool selected = i == 0;
                var tabRect = CreateUIObject(tabObjectNames[i], tabsRect);
                var tabImage = AddSlicedImage(tabRect, selected ? tealSprite : graySprite, CollectionTabRectHeight);
                var tabButton = tabRect.gameObject.AddComponent<Button>();
                tabButton.targetGraphic = tabImage;

                var labelRect = CreateUIObject("Label", tabRect);
                StretchFill(labelRect);
                labelRect.offsetMin = new Vector2(0f, -1f);
                labelRect.offsetMax = new Vector2(0f, -1f);
                var label = AddText(labelRect, tabLabels[i], 24f, FontStyles.Bold,
                    selected ? HomeTextLight : CollectionTabText, TextAlignmentOptions.Center);
                label.enableWordWrapping = false;
                ApplyOutline(label, NavyOutlineMaterial());

                tabRefs[i] = new CollectionScreenV2.TabRefs
                {
                    Button = tabButton,
                    Background = tabImage,
                    Label = label,
                    Panel = panels[i]
                };
            }

            // ---- DecksPanel: ScrollRect verticale (il catalogo mazzi crescera') ----
            var content = CreateCollectionScrollContent(decksPanelRect, 10);

            // ---- Sezione IN USO ----
            BuildCollectionSectionHeader(content, "InUseHeader", "IN USO", false, out _);
            AddVerticalSpacer(content, 14f);

            var heroSlot = CreateUIObject("HeroSlot", content);
            AddLayoutElement(heroSlot, preferredHeight: 468f);
            var heroRect = CreateUIObject("HeroPanel", heroSlot);
            heroRect.anchorMin = Vector2.zero;
            heroRect.anchorMax = Vector2.one;
            heroRect.offsetMin = new Vector2(50f, 0f);
            heroRect.offsetMax = new Vector2(-50f, 0f);
            AddRoundedPanel(heroRect, "panel_fill_r30", 60f, 30f, CollectionGold, 5f, CollectionHeroFill, out _, out _);

            var pillRect = CreateUIObject("InUsePill", heroRect);
            Place(pillRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(180f, 48f));
            var pillImage = pillRect.gameObject.AddComponent<Image>();
            pillImage.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
            pillImage.type = Image.Type.Sliced;
            pillImage.pixelsPerUnitMultiplier = 48f / 14f; // raggio reale sprite 48 -> 14
            pillImage.color = CollectionGold;
            pillImage.raycastTarget = false;
            var pillLabelRect = CreateUIObject("Label", pillRect);
            StretchFill(pillLabelRect);
            AddText(pillLabelRect, "IN USO", 24f, FontStyles.Bold, CollectionPillText, TextAlignmentOptions.Center);

            // Ventaglio di 3 dorsi del mazzo equipaggiato (stesso sprite, solo rotazione - niente
            // stretch): sinistro dietro, destro davanti come nel mockup.
            var fanRect = CreateUIObject("DeckFan", heroRect);
            Place(fanRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -228f), Vector2.zero);
            var fanLeft = AddFanCard(fanRect, "CardLeft", new Vector2(-136.4f, -8.8f), 12f);
            var fanCenter = AddFanCard(fanRect, "CardCenter", new Vector2(6.6f, -3.8f), 0f);
            var fanRight = AddFanCard(fanRect, "CardRight", new Vector2(149.6f, -6.8f), -12f);

            var heroNameRect = CreateUIObject("DeckName", heroRect);
            Place(heroNameRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -372f), new Vector2(900f, 54f));
            var heroName = AddText(heroNameRect, "Mazzo", 42f, FontStyles.Bold, CollectionNameCream, TextAlignmentOptions.Center);
            heroName.enableWordWrapping = false;
            ApplyOutline(heroName, NavyOutlineMaterial());

            var heroSubRect = CreateUIObject("DeckSubtitle", heroRect);
            Place(heroSubRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -422f), new Vector2(900f, 34f));
            var heroSubtitle = AddText(heroSubRect, "-", 24f, FontStyles.Normal, CollectionSubtitle, TextAlignmentOptions.Center);
            heroSubtitle.enableWordWrapping = false;

            // ---- Sezione COLLEZIONE ----
            AddVerticalSpacer(content, 50f);
            BuildCollectionSectionHeader(content, "CollectionHeader", "COLLEZIONE", true, out var countLabel);
            AddVerticalSpacer(content, 12.5f);

            var collectionProgress = AddCollectionProgressBar(content, progressBarPrefab);

            AddVerticalSpacer(content, 30.5f);

            // Griglia 3 colonne 305x300 (x 60..1020), righe a passo 334.
            var gridRect = CreateUIObject("DeckGrid", content);
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(60, 60, 0, 0);
            grid.cellSize = new Vector2(305f, 300f);
            grid.spacing = new Vector2(22.5f, 34f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;

            AddVerticalSpacer(content, 47f);
            var footerRect = CreateUIObject("FooterLabel", content);
            AddLayoutElement(footerRect, preferredHeight: 30f);
            var footer = AddText(footerRect, "Nuovi mazzi in arrivo con gli eventi stagionali", 22f, FontStyles.Normal,
                CollectionFooter, TextAlignmentOptions.Center);
            footer.enableWordWrapping = false;

            var decksPanel = decksPanelRect.gameObject.AddComponent<CollectionDecksPanel>();
            SetPrivateField(decksPanel, "heroArtCards", new[] { fanLeft, fanCenter, fanRight });
            SetPrivateField(decksPanel, "heroNameLabel", heroName);
            SetPrivateField(decksPanel, "heroSubtitleLabel", heroSubtitle);
            SetPrivateField(decksPanel, "collectionCountLabel", countLabel);
            SetPrivateField(decksPanel, "collectionProgress", collectionProgress);
            SetPrivateField(decksPanel, "gridContainer", gridRect);
            SetPrivateField(decksPanel, "deckCardPrefab", deckCardPrefab);
            SetPrivateField(decksPanel, "footerLabel", footer);

            var screen = go.AddComponent<CollectionScreenV2>();
            SetPrivateField(screen, "tabs", tabRefs);
            SetPrivateField(screen, "decksPanel", decksPanel);
            SetPrivateField(screen, "emoticonsPanel", emoticonsPanel);
            SetPrivateField(screen, "accusiPanel", accusiPanel);
            SetPrivateField(screen, "selectedTabSprite", tealSprite);
            SetPrivateField(screen, "normalTabSprite", graySprite);
            SetPrivateField(screen, "selectedTabPixelsPerUnit", tealPpu);
            SetPrivateField(screen, "normalTabPixelsPerUnit", grayPpu);
            SetPrivateField(screen, "selectedLabelColor", HomeTextLight);
            SetPrivateField(screen, "normalLabelColor", CollectionTabText);

            SaveAsPrefab(go, $"{ScreensPrefabDir}/CollectionScreenV2.prefab");
        }

        // ------------------------------------------------------------------
        // EMOTICON (21_collezione_emoticon (1).png: stessa spaziatura del mockup, tutto +43px perche'
        // le tab visibili stanno a y=268 invece di 225)
        // ------------------------------------------------------------------

        private static readonly Color CollectionCardLockedFill = new Color32(14, 24, 40, 255);
        private static readonly Color EmoticonSlotEmptyFill = new Color32(16, 26, 43, 255);
        private static readonly Color EmoticonDash = new Color32(70, 100, 140, 255);
        private static readonly Color EmoticonPlus = new Color32(90, 124, 166, 255);
        private static readonly Color EmoticonEmptyLabel = new Color32(120, 146, 178, 255);
        private static readonly Color EmoticonLockedLabel = new Color32(110, 132, 162, 255);

        /// <summary>
        /// Card griglia 225x208 con UIV2CollectionCard (componente foundation riusato + campi estesi).
        /// </summary>
        private static UIV2CollectionCard BuildEmoticonCardPrefab()
        {
            var go = new GameObject("EmoticonCard", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(225f, 208f);

            var border = AddRoundedPanel(rect, "panel_fill_r24", 48f, 14f, CollectionCardBorder, 3f, CollectionCardFill,
                out var fillRect, out var fill);
            border.raycastTarget = true;
            var button = go.AddComponent<Button>();
            button.targetGraphic = border;
            button.transition = Selectable.Transition.None;

            // Box 106x106 a aspect nativo: le emoticon hanno padding trasparente NON uniforme (0-12%),
            // quindi la taglia visibile varia fra ~93 e ~105 (vedi report asset).
            var iconRect = CreateUIObject("Icon", rect);
            Place(iconRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -73f), new Vector2(106f, 106f));
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = LoadSprite(EmoticonsPath, "emo_risata");
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var lockRect = CreateUIObject("LockIcon", rect);
            Place(lockRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-1.5f, -69f), new Vector2(41f, 48f));
            var lockImage = lockRect.gameObject.AddComponent<Image>();
            lockImage.sprite = LoadSprite(IconsPath, "ic_lock");
            lockImage.preserveAspect = true;
            lockImage.raycastTarget = false;
            lockRect.gameObject.SetActive(false);

            var nameRect = CreateUIObject("NameLabel", rect);
            Place(nameRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -158f), new Vector2(210f, 30f));
            var nameLabel = AddText(nameRect, "Emoticon", 20f, FontStyles.Normal, CollectionSubtitle, TextAlignmentOptions.Center);
            nameLabel.enableWordWrapping = false;
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMin = 14f;
            nameLabel.fontSizeMax = 20f;

            // Check verde ic_check (alpha 80x83) -> 28 visibili, centro 15/13px dentro l'angolo.
            var badgeRect = CreateUIObject("EquippedBadge", rect);
            Place(badgeRect, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-15f, -13f), new Vector2(29f, 31f));
            var badgeImage = badgeRect.gameObject.AddComponent<Image>();
            badgeImage.sprite = LoadSprite(IconsPath, "ic_check");
            badgeImage.preserveAspect = true;
            badgeImage.raycastTarget = false;
            badgeRect.gameObject.SetActive(false);

            var comp = go.AddComponent<UIV2CollectionCard>();
            SetPrivateField(comp, "border", border);
            SetPrivateField(comp, "icon", icon);
            SetPrivateField(comp, "lockIcon", lockRect.gameObject);
            SetPrivateField(comp, "nameLabel", nameLabel);
            SetPrivateField(comp, "equippedBadge", badgeRect.gameObject);
            SetPrivateField(comp, "unlockedBorderColor", CollectionCardBorder);
            SetPrivateField(comp, "equippedBorderColor", CollectionGold);
            SetPrivateField(comp, "button", button);
            SetPrivateField(comp, "fillRect", fillRect);
            SetPrivateField(comp, "fill", fill);
            SetPrivateField(comp, "unlockedFillColor", CollectionCardFill);
            SetPrivateField(comp, "lockedFillColor", CollectionCardLockedFill);
            SetPrivateField(comp, "equippedBorderThickness", 4f);
            SetPrivateField(comp, "normalBorderThickness", 3f);
            SetPrivateField(comp, "overrideNameColors", true);
            SetPrivateField(comp, "nameColor", CollectionSubtitle);
            SetPrivateField(comp, "equippedNameColor", CollectionHeaderGold);
            SetPrivateField(comp, "lockedNameColor", EmoticonLockedLabel);
            SetPrivateField(comp, "lockedTitleText", "Bloccata");

            return SaveAsPrefab(go, $"{ScreensPrefabDir}/EmoticonCard.prefab").GetComponent<UIV2CollectionCard>();
        }

        private static CollectionEmoticonsPanel BuildEmoticonsPanelContent(RectTransform panelRect,
            UIV2CollectionCard cardPrefab, GameObject progressBarPrefab)
        {
            var content = CreateCollectionScrollContent(panelRect, 31);

            var equippedHeader = BuildCollectionSectionHeader(content, "EquippedHeader",
                "EQUIPAGGIATE " + (char)0xB7 + " 3 max", false, out _);
            AddVerticalSpacer(content, 16f);

            // 3 slot 305x231 (x 60..365 / 387.5..692.5 / 715..1020), come le card MAZZI.
            var slotsRow = CreateUIObject("EquippedSlots", content);
            AddLayoutElement(slotsRow, preferredHeight: 231f);
            var slotsLayout = slotsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            slotsLayout.padding = new RectOffset(60, 60, 0, 0);
            slotsLayout.spacing = 22.5f;
            slotsLayout.childAlignment = TextAnchor.UpperCenter;
            slotsLayout.childControlWidth = true;
            slotsLayout.childControlHeight = true;
            slotsLayout.childForceExpandWidth = true;
            slotsLayout.childForceExpandHeight = true;
            var slots = new EmoticonSlotView[3];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = BuildEmoticonSlot(slotsRow, "Slot" + (i + 1));
            }

            AddVerticalSpacer(content, 59f);
            BuildCollectionSectionHeader(content, "CollectionHeader", "COLLEZIONE", true, out var countLabel);
            AddVerticalSpacer(content, 12.5f);
            var progress = AddCollectionProgressBar(content, progressBarPrefab);
            AddVerticalSpacer(content, 32.5f);

            // Griglia 4 colonne 225x208 (x 60..1020), passo righe 230.
            var gridRect = CreateUIObject("EmoticonGrid", content);
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(60, 60, 0, 0);
            grid.cellSize = new Vector2(225f, 208f);
            grid.spacing = new Vector2(20f, 22f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;

            var panel = panelRect.gameObject.AddComponent<CollectionEmoticonsPanel>();
            SetPrivateField(panel, "equippedHeaderLabel", equippedHeader);
            SetPrivateField(panel, "equippedSlots", slots);
            SetPrivateField(panel, "collectionCountLabel", countLabel);
            SetPrivateField(panel, "collectionProgress", progress);
            SetPrivateField(panel, "gridContainer", gridRect);
            SetPrivateField(panel, "cardPrefab", cardPrefab);
            return panel;
        }

        private static EmoticonSlotView BuildEmoticonSlot(RectTransform parent, string objectName)
        {
            var slotRect = CreateUIObject(objectName, parent);

            // ---- Pieno: bordo oro 5px raggio 22, emoticon ~128 visibili, nome, X rossa ----
            var filledRect = CreateUIObject("Filled", slotRect);
            StretchFill(filledRect);
            AddRoundedPanel(filledRect, "panel_fill_r24", 48f, 22f, CollectionGold, 5f, CollectionCardFill, out _, out _);

            var iconRect = CreateUIObject("Icon", filledRect);
            Place(iconRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -101f), new Vector2(155f, 155f));
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = LoadSprite(EmoticonsPath, "emo_risata");
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var nameRect = CreateUIObject("NameLabel", filledRect);
            Place(nameRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -201f), new Vector2(280f, 34f));
            var nameLabel = AddText(nameRect, "Emoticon", 22f, FontStyles.Normal, CollectionSubtitle, TextAlignmentOptions.Center);
            nameLabel.enableWordWrapping = false;

            // X rimuovi: ic_x (alpha 82x83 su 93x92) -> 48 visibili, centrata sull'angolo alto-destro.
            var removeRect = CreateUIObject("RemoveButton", filledRect);
            Place(removeRect, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-31f, -8f), new Vector2(54f, 54f));
            var removeImage = removeRect.gameObject.AddComponent<Image>();
            removeImage.sprite = LoadSprite(IconsPath, "ic_x");
            removeImage.preserveAspect = true;
            var removeButton = removeRect.gameObject.AddComponent<Button>();
            removeButton.targetGraphic = removeImage;

            // ---- Vuoto: fondo scuro, bordo tratteggiato, "+" in cerchio outline, "Slot libero" ----
            var emptyRect = CreateUIObject("Empty", slotRect);
            StretchFill(emptyRect);
            var emptyFillRect = CreateUIObject("Fill", emptyRect);
            emptyFillRect.anchorMin = Vector2.zero;
            emptyFillRect.anchorMax = Vector2.one;
            emptyFillRect.offsetMin = new Vector2(2f, 2f);
            emptyFillRect.offsetMax = new Vector2(-2f, -2f);
            var emptyFill = emptyFillRect.gameObject.AddComponent<Image>();
            emptyFill.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
            emptyFill.type = Image.Type.Sliced;
            emptyFill.pixelsPerUnitMultiplier = 48f / 4f;
            emptyFill.color = EmoticonSlotEmptyFill;
            var emptyButton = emptyRect.gameObject.AddComponent<Button>();
            emptyButton.targetGraphic = emptyFill;
            emptyButton.transition = Selectable.Transition.None;

            AddDashedBorder(emptyRect, 305f, 231f, 13.5f, 10.5f, 4f, EmoticonDash);

            // "+" outline: nel kit ic_plus_circle e' un bottone gemma verde (stile diverso), quindi cerchio
            // = due panel_fill_r24 tinti (anello 4px) + due barre piatte, colori campionati dal mockup.
            var plusRect = CreateUIObject("PlusIcon", emptyRect);
            Place(plusRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, -108f), new Vector2(76f, 76f));
            var ring = plusRect.gameObject.AddComponent<Image>();
            ring.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
            ring.type = Image.Type.Sliced;
            ring.pixelsPerUnitMultiplier = 48f / 38f;
            ring.color = EmoticonPlus;
            ring.raycastTarget = false;
            var holeRect = CreateUIObject("Hole", plusRect);
            holeRect.anchorMin = Vector2.zero;
            holeRect.anchorMax = Vector2.one;
            holeRect.offsetMin = new Vector2(4f, 4f);
            holeRect.offsetMax = new Vector2(-4f, -4f);
            var hole = holeRect.gameObject.AddComponent<Image>();
            hole.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
            hole.type = Image.Type.Sliced;
            hole.pixelsPerUnitMultiplier = 48f / 34f;
            hole.color = EmoticonSlotEmptyFill;
            hole.raycastTarget = false;
            for (int b = 0; b < 2; b++)
            {
                var barRect = CreateUIObject(b == 0 ? "BarH" : "BarV", plusRect);
                Place(barRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                    b == 0 ? new Vector2(40f, 4f) : new Vector2(4f, 40f));
                var bar = barRect.gameObject.AddComponent<Image>();
                bar.color = EmoticonPlus;
                bar.raycastTarget = false;
            }

            var emptyLabelRect = CreateUIObject("Label", emptyRect);
            Place(emptyLabelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -201f), new Vector2(280f, 34f));
            var emptyLabel = AddText(emptyLabelRect, "Slot libero", 22f, FontStyles.Normal, EmoticonEmptyLabel, TextAlignmentOptions.Center);
            emptyLabel.enableWordWrapping = false;
            emptyRect.gameObject.SetActive(false);

            var view = slotRect.gameObject.AddComponent<EmoticonSlotView>();
            SetPrivateField(view, "filledRoot", filledRect.gameObject);
            SetPrivateField(view, "emptyRoot", emptyRect.gameObject);
            SetPrivateField(view, "icon", icon);
            SetPrivateField(view, "nameLabel", nameLabel);
            SetPrivateField(view, "removeButton", removeButton);
            SetPrivateField(view, "emptyButton", emptyButton);
            return view;
        }

        /// <summary>
        /// Bordo tratteggiato da segmenti Image piatti (stessa primitiva delle linee dei section header):
        /// nel kit non c'e' un frame tratteggiato. Misure mockup: tratto 13.5, vuoto 10.5, spessore 4.
        /// </summary>
        private static void AddDashedBorder(RectTransform parent, float width, float height, float dash, float gap,
            float thickness, Color color)
        {
            var root = CreateUIObject("DashedBorder", parent);
            StretchFill(root);
            float pitch = dash + gap;

            int horizontalCount = Mathf.FloorToInt((width - dash) / pitch) + 1;
            float horizontalStart = (width - ((horizontalCount - 1) * pitch + dash)) * 0.5f;
            for (int i = 0; i < horizontalCount; i++)
            {
                float x = horizontalStart + i * pitch;
                AddDash(root, new Vector2(0f, 1f), new Vector2(x, 0f), new Vector2(dash, thickness), color);
                AddDash(root, new Vector2(0f, 0f), new Vector2(x, 0f), new Vector2(dash, thickness), color);
            }

            int verticalCount = Mathf.FloorToInt((height - dash) / pitch) + 1;
            float verticalStart = (height - ((verticalCount - 1) * pitch + dash)) * 0.5f;
            for (int i = 0; i < verticalCount; i++)
            {
                float y = -(verticalStart + i * pitch);
                AddDash(root, new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(thickness, dash), color);
                AddDash(root, new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(thickness, dash), color);
            }
        }

        private static void AddDash(RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var dashRect = CreateUIObject("Dash", parent);
            Place(dashRect, anchor, anchor, position, size); // pivot = anchor: il tratto cresce verso l'interno
            var image = dashRect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        /// <summary>ScrollRect verticale + Viewport (RectMask2D) + Content (VerticalLayoutGroup).</summary>
        private static RectTransform CreateCollectionScrollContent(RectTransform panelRect, int paddingTop)
        {
            var scroll = panelRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            var viewport = CreateUIObject("Viewport", panelRect);
            StretchFill(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f); // hit area per il drag dello scroll

            var content = CreateUIObject("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, paddingTop, 24);
            contentLayout.spacing = 0f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        /// <summary>
        /// Barra COLLEZIONE condivisa MAZZI/EMOTICON: UIV2_ProgressBar riusato (track bar_empty, alpha
        /// 58/63 righe -> rect 37 per 34 visibili), fill oro + fascia chiara come nei mockup.
        /// </summary>
        private static UIV2ProgressBar AddCollectionProgressBar(RectTransform content, GameObject progressBarPrefab)
        {
            var progressSlot = CreateUIObject("ProgressSlot", content);
            AddLayoutElement(progressSlot, preferredHeight: 37f);
            var progressInstance = (GameObject)PrefabUtility.InstantiatePrefab(progressBarPrefab, progressSlot);
            progressInstance.name = "CollectionProgress";
            var progressRect = (RectTransform)progressInstance.transform;
            progressRect.anchorMin = Vector2.zero;
            progressRect.anchorMax = Vector2.one;
            progressRect.offsetMin = new Vector2(57.6f, 0f);
            progressRect.offsetMax = new Vector2(-57.6f, 0f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(progressRect);

            var progressTrack = progressInstance.GetComponent<Image>();
            if (progressTrack != null)
            {
                progressTrack.pixelsPerUnitMultiplier = 63f / 37f;
                progressTrack.color = new Color(0.7f, 0.7f, 0.75f, 1f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(progressTrack);
            }

            var fillArea = progressInstance.transform.Find("FillArea") as RectTransform;
            if (fillArea != null)
            {
                fillArea.offsetMin = new Vector2(2.3f, 2.3f);
                fillArea.offsetMax = new Vector2(-2.3f, -2.3f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(fillArea);
                var progressFillRect = fillArea.Find("Fill") as RectTransform;
                if (progressFillRect != null)
                {
                    var progressFill = progressFillRect.GetComponent<Image>();
                    progressFill.color = CollectionGold;
                    progressFill.pixelsPerUnitMultiplier = 60f / 16.2f;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(progressFill);

                    var stripeRect = CreateUIObject("Highlight", progressFillRect);
                    stripeRect.anchorMin = new Vector2(0f, 0.53f);
                    stripeRect.anchorMax = new Vector2(1f, 0.82f);
                    stripeRect.offsetMin = new Vector2(8f, 0f);
                    stripeRect.offsetMax = new Vector2(-8f, 0f);
                    var stripe = stripeRect.gameObject.AddComponent<Image>();
                    stripe.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
                    stripe.type = Image.Type.Sliced;
                    stripe.pixelsPerUnitMultiplier = 10f;
                    stripe.color = CollectionProgressHighlight;
                    stripe.raycastTarget = false;
                }
            }

            var progressLabel = progressInstance.transform.Find("ValueLabel");
            if (progressLabel != null)
            {
                progressLabel.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(progressLabel.gameObject);
            }

            return progressInstance.GetComponent<UIV2ProgressBar>();
        }

        // ------------------------------------------------------------------
        // ACCUSI (22_collezione_accusi (1).png: stessa spaziatura del mockup, +43px come EMOTICON)
        // ------------------------------------------------------------------

        private static readonly Color AccusiHeroFill = new Color32(21, 37, 58, 255);
        private static readonly Color AccusoRowLockedFill = new Color32(16, 28, 46, 255);
        private static readonly Color AccusoLockedCircle = new Color32(12, 22, 37, 255);
        private static readonly Color AccusoPlaceholderRed = new Color32(200, 58, 48, 255);
        private static readonly Color AccusoLockedTitle = new Color32(140, 162, 192, 255);

        /// <summary>
        /// Cerchio pieno: panel_fill_r24 (raggio reale 48) con pixelsPerUnitMultiplier tale che il raggio
        /// valga diametro/2 - nessuno stretch.
        /// </summary>
        private static Image AddCircleImage(RectTransform rect, float diameter, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 48f / (diameter * 0.5f);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// Placeholder dell'arte accuso (nessuno sprite accuso esiste nel progetto): anello oro + disco
        /// rosso, lo stesso segnaposto che il mockup mostra nella riga lista.
        /// </summary>
        private static GameObject AddAccusoArtPlaceholder(RectTransform parent, Vector2 anchor, Vector2 position,
            float diameter, float ringThickness)
        {
            var root = CreateUIObject("ArtworkPlaceholder", parent);
            Place(root, anchor, new Vector2(0.5f, 0.5f), position, new Vector2(diameter, diameter));
            AddCircleImage(root, diameter, CollectionGold);
            var inner = CreateUIObject("Fill", root);
            float innerDiameter = diameter - ringThickness * 2f;
            Place(inner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(innerDiameter, innerDiameter));
            AddCircleImage(inner, innerDiameter, AccusoPlaceholderRed);
            return root.gameObject;
        }

        /// <summary>Riga lista 960x129 (x 60..1020), passo 146.</summary>
        private static AccusoRowView BuildAccusoRowPrefab()
        {
            var go = new GameObject("AccusoRow", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(960f, 129f);

            var border = AddRoundedPanel(rect, "panel_fill_r24", 48f, 22f, CollectionCardBorder, 3f, CollectionCardFill,
                out var fillRect, out var fill);

            var artCenter = new Vector2(70f, -64f);
            var artRect = CreateUIObject("Artwork", rect);
            Place(artRect, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), artCenter, new Vector2(84f, 84f));
            var art = artRect.gameObject.AddComponent<Image>();
            art.preserveAspect = true;
            art.raycastTarget = false;
            artRect.gameObject.SetActive(false);

            var placeholder = AddAccusoArtPlaceholder(rect, new Vector2(0f, 1f), artCenter, 84f, 4f);

            var lockedRect = CreateUIObject("LockedBadge", rect);
            Place(lockedRect, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), artCenter, new Vector2(84f, 84f));
            AddCircleImage(lockedRect, 84f, AccusoLockedCircle);
            var lockRect = CreateUIObject("LockIcon", lockedRect);
            Place(lockRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -1f), new Vector2(36f, 42f));
            var lockImage = lockRect.gameObject.AddComponent<Image>();
            lockImage.sprite = LoadSprite(IconsPath, "ic_lock");
            lockImage.preserveAspect = true;
            lockImage.raycastTarget = false;
            lockedRect.gameObject.SetActive(false);

            var titleRect = CreateUIObject("TitleLabel", rect);
            Place(titleRect, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(136f, -46f), new Vector2(600f, 40f));
            var title = AddText(titleRect, "Accuso", 28f, FontStyles.Bold, HomeTextLight, TextAlignmentOptions.MidlineLeft);
            title.enableWordWrapping = false;
            ApplyOutline(title, NavyOutlineMaterial());

            var descRect = CreateUIObject("DescriptionLabel", rect);
            Place(descRect, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(136f, -86f), new Vector2(600f, 32f));
            var desc = AddText(descRect, "-", 22f, FontStyles.Normal, CollectionSubtitle, TextAlignmentOptions.MidlineLeft);
            desc.enableWordWrapping = false;
            desc.overflowMode = TextOverflowModes.Ellipsis;

            var equippedRect = CreateUIObject("EquippedLabel", rect);
            Place(equippedRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-35f, 0f), new Vector2(200f, 32f));
            var equippedText = AddText(equippedRect, "IN USO", 22f, FontStyles.Bold, CollectionHeaderGold, TextAlignmentOptions.MidlineRight);
            equippedText.enableWordWrapping = false;
            equippedRect.gameObject.SetActive(false);

            // Bottone prezzo/requisito: parte visibile 186x45 (btn_blue_long, 90 righe visibili su 102).
            var actionRect = CreateUIObject("ActionButton", rect);
            Place(actionRect, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-117.5f, 1.5f), new Vector2(188f, 51f));
            var actionImage = AddSlicedImage(actionRect, LoadSprite(IconsPath, "btn_blue_long"), 51f);
            var actionButton = actionRect.gameObject.AddComponent<Button>();
            actionButton.targetGraphic = actionImage;
            var actionLabelRect = CreateUIObject("Label", actionRect);
            StretchFill(actionLabelRect);
            var actionLabel = AddText(actionLabelRect, "-", 22f, FontStyles.Bold, HomeTextLight, TextAlignmentOptions.Center);
            actionLabel.enableWordWrapping = false;
            ApplyOutline(actionLabel, NavyOutlineMaterial());

            var comp = go.AddComponent<AccusoRowView>();
            SetPrivateField(comp, "border", border);
            SetPrivateField(comp, "fillRect", fillRect);
            SetPrivateField(comp, "fill", fill);
            SetPrivateField(comp, "artwork", art);
            SetPrivateField(comp, "artworkPlaceholder", placeholder);
            SetPrivateField(comp, "lockedBadge", lockedRect.gameObject);
            SetPrivateField(comp, "titleLabel", title);
            SetPrivateField(comp, "descriptionLabel", desc);
            SetPrivateField(comp, "equippedLabel", equippedRect.gameObject);
            SetPrivateField(comp, "actionButton", actionButton);
            SetPrivateField(comp, "actionBackground", actionImage);
            SetPrivateField(comp, "actionLabel", actionLabel);
            SetPrivateField(comp, "ownedActionSprite", LoadSprite(IconsPath, "btn_teal"));
            SetPrivateField(comp, "lockedActionSprite", LoadSprite(IconsPath, "btn_blue_long"));
            SetPrivateField(comp, "equippedBorderColor", CollectionGold);
            SetPrivateField(comp, "normalBorderColor", CollectionCardBorder);
            SetPrivateField(comp, "unlockedFillColor", CollectionCardFill);
            SetPrivateField(comp, "lockedFillColor", AccusoRowLockedFill);
            SetPrivateField(comp, "titleColor", HomeTextLight);
            SetPrivateField(comp, "lockedTitleColor", AccusoLockedTitle);
            SetPrivateField(comp, "descriptionColor", CollectionSubtitle);
            SetPrivateField(comp, "lockedDescriptionColor", EmoticonLockedLabel);

            return SaveAsPrefab(go, $"{ScreensPrefabDir}/AccusoRow.prefab").GetComponent<AccusoRowView>();
        }

        private static CollectionAccusiPanel BuildAccusiPanelContent(RectTransform panelRect, AccusoRowView rowPrefab,
            GameObject progressBarPrefab)
        {
            var content = CreateCollectionScrollContent(panelRect, 31);

            // ---- IN USO: box 980x325 (x 50..1030) ----
            BuildCollectionSectionHeader(content, "InUseHeader", "IN USO", false, out _);
            AddVerticalSpacer(content, 17f);

            var heroSlot = CreateUIObject("HeroSlot", content);
            AddLayoutElement(heroSlot, preferredHeight: 325f);
            var heroRect = CreateUIObject("HeroPanel", heroSlot);
            heroRect.anchorMin = Vector2.zero;
            heroRect.anchorMax = Vector2.one;
            heroRect.offsetMin = new Vector2(50f, 0f);
            heroRect.offsetMax = new Vector2(-50f, 0f);
            AddRoundedPanel(heroRect, "panel_fill_r30", 60f, 30f, CollectionGold, 5f, AccusiHeroFill, out _, out _);

            var heroArtCenter = new Vector2(140f, -118f);
            var heroArtRect = CreateUIObject("Artwork", heroRect);
            Place(heroArtRect, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), heroArtCenter, new Vector2(168f, 168f));
            var heroArt = heroArtRect.gameObject.AddComponent<Image>();
            heroArt.preserveAspect = true;
            heroArt.raycastTarget = false;
            heroArtRect.gameObject.SetActive(false);
            var heroPlaceholder = AddAccusoArtPlaceholder(heroRect, new Vector2(0f, 1f), heroArtCenter, 168f, 6f);

            var heroTitleRect = CreateUIObject("Title", heroRect);
            Place(heroTitleRect, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(272f, -73f), new Vector2(640f, 48f));
            var heroTitle = AddText(heroTitleRect, "ACCUSO", 34f, FontStyles.Bold, CollectionNameCream, TextAlignmentOptions.MidlineLeft);
            heroTitle.enableWordWrapping = false;
            heroTitle.enableAutoSizing = true;
            heroTitle.fontSizeMin = 24f;
            heroTitle.fontSizeMax = 34f;
            ApplyOutline(heroTitle, NavyOutlineMaterial());

            var heroSubRect = CreateUIObject("Subtitle", heroRect);
            Place(heroSubRect, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(272f, -120f), new Vector2(640f, 32f));
            var heroSubtitle = AddText(heroSubRect, "-", 22f, FontStyles.Normal, CollectionSubtitle, TextAlignmentOptions.MidlineLeft);
            heroSubtitle.enableWordWrapping = false;

            // Descrizione a capo automatico in un box stretto (2 righe, passo 36 come nel mockup).
            var heroDescRect = CreateUIObject("Description", heroRect);
            Place(heroDescRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(272f, -164f), new Vector2(320f, 80f));
            var heroDescription = AddText(heroDescRect, "-", 24f, FontStyles.Normal, CollectionSubtitle, TextAlignmentOptions.TopLeft);
            heroDescription.enableWordWrapping = true;
            heroDescription.lineSpacing = 29f;
            heroDescription.overflowMode = TextOverflowModes.Ellipsis;

            // ANTEPRIMA: parte visibile 310x49 (btn_blue_long a scala uniforme).
            var previewRect = CreateUIObject("PreviewButton", heroRect);
            Place(previewRect, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(434.5f, -275f), new Vector2(312.2f, 55.5f));
            var previewImage = AddSlicedImage(previewRect, LoadSprite(IconsPath, "btn_blue_long"), 55.5f);
            var previewButton = previewRect.gameObject.AddComponent<Button>();
            previewButton.targetGraphic = previewImage;
            var previewLabelRect = CreateUIObject("Label", previewRect);
            StretchFill(previewLabelRect);
            var previewLabel = AddText(previewLabelRect, "ANTEPRIMA", 24f, FontStyles.Bold, HomeTextLight, TextAlignmentOptions.Center);
            previewLabel.enableWordWrapping = false;
            ApplyOutline(previewLabel, NavyOutlineMaterial());

            // ---- COLLEZIONE ----
            AddVerticalSpacer(content, 38f);
            BuildCollectionSectionHeader(content, "CollectionHeader", "COLLEZIONE", true, out var countLabel);
            AddVerticalSpacer(content, 12.5f);
            var progress = AddCollectionProgressBar(content, progressBarPrefab);
            AddVerticalSpacer(content, 32.5f);

            var listRect = CreateUIObject("AccusiList", content);
            var listLayout = listRect.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(60, 60, 0, 0);
            listLayout.spacing = 17f;
            listLayout.childAlignment = TextAnchor.UpperCenter;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = false;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            var panel = panelRect.gameObject.AddComponent<CollectionAccusiPanel>();
            SetPrivateField(panel, "heroArtwork", heroArt);
            SetPrivateField(panel, "heroArtworkPlaceholder", heroPlaceholder);
            SetPrivateField(panel, "heroTitleLabel", heroTitle);
            SetPrivateField(panel, "heroSubtitleLabel", heroSubtitle);
            SetPrivateField(panel, "heroDescriptionLabel", heroDescription);
            SetPrivateField(panel, "previewButton", previewButton);
            SetPrivateField(panel, "collectionCountLabel", countLabel);
            SetPrivateField(panel, "collectionProgress", progress);
            SetPrivateField(panel, "listContainer", listRect);
            SetPrivateField(panel, "rowPrefab", rowPrefab);
            return panel;
        }

        // ------------------------------------------------------------------
        // Helper Collezione
        // ------------------------------------------------------------------

        /// <summary>
        /// Pannello arrotondato = due panel_fill neutri tinti (bordo sotto, riempimento sopra inset
        /// dello spessore). spriteRadius = raggio REALE misurato sui pixel: ~48 per panel_fill_r24,
        /// ~60 per panel_fill_r30 (coincide col bordo 9-slice, NON col numero nel nome). Non si usa panel_ring_*: il suo anello cotto e' spesso ~12px e scala
        /// insieme al raggio, mentre qui servono bordi da 3-5px con raggio 22-30.
        /// </summary>
        private static Image AddRoundedPanel(RectTransform rect, string spriteName, float spriteRadius, float radius,
            Color borderColor, float borderThickness, Color fillColor, out RectTransform fillRect, out Image fillImage)
        {
            var sprite = LoadSprite(PanelsNeutralPath, spriteName);

            var border = rect.gameObject.AddComponent<Image>();
            border.sprite = sprite;
            border.type = Image.Type.Sliced;
            border.color = borderColor;
            border.pixelsPerUnitMultiplier = spriteRadius / radius;
            border.raycastTarget = false;

            fillRect = CreateUIObject("Fill", rect);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(borderThickness, borderThickness);
            fillRect.offsetMax = new Vector2(-borderThickness, -borderThickness);
            fillImage = fillRect.gameObject.AddComponent<Image>();
            fillImage.sprite = sprite;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = fillColor;
            fillImage.pixelsPerUnitMultiplier = spriteRadius / Mathf.Max(1f, radius - borderThickness);
            fillImage.raycastTarget = false;

            return border;
        }

        /// <summary>Titolo oro + linea sottile (+ contatore opzionale a destra), alto 36.</summary>
        private static TMP_Text BuildCollectionSectionHeader(RectTransform parent, string objectName, string title,
            bool withCount, out TMP_Text countLabel)
        {
            var header = CreateUIObject(objectName, parent);
            AddLayoutElement(header, preferredHeight: 36f);
            var layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(76, 75, 0, 0);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var titleRect = CreateUIObject("Title", header);
            titleRect.sizeDelta = new Vector2(0f, 36f);
            var titleLabel = AddText(titleRect, title, 24f, FontStyles.Bold, CollectionHeaderGold, TextAlignmentOptions.MidlineLeft);
            titleLabel.enableWordWrapping = false;

            var lineRect = CreateUIObject("Line", header);
            AddLayoutElement(lineRect, flexibleWidth: 1f);
            lineRect.sizeDelta = new Vector2(0f, 2f);
            var line = lineRect.gameObject.AddComponent<Image>();
            line.color = CollectionHeaderLine;
            line.raycastTarget = false;

            countLabel = null;
            if (withCount)
            {
                var countRect = CreateUIObject("Count", header);
                countRect.sizeDelta = new Vector2(0f, 36f);
                countLabel = AddText(countRect, "0 / 0", 26f, FontStyles.Bold, CollectionHeaderGold, TextAlignmentOptions.MidlineRight);
                countLabel.enableWordWrapping = false;
            }
            return titleLabel;
        }

        private static void AddVerticalSpacer(RectTransform parent, float height)
        {
            var spacer = CreateUIObject("Spacer", parent);
            AddLayoutElement(spacer, preferredHeight: height);
        }

        /// <summary>
        /// Dorso mazzo del ventaglio: card_back_green a aspect nativo (alpha 145x210 -> 160x230
        /// visibili), la posizione passata compensa l'offset del margine trasparente.
        /// </summary>
        private static Image AddFanCard(RectTransform parent, string objectName, Vector2 position, float rotationZ)
        {
            var cardRect = CreateUIObject(objectName, parent);
            Place(cardRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(187f, 268f));
            cardRect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            var image = cardRect.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(IconsPath, "card_back_green");
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }
    }
}
