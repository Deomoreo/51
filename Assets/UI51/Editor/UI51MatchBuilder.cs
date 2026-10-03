using System.Collections.Generic;
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
    /// UI51 Fase 13, dalla Home al tavolo (mockup Matchmaking, SalaPrivata, StanzaErrore) dentro i pannelli di OnlineFlowV2
    /// in MainMenu: nasconde il vecchio "Design" e costruisce la schermata UI51, poi collega RoomFlowV2. La logica resta in
    /// RoomFlowV2 / MatchmakingManager. Idempotente: rieseguirlo riusa i nodi per nome.
    /// </summary>
    public static class UI51MatchBuilder
    {
        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        const string Tag = "[UI51 Fase 13]";

        [MenuItem("Tools/UI51/Build Fase 13 (Dalla Home al tavolo)")]
        private static void Menu() => Build();

        public static void Build()
        {
            if (UI51Build.HasDirtyScene()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var flow = Object.FindObjectOfType<RoomFlowV2>(true);
            if (flow == null || flow.SearchPanel == null) { Debug.LogError($"{Tag} RoomFlowV2 o il suo SearchPanel non trovati in MainMenu."); return; }

            var search = BuildSearch(flow.SearchPanel.transform);
            var friends = Object.FindObjectOfType<UI51FriendsView>(true);
            if (friends == null) Debug.LogWarning($"{Tag} UI51FriendsView non trovata: la sala privata non mostrera' gli amici (Build Fase 8 prima).");
            var hostRoom = BuildRoom(flow.LobbyHostPanel.transform, true, friends);
            var guestRoom = BuildRoom(flow.LobbyGuestPanel.transform, false, friends);
            var roomError = BuildRoomError(flow.transform);
            UI51Build.Wire(flow, so =>
            {
                UI51Build.Ref(so, "SearchView", search);
                AddClose(so, search.Cancel);
                UI51Build.Ref(so, "HostView", hostRoom);
                UI51Build.Ref(so, "GuestView", guestRoom);
                AddClose(so, hostRoom.Exit);
                AddClose(so, guestRoom.Exit);
                UI51Build.Ref(so, "RoomError", roomError);
            });

            var panels = Object.FindObjectOfType<QuickSelectionPanels>(true);
            if (panels == null || panels.TabPages == null || panels.TabPages.Length < 3)
                Debug.LogError($"{Tag} QuickSelectionPanels o le sue pagine non trovati: scheda Stanza privata non rifatta (Build Fase 3 prima).");
            else BuildPrivateTab((RectTransform)panels.TabPages[2].transform, panels);

            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene, ScenePath)) Debug.Log($"{Tag} Scena salvata: {ScenePath}");
            else Debug.LogError($"{Tag} Salvataggio fallito: {ScenePath}");
        }

        /// <summary>Aggiunge b ai CloseButtons di RoomFlowV2 (Cancel) se non c'e' gia'.</summary>
        static void AddClose(SerializedObject so, Button b)
        {
            var p = so.FindProperty("CloseButtons");
            for (int i = 0; i < p.arraySize; i++) if (p.GetArrayElementAtIndex(i).objectReferenceValue == b) return;
            p.arraySize++;
            p.GetArrayElementAtIndex(p.arraySize - 1).objectReferenceValue = b;
        }

        // --- Matchmaking

        /// <summary>
        /// Foto della Home con sfumatura; da top 44 formato (Cinzel 10, spaziatura 3), titolo 24 e tempo; animazione da top 170
        /// (bagliore 330, anello 190 con arco d'oro da ore 12 a ore 3, cinque dorsi 38x57 a passo 46 da top 236); posti da top 400 a
        /// 20 dai lati (righe da due card alte 70, stacco 10, righe a 12, scritta della squadra 9 px); fondo a 30: attesa (12 px) e
        /// "Annulla ricerca" (50) o la pillola d'oro AL TAVOLO (54).
        /// </summary>
        static UI51MatchmakingView BuildSearch(Transform panel)
        {
            var safe = UI51AccessBuilder.BuildScreen(panel, UI51Build.Sprite("Backgrounds", "home_bg_base"), UI51Shape.Linear(
                (UI51Tokens.Rgba(4, 9, 20, 0.55f), 0f), (UI51Tokens.Rgba(4, 9, 20, 0.35f), 0.3f),
                (UI51Tokens.Rgba(4, 9, 20, 0.8f), 0.7f), (UI51Tokens.Rgba(3, 7, 16, 0.97f), 1f)));

            var head = UI51Build.TopBand(UI51Build.Child(safe, "Head"), 0f, 0f, 44f, 77f);
            UI51Build.Column(head, 6f, null, TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;
            var mode = Line(head, "Mode", "PARTITA 1 VS 1 · ONLINE", FontFace.CinzelSemiBold, 10f, UI51Tokens.Gold, 14f, 3f);
            var title = Line(head, "Title", "Cerco giocatori…", FontFace.CinzelBold, 24f, UI51Tokens.Cream, 33f, 0f);
            var sub = Line(head, "Sub", "Tempo di attesa 0:07", FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.65f), 18f, 0f);

            // Animazione: centro a 270 (blocco da 170, alto 200)
            UI51Build.Image(UI51Build.CenterAt(UI51Build.Child(safe, "Glow"), 195f, 270f, 330f, 330f), UI51Build.Sprite("Common", "Bagliore_morbido"),
                UI51Tokens.WhiteA(0.55f));
            var spinner = UI51Build.CenterAt(UI51Build.Child(safe, "Spinner"), 195f, 270f, 190f, 190f);
            UI51Build.Solid(UI51Build.Center(UI51Build.Child(spinner, "Track"), 179f, 179f), Color.clear, 89.5f, 3f, UI51Tokens.GoldA(0.15f));
            UI51Build.Polyline(UI51Build.Stretch(UI51Build.Child(spinner, "Arc")), new Vector2(190f, 190f), 3f, UI51Tokens.Gold,
                UI51Polyline.Arc(new Vector2(95f, 95f), 88f, -90f, 0f));
            var cards = UI51Build.CenterAt(UI51Build.Child(safe, "Cards"), 195f, 264.5f, 222f, 57f);
            var backs = UI51AccessBuilder.WaveBacks(cards, 38f, 57f, 46f);

            // Posti
            var seatsBlock = UI51Build.TopBand(UI51Build.Child(safe, "Seats"), 20f, 20f, 400f, 200f);
            UI51Build.Column(seatsBlock, 12f, null, TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;
            UI51Build.Fit(seatsBlock, false, true);
            var rows = new Object[2];
            var labels = new Object[2];
            var seats = new Object[4];
            for (int r = 0; r < 2; r++)
            {
                var team = UI51Build.Child(seatsBlock, "Team" + r);
                UI51Build.Stack(team, 8f);
                labels[r] = Line(team, "Label", r == 0 ? "LA TUA SQUADRA" : "AVVERSARI", FontFace.CinzelSemiBold, 9f, r == 0 ? UI51Tokens.Gold : UI51Tokens.TeamBlueText, 13f, 2f,
                    TextAlignmentOptions.MidlineLeft);
                var row = UI51Build.Child(team, "Row");
                UI51Build.Layout(row, -1f, 70f).flexibleHeight = 0f;
                UI51Build.Row(row, 10f, null, TextAnchor.MiddleCenter, true, true).childForceExpandWidth = true;
                for (int i = 0; i < 2; i++) seats[r * 2 + i] = SearchSeat(row, "Seat" + i, r == 0 && i == 0);
                rows[r] = team.gameObject;
            }

            // Fondo: attesa + Annulla / AL TAVOLO
            var bottom = UI51Build.Child(safe, "Bottom");
            bottom.anchorMin = Vector2.zero;
            bottom.anchorMax = new Vector2(1f, 0f);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.offsetMin = new Vector2(20f, 30f);
            bottom.offsetMax = new Vector2(-20f, 130f);
            UI51Build.Column(bottom, 12f, null, TextAnchor.LowerCenter, true, true).childForceExpandWidth = true;
            UI51Build.Fit(bottom, false, true);
            var wait = Line(bottom, "Wait", "Tempo stimato circa 20 secondi", FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.6f), 17f, 0f);
            var cancelRt = UI51Build.Child(bottom, "Cancel");
            UI51PrefabBuilder.ButtonBody(cancelRt.gameObject, 350f, 50f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(14f), 1f, UI51Tokens.GoldA(0.5f),
                FontFace.NunitoExtraBold, 14f, 0f, UI51Tokens.Gold, "Annulla ricerca");
            UI51Build.Layout(cancelRt, -1f, 50f);
            var toTable = UI51Build.Child(bottom, "ToTable");
            UI51PrefabBuilder.GoldBody(toTable.gameObject, 350f, 54f, 16f, FontFace.CinzelBold, 15f, 2f, "AL TAVOLO");
            UI51Build.Layout(toTable, -1f, 54f);
            // Pillola, non pulsante: il tavolo si carica da solo per tutti.
            toTable.GetComponent<Button>().enabled = false;
            toTable.GetComponent<UI51Shape>().raycastTarget = false;
            toTable.gameObject.SetActive(false);

            var view = UI51Build.GetOrAdd<UI51MatchmakingView>(panel);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "modeLabel", mode);
                UI51Build.Ref(so, "title", title);
                UI51Build.Ref(so, "subtitle", sub);
                UI51Build.Ref(so, "waitLabel", wait);
                UI51Build.Ref(so, "spinner", spinner);
                UI51Build.SetArray(so, "backs", backs);
                UI51Build.SetArray(so, "rows", rows);
                UI51Build.SetArray(so, "rowLabels", labels);
                UI51Build.SetArray(so, "seats", seats);
                UI51Build.Ref(so, "seatsBlock", seatsBlock.gameObject);
                UI51Build.Ref(so, "cancel", cancelRt.GetComponent<Button>());
                UI51Build.Ref(so, "toTable", toTable);
                UI51Build.SetArray(so, "portraits", UI51Build.Sprite("Avatars", "avatar_1"), UI51Build.Sprite("Avatars", "av_4"),
                    UI51Build.Sprite("Avatars", "av_2"), UI51Build.Sprite("Avatars", "av_5"));
            });
            return view;
        }

        /// <summary>
        /// Posto della ricerca, alto 70 e largo meta' riga. Pieno: pannello col bordo della squadra, avatar 44 (anello 2), nome
        /// 13/800 e "Liv. N" 11. Vuoto: fondo (6,13,27,.55) e tratteggio oro .35, cerchio tratteggiato 44 col "?" che pulsa, "In ricerca…".
        /// </summary>
        static UI51SeatCard SearchSeat(RectTransform row, string name, bool sampleFull)
        {
            var root = UI51Build.Child(row, name);
            UI51Build.Layout(root, 0f, 70f, 1f);

            var full = UI51Build.Stretch(UI51Build.Child(root, "Full"));
            var panel = UI51Build.Shape(full, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.GoldA(0.6f));
            UI51Build.Row(full, 10f, UI51Build.Pad(0, 12, 0, 12), TextAnchor.MiddleLeft, true, true).childForceExpandHeight = false;
            var avatarRt = UI51Build.Child(full, "Avatar");
            UI51Build.Layout(avatarRt, 44f, 44f);
            var avatar = UI51PrefabBuilder.BuildAvatar(avatarRt.gameObject, 44f, 2f, FrameStyle.Oro, UI51Build.Sprite("Avatars", "av_4"), 30f);
            var texts = UI51Build.Child(full, "Texts");
            UI51Build.Layout(texts, -1f, 35f, 1f);
            UI51Build.Column(texts, 2f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = true;
            var nameLabel = Line(texts, "Name", "Marco_93", FontFace.NunitoExtraBold, 13f, UI51Tokens.Cream, 18f, 0f, TextAlignmentOptions.MidlineLeft);
            nameLabel.overflowMode = TextOverflowModes.Ellipsis;
            var detail = Line(texts, "Detail", "Liv. 18", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.55f), 15f, 0f, TextAlignmentOptions.MidlineLeft);

            var empty = UI51Build.Stretch(UI51Build.Child(root, "Empty"));
            UI51Build.Solid(empty, UI51Tokens.Rgba(6, 13, 27, 0.55f), 16f);
            var border = UI51Build.Stretch(UI51Build.Child(empty, "Border"));
            UI51Build.Layout(border, ignore: true); // sopra tutta la card, fuori dalla riga
            Dashed(border, 16f, UI51Tokens.GoldA(0.35f));
            UI51Build.Row(empty, 10f, UI51Build.Pad(0, 12, 0, 12), TextAnchor.MiddleLeft, true, true).childForceExpandHeight = false;
            var q = UI51Build.Child(empty, "Q");
            UI51Build.Layout(q, 44f, 44f);
            var pulse = UI51Build.Solid(UI51Build.Center(UI51Build.Child(q, "Pulse"), 44f, 44f), UI51Tokens.WithAlpha(UI51Tokens.Gold, 0f), 22f);
            Dashed(UI51Build.Stretch(UI51Build.Child(q, "Ring")), 22f, UI51Tokens.GoldA(0.5f));
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(q, "Mark")), "?", FontFace.CinzelBold, 16f, UI51Tokens.GoldA(0.7f),
                TextAlignmentOptions.Center));
            var emptyLabel = Line(empty, "Label", "In ricerca…", FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.5f), 17f, 0f, TextAlignmentOptions.MidlineLeft);
            UI51Build.Layout(emptyLabel, -1f, 17f, 1f);

            full.gameObject.SetActive(sampleFull);
            empty.gameObject.SetActive(!sampleFull);
            var card = UI51Build.GetOrAdd<UI51SeatCard>(root);
            UI51Build.Wire(card, so =>
            {
                UI51Build.Ref(so, "full", full);
                UI51Build.Ref(so, "empty", empty.gameObject);
                UI51Build.Ref(so, "panel", panel);
                UI51Build.Ref(so, "avatar", avatar);
                UI51Build.Ref(so, "nameLabel", nameLabel);
                UI51Build.Ref(so, "detailLabel", detail);
                UI51Build.Ref(so, "emptyLabel", emptyLabel);
                UI51Build.Ref(so, "pulse", pulse);
            });
            return card;
        }

        // --- Sala privata

        /// <summary>
        /// Foto sfocata della Home; testata a 22 (indietro 40, "Stanza privata" 22 e "2 VS 2 · ruolo" 12); pannello del codice da 84
        /// (padding 16, stacco 12: CODICE DELLA STANZA, cinque tessere 40x50 a stacco 6, Copia / Condividi alti 40); "AL TAVOLO n / N"
        /// a 286; posti da 308 (righe da due alte 92 a stacco 10); INVITA AMICI ONLINE da 524 (422 con due posti, lo sposta la vista)
        /// con righe da 56; in fondo a 28 la fascia da 54 (AVVIA PARTITA, IN ATTESA DI.. o l'attesa dell'ospite); "Uscire dalla
        /// stanza?" da 290 a 28 dai lati.
        /// </summary>
        static UI51PrivateRoomView BuildRoom(Transform panel, bool host, UI51FriendsView friends)
        {
            var safe = UI51AccessBuilder.BuildScreen(panel, UI51Build.Sprite("Backgrounds", "home_bg_blur"), null);
            var page = UI51Build.Stretch(UI51Build.Child(safe, "Page"));

            var back = UI51AccessBuilder.RoundButton(page, "Back", false, 40f, 20f, UI51Tokens.Rgba(11, 29, 58, 0.6f),
                UI51Build.Sprite("Common", "ic_nav_back_cream"), 15f);
            ((RectTransform)back.transform).anchoredPosition = new Vector2(20f, -22f);
            var head = UI51Build.TopBand(UI51Build.Child(page, "Head"), 72f, 20f, 20.5f, 43f); // centrata sui 40 del pulsante
            UI51Build.Column(head, 2f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = true;
            Line(head, "Title", "Stanza privata", FontFace.CinzelBold, 22f, UI51Tokens.Cream, 25f, 0f, TextAlignmentOptions.MidlineLeft);
            var subtitle = Line(head, "Sub", "2 VS 2 · hai creato tu la stanza", FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.55f), 16f, 0f,
                TextAlignmentOptions.MidlineLeft);

            // Codice
            var codePanel = UI51Build.TopBand(UI51Build.Child(page, "Code"), 20f, 20f, 84f, 160f);
            UI51Build.Shape(codePanel, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            UI51Build.Column(codePanel, 12f, UI51Build.Pad(16, 16, 16, 16), TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;
            UI51Build.Fit(codePanel, false, true);
            Line(codePanel, "Caption", "CODICE DELLA STANZA", FontFace.CinzelSemiBold, 10f, UI51Tokens.Gold, 14f, 2f);
            var tiles = UI51Build.Child(codePanel, "Tiles");
            UI51Build.Layout(tiles, -1f, 50f);
            UI51Build.Row(tiles, 6f, null, TextAnchor.MiddleCenter, true, true).childForceExpandWidth = false;
            var chars = new Object[5];
            for (int i = 0; i < chars.Length; i++)
            {
                var tile = UI51Build.Child(tiles, "Tile" + i);
                UI51Build.Layout(tile, 40f, 50f);
                UI51Build.Solid(tile, UI51Tokens.WhiteA(0.06f), 10f, 1f, UI51Tokens.GoldA(0.45f));
                chars[i] = UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(tile, "Char")), "A7K2Q"[i].ToString(),
                    FontFace.CinzelBold, 24f, UI51Tokens.GoldLight, TextAlignmentOptions.Center));
            }
            var actions = UI51Build.Child(codePanel, "Actions");
            UI51Build.Layout(actions, -1f, 40f);
            UI51Build.Row(actions, 8f, null, TextAnchor.MiddleCenter, true, true).childForceExpandWidth = true;
            var copy = IconButton(actions, "Copy", "ic_copy_cream", "Copia", out var copyLabel);
            var share = IconButton(actions, "Share", "ic_share_cream", "Condividi", out _);

            // Posti
            var table = UI51Build.TopBand(UI51Build.Child(page, "TableHead"), 20f, 20f, 286f, 16f);
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(table, "Caption")), "AL TAVOLO", FontFace.CinzelSemiBold, 10f,
                UI51Tokens.Gold, TextAlignmentOptions.BaselineLeft, 2f));
            var count = UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(table, "Count")), "1 / 4", FontFace.NunitoExtraBold, 12f,
                UI51Tokens.Cream, TextAlignmentOptions.BaselineRight));
            var grid = UI51Build.TopBand(UI51Build.Child(page, "Seats"), 20f, 20f, 308f, 194f);
            UI51Build.Column(grid, 10f, null, TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;
            var seats = new Object[4];
            GameObject secondRow = null;
            for (int r = 0; r < 2; r++)
            {
                var row = UI51Build.Child(grid, "Row" + r);
                UI51Build.Layout(row, -1f, 92f).flexibleHeight = 0f;
                UI51Build.Row(row, 10f, null, TextAnchor.MiddleCenter, true, true).childForceExpandWidth = true;
                for (int i = 0; i < 2; i++) seats[r * 2 + i] = RoomSeat(row, "Seat" + i, host, r == 0 && i == 0);
                if (r == 1) secondRow = row.gameObject;
            }

            // Amici
            var friendsBlock = UI51Build.TopBand(UI51Build.Child(page, "Friends"), 20f, 20f, 524f, 200f);
            UI51Build.Stack(friendsBlock, 8f);
            UI51Build.Fit(friendsBlock, false, true);
            Line(friendsBlock, "Caption", "INVITA AMICI ONLINE", FontFace.CinzelSemiBold, 10f, UI51Tokens.Gold, 14f, 2f, TextAlignmentOptions.MidlineLeft);
            var list = UI51Build.Child(friendsBlock, "List");
            UI51Build.Shape(list, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            UI51Build.Stack(list, 0f, UI51Build.Pad(2, 0, 2, 0));
            var rows = new Object[5];
            for (int i = 0; i < rows.Length; i++) rows[i] = RoomFriend(list, "Friend" + i);
            var none = UI51Build.Child(friendsBlock, "None");
            UI51Build.Layout(none, -1f, 52f);
            UI51Build.Shape(none, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(none, "Label")), "Nessun amico online: condividi il codice",
                FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.5f), TextAlignmentOptions.Center));
            none.gameObject.SetActive(false);

            // Fondo
            var bottom = UI51Build.Child(page, "Bottom");
            bottom.anchorMin = Vector2.zero;
            bottom.anchorMax = new Vector2(1f, 0f);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.offsetMin = new Vector2(20f, 28f);
            bottom.offsetMax = new Vector2(-20f, 82f);
            var start = UI51Build.Stretch(UI51Build.Child(bottom, "Start"));
            UI51PrefabBuilder.GoldBody(start.gameObject, 350f, 54f, 16f, FontFace.CinzelBold, 15f, 2f, "AVVIA PARTITA");
            UI51Build.Stretch(start);
            var waiting = UI51Build.Stretch(UI51Build.Child(bottom, "Waiting"));
            UI51Build.Solid(waiting, UI51Tokens.GoldA(0.15f), 16f, 1f, UI51Tokens.GoldA(0.3f));
            var waitingLabel = UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(waiting, "Label")), "IN ATTESA DI 3 GIOCATORI",
                FontFace.CinzelBold, 13f, UI51Tokens.CreamA(0.5f), TextAlignmentOptions.Center, 1.5f));
            var guestWait = UI51Build.Stretch(UI51Build.Child(bottom, "GuestWait"));
            UI51Build.Solid(guestWait, Color.clear, 16f, 1f, UI51Tokens.GoldA(0.3f));
            UI51Build.Row(guestWait, 8f, null, TextAnchor.MiddleCenter, true, false);
            var dotBox = UI51Build.Size(UI51Build.Child(guestWait, "Dot"), 8f, 8f);
            UI51Build.Layout(dotBox, 8f, 8f);
            var halo = UI51Build.Solid(UI51Build.Center(UI51Build.Child(dotBox, "Pulse"), 8f, 8f), UI51Tokens.WithAlpha(UI51Tokens.Gold, 0f), 4f);
            UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(dotBox, "Fill")), UI51Tokens.Gold, 4f);
            var guestLabel = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(guestWait, "Label"), "Aspettiamo che Giulia avvii la partita",
                FontFace.NunitoBold, 13f, UI51Tokens.CreamA(0.7f), TextAlignmentOptions.MidlineLeft));
            UI51Build.Size((RectTransform)guestLabel.transform, 260f, 18f);
            start.gameObject.SetActive(false);
            waiting.gameObject.SetActive(host);
            guestWait.gameObject.SetActive(!host);

            // Uscire dalla stanza?
            var leave = UI51Build.Stretch(UI51Build.Child(safe, "Leave"));
            var scrimRt = UI51Build.Stretch(UI51Build.Child(leave, "Scrim"), -400f, -400f, -400f, -400f); // oltre la safe area
            var scrim = UI51Build.Button(scrimRt, UI51Build.Solid(scrimRt, UI51Tokens.Rgba(3, 7, 16, 0.65f), 0f, 0f, default, true));
            var card = UI51Build.TopBand(UI51Build.Child(leave, "Card"), 28f, 28f, 290f, 200f);
            UI51Build.Shape(card, UI51Tokens.DialogFill(), 180f, UI51Tokens.Radii(22f), 1f, UI51Tokens.GoldA(0.5f), true, UI51Tokens.ShadowDialog);
            UI51Build.Stack(card, 0f, UI51Build.Pad(23, 21, 19, 21)); // 22/20/18 + 1 di bordo
            UI51Build.Fit(card, false, true);
            Line(card, "Title", "Uscire dalla stanza?", FontFace.CinzelBold, 19f, UI51Tokens.Cream, 26f, 0f);
            UI51Build.Gap(card, "Gap1", 10.9f); // 10 del mockup + mezza interlinea (CSS la mette, TMP no)
            var leaveText = UI51Build.Text(UI51Build.Child(card, "Text"), "Potrai rientrare con lo stesso codice finché la partita non inizia.",
                FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.65f), TextAlignmentOptions.Center);
            UI51Build.Wrap(leaveText, 13.6f); // line-height 1.5
            UI51Build.Gap(card, "Gap2", 16.9f);
            var stayRt = UI51Build.Child(card, "Stay");
            UI51PrefabBuilder.GoldBody(stayRt.gameObject, 300f, 48f, 14f, FontFace.CinzelBold, 13f, 2f, "RESTA");
            UI51Build.Layout(stayRt, -1f, 48f);
            UI51Build.Gap(card, "Gap3", 10f);
            var exitRt = UI51Build.Child(card, "Exit");
            UI51PrefabBuilder.ButtonBody(exitRt.gameObject, 300f, 44f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(14f), 1f,
                UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.5f), FontFace.NunitoExtraBold, 13f, 0f, UI51Tokens.DangerText, "Esci");
            UI51Build.Layout(exitRt, -1f, 44f);
            leave.gameObject.SetActive(false);

            var view = UI51Build.GetOrAdd<UI51PrivateRoomView>(panel);
            UI51Build.Wire(view, so =>
            {
                so.FindProperty("host").boolValue = host;
                UI51Build.Ref(so, "subtitle", subtitle);
                UI51Build.Ref(so, "back", back);
                UI51Build.SetArray(so, "codeChars", chars);
                UI51Build.Ref(so, "copy", copy);
                UI51Build.Ref(so, "share", share);
                UI51Build.Ref(so, "copyLabel", copyLabel);
                UI51Build.Ref(so, "countLabel", count);
                UI51Build.SetArray(so, "seats", seats);
                UI51Build.Ref(so, "secondRow", secondRow);
                UI51Build.Ref(so, "friendsBlock", friendsBlock);
                UI51Build.SetArray(so, "friendRows", rows);
                UI51Build.Ref(so, "friendsPanel", list.gameObject);
                UI51Build.Ref(so, "noFriends", none.gameObject);
                UI51Build.Ref(so, "friends", friends);
                UI51Build.Ref(so, "start", start.GetComponent<Button>());
                UI51Build.Ref(so, "waiting", waiting.gameObject);
                UI51Build.Ref(so, "guestWait", guestWait.gameObject);
                UI51Build.Ref(so, "waitingLabel", waitingLabel);
                UI51Build.Ref(so, "guestLabel", guestLabel);
                UI51Build.Ref(so, "guestDot", halo);
                UI51Build.Ref(so, "leaveDialog", leave.gameObject);
                UI51Build.Ref(so, "leaveCard", card);
                UI51Build.Ref(so, "leaveText", leaveText);
                UI51Build.Ref(so, "stay", stayRt.GetComponent<Button>());
                UI51Build.Ref(so, "scrim", scrim);
                UI51Build.Ref(so, "exit", exitRt.GetComponent<Button>());
                UI51Build.SetArray(so, "portraits", UI51Build.Sprite("Avatars", "avatar_1"), UI51Build.Sprite("Avatars", "av_4"),
                    UI51Build.Sprite("Avatars", "av_2"), UI51Build.Sprite("Avatars", "av_5"));
            });
            return view;
        }

        /// <summary>Pulsante a contorno oro alto 40, raggio 12: icona 14 + scritta Nunito 800 13 a stacco 6, centrate.</summary>
        static Button IconButton(RectTransform row, string name, string icon, string label, out TextMeshProUGUI text)
        {
            var rt = UI51Build.Child(row, name);
            UI51Build.Layout(rt, 0f, 40f, 1f);
            var shape = UI51Build.Solid(rt, Color.clear, 12f, 1f, UI51Tokens.GoldA(0.5f), true);
            UI51Build.GetOrAdd<UI51Press>(rt);
            UI51Build.Row(rt, 6f, null, TextAnchor.MiddleCenter, true, false).childForceExpandWidth = false;
            var iconRt = UI51Build.Size(UI51Build.Child(rt, "Icon"), 14f, 14f);
            UI51Build.Layout(iconRt, 14f, 14f);
            UI51Build.Image(iconRt, UI51Build.Sprite("Common", icon), Color.white);
            text = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(rt, "Label"), label, FontFace.NunitoExtraBold, 13f, UI51Tokens.Gold,
                TextAlignmentOptions.Center));
            UI51Build.Size((RectTransform)text.transform, 60f, 18f);
            return UI51Build.Button(rt, shape);
        }

        /// <summary>
        /// Posto della sala, alto 92 e largo meta' riga. Pieno: pannello (bordo oro .25), avatar 46 (anello 2) e nome 12/800 centrati a
        /// stacco 5, etichetta HOST / BOT 9/800 in alto a destra. Vuoto: fondo (6,13,27,.55), tratteggio oro .35, "+" 22 e la scritta 11.
        /// Host: tutta la card e' un pulsante (aggiunge o toglie il bot).
        /// </summary>
        static UI51SeatCard RoomSeat(RectTransform row, string name, bool host, bool sampleFull)
        {
            var root = UI51Build.Child(row, name);
            UI51Build.Layout(root, 0f, 92f, 1f);

            var full = UI51Build.Stretch(UI51Build.Child(root, "Full"));
            var panel = UI51Build.Shape(full, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            UI51Build.Column(full, 5f, null, TextAnchor.MiddleCenter, false, false); // misure fisse: il nome lungo va in Ellipsis a 150
            var avatarRt = UI51Build.Child(full, "Avatar");
            UI51Build.Size(avatarRt, 46f, 46f);
            UI51Build.Layout(avatarRt, 46f, 46f);
            var avatar = UI51PrefabBuilder.BuildAvatar(avatarRt.gameObject, 46f, 2f, FrameStyle.Oro, UI51Build.Sprite("Avatars", "avatar_1"), 31f);
            var nameLabel = Line(full, "Name", "Tu", FontFace.NunitoExtraBold, 12f, UI51Tokens.Cream, 17f, 0f);
            nameLabel.overflowMode = TextOverflowModes.Ellipsis;
            UI51Build.Size((RectTransform)nameLabel.transform, 150f, 17f);
            var tagRt = UI51Build.Place(UI51Build.Child(full, "Tag"), new Vector2(1f, 1f), new Vector2(40f, 13f), new Vector2(-10f, -8f));
            UI51Build.Layout(tagRt, ignore: true);
            var tag = UI51Build.NoWrap(UI51Build.Text(tagRt, "HOST", FontFace.NunitoExtraBold, 9f, UI51Tokens.Gold, TextAlignmentOptions.TopRight, 1f));

            var empty = UI51Build.Stretch(UI51Build.Child(root, "Empty"));
            UI51Build.Solid(empty, UI51Tokens.Rgba(6, 13, 27, 0.55f), 16f);
            var border = UI51Build.Stretch(UI51Build.Child(empty, "Border"));
            UI51Build.Layout(border, ignore: true);
            Dashed(border, 16f, UI51Tokens.GoldA(0.35f));
            UI51Build.Column(empty, 6f, null, TextAnchor.MiddleCenter, true, true).childForceExpandHeight = false;
            Line(empty, "Plus", "+", FontFace.NunitoRegular, 22f, UI51Tokens.GoldA(0.6f), 22f, 0f);
            var emptyLabel = Line(empty, "Label", host ? "Tocca per un bot" : "Posto libero", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.5f), 15f, 0f);

            Button button = null;
            if (host)
            {
                var hit = UI51Build.Image(root, null, Color.clear, true, false);
                button = UI51Build.Button(root, hit);
                UI51Build.GetOrAdd<UI51Press>(root);
            }
            full.gameObject.SetActive(sampleFull);
            empty.gameObject.SetActive(!sampleFull);
            var card = UI51Build.GetOrAdd<UI51SeatCard>(root);
            UI51Build.Wire(card, so =>
            {
                UI51Build.Ref(so, "full", full);
                UI51Build.Ref(so, "empty", empty.gameObject);
                UI51Build.Ref(so, "panel", panel);
                UI51Build.Ref(so, "avatar", avatar);
                UI51Build.Ref(so, "nameLabel", nameLabel);
                UI51Build.Ref(so, "emptyLabel", emptyLabel);
                UI51Build.Ref(so, "tagLabel", tag);
                UI51Build.Ref(so, "button", button);
                so.FindProperty("teamBorder").boolValue = false;
            });
            return card;
        }

        /// <summary>Riga amico alta 56 (padding 0 12, stacco 10, riga sotto oro .08): avatar 38 col punto verde, nome 13/800, INVITA (oro 30,
        /// Cinzel 11) / "Invitato…" / "Entrato" con la spunta.</summary>
        static UI51FriendItem RoomFriend(RectTransform list, string name)
        {
            var rt = UI51Build.Child(list, name);
            UI51Build.Layout(rt, -1f, 56f);
            var item = UI51Build.GetOrAdd<UI51FriendItem>(rt);
            var line = UI51Build.Child(rt, "Line");
            line.anchorMin = Vector2.zero;
            line.anchorMax = new Vector2(1f, 0f);
            line.pivot = new Vector2(0.5f, 0f);
            line.offsetMin = Vector2.zero;
            line.offsetMax = new Vector2(0f, 1f);
            UI51Build.Image(line, null, UI51Tokens.GoldA(0.08f), false, false);
            UI51Build.Layout(line, ignore: true);
            UI51Build.Row(rt, 10f, UI51Build.Pad(0, 12, 0, 12), TextAnchor.MiddleLeft, true, false).childForceExpandWidth = false;

            var slot = UI51Build.Size(UI51Build.Child(rt, "AvatarSlot"), 38f, 38f);
            UI51Build.Layout(slot, 38f, 38f);
            item.avatar = UI51PrefabBuilder.BuildAvatar(UI51Build.Child(slot, "Avatar").gameObject, 38f, 2f, FrameStyle.Oro,
                UI51Build.Sprite("Avatars", "av_2"), 26f);
            var dot = UI51Build.Place(UI51Build.Child(slot, "Dot"), new Vector2(1f, 0f), new Vector2(10f, 10f), new Vector2(1f, 0f));
            item.dot = UI51Build.Solid(dot, UI51Tokens.Success, 5f, 2f, UI51Tokens.BadgeRing);

            item.title = UI51Build.NoWrap(UI51Build.Text(UI51Build.Size(UI51Build.Child(rt, "Name"), 100f, 18f), "Giulia",
                FontFace.NunitoExtraBold, 13f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            item.title.overflowMode = TextOverflowModes.Ellipsis;
            UI51Build.Layout(item.title, 0f, 18f, 1f);

            var invite = UI51Build.Child(rt, "Invite");
            UI51PrefabBuilder.GoldBody(invite.gameObject, 78f, 30f, 15f, FontFace.CinzelBold, 11f, 1f, "INVITA");
            UI51Build.Size(invite, 78f, 30f);
            UI51Build.Layout(invite, 78f, 30f);
            item.invite = invite.GetComponent<Button>();
            var invited = UI51Build.Size(UI51Build.Child(rt, "Invited"), 70f, 18f);
            UI51Build.Layout(invited, 70f, 18f);
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(invited, "Label")), "Invitato…", FontFace.NunitoExtraBold, 12f,
                UI51Tokens.Gold, TextAlignmentOptions.MidlineRight));
            item.invited = invited.gameObject;
            var joined = UI51Build.Size(UI51Build.Child(rt, "Joined"), 70f, 18f);
            UI51Build.Layout(joined, 70f, 18f);
            UI51Build.Row(joined, 4f, null, TextAnchor.MiddleRight, false, false).childForceExpandWidth = false;
            item.busyLabel = UI51Build.NoWrap(UI51Build.Text(UI51Build.Size(UI51Build.Child(joined, "Label"), 48f, 18f), "Entrato",
                FontFace.NunitoExtraBold, 12f, UI51Tokens.Gold, TextAlignmentOptions.MidlineRight));
            UI51HomeBuilder.CheckMark(UI51Build.Size(UI51Build.Child(joined, "Check"), 12f, 12f), 12f, 3.2f, UI51Tokens.Gold);
            item.busy = joined.gameObject;
            item.invited.SetActive(false);
            item.busy.SetActive(false);
            return item;
        }

        // --- Scheda Stanza privata (Home, Modalita') e StanzaErrore

        /// <summary>
        /// Mockup Home v3, scheda "Stanza privata": riquadro "Crea una stanza" (cerchio col link 34, titolo 14 e riga 11; tre formati
        /// alti 34 a stacco 6; CREA STANZA 46), "OPPURE" a 14, riquadro "Entra con un codice" (campo 46 in Cinzel 15 spaziatura 4
        /// con Incolla 34 dentro a destra, ENTRA 84 a contorno oro). Rifa' la pagina da zero. Chiamata anche da UI51HomeBuilder.
        /// </summary>
        internal static void BuildPrivateTab(RectTransform page, QuickSelectionPanels panels)
        {
            for (int i = page.childCount - 1; i >= 0; i--) Object.DestroyImmediate(page.GetChild(i).gameObject);
            UI51Build.Column(page, 14f, null, TextAnchor.UpperLeft, true, true).childForceExpandWidth = true;

            var create = Box(page, "Create", 12f);
            var intro = UI51Build.Child(create, "Intro");
            UI51Build.Layout(intro, -1f, 36f);
            UI51Build.Row(intro, 10f, null, TextAnchor.MiddleLeft, true, false);
            var circle = UI51Build.Size(UI51Build.Child(intro, "Circle"), 34f, 34f);
            UI51Build.Layout(circle, 34f, 34f);
            UI51Build.Solid(circle, UI51Tokens.GoldA(0.12f), 17f, 1f, UI51Tokens.GoldA(0.4f));
            UI51Build.Image(UI51Build.Center(UI51Build.Child(circle, "Glyph"), 17f, 17f), UI51Build.Sprite("Common", "ic_link_cream"), Color.white);
            var texts = UI51Build.Size(UI51Build.Child(intro, "Texts"), 0f, 36f);
            UI51Build.Layout(texts, 0f, 36f, 1f);
            UI51Build.Column(texts, 2f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = true;
            Line(texts, "Title", "Crea una stanza", FontFace.NunitoBold, 14f, UI51Tokens.Cream, 19f, 0f, TextAlignmentOptions.MidlineLeft);
            Line(texts, "Desc", "Ricevi un codice da condividere con gli amici", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.55f), 15f, 0f,
                TextAlignmentOptions.MidlineLeft);
            var formatsRow = UI51Build.Child(create, "Formats");
            UI51Build.Layout(formatsRow, -1f, 34f);
            UI51Build.Row(formatsRow, 6f, null, TextAnchor.MiddleCenter, true, true).childForceExpandWidth = true;
            string[] names = { "1 vs 1", "2 vs 2", "1 vs 3" };
            var formats = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                var cell = UI51Build.Child(formatsRow, "Format" + i);
                UI51Build.Layout(cell, 0f, 34f, 1f);
                var hit = UI51HomeBuilder.Hit(cell);
                var on = UI51Build.Stretch(UI51Build.Child(cell, "On"));
                UI51Build.Solid(on, UI51Tokens.GoldA(0.14f), 9f, 1f, UI51Tokens.GoldA(0.75f));
                UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(on, "Label")), names[i], FontFace.CinzelBold, 12f, UI51Tokens.Gold,
                    TextAlignmentOptions.Center));
                var off = UI51Build.Stretch(UI51Build.Child(cell, "Off"));
                UI51Build.Solid(off, Color.clear, 9f, 1f, UI51Tokens.GoldA(0.2f));
                UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(off, "Label")), names[i], FontFace.CinzelBold, 12f,
                    UI51Tokens.CreamA(0.75f), TextAlignmentOptions.Center));
                UI51HomeBuilder.Toggle(cell, on.gameObject, off.gameObject, null, null).SetSelected(i == 0);
                formats[i] = hit;
            }
            var createButton = UI51AccessBuilder.GoldButton(create, "CreateRoom", "CREA STANZA", 46f, 13f);
            // QuickSelectionPanels.ModeOption legge l'icona della tile da un figlio "Icon" della riga scelta.
            UI51Build.Image(UI51Build.Center(UI51Build.Child(createButton.transform, "Icon"), 17f, 17f), UI51Build.Sprite("Common", "ic_link_cream"),
                Color.white).enabled = false;

            var or = UI51Build.Child(page, "Or");
            UI51Build.Row(or, 10f, null, TextAnchor.MiddleCenter, true, false);
            UI51Build.Layout(or, -1f, 14f);
            UI51AccessBuilder.OrLine(or, "LineL");
            var orText = UI51Build.Size(UI51Build.Child(or, "Label"), 0f, 14f);
            UI51Build.NoWrap(UI51Build.Text(orText, "OPPURE", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.5f), TextAlignmentOptions.Center, 1f));
            UI51AccessBuilder.OrLine(or, "LineR");

            var join = Box(page, "Join", 10f);
            Line(join, "Label", "Entra con un codice", FontFace.NunitoBold, 14f, UI51Tokens.Cream, 19f, 0f, TextAlignmentOptions.MidlineLeft);
            var row = UI51Build.Child(join, "Row");
            UI51Build.Layout(row, -1f, 46f);
            UI51Build.Row(row, 8f, null, TextAnchor.MiddleCenter, true, true);
            var input = UI51AccessBuilder.BuildInput(row, "Code", "ES. A7K2Q", null, TMP_InputField.ContentType.Alphanumeric, 46f);
            UI51Build.Layout(input, 0f, 46f, 1f);
            UI51Build.HideChild(input.transform, "Icon");
            var area = UI51Build.Stretch((RectTransform)input.textViewport, 14f, 0f, 44f, 0f);
            UI51Build.Text((RectTransform)input.placeholder.transform, "ES. A7K2Q", FontFace.CinzelBold, 15f, UI51Tokens.CreamA(0.45f),
                TextAlignmentOptions.MidlineLeft, 4f).enableWordWrapping = false;
            UI51Build.Text((RectTransform)input.textComponent.transform, "", FontFace.CinzelBold, 15f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft,
                4f).enableWordWrapping = false;
            input.characterLimit = 5;
            var pasteRt = UI51Build.Place(UI51Build.Child(input.transform, "Paste"), new Vector2(1f, 0.5f), new Vector2(34f, 34f), new Vector2(-6f, 0f));
            var paste = UI51Build.Button(pasteRt, UI51Build.Solid(pasteRt, UI51Tokens.GoldA(0.1f), 10f, 0f, default, true));
            UI51Build.GetOrAdd<UI51Press>(pasteRt);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(pasteRt, "Icon"), 15f, 18f), UI51Build.Sprite("Common", "ic_paste_cream"), Color.white);
            var enter = UI51Build.Child(row, "Enter");
            UI51PrefabBuilder.ButtonBody(enter.gameObject, 84f, 46f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(13f), 1f, UI51Tokens.Gold,
                FontFace.CinzelBold, 13f, 1.5f, UI51Tokens.Gold, "ENTRA");
            UI51Build.Layout(enter, 84f, 46f);
            page.gameObject.SetActive(false); // si accende dalla scheda

            UI51Build.Wire(panels, so =>
            {
                UI51Build.Ref(so, "CreateRoom", createButton);
                UI51Build.Ref(so, "JoinRoom", enter.GetComponent<Button>());
                UI51Build.SetArray(so, "PrivateFormats", formats);
                UI51Build.Ref(so, "PrivateCode", input);
                UI51Build.Ref(so, "PrivatePaste", paste);
            });
        }

        /// <summary>Riquadro della scheda: fondo bianco .03, bordo oro .18, raggio 14, padding 14, colonna a stacco gap.</summary>
        static RectTransform Box(RectTransform page, string name, float gap)
        {
            var box = UI51Build.Child(page, name);
            UI51Build.Solid(box, UI51Tokens.WhiteA(0.03f), 14f, 1f, UI51Tokens.GoldA(0.18f));
            UI51Build.Column(box, gap, UI51Build.Pad(14, 14, 14, 14), TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;
            return box;
        }

        /// <summary>
        /// StanzaErrore sul canvas di OnlineFlowV2 (sopra la Home): velo .65, scheda centrata a 28 dai lati col bordo rosso .5,
        /// padding 22 20 18: cerchio 60 con l'icona 28, titolo 19 a 14, testo 13 a 8, pulsante d'oro 48 a 16, "Gioca online invece" a 10.
        /// </summary>
        static UI51RoomErrorView BuildRoomError(Transform flowRoot)
        {
            var root = UI51Build.Stretch(UI51Build.Child(flowRoot, "UI51RoomError"));
            root.SetAsLastSibling();
            UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(root, "Scrim")), UI51Tokens.Rgba(3, 7, 16, 0.65f), 0f, 0f, default, true);
            var safe = UI51Build.Child(root, "Safe");
            var fit = UI51Build.GetOrAdd<DesignCanvasFit>(safe);
            fit.Reference = UI51Tokens.ReferenceResolution;
            fit.Fill = true;
            safe.anchorMin = safe.anchorMax = safe.pivot = new Vector2(0.5f, 0.5f);
            safe.anchoredPosition = Vector2.zero;
            safe.sizeDelta = UI51Tokens.ReferenceResolution;

            var card = UI51Build.Place(UI51Build.Child(safe, "Card"), new Vector2(0.5f, 0.5f), new Vector2(334f, 300f), Vector2.zero);
            Color danger = UI51Tokens.Danger;
            UI51Build.Shape(card, UI51Tokens.DialogFill(), 180f, UI51Tokens.Radii(22f), 1f, UI51Tokens.WithAlpha(danger, 0.5f), true, UI51Tokens.ShadowDialog);
            UI51Build.Stack(card, 0f, UI51Build.Pad(23, 21, 19, 21)); // 22/20/18 + 1 di bordo
            UI51Build.Fit(card, false, true);
            var iconRow = UI51Build.Child(card, "IconRow");
            UI51Build.Layout(iconRow, -1f, 60f);
            var circle = UI51Build.Place(UI51Build.Child(iconRow, "Circle"), new Vector2(0.5f, 1f), new Vector2(60f, 60f), Vector2.zero);
            UI51Build.Solid(circle, UI51Tokens.WithAlpha(danger, 0.12f), 30f, 1f, UI51Tokens.WithAlpha(danger, 0.5f));
            var icon = UI51Build.Image(UI51Build.Center(UI51Build.Child(circle, "Icon"), 28f, 28f), UI51Build.Sprite("Common", "ic_warn_cream"), Color.white);
            UI51Build.Gap(card, "Gap1", 14f);
            var title = Line(card, "Title", "Codice non valido", FontFace.CinzelBold, 19f, UI51Tokens.Cream, 26f, 0f);
            UI51Build.Gap(card, "Gap2", 8.9f); // 8 del mockup + mezza interlinea (CSS la mette, TMP no)
            var text = UI51Build.Text(UI51Build.Child(card, "Text"), "Nessuna stanza corrisponde a questo codice. Controlla le lettere e riprova.",
                FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.65f), TextAlignmentOptions.Center);
            UI51Build.Wrap(text, 13.6f); // line-height 1.5
            UI51Build.Gap(card, "Gap3", 16.9f);
            var cta = UI51Build.Child(card, "Cta");
            UI51PrefabBuilder.GoldBody(cta.gameObject, 292f, 48f, 14f, FontFace.CinzelBold, 13f, 2f, "RIPROVA");
            UI51Build.Layout(cta, -1f, 48f);
            UI51Build.Gap(card, "Gap4", 10f);
            var alt = UI51AccessBuilder.Link(card, "Alt", "Gioca online invece", FontFace.NunitoExtraBold, 13f, UI51Tokens.Gold, TextAlignmentOptions.Center, 18f);

            var view = UI51Build.GetOrAdd<UI51RoomErrorView>(root);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "card", card);
                UI51Build.Ref(so, "icon", icon);
                UI51Build.SetArray(so, "icons", UI51Build.Sprite("Common", "ic_warn_cream"), UI51Build.Sprite("Common", "ic_lock_cream"),
                    UI51Build.Sprite("Common", "ic_cards_cream"));
                UI51Build.Ref(so, "title", title);
                UI51Build.Ref(so, "text", text);
                UI51Build.Ref(so, "ctaLabel", cta.GetComponentInChildren<TextMeshProUGUI>(true));
                UI51Build.Ref(so, "cta", cta.GetComponent<Button>());
                UI51Build.Ref(so, "alt", alt);
            });
            root.gameObject.SetActive(false);
            return view;
        }

        static void Dashed(RectTransform rt, float radius, Color color)
        {
            var border = UI51Build.GetOrAdd<UI51DashedBorder>(rt);
            border.Set(radius, 1.5f);
            border.color = color;
            border.raycastTarget = false;
        }

        /// <summary>Testo su una riga alto h dentro un layout (rect almeno font x 1.37 per l'Ellipsis).</summary>
        static TextMeshProUGUI Line(RectTransform parent, string name, string text, FontFace face, float size, Color color, float h, float spacing,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var t = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(parent, name), text, face, size, color, align, spacing));
            UI51Build.Layout(t, -1f, h);
            return t;
        }
    }
}
