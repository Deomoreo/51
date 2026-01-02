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

        [SerializeField] private ModalManager modalManager;
        
        [Header("Page Controller (auto-find if null)")]
        [SerializeField] private PanelSwipeController swipeController;

        public ModalManager ModalManager => modalManager;

        [Header("Buttons")]
        [SerializeField] private Button btnShop;
        [SerializeField] private Button btnDeck;
        [SerializeField] private Button btnHome;
        [SerializeField] private Button btnProfile;
        [SerializeField] private Button btnSettings;

        [Header("Button Icons (for punch feedback)")]
        [SerializeField] private Transform iconHome;
        [SerializeField] private Transform iconDeck;
        [SerializeField] private Transform iconShop;
        [SerializeField] private Transform iconProfile;

        [Header("Visual")]
        [SerializeField] private Color normal = Color.white;
        [SerializeField] private Color selected = Color.yellow;

        [Header("Indicator (Clash Royale style)")]
        [SerializeField] private RectTransform indicator;
        [SerializeField] private RectTransform tabsContainer;
        [SerializeField] private List<RectTransform> tabRects;
        [SerializeField] private float indicatorFixedWidth = 80f;
        [SerializeField] private float indicatorAnimDuration = 0.25f;
        [SerializeField] private Ease indicatorAnimEase = Ease.OutCubic;

        [Header("Tab Width")]
        [SerializeField] private float tabCollapsedWidth = 250f;
        [SerializeField] private float tabExpandedWidth = 300f;
        [SerializeField] private bool smoothTabWidthTransition = true;
        [SerializeField] private AnimationCurve tabWidthTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool applyWidthToButtons = true;

        public event Action<MainTab> OnTabSelected;

        private MainTab currentTab;
        private MainTab previewTab;
        private bool hasPreview;
        private Tweener indicatorTweener;
        private Tweener indicatorWidthTweener;

        public int CurrentSelectedIndex => (int)currentTab;
        public int TabCount => Enum.GetValues(typeof(MainTab)).Length;

        private void Awake()
        {
            if (indicator != null)
            {
                indicator.anchorMin = new Vector2(0.5f, 0.5f);
                indicator.anchorMax = new Vector2(0.5f, 0.5f);
                indicator.pivot = new Vector2(0.5f, 0.5f);
            }

            // Auto-find swipeController se non assegnato
            if (swipeController == null)
                swipeController = FindObjectOfType<PanelSwipeController>(true);
        }

        private void OnEnable()
        {
            if (btnHome != null) btnHome.onClick.RemoveAllListeners();
            if (btnDeck != null) btnDeck.onClick.RemoveAllListeners();
            if (btnShop != null) btnShop.onClick.RemoveAllListeners();
            if (btnProfile != null) btnProfile.onClick.RemoveAllListeners();

            if (btnHome != null) btnHome.onClick.AddListener(() => OnButtonClicked(MainTab.Home));
            if (btnDeck != null) btnDeck.onClick.AddListener(() => OnButtonClicked(MainTab.Deck));
            if (btnShop != null) btnShop.onClick.AddListener(() => OnButtonClicked(MainTab.Shop));
            if (btnProfile != null) btnProfile.onClick.AddListener(() => OnButtonClicked(MainTab.Profile));

            ForceButtonsInteractable(true);
        }

        private void Start()
        {
            // Auto-find di sicurezza anche qui
            if (swipeController == null)
                swipeController = FindObjectOfType<PanelSwipeController>(true);

            SelectTab(MainTab.Home, fireEvent: false);

        }

        private void OnButtonClicked(MainTab tab)
        {

            // UI update
            SelectTab(tab, fireEvent: false);

            // Navigation event (source of truth)
            OnTabSelected?.Invoke(tab);
        }

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

                // Anche se è lo stesso tab, notifichiamo il click se richiesto,
                // così chi ascolta (MainHudController) può forzare una resync/scroll.
                if (fireEvent)
                    OnTabSelected?.Invoke(tab);

                return;
            }

            if (fireEvent)
                OnTabSelected?.Invoke(tab);
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
        }

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

        private Transform GetIconByTab(MainTab tab)
        {
            return tab switch
            {
                MainTab.Home => iconHome != null ? iconHome : (btnHome != null ? btnHome.transform : null),
                MainTab.Deck => iconDeck != null ? iconDeck : (btnDeck != null ? btnDeck.transform : null),
                MainTab.Shop => iconShop != null ? iconShop : (btnShop != null ? btnShop.transform : null),
                MainTab.Profile => iconProfile != null ? iconProfile : (btnProfile != null ? btnProfile.transform : null),
                _ => null
            };
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