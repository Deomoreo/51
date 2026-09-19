using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Components;
using Project51.UIV2.Core;

namespace Project51.EditorTools
{
    /// <summary>
    /// Pannelli online (MainMenu: crea stanza, entra con codice, ricerca, sala d'attesa host/ospite) e
    /// risultati al tavolo (GameScene: fine smazzata, fine partita), dai mockup
    /// screen_1_ricerca_partita, screen_3_entra_codice, 03_crea_stanza_con_bot, 04_lobby_non_host,
    /// 12_fine_partita, 13_fine_smazzata. Coordinate = pixel del mockup 1080x1920 (y dall'alto) dentro un
    /// contenitore DesignCanvasFit. Ogni menu apre la propria scena, ricostruisce da zero i propri
    /// pannelli (rilanciabile) e salva.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";

        private static readonly Color OnDim = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color OnFrameBorder = new Color32(227, 169, 60, 255);
        private static readonly Color OnFrameFill = new Color32(27, 41, 65, 255);
        private static readonly Color OnSoftText = new Color32(195, 206, 224, 255);
        private static readonly Color OnGold = new Color32(240, 182, 74, 255);
        private static readonly Color OnLine = new Color32(52, 73, 106, 255);
        private static readonly Color OnRowFill = new Color32(31, 49, 80, 255);
        private static readonly Color OnRowBorder = new Color32(58, 86, 128, 255);
        private static readonly Color OnSlotFill = new Color32(20, 31, 52, 255);
        private static readonly Color OnSlotDash = new Color32(62, 84, 116, 255);
        private static readonly Color OnSlotText = new Color32(126, 147, 180, 255);
        private static readonly Color OnCellFill = new Color32(14, 24, 40, 255);
        private static readonly Color OnError = new Color32(240, 120, 138, 255);
        private static readonly Color OnTrack = new Color32(12, 21, 36, 255);
        private static readonly Color OnProgressGold = new Color32(230, 176, 66, 255);
        private static readonly Color OnScoreGold = new Color32(255, 210, 122, 255);
        private static readonly Color OnResultRowFill = new Color32(19, 30, 50, 255);
        private static readonly Color OnResultRowBorder = new Color32(52, 80, 120, 255);

        // ------------------------------------------------------------------
        // MainMenu: flusso online
        // ------------------------------------------------------------------

        [MenuItem("Tools/UIV2/Build Online Flow")]
        private static void BuildOnlineFlow()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            var flow = Object.FindObjectOfType<RoomFlowV2>(true);
            if (flow == null) throw new System.Exception("RoomFlowV2 (OnlineFlowV2) non trovato in MainMenu");
            var root = flow.transform;
            foreach (var old in new[] { "CreateRoom", "JoinRoom", "SearchMatch", "Lobby", "LobbyHost", "LobbyGuest" })
            {
                var child = root.Find(old);
                if (child != null) Object.DestroyImmediate(child.gameObject);
            }

            var closes = new List<Button>();
            BuildCreatePanel(flow, root, closes);
            BuildJoinPanel(flow, root, closes);
            BuildSearchPanel(flow, root, closes);
            BuildLobbyHostPanel(flow, root, closes);
            BuildLobbyGuestPanel(flow, root, closes);
            flow.CloseButtons = closes.ToArray();
            HidePanels(flow.CreatePanel, flow.JoinPanel, flow.SearchPanel, flow.LobbyHostPanel, flow.LobbyGuestPanel);

