using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.Unity.UIKit;

namespace Project51.EditorTools
{
    /// <summary>
    /// Fase 6: estrae la schermata Emoticon (Fase 5, responsive, approvata) in prefab/componenti
    /// riutilizzabili per le altre schermate del progetto. Costruisce e SALVA 7 prefab
    /// (Assets/Prefabs/UI/Kit/) via i componenti in Project51.Unity.UIKit, poi ricompone la
    /// schermata Emoticon in HomeScreen.unity instanziandoli - stesso risultato visivo esatto
    /// della Fase 5 (stessi colori/dimensioni/gap/padding, solo riorganizzati in prefab).
    /// Nessuna coordinata specifica di QUESTA schermata dentro i componenti C# (Project51.Unity.
    /// UIKit) - vivono tutte nei prefab (RectTransform/LayoutGroup), i componenti sono puro
    /// comportamento (stato selezionato, testo, empty/equipped/locked).
    /// </summary>
    public static class EmoticonKitBuilder
    {
        private const string ScenePath = "Assets/Scenes/HomeScreen.unity";
        private const string PrefabDir = "Assets/Prefabs/UI/Kit";
        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;

        // Stessi shift verticali della Fase 4/5 - INVARIATI (nessun ulteriore cambiamento Y).
        private const float TabsYShift = 70f;
        private const float EquippedYShift = 140f;
        private const float CollectionYShift = 210f;

        // Stessi colori placeholder della Fase 2-5.
        private static readonly Color BgColor = new Color(0.04f, 0.05f, 0.10f, 1f);
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
        private static readonly Color EquippedBorder = new Color(0.85f, 0.65f, 0.20f, 1f);
        private static readonly Color CheckGreen = new Color(0.30f, 0.80f, 0.45f, 1f);
        private static readonly Color RemoveBg = new Color(0.55f, 0.20f, 0.20f, 1f);
        private static readonly Color LockColor = new Color(0.80f, 0.75f, 0.55f, 1f);
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

        private static readonly string[] EmotionNames = { "Risata", "Arrabbiato", "Sorpreso", "Pensieroso", "Triste", "Furbo" };
        private static readonly Color[] EmotionColors =
        {
            new Color(1.00f, 0.85f, 0.20f, 1f),
            new Color(0.85f, 0.25f, 0.20f, 1f),
            new Color(0.95f, 0.60f, 0.25f, 1f),
            new Color(0.55f, 0.60f, 0.70f, 1f),
            new Color(0.35f, 0.55f, 0.85f, 1f),
            new Color(0.35f, 0.75f, 0.45f, 1f),
        };

        private static Sprite _knobSprite;
        private static Sprite _checkmarkSprite;

        [MenuItem("Tools/Dragons Hoard/Build Emoticon Screen (Reusable Kit)")]
        private static void Build()
        {
            if (!AssetDatabase.IsValidFolder(PrefabDir))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs/UI", "Kit");
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[EmoticonKitBuilder] Nessun Canvas in HomeScreen.unity.");
                return;
            }
            var canvasRect = (RectTransform)canvas.transform;

            var emoticonScreenExact = canvasRect.Find("EmoticonScreenExact");
            if (emoticonScreenExact != null) emoticonScreenExact.gameObject.SetActive(false);

