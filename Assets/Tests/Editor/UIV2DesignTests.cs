#if UNITY_EDITOR
using NUnit.Framework;
using Project51.UIV2.Components;
using Project51.UIV2.Core;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Tests
{
    public class UIV2DesignTests
    {
        private static UIV2Theme Theme => AssetDatabase.LoadAssetAtPath<UIV2Theme>("Assets/UIV2/Art/UIV2Theme.asset");

        [TestCase("CollectionScreenV2")]
        [TestCase("ProfileScreenV2")]
        public void SavedScreenProgressRemainsContrastedAfterStyling(string screen)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UIV2/Prefabs/Screens/" + screen + ".prefab");
            var instance = Object.Instantiate(prefab);
            try
            {
                UIV2DesignSystem.Apply(instance, Theme);
                var bars = instance.GetComponentsInChildren<UIV2ProgressBar>(true);
                Assert.IsNotEmpty(bars);
                foreach (var bar in bars)
                {
                    var field = new SerializedObject(bar).FindProperty("fillRect");
                    var fill = ((RectTransform)field.objectReferenceValue).GetComponent<Image>();
                    Assert.AreNotEqual(Theme.PanelBlue, fill.color, "Visible progress must stand out from panel: " + bar.name);
                }
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void SceneStylingLeavesProgressFillVisibleAndRecognizesModalChildren()
        {
            var root = new GameObject("Fixture", typeof(RectTransform));
            try
            {
                var progress = new GameObject("Progress", typeof(RectTransform));
                progress.transform.SetParent(root.transform, false);
                var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillObject.transform.SetParent(progress.transform, false);
                var fill = fillObject.GetComponent<Image>();
                fill.sprite = Theme.ContentBackground;
                fill.color = Color.green;
                var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(root.transform, false);
                panel.GetComponent<Image>().sprite = Theme.PanelBackground;
                var ribbonObject = new GameObject("Ribbon", typeof(RectTransform), typeof(Image));
                ribbonObject.transform.SetParent(panel.transform, false);
                ribbonObject.GetComponent<Image>().sprite = Theme.RibbonSprite;
                UIV2DesignSystem.Apply(root, Theme);
                Assert.AreEqual(Color.green, fill.color, "Progress is state, not a content panel.");
                Assert.AreSame(Theme.PanelBackground, panel.GetComponent<Image>().sprite, "A modal owns its ribbon as a child.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void StylingSkipsEverythingUnderUI51Nodes()
        {
            var root = new GameObject("Fixture", typeof(RectTransform));
            try
            {
                var header = new GameObject("UI51CollectionHeader", typeof(RectTransform));
                header.transform.SetParent(root.transform, false);
                var own = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                own.transform.SetParent(header.transform, false);
                var themed = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                themed.transform.SetParent(root.transform, false);
                own.fontSize = themed.fontSize = 22;
                var font = own.font;
                UIV2DesignSystem.Apply(root, Theme);
                Assert.AreEqual(22f, own.fontSize, "UI51 headers keep their own size.");
                Assert.AreSame(font, own.font, "UI51 headers keep their own font.");
                Assert.AreEqual(Theme.CaptionSize, themed.fontSize, "Control: the same text outside UI51 is themed.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ApplyingDesignTwicePreservesContentInputStateAndGeometry()
        {
            var root = new GameObject("Fixture", typeof(RectTransform));
            try
            {
                var titleObject = new GameObject("Heading", typeof(RectTransform), typeof(TextMeshProUGUI));
                titleObject.transform.SetParent(root.transform, false);
                var title = titleObject.GetComponent<TMP_Text>();
                title.text = "Una notizia";
                title.fontSize = 38;
                title.color = Color.red; // Runtime status color is not a decoration.
                title.rectTransform.anchoredPosition = new Vector2(13, 17);
                title.enableAutoSizing = true;
                title.fontSizeMax = 38;
                title.fontSizeMin = 19;
                var inputObject = new GameObject("Input", typeof(RectTransform), typeof(TMP_InputField));
                inputObject.transform.SetParent(root.transform, false);
                var fieldObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                fieldObject.transform.SetParent(inputObject.transform, false);
                var fieldText = fieldObject.GetComponent<TextMeshProUGUI>();
                fieldText.fontSize = 24;
                var input = inputObject.GetComponent<TMP_InputField>();
                input.textComponent = fieldText;
                input.text = "user@example.test";
                input.interactable = false;
                UIV2DesignSystem.Apply(root, Theme);
                string first = EditorJsonUtility.ToJson(title);
                UIV2DesignSystem.Apply(root, Theme);
                Assert.AreEqual(first, EditorJsonUtility.ToJson(title), "Repeated styling must not drift.");
                Assert.AreEqual(40, title.fontSizeMax);
                Assert.AreEqual(20, title.fontSizeMin);
                Assert.AreEqual(Color.red, title.color);
                Assert.AreEqual("Una notizia", title.text);
                Assert.AreEqual(new Vector2(13, 17), title.rectTransform.anchoredPosition);
                Assert.AreEqual("user@example.test", input.text);
                Assert.IsFalse(input.interactable);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void GoldDecorationDoesNotReachNestedButtonsOrWorldSpaceText()
        {
            var root = new GameObject("Gold", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var background = root.GetComponent<Image>();
                background.sprite = Theme.ButtonPrimarySprite;
                root.GetComponent<Button>().targetGraphic = background;
                var nested = new GameObject("Nested", typeof(RectTransform), typeof(Button));
                nested.transform.SetParent(root.transform, false);
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(nested.transform, false);
                var label = labelObject.GetComponent<TMP_Text>();
                label.color = Color.red;
                var world = new GameObject("CardNumber", typeof(TextMeshPro));
                world.transform.SetParent(root.transform, false);
                world.GetComponent<TMP_Text>().fontSize = 6;
                UIV2DesignSystem.Apply(root, Theme);
                Assert.AreEqual(Color.red, label.color);
                Assert.AreEqual(6, world.GetComponent<TMP_Text>().fontSize);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void FlatButtonKeepsItsHitTargetAndClickActionAcrossReopen()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UIV2/Prefabs/Components/UIV2_FlatButton.prefab");
            Assert.IsNotNull(prefab);
            var instance = Object.Instantiate(prefab);
            try
            {
                var button = instance.GetComponent<Button>();
                int clicked = 0;
                button.onClick.AddListener(() => clicked++);
                instance.SetActive(false);
                instance.SetActive(true);
                instance.GetComponent<UIV2Button>().Apply();
                button.onClick.Invoke();
                Assert.AreEqual(1, clicked);
                Assert.IsTrue(button.targetGraphic.raycastTarget);
                Assert.AreEqual(0, button.targetGraphic.color.a);
                Assert.IsNull(((Image)button.targetGraphic).sprite);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void ModalAndContentPanelsKeepAuthoredBoundsAndRaycastPolicy()
        {
            var root = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            try
            {
                var fill = root.GetComponent<Image>();
                fill.rectTransform.sizeDelta = new Vector2(700, 500);
                fill.raycastTarget = false;
                UIV2DesignSystem.ApplyPanel(Theme, UIV2Theme.PanelKind.Modal, fill, null);
                Assert.AreSame(Theme.PanelBackground, fill.sprite);
                UIV2DesignSystem.ApplyPanel(Theme, UIV2Theme.PanelKind.Content, fill, null);
                Assert.AreSame(Theme.ContentBackground, fill.sprite);
                Assert.AreEqual(new Vector2(700, 500), fill.rectTransform.sizeDelta);
                Assert.IsFalse(fill.raycastTarget);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void GoldButtonReapplyRestoresReadableSharedStyleWithoutChangingLayout()
        {
            var root = new GameObject("Gold", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                root.SetActive(false);
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(root.transform, false);
                var label = labelObject.GetComponent<TMP_Text>();
                label.text = "CONTINUA";
                label.fontSize = 31;
                label.rectTransform.sizeDelta = new Vector2(220, 60);
                var component = root.AddComponent<UIV2Button>();
                var serialized = new SerializedObject(component);
                serialized.FindProperty("theme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UIV2Theme>("Assets/UIV2/Art/UIV2Theme.asset");
                serialized.FindProperty("label").objectReferenceValue = label;
                serialized.FindProperty("background").objectReferenceValue = root.GetComponent<Image>();
                serialized.FindProperty("button").objectReferenceValue = root.GetComponent<Button>();
                serialized.ApplyModifiedPropertiesWithoutUndo();

                component.Apply();
                label.color = Color.blue;
                label.enableVertexGradient = true;
                component.Apply();

                Assert.AreEqual((Color)new Color32(255, 252, 242, 255), label.color);
                Assert.AreEqual("Poppins-ExtraBold SDF", label.font.name);
                Assert.AreSame(AssetDatabase.LoadAssetAtPath<Material>("Assets/UIV2/Art/Fonts/Poppins-ExtraBold SDF Outline Gold Button.mat"), label.fontSharedMaterial);
                Assert.IsFalse(label.enableVertexGradient);
                Assert.AreEqual("CONTINUA", label.text);
                Assert.AreEqual(31, label.fontSize);
                Assert.AreEqual(new Vector2(220, 60), label.rectTransform.sizeDelta);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
#endif
