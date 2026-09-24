using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Project51.UIV2.Core;
using Project51.UIV2.Animations;

namespace Project51.UIV2.Components
{
    public class UIV2ModalFrame : MonoBehaviour, IUIV2Modal
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimmerButton;
        [SerializeField] private TMP_Text ribbonTitle;
        [SerializeField] private RectTransform contentArea;
        [SerializeField] private RectTransform frame;
        public RectTransform ContentArea => contentArea;
        public bool IsOpen => root != null && root.activeSelf;
        private Tween motion;
        private Vector3 scale;
        private bool initialized;
        private bool opening;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (dimmerButton != null) dimmerButton.onClick.AddListener(Close);
        }

        public void SetTitle(string title) { if (ribbonTitle != null) ribbonTitle.text = title; }

        private void Initialize()
        {
            if (initialized) return;
            if (frame == null && contentArea != null) frame = contentArea.parent as RectTransform;
            // Never shrink a full-screen backdrop.
            if (frame != null && frame.gameObject == root) frame = null;
            scale = frame != null ? frame.localScale : Vector3.one;
            initialized = true;
        }

        public void Open()
        {
            if (root == null || (opening && root.activeInHierarchy)) return;
            Initialize();
            bool reversing = motion != null && motion.IsActive();
            UIV2Motion.Cancel(ref motion);
            opening = true;
            root.SetActive(true);
            var sequence = DOTween.Sequence().SetUpdate(true);
            motion = sequence;
            if (canvasGroup != null)
            {
                if (!reversing) canvasGroup.alpha = 0;
                canvasGroup.interactable = canvasGroup.blocksRaycasts = true;
                sequence.Join(canvasGroup.DOFade(1, UIV2Motion.Enter));
            }
            if (frame != null)
            {
                if (!reversing) frame.localScale = scale * UIV2Motion.PanelScale;
                sequence.Join(frame.DOScale(scale, UIV2Motion.Enter).SetEase(Ease.OutBack, 1.1f));
            }
        }

        public void Close()
        {
            if (!IsOpen) return;
            Initialize();
            opening = false;
            UIV2Motion.Cancel(ref motion);
            var sequence = DOTween.Sequence().SetUpdate(true);
            motion = sequence;
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                sequence.Join(canvasGroup.DOFade(0, UIV2Motion.Exit));
            }
            if (frame != null) sequence.Join(frame.DOScale(scale * UIV2Motion.PanelScale, UIV2Motion.Exit));
            sequence.OnComplete(CloseImmediate);
        }

        public void CloseImmediate()
        {
            ResetPresentation();
            if (root != null) root.SetActive(false);
        }

        private void ResetPresentation()
        {
            UIV2Motion.Cancel(ref motion);
            opening = false;
            if (canvasGroup != null) { canvasGroup.alpha = 0; canvasGroup.interactable = canvasGroup.blocksRaycasts = false; }
            if (initialized && frame != null) frame.localScale = scale;
        }

        private void LateUpdate() { if (opening && root != null && !root.activeInHierarchy) ResetPresentation(); }
        private void OnDisable() => ResetPresentation();
        private void OnDestroy() => UIV2Motion.Cancel(ref motion);
    }
}