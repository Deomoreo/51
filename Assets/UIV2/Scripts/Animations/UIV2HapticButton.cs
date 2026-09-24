using Project51.Core;
using UnityEngine;

namespace Project51.UIV2.Animations
{
    [DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class UIV2HapticButton : MonoBehaviour
    {
        private UnityEngine.UI.Button button;
        private void Awake()
        {
            button = GetComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(Clicked);
        }
        private void Clicked()
        {
            // Button invokes onClick only for accepted clicks. Earlier listeners may
            // already have closed this panel or disabled the button.
            GameFeedback.TryHaptic(false);
        }
        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(Clicked);
        }
    }
}
