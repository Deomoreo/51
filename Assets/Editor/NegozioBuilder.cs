using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Ricostruisce il contenuto di "ShopPage" (Assets/Mockup/16_negozio.png), gia' esistente
    /// come pagina placeholder sotto PagesViewport (HomeScreenBuilder.CreatePlaceholderPage).
    /// Stessa tecnica di DeckPageBuilder: trova la pagina, la ripulisce, ricostruisce dentro
    /// un ContentArea centrato (mai ancorato al bordo pagina - PanelSwipeController allarga
    /// ogni pagina di pageHorizontalOverflow per lato, il bordo pagina non e' il bordo
    /// schermo). Contenuto piu' alto dello spazio disponibile (banner + 3 sezioni x 3 card)
    /// quindi, a differenza di DeckPage, serve uno ScrollRect (RectMask2D + catcher
    /// trasparente per l'input, non Mask+Image - stessa lezione del bug di culling alpha
    /// gia' visto altrove in questo progetto).
    /// Nessun negozio/economia reale nel progetto: tutti i prezzi sono presentazione statica
    /// dal mockup, ogni bottone "compra" e' uno StubActionButton (Debug.Log).
    /// </summary>
    public static class NegozioBuilder
    {
        private const string ScenePath = "Assets/Scenes/HomeScreen.unity";
        private const string ThemePath = "Assets/Resources/DragonsHoardTheme.asset";

        private const float ContentAreaY0 = 254f;
        private const float ContentWidth = 980f;

        private const float CardGap = 24f;
        private static readonly Color PanelDark = new Color(0.06f, 0.08f, 0.12f, 1f);
        private static readonly Color CardFill = new Color(0.086f, 0.157f, 0.235f);
        private static readonly Color RingMuted = new Color(0.18f, 0.31f, 0.42f);
        private static readonly Color RingHighlight = new Color(0.91f, 0.70f, 0.29f);

        private struct ShopCard
        {
            public string Icon;
            public string Amount;
            public string Price;
            public bool Highlighted;
        }

        private static readonly ShopCard[] Monete =
        {
            new ShopCard { Icon = "ic_coin_clover", Amount = "1.000", Price = "0,99 €" },
            new ShopCard { Icon = "chest_green", Amount = "5.500", Price = "4,99 €", Highlighted = true },
            new ShopCard { Icon = "chest_purple", Amount = "12.000", Price = "9,99 €" },
        };

        private static readonly ShopCard[] Gemme =
        {
            new ShopCard { Icon = "ic_gem_green", Amount = "50", Price = "1,99 €" },
            new ShopCard { Icon = "ic_gem_green", Amount = "300", Price = "7,99 €" },
            new ShopCard { Icon = "ic_gem_green", Amount = "800", Price = "19,99 €" },
        };

        private struct LockedDeck
        {
            public string Name;
            public string Price;
        }

        private static readonly LockedDeck[] Mazzi =
        {
            new LockedDeck { Name = "Reale", Price = "1.500" },
            new LockedDeck { Name = "Smeraldo", Price = "250 gemme" },
            new LockedDeck { Name = "Antico", Price = "3.000" },
        };

        private static UITheme _theme;

        [MenuItem("Tools/Dragons Hoard/Build Negozio")]
        private static void Build()
        {
            if (!LoadContext()) return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[NegozioBuilder] Nessun Canvas trovato in HomeScreen.unity.");
                return;
            }

            var canvasRect = (RectTransform)canvas.transform;
            var shopPage = FindDeepChild(canvasRect, "ShopPage") as RectTransform;
            if (shopPage == null)
            {
                Debug.LogError("[NegozioBuilder] Nessun 'ShopPage' trovato sotto PagesViewport: esegui prima Tools/Dragons Hoard/Build Home Screen.");
                return;
            }

            shopPage.gameObject.SetActive(true);

            for (int i = shopPage.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(shopPage.GetChild(i).gameObject);
            }

            var bg = shopPage.GetComponent<Image>();
            if (bg == null) bg = shopPage.gameObject.AddComponent<Image>();
            bg.color = PanelDark;
            bg.raycastTarget = true;

            var contentArea = CreateUIObject("ContentArea", shopPage);
            SetTopCenter(contentArea, ContentAreaY0, ContentWidth, 1080f);

            BuildScrollContent(contentArea);

            shopPage.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[NegozioBuilder] ShopPage ricostruita in HomeScreen.unity. Salvato.");
        }

        // ------------------------------------------------------------------
        // ScrollView
        // ------------------------------------------------------------------

        private static void BuildScrollContent(RectTransform contentArea)
        {
            var scrollViewRt = CreateUIObject("ScrollView", contentArea);
            StretchFill(scrollViewRt);
            var scrollRect = scrollViewRt.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            var viewport = CreateUIObject("Viewport", scrollViewRt);
            StretchFill(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            catcher.raycastTarget = true;

            var content = CreateUIObject("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            scrollRect.viewport = viewport;
            scrollRect.content = content;

            float y = 0f;
            y += CreateFeaturedBanner(content, y) + 40f;
            y += CreateCardSection(content, "MONETE", Monete, y) + 40f;
            y += CreateCardSection(content, "GEMME", Gemme, y) + 40f;
            y += CreateLockedDeckSection(content, "MAZZI", y) + 40f;

            content.sizeDelta = new Vector2(0f, y);
        }

        // ------------------------------------------------------------------
        // Banner "Pacchetto Starter"
        // ------------------------------------------------------------------

        private static float CreateFeaturedBanner(RectTransform content, float yTop)
        {
            const float height = 216f;

            var banner = CreateUIObject("FeaturedBanner", content);
            SetTopStretch(banner, yTop, height);

            var fill = banner.gameObject.AddComponent<Image>();
            fill.sprite = LoadSprite("panel_fill_r24");
            fill.type = Image.Type.Sliced;
            fill.color = new Color(0.16f, 0.09f, 0.20f);
            fill.raycastTarget = false;

            var ringRt = CreateUIObject("Ring", banner);
            StretchFill(ringRt);
            AddSpriteImage(ringRt, "panel_ring_r24", sliced: true).color = RingHighlight;

            var chestRt = CreateUIObject("Chest", banner);
            SetAnchoredRect(chestRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(130f, 130f), new Vector2(30f, 0f));
            AddSpriteImage(chestRt, "chest_purple", preserveAspect: true);

            var titleRt = CreateUIObject("Title", banner);
            SetAnchoredRect(titleRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(-360f, 40f), new Vector2(180f, -30f));
            AddText(titleRt, "PACCHETTO STARTER", 27f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.MidlineLeft);

            var descRt = CreateUIObject("Description", banner);
            SetAnchoredRect(descRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(-360f, 60f), new Vector2(180f, -80f));
            AddText(descRt, "5.000 monete + 100 gemme\n+ 1 mazzo esclusivo", 20f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.MidlineLeft);

            var discountRt = CreateUIObject("DiscountBadge", banner);
            SetAnchoredRect(discountRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(110f, 48f), new Vector2(-20f, -20f));
            var discountBg = discountRt.gameObject.AddComponent<Image>();
            discountBg.sprite = LoadSprite("panel_fill_r24");
            discountBg.type = Image.Type.Sliced;
            discountBg.color = Hex("#D6473F");
            var discountTextRt = CreateUIObject("Text", discountRt);
            StretchFill(discountTextRt);
            AddText(discountTextRt, "-60%", 22f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            var buyRt = CreateUIObject("BuyButton", banner);
            SetAnchoredRect(buyRt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(240f, 62f), new Vector2(180f, 22f));
            var buyBg = AddSpriteImage(buyRt, "btn_green_small", raycastTarget: true, sliced: true);
            buyRt.gameObject.AddComponent<Button>().targetGraphic = buyBg;
            var buyTextRt = CreateUIObject("Text", buyRt);
            StretchFill(buyTextRt);
            AddText(buyTextRt, "4,99 €", 24f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            AddStub(buyRt.gameObject, "Acquisto Pacchetto Starter: nessun negozio/pagamento reale collegato ancora.");

            return height;
        }

        // ------------------------------------------------------------------
        // Sezioni MONETE / GEMME (3 card uguali)
        // ------------------------------------------------------------------

        private static float CreateCardSection(RectTransform content, string label, ShopCard[] cards, float yTop)
        {
            const float labelHeight = 40f;
            const float cardHeight = 232f;

            var labelRt = CreateUIObject("Label_" + label, content);
            SetTopStretch(labelRt, yTop, labelHeight);
            AddText(labelRt, label, 23f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.MidlineLeft);

            float cardWidth = (ContentWidth - CardGap * 2f) / 3f;
            for (int i = 0; i < cards.Length; i++)
            {
                float x = i * (cardWidth + CardGap) - ContentWidth * 0.5f + cardWidth * 0.5f;
                CreateShopCard(content, cards[i], x, yTop + labelHeight, cardWidth, cardHeight);
            }

            return labelHeight + cardHeight;
        }

        private static void CreateShopCard(RectTransform content, ShopCard card, float xCenter, float yTop, float width, float height)
        {
            var cell = CreateUIObject("Card", content);
            SetAnchoredRect(cell, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(width, height), new Vector2(xCenter, -yTop));

            var fill = cell.gameObject.AddComponent<Image>();
            fill.sprite = LoadSprite("panel_fill_r24");
            fill.type = Image.Type.Sliced;
            fill.color = CardFill;
            fill.raycastTarget = false;

            var ringRt = CreateUIObject("Ring", cell);
            StretchFill(ringRt);
            AddSpriteImage(ringRt, "panel_ring_r24", sliced: true).color = card.Highlighted ? RingHighlight : RingMuted;

            if (card.Highlighted)
            {
                var badgeRt = CreateUIObject("BestBadge", cell);
                SetAnchoredRect(badgeRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(width - 40f, 34f), new Vector2(0f, -6f));
                var badgeBg = AddSpriteImage(badgeRt, "btn_gold_long", sliced: true);
                var badgeTextRt = CreateUIObject("Text", badgeRt);
                StretchFill(badgeTextRt);
                AddText(badgeTextRt, "MIGLIORE", 17f, FontStyles.Bold, Hex("#3A2208"), TextAlignmentOptions.Center);
            }

            var iconRt = CreateUIObject("Icon", cell);
            SetAnchoredRect(iconRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(74f, 74f), new Vector2(0f, card.Highlighted ? -54f : -24f));
            AddSpriteImage(iconRt, card.Icon, preserveAspect: true);

            var amountRt = CreateUIObject("Amount", cell);
            SetAnchoredRect(amountRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(width - 16f, 32f), new Vector2(0f, -(card.Highlighted ? 132f : 102f)));
            AddText(amountRt, card.Amount, 25f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);

            var priceRt = CreateUIObject("PriceButton", cell);
            SetAnchoredRect(priceRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(width - 24f, 54f), new Vector2(0f, 18f));
            var priceBg = AddSpriteImage(priceRt, "btn_green_small", raycastTarget: true, sliced: true);
            priceRt.gameObject.AddComponent<Button>().targetGraphic = priceBg;
            var priceTextRt = CreateUIObject("Text", priceRt);
            StretchFill(priceTextRt);
            AddText(priceTextRt, card.Price, 21f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            AddStub(priceRt.gameObject, $"Acquisto {card.Amount}: nessun negozio/pagamento reale collegato ancora.");
        }

        // ------------------------------------------------------------------
        // Sezione MAZZI (celle bloccate, stesso stile di Collezione)
        // ------------------------------------------------------------------

        private static float CreateLockedDeckSection(RectTransform content, string label, float yTop)
        {
            const float labelHeight = 40f;
            const float cardHeight = 232f;

            var labelRt = CreateUIObject("Label_" + label, content);
            SetTopStretch(labelRt, yTop, labelHeight);
            AddText(labelRt, label, 23f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.MidlineLeft);

            float cardWidth = (ContentWidth - CardGap * 2f) / 3f;
            for (int i = 0; i < Mazzi.Length; i++)
            {
                float x = i * (cardWidth + CardGap) - ContentWidth * 0.5f + cardWidth * 0.5f;
                CreateLockedDeckCard(content, Mazzi[i], x, yTop + labelHeight, cardWidth, cardHeight);
            }

            return labelHeight + cardHeight;
        }

        private static void CreateLockedDeckCard(RectTransform content, LockedDeck deck, float xCenter, float yTop, float width, float height)
        {
            var cell = CreateUIObject("Card", content);
            SetAnchoredRect(cell, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(width, height), new Vector2(xCenter, -yTop));

            var fill = cell.gameObject.AddComponent<Image>();
            fill.sprite = LoadSprite("panel_fill_r24");
            fill.type = Image.Type.Sliced;
            fill.color = CardFill;
            fill.raycastTarget = false;

            var ringRt = CreateUIObject("Ring", cell);
            StretchFill(ringRt);
            AddSpriteImage(ringRt, "panel_ring_r24", sliced: true).color = RingMuted;

            var cardArtRt = CreateUIObject("CardArt", cell);
            SetAnchoredRect(cardArtRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(96f, 128f), new Vector2(0f, -18f));
            var cardImg = AddSpriteImage(cardArtRt, "card_frame_dark", preserveAspect: true);
            cardImg.color = new Color(0.4f, 0.4f, 0.4f);

            var lockRt = CreateUIObject("LockIcon", cardArtRt);
            SetAnchoredRect(lockRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(44f, 44f), Vector2.zero);
            AddSpriteImage(lockRt, "ic_lock", preserveAspect: true);

            var nameRt = CreateUIObject("Name", cell);
            SetAnchoredRect(nameRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(width - 16f, 30f), new Vector2(0f, -156f));
            AddText(nameRt, deck.Name, 22f, FontStyles.Bold, _theme.TextMuted, TextAlignmentOptions.Center);

            var priceRt = CreateUIObject("PriceButton", cell);
            SetAnchoredRect(priceRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(width - 24f, 54f), new Vector2(0f, 18f));
            var priceBg = AddSpriteImage(priceRt, "btn_blue_long", raycastTarget: true, sliced: true);
            priceRt.gameObject.AddComponent<Button>().targetGraphic = priceBg;
            var priceTextRt = CreateUIObject("Text", priceRt);
            StretchFill(priceTextRt);
            AddText(priceTextRt, deck.Price, 21f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            AddStub(priceRt.gameObject, $"Acquisto mazzo {deck.Name}: nessun negozio/pagamento reale collegato ancora.");
        }

        private static void AddStub(GameObject go, string message)
        {
            var stub = go.AddComponent<StubActionButton>();
            var so = new SerializedObject(stub);
            so.FindProperty("logMessage").stringValue = message;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------
        // Contesto / helper generici
        // ------------------------------------------------------------------

        private static bool LoadContext()
        {
            _theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (_theme == null)
            {
                Debug.LogError($"[NegozioBuilder] UITheme non trovato in {ThemePath}.");
                return false;
            }
            return true;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
            return null;
        }

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchoredRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
        }

        private static void SetTopCenter(RectTransform rt, float y0, float w, float h)
        {
            SetAnchoredRect(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(w, h), new Vector2(0f, -y0));
        }

        /// <summary>Larghezza piena del content (stretch orizzontale), altezza fissa, ancorato in alto - usato dentro il Content dello ScrollRect.</summary>
        private static void SetTopStretch(RectTransform rt, float y0, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, h);
            rt.anchoredPosition = new Vector2(0f, -y0);
        }

        private static Sprite LoadSprite(string fileNameNoExt)
        {
            return DragonsHoardSprites.Find(fileNameNoExt);
        }

        private static Image AddSpriteImage(RectTransform rt, string fileNameNoExt,
            bool raycastTarget = false, bool preserveAspect = false, bool sliced = false)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(fileNameNoExt);
            image.raycastTarget = raycastTarget;
            image.preserveAspect = preserveAspect;
            if (sliced) image.type = Image.Type.Sliced;
            return image;
        }

        private static TextMeshProUGUI AddText(RectTransform rt, string content, float fontSize,
            FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;
        }
    }
}