            foreach (var childName in new[] { "Background", "SafeArea", "EmoticonScreen" })
            {
                var old = canvasRect.Find(childName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            // --- 1) Prefab UI_Tabs3 ------------------------------------------------------
            var tabsPrefab = BuildTabsPrefab();

            // --- 2) Prefab UI_EquippedRow3 ------------------------------------------------
            var equippedPrefab = BuildEquippedRowPrefab();

            // --- 3) Prefab UI_CollectionCard ----------------------------------------------
            var cardPrefab = BuildCollectionCardPrefab();

            // --- 4) Prefab UI_CollectionGrid4 (referenzia UI_CollectionCard) --------------
            var gridPrefab = BuildCollectionGridPrefab(cardPrefab);

            // --- 5) Prefab UI_BottomNav4 ---------------------------------------------------
            var bottomNavPrefab = BuildBottomNavPrefab();

            // --- 6) Prefab UI_Header --------------------------------------------------------
            var headerPrefab = BuildHeaderPrefab();

            // --- 7) Prefab UI_ScreenRoot (scheletro generico, riusabile per QUALSIASI schermata) ---
            var screenRootPrefab = BuildScreenRootPrefab();

            // --- Riassembla la schermata Emoticon in HomeScreen.unity instanziando i prefab ---
            AssembleEmoticonScreen(canvasRect, screenRootPrefab, headerPrefab, tabsPrefab, equippedPrefab, gridPrefab, bottomNavPrefab);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[EmoticonKitBuilder] 7 prefab creati in " + PrefabDir + " e schermata Emoticon ricomposta in HomeScreen.unity con le stesse identiche misure della Fase 5.");
        }

        // ------------------------------------------------------------------
        // 1) UI_Tabs3
        // ------------------------------------------------------------------

        private static GameObject BuildTabsPrefab()
        {
            var root = CreateUIObject("UI_Tabs3", null);
            root.sizeDelta = new Vector2(RefWidth, 69f);
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(60, 62, 0, 0);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var entries = new UITabEntry[3];
            string[] labels = { "MAZZI", "EMOTICON", "ACCUSI" };
            float[] heights = { 56f, 69f, 56f };
            for (int i = 0; i < 3; i++)
            {
                var tab = CreateFixedSizeChild(root, "Tab_" + labels[i], 310f, heights[i]);
                var bg = AddFlat(tab, i == 1 ? TabSelectedColor : TabColor);
                var hit = tab.gameObject.AddComponent<Button>();
                hit.targetGraphic = bg;
                var labelRt = CreateUIObject("Label", tab);
                StretchFill(labelRt);
                var label = AddText(labelRt, labels[i], i == 1 ? 27f : 26f, TextAlignmentOptions.Center, LabelWhite, FontStyles.Bold);
                entries[i] = new UITabEntry { button = hit, rect = tab, background = bg, label = label };
            }

            var tabsRow = root.gameObject.AddComponent<UITabsRow>();
            var so = new SerializedObject(tabsRow);
            var tabsProp = so.FindProperty("tabs");
            tabsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var element = tabsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("button").objectReferenceValue = entries[i].button;
                element.FindPropertyRelative("rect").objectReferenceValue = entries[i].rect;
                element.FindPropertyRelative("background").objectReferenceValue = entries[i].background;
                element.FindPropertyRelative("label").objectReferenceValue = entries[i].label;
            }
            so.FindProperty("normalHeight").floatValue = 56f;
            so.FindProperty("selectedHeight").floatValue = 69f;
            so.FindProperty("normalColor").colorValue = TabColor;
            so.FindProperty("selectedColor").colorValue = TabSelectedColor;
            so.FindProperty("selectedIndex").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveAndDestroy(root.gameObject, "UI_Tabs3");
        }

        // ------------------------------------------------------------------
        // 2) UI_EquippedRow3
        // ------------------------------------------------------------------

