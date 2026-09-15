using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Data;
using Project51.UIV2.Screens;

namespace Project51.EditorTools
{
    /// <summary>
    /// NEGOZIO V2 (Tools/UIV2/Build Shop Screen V2) da 16_negozio.png. Coordinate mockup 1080x1920;
    /// nel mockup l'offerta parte a y=190 sopra il cartiglio dell'avatar, qui parte sotto lo ScreenHost
    /// (y=270): tutto il contenuto e' traslato di +80px e sta comunque sopra la bottom nav.
    /// Riusa gli helper della Collezione (header di sezione, pannelli arrotondati, scroll content).
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private static readonly Color ShopOfferFill = new Color32(47, 35, 72, 255);
        private static readonly Color ShopCardFill = new Color32(18, 32, 52, 255);
        private static readonly Color ShopBadgeRed = new Color32(214, 58, 58, 255);

        [MenuItem("Tools/UIV2/Build Shop Screen V2")]
        private static void BuildShopScreenV2Entry()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Bottoni: parte visibile ~260x40/44/47 (btn_green_small 75 righe visibili su 91, btn_teal e
            // btn_blue_long 90 su 102) -> rect piu' alti, 9-slice a scala uniforme via AddSlicedImage.
            var coinCard = BuildShopProductCardPrefab("ShopCoinCard", 301f, LoadSprite(IconsPath, "ic_coin_clover"),
                -97f, new Vector2(120f, 100f), false, 0f, -192f, 36f, CollectionNameCream,
                LoadSprite(IconsPath, "btn_green_small"), new Vector2(262.7f, 48.5f), -254.4f);
            var gemCard = BuildShopProductCardPrefab("ShopGemCard", 281f, LoadSprite(IconsPath, "ic_gem_green"),
                -84f, new Vector2(80f, 92f), false, 0f, -176f, 36f, CollectionNameCream,
                LoadSprite(IconsPath, "btn_teal"), new Vector2(260.5f, 49.9f), -235f);
            var deckCard = BuildShopProductCardPrefab("ShopDeckCard", 321f, LoadSprite(IconsPath, "card_frame_dark"),
                -108f, new Vector2(110f, 152f), true, -105f, -214f, 26f, HomeTextLight,
                LoadSprite(IconsPath, "btn_blue_long"), new Vector2(271f, 53.3f), -272f);

            BuildShopScreenV2Prefab(coinCard, gemCard, deckCard);
            AssetDatabase.SaveAssets();

            Debug.Log("[UIV2FoundationBuilder] ShopScreenV2 costruita in " +
                      $"{ScreensPrefabDir}/ShopScreenV2.prefab. Scena scratch NON salvata.");
        }

        /// <summary>Card 313 x height (3 per riga, x 50..363 / 383..696 / 716..1029).</summary>
        private static ShopProductCardView BuildShopProductCardPrefab(string prefabName, float height, Sprite defaultArt,
            float artCenterY, Vector2 artBox, bool withLock, float lockCenterY, float labelCenterY, float labelSize,
            Color labelColor, Sprite buttonSprite, Vector2 buttonSize, float buttonCenterY)
        {
            var go = new GameObject(prefabName, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(313f, height);

            var border = AddRoundedPanel(rect, "panel_fill_r24", 48f, 22f, CollectionCardBorder, 3f, ShopCardFill,
                out var fillRect, out _);

            var artRect = CreateUIObject("Artwork", rect);
            Place(artRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, artCenterY), artBox);
            var art = artRect.gameObject.AddComponent<Image>();
            art.sprite = defaultArt;
            art.preserveAspect = true;
            art.raycastTarget = false;

            GameObject lockGo = null;
            if (withLock)
            {
                var lockRect = CreateUIObject("LockIcon", rect);
                Place(lockRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, lockCenterY), new Vector2(36f, 44f));
                var lockImage = lockRect.gameObject.AddComponent<Image>();
                lockImage.sprite = LoadSprite(IconsPath, "ic_lock");
                lockImage.preserveAspect = true;
                lockImage.raycastTarget = false;
                lockGo = lockRect.gameObject;
            }

