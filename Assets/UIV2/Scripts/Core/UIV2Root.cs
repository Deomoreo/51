using UnityEngine;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Locator per lo shell globale della UI V2 (UIV2_Root: Background/SafeArea con
    /// TopBarHost-ScreenHost-BottomNavHost/ModalHost/OverlayHost/FXHost). Screen, modal e
    /// HUD lo trovano via Instance invece di richiedere riferimenti diretti, cosi' possono
    /// essere istanziati da qualunque punto senza dipendere da dove vive la UI shell.
    /// </summary>
    public class UIV2Root : MonoBehaviour
    {
        public static UIV2Root Instance { get; private set; }

        [SerializeField] private RectTransform backgroundLayer;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private RectTransform topBarHost;
        [SerializeField] private RectTransform screenHost;
        [SerializeField] private RectTransform bottomNavHost;
        [SerializeField] private RectTransform modalHost;
        [SerializeField] private RectTransform overlayHost;
        [SerializeField] private RectTransform fxHost;

        public RectTransform BackgroundLayer => backgroundLayer;
        public RectTransform SafeArea => safeArea;
        public RectTransform TopBarHost => topBarHost;
        public RectTransform ScreenHost => screenHost;
        public RectTransform BottomNavHost => bottomNavHost;
        public RectTransform ModalHost => modalHost;
        public RectTransform OverlayHost => overlayHost;
        public RectTransform FxHost => fxHost;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
