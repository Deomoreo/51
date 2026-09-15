using UnityEngine;

namespace Project51.Unity.UIKit
{
    /// <summary>
    /// Locator per la struttura base condivisa di una schermata: Background (fuori Safe Area,
    /// copre notch/bordi fisici) + SafeArea (HeaderArea/MainContent/BottomNav dentro). Nessuna
    /// coordinata qui - solo riferimenti; la geometria vive nei RectTransform/LayoutGroup del
    /// prefab (UI_ScreenRoot), riutilizzabile identico su qualunque altra schermata del gioco.
    /// </summary>
    public class UIScreenRoot : MonoBehaviour
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private RectTransform headerArea;
        [SerializeField] private RectTransform mainContent;
        [SerializeField] private RectTransform bottomNav;

        public RectTransform Background => background;
        public RectTransform SafeArea => safeArea;
        public RectTransform HeaderArea => headerArea;
        public RectTransform MainContent => mainContent;
        public RectTransform BottomNav => bottomNav;
    }
}