            var labelRect = CreateUIObject("Label", rect);
            Place(labelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, labelCenterY), new Vector2(290f, labelSize + 14f));
            var label = AddText(labelRect, "-", labelSize, FontStyles.Bold, labelColor, TextAlignmentOptions.Center);
            label.enableWordWrapping = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = labelSize;

            var buttonRect = CreateUIObject("PriceButton", rect);
            Place(buttonRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, buttonCenterY), buttonSize);
            var buttonImage = AddSlicedImage(buttonRect, buttonSprite, buttonSize.y);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var priceLabelRect = CreateUIObject("Label", buttonRect);
            StretchFill(priceLabelRect);
            var priceLabel = AddText(priceLabelRect, "-", 24f, FontStyles.Bold, HomeTextLight, TextAlignmentOptions.Center);
            priceLabel.enableWordWrapping = false;
            ApplyOutline(priceLabel, NavyOutlineMaterial());

            // Badge "MIGLIORE": pillola oro centrata sul bordo superiore della card.
            var badgeRect = CreateUIObject("Badge", rect);
            Place(badgeRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(256f, 36f));
            var badgeImage = badgeRect.gameObject.AddComponent<Image>();
            badgeImage.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
            badgeImage.type = Image.Type.Sliced;
            badgeImage.pixelsPerUnitMultiplier = 48f / 16f;
            badgeImage.color = CollectionGold;
            badgeImage.raycastTarget = false;
            var badgeLabelRect = CreateUIObject("Label", badgeRect);
            StretchFill(badgeLabelRect);
            var badgeLabel = AddText(badgeLabelRect, "-", 18f, FontStyles.Bold, CollectionPillText, TextAlignmentOptions.Center);
            badgeLabel.enableWordWrapping = false;
            badgeRect.gameObject.SetActive(false);

            var comp = go.AddComponent<ShopProductCardView>();
            SetPrivateField(comp, "border", border);
            SetPrivateField(comp, "fillRect", fillRect);
            SetPrivateField(comp, "artwork", art);
            SetPrivateField(comp, "lockIcon", lockGo);
            SetPrivateField(comp, "label", label);
            SetPrivateField(comp, "badgeRoot", badgeRect.gameObject);
            SetPrivateField(comp, "badgeLabel", badgeLabel);
            SetPrivateField(comp, "priceButton", button);
            SetPrivateField(comp, "priceLabel", priceLabel);
            SetPrivateField(comp, "normalBorderColor", CollectionCardBorder);
            SetPrivateField(comp, "highlightedBorderColor", CollectionGold);

            return SaveAsPrefab(go, $"{ScreensPrefabDir}/{prefabName}.prefab").GetComponent<ShopProductCardView>();
        }

        private static void BuildShopScreenV2Prefab(ShopProductCardView coinCard, ShopProductCardView gemCard,
            ShopProductCardView deckCard)
        {
            var go = new GameObject("ShopScreenV2", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            StretchFill(rect);

            var content = CreateCollectionScrollContent(rect, 8);

            // ---- Offerta hero 980x241 (x 50..1030), bordo oro 6px, fondo viola ----
            var offerSlot = CreateUIObject("OfferSlot", content);
            AddLayoutElement(offerSlot, preferredHeight: 241f);
            var offerRect = CreateUIObject("OfferPanel", offerSlot);
            offerRect.anchorMin = Vector2.zero;
            offerRect.anchorMax = Vector2.one;
            offerRect.offsetMin = new Vector2(50f, 0f);
            offerRect.offsetMax = new Vector2(-50f, 0f);
            AddRoundedPanel(offerRect, "panel_fill_r30", 60f, 30f, CollectionGold, 6f, ShopOfferFill, out _, out _);

            // chest_purple (alpha 180x155) -> ~190 visibili.
            var offerArtRect = CreateUIObject("Artwork", offerRect);
            Place(offerArtRect, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(135f, -120f), new Vector2(205f, 173f));
            var offerArt = offerArtRect.gameObject.AddComponent<Image>();
            offerArt.sprite = LoadSprite(IconsPath, "chest_purple");
            offerArt.preserveAspect = true;
            offerArt.raycastTarget = false;

            var offerTitleRect = CreateUIObject("Title", offerRect);
            Place(offerTitleRect, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(250f, -70f), new Vector2(560f, 46f));
            var offerTitle = AddText(offerTitleRect, "OFFERTA", 32f, FontStyles.Bold, CollectionNameCream, TextAlignmentOptions.MidlineLeft);
            offerTitle.enableWordWrapping = false;
            offerTitle.enableAutoSizing = true;
            offerTitle.fontSizeMin = 22f;
            offerTitle.fontSizeMax = 32f;
            ApplyOutline(offerTitle, NavyOutlineMaterial());

            var offerSubRect = CreateUIObject("Subtitle", offerRect);
            Place(offerSubRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(250f, -100f), new Vector2(520f, 84f));
            var offerSubtitle = AddText(offerSubRect, "-", 24f, FontStyles.Normal, CollectionSubtitle, TextAlignmentOptions.TopLeft);
            offerSubtitle.enableWordWrapping = false;
            offerSubtitle.lineSpacing = 43f;

            // Bottone acquisto: parte visibile 259x42 (btn_green_small).
            var offerButtonRect = CreateUIObject("PurchaseButton", offerRect);
            Place(offerButtonRect, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(380.3f, -209.3f), new Vector2(262f, 51f));
            var offerButtonImage = AddSlicedImage(offerButtonRect, LoadSprite(IconsPath, "btn_green_small"), 51f);
            var offerButton = offerButtonRect.gameObject.AddComponent<Button>();
            offerButton.targetGraphic = offerButtonImage;
            var offerPriceRect = CreateUIObject("Label", offerButtonRect);
            StretchFill(offerPriceRect);
            var offerPrice = AddText(offerPriceRect, "-", 26f, FontStyles.Bold, HomeTextLight, TextAlignmentOptions.Center);
            offerPrice.enableWordWrapping = false;
            ApplyOutline(offerPrice, NavyOutlineMaterial());

            // Badge sconto: rettangolo rosso raggio 12 (panel_fill_r24 tinto), in alto a destra.
            var offerBadgeRect = CreateUIObject("Badge", offerRect);
            Place(offerBadgeRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -22f), new Vector2(161f, 49f));
            var offerBadgeImage = offerBadgeRect.gameObject.AddComponent<Image>();
            offerBadgeImage.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
            offerBadgeImage.type = Image.Type.Sliced;
            offerBadgeImage.pixelsPerUnitMultiplier = 48f / 12f;
            offerBadgeImage.color = ShopBadgeRed;
            offerBadgeImage.raycastTarget = false;
            var offerBadgeLabelRect = CreateUIObject("Label", offerBadgeRect);
            StretchFill(offerBadgeLabelRect);
            var offerBadgeLabel = AddText(offerBadgeLabelRect, "-", 30f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            offerBadgeLabel.enableWordWrapping = false;

            var offerView = offerRect.gameObject.AddComponent<ShopOfferView>();
            SetPrivateField(offerView, "artwork", offerArt);
            SetPrivateField(offerView, "titleLabel", offerTitle);
            SetPrivateField(offerView, "subtitleLabel", offerSubtitle);
            SetPrivateField(offerView, "badgeRoot", offerBadgeRect.gameObject);
            SetPrivateField(offerView, "badgeLabel", offerBadgeLabel);
            SetPrivateField(offerView, "purchaseButton", offerButton);
            SetPrivateField(offerView, "priceLabel", offerPrice);

            // ---- Sezioni ----
            AddVerticalSpacer(content, 41f);
            BuildCollectionSectionHeader(content, "CoinsHeader", "MONETE", false, out _);
            AddVerticalSpacer(content, 12f);
            var coinsRow = AddShopCardRow(content, "CoinsRow", 301f);

            AddVerticalSpacer(content, 51f);
            BuildCollectionSectionHeader(content, "GemsHeader", "GEMME", false, out _);
            AddVerticalSpacer(content, 12f);
            var gemsRow = AddShopCardRow(content, "GemsRow", 281f);

            AddVerticalSpacer(content, 51f);
            BuildCollectionSectionHeader(content, "DecksHeader", "MAZZI", false, out _);
            AddVerticalSpacer(content, 12f);
            var decksRow = AddShopCardRow(content, "DecksRow", 321f);

            var screen = go.AddComponent<ShopScreenV2>();
            SetPrivateField(screen, "offerView", offerView);
            SetPrivateField(screen, "sections", new[]
            {
                new ShopScreenV2.SectionRefs { Category = ShopProductCategory.Coins, Container = coinsRow, CardPrefab = coinCard },
                new ShopScreenV2.SectionRefs { Category = ShopProductCategory.Gems, Container = gemsRow, CardPrefab = gemCard },
                new ShopScreenV2.SectionRefs { Category = ShopProductCategory.Decks, Container = decksRow, CardPrefab = deckCard },
            });

            SaveAsPrefab(go, $"{ScreensPrefabDir}/ShopScreenV2.prefab");
        }

        private static RectTransform AddShopCardRow(RectTransform content, string objectName, float height)
        {
            var row = CreateUIObject(objectName, content);
            AddLayoutElement(row, preferredHeight: height);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(50, 51, 0, 0);
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return row;
        }
    }
}
