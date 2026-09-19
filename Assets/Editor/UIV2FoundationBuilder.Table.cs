using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Ripara la grafica del tavolo (GameScene/GameCanvas) rimasta senza sprite dopo il passaggio dai
    /// PNG singoli ai fogli (Icons.png...): pulsante impostazioni, Emoji, Accuso e banner giocatore
    /// tornano come nei mockup 09_tavolo_v4 / 11_tavolo_accuso_v2. Tocca solo sprite e colori, non
    /// posizioni ne' logica. Rilanciabile.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private static readonly Color TableBannerBorder = new Color32(58, 86, 128, 255);
        private static readonly Color TableBannerFill = new Color32(14, 28, 48, 255);

        // Mockup 09_tavolo_v4, pixel 1080x1920 dall'alto.
        private static readonly (string path, Vector2 center)[] TableLayoutV4 =
        {
            ("PlayerBanners/Banner_Top", new Vector2(470f, 250f)),
            ("PlayerBanners/Banner_Left", new Vector2(191f, 589f)),
            ("PlayerBanners/Banner_Right", new Vector2(891f, 589f)),
            ("PlayerBanners/Banner_Local", new Vector2(211f, 1361f)),
            ("TableActionButtons/EmojiButton", new Vector2(875f, 1361f)),
            ("TableActionButtons/AccusoButton", new Vector2(985f, 1361f)),
        };
        private const float TableCardsCenterY = 1008f;
        private const float TableFeltCenterY = 860f;
        private const float TableFeltWidth = 1030f;
        private const float TableFeltHeight = 810f;
        private const float ReferenceOrthoSize = 11.30667f; // CameraResponsiveFit a 1080x1920

        /// <summary>
        /// Porta il tavolo alle posizioni del mockup 09_tavolo_v4: banner, Emoji/Accuso, centro delle
        /// carte in tavolo e feltro. Mani avversarie e mazzetti prese seguono i banner a runtime
        /// (CardViewManager). Rilanciabile.
        /// </summary>
        [MenuItem("Tools/UIV2/Apply Table Layout V4")]
        private static void ApplyTableLayoutV4()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("GameCanvas");
            if (canvas == null) throw new System.Exception("GameCanvas non trovato in GameScene");

            foreach (var (path, center) in TableLayoutV4)
            {
                var rect = canvas.transform.Find(path) as RectTransform;
                if (rect == null) { Debug.LogWarning($"[UIV2FoundationBuilder] Layout tavolo: '{path}' non trovato."); continue; }
                Undo.RecordObject(rect, "Apply Table Layout V4");
                rect.anchoredPosition = new Vector2(center.x, -center.y);
            }

            var manager = Object.FindObjectOfType<Project51.Unity.UI.PlayerBannerManager>(true);
            if (manager != null) SetPrivateField(manager, "roundedFillSprite", LoadSprite(PanelsNeutralPath, "panel_fill_r24"));

            // Il mockup non ha una scritta di turno nella barra in alto (il turno si vede sul banner):
            // il vecchio TurnIndicator si sovrapponeva a "Mano X di Y".
            var turnIndicator = canvas.transform.Find("TurnIndicator");
            if (turnIndicator != null) turnIndicator.gameObject.SetActive(false);

            var extraBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsExtraBoldPath);
            foreach (Transform banner in canvas.transform.Find("PlayerBanners"))
            {
                // Chip MAZZIERE: pillola oro a cavallo del bordo basso, non piu' sopra avatar e nome.
                var chip = banner.Find("DealerLabel") as RectTransform;
                if (chip == null) continue;
                chip.anchorMin = chip.anchorMax = new Vector2(0.5f, 0f);
                chip.pivot = new Vector2(0.5f, 0.5f);
                chip.anchoredPosition = new Vector2(0f, -2f);
                chip.sizeDelta = new Vector2(118f, 30f);
                chip.SetAsLastSibling();
                var chipImage = chip.GetComponent<Image>();
                chipImage.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
                chipImage.type = Image.Type.Sliced;
                chipImage.pixelsPerUnitMultiplier = 48f / 15f;
                chipImage.color = new Color32(232, 178, 74, 255);
                var chipText = chip.GetComponentInChildren<TMP_Text>(true);
                if (chipText != null && extraBold != null)
                {
                    chipText.font = extraBold;
                    chipText.fontSharedMaterial = extraBold.material;
                    chipText.fontSize = 17f;
                    chipText.fontStyle = FontStyles.Normal;
                    chipText.color = new Color32(70, 40, 10, 255);
                }
            }

            // Pannello emoticon: icona di chiusura del mockup 15 al posto della "X" di testo.
            var social = Object.FindObjectOfType<Project51.UIV2.Core.GameSocialV2>(true);
            if (social != null && social.Close != null)
            {
                var label = social.Close.transform.Find("Label");
                if (label != null) label.gameObject.SetActive(false);
                var icon = social.Close.transform.Find("Icon") as RectTransform;
                if (icon == null)
                {
                    icon = CreateUIObject("Icon", social.Close.transform);
                    icon.gameObject.AddComponent<Image>().raycastTarget = false;
                }
                icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
                icon.anchoredPosition = Vector2.zero;
                icon.sizeDelta = new Vector2(38f, 38f);
                var iconImage = icon.GetComponent<Image>();
                iconImage.sprite = LoadSprite(Icons2Path, "ic_x_circle");
                iconImage.preserveAspect = true;
                EditorUtility.SetDirty(social.Close.gameObject);
            }

            // Mondo: 1920 px di mockup = 2 * ReferenceOrthoSize unita'.
            float worldPerPixel = ReferenceOrthoSize * 2f / 1920f;
            var tableCenter = GameObject.Find("TableCardsContainer");
            if (tableCenter != null)
            {
                Undo.RecordObject(tableCenter.transform, "Apply Table Layout V4");
                var p = tableCenter.transform.position;
                tableCenter.transform.position = new Vector3(p.x, (960f - TableCardsCenterY) * worldPerPixel, p.z);
            }

            var felt = Object.FindObjectOfType<Project51.Unity.TableFeltRenderer>(true);
            if (felt != null)
            {
                var so = new SerializedObject(felt);
                so.FindProperty("widthRatio").floatValue = TableFeltWidth / 1080f;
                so.FindProperty("heightRatio").floatValue = TableFeltHeight / 1920f;
                so.FindProperty("verticalOffsetRatio").floatValue = (TableCardsCenterY - TableFeltCenterY) / 1920f;
                so.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Layout tavolo V4 applicato in GameScene.");
        }

        /// <summary>Sprite del pannello "Scegli la presa" (MoveSelectionUI costruisce la grafica a runtime).</summary>
        [MenuItem("Tools/UIV2/Build Capture Choice")]
        private static void BuildCaptureChoice()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var ui = Object.FindObjectOfType<Project51.Unity.MoveSelectionUI>(true);
            if (ui == null) throw new System.Exception("MoveSelectionUI non trovato in GameScene");

            SetPrivateField(ui, "panelFill", LoadSprite(PanelsNeutralPath, "panel_fill_r30"));
            SetPrivateField(ui, "panelRing", LoadSprite(PanelsNeutralPath, "panel_ring_r30"));
            SetPrivateField(ui, "rowFill", LoadSprite(PanelsNeutralPath, "panel_fill_r24"));
            SetPrivateField(ui, "rowRing", LoadSprite(PanelsNeutralPath, "panel_ring_r24"));
            SetPrivateField(ui, "closeBackground", LoadSprite(IconsPath, "sq_blue"));
            SetPrivateField(ui, "closeIcon", LoadSprite(IconsPath, "ic_x"));
            EditorUtility.SetDirty(ui);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Pannello scelta presa aggiornato in GameScene.");
        }

        [MenuItem("Tools/UIV2/Repair Table HUD")]
        private static void RepairTableHud()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("GameCanvas");
            if (canvas == null) throw new System.Exception("GameCanvas non trovato in GameScene");
            var root = canvas.transform;

            SetTableSprite(root, "TableTopBar/SettingsButton", "sq_blue", sliced: true);
            SetTableSprite(root, "TableTopBar/SettingsButton/Icon", "ic_gear", sliced: false);
            SetTableSprite(root, "TableActionButtons/EmojiButton", "sq_blue", sliced: true);
            SetTableSprite(root, "TableActionButtons/EmojiButton/Icon", "ic_person", sliced: false);
            SetTableSprite(root, "TableActionButtons/AccusoButton", "sq_gold", sliced: true);
            SetTableSprite(root, "TableActionButtons/AccusoButton/Icon", "ic_warn", sliced: false);
            // Il bagliore morbido del mockup non esiste nel kit (glow_soft rimosso): niente quadrato giallo.
            var glow = root.Find("TableActionButtons/AccusoButton/Glow");
            if (glow != null) glow.gameObject.SetActive(false);
            AddTableButtonCaption(root, "TableActionButtons/EmojiButton", "Emoji", FontStyles.Normal, OnSoftText);
            AddTableButtonCaption(root, "TableActionButtons/AccusoButton", "ACCUSO", FontStyles.Bold, OnGold);

            var banners = root.Find("PlayerBanners");
            foreach (Transform banner in banners)
            {
                var avatar = banner.Find("AvatarFrame");
                if (avatar != null)
                {
                    var image = avatar.GetComponent<Image>();
                    image.sprite = LoadSprite(IconsPath, "frame_round");
                    image.color = Color.white;
                    image.preserveAspect = true;
                    EditorUtility.SetDirty(image);
                }

                var background = banner.Find("Background") as RectTransform;
                if (background != null)
                {
                    var existingFill = background.Find("Fill");
                    if (existingFill != null) Object.DestroyImmediate(existingFill.gameObject);
                    Object.DestroyImmediate(background.GetComponent<Image>());
                    AddRoundedPanel(background, "panel_fill_r24", 48f, 20f, TableBannerBorder, 3f, TableBannerFill, out _, out _);
                }

                var turnGlow = banner.Find("TurnGlow");
                if (turnGlow != null)
                {
                    var image = turnGlow.GetComponent<Image>();
                    image.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
                    image.type = Image.Type.Sliced;
                    image.pixelsPerUnitMultiplier = 48f / 22f;
                    EditorUtility.SetDirty(image);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Grafica del tavolo riparata in GameScene.");
        }

        /// <summary>Didascalia sotto il pulsante (mockup: "Emoji" chiara, "ACCUSO" oro).</summary>
        private static void AddTableButtonCaption(Transform root, string buttonPath, string caption, FontStyles style, Color color)
        {
            var button = root.Find(buttonPath) as RectTransform;
            if (button == null) return;
            var old = button.Find("Caption");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var rect = CreateUIObject("Caption", button);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -8f);
            rect.sizeDelta = new Vector2(160f, 34f);
            AddText(rect, caption, 22f, style, color, TMPro.TextAlignmentOptions.Center);
        }

        private static void SetTableSprite(Transform root, string path, string spriteName, bool sliced)
        {
            var target = root.Find(path);
            if (target == null)
            {
                Debug.LogWarning($"[UIV2FoundationBuilder] Tavolo: '{path}' non trovato, salto.");
                return;
            }
            var image = target.GetComponent<Image>();
            image.sprite = LoadSprite(IconsPath, spriteName);
            image.color = Color.white;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
            EditorUtility.SetDirty(image);
        }
    }
}