            var legacy = root.Find("LegacyOnlineViews");
            if (legacy != null) legacy.SetAsLastSibling();
            EditorUtility.SetDirty(flow);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Online flow V2 ricostruito in MainMenu.");
        }

        private static void BuildCreatePanel(RoomFlowV2 flow, Transform root, List<Button> closes)
        {
            var design = OnlinePanel(root, "CreateRoom", out var panel);
            flow.CreatePanel = panel;
            closes.Add(ModalFrame(design, 420f, 1330f, "CREA STANZA"));
            MockText(design, "Hint", "Scegli il formato del tavolo", 90f, 560f, 900f, 44f, 26f, FontStyles.Normal, OnSoftText, TextAlignmentOptions.Center);

            string[] titles = { "1 vs 1", "2 vs 2", "1 vs 3" };
            string[] details = { "2 giocatori", "a coppie", "4 giocatori" };
            flow.Formats = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                var button = MockButton(design, "Format" + i, "btn_blue_mid", 90f + i * 306.5f, 640f, 285f, 120f, "", 0f, null);
                MockText(button.transform, "Title", titles[i], 0f, 18f, 285f, 50f, 36f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center)
                    .fontSharedMaterial = NavyOutlineMaterial();
                MockText(button.transform, "Detail", details[i], 0f, 68f, 285f, 32f, 21f, FontStyles.Normal, OnSoftText, TextAlignmentOptions.Center);
                flow.Formats[i] = button;
            }
            flow.FormatSelectedSprite = LoadSprite(IconsPath, "btn_teal");
            flow.FormatNormalSprite = LoadSprite(IconsPath, "btn_blue_mid");
            flow.CreateFormat = MockText(design, "SelectedFormat", "Formato: 1 vs 1", 90f, 800f, 900f, 50f, 32f, FontStyles.Bold, OnGold, TextAlignmentOptions.Center);
            MockText(design, "Explanation", "La stanza è privata: il codice appare subito\ne potrai invitare amici o aggiungere bot.",
                90f, 870f, 900f, 110f, 25f, FontStyles.Normal, OnSoftText, TextAlignmentOptions.Center).lineSpacing = 30f;
            flow.Create = MockButton(design, "Create", "btn_gold_long", 196f, 1080f, 688f, 92f, "CREA STANZA", 42f, BrownOutlineMaterial());
            closes.Add(MockButton(design, "Cancel", "btn_gray_small", 305f, 1225f, 470f, 50f, "ANNULLA", 24f, NavyOutlineMaterial()));
        }

        private static void BuildJoinPanel(RoomFlowV2 flow, Transform root, List<Button> closes)
        {
            var design = OnlinePanel(root, "JoinRoom", out var panel);
            flow.JoinPanel = panel;
            closes.Add(ModalFrame(design, 380f, 1450f, "ENTRA IN STANZA"));
            MockText(design, "Hint", "Inserisci il codice a 5 caratteri\nche ti ha inviato il tuo amico", 90f, 515f, 900f, 100f, 26f,
                FontStyles.Normal, OnSoftText, TextAlignmentOptions.Center).lineSpacing = 40f;

            flow.JoinCells = CodeCells(design, "Cells", 218f, 648f, 116f, 144f, 132f, 64f);
            // Campo invisibile sopra le caselle: riceve tastiera e incolla, le caselle mostrano il codice.
            var fieldRect = MockRect(design, "CodeInput", 218f, 648f, 644f, 144f);
            var fieldImage = fieldRect.gameObject.AddComponent<Image>();
            fieldImage.color = new Color(0f, 0f, 0f, 0.001f);
            var input = fieldRect.gameObject.AddComponent<TMP_InputField>();
            var inputText = AddText(Stretch(fieldRect, "Text"), "", 40f, FontStyles.Normal, Color.clear, TextAlignmentOptions.Center);
            input.textViewport = fieldRect;
            input.textComponent = inputText;
            input.characterLimit = 5;
            input.contentType = TMP_InputField.ContentType.Alphanumeric;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.customCaretColor = true;
            input.caretColor = Color.clear;
            input.selectionColor = Color.clear;
            flow.CodeInput = input;

            var errorRow = MockRect(design, "Error", 90f, 834f, 900f, 46f);
            var layout = errorRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var icon = CreateUIObject("Icon", errorRow);
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = LoadSprite(IconsPath, "ic_warn");
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            var iconLayout = icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = iconLayout.preferredHeight = 34f;
            flow.JoinError = AddText(CreateUIObject("Message", errorRow), "Codice non valido o stanza piena", 23f, FontStyles.Normal, OnError, TextAlignmentOptions.MidlineLeft);
            flow.JoinErrorRow = errorRow.gameObject;

            flow.Paste = MockButton(design, "Paste", "btn_blue_long", 265f, 928f, 548f, 72f, "", 0f, null);
            ButtonIconLabel(flow.Paste, "ic_cards", 24f, 16f, 40f, "INCOLLA", 78f, 28f);
            flow.Join = MockButton(design, "Join", "btn_gold_long", 196f, 1198f, 688f, 92f, "ENTRA", 42f, BrownOutlineMaterial());
            closes.Add(MockButton(design, "Cancel", "btn_gray_small", 305f, 1345f, 470f, 50f, "ANNULLA", 24f, NavyOutlineMaterial()));
        }

        private static void BuildSearchPanel(RoomFlowV2 flow, Transform root, List<Button> closes)
        {
            var design = OnlinePanel(root, "SearchMatch", out var panel);
            flow.SearchPanel = panel;
            closes.Add(ModalFrame(design, 270f, 1700f, "RICERCA PARTITA"));

            var ring = MockRect(design, "Spinner", 444f, 474f, 192f, 192f).gameObject.AddComponent<RingArcGraphic>();
            ring.Thickness = 9f;
            ring.raycastTarget = false;
            MockSprite(design, "CardBack", LoadSprite(IconsPath, "card_back_green"), 504f, 519f, 72f, 102f, false);
            flow.SearchStatus = MockText(design, "Status", "Ricerca giocatori…", 90f, 720f, 900f, 56f, 36f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            flow.SearchDetail = MockText(design, "Detail", "", 90f, 778f, 900f, 40f, 23f, FontStyles.Normal, OnSoftText, TextAlignmentOptions.Center);
            flow.SearchCount = SectionHeader(design, "PlayersHeader", "GIOCATORI 4/4", 100f, 862f, 880f);

            flow.SearchRows = new LobbySlotRowV2[4];
            for (int i = 0; i < 4; i++)
                flow.SearchRows[i] = LobbyRow(design, "Player" + i, 88f, 922f + i * 128f, 904f, 108f, false);

            var track = MockRect(design, "Progress", 88f, 1457f, 904f, 26f);
            AddRoundedPanel(track, "panel_fill_r24", 48f, 13f, OnTrack, 0f, OnTrack, out _, out _);
            var fill = CreateUIObject("Fill", track);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.4f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
            fillImage.type = Image.Type.Sliced;
            fillImage.pixelsPerUnitMultiplier = 48f / 13f;
            fillImage.color = OnProgressGold;
            fillImage.raycastTarget = false;
            flow.SearchProgressFill = fill;

            closes.Add(MockButton(design, "Cancel", "btn_gray_small", 225f, 1596f, 630f, 54f, "ANNULLA", 30f, NavyOutlineMaterial()));
        }

        private static void BuildLobbyHostPanel(RoomFlowV2 flow, Transform root, List<Button> closes)
        {
            var design = OnlinePanel(root, "LobbyHost", out var panel);
            flow.LobbyHostPanel = panel;
            closes.Add(ModalFrame(design, 200f, 1740f, "STANZA PRIVATA"));
            MockText(design, "Hint", "Condividi il codice con i tuoi amici", 90f, 330f, 900f, 40f, 26f, FontStyles.Normal, OnSoftText, TextAlignmentOptions.Center);
            flow.HostCells = CodeCells(design, "Cells", 232f, 388f, 112f, 136f, 126f, 64f);

            flow.HostCopy = MockButton(design, "Copy", "btn_teal", 275f, 564f, 530f, 72f, "", 0f, null);
            ButtonIconLabel(flow.HostCopy, "ic_cards", 22f, 16f, 40f, "COPIA CODICE", 72f, 28f);
            flow.HostFeedback = MockText(design, "Feedback", "", 90f, 640f, 900f, 34f, 20f, FontStyles.Normal, OnSoftText, TextAlignmentOptions.Center);

            SectionHeader(design, "InviteHeader", "INVITA", 100f, 696f, 880f);
            string[] labels = { "Condividi", "Link", "Amici" };
            string[] icons = { "ic_mail", "ic_lock", "ic_person" };
            for (int i = 0; i < 3; i++)
            {
                var button = MockButton(design, "Invite" + labels[i], "btn_blue_mid", 90f + i * 306.5f, 755f, 285f, 110f, "", 0f, null);
                MockSprite(button.transform, "Icon", LoadSprite(IconsPath, icons[i]), 118.5f, 14f, 48f, 48f, false);
                MockText(button.transform, "Label", labels[i], 0f, 70f, 285f, 32f, 21f, FontStyles.Normal, OnSoftText, TextAlignmentOptions.Center);
                if (i == 0) flow.ShareButton = button;
                else button.interactable = false; // link di invito e lista amici: nessun servizio dietro
            }

            flow.HostCount = SectionHeader(design, "PlayersHeader", "GIOCATORI 4/4", 100f, 926f, 880f);
            flow.HostRows = new LobbySlotRowV2[4];
            for (int i = 0; i < 4; i++)
                flow.HostRows[i] = LobbyRow(design, "Player" + i, 88f, 980f + i * 120f, 904f, 104f, true);

            flow.HostHint = MockText(design, "HostHint", "Solo l'host può aggiungere bot o avviare", 90f, 1512f, 900f, 36f, 21f, FontStyles.Normal, OnSoftText, TextAlignmentOptions.Center);
            flow.StartGameButton = MockButton(design, "Start", "btn_gold_long", 196f, 1568f, 688f, 88f, "AVVIA PARTITA", 40f, BrownOutlineMaterial());
            closes.Add(MockButton(design, "Leave", "btn_gray_small", 344f, 1680f, 391f, 40f, "ESCI", 22f, NavyOutlineMaterial()));
        }

        private static void BuildLobbyGuestPanel(RoomFlowV2 flow, Transform root, List<Button> closes)
        {
            var design = OnlinePanel(root, "LobbyGuest", out var panel);
            flow.LobbyGuestPanel = panel;
            closes.Add(ModalFrame(design, 300f, 1620f, "SALA D'ATTESA"));
            flow.GuestSubtitle = MockText(design, "Subtitle", "Stanza di", 90f, 426f, 900f, 46f, 29f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            flow.GuestCells = CodeCells(design, "Cells", 287f, 495f, 92f, 110f, 104f, 56f);

            flow.GuestCopy = MockButton(design, "Copy", "btn_blue_long", 305f, 643f, 470f, 64f, "", 0f, null);
            ButtonIconLabel(flow.GuestCopy, "ic_cards", 22f, 12f, 38f, "COPIA CODICE", 70f, 26f);
            flow.GuestFeedback = MockText(design, "Feedback", "", 90f, 710f, 900f, 34f, 20f, FontStyles.Normal, OnSoftText, TextAlignmentOptions.Center);

            flow.GuestCount = SectionHeader(design, "PlayersHeader", "GIOCATORI 4/4", 100f, 764f, 880f);
            flow.GuestRows = new LobbySlotRowV2[4];
            for (int i = 0; i < 4; i++)
                flow.GuestRows[i] = LobbyRow(design, "Player" + i, 88f, 817f + i * 120f, 904f, 104f, false);

            var ring = MockRect(design, "Spinner", 494f, 1351f, 92f, 92f).gameObject.AddComponent<RingArcGraphic>();
            ring.Thickness = 9f;
            ring.raycastTarget = false;
            flow.GuestStatus = MockText(design, "Status", "In attesa che l'host avvii la partita…", 90f, 1468f, 900f, 40f, 27f, FontStyles.Bold, OnSoftText, TextAlignmentOptions.Center);
            closes.Add(MockButton(design, "Leave", "btn_gray_small", 296f, 1512f, 489f, 56f, "ESCI DALLA STANZA", 24f, NavyOutlineMaterial()));
        }

        // ------------------------------------------------------------------
        // GameScene: risultati
        // ------------------------------------------------------------------

        [MenuItem("Tools/UIV2/Build Match Results")]
        private static void BuildMatchResults()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var social = Object.FindObjectOfType<GameSocialV2>(true);
            if (social == null) throw new System.Exception("GamePresentationV2 non trovato in GameScene");
            var root = social.transform;
            foreach (var old in new[] { "Results", "RoundResults", "MatchResults" })
            {
                var child = root.Find(old);
                if (child != null) Object.DestroyImmediate(child.gameObject);
            }
            var results = root.GetComponent<MatchResultsV2>() ?? root.gameObject.AddComponent<MatchResultsV2>();
            BuildRoundResults(results, root);
            BuildMatchEnd(results, root);
            HidePanels(results.RoundPanel, results.MatchPanel);
            EditorUtility.SetDirty(results);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Risultati V2 ricostruiti in GameScene.");
        }

        private static void BuildRoundResults(MatchResultsV2 results, Transform root)
        {
            var design = OnlinePanel(root, "RoundResults", out var panel);
            results.RoundPanel = panel;
            ModalFrame(design, 300f, 1620f, "FINE SMAZZATA", withClose: false);
            results.RoundSubtitle = MockText(design, "Subtitle", "", 90f, 430f, 900f, 40f, 25f, FontStyles.Normal, OnSoftText, TextAlignmentOptions.Center);

            results.RoundRows = new ResultRowV2[4];
            for (int i = 0; i < 4; i++)
                results.RoundRows[i] = ResultRow(design, "Score" + i, 80f, 500f + i * 120f, 920f, 104f);

            SectionHeader(design, "AwardsHeader", "PUNTI ASSEGNATI", 100f, 1005f, 880f);
            string[] categories = { "Carte", "Denari", "Settebello", "Primiera" };
            results.AwardWinners = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                var card = MockRect(design, "Award" + categories[i], 85f + (i % 2) * 464f, 1048f + (i / 2) * 100f, 446f, 84f);
                AddRoundedPanel(card, "panel_fill_r24", 48f, 18f, new Color32(47, 69, 104, 255), 2f, new Color32(18, 29, 48, 255), out _, out _);
                MockText(card, "Category", categories[i], 21f, 12f, 410f, 30f, 22f, FontStyles.Normal, OnSoftText, TextAlignmentOptions.MidlineLeft);
                var winner = MockText(card, "Winner", "Nessuno", 21f, 42f, 410f, 34f, 24f, FontStyles.Bold, OnGold, TextAlignmentOptions.MidlineLeft);
                winner.enableAutoSizing = true;
                winner.fontSizeMin = 16f;
                winner.fontSizeMax = 24f;
                results.AwardWinners[i] = winner;
            }

            results.RoundContinue = MockButton(design, "Continue", "btn_gold_long", 186f, 1452f, 706f, 88f, "CONTINUA", 40f, BrownOutlineMaterial());
            results.RoundContinueLabel = results.RoundContinue.GetComponentInChildren<TMP_Text>();
            // Come nel mockup nessuna uscita qui: si abbandona dalle Impostazioni in partita (ingranaggio).
            results.RoundExit = null;
        }

        private static void BuildMatchEnd(MatchResultsV2 results, Transform root)
        {
            var design = OnlinePanel(root, "MatchResults", out var panel);
            results.MatchPanel = panel;
            // Schermo intero senza cornice: fondo pieno al posto dell'oscuramento.
            panel.transform.Find("Dim").GetComponent<Image>().color = new Color32(14, 22, 38, 255);

            var confetti = CreateUIObject("Confetti", design);
            StretchFill(confetti);
            results.ConfettiRoot = confetti;
            Color[] palette = { new Color32(84, 204, 170, 255), new Color32(236, 190, 84, 255), new Color32(214, 82, 96, 255),
                new Color32(232, 222, 196, 255), new Color32(92, 132, 220, 255) };
            var random = new System.Random(51);
            for (int i = 0; i < 42; i++)
            {
                float w = 12f + random.Next(0, 12), h = 22f + random.Next(0, 16);
                var piece = CreateUIObject("Piece" + i, confetti);
                piece.anchorMin = piece.anchorMax = new Vector2(0f, 1f);
                piece.anchoredPosition = new Vector2(random.Next(20, 1060), -random.Next(0, 620));
                piece.sizeDelta = new Vector2(w, h);
                piece.localEulerAngles = new Vector3(0f, 0f, random.Next(0, 360));
                var image = piece.gameObject.AddComponent<Image>();
                image.color = palette[i % palette.Length];
                image.raycastTarget = false;
            }

            MockSprite(design, "Ribbon", LoadSprite(IconsPath, "ribbon_teal"), 230f, 116f, 620f, 227f, false);
            results.MatchTitle = MockText(design, "Title", "HAI VINTO!", 280f, 190f, 520f, 76f, 58f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            results.MatchTitle.fontSharedMaterial = NavyOutlineMaterial();
            MockSprite(design, "Trophy", LoadSprite(IconsPath, "ic_trophy"), 445f, 300f, 190f, 200f, false);
            results.MatchWinnerLine = MockText(design, "Winner", "", 90f, 542f, 900f, 52f, 34f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            results.MatchRows = new ResultRowV2[4];
            for (int i = 0; i < 4; i++)
                results.MatchRows[i] = ResultRow(design, "Score" + i, 62f, 622f + i * 128f, 956f, 108f);

            SectionHeader(design, "DetailHeader", "DETTAGLIO ULTIMA SMAZZATA", 84f, 1152f, 912f);
            var box = MockRect(design, "Detail", 62f, 1197f, 956f, 350f);
            AddRoundedPanel(box, "panel_fill_r24", 48f, 24f, OnResultRowBorder, 3f, new Color32(14, 22, 36, 255), out _, out _);
            results.DetailLabels = new TMP_Text[7];
            results.DetailValues = new TMP_Text[7];
            for (int i = 0; i < 7; i++)
            {
                results.DetailLabels[i] = MockText(box, "Line" + i, "", 38f, 12f + i * 40f, 700f, 40f, 25f, FontStyles.Normal, new Color32(205, 214, 230, 255), TextAlignmentOptions.MidlineLeft);
                results.DetailValues[i] = MockText(box, "Value" + i, "", 776f, 12f + i * 40f, 142f, 40f, 25f, FontStyles.Bold, OnScoreGold, TextAlignmentOptions.MidlineRight);
            }
            var divider = MockRect(box, "Divider", 38f, 297f, 880f, 2f).gameObject.AddComponent<Image>();
            divider.color = OnLine;
            divider.raycastTarget = false;
            MockText(box, "TotalLabel", "Totale smazzata", 38f, 304f, 700f, 42f, 27f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            results.DetailTotal = MockText(box, "TotalValue", "", 776f, 304f, 142f, 42f, 27f, FontStyles.Bold, OnScoreGold, TextAlignmentOptions.MidlineRight);

            results.Rematch = MockButton(design, "Rematch", "btn_gold_long", 82f, 1595f, 442f, 95f, "RIVINCITA", 40f, BrownOutlineMaterial());
            results.RematchLabel = results.Rematch.GetComponentInChildren<TMP_Text>();
            results.MatchMenu = MockButton(design, "Menu", "btn_blue_long", 556f, 1595f, 442f, 95f, "MENU", 38f, NavyOutlineMaterial());
        }

        // ------------------------------------------------------------------
        // Pezzi composti
        // ------------------------------------------------------------------

        /// <summary>Pannello a schermo intero: oscuramento + area di design 1080x1920 dentro la safe area.</summary>
        private static RectTransform OnlinePanel(Transform root, string name, out GameObject panel)
        {
            var rect = CreateUIObject(name, root);
            StretchFill(rect);
            rect.gameObject.AddComponent<CanvasGroup>();
            var dim = CreateUIObject("Dim", rect);
            StretchFill(dim);
            var dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = OnDim;
            dimImage.raycastTarget = true; // blocca i tocchi verso la Home sotto
            var design = CreateUIObject("Design", rect);
            design.gameObject.AddComponent<DesignCanvasFit>();
            design.sizeDelta = new Vector2(1080f, 1920f);
            // Resta attivo durante la costruzione (TMP misura i testi solo su oggetti attivi):
            // lo spegne HidePanels a fine build.
            panel = rect.gameObject;
            return design;
        }

        private static void HidePanels(params GameObject[] panels)
        {
            foreach (var panel in panels) panel.SetActive(false);
        }

        /// <summary>Cornice oro, nastro con titolo e (opzionale) X di chiusura in alto a destra.</summary>
        private static Button ModalFrame(RectTransform design, float top, float bottom, string title, bool withClose = true)
        {
            var frame = MockRect(design, "Frame", 45f, top, 990f, bottom - top);
            AddRoundedPanel(frame, "panel_fill_r24", 48f, 44f, OnFrameBorder, 9f, OnFrameFill, out _, out _);
            frame.GetComponent<Image>().raycastTarget = true; // i tocchi sulla cornice non chiudono nulla
            MockSprite(design, "Ribbon", LoadSprite(IconsPath, "ribbon_teal"), 280f, top - 44f, 520f, 190f, false);
            var label = MockText(design, "Title", title, 330f, top + 12f, 420f, 62f, 44f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            label.enableAutoSizing = true;
            label.fontSizeMin = 30f;
            label.fontSizeMax = 44f;
            label.fontSharedMaterial = NavyOutlineMaterial();
            if (!withClose) return null;
            var close = MockButton(design, "Close", "sq_blue", 935f, top + 36f, 80f, 72f, "", 0f, null);
            MockSprite(close.transform, "Icon", LoadSprite(IconsPath, "ic_x"), 21f, 17f, 38f, 38f, false);
            return close;
        }

        private static CodeCellsV2 CodeCells(RectTransform parent, string name, float left, float top, float cellWidth, float cellHeight, float pitch, float letterSize)
        {
            var container = MockRect(parent, name, left, top, pitch * 4f + cellWidth, cellHeight);
            var cells = container.gameObject.AddComponent<CodeCellsV2>();
            cells.Letters = new TMP_Text[5];
            cells.Borders = new Image[5];
            cells.Underlines = new GameObject[5];
            for (int i = 0; i < 5; i++)
            {
                var cell = MockRect(container, "Cell" + i, i * pitch, 0f, cellWidth, cellHeight);
                cells.Borders[i] = AddRoundedPanel(cell, "panel_fill_r24", 48f, 16f, cells.FilledBorder, 4f, OnCellFill, out _, out _);
                var underline = MockRect(cell, "Underline", cellWidth * 0.2f, cellHeight - 28f, cellWidth * 0.6f, 4f).gameObject.AddComponent<Image>();
                underline.color = cells.EmptyBorder;
                underline.raycastTarget = false;
                cells.Underlines[i] = underline.gameObject;
                cells.Letters[i] = AddText(Stretch(cell, "Letter"), "K", letterSize, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
                cells.Letters[i].fontSharedMaterial = NavyOutlineMaterial();
            }
            return cells;
        }

        private static LobbySlotRowV2 LobbyRow(RectTransform parent, string name, float left, float top, float width, float height, bool hostControls)
        {
            var container = MockRect(parent, name, left, top, width, height);
            var row = container.gameObject.AddComponent<LobbySlotRowV2>();
            row.PlayerAvatar = LoadSprite(IconsPath, "frame_round");

            var filled = Stretch(container, "Filled");
            row.Filled = filled.gameObject;
            var filledBg = MockRect(filled, "Background", 0f, 0f, width, height);
            AddRoundedPanel(filledBg, "panel_fill_r24", 48f, 20f, OnRowBorder, 2f, OnRowFill, out _, out _);
            row.Avatar = MockSprite(filled, "Avatar", row.PlayerAvatar, 16f, (height - 62f) * 0.5f, 62f, 62f, false);
            row.Name = MockText(filled, "Name", "Giocatore", 94f, height * 0.5f - 36f, width - 300f, 40f, 30f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            row.Role = MockText(filled, "Role", "PRONTO", 94f, height * 0.5f + 4f, width - 300f, 30f, 19f, FontStyles.Bold, LobbySlotRowV2.ReadyColor, TextAlignmentOptions.MidlineLeft);
            row.Check = MockSprite(filled, "Check", LoadSprite(IconsPath, "ic_check"), width - 50f, height * 0.5f - 16f, 32f, 32f, false).gameObject;

            var empty = Stretch(container, "Empty");
            row.Empty = empty.gameObject;
            var emptyFill = MockRect(empty, "Background", 0f, 0f, width, height).gameObject.AddComponent<Image>();
            emptyFill.color = OnSlotFill;
            emptyFill.raycastTarget = false;
            AddDashedBorder(empty, width, height, 14f, 10f, 2f, OnSlotDash);
            var circle = MockRect(empty, "Circle", 24f, (height - 62f) * 0.5f, 62f, 62f);
            AddRoundedPanel(circle, "panel_fill_r24", 48f, 31f, OnRowBorder, 3f, OnSlotFill, out _, out _);
            row.EmptyLabel = MockText(empty, "Label", "Slot libero", 108f, height * 0.5f - 20f, width - 330f, 40f, 26f, FontStyles.Normal, OnSlotText, TextAlignmentOptions.MidlineLeft);
            empty.gameObject.SetActive(false);

            if (hostControls)
            {
                row.BotButton = MockButton(container, "BotButton", "btn_green_small", width - 186f, height * 0.5f - 22f, 166f, 44f, "+ BOT", 24f, NavyOutlineMaterial());
                row.BotButtonLabel = row.BotButton.GetComponentInChildren<TMP_Text>();
            }
            return row;
        }

        private static ResultRowV2 ResultRow(RectTransform parent, string name, float left, float top, float width, float height)
        {
            var container = MockRect(parent, name, left, top, width, height);
            var row = container.gameObject.AddComponent<ResultRowV2>();
            var background = MockRect(container, "Background", 0f, 0f, width, height);
            AddRoundedPanel(background, "panel_fill_r24", 48f, 22f, OnResultRowBorder, 2f, OnResultRowFill, out _, out _);
            var highlight = MockRect(container, "WinnerHighlight", 0f, 0f, width, height);
            AddRoundedPanel(highlight, "panel_fill_r24", 48f, 22f, OnFrameBorder, 5f, new Color32(38, 52, 84, 255), out _, out _);
            row.WinnerHighlight = highlight.gameObject;

            row.Rank = MockText(container, "Rank", "1", 14f, 0f, 44f, height, 30f, FontStyles.Bold, new Color32(170, 186, 210, 255), TextAlignmentOptions.Center);
            MockSprite(container, "Avatar", LoadSprite(IconsPath, "frame_round"), 66f, (height - 60f) * 0.5f, 60f, 60f, false);
            row.Name = MockText(container, "Name", "Giocatore", 140f, height * 0.5f - 36f, width - 380f, 40f, 29f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            row.Delta = MockText(container, "Delta", "+0 questa smazzata", 140f, height * 0.5f + 2f, width - 380f, 26f, 18f, FontStyles.Normal, OnSoftText, TextAlignmentOptions.MidlineLeft);

            var track = MockRect(container, "Progress", 140f, height * 0.5f + 26f, width - 258f, 16f);
            AddRoundedPanel(track, "panel_fill_r24", 48f, 8f, OnResultRowBorder, 2f, OnTrack, out _, out _);
            var fill = CreateUIObject("Fill", track);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.5f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
            fillImage.type = Image.Type.Sliced;
            fillImage.pixelsPerUnitMultiplier = 48f / 8f;
            fillImage.color = OnProgressGold;
            fillImage.raycastTarget = false;
            row.ProgressFill = fill;

            row.Score = MockText(container, "Points", "0", width - 190f, height * 0.5f - 42f, 166f, 52f, 40f, FontStyles.Bold, OnScoreGold, TextAlignmentOptions.MidlineRight);
            row.Score.enableAutoSizing = true;
            row.Score.fontSizeMin = 24f;
            row.Score.fontSizeMax = 40f;
            row.Target = MockText(container, "Target", "/ 51", width - 190f, height * 0.5f + 8f, 166f, 30f, 19f, FontStyles.Normal, OnSoftText, TextAlignmentOptions.MidlineRight);
            row.Trophy = MockSprite(container, "Trophy", LoadSprite(IconsPath, "ic_trophy"), width - 128f, height * 0.5f - 40f, 44f, 44f, false).gameObject;
            highlight.gameObject.SetActive(false);
            row.Trophy.SetActive(false);
            return row;
        }

        /// <summary>Titolo oro + linea sottile che parte subito dopo il testo.</summary>
        private static TMP_Text SectionHeader(RectTransform parent, string name, string title, float left, float top, float width)
        {
            var label = MockText(parent, name, title, left, top, width, 34f, 23f, FontStyles.Bold, OnGold, TextAlignmentOptions.MidlineLeft);
            float textWidth = label.GetPreferredValues(title).x;
            var line = MockRect(parent, name + "Line", left + textWidth + 20f, top + 16f, width - textWidth - 20f, 2f).gameObject.AddComponent<Image>();
            line.color = OnLine;
            line.raycastTarget = false;
            return label;
        }

        private static Button MockButton(Transform parent, string name, string spriteName, float left, float top, float width, float height,
            string label, float labelSize, Material labelMaterial)
        {
            var container = MockRect(parent, name, left, top, width, height);
            var art = MockSprite(container, "Art", LoadSprite(IconsPath, spriteName), 0f, 0f, width, height, true);
            art.raycastTarget = true;
            var button = container.gameObject.AddComponent<Button>();
            button.targetGraphic = art;
            var colors = button.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.55f);
            button.colors = colors;
            if (!string.IsNullOrEmpty(label))
            {
                var text = AddText(Stretch(container, "Label"), label, labelSize, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
                if (labelMaterial != null) text.fontSharedMaterial = labelMaterial;
            }
            return button;
        }

        private static void ButtonIconLabel(Button button, string iconName, float iconLeft, float iconTop, float iconSize, string label, float labelLeft, float labelSize)
        {
            var rect = (RectTransform)button.transform;
            MockSprite(rect, "Icon", LoadSprite(IconsPath, iconName), iconLeft, iconTop, iconSize, iconSize, false);
            var text = MockText(rect, "Label", label, labelLeft, 0f, rect.sizeDelta.x - labelLeft - 20f, rect.sizeDelta.y, labelSize,
                FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            text.fontSharedMaterial = NavyOutlineMaterial();
        }

        private static Material BrownOutlineMaterial() => GetOutlineMaterial("Outline Brown", OutlineBrown, 0.36f, 0.3f);

        // ------------------------------------------------------------------
        // Primitive in coordinate mockup (origine in alto a sinistra del genitore)
        // ------------------------------------------------------------------

        private static RectTransform MockRect(Transform parent, string name, float left, float top, float width, float height)
        {
            var rect = CreateUIObject(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(left + width * 0.5f, -(top + height * 0.5f));
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static RectTransform Stretch(Transform parent, string name)
        {
            var rect = CreateUIObject(name, parent);
            StretchFill(rect);
            return rect;
        }

        private static TMP_Text MockText(Transform parent, string name, string text, float left, float top, float width, float height,
            float size, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            return AddText(MockRect(parent, name, left, top, width, height), text, size, style, color, alignment);
        }

        /// <summary>
        /// Posiziona uno sprite in modo che la sua parte VISIBILE (senza il margine trasparente) occupi il
        /// riquadro del mockup. sliced: altezza visibile esatta e larghezza libera (9-slice orizzontale);
        /// altrimenti proporzioni preservate e centrate nel riquadro.
        /// </summary>
        private static Image MockSprite(Transform parent, string name, Sprite sprite, float left, float top, float visibleWidth, float visibleHeight, bool sliced)
        {
            var pad = VisiblePadding(sprite);
            var r = sprite.rect;
            float nativeVisibleWidth = r.width - pad.left - pad.right;
            float nativeVisibleHeight = r.height - pad.top - pad.bottom;
            float scale = sliced ? visibleHeight / nativeVisibleHeight : Mathf.Min(visibleWidth / nativeVisibleWidth, visibleHeight / nativeVisibleHeight);
            float width = sliced ? visibleWidth + (pad.left + pad.right) * scale : r.width * scale;
            float height = r.height * scale;
            float centerX = left + visibleWidth * 0.5f + (pad.right - pad.left) * scale * 0.5f;
            float centerY = top + visibleHeight * 0.5f + (pad.bottom - pad.top) * scale * 0.5f;

            var rect = CreateUIObject(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(centerX, -centerY);
            rect.sizeDelta = new Vector2(width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            if (sliced)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f / scale;
            }
            return image;
        }

        private static readonly Dictionary<Texture2D, Texture2D> _readableSheets = new Dictionary<Texture2D, Texture2D>();
        private static readonly Dictionary<Sprite, RectOffset> _visiblePadding = new Dictionary<Sprite, RectOffset>();

        /// <summary>Margine trasparente (alpha &lt;= 0.1) attorno alla parte visibile dello sprite, in pixel.</summary>
        private static RectOffset VisiblePadding(Sprite sprite)
        {
            if (_visiblePadding.TryGetValue(sprite, out var cached)) return cached;
            var source = sprite.texture;
            if (!_readableSheets.TryGetValue(source, out var readable) || readable == null)
            {
                var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(source, rt);
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                readable.Apply();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                _readableSheets[source] = readable;
            }

            var r = sprite.rect;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int y = (int)r.y; y < (int)(r.y + r.height); y++)
                for (int x = (int)r.x; x < (int)(r.x + r.width); x++)
                {
                    if (readable.GetPixel(x, y).a <= 0.1f) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            var padding = maxX < 0
                ? new RectOffset(0, 0, 0, 0)
                : new RectOffset(minX - (int)r.x, (int)(r.x + r.width) - 1 - maxX, (int)(r.y + r.height) - 1 - maxY, minY - (int)r.y);
            _visiblePadding[sprite] = padding;
            return padding;
        }
    }
}
