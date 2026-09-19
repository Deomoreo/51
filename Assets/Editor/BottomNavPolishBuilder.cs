using Project51.UIV2.Components;
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
    /// Rilanciabile.
    /// </summary>
    public static class BottomNavPolishBuilder
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private static readonly Vector2 IndicatorSize = new Vector2(262f, 128f);
        private const float IconCenterY = -60f;
        private const float LabelCenterY = -134f;

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

            foreach (Transform slot in nav)
            {
                if (!slot.name.EndsWith("Slot")) continue;
                if (slot.Find("SelectedIndicator") is RectTransform indicator) indicator.sizeDelta = IndicatorSize;
                if (slot.Find("Icon") is RectTransform icon) icon.anchoredPosition = new Vector2(icon.anchoredPosition.x, IconCenterY);
                if (slot.Find("Label") is RectTransform label) label.anchoredPosition = new Vector2(label.anchoredPosition.x, LabelCenterY);
                foreach (var rt in slot.GetComponentsInChildren<RectTransform>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(rt);
            }

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
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BottomNav] Barra in basso: fino al bordo, linguetta e icone piu' grandi.");
        }
    }
}
