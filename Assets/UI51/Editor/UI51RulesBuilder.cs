using Project51.Core;
using Project51.UIV2.Core;
using Project51.Unity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// UI51 Fase 12 su MainMenu: pagina Regole (mockup Regole, RegoleAccusi, RegolePunteggio) sopra alle Impostazioni,
    /// aperta dalla riga "Regole e tutorial" (la costruisce la Fase 4). Testi del mockup, controllati sul codice delle regole.
    /// </summary>
    public static class UI51RulesBuilder
    {
        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        const string Tag = "[UI51 Fase 12]";

        // Esempio a carte: carta giocata (null = nessuna), carte prese, scritta a destra.
        sealed class Example
        {
            public Card Play; public Card[] Takes; public string Note;
            public Example(Card play, string note, params Card[] takes) { Play = play; Note = note; Takes = takes; }
        }

        sealed class Item
        {
            public string Title, Text; public Example Ex;
            public Item(string title, string text, Example ex = null) { Title = title; Text = text; Ex = ex; }
        }

        sealed class Section
        {
            public string Label, Intro, Tip; public Item[] Items; public string[,] Rows;
        }

        static Card C(Suit s, int rank) => new Card(s, rank);

        static readonly Section[] Sections =
        {
            new Section
            {
                Label = "Prese",
                Intro = "A ogni turno giochi una carta. Se può prendere, porti nel tuo mazzetto la carta giocata e quelle prese; altrimenti resta sul tavolo. Una carta che può prendere non si può scartare. Se sul tavolo c’è la carta uguale, la presa della carta uguale ha la precedenza su somme e 15.",
                Items = new[]
                {
                    new Item("Carta uguale", "Prende una carta del tavolo con lo stesso valore. Ha la precedenza: se c’è, non puoi fare somme o 15.",
                        new Example(C(Suit.Spade, 6), "6 prende 6", C(Suit.Denari, 6))),
                    new Item("Somma", "Prende due o più carte che insieme fanno il suo valore.",
                        new Example(C(Suit.Coppe, 10), "Re (10) prende 6 + 4", C(Suit.Denari, 6), C(Suit.Coppe, 4))),
                    new Item("Regola del 15", "Prende carte che, insieme a lei, fanno 15. Vale anche con una sola carta.",
                        new Example(C(Suit.Coppe, 5), "5 + Re = 15", C(Suit.Coppe, 10))),
                    new Item("Asso", "Se sul tavolo c’è un asso, prende solo quello. Se non ci sono assi, prende tutto il tavolo: “asso piglia tutto”.",
                        new Example(C(Suit.Denari, 1), "Asso prende Asso", C(Suit.Bastoni, 1))),
                },
                Tip = "Valori: Asso 1 · dal 2 al 7 il loro numero · Fante 8 · Cavallo 9 · Re 10. Il 7 di coppe è la matta: in gioco vale 7, negli accusi fa da jolly.",
            },
            new Section
            {
                Label = "Scopa",
                Intro = "Se con una presa lasci il tavolo vuoto fai scopa: vale 1 punto. Anche l’asso che piglia tutto fa scopa. Se invece l’asso prende un solo asso e sul tavolo restano altre carte, non è scopa.",
                Items = new[]
                {
                    new Item("Esempio", "Sul tavolo ci sono 3 e 4: giocando il 7 prendi entrambe e il tavolo resta vuoto.",
                        new Example(C(Suit.Denari, 7), "Scopa!", C(Suit.Spade, 3), C(Suit.Coppe, 4))),
                },
                Tip = "L’ultima giocata della smazzata non vale mai scopa.",
            },
            new Section
            {
                Label = "Accusi",
                Intro = "Quando ricevi le carte puoi accusare: mostri la mano a tutti e prendi subito i punti. Hai 5 secondi per premere ACCUSA.",
                Items = new[]
                {
                    new Item("Cirulla · 3 punti", "Tre carte con somma 9 o meno. La matta conta 1.",
                        new Example(null, "1 + 2 + 3 = 6", C(Suit.Bastoni, 1), C(Suit.Bastoni, 2), C(Suit.Spade, 3))),
                    new Item("Decino · 10 punti", "Tre carte dello stesso valore, oppure una coppia più la matta.",
                        new Example(null, "coppia di 6 + matta", C(Suit.Spade, 6), C(Suit.Denari, 6), C(Suit.Coppe, 7))),
                    new Item("15 o 30 del mazziere", "Se le 4 carte iniziali sul tavolo fanno 15 (1 punto) o 30 (2 punti), il mazziere le prende tutte. La matta vale da 1 a 10. È un accuso, non una scopa."),
                },
                Tip = "Se la mano è sia Cirulla che Decino vale il Decino. Un solo accuso a testa per mano.",
            },
            new Section
            {
                Label = "Punteggio",
                Intro = "Alla fine di ogni smazzata si contano i punti. Vince chi arriva per primo a 51.",
                Rows = new[,]
                {
                    { "Scope", "1 per ogni scopa" },
                    { "Settebello", "1 a chi prende il 7 di denari" },
                    { "Denari", "1 a chi ne ha di più: almeno 6 in 1 vs 1 e 2 vs 2, a 4 giocatori basta la maggioranza" },
                    { "Carte", "1 a chi ne ha di più: almeno 21 in 1 vs 1 e 2 vs 2, a 4 giocatori basta la maggioranza" },
                    { "Primiera", "1 al punteggio di primiera più alto" },
                    { "Grande", "5 per Fante, Cavallo e Re di denari" },
                    { "Piccola", "3 per Asso, 2 e 3 di denari, +1 per ogni 4, 5, 6 di denari" },
                    { "Accusi", "i punti degli accusi della smazzata" },
                },
                Tip = "Primiera: per ogni seme conta la carta migliore (7 = 21, 6 = 18, Asso = 16, 5 = 15, 4 = 14, 3 = 13, 2 = 12, figure = 10). In caso di parità il punto non va a nessuno.",
            },
            new Section
            {
                Label = "Formati",
                Intro = "Le regole sono le stesse in tutte le modalità; cambiano giocatori e mani.",
                Rows = new[,]
                {
                    { "1 vs 1", "2 giocatori · 6 mani per smazzata" },
                    { "2 vs 2", "compagni di fronte · 3 mani · punti di coppia" },
                    { "Tutti contro tutti", "4 giocatori, ognuno per sé · 3 mani" },
                    { "Cappotto", "tutti e 10 i denari: nel 2 vs 2 e a 4 la partita finisce subito" },
                    { "Tre assi", "se ricevi tre assi in mano vinci subito la partita" },
                    { "Pareggio", "se due giocatori sono in testa oltre 51 si gioca un’altra smazzata" },
                },
                Tip = "Si ricevono 3 carte a testa, poi 4 vanno sul tavolo. Quando tutti hanno finito le carte se ne ricevono altre 3.",
            },
        };

        [MenuItem("Tools/UI51/Build Fase 12 (Regole e tutorial)")]
        private static void Menu() => Build();

        public static void Build()
        {
            if (UI51Build.HasDirtyScene()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var settings = Object.FindObjectOfType<SettingsV2Integration>(true);
            var panel = settings != null ? new SerializedObject(settings).FindProperty("panel").objectReferenceValue as GameObject : null;
            var bg = UI51Build.Sprite("Backgrounds", "home_bg_blur");
            if (panel == null || bg == null)
            {
                Debug.LogError($"{Tag} Manca SettingsV2Integration col suo pannello o home_bg_blur. Regole non costruite.");
                return;
            }

            var rules = BuildRules(panel.transform, bg);
            UI51Build.Wire(settings, so => UI51Build.Ref(so, "rules", rules));
            var start = Object.FindObjectOfType<StartScreenV2>(true);
            var welcome = BuildWelcome(panel.transform.parent);
            if (start != null && welcome != null)
                UI51Build.Wire(start, so => { UI51Build.Ref(so, "Welcome", welcome); UI51Build.Ref(so, "Rules", rules); });
            else Debug.LogError($"{Tag} Manca StartScreenV2 o un pezzo del Benvenuto: Benvenuto non collegato.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene, ScenePath)) Debug.Log($"{Tag} Scena salvata: {ScenePath}");
            else Debug.LogError($"{Tag} Salvataggio fallito: {ScenePath}");

            var game = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            if (!BuildTutorial(game)) return;
            EditorSceneManager.MarkSceneDirty(game);
            if (EditorSceneManager.SaveScene(game, GameScenePath)) Debug.Log($"{Tag} Scena salvata: {GameScenePath}");
            else Debug.LogError($"{Tag} Salvataggio fallito: {GameScenePath}");
        }

        const string GameScenePath = "Assets/Scenes/GameScene.unity";

        /// <summary>
        /// Benvenuto (mockup Benvenuto) sopra a tutto, sullo sfondo nitido col gradiente: bagliore 320 e logo 170 a 220,
        /// da 420 "Benvenuto al tavolo!" 24, "Conosci già la Cirulla?" 14, le due scelte (padding 16, raggio 18, cerchio 52) e la nota 11.
        /// Il "+200" della prima scelta non c'è: il premio del tutorial non lo dà ancora il server.
        /// </summary>
        static UI51WelcomeView BuildWelcome(Transform parent)
        {
            var bg = UI51Build.Sprite("Backgrounds", "home_bg_base");
            var glow = UI51Build.Sprite("Common", "Bagliore_morbido");
            var back = UI51Build.Sprite("Cards", "back_giada");
            var arrow = UI51Build.Sprite("Common", "ic_nav_back_cream");
            if (bg == null || glow == null || back == null || arrow == null) return null;

            var root = UI51Build.Stretch(UI51Build.Child(parent, "UI51Welcome"));
            root.gameObject.SetActive(true); // TMP su oggetti spenti lancia eccezioni
            root.SetAsLastSibling();
            var overlay = UI51Shape.Linear((UI51Tokens.Rgba(6, 14, 28, 0.25f), 0f), (UI51Tokens.Rgba(6, 14, 28, 0.1f), 0.25f),
                (UI51Tokens.Rgba(5, 11, 24, 0.7f), 0.52f), (UI51Tokens.Rgba(3, 7, 16, 0.97f), 1f));
            var safe = UI51AccessBuilder.BuildScreen(root, bg, overlay);
            var halo = UI51Build.CenterAt(UI51Build.Child(safe, "Glow"), 195f, 220f, 320f, 320f);
            UI51Build.Image(halo, glow, UI51Tokens.WhiteA(0.85f), false, false);
            UI51AccessBuilder.Logo(safe, 195f, 220f, 170f);

            var body = UI51Build.TopBand(UI51Build.Child(safe, "Body"), 24f, 24f, 420f, 300f);
            UI51Build.Stack(body, 14f);
            UI51Build.Fit(body, false, true);
            var head = UI51Build.Child(body, "Head");
            UI51Build.Stack(head, 6f, UI51Build.Pad(0, 0, 6, 0));
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(head, "Title"), "Benvenuto al tavolo!", FontFace.CinzelBold, 24f,
                UI51Tokens.Cream, TextAlignmentOptions.Center)), -1f, 33f);
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(head, "Sub"), "Conosci già la Cirulla?", FontFace.NunitoRegular, 14f,
                UI51Tokens.CreamA(0.7f), TextAlignmentOptions.Center)), -1f, 19f);

            var teach = Option(body, "Teach", UI51Shape.Linear((UI51Tokens.GoldA(0.2f), 0f), (UI51Tokens.Rgba(12, 26, 50, 0.95f), 1f)), 2f,
                UI51Tokens.Gold, "No, insegnami", "Una partita guidata di 3 minuti", 0.7f, out var teachCircle);
            UI51Build.Shape(teachCircle, UI51Shape.Linear((UI51Tokens.GoldLight, 0f), (UI51Tokens.GoldDark, 1f)), 180f, UI51Tokens.Radii(26f), 0f, Color.clear);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(teachCircle, "Icon"), 24f, 36f), back, Color.white, false, false);
            var play = Option(body, "Play", UI51Shape.Solid(UI51Tokens.Rgba(10, 22, 44, 0.85f)), 1f, UI51Tokens.GoldA(0.35f),
                "Sì, voglio giocare", "Vai subito alla Home", 0.6f, out var playCircle);
            UI51Build.Solid(playCircle, UI51Tokens.WhiteA(0.06f), 26f, 1f, UI51Tokens.GoldA(0.35f));
            var icon = UI51Build.Center(UI51Build.Child(playCircle, "Icon"), 16f, 16f);
            UI51Build.Image(icon, arrow, Color.white);
            icon.localScale = new Vector3(-1f, 1f, 1f); // scaleX(-1): la freccia guarda avanti

            var note = UI51Build.Text(UI51Build.Child(body, "Note"), "Il tutorial e le regole sono sempre nelle Impostazioni", FontFace.NunitoRegular, 11f,
                UI51Tokens.CreamA(0.5f), TextAlignmentOptions.Center);
            UI51Build.Layout(UI51Build.NoWrap(note), -1f, 19f); // 15 di riga + 4 di margine sopra
            note.margin = new Vector4(0f, 4f, 0f, 0f);

            var view = UI51Build.GetOrAdd<UI51WelcomeView>(root);
            UI51Build.Wire(view, so => { UI51Build.Ref(so, "teach", teach); UI51Build.Ref(so, "play", play); });
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Scelta del Benvenuto: padding 16, raggio 18, cerchio 52, gap 14, titolo Cinzel 16 e riga 12.</summary>
        static Button Option(RectTransform parent, string name, Gradient fill, float border, Color borderColor, string title, string sub,
            float subAlpha, out RectTransform circle)
        {
            var rt = UI51Build.Child(parent, name);
            int pad = 16 + (int)border;
            UI51Build.Layout(rt, -1f, 52f + 2f * pad);
            var shape = UI51Build.Shape(rt, fill, 180f, UI51Tokens.Radii(18f), border, borderColor, true);
            UI51Build.Row(rt, 14f, UI51Build.Pad(pad, pad, pad, pad), TextAnchor.MiddleLeft, true, true);
            UI51Build.GetOrAdd<UI51Press>(rt);
            circle = UI51Build.Child(rt, "Circle");
            UI51Build.Layout(circle, 52f, 52f, -1f, 52f);
            var col = UI51Build.Child(rt, "Text");
            UI51Build.Layout(col, 0f, -1f, 1f);
            UI51Build.Column(col, 3f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = true;
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(col, "Title"), title, FontFace.CinzelBold, 16f, UI51Tokens.Cream,
                TextAlignmentOptions.MidlineLeft)), -1f, 22f);
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(col, "Sub"), sub, FontFace.NunitoRegular, 12f,
                UI51Tokens.CreamA(subAlpha), TextAlignmentOptions.MidlineLeft)), -1f, 16f);
            return UI51Build.Button(shape, shape);
        }

        /// <summary>
        /// Partita guidata (mockup TutorialPartita) in GameScene, sopra a tutto in GamePresentationV2: velo .74 col buco e anello d'oro,
        /// dito, barra coi pallini e "Salta" a 60, fumetto di Nonna Rosa (padding 16 16 14, raggio 20), conferma "Saltare il tutorial?"
        /// e "TUTORIAL COMPLETATO". La radice resta accesa: UI51TutorialView si spegne da sola se non c'è un tutorial.
        /// Le ricompense del mockup (+200 monete, dorso Smeraldo) non ci sono: non le dà ancora il server.
        /// </summary>
        static bool BuildTutorial(UnityEngine.SceneManagement.Scene scene)
        {
            var presentation = System.Array.Find(scene.GetRootGameObjects(), g => g.name == "GamePresentationV2")?.transform as RectTransform;
            var pill = UI51Build.FindPath(scene, "Center", "ScorePill") as RectTransform;
            var accuso = UI51Build.FindPath(scene, "AccusoButton", "UI51Accuso") as RectTransform;
            var avatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI51PrefabBuilder.PrefabPath("AvatarFrame"));
            var nonna = UI51Build.Sprite("Avatars", "av_8");
            var finger = UI51Build.Sprite("Common", "tutorial_finger");
            var glow = UI51Build.Sprite("Common", "Bagliore_morbido");
            if (presentation == null || pill == null || accuso == null || avatarPrefab == null || nonna == null || finger == null || glow == null)
            {
                Debug.LogError($"{Tag} Manca un pezzo del tutorial (GamePresentationV2, ScorePill, AccusoButton/UI51Accuso, AvatarFrame, av_8, " +
                               "tutorial_finger, Bagliore_morbido). Tutorial non costruito.");
                return false;
            }

            var root = UI51Build.Stretch(UI51Build.Child(presentation, "UI51Tutorial"));
            root.gameObject.SetActive(true);
            var safe = UI51Build.Child(root, "Safe");
            var fit = UI51Build.GetOrAdd<DesignCanvasFit>(safe);
            fit.Reference = UI51Tokens.ReferenceResolution;
            fit.Fill = true;
            safe.anchorMin = safe.anchorMax = safe.pivot = new Vector2(0.5f, 0.5f);
            safe.anchoredPosition = Vector2.zero;
            safe.sizeDelta = UI51Tokens.ReferenceResolution;

            // Guida: il velo col buco prende tutti i tocchi (anche nel buco), così le carte non si giocano.
            var coach = UI51Build.Stretch(UI51Build.Child(safe, "Coach"));
            coach.gameObject.SetActive(true);
            var spotRt = UI51Build.Child(coach, "Spot");
            spotRt.anchorMin = spotRt.anchorMax = spotRt.pivot = new Vector2(0.5f, 0.5f);
            spotRt.anchoredPosition = new Vector2(0f, -237f);
            spotRt.sizeDelta = new Vector2(310f, 186f) + Vector2.one * (2f * UI51TutorialView.Hole);
            var spot = UI51Build.Shape(spotRt, UI51Shape.Solid(Color.clear), 180f, UI51Tokens.Radii(18f + UI51TutorialView.Hole),
                UI51TutorialView.Hole, UI51Tokens.Rgba(3, 7, 16, 0.74f), true);
            var ringRt = UI51Build.Center(UI51Build.Child(spotRt, "Ring"), 316f, 192f);
            var ring = UI51Build.Solid(ringRt, Color.clear, 21f, 2f, UI51Tokens.Gold);
            var halo = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(ringRt, "Halo")), Color.clear, 21f, 0f, UI51Tokens.Gold);
            halo.color = new Color(1f, 1f, 1f, 0f);

            var fingerRt = UI51Build.Child(coach, "Finger");
            fingerRt.anchorMin = fingerRt.anchorMax = new Vector2(0.5f, 0.5f);
            fingerRt.pivot = new Vector2(0f, 1f);
            fingerRt.sizeDelta = new Vector2(60f, 66f); // glifo 40x46 + 10 di margine per l'ombra
            fingerRt.anchoredPosition = new Vector2(-125f, 232f);
            var glyph = UI51Build.Stretch(UI51Build.Child(fingerRt, "Glyph"));
            UI51Build.Image(glyph, finger, Color.white, false, false);

            var bar = UI51Build.TopBand(UI51Build.Child(coach, "Bar"), 16f, 16f, 60f, 32f);
            var dotsRow = UI51Build.Child(bar, "Dots");
            dotsRow.anchorMin = dotsRow.anchorMax = dotsRow.pivot = new Vector2(0f, 0.5f);
            dotsRow.anchoredPosition = Vector2.zero;
            dotsRow.sizeDelta = new Vector2(60f, 6f);
            UI51Build.Row(dotsRow, 5f, null, TextAnchor.MiddleLeft, false, false);
            var dots = new UI51Shape[6];
            for (int i = 0; i < dots.Length; i++)
                dots[i] = UI51Build.Solid(UI51Build.Size(UI51Build.Child(dotsRow, "Dot" + i), i == 0 ? 20f : 6f, 6f), Color.white, 3f);
            var skip = UI51Build.Child(bar, "Skip");
            UI51PrefabBuilder.ButtonBody(skip.gameObject, 62f, 32f, UI51Shape.Solid(UI51Tokens.Rgba(6, 13, 27, 0.85f)), UI51Tokens.Radii(16f), 1f,
                UI51Tokens.CreamA(0.35f), FontFace.NunitoExtraBold, 12f, 0f, UI51Tokens.Cream, "Salta");
            UI51Build.Place(skip, new Vector2(1f, 0.5f), new Vector2(62f, 32f), Vector2.zero);
            skip.pivot = new Vector2(1f, 0.5f);

            var bubble = UI51Build.Child(coach, "Bubble");
            bubble.anchorMin = new Vector2(0f, 0.5f);
            bubble.anchorMax = new Vector2(1f, 0.5f);
            bubble.pivot = new Vector2(0.5f, 1f);
            bubble.offsetMin = new Vector2(18f, bubble.offsetMin.y);
            bubble.offsetMax = new Vector2(-18f, bubble.offsetMax.y);
            bubble.anchoredPosition = new Vector2(0f, 100f);
            UI51Build.Shape(bubble, UI51Tokens.DialogFill(), 180f, UI51Tokens.Radii(20f), 1f, UI51Tokens.GoldA(0.55f), true,
                new UI51Shadow(0f, 16f, 36f, UI51Tokens.BlackA(0.55f)));
            UI51Build.Stack(bubble, 12f, UI51Build.Pad(17, 17, 15, 17));
            UI51Build.Fit(bubble, false, true);
            var head = UI51Build.Child(bubble, "Head");
            UI51Build.Row(head, 12f, null, TextAnchor.UpperLeft, true, true);
            var slot = UI51Build.Child(head, "AvatarSlot");
            UI51Build.Layout(slot, 46f, 46f, -1f, 46f); // senza minimo la fila lo schiacciava sotto al testo lungo
            var avatar = AvatarIn(slot, avatarPrefab, 46f, 2f);
            avatar.SetAvatar(nonna);
            var col = UI51Build.Child(head, "Column");
            UI51Build.Layout(col, 0f, -1f, 1f);
            UI51Build.Stack(col, 5f);
            var cap = UI51Build.Text(UI51Build.Child(col, "Cap"), "NONNA ROSA · 1 DI 6", FontFace.CinzelSemiBold, 10f, UI51Tokens.GoldA(0.85f),
                TextAlignmentOptions.MidlineLeft, 2f);
            UI51Build.Layout(UI51Build.NoWrap(cap), -1f, 14f);
            var title = UI51Build.Text(UI51Build.Child(col, "Title"), "Le tue carte", FontFace.CinzelBold, 17f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft);
            UI51Build.Layout(UI51Build.NoWrap(title), -1f, 23f);
            var text = UI51Build.Text(UI51Build.Child(col, "Text"), "Queste sono le tue 3 carte.", FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.78f),
                TextAlignmentOptions.TopLeft);
            UI51Build.Wrap(text, 13.6f);
            UI51Build.Layout(text, -1f, -1f);
            var footer = UI51Build.Child(bubble, "Footer");
            UI51Build.Layout(footer, -1f, 38f);
            var backRt = UI51Build.Child(footer, "Back");
            UI51PrefabBuilder.ButtonBody(backRt.gameObject, 76f, 36f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(12f), 0f, Color.clear,
                FontFace.NunitoExtraBold, 12f, 0f, UI51Tokens.CreamA(0.65f), "Indietro");
            UI51Build.Place(backRt, new Vector2(0f, 0.5f), new Vector2(76f, 36f), Vector2.zero);
            backRt.pivot = new Vector2(0f, 0.5f);
            var nextRt = UI51Build.Child(footer, "Next");
            UI51PrefabBuilder.GoldBody(nextRt.gameObject, 104f, 38f, 12f, FontFace.CinzelBold, 12f, 1.5f, "AVANTI");
            UI51Build.Place(nextRt, new Vector2(1f, 0.5f), new Vector2(104f, 38f), Vector2.zero);
            nextRt.pivot = new Vector2(1f, 0.5f);

            // Conferma "Saltare il tutorial?": velo .55, scheda a 280 (28 dai lati), padding 22 20 18, raggio 22.
            var skipRoot = UI51Build.Stretch(UI51Build.Child(safe, "SkipDialog"));
            skipRoot.gameObject.SetActive(true);
            var scrim = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(skipRoot, "Scrim"), -400f, -400f, -400f, -400f),
                UI51Tokens.Rgba(3, 7, 16, 0.55f), 0f, 0f, default, true);
            var skipCard = UI51Build.TopBand(UI51Build.Child(skipRoot, "Card"), 28f, 28f, 280f, 220f);
            UI51Build.Shape(skipCard, UI51Tokens.DialogFill(), 180f, UI51Tokens.Radii(22f), 1f, UI51Tokens.GoldA(0.5f), true, UI51Tokens.ShadowDialog);
            UI51Build.Stack(skipCard, 0f, UI51Build.Pad(23, 21, 19, 21));
            UI51Build.Fit(skipCard, false, true);
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(skipCard, "Title"), "Saltare il tutorial?", FontFace.CinzelBold, 19f,
                UI51Tokens.Cream, TextAlignmentOptions.Center)), -1f, 26f);
            UI51Build.Gap(skipCard, "Gap1", 10f);
            var skipText = UI51Build.Text(UI51Build.Child(skipCard, "Text"), "Puoi rifarlo quando vuoi da Impostazioni → Regole e tutorial.",
                FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.65f), TextAlignmentOptions.Top);
            UI51Build.Wrap(skipText, 13.6f);
            UI51Build.Layout(skipText, -1f, -1f);
            UI51Build.Gap(skipCard, "Gap2", 18f);
            var skipContinue = UI51AccessBuilder.GoldButton(skipCard, "Continue", "CONTINUA IL TUTORIAL", 48f, 13f);
            UI51Build.Gap(skipCard, "Gap3", 10f);
            var homeRt = UI51Build.Child(skipCard, "Home");
            UI51PrefabBuilder.ButtonBody(homeRt.gameObject, 292f, 44f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(14f), 1f, UI51Tokens.GoldA(0.45f),
                FontFace.NunitoExtraBold, 13f, 0f, UI51Tokens.Gold, "Salta e vai alla Home");
            UI51Build.Layout(homeRt, -1f, 44f);

            // Fine: velo .78, bagliore 420 a 330, a 200 TUTORIAL COMPLETATO, "Sei pronto per il tavolo!", pulsante 54 e il link alle regole.
            var doneRoot = UI51Build.Stretch(UI51Build.Child(safe, "Done"));
            doneRoot.gameObject.SetActive(true);
            UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(doneRoot, "Veil"), -400f, -400f, -400f, -400f), UI51Tokens.Rgba(3, 7, 16, 0.78f), 0f, 0f, default, true);
            var burst = UI51Build.CenterAt(UI51Build.Child(doneRoot, "Burst"), 195f, 330f, 420f, 420f);
            UI51Build.Image(burst, glow, Color.white, false, false);
            UI51Build.GetOrAdd<CanvasGroup>(burst);
            var doneHead = UI51Build.TopBand(UI51Build.Child(doneRoot, "Head"), 24f, 24f, 200f, 200f);
            UI51Build.Stack(doneHead, 0f);
            UI51Build.Fit(doneHead, false, true);
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(doneHead, "Cap"), "TUTORIAL COMPLETATO", FontFace.CinzelSemiBold, 11f,
                UI51Tokens.Gold, TextAlignmentOptions.Center, 3f)), -1f, 15f);
            UI51Build.Gap(doneHead, "Gap1", 10f);
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(doneHead, "Title"), "Sei pronto per il tavolo!", FontFace.CinzelBold, 26f,
                UI51Tokens.Cream, TextAlignmentOptions.Center)), -1f, 35f);
            UI51Build.Gap(doneHead, "Gap2", 36f); // gap 10 + margin-top 26 (le ricompense non ci sono)
            var playButton = UI51AccessBuilder.GoldButton(doneHead, "Play", "GIOCA LA PRIMA PARTITA", 54f, 15f);
            UI51Build.Gap(doneHead, "Gap3", 10f);
            var rulesLink = UI51AccessBuilder.Link(doneHead, "Rules", "Leggi tutte le regole", FontFace.NunitoExtraBold, 13f, UI51Tokens.Gold,
                TextAlignmentOptions.Center, 18f);

            var view = UI51Build.GetOrAdd<UI51TutorialView>(root);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "safe", safe);
                UI51Build.Ref(so, "coach", coach);
                UI51Build.Ref(so, "skipDialog", skipRoot);
                UI51Build.Ref(so, "skipCard", skipCard);
                UI51Build.Ref(so, "done", doneRoot);
                UI51Build.Ref(so, "doneBurst", burst);
                UI51Build.Ref(so, "doneHead", doneHead);
                UI51Build.Ref(so, "spot", spot);
                UI51Build.Ref(so, "ring", ring);
                UI51Build.Ref(so, "halo", halo);
                UI51Build.Ref(so, "finger", fingerRt);
                UI51Build.Ref(so, "fingerGlyph", glyph);
                UI51Build.Ref(so, "bubble", bubble);
                UI51Build.SetArray(so, "dots", dots);
                UI51Build.Ref(so, "cap", cap);
                UI51Build.Ref(so, "title", title);
                UI51Build.Ref(so, "text", text);
                UI51Build.Ref(so, "nextLabel", nextRt.Find("Label").GetComponent<TMP_Text>());
                UI51Build.Ref(so, "next", nextRt.GetComponent<Button>());
                UI51Build.Ref(so, "back", backRt.GetComponent<Button>());
                UI51Build.Ref(so, "skip", skip.GetComponent<Button>());
                UI51Build.Ref(so, "skipContinue", skipContinue);
                UI51Build.Ref(so, "skipScrim", UI51Build.Button(scrim, scrim));
                UI51Build.Ref(so, "skipHome", homeRt.GetComponent<Button>());
                UI51Build.Ref(so, "play", playButton);
                UI51Build.Ref(so, "rules", rulesLink);
                UI51Build.Ref(so, "scorePill", pill);
                UI51Build.Ref(so, "accuso", accuso);
            });
            root.SetAsLastSibling();
            return true;
        }

        static AvatarFrame AvatarIn(RectTransform slot, GameObject prefab, float size, float ring)
        {
            var t = slot.Find("Avatar");
            var go = t != null ? t.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, slot);
            go.name = "Avatar";
            var rt = UI51Build.Center((RectTransform)go.transform, size, size);
            rt.localScale = Vector3.one;
            var frame = go.GetComponent<AvatarFrame>();
            UI51Build.Wire(frame, so => UI51Build.Float(so, "m_RingWidth", ring));
            frame.Layout();
            return frame;
        }

        /// <summary>
        /// Pagina a tutto schermo subito sopra alle Impostazioni: indietro, "Regole", "Rifai il tutorial";
        /// schede a 80 (34, passo 6, scorrono di lato); contenuto a 130 fino a 24 dal fondo, passo 12.
        /// </summary>
        static UI51RulesView BuildRules(Transform settingsPanel, Sprite bg)
        {
            var root = UI51Build.Stretch(UI51Build.Child(settingsPanel.parent, "UI51Rules"));
            root.gameObject.SetActive(true); // TMP su oggetti spenti lancia eccezioni
            root.SetSiblingIndex(settingsPanel.GetSiblingIndex() + 1);
            var safe = UI51AccessBuilder.BuildScreen(root, bg, null);
            var page = UI51Build.Stretch(UI51Build.Child(safe, "Page"));

            var back = UI51AccessBuilder.RoundButton(page, "Back", false, 40f, 20f, UI51Tokens.Rgba(11, 29, 58, 0.6f),
                UI51Build.Sprite("Common", "ic_nav_back_cream"), 15f);
            ((RectTransform)back.transform).anchoredPosition = new Vector2(20f, -22f);
            UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(page, "Title"), 72f, 150f, 22f, 40f), "Regole",
                FontFace.CinzelBold, 22f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            var tutorial = UI51Build.Child(page, "Tutorial");
            UI51PrefabBuilder.ButtonBody(tutorial.gameObject, 124f, 34f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(17f), 1f, UI51Tokens.GoldA(0.5f),
                FontFace.NunitoExtraBold, 12f, 0f, UI51Tokens.Gold, "Rifai il tutorial");
            UI51Build.Place(tutorial, new Vector2(1f, 1f), new Vector2(124f, 34f), new Vector2(-20f, -25f));
            tutorial.gameObject.SetActive(true); // apre la partita guidata (UI51TutorialView.Launch)

            // Schede: fila che scorre di lato se non ci sta (su SE e iPhone 12 sta quasi tutta).
            var tabsArea = UI51Build.TopBand(UI51Build.Child(page, "Tabs"), 0f, 0f, 80f, 34f);
            var tabScroll = UI51Build.GetOrAdd<ScrollRect>(tabsArea);
            tabScroll.horizontal = true;
            tabScroll.vertical = false;
            tabScroll.movementType = ScrollRect.MovementType.Elastic;
            var tabViewport = UI51Build.Stretch(UI51Build.Child(tabsArea, "Viewport"));
            UI51Build.Image(tabViewport, null, Color.clear, true, false);
            UI51Build.GetOrAdd<RectMask2D>(tabViewport);
            var tabRow = UI51Build.Child(tabViewport, "Content");
            tabRow.anchorMin = tabRow.anchorMax = tabRow.pivot = new Vector2(0f, 0.5f);
            tabRow.anchoredPosition = Vector2.zero;
            tabRow.sizeDelta = new Vector2(0f, 34f);
            UI51Build.Row(tabRow, 6f, UI51Build.Pad(0, 20, 0, 20), TextAnchor.MiddleLeft, true, true).childForceExpandHeight = true;
            UI51Build.Fit(tabRow, true, false);
            tabScroll.viewport = tabViewport;
            tabScroll.content = tabRow;

            int n = Sections.Length;
            var tabs = new Button[n];
            var tabShapes = new UI51Shape[n];
            var tabLabels = new TextMeshProUGUI[n];
            for (int i = 0; i < n; i++)
            {
                var tab = UI51Build.Child(tabRow, "Tab" + i);
                tabShapes[i] = UI51Build.Solid(tab, UI51Tokens.Rgba(6, 13, 27, 0.6f), 17f, 1f, UI51Tokens.GoldA(0.2f), true);
                UI51Build.Row(tab, 0f, UI51Build.Pad(0, 14, 0, 14), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
                tabLabels[i] = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(tab, "Label"), Sections[i].Label, FontFace.NunitoBold, 13f,
                    UI51Tokens.CreamA(0.75f), TextAlignmentOptions.Center));
                UI51Build.GetOrAdd<UI51Press>(tab);
                tabs[i] = UI51Build.Button(tab, tabShapes[i]);
            }

            // Contenuto: introduzione, riquadri con esempi a carte o tabella, consiglio tratteggiato.
            var area = UI51Build.Stretch(UI51Build.Child(page, "Scroller"), 20f, 24f, 12f, 130f);
            var scroll = UI51SocialBuilder.ScrollArea(area, UI51Build.Pad(0, 8, 0, 0), 12f);
            var cards = new System.Collections.Generic.List<Image>();
            var ids = new System.Collections.Generic.List<int>();
            var sections = new RectTransform[n];
            for (int i = 0; i < n; i++)
                sections[i] = BuildSection(scroll.content, "Section" + i, Sections[i], cards, ids);

            var view = UI51Build.GetOrAdd<UI51RulesView>(root);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "back", back);
                UI51Build.Ref(so, "tutorial", tutorial.GetComponent<Button>());
                UI51Build.SetArray(so, "tabs", tabs);
                UI51Build.SetArray(so, "tabShapes", tabShapes);
                UI51Build.SetArray(so, "tabLabels", tabLabels);
                UI51Build.SetArray(so, "sections", sections);
                UI51Build.Ref(so, "scroll", scroll);
                UI51Build.SetArray(so, "cards", cards.ToArray());
                var p = so.FindProperty("cardIds");
                p.arraySize = ids.Count;
                for (int k = 0; k < ids.Count; k++) p.GetArrayElementAtIndex(k).intValue = ids[k];
            });
            root.gameObject.SetActive(false);
            return view;
        }

        static RectTransform BuildSection(RectTransform content, string name, Section s, System.Collections.Generic.List<Image> cards,
            System.Collections.Generic.List<int> ids)
        {
            var section = UI51Build.Child(content, name);
            UI51Build.Stack(section, 12f);
            var intro = UI51Build.Text(UI51Build.Child(section, "Intro"), s.Intro, FontFace.NunitoRegular, 14f, UI51Tokens.CreamA(0.82f),
                TextAlignmentOptions.TopLeft);
            UI51Build.Wrap(intro, 18.6f); // line-height 1.55

            for (int k = 0; s.Items != null && k < s.Items.Length; k++)
            {
                var item = s.Items[k];
                var panel = UI51MetaBuilder.Panel(section, "Item" + k);
                UI51Build.Stack(panel, 10f, UI51Build.Pad(15, 15, 15, 15)); // padding 14 + 1 di bordo
                var title = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(panel, "Title"), item.Title, FontFace.CinzelBold, 15f,
                    UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
                UI51Build.Layout(title, -1f, 20f);
                var text = UI51Build.Text(UI51Build.Child(panel, "Text"), item.Text, FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.72f),
                    TextAlignmentOptions.TopLeft);
                UI51Build.Wrap(text, 13.6f); // line-height 1.5
                if (item.Ex != null) BuildExample(panel, item.Ex, cards, ids);
                else UI51Build.HideChild(panel, "Example");
            }

            if (s.Rows != null)
            {
                var table = UI51MetaBuilder.Panel(section, "Rows");
                UI51Build.Stack(table, 0f, UI51Build.Pad(5, 1, 5, 1)); // padding 4 0 + 1 di bordo
                for (int r = 0; r < s.Rows.GetLength(0); r++)
                {
                    var row = UI51Build.Child(table, "Row" + r);
                    UI51Build.Row(row, 12f, UI51Build.Pad(11, 14, 11, 14), TextAnchor.UpperLeft, true, true);
                    var key = UI51Build.Text(UI51Build.Child(row, "Key"), s.Rows[r, 0], FontFace.NunitoExtraBold, 13f, UI51Tokens.Gold,
                        TextAlignmentOptions.TopLeft);
                    key.enableWordWrapping = true;
                    UI51Build.Layout(key, 92f, -1f, 0f);
                    var value = UI51Build.Text(UI51Build.Child(row, "Value"), s.Rows[r, 1], FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.78f),
                        TextAlignmentOptions.TopLeft);
                    UI51Build.Wrap(value, 8.6f); // line-height 1.45
                    UI51Build.Layout(value, 0f, -1f, 1f);
                    var line = UI51Build.Child(row, "Line");
                    UI51Build.Layout(line, -1f, -1f, -1f, -1f, true);
                    line.anchorMin = Vector2.zero;
                    line.anchorMax = new Vector2(1f, 0f);
                    line.pivot = new Vector2(0.5f, 0f);
                    line.offsetMin = Vector2.zero;
                    line.offsetMax = new Vector2(0f, 1f);
                    UI51Build.Image(line, null, UI51Tokens.GoldA(0.08f), false, false);
                }
            }
            else UI51Build.HideChild(section, "Rows");

            // Consiglio: bordo tratteggiato oro .35, fondo .55, "i" Cinzel 14 e testo 12.
            var tip = UI51Build.Child(section, "Tip");
            UI51Build.Solid(tip, UI51Tokens.Rgba(6, 13, 27, 0.55f), 14f);
            var dash = UI51Build.Stretch(UI51Build.Child(tip, "Dash"));
            UI51Build.Layout(dash, -1f, -1f, -1f, -1f, true);
            var border = UI51Build.GetOrAdd<UI51DashedBorder>(dash);
            border.Set(14f, 1f);
            border.color = UI51Tokens.GoldA(0.35f);
            border.raycastTarget = false;
            UI51Build.Row(tip, 10f, UI51Build.Pad(12, 14, 12, 14), TextAnchor.UpperLeft, true, true);
            var mark = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(tip, "I"), "i", FontFace.CinzelBold, 14f, UI51Tokens.Gold,
                TextAlignmentOptions.TopLeft));
            UI51Build.Layout(mark, 7f, 19f, 0f);
            var tipText = UI51Build.Text(UI51Build.Child(tip, "Text"), s.Tip, FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.7f),
                TextAlignmentOptions.TopLeft);
            UI51Build.Wrap(tipText, 13.6f); // line-height 1.5
            UI51Build.Layout(tipText, 0f, -1f, 1f);
            return section;
        }

        /// <summary>Riquadro verde 62 + 10 sopra e sotto: carta giocata col filo d'oro, freccia, carte prese a passo 5, scritta oro a destra.</summary>
        static void BuildExample(RectTransform panel, Example ex, System.Collections.Generic.List<Image> cards, System.Collections.Generic.List<int> ids)
        {
            var box = UI51Build.Child(panel, "Example");
            UI51Build.Layout(box, -1f, 84f);
            // radial-gradient(#1A7A58 -> #0C4532): UI51Shape non ha il radiale, va da bordo a centro e ritorno in orizzontale.
            UI51Build.Shape(box, UI51Shape.Linear((UI51Tokens.Hex("#0C4532"), 0f), (UI51Tokens.Hex("#1A7A58"), 0.5f), (UI51Tokens.Hex("#0C4532"), 1f)),
                90f, UI51Tokens.Radii(12f), 1f, UI51Tokens.GoldA(0.3f));
            UI51Build.Row(box, 10f, UI51Build.Pad(11, 13, 11, 13), TextAnchor.MiddleLeft, true, false);

            if (ex.Play != null)
            {
                var play = UI51Build.Size(UI51Build.Child(box, "Play"), 46f, 68f);
                UI51Build.Layout(play, 46f, 68f, 0f);
                UI51Build.Solid(play, Color.clear, 7f, 2f, UI51Tokens.Gold); // outline 2 a 1 dalla carta
                cards.Add(UI51Build.Image(UI51Build.Center(UI51Build.Child(play, "Face"), 40f, 62f), null, Color.white, false, false));
                ids.Add(Id(ex.Play));
                var arrow = UI51Build.Size(UI51Build.Child(box, "Arrow"), 22f, 14f);
                UI51Build.Layout(arrow, 22f, 14f, 0f);
                Stroke(arrow, "Shaft", new Vector2(1f, 7f), new Vector2(18f, 7f));
                Stroke(arrow, "Head", new Vector2(13f, 1f), new Vector2(19f, 7f), new Vector2(13f, 13f));
            }
            else
            {
                UI51Build.HideChild(box, "Play");
                UI51Build.HideChild(box, "Arrow");
            }

            var takes = UI51Build.Size(UI51Build.Child(box, "Takes"), ex.Takes.Length * 45f - 5f, 62f);
            UI51Build.Layout(takes, ex.Takes.Length * 45f - 5f, 62f, 0f);
            UI51Build.Row(takes, 5f, null, TextAnchor.MiddleLeft, false, false);
            for (int i = 0; i < 4; i++)
            {
                if (i >= ex.Takes.Length) { UI51Build.HideChild(takes, "Card" + i); continue; }
                cards.Add(UI51Build.Image(UI51Build.Size(UI51Build.Child(takes, "Card" + i), 40f, 62f), null, Color.white, false, false));
                ids.Add(Id(ex.Takes[i]));
            }
            var note = UI51Build.Text(UI51Build.Child(box, "Note"), ex.Note, FontFace.CinzelBold, 12f, UI51Tokens.GoldLight, TextAlignmentOptions.MidlineRight);
            note.enableWordWrapping = true;
            note.rectTransform.sizeDelta = new Vector2(0f, 62f);
            UI51Build.Layout(note, 0f, 62f, 1f);
        }

        static int Id(Card c) => (int)c.Suit * 10 + c.Rank - 1;

        static void Stroke(RectTransform parent, string name, params Vector2[] points)
        {
            UI51Build.Polyline(UI51Build.Stretch(UI51Build.Child(parent, name)), new Vector2(22f, 14f), 2.2f, UI51Tokens.Gold, points);
        }
    }
}
