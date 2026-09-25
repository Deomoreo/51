using Project51.UIV2.Components;
using Project51.UIV2.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Barra in basso della Home (MainMenu, UIV2_BottomNav):
    /// - sfondo fino al bordo dello schermo e contenuto piu' in basso sui telefoni con la barra di
    ///   sistema (BottomNavSafeAreaBleed, dal vero Screen.safeArea);
    /// - linguetta oro 238x117 -> 262x128 (+10%, 9-slice: i bordi non si stirano) e icone 62 -> 72
    ///   (costante in UIV2BottomNav, preserveAspect); etichette spostate sotto la linguetta piu' alta.
    ///   2026-09-24: linguetta 262x128 -> 254x119, riempimento oro misurato 226x82 come il mockup
    ///   (a 262x128 era 234x91); icone invariate.
    ///   2.20: linguetta 119 -> 132 verso il basso (l'icona sta nel corpo oro, sopra la tacca); ogni
    ///   icona misurata e centrata sulla sua parte visibile (i margini trasparenti degli sprite sono
    ///   diversi: il gamepad stava basso e piccolo); scritte ExtraBold con contorno e ombra attaccata
    ///   come GIOCA; contenuto piu' in basso (sink 0.75) per chiudere il vuoto sotto le scritte.
    ///   2.23: sink 0.5 (barra un po' piu' alta) e la pagina arriva al filo della barra.
    /// Rilanciabile.
    /// </summary>
    public static class BottomNavPolishBuilder
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Poppins-ExtraBold SDF.asset";
        private const string NormalMaterialPath = "Assets/UIV2/Art/Fonts/Poppins-ExtraBold SDF Nav Label.mat";
        private const string SelectedMaterialPath = "Assets/UIV2/Art/Fonts/Poppins-ExtraBold SDF Nav Label Selected.mat";

        private static readonly Vector2 IndicatorSize = new Vector2(254f, 132f);
        // Centro del corpo oro: dal bordo alto della linguetta (+14) alla tacca (26 px dal fondo).
        private const float IconCenterY = -47f;
        private const float IconSide = 62f;                                   // radice di larghezza x altezza visibili
        private static readonly Vector2 IconMax = new Vector2(76f, 66f);
        private const float LabelCenterY = -138f;
        private const float LabelSize = 25f;
        private const float Sink = 0.5f;

        private static readonly Color NormalFace = new Color32(190, 206, 230, 255);
        private static readonly Color SelectedFace = new Color32(255, 252, 242, 255);  // come GIOCA
        private static readonly Color NormalOutline = new Color32(7, 12, 24, 255);
        private static readonly Color SelectedOutline = new Color32(148, 84, 8, 255);  // come GIOCA

        [MenuItem("Tools/UIV2/Build Bottom Nav Polish")]
        private static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            Transform host = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.transform.Find("SafeArea/BottomNavHost");
                if (found != null) host = found;
            }
            var nav = host != null ? host.Find("UIV2_BottomNav") as RectTransform : null;
            if (nav == null)
            {
                Debug.LogError("[BottomNav] UIV2_Home/SafeArea/BottomNavHost/UIV2_BottomNav non trovato.");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var normalMaterial = LabelMaterial(font, NormalMaterialPath, NormalOutline);
            var selectedMaterial = LabelMaterial(font, SelectedMaterialPath, SelectedOutline);
            RestoreShop(nav);

            foreach (Transform slot in nav)
            {
                if (!slot.name.EndsWith("Slot")) continue;
                bool selected = false;
                if (slot.Find("SelectedIndicator") is RectTransform indicator)
                {
                    indicator.sizeDelta = IndicatorSize;
                    selected = indicator.gameObject.activeSelf;
                }
                var icon = slot.Find("Icon");
                if (icon != null) FitIcon(icon.GetComponent<Image>());
                if (slot.Find("Label") is RectTransform labelRect)
                {
                    labelRect.anchoredPosition = new Vector2(labelRect.anchoredPosition.x, LabelCenterY);
                    labelRect.sizeDelta = new Vector2(labelRect.sizeDelta.x, 40f);
                    var label = labelRect.GetComponent<TextMeshProUGUI>();
                    label.font = font;
                    label.fontStyle = FontStyles.Normal;
                    label.fontSize = LabelSize;
                    label.fontSharedMaterial = selected ? selectedMaterial : normalMaterial;
                    label.color = selected ? SelectedFace : NormalFace;
                }
                foreach (var c in slot.GetComponentsInChildren<Component>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            }

            var navSo = new SerializedObject(nav.GetComponent<UIV2BottomNav>());
            navSo.FindProperty("normalLabelColor").colorValue = NormalFace;
            navSo.FindProperty("selectedLabelColor").colorValue = SelectedFace;
            navSo.FindProperty("normalLabelMaterial").objectReferenceValue = normalMaterial;
            navSo.FindProperty("selectedLabelMaterial").objectReferenceValue = selectedMaterial;
            navSo.ApplyModifiedPropertiesWithoutUndo();

            // Riempimento sotto la barra, stesso colore dello sfondo; ignorato dal layout orizzontale.
            var old = nav.Find("SafeAreaFill");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var fillGo = new GameObject("SafeAreaFill", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            var fill = (RectTransform)fillGo.transform;
            fill.SetParent(nav, false);
            fill.SetAsFirstSibling();
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(1f, 0f);
            fill.pivot = new Vector2(0.5f, 1f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = new Vector2(0f, 0f);
            fillGo.GetComponent<LayoutElement>().ignoreLayout = true;
            var fillImage = fillGo.GetComponent<Image>();
            fillImage.color = nav.GetComponent<Image>().color;
            fillImage.raycastTarget = false;

            var bleed = host.GetComponent<BottomNavSafeAreaBleed>();
            if (bleed == null) bleed = host.gameObject.AddComponent<BottomNavSafeAreaBleed>();
            var so = new SerializedObject(bleed);
            so.FindProperty("content").objectReferenceValue = nav;
            so.FindProperty("fill").objectReferenceValue = fill;
            so.FindProperty("sink").floatValue = Sink;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BottomNav] Barra in basso: linguetta, icone centrate, scritte con contorno e ombra.");
        }

        // 2.23: il Negozio torna (la 2.22 l'aveva tolto). Tab al terzo posto e pagina ShopPageV2 allo
        // stesso indice del pager (Gioca 0, Collezione 1, Negozio 2, Profilo 3). Idempotente.
        private static void RestoreShop(RectTransform nav)
        {
            var shopSlot = nav.Find("ShopSlot");
            if (shopSlot == null) return;
            shopSlot.gameObject.SetActive(true);
            var navSo = new SerializedObject(nav.GetComponent<UIV2BottomNav>());
            var slots = navSo.FindProperty("slots");
            bool present = false;
            for (int i = 0; i < slots.arraySize; i++)
                if (slots.GetArrayElementAtIndex(i).FindPropertyRelative("Button").objectReferenceValue is Button button && button.transform == shopSlot)
                    present = true;
            if (!present)
            {
                slots.InsertArrayElementAtIndex(2);
                var slot = slots.GetArrayElementAtIndex(2);
                var icon = shopSlot.Find("Icon");
                slot.FindPropertyRelative("Button").objectReferenceValue = shopSlot.GetComponent<Button>();
                slot.FindPropertyRelative("Icon").objectReferenceValue = icon.GetComponent<Image>();
                slot.FindPropertyRelative("IconLayoutElement").objectReferenceValue = icon.GetComponent<LayoutElement>();
                slot.FindPropertyRelative("Label").objectReferenceValue = shopSlot.Find("Label").GetComponent<TMP_Text>();
                slot.FindPropertyRelative("SelectedIndicator").objectReferenceValue = shopSlot.Find("SelectedIndicator").gameObject;
            }
            navSo.ApplyModifiedPropertiesWithoutUndo();

            var pager = nav.GetComponentInParent<UIV2Pager>(true);
            RectTransform shopPage = null;
            foreach (var t in pager.GetComponentsInChildren<RectTransform>(true)) if (t.name == "ShopPageV2") shopPage = t;
            if (shopPage == null) return;
            shopPage.gameObject.SetActive(true);
            var pagerSo = new SerializedObject(pager);
            var pages = pagerSo.FindProperty("pages");
            for (int i = 0; i < pages.arraySize; i++) if (pages.GetArrayElementAtIndex(i).objectReferenceValue == shopPage) return;
            pages.InsertArrayElementAtIndex(2);
            pages.GetArrayElementAtIndex(2).objectReferenceValue = shopPage;
            pagerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // La parte visibile (alfa) di ogni icona prende la stessa misura e sta al centro del corpo oro;
        // il pivot sul centro visibile fa crescere l'icona selezionata (DOScale) sul posto.
        private static void FitIcon(Image icon)
        {
            var sprite = icon.sprite;
            var texture = new Texture2D(2, 2);
            texture.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
            float k = texture.width / (float)sprite.texture.width;
            int x0 = Mathf.RoundToInt(sprite.rect.x * k), y0 = Mathf.RoundToInt(sprite.rect.y * k);
            int w = Mathf.RoundToInt(sprite.rect.width * k), h = Mathf.RoundToInt(sprite.rect.height * k), tw = texture.width;
            var pixels = texture.GetPixels32();
            Object.DestroyImmediate(texture);
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (pixels[(y0 + y) * tw + x0 + x].a > 60) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
            float vw = maxX - minX + 1, vh = maxY - minY + 1;
            float s = Mathf.Min(IconSide / Mathf.Sqrt(vw * vh), Mathf.Min(IconMax.x / vw, IconMax.y / vh));
            var rect = icon.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(w, h) * s;
            rect.pivot = new Vector2((minX + maxX + 1) * 0.5f / w, (minY + maxY + 1) * 0.5f / h);
            rect.anchoredPosition = new Vector2(0f, IconCenterY);
            icon.preserveAspect = true;
            Debug.Log("[BottomNav] " + sprite.name + ": visibile " + Mathf.RoundToInt(vw * s) + "x" + Mathf.RoundToInt(vh * s));
        }

        // Contorno e dilatazione come GIOCA (0,6 + 0,3: il massimo per il padding dell'atlas) e ombra
        // attaccata sotto la scritta (underlay spostato in basso).
        private static Material LabelMaterial(TMP_FontAsset font, string path, Color outline)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(font.material);
                AssetDatabase.CreateAsset(material, path);
            }
            material.CopyPropertiesFromMaterial(font.material);
            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            material.SetColor(ShaderUtilities.ID_OutlineColor, outline);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.6f);
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.3f);
            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.7f));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.9f);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.4f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.1f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}
