using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Project51.Unity
{
    public class PrimaryButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform target;
        [SerializeField] private float punchScale = 0.08f;
        [SerializeField] private float duration = 0.18f;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            if (target != null)
            {
                target.DOPunchScale(Vector3.one * punchScale, duration, 10, 1f);
            }
        }
    }
}