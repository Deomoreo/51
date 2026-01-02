using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project51.Unity
{
    public class SwipeSectionNavigator : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Controllers")]
        [Tooltip("Controller dello swipe delle pagine")]
        [SerializeField] private PanelSwipeController swipeController;

        [Header("(Optional) BottomNavController preview")]
        [Tooltip("Se true, aggiorna il BottomNavController durante lo swipe (overlay in preview).\nConsigliato: false se BottomNavController è usato solo come animatore UI su snap (MainHudController).")]
        [SerializeField] private bool useBottomNavControllerPreview = false;
        [SerializeField] private BottomNavController bottomNavController;

        [Header("Swipe tuning")]
        [SerializeField] private float maxSwipeTime = 0.8f;
        [SerializeField] private float horizontalBias = 1.2f;

        [Header("Flick (veloce ma corto)")]
        [SerializeField] private float flickVelocityThreshold = 1100f;

        [Header("Swipe Threshold")]
        [Tooltip("Percentuale di schermo da trascinare per cambiare pagina (0.0-1.0)")]
        [SerializeField, Range(0.1f, 0.5f)] private float minSwipePercent = 0.25f;

        [Header("Input Guard")]
        [Tooltip("Se assegnato, lo swipe non parte quando il gesto inizia sopra questo RectTransform (es. root della bottom bar).")]
        [SerializeField] private RectTransform blockSwipeOver;
        [Tooltip("Se > 0, lo swipe non parte quando il gesto inizia sotto questa Y (in pixel, in coordinate schermo).")]
        [SerializeField] private float blockSwipeBelowScreenY = 0f;

        private Vector2 startPos;
        private float startTime;
        private bool dragging;
        private float lastProgress;
        private int dragFromIndex;

        private void ResolveReferencesIfNeeded()
        {
            if (swipeController == null)
                swipeController = FindObjectOfType<PanelSwipeController>(true);

            if (!useBottomNavControllerPreview)
                return;

            if (bottomNavController == null)
                bottomNavController = FindObjectOfType<BottomNavController>(true);
        }

        private bool ShouldBlockSwipe(PointerEventData eventData)
        {
            if (eventData == null) return false;

            // Escludi l'area bassa dello schermo (solo se configurata)
            if (blockSwipeBelowScreenY > 0f && eventData.position.y <= blockSwipeBelowScreenY)
                return true;

            // Escludi un rect specifico (es. root della bottom bar) (solo se assegnato)
            if (blockSwipeOver != null)
            {
                var cam = eventData.pressEventCamera;
                if (RectTransformUtility.RectangleContainsScreenPoint(blockSwipeOver, eventData.position, cam))
                    return true;
            }

            return false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ResolveReferencesIfNeeded();

            if (ShouldBlockSwipe(eventData))
            {
                dragging = false;
                return;
            }

            eventData.useDragThreshold = false;

            if (swipeController == null) return;

            dragFromIndex = swipeController.CurrentIndex;

            startPos = eventData.position;
            startTime = Time.unscaledTime;
            dragging = true;
            lastProgress = 0f;

            swipeController.BeginDrag();

            if (useBottomNavControllerPreview && bottomNavController != null && bottomNavController.isActiveAndEnabled)
                bottomNavController.BeginSwipePreview();
        }

        public void OnDrag(PointerEventData eventData)
        {
            ResolveReferencesIfNeeded();

            if (!dragging || swipeController == null) return;

            Vector2 currentPos = eventData.position;
            Vector2 delta = currentPos - startPos;

            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);
            if (absX < absY * horizontalBias)
                return;

            swipeController.DragToOffset(delta.x);

            if (!useBottomNavControllerPreview || bottomNavController == null || !bottomNavController.isActiveAndEnabled)
                return;

            float w = swipeController.ScreenWidth > 0f ? swipeController.ScreenWidth : Screen.width;
            float progress = Mathf.Clamp(delta.x / w, -1f, 1f);

            if (Mathf.Abs(progress - lastProgress) > 0.002f)
            {
                lastProgress = progress;
                bottomNavController.SetSwipeProgress(dragFromIndex, progress);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ResolveReferencesIfNeeded();

            if (!dragging) return;
            dragging = false;

            if (swipeController == null) return;

            Vector2 endPos = eventData.position;
            Vector2 delta = endPos - startPos;

            float dt = Mathf.Max(0.0001f, Time.unscaledTime - startTime);

            int current = swipeController.CurrentIndex;
            int target = current;

            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);

            if (absX >= absY * horizontalBias)
            {
                float w = swipeController.ScreenWidth > 0f ? swipeController.ScreenWidth : Screen.width;

                if (absX > w * minSwipePercent)
                    target = current + (delta.x < 0 ? 1 : -1);

                float velocityX = absX / dt;
                if (target == current && dt <= maxSwipeTime && velocityX >= flickVelocityThreshold)
                    target = current + (delta.x < 0 ? 1 : -1);
            }

            int maxIndex = swipeController.PageCount > 0 ? swipeController.PageCount - 1 : 3;
            target = Mathf.Clamp(target, 0, maxIndex);

            bool changingPage = (target != current);

            if (useBottomNavControllerPreview && bottomNavController != null && bottomNavController.isActiveAndEnabled)
                bottomNavController.EndSwipePreview(changingPage ? target : -1);

            swipeController.EndDragAndSnapTo(target);
        }
    }
}
