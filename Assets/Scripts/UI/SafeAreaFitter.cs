using UnityEngine;

using Project51.Unity;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [Tooltip("Arriva al bordo alto dello schermo (sfondi a tutta pagina sotto la barra di stato).")]
    [SerializeField] private bool ignoreTop = false;

    private RectTransform _rt;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreen;

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

        Rect safe = SafeAreaUtil.GetSafeAreaRenderingPixels();

        // Anche lo schermo: puo' cambiare a safe area invariata (barre Android, foldable, Simulator).
        var screen = new Vector2Int(Screen.width, Screen.height);
        if (safe == _lastSafeArea && screen == _lastScreen) return;
        _lastSafeArea = safe;
        _lastScreen = screen;

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;
        if (ignoreTop) max.y = Screen.height;

        min.x /= Screen.width; min.y /= Screen.height;
        max.x /= Screen.width; max.y /= Screen.height;

        _rt.anchorMin = min;
        _rt.anchorMax = max;
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;
    }
}
