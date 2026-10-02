using Project51.UIV2.Components;
using Project51.UIV2.Core;
using Project51.UIV2.Data;
using Project51.UIV2.Screens;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// UI51 Fase 4 su MainMenu: pagina Impostazioni, finestra "Elimina account" (mockup Impostazioni),
    /// pagina Profilo con il foglio "Personalizza profilo" (mockup Profilo) e pagina Collezione con le
    /// schede Mazzi, Emoticon e Accuso (mockup Collezione).
    /// Ricollega i componenti esistenti ai nuovi nodi tramite i campi opzionali;
    /// il vecchio aspetto resta in scena spento. Idempotente: rieseguirlo riusa i nodi per nome.
    /// </summary>
    public static class UI51MetaBuilder
    {
        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        const string Tag = "[UI51 Fase 4]";

        [MenuItem("Tools/UI51/Build Fase 4 (Collezione, Profilo, Impostazioni)")]
        private static void Menu() => Build();

        public static void Build()
        {
            if (UI51AccessBuilder.HasDirtyScene()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var settings = UnityEngine.Object.FindObjectOfType<SettingsV2Integration>(true);
            var delete = UnityEngine.Object.FindObjectOfType<DeleteAccountModalV2>(true);
            var legal = UnityEngine.Object.FindObjectOfType<LegalModalV2>(true);
            var panel = settings != null ? new SerializedObject(settings).FindProperty("panel").objectReferenceValue as GameObject : null;
            if (panel == null || delete == null || delete.Modal == null || legal == null)
            {
                Debug.LogError($"{Tag} Manca un pezzo delle Impostazioni (SettingsV2Integration/panel/DeleteAccountModalV2/LegalModalV2).");
                return;
            }
            var home = UnityEngine.Object.FindObjectOfType<HomeV2Integration>(true);
            var topBar = UnityEngine.Object.FindObjectOfType<UIV2TopBar>(true);
            var profile = UnityEngine.Object.FindObjectOfType<ProfileScreenV2>(true);
            var profileScroll = profile != null ? profile.GetComponent<ScrollRect>() : null;
            var modalHost = home != null ? home.transform.Find("ModalHost") : null;
            if (topBar == null || modalHost == null || profileScroll == null || profileScroll.viewport == null)
            {
                Debug.LogError($"{Tag} Manca un pezzo del Profilo (HomeV2Integration/ModalHost/UIV2TopBar/ProfileScreenV2 con ScrollRect).");
                return;
            }
            var cosmetics = UnityEngine.Object.FindObjectOfType<CollectionCosmeticsV2>(true);
            var collection = cosmetics != null ? cosmetics.Screen : null;
            if (collection == null || collection.transform.Find("ContentHost") == null || !HasScroll(collection.DecksPanel)
                || !HasScroll(collection.EmoticonsPanel) || !HasScroll(collection.AccusiPanel))
            {
                Debug.LogError($"{Tag} Manca un pezzo della Collezione (CollectionCosmeticsV2, CollectionScreenV2 con ContentHost e i tre pannelli con ScrollRect).");
                return;
            }

            // Accesi mentre si costruisce: TMP su oggetti spenti lancia eccezioni.
            bool panelWas = panel.activeSelf, deleteWas = delete.gameObject.activeSelf;
            panel.SetActive(true);
            delete.gameObject.SetActive(true);
            BuildSettings(settings, panel.transform, legal);
            BuildDelete(delete);
            panel.SetActive(panelWas);
            delete.gameObject.SetActive(deleteWas);
            BuildProfile(profile, profileScroll, home, topBar, modalHost);
            BuildCollection(collection, cosmetics, home, topBar);

            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene, ScenePath)) Debug.Log($"{Tag} Scena salvata: {ScenePath}");
            else Debug.LogError($"{Tag} Salvataggio fallito: {ScenePath}");
        }

        // --- Impostazioni

        static void BuildSettings(SettingsV2Integration settings, Transform panel, LegalModalV2 legal)
        {
            UI51AccessBuilder.HideChild(panel, "DimBackground");
            UI51AccessBuilder.HideChild(panel, "PanelFrame");
            var safe = UI51AccessBuilder.BuildScreen(panel, UI51Build.Sprite("Backgrounds", "home_bg_blur"), null);
            var page = UI51Build.Stretch(UI51Build.Child(safe, "Page"));

            var back = UI51AccessBuilder.RoundButton(page, "Back", false, 40f, 20f, UI51Tokens.Rgba(11, 29, 58, 0.6f),
                UI51Build.Sprite("Common", "ic_nav_back_cream"), 15f);
            ((RectTransform)back.transform).anchoredPosition = new Vector2(20f, -22f);
            var title = UI51AccessBuilder.TopBand(UI51Build.Child(page, "Title"), 72f, 20f, 22f, 40f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(title, "Impostazioni", FontFace.CinzelBold, 22f, UI51Tokens.Cream,
                TextAlignmentOptions.MidlineLeft));

            // Il corpo scorre fino a 8 sopra al pie' di pagina: con PRIVACY E SOCIALE (2.56) l'account non sta piu' nei 844 del mockup.
            var stale = page.Find("Body"); // corpo fisso delle versioni prima della 2.56
            if (stale != null) Object.DestroyImmediate(stale.gameObject);
            var scroll = UI51SocialBuilder.ScrollArea(UI51Build.Stretch(UI51Build.Child(page, "Scroller"), 20f, 78f, 20f, 84f),
                UI51Build.Pad(0, 0, 12, 0), 18f);
            var body = scroll.content;

            var audio = Panel(Section(body, "Audio", "AUDIO"), "Panel");
            var music = Switch(Row(audio, "Music", "Musica"));
            Divider(audio, "Line1");
            var sfx = Switch(Row(audio, "Sfx", "Effetti sonori"));
            Divider(audio, "Line2");
            var vibration = Switch(Row(audio, "Vibration", "Vibrazione"));

            var graphics = Switch(Row(Panel(Section(body, "Graphics", "GRAFICA"), "Panel"), "Reduced", "Grafica ridotta",
                "Meno effetti e animazioni più brevi"));

            // Una sola lingua e nessuna notifica push: la riga Lingua informa e basta, Notifiche non c'e'.
            var general = Panel(Section(body, "General", "GENERALE"), "Panel");
            Value(Row(general, "Language", "Lingua"), "Italiano");
            Divider(general, "Line1");
            var rules = RowButton(Row(general, "Rules", "Regole e tutorial")); // apre UI51Rules (Fase 12)

            // Solo con un account (gli ospiti non bloccano): apre la pagina Giocatori bloccati.
            var social = Section(body, "Social", "PRIVACY E SOCIALE");
            var blockedRow = RowButton(Row(Panel(social, "Panel"), "Blocked", "Giocatori bloccati", "Emoticon, inviti e messaggi"));
            var blockedView = BuildBlocked(panel);

            var account = Section(body, "Account", "ACCOUNT");
            var member = UI51Build.Child(account, "Member");
            Stack(member, 14f);
            var memberPanel = Panel(member, "Panel");
            var email = Value(Row(memberPanel, "Email", "Email"), "");
            Divider(memberPanel, "Line1");
            var password = RowButton(Row(memberPanel, "Password", "Cambia password")); // Fase 11: manda il link (UI51RecoveryView)
            Divider(memberPanel, "Line2");
            var logout = RowButton(Row(memberPanel, "Logout", "Esci"));
            string[] memberOrder = { "Email", "Line1", "Password", "Line2", "Logout" }; // le righe nuove nascono in fondo
            for (int i = 0; i < memberOrder.Length; i++) memberPanel.Find(memberOrder[i]).SetSiblingIndex(i);
            var deleteRt = UI51Build.Child(member, "Delete");
            UI51PrefabBuilder.ButtonBody(deleteRt.gameObject, 350f, 50f, UI51Shape.Solid(UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.08f)),
                UI51Tokens.Radii(14f), 1f, UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.45f), FontFace.NunitoExtraBold, 14f, 0f,
                UI51Tokens.DangerText, "Elimina account");
            UI51Build.Layout(deleteRt, -1f, 50f);

            var guest = UI51Build.Child(account, "Guest");
            Stack(guest, 14f);
            var invite = UI51Build.Child(guest, "Invite");
            UI51Build.Shape(invite, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            Stack(invite, 12f, UI51Build.Pad(16, 16, 16, 16));
            var words = UI51Build.Child(invite, "Texts");
            Stack(words, 4f);
            var inviteTitle = UI51Build.Child(words, "Title");
            UI51Build.Layout(inviteTitle, -1f, 19f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(inviteTitle, "Stai giocando come ospite", FontFace.NunitoExtraBold, 14f,
                UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            var inviteText = UI51Build.Text(UI51Build.Child(words, "Text"),
                "Crea un account per salvare progressi, ricompense e statistiche. Non c'è nessun account da eliminare.",
                FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.TopLeft);
            Wrap(inviteText, 8.6f); // line-height 1.45
            var createRt = UI51Build.Child(invite, "CreateAccount");
            UI51PrefabBuilder.GoldBody(createRt.gameObject, 318f, 46f, 13f, FontFace.CinzelBold, 13f, 2f, "CREA UN ACCOUNT");
            UI51Build.Layout(createRt, -1f, 46f);
            var guestLogout = RowButton(Row(Panel(guest, "Panel"), "Logout", "Esci dalla sessione ospite", null, UI51Tokens.DangerText));

            // Pie' di pagina: i link sono alti 32 per il tocco, lo stacco negativo riporta i testi a 6 px dalla versione.
            var footer = UI51Build.Child(page, "Footer");
            footer.anchorMin = new Vector2(0f, 0f);
            footer.anchorMax = new Vector2(1f, 0f);
            footer.pivot = new Vector2(0.5f, 0f);
            footer.offsetMin = new Vector2(20f, 0f);
            footer.offsetMax = new Vector2(-20f, 0f);
            footer.anchoredPosition = new Vector2(0f, 26f);
            UI51Build.Column(footer, -2f, null, TextAnchor.UpperCenter, true, true);
            UI51Build.Fit(footer, false, true);
            var links = UI51Build.Child(footer, "Links");
            UI51Build.Row(links, 16f, null, TextAnchor.MiddleCenter, true, true);
            UI51Build.Layout(links, -1f, 32f);
            var privacy = UI51AccessBuilder.Link(links, "Privacy", "Privacy Policy", FontFace.NunitoSemiBold, 12f, UI51Tokens.Gold,
                TextAlignmentOptions.Center, 32f);
            var terms = UI51AccessBuilder.Link(links, "Terms", "Termini di servizio", FontFace.NunitoSemiBold, 12f, UI51Tokens.Gold,
                TextAlignmentOptions.Center, 32f);
            UI51AccessBuilder.Version(footer, 0.35f);

            UI51Build.Wire(settings, so =>
            {
                UI51Build.Ref(so, "closeButton", back);
                UI51Build.Ref(so, "accountButton", createRt.GetComponent<Button>());
                UI51Build.Ref(so, "deleteAccountButton", deleteRt.GetComponent<Button>());
                UI51Build.Ref(so, "panelFrame", page);
                // La pagina si impagina da sola: niente ricalcolo delle righe del vecchio pannello.
                UI51Build.Ref(so, "accountHeader", null);
                UI51Build.Ref(so, "footer", null);
                UI51AccessBuilder.SetArray(so, "rowsAfterAccount");
                UI51Build.Ref(so, "sfxSwitch", sfx);
                UI51Build.Ref(so, "musicSwitch", music);
                UI51Build.Ref(so, "vibrationSwitch", vibration);
                UI51Build.Ref(so, "graphicsSwitch", graphics);
                UI51Build.Ref(so, "accountSection", account.gameObject);
                UI51Build.Ref(so, "accountGroup", member.gameObject);
                UI51Build.Ref(so, "guestGroup", guest.gameObject);
                UI51Build.Ref(so, "emailLabel", email);
                UI51AccessBuilder.SetArray(so, "logoutButtons", logout, guestLogout);
                UI51Build.Ref(so, "rulesButton", rules);
                UI51Build.Ref(so, "socialSection", social.gameObject);
                UI51Build.Ref(so, "blockedButton", blockedRow);
                UI51Build.Ref(so, "blocked", blockedView);
                UI51Build.Ref(so, "scroll", scroll);
                UI51Build.Ref(so, "passwordButton", password);
                UI51Build.Ref(so, "privacyButton", privacy);
                UI51Build.Ref(so, "termsButton", terms);
                UI51Build.Ref(so, "legal", legal);
            });
        }

        /// <summary>
        /// Impostazioni > Privacy e sociale > Giocatori bloccati (scelta dell'utente 02/10, nessun mockup): pagina a tutto schermo sopra
        /// alle Impostazioni come Regole. Indietro e titolo come le Impostazioni, una riga di spiegazione, poi una scheda per giocatore
        /// (riga da 52 col nome e "Sblocca" 88x32 a destra) che scorre fino a 24 dal fondo; vuota: due righe al centro.
        /// </summary>
        static Project51.Unity.UI.UI51BlockedView BuildBlocked(Transform settingsPanel)
        {
            var root = UI51Build.Stretch(UI51Build.Child(settingsPanel.parent, "UI51Blocked"));
            root.gameObject.SetActive(true); // TMP su oggetti spenti lancia eccezioni
            root.SetSiblingIndex(settingsPanel.GetSiblingIndex() + 1);
            var safe = UI51AccessBuilder.BuildScreen(root, UI51Build.Sprite("Backgrounds", "home_bg_blur"), null);
            var page = UI51Build.Stretch(UI51Build.Child(safe, "Page"));

            var back = UI51AccessBuilder.RoundButton(page, "Back", false, 40f, 20f, UI51Tokens.Rgba(11, 29, 58, 0.6f),
                UI51Build.Sprite("Common", "ic_nav_back_cream"), 15f);
            ((RectTransform)back.transform).anchoredPosition = new Vector2(20f, -22f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(page, "Title"), 72f, 20f, 22f, 40f),
                "Giocatori bloccati", FontFace.CinzelBold, 22f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            var hint = UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(page, "Hint"), 20f, 20f, 76f, 36f),
                "Non vedi le loro emoticon e non ricevi inviti o messaggi da loro.", FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.6f),
                TextAlignmentOptions.TopLeft);
            hint.enableWordWrapping = true;

            var scroll = UI51SocialBuilder.ScrollArea(UI51Build.Stretch(UI51Build.Child(page, "Scroller"), 20f, 24f, 20f, 120f),
                UI51Build.Pad(0, 0, 12, 0), 8f);
            var template = Panel(scroll.content, "RowTemplate");
            var row = Row(template, "Row", "Giocatore");
            ((RectTransform)row.Find("Texts")).offsetMax = new Vector2(-116f, 0f); // posto per Sblocca
            var unblock = UI51Build.Child(row, "Unblock");
            UI51PrefabBuilder.ButtonBody(unblock.gameObject, 88f, 32f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(16f), 1f,
                UI51Tokens.GoldA(0.5f), FontFace.NunitoExtraBold, 12f, 0f, UI51Tokens.Gold, "Sblocca");
            UI51Build.Place(unblock, new Vector2(1f, 0.5f), new Vector2(88f, 32f), new Vector2(-14f, 0f)).pivot = new Vector2(1f, 0.5f);

            var empty = UI51AccessBuilder.TopBand(UI51Build.Child(page, "Empty"), 20f, 20f, 200f, 60f);
            UI51Build.Column(empty, 6f, null, TextAnchor.MiddleCenter, true, true).childForceExpandHeight = false;
            UI51Build.Layout(UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(empty, "Title"), "Nessun giocatore bloccato",
                FontFace.NunitoBold, 15f, UI51Tokens.CreamA(0.85f), TextAlignmentOptions.Center)).rectTransform, -1f, 21f);
            var emptyText = UI51Build.Text(UI51Build.Child(empty, "Text"), "Puoi bloccare un giocatore dal suo profilo, durante la partita.",
                FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.55f), TextAlignmentOptions.Center);
            emptyText.enableWordWrapping = true;
            UI51Build.Layout(emptyText.rectTransform, -1f, 34f);

            var view = UI51Build.GetOrAdd<Project51.Unity.UI.UI51BlockedView>(root);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "back", back);
                UI51Build.Ref(so, "rowTemplate", template);
                UI51Build.Ref(so, "empty", empty.gameObject);
                UI51Build.Ref(so, "scroll", scroll);
            });
            template.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            return view;
        }

        // --- Elimina account

        static void BuildDelete(DeleteAccountModalV2 modal)
        {
            var root = modal.transform;
            UI51AccessBuilder.HideChild(root, "Design");
            var dim = root.Find("Dim");
            var dimImage = dim != null ? dim.GetComponent<Image>() : null;
            // Il mockup ha .55 con sfocatura: senza sfocatura serve un velo piu' scuro.
            if (dimImage != null) { dimImage.sprite = null; dimImage.color = UI51Tokens.Rgba(3, 7, 16, 0.72f); }

            var card = UI51Build.Child(SafeRoot(root), "Card");
            card.anchorMin = new Vector2(0f, 1f);
            card.anchorMax = new Vector2(1f, 1f);
            card.pivot = new Vector2(0.5f, 1f);
            card.offsetMin = new Vector2(24f, card.offsetMin.y);
            card.offsetMax = new Vector2(-24f, card.offsetMax.y);
            card.anchoredPosition = new Vector2(0f, -200f);
            UI51Build.Shape(card, UI51Tokens.DialogFill(), 180f, UI51Tokens.Radii(UI51Tokens.RadiusDialog), 1f,
                UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.5f), true, UI51Tokens.ShadowDialog);
            Stack(card, 0f, UI51Build.Pad(22, 20, 20, 20));
            UI51Build.Fit(card, false, true);

            // Cerchio 56 + 14 di stacco dal titolo.
            var iconRow = UI51Build.Child(card, "IconRow");
            UI51Build.Layout(iconRow, -1f, 70f);
            var circle = UI51Build.Place(UI51Build.Child(iconRow, "IconCircle"), new Vector2(0.5f, 1f), new Vector2(56f, 56f), Vector2.zero);
            UI51Build.Solid(circle, UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.12f), 28f, 1f, UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.5f));
            var warn = UI51Build.Sprite("Common", "ic_warn_cream");
            var icon = UI51Build.Image(UI51Build.Center(UI51Build.Child(circle, "Icon"), 28f, 26f), warn, Color.white);

            var title = UI51Build.Text(UI51Build.Child(card, "Title"), DeleteAccountModalV2.AskTitle, FontFace.CinzelBold, 19f,
                UI51Tokens.Cream, TextAlignmentOptions.Center);
            Wrap(title, 0f);
            Gap(card, "GapText", 10f);
            var text = UI51Build.Text(UI51Build.Child(card, "Text"), DeleteAccountModalV2.TypedBody, FontFace.NunitoRegular, 13f,
                UI51Tokens.CreamA(0.65f), TextAlignmentOptions.Center);
            Wrap(text, 13.6f); // line-height 1.5

            var confirm = UI51Build.Child(card, "Confirm");
            Stack(confirm, 8f, UI51Build.Pad(16, 0, 0, 0));
            var label = UI51Build.Child(confirm, "Label");
            UI51Build.Layout(label, -1f, 17f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(label, "Scrivi <color=#F08A8D>" + DeleteAccountModalV2.ConfirmWord + "</color> per confermare",
                FontFace.NunitoBold, 12f, UI51Tokens.CreamA(0.75f), TextAlignmentOptions.MidlineLeft)).richText = true;
            var input = ConfirmInput(confirm);

            Gap(card, "GapButtons", 18f);
            var buttons = UI51Build.Child(card, "Buttons");
            UI51Build.Row(buttons, 10f, null, TextAnchor.MiddleCenter, true, true).childForceExpandWidth = true;
            UI51Build.Layout(buttons, -1f, 48f);
            var cancel = DialogButton(buttons, "Cancel", "Annulla", UI51Shape.Solid(Color.clear), 1f, UI51Tokens.CreamA(0.35f), UI51Tokens.Cream);
            var danger = DialogButton(buttons, "Danger", "Elimina", UI51Shape.Solid(UI51Tokens.Danger), 0f, Color.clear, Color.white);
            // Esito (errore o account eliminato): un solo pulsante a tutta larghezza nella stessa riga.
            var ok = DialogButton(buttons, "Ok", "OK", UI51Tokens.GoldButtonFill(), 1f, UI51Tokens.GoldButtonBorder, UI51Tokens.OnGold);
            ok.gameObject.SetActive(false);

            var group = UI51Build.GetOrAdd<CanvasGroup>(modal.Modal);
            UI51Build.Wire(modal.Modal, so =>
            {
                UI51Build.Ref(so, "Group", group);
                UI51Build.Ref(so, "Frame", card);
                UI51Build.Ref(so, "CloseButton", null);
                UI51Build.Ref(so, "Dimmer", dim != null ? dim.GetComponent<Button>() : null);
            });
            UI51Build.Wire(modal, so =>
            {
                UI51Build.Ref(so, "Title", title);
                UI51Build.Ref(so, "Body", text);
                UI51Build.Ref(so, "Cancel", cancel);
                UI51Build.Ref(so, "Danger", danger);
                UI51Build.Ref(so, "DangerLabel", danger.transform.Find("Label").GetComponent<TMP_Text>());
                UI51Build.Ref(so, "Ok", ok);
                UI51Build.Ref(so, "Icon", icon);
                UI51Build.Ref(so, "WarnIcon", warn);
                UI51Build.Ref(so, "DoneIcon", UI51Build.Sprite("Common", "ic_check_cream"));
                UI51Build.Ref(so, "ConfirmInput", input);
                UI51Build.Ref(so, "ConfirmGroup", confirm.gameObject);
                UI51Build.Ref(so, "DangerFill", danger.GetComponent<UI51Shape>());
            });
        }

        /// <summary>Campo della parola di conferma: quello di Accesso senza icona, bordo rosso, testo maiuscolo spaziato.</summary>
        static TMP_InputField ConfirmInput(RectTransform parent)
        {
            var field = UI51AccessBuilder.BuildInput(parent, "Input", DeleteAccountModalV2.ConfirmWord, null,
                TMP_InputField.ContentType.Standard, 46f);
            var rt = (RectTransform)field.transform;
            UI51AccessBuilder.HideChild(rt, "Icon");
            UI51Build.Stretch(field.textViewport, 14f, 0f, 14f, 0f);
            Color border = UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.4f);
            UI51Build.Solid(rt, UI51Tokens.WhiteA(0.05f), 12f, 1f, border, true);
            UI51AccessBuilder.NoWrap(UI51Build.Text((RectTransform)field.textComponent.transform, "", FontFace.NunitoExtraBold, 14f,
                UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft, 2f)).fontStyle = FontStyles.UpperCase;
            UI51AccessBuilder.NoWrap(UI51Build.Text((RectTransform)field.placeholder.transform, DeleteAccountModalV2.ConfirmWord,
                FontFace.NunitoExtraBold, 14f, UI51Tokens.CreamA(0.35f), TextAlignmentOptions.MidlineLeft, 1f));
            field.characterLimit = 12;
            UI51Build.Wire(rt.GetComponent<UI51Input>(), so =>
            {
                so.FindProperty("m_Border").colorValue = border;
                so.FindProperty("m_BorderFocus").colorValue = UI51Tokens.Danger;
            });
            return field;
        }

        static Button DialogButton(RectTransform row, string name, string label, Gradient fill, float border, Color borderColor, Color text)
        {
            var rt = UI51Build.Child(row, name);
            UI51PrefabBuilder.ButtonBody(rt.gameObject, 146f, 48f, fill, UI51Tokens.Radii(13f), border, borderColor,
                FontFace.NunitoExtraBold, 14f, 0f, text, label);
            UI51Build.Layout(rt, -1f, 48f, 1f);
            return rt.GetComponent<Button>();
        }

        // --- Profilo

        const float S = UI51HomeBuilder.S;
        // Le pagine seguono le voci della barra in basso: Gioca, Collezione, Negozio, Profilo.
        const int PageCount = 4, ProfilePage = 3;
        // Titolo e pulsanti della pagina stanno sulla riga della testata della Home (avatar 58 a 22 dall'alto).
        const float HeaderTop = 22f + 29f - 20f;
        static readonly string[] AvatarNames = { "avatar_1", "av_2", "av_3", "av_4", "av_5", "av_6", "av_7", "av_8" };

        static void BuildProfile(ProfileScreenV2 profile, ScrollRect scroll, HomeV2Integration home, UIV2TopBar topBar, Transform modalHost)
        {
            var avatars = new Sprite[AvatarNames.Length];
            for (int i = 0; i < avatars.Length; i++) avatars[i] = UI51Build.Sprite("Avatars", AvatarNames[i]);

            var pagesBg = PagesBackground(home.transform);
            var header = PageHeader(topBar.transform.parent, "UI51ProfileHeader", "Profilo");
            // Condividi non c'e': la funzione non esiste.
            var settings = UI51AccessBuilder.RoundButton(header, "Settings", true, 40f, 20f, UI51Tokens.Rgba(11, 29, 58, 0.6f),
                UI51Build.Sprite("Common", "ic_settings_cream"), 20f);
            ((RectTransform)settings.transform).anchoredPosition = new Vector2(-20f, -HeaderTop);

            var content = ScrollColumn(scroll);

            // Account: scheda col banner scelto, poi le statistiche che il gioco registra davvero.
            var account = UI51Build.Child(content, "Account");
            Stack(account, 13f);
            var card = UI51Build.Child(account, "Card");
            var banner = UI51Build.Shape(card, UI51Banners.StaticFill(BannerStyle.Notte, out float bannerAngle), bannerAngle,
                UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            Stack(card, 14f, UI51Build.Pad(16, 16, 16, 16));
            var veil = UI51Build.Stretch(UI51Build.Child(card, "Veil"), 1f, 1f, 1f, 1f);
            UI51Build.Layout(veil, -1f, -1f, -1f, -1f, true);
            UI51Build.Shape(veil, UI51Shape.Linear((UI51Tokens.Rgba(6, 13, 27, 0.05f), 0f), (UI51Tokens.Rgba(6, 13, 27, 0.35f), 0.45f),
                (UI51Tokens.Rgba(6, 13, 27, 0.62f), 1f)), 180f, UI51Tokens.Radii(15f), 0f, Color.clear);

            var top = UI51Build.Child(card, "Top");
            UI51Build.Layout(top, -1f, 80f);
            var edit = UI51Build.Place(UI51Build.Child(top, "Avatar"), new Vector2(0f, 0.5f), new Vector2(80f, 80f), Vector2.zero);
            var editButton = UI51HomeBuilder.Hit(edit);
            UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(edit, "Shadow")), UI51Tokens.Navy, 40f, 0f, default, false,
                new UI51Shadow(0f, 4f, 12f, UI51Tokens.BlackA(0.45f)));
            var avatar = UI51PrefabBuilder.BuildAvatar(UI51Build.Child(edit, "Frame").gameObject, 80f, 4f, FrameStyle.Oro, avatars[0], 34f);
            var pencil = UI51Build.Place(UI51Build.Child(edit, "Pencil"), new Vector2(1f, 0f), new Vector2(26f, 26f), new Vector2(2f, -2f));
            UI51Build.Solid(pencil, UI51Tokens.BadgeRing, 13f, 1f, UI51Tokens.Gold);
            // Matita del mockup (path M4 20h4L19 9l-4-4L4 16v4z): non esiste come icona.
            Glyph(pencil, 12f, 2.2f, UI51Tokens.Gold, new Vector2(4f, 20f), new Vector2(8f, 20f), new Vector2(19f, 9f),
                new Vector2(15f, 5f), new Vector2(4f, 16f), new Vector2(4f, 20f));
            var texts = UI51Build.Stretch(UI51Build.Child(top, "Texts"), 94f, 0f, 0f, 0f);
            UI51Build.Column(texts, 5f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = true;
            // Con i puntini il riquadro deve contenere la riga intera (corpo x 1,37), altrimenti il testo sparisce.
            var name = Clip(UI51Build.Text(Line(texts, "Name", 26f), "Giocatore", FontFace.CinzelBold, 18f, UI51Tokens.Cream,
                TextAlignmentOptions.MidlineLeft));
            var id = Clip(UI51Build.Text(Line(texts, "Id", 18f), "", FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.55f),
                TextAlignmentOptions.MidlineLeft));

            UI51Build.Image(Line(card, "Line", 1f), null, UI51Tokens.GoldA(0.14f), false, false);

            var level = UI51Build.Child(card, "Level");
            UI51Build.Layout(level, -1f, 52f);
            var circle = UI51Build.Place(UI51Build.Child(level, "Circle"), new Vector2(0f, 0.5f), new Vector2(44f, 44f), Vector2.zero);
            UI51Build.Shape(circle, UI51Shape.Linear((UI51Tokens.GoldLight, 0f), (UI51Tokens.GoldDark, 1f)), 180f, UI51Tokens.Radii(22f), 0f, Color.clear);
            var levelLabel = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(circle, "Value")), "1",
                FontFace.CinzelBold, 17f, UI51Tokens.OnGold, TextAlignmentOptions.Center));
            var info = UI51Build.Stretch(UI51Build.Child(level, "Info"), 56f, 0f, 0f, 0f);
            UI51Build.Column(info, 6f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = true;
            var head = Line(info, "Head", 18f);
            // Il mockup ha un grado inventato ("Esperto"): qui c'e' il livello vero.
            var title = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(head, "Title")), "Livello 1",
                FontFace.NunitoExtraBold, 13f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            var xp = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(head, "Xp")), "",
                FontFace.NunitoBold, 11f, UI51Tokens.CreamA(0.7f), TextAlignmentOptions.MidlineRight));
            var bar = Line(info, "Bar", 7f);
            UI51Build.Solid(bar, UI51Tokens.WhiteA(0.12f), 3.5f);
            var fill = UI51Build.Child(bar, "Fill");
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0.5f, 1f); fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            UI51Build.Shape(fill, UI51Shape.Linear((UI51Tokens.GoldDark, 0f), (UI51Tokens.GoldLight, 1f)), 90f, UI51Tokens.Radii(3.5f), 0f, Color.clear);
            var xpBar = UI51Build.GetOrAdd<UIV2ProgressBar>(bar);
            UI51Build.Wire(xpBar, so =>
            {
                UI51Build.Ref(so, "fillRect", fill);
                UI51Build.Ref(so, "valueLabel", null);
            });
            var hint = UI51AccessBuilder.NoWrap(UI51Build.Text(Line(info, "Hint", 15f), "", FontFace.NunitoRegular, 11f,
                UI51Tokens.CreamA(0.5f), TextAlignmentOptions.MidlineLeft));

            var grid = Line(Section(account, "Stats", "STATISTICHE"), "Grid", 62f);
            var tiles = UI51Build.Row(grid, 10f, null, TextAnchor.MiddleCenter, true, true);
            tiles.childForceExpandWidth = tiles.childForceExpandHeight = true;
            var matches = StatTile(grid, "Matches", "Partite");
            var wins = StatTile(grid, "Wins", "Vittorie");
            var winRate = StatTile(grid, "WinRate", "% vittorie");

            // Ospite: gli stacchi sono piu' stretti del mockup perche' la pagina parte sotto la testata
            // comune (94,6 invece di 78) e CREA UN ACCOUNT deve restare visibile senza scorrere
            // (iPhone 12: entra tutto; iPhone SE: il pulsante entra intero, "Hai gia' un account?" si raggiunge scorrendo).
            var guest = UI51Build.Child(content, "Guest");
            Stack(guest, 0f);
            var guestCard = UI51Build.Child(guest, "Card");
            UI51Build.Shape(guestCard, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            UI51Build.Row(guestCard, 14f, UI51Build.Pad(16, 16, 16, 16), TextAnchor.MiddleLeft, true, false);
            var person = UI51Build.Size(UI51Build.Child(guestCard, "Circle"), 72f, 72f);
            UI51Build.Layout(person, 72f, 72f);
            // Il mockup ha il bordo tratteggiato: UI51Shape non lo fa, resta continuo.
            UI51Build.Solid(person, UI51Tokens.WhiteA(0.04f), 36f, 1.5f, UI51Tokens.GoldA(0.5f));
            UI51Build.Image(UI51Build.Center(UI51Build.Child(person, "Icon"), 30f, 35f), UI51Build.Sprite("Common", "ic_person_cream"),
                UI51Tokens.WhiteA(0.7f));
            var guestTexts = UI51Build.Size(UI51Build.Child(guestCard, "Texts"), 0f, 76f); // 26 + 22 + 15 e due stacchi da 6
            UI51Build.Layout(guestTexts, 0f, -1f, 1f); // larghezza = quella che resta, non quella dei testi
            UI51Build.Column(guestTexts, 6f, null, TextAnchor.MiddleLeft, true, true);
            var guestName = Clip(UI51Build.Text(Line(guestTexts, "Name", 26f), "Ospite", FontFace.CinzelBold, 18f, UI51Tokens.Cream,
                TextAlignmentOptions.MidlineLeft));
            var chip = Line(guestTexts, "Chip", 22f);
            UI51Build.Row(chip, 0f, UI51Build.Pad(0, 10, 0, 10), TextAnchor.MiddleCenter, true, true);
            UI51Build.Solid(chip, UI51Tokens.WhiteA(0.06f), 11f, 1f, UI51Tokens.CreamA(0.3f));
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(chip, "Label"), "OSPITE", FontFace.NunitoExtraBold, 10f,
                UI51Tokens.CreamA(0.8f), TextAlignmentOptions.Center, 1f));
            UI51AccessBuilder.NoWrap(UI51Build.Text(Line(guestTexts, "Note", 15f), "Sessione temporanea su questo dispositivo",
                FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.45f), TextAlignmentOptions.MidlineLeft));

            Gap(guest, "Gap1", 14f);
            var warning = UI51Build.Child(guest, "Warning");
            UI51Build.Solid(warning, UI51Tokens.GoldA(0.08f), 16f, 1f, UI51Tokens.GoldA(0.4f));
            UI51Build.Row(warning, 12f, UI51Build.Pad(14, 16, 14, 16), TextAnchor.UpperLeft, true, true);
            var warnIcon = UI51Build.Child(warning, "Icon");
            UI51Build.Layout(warnIcon, 24f, 22f);
            UI51Build.Image(warnIcon, UI51Build.Sprite("Common", "ic_warn_cream"), Color.white);
            var warnTexts = UI51Build.Child(warning, "Texts");
            UI51Build.Layout(warnTexts, 0f, -1f, 1f);
            Stack(warnTexts, 4f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(Line(warnTexts, "Title", 19f), "I tuoi progressi non vengono salvati",
                FontFace.NunitoExtraBold, 14f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            Wrap(UI51Build.Text(UI51Build.Child(warnTexts, "Text"),
                "Come ospite puoi giocare online, in allenamento e nelle stanze private, ma non ricevi esperienza, ricompense, statistiche o trofei.",
                FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.65f), TextAlignmentOptions.TopLeft), 8.6f); // line-height 1.45

            Gap(guest, "Gap2", 18f);
            var perks = Panel(Section(guest, "Perks", "CON UN ACCOUNT OTTIENI"), "Panel");
            Stack(perks, 0f, UI51Build.Pad(4, 0, 4, 0));
            Perk(perks, "Levels", "Livelli ed esperienza", "Sali di livello a ogni partita");
            Perk(perks, "Rewards", "Ricompense", "Forzieri, missioni ed eventi");
            Perk(perks, "Stats", "Statistiche e trofei", "Tieni traccia dei tuoi successi");
            // Il mockup dice "Dorsi": qui la scheda della Collezione si chiama "Mazzi".
            Perk(perks, "Collection", "Collezione personale", "Mazzi, emoticon e accusi da sbloccare");

            Gap(guest, "Gap3", 20f);
            var create = UI51AccessBuilder.GoldButton(guest, "CreateAccount", "CREA UN ACCOUNT", 54f, 15f);
            // Un solo effetto al tocco, quello di UIV2Button (come GIOCA): con anche UI51Press il
            // pulsante restava al 97% dopo il primo tocco.
            var createPress = create.GetComponent<UI51Press>();
            if (createPress != null) UnityEngine.Object.DestroyImmediate(createPress);
            var createV2 = UI51Build.GetOrAdd<UIV2Button>(create);
            UI51Build.Wire(createV2, so =>
            {
                UI51Build.Ref(so, "theme", null);
                UI51Build.Ref(so, "background", null);
                UI51Build.Ref(so, "button", create);
                UI51Build.Ref(so, "label", create.transform.Find("Label").GetComponent<TMP_Text>());
                UI51Build.Bool(so, "keepLabelColor", true);
            });
            // Riga alta 32 per il dito: lo stacco di 5 lascia il testo a 12 dal pulsante.
            Gap(guest, "Gap4", 5f);
            var loginRow = Pair(guest, "Login", 32f, 13f, UI51Tokens.CreamA(0.75f), FontFace.NunitoBold, "Hai già un account?", "Accedi");
            var login = UI51HomeBuilder.Hit((RectTransform)loginRow[0].transform.parent);
            guest.gameObject.SetActive(false);

            UI51Build.Wire(profile, so =>
            {
                UI51Build.Ref(so, "defaultAvatarFrame", null);
                UI51Build.Ref(so, "portrait", null);
                UI51Build.Ref(so, "nameLabel", name);
                UI51Build.Ref(so, "infoLabel", null);
                UI51Build.Ref(so, "xpBar", xpBar);
                UI51Build.Ref(so, "xpLabel", xp);
                UI51Build.Ref(so, "settingsButton", settings);
                so.FindProperty("xpSuffixFormat").stringValue = "XP";
                UI51Build.Ref(so, "matchesTile", matches);
                UI51Build.Ref(so, "winsTile", wins);
                UI51Build.Ref(so, "winRateTile", winRate);
                // Scope, settebello e record non sono nel mockup; i trofei non esistono ancora.
                UI51Build.Ref(so, "scopasTile", null);
                UI51Build.Ref(so, "settebelloTile", null);
                UI51Build.Ref(so, "pointRecordTile", null);
                UI51Build.Ref(so, "trophyContainer", null);
                UI51Build.Ref(so, "trophyPrefab", null);
                UI51Build.Ref(so, "registerButton", createV2);
                UI51Build.Ref(so, "shareButton", null);
                UI51Build.Ref(so, "accountGroup", account.gameObject);
                UI51Build.Ref(so, "guestGroup", guest.gameObject);
                UI51Build.Ref(so, "guestNameLabel", guestName);
                UI51Build.Ref(so, "avatar", avatar);
                UI51Build.Ref(so, "banner", banner);
                UI51Build.Ref(so, "levelLabel", levelLabel);
                UI51Build.Ref(so, "titleLabel", title);
                UI51Build.Ref(so, "idLabel", id);
                UI51Build.Ref(so, "xpHintLabel", hint);
                UI51Build.Ref(so, "editButton", editButton);
                UI51Build.Ref(so, "loginButton", login);
            });

            var editor = BuildEditor(modalHost, avatars);
            UI51Build.Wire(home, so =>
            {
                UI51Build.Ref(so, "profileEditor", editor);
                UI51Build.Ref(so, "pagesBackground", pagesBg);
                UI51Build.Ref(so, "topBarGroup", UI51Build.GetOrAdd<CanvasGroup>(topBar));
                SetPageHeader(so, ProfilePage, header.GetComponent<CanvasGroup>());
            });
        }

        /// <summary>Foglio "Personalizza profilo": anteprima, tre schede (avatar, cornice, banner) e SALVA.</summary>
        static ProfileEditorV2 BuildEditor(Transform modalHost, Sprite[] avatars)
        {
            var root = UI51Build.Stretch(UI51Build.Child(modalHost, "ProfileEditorV2"));
            var modal = UI51Build.GetOrAdd<AnimatedModalV2>(root);
            var editor = UI51Build.GetOrAdd<ProfileEditorV2>(root);
            var dim = UI51Build.Stretch(UI51Build.Child(root, "DimBackground"));
            UI51Build.Image(dim, null, UI51Tokens.Scrim, true, false);
            UI51Build.GetOrAdd<DismissOnBackdrop>(dim);
            var sheet = UI51HomeBuilder.SheetFrame(root, "Personalizza profilo", "", out var close);
            UI51AccessBuilder.HideChild(sheet.Find("Header/Titles"), "Subtitle");
            UI51Build.Layout(sheet.Find("Header"), -1f, 36f);
            var body = UI51Build.Child(sheet, "Body");
            Stack(body, 16f);

            var preview = UI51Build.Child(body, "Preview");
            Stack(preview, 8f);
            var face = Line(preview, "Face", 112f);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(face, "Glow"), 190f, 190f), UI51Build.Sprite("Common", "Bagliore_morbido"),
                UI51Tokens.WhiteA(0.55f));
            UI51Build.Solid(UI51Build.Center(UI51Build.Child(face, "Shadow"), 112f, 112f), UI51Tokens.Navy, 56f, 0f, default, false,
                new UI51Shadow(0f, 6f, 16f, UI51Tokens.BlackA(0.5f)));
            var big = UI51PrefabBuilder.BuildAvatar(UI51Build.Child(face, "Avatar").gameObject, 112f, 6f, FrameStyle.Oro, avatars[0], 34f);
            var caption = Pair(preview, "Caption", 16f, 12f, UI51Tokens.CreamA(0.6f), FontFace.NunitoExtraBold, "Cornice", "Oro");

            var tabsRt = UI51Build.Child(body, "Tabs");
            UI51PrefabBuilder.BuildTabs(tabsRt.gameObject);
            UI51Build.Layout(tabsRt, -1f, 44f);
            var tabs = tabsRt.GetComponent<SegmentedTabs>();
            tabs.SetLabels(new[] { "Avatar", "Cornice", "Banner" });

            // Una pagina accesa per volta: il foglio cambia altezza con la scheda, come nel mockup.
            var pages = UI51Build.Child(body, "Pages");
            Stack(pages, 0f);

            var avatarPage = Grid(pages, "Avatars", 4, new Vector2(80f, 66f), new Vector2(10f, 12f));
            var avatarButtons = new Button[avatars.Length];
            var avatarSamples = new AvatarFrame[avatars.Length];
            for (int i = 0; i < avatars.Length; i++)
            {
                var cell = UI51Build.Child(avatarPage, "Avatar" + i);
                avatarButtons[i] = UI51HomeBuilder.Hit(cell);
                var glow = UI51Build.Center(UI51Build.Child(cell, "Glow"), 66f, 66f);
                UI51Build.Solid(glow, UI51Tokens.Gold, 33f, 0f, default, false, new UI51Shadow(0f, 0f, 14f, UI51Tokens.GoldA(0.45f)));
                avatarSamples[i] = UI51PrefabBuilder.BuildAvatar(UI51Build.Child(cell, "Frame").gameObject, 66f, 2f, FrameStyle.Oro, avatars[i], 30f);
                var check = UI51Build.Place(UI51Build.Child(cell, "Check"), new Vector2(0.5f, 0.5f), new Vector2(20f, 20f), new Vector2(26f, 26f));
                UI51Build.Solid(check, UI51Tokens.Gold, 10f, 2f, UI51Tokens.BadgeRing);
                UI51HomeBuilder.CheckMark(check, 10f, 3.6f);
                UI51HomeBuilder.Toggle(cell, null, null, check.gameObject, glow.gameObject).SetSelected(i == 0);
            }

            int frameCount = ProfileCosmetics.FrameIds.Length;
            var framePage = Grid(pages, "Frames", 4, new Vector2(80f, 87f), new Vector2(10f, 12f));
            var frameButtons = new Button[frameCount];
            var frameSamples = new AvatarFrame[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                var cell = UI51Build.Child(framePage, "Frame" + i);
                frameButtons[i] = UI51HomeBuilder.Hit(cell);
                var outline = Outline(cell, 76f, 76f, 38f);
                frameSamples[i] = UI51PrefabBuilder.BuildAvatar(UI51Build.Child(cell, "Sample").gameObject, 66f, 4f, FrameStyle.Oro, avatars[0], 30f);
                UI51Build.Place((RectTransform)frameSamples[i].transform, new Vector2(0.5f, 1f), new Vector2(66f, 66f), Vector2.zero);
                ProfileCosmetics.ApplyFrame(frameSamples[i], i, 2f, 4f);
                var names = OnOff(cell, ProfileCosmetics.FrameNames[i], TextAlignmentOptions.Center);
                UI51HomeBuilder.Toggle(cell, names[0], names[1], null, outline).SetSelected(i == 1);
            }

            int bannerCount = ProfileCosmetics.BannerIds.Length;
            var bannerPage = Grid(pages, "Banners", 2, new Vector2(170f, 69f), new Vector2(10f, 10f));
            var bannerButtons = new Button[bannerCount];
            var bannerSamples = new AvatarFrame[bannerCount];
            var bannerShapes = new UI51Shape[bannerCount];
            var bannerNames = new TMP_Text[bannerCount];
            var bannerLocks = new GameObject[bannerCount];
            for (int i = 0; i < bannerCount; i++)
            {
                var style = ProfileCosmetics.Banner(i);
                var cell = UI51Build.Child(bannerPage, "Banner" + i);
                bannerButtons[i] = UI51HomeBuilder.Hit(cell);
                var outline = Outline(cell, 180f, 58f, 29f);
                var pill = UI51Build.Place(UI51Build.Child(cell, "Pill"), new Vector2(0.5f, 1f), new Vector2(170f, 48f), Vector2.zero);
                // Gli animati (materiale creato a runtime) prendono l'aspetto vero in ProfileEditorV2.Awake.
                bannerShapes[i] = UI51Build.Shape(pill, UI51Banners.StaticFill(style, out float angle), angle, UI51Tokens.Radii(24f), 1f,
                    UI51Banners.BorderColor(style));
                bannerSamples[i] = UI51PrefabBuilder.BuildAvatar(UI51Build.Child(pill, "Avatar").gameObject, 36f, 2f, FrameStyle.Oro, avatars[0], 30f);
                UI51Build.Place((RectTransform)bannerSamples[i].transform, new Vector2(0f, 0.5f), new Vector2(36f, 36f), new Vector2(5f, 0f));
                bannerNames[i] = Clip(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(pill, "Name"), 49f, 0f, 10f, 0f), "Giocatore",
                    FontFace.NunitoExtraBold, 11f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
                var locked = UI51Build.Stretch(UI51Build.Child(pill, "Lock"));
                UI51Build.Solid(locked, UI51Tokens.Rgba(6, 13, 27, 0.55f), 24f);
                UI51Build.Image(UI51Build.Place(UI51Build.Child(locked, "Icon"), new Vector2(1f, 0.5f), new Vector2(14f, 17f), new Vector2(-14f, 0f)),
                    UI51Build.Sprite("Common", "ic_lock_cream"), Color.white);
                bannerLocks[i] = locked.gameObject;
                var names = OnOff(cell, ProfileCosmetics.BannerNames[i], TextAlignmentOptions.MidlineLeft);
                UI51AccessBuilder.NoWrap(UI51Build.Text(Foot(cell, "Tag"), ProfileCosmetics.BannerTags[i], FontFace.NunitoExtraBold, 9f,
                    UI51Banners.IsAnimated(style) ? UI51Tokens.GoldLight : UI51Tokens.CreamA(0.45f), TextAlignmentOptions.MidlineRight, 0.5f));
                UI51HomeBuilder.Toggle(cell, names[0], names[1], null, outline).SetSelected(i == 0);
            }
            framePage.gameObject.SetActive(false);
            bannerPage.gameObject.SetActive(false);

            // La riga "prossimo sblocco" del mockup non c'e': non esiste un percorso di sblocchi da mostrare.

            // Esito del salvataggio: spento finche' non c'e' niente da dire, cosi' il foglio resta quello del mockup.
            var status = UI51AccessBuilder.Status(body, "Status");
            status.gameObject.SetActive(false);
            var save = UI51AccessBuilder.GoldButton(body, "Save", "SALVA", 52f, 14f);
            // Spento durante il salvataggio: opacita' .5 (gancio gia' presente in UI51Press).
            UI51Build.Wire(save.GetComponent<UI51Press>(), so => UI51Build.Ref(so, "m_DisabledGroup", UI51Build.GetOrAdd<CanvasGroup>(save)));

            UI51HomeBuilder.WireModal(modal, sheet, close);
            UI51Build.Wire(editor, so =>
            {
                UI51Build.Ref(so, "modal", modal);
                UI51Build.Ref(so, "tabs", tabs);
                UI51AccessBuilder.SetArray(so, "pages", avatarPage.gameObject, framePage.gameObject, bannerPage.gameObject);
                UI51Build.Ref(so, "preview", big);
                UI51Build.Ref(so, "previewLabel", caption[0]);
                UI51Build.Ref(so, "previewName", caption[1]);
                UI51AccessBuilder.SetArray(so, "avatars", avatars);
                UI51AccessBuilder.SetArray(so, "avatarButtons", avatarButtons);
                UI51AccessBuilder.SetArray(so, "frameButtons", frameButtons);
                UI51AccessBuilder.SetArray(so, "bannerButtons", bannerButtons);
                UI51AccessBuilder.SetArray(so, "avatarSamples", avatarSamples);
                UI51AccessBuilder.SetArray(so, "frameSamples", frameSamples);
                UI51AccessBuilder.SetArray(so, "bannerSamples", bannerSamples);
                UI51AccessBuilder.SetArray(so, "bannerShapes", bannerShapes);
                UI51AccessBuilder.SetArray(so, "bannerPlayerNames", bannerNames);
                UI51AccessBuilder.SetArray(so, "bannerLocks", bannerLocks);
                UI51Build.Ref(so, "save", save);
                UI51Build.Ref(so, "status", status);
            });
            root.gameObject.SetActive(false);
            return editor;
        }

        // --- Collezione

        const int CollectionPage = 1;
        const float DeckW = 108f, DeckH = DeckW * 520f / 353f;
        const float DeckCellH = 198f; // carta 159 + stacco 7 + nome 17 + "IN USO" 14
        static readonly string[] CollectionTabs = { "Mazzi", "Emoticon", "Accuso" };

        static void BuildCollection(CollectionScreenV2 screen, CollectionCosmeticsV2 cosmetics, HomeV2Integration home, UIV2TopBar topBar)
        {
            // Monete e gemme del mockup non ci sono: l'economia non esiste.
            var header = PageHeader(topBar.transform.parent, "UI51CollectionHeader", "Collezione", "Personalizza il tuo modo di giocare");

            UI51AccessBuilder.HideChild(screen.transform, "TabsRow");
            var tabsRt = UI51Build.Child(UI51HomeBuilder.Container(screen.transform), "Tabs");
            UI51PrefabBuilder.BuildTabs(tabsRt.gameObject);
            UI51AccessBuilder.TopBand(tabsRt, 20f, 20f, 0f, 46f);
            UI51Build.Solid(tabsRt, UI51Tokens.Rgba(6, 13, 27, 0.7f), UI51Tokens.RadiusTabs, 1f, UI51Tokens.GoldA(0.18f));
            for (int i = 1; i <= CollectionTabs.Length; i++) UI51Build.Layout(tabsRt.Find("Tab" + i), -1f, 38f, 1f);
            var tabs = tabsRt.GetComponent<SegmentedTabs>();
            tabs.SetLabels(CollectionTabs);
            // Schede 46 + stacco 18 (mockup: da 92 a 156).
            UI51Build.Stretch((RectTransform)screen.transform.Find("ContentHost"), 0f, 0f, 0f, 64f * S);

            // Accesi mentre si costruisce: TMP su oggetti spenti lancia eccezioni.
            var panels = new[] { screen.DecksPanel.gameObject, screen.EmoticonsPanel.gameObject, screen.AccusiPanel.gameObject };
            var was = new bool[panels.Length];
            for (int i = 0; i < panels.Length; i++) { was[i] = panels[i].activeSelf; panels[i].SetActive(true); }
            BuildDecks(screen.DecksPanel);
            var hint = BuildEmoticons(screen.EmoticonsPanel, cosmetics.Emoticons != null && cosmetics.Emoticons.Length > 0 ? cosmetics.Emoticons[0] : null);
            var fist = BuildAccuso(screen.AccusiPanel);
            for (int i = 0; i < panels.Length; i++) panels[i].SetActive(was[i]);

            UI51Build.Wire(screen, so =>
            {
                // Le vecchie schede restano collegate e spente: accese di nuovo, funzionano come prima.
                UI51Build.Ref(so, "segmentedTabs", tabs);
                var names = so.FindProperty("tabNames");
                names.arraySize = CollectionTabs.Length;
                for (int i = 0; i < CollectionTabs.Length; i++) names.GetArrayElementAtIndex(i).stringValue = CollectionTabs[i];
            });
            UI51AccessBuilder.HideChild(screen.EmoticonsPanel.transform, "Feedback");
            UI51Build.Wire(cosmetics, so =>
            {
                UI51Build.Ref(so, "Feedback", hint);
                UI51Build.Ref(so, "PugnoArtwork", UI51Build.Sprite("Common", "pugno"));
                // ANTEPRIMA batte il pugno sul posto; svuotando il campo torna l'animazione del tavolo (Fase 5).
                UI51Build.Ref(so, "PreviewFist", fist);
            });
            UI51Build.Wire(home, so => SetPageHeader(so, CollectionPage, header.GetComponent<CanvasGroup>()));
        }

        static void BuildDecks(CollectionDecksPanel panel)
        {
            var content = ScrollColumn(panel.GetComponent<ScrollRect>());
            Head(content, "Head", "I TUOI MAZZI", "Tocca per usarlo");
            Gap(content, "Gap", 12f);
            var grid = Grid(content, "Grid", 3, new Vector2(DeckW, DeckCellH), new Vector2(13f, 14f));
            var card = UI51Build.EditPrefab(UI51PrefabBuilder.PrefabPath("Collection_DeckCard"), BuildDeckCard);
            // "Prossimi sblocchi" del mockup non c'e': non esiste un percorso di sblocchi da mostrare.
            UI51Build.Wire(panel, so =>
            {
                UI51AccessBuilder.SetArray(so, "heroArtCards");
                UI51Build.Ref(so, "heroNameLabel", null);
                UI51Build.Ref(so, "heroSubtitleLabel", null);
                UI51Build.Ref(so, "collectionCountLabel", null);
                UI51Build.Ref(so, "collectionProgress", null);
                UI51Build.Ref(so, "footerLabel", null);
                UI51Build.Ref(so, "gridContainer", grid);
                UI51Build.Ref(so, "deckCardPrefab", card.GetComponent<DeckCardView>());
            });
        }

        /// <summary>Carta della griglia Mazzi: dorso e nome; su quello scelto contorno oro e "IN USO".</summary>
        static void BuildDeckCard(GameObject root)
        {
            var rt = UI51Build.Size((RectTransform)root.transform, DeckW, DeckCellH);
            var card = UI51AccessBuilder.TopBand(UI51Build.Child(rt, "Card"), 0f, 0f, 0f, DeckH);
            // Outline 2 px a 2 px dalla carta, con l'alone.
            var glow = UI51Build.Stretch(UI51Build.Child(card, "Glow"), -4f, -4f, -4f, -4f);
            UI51Build.Solid(glow, Color.clear, 13f, 2f, UI51Tokens.Gold, false, new UI51Shadow(0f, 0f, 16f, UI51Tokens.GoldA(0.35f)));
            UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(card, "Shadow")), UI51Tokens.Navy, 9f, 0f, default, false,
                new UI51Shadow(0f, 4f, 10f, UI51Tokens.BlackA(0.45f)));
            var face = UI51Build.Stretch(UI51Build.Child(card, "Face"));
            UI51Build.Solid(face, Color.white, 9f);
            UI51Build.GetOrAdd<Mask>(face).showMaskGraphic = false;
            var art = UI51Build.Image(UI51Build.Stretch(UI51Build.Child(face, "Art")), null, Color.white, false, false);
            var name = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(rt, "Name"), 0f, 0f, DeckH + 7f, 17f),
                "Mazzo", FontFace.NunitoExtraBold, 12f, UI51Tokens.Cream, TextAlignmentOptions.Center));
            var status = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(rt, "Status"), 0f, 0f, DeckH + 24f, 14f),
                "IN USO", FontFace.NunitoExtraBold, 10f, UI51Tokens.Gold, TextAlignmentOptions.Center));
            // DeckCardView spegne il pulsante sul mazzo in uso: deve essere un figlio, non la carta.
            var hit = UI51Build.Image(UI51Build.Stretch(UI51Build.Child(rt, "Hit")), null, Color.clear, true, false);
            var button = UI51Build.Button(hit, hit);

            UI51Build.Wire(UI51Build.GetOrAdd<DeckCardView>(root), so =>
            {
                UI51Build.Ref(so, "border", null);
                UI51Build.Ref(so, "fillRect", null);
                UI51Build.Ref(so, "fill", null);
                UI51Build.Ref(so, "art", art);
                UI51Build.Ref(so, "lockIcon", null);
                UI51Build.Ref(so, "equippedBadge", glow.gameObject);
                UI51Build.Ref(so, "nameLabel", name);
                UI51Build.Ref(so, "statusLabel", status);
                UI51Build.Ref(so, "actionButton", button);
                UI51Build.Ref(so, "actionBackground", null);
                UI51Build.Ref(so, "actionLabel", null);
                // I dorsi hanno proporzioni un po' diverse: l'arte riempie la carta, come nel foglio "Scegli il mazzo".
                UI51Build.Bool(so, "preserveArtAspect", false);
                so.FindProperty("nameColor").colorValue = UI51Tokens.Cream;
                so.FindProperty("equippedStatusText").stringValue = "IN USO";
            });
        }

        /// <summary>Scheda Emoticon; restituisce la riga dei messaggi sotto gli slot.</summary>
        static TMP_Text BuildEmoticons(CollectionEmoticonsPanel panel, Sprite sample)
        {
            var content = ScrollColumn(panel.GetComponent<ScrollRect>());
            var inGame = UI51Build.Child(content, "InGame");
            UI51Build.Shape(inGame, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            Stack(inGame, 12f, UI51Build.Pad(14, 16, 16, 16));
            var count = Head(inGame, "Head", "IN PARTITA", "3 / 3");
            var row = Line(inGame, "Slots", 64f);
            var slots = new EmoticonSlotView[3];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = BuildSlot(UI51Build.Place(UI51Build.Child(row, "Slot" + i), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f),
                    new Vector2((i - 1) * 82f, 0f)), sample);
            var hint = UI51AccessBuilder.NoWrap(UI51Build.Text(Line(inGame, "Hint", 15f), "Puoi portare in partita fino a 3 emoticon",
                FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.55f), TextAlignmentOptions.Center));

            Gap(content, "Gap1", 20f);
            var owned = Head(content, "Head", "LE TUE EMOTICON", "6 / 6");
            Gap(content, "Gap2", 10f);
            var grid = Grid(content, "Grid", 3, new Vector2(110f, 92f), new Vector2(10f, 10f));
            var card = UI51Build.EditPrefab(UI51PrefabBuilder.PrefabPath("Collection_EmoticonCard"), root => BuildEmoticonCard(root, sample));
            // La riga "Nuova emoticon" del mockup non c'e': non esiste un percorso di sblocchi da mostrare.
            UI51Build.Wire(panel, so =>
            {
                UI51Build.Ref(so, "equippedHeaderLabel", null);
                UI51AccessBuilder.SetArray(so, "equippedSlots", slots);
                UI51Build.Ref(so, "equippedCountLabel", count);
                UI51Build.Ref(so, "collectionCountLabel", owned);
                UI51Build.Ref(so, "collectionProgress", null);
                UI51Build.Ref(so, "gridContainer", grid);
                UI51Build.Ref(so, "cardPrefab", card.GetComponent<UIV2CollectionCard>());
            });
            return hint;
        }

        /// <summary>Slot da 64: pieno (emoticon e X; tutto il tondo la toglie) o vuoto ("+").</summary>
        static EmoticonSlotView BuildSlot(RectTransform slot, Sprite sample)
        {
            var filled = UI51Build.Stretch(UI51Build.Child(slot, "Filled"));
            var remove = UI51Build.Button(filled, UI51Build.Solid(filled, UI51Tokens.GoldA(0.1f), 32f, 1.5f, UI51Tokens.GoldA(0.7f), true));
            UI51Build.GetOrAdd<UI51Press>(filled);
            var icon = UI51Build.Image(UI51Build.Center(UI51Build.Child(filled, "Icon"), 46f, 46f), sample, Color.white);
            var badge = UI51Build.Place(UI51Build.Child(filled, "Remove"), Vector2.one, new Vector2(20f, 20f), new Vector2(4f, 4f));
            UI51Build.Solid(badge, UI51Tokens.BadgeRing, 10f, 1f, UI51Tokens.GoldA(0.6f));
            UI51Build.Image(UI51Build.Center(UI51Build.Child(badge, "Icon"), 8f, 8f), UI51Build.Sprite("Common", "ic_close_cream"), Color.white);

            var empty = UI51Build.Stretch(UI51Build.Child(slot, "Empty"));
            // Il mockup ha il bordo tratteggiato: UI51Shape non lo fa, resta continuo.
            var add = UI51Build.Button(empty, UI51Build.Solid(empty, Color.clear, 32f, 1.5f, UI51Tokens.GoldA(0.4f), true));
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(empty, "Plus")), "+", FontFace.NunitoRegular, 22f,
                UI51Tokens.GoldA(0.6f), TextAlignmentOptions.Center));
            empty.gameObject.SetActive(false);

            var view = UI51Build.GetOrAdd<EmoticonSlotView>(slot);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "filledRoot", filled.gameObject);
                UI51Build.Ref(so, "emptyRoot", empty.gameObject);
                UI51Build.Ref(so, "icon", icon);
                UI51Build.Ref(so, "nameLabel", null);
                UI51Build.Ref(so, "removeButton", remove);
                UI51Build.Ref(so, "emptyButton", add);
            });
            return view;
        }

        /// <summary>Cella della griglia Emoticon; su quelle in partita il riquadro oro col posto (1, 2, 3).</summary>
        static void BuildEmoticonCard(GameObject root, Sprite sample)
        {
            var rt = UI51Build.Size((RectTransform)root.transform, 110f, 92f);
            var button = UI51Build.Button(rt, UI51Build.Solid(rt, UI51Tokens.Rgba(6, 13, 27, 0.6f), 14f, 1f, UI51Tokens.GoldA(0.18f), true));
            var on = UI51Build.Stretch(UI51Build.Child(rt, "On"));
            UI51Build.Solid(on, UI51Tokens.GoldA(0.12f), 14f, 1.5f, UI51Tokens.Gold);
            var order = UI51Build.Place(UI51Build.Child(on, "Order"), Vector2.one, new Vector2(18f, 18f), new Vector2(-6f, -6f));
            UI51Build.Solid(order, UI51Tokens.Gold, 9f);
            var number = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(order, "Value")), "1", FontFace.CinzelBold, 10f,
                UI51Tokens.OnGold, TextAlignmentOptions.Center));
            // Icona 46 + stacco 6 + nome 15, centrati nei 92.
            var icon = UI51Build.Image(UI51Build.Place(UI51Build.Child(rt, "Icon"), new Vector2(0.5f, 1f), new Vector2(46f, 46f),
                new Vector2(0f, -12.5f)), sample, Color.white);
            var name = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(rt, "Name"), 4f, 4f, 64.5f, 15f),
                "Emoticon", FontFace.NunitoExtraBold, 11f, UI51Tokens.CreamA(0.75f), TextAlignmentOptions.Center));

            UI51Build.Wire(UI51Build.GetOrAdd<UIV2CollectionCard>(root), so =>
            {
                UI51Build.Ref(so, "border", null);
                UI51Build.Ref(so, "icon", icon);
                UI51Build.Ref(so, "lockIcon", null);
                UI51Build.Ref(so, "nameLabel", name);
                UI51Build.Ref(so, "equippedBadge", on.gameObject);
                UI51Build.Ref(so, "button", button);
                UI51Build.Ref(so, "fillRect", null);
                UI51Build.Ref(so, "fill", null);
                UI51Build.Bool(so, "overrideNameColors", true);
                so.FindProperty("nameColor").colorValue = UI51Tokens.CreamA(0.75f);
                so.FindProperty("equippedNameColor").colorValue = UI51Tokens.Gold;
                so.FindProperty("lockedNameColor").colorValue = UI51Tokens.CreamA(0.4f);
                UI51Build.Ref(so, "orderLabel", number);
            });
        }

        /// <summary>Scheda Accuso; restituisce il pugno, che ANTEPRIMA fa battere.</summary>
        static RectTransform BuildAccuso(CollectionAccusiPanel panel)
        {
            var content = ScrollColumn(panel.GetComponent<ScrollRect>());
            var card = UI51Build.Child(content, "Card");
            UI51Build.Shape(card, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            Stack(card, 0f, UI51Build.Pad(18, 18, 20, 18));

            // L'accuso e' uno solo: "In uso" e' fisso.
            var head = (RectTransform)Head(card, "Head", "IL TUO ACCUSO", "", 24f).transform.parent;
            var chip = UI51Build.Place(UI51Build.Child(head, "Chip"), new Vector2(1f, 0.5f), new Vector2(70f, 24f), Vector2.zero);
            UI51Build.Solid(chip, UI51Tokens.GoldA(0.14f), 12f, 1f, UI51Tokens.GoldA(0.6f));
            var check = UI51Build.Place(UI51Build.Child(chip, "Check"), new Vector2(0f, 0.5f), new Vector2(10f, 10f), new Vector2(10f, 0f));
            Glyph(check, 10f, 3.4f, UI51Tokens.Gold, new Vector2(5f, 12.5f), new Vector2(9.5f, 17f), new Vector2(19f, 7.5f));
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(chip, "Label"), 25f, 0f, 0f, 0f), "In uso",
                FontFace.NunitoExtraBold, 11f, UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft));

            Gap(card, "Gap1", 6f);
            // Senza layout group: il pugno lo muove l'animazione.
            // 186 e non 190 del mockup: su iPhone SE la pagina deve stare nei 473 disponibili senza scorrere.
            var art = Line(card, "Art", 186f);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(art, "Glow"), 230f, 230f), UI51Build.Sprite("Common", "Bagliore_morbido"),
                UI51Tokens.WhiteA(0.75f));
            var fist = UI51Build.Image(UI51Build.Center(UI51Build.Child(art, "Fist"), 118f, 123f), UI51Build.Sprite("Common", "pugno"), Color.white);
            var title = UI51AccessBuilder.NoWrap(UI51Build.Text(Line(card, "Title", 26f), "Pugno sul tavolo", FontFace.CinzelBold, 19f,
                UI51Tokens.Cream, TextAlignmentOptions.Center));
            Gap(card, "Gap2", 6f);
            var box = UI51Build.Child(card, "Desc");
            Stack(box, 0f, UI51Build.Pad(0, 22, 0, 22)); // max-width 270
            var desc = UI51Build.Text(UI51Build.Child(box, "Text"),
                "Quando accusi, batti il pugno e fai tremare il tavolo: tutti i giocatori lo vedranno.",
                FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.Top);
            Wrap(desc, 8.6f); // line-height 1.45
            Gap(card, "Gap3", 16f);
            var preview = UI51Build.Child(Line(card, "Action", 44f), "Preview");
            UI51PrefabBuilder.ButtonBody(preview.gameObject, 154f, 44f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(13f), 1f, UI51Tokens.Gold,
                FontFace.CinzelBold, 13f, 1.5f, UI51Tokens.Gold, "ANTEPRIMA");

            Gap(content, "Gap", 12f);
            var soon = Line(content, "Soon", 63f);
            // Il mockup ha il bordo tratteggiato: UI51Shape non lo fa, resta continuo.
            UI51Build.Solid(soon, UI51Tokens.Rgba(6, 13, 27, 0.55f), 14f, 1f, UI51Tokens.GoldA(0.3f));
            var circle = UI51Build.Place(UI51Build.Child(soon, "Circle"), new Vector2(0f, 0.5f), new Vector2(34f, 34f), new Vector2(16f, 0f));
            UI51Build.Solid(circle, UI51Tokens.GoldA(0.1f), 17f, 1f, UI51Tokens.GoldA(0.35f));
            UI51Build.Image(UI51Build.Center(UI51Build.Child(circle, "Icon"), 19f, 19f), UI51Build.Sprite("Common", "ic_chest_cream"), Color.white);
            var texts = UI51Build.Stretch(UI51Build.Child(soon, "Texts"), 62f, 0f, 16f, 0f);
            UI51Build.Column(texts, 2f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = true;
            UI51AccessBuilder.NoWrap(UI51Build.Text(Line(texts, "Title", 18f), "Nuovi accusi in arrivo", FontFace.NunitoExtraBold, 13f,
                UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            UI51AccessBuilder.NoWrap(UI51Build.Text(Line(texts, "Subtitle", 15f), "Arriveranno con eventi e stagioni", FontFace.NunitoRegular, 11f,
                UI51Tokens.CreamA(0.55f), TextAlignmentOptions.MidlineLeft));

            UI51Build.Wire(panel, so =>
            {
                UI51Build.Ref(so, "heroArtwork", fist);
                UI51Build.Ref(so, "heroArtworkPlaceholder", null);
                UI51Build.Ref(so, "heroTitleLabel", title);
                UI51Build.Ref(so, "heroSubtitleLabel", null);
                UI51Build.Ref(so, "heroDescriptionLabel", desc);
                UI51Build.Ref(so, "previewButton", preview.GetComponent<Button>());
                UI51Build.Bool(so, "uppercaseHeroTitle", false);
                UI51Build.Ref(so, "collectionCountLabel", null);
                UI51Build.Ref(so, "collectionProgress", null);
                UI51Build.Ref(so, "listContainer", null);
            });
            return fist.rectTransform;
        }

        /// <summary>Sfondo sfocato delle pagine diverse dalla Home: sopra l'ambiente animato, sotto i contenuti.</summary>
        static CanvasGroup PagesBackground(Transform root)
        {
            var bg = UI51Build.Stretch(UI51Build.Child(root, "UI51PagesBg"));
            var sprite = UI51Build.Sprite("Backgrounds", "home_bg_blur");
            UI51Build.Image(bg, sprite, Color.white, false, false);
            var fitter = UI51Build.GetOrAdd<AspectRatioFitter>(bg);
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite != null ? sprite.rect.width / sprite.rect.height : 1153f / 2048f;
            bg.SetSiblingIndex(3); // dopo BackgroundLayer, HomeMotes e UI51Overlay
            return Hidden(bg);
        }

        /// <summary>
        /// Testata di una pagina: sta in TopBarHost (le pagine sono ritagliate sotto) al posto di quella della Home,
        /// e HomeV2Integration la accende quando la pagina e' quella in vista.
        /// </summary>
        static RectTransform PageHeader(Transform topBarHost, string name, string title, string subtitle = null)
        {
            var header = UI51HomeBuilder.Container(topBarHost, name);
            // Col sottotitolo il blocco (26 + 3 + 17) resta centrato sulla stessa riga.
            float top = subtitle == null ? HeaderTop : HeaderTop - 3f;
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(header, "Title"), 20f, 72f, top,
                subtitle == null ? 40f : 26f), title, FontFace.CinzelBold, 22f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            if (subtitle != null)
                UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(header, "Subtitle"), 20f, 20f, top + 29f, 17f),
                    subtitle, FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.MidlineLeft));
            Hidden(header);
            return header;
        }

        static bool HasScroll(Component panel)
        {
            var scroll = panel != null ? panel.GetComponent<ScrollRect>() : null;
            return scroll != null && scroll.viewport != null;
        }

        /// <summary>
        /// Contenuto di una pagina che scorre: una colonna larga 390 e scalata (la pagina e' in unita' 1080),
        /// al posto del vecchio Content che resta spento.
        /// </summary>
        static RectTransform ScrollColumn(ScrollRect scroll)
        {
            UI51AccessBuilder.HideChild(scroll.viewport, "Content");
            var content = UI51Build.Child(scroll.viewport, "UI51");
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(390f, 0f);
            content.anchoredPosition = Vector2.zero;
            content.localScale = Vector3.one * S;
            Stack(content, 0f, UI51Build.Pad(0, 20, 16, 20));
            UI51Build.Fit(content, false, true);
            scroll.content = content;
            return content;
        }

        /// <summary>Testa di una sezione: etichetta oro a sinistra, nota a destra (restituita).</summary>
        static TextMeshProUGUI Head(RectTransform parent, string name, string caption, string note, float height = 15f)
        {
            var row = Line(parent, name, height);
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(row, "Caption")), caption, FontFace.CinzelSemiBold, 10f,
                UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft, 2f));
            return UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(row, "Note")), note, FontFace.NunitoRegular, 11f,
                UI51Tokens.CreamA(0.5f), TextAlignmentOptions.MidlineRight));
        }

        static CanvasGroup Hidden(RectTransform rt)
        {
            var group = UI51Build.GetOrAdd<CanvasGroup>(rt);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            return group;
        }

        static void SetPageHeader(SerializedObject so, int page, CanvasGroup header)
        {
            var headers = so.FindProperty("pageHeaders");
            // Allungando un array Unity copia l'ultimo elemento: i posti nuovi vanno svuotati.
            for (int i = headers.arraySize; i < PageCount; i++)
            {
                headers.arraySize = i + 1;
                headers.GetArrayElementAtIndex(i).objectReferenceValue = null;
            }
            headers.GetArrayElementAtIndex(page).objectReferenceValue = header;
        }

        static UIV2StatTile StatTile(RectTransform row, string name, string caption)
        {
            var rt = UI51Build.Child(row, name);
            UI51Build.Layout(rt, 0f, -1f, 1f);
            UI51Build.Shape(rt, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(14f), 1f, UI51Tokens.BorderGoldSoft);
            UI51Build.Column(rt, 3f, null, TextAnchor.MiddleCenter, true, true).childForceExpandWidth = true;
            var value = UI51AccessBuilder.NoWrap(UI51Build.Text(Line(rt, "Value", 23f), "0", FontFace.CinzelBold, 18f, UI51Tokens.Cream,
                TextAlignmentOptions.Center));
            var label = UI51AccessBuilder.NoWrap(UI51Build.Text(Line(rt, "Caption", 14f), caption, FontFace.NunitoBold, 10f,
                UI51Tokens.CreamA(0.55f), TextAlignmentOptions.Center));
            var tile = UI51Build.GetOrAdd<UIV2StatTile>(rt);
            UI51Build.Wire(tile, so =>
            {
                UI51Build.Ref(so, "icon", null);
                UI51Build.Ref(so, "valueLabel", value);
                UI51Build.Ref(so, "captionLabel", label);
            });
            return tile;
        }

        /// <summary>Riga "cosa ottieni con un account": spunta oro nel tondo, titolo e descrizione.</summary>
        static void Perk(RectTransform panel, string name, string title, string subtitle)
        {
            // Mockup 55 (stacco 10 sopra e sotto): 47 (stacco 6) fa stare CREA UN ACCOUNT nello schermo di iPhone SE.
            var row = Line(panel, name, 47f);
            var circle = UI51Build.Place(UI51Build.Child(row, "Check"), new Vector2(0f, 0.5f), new Vector2(24f, 24f), new Vector2(14f, 0f));
            UI51Build.Solid(circle, UI51Tokens.GoldA(0.15f), 12f, 1f, UI51Tokens.GoldA(0.5f));
            Glyph(circle, 11f, 3.4f, UI51Tokens.Gold, new Vector2(5f, 12.5f), new Vector2(9.5f, 17f), new Vector2(19f, 7.5f));
            var texts = UI51Build.Stretch(UI51Build.Child(row, "Texts"), 50f, 0f, 14f, 0f);
            UI51Build.Column(texts, 2f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = true;
            UI51AccessBuilder.NoWrap(UI51Build.Text(Line(texts, "Title", 18f), title, FontFace.NunitoExtraBold, 13f, UI51Tokens.Cream,
                TextAlignmentOptions.MidlineLeft));
            UI51AccessBuilder.NoWrap(UI51Build.Text(Line(texts, "Subtitle", 15f), subtitle, FontFace.NunitoRegular, 11f,
                UI51Tokens.CreamA(0.55f), TextAlignmentOptions.MidlineLeft));
        }

        /// <summary>Riga centrata "testo + parola in oro" (Text, Accent).</summary>
        static TextMeshProUGUI[] Pair(RectTransform parent, string name, float height, float size, Color color, FontFace accent,
            string first, string second)
        {
            var row = Line(parent, name, height);
            UI51Build.Row(row, 4f, null, TextAnchor.MiddleCenter, true, false);
            return new[]
            {
                UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Size(UI51Build.Child(row, "Text"), 0f, height), first,
                    FontFace.NunitoRegular, size, color, TextAlignmentOptions.MidlineLeft)),
                UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Size(UI51Build.Child(row, "Accent"), 0f, height), second,
                    accent, size, UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft)),
            };
        }

        /// <summary>Segno a tratti arrotondati: i punti, in coordinate SVG (viewBox 24), uniti in ordine.</summary>
        static void Glyph(RectTransform parent, float size, float strokeSvg, Color color, params Vector2[] points)
        {
            var box = UI51Build.Center(UI51Build.Child(parent, "Glyph"), size, size);
            float k = size / 24f, w = strokeSvg * k;
            for (int i = 0; i + 1 < points.Length; i++)
            {
                Vector2 a = points[i], d = points[i + 1] - a;
                var rt = UI51Build.Place(UI51Build.Child(box, "Stroke" + i), new Vector2(0.5f, 0.5f), new Vector2(d.magnitude * k + w, w),
                    new Vector2(a.x + d.x * 0.5f - 12f, 12f - a.y - d.y * 0.5f) * k);
                UI51Build.Solid(rt, color, w * 0.5f);
                rt.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
        }

        static RectTransform Grid(RectTransform parent, string name, int columns, Vector2 cell, Vector2 spacing)
        {
            var rt = UI51Build.Child(parent, name);
            var g = UI51Build.GetOrAdd<GridLayoutGroup>(rt);
            g.cellSize = cell;
            g.spacing = spacing;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columns;
            g.childAlignment = TextAnchor.UpperCenter;
            return rt;
        }

        /// <summary>Contorno oro della scelta (outline 2 px a 3 px dal bordo): sporge di 5 dalla cella.</summary>
        static GameObject Outline(RectTransform cell, float w, float h, float radius)
        {
            var rt = UI51Build.Place(UI51Build.Child(cell, "Outline"), new Vector2(0.5f, 1f), new Vector2(w, h), new Vector2(0f, 5f));
            UI51Build.Solid(rt, Color.clear, radius, 2f, UI51Tokens.Gold);
            return rt.gameObject;
        }

        /// <summary>Fascia alta 15 in fondo alla cella, con 4 di margine ai lati.</summary>
        static RectTransform Foot(RectTransform cell, string name)
        {
            var rt = UI51Build.Child(cell, name);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(4f, 0f);
            rt.offsetMax = new Vector2(-4f, 15f);
            return rt;
        }

        /// <summary>Nome in fondo alla cella: oro se scelto (NameOn), crema altrimenti (NameOff).</summary>
        static GameObject[] OnOff(RectTransform cell, string text, TextAlignmentOptions align)
        {
            var on = UI51AccessBuilder.NoWrap(UI51Build.Text(Foot(cell, "NameOn"), text, FontFace.NunitoExtraBold, 11f, UI51Tokens.Gold, align));
            var off = UI51AccessBuilder.NoWrap(UI51Build.Text(Foot(cell, "NameOff"), text, FontFace.NunitoExtraBold, 11f,
                UI51Tokens.CreamA(0.75f), align));
            return new[] { on.gameObject, off.gameObject };
        }

        // --- Mattoni

        /// <summary>Figlio alto height in una colonna.</summary>
        static RectTransform Line(Transform parent, string name, float height)
        {
            var rt = UI51Build.Child(parent, name);
            UI51Build.Layout(rt, -1f, height);
            return rt;
        }

        /// <summary>Una riga sola, con i puntini se non entra.</summary>
        static TextMeshProUGUI Clip(TextMeshProUGUI t)
        {
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        /// <summary>UI51 (stretch) -> Safe (DesignCanvasFit 390x844) senza sfondo: per le finestre sopra a una pagina.</summary>
        static RectTransform SafeRoot(Transform host)
        {
            var root = UI51Build.Stretch(UI51Build.Child(host, "UI51"));
            var safe = UI51Build.Child(root, "Safe");
            var fit = UI51Build.GetOrAdd<DesignCanvasFit>(safe);
            fit.Reference = UI51Tokens.ReferenceResolution;
            fit.Fill = true;
            safe.anchorMin = safe.anchorMax = safe.pivot = new Vector2(0.5f, 0.5f);
            safe.anchoredPosition = Vector2.zero;
            safe.sizeDelta = UI51Tokens.ReferenceResolution;
            return safe;
        }

        /// <summary>Colonna a tutta larghezza, altezza dai figli.</summary>
        internal static void Stack(RectTransform rt, float spacing, RectOffset padding = null) =>
            UI51Build.Column(rt, spacing, padding, TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;

        internal static void Gap(Transform parent, string name, float height) =>
            UI51Build.Layout(UI51Build.Child(parent, name), -1f, height);

        internal static void Wrap(TextMeshProUGUI t, float lineSpacing)
        {
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Overflow;
            t.lineSpacing = lineSpacing;
        }

        /// <summary>Etichetta maiuscola oro + quello che segue, a gap px.</summary>
        internal static RectTransform Section(RectTransform body, string name, string caption, float gap = 6f)
        {
            var section = UI51Build.Child(body, name);
            Stack(section, gap);
            var cap = UI51Build.Child(section, "Caption");
            UI51Build.Layout(cap, -1f, 14f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(cap, caption, FontFace.CinzelSemiBold, 10f, UI51Tokens.Gold,
                TextAlignmentOptions.MidlineLeft, 2f));
            return section;
        }

        internal static RectTransform Panel(RectTransform parent, string name)
        {
            var panel = UI51Build.Child(parent, name);
            UI51Build.Shape(panel, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            Stack(panel, 0f, UI51Build.Pad(1, 0, 1, 0));
            return panel;
        }

        /// <summary>Riga alta 52: titolo (e sottotitolo) a sinistra, a destra resta lo spazio per interruttore, valore o freccia.</summary>
        internal static RectTransform Row(RectTransform panel, string name, string title, string subtitle = null, Color? color = null)
        {
            var row = UI51Build.Child(panel, name);
            UI51Build.Layout(row, -1f, 52f);
            var texts = UI51Build.Stretch(UI51Build.Child(row, "Texts"), 16f, 0f, 74f, 0f);
            UI51Build.Column(texts, 2f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = true;
            var t = UI51Build.Child(texts, "Title");
            UI51Build.Layout(t, -1f, 19f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(t, title, FontFace.NunitoBold, 14f, color ?? UI51Tokens.Cream,
                TextAlignmentOptions.MidlineLeft));
            if (subtitle == null) { UI51AccessBuilder.HideChild(texts, "Subtitle"); return row; }
            var s = UI51Build.Child(texts, "Subtitle");
            UI51Build.Layout(s, -1f, 15f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(s, subtitle, FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.5f),
                TextAlignmentOptions.MidlineLeft));
            return row;
        }

        internal static void Divider(RectTransform panel, string name)
        {
            var d = UI51Build.Child(panel, name);
            UI51Build.Layout(d, -1f, 1f);
            UI51Build.Image(UI51Build.Stretch(UI51Build.Child(d, "Line"), 16f, 0f, 16f, 0f), null, UI51Tokens.GoldA(0.12f), false, false);
        }

        internal static UI51Toggle Switch(RectTransform row)
        {
            var rt = UI51Build.Child(row, "Switch");
            UI51PrefabBuilder.BuildToggle(rt.gameObject);
            UI51Build.Place(rt, new Vector2(1f, 0.5f), new Vector2(46f, 26f), new Vector2(-16f, 0f));
            // 46x26 e' piccolo per il dito: area di tocco 70x52, invisibile (il clic risale all'interruttore).
            UI51Build.Image(UI51Build.Stretch(UI51Build.Child(rt, "Hit"), -12f, -13f, -12f, -13f), null, Color.clear, true, false)
                .canvasRenderer.cullTransparentMesh = false; // trasparente ma deve prendere i tocchi
            return rt.GetComponent<UI51Toggle>();
        }

        static TextMeshProUGUI Value(RectTransform row, string text)
        {
            var v = UI51Build.Child(row, "Value");
            v.anchorMin = new Vector2(0.4f, 0f);
            v.anchorMax = new Vector2(1f, 1f);
            v.pivot = new Vector2(1f, 0.5f);
            v.offsetMin = Vector2.zero;
            v.offsetMax = new Vector2(-16f, 0f);
            return UI51AccessBuilder.NoWrap(UI51Build.Text(v, text, FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.5f),
                TextAlignmentOptions.MidlineRight));
        }

        /// <summary>Tutta la riga cliccabile, con la freccia del mockup (ic_nav_back specchiata, 10 px, .55).</summary>
        static Button RowButton(RectTransform row)
        {
            var hit = UI51Build.Image(row, null, Color.clear, true, false);
            var chevron = UI51Build.Place(UI51Build.Child(row, "Chevron"), new Vector2(1f, 0.5f), new Vector2(10f, 10f), Vector2.zero);
            chevron.pivot = new Vector2(0.5f, 0.5f);
            chevron.anchoredPosition = new Vector2(-21f, 0f);
            chevron.localScale = new Vector3(-1f, 1f, 1f);
            UI51Build.Image(chevron, UI51Build.Sprite("Common", "ic_nav_back_cream"), new Color(1f, 1f, 1f, 0.55f));
            UI51Build.GetOrAdd<UI51Press>(row);
            return UI51Build.Button(row, hit);
        }
    }
}
