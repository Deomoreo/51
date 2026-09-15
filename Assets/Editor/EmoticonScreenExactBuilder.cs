using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Replica pixel-accurate (tolleranza 2px) della schermata Emoticon, costruita DIRETTAMENTE
    /// sotto Canvas - sibling di SafeArea, MAI dentro: zero Safe Area, zero swipe carousel
    /// (PanelSwipeController), zero Layout Group. Ogni RectTransform e' posizionato con
    /// SetTopLeft (anchorMin=anchorMax=pivot=(0,1), anchoredPosition=(x,-y), sizeDelta=(w,h))
    /// sui numeri forniti dall'utente, presi alla lettera - nessuna interpretazione.
    ///
    /// Perche' un file separato da DeckPageBuilder invece di riusarlo: DeckPageBuilder nidifica
    /// il contenuto dentro SafeArea/PagesViewport (necessari per lo swipe fra le 4 pagine home),
    /// e su un dispositivo/simulatore con un vero notch SafeAreaFitter restringe il rect di
    /// SafeArea sotto i 1080x1920 pieni - tutto quello che ci sta dentro appare quindi rimpicciolito
    /// rispetto al mockup anche se i numeri LOCALI sono esatti (verificato nel giro precedente).
    /// Questo screen e' l'unica garanzia di fedelta' 1:1 al mockup indipendente dal device.
    /// </summary>
    public static class EmoticonScreenExactBuilder
    {
        private const string ScenePath = "Assets/Scenes/HomeScreen.unity";
        private const string MockupPath = "Assets/Mockup/21_collezione_emoticon (1).png";
        private const string ThemePath = "Assets/Resources/DragonsHoardTheme.asset";

        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;

        private static readonly Color PanelDark = new Color(0.043f, 0.078f, 0.125f, 1f);
        private static readonly Color GoldRing = new Color(232f / 255f, 178f / 255f, 74f / 255f, 1f);
        private static readonly Color LineRing = new Color(70f / 255f, 102f / 255f, 142f / 255f, 1f);
        private static readonly Color CellFill = new Color(26f / 255f, 44f / 255f, 68f / 255f, 252f / 255f);
        private static readonly Color EmptyFill = new Color(14f / 255f, 24f / 255f, 40f / 255f, 220f / 255f);
        private static readonly Color EmptyDash = new Color(70f / 255f, 100f / 255f, 140f / 255f, 1f);
        private static readonly Color GreenCheck = new Color(0.290f, 0.839f, 0.545f, 1f);
        private static readonly Color BadgeRed = new Color(196f / 255f, 54f / 255f, 44f / 255f, 1f);
        private static readonly Color MutedText = new Color(0.706f, 0.816f, 0.918f, 1f);

        private static UITheme _theme;

        /// <summary>Nome -> (x, y, w, h) assoluti canvas, source of truth fornita dall'utente. Popolato durante Build() e usato da RunDebugVerification.</summary>
        private static readonly Dictionary<string, Vector4> Targets = new Dictionary<string, Vector4>();
        private static readonly Dictionary<string, RectTransform> Built = new Dictionary<string, RectTransform>();

        [MenuItem("Tools/Dragons Hoard/Build Emoticon Screen EXACT")]
        private static void Build()
        {
            _theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (_theme == null)
            {
                Debug.LogError("[EmoticonScreenExactBuilder] UITheme non trovato in " + ThemePath);
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[EmoticonScreenExactBuilder] Nessun Canvas in HomeScreen.unity.");
                return;
            }
            var canvasRect = (RectTransform)canvas.transform;

            var existing = canvasRect.Find("EmoticonScreenExact");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            Targets.Clear();
            Built.Clear();

            // Root: SIBLING diretto di Canvas (NON figlio di SafeArea) - stretch pieno 0..1 con
            // offset 0 equivale esattamente al reference 1080x1920, sempre, su ogni device/aspect,
            // perche' non passa mai da SafeAreaFitter.
            var root = CreateUIObject("EmoticonScreenExact", canvasRect);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.localScale = Vector3.one;

            var background = CreateUIObject("Background", root);
            SetTopLeft(background, 0f, 0f, RefWidth, RefHeight);
            var bg = background.gameObject.AddComponent<Image>();
            bg.color = PanelDark;
            bg.raycastTarget = false;

            // Header: nessuna misura esatta fornita in questo giro (solo Tab/Equipped/Collection/
            // BottomNav lo sono) - placeholder vuoto per rispettare la gerarchia richiesta, non
            // inventato a pixel.
            var header = CreateUIObject("Header", root);
            SetTopLeft(header, 0f, 0f, RefWidth, 200f);

            var tabs = CreateUIObject("Tabs", root);
            SetTopLeft(tabs, 0f, 0f, RefWidth, RefHeight);
            BuildTabs(tabs);

            var equipped = CreateUIObject("EquippedSection", root);
            SetTopLeft(equipped, 0f, 0f, RefWidth, RefHeight);
            BuildEquippedSection(equipped);

            var collection = CreateUIObject("CollectionSection", root);
            SetTopLeft(collection, 0f, 0f, RefWidth, RefHeight);
            BuildCollectionSection(collection);

            var bottomNav = CreateUIObject("BottomNav", root);
            SetTopLeft(bottomNav, 0f, 0f, RefWidth, RefHeight);
            BuildBottomNav(bottomNav);

            // Fase 1: reference overlay - DIETRO a tutti gli elementi UI (richiesto esplicitamente),
            // quindi primo sibling anche se elencato per ultimo nella gerarchia richiesta.
            var referenceOverlay = CreateUIObject("ReferenceOverlay", root);
            SetTopLeft(referenceOverlay, 0f, 0f, RefWidth, RefHeight);
            referenceOverlay.SetAsFirstSibling();
            var overlaySprite = AssetDatabase.LoadAssetAtPath<Sprite>(MockupPath);
            var overlayImg = referenceOverlay.gameObject.AddComponent<Image>();
            overlayImg.raycastTarget = false;
            if (overlaySprite != null)
            {
                overlayImg.sprite = overlaySprite;
                overlayImg.color = new Color(1f, 1f, 1f, 0.35f);
            }
            else
            {
                Debug.LogWarning("[EmoticonScreenExactBuilder] Mockup non caricabile come Sprite: " + MockupPath + " - ReferenceOverlay lasciato vuoto/trasparente.");
                overlayImg.color = new Color(1f, 0f, 1f, 0.15f);
            }

            EnforceUniformScale(root);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            RunDebugVerification();

            Debug.Log("[EmoticonScreenExactBuilder] EmoticonScreenExact ricostruito in HomeScreen.unity (root diretto sotto Canvas, no SafeArea). Salvato.");
        }

        [MenuItem("Tools/Dragons Hoard/Toggle Reference Overlay")]
        private static void ToggleOverlay()
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            var overlay = canvas != null ? canvas.transform.Find("EmoticonScreenExact/ReferenceOverlay") : null;
            if (overlay == null)
            {
                Debug.LogWarning("[EmoticonScreenExactBuilder] ReferenceOverlay non trovato - esegui prima Build Emoticon Screen EXACT.");
                return;
            }
            overlay.gameObject.SetActive(!overlay.gameObject.activeSelf);
            Debug.Log("[EmoticonScreenExactBuilder] ReferenceOverlay -> " + (overlay.gameObject.activeSelf ? "ON" : "OFF"));
        }

        // ------------------------------------------------------------------
        // TAB ROW
        // ------------------------------------------------------------------

        private static void BuildTabs(RectTransform parent)
        {
            AddTab(parent, "Tab_MAZZI", "MAZZI", 60f, 224f, 310f, 56f, selected: false);
            AddTab(parent, "Tab_EMOTICON", "EMOTICON", 384f, 217f, 310f, 69f, selected: true);
            AddTab(parent, "Tab_ACCUSI", "ACCUSI", 708f, 224f, 310f, 56f, selected: false);
        }

        private static void AddTab(RectTransform parent, string name, string label, float x, float y, float w, float h, bool selected)
        {
            var tab = CreateUIObject(name, parent);
            SetTopLeft(tab, x, y, w, h);
            RegisterTarget(name, x, y, w, h, tab);

            var bgImg = AddSpriteImage(tab, selected ? "btn_teal" : "btn_gray_small", sliced: true);
            bgImg.raycastTarget = true;
            tab.gameObject.AddComponent<Button>().targetGraphic = bgImg;

            var labelRt = CreateUIObject("Label", tab);
            StretchFill(labelRt);
            var text = AddText(labelRt, label, 27f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
            text.fontMaterial = new Material(text.fontMaterial);
            text.outlineWidth = 0.2f;
            text.outlineColor = Color.black;
        }

        // ------------------------------------------------------------------
        // EQUIPAGGIATE
        // ------------------------------------------------------------------

        private static void BuildEquippedSection(RectTransform parent)
        {
            var header = CreateUIObject("EquippedHeader", parent);
            SetTopLeft(header, 59f, 322f, 959f, 35f);
            AddText(header, "EQUIPAGGIATE · 3 max", 26f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.Left);

            AddEquippedSlot(parent, "Slot1", 59f, 365f, 307f, 249f, filled: true, spriteName: "emo_risata", label: "Risata");
            AddEquippedSlot(parent, "Slot2", 387f, 365f, 307f, 249f, filled: true, spriteName: "emo_arrabbiato", label: "Arrabbiato");
            AddEquippedSlot(parent, "Slot3", 715f, 365f, 307f, 249f, filled: false, spriteName: null, label: null);
        }

        private static void AddEquippedSlot(RectTransform parent, string name, float x, float y, float w, float h,
            bool filled, string spriteName, string label)
        {
            var slot = CreateUIObject(name, parent);
            SetTopLeft(slot, x, y, w, h);
            RegisterTarget(name, x, y, w, h, slot);

            if (filled)
            {
                AddFillLine(slot, CellFill, GoldRing, 6f);

                var iconRt = CreateUIObject("Icon", slot);
                SetAnchoredRect(iconRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(128f, 128f), new Vector2(0f, -14f));
                AddSpriteImage(iconRt, spriteName, preserveAspect: true);

                var nameRt = CreateUIObject("NameText", slot);
                SetAnchoredRect(nameRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(w - 16f, 32f), new Vector2(0f, 16f));
                AddText(nameRt, label, 22f, FontStyles.Normal, MutedText, TextAlignmentOptions.Center);

                var removeRt = CreateUIObject("RemoveButton", slot);
                SetAnchoredRect(removeRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(52f, 52f), new Vector2(-2f, -2f));
                var removeBgRt = CreateUIObject("Bg", removeRt);
                SetAnchoredRect(removeBgRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 40f), Vector2.zero);
                AddSpriteImage(removeBgRt, "frame_round", preserveAspect: true).color = BadgeRed;
                var removeIconRt = CreateUIObject("Icon", removeRt);
                SetAnchoredRect(removeIconRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(20f, 20f), Vector2.zero);
                AddSpriteImage(removeIconRt, "ic_x", preserveAspect: true).color = Color.white;
            }
            else
            {
                var fillRt = CreateUIObject("Fill", slot);
                StretchFill(fillRt);
                AddSpriteImage(fillRt, "panel_fill_r24", sliced: true).color = EmptyFill;
                CreateDashedBorder(slot, w, h, EmptyDash);

                // Bounding box ESATTA fornita dall'utente: x=829,y=451,w=78,h=78 in coordinate
                // assolute canvas. Slot3 e' a x=715,y=365 => offset locale (114,86).
                var plusRt = CreateUIObject("PlusIcon", slot);
                SetTopLeft(plusRt, 114f, 86f, 78f, 78f);
                RegisterTarget("PlusIcon", 829f, 451f, 78f, 78f, plusRt, parentOffset: new Vector2(x, y));
                AddSpriteImage(plusRt, "ic_plus_circle", preserveAspect: true).color = MutedText;

                var textRt = CreateUIObject("Text", slot);
                SetAnchoredRect(textRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(w - 16f, 32f), new Vector2(0f, 16f));
                AddText(textRt, "Slot libero", 21f, FontStyles.Normal, MutedText, TextAlignmentOptions.Center);
            }
        }

        // ------------------------------------------------------------------
        // COLLEZIONE (barra + griglia 4x3)
        // ------------------------------------------------------------------

        private static void BuildCollectionSection(RectTransform parent)
        {
            var header = CreateUIObject("CollectionHeader", parent);
            SetTopLeft(header, 59f, 677f, 959f, 35f);
            var labelRt = CreateUIObject("Label", header);
            SetAnchoredRect(labelRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(260f, 35f), Vector2.zero);
            AddText(labelRt, "COLLEZIONE", 26f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.Left);
            var counterRt = CreateUIObject("Counter", header);
            SetAnchoredRect(counterRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(140f, 35f), Vector2.zero);
            AddText(counterRt, "6 / 12", 26f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.Right);

            // Barra: esterna 59,721,959,35 - fill 59,721,488,35 (numero ESATTO fornito, statico:
            // questa e' una replica pixel-fedele di uno snapshot, non una barra data-driven).
            var bar = CreateUIObject("ProgressBarOuter", parent);
            SetTopLeft(bar, 59f, 721f, 959f, 35f);
            RegisterTarget("ProgressBarOuter", 59f, 721f, 959f, 35f, bar);
            AddSpriteImage(bar, "panel_fill_r24", sliced: true).color = new Color(0.086f, 0.129f, 0.192f, 1f);

            var fill = CreateUIObject("ProgressBarFill", bar);
            SetTopLeft(fill, 0f, 0f, 488f, 35f);
            RegisterTarget("ProgressBarFill", 59f, 721f, 488f, 35f, fill, parentOffset: new Vector2(59f, 721f));
            var fillImg = AddSpriteImage(fill, "panel_fill_r24", sliced: true);
            fillImg.color = _theme.Gold;
            var gloss = CreateUIObject("Gloss", fill);
            gloss.anchorMin = new Vector2(0f, 0.5f);
            gloss.anchorMax = new Vector2(1f, 1f);
            gloss.pivot = new Vector2(0.5f, 1f);
            gloss.offsetMin = new Vector2(4f, 0f);
            gloss.offsetMax = new Vector2(-4f, -3f);
            AddSpriteImage(gloss, "panel_fill_r24", sliced: true).color = new Color(1f, 1f, 1f, 0.28f);

            float[] colX = { 59f, 304f, 549f, 794f };
            float[] rowY = { 789f, 1019f, 1249f };
            string[] names = { "Risata", "Arrabbiato", "Sorpreso", "Pensieroso", "Triste", "Furbo" };
            string[] sprites = { "emo_risata", "emo_arrabbiato", "emo_sorpreso", "emo_pensieroso", "emo_triste", "emo_furbo" };

            for (int i = 0; i < 12; i++)
            {
                int row = i / 4;
                int col = i % 4;
                float x = colX[col];
                float y = rowY[row];
                string cellName = "Card_" + row + "_" + col;

                if (i < names.Length)
                {
                    bool selected = i == 0 || i == 1;
                    AddGridCard(parent, cellName, x, y, names[i], sprites[i], selected);
                }
                else
                {
                    AddLockedCard(parent, cellName, x, y);
                }
            }
        }

        private static void AddGridCard(RectTransform parent, string name, float x, float y, string label, string spriteName, bool selected)
        {
            const float w = 228f, h = 211f;
            var cell = CreateUIObject(name, parent);
            SetTopLeft(cell, x, y, w, h);
            RegisterTarget(name, x, y, w, h, cell);

            AddFillLine(cell, CellFill, selected ? GoldRing : LineRing, selected ? 6f : 3f);

            var iconRt = CreateUIObject("Icon", cell);
            SetAnchoredRect(iconRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(148f, 148f), new Vector2(0f, -10f));
            AddSpriteImage(iconRt, spriteName, preserveAspect: true);

            var nameRt = CreateUIObject("NameText", cell);
            SetAnchoredRect(nameRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(w - 12f, 24f), new Vector2(0f, 8f));
            AddText(nameRt, label, 18f, FontStyles.Normal, _theme.Cream, TextAlignmentOptions.Center);

            if (selected)
            {
                var checkRt = CreateUIObject("CheckIcon", cell);
                SetAnchoredRect(checkRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(40f, 40f), new Vector2(-4f, -4f));
                AddSpriteImage(checkRt, "ic_check", preserveAspect: true).color = GreenCheck;
            }
        }

        private static void AddLockedCard(RectTransform parent, string name, float x, float y)
        {
            const float w = 228f, h = 211f;
            var cell = CreateUIObject(name, parent);
            SetTopLeft(cell, x, y, w, h);
            RegisterTarget(name, x, y, w, h, cell);

            AddFillLine(cell, new Color(0.055f, 0.094f, 0.157f, 240f / 255f), LineRing, 3f);

            var lockRt = CreateUIObject("LockIcon", cell);
            SetAnchoredRect(lockRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(52f, 52f), new Vector2(0f, -48f));
            AddSpriteImage(lockRt, "ic_lock", preserveAspect: true);

            var nameRt = CreateUIObject("NameText", cell);
            SetAnchoredRect(nameRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(w - 12f, 30f), new Vector2(0f, 12f));
            AddText(nameRt, "Bloccata", 19f, FontStyles.Normal, MutedText, TextAlignmentOptions.Center);
        }

        // ------------------------------------------------------------------
        // BOTTOM NAV
        // ------------------------------------------------------------------

        private static void BuildBottomNav(RectTransform parent)
        {
            // Altezza barra non misurata esplicitamente: dedotta da bottomBarFixedHeightPx=190
            // gia' in uso da PanelSwipeController per lo stesso nav in produzione, posizionata
            // in modo che la pill "Carte" (y=1748,h=100) ci stia dentro con margine coerente
            // (1920-190=1730, pill parte 18px sotto il bordo superiore della barra).
            var bar = CreateUIObject("NavBar", parent);
            SetTopLeft(bar, 0f, 1730f, RefWidth, 190f);
            var barBg = bar.gameObject.AddComponent<Image>();
            barBg.color = new Color(0.043f, 0.078f, 0.125f, 1f);

            float[] centers = { 135f, 405f, 675f, 945f };
            string[] labels = { "Gioca", "Carte", "Negozio", "Profilo" };
            const float slotW = 270f;
            const int selectedIndex = 1;

            for (int i = 0; i < 4; i++)
            {
                float slotX = centers[i] - slotW / 2f;
                var item = CreateUIObject("NavItem_" + labels[i], parent);
                SetTopLeft(item, slotX, 1730f, slotW, 190f);
                RegisterTarget("NavItem_" + labels[i], slotX, 1730f, slotW, 190f, item);

                if (i == selectedIndex)
                {
                    // Pill selezionata: bounding box ESATTA fornita, x=284,y=1748,w=243,h=100
                    // assoluta canvas -> offset locale rispetto a questo slot (x=284-slotX).
                    var pillRt = CreateUIObject("Pill", item);
                    SetTopLeft(pillRt, 284f - slotX, 1748f - 1730f, 243f, 100f);
                    RegisterTarget("Pill_Carte", 284f, 1748f, 243f, 100f, pillRt, parentOffset: new Vector2(slotX, 1730f));
                    AddSpriteImage(pillRt, "btn_gold_long", sliced: true);

                    var iconRt = CreateUIObject("Icon", pillRt);
                    SetAnchoredRect(iconRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(56f, 56f), new Vector2(0f, -14f));
                    AddSpriteImage(iconRt, "ic_cards", preserveAspect: true);

                    var labelRt = CreateUIObject("Label", pillRt);
                    SetAnchoredRect(labelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(220f, 28f), new Vector2(0f, 10f));
                    AddText(labelRt, labels[i], 20f, FontStyles.Bold, new Color(0.227f, 0.086f, 0.031f, 1f), TextAlignmentOptions.Center);
                }
                else
                {
                    var iconRt = CreateUIObject("Icon", item);
                    SetAnchoredRect(iconRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(48f, 48f), new Vector2(0f, -20f));
                    var iconSprite = i == 0 ? "ic_gamepad" : i == 2 ? "ic_cart" : "ic_person";
                    AddSpriteImage(iconRt, iconSprite, preserveAspect: true);

                    var labelRt = CreateUIObject("Label", item);
                    SetAnchoredRect(labelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(slotW - 8f, 28f), new Vector2(0f, 20f));
                    AddText(labelRt, labels[i], 18f, FontStyles.Bold, MutedText, TextAlignmentOptions.Center);
                }
            }
        }

        // ------------------------------------------------------------------
        // Debug/verifica: stampa target vs reale, diff, PASS/FAIL, tolleranza 2px
        // ------------------------------------------------------------------

        private static void RegisterTarget(string name, float x, float y, float w, float h, RectTransform built, Vector2? parentOffset = null)
        {
            Targets[name] = new Vector4(x, y, w, h);
            Built[name] = built;
            if (parentOffset.HasValue)
            {
                // Il RectTransform e' figlio di un contenitore gia' offsettato: memorizza l'offset
                // extra da sommare al suo anchoredPosition locale per ottenere l'assoluto canvas
                // in fase di verifica (vedi RunDebugVerification).
                _extraOffsets[name] = parentOffset.Value;
            }
        }

        private static readonly Dictionary<string, Vector2> _extraOffsets = new Dictionary<string, Vector2>();

        private static void RunDebugVerification()
        {
            const float tolerance = 2f;
            int pass = 0, fail = 0;
            var report = new System.Text.StringBuilder();
            report.AppendLine("[EmoticonScreenExactBuilder] Verifica pixel-accuracy (tolleranza " + tolerance + "px):");

            foreach (var kvp in Targets)
            {
                string name = kvp.Key;
                Vector4 target = kvp.Value;
                if (!Built.TryGetValue(name, out var rt) || rt == null)
                {
                    report.AppendLine($"  [SKIP] {name}: RectTransform non trovato");
                    continue;
                }

                Vector2 extra = _extraOffsets.TryGetValue(name, out var off) ? off : Vector2.zero;
                float actualX = rt.anchoredPosition.x + extra.x;
                float actualY = -rt.anchoredPosition.y + extra.y;
                float actualW = rt.sizeDelta.x;
                float actualH = rt.sizeDelta.y;

                float dx = actualX - target.x;
                float dy = actualY - target.y;
                float dw = actualW - target.z;
                float dh = actualH - target.w;
                bool ok = Mathf.Abs(dx) <= tolerance && Mathf.Abs(dy) <= tolerance && Mathf.Abs(dw) <= tolerance && Mathf.Abs(dh) <= tolerance;

                if (ok) pass++; else fail++;

                report.AppendLine($"  [{(ok ? "PASS" : "FAIL")}] {name}: target=({target.x},{target.y},{target.z},{target.w}) reale=({actualX},{actualY},{actualW},{actualH}) diff=({dx:F1},{dy:F1},{dw:F1},{dh:F1}) scale={rt.localScale}");
            }

            report.AppendLine($"[EmoticonScreenExactBuilder] Totale: {pass} PASS, {fail} FAIL su {Targets.Count} elementi.");
            if (fail > 0)
                Debug.LogError(report.ToString());
            else
                Debug.Log(report.ToString());
        }

        private static void EnforceUniformScale(Transform root)
        {
            var rt = root as RectTransform;
            if (rt != null) rt.localScale = Vector3.one;
            for (int i = 0; i < root.childCount; i++)
            {
                EnforceUniformScale(root.GetChild(i));
            }
        }

        // ------------------------------------------------------------------
        // Helper generici (stessa tecnica di DeckPageBuilder, duplicati qui per tenere questo
        // screen di verifica completamente indipendente dal builder di produzione)
        // ------------------------------------------------------------------

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        private static void SetTopLeft(RectTransform rt, float x0, float y0, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x0, -y0);
        }

        private static void SetAnchoredRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
        }

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Image AddSpriteImage(RectTransform rt, string fileNameNoExt, bool preserveAspect = false, bool sliced = false)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = DragonsHoardSprites.Find(fileNameNoExt);
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            if (sliced)
            {
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
            }
            return image;
        }

        private static TextMeshProUGUI AddText(RectTransform rt, string content, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
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

        /// <summary>Bordo arrotondato reale (stesso identico sprite Sliced disegnato due volte,
        /// grande dietro/colore linea + piu' piccolo di 'thickness' sopra/colore fill) - vedi
        /// nota gemella in DeckPageBuilder.AddFillLine sul perche' 4 barre piatte non vanno bene.</summary>
        private static void AddFillLine(RectTransform target, Color fillColor, Color lineColor, float thickness = 3f)
        {
            var lineRt = CreateUIObject("Line", target);
            StretchFill(lineRt);
            AddSpriteImage(lineRt, "panel_fill_r24", sliced: true).color = lineColor;

            var fillRt = CreateUIObject("Fill", target);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.offsetMin = new Vector2(thickness, thickness);
            fillRt.offsetMax = new Vector2(-thickness, -thickness);
            AddSpriteImage(fillRt, "panel_fill_r24", sliced: true).color = fillColor;
        }

        private static void CreateDashedBorder(RectTransform parent, float width, float height, Color color, float thickness = 3f, float dash = 12f, float gap = 8f)
        {
            CreateDashRow(parent, color, thickness, dash, gap, width, new Vector2(0f, 1f), true);
            CreateDashRow(parent, color, thickness, dash, gap, width, new Vector2(0f, 0f), true);
            CreateDashRow(parent, color, thickness, dash, gap, height, new Vector2(0f, 0f), false);
            CreateDashRow(parent, color, thickness, dash, gap, height, new Vector2(1f, 0f), false);
        }

        private static void CreateDashRow(RectTransform parent, Color color, float thickness, float dash, float gap, float length, Vector2 anchor, bool horizontal)
        {
            int count = Mathf.Max(1, Mathf.FloorToInt(length / (dash + gap)));
            for (int i = 0; i < count; i++)
            {
                var seg = CreateUIObject("Dash", parent);
                float offset = i * (dash + gap);
                if (horizontal)
                {
                    SetAnchoredRect(seg, anchor, anchor, new Vector2(0f, 0.5f), new Vector2(dash, thickness), new Vector2(offset, 0f));
                }
                else
                {
                    SetAnchoredRect(seg, anchor, anchor, new Vector2(0.5f, 1f), new Vector2(thickness, dash), new Vector2(0f, -offset));
                }
                var img = seg.gameObject.AddComponent<Image>();
                img.color = color;
                img.raycastTarget = false;
            }
        }
    }
}
