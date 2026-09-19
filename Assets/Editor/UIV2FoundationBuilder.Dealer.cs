using Project51.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Roulette del mazziere dai mockup 27_selezione_mazziere_ciclo / 28_selezione_mazziere_risultato
    /// (GameScene/GameCanvas/DealerRoulette). Sostituisce il vecchio pannello a 4 slot in fila.
    /// Rilanciabile: distrugge e ricostruisce il pannello.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const string PoppinsBoldPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Poppins-Bold SDF.asset";
        private const string PoppinsExtraBoldPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Poppins-ExtraBold SDF.asset";

        private static readonly Color RouletteBoxFill = new Color32(15, 26, 44, 255);
        private static readonly Color RouletteBoxRing = new Color32(53, 78, 115, 255);
        private static readonly Color RouletteGold = new Color32(232, 178, 74, 255);
        private static readonly Color RouletteChipText = new Color32(70, 40, 10, 255);
        private static readonly Color RouletteStatus = new Color32(200, 210, 225, 255);

        // Riquadri 280x140 per posto (0=basso, 1=sinistra, 2=alto, 3=destra), angolo in alto a sinistra.
        private static readonly Vector2[] RouletteBoxTopLeft =
        {
            new Vector2(400f, 1250f),
            new Vector2(110f, 830f),
            new Vector2(400f, 410f),
            new Vector2(690f, 830f),
        };

        [MenuItem("Tools/UIV2/Build Dealer Roulette")]
        private static void BuildDealerRoulette()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("GameCanvas");
            if (canvas == null) throw new System.Exception("GameCanvas non trovato in GameScene");

            var bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsBoldPath);
            var extraBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsExtraBoldPath);
            if (bold == null || extraBold == null) throw new System.Exception("Font Poppins Bold/ExtraBold non trovati");
            var navyOutline = GetOutlineMaterial("Outline Navy", OutlineNavy, 0.3f, 0.2f, extraBold);
            var brownOutline = GetOutlineMaterial("Outline Brown", OutlineBrown, 0.3f, 0.2f, extraBold);

            var old = canvas.transform.Find("DealerRoulette");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var root = CreateUIObject("DealerRoulette", canvas.transform);
            StretchFill(root);
            // Sopra banner e carte (GameCanvas), sotto il pannello scelta presa (550) e la presentazione V2 (600).
            var overlay = root.gameObject.AddComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = 540;
            root.gameObject.AddComponent<GraphicRaycaster>();

            // Mockup 27-28: tavolo sfocato sotto al velo "Sfocatura sfondo".
            var backdropBlur = AddBlurBackdrop(root);

            var design = Stretch(root, "Design");

            MockSprite(design, "Ribbon", LoadSprite(IconsPath, "ribbon_teal"), 220f, 205f, 640f, 230f, false);
            var title = MockText(design, "Title", "SI SCEGLIE IL MAZZIERE", 200f, 285f, 680f, 70f, 38f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            UseFont(title, extraBold, navyOutline);

            var controller = root.gameObject.AddComponent<DealerRouletteController>();
            var slotRoots = new GameObject[4];
            var names = new TMP_Text[4];
            var rings = new Image[4];
            var trophies = new GameObject[4];
            var chips = new GameObject[4];
            var glows = new Image[4];
            var glowSprite = LoadSprite(GlowSheetDir + "Bagliore morbido rettangolo.png", "Bagliore morbido rettangolo");

            for (int i = 0; i < 4; i++)
            {
                var topLeft = RouletteBoxTopLeft[i];
                var box = MockRect(design, "Seat" + i, topLeft.x, topLeft.y, 280f, 140f);
                slotRoots[i] = box.gameObject;

                glows[i] = AddSoftRectGlow(box, glowSprite, RouletteGold);
                glows[i].gameObject.SetActive(false);

                var fill = Stretch(box, "Fill").gameObject.AddComponent<Image>();
                SetRounded(fill, LoadSprite(PanelsNeutralPath, "panel_fill_r24"), RouletteBoxFill, 24f);
                rings[i] = Stretch(box, "Ring").gameObject.AddComponent<Image>();
                SetRounded(rings[i], LoadSprite(PanelsNeutralPath, "panel_ring_r24"), RouletteBoxRing, 24f);

                MockSprite(box, "Avatar", LoadSprite(IconsPath, "frame_round"), 107f, 30f, 66f, 66f, false);
                names[i] = MockText(box, "Name", "Giocatore", 10f, 88f, 260f, 44f, 26f, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
                UseFont(names[i], bold, null);

                var trophy = MockSprite(box, "Trophy", LoadSprite(IconsPath, "ic_trophy"), 115f, -48f, 50f, 50f, false);
                trophies[i] = trophy.gameObject;
                trophies[i].SetActive(false);

                var chip = MockRect(box, "DealerChip", 47f, 154f, 186f, 46f);
                var chipFill = chip.gameObject.AddComponent<Image>();
                SetRounded(chipFill, LoadSprite(PanelsNeutralPath, "panel_fill_r24"), RouletteGold, 23f);
                var chipText = AddText(Stretch(chip, "Label"), "MAZZIERE", 24f, FontStyles.Normal, RouletteChipText, TextAlignmentOptions.Center);
                UseFont(chipText, extraBold, null);
                chips[i] = chip.gameObject;
                chips[i].SetActive(false);
            }

            var status = MockText(design, "Status", "Selezione in corso...", 90f, 1550f, 900f, 60f, 30f, FontStyles.Normal, RouletteStatus, TextAlignmentOptions.Center);
            UseFont(status, bold, null);
            status.fontStyle = FontStyles.Normal;

            var continueButton = MockButton(design, "Continue", "btn_gold_long", 300f, 1625f, 480f, 100f, "CONTINUA", 42f, brownOutline);
            foreach (var label in continueButton.GetComponentsInChildren<TMP_Text>(true)) UseFont(label, extraBold, brownOutline);
            var continueGlow = AddSoftRectGlow(continueButton.transform, glowSprite, RouletteGold);
            continueGlow.color = new Color(RouletteGold.r, RouletteGold.g, RouletteGold.b, 0.55f);
            continueButton.gameObject.SetActive(false);

            SetPrivateField(controller, "panelRoot", root.gameObject);
            SetPrivateField(controller, "slotRoots", slotRoots);
            SetPrivateField(controller, "slotNameTexts", names);
            SetPrivateField(controller, "slotRings", rings);
            SetPrivateField(controller, "slotTrophies", trophies);
            SetPrivateField(controller, "slotDealerChips", chips);
            SetPrivateField(controller, "slotGlows", glows);
            SetPrivateField(controller, "backdropBlur", backdropBlur);
            SetPrivateField(controller, "statusText", status);
            SetPrivateField(controller, "continueButton", continueButton);
            EditorUtility.SetDirty(controller);

            // Il TurnController tiene un riferimento al vecchio controller: lo ricollega a quello nuovo.
            var turns = Object.FindObjectOfType<TurnController>(true);
            if (turns != null) SetPrivateField(turns, "dealerRouletteController", controller);

            root.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Roulette mazziere (mockup 27-28) costruita in GameScene.");
        }

        /// <summary>
        /// Bagliore morbido rettangolo dietro a un riquadro (mockup 28). I bordi 9-slice dello sprite
        /// (336/528/361/517) ridotti di 6 volte; il centro pieno rientra di 12 px nel riquadro, cosi' fuori
        /// resta solo la sfumatura (circa 30 px) con gli angoli arrotondati.
        /// </summary>
        private static Image AddSoftRectGlow(Transform box, Sprite sprite, Color color)
        {
            const float shrink = 6f;
            const float inset = 12f;
            var rect = CreateUIObject("Glow", box);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            var border = sprite.border; // x = sinistra, y = basso, z = destra, w = alto
            rect.offsetMin = new Vector2(-(border.x / shrink - inset), -(border.y / shrink - inset));
            rect.offsetMax = new Vector2(border.z / shrink - inset, border.w / shrink - inset);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = shrink;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void SetRounded(Image image, Sprite sprite, Color color, float radius)
        {
            image.sprite = sprite;
            image.color = color;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 48f / radius;
            image.raycastTarget = false;
        }

        private static void UseFont(TMP_Text text, TMP_FontAsset font, Material material)
        {
            text.font = font;
            text.fontSharedMaterial = material != null ? material : font.material;
        }
    }
}
