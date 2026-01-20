using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Mantiene un header esattamente "a filo" sopra una ScrollRect.
    /// Utile quando layout/CanvasScaler cambiano tra device e posizioni hardcoded saltano.
    /// 
    /// Assunzione: header e scrollRect stanno nello stesso parent (o comunque condividono uno spazio UI coerente).
    /// </summary>
    [ExecuteAlways]
    public sealed class HeaderAlignToScrollView : MonoBehaviour
    {
        [SerializeField] private RectTransform header;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private float extraGap = 0f; // in unità UI (non pixel)

        private Vector2Int _lastScreen;

        private void Reset()
        {
            header = transform as RectTransform;
            if (header != null)
                scrollRect = header.GetComponentInParent<ScrollRect>();
        }

        private void LateUpdate()
        {
            // In edit mode o quando cambia risoluzione, riallinea.
            var s = new Vector2Int(Screen.width, Screen.height);
            if (_lastScreen != s)
            {
                _lastScreen = s;
                Align();
            }

            // In play mode può cambiare anche per layout rebuild.
            Align();
        }

        private void Align()
        {
            if (header == null || scrollRect == null) return;

            var scrollRT = scrollRect.transform as RectTransform;
            if (scrollRT == null) return;

            // Forza rebuild per avere rect aggiornati
            Canvas.ForceUpdateCanvases();

            // Portiamo il top della scrollview in coordinate locali del parent dell'header
            var parent = header.parent as RectTransform;
            if (parent == null) return;

            var corners = new Vector3[4];
            scrollRT.GetWorldCorners(corners);

            float topLocalY = float.NegativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                Vector2 local = parent.InverseTransformPoint(corners[i]);
                topLocalY = Mathf.Max(topLocalY, local.y);
            }

            // Sposta il bottom dell'header a filo sul top della scrollview.
            // Non usare anchoredPosition: con anchor stretch/non centrati non è nello stesso spazio.
            // Usiamo invece i world corners dell'header e li convertiamo in local del parent.
            var headerCorners = new Vector3[4];
            header.GetWorldCorners(headerCorners);
            float headerBottomLocalY = float.PositiveInfinity;
            for (int i = 0; i < 4; i++)
            {
                Vector2 local = parent.InverseTransformPoint(headerCorners[i]);
                headerBottomLocalY = Mathf.Min(headerBottomLocalY, local.y);
            }

            float delta = (topLocalY + extraGap) - headerBottomLocalY;
            if (Mathf.Abs(delta) < 0.001f) return;

            header.anchoredPosition = new Vector2(header.anchoredPosition.x, header.anchoredPosition.y + delta);
        }
    }
}
