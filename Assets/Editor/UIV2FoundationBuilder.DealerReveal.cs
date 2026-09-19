using Project51.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Cartello dell'accuso del mazziere (Scopa da 15/30) in GameScene/GameCanvas/DealerAccusoReveal:
    /// cornice oro con bagliore morbido sopra al tavolo, titolo in Poppins ExtraBold e riga di dettaglio.
    /// Sostituisce il vecchio rettangolo scuro di Tools/51/Build Dealer Accuso Reveal. Rilanciabile.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private static readonly Color RevealFill = new Color32(20, 34, 54, 245);
        private static readonly Color RevealDetail = new Color32(186, 205, 228, 255);

        [MenuItem("Tools/UIV2/Build Dealer Accuso Reveal")]
        private static void BuildDealerAccusoReveal()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("GameCanvas");
            if (canvas == null) throw new System.Exception("GameCanvas non trovato in GameScene");

            var extraBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsExtraBoldPath);
            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsRegularPath);
            if (extraBold == null || regular == null) throw new System.Exception("Font Poppins non trovati");
            var navyOutline = GetOutlineMaterial("Outline Navy", OutlineNavy, 0.3f, 0.2f, extraBold);

            var old = canvas.transform.Find("DealerAccusoReveal");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var root = CreateUIObject("DealerAccusoReveal", canvas.transform);
            StretchFill(root);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            // Sopra la parte alta del feltro, come il pugno dell'accuso: non copre le carte in tavolo.
            var window = CreateUIObject("Window", root);
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
            window.sizeDelta = new Vector2(820f, 190f);
            window.anchoredPosition = new Vector2(0f, 300f);

            var glow = AddSoftRectGlow(window, LoadSprite(GlowSheetDir + "Bagliore morbido rettangolo.png", "Bagliore morbido rettangolo"), RouletteGold);
            glow.color = new Color(RouletteGold.r, RouletteGold.g, RouletteGold.b, 0f);

            AddRoundedPanel(window, "panel_fill_r24", 48f, 26f, RouletteGold, 4f, RevealFill, out var fill, out _);

            var title = CreateUIObject("Title", fill);
            title.anchorMin = title.anchorMax = title.pivot = new Vector2(0.5f, 0.5f);
            title.sizeDelta = new Vector2(760f, 76f);
            title.anchoredPosition = new Vector2(0f, 26f);
            var titleText = AddText(title, "SCOPA DA 30!", 50f, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
            UseFont(titleText, extraBold, navyOutline);

            var detail = CreateUIObject("Detail", fill);
            detail.anchorMin = detail.anchorMax = detail.pivot = new Vector2(0.5f, 0.5f);
            detail.sizeDelta = new Vector2(760f, 46f);
            detail.anchoredPosition = new Vector2(0f, -38f);
            var detailText = AddText(detail, "Prende le carte del tavolo", 26f, FontStyles.Normal, RevealDetail, TextAlignmentOptions.Center);
            UseFont(detailText, regular, null);

            var controller = Object.FindObjectOfType<DealerAccusoRevealController>(true);
            if (controller != null && controller.gameObject != root.gameObject)
            {
                // Il vecchio controller stava dentro il banner distrutto: se ne resta uno sciolto, via.
                if (controller.transform.parent == null || controller.transform.parent == canvas.transform)
                {
                    Object.DestroyImmediate(controller.gameObject);
                }
                controller = null;
            }
            if (controller == null) controller = root.gameObject.AddComponent<DealerAccusoRevealController>();

            SetPrivateField(controller, "panelRoot", root.gameObject);
            SetPrivateField(controller, "group", group);
            SetPrivateField(controller, "window", window);
            SetPrivateField(controller, "resultText", titleText);
            SetPrivateField(controller, "detailText", detailText);
            SetPrivateField(controller, "glow", glow);
            EditorUtility.SetDirty(controller);

            var turns = Object.FindObjectOfType<TurnController>(true);
            if (turns != null)
            {
                SetPrivateField(turns, "dealerAccusoRevealController", controller);
                EditorUtility.SetDirty(turns);
            }

            root.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Cartello accuso del mazziere ricostruito in GameScene.");
        }
    }
}
