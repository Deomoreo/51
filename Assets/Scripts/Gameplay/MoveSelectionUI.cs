using System;
using System.Collections.Generic;
using DG.Tweening;
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
    /// La grafica e' costruita qui a runtime con gli sprite assegnati da Tools/UIV2/Build Capture Choice.
    /// Sostituisce i vecchi rettangoli di testo e i quadratini gialli che restavano a schermo.
    /// </summary>
    public class MoveSelectionUI : MonoBehaviour
    {
        public struct CaptureChoice
        {
            public string Title;
            public string Detail;
            public List<Sprite> Cards;
        }

        [Header("Legacy (nascosti, restano per compatibilita' con la scena)")]
        [SerializeField] private RectTransform container;
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Color invalidMessageColor = new Color(1f, 0.3f, 0.3f);

        [Header("Grafica V2")]
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Sprite panelFill;
        [SerializeField] private Sprite panelRing;
        [SerializeField] private Sprite rowFill;
        [SerializeField] private Sprite rowRing;
        [SerializeField] private Sprite closeBackground;
        [SerializeField] private Sprite closeIcon;

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

        public bool IsVisible => gameObject.activeInHierarchy && sheet != null && sheet.gameObject.activeSelf;

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
        public void ShowCaptureChoices(IList<CaptureChoice> choices, Action<int> onChoose, Action<int> onPreview, Action onCancel, string title = "SCEGLI LA PRESA")
        {
            if (choices == null || choices.Count == 0) return;

            gameObject.SetActive(true);
            EnsureBuilt();
            ClearRows();
            previewCallback = onPreview;
            cancelCallback = onCancel;
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

        /// <summary>Compatibilita' con i vecchi chiamanti: opzioni solo testo.</summary>
        public void ShowMoves(List<string> moveDescriptions, Action<int> onChoose, bool autoHideOnChoose = true, Action<int> onHover = null, List<Sprite> icons = null)
        {
            if (moveDescriptions == null || moveDescriptions.Count == 0) return;
            var choices = new List<CaptureChoice>();
            for (int i = 0; i < moveDescriptions.Count; i++)
            {
                var cards = icons != null && i < icons.Count && icons[i] != null ? new List<Sprite> { icons[i] } : null;
                choices.Add(new CaptureChoice { Title = moveDescriptions[i], Cards = cards });
            }
            ShowCaptureChoices(choices, onChoose, onHover, null, moveDescriptions.Count == 1 ? "SCEGLI MOSSA" : "PIÙ MOSSE DISPONIBILI");
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
            if (sheet != null)
            {
                DOTween.Kill(sheet);
                sheet.gameObject.SetActive(false);
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
            return text;
        }
    }
}