        private static GameObject BuildEquippedRowPrefab()
        {
            var root = CreateUIObject("UI_EquippedRow3", null);
            root.sizeDelta = new Vector2(RefWidth, 249f);
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(59, 58, 0, 0);
            layout.spacing = 21f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var slotsData = new UIEquippedSlotRefs[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = CreateFixedSizeChild(root, "EquippedSlot" + (i + 1), 307f, 249f);
                AddFlat(slot, EquippedColor);

                // --- stato Filled ---
                var filled = CreateUIObject("Filled", slot);
                StretchFill(filled);
                AddFillLineLocal(filled, EquippedColor, EquippedBorder, 6f);
                var iconRt = CreateUIObject("Icon", filled);
                SetAnchoredRect(iconRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(128f, 128f), new Vector2(0f, -10f));
                var icon = iconRt.gameObject.AddComponent<Image>();
                icon.sprite = KnobSprite();
                icon.color = EmotionColors[0];
                var nameRt = CreateUIObject("NameText", filled);
                SetAnchoredRect(nameRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(307f - 12f, 24f), new Vector2(0f, 8f));
                var nameLabel = AddText(nameRt, "", 22f, TextAlignmentOptions.Center, LabelWhite);
                var removeRt = CreateUIObject("RemoveButton", filled);
                SetAnchoredRect(removeRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(32f, 32f), new Vector2(-4f, -4f));
                var removeBg = removeRt.gameObject.AddComponent<Image>();
                removeBg.sprite = KnobSprite();
                removeBg.color = RemoveBg;
                var removeButton = removeRt.gameObject.AddComponent<Button>();
                removeButton.targetGraphic = removeBg;
                var removeLabelRt = CreateUIObject("Label", removeRt);
                StretchFill(removeLabelRt);
                AddText(removeLabelRt, "X", 16f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

                // --- stato Empty ---
                var empty = CreateUIObject("Empty", slot);
                StretchFill(empty);
                var emptyFillRt = CreateUIObject("Fill", empty);
                StretchFill(emptyFillRt);
                AddFlat(emptyFillRt, new Color(0.055f, 0.094f, 0.157f, 220f / 255f));
                CreateDashedBorder(empty, 307f, 249f, new Color(0.275f, 0.392f, 0.549f, 1f));
                var plusRt = CreateUIObject("PlusIcon", empty);
                SetTopLeft(plusRt, 114f, 86f, 78f, 78f);
                AddFlat(plusRt, PlusColor);
                var plusLabelRt = CreateUIObject("Label", plusRt);
                StretchFill(plusLabelRt);
                AddText(plusLabelRt, "+", 40f, TextAlignmentOptions.Center, LabelWhite);
                var emptyTextRt = CreateUIObject("Text", empty);
                SetAnchoredRect(emptyTextRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(307f - 12f, 24f), new Vector2(0f, 8f));
                AddText(emptyTextRt, "Slot libero", 22f, TextAlignmentOptions.Center, LabelWhite);

                empty.gameObject.SetActive(i == 2); // di default: slot 1 e 2 filled, slot 3 empty (stesso stato della Fase 5)
                filled.gameObject.SetActive(i != 2);

                slotsData[i] = new UIEquippedSlotRefs
                {
                    filledGroup = filled.gameObject,
                    emptyGroup = empty.gameObject,
                    icon = icon,
                    nameLabel = nameLabel,
                    removeButton = removeButton,
                };
            }

            var equippedRow = root.gameObject.AddComponent<UIEquippedRow>();
            var so = new SerializedObject(equippedRow);
            var slotsProp = so.FindProperty("slots");
            slotsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var element = slotsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("filledGroup").objectReferenceValue = slotsData[i].filledGroup;
                element.FindPropertyRelative("emptyGroup").objectReferenceValue = slotsData[i].emptyGroup;
                element.FindPropertyRelative("icon").objectReferenceValue = slotsData[i].icon;
                element.FindPropertyRelative("nameLabel").objectReferenceValue = slotsData[i].nameLabel;
                element.FindPropertyRelative("removeButton").objectReferenceValue = slotsData[i].removeButton;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveAndDestroy(root.gameObject, "UI_EquippedRow3");
        }

        // ------------------------------------------------------------------
        // 3) UI_CollectionCard
        // ------------------------------------------------------------------

        private static GameObject BuildCollectionCardPrefab()
        {
            var root = CreateUIObject("UI_CollectionCard", null);
            root.sizeDelta = new Vector2(228f, 211f);

            var borderRt = CreateUIObject("Border", root);
            StretchFill(borderRt);
            var border = AddFlat(borderRt, CardBorder);
            var fillRt = CreateUIObject("Fill", root);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.offsetMin = new Vector2(2f, 2f);
            fillRt.offsetMax = new Vector2(-2f, -2f);
            AddFlat(fillRt, CardColor);

            var iconRt = CreateUIObject("EmojiPlaceholder", root);
            SetAnchoredRect(iconRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(100f, 100f), new Vector2(0f, -12f));
            var icon = iconRt.gameObject.AddComponent<Image>();
            icon.sprite = KnobSprite();
            icon.color = EmotionColors[0];

            var lockHolder = CreateUIObject("LockIcon", root);
            SetAnchoredRect(lockHolder, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(52f, 52f), new Vector2(0f, -26f));
            BuildLockGlyphContent(lockHolder);
            lockHolder.gameObject.SetActive(false);

            var nameRt = CreateUIObject("NameText", root);
            SetAnchoredRect(nameRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(228f - 12f, 24f), new Vector2(0f, 8f));
            var nameLabel = AddText(nameRt, "Risata", 18f, TextAlignmentOptions.Center, LabelWhite);

            var badgeRt = CreateUIObject("CheckBadge", root);
            SetAnchoredRect(badgeRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(28f, 28f), new Vector2(-4f, -4f));
            var badgeBg = badgeRt.gameObject.AddComponent<Image>();
            badgeBg.sprite = KnobSprite();
            badgeBg.color = CheckGreen;
            var checkRt = CreateUIObject("Check", badgeRt);
            SetAnchoredRect(checkRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(16f, 16f), Vector2.zero);
            var checkImg = checkRt.gameObject.AddComponent<Image>();
            checkImg.sprite = CheckmarkSprite();
            checkImg.color = Color.white;
            badgeRt.gameObject.SetActive(false);

            var card = root.gameObject.AddComponent<UICollectionCard>();
            var so = new SerializedObject(card);
            so.FindProperty("border").objectReferenceValue = border;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("lockIcon").objectReferenceValue = lockHolder.gameObject;
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("checkBadge").objectReferenceValue = badgeRt.gameObject;
            so.FindProperty("unlockedBorderColor").colorValue = CardBorder;
            so.FindProperty("equippedBorderColor").colorValue = EquippedBorder;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveAndDestroy(root.gameObject, "UI_CollectionCard");
        }

        // ------------------------------------------------------------------
        // 4) UI_CollectionGrid4
        // ------------------------------------------------------------------

        private static GameObject BuildCollectionGridPrefab(GameObject cardPrefab)
        {
            const float cellW = 228f, cellH = 211f, gapX = 17f, gapY = 19f;
            var root = CreateUIObject("UI_CollectionGrid4", null);
            root.sizeDelta = new Vector2(RefWidth, cellH * 3f + gapY * 2f);
            var gridLayout = root.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(59, 58, 0, 0);
            gridLayout.cellSize = new Vector2(cellW, cellH);
            gridLayout.spacing = new Vector2(gapX, gapY);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;

            var gridComp = root.gameObject.AddComponent<UICollectionGrid>();
            var so = new SerializedObject(gridComp);
            so.FindProperty("cardPrefab").objectReferenceValue = cardPrefab.GetComponent<UICollectionCard>();
            so.FindProperty("container").objectReferenceValue = root;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveAndDestroy(root.gameObject, "UI_CollectionGrid4");
        }

        // ------------------------------------------------------------------
        // 5) UI_BottomNav4
        // ------------------------------------------------------------------

        private static GameObject BuildBottomNavPrefab()
        {
            var root = CreateUIObject("UI_BottomNav4", null);
            SetBottomLeft(root, 0f, 0f, RefWidth, 190f);
            AddFlat(root, NavColor);

            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            string[] labels = { "Gioca", "Carte", "Negozio", "Profilo" };
            var itemsData = new UINavItemRefs[4];
            for (int i = 0; i < 4; i++)
            {
                var item = CreateUIObject("NavItem_" + labels[i], root);
                var le = item.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                var hit = item.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
                var button = item.gameObject.AddComponent<Button>();
                button.targetGraphic = hit;

                var iconHolder = CreateUIObject("Icon", item);
                SetAnchoredRect(iconHolder, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(60f, 60f), new Vector2(0f, -18f));
                BuildNavIconContent(iconHolder, labels[i]);

                var labelRt = CreateUIObject("Label", item);
                SetAnchoredRect(labelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(262f, 28f), new Vector2(0f, 20f));
                var label = AddText(labelRt, labels[i], 18f, TextAlignmentOptions.Center, LabelWhite);

                var pill = CreateUIObject("Pill", item);
                SetAnchoredRect(pill, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(243f, 100f), new Vector2(0f, 72f));
                AddFlat(pill, PillColor);
                var pillIconHolder = CreateUIObject("Icon", pill);
                SetAnchoredRect(pillIconHolder, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(40f, 40f), new Vector2(0f, -12f));
                BuildNavIconContent(pillIconHolder, labels[i]);
                var pillLabelRt = CreateUIObject("Label", pill);
                SetAnchoredRect(pillLabelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(220f, 26f), new Vector2(0f, 8f));
                AddText(pillLabelRt, labels[i].ToUpperInvariant(), 22f, TextAlignmentOptions.Center, Color.black, FontStyles.Bold);
                pill.gameObject.SetActive(i == 1); // default: "Carte" selezionato, come nella Fase 5

                itemsData[i] = new UINavItemRefs { button = button, iconHolder = iconHolder.gameObject, label = label, pill = pill.gameObject };
            }

            var navBar = root.gameObject.AddComponent<UIBottomNavBar>();
            var so = new SerializedObject(navBar);
            var itemsProp = so.FindProperty("items");
            itemsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                var element = itemsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("button").objectReferenceValue = itemsData[i].button;
                element.FindPropertyRelative("iconHolder").objectReferenceValue = itemsData[i].iconHolder;
                element.FindPropertyRelative("label").objectReferenceValue = itemsData[i].label;
                element.FindPropertyRelative("pill").objectReferenceValue = itemsData[i].pill;
            }
            so.FindProperty("selectedIndex").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveAndDestroy(root.gameObject, "UI_BottomNav4");
        }

