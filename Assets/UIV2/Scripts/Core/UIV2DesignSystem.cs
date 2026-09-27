using Project51.UIV2.Components;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    /// <summary>K2: shared, idempotent styling. Never changes layout, listeners or game data.</summary>
    public static class UIV2DesignSystem
    {
        private static UIV2Theme loadedTheme;
        public static UIV2Theme Theme
        {
            get
            {
                if (loadedTheme == null)
                {
                    var catalog = Resources.Load<UIV2DesignCatalog>("UIV2DesignCatalog");
                    if (catalog != null) loadedTheme = catalog.Theme;
                }
                return loadedTheme;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            loadedTheme = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Project51.Unity.MoveSelectionUI.TextCreated -= StyleCaptureText;
            Project51.Unity.MoveSelectionUI.TextCreated += StyleCaptureText;
        }

        private static void StyleCaptureText(TMP_Text text) => ApplyRuntimeText(text, RoleFor(text));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitialScene() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded || Theme == null) return;
            foreach (var root in scene.GetRootGameObjects()) ApplySceneRoot(root, Theme);
        }

        public static void ApplySceneRoot(GameObject root, UIV2Theme theme)
        {
            switch (root.name)
            {
                case "UIV2_Home": case "UIV2_Root": case "StartScreenV2":
                case "AppLoadingV2": case "NewsV2": case "OnlineFlowV2":
                case "GamePresentationV2": case "GameCanvas":
                    Apply(root, theme);
                    break;
                case "Canvas_Login":
                    foreach (string panel in new[] { "LoginPanel", "RegisterPanel", "AccountPanel" })
                    {
                        var design = root.transform.Find(panel + "/Design");
                        if (design != null) Apply(design.gameObject, theme);
                    }
                    break;
            }
        }

        public static void Apply(GameObject root, UIV2Theme theme)
        {
            if (root == null || theme == null) return;
            foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (Excluded(text.transform)) continue;
                // Testi della barra del tavolo: font/misure calibrati sul mockup da Apply Table Layout V4.
                if (text.transform.parent != null && text.transform.parent.name == "TableTopBar") continue;
                var material = text.fontSharedMaterial;
                bool calibrated = material != null && (material.name.Contains("Outline Ribbon") || material.name.EndsWith(" Button"));
                // Numeric HUD, room-code cells and tiny badges have their own geometry.
                bool special = calibrated || text.name == "Letter" || text.name == "NameLabel" ||
                    text.name == "Count" || text.name == "ValueLabel" || text.fontSize < 20 || text.fontSize > 48;
                theme.ApplyTypography(text, RoleFor(text), special);
            }
            foreach (var button in root.GetComponentsInChildren<Button>(true))
                if (!Excluded(button.transform)) ApplyButton(button, theme);
            foreach (var fill in root.GetComponentsInChildren<Image>(true))
            {
                if (Excluded(fill.transform) || fill.sprite == null || fill.GetComponentInParent<Selectable>(true) != null) continue;
                if (fill.sprite.name != "panel_fill_r30" && fill.sprite.name != "panel_fill_r24") continue;
                // Some modal prefabs put the fill on Panel, others on Frame/Fill.
                // An explicit structural owner is essential: Progress/Fill uses the same sprites.
                var owner = fill.name == "Panel" ? fill.transform : fill.transform.parent;
                if (owner == null || (fill.name != "Fill" && fill.name != "Background" && fill.name != "Panel")) continue;
                bool content = owner.name == "HeroPanel" || owner.name == "UIV2_ContentPanel";
                bool modal = owner.name == "Panel" || owner.name == "PanelFrame" || owner.name == "Frame";
                if (!content && !modal) continue;
                Image border = null, ribbon = null;
                foreach (var image in owner.GetComponentsInChildren<Image>(true))
                {
                    if (image.transform.parent != owner || image.sprite == null) continue;
                    if (image.sprite.name.StartsWith("panel_ring_")) border = image;
                    if (image.sprite.name == "ribbon_teal") ribbon = image;
                }
                ApplyPanel(theme, modal ? UIV2Theme.PanelKind.Modal : UIV2Theme.PanelKind.Content, fill, border, ribbon);
            }
        }

        private static bool Excluded(Transform transform)
        {
            for (var t = transform; t != null; t = t.parent)
                // "UI51": schermate ricostruite col design UI51, hanno font e colori propri.
                if (t.name == "LegacyOnlineViews" || t.name == "UI51") return true;
            return false;
        }

        public static UIV2Theme.TextRole RoleFor(TMP_Text text)
        {
            // Existing local sizes carry the visual hierarchy. Snap them to the shared scale;
            // a small row title must not become a full-page headline just because it says Title.
            float size = text.enableAutoSizing ? text.fontSizeMax : text.fontSize;
            if (size >= 36) return UIV2Theme.TextRole.Title;
            if (size >= 28) return UIV2Theme.TextRole.Subtitle;
            if (size >= 23) return UIV2Theme.TextRole.Body;
            return UIV2Theme.TextRole.Caption;
        }

        public static void ApplyButton(Button button, UIV2Theme theme)
        {
            if (button == null || theme == null) return;
            var background = button.targetGraphic as Image;
            if (background == null || background.sprite == null) return;
            string sprite = background.sprite.name;
            if (sprite == "btn_gray_small" && (button.name == "Cancel" || button.name == "Leave"))
            {
                background.sprite = null;
                background.color = Color.clear; // Graphic still receives raycasts.
                return;
            }
            if (sprite == "btn_teal" && (button.name == "RetryButton" || button.name == "Copy"))
            {
                background.sprite = theme.ButtonSecondarySprite;
                background.color = Color.white;
                sprite = "btn_blue_long";
            }
            bool gold = sprite.StartsWith("btn_gold_");
            bool blue = sprite.StartsWith("btn_blue_");
            if (!gold && !blue) return; // Stateful tabs, icon art, danger buttons and backdrops keep their semantics.
            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.GetComponentInParent<Button>(true) != button || label.GetComponentInParent<TMP_InputField>(true) != null) continue;
                if (label.name != "Label" && label.name != "Text") continue;
                if (gold) theme.ApplyGoldLabel(label);
            }
        }

        public static void ApplyPanel(UIV2Theme theme, UIV2Theme.PanelKind kind, Image fill, Image border, Image ribbon = null)
        {
            if (theme == null) return;
            bool modal = kind == UIV2Theme.PanelKind.Modal;
            if (fill != null)
            {
                fill.sprite = modal ? theme.PanelBackground : theme.ContentBackground;
                fill.type = Image.Type.Sliced;
                fill.color = theme.PanelBlue;
            }
            if (border != null)
            {
                border.sprite = modal ? theme.PanelBorder : theme.ContentBorder;
                border.type = Image.Type.Sliced;
                border.color = modal ? theme.BorderGold : theme.ButtonSecondaryBlue;
            }
            if (ribbon != null && modal)
            {
                ribbon.sprite = theme.RibbonSprite;
                ribbon.color = Color.white;
            }
        }

        public static void ApplyRuntimeText(TMP_Text text, UIV2Theme.TextRole role, bool preserveSize = false)
        {
            if (Theme != null) Theme.ApplyTypography(text, role, preserveSize);
        }
    }
}
