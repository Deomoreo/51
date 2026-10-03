using Project51.Auth;
using Project51.UIV2.Core;
using Project51.UIV2.Screens;
using Project51.Unity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// UI51 Fase 11 su MainMenu: Password dimenticata (mockup PasswordDimenticata e PasswordInviata), aperta dal link
    /// "Password dimenticata?" (AuthScreensV2.Recovery) e da "Cambia password" delle Impostazioni; moderazione (2.54): Gioco online
    /// sospeso (mockup Sospensione) ed Esito segnalazione (mockup SegnalazioneEsito) nel canvas UI51Moderation.
    /// </summary>
    public static class UI51AccountBuilder
    {
        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        const string Tag = "[UI51 Fase 11]";
        const int RecoveryOrder = 2100;   // sopra all'Accesso (2000) e alle Impostazioni (1500 prima dell'ingresso)
        const int ModerationOrder = 1200; // sopra a Home, Modalita', stanze (OnlineFlowV2 800) e pagine social

        [MenuItem("Tools/UI51/Build Fase 11 (Account)")]
        private static void Menu() => Build();

        public static void Build()
        {
            if (UI51Build.HasDirtyScene()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var auth = Object.FindObjectOfType<AuthUIController>(true);
            var screens = Object.FindObjectOfType<AuthScreensV2>(true);
            var loginPanel = auth != null ? UI51Build.PanelRef(new SerializedObject(auth), "loginPanel", scene, "LoginPanel") : null;
            var bg = UI51Build.Sprite("Backgrounds", "home_bg_base");
            var back = UI51Build.Sprite("Common", "ic_nav_back_cream");
            var lockIcon = UI51Build.Sprite("Common", "ic_lock_cream");
            var mailIcon = UI51Build.Sprite("Common", "ic_mail");
            var home = Object.FindObjectOfType<HomeScreenV2>(true);
            var canvases = home != null ? home.GetComponentsInParent<Canvas>(true) : new Canvas[0];
            var homeCanvas = canvases.Length > 0 ? canvases[canvases.Length - 1] : null;
            var start = Object.FindObjectOfType<StartScreenV2>(true);
            var settings = Object.FindObjectOfType<SettingsV2Integration>(true);
            var modes = Object.FindObjectOfType<QuickSelectionPanels>(true);
            var alert = UI51Build.Sprite("Common", "shield_alert");
            var check = UI51Build.Sprite("Common", "shield_check");
            var warn = UI51Build.Sprite("Common", "ic_warn_cream");
            var glow = UI51Build.Sprite("Common", "Bagliore_morbido");
            if (screens == null || loginPanel == null || bg == null || back == null || lockIcon == null || mailIcon == null || homeCanvas == null ||
                start == null || settings == null || modes == null || alert == null || check == null || warn == null || glow == null)
            {
                Debug.LogError($"{Tag} Manca AuthScreensV2, il pannello dell'Accesso, la Home, StartScreenV2, le Impostazioni, Modalita' o " +
                               "un'immagine (home_bg_base, ic_nav_back_cream, ic_lock_cream, ic_mail, shield_alert, shield_check, ic_warn_cream, " +
                               "Bagliore_morbido). Non tocco nulla.");
                return;
            }

            var view = BuildRecovery(loginPanel.transform, bg, back, lockIcon, mailIcon);
            UI51Build.Wire(screens, so => UI51Build.Ref(so, "Recovery", view));
            UI51Build.Wire(settings, so => UI51Build.Ref(so, "recovery", view));

            var moderation = UI51SocialBuilder.Root(home, homeCanvas, "UI51Moderation", out var group, ModerationOrder);
            group.alpha = 1f; // le due schermate si accendono da sole
            group.blocksRaycasts = group.interactable = true;
            var legal = new SerializedObject(settings).FindProperty("legal").objectReferenceValue as LegalModalV2;
            var suspension = BuildSuspension((RectTransform)moderation.transform, bg, back, alert, modes, legal);
            var outcome = BuildOutcome((RectTransform)moderation.transform, check, glow, warn);
            UI51Build.Wire(start, so =>
            {
                UI51Build.Ref(so, "Suspension", suspension);
                UI51Build.Ref(so, "ReportOutcome", outcome);
            });

            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene, ScenePath)) Debug.Log($"{Tag} Scena salvata: {ScenePath}");
            else Debug.LogError($"{Tag} Salvataggio fallito: {ScenePath}");
        }

        /// <summary>
        /// Pagina sopra all'Accesso: indietro a 20, da 96 cerchio 84 col lucchetto (poi la busta), titolo 22, testo 13 largo 300;
        /// da 340 (22 dai lati, passo 14) email e INVIA IL LINK, oppure il riquadro con la spunta verde, TORNA AL LOGIN e il reinvio.
        /// </summary>
        static UI51RecoveryView BuildRecovery(Transform loginPanel, Sprite bg, Sprite backIcon, Sprite lockIcon, Sprite mailIcon)
        {
            var root = UI51Build.Stretch(UI51Build.Child(loginPanel.parent, "UI51Recovery"));
            root.gameObject.SetActive(true); // TMP su oggetti spenti lancia eccezioni
            root.SetSiblingIndex(loginPanel.GetSiblingIndex() + 1);
            // Un canvas suo: si apre anche dalle Impostazioni, quando l'Accesso e' nascosto (alpha 0 del suo CanvasGroup).
            var canvas = UI51Build.GetOrAdd<Canvas>(root);
            canvas.overrideSorting = true;
            canvas.sortingOrder = RecoveryOrder;
            UI51Build.GetOrAdd<GraphicRaycaster>(root);
            UI51Build.GetOrAdd<CanvasGroup>(root).ignoreParentGroups = true;
            var overlay = UI51Shape.Linear((UI51Tokens.Rgba(6, 14, 28, 0.4f), 0f), (UI51Tokens.Rgba(5, 11, 24, 0.65f), 0.3f),
                (UI51Tokens.Rgba(4, 9, 20, 0.9f), 0.6f), (UI51Tokens.Rgba(3, 7, 16, 0.97f), 1f));
            var safe = UI51AccessBuilder.BuildScreen(root, bg, overlay);
            var back = UI51AccessBuilder.RoundButton(safe, "Back", false, 40f, 20f, UI51Tokens.Rgba(11, 29, 58, 0.6f), backIcon, 15f);

            var head = UI51Build.TopBand(UI51Build.Child(safe, "Head"), 0f, 0f, 96f, 200f);
            UI51Build.Column(head, 8f, null, TextAnchor.UpperCenter, false, true);
            UI51Build.Fit(head, false, true);
            var circle = UI51Build.Size(UI51Build.Child(head, "Circle"), 84f, 84f);
            UI51Build.Solid(circle, UI51Tokens.GoldA(0.12f), 42f, 1f, UI51Tokens.GoldA(0.45f));
            UI51Build.Layout(circle, 84f, 84f); // nella colonna senza altezza preferita finiva alto 0
            var icon = UI51Build.Image(UI51Build.Center(UI51Build.Child(circle, "Icon"), 38f, 38f), lockIcon, Color.white);
            UI51Build.Size(UI51Build.Child(head, "Gap"), 0f, 0f); // margin-top 8 del titolo
            var title = UI51Build.NoWrap(UI51Build.Text(UI51Build.Size(UI51Build.Child(head, "Title"), 346f, 26f), "Password dimenticata?",
                FontFace.CinzelBold, 22f, UI51Tokens.Cream, TextAlignmentOptions.Center));
            UI51Build.Layout(title, -1f, 26f);
            var text = UI51Build.Text(UI51Build.Size(UI51Build.Child(head, "Text"), 300f, 40f),
                "Scrivi l’email del tuo account: ti mandiamo un link per scegliere una nuova password.", FontFace.NunitoRegular, 13f,
                UI51Tokens.CreamA(0.7f), TextAlignmentOptions.Top);
            UI51Build.Wrap(text, 13.6f);

            var body = UI51Build.TopBand(UI51Build.Child(safe, "Body"), 22f, 22f, 340f, 200f);
            UI51Build.Stack(body, 14f);
            UI51Build.Fit(body, false, true);

            var form = UI51Build.Child(body, "Form");
            UI51Build.Stack(form, 14f);
            var email = UI51AccessBuilder.BuildInput(form, "Email", "La tua email", mailIcon, TMP_InputField.ContentType.EmailAddress, 50f);
            var send = UI51AccessBuilder.GoldButton(form, "Send", "INVIA IL LINK", 54f, 15f);

            var sent = UI51Build.Child(body, "Sent");
            UI51Build.Stack(sent, 14f);
            var panel = UI51Build.Child(sent, "Panel");
            UI51Build.Shape(panel, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.GoldA(0.25f));
            UI51Build.Row(panel, 12f, UI51Build.Pad(15, 17, 15, 17), TextAnchor.MiddleLeft, true, true); // 14 16 + 1 di bordo
            var tick = UI51Build.Child(panel, "Check");
            UI51Build.Layout(tick, 28f, 28f, -1f, 28f);
            UI51Build.Solid(tick, UI51Tokens.Success, 14f);
            // Spunta del mockup: path M5 12.5 l4.5 4.5 L19 7.5 (viewBox 24), tratto bianco 3.2.
            UI51Build.Polyline(UI51Build.Center(UI51Build.Child(tick, "Mark"), 14f, 14f), new Vector2(24f, 24f), 3.2f, Color.white,
                new Vector2(5f, 12.5f), new Vector2(9.5f, 17f), new Vector2(19f, 7.5f));
            var sentText = UI51Build.Text(UI51Build.Child(panel, "Text"), "Se <b>g•••••@mail.com</b> è l’email di un account 51, il link è in arrivo.",
                FontFace.NunitoRegular, 13f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft);
            UI51Build.Wrap(sentText, 8.6f);
            UI51Build.Layout(sentText, 0f, -1f, 1f);
            var toLogin = UI51AccessBuilder.GoldButton(sent, "ToLogin", "TORNA AL LOGIN", 54f, 15f);
            var resend = UI51AccessBuilder.Link(sent, "Resend", "Non è arrivata? Reinvia", FontFace.NunitoExtraBold, 13f, UI51Tokens.Gold,
                TextAlignmentOptions.Center, 18f);
            var status = UI51AccessBuilder.Status(body, "Status");

            var view = UI51Build.GetOrAdd<UI51RecoveryView>(root);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "back", back);
                UI51Build.Ref(so, "send", send);
                UI51Build.Ref(so, "toLogin", toLogin);
                UI51Build.Ref(so, "resend", resend);
                UI51Build.Ref(so, "email", email);
                UI51Build.Ref(so, "icon", icon);
                UI51Build.Ref(so, "lockIcon", lockIcon);
                UI51Build.Ref(so, "mailIcon", mailIcon);
                UI51Build.Ref(so, "title", title);
                UI51Build.Ref(so, "text", text);
                UI51Build.Ref(so, "status", status);
                UI51Build.Ref(so, "sentText", sentText);
                UI51Build.Ref(so, "resendLabel", resend.GetComponent<TMP_Text>());
                UI51Build.Ref(so, "form", form);
                UI51Build.Ref(so, "sent", sent);
                UI51Build.Ref(so, "sentPanel", panel);
            });
            sent.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// Gioco online sospeso, a tutto schermo: da 110 scudo 74x84, AVVISO DI COMPORTAMENTO, titolo 23 e testo; a 410 il riquadro
        /// MOTIVO / TORNI ONLINE TRA; in basso (30) GIOCA CONTRO I BOT e Regole di comportamento. L'indietro non e' nel mockup.
        /// </summary>
        static UI51SuspensionView BuildSuspension(RectTransform parent, Sprite bg, Sprite backIcon, Sprite shield, QuickSelectionPanels modes,
            LegalModalV2 legal)
        {
            var root = UI51Build.Stretch(UI51Build.Child(parent, "Suspension"));
            root.gameObject.SetActive(true);
            var overlay = UI51Shape.Linear((UI51Tokens.Rgba(4, 9, 20, 0.65f), 0f), (UI51Tokens.Rgba(4, 9, 20, 0.8f), 0.4f),
                (UI51Tokens.Rgba(3, 7, 16, 0.97f), 1f));
            var safe = UI51AccessBuilder.BuildScreen(root, bg, overlay);
            var back = UI51AccessBuilder.RoundButton(safe, "Back", false, 40f, 20f, UI51Tokens.Rgba(11, 29, 58, 0.6f), backIcon, 15f);

            var head = UI51Build.TopBand(UI51Build.Child(safe, "Head"), 24f, 24f, 110f, 260f);
            UI51Build.Stack(head, 0f);
            UI51Build.Fit(head, false, true);
            var icon = UI51Build.Child(head, "Icon");
            UI51Build.Layout(icon, -1f, 84f);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(icon, "Shield"), 74f, 84f), shield, Color.white);
            UI51Build.Gap(head, "Gap1", 16f); // gap 10 + margin-top 6
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(head, "Cap"), "AVVISO DI COMPORTAMENTO", FontFace.CinzelSemiBold,
                10f, UI51Tokens.DangerText, TextAlignmentOptions.Center, 3f)), -1f, 14f);
            UI51Build.Gap(head, "Gap2", 10f);
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(head, "Title"), "Gioco online sospeso", FontFace.CinzelBold, 23f,
                UI51Tokens.Cream, TextAlignmentOptions.Center)), -1f, 27f);
            UI51Build.Gap(head, "Gap3", 10f);
            var text = UI51Build.Text(UI51Build.Child(head, "Text"), UI51SuspensionView.Body("segnalazioni"), FontFace.NunitoRegular, 13f,
                UI51Tokens.CreamA(0.72f), TextAlignmentOptions.Top);
            UI51Build.Wrap(text, 13.6f);
            UI51Build.Layout(text, -1f, -1f);

            var panel = UI51Build.TopBand(UI51Build.Child(safe, "Panel"), 24f, 24f, 410f, 160f);
            UI51Build.Shape(panel, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.4f));
            UI51Build.Stack(panel, 12f, UI51Build.Pad(17, 17, 17, 17)); // 16 + 1 di bordo
            UI51Build.Fit(panel, false, true);
            var reason = PanelRow(panel, "Reason", "MOTIVO", 18f, "Abbandoni ripetuti", FontFace.NunitoExtraBold, 13f, UI51Tokens.Cream);
            UI51Build.Layout(UI51Build.Solid(UI51Build.Child(panel, "Line"), UI51Tokens.GoldA(0.12f), 0f), -1f, 1f);
            var left = PanelRow(panel, "Left", "TORNI ONLINE TRA", 30f, "23:41:09", FontFace.CinzelBold, 22f, UI51Tokens.GoldLight);
            var note = UI51Build.Text(UI51Build.Child(panel, "Note"), "", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.5f),
                TextAlignmentOptions.TopLeft);
            UI51Build.Wrap(note, 8.6f); // line-height 1.45
            UI51Build.Layout(note, -1f, -1f);

            var actions = UI51Build.Child(safe, "Actions");
            actions.anchorMin = new Vector2(0f, 0f);
            actions.anchorMax = new Vector2(1f, 0f);
            actions.pivot = new Vector2(0.5f, 0f);
            actions.sizeDelta = new Vector2(-48f, actions.sizeDelta.y);
            actions.anchoredPosition = new Vector2(0f, 30f);
            UI51Build.Stack(actions, 10f);
            UI51Build.Fit(actions, false, true);
            var play = UI51AccessBuilder.GoldButton(actions, "Play", "GIOCA CONTRO I BOT", 54f, 14f);
            var rulesRt = UI51Build.Child(actions, "Rules");
            UI51PrefabBuilder.ButtonBody(rulesRt.gameObject, 342f, 46f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(14f), 1f, UI51Tokens.GoldA(0.5f),
                FontFace.NunitoExtraBold, 13f, 0f, UI51Tokens.Gold, "Regole di comportamento");
            UI51Build.Layout(rulesRt, -1f, 46f);

            var view = UI51Build.GetOrAdd<UI51SuspensionView>(root);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "back", back);
                UI51Build.Ref(so, "play", play);
                UI51Build.Ref(so, "rules", rulesRt.GetComponent<Button>());
                UI51Build.Ref(so, "text", text);
                UI51Build.Ref(so, "reason", reason);
                UI51Build.Ref(so, "left", left);
                UI51Build.Ref(so, "note", note);
                UI51Build.Ref(so, "modes", modes);
                UI51Build.Ref(so, "legal", legal);
            });
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Riga del riquadro: etichetta Cinzel 10 rossa a sinistra, valore a destra. Ritorna il valore.</summary>
        static TextMeshProUGUI PanelRow(RectTransform panel, string name, string caption, float height, string value, FontFace face, float size,
            Color color)
        {
            var row = UI51Build.Child(panel, name);
            UI51Build.Layout(row, -1f, height);
            UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(row, "Caption")), caption, FontFace.CinzelSemiBold, 10f,
                UI51Tokens.DangerText, TextAlignmentOptions.MidlineLeft, 2f));
            return UI51Build.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(row, "Value")), value, face, size, color,
                TextAlignmentOptions.MidlineRight));
        }

        /// <summary>
        /// Esito segnalazione: velo .55 e scheda centrata (26 dai lati, raggio 22, padding 24 20 20) con lo scudo d'oro nel bagliore,
        /// TAVOLO PULITO, titolo 21, testo, riquadro con data e formato, PREGO!.
        /// </summary>
        static UI51ReportOutcomeView BuildOutcome(RectTransform parent, Sprite shield, Sprite glow, Sprite warn)
        {
            var root = UI51Build.Stretch(UI51Build.Child(parent, "ReportOutcome"));
            root.gameObject.SetActive(true);
            var safe = UI51Build.Child(root, "Safe");
            var fit = UI51Build.GetOrAdd<DesignCanvasFit>(safe);
            fit.Reference = UI51Tokens.ReferenceResolution;
            fit.Fill = true;
            safe.anchorMin = safe.anchorMax = safe.pivot = new Vector2(0.5f, 0.5f);
            safe.anchoredPosition = Vector2.zero;
            safe.sizeDelta = UI51Tokens.ReferenceResolution;
            var scrim = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(safe, "Scrim"), -400f, -400f, -400f, -400f),
                UI51Tokens.Rgba(3, 7, 16, 0.55f), 0f, 0f, default, true);

            var card = UI51Build.Child(safe, "Card");
            card.anchorMin = new Vector2(0f, 0.5f);
            card.anchorMax = new Vector2(1f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(-52f, card.sizeDelta.y);
            card.anchoredPosition = Vector2.zero;
            UI51Build.Shape(card, UI51Tokens.DialogFill(), 180f, UI51Tokens.Radii(22f), 1f, UI51Tokens.GoldA(0.5f), true, UI51Tokens.ShadowDialog);
            UI51Build.Stack(card, 0f, UI51Build.Pad(25, 21, 21, 21));
            UI51Build.Fit(card, false, true);
            var icon = UI51Build.Child(card, "Icon");
            UI51Build.Layout(icon, -1f, 100f);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(icon, "Glow"), 200f, 200f), glow, new Color(1f, 1f, 1f, 0.7f));
            UI51Build.Image(UI51Build.Center(UI51Build.Child(icon, "Shield"), 74f, 84f), shield, Color.white);
            UI51Build.Gap(card, "Gap1", 8f);
            UI51Build.Layout(UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(card, "Cap"), "TAVOLO PULITO", FontFace.CinzelSemiBold, 10f,
                UI51Tokens.Gold, TextAlignmentOptions.Center, 3f)), -1f, 14f);
            UI51Build.Gap(card, "Gap2", 6f);
            var title = UI51Build.NoWrap(UI51Build.Text(UI51Build.Child(card, "Title"), "Grazie per la segnalazione!", FontFace.CinzelBold,
                21f, UI51Tokens.Cream, TextAlignmentOptions.Center));
            UI51Build.Layout(title, -1f, 28f);
            UI51Build.Gap(card, "Gap3", 10f);
            var text = UI51Build.Text(UI51Build.Child(card, "Text"),
                "Un giocatore che hai segnalato è stato sanzionato. Le tue segnalazioni aiutano a mantenere le partite corrette per tutti.",
                FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.72f), TextAlignmentOptions.Top);
            UI51Build.Wrap(text, 13.6f);
            UI51Build.Layout(text, -1f, -1f);
            UI51Build.Gap(card, "Gap4", 14f);
            var infoRow = UI51Build.Child(card, "Info");
            UI51Build.Shape(infoRow, UI51Shape.Solid(UI51Tokens.WhiteA(0.04f)), 180f, UI51Tokens.Radii(14f), 1f, UI51Tokens.GoldA(0.2f));
            UI51Build.Row(infoRow, 10f, UI51Build.Pad(11, 13, 11, 13), TextAnchor.MiddleLeft, true, true);
            var warnRt = UI51Build.Child(infoRow, "Icon");
            UI51Build.Layout(warnRt, 20f, 18f, -1f, 20f);
            UI51Build.Image(warnRt, warn, new Color(1f, 1f, 1f, 0.8f));
            var info = UI51Build.Text(UI51Build.Child(infoRow, "Text"), UI51ReportOutcomeView.Info("2026-09-28", "1 vs 1"), FontFace.NunitoRegular, 12f,
                UI51Tokens.CreamA(0.65f), TextAlignmentOptions.MidlineLeft);
            UI51Build.Wrap(info, 6.1f); // line-height 1.4
            UI51Build.Layout(info, 0f, -1f, 1f);
            UI51Build.Gap(card, "Gap5", 16f);
            var ok = UI51AccessBuilder.GoldButton(card, "Ok", "PREGO!", 50f, 14f);

            var view = UI51Build.GetOrAdd<UI51ReportOutcomeView>(root);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "ok", ok);
                UI51Build.Ref(so, "scrim", UI51Build.Button(scrim, scrim));
                UI51Build.Ref(so, "card", card);
                UI51Build.Ref(so, "title", title);
                UI51Build.Ref(so, "text", text);
                UI51Build.Ref(so, "info", info);
            });
            root.gameObject.SetActive(false);
            return view;
        }
    }
}
