using Project51.UIV2.Core;
using Project51.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Impostazioni in partita dal mockup 26_impostazioni_ingame (GameScene/GamePresentationV2/InGameSettings):
    /// cornice oro, nastro, due sezioni di interruttori, abbandona partita, sfocatura vera del tavolo sotto
    /// al velo "Sfocatura sfondo". Collega l'ingranaggio della barra in alto, il bagliore dei suggerimenti
    /// e toglie l'ESCI in piu' dal fine smazzata. Rilanciabile.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const string PoppinsRegularPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/poppins-regular SDF.asset";
        private const string VeilSpritePath = GlowSheetDir + "Sfocatura sfondo.png";

        private static readonly Color SetFrameGold = new Color32(232, 178, 74, 255);
        private static readonly Color SetFrameBrown = new Color32(120, 80, 24, 255);
        private static readonly Color SetFrameFill = new Color32(24, 40, 64, 255);
        private static readonly Color SetRowBorder = new Color32(70, 102, 142, 255);
        private static readonly Color SetRowFill = new Color32(20, 34, 54, 255);
        private static readonly Color SetTitle = new Color32(255, 250, 238, 255);
        private static readonly Color SetSubtitle = new Color32(186, 205, 228, 255);
        private static readonly Color SetToggleOff = new Color32(44, 62, 88, 255);
        private static readonly Color SetToggleOn = new Color32(46, 168, 120, 255);
        private static readonly Color SetKnobRing = new Color32(120, 140, 165, 255);
        private static readonly Color SetKnobFill = new Color32(248, 250, 252, 255);
        private static readonly Color SetDangerBorder = new Color32(200, 64, 52, 255);
        private static readonly Color SetDangerFill = new Color32(56, 21, 23, 255);
        private static readonly Color SetDangerTitle = new Color32(255, 220, 214, 255);
        private static readonly Color SetDangerSubtitle = new Color32(214, 150, 144, 255);
        private static readonly Color SetFooter = new Color32(140, 160, 190, 255);

        [MenuItem("Tools/UIV2/Build In-Game Settings")]
        private static void BuildInGameSettings()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var social = Object.FindObjectOfType<GameSocialV2>(true);
            if (social == null) throw new System.Exception("GamePresentationV2 non trovato in GameScene");

            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsRegularPath);
            var semiBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsSemiBoldPath);
            var extraBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsExtraBoldPath);
            if (regular == null || semiBold == null || extraBold == null) throw new System.Exception("Font Poppins non trovati");
            var navyOutline = GetOutlineMaterial("Outline Navy", OutlineNavy, 0.3f, 0.2f, extraBold);
            var thinNavyOutline = GetOutlineMaterial("Outline Navy Thin", OutlineNavy, 0.18f, 0.1f, extraBold);

            var root = social.transform;
            var old = root.Find("InGameSettings");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var container = CreateUIObject("InGameSettings", root);
            StretchFill(container);
            var controller = container.gameObject.AddComponent<InGameSettingsV2>();

            var design = OnlinePanel(container, "Panel", out var panel);
            controller.Panel = panel;
            controller.Group = panel.GetComponent<CanvasGroup>();

            // Sfondo: foto sfocata del tavolo + velo; il vecchio oscuramento resta solo per i tocchi.
            var dim = panel.transform.Find("Dim");
            controller.Blur = AddBlurBackdrop(panel.transform);
            dim.SetAsLastSibling();
            design.SetAsLastSibling();
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            controller.Backdrop = dim.gameObject.AddComponent<Button>();
            controller.Backdrop.transition = Selectable.Transition.None;

            // La scala di Design la decide DesignCanvasFit: l'animazione di apertura scala Window.
            var window = Stretch(design, "Window");
            controller.Window = window;

            var frame = MockRect(window, "Frame", 60f, 380f, 961f, 1161f);
            AddRoundedPanel(frame, "panel_fill_r24", 48f, 28f, SetFrameGold, 8f, SetFrameBrown, out var brown, out _);
            frame.GetComponent<Image>().raycastTarget = true; // i tocchi sulla cornice non chiudono
            var fill = Stretch(brown, "Inner");
            fill.offsetMin = new Vector2(6f, 6f);
            fill.offsetMax = new Vector2(-6f, -6f);
            SetRounded(fill.gameObject.AddComponent<Image>(), LoadSprite(PanelsNeutralPath, "panel_fill_r24"), SetFrameFill, 14f);

            // Nel mockup il nastro e' piu' largo delle proporzioni dello sprite: 9-slice in larghezza.
            MockSprite(window, "Ribbon", LoadSprite(IconsPath, "ribbon_teal"), 282f, 329f, 516f, 173f, true);
            var title = MockText(window, "Title", "IMPOSTAZIONI", 325f, 384f, 420f, 60f, 36f, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
            UseFont(title, extraBold, navyOutline);

            controller.Close = MockButton(window, "Close", "sq_blue", 924f, 407f, 80f, 73f, "", 0f, null);
            var closeIcon = MockSprite(controller.Close.transform, "Icon", LoadSprite(IconsPath, "ic_x"), 23.5f, 20.5f, 33f, 33f, false);
            closeIcon.raycastTarget = false;

            SettingsSection(window, "PartitaHeader", "PARTITA", 504f, semiBold);
            controller.FastAnimations = SettingsToggleRow(window, "FastAnimations", "Animazioni veloci", "Riduce i tempi delle animazioni", 560f, extraBold, thinNavyOutline, regular, false);
            controller.MoveHints = SettingsToggleRow(window, "MoveHints", "Suggerimenti mosse", "Evidenzia le carte che fanno una presa", 678f, extraBold, thinNavyOutline, regular, true);

            SettingsSection(window, "AudioHeader", "AUDIO", 800f, semiBold);
            controller.Music = SettingsToggleRow(window, "Music", "Musica", "Musica di sottofondo", 856f, extraBold, thinNavyOutline, regular, true);
            controller.Effects = SettingsToggleRow(window, "Effects", "Effetti sonori", "Carte, prese, accusi", 974f, extraBold, thinNavyOutline, regular, true);

            var divider = MockRect(window, "Divider", 90f, 1122f, 901f, 2f).gameObject.AddComponent<Image>();
            divider.color = SetRowBorder;
            divider.raycastTarget = false;

            var abandon = MockRect(window, "Abandon", 90f, 1156f, 901f, 129f);
            var abandonBorder = AddRoundedPanel(abandon, "panel_fill_r24", 48f, 24f, SetDangerBorder, 3f, SetDangerFill, out _, out _);
            abandonBorder.raycastTarget = true;
            controller.Abandon = abandon.gameObject.AddComponent<Button>();
            controller.Abandon.targetGraphic = abandonBorder;
            MockSprite(abandon, "Icon", LoadSprite(IconsPath, "ic_x"), 36f, 48f, 43f, 43f, false);
            controller.AbandonTitle = MockText(abandon, "Title", "Abbandona partita", 99f, 20f, 760f, 44f, 26f, FontStyles.Normal, SetDangerTitle, TextAlignmentOptions.MidlineLeft);
            UseFont(controller.AbandonTitle, extraBold, null);
            controller.AbandonSubtitle = MockText(abandon, "Subtitle", "Conta come sconfitta", 99f, 60f, 760f, 38f, 19f, FontStyles.Normal, SetDangerSubtitle, TextAlignmentOptions.MidlineLeft);
            UseFont(controller.AbandonSubtitle, regular, null);

            // Il mockup dice "Torna al menu...": la X chiude il pannello e riporta alla partita.
            var footer = MockText(window, "Footer", "Per tornare alla partita chiudi con la X in alto", 90f, 1309f, 900f, 36f, 20f, FontStyles.Normal, SetFooter, TextAlignmentOptions.Center);
            UseFont(footer, regular, null);

            // Ingranaggio della barra in alto del tavolo.
            var canvas = GameObject.Find("GameCanvas");
            var gear = canvas != null ? canvas.transform.Find("TableTopBar/SettingsButton") : null;
            controller.OpenButton = gear != null ? gear.GetComponent<Button>() : null;
            if (controller.OpenButton == null) Debug.LogWarning("[UIV2FoundationBuilder] TableTopBar/SettingsButton non trovato: il pannello non ha un pulsante per aprirsi.");

            // Bagliore dietro alle carte che fanno una presa.
            var cardViews = Object.FindObjectOfType<CardViewManager>(true);
            if (cardViews != null)
            {
                SetPrivateField(cardViews, "moveHintGlowSprite", LoadSprite(GlowSheetDir + "Bagliore morbido cerchio.png", "Bagliore morbido cerchio"));
                EditorUtility.SetDirty(cardViews);
            }

            // Fine smazzata come nel mockup 13: niente ESCI, si abbandona da qui.
            var results = root.GetComponent<MatchResultsV2>();
            if (results != null && results.RoundExit != null)
            {
                Object.DestroyImmediate(results.RoundExit.gameObject);
                results.RoundExit = null;
                EditorUtility.SetDirty(results);
            }

            EditorUtility.SetDirty(controller);
            HidePanels(panel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Impostazioni in partita (mockup 26) costruite in GameScene.");
        }

        /// <summary>
        /// Sfondo dei pannelli sopra al tavolo: foto sfocata (BackdropBlur, spenta finche' non scatta) e
        /// velo "Sfocatura sfondo" sopra. Blocca i tocchi verso il tavolo.
        /// </summary>
        private static BackdropBlur AddBlurBackdrop(Transform parent)
        {
            var blurRect = Stretch(parent, "Blur");
            blurRect.SetAsFirstSibling();
            var raw = blurRect.gameObject.AddComponent<RawImage>();
            raw.color = Color.white;
            raw.raycastTarget = true;
            raw.enabled = false;
            var blur = blurRect.gameObject.AddComponent<BackdropBlur>();

            var veilRect = Stretch(parent, "Veil");
            veilRect.SetSiblingIndex(1);
            var veil = veilRect.gameObject.AddComponent<Image>();
            veil.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(VeilSpritePath);
            veil.color = Color.white;
            veil.raycastTarget = true;
            return blur;
        }

        private static void SettingsSection(RectTransform parent, string name, string title, float top, TMP_FontAsset font)
        {
            var label = SectionHeader(parent, name, title, 105f, top, 875f);
            UseFont(label, font, null);
            label.fontStyle = FontStyles.Normal;
            label.color = SetFrameGold;
            // SectionHeader misura il testo col font predefinito: riallinea la linea al testo in SemiBold.
            var line = parent.Find(name + "Line") as RectTransform;
            float textWidth = label.GetPreferredValues(title).x;
            float left = 105f + textWidth + 20f;
            float width = 980f - left;
            line.anchoredPosition = new Vector2(left + width * 0.5f, line.anchoredPosition.y);
            line.sizeDelta = new Vector2(width, 2f);
            line.GetComponent<Image>().color = SetRowBorder;
        }

        private static SimpleToggleSwitch SettingsToggleRow(RectTransform parent, string name, string title, string subtitle, float top,
            TMP_FontAsset titleFont, Material titleMaterial, TMP_FontAsset subtitleFont, bool on)
        {
            var row = MockRect(parent, name, 90f, top, 901f, 105f);
            AddRoundedPanel(row, "panel_fill_r24", 48f, 22f, SetRowBorder, 3f, SetRowFill, out _, out _);

            var titleText = MockText(row, "Title", title, 26f, 12f, 700f, 44f, 26f, FontStyles.Normal, SetTitle, TextAlignmentOptions.MidlineLeft);
            UseFont(titleText, titleFont, titleMaterial);
            var subtitleText = MockText(row, "Subtitle", subtitle, 26f, 53f, 720f, 38f, 19.5f, FontStyles.Normal, SetSubtitle, TextAlignmentOptions.MidlineLeft);
            UseFont(subtitleText, subtitleFont, null);

            var toggleRect = MockRect(row, "Toggle", 767f, 33f, 113f, 55f);
            var track = toggleRect.gameObject.AddComponent<Image>();
            SetRounded(track, LoadSprite(PanelsNeutralPath, "panel_fill_r24"), on ? SetToggleOn : SetToggleOff, 27.5f);
            track.raycastTarget = true;
            var button = toggleRect.gameObject.AddComponent<Button>();
            button.targetGraphic = track;
            button.transition = Selectable.Transition.None;

            var knob = CreateUIObject("Knob", toggleRect);
            knob.anchorMin = knob.anchorMax = knob.pivot = new Vector2(0.5f, 0.5f);
            knob.sizeDelta = new Vector2(53f, 53f);
            knob.anchoredPosition = new Vector2(on ? 29f : -29f, 0f);
            AddRoundedPanel(knob, "panel_fill_r24", 48f, 26.5f, SetKnobRing, 2f, SetKnobFill, out _, out _);

            var toggle = toggleRect.gameObject.AddComponent<SimpleToggleSwitch>();
            var so = new SerializedObject(toggle);
            so.FindProperty("trackImage").objectReferenceValue = track;
            so.FindProperty("knob").objectReferenceValue = knob;
            so.FindProperty("colorOn").colorValue = SetToggleOn;
            so.FindProperty("colorOff").colorValue = SetToggleOff;
            so.FindProperty("knobOffsetX").floatValue = 29f;
            so.FindProperty("isOn").boolValue = on;
            so.ApplyModifiedPropertiesWithoutUndo();
            return toggle;
        }
    }
}
