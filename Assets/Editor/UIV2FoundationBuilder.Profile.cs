using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Components;
using Project51.UIV2.Screens;

namespace Project51.EditorTools
{
    /// <summary>
    /// PROFILO V2 (Tools/UIV2/Build Profile Screen V2) da 17_profilo.png. Coordinate mockup 1080x1920;
    /// nel mockup la corona dell'avatar grande sale sopra lo ScreenHost (y=262), qui tutto il contenuto
    /// e' traslato di +60px e resta comunque sopra la bottom nav. Riusa gli helper Collezione/Negozio.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        [MenuItem("Tools/UIV2/Build Profile Screen V2")]
        private static void BuildProfileScreenV2Entry()
        {
            var goldButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ComponentsPrefabDir}/UIV2_PrimaryGoldButton.prefab");
            var blueButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ComponentsPrefabDir}/UIV2_SecondaryBlueButton.prefab");
            var progressBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ComponentsPrefabDir}/UIV2_ProgressBar.prefab");
            if (goldButtonPrefab == null || blueButtonPrefab == null || progressBarPrefab == null)
            {
                Debug.LogError("[UIV2FoundationBuilder] Prefab foundation mancanti - esegui prima 'Tools/UIV2/Build Foundation'.");
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var trophyTilePrefab = BuildProfileTrophyTilePrefab();
            BuildProfileScreenV2Prefab(trophyTilePrefab, goldButtonPrefab, blueButtonPrefab, progressBarPrefab);
            AssetDatabase.SaveAssets();

            Debug.Log("[UIV2FoundationBuilder] ProfileScreenV2 costruita in " +
                      $"{ScreensPrefabDir}/ProfileScreenV2.prefab. Scena scratch NON salvata.");
        }

        /// <summary>Casella trofeo 183x183 (5 per riga, passo 199).</summary>
        private static ProfileTrophyView BuildProfileTrophyTilePrefab()
        {
            var go = new GameObject("ProfileTrophyTile", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(183f, 183f);

            var border = AddRoundedPanel(rect, "panel_fill_r24", 48f, 18f, CollectionGold, 4f, ShopCardFill,
                out var fillRect, out var fill);
            border.raycastTarget = true;
            var button = go.AddComponent<Button>();
            button.targetGraphic = border;
            button.transition = Selectable.Transition.None;

            // Box 98x98 a aspect nativo: le icone del kit hanno padding diversi -> ~80-90 visibili.
            var iconRect = CreateUIObject("Icon", rect);
            Place(iconRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -1f), new Vector2(98f, 98f));
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = LoadSprite(IconsPath, "ic_trophy");
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var comp = go.AddComponent<ProfileTrophyView>();
            SetPrivateField(comp, "border", border);
            SetPrivateField(comp, "fillRect", fillRect);
            SetPrivateField(comp, "fill", fill);
            SetPrivateField(comp, "icon", icon);
            SetPrivateField(comp, "button", button);

            return SaveAsPrefab(go, $"{ScreensPrefabDir}/ProfileTrophyTile.prefab").GetComponent<ProfileTrophyView>();
        }

        private static void BuildProfileScreenV2Prefab(ProfileTrophyView trophyTilePrefab, GameObject goldButtonPrefab,
            GameObject blueButtonPrefab, GameObject progressBarPrefab)
        {
            var go = new GameObject("ProfileScreenV2", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            StretchFill(rect);

            var content = CreateCollectionScrollContent(rect, 4);

            // ---- Testata profilo: 462.5 di altezza, figli posizionati assoluti (si sovrappongono come
            // nel mockup: bottone impostazioni sulla cornice, nome sul cartiglio) ----
            var headerRect = CreateUIObject("ProfileHeader", content);
            AddLayoutElement(headerRect, preferredHeight: 462.5f);

            // avatar_frame intero (alpha 162x156 -> 295 visibili di larghezza sul cartiglio).
            var frameRect = CreateUIObject("AvatarFrame", headerRect);
            Place(frameRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -153f), new Vector2(304f, 306f));
            var frame = frameRect.gameObject.AddComponent<Image>();
            frame.sprite = LoadSprite(IconsPath, "avatar_frame");
            frame.preserveAspect = true;
            frame.raycastTarget = false;

            // Ritratto reale (avatar_01..08 hanno gia' la propria cornice): sostituisce avatar_frame,
            // mai sovrapposto ad esso.
            var portraitRect = CreateUIObject("Portrait", headerRect);
            Place(portraitRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -162.5f), new Vector2(250f, 250f));
            var portrait = portraitRect.gameObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portraitRect.gameObject.SetActive(false);

            // Bottone impostazioni: sq_blue (alpha 120x113) -> 74x68 visibili + ic_gear.
            var settingsRect = CreateUIObject("SettingsButton", headerRect);
            Place(settingsRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(148f, -212.5f), new Vector2(79f, 73f));
            var settingsImage = AddSlicedImage(settingsRect, LoadSprite(IconsPath, "sq_blue"), 73f);
            var settingsButton = settingsRect.gameObject.AddComponent<Button>();
            settingsButton.targetGraphic = settingsImage;
            var gearRect = CreateUIObject("GearIcon", settingsRect);
            Place(gearRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-1f, 1f), new Vector2(40f, 42f));
            var gear = gearRect.gameObject.AddComponent<Image>();
            gear.sprite = LoadSprite(IconsPath, "ic_gear");
            gear.preserveAspect = true;
            gear.raycastTarget = false;

            var nameRect = CreateUIObject("NameLabel", headerRect);
            Place(nameRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -292.5f), new Vector2(760f, 64f));
            var nameLabel = AddText(nameRect, "Giocatore", 46f, FontStyles.Bold, HomeTextLight, TextAlignmentOptions.Center);
            nameLabel.enableWordWrapping = false;
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMin = 28f;
            nameLabel.fontSizeMax = 46f;
            ApplyOutline(nameLabel, GetOutlineMaterial("Outline Brown", OutlineBrown, 0.36f, 0.3f));

            var infoRect = CreateUIObject("InfoLabel", headerRect);
            Place(infoRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -341.5f), new Vector2(860f, 34f));
            var infoLabel = AddText(infoRect, "-", 24f, FontStyles.Normal, CollectionSubtitle, TextAlignmentOptions.Center);
            infoLabel.enableWordWrapping = false;

            // Barra XP: UIV2_ProgressBar riusato, parte visibile 600x43 (bar_empty alpha 58/63 righe).
            const float xpRectHeight = 46.7f;
            var xpInstance = (GameObject)PrefabUtility.InstantiatePrefab(progressBarPrefab, headerRect);
            xpInstance.name = "XpBar";
            var xpRect = (RectTransform)xpInstance.transform;
            Place(xpRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -399.5f), new Vector2(606f, xpRectHeight));
            PrefabUtility.RecordPrefabInstancePropertyModifications(xpRect);
            var xpTrack = xpInstance.GetComponent<Image>();
            if (xpTrack != null)
            {
                xpTrack.pixelsPerUnitMultiplier = 63f / xpRectHeight;
                xpTrack.color = new Color(0.7f, 0.7f, 0.75f, 1f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(xpTrack);
            }
            var xpFillArea = xpInstance.transform.Find("FillArea") as RectTransform;
            if (xpFillArea != null)
            {
                float inset = 2.3f * xpRectHeight / 37f;
                xpFillArea.offsetMin = new Vector2(inset, inset);
                xpFillArea.offsetMax = new Vector2(-inset, -inset);
                PrefabUtility.RecordPrefabInstancePropertyModifications(xpFillArea);
                var xpFillRect = xpFillArea.Find("Fill") as RectTransform;
                if (xpFillRect != null)
                {
                    var xpFill = xpFillRect.GetComponent<Image>();
                    xpFill.color = CollectionGold;
                    xpFill.pixelsPerUnitMultiplier = 60f / ((xpRectHeight - inset * 2f) * 0.5f);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(xpFill);

                    var stripeRect = CreateUIObject("Highlight", xpFillRect);
                    stripeRect.anchorMin = new Vector2(0f, 0.53f);
                    stripeRect.anchorMax = new Vector2(1f, 0.82f);
                    stripeRect.offsetMin = new Vector2(8f, 0f);
                    stripeRect.offsetMax = new Vector2(-8f, 0f);
                    var stripe = stripeRect.gameObject.AddComponent<Image>();
                    stripe.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
                    stripe.type = Image.Type.Sliced;
                    stripe.pixelsPerUnitMultiplier = 10f;
                    stripe.color = CollectionProgressHighlight;
                    stripe.raycastTarget = false;
                }
            }
            var xpValueLabel = xpInstance.transform.Find("ValueLabel");
            if (xpValueLabel != null)
            {
                xpValueLabel.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(xpValueLabel.gameObject);
            }

            var xpLabelRect = CreateUIObject("XpLabel", headerRect);
            Place(xpLabelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -449.5f), new Vector2(800f, 32f));
            var xpLabel = AddText(xpLabelRect, "-", 22f, FontStyles.Normal, CollectionSubtitle, TextAlignmentOptions.Center);
            xpLabel.enableWordWrapping = false;

            // ---- STATISTICHE: 3x2, card 315x129 (x 50..364 / 382..696 / 714..1028) ----
            AddVerticalSpacer(content, 30f);
            BuildCollectionSectionHeader(content, "StatsHeader", "STATISTICHE", false, out _);
            AddVerticalSpacer(content, 18f);

            var statsRect = CreateUIObject("StatsGrid", content);
            var statsGrid = statsRect.gameObject.AddComponent<GridLayoutGroup>();
            statsGrid.padding = new RectOffset(50, 50, 0, 0);
            statsGrid.cellSize = new Vector2(315f, 129f);
            statsGrid.spacing = new Vector2(17f, 21f);
            statsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statsGrid.constraintCount = 3;
            statsGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            statsGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            statsGrid.childAlignment = TextAnchor.UpperLeft;

            var matchesTile = BuildProfileStatTile(statsRect, "MatchesTile", "Partite");
            var winsTile = BuildProfileStatTile(statsRect, "WinsTile", "Vittorie");
            var winRateTile = BuildProfileStatTile(statsRect, "WinRateTile", "% vittorie");
            var scopasTile = BuildProfileStatTile(statsRect, "ScopasTile", "Scope totali");
            var settebelloTile = BuildProfileStatTile(statsRect, "SettebelloTile", "Settebello");
            var recordTile = BuildProfileStatTile(statsRect, "PointRecordTile", "Record punti");

            // ---- TROFEI: caselle 183x183, passo 199 ----
            AddVerticalSpacer(content, 31f);
            BuildCollectionSectionHeader(content, "TrophiesHeader", "TROFEI", false, out _);
            AddVerticalSpacer(content, 18f);

            var trophiesRect = CreateUIObject("TrophiesRow", content);
            AddLayoutElement(trophiesRect, preferredHeight: 183f);
            var trophiesLayout = trophiesRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            trophiesLayout.padding = new RectOffset(50, 0, 0, 0);
            trophiesLayout.spacing = 16f;
            trophiesLayout.childAlignment = TextAnchor.UpperLeft;
            trophiesLayout.childControlWidth = false;
            trophiesLayout.childControlHeight = false;
            trophiesLayout.childForceExpandWidth = false;
            trophiesLayout.childForceExpandHeight = false;

            // ---- Azioni: parte visibile gold 778x97 (btn_gold_long, alpha asimmetrico) e blu 558x78 ----
            AddVerticalSpacer(content, 110f);
            var registerSlot = CreateUIObject("RegisterSlot", content);
            AddLayoutElement(registerSlot, preferredHeight: 123.8f);
            var registerInstance = (GameObject)PrefabUtility.InstantiatePrefab(goldButtonPrefab, registerSlot);
            var registerButton = SetupProfileActionButton(registerInstance, new Vector2(-11.6f, 0f), new Vector2(810.4f, 123.8f),
                111f, "REGISTRATI PER SALVARE", 32f, new Vector2(10.6f, -5.6f), true);

            AddVerticalSpacer(content, 27.2f);
            var shareSlot = CreateUIObject("ShareSlot", content);
            AddLayoutElement(shareSlot, preferredHeight: 88.4f);
            var shareInstance = (GameObject)PrefabUtility.InstantiatePrefab(blueButtonPrefab, shareSlot);
            var shareButton = SetupProfileActionButton(shareInstance, Vector2.zero, new Vector2(561.5f, 88.4f),
                102f, "CONDIVIDI PROFILO", 26f, new Vector2(0f, -1.7f), false);

            var screen = go.AddComponent<ProfileScreenV2>();
            SetPrivateField(screen, "defaultAvatarFrame", frame);
            SetPrivateField(screen, "portrait", portrait);
            SetPrivateField(screen, "nameLabel", nameLabel);
            SetPrivateField(screen, "infoLabel", infoLabel);
            SetPrivateField(screen, "xpBar", xpInstance.GetComponent<UIV2ProgressBar>());
            SetPrivateField(screen, "xpLabel", xpLabel);
            SetPrivateField(screen, "settingsButton", settingsButton);
            SetPrivateField(screen, "matchesTile", matchesTile);
            SetPrivateField(screen, "winsTile", winsTile);
            SetPrivateField(screen, "winRateTile", winRateTile);
            SetPrivateField(screen, "scopasTile", scopasTile);
            SetPrivateField(screen, "settebelloTile", settebelloTile);
            SetPrivateField(screen, "pointRecordTile", recordTile);
            SetPrivateField(screen, "trophyContainer", trophiesRect);
            SetPrivateField(screen, "trophyPrefab", trophyTilePrefab);
            SetPrivateField(screen, "registerButton", registerButton);
            SetPrivateField(screen, "shareButton", shareButton);

            SaveAsPrefab(go, $"{ScreensPrefabDir}/ProfileScreenV2.prefab");
        }

        /// <summary>Card statistica 315x129 con UIV2StatTile (componente foundation riusato, senza icona).</summary>
        private static UIV2StatTile BuildProfileStatTile(RectTransform parent, string objectName, string caption)
        {
            var tileRect = CreateUIObject(objectName, parent);
            AddRoundedPanel(tileRect, "panel_fill_r24", 48f, 20f, CollectionCardBorder, 3f, ShopCardFill, out _, out _);

            var valueRect = CreateUIObject("ValueLabel", tileRect);
            Place(valueRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -48f), new Vector2(290f, 44f));
            var value = AddText(valueRect, "0", 32f, FontStyles.Bold, CollectionHeaderGold, TextAlignmentOptions.Center);
            value.enableWordWrapping = false;

            var captionRect = CreateUIObject("CaptionLabel", tileRect);
            Place(captionRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -95f), new Vector2(290f, 30f));
            var captionLabel = AddText(captionRect, caption, 20f, FontStyles.Normal, CollectionSubtitle, TextAlignmentOptions.Center);
            captionLabel.enableWordWrapping = false;

            var tile = tileRect.gameObject.AddComponent<UIV2StatTile>();
            SetPrivateField(tile, "valueLabel", value);
            SetPrivateField(tile, "captionLabel", captionLabel);
            return tile;
        }

        /// <summary>
        /// Istanza di UIV2_PrimaryGoldButton / UIV2_SecondaryBlueButton: rect compensato sull'alpha
        /// trasparente dello sprite, 9-slice a scala uniforme, label per istanza (il prefab condiviso
        /// resta invariato).
        /// </summary>
        private static UIV2Button SetupProfileActionButton(GameObject instance, Vector2 position, Vector2 size,
            float spriteNativeHeight, string text, float fontSize, Vector2 labelOffset, bool goldStyle)
        {
            var buttonRect = (RectTransform)instance.transform;
            Place(buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            PrefabUtility.RecordPrefabInstancePropertyModifications(buttonRect);

            var uiv2Button = instance.GetComponent<UIV2Button>();
            uiv2Button.SetLabel(text);
            SetPrivateField(uiv2Button, "keepLabelColor", true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(uiv2Button);

            var background = instance.GetComponent<Image>();
            if (background != null)
            {
                background.pixelsPerUnitMultiplier = spriteNativeHeight / size.y;
                PrefabUtility.RecordPrefabInstancePropertyModifications(background);
            }

            var label = instance.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.fontSize = fontSize;
                label.fontStyle = FontStyles.Bold;
                label.enableWordWrapping = false;
                label.rectTransform.offsetMin = labelOffset;
                label.rectTransform.offsetMax = labelOffset;
                if (goldStyle)
                {
                    label.color = Color.white;
                    label.enableVertexGradient = true;
                    var top = new Color32(255, 253, 245, 255);
                    var bottom = new Color32(255, 234, 186, 255);
                    label.colorGradient = new VertexGradient(top, top, bottom, bottom);
                    ApplyOutline(label, GetOutlineMaterial("Outline Brown", OutlineBrown, 0.36f, 0.3f));
                }
                else
                {
                    label.color = HomeTextLight;
                    ApplyOutline(label, NavyOutlineMaterial());
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(label);
                PrefabUtility.RecordPrefabInstancePropertyModifications(label.rectTransform);
            }

            return uiv2Button;
        }
    }
}
