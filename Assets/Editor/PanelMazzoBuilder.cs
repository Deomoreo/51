using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce l'overlay "Panel Mazzo" (Assets/UI_SPEC_PanelMazzo.md) come figlio
    /// disattivato del Canvas nella scena correntemente aperta (pensato per HomeScreen.unity).
    /// Stessa architettura di PanelModalitaBuilder (stesso pattern gia' testato e funzionante):
    /// PanelFrame ancorato al CENTRO dell'overlay (non al bordo, il Canvas reale raramente
    /// misura esattamente 1080x1920 in unita' locali), tutto il resto dentro PanelFrame con
    /// coordinate PanelFrame-relative, RectMask2D (non Mask+Image) per il clipping dello
    /// ScrollView, gradiente vero (fasce impilate) sopra/sotto invece di una striscia piatta,
    /// SelectableToggleGroup/Item per la selezione, un controller su un GameObject sempre
    /// attivo per il collegamento con Home. File auto-contenuto (non condivide helper con
    /// HomeScreenBuilder/PanelModalitaBuilder) per non accoppiare i tre Editor tool fra loro.
    ///
    /// SOLO Mazzo (Accuso/Emoticon vivono nella pagina CARTE dedicata, vedi DeckPageBuilder -
    /// un tentativo precedente li aveva messi qui per errore, la scorciatoia su Home deve
    /// restare esclusivamente per la scelta rapida del mazzo).
    /// </summary>
    public static class PanelMazzoBuilder
    {
        private const string ScenePath = "Assets/Scenes/HomeScreen.unity";
        private const string ThemePath = "Assets/Resources/DragonsHoardTheme.asset";

        // Box assoluto del PanelFrame nel sistema di riferimento 1080x1920 della spec.
        private const float FrameX0 = 44f;
        private const float FrameY0 = 210f;
        private const float FrameX1 = 1036f;
        private const float FrameY1 = 1680f;
        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;

        // Aspect ratio reale dello sprite card_back_green (151x219), usata per calcolare la
        // larghezza delle carte del ventaglio dalla sola altezza data in spec (300px).
        private const float CardAspect = 151f / 219f;

        private static readonly string[] DeckNames =
        {
            "Napoletano", "Classico", "Reale",
            "Smeraldo", "Antico", "Drago",
            "Notturno", "Oro", "Rubino",
        };

        private static UITheme _theme;

        [MenuItem("Tools/Dragons Hoard/Build Panel Mazzo")]
        private static void Build()
        {
            if (!LoadContext())
            {
                return;
            }

            // Forza SEMPRE HomeScreen.unity invece di operare su "qualunque scena hai aperta in
            // quel momento" - senza questo, lanciare il tool con un'altra scena aperta (es.
            // GameScene) costruisce il pannello nel posto sbagliato (bug reale gia' capitato).
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[PanelMazzoBuilder] Nessun Canvas trovato in HomeScreen.unity.");
                return;
            }

            var canvasRect = (RectTransform)canvas.transform;

            // Ripulisce eventuali residui di tentativi precedenti scartati (pannello
            // "Personalizza" separato, poi tab Accuso/Emoticon infilate qui per errore).
            DestroyIfExists(canvasRect, "PanelPersonalizzaOverlay");
            DestroyIfExists(canvasRect, "PanelPersonalizzaManager");
            RemoveButtonFromRightRail(canvasRect, "PersonalizzaButton");

            DestroyIfExists(canvasRect, "PanelMazzoOverlay");
            DestroyIfExists(canvasRect, "PanelMazzoManager");

            var overlay = CreateUIObject("PanelMazzoOverlay", canvasRect);
            StretchFill(overlay);
            var overlayCanvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();

            CreateDimBackground(overlay);
            var panelFrame = CreatePanelFrame(overlay);
            CreateTitleRibbon(panelFrame);
            var closeButton = CreateCloseButton(panelFrame);

            var fanCardArt = CreatePreviewBox(panelFrame, out var captionText);

            var deckItems = new List<SelectableToggleItem>();
            var deckLabels = new List<string>();
            var scrollRect = CreateScrollView(panelFrame, deckItems, deckLabels);

            var confirmButton = CreateConfirmButton(panelFrame);

            var deckGroup = WireSelectionGroup(overlay, deckItems);
            CreateController(canvasRect, overlay, overlayCanvasGroup, closeButton, confirmButton,
                scrollRect, deckGroup, fanCardArt, captionText, deckLabels);

            overlay.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[PanelMazzoBuilder] PanelMazzoOverlay creato in HomeScreen.unity. Salvato.");
        }

        private static void DestroyIfExists(RectTransform canvasRect, string name)
        {
            var existing = canvasRect.Find(name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static void RemoveButtonFromRightRail(RectTransform canvasRect, string buttonName)
        {
            var rail = FindDeepChild(canvasRect, "RightRail");
            if (rail == null) return;

            var button = FindDeepChild(rail, buttonName);
            if (button != null)
            {
                UnityEngine.Object.DestroyImmediate(button.gameObject);
            }
        }

        // ------------------------------------------------------------------
        // Wiring: selezione mazzi, controller Home<->Panel
        // ------------------------------------------------------------------

        private static SelectableToggleGroup WireSelectionGroup(RectTransform overlay, List<SelectableToggleItem> deckItems)
        {
            var groupGO = CreateUIObject("DeckSelectionGroup", overlay).gameObject;
            var group = groupGO.AddComponent<SelectableToggleGroup>();
            var so = new SerializedObject(group);
            AssignList(so, "items", deckItems);
            // Napoletano e' l'unico sbloccato -> unico item nella lista filtrata -> indice 0.
            so.FindProperty("defaultSelectedIndex").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            return group;
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

        private static void CreateController(RectTransform canvasRect, RectTransform overlay, CanvasGroup overlayCanvasGroup,
            Button closeButton, Button confirmButton, ScrollRect scrollRect, SelectableToggleGroup deckGroup,
            Image[] fanCardArt, TMP_Text captionText, List<string> deckLabels)
        {
            var deckSelectorTransform = FindDeepChild(canvasRect, "DeckSelector");
            var openButton = deckSelectorTransform?.GetComponent<Button>();
            var deckSelectorValueText = deckSelectorTransform?.Find("ValueText")?.GetComponent<TMP_Text>();
            if (openButton == null)
            {
                Debug.LogWarning("[PanelMazzoBuilder] Non ho trovato un bottone 'DeckSelector' in scena (il selettore Mazzo su Home, creato da HomeScreenBuilder): il pannello non si apre automaticamente, va attivato a mano.");
            }

            var managerGO = CreateUIObject("PanelMazzoManager", canvasRect).gameObject;
            var controller = managerGO.AddComponent<PanelMazzoController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = overlay.gameObject;
            so.FindProperty("canvasGroup").objectReferenceValue = overlayCanvasGroup;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("openButton").objectReferenceValue = openButton;
            so.FindProperty("confirmButton").objectReferenceValue = confirmButton;
            so.FindProperty("scrollRect").objectReferenceValue = scrollRect;
            so.FindProperty("deckGroup").objectReferenceValue = deckGroup;
            so.FindProperty("deckSelectorValueText").objectReferenceValue = deckSelectorValueText;

            var fanProp = so.FindProperty("fanCardArt");
            fanProp.arraySize = fanCardArt.Length;
            for (int i = 0; i < fanCardArt.Length; i++)
            {
                fanProp.GetArrayElementAtIndex(i).objectReferenceValue = fanCardArt[i];
            }

            so.FindProperty("captionText").objectReferenceValue = captionText;

            var labelsProp = so.FindProperty("deckLabels");
            labelsProp.arraySize = deckLabels.Count;
            for (int i = 0; i < deckLabels.Count; i++)
            {
                labelsProp.GetArrayElementAtIndex(i).stringValue = deckLabels[i];
            }

            // Un solo sprite dorso carta esiste nel manifest (card_back_green): oggi ogni
            // mazzo sbloccato lo condivide, ma il campo e' un array (uno slot per mazzo
            // sbloccato) apposta - quando arriveranno sprite dedicati per mazzo basta
            // popolare questo array nel builder, OnDeckSelected in PanelMazzoController
            // gia' li usa dinamicamente senza altre modifiche.
            var cardBackSprite = LoadSprite("card_back_green");
            var spritesProp = so.FindProperty("deckCardBackSprites");
            spritesProp.arraySize = deckLabels.Count;
            for (int i = 0; i < deckLabels.Count; i++)
            {
                spritesProp.GetArrayElementAtIndex(i).objectReferenceValue = cardBackSprite;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // Stato iniziale (indice 0 = Napoletano) applicato subito a mano: il gruppo vive
            // dentro l'overlay disattivato quindi il suo Awake()/Select() di default non
            // girerebbe finche' il pannello non si apre la prima volta (stesso problema visto
            // per il ValueText del ModeSelector in Panel Modalita).
            if (captionText != null && deckLabels.Count > 0)
            {
                captionText.text = deckLabels[0] + " · 40 carte";
            }
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
            img.color = new Color(dimColor.r, dimColor.g, dimColor.b, 220f / 255f);
            img.raycastTarget = true;
        }

        private static RectTransform CreatePanelFrame(RectTransform overlay)
        {
            float w = FrameX1 - FrameX0;
            float h = FrameY1 - FrameY0;
            float boxCenterX = (FrameX0 + FrameX1) * 0.5f;
            float boxCenterY = (FrameY0 + FrameY1) * 0.5f;
            float offsetX = boxCenterX - RefWidth * 0.5f;
            float offsetY = RefHeight * 0.5f - boxCenterY;

            var outer = CreateUIObject("PanelFrame", overlay);
            SetAnchoredRect(outer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(w, h), new Vector2(offsetX, offsetY));
            var outerImg = outer.gameObject.AddComponent<Image>();
            outerImg.color = Hex("#785018");
            outerImg.raycastTarget = true;

            var gold = CreateUIObject("GoldBorder", outer);
            StretchFill(gold);
            gold.offsetMin = new Vector2(14f, 14f);
            gold.offsetMax = new Vector2(-14f, -14f);
            var goldImg = gold.gameObject.AddComponent<Image>();
            goldImg.color = Hex("#E8B24A");
            goldImg.raycastTarget = false;

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
            SetTopCenter(ribbon, 154f - FrameY0, 520f, 188f);
            AddSpriteImage(ribbon, "ribbon_teal", raycastTarget: false, sliced: true);

            var textRt = CreateUIObject("Text", ribbon);
            StretchFill(textRt);
            var text = AddText(textRt, "MAZZO", 40f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
            text.outlineWidth = 0.2f;
            text.outlineColor = Hex("#241608");
        }

        private static Button CreateCloseButton(RectTransform panelFrame)
        {
            var button = CreateUIObject("CloseButton", panelFrame);
            SetTopLeft(button, 932f - FrameX0, 236f - FrameY0, 86f, 86f);
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
        // Riquadro Anteprima: ventaglio 3 carte + didascalia
        // ------------------------------------------------------------------

        private static Image[] CreatePreviewBox(RectTransform panelFrame, out TMP_Text captionText)
        {
            var box = CreateUIObject("PreviewBox", panelFrame);
            SetTopLeft(box, 74f - FrameX0, 340f - FrameY0, 1006f - 74f, 870f - 340f);
            var bg = box.gameObject.AddComponent<Image>();
            bg.color = Hex("#101C2E");
            bg.raycastTarget = false;

            // Bordo sottile 3px: quattro strisce invece di uno sprite dedicato (nessuno nel
            // manifest per un semplice bordo 1px di colore pieno).
            CreateThinBorder(box, Hex("#466E8E"), 3f);

            // Label e' figlio di PreviewBox, quindi le coordinate vanno relative a PreviewBox
            // (assoluta_spec - box_Y0_assoluto = 340), non a PanelFrame.
            var label = CreateUIObject("Label", box);
            SetTopCenter(label, 382f - 340f, 400f, 32f);
            AddText(label, "ANTEPRIMA", 22f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.Center);

            var fanCardArt = CreateCardFan(box);

            var captionRt = CreateUIObject("Caption", box);
            // Y=814 assoluto -> relativo a PreviewBox (Y0=340): 814-340=474
            SetTopCenter(captionRt, 814f - 340f, 800f, 36f);
            captionText = AddText(captionRt, "", 26f, FontStyles.Normal, Hex("#FFECBE"), TextAlignmentOptions.Center);

            return fanCardArt;
        }

        private static void CreateThinBorder(RectTransform parent, Color color, float thickness)
        {
            var top = CreateUIObject("BorderTop", parent);
            SetAnchoredRect(top, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, thickness), Vector2.zero);
            var topImg = top.gameObject.AddComponent<Image>();
            topImg.color = color;
            topImg.raycastTarget = false;

            var bottom = CreateUIObject("BorderBottom", parent);
            SetAnchoredRect(bottom, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, thickness), Vector2.zero);
            var bottomImg = bottom.gameObject.AddComponent<Image>();
            bottomImg.color = color;
            bottomImg.raycastTarget = false;

            var left = CreateUIObject("BorderLeft", parent);
            SetAnchoredRect(left, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(thickness, 0f), Vector2.zero);
            var leftImg = left.gameObject.AddComponent<Image>();
            leftImg.color = color;
            leftImg.raycastTarget = false;

            var right = CreateUIObject("BorderRight", parent);
            SetAnchoredRect(right, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(thickness, 0f), Vector2.zero);
            var rightImg = right.gameObject.AddComponent<Image>();
            rightImg.color = color;
            rightImg.raycastTarget = false;
        }

        /// <summary>
        /// Ventaglio di 3 carte sovrapposte e ruotate. Ritorna le 3 Image "CardArt" (non le
        /// ombre), nello stesso ordine sinistra/centro/destra, per il cambio sprite dinamico
        /// da PanelMazzoController quando si seleziona un mazzo diverso.
        /// </summary>
        private static Image[] CreateCardFan(RectTransform previewBox)
        {
            float cardHeight = 300f;
            float cardWidth = cardHeight * CardAspect;
            // Centro ventaglio assoluto (540,640) -> relativo a PreviewBox (X0=74,Y0=340).
            float centerX = 540f - 74f;
            float centerY = 640f - 340f;

            // Ordine di creazione: sinistra, destra, poi centro per ultimo cosi' la carta
            // centrale (quella "principale") e' disegnata sopra le altre due.
            var left = CreateFanCard(previewBox, "CardLeft", centerX - 150f, centerY, 16f, cardWidth, cardHeight);
            var right = CreateFanCard(previewBox, "CardRight", centerX + 150f, centerY, -16f, cardWidth, cardHeight);
            var center = CreateFanCard(previewBox, "CardCenter", centerX, centerY, 0f, cardWidth, cardHeight);

            return new[] { left, center, right };
        }

        private static Image CreateFanCard(RectTransform previewBox, string name, float centerX, float centerY,
            float rotationZ, float width, float height)
        {
            var slot = CreateUIObject(name, previewBox);
            SetAnchoredRect(slot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(width, height), new Vector2(centerX, -centerY));
            slot.localEulerAngles = new Vector3(0f, 0f, rotationZ);

            // Ombra: stesso sprite, tinta scura a bassa opacita', leggermente spostata -
            // essendo figlia dello slot ruotato eredita la stessa rotazione, resta "attaccata".
            var shadow = CreateUIObject("Shadow", slot);
            SetAnchoredRect(shadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(width, height), new Vector2(8f, -12f));
            var shadowImg = AddSpriteImage(shadow, "card_back_green");
            shadowImg.color = new Color(0f, 0f, 0f, 0.35f);

            var art = CreateUIObject("CardArt", slot);
            SetAnchoredRect(art, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(width, height), Vector2.zero);
            var artImg = AddSpriteImage(art, "card_back_green");

            return artImg;
        }

        // ------------------------------------------------------------------
        // ScrollView: griglia 3x3 di DeckCell
        // ------------------------------------------------------------------

        // GridTopPadding tiene la prima riga di carte sotto la sfumatura FadeTop (48px):
        // senza questo margine, a scroll=0 la sfumatura cadeva sopra l'arte delle carte
        // della prima riga (bug segnalato dall'utente) invece di sfumare solo lo sfondo.
        private const float GridTopPadding = 44f;
        private const float ContentHeight = 858f + GridTopPadding;
        private const float CellWidth = 290f;
        private const float CellHeight = 260f;
        private const float ColStep = 310f;
        private const float RowStep = 286f;

        private static ScrollRect CreateScrollView(RectTransform panelFrame, List<SelectableToggleItem> deckItems,
            List<string> deckLabels)
        {
            var scrollViewRt = CreateUIObject("ScrollView", panelFrame);
            SetTopLeft(scrollViewRt, 74f - FrameX0, 904f - FrameY0, 984f - 74f, 1490f - 904f);
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
            content.sizeDelta = new Vector2(0f, ContentHeight);
            content.anchoredPosition = Vector2.zero;

            scrollRect.viewport = viewport;
            scrollRect.content = content;

            BuildGrid(content, deckItems, deckLabels);

            CreateGradientEdge(viewport, "FadeBottom", atTop: false, scrollRect);
            CreateGradientEdge(viewport, "FadeTop", atTop: true, scrollRect);

            return scrollRect;
        }

        private static void BuildGrid(RectTransform content, List<SelectableToggleItem> deckItems, List<string> deckLabels)
        {
            for (int row = 0; row < 3; row++)
            {
                float y = GridTopPadding + row * RowStep;
                for (int col = 0; col < 3; col++)
                {
                    float x = col * ColStep;
                    int deckIndex = row * 3 + col;
                    string deckName = DeckNames[deckIndex];
                    bool unlocked = deckIndex == 0; // solo Napoletano, per ora (vedi spec §6)
                    bool selected = deckIndex == 0;

                    var item = CreateDeckCell(content, "DeckCell_" + deckName, deckName, x, y, unlocked, selected);
                    if (item != null)
                    {
                        deckItems.Add(item);
                        deckLabels.Add(deckName);
                    }
                }
            }
        }

        /// <summary>
        /// Una cella della griglia mazzi. Ritorna il SelectableToggleItem SOLO se sbloccata
        /// (le celle bloccate non hanno Button ne' item nel gruppo: spec §5, cliccarle non
        /// deve cambiare nulla per ora - niente listener e' piu' semplice/onesto che un
        /// Button che non fa nulla).
        /// </summary>
        private static SelectableToggleItem CreateDeckCell(RectTransform content, string name, string deckName,
            float x, float y, bool unlocked, bool selected)
        {
            var cell = CreateUIObject(name, content);
            SetTopLeft(cell, x, y, CellWidth, CellHeight);

            GameObject glow = null;
            if (unlocked)
            {
                var glowRt = CreateUIObject("Glow", cell);
                SetAnchoredRect(glowRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(CellWidth + 24f, CellHeight + 24f), Vector2.zero);
                var glowImg = AddSpriteImage(glowRt, "sq_gold", sliced: true);
                glowImg.color = new Color(1f, 1f, 1f, 0.85f);
                glow = glowRt.gameObject;
            }

            float cardArtHeight = 170f;
            float cardArtWidth = cardArtHeight * CardAspect;
            var cardArt = CreateUIObject("CardArt", cell);
            SetAnchoredRect(cardArt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(cardArtWidth, cardArtHeight), new Vector2(0f, -12f));
            var cardImg = AddSpriteImage(cardArt, unlocked ? "card_back_green" : "card_frame_dark");
            // "scurito ~60%" per i mazzi bloccati: moltiplica il colore invece di usare uno
            // sprite dedicato (non ce n'e' uno nel manifest per questo).
            cardImg.color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);

            if (!unlocked)
            {
                var lockRt = CreateUIObject("LockIcon", cardArt);
                SetAnchoredRect(lockRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(52f, 52f), Vector2.zero);
                AddSpriteImage(lockRt, "ic_lock", preserveAspect: true);
            }

            var nameRt = CreateUIObject("NameText", cell);
            SetAnchoredRect(nameRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(CellWidth - 12f, 30f), new Vector2(0f, -12f - cardArtHeight - 6f));
            AddText(nameRt, deckName, 21f, FontStyles.Normal, unlocked ? _theme.Cream : _theme.TextMuted, TextAlignmentOptions.Center);

            if (!unlocked)
            {
                return null;
            }

            var checkRt = CreateUIObject("CheckIcon", cell);
            SetAnchoredRect(checkRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(46f, 46f), new Vector2(-6f, -6f));
            AddSpriteImage(checkRt, "ic_check", preserveAspect: true);
            checkRt.gameObject.SetActive(selected);

            var bg = cell.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f);
            bg.raycastTarget = true;
            cell.gameObject.AddComponent<Button>().targetGraphic = bg;

            var toggle = cell.gameObject.AddComponent<SelectableToggleItem>();
            var so = new SerializedObject(toggle);
            so.FindProperty("checkIcon").objectReferenceValue = checkRt.gameObject;
            so.FindProperty("glowObject").objectReferenceValue = glow;
            so.ApplyModifiedPropertiesWithoutUndo();

            return toggle;
        }

        /// <summary>
        /// Stessa tecnica di PanelModalitaBuilder.CreateGradientEdge (gia' testata): N fasce
        /// sottili impilate con alpha crescente verso il bordo vero, invece di una striscia
        /// piatta con uno stacco brusco. Duplicato qui (non condiviso) per lo stesso motivo
        /// per cui questo file non condivide altri helper con PanelModalitaBuilder.
        /// </summary>
        private static void CreateGradientEdge(RectTransform viewport, string name, bool atTop, ScrollRect scrollRect)
        {
            const int bands = 6;
            const float totalHeight = 48f;
            var edge = CreateUIObject(name, viewport);
            edge.anchorMin = new Vector2(0f, atTop ? 1f : 0f);
            edge.anchorMax = new Vector2(1f, atTop ? 1f : 0f);
            edge.pivot = new Vector2(0.5f, atTop ? 1f : 0f);
            edge.sizeDelta = new Vector2(0f, totalHeight);
            edge.anchoredPosition = Vector2.zero;

            var baseColor = Hex("#182838");
            for (int i = 0; i < bands; i++)
            {
                float bandHeight = totalHeight / bands;
                var band = CreateUIObject($"Band_{i}", edge);
                band.anchorMin = new Vector2(0f, atTop ? 1f : 0f);
                band.anchorMax = new Vector2(1f, atTop ? 1f : 0f);
                band.pivot = new Vector2(0.5f, atTop ? 1f : 0f);
                band.sizeDelta = new Vector2(0f, bandHeight);
                band.anchoredPosition = new Vector2(0f, atTop ? -(i * bandHeight) : (i * bandHeight));

                float t = 1f - (float)i / (bands - 1);
                var img = band.gameObject.AddComponent<Image>();
                img.color = new Color(baseColor.r, baseColor.g, baseColor.b, t);
                img.raycastTarget = false;
            }
        }

        // ------------------------------------------------------------------
        // Pulsante conferma "USA QUESTO"
        // ------------------------------------------------------------------

        private static Button CreateConfirmButton(RectTransform panelFrame)
        {
            var button = CreateUIObject("ConfirmButton", panelFrame);
            SetTopLeft(button, 310f - FrameX0, 1540f - FrameY0, 770f - 310f, 1648f - 1540f);
            var bg = AddSpriteImage(button, "btn_gold_long", raycastTarget: true, sliced: true);
            var btn = button.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;

            var textRt = CreateUIObject("Text", button);
            StretchFill(textRt);
            var text = AddText(textRt, "USA QUESTO", 34f, FontStyles.Bold, Hex("#3A2208"), TextAlignmentOptions.Center);
            text.outlineWidth = 0.15f;
            text.outlineColor = Hex("#241608");

            return btn;
        }

        // ------------------------------------------------------------------
        // Contesto: manifest sprite + tema (stessa logica di PanelModalitaBuilder)
        // ------------------------------------------------------------------

        private static bool LoadContext()
        {
            _theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (_theme == null)
            {
                Debug.LogError($"[PanelMazzoBuilder] UITheme non trovato in {ThemePath}.");
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

        private static void SetTopLeft(RectTransform rt, float x0, float y0, float w, float h)
        {
            SetAnchoredRect(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(w, h), new Vector2(x0, -y0));
        }

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
