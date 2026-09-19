using Project51.UIV2.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// H7 - Impostazioni -> Account -> Elimina account, in MainMenu:
    /// - riga rossa "Elimina account" in fondo alla sezione ACCOUNT del pannello Impostazioni (Lingua e
    ///   Supporto scendono a 96 di altezza per farle spazio dentro la cornice, che non cambia);
    /// - finestra di conferma DeleteAccountV2 dentro il ModalHost gia' esistente, su AnimatedModalV2
    ///   come LegalV2: nessuna seconda architettura di modal.
    /// Rilanciabile: rimuove e ricostruisce solo i propri oggetti.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const float DelRowTop = 1478f;
        private const float DelRowHeight = 96f;
        // La riga in piu' finisce a 1574: la cornice cresce a 1650 e la scritta "51Cirulla · v..."
        // sta fra la riga e il bordo oro (prima era a 1630, fuori da una cornice alta 1620).
        private const float SettingsFrameHeight = 1650f;
        private const float SettingsFooterTop = 1584f;

        private static readonly Color DelButtonBorder = new Color32(232, 96, 80, 255);
        private static readonly Color DelButtonFill = new Color32(170, 40, 34, 255);

        [MenuItem("Tools/UIV2/Build Delete Account")]
        private static void BuildDeleteAccount()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            var settings = Object.FindObjectOfType<SettingsV2Integration>(true);
            if (settings == null) throw new System.Exception("SettingsV2Integration non trovato in MainMenu");
            var settingsSo = new SerializedObject(settings);
            var panel = (GameObject)settingsSo.FindProperty("panel").objectReferenceValue;
            var frame = panel.transform.Find("PanelFrame") as RectTransform;
            if (frame == null) throw new System.Exception("SettingsV2/PanelFrame non trovato");

            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsRegularPath);
            var bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsBoldPath);
            var extraBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsExtraBoldPath);
            if (regular == null || bold == null || extraBold == null) throw new System.Exception("Font Poppins non trovati");

            var row = BuildDeleteAccountRow(frame);
            frame.sizeDelta = new Vector2(frame.sizeDelta.x, SettingsFrameHeight);
            var footer = frame.Find("FooterText") as RectTransform;
            if (footer != null) footer.anchoredPosition = new Vector2(footer.anchoredPosition.x, -SettingsFooterTop);
            var modal = BuildDeleteAccountModal(regular, bold, extraBold);

            settingsSo.FindProperty("deleteAccountButton").objectReferenceValue = row;
            settingsSo.FindProperty("deleteAccount").objectReferenceValue = modal;

            // Sezione Account visibile solo dopo l'ingresso: SettingsV2Integration la ricompatta a runtime.
            settingsSo.FindProperty("startScreen").objectReferenceValue = Object.FindObjectOfType<StartScreenV2>(true);
            settingsSo.FindProperty("panelFrame").objectReferenceValue = frame;
            settingsSo.FindProperty("accountHeader").objectReferenceValue = frame.Find("Section_ACCOUNT").GetComponent<TMP_Text>();
            var after = settingsSo.FindProperty("rowsAfterAccount");
            after.arraySize = 2;
            after.GetArrayElementAtIndex(0).objectReferenceValue = frame.Find("Row_Lingua");
            after.GetArrayElementAtIndex(1).objectReferenceValue = frame.Find("Row_Supporto");
            settingsSo.FindProperty("footer").objectReferenceValue = frame.Find("FooterText");
            settingsSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Elimina account (H7): riga nelle Impostazioni e finestra DeleteAccountV2 costruite in MainMenu.");
        }

        private static Button BuildDeleteAccountRow(RectTransform frame)
        {
            var old = frame.Find("Row_EliminaAccount");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // Lingua e Supporto piu' basse (96 invece di 110), stesso passo di 16 fra le righe.
            ResizeInfoRow(frame, "Row_Lingua", 1254f, DelRowHeight);
            ResizeInfoRow(frame, "Row_Supporto", 1366f, DelRowHeight);

            var template = frame.Find("Row_Supporto") as RectTransform;
            var templateTitle = template.Find("Title").GetComponent<TMP_Text>();
            var templateSubtitle = template.Find("Subtitle").GetComponent<TMP_Text>();
            var templateRing = template.Find("Ring").GetComponent<Image>();
            var templateFill = template.GetComponent<Image>();

            var row = CreateUIObject("Row_EliminaAccount", frame);
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(0f, 1f);
            row.sizeDelta = new Vector2(template.sizeDelta.x, DelRowHeight);
            row.anchoredPosition = new Vector2(template.anchoredPosition.x, -DelRowTop);
            row.SetSiblingIndex(template.GetSiblingIndex() + 1);

            // Stessa costruzione delle altre righe (fill + anello 9-slice), nei rossi di "Abbandona partita".
            var fill = row.gameObject.AddComponent<Image>();
            fill.sprite = templateFill.sprite;
            fill.type = Image.Type.Sliced;
            fill.color = SetDangerFill;
            fill.raycastTarget = true;
            var ring = Stretch(row, "Ring").gameObject.AddComponent<Image>();
            ring.sprite = templateRing.sprite;
            ring.type = Image.Type.Sliced;
            ring.color = SetDangerBorder;
            ring.raycastTarget = false;

            var icon = CreateUIObject("Icon", row);
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0f, 0.5f);
            icon.sizeDelta = new Vector2(48f, 48f);
            icon.anchoredPosition = new Vector2(28f, 0f);
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = LoadSprite(IconsPath, "ic_warn");
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            float textWidth = row.sizeDelta.x - 130f;
            var title = CopyTextStyle(templateTitle, row, "Title", "Elimina account", 96f, DelRowHeight * 0.5f - 34f, textWidth, 34f);
            title.color = SetDangerTitle;
            var subtitle = CopyTextStyle(templateSubtitle, row, "Subtitle", "Rimuove l'account in modo permanente", 96f, DelRowHeight * 0.5f + 2f, textWidth, 30f);
            subtitle.color = SetDangerSubtitle;

            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            return button;
        }

        private static void ResizeInfoRow(RectTransform frame, string name, float top, float height)
        {
            var row = frame.Find(name) as RectTransform;
            if (row == null) throw new System.Exception("SettingsV2/PanelFrame/" + name + " non trovato");
            row.sizeDelta = new Vector2(row.sizeDelta.x, height);
            row.anchoredPosition = new Vector2(row.anchoredPosition.x, -top);
            // Stesse formule di ImpostazioniBuilder.CreateInfoRow.
            ((RectTransform)row.Find("Title")).anchoredPosition = new Vector2(96f, -(height * 0.5f - 34f));
            ((RectTransform)row.Find("Subtitle")).anchoredPosition = new Vector2(96f, -(height * 0.5f + 2f));
        }

        private static TMP_Text CopyTextStyle(TMP_Text template, RectTransform parent, string name, string text, float left, float top, float width, float height)
        {
            var rect = CreateUIObject(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(left, -top);
            var label = AddText(rect, text, template.fontSize, template.fontStyle, template.color, template.alignment);
            label.font = template.font;
            label.fontSharedMaterial = template.fontSharedMaterial;
            return label;
        }

        private static DeleteAccountModalV2 BuildDeleteAccountModal(TMP_FontAsset regular, TMP_FontAsset bold, TMP_FontAsset extraBold)
        {
            var host = Object.FindObjectOfType<UIV2ModalHost>(true);
            if (host == null) throw new System.Exception("UIV2ModalHost non trovato in MainMenu");

            var old = host.transform.Find("DeleteAccountV2");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var root = CreateUIObject("DeleteAccountV2", host.transform);
            StretchFill(root);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2200;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var dim = Stretch(root, "Dim");
            var dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = OnDim;
            dimImage.raycastTarget = true;
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.targetGraphic = dimImage;
            dimButton.transition = Selectable.Transition.None;

            var design = CreateUIObject("Design", root);
            design.gameObject.AddComponent<DesignCanvasFit>();
            design.sizeDelta = new Vector2(1080f, 1920f);

            const float left = 90f, top = 655f, width = 900f, height = 610f;
            var frame = MockRect(design, "Frame", left, top, width, height);
            AddRoundedPanel(frame, "panel_fill_r24", 48f, 44f, OnFrameBorder, 9f, OnFrameFill, out _, out _);
            frame.GetComponent<Image>().raycastTarget = true; // i tocchi sulla cornice non chiudono

            // Coordinate interne alla cornice.
            var close = MockButton(frame, "Close", "sq_blue", width - 110f, 30f, 80f, 72f, "", 0f, null);
            MockSprite(close.transform, "Icon", LoadSprite(IconsPath, "ic_x"), 21f, 17f, 38f, 38f, false);

            // ic_warn e ic_check hanno lo stesso riquadro: si scambia solo lo sprite (vedi DeleteAccountModalV2.Show).
            var icon = MockSprite(frame, "Icon", LoadSprite(IconsPath, "ic_warn"), width * 0.5f - 50f, 50f, 100f, 100f, false);
            icon.preserveAspect = true;

            var title = MockText(frame, "Title", DeleteAccountModalV2.AskTitle, 60f, 180f, width - 120f, 64f, 40f,
                FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
            UseFont(title, extraBold, NavyOutlineMaterial());
            title.enableAutoSizing = true;
            title.fontSizeMin = 30f;
            title.fontSizeMax = 40f;

            var body = MockText(frame, "Body", DeleteAccountModalV2.AskBody, 70f, 270f, width - 140f, 170f, 28f,
                FontStyles.Normal, LegalBodyText, TextAlignmentOptions.Top);
            UseFont(body, regular, null);
            body.enableWordWrapping = true;
            body.lineSpacing = 6f;

            const float buttonsTop = 470f, buttonHeight = 88f, buttonWidth = 360f;
            var cancel = MockButton(frame, "Cancel", "btn_blue_long", 70f, buttonsTop, buttonWidth, buttonHeight,
                "ANNULLA", 28f, NavyOutlineMaterial());
            UseFont(cancel.GetComponentInChildren<TMP_Text>(), bold, NavyOutlineMaterial());

            var danger = DeleteDangerButton(frame, width - 70f - buttonWidth, buttonsTop, buttonWidth, buttonHeight, bold, out var dangerLabel);

            var ok = MockButton(frame, "Ok", "btn_blue_long", (width - 420f) * 0.5f, buttonsTop, 420f, buttonHeight,
                "OK", 30f, NavyOutlineMaterial());
            UseFont(ok.GetComponentInChildren<TMP_Text>(), bold, NavyOutlineMaterial());
            ok.gameObject.SetActive(false);

            var animated = root.gameObject.AddComponent<AnimatedModalV2>();
            animated.Group = group;
            animated.Frame = frame;
            animated.CloseButton = close;
            animated.Dimmer = dimButton;

            var modal = root.gameObject.AddComponent<DeleteAccountModalV2>();
            modal.Modal = animated;
            modal.Title = title;
            modal.Body = body;
            modal.Cancel = cancel;
            modal.Danger = danger;
            modal.DangerLabel = dangerLabel;
            modal.Ok = ok;
            modal.Icon = icon;
            modal.WarnIcon = LoadSprite(IconsPath, "ic_warn");
            modal.DoneIcon = LoadSprite(IconsPath, "ic_check");

            root.gameObject.SetActive(false); // AnimatedModalV2 considera aperto = oggetto attivo
            return modal;
        }

        /// <summary>
        /// Nel kit non c'e' un pulsante rosso: pannello arrotondato tinto, stessa tecnica delle righe
        /// "pericolo" (Abbandona partita) ma piu' acceso perche' e' un pulsante.
        /// </summary>
        private static Button DeleteDangerButton(Transform parent, float left, float top, float width, float height,
            TMP_FontAsset font, out TMP_Text label)
        {
            var rect = MockRect(parent, "Danger", left, top, width, height);
            var border = AddRoundedPanel(rect, "panel_fill_r24", 48f, 26f, DelButtonBorder, 4f, DelButtonFill, out _, out _);
            border.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = border;
            var colors = button.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.55f);
            button.colors = colors;

            label = AddText(Stretch(rect, "Label"), "CONTINUA", 28f, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
            UseFont(label, font, null);
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 28f;
            label.margin = new Vector4(16f, 0f, 16f, 0f);
            return button;
        }
    }
}
