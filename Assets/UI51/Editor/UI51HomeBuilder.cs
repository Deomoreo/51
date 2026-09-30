using Project51.Core;
using Project51.UIV2.Components;
using Project51.UIV2.Core;
using Project51.UIV2.Screens;
using Project51.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// UI51 Fase 3 (Home e pannelli) su MainMenu: velo sull'ambiente animato, testata account/ospite,
    /// pulsanti laterali, tile Modalita'/Mazzo, GIOCA, barra in basso e i fogli Modalita'/Mazzo.
    /// Ricollega i componenti UIV2 esistenti ai nuovi nodi; il vecchio aspetto resta in scena spento.
    /// Idempotente: rieseguirlo riusa i nodi per nome.
    /// </summary>
    public static class UI51HomeBuilder
    {
        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        const string Tag = "[UI51 Home]";
        // Gli host UIV2 sono in unita' 1080 di larghezza: ogni contenitore UI51 e' largo 390 e scalato.
        internal const float S = 1080f / 390f;
        internal const float TopBarH = 262f / S; // altezza di TopBarHost in unita' mockup

        [MenuItem("Tools/UI51/Build Fase 3 (Home)")]
        private static void Menu() => Build();

        public static void Build()
        {
            if (HasDirtyScene()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var panels = UnityEngine.Object.FindObjectOfType<QuickSelectionPanels>(true);
            var topBar = UnityEngine.Object.FindObjectOfType<UIV2TopBar>(true);
            var home = UnityEngine.Object.FindObjectOfType<HomeScreenV2>(true);
            var nav = UnityEngine.Object.FindObjectOfType<UIV2BottomNav>(true);
            if (panels == null || topBar == null || home == null || nav == null || panels.ModeModal == null || panels.DeckModal == null)
            {
                Debug.LogError($"{Tag} Manca un pezzo della Home (QuickSelectionPanels/TopBar/HomeScreenV2/BottomNav/modali).");
                return;
            }

            BuildOverlay(panels.transform);
            BuildTopBar(topBar);
            BuildHome(home);
            BuildNav(nav);

            var modeModal = panels.ModeModal.gameObject;
            var deckModal = panels.DeckModal.gameObject;
            bool modeWas = modeModal.activeSelf, deckWas = deckModal.activeSelf;
            modeModal.SetActive(true);
            deckModal.SetActive(true);
            BuildModeSheet(panels);
            BuildDeckSheet(panels);
            modeModal.SetActive(modeWas);
            deckModal.SetActive(deckWas);

            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene, ScenePath)) Debug.Log($"{Tag} Scena salvata: {ScenePath}");
            else Debug.LogError($"{Tag} Salvataggio fallito: {ScenePath}");
        }

        // --- Sfondo

        static void BuildOverlay(Transform root)
        {
            var overlay = UI51Build.Stretch(UI51Build.Child(root, "UI51Overlay"));
            UI51Build.Shape(overlay, UI51Shape.Linear(
                (UI51Tokens.Rgba(4, 9, 20, 0.78f), 0f), (UI51Tokens.Rgba(5, 11, 24, 0.25f), 0.16f),
                (UI51Tokens.Rgba(5, 11, 24, 0.05f), 0.34f), (UI51Tokens.Rgba(5, 11, 24, 0.1f), 0.55f),
                (UI51Tokens.Rgba(4, 9, 20, 0.7f), 0.72f), (UI51Tokens.Rgba(3, 7, 16, 0.95f), 1f)),
                180f, Vector4.zero, 0f, Color.clear);
            overlay.SetSiblingIndex(2); // sopra BackgroundLayer e HomeMotes, sotto SafeArea
        }

        // --- Testata

        static void BuildTopBar(UIV2TopBar topBar)
        {
            var c = Container(topBar.transform);
            UI51AccessBuilder.HideChild(topBar.transform, "ProfileGroup");
            UI51AccessBuilder.HideChild(topBar.transform, "ResourceRow");

            // Account
            var account = UI51AccessBuilder.TopBand(UI51Build.Child(c, "Account"), 20f, 20f, 22f, 58f);
            var avatar = UI51Build.Place(UI51Build.Child(account, "Avatar"), new Vector2(0f, 0.5f), new Vector2(58f, 58f), Vector2.zero);
            UI51Build.Solid(avatar, UI51Tokens.Navy, 29f, 2f, UI51Tokens.Gold);
            var inner = UI51Build.Stretch(UI51Build.Child(avatar, "Inner"), 2f, 2f, 2f, 2f);
            UI51Build.Solid(inner, UI51Tokens.Navy, 27f);
            UI51Build.GetOrAdd<Mask>(inner).showMaskGraphic = true;
            UI51Build.Image(UI51Build.Stretch(UI51Build.Child(inner, "Fallback")), UI51Build.Sprite("Avatars", "avatar_1"), Color.white, false, false);
            var portrait = UI51Build.Image(UI51Build.Stretch(UI51Build.Child(inner, "Portrait")), null, Color.white, false, false);
            portrait.enabled = false;
            var badge = UI51Build.Place(UI51Build.Child(avatar, "Level"), new Vector2(1f, 0f), new Vector2(24f, 24f), new Vector2(4f, -4f));
            UI51Build.Shape(badge, UI51Shape.Linear((UI51Tokens.GoldLight, 0f), (UI51Tokens.GoldDark, 1f)), 180f, UI51Tokens.Radii(12f), 2f, UI51Tokens.BadgeRing);
            var level = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(badge, "Value")), "1", FontFace.CinzelBold, 11f,
                UI51Tokens.OnGold, TextAlignmentOptions.Center));

            var info = UI51Build.Stretch(UI51Build.Child(account, "Info"), 70f, 0f, 0f, 0f);
            UI51Build.Column(info, 4f, null, TextAnchor.MiddleLeft, true, true);
            var nameRt = UI51Build.Child(info, "Name");
            UI51Build.Layout(nameRt, -1f, 22f);
            var name = UI51Build.Text(nameRt, "Giocatore", FontFace.CinzelBold, 15f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft);
            var captionRt = UI51Build.Child(info, "Caption");
            UI51Build.Layout(captionRt, -1f, 16f);
            var caption = UI51Build.Text(captionRt, "Livello 1", FontFace.NunitoBold, 11f, UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft);
            var xp = UI51Build.Child(info, "Xp");
            UI51Build.Layout(xp, -1f, 12f);
            var track = UI51Build.Place(UI51Build.Child(xp, "Track"), new Vector2(0f, 0.5f), new Vector2(112f, 6f), Vector2.zero);
            UI51Build.Solid(track, UI51Tokens.WhiteA(0.15f), 3f);
            var fill = UI51Build.Child(track, "Fill");
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0.5f, 1f); fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            UI51Build.Shape(fill, UI51Shape.Linear((UI51Tokens.GoldDark, 0f), (UI51Tokens.GoldLight, 1f)), 90f, UI51Tokens.Radii(3f), 0f, Color.clear);
            var xpLabelRt = UI51Build.Place(UI51Build.Child(xp, "Label"), new Vector2(0f, 0.5f), new Vector2(90f, 12f), new Vector2(119f, 0f));
            var xpLabel = UI51AccessBuilder.NoWrap(UI51Build.Text(xpLabelRt, "0/100 XP", FontFace.NunitoBold, 10f, UI51Tokens.CreamA(0.75f), TextAlignmentOptions.MidlineLeft));
            var xpBar = UI51Build.GetOrAdd<UIV2ProgressBar>(xp);
            UI51Build.Wire(xpBar, so =>
            {
                UI51Build.Ref(so, "fillRect", fill);
                UI51Build.Ref(so, "valueLabel", xpLabel);
                so.FindProperty("labelFormat").stringValue = "{0}/{1} XP";
            });

            // Ospite
            var guest = UI51AccessBuilder.TopBand(UI51Build.Child(c, "Guest"), 20f, 20f, 22f, 58f);
            var gAvatar = UI51Build.Place(UI51Build.Child(guest, "Avatar"), new Vector2(0f, 0.5f), new Vector2(58f, 58f), Vector2.zero);
            UI51Build.Solid(gAvatar, UI51Tokens.WithAlpha(UI51Tokens.Navy, 0.6f), 29f, 1.5f, UI51Tokens.GoldA(0.55f));
            UI51Build.Image(UI51Build.Center(UI51Build.Child(gAvatar, "Icon"), 24f, 28f), UI51Build.Sprite("Common", "ic_person_cream"), UI51Tokens.WhiteA(0.75f));
            var gInfo = UI51Build.Stretch(UI51Build.Child(guest, "Info"), 70f, 0f, 0f, 0f);
            UI51Build.Column(gInfo, 6f, null, TextAnchor.MiddleLeft, true, true);
            var chip = UI51Build.Child(gInfo, "Chip");
            UI51Build.Layout(chip, -1f, 20f);
            UI51Build.Row(chip, 0f, UI51Build.Pad(0, 9, 0, 9), TextAnchor.MiddleCenter, true, true);
            UI51Build.Solid(chip, Color.clear, 10f, 1f, UI51Tokens.CreamA(0.35f));
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(chip, "Label"), "OSPITE \u00b7 PROGRESSI NON SALVATI", FontFace.NunitoExtraBold, 9f,
                UI51Tokens.CreamA(0.85f), TextAlignmentOptions.Center, 1f));
            var register = UI51Build.Child(gInfo, "Register");
            UI51Build.Layout(register, -1f, 34f);
            UI51Build.Row(register, 0f, UI51Build.Pad(0, 14, 0, 14), TextAnchor.MiddleCenter, true, true);
            var regShape = UI51Build.Solid(register, UI51Tokens.WithAlpha(UI51Tokens.Navy, 0.6f), 17f, 1f, UI51Tokens.Gold, true);
            var registerButton = UI51Build.Button(regShape, regShape);
            UI51Build.GetOrAdd<UI51Press>(register);
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(register, "Label"), "Registrati", FontFace.NunitoExtraBold, 12f, UI51Tokens.Gold, TextAlignmentOptions.Center));
            guest.gameObject.SetActive(false);

            UI51Build.Wire(topBar, so =>
            {
                UI51Build.Ref(so, "profileAvatar", null);
                UI51Build.Ref(so, "resourcesContainer", null);
                UI51Build.Ref(so, "energyBar", null);
                UI51Build.Ref(so, "portraitImage", portrait);
                UI51Build.Ref(so, "nameLabel", name);
                UI51Build.Ref(so, "xpBar", xpBar);
                UI51Build.Ref(so, "levelLabel", level);
                UI51Build.Ref(so, "levelCaption", caption);
                UI51Build.Ref(so, "accountGroup", account.gameObject);
                UI51Build.Ref(so, "guestGroup", guest.gameObject);
                UI51Build.Ref(so, "registerButton", registerButton);
            });
        }

        // --- Home: pulsanti laterali, tile, GIOCA

        static void BuildHome(HomeScreenV2 home)
        {
            var c = Container(home.transform);
            UI51AccessBuilder.HideChild(home.transform, "QuickActionsColumn");
            UI51AccessBuilder.HideChild(home.transform, "BottomControlsGroup");
            // L'Image resta (bersaglio dello swipe tra pagine), solo trasparente.
            var bg = home.GetComponent<Image>();
            if (bg != null) bg.color = Color.clear;

            float top = 118f - TopBarH;
            var mail = QuickAction(c, "Mail", "ic_mail", 22f, "Posta", 14f, top);
            // Colonna destra impilata (come il flex del mockup): l'ospite vede Opzioni in cima.
            var right = UI51Build.Place(UI51Build.Child(c, "Right"), new Vector2(1f, 1f), new Vector2(56f, 214f), new Vector2(-14f, -top));
            UI51Build.Column(right, 14f, null, TextAnchor.UpperCenter, false, false);
            var rewards = QuickAction(right, "Rewards", "ic_chest_cream", 24f, "Premi", 0f, 0f);
            var ranking = QuickAction(right, "Ranking", "ic_trophy_cream", 22f, "Classifica", 0f, 0f);
            var settings = QuickAction(right, "Settings", "ic_settings_cream", 21f, "Opzioni", 0f, 0f);
            foreach (var old in new[] { "Rewards", "Ranking", "Settings" }) UI51AccessBuilder.HideChild(c, old);

            // Tile Modalita' / Mazzo
            var mode = Tile(c, "Mode", 20f);
            var modeBadge = UI51Build.Child(mode.transform, "Badge");
            UI51Build.Size(modeBadge, 38f, 38f);
            UI51Build.Layout(modeBadge, 38f, 38f);
            UI51Build.Solid(modeBadge, UI51Tokens.GoldA(0.12f), 19f, 1f, UI51Tokens.GoldA(0.4f));
            var modeBadgeText = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(modeBadge, "Value")), "1v1",
                FontFace.CinzelBold, 10f, UI51Tokens.Gold, TextAlignmentOptions.Center));
            var modeTexts = TileTexts(mode.transform, "ONLINE", "1 vs 1");
            TileChevron(mode.transform);

            var deck = Tile(c, "Deck", 201f);
            var thumb = UI51Build.Child(deck.transform, "Thumb");
            UI51Build.Size(thumb, 30f, 44f);
            UI51Build.Layout(thumb, 30f, 44f);
            UI51Build.Solid(thumb, UI51Tokens.Navy, 3f, 0f, default, false, new UI51Shadow(0f, 2f, 6f, UI51Tokens.BlackA(0.5f)));
            var thumbArt = UI51Build.Image(UI51Build.Stretch(UI51Build.Child(thumb, "Art")), UI51Build.Sprite("Cards", "back_tradizionale"), Color.white, false, false);
            var deckTexts = TileTexts(deck.transform, "MAZZO", "Tradizionale");
            TileChevron(deck.transform);

            WireChip(mode, null, modeTexts[0], modeTexts[1], modeBadgeText);
            WireChip(deck, thumbArt, deckTexts[0], deckTexts[1], null);

            // GIOCA
            var play = UI51Build.Place(UI51Build.Child(c, "Play"), new Vector2(0.5f, 0f), new Vector2(350f, 60f), new Vector2(0f, 24f));
            var playShape = UI51Build.Shape(play, UI51Tokens.GoldButtonFill(), 180f, UI51Tokens.Radii(18f), 1f, UI51Tokens.GoldButtonBorder, true,
                new UI51Shadow(0f, 10f, 24f, UI51Tokens.BlackA(0.45f)), new UI51Shadow(0f, 0f, 24f, UI51Tokens.GoldA(0.25f)));
            var playButton = UI51Build.Button(playShape, playShape);
            var playLabel = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(play, "Label")), "GIOCA", FontFace.CinzelBold, 18f,
                UI51Tokens.OnGold, TextAlignmentOptions.Center, 4f));
            var playV2 = UI51Build.GetOrAdd<UIV2Button>(play);
            UI51Build.Wire(playV2, so =>
            {
                UI51Build.Ref(so, "theme", null);
                UI51Build.Ref(so, "background", null);
                UI51Build.Ref(so, "button", playButton);
                UI51Build.Ref(so, "label", playLabel);
                UI51Build.Bool(so, "keepLabelColor", true);
            });

            UI51Build.Wire(home, so =>
            {
                UI51Build.Ref(so, "mailButton", mail);
                UI51Build.Ref(so, "rewardsButton", rewards);
                UI51Build.Ref(so, "rankingButton", ranking);
                UI51Build.Ref(so, "settingsButton", settings);
                UI51Build.Ref(so, "modeSelector", mode);
                UI51Build.Ref(so, "deckSelector", deck);
                UI51Build.Ref(so, "playButton", playV2);
            });
        }

        /// <summary>Cerchio 44 con icona + etichetta sotto (larghezza 56), pallino rosso spento.</summary>
        static UIV2QuickActionButton QuickAction(RectTransform parent, string name, string icon, float iconSize, string label, float x, float y)
        {
            var rt = UI51Build.Place(UI51Build.Child(parent, name), new Vector2(0f, 1f), new Vector2(56f, 62f), new Vector2(x, -y));
            var circle = UI51Build.Place(UI51Build.Child(rt, "Circle"), new Vector2(0.5f, 1f), new Vector2(44f, 44f), Vector2.zero);
            var shape = UI51Build.Solid(circle, UI51Tokens.Rgba(11, 29, 58, 0.6f), 22f, 1f, UI51Tokens.GoldA(0.45f), true);
            var button = UI51Build.Button(rt, shape);
            var img = UI51Build.Image(UI51Build.Center(UI51Build.Child(circle, "Icon"), iconSize, iconSize), UI51Build.Sprite("Common", icon), Color.white);
            var dot = UI51Build.Place(UI51Build.Child(circle, "Badge"), new Vector2(1f, 1f), new Vector2(9f, 9f), new Vector2(-2f, -2f));
            UI51Build.Solid(dot, UI51Tokens.Danger, 4.5f, 1.5f, UI51Tokens.BadgeRing);
            dot.gameObject.SetActive(false);
            var text = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Place(UI51Build.Child(rt, "Label"), new Vector2(0.5f, 0f), new Vector2(70f, 13f), Vector2.zero),
                label, FontFace.NunitoBold, 10f, UI51Tokens.Cream, TextAlignmentOptions.Center));
            var qa = UI51Build.GetOrAdd<UIV2QuickActionButton>(rt);
            UI51Build.Wire(qa, so =>
            {
                UI51Build.Ref(so, "button", button);
                UI51Build.Ref(so, "icon", img);
                UI51Build.Ref(so, "label", text);
                UI51Build.Ref(so, "badgeRoot", dot.gameObject);
                UI51Build.Ref(so, "badgeLabel", null);
            });
            return qa;
        }

        static UIV2SelectorChip Tile(RectTransform parent, string name, float x)
        {
            var rt = UI51Build.Place(UI51Build.Child(parent, name), Vector2.zero, new Vector2(169f, 64f), new Vector2(x, 96f));
            var shape = UI51Build.Shape(rt, UI51Shape.Linear((UI51Tokens.Rgba(10, 22, 44, 0.85f), 0f), (UI51Tokens.Rgba(6, 13, 27, 0.92f), 1f)),
                180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.GoldA(0.35f), true);
            UI51Build.Button(shape, shape);
            UI51Build.GetOrAdd<UI51Press>(rt);
            UI51Build.Row(rt, 10f, UI51Build.Pad(12, 12, 12, 12), TextAnchor.MiddleLeft, true, false);
            return UI51Build.GetOrAdd<UIV2SelectorChip>(rt);
        }

        static TextMeshProUGUI[] TileTexts(Transform tile, string caption, string value)
        {
            var texts = UI51Build.Child(tile, "Texts");
            UI51Build.Size(texts, 0f, 38f);
            UI51Build.Layout(texts, -1f, -1f, 1f);
            UI51Build.Column(texts, 3f, null, TextAnchor.MiddleLeft, true, true);
            var cRt = UI51Build.Child(texts, "Caption");
            UI51Build.Layout(cRt, -1f, 14f);
            var c = UI51Build.Text(cRt, caption, FontFace.CinzelSemiBold, 9f, UI51Tokens.GoldA(0.85f), TextAlignmentOptions.MidlineLeft, 1.5f);
            c.enableAutoSizing = true; c.fontSizeMin = 7f; c.fontSizeMax = 9f; // "ALLENAMENTO" non entra in 77 a 9pt
            var vRt = UI51Build.Child(texts, "Value");
            UI51Build.Layout(vRt, -1f, 21f);
            var v = UI51Build.Text(vRt, value, FontFace.NunitoBold, 14f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft);
            return new[] { c, v };
        }

        static void TileChevron(Transform tile)
        {
            var rt = UI51Build.Child(tile, "Chevron");
            UI51Build.Size(rt, 10f, 10f);
            UI51Build.Layout(rt, 10f, 10f);
            UI51Build.Image(rt, UI51Build.Sprite("Common", "ic_nav_back_cream"), UI51Tokens.WhiteA(0.6f));
            rt.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }

        static void WireChip(UIV2SelectorChip chip, Image icon, TextMeshProUGUI small, TextMeshProUGUI value, TextMeshProUGUI badge)
        {
            UI51Build.Wire(chip, so =>
            {
                UI51Build.Ref(so, "icon", icon);
                UI51Build.Ref(so, "smallLabel", small);
                UI51Build.Ref(so, "valueLabel", value);
                UI51Build.Ref(so, "button", chip.GetComponent<Button>());
                UI51Build.Ref(so, "badgeLabel", badge);
            });
        }

        // --- Barra in basso

        static void BuildNav(UIV2BottomNav nav)
        {
            var c = Container(nav.transform);
            foreach (var old in new[] { "SafeAreaFill", "GiocaSlot", "CardsSlot", "ShopSlot", "ProfileSlot" })
                UI51AccessBuilder.HideChild(nav.transform, old);
            var navImage = nav.GetComponent<Image>();
            if (navImage != null) navImage.enabled = false;

            // Il fondo scende di 80 oltre la barra: copre la fascia dell'indicatore home.
            var bar = UI51Build.Stretch(UI51Build.Child(c, "Bar"), 0f, -80f, 0f, 0f);
            UI51Build.Image(bar, null, UI51Tokens.NavBar, true, false);
            UI51Build.Image(UI51AccessBuilder.TopBand(UI51Build.Child(bar, "Line"), 0f, 0f, 0f, 1f), null, UI51Tokens.GoldA(0.3f), false, false);

            string[] icons = { "ic_home_cream", "ic_cards_cream", "ic_chest_cream", "ic_person_cream" };
            string[] labels = { "Gioca", "Collezione", "Negozio", "Profilo" };
            const float w = (390f - 24f) / 4f;
            var so = new SerializedObject(nav);
            so.Update();
            var slots = so.FindProperty("slots");
            slots.arraySize = icons.Length;
            for (int i = 0; i < icons.Length; i++)
            {
                var item = UI51Build.Child(c, "Item" + i);
                item.anchorMin = new Vector2((12f + i * w) / 390f, 0f);
                item.anchorMax = new Vector2((12f + (i + 1) * w) / 390f, 1f);
                item.pivot = new Vector2(0.5f, 0.5f);
                item.offsetMin = item.offsetMax = Vector2.zero;
                var hit = UI51Build.Image(item, null, Color.clear, true, false);
                var button = UI51Build.Button(hit, hit);
                var icon = UI51Build.Image(UI51Build.Place(UI51Build.Child(item, "Icon"), new Vector2(0.5f, 0.5f), new Vector2(22f, 22f), new Vector2(0f, 9.5f)),
                    UI51Build.Sprite("Common", icons[i]), Color.white);
                var label = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Place(UI51Build.Child(item, "Label"), new Vector2(0.5f, 0.5f), new Vector2(w, 14f), new Vector2(0f, -13.5f)),
                    labels[i], FontFace.NunitoBold, 11f, UI51Tokens.CreamA(0.55f), TextAlignmentOptions.Center));
                var dash = UI51Build.Place(UI51Build.Child(item, "Dash"), new Vector2(0.5f, 1f), new Vector2(24f, 2f), Vector2.zero);
                UI51Build.Image(dash, null, UI51Tokens.Gold, false, false);
                dash.gameObject.SetActive(i == 0);

                var slot = slots.GetArrayElementAtIndex(i);
                slot.FindPropertyRelative("Button").objectReferenceValue = button;
                slot.FindPropertyRelative("Icon").objectReferenceValue = icon;
                slot.FindPropertyRelative("IconLayoutElement").objectReferenceValue = null;
                slot.FindPropertyRelative("Label").objectReferenceValue = label;
                slot.FindPropertyRelative("SelectedIndicator").objectReferenceValue = dash.gameObject;
            }
            so.FindProperty("normalLabelColor").colorValue = UI51Tokens.CreamA(0.55f);
            so.FindProperty("selectedLabelColor").colorValue = UI51Tokens.Gold;
            so.FindProperty("normalLabelMaterial").objectReferenceValue = null;
            so.FindProperty("selectedLabelMaterial").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- Foglio Modalita'

        static void BuildModeSheet(QuickSelectionPanels panels)
        {
            var modal = panels.ModeModal.transform;
            foreach (var old in new[] { "PanelFrame", "ModeRowGroup", "DifficultyPillGroup" }) UI51AccessBuilder.HideChild(modal, old);
            var sheet = SheetFrame(modal, "Modalit\u00e0 di gioco", "Scegli come vuoi giocare", out var close);

            // Schede
            var tabsRt = UI51Build.Child(sheet, "Tabs");
            UI51Build.Layout(tabsRt, -1f, 44f);
            UI51Build.Solid(tabsRt, UI51Tokens.WhiteA(0.04f), 12f, 1f, UI51Tokens.GoldA(0.18f));
            var tabsRow = UI51Build.Row(tabsRt, 4f, UI51Build.Pad(4, 4, 4, 4), TextAnchor.MiddleCenter, true, true);
            tabsRow.childForceExpandWidth = tabsRow.childForceExpandHeight = true;
            string[] tabNames = { "Online", "Allenamento", "Stanza privata" };
            var tabs = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                var tab = UI51Build.Child(tabsRt, "Tab" + i);
                UI51Build.Layout(tab, 0f, -1f, 1f);
                tabs[i] = Hit(tab);
                var on = UI51Build.Stretch(UI51Build.Child(tab, "On"));
                UI51Build.Solid(on, UI51Tokens.GoldA(0.16f), 9f, 1f, UI51Tokens.GoldA(0.7f));
                UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(on, "Label")), tabNames[i], FontFace.NunitoExtraBold, 12f, UI51Tokens.Gold, TextAlignmentOptions.Center));
                var off = UI51Build.Stretch(UI51Build.Child(tab, "Off"));
                UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(off, "Label")), tabNames[i], FontFace.NunitoBold, 12f, UI51Tokens.CreamA(0.65f), TextAlignmentOptions.Center));
                Toggle(tab, on.gameObject, off.gameObject, null, null).SetSelected(i == 0);
            }

            // Scroll con le tre pagine
            var scrollRt = UI51Build.Child(sheet, "Scroll");
            UI51Build.Layout(scrollRt, -1f, 262f);
            var sr = UI51Build.GetOrAdd<ScrollRect>(scrollRt);
            sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Elastic; sr.scrollSensitivity = 30f;
            var viewport = UI51Build.Stretch(UI51Build.Child(scrollRt, "Viewport"));
            UI51Build.Image(viewport, null, Color.clear, true, false);
            UI51Build.GetOrAdd<RectMask2D>(viewport);
            var content = UI51Build.Child(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            UI51Build.Column(content, 0f, null, TextAnchor.UpperLeft, true, true).childForceExpandWidth = true;
            UI51Build.Fit(content, false, true);
            sr.viewport = viewport; sr.content = content;

            var pages = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                pages[i] = UI51Build.Child(content, "Page" + i);
                UI51Build.Column(pages[i], 8f, null, TextAnchor.UpperLeft, true, true).childForceExpandWidth = true;
            }

            // Online
            SectionHeader(pages[0], "Header", "FORMATO");
            var modeButtons = new Button[6];
            modeButtons[0] = ModeRow(pages[0], "Row0", "1v1", "1 vs 1", "Classica, testa a testa");
            modeButtons[1] = ModeRow(pages[0], "Row1", "2v2", "2 vs 2", "In coppia con un compagno");
            modeButtons[2] = ModeRow(pages[0], "Row2", "1v3", "1 vs 3", "Tutti contro tutti");

            // Allenamento
            SectionHeader(pages[1], "Header", "FORMATO");
            var formats = CellRow(pages[1], "Formats", 56f, 8f);
            string[] formatNames = { "1 vs 1", "2 vs 2", "1 vs 3" };
            for (int i = 0; i < 3; i++) modeButtons[3 + i] = Cell(formats, "Cell" + i, formatNames[i], 0);
            SectionHeader(pages[1], "LevelHeader", "DIFFICOLT\u00c0 DEL BOT");
            var levels = CellRow(pages[1], "Levels", 60f, 7f);
            string[] levelNames = { "Facile", "Medio", "Difficile" };
            var difficulty = new Button[3];
            for (int i = 0; i < 3; i++) difficulty[i] = Cell(levels, "Cell" + i, levelNames[i], i + 1);
            var infoRt = UI51Build.Child(pages[1], "Info");
            UI51Build.Layout(infoRt, -1f, 52f);
            UI51Build.Solid(infoRt, UI51Tokens.WhiteA(0.03f), 12f, 1f, UI51Tokens.GoldA(0.12f));
            UI51Build.Row(infoRt, 10f, UI51Build.Pad(10, 12, 10, 12), TextAnchor.MiddleLeft, true, false);
            var bot = UI51Build.Child(infoRt, "Bot");
            UI51Build.Size(bot, 22f, 22f);
            UI51Build.Layout(bot, 22f, 22f);
            UI51Build.Image(bot, UI51Build.Sprite("Common", "ic_bot_cream"), Color.white);
            var infoTextRt = UI51Build.Child(infoRt, "Text");
            UI51Build.Size(infoTextRt, 0f, 32f);
            UI51Build.Layout(infoTextRt, -1f, -1f, 1f);
            var info = UI51Build.Text(infoTextRt, "Il bot gioca in modo equilibrato, senza strategie avanzate.", FontFace.NunitoRegular, 12f,
                UI51Tokens.CreamA(0.7f), TextAlignmentOptions.MidlineLeft);
            info.enableWordWrapping = true;

            // Stanza privata
            var card = UI51Build.Child(pages[2], "Create");
            UI51Build.Solid(card, UI51Tokens.WhiteA(0.03f), 14f, 1f, UI51Tokens.GoldA(0.18f));
            UI51Build.Column(card, 12f, UI51Build.Pad(14, 14, 14, 14), TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;
            var intro = UI51Build.Child(card, "Intro");
            UI51Build.Layout(intro, -1f, 40f);
            var circle = UI51Build.Place(UI51Build.Child(intro, "Circle"), new Vector2(0f, 0.5f), new Vector2(34f, 34f), Vector2.zero);
            UI51Build.Solid(circle, UI51Tokens.GoldA(0.12f), 17f, 1f, UI51Tokens.GoldA(0.4f));
            UI51Build.Image(UI51Build.Center(UI51Build.Child(circle, "Glyph"), 17f, 17f), UI51Build.Sprite("Common", "ic_link_cream"), Color.white);
            var introTexts = UI51Build.Stretch(UI51Build.Child(intro, "Texts"), 46f, 0f, 0f, 0f);
            UI51Build.Column(introTexts, 1f, null, TextAnchor.MiddleLeft, true, true);
            var t1 = UI51Build.Child(introTexts, "Title");
            UI51Build.Layout(t1, -1f, 21f);
            UI51Build.Text(t1, "Gioca con gli amici", FontFace.NunitoBold, 14f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft);
            var t2 = UI51Build.Child(introTexts, "Desc");
            UI51Build.Layout(t2, -1f, 17f);
            UI51Build.Text(t2, "Crea una stanza e condividi il codice", FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.MidlineLeft);
            var create = UI51AccessBuilder.GoldButton(card, "CreateRoom", "CREA STANZA", 46f, 13f);
            // QuickSelectionPanels.ModeOption legge l'icona della tile da un figlio "Icon" della riga scelta.
            var linkIcon = UI51Build.Image(UI51Build.Center(UI51Build.Child(create.transform, "Icon"), 17f, 17f), UI51Build.Sprite("Common", "ic_link_cream"), Color.white);
            linkIcon.enabled = false;
            var orRow = UI51Build.Child(card, "Or");
            UI51Build.Row(orRow, 10f, null, TextAnchor.MiddleCenter, true, false);
            UI51Build.Layout(orRow, -1f, 14f);
            UI51AccessBuilder.OrLine(orRow, "LineL");
            var orText = UI51Build.Child(orRow, "Label");
            UI51Build.Size(orText, 0f, 14f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(orText, "OPPURE", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.55f), TextAlignmentOptions.Center, 1f));
            UI51AccessBuilder.OrLine(orRow, "LineR");
            var joinRt = UI51Build.Child(card, "JoinRoom");
            UI51PrefabBuilder.ButtonBody(joinRt.gameObject, 346f, 46f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(13f), 1f, UI51Tokens.Gold,
                FontFace.CinzelBold, 13f, 2f, UI51Tokens.Gold, "ENTRA CON CODICE");
            UI51Build.Layout(joinRt, -1f, 46f);
            var join = joinRt.GetComponent<Button>();

            for (int i = 0; i < 3; i++) pages[i].gameObject.SetActive(i == 0);

            var confirm = UI51AccessBuilder.GoldButton(sheet, "Confirm", "CONFERMA", 52f, 14f);

            WireModal(panels.ModeModal, sheet, close);
            UI51Build.Wire(panels, so =>
            {
                UI51AccessBuilder.SetArray(so, "ModeButtons", modeButtons);
                UI51AccessBuilder.SetArray(so, "DifficultyButtons", difficulty);
                UI51Build.Ref(so, "CreateRoom", create);
                UI51Build.Ref(so, "JoinRoom", join);
                UI51Build.Ref(so, "ModeScroll", sr);
                UI51AccessBuilder.SetArray(so, "Tabs", tabs);
                UI51AccessBuilder.SetArray(so, "TabPages", pages[0].gameObject, pages[1].gameObject, pages[2].gameObject);
                UI51Build.Ref(so, "ModeConfirm", confirm);
                UI51Build.Ref(so, "DifficultyInfo", info);
            });
        }

        static void SectionHeader(RectTransform page, string name, string text)
        {
            var rt = UI51Build.Child(page, name);
            UI51Build.Layout(rt, -1f, 16f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(rt, text, FontFace.CinzelBold, 10f, UI51Tokens.Gold, TextAlignmentOptions.BottomLeft, 2f));
        }

        /// <summary>Riga formato online: tondo con la sigla, titolo+descrizione, spunta (scelta) o anello (libera).</summary>
        static Button ModeRow(RectTransform page, string name, string badgeText, string label, string desc)
        {
            var row = UI51Build.Child(page, name);
            UI51Build.Layout(row, -1f, 64f);
            var button = Hit(row);
            var on = UI51Build.Stretch(UI51Build.Child(row, "On"));
            UI51Build.Solid(on, UI51Tokens.GoldA(0.1f), 14f, 1f, UI51Tokens.GoldA(0.75f));
            var check = UI51Build.Place(UI51Build.Child(on, "Check"), new Vector2(1f, 0.5f), new Vector2(22f, 22f), new Vector2(-14f, 0f));
            UI51Build.Solid(check, UI51Tokens.Gold, 11f);
            CheckMark(check, 12f, 3.2f);
            var off = UI51Build.Stretch(UI51Build.Child(row, "Off"));
            UI51Build.Solid(off, UI51Tokens.WhiteA(0.03f), 14f, 1f, UI51Tokens.GoldA(0.15f));
            var ring = UI51Build.Place(UI51Build.Child(off, "Ring"), new Vector2(1f, 0.5f), new Vector2(22f, 22f), new Vector2(-14f, 0f));
            UI51Build.Solid(ring, Color.clear, 11f, 1.5f, UI51Tokens.GoldA(0.35f));

            var badge = UI51Build.Place(UI51Build.Child(row, "Badge"), new Vector2(0f, 0.5f), new Vector2(40f, 40f), new Vector2(14f, 0f));
            UI51Build.Solid(badge, UI51Tokens.GoldA(0.12f), 20f, 1f, UI51Tokens.GoldA(0.4f));
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(badge, "Value")), badgeText, FontFace.CinzelBold, 11f, UI51Tokens.Gold, TextAlignmentOptions.Center));
            var texts = UI51Build.Stretch(UI51Build.Child(row, "Texts"), 66f, 0f, 50f, 0f);
            UI51Build.Column(texts, 2f, null, TextAnchor.MiddleLeft, true, true);
            var l = UI51Build.Child(texts, "Label");
            UI51Build.Layout(l, -1f, 21f);
            UI51Build.Text(l, label, FontFace.NunitoBold, 14f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft);
            var d = UI51Build.Child(texts, "Desc");
            UI51Build.Layout(d, -1f, 17f);
            UI51Build.Text(d, desc, FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.MidlineLeft);

            Toggle(row, on.gameObject, off.gameObject, null, null).SetSelected(name == "Row0");
            return button;
        }

        static RectTransform CellRow(RectTransform page, string name, float height, float spacing)
        {
            var rt = UI51Build.Child(page, name);
            UI51Build.Layout(rt, -1f, height);
            var g = UI51Build.Row(rt, spacing, null, TextAnchor.MiddleCenter, true, true);
            g.childForceExpandWidth = g.childForceExpandHeight = true;
            return rt;
        }

        /// <summary>Cella a scelta singola (formato bot o difficolta'); dots > 0 aggiunge i pallini del livello.</summary>
        static Button Cell(RectTransform row, string name, string label, int dots)
        {
            var cell = UI51Build.Child(row, name);
            UI51Build.Layout(cell, 0f, -1f, 1f);
            var button = Hit(cell);
            var on = UI51Build.Stretch(UI51Build.Child(cell, "On"));
            UI51Build.Solid(on, UI51Tokens.GoldA(0.1f), 12f, 1f, UI51Tokens.GoldA(0.75f));
            var off = UI51Build.Stretch(UI51Build.Child(cell, "Off"));
            UI51Build.Solid(off, UI51Tokens.WhiteA(0.03f), 12f, 1f, UI51Tokens.GoldA(0.15f));
            float y = dots > 0 ? 7f : 0f;
            var onLabel = UI51Build.Place(UI51Build.Child(on, "Label"), new Vector2(0.5f, 0.5f), new Vector2(100f, 18f), new Vector2(0f, y));
            UI51AccessBuilder.NoWrap(UI51Build.Text(onLabel, label, FontFace.NunitoExtraBold, 13f, UI51Tokens.Gold, TextAlignmentOptions.Center));
            var offLabel = UI51Build.Place(UI51Build.Child(off, "Label"), new Vector2(0.5f, 0.5f), new Vector2(100f, 18f), new Vector2(0f, y));
            UI51AccessBuilder.NoWrap(UI51Build.Text(offLabel, label, FontFace.NunitoBold, 13f, UI51Tokens.Cream, TextAlignmentOptions.Center));
            if (dots > 0)
            {
                var row2 = UI51Build.Place(UI51Build.Child(cell, "Dots"), new Vector2(0.5f, 0.5f), new Vector2(dots * 6f + (dots - 1) * 4f, 6f), new Vector2(0f, -11f));
                for (int i = 0; i < dots; i++)
                    UI51Build.Solid(UI51Build.Place(UI51Build.Child(row2, "Dot" + i), new Vector2(0f, 0.5f), new Vector2(6f, 6f), new Vector2(i * 10f, 0f)), UI51Tokens.Gold, 3f);
            }
            Toggle(cell, on.gameObject, off.gameObject, null, null).SetSelected(false);
            return button;
        }

        // --- Foglio Mazzo

        static void BuildDeckSheet(QuickSelectionPanels panels)
        {
            var modal = panels.DeckModal.transform;
            foreach (var old in new[] { "PanelFrame", "DeckSelectionGroup" }) UI51AccessBuilder.HideChild(modal, old);
            var catalog = CardDecks.Catalog;
            int count = catalog != null ? catalog.Entries.Count : 0;
            if (catalog == null) Debug.LogWarning($"{Tag} CardDeckCatalog non trovato: foglio Mazzo senza carte.");
            var sheet = SheetFrame(modal, "Scegli il mazzo", count + " sbloccati \u00b7 sali di livello per ottenerne altri", out var close);

            const float cardW = 80f, cardH = cardW * 520f / 353f;
            var grid = UI51Build.Child(sheet, "Grid");
            var g = UI51Build.GetOrAdd<GridLayoutGroup>(grid);
            g.cellSize = new Vector2(cardW, cardH + 22f);
            g.spacing = new Vector2(10f, 14f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 4;
            g.childAlignment = TextAnchor.UpperCenter;
            g.padding = UI51Build.Pad(4, 0, 4, 0);

            var buttons = new Button[count];
            for (int i = 0; i < count; i++)
            {
                var entry = catalog.Entries[i];
                var cell = UI51Build.Child(grid, "Deck" + i);
                buttons[i] = Hit(cell);
                var glow = UI51Build.Place(UI51Build.Child(cell, "Glow"), new Vector2(0.5f, 1f), new Vector2(cardW + 4f, cardH + 4f), new Vector2(0f, 2f));
                UI51Build.Solid(glow, UI51Tokens.GoldA(0.35f), 10f, 2f, UI51Tokens.Gold, false, new UI51Shadow(0f, 0f, 14f, UI51Tokens.GoldA(0.35f)));
                var shadow = UI51Build.Place(UI51Build.Child(cell, "Shadow"), new Vector2(0.5f, 1f), new Vector2(cardW, cardH), Vector2.zero);
                UI51Build.Solid(shadow, UI51Tokens.Navy, 8f, 0f, default, false, new UI51Shadow(0f, 4f, 10f, UI51Tokens.BlackA(0.45f)));
                var face = UI51Build.Place(UI51Build.Child(cell, "Face"), new Vector2(0.5f, 1f), new Vector2(cardW, cardH), Vector2.zero);
                UI51Build.Solid(face, Color.white, 8f);
                UI51Build.GetOrAdd<Mask>(face).showMaskGraphic = false;
                UI51Build.Image(UI51Build.Stretch(UI51Build.Child(face, "Art")), entry.Artwork, Color.white, false, false);
                var check = UI51Build.Place(UI51Build.Child(cell, "Check"), new Vector2(1f, 1f), new Vector2(20f, 20f), new Vector2(5f, 5f));
                UI51Build.Solid(check, UI51Tokens.Gold, 10f, 2f, UI51Tokens.BadgeRing);
                CheckMark(check, 10f, 3.6f);
                var on = UI51Build.Place(UI51Build.Child(cell, "LabelOn"), new Vector2(0.5f, 0f), new Vector2(cardW + 10f, 16f), Vector2.zero);
                UI51Build.Text(on, entry.DisplayName, FontFace.NunitoExtraBold, 11f, UI51Tokens.Gold, TextAlignmentOptions.Center);
                var off = UI51Build.Place(UI51Build.Child(cell, "LabelOff"), new Vector2(0.5f, 0f), new Vector2(cardW + 10f, 16f), Vector2.zero);
                UI51Build.Text(off, entry.DisplayName, FontFace.NunitoBold, 11f, UI51Tokens.Cream, TextAlignmentOptions.Center);
                Toggle(cell, on.gameObject, off.gameObject, check.gameObject, glow.gameObject).SetSelected(entry.Id == CardDecks.SelectedId);
            }
            for (int i = count; grid.Find("Deck" + i) != null; i++) grid.Find("Deck" + i).gameObject.SetActive(false);

            var captionRt = UI51Build.Child(sheet, "Caption");
            UI51Build.Layout(captionRt, -1f, 18f);
            var caption = UI51Build.Text(captionRt, "", FontFace.NunitoBold, 12f, UI51Tokens.CreamA(0.75f), TextAlignmentOptions.Center);
            var confirm = UI51AccessBuilder.GoldButton(sheet, "Confirm", "USA QUESTO MAZZO", 52f, 14f);

            WireModal(panels.DeckModal, sheet, close);
            UI51Build.Wire(panels, so =>
            {
                UI51AccessBuilder.SetArray(so, "DeckButtons", buttons);
                UI51AccessBuilder.SetArray(so, "Fan");
                UI51Build.Ref(so, "Caption", caption);
                UI51Build.Ref(so, "Confirm", confirm);
            });
        }

        // --- Mattoni comuni

        /// <summary>Contenitore 390 di larghezza scalato a tutta larghezza dell'host, agganciato in alto, fuori dai layout group.</summary>
        internal static RectTransform Container(Transform host, string name = "UI51")
        {
            var rt = UI51Build.Child(host, name);
            rt.anchorMin = new Vector2(0.5f - 0.5f / S, 1f - 1f / S);
            rt.anchorMax = new Vector2(0.5f + 0.5f / S, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            UI51Build.Layout(rt, -1f, -1f, -1f, -1f, true);
            rt.localScale = Vector3.one * S;
            return rt;
        }

        /// <summary>UI51 -> Safe (DesignCanvasFit 390x844) -> Sheet con maniglia e intestazione (titolo, sottotitolo, chiudi).</summary>
        internal static RectTransform SheetFrame(Transform modal, string title, string subtitle, out Button close)
        {
            var root = UI51Build.Stretch(UI51Build.Child(modal, "UI51"));
            var safe = UI51Build.Child(root, "Safe");
            var fit = UI51Build.GetOrAdd<DesignCanvasFit>(safe);
            fit.Reference = UI51Tokens.ReferenceResolution;
            fit.Fill = true;
            safe.anchorMin = safe.anchorMax = safe.pivot = new Vector2(0.5f, 0.5f);
            safe.anchoredPosition = Vector2.zero;
            safe.sizeDelta = UI51Tokens.ReferenceResolution;

            var sheet = UI51AccessBuilder.Sheet(safe, UI51Build.Pad(12, 20, 24, 20), 14f, // opaco: senza il blur del mockup il .97 lascia trasparire GIOCA
                UI51Shape.Linear((UI51Tokens.Rgba(12, 26, 50, 1f), 0f), (UI51Tokens.Rgba(6, 13, 27, 1f), 1f)));
            SheetHeader(sheet, title, subtitle, out close);
            return sheet;
        }

        /// <summary>Maniglia 40x4 e intestazione alta 42 (titolo, sottotitolo, chiudi 36) in cima a un foglio dal basso.</summary>
        internal static void SheetHeader(RectTransform sheet, string title, string subtitle, out Button close)
        {
            var handle = UI51Build.Child(sheet, "Handle");
            UI51Build.Layout(handle, -1f, 4f);
            UI51Build.Solid(UI51Build.Center(UI51Build.Child(handle, "Bar"), 40f, 4f), UI51Tokens.CreamA(0.25f), 2f);

            var header = UI51Build.Child(sheet, "Header");
            UI51Build.Layout(header, -1f, 42f);
            var titles = UI51Build.Stretch(UI51Build.Child(header, "Titles"), 0f, 0f, 48f, 0f);
            UI51Build.Column(titles, 2f, null, TextAnchor.MiddleLeft, true, true);
            var t = UI51Build.Child(titles, "Title");
            UI51Build.Layout(t, -1f, 23f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(t, title, FontFace.CinzelBold, 18f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            var st = UI51Build.Child(titles, "Subtitle");
            UI51Build.Layout(st, -1f, 17f);
            UI51Build.Text(st, subtitle, FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.55f), TextAlignmentOptions.MidlineLeft);

            var closeRt = UI51Build.Place(UI51Build.Child(header, "Close"), new Vector2(1f, 0.5f), new Vector2(36f, 36f), Vector2.zero);
            var shape = UI51Build.Solid(closeRt, UI51Tokens.WhiteA(0.06f), 18f, 1f, UI51Tokens.GoldA(0.35f), true);
            close = UI51Build.Button(shape, shape);
            UI51Build.GetOrAdd<UI51Press>(closeRt);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(closeRt, "Icon"), 14f, 14f), UI51Build.Sprite("Common", "ic_close_cream"), Color.white);
        }

        internal static void WireModal(AnimatedModalV2 modal, RectTransform sheet, Button close)
        {
            var group = UI51Build.GetOrAdd<CanvasGroup>(modal);
            UI51Build.Wire(modal, so =>
            {
                UI51Build.Ref(so, "Group", group);
                UI51Build.Ref(so, "Frame", sheet);
                UI51Build.Ref(so, "CloseButton", close);
                UI51Build.Ref(so, "Dimmer", null);
            });
            var dim = modal.transform.Find("DimBackground");
            if (dim == null) return;
            var img = dim.GetComponent<Image>();
            if (img != null) { img.sprite = null; img.color = UI51Tokens.Scrim; }
            var dismiss = dim.GetComponent<DismissOnBackdrop>();
            if (dismiss != null) UI51Build.Wire(dismiss, so => UI51Build.Ref(so, "closeButton", close));
        }

        /// <summary>Spunta del mockup (path SVG M5 12.5 l4.5 4.5 L19 7.5 su 24) fatta con due tratti arrotondati OnGold.</summary>
        internal static void CheckMark(RectTransform parent, float size, float strokeSvg)
        {
            var glyph = UI51Build.Center(UI51Build.Child(parent, "Glyph"), size, size);
            UI51Build.Remove<Image>(glyph.gameObject); // prima versione: icona tinta
            float k = size / 24f, w = strokeSvg * k;
            Stroke(glyph, "Short", new Vector2(-4.75f, -2.75f) * k, 6.36f * k + w, w, -45f);
            Stroke(glyph, "Long", new Vector2(2.25f, -0.25f) * k, 13.43f * k + w, w, 45f);
        }

        static void Stroke(RectTransform parent, string name, Vector2 pos, float length, float width, float angle)
        {
            var rt = UI51Build.Place(UI51Build.Child(parent, name), new Vector2(0.5f, 0.5f), new Vector2(length, width), pos);
            UI51Build.Solid(rt, UI51Tokens.OnGold, width / 2f);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>Bersaglio trasparente a tutta misura + Button + pressione.</summary>
        internal static Button Hit(RectTransform rt)
        {
            var hit = UI51Build.Image(rt, null, Color.clear, true, false);
            UI51Build.GetOrAdd<UI51Press>(rt);
            return UI51Build.Button(hit, hit);
        }

        internal static SelectableToggleItem Toggle(RectTransform rt, GameObject on, GameObject off, GameObject check, GameObject glow)
        {
            var t = UI51Build.GetOrAdd<SelectableToggleItem>(rt);
            UI51Build.Wire(t, so =>
            {
                UI51Build.Ref(so, "background", null);
                UI51Build.Ref(so, "checkIcon", check);
                UI51Build.Ref(so, "glowObject", glow);
                UI51Build.Ref(so, "equippedOnlyObject", on);
                UI51Build.Ref(so, "availableOnlyObject", off);
            });
            return t;
        }

        static bool HasDirtyScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.isDirty) continue;
                Debug.LogError($"{Tag} La scena aperta '{(string.IsNullOrEmpty(s.path) ? s.name : s.path)}' ha modifiche non salvate: " +
                               "salvala o scartala e riesegui. Non la tocco.");
                return true;
            }
            return false;
        }
    }
}
