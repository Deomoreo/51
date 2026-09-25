using Project51.Core;
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
    /// 2.20 - pannello Mazzo e pulsanti MODALITA'/MAZZO della Home, scritte semplificate.
    /// Il catalogo mostra ovunque il dorso vero del mazzo (Classico usava una carta di denari),
    /// l'anteprima ha un bordo arrotondato invece delle quattro strisce, e le scritte sono
    /// ExtraBold con contorno come i titoli delle righe di Modalita'. Rilanciabile.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const string DeckPolishFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Poppins-ExtraBold SDF.asset";
        private const string DeckPolishThickPath = "Assets/UIV2/Art/Fonts/Poppins-ExtraBold SDF Outline Navy Thick.mat";
        private const string DeckPolishThinPath = "Assets/UIV2/Art/Fonts/Poppins-ExtraBold SDF Outline Navy Thin.mat";
        private static readonly Color DeckPreviewRing = new Color32(70, 110, 142, 255);

        [MenuItem("Tools/UIV2/Polish Quick Deck Panel")]
        private static void PolishQuickDeckPanel()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            var catalog = CardDecks.Catalog;
            foreach (var entry in catalog.Entries)
            {
                var deck = Resources.Load<CardDeckDefinition>(entry.ResourcePath);
                if (deck != null && deck.Back != null) entry.Artwork = deck.Back;
                entry.Subtitle = "40 carte";
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DeckPolishFontPath);
            var thick = AssetDatabase.LoadAssetAtPath<Material>(DeckPolishThickPath);
            var thin = AssetDatabase.LoadAssetAtPath<Material>(DeckPolishThinPath);
            var frame = FindInScene(scene, "QuickDeckV2/PanelFrame");

            var box = frame.Find("PreviewBox");
            foreach (var side in new[] { "BorderTop", "BorderBottom", "BorderLeft", "BorderRight" })
            {
                var stripe = box.Find(side);
                if (stripe == null) continue;
                var image = stripe.GetComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
                image.canvasRenderer.cullTransparentMesh = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            }
            var fill = box.GetComponent<Image>();
            SetRounded(fill, LoadSprite(PanelsNeutralPath, "panel_fill_r24"), fill.color, 24f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(fill);
            var oldRing = box.Find("Ring");
            if (oldRing != null) Object.DestroyImmediate(oldRing.gameObject);
            var ring = Stretch(box, "Ring");
            ring.SetSiblingIndex(0);
            SetRounded(ring.gameObject.AddComponent<Image>(), LoadSprite(PanelsNeutralPath, "panel_ring_r24"), DeckPreviewRing, 24f);

            DeckText(box.Find("Label"), font, thin);
            DeckText(box.Find("Caption"), font, thick);
            // Misura "subtitle" (32): maiuscole crema ~19 px come "Napoletano · 40 carte" del mockup (a 24 erano 15).
            var caption = box.Find("Caption").GetComponent<TMP_Text>();
            caption.enableAutoSizing = false;
            caption.enableWordWrapping = false;
            caption.fontSize = 32f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(caption);
            var content = frame.Find("ScrollView/Viewport/Content");
            // 2.21 - mazzi aggiunti al catalogo dopo la prima build: la cella si clona dalla prima,
            // griglia 3 colonne (passo 310x286 come le celle originali, 3x290+2x10 = 910 = viewport).
            var panels = content.GetComponentInParent<QuickSelectionPanels>(true);
            var buttons = new Button[catalog.Entries.Count];
            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                var entry = catalog.Entries[i];
                var cell = content.Find("Deck_" + entry.Id);
                if (cell == null)
                {
                    cell = Object.Instantiate(content.Find("Deck_" + catalog.Entries[0].Id).gameObject, content, false).transform;
                    cell.name = "Deck_" + entry.Id;
                    cell.GetComponent<SelectableToggleItem>().SetSelected(false);
                }
                ((RectTransform)cell).anchoredPosition = new Vector2(i % 3 * 310, -44 - i / 3 * 286);
                cell.Find("NameText").GetComponent<TMP_Text>().text = entry.DisplayName;
                buttons[i] = cell.GetComponent<Button>();
                var art = cell.Find("CardArt").GetComponent<Image>();
                art.sprite = entry.Artwork;
                PrefabUtility.RecordPrefabInstancePropertyModifications(art);
                DeckText(cell.Find("NameText"), font, thick);
            }
            var contentRect = (RectTransform)content;
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, 44 + (catalog.Entries.Count + 2) / 3 * 286);
            panels.DeckButtons = buttons;
            EditorUtility.SetDirty(panels);
            PrefabUtility.RecordPrefabInstancePropertyModifications(panels);

            foreach (var chip in new[] { "SelectorsRow/UIV2_ModeSelector", "SelectorsRow/UIV2_DeckSelector" })
            {
                var selector = FindInScene(scene, chip);
                DeckText(selector.Find("TextColumn/SmallLabel"), font, thin);
                DeckText(selector.Find("TextColumn/ValueLabel"), font, thick);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[QuickDeck] dorsi veri nel catalogo, anteprima arrotondata, scritte ExtraBold con contorno.");
        }

        // Materiale con contorno: UIV2DesignSystem allora non rimette il font del tema.
        private static void DeckText(Transform target, TMP_FontAsset font, Material material)
        {
            if (target == null) { Debug.LogError("[QuickDeck] scritta mancante"); return; }
            var text = target.GetComponent<TMP_Text>();
            text.font = font;
            text.fontSharedMaterial = material;
            text.fontStyle = FontStyles.Normal;
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }
    }
}
