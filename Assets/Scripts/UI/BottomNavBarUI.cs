using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Project51.Unity
{
    public class BottomNavBarUI : MonoBehaviour
    {
        public enum MainTab { Home, Deck, Shop, Profile }

        [SerializeField] private MainHudController mainHud;
        [SerializeField] private ModalManager modalManager;

        public ModalManager ModalManager => modalManager;

        [Header("Buttons")]
        [SerializeField] private Button btnShop;
        [SerializeField] private Button btnDeck;
        [SerializeField] private Button btnHome;
        [SerializeField] private Button btnProfile;
        [SerializeField] private Button btnSettings;

        [Header("Button Icons (for punch feedback)")]
        [Tooltip("Transform dell'icona dentro BtnHome (opzionale, se vuoto usa il bottone stesso)")]
        [SerializeField] private Transform iconHome;
        [Tooltip("Transform dell'icona dentro BtnDeck")]
        [SerializeField] private Transform iconDeck;
        [Tooltip("Transform dell'icona dentro BtnShop")]
        [SerializeField] private Transform iconShop;
        [Tooltip("Transform dell'icona dentro BtnProfile")]
        [SerializeField] private Transform iconProfile;

        [Header("Visual")]
        [SerializeField] private Color normal = Color.white;
        [SerializeField] private Color selected = Color.yellow;

        [Header("Indicator (Clash Royale style)")]
        [Tooltip("RectTransform dell'indicator/pill che scorre sotto i bottoni")]
        [SerializeField] private RectTransform indicator;
        [Tooltip("Parent RectTransform dei bottoni (usato per calcolare posizioni locali)")]
        [SerializeField] private RectTransform tabsContainer;
        [Tooltip("Lista ordinata dei RectTransform dei bottoni (Home, Deck, Shop, Profile)")]
        [SerializeField] private List<RectTransform> tabRects;
        [Tooltip("Se true, interpola anche la larghezza dell'indicator tra i tab")]
        [SerializeField] private bool interpolateIndicatorWidth = true;
        [Tooltip("Larghezza fissa dell'indicator (usata se interpolateIndicatorWidth è false)")]
        [SerializeField] private float indicatorFixedWidth = 80f;
        [Tooltip("Durata animazione indicator quando si clicca un tab")]
        [SerializeField] private float indicatorAnimDuration = 0.25f;
        [SerializeField] private Ease indicatorAnimEase = Ease.OutCubic;

        [Header("Tab Width (per calcolo centri stabili)")]
        [Tooltip("Larghezza di un tab quando NON è selezionato (collassato)")]
        [SerializeField] private float tabCollapsedWidth = 250f;
        [Tooltip("Larghezza di un tab quando è selezionato (espanso)")]
        [SerializeField] private float tabExpandedWidth = 300f;
        [Tooltip("Se true, interpola smooth le larghezze dei bottoni durante lo swipe (più fluido)")]
        [SerializeField] private bool smoothTabWidthTransition = true;
        [Tooltip("Curva che controlla l'espansione smooth dei tab durante lo swipe (0=collapsed, 1=expanded)")]
        [SerializeField] private AnimationCurve tabWidthTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Se true, applica le width interpolate ai LayoutElement dei bottoni durante lo swipe")]
        [SerializeField] private bool applyWidthToButtons = true;

        public event Action<MainTab> OnTabSelected;

        private MainTab currentTab;
        private MainTab previewTab;
        private bool hasPreview;
        private Tweener indicatorTweener;
        private Tweener indicatorWidthTweener;

        public int CurrentSelectedIndex => (int)currentTab;
        public int TabCount => Enum.GetValues(typeof(MainTab)).Length;

        public void SelectTab(MainTab tab, bool fireEvent = true)
        {
            hasPreview = false;

            bool sameTab = (currentTab == tab);
            currentTab = tab;
            ApplyVisualState();

            // Anima indicator verso la nuova posizione
            AnimateIndicatorToTab((int)tab);

            if (sameTab)
            {
                // Feedback "già selezionato": punch scale sull'icona (non tutto il bottone)
                Transform icon = GetIconByTab(tab);
                if (icon != null)
                {
                    icon.DOKill(complete: true);
                    icon.localScale = Vector3.one;
                    icon.DOPunchScale(new Vector3(-0.15f, -0.15f, 0), 0.25f, 8, 0.8f);
                }
                return;
            }

            if (fireEvent)
                OnTabSelected?.Invoke(tab);
        }

        private Transform GetIconByTab(MainTab tab)
        {
            switch (tab)
            {
                case MainTab.Home:
                    return iconHome != null ? iconHome : (btnHome != null ? btnHome.transform : null);
                case MainTab.Deck:
                    return iconDeck != null ? iconDeck : (btnDeck != null ? btnDeck.transform : null);
                case MainTab.Shop:
                    return iconShop != null ? iconShop : (btnShop != null ? btnShop.transform : null);
                case MainTab.Profile:
                    return iconProfile != null ? iconProfile : (btnProfile != null ? btnProfile.transform : null);
                default:
                    return null;
            }
        }

        public void PreviewTab(MainTab tab)
        {
            hasPreview = true;
            previewTab = tab;
            ApplyVisualState();
        }

        /// <summary>
        /// Impostazione della posizione dell'indicator basandosi su un valore continuo pageFloat.
        /// E.g., pageFloat=1.5 significa a metà tra tab 1 (Deck) e tab 2 (Shop).
        /// Chiamare durante lo swipe per avere movimento fluido dell'indicator.
        /// </summary>
        private Vector2 GetTabLocalCenterInIndicatorParent(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= tabRects.Count) return Vector2.zero;
            RectTransform tabRect = tabRects[tabIndex];
            if (tabRect == null) return Vector2.zero;

            Vector3[] corners = new Vector3[4];
            tabRect.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;

            RectTransform indicatorParent = indicator != null ? indicator.parent as RectTransform : null;
            if (indicatorParent == null)
                return Vector2.zero;

            Vector3 localPos = indicatorParent.InverseTransformPoint(worldCenter);
            return new Vector2(localPos.x, localPos.y);
        }

        /// <summary>
        /// Calcola il centro X di un tab nello spazio del parent dell'indicator,
        /// usando le width target (collapsed/expanded) con interpolazione smooth durante swipe.
        /// </summary>
        private float GetTabCenterXStable(float pageFloat)
        {
            if (tabRects == null || tabRects.Count == 0 || indicator == null) return 0f;

            // Clamp
            pageFloat = Mathf.Clamp(pageFloat, 0f, tabRects.Count - 1);

            int fromIndex = Mathf.FloorToInt(pageFloat);
            int toIndex = Mathf.CeilToInt(pageFloat);
            float t = pageFloat - fromIndex;

            fromIndex = Mathf.Clamp(fromIndex, 0, tabRects.Count - 1);
            toIndex = Mathf.Clamp(toIndex, 0, tabRects.Count - 1);

            float fromX = CalculateTabCenterXFixed(fromIndex, pageFloat);
            float toX = CalculateTabCenterXFixed(toIndex, pageFloat);

            return Mathf.Lerp(fromX, toX, t);
        }

        /// <summary>
        /// Calcola il centro X di un tab specifico, con interpolazione smooth delle width durante swipe.
        /// </summary>
        private float CalculateTabCenterXFixed(int tabIndex, float pageFloat)
        {
            if (tabRects == null || tabRects.Count == 0) return 0f;

            // Calcola X cumulativa partendo da sinistra
            float cumulativeX = 0f;
            for (int i = 0; i < tabIndex; i++)
            {
                float w = GetTabWidthInterpolated(i, pageFloat);
                cumulativeX += w;
            }

            // Centro del tab corrente
            float myWidth = GetTabWidthInterpolated(tabIndex, pageFloat);
            cumulativeX += myWidth * 0.5f;

            // Converti in spazio del parent dell'indicator
            RectTransform firstTab = tabRects[0];
            if (firstTab == null) return cumulativeX;

            Vector3[] corners = new Vector3[4];
            firstTab.GetWorldCorners(corners);
            Vector3 firstTabWorldLeft = corners[0];

            RectTransform indicatorParent = indicator.parent as RectTransform;
            if (indicatorParent == null) return cumulativeX;

            Vector3 leftInIndicatorSpace = indicatorParent.InverseTransformPoint(firstTabWorldLeft);

            return leftInIndicatorSpace.x + cumulativeX;
        }

        /// <summary>
        /// Restituisce la larghezza interpolata di un tab durante lo swipe con curva smooth.
        /// </summary>
        private float GetTabWidthInterpolated(int tabIndex, float pageFloat)
        {
            if (!smoothTabWidthTransition)
            {
                // Comportamento originale: scatta tra collapsed/expanded
                int expandedIndex = Mathf.RoundToInt(pageFloat);
                expandedIndex = Mathf.Clamp(expandedIndex, 0, tabRects.Count - 1);
                return (tabIndex == expandedIndex) ? tabExpandedWidth : tabCollapsedWidth;
            }

            // Smooth transition con curva: lerp la width in base alla distanza da pageFloat
            float distance = Mathf.Abs(pageFloat - tabIndex);
            
            if (distance >= 1f)
            {
                // Troppo lontano: completamente collapsed
                return tabCollapsedWidth;
            }

            // Usa la curva per un'espansione più "gradevole"
            // distance 0 ? curva 1.0 ? expanded
            // distance 1 ? curva 0.0 ? collapsed
            float curveValue = tabWidthTransitionCurve.Evaluate(1f - distance);
            return Mathf.Lerp(tabCollapsedWidth, tabExpandedWidth, curveValue);
        }

        public void SetSelectedByProgress(float pageFloat)
        {
            if (indicator == null || tabRects == null || tabRects.Count == 0) return;

            indicatorTweener?.Kill(false);
            indicatorWidthTweener?.Kill(false);

            float targetX = GetTabCenterXStable(pageFloat);
            indicator.anchoredPosition = new Vector2(targetX, indicator.anchoredPosition.y);

            if (indicatorFixedWidth > 0f)
                indicator.sizeDelta = new Vector2(indicatorFixedWidth, indicator.sizeDelta.y);

            // Applica le width interpolate ai bottoni per un effetto smooth
            if (applyWidthToButtons && smoothTabWidthTransition)
            {
                for (int i = 0; i < tabRects.Count; i++)
                {
                    float targetWidth = GetTabWidthInterpolated(i, pageFloat);
                    ApplyWidthToButton(i, targetWidth);
                }
            }

            int nearestTab = Mathf.RoundToInt(pageFloat);
            nearestTab = Mathf.Clamp(nearestTab, 0, TabCount - 1);
            previewTab = (MainTab)nearestTab;
            hasPreview = true;
            ApplyVisualState();

            UpdateButtonColorsInterpolated(pageFloat);
        }

        /// <summary>
        /// Applica la width target al LayoutElement del bottone (se esiste).
        /// </summary>
        private void ApplyWidthToButton(int index, float width)
        {
            if (index < 0 || index >= tabRects.Count) return;
            RectTransform tabRect = tabRects[index];
            if (tabRect == null) return;

            LayoutElement le = tabRect.GetComponent<LayoutElement>();
            if (le == null) return;

            le.preferredWidth = width;
            le.minWidth = width;
            le.flexibleWidth = 0f;
        }

        /// <summary>
        /// Anima l'indicator verso il tab specificato (usato quando si clicca un bottone).
        /// </summary>
        private void AnimateIndicatorToTab(int tabIndex)
        {
            if (indicator == null || tabRects == null || tabRects.Count == 0) return;

            tabIndex = Mathf.Clamp(tabIndex, 0, tabRects.Count - 1);

            indicatorTweener?.Kill(false);
            indicatorWidthTweener?.Kill(false);

            float targetX = GetTabCenterXStable(tabIndex);
            float targetWidth = indicatorFixedWidth;

            indicatorTweener = indicator.DOAnchorPosX(targetX, indicatorAnimDuration)
                .SetEase(indicatorAnimEase)
                .SetTarget(indicator);

            if (targetWidth > 0f)
            {
                indicatorWidthTweener = indicator.DOSizeDelta(
                    new Vector2(targetWidth, indicator.sizeDelta.y),
                    indicatorAnimDuration)
                    .SetEase(indicatorAnimEase)
                    .SetTarget(indicator);
            }
        }

        /// <summary>
        /// Calcola il centro locale del tab nel sistema di coordinate del tabsContainer.
        /// Questo evita offset quando indicator ha parent diverso / anchor diversi,
        /// e quando i tab variano larghezza (250/300).
        /// </summary>
        private Vector2 GetTabLocalCenter(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= tabRects.Count) return Vector2.zero;

            RectTransform tabRect = tabRects[tabIndex];
            if (tabRect == null) return Vector2.zero;

            // Get world center of the tab
            Vector3[] corners = new Vector3[4];
            tabRect.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;

            // Converti SEMPRE nello spazio del tabsContainer (sorgente di verità per i tab)
            RectTransform space = tabsContainer != null ? tabsContainer : transform as RectTransform;
            if (space == null) return Vector2.zero;

            Vector3 localPos = space.InverseTransformPoint(worldCenter);
            return new Vector2(localPos.x, localPos.y);
        }

        private float GetTabWidth(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= tabRects.Count) return indicatorFixedWidth;

            RectTransform tabRect = tabRects[tabIndex];
            if (tabRect == null) return indicatorFixedWidth;

            return tabRect.rect.width;
        }

        /// <summary>
        /// Interpola i colori dei bottoni basandosi sul pageFloat.
        /// I tab adiacenti al pageFloat corrente ricevono colori interpolati.
        /// </summary>
        private void UpdateButtonColorsInterpolated(float pageFloat)
        {
            if (tabRects == null) return;

            int fromIndex = Mathf.FloorToInt(pageFloat);
            int toIndex = Mathf.CeilToInt(pageFloat);
            float t = pageFloat - fromIndex;

            // Clamp indices
            fromIndex = Mathf.Clamp(fromIndex, 0, tabRects.Count - 1);
            toIndex = Mathf.Clamp(toIndex, 0, tabRects.Count - 1);

            // Reset all buttons to normal
            SetButtonColorDirect(btnHome, normal);
            SetButtonColorDirect(btnDeck, normal);
            SetButtonColorDirect(btnShop, normal);
            SetButtonColorDirect(btnProfile, normal);

            // Get buttons for fromIndex and toIndex
            Button fromBtn = GetButtonByIndex(fromIndex);
            Button toBtn = GetButtonByIndex(toIndex);

            if (fromBtn != null)
            {
                Color fromColor = Color.Lerp(selected, normal, t);
                SetButtonColorDirect(fromBtn, fromColor);
            }

            if (toBtn != null && toIndex != fromIndex)
            {
                Color toColor = Color.Lerp(normal, selected, t);
                SetButtonColorDirect(toBtn, toColor);
            }
        }

        private Button GetButtonByIndex(int index)
        {
            switch (index)
            {
                case 0: return btnHome;
                case 1: return btnDeck;
                case 2: return btnShop;
                case 3: return btnProfile;
                default: return null;
            }
        }

        private void OnEnable()
        {
            // Evita duplicati
            if (btnHome != null) btnHome.onClick.RemoveAllListeners();
            if (btnDeck != null) btnDeck.onClick.RemoveAllListeners();
            if (btnShop != null) btnShop.onClick.RemoveAllListeners();
            if (btnProfile != null) btnProfile.onClick.RemoveAllListeners();

            if (btnHome != null) btnHome.onClick.AddListener(() => SelectTab(MainTab.Home, fireEvent: true));
            if (btnDeck != null) btnDeck.onClick.AddListener(() => SelectTab(MainTab.Deck, fireEvent: true));
            if (btnShop != null) btnShop.onClick.AddListener(() => SelectTab(MainTab.Shop, fireEvent: true));
            if (btnProfile != null) btnProfile.onClick.AddListener(() => SelectTab(MainTab.Profile, fireEvent: true));

            // Evita che diventino "grigi" per via di stati disabled: li vogliamo sempre cliccabili
            ForceButtonsInteractable(true);
        }

        private void Awake()
        {
            // Assicura un sistema di coordinate stabile per l'indicator.
            if (indicator != null)
            {
                indicator.anchorMin = new Vector2(0.5f, 0.5f);
                indicator.anchorMax = new Vector2(0.5f, 0.5f);
                indicator.pivot = new Vector2(0.5f, 0.5f);
            }

            // Con tab che cambiano width (250/300) durante swipe, l'interpolazione della width dell'indicator
            // introduce spesso un offset percepito. Usiamo width fissa.
            interpolateIndicatorWidth = false;
        }

        private void Start()
        {
            SelectTab(MainTab.Home, fireEvent: false);

            // Initialize indicator position
            if (indicator != null && tabRects != null && tabRects.Count > 0)
            {
                // Force layout update first
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(tabsContainer != null ? tabsContainer : transform as RectTransform);

                Vector2 pos = GetTabLocalCenter(0);
                indicator.anchoredPosition = new Vector2(pos.x, indicator.anchoredPosition.y);

                if (interpolateIndicatorWidth)
                {
                    float w = GetTabWidth(0);
                    indicator.sizeDelta = new Vector2(w, indicator.sizeDelta.y);
                }
            }
        }

        private void ApplyVisualState()
        {
            var shown = hasPreview ? previewTab : currentTab;

            SetButtonColor(btnHome, shown == MainTab.Home);
            SetButtonColor(btnDeck, shown == MainTab.Deck);
            SetButtonColor(btnShop, shown == MainTab.Shop);
            SetButtonColor(btnProfile, shown == MainTab.Profile);
        }

        private void SetButtonColor(Button btn, bool isSelected)
        {
            if (btn == null) return;

            // Non tocchiamo ColorBlock (highlight/pressed), cambiamo solo l'immagine base.
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            img.color = isSelected ? selected : normal;
        }

        private void SetButtonColorDirect(Button btn, Color color)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            img.color = color;
        }

        private void ForceButtonsInteractable(bool value)
        {
            if (btnHome != null) btnHome.interactable = value;
            if (btnDeck != null) btnDeck.interactable = value;
            if (btnShop != null) btnShop.interactable = value;
            if (btnProfile != null) btnProfile.interactable = value;
            if (btnSettings != null) btnSettings.interactable = value;
        }
    }
}