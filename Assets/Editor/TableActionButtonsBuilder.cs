using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project51.Unity.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce i pulsanti Emoji/Accuso (Assets/UI_SPEC_Tavolo.md, sezione 9) dentro
    /// GameCanvas in GameScene.unity, fissi in basso a destra sopra la mano locale.
    /// Nessuno dei due e' collegato a un'azione di gioco reale (vedi TableActionButtonsController
    /// e sezione 10 della spec: il pannello emoticon non esiste ancora, gli accusi sono
    /// rilevati automaticamente e non dichiarati manualmente).
    /// </summary>
    public static class TableActionButtonsBuilder
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";

        // Y allineata a Banner_Local (TablePlayerBannersBuilder), su richiesta esplicita.
        private static readonly Vector2 EmojiCenter = new Vector2(832f, 1560f);
        private static readonly Vector2 AccusoCenter = new Vector2(942f, 1560f);
        private static readonly Vector2 ButtonSize = new Vector2(88f, 88f);
        private static readonly Color AccusoGlowColor = HexColor("#E8B24A", 0.55f);

        [MenuItem("Tools/51/Build Table Action Buttons")]
        private static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvasGO = GameObject.Find("GameCanvas");
            if (canvasGO == null)
            {
                Debug.LogError("[TableActionButtonsBuilder] 'GameCanvas' non trovato in GameScene.unity.");
                return;
            }

            var existing = canvasGO.transform.Find("TableActionButtons");
            if (existing != null)
            {
                Debug.LogWarning("[TableActionButtonsBuilder] 'TableActionButtons' esiste gia': lo rimuovo e ricreo.");
                Object.DestroyImmediate(existing.gameObject);
            }

            var root = new GameObject("TableActionButtons", typeof(RectTransform));
            root.transform.SetParent(canvasGO.transform, false);
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0f, 1f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = Vector2.zero;
            rootRt.anchoredPosition = Vector2.zero;

            var emojiButton = BuildButton(rootRt, "EmojiButton", EmojiCenter, "sq_blue", "ic_person", withGlow: false);
            var accusoButton = BuildButton(rootRt, "AccusoButton", AccusoCenter, "sq_gold", "ic_warn", withGlow: true);

            // Countdown della finestra Accuso manuale (TurnController.AccusoWindowSecondsRemaining),
            // sopra il bottone: nascosto di default, TableActionButtonsController lo attiva/spegne.
            var countdownRt = CreateUIObject("AccusoCountdown", accusoButton.GetComponent<RectTransform>());
            countdownRt.anchorMin = countdownRt.anchorMax = new Vector2(0.5f, 1f);
            countdownRt.pivot = new Vector2(0.5f, 0f);
            countdownRt.sizeDelta = new Vector2(60f, 30f);
            countdownRt.anchoredPosition = new Vector2(0f, 6f);
            var countdownText = countdownRt.gameObject.AddComponent<TextMeshProUGUI>();
            countdownText.text = "3";
            countdownText.fontSize = 26f;
            countdownText.fontStyle = FontStyles.Bold;
            countdownText.color = HexColor("#F5EFE0");
            countdownText.alignment = TextAlignmentOptions.Center;
            countdownRt.gameObject.SetActive(false);

            var controllerGO = new GameObject("TableActionButtonsController", typeof(TableActionButtonsController));
            controllerGO.transform.SetParent(rootRt, false);
            var so = new SerializedObject(controllerGO.GetComponent<TableActionButtonsController>());
            so.FindProperty("emojiButton").objectReferenceValue = emojiButton;
            so.FindProperty("accusoButton").objectReferenceValue = accusoButton;
            so.FindProperty("accusoCountdownText").objectReferenceValue = countdownText;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[TableActionButtonsBuilder] Pulsanti Emoji/Accuso creati. Accuso collegato a TryDeclareLocalManualAccuso (vedi TableActionButtonsController). Emoji ancora senza pannello reale.");
        }

        private static Button BuildButton(RectTransform parent, string name, Vector2 center, string bgSpriteName, string iconSpriteName, bool withGlow)
        {
            var buttonRt = CreateUIObject(name, parent);
            buttonRt.anchorMin = buttonRt.anchorMax = new Vector2(0f, 1f);
            buttonRt.pivot = new Vector2(0.5f, 0.5f);
            buttonRt.sizeDelta = ButtonSize;
            buttonRt.anchoredPosition = new Vector2(center.x, -center.y);

            if (withGlow)
            {
                // Bagliore dorato dietro, sempre attivo (sezione 9: "bagliore dorato dietro sempre attivo").
                var glowRt = CreateUIObject("Glow", buttonRt);
                SetStretchWithPadding(glowRt, -12f);
                glowRt.gameObject.AddComponent<Image>().color = AccusoGlowColor;
            }

            var bgSprite = FindSprite(bgSpriteName);
            var bgImage = buttonRt.gameObject.AddComponent<Image>();
            if (bgSprite != null)
            {
                bgImage.sprite = bgSprite;
                bgImage.type = Image.Type.Sliced;
            }
            else
            {
                bgImage.color = HexColor("#1C2E44");
            }
            var button = buttonRt.gameObject.AddComponent<Button>();

            var iconSprite = FindSprite(iconSpriteName);
            if (iconSprite != null)
            {
                var icon = CreateUIObject("Icon", buttonRt);
                icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 0.5f);
                icon.pivot = new Vector2(0.5f, 0.5f);
                icon.sizeDelta = new Vector2(46f, 46f);
                icon.anchoredPosition = Vector2.zero;
                var img = icon.gameObject.AddComponent<Image>();
                img.sprite = iconSprite;
                img.preserveAspect = true;
            }

            return button;
        }

        private static Sprite FindSprite(string exactName)
        {
            var guids = AssetDatabase.FindAssets($"{exactName} t:Sprite");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == exactName)
                {
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
            return null;
        }

        private static RectTransform CreateUIObject(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void SetStretchWithPadding(RectTransform rt, float padding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-padding, -padding);
            rt.offsetMax = new Vector2(padding, padding);
        }

        private static Color HexColor(string hex, float alphaOverride = -1f)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c))
            {
                if (alphaOverride >= 0f) c.a = alphaOverride;
                return c;
            }
            return Color.magenta;
        }
    }
}
