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
            if (manager != null)
            {
                SetPrivateField(manager, "roundedFillSprite", LoadSprite(PanelsNeutralPath, "panel_fill_r24"));
                // Stessi ritratti della sandbox (giocatore, Marco, Luca, Giulia), uno per posto assoluto.
                SetPrivateField(manager, "seatAvatars", new[] {
                    LoadSprite(AvatarsPath, "avatar_08"), LoadSprite(AvatarsPath, "avatar_03"),
                    LoadSprite(AvatarsPath, "avatar_06"), LoadSprite(AvatarsPath, "avatar_01") });
            }

            // Il mockup non ha una scritta di turno nella barra in alto (il turno si vede sul banner):
            // il vecchio TurnIndicator si sovrapponeva a "Mano X di Y".
            var turnIndicator = canvas.transform.Find("TurnIndicator");
            if (turnIndicator != null) turnIndicator.gameObject.SetActive(false);

            var extraBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsExtraBoldPath);
            foreach (Transform banner in canvas.transform.Find("PlayerBanners"))
            {
                // Ritratto al posto del cerchio frame_round: box 66x76 cosi' l'anello oro del ritratto
                // (~81% dell'altezza sprite) resta ~62px come il cerchio del mockup; testa e cappello escono sopra.
                var avatarFrame = banner.Find("AvatarFrame") as RectTransform;
                var bannerView = banner.GetComponent<Project51.Unity.UI.PlayerBanner>();
                if (avatarFrame != null && bannerView != null)
                {
                    avatarFrame.sizeDelta = new Vector2(66f, 76f);
                    SetPrivateField(bannerView, "avatarImage", avatarFrame.GetComponent<Image>());
                    EditorUtility.SetDirty(bannerView);
                }

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

            // Mondo: 1920 px di mockup = DesignWorldHeight unita' (G1: area di design fissa, non l'ortho live).
            const float worldPerPixel = Project51.Unity.CameraResponsiveFit.DesignWorldHeight / Project51.Unity.CameraResponsiveFit.DesignHeightPx;
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

            // Barra in alto come nel mockup: "Mano" Bold 25pt crema, "Carte rimaste" 22pt azzurro.
            var bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsBoldPath);
            var handText = canvas.transform.Find("TableTopBar/HandText")?.GetComponent<TMP_Text>();
            if (handText != null && bold != null)
            {
                handText.font = bold;
                handText.fontSharedMaterial = bold.material;
                handText.fontSize = 25f;
                handText.fontStyle = FontStyles.Normal;
                handText.color = new Color32(255, 250, 238, 255);
                EditorUtility.SetDirty(handText);
            }
            var cardsLeftText = canvas.transform.Find("TableTopBar/CardsLeftText")?.GetComponent<TMP_Text>();
            if (cardsLeftText != null)
            {
                cardsLeftText.fontSize = 22f;
                cardsLeftText.color = new Color32(186, 205, 228, 255);
                EditorUtility.SetDirty(cardsLeftText);
            }

            // Sfondo: sfumatura verticale misurata sul mockup (piu' chiara in alto), cosi' la barra
            // scura in alto si stacca come nel mockup invece di sparire su un navy piatto.
            var background = GameObject.Find("GameBackground")?.GetComponent<SpriteRenderer>();
            if (background != null)
            {
                Undo.RecordObject(background, "Apply Table Layout V4");
                background.sprite = CreateNavyGradientSprite(new Color32(16, 34, 56, 255), new Color32(7, 16, 28, 255));
                EditorUtility.SetDirty(background);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Layout tavolo V4 applicato in GameScene.");
        }

        private const string NavyGradientAssetPath = "Assets/Art/Generated/GameBackground_NavyGradient.png";

        /// <summary>PNG quadrato (GameBackgroundFitter scala in Cover uniforme), rigenerato a ogni lancio.</summary>
        private static Sprite CreateNavyGradientSprite(Color32 top, Color32 bottom)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                var row = Color32.Lerp(bottom, top, y / (size - 1f)); // riga 0 = basso
                for (int x = 0; x < size; x++) pixels[y * size + x] = row;
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(NavyGradientAssetPath));
            System.IO.File.WriteAllBytes(NavyGradientAssetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(NavyGradientAssetPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(NavyGradientAssetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = size;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(NavyGradientAssetPath);
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
            SetPrivateField(ui, "closeIcon", NewIcon("ic_X"));
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
            SetTableSprite(root, "TableTopBar/SettingsButton/Icon", "ic_option", sliced: false);
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
            image.sprite = System.Array.IndexOf(IconSetV2, spriteName) >= 0 ? NewIcon(spriteName) : LoadSprite(IconsPath, spriteName);
            image.color = Color.white;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
            EditorUtility.SetDirty(image);
        }
    }
}
