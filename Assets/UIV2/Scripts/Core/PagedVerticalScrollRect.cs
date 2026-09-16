using UnityEngine;
using UnityEngine.EventSystems;

namespace Project51.UIV2.Core
{
    public sealed class PagedVerticalScrollRect : UnityEngine.UI.ScrollRect
    {
        public UIV2Pager Pager;
        private bool horizontalGesture;
        public override void OnBeginDrag(PointerEventData e)
        {
            Vector2 d = e.position - e.pressPosition;
            horizontalGesture = Pager != null && Mathf.Abs(d.x) > Mathf.Abs(d.y) * 1.2f;
            if (horizontalGesture) { StopMovement(); Pager.BeginSwipe(e); }
            else base.OnBeginDrag(e);
        }
        public override void OnDrag(PointerEventData e)
        {
            if (horizontalGesture) Pager.DragSwipe(e); else base.OnDrag(e);
        }
        public override void OnEndDrag(PointerEventData e)
        {
            if (horizontalGesture) Pager.EndSwipe(e); else base.OnEndDrag(e);
            horizontalGesture = false;
        }
    }
}
