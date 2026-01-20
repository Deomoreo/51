using UnityEngine;
using DG.Tweening;

namespace Project51.Unity
{
    /// <summary>
    /// Simple shine effect without ShaderGraph: moves a SpriteRenderer overlay across the card.
    /// Attach this to the overlay child object (with SpriteRenderer + additive material).
    /// </summary>
    public sealed class CardShineOverlayMove : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Leave null to use the parent as bounds.")]
        [SerializeField] private SpriteRenderer targetCardRenderer;

        [Header("Motion")]
        [SerializeField] private Vector2 direction = new Vector2(1f, 1f);
        [SerializeField] private float travelPadding = 0.25f;
        [SerializeField] private float sweepDuration = 0.6f;
        [SerializeField] private Vector2 delayRange = new Vector2(2.8f, 4.2f);

        [Header("Behavior")]
        [SerializeField] private bool playOnEnable = true;

        private Sequence seq;
        private Vector3 baseLocalPos;

        private void Awake()
        {
            baseLocalPos = transform.localPosition;
            if (targetCardRenderer == null && transform.parent != null)
                targetCardRenderer = transform.parent.GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (playOnEnable)
                Play();
        }

        private void OnDisable()
        {
            Stop();
            transform.localPosition = baseLocalPos;
        }

        public void Play()
        {
            Stop();

            baseLocalPos = transform.localPosition;

            if (targetCardRenderer == null)
                return;

            var b = targetCardRenderer.bounds;

            // Convert world bounds extents into parent-local delta.
            // We assume overlay is a child of the card so local axes match card axes.
            float halfW = b.extents.x;
            float halfH = b.extents.y;

            var dir = direction.sqrMagnitude <= 0.0001f ? Vector2.one : direction.normalized;

            // How far we need to travel in local space to go fully across card + padding.
            var travel = new Vector3(dir.x * (halfW * 2f + travelPadding), dir.y * (halfH * 2f + travelPadding), 0f);
            var from = baseLocalPos - travel * 0.5f;
            var to = baseLocalPos + travel * 0.5f;

            seq = DOTween.Sequence();
            seq.SetTarget(this);
            seq.SetLoops(-1);

            // initial delay randomized
            seq.AppendInterval(Random.Range(delayRange.x, delayRange.y));

            seq.AppendCallback(() => transform.localPosition = from);
            seq.Append(transform.DOLocalMove(to, sweepDuration).SetEase(Ease.InOutSine));

            // delay before next sweep
            seq.AppendInterval(Random.Range(delayRange.x, delayRange.y));

            seq.Play();
        }

        public void Stop()
        {
            if (seq != null)
            {
                seq.Kill();
                seq = null;
            }

            DOTween.Kill(this);
        }

        private void OnValidate()
        {
            sweepDuration = Mathf.Max(0.05f, sweepDuration);
            delayRange.x = Mathf.Max(0f, delayRange.x);
            delayRange.y = Mathf.Max(delayRange.x, delayRange.y);
            travelPadding = Mathf.Max(0f, travelPadding);
        }
    }
}
