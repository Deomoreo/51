using UnityEngine;

namespace Project51.UI
{
    /// <summary>
    /// Anchors a UI element to a specific position on screen.
    /// Use this for UI elements that should stay at edges (bottom bar, top bar, side buttons, etc.)
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class AnchorToPosition : MonoBehaviour
    {
        public enum AnchorPosition
        {
            TopLeft,
            TopCenter,
            TopRight,
            MiddleLeft,
            MiddleCenter,
            MiddleRight,
            BottomLeft,
            BottomCenter,
            BottomRight,
            
            // Stretch modes
            TopStretch,      // Stretches horizontally at top
            BottomStretch,   // Stretches horizontally at bottom
            LeftStretch,     // Stretches vertically on left
            RightStretch,    // Stretches vertically on right
            FullStretch      // Stretches both ways (fills parent)
        }

        [Header("Anchor Settings")]
        [SerializeField] private AnchorPosition anchorPosition = AnchorPosition.MiddleCenter;
        
        [Header("Size (for non-stretch modes)")]
        [Tooltip("Width of the element. Set to 0 to keep current width.")]
        [SerializeField] private float width = 0f;
        
        [Tooltip("Height of the element. Set to 0 to keep current height.")]
        [SerializeField] private float height = 0f;

        [Header("Offset from Anchor")]
        [SerializeField] private Vector2 offset = Vector2.zero;

        private RectTransform _rt;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            ApplyAnchor();
        }

        private void OnEnable()
        {
            ApplyAnchor();
        }

        [ContextMenu("Apply Anchor Now")]
        public void ApplyAnchor()
        {
            if (_rt == null)
                _rt = GetComponent<RectTransform>();

            if (_rt == null) return;

            Vector2 anchorMin, anchorMax, pivot;
            GetAnchorValues(anchorPosition, out anchorMin, out anchorMax, out pivot);

            _rt.anchorMin = anchorMin;
            _rt.anchorMax = anchorMax;
            _rt.pivot = pivot;

            // Handle stretch modes
            bool isHorizontalStretch = anchorPosition == AnchorPosition.TopStretch || 
                                       anchorPosition == AnchorPosition.BottomStretch ||
                                       anchorPosition == AnchorPosition.FullStretch;
            
            bool isVerticalStretch = anchorPosition == AnchorPosition.LeftStretch || 
                                     anchorPosition == AnchorPosition.RightStretch ||
                                     anchorPosition == AnchorPosition.FullStretch;

            if (isHorizontalStretch && isVerticalStretch)
            {
                // Full stretch
                _rt.offsetMin = new Vector2(offset.x, offset.y);
                _rt.offsetMax = new Vector2(-offset.x, -offset.y);
            }
            else if (isHorizontalStretch)
            {
                // Horizontal stretch only
                _rt.offsetMin = new Vector2(offset.x, _rt.offsetMin.y);
                _rt.offsetMax = new Vector2(-offset.x, _rt.offsetMax.y);
                if (height > 0)
                    _rt.sizeDelta = new Vector2(_rt.sizeDelta.x, height);
                _rt.anchoredPosition = new Vector2(_rt.anchoredPosition.x, offset.y);
            }
            else if (isVerticalStretch)
            {
                // Vertical stretch only
                _rt.offsetMin = new Vector2(_rt.offsetMin.x, offset.y);
                _rt.offsetMax = new Vector2(_rt.offsetMax.x, -offset.y);
                if (width > 0)
                    _rt.sizeDelta = new Vector2(width, _rt.sizeDelta.y);
                _rt.anchoredPosition = new Vector2(offset.x, _rt.anchoredPosition.y);
            }
            else
            {
                // Fixed position
                if (width > 0 || height > 0)
                {
                    _rt.sizeDelta = new Vector2(
                        width > 0 ? width : _rt.sizeDelta.x,
                        height > 0 ? height : _rt.sizeDelta.y
                    );
                }
                _rt.anchoredPosition = offset;
            }
        }

        private void GetAnchorValues(AnchorPosition pos, out Vector2 min, out Vector2 max, out Vector2 pivot)
        {
            switch (pos)
            {
                case AnchorPosition.TopLeft:
                    min = max = new Vector2(0, 1);
                    pivot = new Vector2(0, 1);
                    break;
                case AnchorPosition.TopCenter:
                    min = max = new Vector2(0.5f, 1);
                    pivot = new Vector2(0.5f, 1);
                    break;
                case AnchorPosition.TopRight:
                    min = max = new Vector2(1, 1);
                    pivot = new Vector2(1, 1);
                    break;
                case AnchorPosition.MiddleLeft:
                    min = max = new Vector2(0, 0.5f);
                    pivot = new Vector2(0, 0.5f);
                    break;
                case AnchorPosition.MiddleCenter:
                    min = max = new Vector2(0.5f, 0.5f);
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                case AnchorPosition.MiddleRight:
                    min = max = new Vector2(1, 0.5f);
                    pivot = new Vector2(1, 0.5f);
                    break;
                case AnchorPosition.BottomLeft:
                    min = max = new Vector2(0, 0);
                    pivot = new Vector2(0, 0);
                    break;
                case AnchorPosition.BottomCenter:
                    min = max = new Vector2(0.5f, 0);
                    pivot = new Vector2(0.5f, 0);
                    break;
                case AnchorPosition.BottomRight:
                    min = max = new Vector2(1, 0);
                    pivot = new Vector2(1, 0);
                    break;
                    
                // Stretch modes
                case AnchorPosition.TopStretch:
                    min = new Vector2(0, 1);
                    max = new Vector2(1, 1);
                    pivot = new Vector2(0.5f, 1);
                    break;
                case AnchorPosition.BottomStretch:
                    min = new Vector2(0, 0);
                    max = new Vector2(1, 0);
                    pivot = new Vector2(0.5f, 0);
                    break;
                case AnchorPosition.LeftStretch:
                    min = new Vector2(0, 0);
                    max = new Vector2(0, 1);
                    pivot = new Vector2(0, 0.5f);
                    break;
                case AnchorPosition.RightStretch:
                    min = new Vector2(1, 0);
                    max = new Vector2(1, 1);
                    pivot = new Vector2(1, 0.5f);
                    break;
                case AnchorPosition.FullStretch:
                    min = new Vector2(0, 0);
                    max = new Vector2(1, 1);
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                default:
                    min = max = pivot = new Vector2(0.5f, 0.5f);
                    break;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyAnchor();
        }
#endif
    }
}
