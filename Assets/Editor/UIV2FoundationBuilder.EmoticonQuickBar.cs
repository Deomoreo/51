using Project51.UIV2.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// I3 - scelta rapida delle emoticon al tavolo: al posto del pannello modale (che copriva il tavolo
    /// e fermava il gioco) il pulsante Emoji apre una striscia piccola sopra di se' con le emoticon
    /// equipaggiate. Stessa grafica dei banner (bordo blu, fondo navy). Posizione, larghezza e
    /// chiusura (tocco fuori, invio, 3,5 s) li gestisce GameSocialV2 a runtime. Rilanciabile.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const string QuickBarName = "EmoticonQuickBar";
        private const int QuickBarSlots = 3;

        [MenuItem("Tools/UIV2/Build Emoticon Quick Bar")]
        private static void BuildEmoticonQuickBar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            var canvas = GameObject.Find("GameCanvas");
            var buttons = canvas != null ? canvas.transform.Find("TableActionButtons") : null;
            var emoji = buttons != null ? buttons.Find("EmojiButton") as RectTransform : null;
            var social = Object.FindObjectOfType<GameSocialV2>(true);
            if (emoji == null || social == null) throw new System.Exception("[EmoticonQuickBar] EmojiButton o GameSocialV2 non trovati in GameScene");

            var old = buttons.Find(QuickBarName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var panelSprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
            var bar = CreateUIObject(QuickBarName, buttons);
            bar.anchorMin = bar.anchorMax = new Vector2(0f, 1f); // come i fratelli: posizioni in pixel del design
            bar.pivot = new Vector2(1f, 0f);                      // cresce verso sinistra e verso l'alto
            bar.sizeDelta = new Vector2(356f, 128f);
            bar.anchoredPosition = emoji.anchoredPosition + new Vector2(154f, 60f);
            var group = bar.gameObject.AddComponent<CanvasGroup>();
            var border = bar.gameObject.AddComponent<Image>();
            border.sprite = panelSprite;
            border.type = Image.Type.Sliced;
            border.pixelsPerUnitMultiplier = 2.4f;
            border.color = TableBannerBorder;

            var fill = CreateUIObject("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.sizeDelta = new Vector2(-6f, -6f);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = panelSprite;
            fillImage.type = Image.Type.Sliced;
            fillImage.pixelsPerUnitMultiplier = 2.823529f;
            fillImage.color = TableBannerFill;
            fillImage.raycastTarget = false;

            var slots = new Button[QuickBarSlots];
            var icons = new Image[QuickBarSlots];
            for (int i = 0; i < QuickBarSlots; i++)
            {
                var slot = CreateUIObject("Slot_" + i, bar);
                slot.anchorMin = slot.anchorMax = Vector2.zero; // GameSocialV2 li mette dal basso a sinistra
                slot.pivot = new Vector2(0.5f, 0.5f);
                slot.sizeDelta = new Vector2(96f, 96f);
                var icon = slot.gameObject.AddComponent<Image>();
                icon.preserveAspect = true;
                icon.sprite = social.Sprites != null && social.Sprites.Length > i ? social.Sprites[i] : null;
                var button = slot.gameObject.AddComponent<Button>();
                button.targetGraphic = icon;
                var colors = button.colors;
                colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
                button.colors = colors;
                slots[i] = button;
                icons[i] = icon;
            }

            var hint = CreateUIObject("Hint", bar);
            hint.anchorMin = Vector2.zero;
            hint.anchorMax = Vector2.one;
            hint.sizeDelta = new Vector2(-32f, 0f);
            var hintText = hint.gameObject.AddComponent<TextMeshProUGUI>();
            hintText.text = "Equipaggia le emoticon nella Collezione";
            hintText.fontSize = 24f;
            hintText.color = new Color32(214, 224, 238, 255);
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.raycastTarget = false;
            hint.gameObject.SetActive(false);

            social.QuickBar = group;
            social.QuickBarAnchor = emoji;
            social.QuickSlots = slots;
            social.QuickIcons = icons;
            social.QuickHint = hintText;
            EditorUtility.SetDirty(social);

            bar.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EmoticonQuickBar] Striscia emoticon rapida costruita sopra il pulsante Emoji.");
        }
    }
}
