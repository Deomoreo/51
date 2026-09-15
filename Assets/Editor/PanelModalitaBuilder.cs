using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce l'overlay "Panel Modalita" (Assets/UI_SPEC_PanelModalita.md) come figlio
    /// disattivato del Canvas nella scena correntemente aperta (pensato per HomeScreen.unity,
    /// gia' costruita da HomeScreenBuilder). Non tocca nessun altro oggetto della scena: se
    /// "ModalitaPanelOverlay" o "PanelModalitaManager" esistono gia' li ricostruisce da zero
    /// (rebuild idempotente), il resto della gerarchia resta intatto.
    ///
    /// Le coordinate della spec sono assolute rispetto a un canvas di riferimento 1080x1920,
    /// origine in alto a sinistra, Y che cresce verso il basso (stesso sistema del mockup
    /// Python che le ha generate). PanelFrame viene ancorato al CENTRO dell'overlay (non
    /// all'angolo) perche' il Canvas reale, sotto CanvasScaler con match width/height 0.5,
    /// quasi mai misura esattamente 1080x1920 in unita' locali (dipende dall'aspect ratio del
    /// device) - ancorare dal bordo sinistro con un offset assoluto faceva percio' scentrare
    /// il pannello. Tutto cio' che sta DENTRO PanelFrame (ribbon, chiudi, scrollview) e'
    /// invece ancorato al SUO angolo in alto a sinistra e usa coordinate relative a lui
    /// (assoluta_spec - 44, assoluta_spec - 250), cosi' restano allineati a lui qualunque
    /// deviazione ci sia.
    /// </summary>
    public static class PanelModalitaBuilder
    {
        private const string ThemePath = "Assets/Resources/DragonsHoardTheme.asset";

        private const float ContentWidth = 908f;
        private const float RowMargin = 10f; // margine sx/dx per righe "larghezza piena" e pillole

        // Box assoluto del PanelFrame nel sistema di riferimento 1080x1920 della spec.
        private const float FrameX0 = 44f;
        private const float FrameY0 = 250f;
        private const float FrameX1 = 1036f;
        private const float FrameY1 = 1640f;
        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;

        private static UITheme _theme;

        [MenuItem("Tools/Dragons Hoard/Build Panel Modalita")]
        private static void Build()
        {
            if (!LoadContext())
            {
                return;
            }

            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[PanelModalitaBuilder] Nessun Canvas trovato nella scena aperta. Apri HomeScreen.unity (gia' costruita da HomeScreenBuilder) prima di lanciare questo tool.");
                return;
            }

            var canvasRect = (RectTransform)canvas.transform;

            DestroyIfExists(canvasRect, "ModalitaPanelOverlay");
            DestroyIfExists(canvasRect, "PanelModalitaManager");

            var overlay = CreateUIObject("ModalitaPanelOverlay", canvasRect);
            StretchFill(overlay);
            var overlayCanvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();

            CreateDimBackground(overlay);
            var panelFrame = CreatePanelFrame(overlay);
            CreateTitleRibbon(panelFrame);
            var closeButton = CreateCloseButton(panelFrame);
            var modeRowItems = new List<SelectableToggleItem>();
            var modeRowLabels = new List<string>();
            var pillItems = new List<SelectableToggleItem>();
            var scrollRect = CreateScrollView(panelFrame, modeRowItems, modeRowLabels, pillItems);

            var modeGroup = WireSelectionGroups(overlay, modeRowItems, pillItems);
            CreateController(canvasRect, overlay, overlayCanvasGroup, panelFrame, closeButton, scrollRect, modeGroup, modeRowLabels);

            overlay.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvas.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

            Debug.Log("[PanelModalitaBuilder] ModalitaPanelOverlay creato (disattivato) sotto " + canvas.name + ". Ricorda di salvare la scena.");
        }

        private static void DestroyIfExists(RectTransform canvasRect, string name)
        {
            var existing = canvasRect.Find(name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
        }

        // ------------------------------------------------------------------
        // Wiring: selezione righe/pillole, apertura/chiusura pannello
        // ------------------------------------------------------------------

        private static SelectableToggleGroup WireSelectionGroups(RectTransform overlay, List<SelectableToggleItem> modeRowItems,
            List<SelectableToggleItem> pillItems)
        {
            var modeGroupGO = CreateUIObject("ModeRowGroup", overlay).gameObject;
            var modeGroup = modeGroupGO.AddComponent<SelectableToggleGroup>();
            var soModeGroup = new SerializedObject(modeGroup);
            AssignList(soModeGroup, "items", modeRowItems);
            // Row_1v1Bot e' il quarto item aggiunto (indice 3): 1v1, 2v2, 1v3, 1v1Bot, ...
            soModeGroup.FindProperty("defaultSelectedIndex").intValue = 3;
            soModeGroup.ApplyModifiedPropertiesWithoutUndo();

            var pillGroupGO = CreateUIObject("DifficultyPillGroup", overlay).gameObject;
            var pillGroup = pillGroupGO.AddComponent<SelectableToggleGroup>();
            var soPillGroup = new SerializedObject(pillGroup);
            AssignList(soPillGroup, "items", pillItems);
            // Facile, Medio, Difficile -> Medio e' indice 1.
            soPillGroup.FindProperty("defaultSelectedIndex").intValue = 1;
            soPillGroup.ApplyModifiedPropertiesWithoutUndo();

            // Il collegamento a PanelModalitaController.OnModeSelected NON si fa qui: lo fa
            // il controller stesso in Awake() via modeGroup.onSelectedRuntime (delegate C#
            // diretto, vedi SelectableToggleGroup) - ritorniamo solo il riferimento.
            return modeGroup;
        }

        private static void AssignList(SerializedObject so, string propertyName, List<SelectableToggleItem> values)
        {
            var prop = so.FindProperty(propertyName);
            prop.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static PanelModalitaController CreateController(RectTransform canvasRect, RectTransform overlay,
            CanvasGroup overlayCanvasGroup, RectTransform panelFrame, Button closeButton, ScrollRect scrollRect,
            SelectableToggleGroup modeGroup, List<string> modeRowLabels)
        {
            var modeSelectorTransform = FindDeepChild(canvasRect, "ModeSelector");
            var openButton = modeSelectorTransform?.GetComponent<Button>();
            var modeSelectorValueText = modeSelectorTransform?.Find("ValueText")?.GetComponent<TMP_Text>();
            if (openButton == null)
            {
                Debug.LogWarning("[PanelModalitaBuilder] Non ho trovato un bottone 'ModeSelector' in scena (dovrebbe essere il selettore Modalita' su Home, creato da HomeScreenBuilder): il pannello non si apre automaticamente, va attivato a mano.");
            }

            var playButton = FindDeepChild(canvasRect, "PlayButton")?.GetComponent<Button>();
            if (playButton == null)
            {
                Debug.LogWarning("[PanelModalitaBuilder] Non ho trovato un bottone 'PlayButton' in scena: il collegamento GIOCA -> modalita' selezionata non e' agganciato.");
            }

            var managerGO = CreateUIObject("PanelModalitaManager", canvasRect).gameObject;
            var controller = managerGO.AddComponent<PanelModalitaController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = overlay.gameObject;
            so.FindProperty("canvasGroup").objectReferenceValue = overlayCanvasGroup;
            so.FindProperty("panelFrame").objectReferenceValue = panelFrame;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("openButton").objectReferenceValue = openButton;
            so.FindProperty("playButton").objectReferenceValue = playButton;
            so.FindProperty("scrollRect").objectReferenceValue = scrollRect;
            so.FindProperty("modeRowGroup").objectReferenceValue = modeGroup;
            so.FindProperty("modeSelectorValueText").objectReferenceValue = modeSelectorValueText;

            var labelsProp = so.FindProperty("modeLabels");
            labelsProp.arraySize = modeRowLabels.Count;
            for (int i = 0; i < modeRowLabels.Count; i++)
            {
                labelsProp.GetArrayElementAtIndex(i).stringValue = modeRowLabels[i];
            }

            // Indice 3 = Row_1v1Bot, stesso default del ModeRowGroup: cosi' il ValueText su
            // Home mostra gia' la modalita' giusta dal primo frame, non il placeholder
            // "Allenamento" finche' l'utente non apre il pannello almeno una volta (il group
            // che normalmente farebbe questo aggiornamento vive dentro l'overlay disattivato,
            // il suo Awake() non gira finche' il pannello non si apre la prima volta).
            so.FindProperty("selectedModeIndex").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (modeSelectorValueText != null && modeRowLabels.Count > 3)
            {
                modeSelectorValueText.text = modeRowLabels[3];
            }

            return controller;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t.name == name)
                {
                    return t;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------
        // DimBackground / PanelFrame / TitleRibbon / CloseButton
        // ------------------------------------------------------------------

        private static void CreateDimBackground(RectTransform overlay)
        {
            var dim = CreateUIObject("DimBackground", overlay);
            StretchFill(dim);
            var img = dim.gameObject.AddComponent<Image>();
            var dimColor = Hex("#040A14");
            // 220/255 invece del 170/255 della spec: niente vero blur (richiederebbe uno
            // shader/RenderTexture, task separato), ma un dim piu' denso da' comunque piu'
            // separazione visiva dalla Home dietro. Su richiesta utente 2026-09-09.
            img.color = new Color(dimColor.r, dimColor.g, dimColor.b, 220f / 255f);
            img.raycastTarget = true; // blocca il click sul contenuto sotto, come un dialog modale
        }

        /// <summary>
        /// Ancorato al CENTRO dell'overlay (non all'angolo): il box assoluto (44,250 ->
        /// 1036,1640) e' centrato quasi esattamente sul canvas di riferimento 1080x1920
        /// (centro box 540,945 vs centro canvas 540,960), quindi lo riproduciamo come
        /// offset dal centro invece che dal bordo. Cosi' resta centrato anche se il Canvas
        /// reale non e' esattamente 1080x1920 in unita' locali (deviazione tipica del blend
        /// width/height di CanvasScaler su aspect ratio diversi da quello di riferimento).
        /// </summary>
        private static RectTransform CreatePanelFrame(RectTransform overlay)
        {
            float w = FrameX1 - FrameX0;
            float h = FrameY1 - FrameY0;
            float boxCenterX = (FrameX0 + FrameX1) * 0.5f;
            float boxCenterY = (FrameY0 + FrameY1) * 0.5f; // in coordinate Y-verso-il-basso
            float offsetX = boxCenterX - RefWidth * 0.5f;
            float offsetY = RefHeight * 0.5f - boxCenterY; // conversione a Y-verso-l'alto di Unity

            var outer = CreateUIObject("PanelFrame", overlay);
            SetAnchoredRect(outer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(w, h), new Vector2(offsetX, offsetY));
            var outerImg = outer.gameObject.AddComponent<Image>();
            outerImg.color = Hex("#785018");
            outerImg.raycastTarget = true;

            // Strato oro, inset di 14px su ogni lato rispetto all'outer.
            var gold = CreateUIObject("GoldBorder", outer);
            StretchFill(gold);
            gold.offsetMin = new Vector2(14f, 14f);
            gold.offsetMax = new Vector2(-14f, -14f);
            var goldImg = gold.gameObject.AddComponent<Image>();
            goldImg.color = Hex("#E8B24A");
            goldImg.raycastTarget = false;

            // Riempimento, ulteriore inset di 8px (14+8=22 totali dal bordo esterno).
            var fill = CreateUIObject("Fill", gold);
            StretchFill(fill);
            fill.offsetMin = new Vector2(8f, 8f);
            fill.offsetMax = new Vector2(-8f, -8f);
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = Hex("#182838");
            fillImg.raycastTarget = false;

            return outer;
        }

        private static void CreateTitleRibbon(RectTransform panelFrame)
        {
            var ribbon = CreateUIObject("TitleRibbon", panelFrame);
            // 194 e' assoluto spec, relativo a PanelFrame: 194 - FrameY0 = -56 (sopra il
            // bordo superiore del frame di 56px, il ribbon "sbuca" sopra il pannello).
            SetTopCenter(ribbon, 194f - FrameY0, 520f, 188f);
            AddSpriteImage(ribbon, "ribbon_teal", raycastTarget: false, sliced: true);

            var textRt = CreateUIObject("Text", ribbon);
            StretchFill(textRt);
            var text = AddText(textRt, "MODALITÀ", 40f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
            text.outlineWidth = 0.2f;
            text.outlineColor = Hex("#241608");
        }

        private static Button CreateCloseButton(RectTransform panelFrame)
        {
            var button = CreateUIObject("CloseButton", panelFrame);
            SetTopLeft(button, 932f - FrameX0, 276f - FrameY0, 86f, 86f);
            var bg = AddSpriteImage(button, "sq_blue", raycastTarget: true, sliced: true);
            var btn = button.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;

            var icon = CreateUIObject("Icon", button);
            SetAnchoredRect(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(46f, 46f), Vector2.zero);
            AddSpriteImage(icon, "ic_x", preserveAspect: true);

            return btn;
        }

        // ------------------------------------------------------------------
        // ScrollView
        // ------------------------------------------------------------------

        private static ScrollRect CreateScrollView(RectTransform panelFrame, List<SelectableToggleItem> modeRowItems,
            List<string> modeRowLabels, List<SelectableToggleItem> pillItems)
        {
            var scrollViewRt = CreateUIObject("ScrollView", panelFrame);
            SetTopLeft(scrollViewRt, 76f - FrameX0, 390f - FrameY0, 984f - 76f, 1608f - 390f);
            var scrollRect = scrollViewRt.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            var viewport = CreateUIObject("Viewport", scrollViewRt);
            StretchFill(viewport);
            // RectMask2D invece di Mask+Image per il clipping (vedi PanelSwipeController.
            // ApplyViewportMask: Mask+Image ad alpha 0 fa sparire anche i figli, bug noto).
            viewport.gameObject.AddComponent<RectMask2D>();

            // Serve comunque un Image raycastable a tutta area sul Viewport: senza, un drag
            // che parte da un punto senza nessun Graphic sotto (es. sopra un header, o sopra
            // Fill che ha raycastTarget=false) non genera nessun hit per l'EventSystem, quindi
            // lo ScrollRect non riceve mai OnBeginDrag da li' e sembra "non scrollare" se non
            // parti esattamente sopra una riga/pillola cliccabile.
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            catcher.raycastTarget = true;

            var content = CreateUIObject("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 1342f);
            content.anchoredPosition = Vector2.zero;

            scrollRect.viewport = viewport;
            scrollRect.content = content;

            BuildContent(content, modeRowItems, modeRowLabels, pillItems);

            // Un solo bordo netto (striscia piatta) dava uno "stacco" troppo brusco tra
            // contenuto e sfondo, sia scendendo verso il fondo che salendo verso la cima -
            // sostituito con un gradiente vero (piu' fasce sottili impilate, alpha crescente
            // verso il bordo) sia sotto che sopra il Viewport.
            CreateGradientEdge(viewport, "FadeBottom", atTop: false, scrollRect);
            CreateGradientEdge(viewport, "FadeTop", atTop: true, scrollRect);

            return scrollRect;
        }

        /// <summary>
        /// Indicatore "c'e' altro sopra/sotto" per lo ScrollView: un gradiente vero,
        /// approssimato con N fasce sottili impilate (nessun asset gradiente nel manifest)
        /// con alpha che cresce verso il bordo vero del Viewport (curva quadratica, non
        /// lineare, per una transizione piu' morbida vicino al contenuto). ScrollFadeIndicator
        /// lo fa sparire del tutto quando lo scroll arriva all'estremo corrispondente.
        /// </summary>
        private static void CreateGradientEdge(RectTransform viewport, string name, bool atTop, ScrollRect scrollRect)
        {
            const float totalHeight = 90f;
            const int bandCount = 8;
            const float maxAlpha = 0.85f;

            float anchorY = atTop ? 1f : 0f;
            var container = CreateUIObject(name, viewport);
            SetAnchoredRect(container, new Vector2(0f, anchorY), new Vector2(1f, anchorY), new Vector2(0.5f, anchorY),
                new Vector2(0f, totalHeight), Vector2.zero);

            var fadeColor = Hex("#182838");
            float bandHeight = totalHeight / bandCount;
            for (int i = 0; i < bandCount; i++)
            {
                // t=0 (i=0) -> fascia piu' vicina al contenuto, quasi trasparente.
                // t=1 (i=bandCount-1) -> fascia sul bordo vero, quasi opaca. t^2 invece di
                // t lineare: la transizione resta piu' morbida vicino al contenuto.
                float t = (i + 1f) / bandCount;
                float alpha = maxAlpha * (t * t);

                // Il bordo VERO (piu' opaco) deve stare sull'estremo del Viewport: per
                // FadeBottom (container ancorato in basso, si estende verso l'alto) e'
                // l'indice piu' alto che va vicino al fondo del container; per FadeTop
                // (container ancorato in cima, si estende verso il basso) e' l'indice piu'
                // alto che va vicino alla cima del container - da qui i due ordini opposti.
                float bandY = atTop ? (bandCount - 1 - i) * bandHeight : i * bandHeight;

                var band = CreateUIObject("Band" + i, container);
                SetAnchoredRect(band, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, bandHeight + 1f), new Vector2(0f, -bandY));
                var bandImg = band.gameObject.AddComponent<Image>();
                bandImg.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                bandImg.raycastTarget = false;
            }

            container.gameObject.AddComponent<CanvasGroup>();
            var indicator = container.gameObject.AddComponent<ScrollFadeIndicator>();
            var so = new SerializedObject(indicator);
            so.FindProperty("scrollRect").objectReferenceValue = scrollRect;
            so.FindProperty("fadeNearTop").boolValue = atTop;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildContent(RectTransform content, List<SelectableToggleItem> modeRowItems,
            List<string> modeRowLabels, List<SelectableToggleItem> pillItems)
        {
            CreateSectionHeader(content, "Header_PartitaVeloce", "PARTITA VELOCE", 24f);
            AddModeRow(content, modeRowItems, modeRowLabels, "Row_1v1", "1 vs 1", "Online", 52f, 126f, selected: false);
            AddModeRow(content, modeRowItems, modeRowLabels, "Row_2v2", "2 vs 2", "Online", 194f, 126f, selected: false);
            AddModeRow(content, modeRowItems, modeRowLabels, "Row_1v3", "1 vs 3", "Online", 336f, 126f, selected: false);

            CreateSectionHeader(content, "Header_Allenamento", "ALLENAMENTO (BOT)", 520f);
            AddModeRow(content, modeRowItems, modeRowLabels, "Row_1v1Bot", "1 vs 1 BOT", "Allenamento", 548f, 126f, selected: true);
            AddModeRow(content, modeRowItems, modeRowLabels, "Row_2v2Bot", "2 vs 2 BOT", "Allenamento", 690f, 126f, selected: false);
            AddModeRow(content, modeRowItems, modeRowLabels, "Row_1v3Bot", "1 vs 3 BOT", "Allenamento", 832f, 126f, selected: false);

            CreateSectionHeader(content, "DifficoltaLabel", "Difficoltà", 1006f);
            pillItems.Add(CreateDifficultyPill(content, "Pill_Facile", "Facile", 10f, 1032f, active: false));
            pillItems.Add(CreateDifficultyPill(content, "Pill_Medio", "Medio", 311f, 1032f, active: true));
            pillItems.Add(CreateDifficultyPill(content, "Pill_Difficile", "Difficile", 612f, 1032f, active: false));

            CreateSectionHeader(content, "Header_StanzaPrivata", "STANZA PRIVATA", 1164f);
            // Crea/Entra sono azioni, non modalita' selezionabili (spec §6: non implementarne
            // ancora l'azione) - niente SelectableToggleItem, restano bottoni "inerti".
            CreateModeRow(content, "Row_Crea", "Crea", "Stanza privata", 1192f, 126f, selected: false,
                partOfSelectionGroup: false, xOffset: RowMargin, width: 434f);
            CreateModeRow(content, "Row_Entra", "Entra", "Stanza privata", 1192f, 126f, selected: false,
                partOfSelectionGroup: false, xOffset: RowMargin + 434f + 20f, width: 434f);
        }

        private static void AddModeRow(RectTransform content, List<SelectableToggleItem> modeRowItems,
            List<string> modeRowLabels, string name, string title, string subtitle, float y, float height, bool selected)
        {
            modeRowItems.Add(CreateModeRow(content, name, title, subtitle, y, height, selected));
            modeRowLabels.Add(title);
        }

        private static void CreateSectionHeader(RectTransform content, string name, string text, float y)
        {
            var header = CreateUIObject(name, content);
            SetTopLeft(header, RowMargin, y, ContentWidth - RowMargin * 2f, 40f);
            AddText(header, text, 24f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.Left);
        }

        /// <summary>
        /// Riga modalita' riutilizzabile (ModeRow nella spec). xOffset/width di default
        /// coprono il caso "larghezza piena" (10px di margine, 888px di larghezza); Crea/Entra
        /// passano i propri xOffset/width per stare affiancate a meta' ciascuna.
        /// partOfSelectionGroup=false salta la creazione del CheckIcon/SelectableToggleItem
        /// (Crea/Entra sono bottoni azione, non voci selezionabili).
        /// </summary>
        private static SelectableToggleItem CreateModeRow(RectTransform content, string name, string title, string subtitle,
            float y, float height, bool selected, bool partOfSelectionGroup = true,
            float xOffset = RowMargin, float width = ContentWidth - RowMargin * 2f)
        {
            var row = CreateUIObject(name, content);
            SetTopLeft(row, xOffset, y, width, height);
            var selectedSprite = LoadSprite("btn_teal");
            var unselectedSprite = LoadSprite("btn_blue_long");
            var bg = row.gameObject.AddComponent<Image>();
            bg.sprite = selected ? selectedSprite : unselectedSprite;
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;
            row.gameObject.AddComponent<Button>().targetGraphic = bg;

            float iconSize = height * 0.5f;
            var icon = CreateUIObject("Icon", row);
            SetAnchoredRect(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(iconSize, iconSize), new Vector2(30f, 0f));
            AddSpriteImage(icon, "ic_gamepad", preserveAspect: true);

            float textX = 30f + iconSize + 24f;
            float textWidth = width - textX - (partOfSelectionGroup ? height * 0.44f + 26f + 12f : 20f);

            var titleRt = CreateUIObject("TitleText", row);
            SetAnchoredRect(titleRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(textWidth, 34f), new Vector2(textX, -20f));
            AddText(titleRt, title, 28f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Left);

            var subRt = CreateUIObject("SubText", row);
            SetAnchoredRect(subRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(textWidth, 28f), new Vector2(textX, -64f));
            AddText(subRt, subtitle, 19f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Left);

            if (!partOfSelectionGroup)
            {
                return null;
            }

            float checkSize = height * 0.44f;
            var check = CreateUIObject("CheckIcon", row);
            SetAnchoredRect(check, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(checkSize, checkSize), new Vector2(-26f, 0f));
            AddSpriteImage(check, "ic_check", preserveAspect: true);
            check.gameObject.SetActive(selected);

            var toggle = row.gameObject.AddComponent<SelectableToggleItem>();
            var so = new SerializedObject(toggle);
            so.FindProperty("background").objectReferenceValue = bg;
            so.FindProperty("selectedSprite").objectReferenceValue = selectedSprite;
            so.FindProperty("unselectedSprite").objectReferenceValue = unselectedSprite;
            so.FindProperty("checkIcon").objectReferenceValue = check.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            return toggle;
        }

        private static SelectableToggleItem CreateDifficultyPill(RectTransform content, string name, string label,
            float x, float y, bool active)
        {
            var pill = CreateUIObject(name, content);
            SetTopLeft(pill, x, y, 285f, 76f);
            var activeSprite = LoadSprite("btn_green_small");
            var inactiveSprite = LoadSprite("btn_gray_small");
            var bg = pill.gameObject.AddComponent<Image>();
            bg.sprite = active ? activeSprite : inactiveSprite;
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;
            pill.gameObject.AddComponent<Button>().targetGraphic = bg;

            var textRt = CreateUIObject("Text", pill);
            StretchFill(textRt);
            AddText(textRt, label, 25f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);

            var toggle = pill.gameObject.AddComponent<SelectableToggleItem>();
            var so = new SerializedObject(toggle);
            so.FindProperty("background").objectReferenceValue = bg;
            so.FindProperty("selectedSprite").objectReferenceValue = activeSprite;
            so.FindProperty("unselectedSprite").objectReferenceValue = inactiveSprite;
            so.FindProperty("checkIcon").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            return toggle;
        }

        // ------------------------------------------------------------------
        // Contesto: manifest sprite + tema (stessa logica di HomeScreenBuilder)
        // ------------------------------------------------------------------

        private static bool LoadContext()
        {
            _theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (_theme == null)
            {
                Debug.LogError($"[PanelModalitaBuilder] UITheme non trovato in {ThemePath}.");
                return false;
            }

            return true;
        }

        // ------------------------------------------------------------------
        // Helper generici
        // ------------------------------------------------------------------

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

        /// <summary>
        /// Ancora rt all'angolo in alto a sinistra del parent, con box (x0,y0) -> (x0+w,y0+h)
        /// nel sistema Y-verso-il-basso della spec (stesso sistema del mockup Python).
        /// </summary>
        private static void SetTopLeft(RectTransform rt, float x0, float y0, float w, float h)
        {
            SetAnchoredRect(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(w, h), new Vector2(x0, -y0));
        }

        /// <summary>
        /// Come SetTopLeft ma centrato orizzontalmente rispetto al parent (per elementi
        /// "centrato X" della spec, es. TitleRibbon).
        /// </summary>
        private static void SetTopCenter(RectTransform rt, float y0, float w, float h)
        {
            SetAnchoredRect(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(w, h), new Vector2(0f, -y0));
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
            if (sliced)
            {
                image.type = Image.Type.Sliced;
            }

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
