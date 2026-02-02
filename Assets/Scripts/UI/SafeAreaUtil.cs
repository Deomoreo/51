using UnityEngine;

namespace Project51.Unity
{
    public static class SafeAreaUtil
    {
        public static Rect GetSafeAreaRenderingPixels(bool debugLog = false)
        {
            Rect raw = Screen.safeArea;

            // Se è già coerente con Screen.width/height (player window), non convertire
            if (raw.width <= Screen.width + 1f && raw.height <= Screen.height + 1f)
            {
#if UNITY_EDITOR
                if (debugLog)
                    Debug.Log($"[SafeAreaUtil] raw={raw} screen={Screen.width}x{Screen.height} rendering={Display.main.renderingWidth}x{Display.main.renderingHeight} (no conversion)");
#endif
                return raw;
            }

            float rw = Display.main.renderingWidth;
            float rh = Display.main.renderingHeight;

            float cw = Mathf.Max(1f, Screen.currentResolution.width);
            float ch = Mathf.Max(1f, Screen.currentResolution.height);

            float sx = rw / cw;
            float sy = rh / ch;

            Rect converted = new Rect(raw.x * sx, raw.y * sy, raw.width * sx, raw.height * sy);

#if UNITY_EDITOR
            if (debugLog)
            {
                Debug.Log($"[SafeAreaUtil] raw={raw} screen={Screen.width}x{Screen.height} currRes={cw}x{ch} rendering={rw}x{rh}");
                Debug.Log($"[SafeAreaUtil] converted={converted} (sx={sx:F4} sy={sy:F4})");
            }
#endif
            return converted;
        }
    }
}
