using Project51.Core;
using Project51.UIV2.Core;
using Project51.UIV2.Screens;
using Project51.UIV2.Data;
using Project51.Unity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// UI51 Fase 15 (Progressione) su MainMenu, con le scelte dell'utente del 02/10: LivelloSu solo con cose vere.
    /// </summary>
    public static class UI51ProgressBuilder
    {
        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        const string Tag = "[UI51 Fase 15]";

        [MenuItem("Tools/UI51/Build Fase 15 (Progressione)")]
        private static void Menu() => Build();

        public static void Build()
        {
            if (UI51Build.HasDirtyScene()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var settings = Object.FindObjectOfType<SettingsV2Integration>(true);
            var panel = settings != null ? new SerializedObject(settings).FindProperty("panel").objectReferenceValue as GameObject : null;
            var start = Object.FindObjectOfType<StartScreenV2>(true);
            if (panel == null || start == null)
            {
                Debug.LogError($"{Tag} Manca SettingsV2Integration col suo pannello o StartScreenV2. Fase 15 non costruita.");
                return;
            }

            var levelUp = BuildLevelUp(panel.transform.parent);
            if (levelUp == null) { Debug.LogError($"{Tag} Mancano sprite (home_bg_base, rays_conic, Bagliore_morbido). LivelloSu non costruito."); return; }
            UI51Build.Wire(start, so => UI51Build.Ref(so, "LevelUp", levelUp));

            var chest = BuildChest(panel.transform.parent);
            var rewards = Object.FindObjectOfType<UI51RewardsView>(true);
            var mail = Object.FindObjectOfType<UI51MailView>(true);
            if (chest == null || rewards == null || mail == null)
                Debug.LogError($"{Tag} Manca un pezzo del Forziere (sprite chest_green/chest_purple/ic_coin/ic_gem) o le viste Premi/Posta.");
            else
            {
                UI51Build.Wire(rewards, so => UI51Build.Ref(so, "chest", chest));
                UI51Build.Wire(mail, so => UI51Build.Ref(so, "chest", chest));
            }

            var medals = new[] { UI51Build.Sprite("Common", "medal_trophy"), UI51Build.Sprite("Common", "medal_sun"),
                UI51Build.Sprite("Common", "medal_club"), UI51Build.Sprite("Common", "medal_sword") };
            var profile = Object.FindObjectOfType<ProfileScreenV2>(true);
            RectTransform account = null;
            if (profile != null)
                foreach (var t in profile.GetComponentsInChildren<RectTransform>(true))
                    if (t.name == "Account" && t.Find("Stats") != null) account = t;
            if (System.Array.IndexOf(medals, null) >= 0 || account == null)
                Debug.LogError($"{Tag} Mancano le medaglie (medal_*) o il gruppo Account del Profilo (Fase 4). Trofei non costruiti.");
            else BuildTrophySummary(account, BuildTrophies(panel.transform.parent, medals), medals);

            var homeScreen = Object.FindObjectOfType<HomeScreenV2>(true);
            var friends = Object.FindObjectOfType<UI51FriendsView>(true);
            if (homeScreen == null || friends == null) Debug.LogError($"{Tag} Manca la Home o la vista Amici (ritratti). Classifica non costruita.");
            else
            {
                var portraits = new SerializedObject(friends).FindProperty("avatars");
                var avatars = new Sprite[portraits.arraySize];
                for (int i = 0; i < avatars.Length; i++) avatars[i] = portraits.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                BuildRanking(panel.transform.parent, homeScreen, avatars);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene, ScenePath)) Debug.Log($"{Tag} Scena salvata: {ScenePath}");
            else Debug.LogError($"{Tag} Salvataggio fallito: {ScenePath}");
        }

        /// <summary>
        /// Classifica (mockup Classifica) sopra a tutto: indietro, "Classifica" 22 e la riga 11 dell'azzeramento; schede a 80 (44);
        /// podio da 140 a 330 (colonne 104, gap 10: secondo, primo, terzo; avatar 62/76 col numero 24, nome 12, blocco 52/70/40 col
        /// valore Cinzel 15); da 340 la lista (righe 52) che scorre fino alla riga "Tu" (58, bordo oro 1,5) a 28 dal fondo.
        /// Il riquadro dei premi per fascia non c'e': niente premi (scelta 02/10), la riga "Tu" scende al suo posto.
        /// </summary>
        static void BuildRanking(Transform parent, HomeScreenV2 home, Sprite[] avatars)
        {
            var root = UI51Build.Stretch(UI51Build.Child(parent, "UI51Ranking"));
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            var page = UI51AccessBuilder.BuildScreen(root, UI51Build.Sprite("Backgrounds", "home_bg_blur"), null);
            Sprite avatar0 = avatars.Length > 0 ? avatars[0] : null;

            var back = UI51AccessBuilder.RoundButton(page, "Back", false, 40f, 20f, UI51Tokens.Rgba(11, 29, 58, 0.6f),
                UI51Build.Sprite("Common", "ic_nav_back_cream"), 15f);
            ((RectTransform)back.transform).anchoredPosition = new Vector2(20f, -22f);
            UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(page, "Title"), 72f, 20f, 20f, 28f),
                "Classifica", FontFace.CinzelBold, 22f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            var subtitle = UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(page, "Subtitle"), 72f, 20f, 48f, 15f),
                "Classifica della settimana", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.55f), TextAlignmentOptions.MidlineLeft));

            var tabsRt = UI51Build.TopBand(UI51Build.Child(page, "Tabs"), 20f, 20f, 80f, 44f);
            UI51PrefabBuilder.BuildTabs(tabsRt.gameObject);
            UI51Build.TopBand(tabsRt, 20f, 20f, 80f, 44f); // BuildTabs lo centra a 342: torna largo quanto la pagina
            var tabs = tabsRt.GetComponent<SegmentedTabs>();
            tabs.SetLabels(new[] { "Settimana", "Amici", "Sempre" });

            // Podio: colonne centrate a 195 -/+ 114, fondo a 330.
            var podium = new RectTransform[3];
            float[] centers = { 0f, -114f, 114f }, sizes = { 76f, 62f, 62f }, heights = { 70f, 52f, 40f };
            var medalFills = new[]
            {
                UI51Shape.Linear((UI51Tokens.GoldLight, 0f), (UI51Tokens.GoldDark, 1f)),
                UI51Shape.Linear((UI51Tokens.Hex("#F2F2F2"), 0f), (UI51Tokens.Hex("#9AA3AF"), 1f)),
                UI51Shape.Linear((UI51Tokens.Hex("#E8B07A"), 0f), (UI51Tokens.Hex("#9A5B2A"), 1f)),
            };
            var podiumRoot = UI51Build.TopBand(UI51Build.Child(page, "Podium"), 0f, 0f, 140f, 190f);
            for (int i = 0; i < 3; i++)
            {
                var col = UI51Build.Place(UI51Build.Child(podiumRoot, "Place" + (i + 1)), new Vector2(0.5f, 0f), new Vector2(104f, 190f),
                    new Vector2(centers[i], 0f));
                col.pivot = new Vector2(0.5f, 0f);
                float h = heights[i], size = sizes[i], nameBottom = h + 6f, avatarBottom = nameBottom + 17f + 12f;
                var block = UI51Build.Place(UI51Build.Child(col, "Block"), new Vector2(0.5f, 0f), new Vector2(104f, h), Vector2.zero);
                block.pivot = new Vector2(0.5f, 0f);
                UI51Build.Shape(block, UI51Shape.Linear((i == 0 ? UI51Tokens.GoldA(0.35f) : UI51Tokens.WithAlpha(UI51Tokens.TeamBlue, 0.25f), 0f),
                    (UI51Tokens.Rgba(6, 13, 27, 0.4f), 1f)), 180f, UI51Tokens.RadiiTop(12f), 1f, UI51Tokens.GoldA(0.3f));
                UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(block, "Value"), 0f, 0f, 8f, 20f), "0",
                    FontFace.CinzelBold, 15f, UI51Tokens.Cream, TextAlignmentOptions.Center));
                var name = UI51Build.Place(UI51Build.Child(col, "Name"), new Vector2(0.5f, 0f), new Vector2(104f, 17f), new Vector2(0f, nameBottom));
                name.pivot = new Vector2(0.5f, 0f);
                UI51Build.NoWrap(UI51Build.Text(name, "Giocatore", FontFace.NunitoExtraBold, 12f, UI51Tokens.Cream, TextAlignmentOptions.Center));
                var face = UI51Build.Place(UI51Build.Child(col, "Avatar"), new Vector2(0.5f, 0f), new Vector2(size, size), new Vector2(0f, avatarBottom));
                face.pivot = new Vector2(0.5f, 0f);
                if (i == 0) UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(face, "Glow")), Color.clear, size / 2f, 0f, default, false,
                    new UI51Shadow(0f, 0f, 22f, UI51Tokens.GoldA(0.55f)));
                UI51PrefabBuilder.BuildAvatar(face.gameObject, size, 3f, i == 0 ? FrameStyle.Oro : FrameStyle.Blu, avatar0, 30f);
                UI51Build.Place(face, new Vector2(0.5f, 0f), new Vector2(size, size), new Vector2(0f, avatarBottom)).pivot = new Vector2(0.5f, 0f);
                var badge = UI51Build.Place(UI51Build.Child(face, "Badge"), new Vector2(0.5f, 0f), new Vector2(24f, 24f), new Vector2(0f, -8f));
                badge.pivot = new Vector2(0.5f, 0f);
                UI51Build.Shape(badge, medalFills[i], 180f, UI51Tokens.Radii(12f), 2f, UI51Tokens.BadgeRing);
                UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(badge, "Pos")), (i + 1).ToString(), FontFace.CinzelBold, 11f,
                    UI51Tokens.BadgeRing, TextAlignmentOptions.Center));
                podium[i] = col;
            }

            // Lista dalla 4: riquadro (padding 2) che scorre, righe 52 con la linea .08 sotto.
            var list = UI51Build.Stretch(UI51Build.Child(page, "List"), 20f, 98f, 20f, 340f);
            UI51Build.Shape(list, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            var scroll = UI51SocialBuilder.ScrollArea(UI51Build.Stretch(UI51Build.Child(list, "Scroller"), 0f, 2f, 0f, 2f), null, 0f);
            var template = UI51Build.Child(scroll.content, "RowTemplate");
            UI51Build.Layout(template, -1f, 52f);
            UI51Build.Image(UI51Build.Place(UI51Build.Child(template, "Line"), new Vector2(0.5f, 0f), new Vector2(-28f, 1f), Vector2.zero), null,
                UI51Tokens.GoldA(0.08f), false, false).rectTransform.anchorMin = new Vector2(0f, 0f);
            var line = (RectTransform)template.Find("Line");
            line.anchorMin = Vector2.zero; line.anchorMax = new Vector2(1f, 0f); line.pivot = new Vector2(0.5f, 0f);
            line.offsetMin = new Vector2(14f, 0f); line.offsetMax = new Vector2(-14f, 1f);
            var pos = UI51Build.Place(UI51Build.Child(template, "Pos"), new Vector2(0f, 0.5f), new Vector2(24f, 20f), new Vector2(14f, 0f));
            pos.pivot = new Vector2(0f, 0.5f);
            UI51Build.NoWrap(UI51Build.Text(pos, "4", FontFace.CinzelBold, 13f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.MidlineLeft));
            var rowFace = UI51Build.Place(UI51Build.Child(template, "Avatar"), new Vector2(0f, 0.5f), new Vector2(34f, 34f), new Vector2(48f, 0f));
            UI51PrefabBuilder.BuildAvatar(rowFace.gameObject, 34f, 2f, FrameStyle.Blu, avatar0, 20f);
            UI51Build.Place(rowFace, new Vector2(0f, 0.5f), new Vector2(34f, 34f), new Vector2(48f, 0f));
            var texts = UI51Build.Stretch(UI51Build.Child(template, "Texts"), 92f, 0f, 90f, 0f);
            UI51Build.Column(texts, 1f, null, TextAnchor.MiddleLeft, true, true).childForceExpandHeight = false;
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(texts, "Name"), "Giocatore", FontFace.NunitoExtraBold, 13f,
                UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft)), -1f, 18f);
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(texts, "Level"), "Liv. 1", FontFace.NunitoRegular, 10f,
                UI51Tokens.CreamA(0.5f), TextAlignmentOptions.MidlineLeft)), -1f, 14f);
            var value = UI51Build.Place(UI51Build.Child(template, "Value"), new Vector2(1f, 0.5f), new Vector2(80f, 20f), new Vector2(-14f, 0f));
            value.pivot = new Vector2(1f, 0.5f);
            UI51Build.NoWrap(UI51Build.Text(value, "0", FontFace.CinzelBold, 14f, UI51Tokens.Cream, TextAlignmentOptions.MidlineRight));

            var status = UI51Build.Text(UI51Build.TopBand(UI51Build.Child(page, "Status"), 30f, 30f, 400f, 40f), "", FontFace.NunitoRegular, 13f,
                UI51Tokens.CreamA(0.6f), TextAlignmentOptions.Center);
            status.enableWordWrapping = true;

            // Riga "Tu": 58, gradiente oro .2 -> blu .95, bordo oro 1,5, raggio 16, padding 14, gap 10.
            var me = UI51Build.Place(UI51Build.Child(page, "Me"), new Vector2(0.5f, 0f), new Vector2(350f, 58f), new Vector2(0f, 28f));
            me.anchorMin = new Vector2(0f, 0f); me.anchorMax = new Vector2(1f, 0f); me.pivot = new Vector2(0.5f, 0f);
            me.offsetMin = new Vector2(20f, 28f); me.offsetMax = new Vector2(-20f, 86f);
            UI51Build.Shape(me, UI51Shape.Linear((UI51Tokens.GoldA(0.2f), 0f), (UI51Tokens.Rgba(12, 26, 50, 0.95f), 1f)), 180f, UI51Tokens.Radii(16f),
                1.5f, UI51Tokens.Gold);
            var myPos = UI51Build.Place(UI51Build.Child(me, "Pos"), new Vector2(0f, 0.5f), new Vector2(40f, 20f), new Vector2(14f, 0f));
            myPos.pivot = new Vector2(0f, 0.5f);
            var myPosition = UI51Build.NoWrap(UI51Build.Text(myPos, "#14", FontFace.CinzelBold, 14f, UI51Tokens.GoldLight, TextAlignmentOptions.MidlineLeft));
            myPosition.enableAutoSizing = true; myPosition.fontSizeMin = 10f; myPosition.fontSizeMax = 14f; // "#1.204"
            var myFace = UI51Build.Child(me, "Avatar");
            var myAvatar = UI51PrefabBuilder.BuildAvatar(myFace.gameObject, 38f, 2f, FrameStyle.Oro, avatar0, 22f);
            UI51Build.Place(myFace, new Vector2(0f, 0.5f), new Vector2(38f, 38f), new Vector2(58f, 0f));
            var myTexts = UI51Build.Stretch(UI51Build.Child(me, "Texts"), 106f, 0f, 90f, 0f);
            UI51Build.Column(myTexts, 1f, null, TextAnchor.MiddleLeft, true, true).childForceExpandHeight = false;
            var myName = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(myTexts, "Name"), "Tu", FontFace.NunitoExtraBold, 13f,
                UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            UI51Build.Layout(myName, -1f, 18f);
            var myNote = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(myTexts, "Note"), "", FontFace.NunitoRegular, 10f,
                UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft));
            UI51Build.Layout(myNote, -1f, 14f);
            var myVal = UI51Build.Place(UI51Build.Child(me, "Value"), new Vector2(1f, 0.5f), new Vector2(80f, 20f), new Vector2(-14f, 0f));
            myVal.pivot = new Vector2(1f, 0.5f);
            var myValue = UI51Build.NoWrap(UI51Build.Text(myVal, "0", FontFace.CinzelBold, 15f, UI51Tokens.GoldLight, TextAlignmentOptions.MidlineRight));

            var view = UI51Build.GetOrAdd<UI51RankingView>(root);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "home", home); UI51Build.Ref(so, "page", root.Find("UI51").gameObject); UI51Build.Ref(so, "back", back);
                UI51Build.Ref(so, "subtitle", subtitle); UI51Build.Ref(so, "status", status); UI51Build.Ref(so, "tabs", tabs);
                UI51Build.SetArray(so, "podium", podium);
                UI51Build.Ref(so, "list", list.gameObject); UI51Build.Ref(so, "scroll", scroll); UI51Build.Ref(so, "rowTemplate", template);
                UI51Build.Ref(so, "myPosition", myPosition); UI51Build.Ref(so, "myName", myName); UI51Build.Ref(so, "myNote", myNote);
                UI51Build.Ref(so, "myValue", myValue); UI51Build.Ref(so, "myAvatar", myAvatar);
                UI51Build.SetArray(so, "avatars", avatars);
            });
            template.gameObject.SetActive(false);
            root.Find("UI51").gameObject.SetActive(false); // la radice resta accesa: Awake si iscrive al pulsante della Home
        }

        /// <summary>
        /// Pagina Trofei (mockup Trofei) sopra a tutto: indietro e "Trofei" 22 a 22 col conto Cinzel 15 a destra, barra 8 a 76,
        /// categorie (pillole alte 32, gap 6) a 100, da 148 la griglia a 3 (110x142, gap 10) che scorre fino a 20 dal fondo.
        /// Scheda: medaglia 50, nome 11, "Ottenuto" 9 (al posto della data, che non si salva) o barra 4 con "x / y" 9.
        /// Dettaglio dal basso: medaglia 90, nome 19, descrizione 13, stato 12, Chiudi; niente "Premio" (i trofei non ne danno).
        /// </summary>
        static UI51TrophiesView BuildTrophies(Transform parent, Sprite[] medals)
        {
            var root = UI51Build.Stretch(UI51Build.Child(parent, "UI51Trophies"));
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            var page = UI51AccessBuilder.BuildScreen(root, UI51Build.Sprite("Backgrounds", "home_bg_blur"), null);

            var back = UI51AccessBuilder.RoundButton(page, "Back", false, 40f, 20f, UI51Tokens.Rgba(11, 29, 58, 0.6f),
                UI51Build.Sprite("Common", "ic_nav_back_cream"), 15f);
            ((RectTransform)back.transform).anchoredPosition = new Vector2(20f, -22f);
            UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(page, "Title"), 72f, 100f, 22f, 40f),
                "Trofei", FontFace.CinzelBold, 22f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            var count = UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(page, "Count"), 200f, 20f, 22f, 40f),
                "0 / 15", FontFace.CinzelBold, 15f, UI51Tokens.GoldLight, TextAlignmentOptions.MidlineRight));
            var track = UI51Build.TopBand(UI51Build.Child(page, "Progress"), 20f, 20f, 76f, 8f);
            UI51Build.Solid(track, UI51Tokens.WhiteA(0.12f), 4f);
            var fill = Fill(track, 4f, UI51Shape.Linear((UI51Tokens.GoldDark, 0f), (UI51Tokens.GoldLight, 1f)));

            var chipRow = UI51Build.TopBand(UI51Build.Child(page, "Chips"), 20f, 20f, 100f, 32f);
            UI51Build.Row(chipRow, 6f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = false;
            var labels = new string[Trophies.Categories.Length + 1];
            labels[0] = "Tutti";
            Trophies.Categories.CopyTo(labels, 1);
            var chips = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                var chip = UI51Build.Child(chipRow, "Chip" + i);
                var shape = UI51Build.Solid(chip, UI51Tokens.Rgba(6, 13, 27, 0.6f), 16f, 1f, UI51Tokens.GoldA(0.2f), true);
                var label = UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(chip, "Label")), labels[i],
                    FontFace.NunitoExtraBold, 12f, UI51Tokens.CreamA(0.75f), TextAlignmentOptions.Center));
                UI51Build.Layout(chip, Mathf.Ceil(label.GetPreferredValues(labels[i]).x) + 24f, 32f);
                chips[i] = UI51Build.Button(chip, shape);
            }

            var built = page.Find("Scroller/Viewport/Content"); // ricostruzione: la griglia di prima impedirebbe la colonna di ScrollArea
            if (built != null) UI51Build.Remove<GridLayoutGroup>(built.gameObject);
            var scroll = UI51SocialBuilder.ScrollArea(UI51Build.Stretch(UI51Build.Child(page, "Scroller"), 20f, 20f, 20f, 148f), null, 0f);
            var content = (RectTransform)scroll.content;
            UI51Build.Remove<VerticalLayoutGroup>(content.gameObject);
            var grid = UI51Build.GetOrAdd<GridLayoutGroup>(content);
            grid.cellSize = new Vector2(110f, 142f);
            grid.spacing = new Vector2(10f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter; // la pagina e' larga 431 su iPhone 12
            grid.padding = UI51Build.Pad(0, 0, 12, 0);
            var cards = new RectTransform[Trophies.All.Length];
            for (int i = 0; i < cards.Length; i++) cards[i] = TrophyCard(content, "Trophy" + i, Trophies.All[i], medals);

            // Dettaglio: velo e foglio alto 290 dal fondo (padding 22/20/26, gap 8).
            var detail = UI51Build.Stretch(UI51Build.Child(page, "Detail"));
            var scrimRt = UI51Build.Stretch(UI51Build.Child(detail, "Scrim"), -400f, -400f, -400f, -400f); // anche fuori dall'area sicura
            var scrim = UI51Build.Button(scrimRt, UI51Build.Solid(scrimRt, UI51Tokens.Rgba(3, 7, 16, 0.55f), 0f, 0f, default, true));
            var sheet = UI51Build.Place(UI51Build.Child(detail, "Sheet"), new Vector2(0.5f, 0f), new Vector2(390f, 290f), Vector2.zero);
            sheet.pivot = new Vector2(0.5f, 0f);
            UI51Build.Shape(sheet, UI51Shape.Linear((UI51Tokens.Rgba(14, 28, 52, 0.98f), 0f), (UI51Tokens.Rgba(7, 14, 28, 0.99f), 1f)), 180f,
                UI51Tokens.RadiiTop(24f), 1f, UI51Tokens.GoldA(0.5f), true, new UI51Shadow(0f, -10f, 40f, UI51Tokens.BlackA(0.5f)));
            var sheetColumn = UI51Build.Column(sheet, 8f, UI51Build.Pad(22, 20, 26, 20), TextAnchor.UpperCenter, true, true);
            sheetColumn.childForceExpandHeight = false;
            sheetColumn.childForceExpandWidth = true;
            var under = UI51Build.Place(UI51Build.Child(sheet, "Under"), new Vector2(0.5f, 0f), new Vector2(390f, 400f), Vector2.zero);
            under.pivot = new Vector2(0.5f, 1f); // il foglio continua sotto l'area sicura fino al bordo dello schermo
            UI51Build.Layout(under, -1f, -1f, -1f, -1f, true);
            UI51Build.Solid(under, UI51Tokens.Rgba(7, 14, 28, 0.99f), 0f);
            var detailMedal = UI51Build.Image(UI51Build.Child(sheet, "Medal"), medals[0], Color.white);
            UI51Build.Layout(detailMedal, -1f, 90f);
            var detailName = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(sheet, "Name"), "Prima vittoria", FontFace.CinzelBold, 19f,
                UI51Tokens.Cream, TextAlignmentOptions.Center));
            UI51Build.Layout(detailName, -1f, 26f);
            var detailText = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(sheet, "Text"), "Vinci la tua prima partita", FontFace.NunitoRegular,
                13f, UI51Tokens.CreamA(0.7f), TextAlignmentOptions.Center));
            UI51Build.Layout(detailText, -1f, 18f);
            var detailStatus = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(sheet, "Status"), "Ottenuto", FontFace.NunitoExtraBold, 12f,
                UI51Tokens.Gold, TextAlignmentOptions.Center));
            UI51Build.Layout(detailStatus, -1f, 17f);
            UI51Build.Layout(UI51Build.Child(sheet, "Gap"), -1f, 2f);
            var close = UI51AccessBuilder.GhostButton(sheet, "Close", "Chiudi", 46f);

            var view = UI51Build.GetOrAdd<UI51TrophiesView>(root);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "back", back); UI51Build.Ref(so, "count", count); UI51Build.Ref(so, "progressFill", fill);
                UI51Build.SetArray(so, "chips", chips);
                UI51Build.SetArray(so, "cards", cards);
                UI51Build.SetArray(so, "medals", medals);
                UI51Build.Ref(so, "scroll", scroll);
                UI51Build.Ref(so, "detail", detail.gameObject); UI51Build.Ref(so, "sheet", sheet);
                UI51Build.Ref(so, "scrim", scrim); UI51Build.Ref(so, "close", close);
                UI51Build.Ref(so, "detailMedal", detailMedal); UI51Build.Ref(so, "detailName", detailName);
                UI51Build.Ref(so, "detailText", detailText); UI51Build.Ref(so, "detailStatus", detailStatus);
            });
            detail.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Scheda 110x142 (padding 8, gap 6, al centro): Medal 50, Name 11 (due righe), Done 9 oppure Progress (Track 4 + Value 9).</summary>
        static RectTransform TrophyCard(RectTransform content, string name, Trophy t, Sprite[] medals)
        {
            var card = UI51Build.Child(content, name);
            var shape = UI51Build.Shape(card, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.GoldA(0.5f), true);
            UI51Build.Button(card, shape);
            UI51Build.Column(card, 6f, UI51Build.Pad(8, 8, 8, 8), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = false;
            UI51Build.Layout(UI51Build.Image(UI51Build.Child(card, "Medal"), medals[t.Medal], Color.white), -1f, 50f);
            var label = UI51Build.Text(UI51Build.Child(card, "Name"), t.Name, FontFace.NunitoExtraBold, 11f, UI51Tokens.Cream, TextAlignmentOptions.Center);
            label.enableWordWrapping = true;
            label.lineSpacing = -12f;
            UI51Build.Layout(label, -1f, 30f);
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(card, "Done"), "Ottenuto", FontFace.NunitoRegular, 9f,
                UI51Tokens.CreamA(0.45f), TextAlignmentOptions.Center)), -1f, 20f);
            var progress = UI51Build.Child(card, "Progress");
            UI51Build.Layout(progress, -1f, 20f);
            var track = UI51Build.TopBand(UI51Build.Child(progress, "Track"), 0f, 0f, 0f, 4f);
            UI51Build.Solid(track, UI51Tokens.WhiteA(0.12f), 2f);
            Fill(track, 2f, UI51Shape.Solid(UI51Tokens.Gold));
            UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(progress, "Value"), 0f, 0f, 7f, 13f), "0 / 1",
                FontFace.NunitoBold, 9f, UI51Tokens.CreamA(0.55f), TextAlignmentOptions.Center));
            return card;
        }

        /// <summary>
        /// Sezione TROFEI del Profilo (mockup Profilo) in fondo al gruppo Account, sotto STATISTICHE: "N / 15 · Vedi tutti" 12 a destra
        /// della scritta; scheda (padding 14/16/16, gap 14) con 4 medaglie 50 e nome 10, riga, "IN CORSO" 9 e due righe (nome 13,
        /// descrizione 11, valore 11 oro, barra 5). Il Build Fase 4 la lascia in fondo.
        /// </summary>
        static void BuildTrophySummary(RectTransform account, UI51TrophiesView page, Sprite[] medals)
        {
            var section = UI51MetaBuilder.Section(account, "Trophies", "TROFEI", 8f);
            section.SetAsLastSibling();
            var link = UI51AccessBuilder.Link((RectTransform)section.Find("Caption"), "Link", "0 / 15 · Vedi tutti", FontFace.NunitoBold, 12f,
                UI51Tokens.Gold, TextAlignmentOptions.MidlineRight, 20f);
            var linkRt = UI51Build.Place((RectTransform)link.transform, new Vector2(1f, 0.5f), new Vector2(200f, 22f), Vector2.zero);
            linkRt.pivot = new Vector2(1f, 0.5f);

            var panel = UI51Build.Child(section, "Panel");
            UI51Build.Shape(panel, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            UI51Build.Stack(panel, 14f, UI51Build.Pad(14, 16, 16, 16));
            var row = UI51Build.Child(panel, "Medals");
            UI51Build.Layout(row, -1f, 84f);
            UI51Build.Row(row, 8f, null, TextAnchor.UpperLeft, true, true).childForceExpandWidth = false;
            var slots = new GameObject[4];
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = UI51Build.Child(row, "Slot" + i);
                UI51Build.Layout(slot, 73.5f, 84f);
                UI51Build.Column(slot, 6f, null, TextAnchor.UpperCenter, true, true).childForceExpandHeight = false;
                UI51Build.Layout(UI51Build.Image(UI51Build.Child(slot, "Medal"), medals[i], Color.white), -1f, 50f);
                var label = UI51Build.Text(UI51Build.Child(slot, "Name"), Trophies.All[i].Name, FontFace.NunitoBold, 10f, UI51Tokens.CreamA(0.8f),
                    TextAlignmentOptions.Top);
                label.enableWordWrapping = true;
                label.lineSpacing = -12f;
                UI51Build.Layout(label, -1f, 28f);
                slots[i] = slot.gameObject;
            }
            var emptyRt = UI51Build.Stretch(UI51Build.Child(row, "Empty"));
            UI51Build.Layout(emptyRt, -1f, -1f, -1f, -1f, true);
            var empty = UI51Build.Text(emptyRt, "Ancora nessun trofeo: gioca una partita per ottenere il primo.", FontFace.NunitoRegular, 12f,
                UI51Tokens.CreamA(0.6f), TextAlignmentOptions.Center);
            empty.enableWordWrapping = true;

            var progress = UI51Build.Child(panel, "InProgress");
            UI51Build.Stack(progress, 12f);
            UI51Build.Layout(UI51Build.Image(UI51Build.Child(progress, "Line"), null, UI51Tokens.GoldA(0.14f), false, false), -1f, 1f);
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(progress, "Caption"), "IN CORSO", FontFace.CinzelSemiBold, 9f,
                UI51Tokens.GoldA(0.8f), TextAlignmentOptions.MidlineLeft, 2f)), -1f, 12f);
            var rows = new GameObject[2];
            for (int i = 0; i < rows.Length; i++)
            {
                var r = UI51Build.Child(progress, "Row" + i);
                UI51Build.Layout(r, -1f, 29f);
                var n = UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(r, "Name"), 0f, 56f, 0f, 18f), "Trofeo",
                    FontFace.NunitoExtraBold, 13f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
                n.richText = true;
                UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(r, "Value"), 200f, 0f, 0f, 18f), "0/1",
                    FontFace.NunitoExtraBold, 11f, UI51Tokens.Gold, TextAlignmentOptions.MidlineRight));
                var track = UI51Build.TopBand(UI51Build.Child(r, "Track"), 0f, 0f, 24f, 5f);
                UI51Build.Solid(track, UI51Tokens.WhiteA(0.12f), 2.5f);
                Fill(track, 2.5f, UI51Shape.Linear((UI51Tokens.GoldDark, 0f), (UI51Tokens.GoldLight, 1f)));
                rows[i] = r.gameObject;
            }

            var view = UI51Build.GetOrAdd<UI51TrophySummary>(section);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "link", link); UI51Build.Ref(so, "linkLabel", link.GetComponent<TMP_Text>()); UI51Build.Ref(so, "empty", empty);
                UI51Build.SetArray(so, "slots", slots);
                UI51Build.SetArray(so, "rows", rows);
                UI51Build.Ref(so, "inProgress", progress.gameObject);
                UI51Build.SetArray(so, "medals", medals);
                UI51Build.Ref(so, "page", page);
            });
        }

        /// <summary>Riempimento ancorato a sinistra (lo allarga anchorMax.x), angolo 90 = da sinistra a destra.</summary>
        static RectTransform Fill(RectTransform track, float radius, Gradient fill)
        {
            var rt = UI51Build.Child(track, "Fill");
            rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(0.4f, 1f); rt.pivot = new Vector2(0f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            UI51Build.Shape(rt, fill, 90f, UI51Tokens.Radii(radius), 0f, Color.clear);
            return rt;
        }

        /// <summary>
        /// Forziere e ForziereAperto (mockup Forziere, 390x844) sopra a tutto: sfondo sfocato scuro; da 50 la scritta del colore,
        /// il titolo 24 e la riga 12. Chiuso: bagliore 380 a .55 e forziere 220 centrati a 390, "TOCCA PER APRIRE" 13 a 530.
        /// Aperto: scoppio 460 a 300, forziere 120 a 160, schede 110x190 da 290 (monete e gemme vere al posto delle 3 carte con
        /// rarita' e frammenti), RACCOGLI a 28 dal fondo.
        /// </summary>
        static UI51ChestView BuildChest(Transform parent)
        {
            var bg = UI51Build.Sprite("Backgrounds", "home_bg_blur");
            var glow = UI51Build.Sprite("Common", "Bagliore_morbido");
            var green = UI51Build.Sprite("Common", "chest_green");
            var purple = UI51Build.Sprite("Common", "chest_purple");
            var coin = UI51Build.Sprite("Common", "ic_coin");
            var gem = UI51Build.Sprite("Common", "ic_gem");
            if (bg == null || glow == null || green == null || purple == null || coin == null || gem == null) return null;

            var root = UI51Build.Stretch(UI51Build.Child(parent, "UI51Chest"));
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            // Si apre da Premi e Posta (canvas a 700): canvas suo sopra di loro.
            var canvas = UI51Build.GetOrAdd<Canvas>(root);
            canvas.overrideSorting = true;
            canvas.sortingOrder = 750;
            UI51Build.GetOrAdd<GraphicRaycaster>(root);
            var safe = UI51AccessBuilder.BuildScreen(root, bg, UI51Shape.Solid(UI51Tokens.Rgba(3, 7, 16, 0.62f)));

            var closed = UI51Build.Stretch(UI51Build.Child(safe, "Closed"));
            UI51Build.Image(UI51Build.CenterAt(UI51Build.Child(closed, "Glow"), 195f, 390f, 380f, 380f), glow, UI51Tokens.WhiteA(0.55f), false, false);
            var chest = UI51Build.CenterAt(UI51Build.Child(closed, "Chest"), 195f, 385f, 220f, 191f);
            var hit = UI51Build.Image(chest, null, Color.clear, true, false);
            var chestButton = UI51Build.Button(chest, hit);
            var chestImageRt = UI51Build.Stretch(UI51Build.Child(chest, "Image"));
            chestImageRt.pivot = new Vector2(0.5f, 0.1f); // transform-origin 50% 90%
            var chestImage = UI51Build.Image(chestImageRt, purple, Color.white);
            var tap = UI51Build.TopBand(UI51Build.Child(closed, "Tap"), 0f, 0f, 530f, 18f);
            UI51Build.NoWrap(UI51Build.Text(tap, "TOCCA PER APRIRE", FontFace.CinzelBold, 13f, UI51Tokens.GoldLight, TextAlignmentOptions.Center, 2f));

            var opened = UI51Build.Stretch(UI51Build.Child(safe, "Opened"));
            var burst = UI51Build.CenterAt(UI51Build.Child(opened, "Burst"), 195f, 300f, 460f, 460f);
            UI51Build.Image(burst, glow, UI51Tokens.WhiteA(0.9f), false, false);
            var small = UI51Build.CenterAt(UI51Build.Child(opened, "Chest"), 195f, 212f, 120f, 104f);
            var smallImage = UI51Build.Image(small, purple, UI51Tokens.WhiteA(0.85f));
            var row = UI51Build.TopBand(UI51Build.Child(opened, "Cards"), 20f, 20f, 290f, 190f);
            var h = UI51Build.Row(row, 10f, null, TextAnchor.MiddleCenter, true, true);
            h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            var coinsCard = Card(row, "Coins", "MONETE", UI51Tokens.CreamA(0.6f), UI51Tokens.GoldA(0.3f), null, coin, 54f, out var coinsText);
            var gemsCard = Card(row, "Gems", "GEMME", UI51Tokens.TeamBlueText, UI51Tokens.WithAlpha(UI51Tokens.TeamBlue, 0.7f),
                new UI51Shadow(0f, 0f, 18f, UI51Tokens.WithAlpha(UI51Tokens.TeamBlue, 0.4f)), gem, 44f, out var gemsText);

            var collectRt = UI51Build.Place(UI51Build.Child(opened, "Collect"), new Vector2(0.5f, 0f), new Vector2(350f, 54f), new Vector2(0f, 28f));
            collectRt.pivot = new Vector2(0.5f, 0f);
            UI51Build.Column(collectRt, 0f, null, TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;
            var collect = UI51AccessBuilder.GoldButton(collectRt, "Button", "RACCOGLI", 54f, 15f);

            // Testata sopra a entrambi gli stati.
            var header = UI51Build.TopBand(UI51Build.Child(safe, "Header"), 20f, 20f, 50f, 72f);
            var caption = UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(header, "Caption"), 0f, 0f, 0f, 14f),
                "FORZIERE VIOLA", FontFace.CinzelSemiBold, 10f, UI51Tokens.Gold, TextAlignmentOptions.Center, 3f));
            var title = UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(header, "Title"), 0f, 0f, 18f, 33f),
                "Hai un forziere da aprire", FontFace.CinzelBold, 24f, UI51Tokens.Cream, TextAlignmentOptions.Center));
            var sub = UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(header, "Sub"), 0f, 0f, 55f, 17f),
                "Dentro ci sono monete e gemme", FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.Center));

            var view = UI51Build.GetOrAdd<UI51ChestView>(root);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "closed", closed.gameObject); UI51Build.Ref(so, "opened", opened.gameObject);
                UI51Build.Ref(so, "chestButton", chestButton); UI51Build.Ref(so, "collect", collect);
                UI51Build.Ref(so, "chest", chest); UI51Build.Ref(so, "tapLabel", tap); UI51Build.Ref(so, "burst", burst);
                UI51Build.Ref(so, "smallChest", small); UI51Build.Ref(so, "coinsCard", coinsCard); UI51Build.Ref(so, "gemsCard", gemsCard);
                UI51Build.Ref(so, "collectRect", collectRt); UI51Build.Ref(so, "chestImage", chestImage); UI51Build.Ref(so, "smallChestImage", smallImage);
                UI51Build.Ref(so, "green", green); UI51Build.Ref(so, "purple", purple);
                UI51Build.Ref(so, "caption", caption); UI51Build.Ref(so, "title", title); UI51Build.Ref(so, "sub", sub);
                UI51Build.Ref(so, "coinsText", coinsText); UI51Build.Ref(so, "gemsText", gemsText);
            });
            opened.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Scheda del premio 110x190: etichetta 9 extrabold, icona, valore 15 Cinzel oro chiaro; bordo e alone del tipo.</summary>
        static RectTransform Card(RectTransform row, string name, string label, Color labelColor, Color border, UI51Shadow? halo, Sprite icon,
            float iconWidth, out TextMeshProUGUI value)
        {
            var card = UI51Build.Child(row, name);
            UI51Build.Layout(card, 110f, 190f);
            if (halo.HasValue) UI51Build.Shape(card, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, border, false, halo.Value);
            else UI51Build.Shape(card, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, border);
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Place(UI51Build.Child(card, "Label"), new Vector2(0.5f, 0.5f), new Vector2(100f, 13f),
                new Vector2(0f, 58f)), label, FontFace.NunitoExtraBold, 9f, labelColor, TextAlignmentOptions.Center, 1f));
            UI51Build.Image(UI51Build.Place(UI51Build.Child(card, "Icon"), new Vector2(0.5f, 0.5f), new Vector2(iconWidth, 56f), new Vector2(0f, 8f)),
                icon, Color.white);
            value = UI51Build.NoWrap(UI51Build.Text(UI51Build.Place(UI51Build.Child(card, "Value"), new Vector2(0.5f, 0.5f), new Vector2(100f, 22f),
                new Vector2(0f, -46f)), "+150", FontFace.CinzelBold, 15f, UI51Tokens.GoldLight, TextAlignmentOptions.Center));
            return card;
        }

        /// <summary>
        /// LivelloSu (mockup LivelloSu, 390x844): sfondo sfocato, raggi e bagliore centrati a 195,250; "NUOVO LIVELLO" e
        /// "Sei salito di livello!" 30 da 90; medaglia 140 a 180; titolo nuovo 13 a 340; da 384 "HAI SBLOCCATO" (celle 132) e la
        /// barra XP; pulsante a 28 dal fondo. Niente "+250 monete" ne' forziere ne' "Più tardi": salire di livello non da' premi.
        /// </summary>
        static UI51LevelUpView BuildLevelUp(Transform parent)
        {
            var bg = UI51Build.Sprite("Backgrounds", "home_bg_blur");
            var raysSprite = UI51Build.Sprite("Common", "rays_conic");
            var glow = UI51Build.Sprite("Common", "Bagliore_morbido");
            if (bg == null || raysSprite == null || glow == null) return null;

            var root = UI51Build.Stretch(UI51Build.Child(parent, "UI51LevelUp"));
            root.gameObject.SetActive(true); // TMP su oggetti spenti lancia eccezioni
            root.SetAsLastSibling();
            var safe = UI51AccessBuilder.BuildScreen(root, bg, UI51Shape.Solid(UI51Tokens.Rgba(3, 7, 16, 0.55f)));

            var rays = UI51Build.CenterAt(UI51Build.Child(safe, "Rays"), 195f, 250f, 440f, 440f);
            UI51Build.Image(rays, raysSprite, UI51Tokens.WhiteA(0.16f)); // come la fine partita
            var burst = UI51Build.CenterAt(UI51Build.Child(safe, "Burst"), 195f, 250f, 380f, 380f);
            UI51Build.Image(burst, glow, UI51Tokens.WhiteA(0.9f), false, false);

            var header = UI51Build.TopBand(UI51Build.Child(safe, "Header"), 0f, 0f, 90f, 60f);
            header.pivot = new Vector2(0.5f, 0.5f);
            UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(header, "Caption"), 0f, 0f, 0f, 14f),
                "NUOVO LIVELLO", FontFace.CinzelSemiBold, 10f, UI51Tokens.Gold, TextAlignmentOptions.Center, 3f));
            UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(header, "Title"), 0f, 0f, 18f, 40f),
                "Sei salito di livello!", FontFace.CinzelBold, 30f, UI51Tokens.Cream, TextAlignmentOptions.Center));

            // Medaglia: oro FCE29A -> C4922F, bordo 4 FFF1C4, alone 0 0 40 oro .7 e ombra 0 12 28 nero .6.
            var medal = UI51Build.CenterAt(UI51Build.Child(safe, "Medal"), 195f, 250f, 140f, 140f);
            UI51Build.Shape(medal, UI51Shape.Linear((UI51Tokens.GoldLight, 0f), (UI51Tokens.GoldDark, 1f)), 180f, UI51Tokens.Radii(70f), 4f,
                UI51Tokens.Hex("#FFF1C4"), false, new UI51Shadow(0f, 0f, 40f, UI51Tokens.GoldA(0.7f)), new UI51Shadow(0f, 12f, 28f, UI51Tokens.BlackA(0.6f)));
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Place(UI51Build.Child(medal, "Caption"), new Vector2(0.5f, 0.5f), new Vector2(130f, 18f),
                new Vector2(0f, 30f)), "LIVELLO", FontFace.CinzelBold, 13f, UI51Tokens.WithAlpha(UI51Tokens.OnGold, 0.7f), TextAlignmentOptions.Center, 2f));
            var level = UI51Build.NoWrap(UI51Build.Text(UI51Build.Place(UI51Build.Child(medal, "Level"), new Vector2(0.5f, 0.5f),
                new Vector2(130f, 64f), new Vector2(0f, -10f)), "13", FontFace.CinzelBold, 58f, UI51Tokens.OnGold, TextAlignmentOptions.Center));

            var titleLine = UI51Build.TopBand(UI51Build.Child(safe, "NewTitle"), 20f, 20f, 340f, 18f);
            var titleText = UI51Build.NoWrap(UI51Build.Text(titleLine, "Titolo nuovo: Esperto → Maestro", FontFace.NunitoBold, 13f,
                UI51Tokens.Gold, TextAlignmentOptions.Center));

            var body = UI51Build.TopBand(UI51Build.Child(safe, "Body"), 20f, 20f, 384f, 200f);
            UI51Build.Stack(body, 10f);
            var unlocks = UI51MetaBuilder.Section(body, "Unlocks", "HAI SBLOCCATO", 10f);
            var cells = UI51Build.Child(unlocks, "Cells");
            UI51Build.Layout(cells, -1f, 132f);
            var cell = UI51Build.Place(UI51Build.Child(cells, "Porpora"), new Vector2(0.5f, 0.5f), new Vector2(110f, 132f), Vector2.zero);
            UI51Build.Shape(cell, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.GoldA(0.5f));
            var pill = UI51Build.Place(UI51Build.Child(cell, "Banner"), new Vector2(0.5f, 1f), new Vector2(86f, 30f), new Vector2(0f, -30f));
            UI51Build.Shape(pill, UI51Banners.StaticFill(BannerStyle.Porpora, out float angle), angle, UI51Tokens.Radii(15f), 1f,
                UI51Banners.BorderColor(BannerStyle.Porpora));
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Place(UI51Build.Child(cell, "Name"), new Vector2(0.5f, 1f), new Vector2(104f, 16f),
                new Vector2(0f, -76f)), "Banner Porpora", FontFace.NunitoExtraBold, 11f, UI51Tokens.Cream, TextAlignmentOptions.Center));
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Place(UI51Build.Child(cell, "Kind"), new Vector2(0.5f, 1f), new Vector2(104f, 13f),
                new Vector2(0f, -96f)), "BANNER", FontFace.NunitoExtraBold, 9f, UI51Tokens.TeamBlueText, TextAlignmentOptions.Center));

            // Barra: padding 12/14, gap 10, "Liv. 13" 11 extrabold, binario 7 bianco .12, "80 / 340 XP" 11 crema .6.
            var bar = UI51Build.Child(body, "Xp");
            UI51Build.Layout(bar, -1f, 40f);
            UI51Build.Shape(bar, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            var xpLevel = UI51Build.NoWrap(UI51Build.Text(UI51Build.Place(UI51Build.Child(bar, "Level"), new Vector2(0f, 0.5f),
                new Vector2(56f, 16f), new Vector2(14f, 0f)), "Liv. 13", FontFace.NunitoExtraBold, 11f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            ((RectTransform)xpLevel.transform).pivot = new Vector2(0f, 0.5f);
            var xpValue = UI51Build.NoWrap(UI51Build.Text(UI51Build.Place(UI51Build.Child(bar, "Value"), new Vector2(1f, 0.5f),
                new Vector2(90f, 16f), new Vector2(-14f, 0f)), "80 / 340 XP", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.MidlineRight));
            ((RectTransform)xpValue.transform).pivot = new Vector2(1f, 0.5f);
            var track = UI51Build.Stretch(UI51Build.Child(bar, "Track"), 70f, 16.5f, 114f, 16.5f);
            UI51Build.Solid(track, UI51Tokens.WhiteA(0.12f), 3.5f);
            var fill = UI51Build.Child(track, "Fill");
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0.24f, 1f); fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            UI51Build.Shape(fill, UI51Shape.Linear((UI51Tokens.GoldDark, 0f), (UI51Tokens.GoldLight, 1f)), 90f, UI51Tokens.Radii(3.5f), 0f, Color.clear);

            var buttons = UI51Build.Place(UI51Build.Child(safe, "Buttons"), new Vector2(0.5f, 0f), new Vector2(350f, 54f), new Vector2(0f, 28f));
            buttons.pivot = new Vector2(0.5f, 0f);
            UI51Build.Column(buttons, 10f, null, TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;
            var next = UI51AccessBuilder.GoldButton(buttons, "Continue", "CONTINUA", 54f, 15f);

            var view = UI51Build.GetOrAdd<UI51LevelUpView>(root);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "rays", rays); UI51Build.Ref(so, "burst", burst); UI51Build.Ref(so, "header", header);
                UI51Build.Ref(so, "medal", medal); UI51Build.Ref(so, "titleLine", titleLine); UI51Build.Ref(so, "body", body);
                UI51Build.Ref(so, "buttons", buttons); UI51Build.Ref(so, "xpFill", fill); UI51Build.Ref(so, "unlocks", unlocks.gameObject);
                UI51Build.Ref(so, "level", level); UI51Build.Ref(so, "titleText", titleText); UI51Build.Ref(so, "xpLevel", xpLevel);
                UI51Build.Ref(so, "xpValue", xpValue); UI51Build.Ref(so, "next", next);
            });
            root.gameObject.SetActive(false);
            return view;
        }
    }
}
