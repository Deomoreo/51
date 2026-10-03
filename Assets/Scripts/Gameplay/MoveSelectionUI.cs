using System;
using System.Collections.Generic;
using DG.Tweening;
using Project51.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Pannello "Scegli la presa" quando la stessa carta puo' prendere gruppi di carte diversi.
    /// Ogni opzione mostra le miniature delle carte che prende: tenendo premuto (o passandoci sopra
    /// col mouse) le carte si alzano sul tavolo, rilasciando sull'opzione si gioca la mossa.
    /// Il vassoio UI51 lo costruisce Tools/UI51/Build Fase 5 (Tavolo 1v1); qui solo riempimento e animazione.
    /// Sostituisce i vecchi rettangoli di testo e i quadratini gialli che restavano a schermo.
    /// </summary>
    public class MoveSelectionUI : MonoBehaviour
    {
        // Optional presentation hook; Gameplay cannot reference the UI assembly.
        public static event Action<TMP_Text> TextCreated;
        public struct CaptureChoice
        {
            public string Title;
            public string Detail;
            public List<Sprite> Cards;
            public int Denari;                // carte di denari prese (chip con la moneta)
            public bool Scopa;                // prende tutto il tavolo (chip SCOPA)
            public List<Renderer> TableCards; // le carte sul tavolo: anelli e numeri del vassoio UI51
        }

        /// <summary>Colori delle prese del mockup (oro, verde acqua, rosa, viola); dalla quinta si riparte, i numeri restano unici.</summary>
        public static readonly Color[] OptionColors =
        {
            new Color32(243, 201, 105, 255), new Color32(79, 209, 197, 255), new Color32(240, 138, 141, 255), new Color32(167, 139, 250, 255),
        };

        public static Color OptionColor(int index) => OptionColors[index % OptionColors.Length];

        /// <summary>Scala della fila delle prese: intera se entra, fino al 75% per farla entrare, oltre si scorre.</summary>
        public static float OptionsScale(float rowWidth, float viewportWidth) =>
            rowWidth <= 0f ? 1f : Mathf.Clamp(viewportWidth / rowWidth, 0.75f, 1f);

        /// <summary>
        /// Per ogni carta del tavolo (in ordine di prima comparsa) le prese che la contengono, in ordine: la prima da'
        /// il colore dell'anello e il tocco sulla carta, tutte danno un numero.
        /// </summary>
        public static List<KeyValuePair<T, List<int>>> OptionsPerItem<T>(IReadOnlyList<IReadOnlyList<T>> options)
        {
            var result = new List<KeyValuePair<T, List<int>>>();
            var at = new Dictionary<T, int>();
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i] == null) continue;
                foreach (var item in options[i])
                {
                    if (item == null) continue;
                    if (!at.TryGetValue(item, out int k))
                    {
                        at[item] = k = result.Count;
                        result.Add(new KeyValuePair<T, List<int>>(item, new List<int>()));
                    }
                    if (!result[k].Value.Contains(i)) result[k].Value.Add(i);
                }
            }
            return result;
        }

        [Header("Legacy (nascosti, restano per compatibilita' con la scena)")]
        [SerializeField] private RectTransform container;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;

        [Header("Grafica V2")]
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Sprite panelFill;
        [SerializeField] private Sprite panelRing;
        [SerializeField] private Sprite rowFill;
        [SerializeField] private Sprite rowRing;
        [SerializeField] private Sprite closeBackground;
        [SerializeField] private Sprite closeIcon;

        [Header("UI51 Fase 5: vassoio del mockup Partita (Tools/UI51/Build Fase 5). Vuoti = pannello V2 qui sopra")]
        [SerializeField] private CanvasGroup ui51Tray;
        [SerializeField] private RectTransform traySheet;   // scala Unit, alto quanto serve (bordo alto 198 sopra il mio banner)
        [SerializeField] private RectTransform trayAnchor;  // Banner_Local
        [SerializeField] private ScrollRect trayScroll;     // content = fila delle prese, figlio 0 = modello Choice
        [SerializeField] private RectTransform trayMarks;   // anelli e numeri sulle carte del tavolo, figlio 0 = modello UI51Mark
        [SerializeField] private Image trayPlayedCard;

        private static readonly Color SheetColor = new Color32(17, 32, 51, 250);
        private static readonly Color Gold = new Color32(232, 178, 74, 255);
        private static readonly Color RowColor = new Color32(26, 44, 68, 255);
        private static readonly Color RowHoverColor = new Color32(38, 62, 94, 255);
        private static readonly Color RowBorder = new Color32(58, 86, 128, 255);
        private static readonly Color SoftText = new Color32(170, 190, 215, 255);

        private const float SheetWidth = 1000f;
        private const float HeaderHeight = 88f;
        private const float RowHeight = 124f;
        private const float RowGap = 12f;
        private const float BottomPadding = 24f;
        private const float ThumbWidth = 64f;
        private const float ThumbHeight = 96f;

        private RectTransform sheet;
        private CanvasGroup sheetGroup;
        private RectTransform rowsRoot;
        private TMP_Text sheetTitle;
        private TMP_Text toast;
        private readonly List<(Image fill, Image ring)> rows = new List<(Image, Image)>();
        private Action<int> previewCallback;
        private Action cancelCallback;
        private int previewedIndex = -1;

        // Vassoio UI51
        private Action<int> chooseCallback;
        private readonly List<Renderer> markTargets = new List<Renderer>();
        private readonly List<int> markFirst = new List<int>();
        private readonly List<int> markBadges = new List<int>();
        private float openedAt;
        private int releasedFrame = -1;

        public bool IsVisible => gameObject.activeInHierarchy &&
            (ui51Tray != null ? ui51Tray.gameObject.activeSelf : sheet != null && sheet.gameObject.activeSelf);

        /// <summary>X del vassoio, tocco su un'altra carta, cambio di turno: chiude e avvisa chi l'ha aperto.</summary>
        public void Cancel()
        {
            var cb = cancelCallback;
            Hide();
            cb?.Invoke();
        }

        private void Choose(int index)
        {
            // Il suono lo aggancia GameAudio una volta al secondo: una copia appena creata puo' non averlo ancora.
            GameAudio.PlayUi(SoundId.UiClick);
            var cb = chooseCallback;
            Hide();
            cb?.Invoke(index);
        }

        private void Update()
        {
            if (ui51Tray == null || ui51Tray.interactable || !ui51Tray.gameObject.activeInHierarchy) return;
            // Si arma dopo il rilascio del tocco che l'ha aperto e dopo il tempo del doppio tocco: il secondo tocco di un
            // doppio tocco non sceglie una presa. Un dito rimasto giu' (secondo dito) non lo blocca oltre il secondo.
            float age = Time.unscaledTime - openedAt;
            if (Input.GetMouseButton(0) || Input.touchCount > 0) releasedFrame = -1;
            else if (releasedFrame < 0) releasedFrame = Time.frameCount;
            if (age >= 1f || age >= 0.35f && releasedFrame >= 0 && Time.frameCount > releasedFrame) ui51Tray.interactable = true;
        }

        private void LateUpdate()
        {
            if (ui51Tray == null || !ui51Tray.gameObject.activeInHierarchy || trayMarks == null) return;
            // Gli anelli seguono le carte (salita, scala, refresh): angoli dello sprite nel mondo -> schermo -> Marks.
            var cam = Camera.main;
            for (int i = 0; i < markTargets.Count; i++)
            {
                var mark = (RectTransform)trayMarks.GetChild(i + 1);
                var r = markTargets[i];
                bool show = cam != null && r != null && r.enabled && r.gameObject.activeInHierarchy;
                if (mark.gameObject.activeSelf != show) mark.gameObject.SetActive(show);
                if (!show) continue;
                var b = r.bounds;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(trayMarks, cam.WorldToScreenPoint(b.min), null, out var min);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(trayMarks, cam.WorldToScreenPoint(b.max), null, out var max);
                mark.anchoredPosition = (min + max) * 0.5f;
                mark.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
                // Numeri impilati (20 + 2 l'uno): sulle carte piccole dei 4 giocatori la pila si stringe per restare nella carta.
                float fit = Mathf.Min(1f, (mark.sizeDelta.y - 6f) / (22f * markBadges[i] - 2f));
                mark.Find("Badges").localScale = new Vector3(fit, fit, 1f);
            }
        }

        private void Awake()
        {
            if (EventSystem.current == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        /// <summary>Opzioni di presa con miniature. onPreview riceve -1 quando nessuna e' evidenziata.</summary>
        public void ShowCaptureChoices(IList<CaptureChoice> choices, Action<int> onChoose, Action<int> onPreview, Action onCancel, string title = "SCEGLI LA PRESA",
            Sprite playedCard = null, Sprite cardBack = null)
        {
            if (choices == null || choices.Count == 0) return;

            gameObject.SetActive(true);
            EnsureBuilt(); // prima di misurare: stende la radice e le da' il Canvas 550
            ClearRows();
            previewCallback = onPreview;
            cancelCallback = onCancel;
            if (ui51Tray != null && playedCard != null) // i vecchi chiamanti solo testo (trascinamento con la matta) restano sul pannello V2
            {
                ShowTray(choices, onChoose, playedCard, cardBack);
                return;
            }
            sheetTitle.text = title;

            for (int i = 0; i < choices.Count; i++)
            {
                CreateRow(i, choices[i], onChoose);
            }

            float height = HeaderHeight + choices.Count * (RowHeight + RowGap) - RowGap + BottomPadding;
            sheet.sizeDelta = new Vector2(SheetWidth, height);
            sheet.gameObject.SetActive(true);
            GameAudio.PlayUi(SoundId.PopupOpen);

            // DOTween.To generico: i moduli UI/TMP di DOTween non sono referenziati da questo assembly.
            DOTween.Kill(sheet);
            sheet.anchoredPosition = new Vector2(0f, -30f);
            sheetGroup.alpha = 0f;
            DOTween.To(() => sheet.anchoredPosition.y, y => sheet.anchoredPosition = new Vector2(0f, y), 24f, 0.18f).SetEase(Ease.OutQuad).SetTarget(sheet);
            DOTween.To(() => sheetGroup.alpha, a => sheetGroup.alpha = a, 1f, 0.18f).SetTarget(sheet);
        }

        public void ShowInvalid(string message, float duration = 1.5f)
        {
            gameObject.SetActive(true);
            EnsureBuilt();
            GameAudio.PlayUi(SoundId.UiError);
            toast.text = message;
            DOTween.Kill(toast);
            toast.gameObject.SetActive(true);
            toast.alpha = 1f;
            DOTween.To(() => toast.alpha, a => toast.alpha = a, 0f, 0.3f).SetDelay(duration).SetTarget(toast)
                .OnComplete(() => toast.gameObject.SetActive(false));
        }

        public void Hide()
        {
            SetPreview(-1);
            ClearRows();
            previewCallback = null;
            cancelCallback = null;
            chooseCallback = null;
            if (sheet != null)
            {
                DOTween.Kill(sheet);
                sheet.gameObject.SetActive(false);
            }
            if (ui51Tray != null)
            {
                if (traySheet != null) DOTween.Kill(traySheet);
                markTargets.Clear();
                ui51Tray.gameObject.SetActive(false);
            }
            bool toastVisible = toast != null && toast.gameObject.activeSelf;
            if (!toastVisible) gameObject.SetActive(false);
        }

        private void EnsureBuilt()
        {
            if (sheet != null) return;

            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            // Sopra banner e pulsanti del GameCanvas (che vengono dopo nella gerarchia), sotto
            // al canvas di presentazione V2 (ordine 600: risultati, emoticon).
            var overlay = gameObject.GetComponent<Canvas>();
            if (overlay == null) overlay = gameObject.AddComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = 550;
            if (gameObject.GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
            if (titleText != null) titleText.gameObject.SetActive(false);
            if (messageText != null) messageText.gameObject.SetActive(false);
            if (container != null) container.gameObject.SetActive(false);
            if (font == null && titleText != null) font = titleText.font;

            sheet = NewRect("CaptureSheet", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(SheetWidth, 300f));
            sheet.pivot = new Vector2(0.5f, 0f);
            sheetGroup = sheet.gameObject.AddComponent<CanvasGroup>();
            AddSliced(sheet, "Fill", panelFill, SheetColor, 60f / 24f, raycast: true);
            AddSliced(sheet, "Ring", panelRing, Gold, 60f / 24f, raycast: false);

            sheetTitle = AddText(sheet, "Title", "SCEGLI LA PRESA", 34f, Gold, TextAlignmentOptions.MidlineLeft);
            var titleRect = sheetTitle.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(36f, -HeaderHeight);
            titleRect.offsetMax = new Vector2(-110f, 0f);

            var close = NewRect("Close", sheet, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(64f, 64f));
            close.anchoredPosition = new Vector2(-50f, -44f);
            var closeImage = AddSliced(close, "Background", closeBackground, closeBackground != null ? Color.white : RowBorder, 1f, raycast: true);
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(() =>
            {
                var cancel = cancelCallback;
                Hide();
                cancel?.Invoke();
            });
            var icon = NewRect("Icon", close, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34f, 34f)).gameObject.AddComponent<Image>();
            icon.sprite = closeIcon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            if (closeIcon == null) icon.enabled = false;

            rowsRoot = NewRect("Rows", sheet, Vector2.zero, Vector2.one, Vector2.zero);
            rowsRoot.offsetMin = new Vector2(24f, BottomPadding);
            rowsRoot.offsetMax = new Vector2(-24f, -HeaderHeight);

            toast = AddText(root, "Toast", string.Empty, 30f, new Color32(255, 120, 110, 255), TextAlignmentOptions.Center);
            var toastRect = toast.rectTransform;
            toastRect.anchorMin = toastRect.anchorMax = new Vector2(0.5f, 0f);
            toastRect.sizeDelta = new Vector2(900f, 60f);
            toastRect.anchoredPosition = new Vector2(0f, 560f);
            toast.gameObject.SetActive(false);
            sheet.gameObject.SetActive(false); // altrimenti il primo ShowInvalid mostra il pannello vuoto
        }

        /// <summary>Vassoio del mockup: carta giocata, prese in fila (numero, colore, carte, chip), anelli e numeri sul tavolo.</summary>
        private void ShowTray(IList<CaptureChoice> choices, Action<int> onChoose, Sprite playedCard, Sprite cardBack)
        {
            chooseCallback = onChoose;
            ui51Tray.gameObject.SetActive(true); // prima del layout: le file spente non si misurano
            ui51Tray.interactable = false;       // si arma in Update
            openedAt = Time.unscaledTime;
            releasedFrame = -1;
            if (trayPlayedCard != null) trayPlayedCard.sprite = playedCard;

            var options = trayScroll.content;
            var template = options.GetChild(0);
            template.gameObject.SetActive(false);
            for (int i = 0; i < Mathf.Max(choices.Count, options.childCount - 1); i++)
            {
                Transform option;
                if (i + 1 < options.childCount) option = options.GetChild(i + 1);
                else
                {
                    option = Instantiate(template, options);
                    int k = i;
                    // Una volta sola: RemoveAllListeners toglierebbe anche il suono di GameAudio.
                    option.GetComponent<Button>().onClick.AddListener(() => Choose(k));
                }
                option.gameObject.SetActive(i < choices.Count);
                if (i < choices.Count) FillOption(option, i, choices[i], cardBack);
            }

            // Fila intera se entra, fino al 75% per farla entrare, oltre scorre col dito (scelta dell'utente, 30/09).
            LayoutRebuilder.ForceRebuildLayoutImmediate(options);
            var viewport = trayScroll.viewport != null ? trayScroll.viewport : (RectTransform)trayScroll.transform;
            float width = viewport.rect.width, row = options.rect.width, scale = OptionsScale(row, width);
            options.localScale = new Vector3(scale, scale, 1f);
            bool scroll = row * scale > width + 0.5f;
            // Acceso solo se serve: anche fermo resterebbe il bersaglio del trascinamento e un tocco che si muove di 10 px
            // (mezzo millimetro su iPhone) non sceglierebbe la presa.
            trayScroll.enabled = scroll;
            trayScroll.horizontal = scroll;
            trayScroll.StopMovement();
            options.anchoredPosition = new Vector2(scroll ? 0f : (width - row * scale) * 0.5f, options.anchoredPosition.y);

            // Bordo alto 198 sopra il centro del mio banner, come nel mockup (Partita e Partita4).
            float unit = traySheet.localScale.x, height = 241f;
            var tray = (RectTransform)ui51Tray.transform;
            if (trayAnchor != null)
            {
                var center = tray.InverseTransformPoint(trayAnchor.TransformPoint(trayAnchor.rect.center));
                height = (center.y + 198f * unit - tray.rect.yMin) / unit;
            }
            traySheet.sizeDelta = new Vector2(traySheet.sizeDelta.x, height);

            ShowMarks(choices);

            GameAudio.PlayUi(SoundId.PopupOpen);
            // Sale dal basso in 0,28 s (SheetUp del mockup); scegliendo o annullando si chiude di colpo, come nel mockup.
            DOTween.Kill(traySheet);
            traySheet.anchoredPosition = new Vector2(0f, -height * unit);
            DOTween.To(() => traySheet.anchoredPosition.y, y => traySheet.anchoredPosition = new Vector2(0f, y), 0f, GamePreferences.Scaled(0.28f))
                .SetEase(Ease.OutQuart).SetTarget(traySheet);
        }

        private static void FillOption(Transform option, int index, CaptureChoice choice, Sprite cardBack)
        {
            for (int f = 0; f < OptionColors.Length; f++)
            {
                var frame = option.Find("Frame" + f);
                if (frame != null) frame.gameObject.SetActive(f == index % OptionColors.Length);
            }
            var badge = option.Find("Badge");
            badge.GetComponent<Graphic>().color = OptionColor(index); // UI51Shape: il colore tinge solo il riempimento
            badge.GetComponentInChildren<TMP_Text>(true).text = (index + 1).ToString();

            var cards = option.Find("Cards");
            int n = choice.Cards?.Count ?? 0;
            for (int c = 0; c < Mathf.Max(n, cards.childCount); c++)
            {
                var card = c < cards.childCount ? cards.GetChild(c) : Instantiate(cards.GetChild(0), cards);
                card.gameObject.SetActive(c < n);
                if (c < n) card.Find("Face").GetComponent<Image>().sprite = choice.Cards[c];
            }
            SetChip(option.Find("Chips/Count"), true, "+" + n, cardBack);
            SetChip(option.Find("Chips/Denari"), choice.Denari > 0, choice.Denari.ToString(), null);
            SetChip(option.Find("Chips/Scopa"), choice.Scopa, null, null);
        }

        private static void SetChip(Transform chip, bool show, string text, Sprite back)
        {
            chip.gameObject.SetActive(show);
            if (!show) return;
            if (text != null) chip.GetComponentInChildren<TMP_Text>(true).text = text;
            var backImage = back != null ? chip.Find("Back") : null;
            if (backImage != null) backImage.GetComponent<Image>().sprite = back;
        }

        /// <summary>Un anello per carta del tavolo, colore della prima presa che la contiene, un numero per presa.</summary>
        private void ShowMarks(IList<CaptureChoice> choices)
        {
            markTargets.Clear();
            markFirst.Clear();
            markBadges.Clear();
            if (trayMarks == null || trayMarks.childCount == 0) return;
            var lists = new List<IReadOnlyList<Renderer>>();
            foreach (var choice in choices) lists.Add(choice.TableCards);
            var perCard = OptionsPerItem<Renderer>(lists);

            var template = trayMarks.GetChild(0);
            template.gameObject.SetActive(false);
            for (int i = 0; i < Mathf.Max(perCard.Count, trayMarks.childCount - 1); i++)
            {
                Transform mark;
                if (i + 1 < trayMarks.childCount) mark = trayMarks.GetChild(i + 1);
                else
                {
                    mark = Instantiate(template, trayMarks);
                    int k = i;
                    mark.GetComponent<Button>().onClick.AddListener(() => Choose(markFirst[k])); // tocco sulla carta = sua prima presa
                }
                mark.gameObject.SetActive(false); // lo accende LateUpdate, dopo averlo messo sulla carta
                if (i >= perCard.Count) continue;

                var options = perCard[i].Value;
                markTargets.Add(perCard[i].Key);
                markFirst.Add(options[0]);
                markBadges.Add(options.Count);
                for (int f = 0; f < OptionColors.Length; f++)
                {
                    var ring = mark.Find("Ring" + f);
                    if (ring != null) ring.gameObject.SetActive(f == options[0] % OptionColors.Length);
                }
                var badges = mark.Find("Badges");
                for (int b = 0; b < Mathf.Max(options.Count, badges.childCount); b++)
                {
                    var badge = b < badges.childCount ? badges.GetChild(b) : Instantiate(badges.GetChild(0), badges);
                    badge.gameObject.SetActive(b < options.Count);
                    if (b >= options.Count) continue;
                    badge.GetComponent<Graphic>().color = OptionColor(options[b]);
                    badge.GetComponentInChildren<TMP_Text>(true).text = (options[b] + 1).ToString();
                }
            }
        }

        private void CreateRow(int index, CaptureChoice choice, Action<int> onChoose)
        {
            var row = NewRect("Choice" + index, rowsRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(0f, RowHeight);
            row.anchoredPosition = new Vector2(0f, -index * (RowHeight + RowGap));

            var fill = AddSliced(row, "Fill", rowFill, RowColor, 48f / 20f, raycast: true);
            var ring = AddSliced(row, "Ring", rowRing, RowBorder, 48f / 20f, raycast: false);
            rows.Add((fill, ring));

            var title = AddText(row, "Title", choice.Title ?? string.Empty, 30f, Color.white, TextAlignmentOptions.BottomLeft);
            PlaceLabel(title.rectTransform, 0.5f, 1f, 4f);
            var detail = AddText(row, "Detail", choice.Detail ?? string.Empty, 22f, SoftText, TextAlignmentOptions.TopLeft);
            PlaceLabel(detail.rectTransform, 0f, 0.5f, -4f);
            detail.fontStyle = FontStyles.Normal;

            int count = choice.Cards?.Count ?? 0;
            for (int c = 0; c < count; c++)
            {
                var thumb = NewRect("Card" + c, row, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(ThumbWidth, ThumbHeight));
                thumb.anchoredPosition = new Vector2(-24f - ThumbWidth * 0.5f - (count - 1 - c) * (ThumbWidth + 8f), 0f);
                var image = thumb.gameObject.AddComponent<Image>();
                image.sprite = choice.Cards[c];
                image.preserveAspect = true;
                image.raycastTarget = false;
            }

            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                Hide();
                onChoose?.Invoke(index);
            });

            var trigger = row.gameObject.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerEnter, () => SetPreview(index));
            AddTrigger(trigger, EventTriggerType.PointerDown, () => SetPreview(index));
            AddTrigger(trigger, EventTriggerType.PointerExit, () => { if (previewedIndex == index) SetPreview(-1); });
        }

        private void SetPreview(int index)
        {
            if (previewedIndex == index) return;
            previewedIndex = index;
            for (int i = 0; i < rows.Count; i++)
            {
                bool on = i == index;
                rows[i].fill.color = on ? RowHoverColor : RowColor;
                rows[i].ring.color = on ? Gold : RowBorder;
            }
            previewCallback?.Invoke(index);
        }

        private void ClearRows()
        {
            previewedIndex = -1;
            rows.Clear();
            if (rowsRoot == null) return;
            for (int i = rowsRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(rowsRoot.GetChild(i).gameObject);
            }
        }

        private static void PlaceLabel(RectTransform rect, float anchorMinY, float anchorMaxY, float offsetY)
        {
            rect.anchorMin = new Vector2(0f, anchorMinY);
            rect.anchorMax = new Vector2(1f, anchorMaxY);
            rect.offsetMin = new Vector2(28f, offsetY);
            rect.offsetMax = new Vector2(-300f, offsetY);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image AddSliced(RectTransform parent, string name, Sprite sprite, Color color, float pixelsPerUnitMultiplier, bool raycast)
        {
            var rect = NewRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (sprite != null)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            }
            return image;
        }

        private TMP_Text AddText(Transform parent, string name, string value, float size, Color color, TextAlignmentOptions alignment)
        {
            var rect = NewRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            TextCreated?.Invoke(text);
            return text;
        }
    }
}
