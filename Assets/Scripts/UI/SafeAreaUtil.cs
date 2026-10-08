using UnityEngine;

namespace Project51.Unity
{
    public static class SafeAreaUtil
    {
        public static Rect GetSafeAreaRenderingPixels()
        {
            Rect raw = Screen.safeArea;

            // Se è già coerente con Screen.width/height (player window), non convertire
            if (raw.width <= Screen.width + 1f && raw.height <= Screen.height + 1f)
            {
                return raw;
            }

            float rw = Display.main.renderingWidth;
            float rh = Display.main.renderingHeight;

            float cw = Mathf.Max(1f, Screen.currentResolution.width);
            float ch = Mathf.Max(1f, Screen.currentResolution.height);

            float sx = rw / cw;
            float sy = rh / ch;

            Rect converted = new Rect(raw.x * sx, raw.y * sy, raw.width * sx, raw.height * sy);
            return converted;
        }
    }
}
