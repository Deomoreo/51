using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce il banner di testo "il dealer ha fatto Scopa da 15/30" dentro GameCanvas in
    /// GameScene.unity: un piccolo pannello centrato in alto (NON a schermo intero - le carte
    /// vere restano visibili sul tavolo sotto, vedi TurnController.PlayDealerAccusoRevealIfAny),
    /// spento di default, mostrato solo quando RoundManager.OnDealerAccusoDeclared scatta.
    /// </summary>
    public static class DealerAccusoRevealBuilder
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";

        private static readonly Color BannerColor = HexColor("#050A12", 0.92f);
        private static readonly Color TitleColor = HexColor("#E8B24A");

        private static readonly Vector2 BannerSize = new Vector2(760f, 90f);
        private const float BannerY = 260f; // sopra il centro tavolo, relativo al centro schermo

        [MenuItem("Tools/51/Build Dealer Accuso Reveal")]
        private static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvasGO = GameObject.Find("GameCanvas");
            if (canvasGO == null)
            {
                Debug.LogError("[DealerAccusoRevealBuilder] 'GameCanvas' non trovato in GameScene.unity.");
                return;
            }

            var existingRoot = canvasGO.transform.Find("DealerAccusoReveal");
            if (existingRoot != null)
            {
                Debug.LogWarning("[DealerAccusoRevealBuilder] 'DealerAccusoReveal' esiste gia': lo rimuovo e ricreo.");
                Object.DestroyImmediate(existingRoot.gameObject);
            }

            var root = new GameObject("DealerAccusoReveal", typeof(RectTransform));
            root.transform.SetParent(canvasGO.transform, false);
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = BannerSize;
            rootRt.anchoredPosition = new Vector2(0f, BannerY);
            root.SetActive(false);

            var background = CreateUIObject("Background", rootRt);
            StretchFill(background);
            background.gameObject.AddComponent<Image>().color = BannerColor;

            var textRt = CreateUIObject("Text", rootRt);
            StretchFill(textRt);
            var resultText = AddText(textRt, "Il mazziere fa Scopa!", 28f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center);

            var controllerGO = new GameObject("DealerAccusoRevealController", typeof(DealerAccusoRevealController));
            controllerGO.transform.SetParent(rootRt, false);
            var so = new SerializedObject(controllerGO.GetComponent<DealerAccusoRevealController>());
            so.FindProperty("panelRoot").objectReferenceValue = root;
            so.FindProperty("resultText").objectReferenceValue = resultText;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[DealerAccusoRevealBuilder] Banner reveal accuso dealer creato (spento di default). Collegato a DealerAccusoRevealController.");
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
