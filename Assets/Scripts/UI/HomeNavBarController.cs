using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Project51.Unity
{
    /// <summary>
    /// Controller unico per la bottom nav bar di HomeScreen: sincronizza tab attivo,
    /// colore icone/etichette e posizione dell'highlight con PanelSwipeController
    /// (sia su click di un tab, sia durante lo swipe delle pagine).
    /// Sostituisce volutamente la coppia BottomNavBarUI + BottomNavController del
    /// vecchio MainHud: qui c'e' un solo script che scrive la posizione/larghezza
    /// dell'highlight, per evitare il conflitto che avevamo trovato la' (due script
    /// che si sovrascrivevano a vicenda lo stesso RectTransform).
    /// </summary>
    public class HomeNavBarController : MonoBehaviour
    {
        [SerializeField] private PanelSwipeController swipeController;
        [SerializeField] private RectTransform highlight;
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private RectTransform[] tabRects;
        [SerializeField] private Graphic[] tabIcons;
        [SerializeField] private Graphic[] tabLabels;

        [SerializeField] private Color activeColor = new Color(1f, 0.89411765f, 0.6117647f, 1f);   // #FFE49C
        [SerializeField] private Color inactiveColor = new Color(0.5803922f, 0.6745098f, 0.7921569f, 1f); // #94ACCA

        [SerializeField] private float highlightTweenDuration = 0.25f;
        [SerializeField] private Ease highlightTweenEase = Ease.OutCubic;

        [Tooltip("Margine orizzontale (somma dei due lati) sottratto alla larghezza reale del tab per ottenere la larghezza dell'highlight. Sostituisce una larghezza fissa in pixel, che a risoluzioni/aspect ratio diversi da quella con cui era stata tarata a mano risultava troppo stretta/larga e disallineata rispetto al centro del bottone.")]
        [SerializeField] private float highlightHorizontalPadding = 20f;

        [Tooltip("Scala del 'punch' dell'icona quando un tab diventa attivo (0.18 = +-18%).")]
        [SerializeField] private float iconPunchScale = 0.18f;
        [SerializeField] private float iconPunchDuration = 0.28f;

        private float[] _tabCentersX;
        private float _tabWidth;
        private Tweener _highlightTweener;
        private Tweener[] _iconTweeners;
        private int _currentIndex;
        private int _lastColorIndex = -1;

        private void Awake()
        {
            if (highlight != null)
            {
                // Ricentriamo SOLO l'asse X (per CacheTabCenters): l'asse Y resta quello
                // gia' verificato a mano in HomeScreenBuilder.CreateNavBar, non lo tocchiamo.
                highlight.anchorMin = new Vector2(0.5f, highlight.anchorMin.y);
                highlight.anchorMax = new Vector2(0.5f, highlight.anchorMax.y);
            }

            if (tabButtons == null) return;

            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                int index = i;
                tabButtons[i].onClick.AddListener(() => OnTabClicked(index));
            }
        }

        private void OnEnable()
        {
            if (swipeController == null) return;
            swipeController.OnPageChanged += HandlePageChanged;
            swipeController.OnPageProgress += HandlePageProgress;
        }

        private void OnDisable()
        {
            if (swipeController == null) return;
            swipeController.OnPageChanged -= HandlePageChanged;
            swipeController.OnPageProgress -= HandlePageProgress;
        }

        private void Start()
        {
            CacheTabCenters();
            SetHighlightImmediate(_currentIndex);
            ApplyColors(_currentIndex);
        }

        /// <summary>
        /// Chiamato da Unity quando cambiano le dimensioni del RectTransform della NavBar
        /// (es. resize del Game View / Device Simulator, cambio risoluzione o orientamento).
        /// Senza questo, _tabCentersX/_tabWidth restavano quelli calcolati al primo Start()
        /// e l'highlight finiva disallineato o con la larghezza sbagliata su risoluzioni
        /// diverse da quella con cui era stato tarato a mano.
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled) return;
            if (tabRects == null || highlight == null) return;

            CacheTabCenters();
            MoveHighlightTo(_currentIndex, animate: false);
        }

        /// <summary>
        /// Calcola il centro X e la larghezza di ogni tab, in coordinate locali del
        /// parent dell'highlight — funziona qualunque siano anchor/pivot dei tab,
        /// niente formule hard-coded sulla larghezza della bar. Applica anche la
        /// larghezza risultante all'highlight (derivata dal tab, non piu' fissa) cosi'
        /// che resti proporzionata al bottone a qualunque risoluzione/aspect ratio.
        /// </summary>
        private void CacheTabCenters()
        {
            if (tabRects == null || highlight == null) return;

            var highlightParent = highlight.parent as RectTransform;
            if (highlightParent == null) return;

            Canvas.ForceUpdateCanvases();

            _tabCentersX = new float[tabRects.Length];
            var corners = new Vector3[4];
            for (int i = 0; i < tabRects.Length; i++)
            {
                if (tabRects[i] == null) continue;
                tabRects[i].GetWorldCorners(corners);
                Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
                Vector3 local = highlightParent.InverseTransformPoint(worldCenter);
                _tabCentersX[i] = local.x;

                if (i == 0)
                {
                    float localLeft = highlightParent.InverseTransformPoint(corners[0]).x;
                    float localRight = highlightParent.InverseTransformPoint(corners[2]).x;
                    _tabWidth = Mathf.Abs(localRight - localLeft);
                }
            }

            if (_tabWidth > 0f)
            {
                float width = Mathf.Max(0f, _tabWidth - highlightHorizontalPadding);
                highlight.sizeDelta = new Vector2(width, highlight.sizeDelta.y);
            }
        }

        private void OnTabClicked(int index)
        {
            if (swipeController == null) return;

            // Gia' sul tab cliccato: non richiamare SwitchPage. PanelSwipeController ha gia'
            // un suo guard interno per il caso "stesso indice", ma in certi rami (es.
            // pageSpacing non ancora inizializzato) puo' comunque rifar partire l'animazione
            // di swipe e i conseguenti eventi (highlight/punch icona), dando l'impressione di
            // "premere" un tab gia' attivo. Bloccarlo qui, al punto dove l'utente lo nota, e'
            // piu' robusto che inseguire ogni ramo interno di SwitchPage.
            if (index == _currentIndex) return;

            swipeController.SwitchPage(index);
        }

        private void HandlePageChanged(int index)
        {
            _currentIndex = index;
            MoveHighlightTo(index, animate: true);
            ApplyColors(index);
        }

        private void HandlePageProgress(float pageFloat)
        {
            MoveHighlightTo(pageFloat, animate: false);
            ApplyColors(Mathf.RoundToInt(pageFloat));
        }

        private void SetHighlightImmediate(int index)
        {
            if (highlight == null || _tabCentersX == null || index < 0 || index >= _tabCentersX.Length) return;
            highlight.anchoredPosition = new Vector2(_tabCentersX[index], highlight.anchoredPosition.y);
        }

        private void MoveHighlightTo(float pageFloat, bool animate)
        {
            if (highlight == null || _tabCentersX == null || _tabCentersX.Length == 0) return;

            pageFloat = Mathf.Clamp(pageFloat, 0, _tabCentersX.Length - 1);
            int from = Mathf.FloorToInt(pageFloat);
            int to = Mathf.CeilToInt(pageFloat);
            float t = pageFloat - from;

            float x = Mathf.Lerp(_tabCentersX[from], _tabCentersX[to], t);

            if (animate)
            {
                _highlightTweener?.Kill();
                _highlightTweener = highlight.DOAnchorPosX(x, highlightTweenDuration).SetEase(highlightTweenEase);
            }
            else
            {
                _highlightTweener?.Kill();
                highlight.anchoredPosition = new Vector2(x, highlight.anchoredPosition.y);
            }
        }

        private void ApplyColors(int activeIndex)
        {
            for (int i = 0; i < (tabIcons?.Length ?? 0); i++)
            {
                if (tabIcons[i] == null) continue;
                tabIcons[i].color = (i == activeIndex) ? activeColor : inactiveColor;
            }

            for (int i = 0; i < (tabLabels?.Length ?? 0); i++)
            {
                if (tabLabels[i] == null) continue;
                tabLabels[i].color = (i == activeIndex) ? activeColor : inactiveColor;
            }

            // Punch solo quando l'indice arrotondato cambia davvero, non a ogni chiamata:
            // HandlePageProgress chiama ApplyColors a ogni frame di drag, quindi senza questo
            // controllo uno swipe veloce genererebbe decine di punch a raffica sullo stesso tab.
            if (activeIndex != _lastColorIndex)
            {
                _lastColorIndex = activeIndex;
                AnimateIconPunch(activeIndex);
            }
        }

        private void AnimateIconPunch(int index)
        {
            if (tabIcons == null || index < 0 || index >= tabIcons.Length || tabIcons[index] == null) return;

            if (_iconTweeners == null || _iconTweeners.Length != tabIcons.Length)
            {
                _iconTweeners = new Tweener[tabIcons.Length];
            }

            var rt = tabIcons[index].rectTransform;

            // Uccidi solo il tween DI QUESTO tab prima di ripartire (non tocca gli altri:
            // un DOPunchScale torna sempre alla propria scala di partenza da solo a fine
            // corsa, quindi non serve resettare quelli non toccati anche con swipe veloci).
            _iconTweeners[index]?.Kill();
            rt.localScale = Vector3.one;
            _iconTweeners[index] = rt.DOPunchScale(Vector3.one * iconPunchScale, iconPunchDuration, 6, 0.6f)
                .SetUpdate(true);
        }
    }
}
