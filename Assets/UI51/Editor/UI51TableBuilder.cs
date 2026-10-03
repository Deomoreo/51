using Project51.UIV2.Core;
using Project51.Unity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// UI51 Fase 5 su GameScene: tavolo 1 contro 1 (mockup Partita). Cresce un passo alla volta:
    /// S1 barra in alto (Abbandona, punteggio, Opzioni); S2 banner dei giocatori, gettone del mazziere e scope;
    /// S3 posti del mockup (banner, fila in fondo con Emoji e ACCUSA; carte in CardViewManager); S4 sfondo, tavolo e mazzo;
    /// S5 emoticon (pulsante Emoji, scelta dentro al mio banner, nuvoletta che sale); S6 accuso (medaglione ACCUSA, avviso,
    /// pugno al centro del tavolo che trema); S7 visore delle scope (tocco sulle scope di un banner); S8 scelta della presa
    /// (vassoio dal basso con le prese in fila, anelli e numeri sulle carte del tavolo); S9 foglio delle opzioni e finestra
    /// "Abbandonare la partita?"; S10 ruota del sorteggio in 1 contro 1 (a 4 resta la vecchia roulette).
    /// Fase 6 (mockup Partita4, disposizione compatta) negli stessi passi: pillola a 4 punteggi, banner verticali ai lati con
    /// scope coricate, gettone e tocco sulle scope, dorsi piccoli degli altri tre.
    /// Le misure del mockup sono su 390 di larghezza, il tavolo e' disegnato su 1080: i nodi UI51 stanno
    /// dentro contenitori con scala <see cref="UI51Build.Unit"/>, cosi' nel codice restano i numeri del mockup.
    /// Il vecchio aspetto resta in scena spento. Idempotente: rieseguirlo riusa i nodi per nome.
    /// </summary>
    public static partial class UI51TableBuilder
    {
        const string ScenePath = "Assets/Scenes/GameScene.unity";
        const string Tag = "[UI51 Fase 5]";

        [MenuItem("Tools/UI51/Build Fase 5 (Tavolo 1v1)")]
        private static void Menu() => Build();

        public static void Build()
        {
            if (UI51Build.HasDirtyScene()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var topBar = UnityEngine.Object.FindObjectOfType<TableTopBarController>(true);
            var settings = UnityEngine.Object.FindObjectOfType<InGameSettingsV2>(true);
            var bar = UI51Build.FindPath(scene, "GameCanvas", "TableTopBar");
            var banners = UnityEngine.Object.FindObjectOfType<PlayerBannerManager>(true);
            var seatLocal = UI51Build.FindPath(scene, "PlayerBanners", "Banner_Local");
            var seatTop = UI51Build.FindPath(scene, "PlayerBanners", "Banner_Top");
            var seatLeft = UI51Build.FindPath(scene, "PlayerBanners", "Banner_Left");
            var seatRight = UI51Build.FindPath(scene, "PlayerBanners", "Banner_Right");
            var ownPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI51PrefabBuilder.PrefabPath("PlayerBanner_Own"));
            var rivalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI51PrefabBuilder.PrefabPath("PlayerBanner_Opponent"));
            var sidePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI51PrefabBuilder.PrefabPath("PlayerBanner_Vertical"));
            var morePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI51PrefabBuilder.PrefabPath("Badge_More"));
            if (topBar == null || settings == null || bar == null)
            {
                Debug.LogError($"{Tag} Manca un pezzo del tavolo (GameCanvas/TableTopBar, TableTopBarController, InGameSettingsV2). Non tocco nulla.");
                return;
            }
            if (banners == null || seatLocal == null || seatTop == null || seatLeft == null || seatRight == null ||
                ownPrefab == null || rivalPrefab == null || sidePrefab == null || morePrefab == null)
            {
                Debug.LogError($"{Tag} Manca un pezzo dei banner (PlayerBannerManager, PlayerBanners/Banner_Local, Banner_Top, Banner_Left e " +
                               "Banner_Right, prefab PlayerBanner_Own, PlayerBanner_Opponent, PlayerBanner_Vertical, Badge_More). Non tocco nulla.");
                return;
            }

            BuildTopHud(topBar, bar, settings);
            BuildBanners(banners, seatLocal, seatTop, seatLeft, seatRight, ownPrefab, rivalPrefab, sidePrefab, morePrefab);
            BuildLayout(scene, seatLocal, seatTop, seatLeft, seatRight);
            BuildTable(scene);
            BuildEmoticons(scene, seatLocal);
            BuildAccuso(scene);
            BuildScope(banners, seatLocal, seatTop, seatLeft, seatRight, bar.parent);
            BuildCapture(UnityEngine.Object.FindObjectOfType<Project51.Unity.MoveSelectionUI>(true), seatLocal);
            BuildOptions(settings);
            BuildLeave(settings);
            BuildSorteggio(banners);
            BuildQuickProfile(banners, seatLocal, seatTop, seatLeft, seatRight, bar.parent);
            BuildResults(banners);
            BuildMoments(banners, bar.parent, seatLocal, seatTop, seatLeft, seatRight);

            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene, ScenePath)) Debug.Log($"{Tag} Scena salvata: {ScenePath}");
            else Debug.LogError($"{Tag} Salvataggio fallito: {ScenePath}");
        }

        // --- S1: barra in alto

        static void BuildTopHud(TableTopBarController controller, Transform bar, InGameSettingsV2 settings)
        {
            var hud = UI51Build.Stretch(UI51Build.Child(bar, "UI51TopHud"));
            var fill = UI51Tokens.Rgba(8, 17, 34, 0.7f);

            var leave = UI51AccessBuilder.RoundButton(Holder(hud, "Left", 0f), "LeaveButton", false, 40f, 20f, fill,
                UI51Build.Sprite("Common", "ic_exit_cream"), 17f);
            ((RectTransform)leave.transform).anchoredPosition = new Vector2(14f, -14f);
            var options = UI51AccessBuilder.RoundButton(Holder(hud, "Right", 1f), "OptionsButton", true, 40f, 20f, fill,
                UI51Build.Sprite("Common", "ic_settings_cream"), 18f);
            ((RectTransform)options.transform).anchoredPosition = new Vector2(-14f, -14f);

            var pill = UI51Build.Place(UI51Build.Child(Holder(hud, "Center", 0.5f), "ScorePill"),
                new Vector2(0.5f, 1f), new Vector2(0f, 40f), new Vector2(0f, -14f));
            UI51Build.Solid(pill, UI51Tokens.Rgba(8, 17, 34, 0.75f), 20f, 1f, UI51Tokens.GoldA(0.45f));
            // Bordo di 1 tutto intorno: i segmenti restano dentro, come con overflow:hidden nel mockup.
            UI51Build.Row(pill, 0f, UI51Build.Pad(1, 1, 1, 1), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            UI51Build.Fit(pill, true, false);

            var mine = Segment(pill, "Mine", 14, "TU", 9f, UI51Tokens.CreamA(0.6f), "0", 17f, UI51Tokens.Cream);
            var target = Segment(pill, "Target", 11, "A", 8f, UI51Tokens.GoldA(0.85f), "51", 14f, UI51Tokens.Gold);
            var rival = Segment(pill, "Rival", 14, "AVVERSARIO", 9f, UI51Tokens.CreamA(0.6f), "0", 17f, UI51Tokens.Cream);
            var targetRt = (RectTransform)target[0].transform.parent;
            UI51Build.Image(targetRt, null, UI51Tokens.GoldA(0.14f), false, false);
            SideLine(targetRt, "BorderLeft", 0f);
            SideLine(targetRt, "BorderRight", 1f);
            // Fase 6, tutti contro tutti a 4: gli altri due rivali, divisi da un filo piu' tenue (accesi da TableTopBarController).
            var more = new TextMeshProUGUI[2][];
            for (int i = 0; i < more.Length; i++)
            {
                more[i] = Segment(pill, "Rival" + (i + 2), 14, "RIVALE", 9f, UI51Tokens.CreamA(0.6f), "0", 17f, UI51Tokens.Cream);
                var segment = more[i][0].transform.parent;
                SideLine((RectTransform)segment, "BorderLeft", 0f, 0.2f);
                segment.gameObject.SetActive(false);
            }

            UI51Build.Wire(controller, so =>
            {
                UI51Build.Ref(so, "myLabel", mine[0]);
                UI51Build.Ref(so, "myScore", mine[1]);
                UI51Build.Ref(so, "targetScore", target[1]);
                UI51Build.Ref(so, "rivalLabel", rival[0]);
                UI51Build.Ref(so, "rivalScore", rival[1]);
                var labels = so.FindProperty("moreLabels");
                var scores = so.FindProperty("moreScores");
                labels.arraySize = scores.arraySize = more.Length;
                for (int i = 0; i < more.Length; i++)
                {
                    labels.GetArrayElementAtIndex(i).objectReferenceValue = more[i][0];
                    scores.GetArrayElementAtIndex(i).objectReferenceValue = more[i][1];
                }
            });
            UI51Build.Wire(settings, so => UI51Build.Ref(so, "OpenButton", options));
            Listen(leave, settings.OpenLeave); // finestra "Abbandonare la partita?" (S9)
        }

        /// <summary>Un solo ascoltatore salvato in scena: rieseguire il builder non lo raddoppia.</summary>
        static void Listen(Button button, UnityEngine.Events.UnityAction call)
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, call);
            EditorUtility.SetDirty(button);
        }

        // --- S2: banner dei giocatori

        /// <summary>
        /// I banner UI51 stanno dentro ai vecchi Banner_*: questi restano accesi e con lo stesso nome, perche' le carte si
        /// agganciano a loro (CardViewManager) e LocalSeatBottomShift li sposta; la loro grafica si spegne. Il banner proprio
        /// parte dal bordo sinistro del posto, quello in alto ne prende il centro; posto e misure del mockup: BuildLayout (S3).
        /// </summary>
        static void BuildBanners(PlayerBannerManager manager, Transform seatLocal, Transform seatTop, Transform seatLeft, Transform seatRight,
            GameObject ownPrefab, GameObject rivalPrefab, GameObject sidePrefab, GameObject morePrefab)
        {
            // Proprio: scope a destra, 26x40, sporgono di 8 + 13 per carta (la prima e' sopra); "+N" fino a 80 dal bordo.
            var own = Banner(seatLocal, ownPrefab, 0f, 186f, morePrefab, new Vector2(186f + 80f, 13f), new Vector2(4f, -26f),
                i => new Vector3(186f + 8f + 13f * i - 26f, 5f, i % 2 == 0 ? -3f : 4f), new Vector2(26f, 40f), true);
            // In alto: scope sopra al banner, 24x37 (mockup: due carte a 70 e 83), le altre verso sinistra.
            var rival = Banner(seatTop, rivalPrefab, 0.5f, 120f, morePrefab, new Vector2(131f, -24f), new Vector2(-29f, 13f),
                i => new Vector3(83f - 13f * i, -13f, i % 2 == 0 ? 3f : -4f), new Vector2(24f, 37f), true);
            // Lati (Fase 6, Partita4 compatta): verticali 64x100. Scopa coricata 37x24 a 26 dall'alto che sporge di 14 verso
            // l'esterno (carta 24x37 girata di 90), le altre 13 piu' in giu'; "+N" sotto la quarta; gettone 30 sopra l'angolo esterno.
            var left = Banner(seatLeft, sidePrefab, 0.5f, 64f, morePrefab, new Vector2(23f, 91f), new Vector2(0f, -30f),
                i => new Vector3(-7.5f, 19.5f + 13f * i, i % 2 == 0 ? 90f : 94f), new Vector2(24f, 37f), true, 100f);
            var right = Banner(seatRight, sidePrefab, 0.5f, 64f, morePrefab, new Vector2(78f, 91f), new Vector2(40f, -30f),
                i => new Vector3(47.5f, 19.5f + 13f * i, i % 2 == 0 ? 90f : 86f), new Vector2(24f, 37f), true, 100f);

            own.SetName("Tu");
            own.SetLevel(1);
            foreach (var other in new[] { rival, left, right })
            {
                other.SetName(string.Empty);
                other.SetLevel(-1); // livello e aspetto degli avversari: passo S2b
            }

            UI51Build.Wire(manager, so =>
            {
                var list = so.FindProperty("ui51Banners");
                var seats = new[] { own, left, rival, right }; // 0 locale, 1 sinistra, 2 alto, 3 destra
                list.arraySize = seats.Length;
                for (int i = 0; i < seats.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = seats[i];
            });
        }

        /// <summary>
        /// Un posto: scope (dietro), banner dal prefab, gettone del mazziere (davanti). Misure del mockup, origine
        /// nell'angolo in alto a sinistra del banner. card(i) = sinistra, alto, rotazione in gradi della scopa i.
        /// </summary>
        static PlayerBanner Banner(Transform seat, GameObject prefab, float alignX, float width, GameObject morePrefab,
            Vector2 moreAt, Vector2 dealerAt, System.Func<int, Vector3> card, Vector2 cardSize, bool firstCardOnTop, float height = 50f)
        {
            foreach (Transform child in seat)
                if (child.name != "UI51Banner") child.gameObject.SetActive(false);

            var holder = UI51Build.Place(UI51Build.Child(seat, "UI51Banner"), new Vector2(alignX, 0.5f), new Vector2(width, height), Vector2.zero);
            holder.localScale = new Vector3(UI51Build.Unit, UI51Build.Unit, 1f);

            var scope = UI51Build.Stretch(UI51Build.Child(holder, "Scope"));
            var cards = new Image[4];
            for (int n = 0; n < cards.Length; n++)
            {
                // L'ordine dei fratelli decide chi sta sopra: l'ultimo creato.
                int i = firstCardOnTop ? cards.Length - 1 - n : n;
                var at = card(i);
                cards[i] = UI51Build.Image(Box(UI51Build.Child(scope, "Card" + i), at.x, at.y, cardSize.x, cardSize.y, at.z), null, Color.white);
                cards[i].gameObject.SetActive(false);
            }
            var moreT = scope.Find(morePrefab.name);
            var more = moreT != null ? moreT.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(morePrefab, scope);
            more.transform.SetAsLastSibling();
            var moreRt = (RectTransform)more.transform;
            moreRt.anchorMin = moreRt.anchorMax = new Vector2(0f, 1f);
            moreRt.pivot = new Vector2(1f, 1f); // moreAt = angolo in alto a destra: con "+12" cresce verso sinistra
            moreRt.anchoredPosition = new Vector2(moreAt.x, -moreAt.y);
            more.SetActive(false);

            var bannerT = holder.Find(prefab.name);
            var bannerGo = bannerT != null ? bannerT.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
            bannerGo.transform.SetAsLastSibling();
            UI51Build.Stretch((RectTransform)bannerGo.transform);
            var banner = bannerGo.GetComponent<PlayerBanner>();

            var dealer = Box(UI51Build.Child(holder, "Dealer"), dealerAt.x, dealerAt.y, 24f, 24f);
            UI51Build.Shape(dealer, AvatarFrame.FrameFill(FrameStyle.Oro), 110f, UI51Tokens.Radii(12f), 0f, Color.clear, false,
                new UI51Shadow(0f, 3f, 6f, UI51Tokens.BlackA(0.5f)));
            UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(dealer, "Disc"), 2f, 2f, 2f, 2f), UI51Tokens.Hex("#F8EBCF"), 10f);
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(dealer, "M")), "M", FontFace.CinzelBold, 10f,
                UI51Tokens.Hex("#6B4418"), TextAlignmentOptions.Center));
            dealer.gameObject.SetActive(false);

            banner.SetTimer(-1f); // il prefab proprio mostra l'arco di prova del timer
            banner.SetCaptures(0);
            UI51Build.Wire(banner, so =>
            {
                UI51Build.Ref(so, "m_Dealer", dealer.gameObject);
                UI51Build.Ref(so, "m_ScopeMore", more.GetComponent<UI51Badge>());
                var list = so.FindProperty("m_Scope");
                list.arraySize = cards.Length;
                for (int i = 0; i < cards.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            });
            return banner;
        }

        // --- S3: posti del tavolo (mockup Partita)

        /// <summary>
        /// Banner, Emoji e ACCUSA ai posti del mockup, in unita' del mockup dal bordo alto dell'area 1080x1920. A runtime
        /// LocalSeatBottomShift porta la fila in fondo a 18 dalla safe area in basso (tutto lo spazio libero) e il banner
        /// avversario a 82 dalla safe area in alto, come la barra in alto: sui telefoni lunghi l'area di design e' piu'
        /// bassa dello schermo. Le carte seguono i banner (CardViewManager: misure Duel* nel 1v1, i suoi campi nei 4 giocatori).
        /// Anche i 4 giocatori prendono questi posti per il proprio banner e quello in alto; i laterali restano (Fase 6).
        /// I vecchi builder del tavolo (UIV2 e Table*) sono stati rimossi in Fase 10: questo e' l'unico builder del layout.
        /// </summary>
        static void BuildLayout(UnityEngine.SceneManagement.Scene scene, Transform seatLocal, Transform seatTop, Transform seatLeft, Transform seatRight)
        {
            var shifter = UnityEngine.Object.FindObjectOfType<LocalSeatBottomShift>(true);
            var cards = UnityEngine.Object.FindObjectOfType<Project51.Unity.CardViewManager>(true);
            var emoji = UI51Build.FindPath(scene, "TableActionButtons", "EmojiButton");
            var accuso = new[] { "AccusoButton", "AccusoWindowRing" };
            var badge = UI51Build.FindPath(scene, "TableActionButtons", "AccusoWindowBadge");
            var prompt = UI51Build.FindPath(scene, "TableActionButtons", "AccusoWindowPrompt");
            if (shifter == null || cards == null || emoji == null || badge == null || prompt == null ||
                System.Array.Exists(accuso, n => UI51Build.FindPath(scene, "TableActionButtons", n) == null))
            {
                Debug.LogError($"{Tag} Manca un pezzo per i posti (LocalSeatBottomShift, CardViewManager, TableActionButtons/EmojiButton, " +
                               "AccusoButton, AccusoWindowRing, AccusoWindowBadge, AccusoWindowPrompt). Posti non toccati.");
                return;
            }

            float row = 1920f / UI51Build.Unit - 18f - 25f; // centro della fila in fondo: banner alto 50, a 18 dal fondo
            Seat(seatTop, 135f + 60f, 82f + 25f, 120f);
            Seat(seatLocal, 60f + 93f, row, 186f);
            // Lati (Fase 6): 64x100 a 22 dai bordi; l'altezza la decide LocalSeatBottomShift.SideSeatY anche a runtime.
            float side = LocalSeatBottomShift.SideSeatY(82f + 25f, row);
            Seat(seatLeft, 22f + 32f, side, 64f, 100f);
            Seat(seatRight, 390f - 22f - 32f, side, 64f, 100f);
            // Emoji e ACCUSA ai lati del banner (grafica nuova in BuildEmoticons e BuildAccuso).
            Center(emoji, 34f, row);
            foreach (var n in accuso) Center(UI51Build.FindPath(scene, "TableActionButtons", n), 353f, row + 1f);
            ((RectTransform)badge).anchoredPosition = new Vector2((353f + 20f) * UI51Build.Unit, -(row + 1f - 20f) * UI51Build.Unit); // sull'anello, a 45 gradi
            Center(prompt, 195f, row - 25f - 16.5f); // tra la mano e il banner

            UI51Build.Wire(shifter, so =>
            {
                so.FindProperty("fractionOfFreeSpace").floatValue = 1f;
                var top = so.FindProperty("topTargets");
                top.arraySize = 1;
                top.GetArrayElementAtIndex(0).objectReferenceValue = seatTop;
                var sides = so.FindProperty("sideTargets");
                sides.arraySize = 2;
                sides.GetArrayElementAtIndex(0).objectReferenceValue = seatLeft;
                sides.GetArrayElementAtIndex(1).objectReferenceValue = seatRight;
            });
            // Fase 6 (Partita4): la mia mano e' quella del 1v1 (CardViewManager). Dorsi da 30 per gli altri tre: in alto dritti
            // ogni 23, 22 sotto al centro del banner (ne spuntano 12, il resto sta dietro); ai lati coricati ogni 23 sull'altezza
            // del banner, centro a 61 dal bordo esterno. Scritti qui: il prefab in memoria tiene i valori vecchi.
            UI51Build.Wire(cards, so =>
            {
                so.FindProperty("opponentCardHeight").floatValue = 30f * UI51Build.Unit;
                so.FindProperty("topHandBelowBanner").floatValue = 22f * UI51Build.Unit;
                so.FindProperty("topHandStep").floatValue = 23f * UI51Build.Unit;
                so.FindProperty("topHandFanDegrees").floatValue = 0f;
                so.FindProperty("sideHandBelowBanner").floatValue = 0f;
                so.FindProperty("sideHandInsetFromBannerEdge").floatValue = 61f * UI51Build.Unit;
                so.FindProperty("sideHandStep").floatValue = 23f * UI51Build.Unit;
            });
        }

        // --- S4: sfondo, tavolo e mazzo

        /// <summary>
        /// Sfondo: la Home sfocata come nelle altre schermate UI51. Tavolo: TableFeltRenderer disegnato via codice col bordo preso
        /// dai banner a runtime (CardViewManager.TableRim: angoli 150/120, legno 9 a tre toni, filo d'oro 1,5, sole all'8%).
        /// Mazzo: UI51TableDeck, canvas nel mondo a ordine 5 (sopra al feltro, sotto alle carte) con cuscino, dorsi e medaglione
        /// delle carte rimaste (TableDeckView). Stesso tavolo nei 4 giocatori (Partita4 ha lo stesso bordo e lo stesso cuscino).
        /// </summary>
        static void BuildTable(UnityEngine.SceneManagement.Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var background = System.Array.Find(roots, g => g.name == "GameBackground");
            var ambient = background != null ? background.GetComponent<SpriteRenderer>() : null;
            var fitter = background != null ? background.GetComponent<Project51.Unity.GameBackgroundFitter>() : null;
            var felt = UnityEngine.Object.FindObjectOfType<Project51.Unity.TableFeltRenderer>(true);
            var camera = Camera.main;
            var blur = UI51Build.Sprite("Backgrounds", "home_bg_blur");
            var sun = UI51Build.Sprite("Common", "Sun_fix");
            var cardBack = Resources.Load<Sprite>("Cards/CardBack");
            if (ambient == null || fitter == null || felt == null || camera == null || blur == null || sun == null || cardBack == null)
            {
                Debug.LogError($"{Tag} Manca un pezzo del tavolo (GameBackground con GameBackgroundFitter, TableFeltRenderer, Main Camera, " +
                               "home_bg_blur, Sun_fix, Resources/Cards/CardBack). Tavolo non toccato.");
                return;
            }

            // Mockup: sfondo a luminosita' .38 (lo sfocato e' a .41) e piu' stretto del semplice Cover (inset -30, scala 1,15).
            ambient.sprite = blur;
            ambient.color = new Color(0.93f, 0.93f, 0.93f, 1f);
            UI51Build.Wire(fitter, so => so.FindProperty("extraMargin").vector2Value = new Vector2(0f, 1.5f));
            fitter.Apply();
            EditorUtility.SetDirty(ambient);

            UI51Build.Wire(felt, so =>
            {
                foreach (var kit in new[] { "feltSprite", "vignetteSprite", "frameSprite" }) UI51Build.Ref(so, kit, null);
                UI51Build.Float(so, "cornerRadiusRatio", 150f / 370f);
                UI51Build.Float(so, "cornerRadiusYRatio", 120f / 370f);
                UI51Build.Float(so, "woodThicknessRatio", 9f / 370f);
                UI51Build.Float(so, "goldThicknessRatio", 1.5f / 370f);
                UI51Build.Int(so, "textureResolution", 1024);
                so.FindProperty("feltColor").colorValue = UI51Tokens.Felt[3];
                so.FindProperty("feltCenterColor").colorValue = UI51Tokens.Felt[0];
                // Filo d'oro rgba(243,201,105,.55) sul bordo scuro del feltro, gia' sotto l'ombra interna del mockup.
                so.FindProperty("goldColor").colorValue = UI51Tokens.Hex("#686C3C");
                so.FindProperty("woodColor").colorValue = UI51Tokens.WoodDark;
                so.FindProperty("woodTopColor").colorValue = UI51Tokens.Wood;
                so.FindProperty("woodBottomColor").colorValue = UI51Tokens.WoodMid;
                UI51Build.Float(so, "centerGlowStrength", 1f);
                UI51Build.Float(so, "weaveStrength", 0.08f);
                UI51Build.Float(so, "feltEdgeShade", 0.25f);
                UI51Build.Ref(so, "emblemSprite", sun);
                UI51Build.Float(so, "emblemAlpha", 0.08f);
            });

            BuildDeck(scene, roots, camera, cardBack);
        }

        static void BuildDeck(UnityEngine.SceneManagement.Scene scene, GameObject[] roots, Camera camera, Sprite cardBack)
        {
            var go = System.Array.Find(roots, g => g.name == "UI51TableDeck");
            if (go == null)
            {
                go = new GameObject("UI51TableDeck", typeof(RectTransform));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            }
            var rt = (RectTransform)go.transform;
            var canvas = UI51Build.GetOrAdd<Canvas>(rt);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingLayerName = "Default";
            canvas.sortingOrder = 5;
            UI51Build.GetOrAdd<GraphicRaycaster>(rt);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(72f, 64f);
            rt.localRotation = Quaternion.identity;
            // Posa di riserva finche' TableDeckView non misura il tavolo: gia' in unita' del mockup, mai a scala 1 davanti alle carte.
            float unit = Project51.Unity.CameraResponsiveFit.DesignWorldSize.x / 390f;
            rt.localScale = new Vector3(unit, unit, 1f);
            rt.position = new Vector3((52f - 195f) * unit, (1920f / UI51Build.Unit * 0.5f - 92f) * unit, 0f);

            var cushion = UI51Build.Stretch(UI51Build.Child(rt, "Cushion"));
            // Velluto: il radiale del mockup (luce al 35%) letto lungo la verticale, bordo d'oro 2.
            var velvet = UI51Build.Shape(cushion, UI51Shape.Linear((UI51Tokens.Hex("#8F1D25"), 0f), (UI51Tokens.Hex("#B32A33"), 0.35f),
                    (UI51Tokens.Hex("#962028"), 0.62f), (UI51Tokens.Hex("#6E121A"), 1f)), 180f, UI51Tokens.Radii(20f), 2f,
                UI51Tokens.Hex("#E0B252"), true, new UI51Shadow(0f, 7f, 12f, UI51Tokens.BlackA(0.55f)));
            var button = UI51Build.Button(velvet, velvet);
            var press = UI51Build.GetOrAdd<UI51Press>(cushion); // niente pressione UIV2 (rimpicciolirebbe il cuscino sotto al mazzo)
            UI51Build.Wire(press, so => UI51Build.Float(so, "m_Scale", 1f));
            UI51Build.Shape(UI51Build.Stretch(UI51Build.Child(cushion, "Sheen"), 2f, 2f, 2f, 2f),
                UI51Shape.Linear((new Color(1f, 1f, 1f, 0.15f), 0f), (new Color(1f, 1f, 1f, 0f), 0.2f)), 180f, UI51Tokens.Radii(18f), 0f, Color.clear);
            // Cucitura tratteggiata al .6 del mockup: continua a meta' intensita'.
            UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(cushion, "Stitch"), 6f, 6f, 6f, 6f), Color.clear, 15f, 1f,
                new Color(252f / 255f, 226f / 255f, 154f / 255f, 0.33f));
            var studs = new[] { new Vector2(-3f, -3f), new Vector2(66f, -3f), new Vector2(-3f, 58f), new Vector2(66f, 58f) };
            for (int i = 0; i < studs.Length; i++)
                UI51Build.Shape(Box(UI51Build.Child(cushion, "Stud" + i), studs[i].x, studs[i].y, 9f, 9f),
                    AvatarFrame.FrameFill(FrameStyle.Oro), 110f, UI51Tokens.Radii(4.5f), 0f, Color.clear);

            // Mazzo: un dorso intero e sotto quattro bordi da 1 (crema e verde alternati), ruotato di -8 gradi sul centro del cuscino.
            var stack = Box(UI51Build.Child(rt, "Stack"), 19f, 7f, 34f, 50f, -8f);
            for (int n = 4; n >= 1; n--)
            {
                var edge = UI51Build.Stretch(UI51Build.Child(stack, "Edge" + n));
                edge.anchoredPosition = new Vector2(n, -n);
                if (n == 4) UI51Build.Solid(edge, UI51Tokens.Hex("#0E4F3A"), 4f, 0f, default, false, new UI51Shadow(0f, 2f, 8f, UI51Tokens.BlackA(0.5f)));
                else UI51Build.Solid(edge, n % 2 == 0 ? UI51Tokens.Hex("#0E4F3A") : UI51Tokens.Hex("#EFE2C2"), 4f);
            }
            var back = UI51Build.Image(UI51Build.Stretch(UI51Build.Child(stack, "Back")), cardBack, Color.white, false, false);

            // Medaglione delle carte rimaste (52, sul centro del mazzo): ombra sotto all'alone, come nel mockup.
            var medal = Box(UI51Build.Child(rt, "Count"), 10f, 6f, 52f, 52f);
            UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(medal, "Shadow")), Color.clear, 26f, 0f, default, false,
                new UI51Shadow(0f, 8f, 16f, UI51Tokens.BlackA(0.5f)));
            UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(medal, "Halo"), -4f, -4f, -4f, -4f), UI51Tokens.GoldA(0.25f), 30f);
            UI51Build.Shape(UI51Build.Stretch(UI51Build.Child(medal, "Disc")), UI51Shape.Linear((UI51Tokens.GoldLight, 0f), (UI51Tokens.GoldDark, 1f)),
                180f, UI51Tokens.Radii(26f), 2f, UI51Tokens.BadgeRing);
            var column = UI51Build.Stretch(UI51Build.Child(medal, "Text"));
            UI51Build.Column(column, 0f, UI51Build.Pad(0, 0, 0, 0), TextAnchor.MiddleCenter);
            var value = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(column, "Value"), "0", FontFace.CinzelBold, 19f,
                UI51Tokens.OnGold, TextAlignmentOptions.Center));
            UI51Build.Layout(value, -1f, 19f);
            var label = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(column, "Label"), "RIMASTE", FontFace.NunitoExtraBold, 8f,
                UI51Tokens.WithAlpha(UI51Tokens.OnGold, 0.8f), TextAlignmentOptions.Center, 0.5f));
            UI51Build.Layout(label, -1f, 11f);

            var view = UI51Build.GetOrAdd<TableDeckView>(rt);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "m_Stack", stack);
                UI51Build.Ref(so, "m_Back", back);
                UI51Build.Ref(so, "m_Medal", medal);
                UI51Build.Ref(so, "m_Count", value);
            });
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, view.Tap);
            EditorUtility.SetDirty(button);
            medal.gameObject.SetActive(false); // UI51Build.Child lo riaccende: si vede solo al tocco
        }

        // --- S5: emoticon

        /// <summary>
        /// Mockup Partita. Emoji: stesso nodo e stesso Button di prima (lo usano TableActionButtonsController, GameSocialV2 e
        /// LocalSeatBottomShift), tondo 40 col fumetto crema. Scelta: fila fino a 3 dentro al mio banner al posto di nome e chip
        /// (GameSocialV2.QuickBar*), cosi' non copre ne' la mano ne' il gettone "M". Mia emoticon: nuvoletta 76 (+2 d'oro) che sale
        /// dal banner (UIAnim.EmoticonBubble, GameSocialV2.OwnFly); quelle degli altri vanno al posto del loro avatar (AvatarFrame).
        /// Le vecchie nuvolette restano in scena, spente o per i posti laterali dei 4 giocatori.
        /// </summary>
        static void BuildEmoticons(UnityEngine.SceneManagement.Scene scene, Transform seatLocal)
        {
            var social = UnityEngine.Object.FindObjectOfType<GameSocialV2>(true);
            var emoji = UI51Build.FindPath(scene, "TableActionButtons", "EmojiButton") as RectTransform;
            var holder = seatLocal.Find("UI51Banner") as RectTransform;
            var chat = UI51Build.Sprite("Common", "ic_chat_cream");
            var frames = UI51EmoticonSet.Frames(0);
            if (social == null || emoji == null || holder == null || chat == null || frames == null || frames.Length == 0)
            {
                Debug.LogError($"{Tag} Manca un pezzo delle emoticon (GameSocialV2, TableActionButtons/EmojiButton, Banner_Local/UI51Banner, " +
                               "ic_chat_cream, Resources/UI51/EmoticonSet). Emoticon non toccate.");
                return;
            }

            // Emoji come i tondi di S1: forma (anche area di tocco), Button e UI51Press sullo stesso nodo, in scala UI51Build.Unit (misure del
            // mockup). Il posto resta quello di BuildLayout: la scala del nodo non cambia la sua anchoredPosition.
            emoji.sizeDelta = new Vector2(40f, 40f);
            emoji.localScale = new Vector3(UI51Build.Unit, UI51Build.Unit, 1f);
            var disc = UI51Build.Solid(emoji, UI51Tokens.Rgba(8, 17, 34, 0.7f), 20f, 1f, UI51Tokens.GoldA(0.45f), true);
            UI51Build.Button(UI51Build.GetOrAdd<Button>(emoji), disc);
            UI51Build.GetOrAdd<UI51Press>(emoji);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(emoji, "UI51Icon"), 20f, 20f), chat, Color.white);

            // Scelta: righe 123..239 del mockup (banner da 60, bordo 1 e margine 6), 3 tondi da 36 a passo 40 allineati a destra.
            var picker = Box(UI51Build.Child(holder, "EmoPicker"), 63f, 7f, 116f, 36f);
            UI51Build.Row(picker, 4f, UI51Build.Pad(0, 0, 0, 0), TextAnchor.MiddleRight, false, false);
            var group = UI51Build.GetOrAdd<CanvasGroup>(picker);
            var slots = new Button[3];
            var icons = new Image[3];
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = UI51Build.Size(UI51Build.Child(picker, "Slot_" + i), 36f, 36f);
                var slotDisc = UI51Build.Solid(slot, UI51Tokens.WhiteA(0.08f), 18f, 0f, default, true);
                slots[i] = UI51Build.Button(slotDisc, slotDisc);
                UI51Build.GetOrAdd<UI51Press>(slot);
                // 36 e non 30: il viso disegnato occupa ~81% del riquadro, cosi' se ne vede ~29 come nel mockup.
                icons[i] = UI51Build.Image(UI51Build.Center(UI51Build.Child(slot, "Icon"), 36f, 36f),
                    i < social.Sprites.Length ? social.Sprites[i] : null, Color.white);
            }
            var hint = UI51Build.NoWrap(UI51Build.Text(Box(UI51Build.Child(picker, "Hint"), -10f, 0f, 126f, 36f),
                "Nessuna emoticon\nScegline in Collezione", FontFace.NunitoExtraBold, 9f, UI51Tokens.CreamA(0.7f), TextAlignmentOptions.Right));
            UI51Build.Layout(hint, ignore: true);
            hint.gameObject.SetActive(false);

            // Nuvoletta: centro a riposo (62, banner - 123), cioe' 2 a destra del bordo sinistro del banner; 76 + anello d'oro 2 = 80.
            var fly = Box(UI51Build.Child(holder, "EmoFly"), -38f, -138f, 80f, 80f);
            UI51Build.Shape(fly, UI51Shape.Solid(UI51Tokens.CreamA(0.96f)), 180f, new Vector4(26f, 26f, 26f, 8f), 2f, UI51Tokens.Gold, false,
                new UI51Shadow(0f, 10f, 22f, UI51Tokens.BlackA(0.5f)));
            var flyGroup = UI51Build.GetOrAdd<CanvasGroup>(fly);
            flyGroup.blocksRaycasts = false; // si tocca la mano sotto
            flyGroup.interactable = false;
            var face = UI51Build.Image(UI51Build.Center(UI51Build.Child(fly, "Face"), 72f, 72f), frames[0], Color.white);
            var player = UI51Build.GetOrAdd<EmoticonPlayer>(face);

            UI51Build.Wire(social, so =>
            {
                UI51Build.Ref(so, "QuickBar", group);
                UI51Build.Ref(so, "QuickBarAnchor", emoji);
                UI51Build.Ref(so, "QuickHint", hint);
                var s = so.FindProperty("QuickSlots");
                var ic = so.FindProperty("QuickIcons");
                s.arraySize = ic.arraySize = slots.Length;
                for (int i = 0; i < slots.Length; i++)
                {
                    s.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
                    ic.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
                }
                UI51Build.Ref(so, "OwnFly", fly);
                UI51Build.Ref(so, "OwnFlyFace", player);
            });
            // UI51Build.Child li riaccende: si vedono solo quando servono.
            picker.gameObject.SetActive(false);
            fly.gameObject.SetActive(false);
        }

        // --- S6: accuso

        /// <summary>
        /// Mockup Partita. ACCUSA: stesso nodo e stesso Button (TableActionButtonsController ne scrive scala e rotazione, lo sposta
        /// LocalSeatBottomShift), area di tocco 50 trasparente; il medaglione d'oro col pugno sta nel figlio UI51Accuso in scala UI51Build.Unit:
        /// ombra, anello che pulsa e faccia con riflesso mentre la finestra e' aperta. Anello del tempo 58 e pallino dei secondi a 45
        /// gradi restano (decisione: sempre visibili nella finestra). Avviso: chip del mockup fra mano e banner. Pugno al centro del
        /// tavolo: UI51AccusoReveal sotto AccusoImpact (AccusoImpactV2.Parts, UIAnim.Accuso), posato da GameSocialV2 sul tavolo.
        /// La vecchia grafica (Icon, Caption, Fill, Number, Ring, Text) resta in scena spenta.
        /// </summary>
        static void BuildAccuso(UnityEngine.SceneManagement.Scene scene)
        {
            var controller = UnityEngine.Object.FindObjectOfType<TableActionButtonsController>(true);
            var impact = UnityEngine.Object.FindObjectOfType<AccusoImpactV2>(true);
            var social = UnityEngine.Object.FindObjectOfType<GameSocialV2>(true);
            var button = UI51Build.FindPath(scene, "TableActionButtons", "AccusoButton") as RectTransform;
            var ring = UI51Build.FindPath(scene, "TableActionButtons", "AccusoWindowRing");
            var badge = UI51Build.FindPath(scene, "TableActionButtons", "AccusoWindowBadge") as RectTransform;
            var prompt = UI51Build.FindPath(scene, "TableActionButtons", "AccusoWindowPrompt") as RectTransform;
            var fist = UI51Build.Sprite("Common", "pugno");
            var glowSprite = UI51Build.Sprite("Common", "Bagliore_morbido");
            var dimSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UIV2/Art/Generated/glow_soft_pill.png");
            var arc = ring != null ? ring.GetComponent<Project51.UIV2.Components.RingArcGraphic>() : null;
            var hitArea = button != null ? button.GetComponent<Image>() : null;
            if (controller == null || impact == null || social == null || button == null || hitArea == null || arc == null ||
                badge == null || prompt == null || fist == null || glowSprite == null || dimSprite == null)
            {
                Debug.LogError($"{Tag} Manca un pezzo dell'accuso (TableActionButtonsController, AccusoImpactV2, GameSocialV2, " +
                               "TableActionButtons/AccusoButton con Image, AccusoWindowRing, AccusoWindowBadge, AccusoWindowPrompt, " +
                               "pugno, Bagliore_morbido, UIV2/Art/Generated/glow_soft_pill). Accuso non toccato.");
                return;
            }

            // ACCUSA: il nodo resta a scala 1 (la scrive il controller); l'Image trasparente e' l'area di tocco.
            hitArea.sprite = null;
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;
            hitArea.canvasRenderer.cullTransparentMesh = false; // trasparente ma deve prendere i tocchi
            button.sizeDelta = new Vector2(50f, 50f) * UI51Build.Unit;
            UI51Build.Button(button.GetComponent<Button>(), hitArea);
            UI51Build.GetOrAdd<UI51Press>(button);
            var look = UI51Build.Center(UI51Build.Child(button, "UI51Accuso"), 50f, 50f);
            look.localScale = new Vector3(UI51Build.Unit, UI51Build.Unit, 1f);
            // Box-shadow del mockup: l'anello d'oro (primo) sopra l'ombra nera.
            UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(look, "Shadow")), Color.clear, 25f, 0f, default, false,
                new UI51Shadow(0f, 6f, 14f, UI51Tokens.BlackA(0.5f)));
            var pulse = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(look, "Pulse")), Color.white, 25f);
            pulse.color = UI51Tokens.WithAlpha(UI51Tokens.Gold, 0f); // UIAnim.Pulse anima la tinta
            var face = UI51Build.Shape(UI51Build.Stretch(UI51Build.Child(look, "Face")), UI51Tokens.GoldButtonFill(), 180f,
                UI51Tokens.Radii(25f), 1f, UI51Tokens.WhiteA(0.55f));
            UI51Build.GetOrAdd<Mask>(face).showMaskGraphic = true;
            // Riflesso: striscia 40% inclinata (la rotazione fa le veci dello skewX), scorre con UIAnim.Shine.
            var shine = UI51Build.Place(UI51Build.Child(face.rectTransform, "Shine"), new Vector2(0f, 0.5f), new Vector2(20f, 60f), Vector2.zero);
            shine.localRotation = Quaternion.Euler(0f, 0f, -20f);
            UI51Build.Shape(shine, UI51Shape.Linear((UI51Tokens.WhiteA(0f), 0f), (UI51Tokens.WhiteA(0.7f), 0.5f), (UI51Tokens.WhiteA(0f), 1f)),
                90f, Vector4.zero, 0f, default);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(face.rectTransform, "Icon"), 31f, 32f), fist, Color.white);

            // Anello del tempo appena fuori dal medaglione e secondi sul suo bordo (in BuildLayout, a 45 gradi).
            ((RectTransform)ring).sizeDelta = new Vector2(58f, 58f) * UI51Build.Unit;
            arc.Thickness = 3f * UI51Build.Unit;
            arc.TrackColor = UI51Tokens.Rgba(8, 17, 34, 0.4f);
            arc.TailColor = UI51Tokens.GoldDark;
            arc.HeadColor = UI51Tokens.GoldLight;
            EditorUtility.SetDirty(arc);
            var badgeImage = badge.GetComponent<Image>();
            if (badgeImage != null) badgeImage.enabled = false;
            var dot = UI51Build.Center(UI51Build.Child(badge, "UI51Badge"), 20f, 20f);
            dot.localScale = new Vector3(UI51Build.Unit, UI51Build.Unit, 1f);
            UI51Build.Solid(dot, UI51Tokens.Rgba(8, 17, 34, 0.92f), 10f, 1.5f, UI51Tokens.Gold);
            var seconds = UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(dot, "Value")), "5",
                FontFace.NunitoExtraBold, 11f, UI51Tokens.Cream, TextAlignmentOptions.Center));

            // Avviso: chip del mockup (.chip) che si allarga col testo; alpha e visibilita' restano al controller.
            var chip = UI51Build.Center(UI51Build.Child(prompt, "UI51Prompt"), 0f, 24f);
            chip.localScale = new Vector3(UI51Build.Unit, UI51Build.Unit, 1f);
            UI51Build.Solid(chip, UI51Tokens.BlackA(0.45f), 12f, 1f, UI51Tokens.GoldA(0.35f));
            UI51Build.Row(chip, 0f, UI51Build.Pad(0, 12, 0, 12), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            UI51Build.Fit(chip, true, false);
            var ask = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(chip, "Text"), "Hai un accuso? Tocca il <color=#F3C969>pugno</color>",
                FontFace.NunitoExtraBold, 12f, UI51Tokens.Cream, TextAlignmentOptions.Center));
            ask.richText = true;

            // Pugno al centro del tavolo: riquadro 390x320 del mockup, 4 sopra il centro del tavolo. Anchor e' posato a runtime.
            var reveal = UI51Build.Stretch(UI51Build.Child(impact.transform, "UI51AccusoReveal"));
            var revealGroup = UI51Build.GetOrAdd<CanvasGroup>(reveal);
            revealGroup.blocksRaycasts = false;
            revealGroup.interactable = false;
            var anchor = UI51Build.Place(UI51Build.Child(reveal, "Anchor"), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            anchor.localScale = new Vector3(UI51Build.Unit, UI51Build.Unit, 1f); // ripiego: centro dello schermo, misure del mockup
            var box = UI51Build.Place(UI51Build.Child(anchor, "Box"), new Vector2(0.5f, 0.5f), new Vector2(390f, 320f), new Vector2(0f, 4f));
            var dim = UI51Build.Image(UI51Build.Center(UI51Build.Child(box, "Dim"), 446f, 340f), dimSprite, UI51Tokens.Rgba(3, 8, 18, 0.82f), false, false);
            UI51Build.GetOrAdd<CanvasGroup>(dim);
            var hit = Box(UI51Build.Child(box, "Hit"), 195f, 140.8f, 0f, 0f);
            var ring1 = UI51Build.Solid(UI51Build.Center(UI51Build.Child(hit, "Ring1"), 150f, 150f), Color.clear, 75f, 3f, UI51Tokens.Rgba(252, 226, 154, 0.9f));
            var ring2 = UI51Build.Solid(UI51Build.Center(UI51Build.Child(hit, "Ring2"), 150f, 150f), Color.clear, 75f, 2f, UI51Tokens.Rgba(252, 226, 154, 0.7f));
            var glow = UI51Build.Image(UI51Build.Center(UI51Build.Child(hit, "Glow"), 280f, 280f), glowSprite, Color.white);
            var punch = UI51Build.Image(UI51Build.Center(UI51Build.Child(hit, "Fist"), 112f, 116f), fist, Color.white);
            foreach (var g in new Graphic[] { ring1, ring2, glow, punch }) UI51Build.GetOrAdd<CanvasGroup>(g);
            var text = UI51Build.Place(UI51Build.Child(box, "Text"), new Vector2(0f, 1f), new Vector2(390f, 200f), new Vector2(195f, -220.8f));
            text.pivot = new Vector2(0.5f, 1f);
            UI51Build.GetOrAdd<CanvasGroup>(text);
            // Titolo con l'ombra netta del mockup (0 3px 0 #6B4418); l'ombra sfumata non c'e'.
            var titleShadow = UI51Build.NoWrap(UI51Build.Text(TopLine(text, "TitleShadow", 3f, 38f), "ACCUSO",
                FontFace.CinzelBold, 28f, UI51Tokens.Hex("#6B4418"), TextAlignmentOptions.Center, 4f));
            var title = UI51Build.NoWrap(UI51Build.Text(TopLine(text, "Title", 0f, 38f), "ACCUSO",
                FontFace.CinzelBold, 28f, UI51Tokens.GoldLight, TextAlignmentOptions.Center, 4f));
            var who = UI51Build.NoWrap(UI51Build.Text(TopLine(text, "Who", 40f, 18f), "Tu · <color=#FCE29A>+3</color>",
                FontFace.NunitoExtraBold, 13f, UI51Tokens.Cream, TextAlignmentOptions.Center));
            who.richText = true;
            // Carte accusate (4 giocatori, accuso di un altro): 76x118 a passo 86, bordo d'oro staccato 2 e bagliore.
            var cards = UI51Build.Place(UI51Build.Child(text, "Cards"), new Vector2(0.5f, 1f), new Vector2(248f, 118f), new Vector2(0f, -74f));
            cards.pivot = new Vector2(0.5f, 1f);
            var faces = new Image[3];
            for (int i = 0; i < faces.Length; i++)
            {
                var card = Box(UI51Build.Child(cards, "Card" + i), i * 86f, 0f, 76f, 118f);
                UI51Build.Shape(UI51Build.Stretch(UI51Build.Child(card, "Frame"), -4f, -4f, -4f, -4f), UI51Shape.Solid(Color.clear), 180f,
                    UI51Tokens.Radii(11f), 2f, UI51Tokens.Gold, false,
                    new UI51Shadow(0f, 0f, 18f, UI51Tokens.GoldA(0.55f)), new UI51Shadow(0f, 8f, 16f, UI51Tokens.BlackA(0.5f)));
                faces[i] = UI51Build.Image(UI51Build.Stretch(UI51Build.Child(card, "Face")), null, Color.white, false, false);
            }

            UI51Build.Wire(controller, so =>
            {
                UI51Build.Ref(so, "accusoCountdownText", seconds);
                UI51Build.Ref(so, "accusoPulse", pulse);
                UI51Build.Ref(so, "accusoShine", shine);
            });
            UI51Build.Wire(impact, so =>
            {
                UI51Build.Ref(so, "Parts.root", reveal);
                UI51Build.Ref(so, "Parts.dim", dim.rectTransform);
                UI51Build.Ref(so, "Parts.fist", punch.rectTransform);
                UI51Build.Ref(so, "Parts.ring1", ring1.rectTransform);
                UI51Build.Ref(so, "Parts.ring2", ring2.rectTransform);
                UI51Build.Ref(so, "Parts.glow", glow.rectTransform);
                UI51Build.Ref(so, "Parts.text", text);
                UI51Build.Ref(so, "Parts.table", null); // trema il tavolo del mondo (GameSocialV2)
                UI51Build.Ref(so, "Caption", title);
                UI51Build.Ref(so, "CaptionShadow", titleShadow);
                UI51Build.Ref(so, "Who", who);
                UI51Build.Ref(so, "Anchor", anchor);
                UI51Build.Ref(so, "Hit", hit);
                UI51Build.Ref(so, "CardsRow", cards);
            });
            UI51Build.Wire(social, so =>
            {
                var list = so.FindProperty("AccusoCards");
                list.arraySize = faces.Length;
                for (int i = 0; i < faces.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = faces[i];
            });
            // UI51Build.Child li riaccende: si vedono solo quando servono.
            shine.gameObject.SetActive(false);
            cards.gameObject.SetActive(false);
            reveal.gameObject.SetActive(false);
        }

        // --- S7: scope

        /// <summary>
        /// Mockup Partita. Tocco sulle scope (mie o del posto in alto): velo scuro, titolo, ventaglio delle carte e due chip
        /// (prese, scope); si chiude toccando ovunque. Le aree di tocco sono trasparenti dentro a Scope dei banner, quindi dietro
        /// a banner e scelta emoticon, che vincono dove si sovrappongono. Il visore e' l'ultimo figlio di GameCanvas: sopra
        /// banner e pulsanti, sotto scelta della presa, pugno e risultati. Dati, apertura e chiusura in PlayerBannerManager.
        /// </summary>
        static void BuildScope(PlayerBannerManager manager, Transform seatLocal, Transform seatTop, Transform seatLeft, Transform seatRight,
            Transform canvas)
        {
            var own = seatLocal.Find("UI51Banner/Scope") as RectTransform;
            var top = seatTop.Find("UI51Banner/Scope") as RectTransform;
            var left = seatLeft.Find("UI51Banner/Scope") as RectTransform;
            var right = seatRight.Find("UI51Banner/Scope") as RectTransform;
            if (own == null || top == null || left == null || right == null)
            {
                Debug.LogError($"{Tag} Manca Scope nei banner UI51 (Banner_Local, Banner_Top, Banner_Left, Banner_Right, S2). Visore delle scope non toccato.");
                return;
            }

            // Mia: da 180 (prima c'e' la scelta emoticon) a 266, carte e "+N". In alto: carte, "+N" e meta' alta del banner,
            // 4 sotto la pillola del punteggio. Ai lati (Fase 6): le carte coricate fino alla quarta e 30 dentro al banner.
            var hits = new[]
            {
                ScopeHit(own, manager, 0, 180f, 0f, 86f, 50f), ScopeHit(left, manager, 1, -14f, 22f, 44f, 72f),
                ScopeHit(top, manager, 2, 44f, -24f, 87f, 49f), ScopeHit(right, manager, 3, 34f, 22f, 44f, 72f),
            };

            var viewer = UI51Build.Stretch(UI51Build.Child(canvas, "UI51ScopeViewer"));
            viewer.SetAsLastSibling();
            UI51Build.GetOrAdd<CanvasGroup>(viewer); // dissolvenza d'apertura (PlayerBannerManager)
            // Il nome "Backdrop" tiene lontane pressione e vibrazione di UIV2MotionInstaller.
            var backdrop = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(viewer, "Backdrop")), UI51Tokens.Rgba(3, 8, 18, 0.55f), 0f, 0f,
                default, true);
            var close = UI51Build.Button(backdrop, backdrop);
            for (int i = close.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(close.onClick, i);
            UnityEventTools.AddPersistentListener(close.onClick, manager.CloseScope);
            EditorUtility.SetDirty(close);

            // Blocco del mockup appeso 172 sopra il centro dello schermo: su iPhone 12 il titolo cade a 250 come nel mockup,
            // su SE alla stessa distanza dal centro del tavolo. Niente raycast dentro: ogni tocco arriva al velo e chiude.
            var content = UI51Build.Place(UI51Build.Child(viewer, "Content"), new Vector2(0.5f, 0.5f), new Vector2(390f, 300.6f),
                new Vector2(0f, 172f * UI51Build.Unit));
            content.pivot = new Vector2(0.5f, 1f);
            content.localScale = new Vector3(UI51Build.Unit, UI51Build.Unit, 1f);
            var title = UI51Build.Text(Box(UI51Build.Child(content, "Title"), 20f, -0.7f, 350f, 27f), "Le tue scope", FontFace.CinzelBold, 19f,
                UI51Tokens.Cream, TextAlignmentOptions.Center);
            title.enableWordWrapping = false; // nomi lunghi: "..." in fondo
            var fan = Box(UI51Build.Child(content, "Fan"), 30f, 43.6f, 330f, 190f);
            // Carta a riposo al centro, pivot al 90% dell'altezza (transform-origin del mockup). Le altre sono sue copie a runtime.
            var card = UI51Build.Place(UI51Build.Child(fan, "Card0"), new Vector2(0f, 1f), new Vector2(96f, 149f), new Vector2(165f, -144.1f));
            card.pivot = new Vector2(0.5f, 0.1f);
            UI51Build.Solid(card, Color.white, 7f, 0f, default, false, new UI51Shadow(0f, 3f, 8f, UI51Tokens.BlackA(0.45f)));
            UI51Build.GetOrAdd<CanvasGroup>(card);
            var face = UI51Build.Image(UI51Build.Stretch(UI51Build.Child(card, "Face")), null, Color.white, false, false);
            // Matta trasformata (carte accusate, nostra aggiunta): bordo d'oro, anello che pulsa verso fuori, cartellino "MATTA".
            // Spento: PlayerBannerManager lo accende sulla matta dopo che si e' girata.
            var matta = UI51Build.Stretch(UI51Build.Child(card, "Matta"));
            UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(matta, "Frame"), -2f, -2f, -2f, -2f), Color.clear, 9f, 2.5f, UI51Tokens.Gold);
            UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(matta, "Pulse"), -2f, -2f, -2f, -2f), Color.clear, 9f, 2f, UI51Tokens.GoldLight);
            var tag = UI51Build.Place(UI51Build.Child(matta, "Tag"), new Vector2(0.5f, 1f), new Vector2(58f, 18f), new Vector2(0f, 9f));
            UI51Build.Solid(tag, UI51Tokens.Gold, 9f, 1f, UI51Tokens.GoldLight, false, new UI51Shadow(0f, 2f, 4f, UI51Tokens.BlackA(0.4f)));
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(tag, "Text")), "MATTA", FontFace.CinzelBold, 10f,
                UI51Tokens.OnGold, TextAlignmentOptions.Center));
            matta.gameObject.SetActive(false);
            var chips = UI51Build.Place(UI51Build.Child(content, "Chips"), new Vector2(0.5f, 1f), new Vector2(0f, 28f), new Vector2(0f, -243.6f));
            UI51Build.Row(chips, 10f, UI51Build.Pad(0, 0, 0, 0), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            UI51Build.Fit(chips, true, false);
            var captures = Chip(chips, "ChipA", UI51Tokens.Cream);
            var count = Chip(chips, "ChipB", UI51Tokens.Gold);
            UI51Build.NoWrap(UI51Build.Text(TopLine(content, "Hint", 285.1f, 16f), "Tocca ovunque per chiudere", FontFace.NunitoRegular,
                11f, UI51Tokens.CreamA(0.55f), TextAlignmentOptions.Center));

            UI51Build.Wire(manager, so =>
            {
                UI51Build.Ref(so, "scopeViewer", viewer.gameObject);
                UI51Build.Ref(so, "scopeTitle", title);
                UI51Build.Ref(so, "scopeCaptures", captures);
                UI51Build.Ref(so, "scopeCount", count);
                UI51Build.Ref(so, "scopeFace", face);
                var list = so.FindProperty("scopeHits");
                list.arraySize = hits.Length;
                for (int i = 0; i < hits.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = hits[i];
            });
            // UI51Build.Child li riaccende: si vedono solo al tocco.
            card.gameObject.SetActive(false);
            viewer.gameObject.SetActive(false);
        }

        // --- S8: scelta della presa

        /// <summary>
        /// Mockup Partita (vassoio "Scegli la presa"). Sotto MoveSelectionUI (canvas 550): UI51CaptureTray con Marks (anelli e numeri
        /// sulle carte del tavolo, template UI51Mark) e Sheet dal basso in scala UI51Build.Unit (carta giocata, freccia, titolo, X, divisore,
        /// fila delle prese che scorre: template Choice). Altezza, riempimento e animazione a runtime in MoveSelectionUI.ShowTray.
        /// Il vecchio pannello V2 resta come ripiego per i chiamanti solo testo.
        /// </summary>
        static void BuildCapture(Project51.Unity.MoveSelectionUI ui, Transform seatLocal)
        {
            var closeIcon = UI51Build.Sprite("Common", "ic_close_cream");
            var coin = UI51Build.Sprite("Common", "ic_coin");
            if (ui == null || closeIcon == null || coin == null)
            {
                Debug.LogError($"{Tag} Manca un pezzo della scelta della presa (MoveSelectionUI in GameCanvas, ic_close_cream, ic_coin). Non toccata.");
                return;
            }
            var colors = Project51.Unity.MoveSelectionUI.OptionColors;

            // Radice stesa come a runtime (EnsureBuilt) e senza raycast: sopra il tavolo passano i tocchi a HUD, banner in alto e mano.
            var root = UI51Build.Stretch((RectTransform)ui.transform);
            var rootImage = root.GetComponent<Image>();
            if (rootImage != null) rootImage.raycastTarget = false;
            var tray = UI51Build.Stretch(UI51Build.Child(root, "UI51CaptureTray"));
            UI51Build.GetOrAdd<CanvasGroup>(tray);

            // Anelli sulle carte del tavolo: il centro di Marks e' l'origine, LateUpdate posa ogni copia sui bounds della carta.
            var marks = UI51Build.Place(UI51Build.Child(tray, "Marks"), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            marks.localScale = new Vector3(UI51Build.Unit, UI51Build.Unit, 1f);
            var mark = UI51Build.Place(UI51Build.Child(marks, "UI51Mark"), new Vector2(0.5f, 0.5f), new Vector2(70f, 108f), Vector2.zero);
            Graphic firstRing = null;
            for (int f = 0; f < colors.Length; f++)
            {
                // Bordo sul filo della carta (non staccato 2 come nel mockup: con x1.12 le carte vicine si toccherebbero).
                var ring = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(mark, "Ring" + f)), Color.clear, 7f, 2f, colors[f], true,
                    new UI51Shadow(0f, 0f, 18f, UI51Tokens.WithAlpha(colors[f], 0.6f)));
                if (firstRing == null) firstRing = ring;
                ring.gameObject.SetActive(f == 0);
            }
            UI51Build.Button(mark, firstRing);
            UI51Build.Wire(UI51Build.GetOrAdd<UI51Press>(mark), so => UI51Build.Float(so, "m_Scale", 1f)); // la carta non si rimpicciolisce
            // Numeri dentro la carta in alto a destra (nel mockup sporgono di 9: coprirebbero la carta vicina), uno per presa.
            var badges = UI51Build.Place(UI51Build.Child(mark, "Badges"), Vector2.one, new Vector2(20f, 0f), new Vector2(-3f, -3f));
            UI51Build.Column(badges, 2f, UI51Build.Pad(0, 0, 0, 0), TextAnchor.UpperRight, false, false);
            var markBadge = UI51Build.Size(UI51Build.Child(badges, "Badge"), 20f, 20f);
            UI51Build.Solid(markBadge, Color.white, 10f, 2f, UI51Tokens.BadgeRing); // la tinta (colore della presa) la da' il runtime
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(markBadge, "Value")), "1", FontFace.CinzelBold, 10f,
                UI51Tokens.BadgeRing, TextAlignmentOptions.Center));

            // Vassoio: bordo alto d'oro su fondo scuro (CSS border-top sopra lo sfondo), riempimento che prende i tocchi.
            var sheet = UI51Build.Place(UI51Build.Child(tray, "Sheet"), new Vector2(0.5f, 0f), new Vector2(390f, 241f), Vector2.zero);
            sheet.localScale = new Vector3(UI51Build.Unit, UI51Build.Unit, 1f);
            UI51Build.Shape(sheet, UI51Shape.Solid(Color.Lerp(UI51Tokens.Rgba(12, 26, 50, 1f), UI51Tokens.Gold, 0.5f)), 180f,
                UI51Tokens.RadiiTop(24f), 0f, default, false, new UI51Shadow(0f, -14f, 34f, UI51Tokens.BlackA(0.55f)));
            UI51Build.Shape(UI51Build.Stretch(UI51Build.Child(sheet, "Fill"), 0f, 0f, 0f, 1f), UI51Tokens.SheetFill(), 180f,
                UI51Tokens.RadiiTop(24f), 0f, default, true);

            // Intestazione (alta 46, centro 36 sotto il bordo): carta giocata con contorno, freccia, titolo, X.
            UI51Build.Solid(Box(UI51Build.Child(sheet, "PlayedOutline"), 15.5f, 10.5f, 35f, 51f), Color.clear, 9.5f, 1.5f, UI51Tokens.Gold);
            var played = Box(UI51Build.Child(sheet, "PlayedCard"), 18f, 13f, 30f, 46f);
            UI51Build.Solid(played, Color.white, 7f, 0f, default, false, new UI51Shadow(0f, 3f, 8f, UI51Tokens.BlackA(0.45f)));
            var playedFace = UI51Build.Image(UI51Build.Stretch(UI51Build.Child(played, "Face")), null, Color.white, false, false);
            // Freccia dell'SVG del mockup (M1 6h14 M11 1l5 5-5 5, tratto 2 arrotondato): tre barrette.
            var arrow = Box(UI51Build.Child(sheet, "Arrow"), 58f, 30f, 18f, 12f);
            UI51Build.Solid(Box(UI51Build.Child(arrow, "Shaft"), 0f, 5f, 16f, 2f), UI51Tokens.Gold, 1f);
            UI51Build.Solid(Box(UI51Build.Child(arrow, "HeadUp"), 8.95f, 2.5f, 9.1f, 2f, 45f), UI51Tokens.Gold, 1f);
            UI51Build.Solid(Box(UI51Build.Child(arrow, "HeadDown"), 8.95f, 7.5f, 9.1f, 2f, -45f), UI51Tokens.Gold, 1f);
            UI51Build.NoWrap(UI51Build.Text(Box(UI51Build.Child(sheet, "Title"), 86f, 13f, 246f, 46f), "SCEGLI LA PRESA",
                FontFace.CinzelBold, 13f, UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft, 2f));
            // X: unica uscita oltre alla scelta, area di tocco 44 (30 + 7 per lato). Il nome "Close" da' il suono UiBack.
            var close = Box(UI51Build.Child(sheet, "Close"), 342f, 21f, 30f, 30f);
            var closeDisc = UI51Build.Solid(close, UI51Tokens.WhiteA(0.06f), 15f, 0f, default, true);
            closeDisc.raycastPadding = new Vector4(-7f, -7f, -7f, -7f);
            var closeButton = UI51Build.Button(close, closeDisc);
            UI51Build.GetOrAdd<UI51Press>(close);
            for (int i = closeButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(closeButton.onClick, i);
            UnityEventTools.AddPersistentListener(closeButton.onClick, ui.Cancel);
            EditorUtility.SetDirty(closeButton);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(close, "Icon"), 12.6f, 12.6f), closeIcon, Color.white);
            UI51Build.Shape(Box(UI51Build.Child(sheet, "Divider"), 18f, 69f, 354f, 1f),
                UI51Shape.Linear((UI51Tokens.GoldA(0f), 0f), (UI51Tokens.GoldA(0.5f), 0.5f), (UI51Tokens.GoldA(0f), 1f)), 90f, Vector4.zero, 0f, default);

            // Fila delle prese (alta 135 da 86): una riga, scala fino al 75% e poi scorre col dito (scelta dell'utente, 30/09).
            // La maschera lascia 16 attorno per bagliore e numeri; lo sfondo trasparente prende il trascinamento fra le prese.
            var viewport = Box(UI51Build.Child(sheet, "Viewport"), 0f, 70f, 390f, 167f);
            var viewportHit = UI51Build.Image(viewport, null, Color.clear, true, false);
            viewportHit.canvasRenderer.cullTransparentMesh = false;
            UI51Build.GetOrAdd<RectMask2D>(viewport);
            var options = UI51Build.Place(UI51Build.Child(viewport, "Options"), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            UI51Build.Row(options, 14f, UI51Build.Pad(0, 14, 0, 14), TextAnchor.MiddleCenter, true, true);
            UI51Build.Fit(options, true, true);
            var scroll = UI51Build.GetOrAdd<ScrollRect>(viewport);
            scroll.viewport = viewport;
            scroll.content = options;
            scroll.horizontal = false; // lo accende ShowTray quando la fila non entra
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            EditorUtility.SetDirty(scroll);

            var choice = UI51Build.Child(options, "Choice"); // il nome da' il suono UiClick
            UI51Build.Column(choice, 9f, UI51Build.Pad(16, 14, 12, 14), TextAnchor.UpperCenter, true, true);
            Graphic firstFrame = null;
            for (int f = 0; f < colors.Length; f++)
            {
                var frame = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(choice, "Frame" + f)), UI51Tokens.WhiteA(0.04f), 18f, 2f, colors[f], true,
                    new UI51Shadow(0f, 0f, 16f, UI51Tokens.WithAlpha(colors[f], 0.33f)));
                UI51Build.Layout(frame, ignore: true);
                if (firstFrame == null) firstFrame = frame;
                frame.gameObject.SetActive(f == 0);
            }
            UI51Build.Button(choice, firstFrame);
            UI51Build.GetOrAdd<UI51Press>(choice);
            var badge = UI51Build.Place(UI51Build.Child(choice, "Badge"), new Vector2(0.5f, 1f), new Vector2(24f, 24f), new Vector2(0f, 9f));
            UI51Build.Layout(badge, ignore: true);
            UI51Build.Solid(badge, Color.white, 12f, 2f, UI51Tokens.BadgeRing);
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(badge, "Value")), "1", FontFace.CinzelBold, 11f,
                UI51Tokens.BadgeRing, TextAlignmentOptions.Center));
            var cards = UI51Build.Child(choice, "Cards");
            UI51Build.Row(cards, 6f, UI51Build.Pad(0, 0, 0, 0), TextAnchor.MiddleCenter, true, true);
            var card = UI51Build.Child(cards, "Card");
            UI51Build.Layout(card, 50f, 78f);
            UI51Build.Solid(card, Color.white, 7f, 0f, default, false, new UI51Shadow(0f, 3f, 8f, UI51Tokens.BlackA(0.45f)));
            UI51Build.Image(UI51Build.Stretch(UI51Build.Child(card, "Face")), null, Color.white, false, false);
            // Chip alte 20: "+N" col dorso del mazzo, denari con la moneta, SCOPA. Le immagini tengono le proporzioni nei 20.
            var chips = UI51Build.Child(choice, "Chips");
            UI51Build.Row(chips, 5f, UI51Build.Pad(0, 0, 0, 0), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            UI51Build.Layout(chips, -1f, 20f);
            var count = CaptureChip(chips, "Count", UI51Shape.Solid(UI51Tokens.WhiteA(0.07f)), 4f, 7, 5);
            UI51Build.Layout(UI51Build.Image(UI51Build.Child(count, "Back"), null, Color.white), 9f, -1f);
            CaptureChipText(count, "+2", FontFace.NunitoExtraBold, 11f, UI51Tokens.Cream);
            var denari = CaptureChip(chips, "Denari", UI51Shape.Solid(UI51Tokens.GoldA(0.12f)), 4f, 7, 4);
            UI51Build.Layout(UI51Build.Image(UI51Build.Child(denari, "Coin"), coin, Color.white), 12f, -1f);
            CaptureChipText(denari, "1", FontFace.NunitoExtraBold, 11f, UI51Tokens.Gold);
            var scopa = CaptureChip(chips, "Scopa", UI51Shape.Linear((UI51Tokens.GoldLight, 0f), (UI51Tokens.GoldDark, 1f)), 0f, 8, 8);
            CaptureChipText(scopa, "SCOPA", FontFace.CinzelBold, 10f, UI51Tokens.OnGold);

            UI51Build.Wire(ui, so =>
            {
                UI51Build.Ref(so, "ui51Tray", tray.GetComponent<CanvasGroup>());
                UI51Build.Ref(so, "traySheet", sheet);
                UI51Build.Ref(so, "trayAnchor", seatLocal);
                UI51Build.Ref(so, "trayScroll", scroll);
                UI51Build.Ref(so, "trayMarks", marks);
                UI51Build.Ref(so, "trayPlayedCard", playedFace);
            });
            // UI51Build.Child li riaccende: i modelli si copiano a runtime, il vassoio si vede solo quando serve.
            mark.gameObject.SetActive(false);
            choice.gameObject.SetActive(false);
            tray.gameObject.SetActive(false);
        }

        /// <summary>Chip alta 20 (raggio 10) della scelta della presa: padding destra/sinistra del mockup, figli alti quanto la chip.</summary>
        static RectTransform CaptureChip(RectTransform row, string name, Gradient fill, float gap, int padRight, int padLeft)
        {
            var chip = UI51Build.Child(row, name);
            UI51Build.Shape(chip, fill, 180f, UI51Tokens.Radii(10f), 0f, default);
            UI51Build.Row(chip, gap, UI51Build.Pad(0, padRight, 0, padLeft), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            return chip;
        }

        static void CaptureChipText(RectTransform chip, string text, FontFace face, float size, Color color) =>
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(chip, "Text"), text, face, size, color, TextAlignmentOptions.Center));

        // --- S9: opzioni e abbandono

        /// <summary>
        /// Mockup Partita (opzioni). Dentro InGameSettings/Panel (canvas 600: sopra vassoio, risultati e pugno): velo sopra la foto
        /// sfocata che c'era gia' (Blur), foglio dal basso in scala UI51Build.Unit con Audio, Grafica (una sola "Grafica ridotta", come la Home:
        /// Bassa/Media/Alta del mockup non hanno un effetto vero), Partita (suggerimenti mosse, in piu' del mockup) e TORNA AL TAVOLO.
        /// Apertura, posto sopra la safe area e animazione in InGameSettingsV2.
        /// </summary>
        static void BuildOptions(InGameSettingsV2 settings)
        {
            var panel = settings.Panel;
            if (panel == null || UI51Build.Sprite("Common", "ic_close_cream") == null)
            {
                Debug.LogError($"{Tag} Manca un pezzo delle opzioni (InGameSettings/Panel, ic_close_cream). Non toccate.");
                return;
            }
            bool was = panel.activeSelf;
            panel.SetActive(true); // TMP su oggetti spenti lancia eccezioni

            var root = UI51Build.Stretch(UI51Build.Child(panel.transform, "UI51Options"));
            // Il nome "Backdrop" tiene lontane pressione e vibrazione di UIV2MotionInstaller; il tocco chiude (InGameSettingsV2.Awake).
            var backdrop = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(root, "Backdrop")), UI51Tokens.Rgba(3, 8, 18, 0.55f), 0f, 0f,
                default, true);
            var backdropButton = UI51Build.Button(backdrop, backdrop);

            var bottom = UI51Build.Place(UI51Build.Child(root, "Bottom"), new Vector2(0.5f, 0f), new Vector2(390f, 0f), Vector2.zero);
            bottom.localScale = new Vector3(UI51Build.Unit, UI51Build.Unit, 1f);
            // Padding 12 + 1 di bordo. Sheet disegna r26 e oro .3: il tavolo ha r24, oro .4, ombra verso l'alto; il foglio prende i
            // tocchi, cosi' non arrivano al velo.
            var sheet = UI51AccessBuilder.Sheet(bottom, UI51Build.Pad(13, 20, 24, 20), 14f, UI51Tokens.SheetFill());
            UI51Build.Shape(sheet, UI51Tokens.SheetFill(), 180f, UI51Tokens.RadiiTop(24f), 1f, UI51Tokens.GoldA(0.4f), true,
                new UI51Shadow(0f, -12f, 40f, UI51Tokens.BlackA(0.5f)));
            UI51HomeBuilder.SheetHeader(sheet, "Opzioni", "La partita continua mentre sei qui", out var close);

            var body = UI51Build.Child(sheet, "Body");
            UI51Build.Stack(body, 0f);
            var audio = UI51MetaBuilder.Panel(UI51MetaBuilder.Section(body, "Audio", "AUDIO", 8f), "Panel");
            var music = UI51MetaBuilder.Switch(UI51MetaBuilder.Row(audio, "Music", "Musica"));
            UI51MetaBuilder.Divider(audio, "Line1");
            var sfx = UI51MetaBuilder.Switch(UI51MetaBuilder.Row(audio, "Sfx", "Effetti sonori"));
            UI51MetaBuilder.Divider(audio, "Line2");
            var vibration = UI51MetaBuilder.Switch(UI51MetaBuilder.Row(audio, "Vibration", "Vibrazione"));
            UI51Build.Gap(body, "Gap1", 16f);
            var graphics = UI51MetaBuilder.Switch(UI51MetaBuilder.Row(UI51MetaBuilder.Panel(UI51MetaBuilder.Section(body, "Graphics", "GRAFICA", 8f),
                "Panel"), "Reduced", "Grafica ridotta", "Meno effetti e animazioni più brevi"));
            UI51Build.Gap(body, "Gap2", 16f);
            var hints = UI51MetaBuilder.Switch(UI51MetaBuilder.Row(UI51MetaBuilder.Panel(UI51MetaBuilder.Section(body, "Match", "PARTITA", 8f),
                "Panel"), "Hints", "Suggerimenti mosse", "Evidenzia le carte che fanno una presa"));
            UI51Build.Gap(body, "Gap3", 18f);
            var back = UI51Build.Child(body, "BackToTable");
            UI51PrefabBuilder.GoldBody(back.gameObject, 350f, 52f, 16f, FontFace.CinzelBold, 14f, 2f, "TORNA AL TAVOLO");
            UI51Build.Layout(back, -1f, 52f);
            Listen(back.GetComponent<Button>(), settings.Hide);

            UI51Build.Wire(settings, so =>
            {
                UI51Build.Ref(so, "Close", close);
                UI51Build.Ref(so, "Backdrop", backdropButton);
                UI51Build.Ref(so, "Sheet", sheet);
                UI51Build.Ref(so, "MusicSwitch", music);
                UI51Build.Ref(so, "EffectsSwitch", sfx);
                UI51Build.Ref(so, "VibrationSwitch", vibration);
                UI51Build.Ref(so, "GraphicsSwitch", graphics);
                UI51Build.Ref(so, "HintsSwitch", hints);
            });
            panel.SetActive(was);
        }

        /// <summary>
        /// Mockup Partita (abbandono): velo che vale "resta", scheda dal bordo rosso con porta, titolo, testo per formato
        /// (InGameSettingsV2.LeaveMessage), chip Sconfitta / Nessuna esperienza, RESTA AL TAVOLO e Abbandona. Ultimo figlio di
        /// InGameSettings: sopra anche le opzioni, fuori da Panel (IsOpen e foto sfocata non cambiano).
        /// </summary>
        static void BuildLeave(InGameSettingsV2 settings)
        {
            var exit = UI51Build.Sprite("Common", "ic_exit_cream");
            if (exit == null)
            {
                Debug.LogError($"{Tag} Manca ic_exit_cream. Finestra di abbandono non toccata (Abbandona apre le opzioni).");
                return;
            }
            Color danger = UI51Tokens.Danger;

            var dialog = UI51Build.Stretch(UI51Build.Child(settings.transform, "UI51LeaveDialog"));
            var backdrop = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(dialog, "Backdrop")), UI51Tokens.Rgba(3, 8, 18, 0.55f), 0f, 0f,
                default, true);
            Listen(UI51Build.Button(backdrop, backdrop), settings.CloseLeave);

            // Bordo alto 202 sopra il centro dello schermo (regola del visore S7): su iPhone 12 a 220 come nel mockup, su SE alla
            // stessa distanza dal centro. Pivot al centro per il pop; OpenLeave abbassa la scheda di meta' della sua altezza.
            var center = UI51Build.Place(UI51Build.Child(dialog, "Center"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 202f * UI51Build.Unit));
            center.localScale = new Vector3(UI51Build.Unit, UI51Build.Unit, 1f);
            var card = UI51Build.Place(UI51Build.Child(center, "Card"), new Vector2(0.5f, 0.5f), new Vector2(342f, 353f), new Vector2(0f, -176.5f));
            UI51Build.Shape(card, UI51Tokens.DialogFill(), 180f, UI51Tokens.Radii(UI51Tokens.RadiusDialog), 1f, UI51Tokens.WithAlpha(danger, 0.5f),
                true, UI51Tokens.ShadowDialog);
            UI51Build.Stack(card, 0f, UI51Build.Pad(23, 21, 21, 21)); // 22/20/20 + 1 di bordo: dentro restano i 300 del mockup
            UI51Build.Fit(card, false, true);

            var iconRow = UI51Build.Child(card, "IconRow");
            UI51Build.Layout(iconRow, -1f, 56f);
            var circle = UI51Build.Place(UI51Build.Child(iconRow, "IconCircle"), new Vector2(0.5f, 1f), new Vector2(56f, 56f), Vector2.zero);
            UI51Build.Solid(circle, UI51Tokens.WithAlpha(danger, 0.12f), 28f, 1f, UI51Tokens.WithAlpha(danger, 0.5f));
            UI51Build.Image(UI51Build.Center(UI51Build.Child(circle, "Icon"), 26f, 26f), exit, Color.white);
            UI51Build.Gap(card, "Gap1", 14f);
            var title = UI51Build.Child(card, "Title");
            UI51Build.Layout(title, -1f, 26f);
            UI51Build.NoWrap(UI51Build.Text(title, "Abbandonare la partita?", FontFace.CinzelBold, 19f, UI51Tokens.Cream,
                TextAlignmentOptions.Center));
            // Stacchi 10 e 14 del mockup + mezza interlinea sopra e sotto il testo (CSS la mette, TMP no).
            UI51Build.Gap(card, "Gap2", 10.9f);
            var body = UI51Build.Text(UI51Build.Child(card, "Body"), InGameSettingsV2.LeaveLost, FontFace.NunitoRegular, 13f,
                UI51Tokens.CreamA(0.65f), TextAlignmentOptions.Center);
            UI51Build.Wrap(body, 13.6f); // line-height 1.5
            UI51Build.Gap(card, "Gap3", 14.9f);
            var chips = UI51Build.Child(card, "Chips");
            UI51Build.Row(chips, 8f, UI51Build.Pad(0, 0, 0, 0), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            UI51Build.Layout(chips, -1f, 26f);
            Color chipFill = UI51Tokens.WithAlpha(danger, 0.12f), chipBorder = UI51Tokens.WithAlpha(danger, 0.4f);
            Chip(chips, "Defeat", UI51Tokens.DangerText, chipFill, chipBorder, 10, 11f).text = "Sconfitta";
            Chip(chips, "NoXp", UI51Tokens.DangerText, chipFill, chipBorder, 10, 11f).text = "Nessuna esperienza";
            UI51Build.Gap(card, "Gap4", 18f);
            var stay = UI51Build.Child(card, "Stay");
            UI51PrefabBuilder.GoldBody(stay.gameObject, 300f, 50f, 14f, FontFace.CinzelBold, 14f, 2f, "RESTA AL TAVOLO");
            UI51Build.Layout(stay, -1f, 50f);
            Listen(stay.GetComponent<Button>(), settings.CloseLeave);
            UI51Build.Gap(card, "Gap5", 10f);
            var leave = UI51Build.Child(card, "Leave"); // come DangerButton: fondo e bordo rossi tenui, testo rosa
            UI51PrefabBuilder.ButtonBody(leave.gameObject, 300f, 46f, UI51Shape.Solid(UI51Tokens.WithAlpha(danger, 0.08f)), UI51Tokens.Radii(14f),
                1f, UI51Tokens.WithAlpha(danger, 0.5f), FontFace.NunitoExtraBold, 14f, 0f, UI51Tokens.DangerText, "Abbandona");
            UI51Build.Layout(leave, -1f, 46f);
            Listen(leave.GetComponent<Button>(), settings.ConfirmLeave);

            UI51Build.Wire(settings, so =>
            {
                UI51Build.Ref(so, "LeaveDialog", dialog.gameObject);
                UI51Build.Ref(so, "LeaveCard", card);
                UI51Build.Ref(so, "LeaveBody", body);
            });
            dialog.gameObject.SetActive(false); // UI51Build.Child la riaccende: si vede solo da Abbandona
        }

        // --- Profilo rapido (01/10)

        /// <summary>
        /// Mockup Partita/Partita4 "profilo" e PartitaMioProfilo: GameCanvas/UI51QuickProfile (velo .25 che chiude, scheda 300 r22
        /// col banner del giocatore sotto un'ombra scura, intestazione 64, statistiche, medaglie, pulsanti o barra XP). Aree di
        /// tocco UI51ProfileHit come PRIMO figlio di ogni Banner_*/UI51Banner: dove si sovrappongono alle scope vince il visore.
        /// La mia e' 130x50 a sinistra (avatar e nome, mockup), e GameSocialV2 la spegne mentre la scelta emoticon e' aperta.
        /// </summary>
        static void BuildQuickProfile(PlayerBannerManager manager, Transform seatLocal, Transform seatTop, Transform seatLeft,
            Transform seatRight, Transform canvas)
        {
            var avatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI51PrefabBuilder.PrefabPath("AvatarFrame"));
            var closeIcon = UI51Build.Sprite("Common", "ic_close_cream");
            var social = UnityEngine.Object.FindObjectOfType<GameSocialV2>(true);
            var medalNames = new[] { "medal_trophy", "medal_sun", "medal_club", "medal_sword" };
            var medalSprites = new Sprite[medalNames.Length];
            for (int i = 0; i < medalNames.Length; i++) medalSprites[i] = UI51Build.Sprite("Common", medalNames[i]);
            var seats = new[] { seatLocal, seatLeft, seatTop, seatRight };
            if (avatarPrefab == null || closeIcon == null || social == null || System.Array.IndexOf(medalSprites, null) >= 0 ||
                System.Array.Exists(seats, t => t.Find("UI51Banner") == null))
            {
                Debug.LogError($"{Tag} Manca un pezzo del profilo rapido (prefab AvatarFrame, ic_close_cream, medal_*, GameSocialV2, " +
                               "Banner_*/UI51Banner). Non costruito.");
                return;
            }

            GameObject ownHit = null;
            for (int slot = 0; slot < seats.Length; slot++)
            {
                var holder = (RectTransform)seats[slot].Find("UI51Banner");
                var hitRt = UI51Build.Child(holder, "UI51ProfileHit");
                hitRt.SetAsFirstSibling();
                if (slot == 0) Box(hitRt, 0f, 0f, 130f, 50f);
                else UI51Build.Stretch(hitRt);
                var hit = UI51Build.Image(hitRt, null, Color.clear, true, false);
                hit.canvasRenderer.cullTransparentMesh = false; // trasparente ma deve prendere i tocchi
                var button = UI51Build.Button(hit, hit);
                var press = UI51Build.GetOrAdd<UI51Press>(hit);
                UI51Build.Wire(press, so => UI51Build.Float(so, "m_Scale", 1f));
                for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                    UnityEventTools.RemovePersistentListener(button.onClick, i);
                UnityEventTools.AddIntPersistentListener(button.onClick, manager.OpenProfile, slot);
                EditorUtility.SetDirty(button);
                if (slot == 0) ownHit = hitRt.gameObject;
            }

            var root = UI51Build.Stretch(UI51Build.Child(canvas, "UI51QuickProfile"));
            root.SetAsLastSibling();
            UI51Build.GetOrAdd<CanvasGroup>(root); // dissolvenza d'apertura
            // Il nome "Backdrop" tiene lontane pressione e vibrazione di UIV2MotionInstaller.
            var backdrop = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(root, "Backdrop")), UI51Tokens.Rgba(3, 8, 18, 0.25f), 0f, 0f,
                default, true);
            Listen(UI51Build.Button(backdrop, backdrop), manager.CloseProfile);

            // Bordo alto della scheda: QuickProfileCard lo mette a (422 - top del mockup) dal centro dello schermo, per posto.
            var content = UI51Build.Place(UI51Build.Child(root, "Content"), new Vector2(0.5f, 0.5f), new Vector2(300f, 0f),
                new Vector2(0f, 122f * UI51Build.Unit));
            content.pivot = new Vector2(0.5f, 1f);
            content.localScale = new Vector3(UI51Build.Unit, UI51Build.Unit, 1f);
            var card = UI51Build.Place(UI51Build.Child(content, "Card"), new Vector2(0.5f, 1f), new Vector2(300f, 0f), Vector2.zero);
            card.pivot = new Vector2(0.5f, 0.5f); // pop dal centro; la scheda cresce verso il basso dal bordo alto (Fit + pivot del Content)
            UI51Build.Shape(card, UI51Shape.Linear((UI51Tokens.Rgba(14, 28, 52, 0.98f), 0f), (UI51Tokens.Rgba(7, 14, 28, 0.99f), 1f)), 180f,
                UI51Tokens.Radii(22f), 1f, UI51Tokens.GoldA(0.5f), true, new UI51Shadow(0f, 20f, 44f, UI51Tokens.BlackA(0.6f)));
            UI51Build.Stack(card, 0f, UI51Build.Pad(16, 16, 16, 16));
            UI51Build.Fit(card, false, true);
            var bannerRt = UI51Build.Stretch(UI51Build.Child(card, "Banner"), 1f, 1f, 1f, 1f);
            UI51Build.Layout(bannerRt, -1f, -1f, -1f, -1f, true);
            var banner = UI51Build.Solid(bannerRt, Color.white, 21f);
            var shade = UI51Build.Stretch(UI51Build.Child(card, "Shade"), 1f, 1f, 1f, 1f);
            UI51Build.Layout(shade, -1f, -1f, -1f, -1f, true);
            UI51Build.Shape(shade, UI51Shape.Linear((UI51Tokens.Rgba(6, 13, 27, 0.05f), 0f), (UI51Tokens.Rgba(6, 13, 27, 0.35f), 0.45f),
                (UI51Tokens.Rgba(6, 13, 27, 0.62f), 1f)), 180f, UI51Tokens.Radii(21f), 0f, Color.clear);

            // Intestazione 64: avatar 62 (anello 3), poi a 74 nome, livello + titolo, squadra; X 28 in alto a destra.
            var header = UI51Build.Child(card, "Header");
            UI51Build.Layout(header, -1f, 64f);
            var avatar = Avatar(header, avatarPrefab, 62f, 3f, new Vector2(31f - 134f, 0f));
            var name = UI51Build.Text(Box(UI51Build.Child(header, "Name"), 74f, 0f, 160f, 22f), "Giocatore", FontFace.CinzelBold, 16f,
                UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft);
            name.enableWordWrapping = false; // puntini in fondo (riga alta 22 >= 16 x 1,37)
            var pill = Box(UI51Build.Child(header, "LevelPill"), 74f, 24f, 22f, 22f);
            UI51Build.Shape(pill, UI51Shape.Linear((UI51Tokens.GoldLight, 0f), (UI51Tokens.GoldDark, 1f)), 180f, UI51Tokens.Radii(11f), 0f, Color.clear);
            var level = UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(pill, "Value")), "1", FontFace.CinzelBold, 10f,
                UI51Tokens.OnGold, TextAlignmentOptions.Center));
            var title = UI51Build.NoWrap(UI51Build.Text(Box(UI51Build.Child(header, "Title"), 102f, 24f, 130f, 22f), "Principiante",
                FontFace.NunitoBold, 12f, UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft));
            var team = UI51Build.NoWrap(UI51Build.Text(Box(UI51Build.Child(header, "Team"), 74f, 47f, 160f, 16f), "Avversario",
                FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.5f), TextAlignmentOptions.MidlineLeft));
            var x = UI51Build.Solid(Box(UI51Build.Child(header, "Close"), 240f, 0f, 28f, 28f), UI51Tokens.WhiteA(0.06f), 14f, 0f, default, true);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(x.transform, "Icon"), 10f, 10f), closeIcon, Color.white);
            Listen(UI51Build.Button(x, x), manager.CloseProfile);

            // Statistiche: 3 riquadri bianco .04 r12, valore Cinzel 16 e etichetta 9, 14 sotto l'intestazione.
            var stats = UI51Build.Child(card, "Stats");
            UI51Build.Row(stats, 6f, UI51Build.Pad(14, 0, 0, 0), TextAnchor.UpperLeft, true, true).childForceExpandWidth = true;
            UI51Build.Layout(stats, -1f, 66f);
            var values = new TextMeshProUGUI[3];
            string[] labels = { "Partite", "Vittorie", "Scope" };
            for (int i = 0; i < 3; i++)
            {
                var tile = UI51Build.Child(stats, "Tile" + i);
                UI51Build.Layout(tile, -1f, 52f, 1f);
                UI51Build.Solid(tile, UI51Tokens.WhiteA(0.04f), 12f);
                values[i] = UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(tile, "Value"), 0f, 22f, 0f, 8f), "0",
                    FontFace.CinzelBold, 16f, UI51Tokens.Cream, TextAlignmentOptions.Center));
                UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(tile, "Label"), 0f, 8f, 0f, 32f), labels[i],
                    FontFace.NunitoBold, 9f, UI51Tokens.CreamA(0.55f), TextAlignmentOptions.Center));
            }

            // Medaglie: fino a 4 da 34, passo 10, 12 sotto le statistiche.
            var medals = UI51Build.Child(card, "Medals");
            UI51Build.Row(medals, 10f, UI51Build.Pad(12, 0, 0, 0), TextAnchor.UpperCenter, false, false);
            UI51Build.Layout(medals, -1f, 46f);
            var medalIcons = new GameObject[medalSprites.Length];
            for (int i = 0; i < medalSprites.Length; i++)
                medalIcons[i] = UI51Build.Image(UI51Build.Size(UI51Build.Child(medals, "Medal" + i), 34f, 34f), medalSprites[i], Color.white).gameObject;

            // Altri giocatori con account: Aggiungi amico (oro) / Richiesta inviata, Silenzia emoticon; sotto Segnala in rosso.
            var actions = UI51Build.Child(card, "Actions");
            UI51Build.Stack(actions, 0f, UI51Build.Pad(14, 0, 0, 0));
            var row = UI51Build.Child(actions, "Row");
            UI51Build.Row(row, 6f, null, TextAnchor.MiddleCenter, true, true).childForceExpandWidth = true;
            UI51Build.Layout(row, -1f, 38f);
            var add = UI51Build.Child(row, "Add");
            UI51PrefabBuilder.GoldBody(add.gameObject, 131f, 38f, 12f, FontFace.NunitoExtraBold, 12f, 0f, "Aggiungi amico");
            UI51Build.Layout(add, -1f, 38f, 1f);
            var added = UI51Build.Child(row, "Added");
            UI51Build.Solid(added, Color.clear, 12f, 1f, UI51Tokens.GoldA(0.5f));
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(added, "Label")), "Richiesta inviata",
                FontFace.NunitoExtraBold, 12f, UI51Tokens.Gold, TextAlignmentOptions.Center));
            UI51Build.Layout(added, -1f, 38f, 1f);
            var mute = UI51Build.Child(row, "Mute");
            UI51PrefabBuilder.ButtonBody(mute.gameObject, 131f, 38f, UI51Shape.Solid(UI51Tokens.WhiteA(0.04f)), UI51Tokens.Radii(12f), 1f,
                UI51Tokens.CreamA(0.3f), FontFace.NunitoExtraBold, 12f, 0f, UI51Tokens.Cream, "Silenzia emoticon");
            UI51Build.Layout(mute, -1f, 38f, 1f);
            UI51Build.Gap(actions, "Gap", 8f);
            // 2.55: Segnala (rosso) e Blocca affiancati; Segnala apre i motivi.
            var bottom = UI51Build.Child(actions, "Bottom");
            UI51Build.Row(bottom, 6f, null, TextAnchor.MiddleCenter, true, true).childForceExpandWidth = true;
            UI51Build.Layout(bottom, -1f, 34f);
            var report = UI51Build.Child(bottom, "Report");
            UI51PrefabBuilder.ButtonBody(report.gameObject, 131f, 34f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(12f), 0f, Color.clear,
                FontFace.NunitoExtraBold, 12f, 0f, UI51Tokens.DangerText, "Segnala giocatore");
            UI51Build.Layout(report, -1f, 34f, 1f);
            var block = UI51Build.Child(bottom, "Block");
            UI51PrefabBuilder.ButtonBody(block.gameObject, 131f, 34f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(12f), 0f, Color.clear,
                FontFace.NunitoExtraBold, 12f, 0f, UI51Tokens.CreamA(0.6f), "Blocca giocatore");
            UI51Build.Layout(block, -1f, 34f, 1f);

            // Motivi della segnalazione (scelta dell'utente 01/10), al posto dei pulsanti: tre righe e Annulla.
            var reasons = UI51Build.Child(card, "Reasons");
            UI51Build.Stack(reasons, 0f, UI51Build.Pad(14, 0, 0, 0));
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(reasons, "Caption"), "Perché lo segnali?", FontFace.NunitoBold,
                12f, UI51Tokens.CreamA(0.7f), TextAlignmentOptions.Center)), -1f, 18f);
            var reasonButtons = new Button[QuickProfileCard.ReasonLabels.Length];
            for (int i = 0; i < reasonButtons.Length; i++)
            {
                UI51Build.Gap(reasons, "Gap" + i, i == 0 ? 8f : 6f);
                var reason = UI51Build.Child(reasons, "Reason" + i);
                UI51PrefabBuilder.ButtonBody(reason.gameObject, 268f, 36f, UI51Shape.Solid(UI51Tokens.WhiteA(0.04f)), UI51Tokens.Radii(12f), 1f,
                    UI51Tokens.CreamA(0.2f), FontFace.NunitoExtraBold, 12f, 0f, UI51Tokens.Cream, QuickProfileCard.ReasonLabels[i]);
                UI51Build.Layout(reason, -1f, 36f);
                reasonButtons[i] = reason.GetComponent<Button>();
            }
            UI51Build.Gap(reasons, "GapCancel", 4f);
            var cancel = UI51Build.Child(reasons, "Cancel");
            UI51PrefabBuilder.ButtonBody(cancel.gameObject, 268f, 30f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(12f), 0f, Color.clear,
                FontFace.NunitoBold, 12f, 0f, UI51Tokens.CreamA(0.55f), "Annulla");
            UI51Build.Layout(cancel, -1f, 30f);

            // Io: "Livello N" e "x / y XP", barra 7, nota (PartitaMioProfilo).
            var self = UI51Build.Child(card, "Self");
            UI51Build.Stack(self, 0f, UI51Build.Pad(14, 0, 0, 0));
            var head = UI51Build.Child(self, "Head");
            UI51Build.Layout(head, -1f, 15f);
            var xpLevel = UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(head, "Level")), "Livello 1",
                FontFace.NunitoExtraBold, 11f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            var xpText = UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(head, "Xp")), "0 / 100 XP",
                FontFace.NunitoBold, 11f, UI51Tokens.CreamA(0.65f), TextAlignmentOptions.MidlineRight));
            UI51Build.Gap(self, "Gap1", 6f);
            var bar = UI51Build.Child(self, "Bar");
            UI51Build.Layout(bar, -1f, 7f);
            UI51Build.Solid(bar, UI51Tokens.WhiteA(0.12f), 3.5f);
            var fill = UI51Build.Child(bar, "Fill");
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0.62f, 1f); fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            UI51Build.Shape(fill, UI51Shape.Linear((UI51Tokens.GoldDark, 0f), (UI51Tokens.GoldLight, 1f)), 90f, UI51Tokens.Radii(3.5f), 0f, Color.clear);
            UI51Build.Gap(self, "Gap2", 10f);
            var note = UI51Build.Child(self, "Note");
            UI51Build.Layout(note, -1f, 15f);
            UI51Build.NoWrap(UI51Build.Text(note, "Banner, avatar e cornice si cambiano dal Profilo", FontFace.NunitoRegular, 11f,
                UI51Tokens.CreamA(0.5f), TextAlignmentOptions.Center));

            var view = UI51Build.GetOrAdd<QuickProfileCard>(root);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "content", content);
                UI51Build.Ref(so, "card", card);
                UI51Build.Ref(so, "banner", banner);
                UI51Build.Ref(so, "avatar", avatar);
                UI51Build.Ref(so, "nameText", name);
                UI51Build.Ref(so, "levelText", level);
                UI51Build.Ref(so, "titleText", title);
                UI51Build.Ref(so, "teamText", team);
                UI51Build.Ref(so, "levelPill", pill.gameObject);
                UI51Build.Ref(so, "stats", stats.gameObject);
                UI51Build.Ref(so, "gamesText", values[0]);
                UI51Build.Ref(so, "winsText", values[1]);
                UI51Build.Ref(so, "scopeText", values[2]);
                UI51Build.Ref(so, "medals", medals.gameObject);
                var list = so.FindProperty("medalIcons");
                list.arraySize = medalIcons.Length;
                for (int i = 0; i < medalIcons.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = medalIcons[i];
                UI51Build.Ref(so, "actions", actions.gameObject);
                UI51Build.Ref(so, "addButton", add.GetComponent<Button>());
                UI51Build.Ref(so, "muteButton", mute.GetComponent<Button>());
                UI51Build.Ref(so, "reportButton", report.GetComponent<Button>());
                UI51Build.Ref(so, "addedLabel", added.gameObject);
                UI51Build.Ref(so, "muteLabel", mute.Find("Label").GetComponent<TextMeshProUGUI>());
                UI51Build.Ref(so, "reportLabel", report.Find("Label").GetComponent<TextMeshProUGUI>());
                UI51Build.Ref(so, "actionRow", row.gameObject);
                UI51Build.Ref(so, "blockButton", block.GetComponent<Button>());
                UI51Build.Ref(so, "blockLabel", block.Find("Label").GetComponent<TextMeshProUGUI>());
                UI51Build.Ref(so, "reasons", reasons.gameObject);
                var reasonList = so.FindProperty("reasonButtons");
                reasonList.arraySize = reasonButtons.Length;
                for (int i = 0; i < reasonButtons.Length; i++) reasonList.GetArrayElementAtIndex(i).objectReferenceValue = reasonButtons[i];
                UI51Build.Ref(so, "cancelReasons", cancel.GetComponent<Button>());
                UI51Build.Ref(so, "self", self.gameObject);
                UI51Build.Ref(so, "xpLevel", xpLevel);
                UI51Build.Ref(so, "xpText", xpText);
                UI51Build.Ref(so, "xpFill", fill);
            });
            UI51Build.Wire(manager, so => UI51Build.Ref(so, "profileCard", view));
            UI51Build.Wire(social, so => UI51Build.Ref(so, "ProfileHit", ownHit));
            added.gameObject.SetActive(false);
            reasons.gameObject.SetActive(false);
            root.gameObject.SetActive(false); // UI51Build.Child la riaccende: si vede solo al tocco su un banner
        }

        // --- S10: sorteggio

        /// <summary>
        /// Mockup Sorteggio (1 contro 1) e Sorteggio4 (Fase 6): DealerRoulette/UI51. Schermata intera in scala uniforme (BuildScreen: DesignCanvasFit 390x844, come Accesso): intestazione,
        /// ruota a 2 o 4 spicchi con avatar e nomi, mozzo col sole, lancetta, stato, scheda MAZZIERE, conto e AL TAVOLO
        /// ("Continue": suono UiConfirm). Tempi, animazioni e spicchi in SorteggioView, attesa e consegna in
        /// DealerRouletteController.PlayWheel.
        /// </summary>
        static void BuildSorteggio(PlayerBannerManager banners)
        {
            var roulette = UnityEngine.Object.FindObjectOfType<Project51.Unity.DealerRouletteController>(true);
            var bg = UI51Build.Sprite("Backgrounds", "home_bg_blur");
            var wash = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UIV2/Art/Generated/glow_soft_pill.png");
            var glow = UI51Build.Sprite("Common", "Bagliore_morbido");
            var sun = UI51Build.Sprite("Common", "Sun_fix");
            var avatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI51PrefabBuilder.PrefabPath("AvatarFrame"));
            if (roulette == null || bg == null || wash == null || glow == null || sun == null || avatarPrefab == null)
            {
                Debug.LogError($"{Tag} Manca un pezzo del sorteggio (DealerRouletteController, home_bg_blur, glow_soft_pill, Bagliore_morbido, " +
                               "Sun_fix, prefab AvatarFrame). Sorteggio non toccato.");
                return;
            }
            var panel = roulette.gameObject;
            bool was = panel.activeSelf;
            panel.SetActive(true); // TMP su oggetti spenti lancia eccezioni

            var safe = UI51AccessBuilder.BuildScreen(roulette.transform, bg, null);
            var root = (RectTransform)safe.parent;
            // Mockup: sfondo a luminosita' .40, lo sfocato e' gia' a .41.
            root.Find("Bg").GetComponent<Image>().color = new Color(0.975f, 0.975f, 0.975f, 1f);
            // Alone oro del mockup (ellisse al 50%/45%, fino al 60%): primo figlio di Safe, dietro a tutto.
            UI51Build.Image(UI51Build.CenterAt(UI51Build.Child(safe, "Wash"), 195f, 379.8f, 331f, 788f), wash, UI51Tokens.GoldA(0.12f),
                false, false);

            // Intestazione: testi allineati in alto sul top del mockup, senza "..." (altezze = righe CSS).
            var mode = UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(safe, "Mode"), 20f, 20f, 60f, 15f),
                "PARTITA 1 VS 1", FontFace.CinzelSemiBold, 11f, UI51Tokens.Gold, TextAlignmentOptions.Top, 3f));
            UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(safe, "Title"), 20f, 20f, 81f, 32f),
                "Chi fa il mazziere?", FontFace.CinzelBold, 24f, UI51Tokens.Cream, TextAlignmentOptions.Top));
            UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(safe, "Subtitle"), 20f, 20f, 119f, 18f),
                "Il mazziere distribuisce le carte", FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.Top));

            // Ruota 300 con centro (195, 346): alone, anello d'oro, faccia che gira (verde a sinistra = avversario, blu a destra = io).
            var wheel = UI51Build.CenterAt(UI51Build.Child(safe, "Wheel"), 195f, 346f, 300f, 300f);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(wheel, "Glow"), 440f, 440f), glow, UI51Tokens.WhiteA(0.35f), false, false);
            UI51Build.Shape(UI51Build.Center(UI51Build.Child(wheel, "Rim"), 300f, 300f), AvatarFrame.FrameFill(FrameStyle.Oro), 110f,
                UI51Tokens.Radii(150f), 0f, Color.clear, false, new UI51Shadow(0f, 20f, 40f, UI51Tokens.BlackA(0.6f)));
            var face = UI51Build.Center(UI51Build.Child(wheel, "Face"), 284f, 284f);
            Color green = UI51Tokens.Hex("#0F5A42"), blue = UI51Tokens.Hex("#123B6B");
            UI51Build.Shape(face, UI51Shape.Linear((green, 0f), (green, 0.499f), (blue, 0.501f), (blue, 1f)), 90f, UI51Tokens.Radii(142f),
                0f, Color.clear);
            var line = UI51Build.Solid(UI51Build.Center(UI51Build.Child(face, "Line"), 2f, 284f), UI51Tokens.GoldA(0.75f), 0f); // copre lo stacco
            // A 4 (Sorteggio4): spicchi CSS da 90 gradi orari da mezzogiorno nei colori del mockup (0 in alto a destra = io, poi
            // in ordine di posto) dentro al cerchio della faccia, e la seconda riga d'oro. Accesi da SorteggioView.
            var slices = UI51Build.Center(UI51Build.Child(face, "Slices4"), 284f, 284f);
            slices.SetAsFirstSibling();
            UI51Build.Solid(slices, Color.white, 142f);
            UI51Build.GetOrAdd<Mask>(slices).showMaskGraphic = false;
            string[] sliceColors = { "#123B6B", "#0F5A42", "#1B2F57", "#0C4A36" };
            for (int i = 0; i < sliceColors.Length; i++)
                UI51Build.Solid(UI51Build.Place(UI51Build.Child(slices, "Slice" + i), new Vector2(0.5f, 0.5f), new Vector2(142f, 142f),
                    new Vector2(i < 2 ? 71f : -71f, i == 0 || i == 3 ? 71f : -71f)), UI51Tokens.Hex(sliceColors[i]), 0f);
            var cross = UI51Build.Center(UI51Build.Child(face, "Cross"), 284f, 2f);
            UI51Build.Solid(cross, UI51Tokens.GoldA(0.75f), 0f);
            cross.SetSiblingIndex(line.transform.GetSiblingIndex() + 1); // sotto agli avatar
            var seats = new RectTransform[4];
            var seatAvatars = new AvatarFrame[4];
            var seatNames = new TextMeshProUGUI[4];
            for (int i = 0; i < seats.Length; i++)
            {
                // Perno al centro girato come nel mockup (SorteggioView: spicchio i a (i + 0,5) * 360 / n gradi CSS; qui quelli
                // a 4). Avatar e nome stanno in Upright, che SorteggioView tiene dritto mentre la ruota gira (scelta dell'utente
                // 30/09; il mockup li fa girare): in cima avatar a raggio 90 e nome a 51 come nel mockup, in fondo l'avatar resta
                // a 9 dal sole.
                var seat = seats[i] = UI51Build.Place(UI51Build.Child(face, "Seat" + i), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                seat.localRotation = Quaternion.Euler(0f, 0f, -(i + 0.5f) * 90f);
                var upright = UI51Build.Place(UI51Build.Child(seat, "Upright"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 82f));
                upright.localRotation = Quaternion.Inverse(seat.localRotation);
                foreach (var moved in new[] { "Avatar", "Name" }) // scene costruite prima: si spostano, i collegamenti restano
                {
                    var old = seat.Find(moved);
                    if (old != null) old.SetParent(upright, false);
                }
                seatAvatars[i] = Avatar(upright, avatarPrefab, 56f, 2f, new Vector2(0f, 8f));
                seatAvatars[i].SetRing(UI51Tokens.Gold);
                seatNames[i] = UI51Build.Text(UI51Build.Place(UI51Build.Child(upright, "Name"), new Vector2(0.5f, 0.5f), new Vector2(100f, 14f),
                    new Vector2(0f, -31f)), "", FontFace.NunitoExtraBold, 10f, UI51Tokens.Cream, TextAlignmentOptions.Center);
                seatNames[i].enableWordWrapping = false; // nomi lunghi: "..." in fondo
            }
            var hub = UI51Build.Center(UI51Build.Child(wheel, "Hub"), 70f, 70f);
            UI51Build.Shape(hub, AvatarFrame.FrameFill(FrameStyle.Oro), 110f, UI51Tokens.Radii(35f), 0f, Color.clear, false,
                new UI51Shadow(0f, 6f, 14f, UI51Tokens.BlackA(0.5f)));
            var inner = UI51Build.Center(UI51Build.Child(hub, "Inner"), 62f, 62f);
            UI51Build.Solid(inner, UI51Tokens.Hex("#0B3F2E"), 31f);
            UI51Build.GetOrAdd<Mask>(inner).showMaskGraphic = true;
            UI51Build.Image(UI51Build.Center(UI51Build.Child(inner, "Emblem"), 62f, 62f), sun, Color.white);

            // Lancetta 34x40 (SVG del mockup: triangolo col lato alto curvo, bordo marrone, punto): perno al 20% dall'alto,
            // 10 sopra il bordo della ruota. Cupola sotto, triangolo = meta' bassa di un rombo tagliata da una maschera che gira con lei.
            var pointer = UI51Build.Child(wheel, "Pointer");
            pointer.anchorMin = pointer.anchorMax = new Vector2(0f, 1f);
            pointer.pivot = new Vector2(0.5f, 0.8f);
            pointer.sizeDelta = new Vector2(34f, 40f);
            pointer.anchoredPosition = new Vector2(150f, 10f);
            Color brown = UI51Tokens.Hex("#6B4418");
            UI51Build.Solid(Box(UI51Build.Child(pointer, "Cap"), 3f, 3f, 28f, 10f), UI51Tokens.Gold, 5f, 1.5f, brown);
            var clip = Box(UI51Build.Child(pointer, "Clip"), 0f, 8f, 34f, 32f);
            UI51Build.Solid(clip, Color.white, 0f);
            UI51Build.GetOrAdd<Mask>(clip).showMaskGraphic = false;
            var stretch = UI51Build.Place(UI51Build.Child(clip, "Stretch"), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            stretch.localScale = new Vector3(1f, 30f / 14f, 1f); // rombo con mezza diagonale 14 -> punta 30 sotto il lato alto
            var diamond = UI51Build.Center(UI51Build.Child(stretch, "Diamond"), 19.8f, 19.8f);
            diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
            UI51Build.Solid(diamond, UI51Tokens.Gold, 0f, 1.2f, brown);
            UI51Build.Solid(Box(UI51Build.Child(pointer, "Dot"), 13f, 7f, 8f, 8f), brown, 4f);

            var status = UI51Build.NoWrap(UI51Build.Text(UI51Build.TopBand(UI51Build.Child(safe, "Status"), 0f, 0f, 584f, 19f),
                Project51.Unity.UI.SorteggioView.Waiting, FontFace.CinzelBold, 14f, UI51Tokens.CreamA(0.75f), TextAlignmentOptions.Top, 3f));

            // Scheda MAZZIERE (top 540, alta 103): pivot al centro per il pop del mockup. Avatar 60 con alone, testi che si stringono.
            var card = UI51Build.Place(UI51Build.Child(safe, "Card"), new Vector2(0.5f, 1f), new Vector2(350f, 103.2f), new Vector2(0f, -591.6f));
            card.pivot = new Vector2(0.5f, 0.5f);
            UI51Build.Shape(card, UI51Shape.Linear((UI51Tokens.Rgba(12, 26, 50, 0.95f), 0f), (UI51Tokens.Rgba(6, 13, 27, 0.97f), 1f)), 180f,
                UI51Tokens.Radii(18f), 1f, UI51Tokens.GoldA(0.5f));
            UI51Build.Row(card, 14f, UI51Build.Pad(17, 19, 17, 19), TextAnchor.MiddleLeft, true, true); // 16/18 + 1 di bordo
            var winner = UI51Build.Child(card, "Winner");
            UI51Build.Layout(winner, 60f, 60f, -1f, 60f);
            var pulse = UI51Build.Solid(UI51Build.Center(UI51Build.Child(winner, "Pulse"), 60f, 60f), UI51Tokens.Gold, 30f);
            var winAvatar = Avatar(winner, avatarPrefab, 60f, 3f, Vector2.zero);
            winAvatar.SetFrame(FrameStyle.Oro);
            var texts = UI51Build.Child(card, "Texts");
            UI51Build.Layout(texts, 0f, -1f, 1f); // prende il resto, i nomi lunghi finiscono in "..."
            UI51Build.Column(texts, 4f, null, TextAnchor.MiddleLeft, true, true);
            var chip = CaptureChip(texts, "Chip", UI51Shape.Solid(UI51Tokens.Gold), 0f, 8, 8);
            UI51Build.Layout(chip, -1f, 20f);
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(chip, "Text"), "MAZZIERE", FontFace.CinzelBold, 10f, UI51Tokens.OnGold,
                TextAlignmentOptions.Center, 1f));
            var winName = UI51Build.Text(UI51Build.Child(texts, "Name"), "", FontFace.CinzelBold, 18f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft);
            winName.enableWordWrapping = false;
            UI51Build.Layout(winName, -1f, 24.7f); // >= 18 x 1,37: con "..." TMP non scrive nulla in un rettangolo piu' basso
            var winNote = UI51Build.Text(UI51Build.Child(texts, "Note"), "", FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.6f),
                TextAlignmentOptions.MidlineLeft);
            winNote.enableWordWrapping = false;
            UI51Build.Layout(winNote, -1f, 16.5f);

            // Conto e AL TAVOLO (top 672): riga centrata "La partita inizia tra N", sotto il pulsante contornato d'oro.
            var footer = UI51Build.TopBand(UI51Build.Child(safe, "Footer"), 0f, 0f, 672f, 74f);
            var countRow = UI51Build.Place(UI51Build.Child(footer, "CountRow"), new Vector2(0.5f, 1f), new Vector2(0f, 18f), Vector2.zero);
            UI51Build.Row(countRow, 3.5f, UI51Build.Pad(0, 0, 0, 0), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            UI51Build.Fit(countRow, true, false);
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(countRow, "Label"), "La partita inizia tra", FontFace.NunitoRegular, 13f,
                UI51Tokens.CreamA(0.7f), TextAlignmentOptions.MidlineLeft));
            var count = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(countRow, "Number"), "3", FontFace.CinzelBold, 13f,
                UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft));
            var cont = UI51Build.Child(footer, "Continue");
            UI51PrefabBuilder.ButtonBody(cont.gameObject, 158f, 46f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(14f), 1f, UI51Tokens.Gold,
                FontFace.CinzelBold, 13f, 2f, UI51Tokens.Gold, "AL TAVOLO");
            UI51Build.Place(cont, new Vector2(0.5f, 1f), new Vector2(158f, 46f), new Vector2(0f, -28f));
            Listen(cont.GetComponent<Button>(), roulette.Continue);

            UI51Build.Wire(UI51Build.GetOrAdd<Project51.Unity.UI.SorteggioView>(root), so =>
            {
                UI51Build.Ref(so, "m_Roulette", roulette);
                UI51Build.Ref(so, "m_Banners", banners);
                UI51Build.Ref(so, "m_Mode", mode);
                UI51Build.Ref(so, "m_Face", face);
                UI51Build.Ref(so, "m_Slices4", slices.gameObject);
                UI51Build.Ref(so, "m_Cross", cross.gameObject);
                UI51Build.Ref(so, "m_Pointer", pointer);
                UI51Build.SetArray(so, "m_Seats", seats);
                UI51Build.SetArray(so, "m_SeatAvatars", seatAvatars);
                UI51Build.SetArray(so, "m_SeatNames", seatNames);
                UI51Build.Ref(so, "m_Status", status);
                UI51Build.Ref(so, "m_Card", card);
                UI51Build.Ref(so, "m_WinAvatar", winAvatar);
                UI51Build.Ref(so, "m_WinPulse", pulse);
                UI51Build.Ref(so, "m_WinName", winName);
                UI51Build.Ref(so, "m_WinNote", winNote);
                UI51Build.Ref(so, "m_Footer", footer);
                UI51Build.Ref(so, "m_Count", count);
                UI51Build.Ref(so, "m_Continue", cont.gameObject);
            });
            UI51Build.Wire(roulette, so => UI51Build.Ref(so, "ui51Wheel", root.gameObject));
            // Accesi solo quando servono: la ruota da PlayWheel, scheda e conto al risultato (SorteggioView).
            card.gameObject.SetActive(false);
            footer.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            panel.SetActive(was);
        }

        /// <summary>AvatarFrame del prefab dentro parent (riusato per nome "Avatar"), lato size, anello ring, centro a at.</summary>
        static AvatarFrame Avatar(RectTransform parent, GameObject prefab, float size, float ring, Vector2 at)
        {
            var t = parent.Find("Avatar");
            var go = t != null ? t.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = "Avatar";
            go.transform.SetAsLastSibling();
            var rt = UI51Build.Center((RectTransform)go.transform, size, size);
            rt.anchoredPosition = at;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            var frame = go.GetComponent<AvatarFrame>();
            UI51Build.Wire(frame, so => UI51Build.Float(so, "m_RingWidth", ring));
            frame.Layout();
            return frame;
        }

        /// <summary>Area di tocco trasparente sulle scope (Box nel banner, misure del mockup): apre il visore del posto slot.</summary>
        static Button ScopeHit(RectTransform scope, PlayerBannerManager manager, int slot, float left, float top, float w, float h)
        {
            var hit = UI51Build.Image(Box(UI51Build.Child(scope, "UI51ScopeHit"), left, top, w, h), null, Color.clear, true, false);
            hit.canvasRenderer.cullTransparentMesh = false; // trasparente ma deve prendere i tocchi
            var button = UI51Build.Button(hit, hit);
            var press = UI51Build.GetOrAdd<UI51Press>(hit); // niente pressione UIV2: non c'e' un tasto da rimpicciolire
            UI51Build.Wire(press, so => UI51Build.Float(so, "m_Scale", 1f));
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddIntPersistentListener(button.onClick, manager.OpenScope, slot);
            EditorUtility.SetDirty(button);
            return button;
        }

        /// <summary>Chip del mockup (.chip): larga quanto il testo, la larghezza la da' la fila. Senza fill e bordo: quella del visore.</summary>
        static TextMeshProUGUI Chip(RectTransform row, string name, Color color, Color? fill = null, Color? border = null, int pad = 13,
            float size = 12f)
        {
            var chip = UI51Build.Child(row, name);
            UI51Build.Solid(chip, fill ?? UI51Tokens.BlackA(0.45f), 13f, 1f, border ?? UI51Tokens.GoldA(0.35f));
            UI51Build.Row(chip, 0f, UI51Build.Pad(0, pad, 0, pad), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            return UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(chip, "Text"), "", FontFace.NunitoExtraBold, size, color,
                TextAlignmentOptions.Center));
        }

        /// <summary>Riga del blocco di testo: tutta la larghezza, a top dal bordo alto, alta h (mockup).</summary>
        static RectTransform TopLine(RectTransform parent, string name, float top, float h)
        {
            var rt = UI51Build.Place(UI51Build.Child(parent, name), new Vector2(0.5f, 1f), new Vector2(390f, h), new Vector2(0f, -top));
            rt.pivot = new Vector2(0.5f, 1f);
            return rt;
        }

        static void Seat(Transform seat, float centerX, float centerY, float width, float height = 50f)
        {
            var rt = (RectTransform)seat;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height) * UI51Build.Unit;
            Center(rt, centerX, centerY);
        }

        static void Center(Transform t, float x, float y) => ((RectTransform)t).anchoredPosition = new Vector2(x, -y) * UI51Build.Unit;

        /// <summary>Rettangolo in coordinate del mockup: origine in alto a sinistra del genitore, y in giu', gradi orari come in CSS.</summary>
        static RectTransform Box(RectTransform rt, float left, float top, float w, float h, float degrees = 0f)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(left + w * 0.5f, -(top + h * 0.5f));
            rt.localRotation = Quaternion.Euler(0f, 0f, -degrees);
            return rt;
        }

        /// <summary>Punto di ancoraggio sul bordo alto della barra (x 0 sinistra, 0.5 centro, 1 destra), in unita' del mockup.</summary>
        static RectTransform Holder(RectTransform parent, string name, float x)
        {
            var rt = UI51Build.Place(UI51Build.Child(parent, name), new Vector2(x, 1f), Vector2.zero, Vector2.zero);
            rt.localScale = new Vector3(UI51Build.Unit, UI51Build.Unit, 1f);
            return rt;
        }

        /// <summary>Segmento della pillola: etichetta piccola sopra, numero sotto. Restituisce { etichetta, numero }.</summary>
        static TextMeshProUGUI[] Segment(RectTransform pill, string name, int pad, string label, float labelSize, Color labelColor,
            string value, float valueSize, Color valueColor)
        {
            var seg = UI51Build.Child(pill, name);
            UI51Build.Column(seg, 0f, UI51Build.Pad(0, pad, 0, pad), TextAnchor.MiddleCenter);
            var l = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(seg, "Label"), label, FontFace.NunitoExtraBold,
                labelSize, labelColor, TextAlignmentOptions.Center, 1f));
            UI51Build.Layout(l, -1f, Mathf.Ceil(labelSize * 1.36f));
            var v = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(seg, "Value"), value, FontFace.CinzelBold,
                valueSize, valueColor, TextAlignmentOptions.Center));
            UI51Build.Layout(v, -1f, valueSize); // line-height 1 nel mockup
            return new[] { l, v };
        }

        static void SideLine(RectTransform parent, string name, float x, float alpha = 0.35f)
        {
            var rt = UI51Build.Child(parent, name);
            rt.anchorMin = new Vector2(x, 0f);
            rt.anchorMax = new Vector2(x, 1f);
            rt.pivot = new Vector2(x, 0.5f);
            rt.sizeDelta = new Vector2(1f, 0f);
            rt.anchoredPosition = Vector2.zero;
            UI51Build.Layout(rt, ignore: true);
            UI51Build.Image(rt, null, UI51Tokens.GoldA(alpha), false, false);
        }
    }
}
