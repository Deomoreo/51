using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rt;
    private Rect _lastSafeArea;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        Apply();
    }

    private void OnEnable() => Apply();

    private void OnRectTransformDimensionsChange() => Apply();

    private void Apply()
    {
        if (_rt == null) return;

        Rect safe = Screen.safeArea;
        if (safe == _lastSafeArea) return;
        _lastSafeArea = safe;

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;

        min.x /= Screen.width; min.y /= Screen.height;
        max.x /= Screen.width; max.y /= Screen.height;

        _rt.anchorMin = min;
        _rt.anchorMax = max;
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;
    }
}