        // ------------------------------------------------------------------
        // 6) UI_Header
        // ------------------------------------------------------------------

        private static GameObject BuildHeaderPrefab()
        {
            var root = CreateUIObject("UI_Header", null);
            root.sizeDelta = new Vector2(RefWidth, 205f);

            var profileArea = CreateUIObject("ProfileArea", root);
            SetTopLeft(profileArea, 55f, 25f, 180f, 150f);
            var avatarFrame = CreateUIObject("AvatarFrame", profileArea);
            SetTopLeft(avatarFrame, 0f, 0f, 110f, 110f);
            var avatarFrameImg = avatarFrame.gameObject.AddComponent<Image>();
            avatarFrameImg.sprite = KnobSprite();
            avatarFrameImg.color = AvatarFrameColor;
            var avatarPlaceholder = CreateUIObject("AvatarPlaceholder", avatarFrame);
            SetTopLeft(avatarPlaceholder, 6f, 6f, 98f, 98f);
            var avatarFillImg = avatarPlaceholder.gameObject.AddComponent<Image>();
            avatarFillImg.sprite = KnobSprite();
            avatarFillImg.color = AvatarFillColor;

            var levelBadge = CreateUIObject("LevelBadge", profileArea);
            SetTopLeft(levelBadge, -15f, -10f, 40f, 40f);
            var levelBg = levelBadge.gameObject.AddComponent<Image>();
            levelBg.sprite = KnobSprite();
            levelBg.color = LevelBadgeColor;
            var levelLabelRt = CreateUIObject("Label", levelBadge);
            StretchFill(levelLabelRt);
            var levelText = AddText(levelLabelRt, "1", 20f, TextAlignmentOptions.Center, Color.black, FontStyles.Bold);

            var playerName = CreateUIObject("PlayerName", profileArea);
            SetTopLeft(playerName, 0f, 115f, 150f, 32f);
            AddFlat(playerName, NamePlateColor);
            var nameLabelRt = CreateUIObject("Label", playerName);
            StretchFill(nameLabelRt);
            var playerNameText = AddText(nameLabelRt, "Giocatore", 16f, TextAlignmentOptions.Center, LabelWhite);

            var currencyArea = CreateUIObject("CurrencyArea", root);
            SetTopLeft(currencyArea, 250f, 45f, 260f, 50f);
            var coinIcon = CreateUIObject("CoinIcon", currencyArea);
            SetTopLeft(coinIcon, 0f, 7f, 36f, 36f);
            var coinIconImg = coinIcon.gameObject.AddComponent<Image>();
            coinIconImg.sprite = KnobSprite();
            coinIconImg.color = CoinColor;
            var coinValue = CreateUIObject("CoinValue", currencyArea);
            SetTopLeft(coinValue, 46f, 10f, 110f, 32f);
            var coinValueText = AddText(coinValue, "1.250", 24f, TextAlignmentOptions.Left, LabelWhite, FontStyles.Bold);
            var addCurrencyButton = CreateUIObject("AddCurrencyButton", currencyArea);
            SetTopLeft(addCurrencyButton, 166f, 7f, 36f, 36f);
            var addBg = AddFlat(addCurrencyButton, AddButtonColor);
            var addButton = addCurrencyButton.gameObject.AddComponent<Button>();
            addButton.targetGraphic = addBg;
            var addLabelRt = CreateUIObject("Label", addCurrencyButton);
            StretchFill(addLabelRt);
            AddText(addLabelRt, "+", 22f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

            var energyArea = CreateUIObject("EnergyArea", root);
            SetTopLeft(energyArea, 530f, 45f, 220f, 50f);
            var energyIcon = CreateUIObject("EnergyIcon", energyArea);
            SetTopLeft(energyIcon, 0f, 10f, 30f, 30f);
            AddFlat(energyIcon, EnergyIconColor);
            var energyBarBg = CreateUIObject("EnergyBarBG", energyArea);
            SetTopLeft(energyBarBg, 40f, 13f, 140f, 24f);
            AddFlat(energyBarBg, EnergyBarBgColor);
            var energyBarFill = CreateUIObject("EnergyBarFill", energyBarBg);
            SetTopLeft(energyBarFill, 0f, 0f, 84f, 24f);
            AddFlat(energyBarFill, EnergyBarFillColor);
            var energyValue = CreateUIObject("EnergyValue", energyArea);
            SetTopLeft(energyValue, 184f, 10f, 36f, 30f);
            var energyValueText = AddText(energyValue, "48", 20f, TextAlignmentOptions.Center, LabelWhite, FontStyles.Bold);

            var progressArea = CreateUIObject("ProgressArea", root);
            SetTopLeft(progressArea, 800f, 55f, 225f, 44f);
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
            var progressTextRt = CreateUIObject("ProgressText", progressArea);
            SetTopLeft(progressTextRt, 0f, 0f, 225f, 44f);
            var progressText = AddText(progressTextRt, "320 / 500", 22f, TextAlignmentOptions.Center, LabelWhite, FontStyles.Bold);

            var header = root.gameObject.AddComponent<UIHeaderWidget>();
            var so = new SerializedObject(header);
            so.FindProperty("levelText").objectReferenceValue = levelText;
            so.FindProperty("playerNameText").objectReferenceValue = playerNameText;
            so.FindProperty("coinValueText").objectReferenceValue = coinValueText;
            so.FindProperty("addCurrencyButton").objectReferenceValue = addButton;
            so.FindProperty("energyBarFill").objectReferenceValue = energyBarFill;
            so.FindProperty("energyValueText").objectReferenceValue = energyValueText;
            so.FindProperty("energyBarMaxWidth").floatValue = 140f;
            so.FindProperty("progressText").objectReferenceValue = progressText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveAndDestroy(root.gameObject, "UI_Header");
        }

        // ------------------------------------------------------------------
        // 7) UI_ScreenRoot (scheletro generico - Background + SafeArea/HeaderArea+MainContent+BottomNav)
        // ------------------------------------------------------------------

        private static GameObject BuildScreenRootPrefab()
        {
            var root = CreateUIObject("UI_ScreenRoot", null);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var background = CreateUIObject("Background", root);
            SetTopLeft(background, 0f, 0f, RefWidth, RefHeight);
            AddFlat(background, BgColor);

            var safeArea = CreateUIObject("SafeArea", root);
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.pivot = new Vector2(0.5f, 0.5f);
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
            var fitter = safeArea.gameObject.AddComponent<SafeAreaFitter>();
            var fitterSo = new SerializedObject(fitter);
            fitterSo.FindProperty("debugLog").boolValue = false; // il log di debug resta disponibile ma spento di default sul prefab riusabile
            fitterSo.ApplyModifiedPropertiesWithoutUndo();

            var headerArea = CreateUIObject("HeaderArea", safeArea);
            headerArea.anchorMin = headerArea.anchorMax = new Vector2(0f, 1f);
            headerArea.pivot = new Vector2(0f, 1f);

            var mainContent = CreateUIObject("MainContent", safeArea);
            mainContent.anchorMin = mainContent.anchorMax = new Vector2(0f, 1f);
            mainContent.pivot = new Vector2(0f, 1f);

            var bottomNav = CreateUIObject("BottomNav", safeArea);
            StretchFill(bottomNav);

            var screenRoot = root.gameObject.AddComponent<UIScreenRoot>();
            var so = new SerializedObject(screenRoot);
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("safeArea").objectReferenceValue = safeArea;
            so.FindProperty("headerArea").objectReferenceValue = headerArea;
            so.FindProperty("mainContent").objectReferenceValue = mainContent;
            so.FindProperty("bottomNav").objectReferenceValue = bottomNav;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveAndDestroy(root.gameObject, "UI_ScreenRoot");
        }

        // ------------------------------------------------------------------
        // Riassemblaggio schermata Emoticon: istanzia i 7 prefab e li configura con gli stessi
        // dati/posizioni della Fase 5 - nessuna nuova misura, solo composizione.
        // ------------------------------------------------------------------

        private static void AssembleEmoticonScreen(RectTransform canvasRect, GameObject screenRootPrefab, GameObject headerPrefab,
            GameObject tabsPrefab, GameObject equippedPrefab, GameObject gridPrefab, GameObject bottomNavPrefab)
        {
            var screenGo = (GameObject)PrefabUtility.InstantiatePrefab(screenRootPrefab, canvasRect);
            screenGo.name = "EmoticonScreen";
            var screenRoot = screenGo.GetComponent<UIScreenRoot>();
            var screenRt = (RectTransform)screenGo.transform;
            screenRt.anchorMin = Vector2.zero;
            screenRt.anchorMax = Vector2.one;
            screenRt.pivot = new Vector2(0.5f, 0.5f);
            screenRt.offsetMin = Vector2.zero;
            screenRt.offsetMax = Vector2.zero;
            screenRt.localScale = Vector3.one;
            // Il log della SafeAreaFitter di QUESTA schermata resta attivo (utile per il debug
            // gia' visto nelle fasi precedenti) - il prefab di base lo lascia spento di default.
            var fitterSo = new SerializedObject(screenRoot.SafeArea.GetComponent<SafeAreaFitter>());
            fitterSo.FindProperty("debugLog").boolValue = true;
            fitterSo.ApplyModifiedPropertiesWithoutUndo();

            // --- Header ---
            var headerInstance = (GameObject)PrefabUtility.InstantiatePrefab(headerPrefab, screenRoot.HeaderArea);
            var headerRt = (RectTransform)headerInstance.transform;
            SetTopLeft(headerRt, 0f, 0f, RefWidth, 205f);
            var headerWidget = headerInstance.GetComponent<UIHeaderWidget>();
            headerWidget.SetLevel(1);
            headerWidget.SetPlayerName("Giocatore");
            headerWidget.SetCoins(1250);
            headerWidget.SetEnergy(48, 100);
            headerWidget.SetProgress(320, 500);

            // --- MainContent: Tabs / EquippedSection / CollectionSection ---
            var mainContent = screenRoot.MainContent;

            var tabsInstance = (GameObject)PrefabUtility.InstantiatePrefab(tabsPrefab, mainContent);
            SetTopLeft((RectTransform)tabsInstance.transform, 0f, 217f + TabsYShift, RefWidth, 69f);
            // Riapplica lo stato del LayoutGroup (il prefab e' istanziato con selectedIndex=1 di
            // default, gia' coerente con "EMOTICON" - nessuna ulteriore chiamata necessaria).

            var equippedSection = CreateUIObject("EquippedSection", mainContent);
            equippedSection.anchorMin = equippedSection.anchorMax = new Vector2(0f, 1f);
            equippedSection.pivot = new Vector2(0f, 1f);
            var equippedHeader = CreateUIObject("EquippedHeader", equippedSection);
            SetTopLeft(equippedHeader, 59f, 322f + EquippedYShift, 959f, 35f);
            AddText(equippedHeader, "EQUIPAGGIATE · 3 max", 26f, TextAlignmentOptions.Left, LabelWhite);
            var equippedInstance = (GameObject)PrefabUtility.InstantiatePrefab(equippedPrefab, equippedSection);
            SetTopLeft((RectTransform)equippedInstance.transform, 0f, 365f + EquippedYShift, RefWidth, 249f);
            var equippedRow = equippedInstance.GetComponent<UIEquippedRow>();
            equippedRow.SetEquipped(0, EmotionColors[0], "Risata");
            equippedRow.SetEquipped(1, EmotionColors[1], "Arrabbiato");
            equippedRow.SetEmpty(2);

            var collectionSection = CreateUIObject("CollectionSection", mainContent);
            collectionSection.anchorMin = collectionSection.anchorMax = new Vector2(0f, 1f);
            collectionSection.pivot = new Vector2(0f, 1f);
            var collectionHeader = CreateUIObject("CollectionHeader", collectionSection);
            SetTopLeft(collectionHeader, 59f, 677f + CollectionYShift, 959f, 35f);
            AddText(collectionHeader, "COLLEZIONE", 26f, TextAlignmentOptions.Left, LabelWhite);
            var counterLabelRt = CreateUIObject("CounterLabel", collectionSection);
            SetTopLeft(counterLabelRt, 59f, 677f + CollectionYShift, 959f, 35f);
            AddText(counterLabelRt, "6 / 12", 26f, TextAlignmentOptions.Right, LabelWhite);
            var barBg = CreateUIObject("ProgressBar_BG", collectionSection);
            SetTopLeft(barBg, 59f, 721f + CollectionYShift, 959f, 35f);
            AddFlat(barBg, BarBgColor);
            var barFill = CreateUIObject("ProgressBar_Fill", collectionSection);
            SetTopLeft(barFill, 59f, 721f + CollectionYShift, 488f, 35f);
            AddFlat(barFill, BarFillColor);

            var gridInstance = (GameObject)PrefabUtility.InstantiatePrefab(gridPrefab, collectionSection);
            SetTopLeft((RectTransform)gridInstance.transform, 0f, 789f + CollectionYShift, RefWidth, 211f * 3f + 19f * 2f);
            var gridComp = gridInstance.GetComponent<UICollectionGrid>();
            var cardData = new List<UICollectionCardData>(12);
            for (int i = 0; i < EmotionNames.Length; i++)
            {
                cardData.Add(new UICollectionCardData(true, i == 0 || i == 1, EmotionNames[i], EmotionColors[i]));
            }
            for (int i = EmotionNames.Length; i < 12; i++)
            {
                cardData.Add(new UICollectionCardData(false, false, "Bloccata", Color.clear));
            }
            gridComp.Populate(cardData);

            // --- BottomNav ---
            var navInstance = (GameObject)PrefabUtility.InstantiatePrefab(bottomNavPrefab, screenRoot.BottomNav);
            SetBottomLeft((RectTransform)navInstance.transform, 0f, 0f, RefWidth, 190f);
            var navBar = navInstance.GetComponent<UIBottomNavBar>();
            navBar.SelectIndex(1); // "Carte"

            EnforceUniformScale(screenGo.transform);
        }

        // ------------------------------------------------------------------
        // Helper condivisi (stessa tecnica delle fasi precedenti, duplicati qui per tenere
        // questo builder indipendente - vedi EmoticonWireframeBuilder per lo stesso pattern)
        // ------------------------------------------------------------------

        private static GameObject SaveAndDestroy(GameObject go, string prefabName)
        {
            string path = $"{PrefabDir}/{prefabName}.prefab";
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return savedPrefab;
        }

        private static RectTransform CreateFixedSizeChild(RectTransform parent, string name, float width, float height)
        {
            var rt = CreateUIObject(name, parent);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        private static void AddFillLineLocal(RectTransform target, Color fillColor, Color lineColor, float thickness)
        {
            var lineRt = CreateUIObject("Line", target);
            StretchFill(lineRt);
            AddFlat(lineRt, lineColor);
            var fillRt = CreateUIObject("Fill", target);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.offsetMin = new Vector2(thickness, thickness);
            fillRt.offsetMax = new Vector2(-thickness, -thickness);
            AddFlat(fillRt, fillColor);
        }

        private static void BuildLockGlyphContent(RectTransform holder)
        {
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

        private static void BuildNavIconContent(RectTransform holder, string kind)
        {
            float size = holder.sizeDelta.x;
            switch (kind)
            {
                case "Gioca":
                    var body = CreateUIObject("Body", holder);
                    SetAnchoredRect(body, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size, size * 0.55f), Vector2.zero);
                    AddFlat(body, LabelWhite);
                    var btn1 = CreateUIObject("Btn1", holder);
                    SetAnchoredRect(btn1, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size * 0.22f, size * 0.22f), new Vector2(size * 0.18f, 0f));
                    var btn1Img = btn1.gameObject.AddComponent<Image>();
                    btn1Img.sprite = KnobSprite();
                    btn1Img.color = NavColor;
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
                    var wheelLImg = wheelL.gameObject.AddComponent<Image>();
                    wheelLImg.sprite = KnobSprite();
                    wheelLImg.color = LabelWhite;
                    var wheelR = CreateUIObject("WheelR", holder);
                    SetAnchoredRect(wheelR, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(size * 0.2f, size * 0.2f), new Vector2(size * 0.2f, 0f));
                    var wheelRImg = wheelR.gameObject.AddComponent<Image>();
                    wheelRImg.sprite = KnobSprite();
                    wheelRImg.color = LabelWhite;
                    break;
                default: // Profilo
                    var head = CreateUIObject("Head", holder);
                    SetAnchoredRect(head, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(size * 0.42f, size * 0.42f), Vector2.zero);
                    var headImg = head.gameObject.AddComponent<Image>();
                    headImg.sprite = KnobSprite();
                    headImg.color = LabelWhite;
                    var shoulders = CreateUIObject("Shoulders", holder);
                    SetAnchoredRect(shoulders, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(size * 0.8f, size * 0.4f), Vector2.zero);
                    AddFlat(shoulders, LabelWhite);
                    break;
            }
        }

        private static void CreateDashedBorder(RectTransform parent, float width, float height, Color color, float thickness = 3f, float dash = 12f, float gap = 8f)
        {
            CreateDashRow(parent, color, thickness, dash, gap, width, new Vector2(0f, 1f), true);
            CreateDashRow(parent, color, thickness, dash, gap, width, new Vector2(0f, 0f), true);
            // Bug reale trovato via GetWorldCorners: anchor.y=0 (fondo) + pivot top + offset
            // negativo spinge i trattini SOTTO il box (offset negativo da un ancoraggio gia' al
            // fondo esce dal contenitore) invece che al suo interno - deve ancorare dal TOP
            // (y=1), coerente con lo schema gia' corretto delle righe orizzontali sopra.
            CreateDashRow(parent, color, thickness, dash, gap, height, new Vector2(0f, 1f), false);
            CreateDashRow(parent, color, thickness, dash, gap, height, new Vector2(1f, 1f), false);
        }

        private static void CreateDashRow(RectTransform parent, Color color, float thickness, float dash, float gap, float length, Vector2 anchor, bool horizontal)
        {
            int count = Mathf.Max(1, Mathf.FloorToInt(length / (dash + gap)));
            for (int i = 0; i < count; i++)
            {
                var seg = CreateUIObject("Dash", parent);
                float offset = i * (dash + gap);
                if (horizontal)
                    SetAnchoredRect(seg, anchor, anchor, new Vector2(0f, 0.5f), new Vector2(dash, thickness), new Vector2(offset, 0f));
                else
                    SetAnchoredRect(seg, anchor, anchor, new Vector2(0.5f, 1f), new Vector2(thickness, dash), new Vector2(0f, -offset));
                AddFlat(seg, color);
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
            if (parent != null) rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
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
