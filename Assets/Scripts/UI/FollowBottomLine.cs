using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class FollowBottomLine : MonoBehaviour
{
    [SerializeField] RectTransform bottomLine;
    [SerializeField] RectTransform bottomBar;
    [SerializeField] Canvas canvas;

    [Header("Options")]
    [SerializeField] bool followX = false;     // di solito vuoi solo Y
    [SerializeField] float extraCanvasY = 0f; // offset in canvas units
    [SerializeField] bool forceEveryFrame = true;

    Vector2 _lastLineCanvasLocal;

    void OnEnable()
    {
        if (canvas == null) canvas = bottomBar != null ? bottomBar.GetComponentInParent<Canvas>() : null;
    }

    void LateUpdate()
    {
        if (!forceEveryFrame && Application.isPlaying) return;
        if (bottomLine == null || bottomBar == null) return;
        if (canvas == null) canvas = bottomBar.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var canvasRT = (RectTransform)canvas.rootCanvas.transform;
        var cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        // 1) bottomLine pivot -> screen
        Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, bottomLine.position);

        // 2) screen -> local del parent della BottomBar (non del canvas root!)
        RectTransform barParent = bottomBar.parent as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            barParent, sp, cam, out Vector2 lineLocalInBarParent);

        // 3) applica (di solito solo Y)
        var ap = bottomBar.anchoredPosition;
        bottomBar.anchoredPosition = new Vector2(ap.x, lineLocalInBarParent.y + extraCanvasY);

    }

    public void RefreshNow()
    {
        if (!isActiveAndEnabled) return;
        ApplyOnce();
    }

    private void ApplyOnce()
    {
        if (bottomLine == null || bottomBar == null) return;
        if (canvas == null) canvas = bottomBar.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;
        Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, bottomLine.position);
        RectTransform barParent = bottomBar.parent as RectTransform;
        if (barParent == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(barParent, sp, cam, out Vector2 lineLocalInBarParent);
        var ap = bottomBar.anchoredPosition;
        bottomBar.anchoredPosition = new Vector2(ap.x, lineLocalInBarParent.y + extraCanvasY);
    }
}
