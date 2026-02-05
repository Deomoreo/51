using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Project51.Unity
{
    public class ModalWindowBaseUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform window;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button dimmerButton;
        [SerializeField] private RectTransform dimmerRect;

		[Header("Fade Other UI (TopBar/Banner/etc.)")]
		[SerializeField] private CanvasGroup[] fadeTargets;
		[SerializeField, Range(0f, 1f)] private float fadeTargetsOpenAlpha = 0.35f;
		[SerializeField] private float fadeTargetsDuration = 0.18f;

        [Header("Header drag -> ScrollRect (optional)")]
        [SerializeField] private RectTransform headerDragArea;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Responsive")]
        [SerializeField] private bool respectSafeArea = true;
        [SerializeField] private float duration = 0.25f;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(Close);
            }
            if (dimmerButton != null)
            {
                dimmerButton.onClick.AddListener(Close);
            }
        }

		private void ApplyFadeTargetsAlpha(float alpha, float durationOverride)
		{
			if (fadeTargets == null || fadeTargets.Length == 0)
				return;

			for (int i = 0; i < fadeTargets.Length; i++)
			{
				var cg = fadeTargets[i];
				if (cg == null)
					continue;

				cg.DOKill();
				if (durationOverride <= 0f)
				{
					cg.alpha = alpha;
				}
				else
				{
					cg.DOFade(alpha, durationOverride);
				}
			}
		}

        public void Open()
        {
            gameObject.SetActive(true);

            // stop tween vecchi (fondamentale)
            canvasGroup.DOKill();
            window.DOKill();

            // stato iniziale deterministico
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            IsOpen = true;

            window.localScale = Vector3.one * 0.8f;

            canvasGroup.DOFade(1f, duration);
            window.DOScale(1f, duration).SetEase(Ease.OutBack);

			ApplyFadeTargetsAlpha(fadeTargetsOpenAlpha, fadeTargetsDuration);
            if (dimmerButton != null)
            {
                dimmerButton.interactable = false;
                DOVirtual.DelayedCall(0.05f, () =>
                {
                    if (IsOpen && dimmerButton != null) dimmerButton.interactable = true;
                });
            }
        }

        public void Close()
        {
            if (!IsOpen) return;

            canvasGroup.DOKill();
            window.DOKill();

            // disabilita input subito
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            IsOpen = false;

            canvasGroup.DOFade(0f, duration);
            window.DOScale(0.8f, duration).SetEase(Ease.InBack)
                .OnComplete(() => gameObject.SetActive(false));

			ApplyFadeTargetsAlpha(1f, fadeTargetsDuration);
        }

    }
}