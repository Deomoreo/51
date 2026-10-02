using Project51.Networking;
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
    /// UI51 Fase 9, avvisi e connessione. Prefab in Resources creati al primo uso e tenuti tra le scene (valgono in Home e al tavolo):
    /// Toast (mockup Toast), overlay di connessione (mockup Connessione e Conn*) e schermate di servizio (mockup Aggiornamento e
    /// Manutenzione). In MainMenu mette HomeConnectionWatcher su StartScreenV2. Idempotente: rieseguirlo riusa i nodi per nome.
    /// </summary>
    public static class UI51SystemBuilder
    {
        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        const string ResourcesRoot = UI51Build.Root + "/Resources/";
        const string Tag = "[UI51 Fase 9]";

        [MenuItem("Tools/UI51/Build Fase 9 (Avvisi e connessione)")]
        private static void Menu() => Build();

        public static void Build()
        {
            if (UI51AccessBuilder.HasDirtyScene()) return;
            UI51Build.EditPrefab(ResourcesRoot + UI51Toast.ResourcePath + ".prefab", BuildToast);
            UI51Build.EditPrefab(ResourcesRoot + UI51ConnectionOverlay.ResourcePath + ".prefab", BuildConnection);
            UI51Build.EditPrefab(ResourcesRoot + UI51ServiceScreen.ResourcePath + ".prefab", BuildService);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var start = Object.FindObjectOfType<StartScreenV2>(true);
            if (start == null) { Debug.LogError($"{Tag} StartScreenV2 non trovato in MainMenu."); return; }
            var watcher = UI51Build.GetOrAdd<HomeConnectionWatcher>(start);
            UI51Build.Wire(watcher, so => UI51Build.Ref(so, "startScreen", start));
            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene, ScenePath)) Debug.Log($"{Tag} Scena salvata: {ScenePath}");
            else Debug.LogError($"{Tag} Salvataggio fallito: {ScenePath}");
        }

        /// <summary>Canvas sopra tutto (overlay: vale in ogni scena).</summary>
        static void OverlayCanvas(GameObject root, int order, bool raycasts)
        {
            var canvas = UI51Build.GetOrAdd<Canvas>(root);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            UI51Build.GetOrAdd<CanvasScaler>(root).uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            if (raycasts) UI51Build.GetOrAdd<GraphicRaycaster>(root);
            else UI51Build.Remove<GraphicRaycaster>(root);
        }

        /// <summary>Canvas sopra tutto e area 390x844 nella safe area. Ritorna Safe.</summary>
        static RectTransform OverlayRoot(GameObject root, int order, bool raycasts)
        {
            OverlayCanvas(root, order, raycasts);
            var ui = UI51Build.Stretch(UI51Build.Child(root.transform, "UI51"));
            var safe = UI51Build.Child(ui, "Safe");
            var fit = UI51Build.GetOrAdd<DesignCanvasFit>(safe);
            fit.Reference = UI51Tokens.ReferenceResolution;
            fit.Fill = true;
            safe.anchorMin = safe.anchorMax = safe.pivot = new Vector2(0.5f, 0.5f);
            safe.anchoredPosition = Vector2.zero;
            safe.sizeDelta = UI51Tokens.ReferenceResolution;
            return safe;
        }

        // --- Toast

        /// <summary>
        /// Pillola a 96 dal fondo, centrata, larga quanto il contenuto (padding 0 16 0 12, stacco 10), fondo (6,13,27,.95), bordo per tipo,
        /// ombra 0 10 24 .5. Icone: spunta bianca su cerchio verde 22, moneta 18, "!" bianco su cerchio rosso 20. Testo Nunito 800 13.
        /// </summary>
        static void BuildToast(GameObject root)
        {
            var safe = OverlayRoot(root, 5000, false);
            var pill = UI51Build.Place(UI51Build.Child(safe, "Pill"), new Vector2(0.5f, 0f), new Vector2(200f, 42f), new Vector2(0f, 96f));
            var shape = UI51Build.Solid(pill, UI51Tokens.Rgba(6, 13, 27, 0.95f), 21f, 1f, UI51Tokens.WithAlpha(UI51Tokens.SuccessText, 0.55f), false,
                new UI51Shadow(0f, 10f, 24f, UI51Tokens.BlackA(0.5f)));
            var row = UI51Build.Row(pill, 10f, UI51Build.Pad(0, 16, 0, 12), TextAnchor.MiddleCenter, true, true);
            row.childForceExpandHeight = false;
            UI51Build.Fit(pill, true, false);

            var success = UI51Build.Child(pill, "Success");
            UI51Build.Layout(success, 22f, 22f);
            UI51Build.Solid(success, UI51Tokens.Success, 11f);
            var check = UI51Build.GetOrAdd<UI51Polyline>(UI51Build.Center(UI51Build.Child(success, "Check"), 11f, 11f));
            check.Set(new Vector2(24f, 24f), 3.4f, new Vector2(5f, 12.5f), new Vector2(9.5f, 17f), new Vector2(19f, 7.5f));
            check.color = Color.white;
            check.raycastTarget = false;

            var coin = UI51Build.Child(pill, "Coin");
            UI51Build.Layout(coin, 18f, 18f);
            UI51Build.Image(coin, UI51Build.Sprite("Common", "ic_coin"), Color.white);

            var error = UI51Build.Child(pill, "Error");
            UI51Build.Layout(error, 20f, 20f);
            UI51Build.Solid(error, UI51Tokens.Danger, 10f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(error, "Mark")), "!", FontFace.NunitoExtraBold, 12f, Color.white,
                TextAlignmentOptions.Center));

            var label = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(pill, "Label"), "ID copiato negli appunti", FontFace.NunitoExtraBold, 13f,
                UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            UI51Build.Layout(label, -1f, 18f);

            var toast = UI51Build.GetOrAdd<UI51Toast>(root);
            UI51Build.Wire(toast, so =>
            {
                UI51Build.Ref(so, "pill", pill);
                UI51Build.Ref(so, "shape", shape);
                UI51Build.Ref(so, "label", label);
                UI51Build.Ref(so, "successIcon", success.gameObject);
                UI51Build.Ref(so, "coinIcon", coin.gameObject);
                UI51Build.Ref(so, "errorIcon", error.gameObject);
            });
        }

        // --- Connessione

        /// <summary>
        /// Velo (3,7,16,.65: niente sfocatura come negli altri pannelli, un po' piu' scuro del .55 del mockup) e due card a 28 dai lati,
        /// raggio 22, padding 24 20 20: "Riconnessione…" da top 240 e "Nessuna connessione" da top 220.
        /// </summary>
        static void BuildConnection(GameObject root)
        {
            var view = UI51Build.Stretch(UI51Build.Child(root.transform, "UI51"));
            var veil = UI51Build.Stretch(UI51Build.Child(view, "Veil"));
            veil.SetAsFirstSibling();
            UI51Build.Image(veil, null, UI51Tokens.Rgba(3, 7, 16, 0.65f), true, false);
            var safe = OverlayRoot(root, 4000, true);

            // Riconnessione
            var reconnect = Card(safe, "Reconnect", 240f, UI51Tokens.GoldA(0.45f));
            var ring = UI51Build.Child(reconnect, "Ring");
            UI51Build.Layout(ring, 84f, 84f);
            var spinner = UI51Build.Stretch(UI51Build.Child(ring, "Spinner"));
            UI51Build.Solid(UI51Build.Center(UI51Build.Child(spinner, "Track"), 80f, 80f), Color.clear, 40f, 4f, UI51Tokens.GoldA(0.15f));
            Stroke(UI51Build.Stretch(UI51Build.Child(spinner, "Arc")), new Vector2(84f, 84f), 4f, UI51Tokens.Gold,
                UI51Polyline.Arc(new Vector2(42f, 42f), 38f, -90f, 0f));
            var wifi = UI51Build.Place(UI51Build.Child(ring, "Wifi"), new Vector2(0f, 1f), new Vector2(44f, 36f), new Vector2(20f, -26f));
            var vb = new Vector2(44f, 36f);
            var w1 = Stroke(UI51Build.Stretch(UI51Build.Child(wifi, "W1")), vb, 3.5f, UI51Tokens.Gold,
                UI51Polyline.Quad(new Vector2(17f, 27f), new Vector2(22f, 23f), new Vector2(27f, 27f), 8));
            var w2 = Stroke(UI51Build.Stretch(UI51Build.Child(wifi, "W2")), vb, 3.5f, UI51Tokens.Gold,
                UI51Polyline.Quad(new Vector2(10f, 20f), new Vector2(22f, 9f), new Vector2(34f, 20f)));
            var w3 = Stroke(UI51Build.Stretch(UI51Build.Child(wifi, "W3")), vb, 3.5f, UI51Tokens.Gold,
                UI51Polyline.Quad(new Vector2(3f, 12f), new Vector2(22f, -4f), new Vector2(41f, 12f), 20));
            Dot(wifi, new Vector2(22f, 32f), 2.8f, UI51Tokens.Gold);

            UI51MetaBuilder.Gap(reconnect, "GapTitle", 16f);
            Title(reconnect, "Riconnessione…");
            UI51MetaBuilder.Gap(reconnect, "GapText", 8f);
            Body(reconnect, "Text", "La connessione è instabile, stiamo provando a ricollegarti.");
            UI51MetaBuilder.Gap(reconnect, "GapBars", 14f);
            var barsRow = UI51Build.Child(reconnect, "Attempts");
            UI51Build.Layout(barsRow, -1f, 6f);
            UI51Build.Row(barsRow, 6f, null, TextAnchor.MiddleCenter, true, true);
            var bars = new Object[5];
            for (int i = 0; i < bars.Length; i++)
            {
                var bar = UI51Build.Child(barsRow, "Bar" + i);
                UI51Build.Layout(bar, 26f, 6f);
                bars[i] = UI51Build.Solid(bar, Color.white, 3f);
            }
            UI51MetaBuilder.Gap(reconnect, "GapAttempt", 8f);
            var attempt = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(reconnect, "Attempt"), "Tentativo 1 di 5", FontFace.NunitoBold, 11f,
                UI51Tokens.CreamA(0.5f), TextAlignmentOptions.Center));
            UI51Build.Layout(attempt, -1f, 15f, 1f);
            var seatBlock = UI51Build.Child(reconnect, "Seat");
            UI51Build.Layout(seatBlock, -1f, -1f, 1f);
            UI51MetaBuilder.Stack(seatBlock, 0f, UI51Build.Pad(14, 0, 0, 0));
            var seatBox = UI51Build.Child(seatBlock, "Box");
            UI51Build.Solid(seatBox, UI51Tokens.GoldA(0.08f), 12f, 1f, UI51Tokens.GoldA(0.3f));
            UI51MetaBuilder.Stack(seatBox, 0f, UI51Build.Pad(10, 12, 10, 12));
            var seat = UI51Build.Text(UI51Build.Child(seatBox, "Text"), "Il tuo posto al tavolo resta tuo per 60s. Gli altri giocatori ti aspettano.",
                FontFace.NunitoRegular, 12f, UI51Tokens.Cream, TextAlignmentOptions.Center);
            UI51MetaBuilder.Wrap(seat, 8.6f); // line-height 1.45

            // Nessuna connessione
            var error = Card(safe, "Error", 220f, UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.5f));
            var iconRow = UI51Build.Child(error, "IconRow");
            UI51Build.Layout(iconRow, 76f, 76f);
            var icon = UI51Build.Center(UI51Build.Child(iconRow, "Icon"), 76f, 76f);
            UI51Build.Solid(icon, UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.12f), 38f, 1f, UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.5f));
            var off = UI51Build.Center(UI51Build.Child(icon, "WifiOff"), 46f, 38f);
            var vbOff = new Vector2(46f, 38f);
            var faded = UI51Tokens.WithAlpha(UI51Tokens.DangerText, 0.45f);
            Stroke(UI51Build.Stretch(UI51Build.Child(off, "W2")), vbOff, 3.5f, faded,
                UI51Polyline.Quad(new Vector2(4f, 13f), new Vector2(23f, -3f), new Vector2(42f, 13f), 20));
            Stroke(UI51Build.Stretch(UI51Build.Child(off, "W1")), vbOff, 3.5f, faded,
                UI51Polyline.Quad(new Vector2(11f, 21f), new Vector2(23f, 10f), new Vector2(35f, 21f)));
            Dot(off, new Vector2(23f, 32f), 3f, faded);
            Stroke(UI51Build.Stretch(UI51Build.Child(off, "Slash")), vbOff, 4f, UI51Tokens.DangerText, new Vector2(8f, 4f), new Vector2(38f, 34f));

            UI51MetaBuilder.Gap(error, "GapTitle", 16f);
            Title(error, "Nessuna connessione");
            UI51MetaBuilder.Gap(error, "GapText", 8f);
            var errorText = Body(error, "Text", UI51ConnectionOverlay.HomeErrorText);
            UI51MetaBuilder.Gap(error, "GapRetry", 18f);
            var retry = UI51Build.Child(error, "Retry");
            UI51PrefabBuilder.GoldBody(retry.gameObject, 300f, 50f, 14f, FontFace.CinzelBold, 14f, 2f, "RIPROVA");
            UI51Build.Layout(retry, -1f, 50f, 1f);
            UI51MetaBuilder.Gap(error, "GapExit", 10f);
            var exit = UI51Build.Child(error, "Exit");
            UI51PrefabBuilder.ButtonBody(exit.gameObject, 300f, 44f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(14f), 1f, UI51Tokens.GoldA(0.45f),
                FontFace.NunitoExtraBold, 13f, 0f, UI51Tokens.Gold, "Continua offline");
            UI51Build.Layout(exit, -1f, 44f, 1f);

            reconnect.gameObject.SetActive(false);
            error.gameObject.SetActive(false);
            var overlay = UI51Build.GetOrAdd<UI51ConnectionOverlay>(root);
            UI51Build.Wire(overlay, so =>
            {
                UI51Build.Ref(so, "view", view.gameObject);
                UI51Build.Ref(so, "reconnectCard", reconnect);
                UI51Build.Ref(so, "spinner", spinner);
                UI51AccessBuilder.SetArray(so, "waves", w1.rectTransform, w2.rectTransform, w3.rectTransform);
                UI51AccessBuilder.SetArray(so, "attemptBars", bars);
                UI51Build.Ref(so, "attemptLabel", attempt);
                UI51Build.Ref(so, "seatBox", seatBlock.gameObject);
                UI51Build.Ref(so, "seatLabel", seat);
                UI51Build.Ref(so, "errorCard", error);
                UI51Build.Ref(so, "errorIcon", icon);
                UI51Build.Ref(so, "errorText", errorText);
                UI51Build.Ref(so, "retry", retry.GetComponent<Button>());
                UI51Build.Ref(so, "exit", exit.GetComponent<Button>());
                UI51Build.Ref(so, "exitLabel", exit.Find("Label").GetComponent<TextMeshProUGUI>());
            });
        }

        // --- Servizio

        /// <summary>
        /// Due schermate a tutto schermo sulla foto della Home con la sfumatura del mockup, margini 24, che prendono tutti i tocchi.
        /// Aggiornamento: bagliore 300 e logo 150 centrati a 180; da top 310 pillola versione, titolo 24, testo; da top 470 il
        /// pannello NOVITÀ (fino a 3 righe con ✦); a 30 dal fondo AGGIORNA ORA (54) e la versione installata.
        /// Manutenzione: da top 150 ingranaggio 80 nel bagliore 220, MANUTENZIONE, titolo, testo; da top 470 il conto alla rovescia;
        /// a 30 dal fondo RIPROVA (54) e "Leggi le novità" (46).
        /// </summary>
        static void BuildService(GameObject root)
        {
            OverlayCanvas(root, 3500, true);
            var bg = UI51Build.Sprite("Backgrounds", "home_bg_base");
            var glow = UI51Build.Sprite("Common", "Bagliore_morbido");

            // Aggiornamento
            var update = UI51Build.Stretch(UI51Build.Child(root.transform, "Update"));
            var safe = UI51AccessBuilder.BuildScreen(update, bg, UI51Shape.Linear(
                (UI51Tokens.Rgba(6, 14, 28, 0.3f), 0f), (UI51Tokens.Rgba(5, 11, 24, 0.55f), 0.3f),
                (UI51Tokens.Rgba(4, 9, 20, 0.92f), 0.55f), (UI51Tokens.Rgba(3, 7, 16, 0.98f), 1f)));
            UI51Build.Image(UI51AccessBuilder.CenterAt(UI51Build.Child(safe, "Glow"), 195f, 180f, 300f, 300f), glow, UI51Tokens.WhiteA(0.8f));
            UI51AccessBuilder.Logo(safe, 195f, 180f, 150f);

            var head = Band(safe, "Head", 310f);
            var pill = UI51Build.Child(head, "Version");
            UI51Build.Layout(pill, -1f, 24f);
            UI51Build.Solid(pill, UI51Tokens.GoldA(0.15f), 12f, 1f, UI51Tokens.GoldA(0.5f));
            UI51Build.Row(pill, 0f, UI51Build.Pad(0, 10, 0, 10), TextAnchor.MiddleCenter, true, true);
            var version = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(pill, "Label"), "Versione 2.50 disponibile", FontFace.NunitoExtraBold, 11f,
                UI51Tokens.Gold, TextAlignmentOptions.Center));
            UI51MetaBuilder.Gap(head, "GapTitle", 8f);
            Headline(head, "È ora di aggiornare");
            UI51MetaBuilder.Gap(head, "GapText", 8f);
            Paragraph(head, "Per continuare a giocare online serve la nuova versione. Ci vuole meno di un minuto.");

            var newsHolder = Band(safe, "News", 470f);
            var news = UI51Build.Child(newsHolder, "Panel");
            UI51Build.Shape(news, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.GoldA(0.25f));
            UI51Build.Layout(news, -1f, -1f, 1f);
            UI51MetaBuilder.Stack(news, 10f, UI51Build.Pad(14, 16, 14, 16));
            Caption(news, "NOVITÀ", 2f, TextAlignmentOptions.MidlineLeft);
            var newsRows = new Object[3];
            var newsTexts = new Object[3];
            string[] sample = { "Banner animati per il tuo profilo", "Classifica settimanale con premi", "Partita guidata per i nuovi giocatori" };
            for (int i = 0; i < newsRows.Length; i++)
            {
                var row = UI51Build.Child(news, "Row" + i);
                UI51Build.Row(row, 10f, null, TextAnchor.UpperLeft, true, true).childForceExpandHeight = false;
                var dot = UI51Build.Child(row, "Dot");
                UI51Build.Layout(dot, 20f, 20f);
                UI51Build.Solid(dot, UI51Tokens.GoldA(0.15f), 10f);
                // ✦ non c'e' in nessun font del progetto: stella a 4 punte disegnata (8 px, tratto pieno quasi quanto il glifo).
                Stroke(UI51Build.Center(UI51Build.Child(dot, "Star"), 9f, 9f), new Vector2(10f, 10f), 1.3f, UI51Tokens.Gold, new Vector2(5f, 0.6f), new Vector2(6.1f, 3.9f), new Vector2(9.4f, 5f),
                    new Vector2(6.1f, 6.1f), new Vector2(5f, 9.4f), new Vector2(3.9f, 6.1f), new Vector2(0.6f, 5f), new Vector2(3.9f, 3.9f), new Vector2(5f, 0.6f));
                var text = UI51Build.Text(UI51Build.Child(row, "Text"), sample[i], FontFace.NunitoRegular, 13f, UI51Tokens.Cream, TextAlignmentOptions.TopLeft);
                UI51MetaBuilder.Wrap(text, 3.6f); // line-height 1.4
                UI51Build.Layout(text, -1f, -1f, 1f);
                newsRows[i] = row.gameObject;
                newsTexts[i] = text;
            }

            var updateBottom = Bottom(safe, "Bottom", 79f);
            var updateNow = UI51Build.Child(updateBottom, "UpdateNow");
            UI51PrefabBuilder.GoldBody(updateNow.gameObject, 342f, 54f, 16f, FontFace.CinzelBold, 15f, 2f, "AGGIORNA ORA");
            UI51Build.Layout(updateNow, -1f, 54f, 1f);
            UI51MetaBuilder.Gap(updateBottom, "Gap", 10f);
            var installed = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(updateBottom, "Installed"),
                "Versione installata 2.44 · I tuoi progressi sono salvati", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.45f), TextAlignmentOptions.Center));
            UI51Build.Layout(installed, -1f, 15f, 1f);

            // Manutenzione
            var maintenance = UI51Build.Stretch(UI51Build.Child(root.transform, "Maintenance"));
            safe = UI51AccessBuilder.BuildScreen(maintenance, bg, UI51Shape.Linear(
                (UI51Tokens.Rgba(4, 9, 20, 0.6f), 0f), (UI51Tokens.Rgba(4, 9, 20, 0.75f), 0.4f), (UI51Tokens.Rgba(3, 7, 16, 0.97f), 1f)));
            head = Band(safe, "Head", 150f);
            var art = UI51Build.Child(head, "Art");
            UI51Build.Layout(art, 120f, 120f);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(art, "Glow"), 220f, 220f), glow, UI51Tokens.WhiteA(0.6f));
            var gear = UI51Build.Center(UI51Build.Child(art, "Gear"), 80f, 80f);
            UI51Build.Image(gear, UI51Build.Sprite("Common", "ic_settings_cream"), Color.white);
            UI51MetaBuilder.Gap(head, "GapCaption", 18f); // gap 10 + margin-top 8
            Caption(head, "MANUTENZIONE", 3f, TextAlignmentOptions.Center);
            UI51MetaBuilder.Gap(head, "GapTitle", 10f);
            Headline(head, "Stiamo sistemando il tavolo");
            UI51MetaBuilder.Gap(head, "GapText", 10f);
            Paragraph(head, "I server sono in manutenzione per migliorare il gioco. Le partite in corso sono state salvate.");

            var timerHolder = Band(safe, "Timer", 470f);
            var timer = UI51Build.Child(timerHolder, "Panel");
            UI51Build.Shape(timer, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.GoldA(0.25f));
            UI51Build.Layout(timer, -1f, -1f, 1f);
            UI51MetaBuilder.Stack(timer, 6f, UI51Build.Pad(16, 16, 16, 16));
            Caption(timer, "TORNIAMO TRA CIRCA", 2f, TextAlignmentOptions.Center);
            var countdown = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(timer, "Countdown"), "42:18", FontFace.CinzelBold, 34f,
                UI51Tokens.GoldLight, TextAlignmentOptions.Center, 2f));
            UI51Build.Layout(countdown, -1f, 46f, 1f);
            var end = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(timer, "End"), "Fine prevista alle 4:00", FontFace.NunitoRegular, 11f,
                UI51Tokens.CreamA(0.5f), TextAlignmentOptions.Center));
            UI51Build.Layout(end, -1f, 15f, 1f);

            var maintenanceBottom = Bottom(safe, "Bottom", 110f);
            var retry = UI51Build.Child(maintenanceBottom, "Retry");
            UI51PrefabBuilder.GoldBody(retry.gameObject, 342f, 54f, 16f, FontFace.CinzelBold, 15f, 2f, "RIPROVA");
            UI51Build.Layout(retry, -1f, 54f, 1f);
            UI51MetaBuilder.Gap(maintenanceBottom, "Gap", 10f);
            var readNews = UI51Build.Child(maintenanceBottom, "ReadNews");
            UI51PrefabBuilder.ButtonBody(readNews.gameObject, 342f, 46f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(14f), 1f, UI51Tokens.GoldA(0.5f),
                FontFace.NunitoExtraBold, 14f, 0f, UI51Tokens.Gold, "Leggi le novità");
            UI51Build.Layout(readNews, -1f, 46f, 1f);

            update.gameObject.SetActive(false);
            maintenance.gameObject.SetActive(false);
            var screen = UI51Build.GetOrAdd<UI51ServiceScreen>(root);
            UI51Build.Wire(screen, so =>
            {
                UI51Build.Ref(so, "updateView", update.gameObject);
                UI51Build.Ref(so, "versionLabel", version);
                UI51Build.Ref(so, "newsPanel", newsHolder.gameObject);
                UI51AccessBuilder.SetArray(so, "newsRows", newsRows);
                UI51AccessBuilder.SetArray(so, "newsTexts", newsTexts);
                UI51Build.Ref(so, "installedLabel", installed);
                UI51Build.Ref(so, "updateNow", updateNow.GetComponent<Button>());
                UI51Build.Ref(so, "maintenanceView", maintenance.gameObject);
                UI51Build.Ref(so, "gear", gear);
                UI51Build.Ref(so, "countdown", countdown);
                UI51Build.Ref(so, "endLabel", end);
                UI51Build.Ref(so, "retry", retry.GetComponent<Button>());
                UI51Build.Ref(so, "readNews", readNews.GetComponent<Button>());
            });
        }

        /// <summary>Colonna centrata a 24 dai lati, da top px, alta quanto il contenuto.</summary>
        static RectTransform Band(RectTransform safe, string name, float top)
        {
            var band = UI51AccessBuilder.TopBand(UI51Build.Child(safe, name), 24f, 24f, top, 100f);
            UI51Build.Column(band, 0f, null, TextAnchor.UpperCenter, true, true);
            UI51Build.Fit(band, false, true);
            return band;
        }

        /// <summary>Colonna a 24 dai lati e 30 dal fondo, alta h.</summary>
        static RectTransform Bottom(RectTransform safe, string name, float h)
        {
            var band = UI51Build.Child(safe, name);
            band.anchorMin = Vector2.zero;
            band.anchorMax = new Vector2(1f, 0f);
            band.pivot = new Vector2(0.5f, 0f);
            band.offsetMin = new Vector2(24f, 30f);
            band.offsetMax = new Vector2(-24f, 30f + h);
            UI51MetaBuilder.Stack(band, 0f);
            return band;
        }

        static void Headline(RectTransform parent, string text)
        {
            var t = UI51Build.Text(UI51Build.Child(parent, "Title"), text, FontFace.CinzelBold, 24f, UI51Tokens.Cream, TextAlignmentOptions.Center);
            UI51MetaBuilder.Wrap(t, 0f);
            UI51Build.Layout(t, -1f, -1f, 1f);
        }

        static void Paragraph(RectTransform parent, string text)
        {
            var t = UI51Build.Text(UI51Build.Child(parent, "Text"), text, FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.7f), TextAlignmentOptions.Center);
            UI51MetaBuilder.Wrap(t, 13.6f); // line-height 1.5
            UI51Build.Layout(t, -1f, -1f, 1f);
        }

        static void Caption(RectTransform parent, string text, float spacing, TextAlignmentOptions align)
        {
            var t = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(parent, "Caption"), text, FontFace.CinzelSemiBold, 10f, UI51Tokens.Gold, align, spacing));
            UI51Build.Layout(t, -1f, 14f, 1f);
        }

        /// <summary>
        /// Card a 28 dai lati e top px dall'alto, alta quanto il contenuto. Il supporto (Holder) la impagina, la card (con pivot al centro)
        /// e' quella che fa il pop. Ritorna la card: colonna centrata, figli larghi quanto vogliono (testi a tutta larghezza).
        /// </summary>
        static RectTransform Card(RectTransform safe, string name, float top, Color border)
        {
            var holder = UI51AccessBuilder.TopBand(UI51Build.Child(safe, name), 28f, 28f, top, 300f);
            UI51MetaBuilder.Stack(holder, 0f);
            UI51Build.Fit(holder, false, true);
            var card = UI51Build.Child(holder, "Card");
            UI51Build.Shape(card, UI51Tokens.DialogFill(), 180f, UI51Tokens.Radii(22f), 1f, border, true, UI51Tokens.ShadowDialog);
            UI51Build.Column(card, 0f, UI51Build.Pad(24, 20, 20, 20), TextAnchor.UpperCenter, true, true);
            return card;
        }

        static void Title(RectTransform card, string text)
        {
            var t = UI51Build.Text(UI51Build.Child(card, "Title"), text, FontFace.CinzelBold, 19f, UI51Tokens.Cream, TextAlignmentOptions.Center);
            UI51MetaBuilder.Wrap(t, 0f);
            UI51Build.Layout(t, -1f, -1f, 1f);
        }

        static TextMeshProUGUI Body(RectTransform card, string name, string text)
        {
            var t = UI51Build.Text(UI51Build.Child(card, name), text, FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.65f), TextAlignmentOptions.Center);
            UI51MetaBuilder.Wrap(t, 13.6f); // line-height 1.5
            UI51Build.Layout(t, -1f, -1f, 1f);
            return t;
        }

        static UI51Polyline Stroke(RectTransform rt, Vector2 viewBox, float width, Color color, params Vector2[] points)
        {
            var line = UI51Build.GetOrAdd<UI51Polyline>(rt);
            line.Set(viewBox, width, points);
            line.color = color;
            line.raycastTarget = false;
            return line;
        }

        /// <summary>Pallino (circle cx cy r del mockup) in un riquadro con origine in alto a sinistra.</summary>
        static void Dot(RectTransform parent, Vector2 center, float r, Color color) =>
            UI51Build.Solid(UI51Build.Place(UI51Build.Child(parent, "Dot"), new Vector2(0f, 1f), new Vector2(r * 2f, r * 2f),
                new Vector2(center.x - r, -(center.y - r))), color, r);
    }
}
