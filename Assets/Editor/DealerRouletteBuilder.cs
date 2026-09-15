using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce il pannello "roulette" di selezione dealer (DealerRouletteController) dentro
    /// GameCanvas in GameScene.unity: backdrop semi-trasparente + 4 slot nome/icona in fila,
    /// con evidenziatore che TurnController.PlayDealerDeclareSequence attiva/disattiva in
    /// sequenza. Spento di default, mostrato solo durante l'animazione.
    /// </summary>
    public static class DealerRouletteBuilder
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";

        // Opaco (non 0.78 come prima): con lo sfondo trasparente si vedeva il tavolo/banner
        // dietro, compreso chi ha gia' il turno (rivelando il dealer prima che finisse la
        // roulette) - segnalato esplicitamente dall'utente.
        private static readonly Color BackdropColor = HexColor("#050A12", 1f);
        private static readonly Color SlotBackgroundColor = HexColor("#0E1C30");
        private static readonly Color SlotHighlightColor = HexColor("#E8B24A", 0.55f);
        private static readonly Color NameColor = HexColor("#F5EFE0");
        private static readonly Color IconColor = HexColor("#9AA6B8");
        private static readonly Color TitleColor = HexColor("#E8B24A");

        private static readonly Vector2 SlotSize = new Vector2(190f, 240f);
        private const float SlotGap = 24f;
        private const float TitleY = 210f; // sopra la fila di slot, relativo al CENTRO dello schermo

        [MenuItem("Tools/51/Build Dealer Roulette")]
        private static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvasGO = GameObject.Find("GameCanvas");
            if (canvasGO == null)
            {
                Debug.LogError("[DealerRouletteBuilder] 'GameCanvas' non trovato in GameScene.unity.");
                return;
            }

            var existing = canvasGO.transform.Find("DealerRoulette");
            if (existing != null)
            {
                Debug.LogWarning("[DealerRouletteBuilder] 'DealerRoulette' esiste gia': lo rimuovo e ricreo.");
                Object.DestroyImmediate(existing.gameObject);
            }

            var root = new GameObject("DealerRoulette", typeof(RectTransform));
            root.transform.SetParent(canvasGO.transform, false);
            var rootRt = (RectTransform)root.transform;
            StretchFill(rootRt);
            root.SetActive(false);

            var backdrop = CreateUIObject("Backdrop", rootRt);
            StretchFill(backdrop);
            backdrop.gameObject.AddComponent<Image>().color = BackdropColor;

            // Titolo: spiega cosa sta succedendo (segnalato: "non si capisce cosa sta succedendo").
            var titleRt = CreateUIObject("Title", rootRt);
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(700f, 60f);
            titleRt.anchoredPosition = new Vector2(0f, TitleY);
            AddText(titleRt, "Si sceglie il mazziere...", 28f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center);

            // Slot ancorati al CENTRO dello schermo (0.5, 0.5), non ad un punto fisso dall'angolo:
            // cosi' sono davvero centrati su qualunque dimensione di canvas, non solo su 1080x1920
            // esatto (segnalato: "non e' adattato allo schermo e non e' neanche centrato").
            float step = SlotSize.x + SlotGap;
            var nameTexts = new TMP_Text[4];
            var highlights = new GameObject[4];

            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * step;
                BuildSlot(rootRt, $"Slot_{i}", new Vector2(x, 0f), out var nameText, out var highlight);
                nameTexts[i] = nameText;
                highlights[i] = highlight;
            }

            var controllerGO = new GameObject("DealerRouletteController", typeof(DealerRouletteController));
            controllerGO.transform.SetParent(rootRt, false);
            var so = new SerializedObject(controllerGO.GetComponent<DealerRouletteController>());
            so.FindProperty("panelRoot").objectReferenceValue = root;

            var namesProp = so.FindProperty("slotNameTexts");
            namesProp.arraySize = 4;
            var highlightsProp = so.FindProperty("slotHighlights");
            highlightsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                namesProp.GetArrayElementAtIndex(i).objectReferenceValue = nameTexts[i];
                highlightsProp.GetArrayElementAtIndex(i).objectReferenceValue = highlights[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[DealerRouletteBuilder] Pannello roulette dealer creato (spento di default). Collegato a DealerRouletteController.");
        }

        private static void BuildSlot(RectTransform parent, string name, Vector2 center, out TMP_Text nameText, out GameObject highlight)
        {
            var slotRt = CreateUIObject(name, parent);
            slotRt.anchorMin = slotRt.anchorMax = new Vector2(0.5f, 0.5f);
            slotRt.pivot = new Vector2(0.5f, 0.5f);
            slotRt.sizeDelta = SlotSize;
            slotRt.anchoredPosition = center;

            var background = CreateUIObject("Background", slotRt);
            StretchFill(background);
            background.gameObject.AddComponent<Image>().color = SlotBackgroundColor;

            var iconSprite = FindSprite("ic_person");
            var iconRt = CreateUIObject("Icon", slotRt);
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.sizeDelta = new Vector2(84f, 84f);
            iconRt.anchoredPosition = new Vector2(0f, -28f);
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            if (iconSprite != null)
            {
                iconImg.sprite = iconSprite;
                iconImg.preserveAspect = true;
            }
            else
            {
                iconImg.color = IconColor;
            }

            var nameRt = CreateUIObject("NameText", slotRt);
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 0.5f);
            nameRt.offsetMin = new Vector2(8f, 16f);
            nameRt.offsetMax = new Vector2(-8f, -120f);
            nameText = AddText(nameRt, "Giocatore", 22f, FontStyles.Bold, NameColor, TextAlignmentOptions.Bottom);

            // Highlight: SOPRA tutto il resto (ultimo sibling), tinta piena semi-trasparente che
            // copre l'intero slot invece di un bordo sottile - segnalato "non si capisce cosa sta
            // succedendo" con il vecchio alone di 8px, troppo discreto per essere notato.
            var highlightRt = CreateUIObject("Highlight", slotRt);
            StretchFill(highlightRt);
            highlightRt.gameObject.AddComponent<Image>().color = SlotHighlightColor;
            highlightRt.gameObject.SetActive(false);
            highlight = highlightRt.gameObject;
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
