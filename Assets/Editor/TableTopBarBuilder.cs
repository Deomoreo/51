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
    /// Costruisce la barra superiore del tavolo (Assets/UI_SPEC_Tavolo.md, sezione 2) dentro
    /// GameCanvas in GameScene.unity: bottone impostazioni (solo visivo, vedi sezione 10),
    /// "Mano X di Y" e "Carte rimaste N" agganciati a dati reali via TableTopBarController.
    /// </summary>
    public static class TableTopBarBuilder
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";

        private static readonly Color BarColor = HexColor("#091220", 0.95f);
        private static readonly Color HandTextColor = HexColor("#F5EFE0");
        private static readonly Color CardsLeftTextColor = HexColor("#9AA6B8"); // "TextMuted"

        [MenuItem("Tools/51/Build Table Top Bar")]
        private static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvasGO = GameObject.Find("GameCanvas");
            if (canvasGO == null)
            {
                Debug.LogError("[TableTopBarBuilder] 'GameCanvas' non trovato in GameScene.unity.");
                return;
            }

            var existing = canvasGO.transform.Find("TableTopBar");
            if (existing != null)
            {
                Debug.LogWarning("[TableTopBarBuilder] 'TableTopBar' esiste gia': lo rimuovo e ricreo.");
                Object.DestroyImmediate(existing.gameObject);
            }

            var root = new GameObject("TableTopBar", typeof(RectTransform));
            root.transform.SetParent(canvasGO.transform, false);
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = new Vector2(0f, 1f);
            rootRt.anchorMax = new Vector2(1f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.sizeDelta = new Vector2(0f, 124f);
            rootRt.anchoredPosition = Vector2.zero;

            // Si sposta in basso da solo in base al notch/status bar del device (Screen.safeArea):
            // niente piu' offset fisso tarato su un solo telefono (es. iPhone 12) che sbaglierebbe su tutti gli altri.
            root.AddComponent<SafeAreaTopOffset>();

            var background = CreateUIObject("Background", rootRt);
            StretchFill(background);
            background.gameObject.AddComponent<Image>().color = BarColor;

            BuildSettingsButton(rootRt);

            var handText = CreateUIObject("HandText", rootRt);
            handText.anchorMin = handText.anchorMax = new Vector2(0.5f, 1f);
            handText.pivot = new Vector2(0.5f, 1f);
            handText.sizeDelta = new Vector2(320f, 40f);
            handText.anchoredPosition = new Vector2(-70f, -44f);
            var handTmp = AddText(handText, "Mano 1 di 1", 24f, FontStyles.Bold, HandTextColor, TextAlignmentOptions.Center);

            var cardsLeftText = CreateUIObject("CardsLeftText", rootRt);
            cardsLeftText.anchorMin = cardsLeftText.anchorMax = new Vector2(0.5f, 1f);
            cardsLeftText.pivot = new Vector2(0.5f, 1f);
            cardsLeftText.sizeDelta = new Vector2(260f, 36f);
            cardsLeftText.anchoredPosition = new Vector2(150f, -46f);
            var cardsLeftTmp = AddText(cardsLeftText, "Carte rimaste 0", 22f, FontStyles.Normal, CardsLeftTextColor, TextAlignmentOptions.Center);

            var controllerGO = new GameObject("TableTopBarController", typeof(TableTopBarController));
            controllerGO.transform.SetParent(rootRt, false);
            var controller = controllerGO.GetComponent<TableTopBarController>();
            var so = new SerializedObject(controller);
            so.FindProperty("handText").objectReferenceValue = handTmp;
            so.FindProperty("cardsLeftText").objectReferenceValue = cardsLeftTmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[TableTopBarBuilder] Barra superiore creata. Bottone Impostazioni SOLO visivo: nessun pannello Impostazioni esiste ancora nel progetto (vedi sezione 10 della spec).");
        }

        private static void BuildSettingsButton(RectTransform parent)
        {
            var button = CreateUIObject("SettingsButton", parent);
            button.anchorMin = button.anchorMax = new Vector2(0f, 1f);
            button.pivot = new Vector2(0f, 1f);
            button.sizeDelta = new Vector2(84f, 84f);
            button.anchoredPosition = new Vector2(26f, -22f);

            var bgSprite = FindSprite("sq_blue");
            var bgImage = button.gameObject.AddComponent<Image>();
            if (bgSprite != null)
            {
                bgImage.sprite = bgSprite;
                bgImage.type = Image.Type.Sliced;
            }
            else
            {
                bgImage.color = HexColor("#1C2E44");
            }
            button.gameObject.AddComponent<Button>();

            var iconSprite = FindSprite("ic_gear");
            if (iconSprite != null)
            {
                var icon = CreateUIObject("Icon", button);
                icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 0.5f);
                icon.pivot = new Vector2(0.5f, 0.5f);
                icon.sizeDelta = new Vector2(46f, 46f);
                icon.anchoredPosition = Vector2.zero;
                var img = icon.gameObject.AddComponent<Image>();
                img.sprite = iconSprite;
                img.preserveAspect = true;
            }
        }

        private static Sprite FindSprite(string exactName)
        {
            // Gli sprite UI ora sono sotto-asset dei fogli (Icons.png...): si cerca per nome dello
            // sprite, non piu' per nome del file PNG (i PNG singoli sono stati rimossi).
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/UI/Sprites" }))
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                {
                    if (asset is Sprite sprite && sprite.name == exactName) return sprite;
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

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TMP_Text AddText(RectTransform rt, string text, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableAutoSizing = false;
            return tmp;
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
