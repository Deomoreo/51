using System;
using System.Text;
using Project51.UIV2.Animations;
using Project51.UIV2.Components;
using Project51.UIV2.Core;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>K2 source of truth. Prefabs only: never saves ExecuteAlways scene layout.</summary>
    public static class UIV2DesignBuilder
    {
        private const string ThemePath = "Assets/UIV2/Art/UIV2Theme.asset";
        private const string FontDir = "Assets/TextMesh Pro/Resources/Fonts & Materials/";
        private const string ResourceDir = "Assets/UIV2/Resources";
        private const string ComponentsDir = "Assets/UIV2/Prefabs/Components/";

        public static UIV2Theme PrepareTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UIV2Theme>(ThemePath);
            if (theme == null) throw new InvalidOperationException("UIV2Theme is missing; build the UI foundation first.");
            theme.TitleFont = Required<TMP_FontAsset>(FontDir + "Poppins-ExtraBold SDF.asset");
            theme.SubtitleFont = Required<TMP_FontAsset>(FontDir + "Poppins-Bold SDF.asset");
            theme.BodyFont = Required<TMP_FontAsset>(FontDir + "poppins-medium SDF.asset");
            theme.GoldLabelMaterial = Required<Material>("Assets/UIV2/Art/Fonts/Poppins-ExtraBold SDF Outline Gold Button.mat");
            theme.ContentBackground = FindSprite("panel_fill_r24");
            theme.ContentBorder = FindSprite("panel_ring_r24");
            theme.IconButtonBackground = FindSprite("sq_blue");
            EditorUtility.SetDirty(theme);
            if (!AssetDatabase.IsValidFolder(ResourceDir)) AssetDatabase.CreateFolder("Assets/UIV2", "Resources");
            string catalogPath = ResourceDir + "/UIV2DesignCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<UIV2DesignCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<UIV2DesignCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }
            catalog.Theme = theme;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return theme;
        }

        private static T Required<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Missing design asset: " + path);
            return asset;
        }

        private static Sprite FindSprite(string name)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/UI/Sprites/DragonsHoard" }))
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                    if (asset is Sprite sprite && sprite.name == name) return sprite;
            throw new InvalidOperationException("Missing design sprite: " + name);
        }

        [MenuItem("Tools/UIV2/Apply Design System")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying the design system.");
            var theme = PrepareTheme();
            CreateButtonVariant(theme, "UIV2_FlatButton", UIV2Button.Style.Flat);
            CreateButtonVariant(theme, "UIV2_IconButton", UIV2Button.Style.Icon);
            CreateContentPanel(theme);
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/UIV2" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    string before = Snapshot(root);
                    UIV2DesignSystem.Apply(root, theme);
                    if (before == Snapshot(root)) continue;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[UIV2 Design] Updated {changed} prefabs. Scene styling runs on load; scene files are unchanged.");
        }

        private static string Snapshot(GameObject root)
        {
            var result = new StringBuilder();
            foreach (var component in root.GetComponentsInChildren<Component>(true))
                if (component != null) result.Append(EditorJsonUtility.ToJson(component));
            return result.ToString();
        }

        private static void CreateButtonVariant(UIV2Theme theme, string name, UIV2Button.Style style)
        {
            string path = ComponentsDir + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                root.SetActive(false);
                bool icon = style == UIV2Button.Style.Icon;
                ((RectTransform)root.transform).sizeDelta = icon ? new Vector2(88, 88) : new Vector2(280, 64);
                var button = root.GetComponent<Button>();
                button.targetGraphic = root.GetComponent<Image>();
                var styled = root.AddComponent<UIV2Button>();
                var serialized = new SerializedObject(styled);
                serialized.FindProperty("theme").objectReferenceValue = theme;
                serialized.FindProperty("style").enumValueIndex = (int)style;
                serialized.FindProperty("button").objectReferenceValue = button;
                serialized.FindProperty("background").objectReferenceValue = button.targetGraphic;
                serialized.FindProperty("visualRoot").objectReferenceValue = root.transform;
                if (icon)
                {
                    var child = Child(root.transform, "Icon", new Vector2(36, 36));
                    var image = child.AddComponent<Image>();
                    image.sprite = FindSprite("ic_x");
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                }
                else
                {
                    var child = Child(root.transform, "Label", new Vector2(260, 56));
                    var text = child.AddComponent<TextMeshProUGUI>();
                    text.text = "INDIETRO";
                    text.alignment = TextAlignmentOptions.Center;
                    text.raycastTarget = false;
                    theme.ApplyTypography(text, UIV2Theme.TextRole.Body);
                    serialized.FindProperty("label").objectReferenceValue = text;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                root.AddComponent<UIV2ButtonFeedback>();
                styled.Apply();
                root.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void CreateContentPanel(UIV2Theme theme)
        {
            string path = ComponentsDir + "UIV2_ContentPanel.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var root = new GameObject("UIV2_ContentPanel", typeof(RectTransform));
            try
            {
                ((RectTransform)root.transform).sizeDelta = new Vector2(640, 240);
                var fill = Child(root.transform, "Fill", new Vector2(640, 240)).AddComponent<Image>();
                var border = Child(root.transform, "Border", new Vector2(640, 240)).AddComponent<Image>();
                foreach (var graphic in new[] { fill, border })
                {
                    graphic.rectTransform.anchorMin = Vector2.zero;
                    graphic.rectTransform.anchorMax = Vector2.one;
                    graphic.rectTransform.sizeDelta = Vector2.zero;
                    graphic.raycastTarget = false;
                }
                UIV2DesignSystem.ApplyPanel(theme, UIV2Theme.PanelKind.Content, fill, border);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static GameObject Child(Transform parent, string name, Vector2 size)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            ((RectTransform)child.transform).sizeDelta = size;
            return child;
        }
    }
}
