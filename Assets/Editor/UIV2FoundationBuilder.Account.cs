using Project51.Auth;
using Project51.UIV2.Components;
using Project51.UIV2.Core;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// "Il tuo account" (H12), per chi ha fatto un login vero: al posto del vecchio AccountPanel
    /// (Logout/Back senza grafica). Nessun mockup: stesso linguaggio di Accesso e Registrazione
    /// (fondale, freccia indietro, riquadri dei campi, pulsante blu lungo). Chiamato da
    /// Tools/UIV2/Build Auth Screens.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const float AccountRowsTop = 540f;
        private const float AccountRowHeight = 96f;

        private static void BuildAccountScreen(AuthUIController auth, AuthScreensV2 screens, TMP_FontAsset bold, TMP_FontAsset extraBold)
        {
            var panel = auth.transform.Find("AccountPanel");
            if (panel == null) throw new System.Exception("AccountPanel non trovato sotto Canvas_Login");
            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsRegularPath);

            bool wasActive = panel.gameObject.activeSelf;
            panel.gameObject.SetActive(true);
            for (int i = panel.childCount - 1; i >= 0; i--) Object.DestroyImmediate(panel.GetChild(i).gameObject);

            var background = panel.GetComponent<Image>();
            background.sprite = null;
            background.color = Color.white;
            background.material = AssetDatabase.LoadAssetAtPath<Material>(FlowBackdropPath);
            background.raycastTarget = true;

            var design = CreateUIObject("Design", panel);
            design.gameObject.AddComponent<DesignCanvasFit>();
            design.sizeDelta = new Vector2(1080f, 1920f);

            var back = BackButton(design, "Back", 78f, 96f, 88f);
            // Stessa cornice con sagoma del Profilo quando non c'e' un ritratto.
            MockSprite(design, "Avatar", LoadSprite(IconsPath, "avatar_frame"), 430f, 112f, 220f, 224f, false);

            var name = MockText(design, "Name", "Giocatore", 60f, 356f, 960f, 54f, 32f, FontStyles.Normal, AuthTitleGold, TextAlignmentOptions.Center);
            UseFont(name, extraBold, extraBold.material);
            name.enableAutoSizing = true;
            name.fontSizeMin = 22f;
            name.fontSizeMax = 32f;
            var status = MockText(design, "Status", "Account registrato", 90f, 410f, 900f, 40f, 21f, FontStyles.Normal, AuthSubtitle, TextAlignmentOptions.Center);
            UseFont(status, regular, null);

            var header = MockText(design, "Header", "DATI DELL'ACCOUNT", 91f, 488f, 898f, 34f, 20f, FontStyles.Normal, AuthTitleGold, TextAlignmentOptions.MidlineLeft);
            UseFont(header, bold, null);

            // Riquadro unico con tre righe etichetta/valore, divise da linee sottili.
            var box = MockRect(design, "Details", 91f, AccountRowsTop, 898f, AccountRowHeight * 3f);
            AddRoundedPanel(box, "panel_fill_r24", 48f, 24f, AuthFieldBorder, 2f, AuthFieldFill, out _, out _);
            screens.AccountUsername = AccountRow(box, 0, "Nome utente", regular, bold);
            screens.AccountEmail = AccountRow(box, 1, "Email", regular, bold);
            screens.AccountId = AccountRow(box, 2, "ID giocatore", regular, bold);
            for (int i = 1; i < 3; i++)
            {
                var line = MockRect(box, "Line" + i, 24f, AccountRowHeight * i - 1f, 850f, 2f).gameObject.AddComponent<Image>();
                line.color = AuthFieldBorder;
                line.raycastTarget = false;
            }

            float noteTop = AccountRowsTop + AccountRowHeight * 3f + 28f;
            var note = MockText(design, "Note", "I tuoi progressi sono legati a questo account: accedi con la stessa email per ritrovarli su qualsiasi dispositivo.",
                110f, noteTop, 860f, 64f, 19f, FontStyles.Normal, AuthSoftLink, TextAlignmentOptions.Top);
            UseFont(note, regular, null);
            note.enableWordWrapping = true;

            var logout = MockButton(design, "Logout", "btn_blue_long", 91f, noteTop + 110f, 898f, 92f, "ESCI DALL'ACCOUNT", 23f, NavyOutlineMaterial());
            UseFont(logout.GetComponentInChildren<TMP_Text>(), bold, NavyOutlineMaterial());

            var version = MockText(design, "Version", "v" + PlayerSettings.bundleVersion, 90f, 1846f, 900f, 38f, 17f, FontStyles.Normal, AuthVersion, TextAlignmentOptions.Center);
            version.gameObject.AddComponent<VersionLabelV2>();

            // AuthUIController continua ad aprire e chiudere il pannello; nome e stato li scrive
            // AuthScreensV2 (il controller storico scriveva "Guest" a chi accede da un altro dispositivo)
            // e l'uscita non passa piu' dal suo Logout, che riportava al pannello Ospite legacy.
            SetPrivateField(auth, "accountPanel", panel.gameObject);
            SetPrivateField(auth, "accountPlayerNameText", null);
            SetPrivateField(auth, "accountStatusText", null);
            SetPrivateField(auth, "accountCloseButton", back);
            SetPrivateField(auth, "logoutButton", null);

            screens.AccountPanel = panel.gameObject;
            screens.AccountStatus = status;
            screens.AccountLogout = logout;
            screens.AccountName = name;

            panel.gameObject.SetActive(wasActive);
        }

        private static TMP_Text AccountRow(RectTransform box, int index, string label, TMP_FontAsset regular, TMP_FontAsset bold)
        {
            float top = AccountRowHeight * index;
            var caption = MockText(box, "Label" + index, label, 28f, top, 260f, AccountRowHeight, 20f, FontStyles.Normal, AuthPlaceholder, TextAlignmentOptions.MidlineLeft);
            UseFont(caption, regular, null);
            var value = MockText(box, "Value" + index, "—", 290f, top, 580f, AccountRowHeight, 22f, FontStyles.Normal, AuthStrongLink, TextAlignmentOptions.MidlineRight);
            UseFont(value, bold, null);
            value.enableWordWrapping = false;
            value.overflowMode = TextOverflowModes.Ellipsis;
            value.richText = false;
            return value;
        }
    }
}
