using UnityEngine;
using UnityEngine.EventSystems;

namespace Project51.UIV2.Core
{
    public sealed class PageSwipeSurface : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public UIV2Pager Pager;
        private bool horizontal;
        public void OnBeginDrag(PointerEventData e)
        {
            Vector2 d = e.position - e.pressPosition;
            horizontal = Mathf.Abs(d.x) > Mathf.Abs(d.y) * 1.2f && Pager.BeginSwipe(e);
        }
        public void OnDrag(PointerEventData e) { if (horizontal) Pager.DragSwipe(e); }
        public void OnEndDrag(PointerEventData e) { if (horizontal) Pager.EndSwipe(e); horizontal = false; }
    }
}
