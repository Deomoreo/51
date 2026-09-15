using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project51.Unity.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce i 4 PlayerBanner (Assets/UI_SPEC_Tavolo.md, sezione 3) dentro GameCanvas
    /// in GameScene.unity e li collega a un PlayerBannerManager. Va eseguito DOPO
    /// GameCanvasResolutionFixer (assume canvas 1080x1920, ancore top-left).
    ///
    /// Pass 1: niente sprite (colori pieni), niente cornice tavolo, niente avatar reale -
    /// quella pipeline (Assets/UI/Sprites/DragonsHoard/sprites_unity/...) e' ancora in
    /// migrazione (SpriteImportAutomation.cs) e non l'ho toccata qui.
    /// </summary>
    public static class TablePlayerBannersBuilder
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";

        private static readonly Color BackgroundColor = HexColor("#0E1C30");
        private static readonly Color TurnGlowColor = HexColor("#E8B24A", 0.55f);
        private static readonly Color NameColor = HexColor("#F5EFE0");
        private static readonly Color ScoreColor = HexColor("#E8B24A");
        private static readonly Color TurnLabelColor = HexColor("#1A1206");
        private static readonly Color AvatarPlaceholderColor = HexColor("#1C2E44");
        private static readonly Color ScopeBadgeFillColor = HexColor("#0E1C30");
        private static readonly Color ScopeBadgeBorderColor = HexColor("#E8B24A");

        private const int MaxScopeSlots = 4; // sezione 4: "Massimo 4 visibili"
        private static readonly Vector2 ScopeCardSize = new Vector2(54f, 76f);
        private const float ScopeGroupTopOffset = 38f; // 38px sopra il bordo superiore del banner
        private static readonly Vector2 ScopeBadgeSize = new Vector2(44f, 44f);

        private struct BannerSpec
        {
            public string Name;
            public Vector2 Center;
            public Vector2 Size;
        }

        [MenuItem("Tools/51/Build Table Player Banners")]
        private static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvasGO = GameObject.Find("GameCanvas");
            if (canvasGO == null)
            {
                Debug.LogError("[TablePlayerBannersBuilder] 'GameCanvas' non trovato in GameScene.unity.");
                return;
            }

            var existingRoot = canvasGO.transform.Find("PlayerBanners");
            if (existingRoot != null)
            {
                Debug.LogWarning("[TablePlayerBannersBuilder] 'PlayerBanners' esiste gia': lo rimuovo e ricreo da zero.");
                Object.DestroyImmediate(existingRoot.gameObject);
            }

            var root = new GameObject("PlayerBanners", typeof(RectTransform));
            root.transform.SetParent(canvasGO.transform, false);
            var rootRt = (RectTransform)root.transform;
            StretchFill(rootRt);

            // Y rispetto alla spec originale: il tavolo e' stato centrato piu' in basso
            // (TableFeltRenderer.verticalOffsetRatio) quindi Top/Left/Right seguono, poi ulteriori
            // discese su Left/Right/Local su richiesta esplicita ripetuta (Top lasciato fermo).
            var specs = new[]
            {
                new BannerSpec { Name = "Banner_Top",   Center = new Vector2(470, 320),  Size = new Vector2(290, 88) },
                new BannerSpec { Name = "Banner_Left",  Center = new Vector2(190, 738),  Size = new Vector2(270, 84) },
                new BannerSpec { Name = "Banner_Right", Center = new Vector2(890, 738),  Size = new Vector2(270, 84) },
                new BannerSpec { Name = "Banner_Local", Center = new Vector2(210, 1560), Size = new Vector2(300, 92) },
            };

            var byName = new System.Collections.Generic.Dictionary<string, PlayerBanner>();
            foreach (var spec in specs)
            {
                byName[spec.Name] = BuildBanner(rootRt, spec);
            }

            // A differenza di 'PlayerBanners' qui sopra, questo NON veniva mai controllato prima
            // di ricrearlo: ogni rilancio del tool ne aggiungeva uno nuovo senza mai rimuovere il
            // precedente (bug segnalato: "ci sono 15 PlayerBannerManager nella scena"). Usa
            // GetComponentsInChildren (include inattivi) invece di Find/singola destroy per
            // ripulire TUTTI i duplicati gia' accumulati, non solo il primo.
            var existingManagers = canvasGO.GetComponentsInChildren<PlayerBannerManager>(true);
            if (existingManagers.Length > 0)
            {
                Debug.LogWarning($"[TablePlayerBannersBuilder] {existingManagers.Length} 'PlayerBannerManager' trovati: li rimuovo tutti e ricreo uno pulito.");
                foreach (var existing in existingManagers)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }
            }

            var managerGO = new GameObject("PlayerBannerManager", typeof(PlayerBannerManager));
            managerGO.transform.SetParent(canvasGO.transform, false);
            var manager = managerGO.GetComponent<PlayerBannerManager>();

            // Ordine campo array: 0=Locale, 1=Sinistra, 2=Alto, 3=Destra (vedi PlayerBannerManager.ResolveRelativeSlot)
            var so = new SerializedObject(manager);
            var bannersProp = so.FindProperty("banners");
            bannersProp.arraySize = 4;
            bannersProp.GetArrayElementAtIndex(0).objectReferenceValue = byName["Banner_Local"];
            bannersProp.GetArrayElementAtIndex(1).objectReferenceValue = byName["Banner_Left"];
            bannersProp.GetArrayElementAtIndex(2).objectReferenceValue = byName["Banner_Top"];
            bannersProp.GetArrayElementAtIndex(3).objectReferenceValue = byName["Banner_Right"];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[TablePlayerBannersBuilder] 4 PlayerBanner creati e collegati a PlayerBannerManager. Salvato.");
        }

        private static PlayerBanner BuildBanner(RectTransform parent, BannerSpec spec)
        {
            var go = new GameObject(spec.Name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = spec.Size;
            rt.anchoredPosition = new Vector2(spec.Center.x, -spec.Center.y);

            // TurnGlow: dietro tutto, leggermente piu' grande, spento di default.
            var glow = CreateUIObject("TurnGlow", rt);
            SetStretchWithPadding(glow, -10f);
            var glowImg = glow.gameObject.AddComponent<Image>();
            glowImg.color = TurnGlowColor;
            glow.gameObject.SetActive(false);

            // ScopeRow: creato PRIMA di Background cosi' il banner (sibling successivo) lo
            // copre per la meta' inferiore, come richiesto dalla sezione 4 ("le carte scope
            // stanno DIETRO il banner").
            var scopeSlots = BuildScopeRow(rt, out var scopeBadgeGO, out var scopeBadgeText);

            // Background pieno (placeholder colore, no sprite - vedi nota in cima al file).
            var background = CreateUIObject("Background", rt);
            StretchFill(background);
            background.gameObject.AddComponent<Image>().color = BackgroundColor;

            // AvatarFrame: segnaposto quadrato a sinistra, H = 78% bh.
            float avatarSize = spec.Size.y * 0.78f;
            var avatar = CreateUIObject("AvatarFrame", rt);
            avatar.anchorMin = avatar.anchorMax = new Vector2(0f, 0.5f);
            avatar.pivot = new Vector2(0f, 0.5f);
            avatar.sizeDelta = new Vector2(avatarSize, avatarSize);
            avatar.anchoredPosition = new Vector2(8f, 0f);
            avatar.gameObject.AddComponent<Image>().color = AvatarPlaceholderColor;

            // NameText
            var nameRt = CreateUIObject("NameText", rt);
            nameRt.anchorMin = new Vector2(0f, 0.5f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 0.5f);
            nameRt.offsetMin = new Vector2(avatarSize + 16f, 0f);
            nameRt.offsetMax = new Vector2(-8f, -6f);
            var nameText = AddText(nameRt, "Giocatore", 21f, FontStyles.Bold, NameColor, TextAlignmentOptions.MidlineLeft);

            // ScoreText
            var scoreRt = CreateUIObject("ScoreText", rt);
            scoreRt.anchorMin = new Vector2(0f, 0f);
            scoreRt.anchorMax = new Vector2(1f, 0.5f);
            scoreRt.pivot = new Vector2(0.5f, 0.5f);
            scoreRt.offsetMin = new Vector2(avatarSize + 16f, 6f);
            scoreRt.offsetMax = new Vector2(-8f, 0f);
            var scoreText = AddText(scoreRt, "0 punti", 19f, FontStyles.Normal, ScoreColor, TextAlignmentOptions.MidlineLeft);

            // TurnLabel: in alto a destra, spento di default.
            var turnLabelRt = CreateUIObject("TurnLabel", rt);
            turnLabelRt.anchorMin = turnLabelRt.anchorMax = new Vector2(1f, 1f);
            turnLabelRt.pivot = new Vector2(1f, 1f);
            turnLabelRt.sizeDelta = new Vector2(60f, 22f);
            turnLabelRt.anchoredPosition = new Vector2(-6f, -4f);
            var turnLabelBg = turnLabelRt.gameObject.AddComponent<Image>();
            turnLabelBg.color = HexColor("#E8B24A");

            // Il testo va su un figlio separato: Image + TextMeshProUGUI sullo STESSO
            // GameObject fa fallire AddComponent<TextMeshProUGUI>() (torna null) in questo
            // progetto - stessa struttura Background+Label usata di default dai widget Unity.
            var turnLabelTextRt = CreateUIObject("Text", turnLabelRt);
            StretchFill(turnLabelTextRt);
            AddText(turnLabelTextRt, "TURNO", 15f, FontStyles.Bold, TurnLabelColor, TextAlignmentOptions.Center);

            turnLabelRt.gameObject.SetActive(false);

            // DealerLabel: in alto a sinistra (speculare a TurnLabel), spento di default -
            // mostrato brevemente da TurnController a inizio smazzata (animazione dichiarazione dealer).
            var dealerLabelRt = CreateUIObject("DealerLabel", rt);
            dealerLabelRt.anchorMin = dealerLabelRt.anchorMax = new Vector2(0f, 1f);
            dealerLabelRt.pivot = new Vector2(0f, 1f);
            dealerLabelRt.sizeDelta = new Vector2(86f, 22f);
            dealerLabelRt.anchoredPosition = new Vector2(6f, -4f);
            var dealerLabelBg = dealerLabelRt.gameObject.AddComponent<Image>();
            dealerLabelBg.color = HexColor("#78F0BE");

            var dealerLabelTextRt = CreateUIObject("Text", dealerLabelRt);
            StretchFill(dealerLabelTextRt);
            AddText(dealerLabelTextRt, "MAZZIERE", 13f, FontStyles.Bold, TurnLabelColor, TextAlignmentOptions.Center);

            dealerLabelRt.gameObject.SetActive(false);

            var banner = go.AddComponent<PlayerBanner>();
            var so = new SerializedObject(banner);
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("scoreText").objectReferenceValue = scoreText;
            so.FindProperty("turnGlow").objectReferenceValue = glow.gameObject;
            so.FindProperty("turnLabel").objectReferenceValue = turnLabelRt.gameObject;
            so.FindProperty("dealerLabel").objectReferenceValue = dealerLabelRt.gameObject;

            var scopeSlotsProp = so.FindProperty("scopeCardSlots");
            scopeSlotsProp.arraySize = scopeSlots.Length;
            for (int i = 0; i < scopeSlots.Length; i++)
            {
                scopeSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue = scopeSlots[i];
            }
            so.FindProperty("scopeBadge").objectReferenceValue = scopeBadgeGO;
            so.FindProperty("scopeBadgeText").objectReferenceValue = scopeBadgeText;

            so.ApplyModifiedPropertiesWithoutUndo();

            return banner;
        }

        /// <summary>
        /// Riga scope (sezione 4): fino a 4 slot carta miniatura + badge "+N", ancorati al
        /// top-center del banner. Le posizioni orizzontali esatte vengono ricalcolate a runtime
        /// da PlayerBanner.SetScopeCards in base al conteggio reale; qui vengono solo creati
        /// e disattivati di default (nessuna scopa ancora fatta).
        /// </summary>
        private static Image[] BuildScopeRow(RectTransform bannerRt, out GameObject badgeGO, out TMP_Text badgeText)
        {
            var container = CreateUIObject("ScopeRow", bannerRt);
            container.anchorMin = container.anchorMax = new Vector2(0.5f, 1f);
            container.pivot = new Vector2(0.5f, 1f);
            container.sizeDelta = Vector2.zero;
            container.anchoredPosition = new Vector2(0f, ScopeGroupTopOffset);

            var slots = new Image[MaxScopeSlots];
            for (int i = 0; i < MaxScopeSlots; i++)
            {
                var slotRt = CreateUIObject($"ScopeSlot_{i}", container);
                slotRt.anchorMin = slotRt.anchorMax = new Vector2(0.5f, 1f);
                slotRt.pivot = new Vector2(0.5f, 1f);
                slotRt.sizeDelta = ScopeCardSize;
                slotRt.anchoredPosition = Vector2.zero;

                var img = slotRt.gameObject.AddComponent<Image>();
                img.preserveAspect = true;
                slots[i] = img;
                slotRt.gameObject.SetActive(false);
            }

            badgeGO = BuildScopeBadge(container, out badgeText);
            badgeGO.SetActive(false);

            return slots;
        }

        private static GameObject BuildScopeBadge(RectTransform container, out TMP_Text badgeText)
        {
            var badgeRt = CreateUIObject("ScopeBadge", container);
            badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(0.5f, 1f);
            badgeRt.pivot = new Vector2(0.5f, 1f);
            badgeRt.sizeDelta = ScopeBadgeSize;
            badgeRt.anchoredPosition = Vector2.zero;

            var border = badgeRt.gameObject.AddComponent<Image>();
            border.color = ScopeBadgeBorderColor;

            var fillRt = CreateUIObject("Fill", badgeRt);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(3f, 3f);
            fillRt.offsetMax = new Vector2(-3f, -3f);
            fillRt.gameObject.AddComponent<Image>().color = ScopeBadgeFillColor;

            var textRt = CreateUIObject("Text", fillRt);
            StretchFill(textRt);
            badgeText = AddText(textRt, "+0", 16f, FontStyles.Bold, HexColor("#F5EFE0"), TextAlignmentOptions.Center);

            return badgeRt.gameObject;
        }

        private static RectTransform CreateUIObject(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetStretchWithPadding(RectTransform rt, float padding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-padding, -padding);
            rt.offsetMax = new Vector2(padding, padding);
        }

        private static TMP_Text AddText(RectTransform rt, string text, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableAutoSizing = false;
            return tmp;
        }

        private static Color HexColor(string hex, float alphaOverride = -1f)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c))
            {
                if (alphaOverride >= 0f) c.a = alphaOverride;
                return c;
            }
            return Color.magenta;
        }
    }
}
