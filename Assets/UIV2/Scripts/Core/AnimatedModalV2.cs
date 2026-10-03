using DG.Tweening;
using Project51.UIV2.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    public sealed class AnimatedModalV2 : MonoBehaviour
    {
        public CanvasGroup Group;
        [Tooltip("Facoltativo: senza cornice la finestra fa solo la dissolvenza.")]
        public RectTransform Frame;
        public Button CloseButton;
        public Button Dimmer;
        public bool HandleEscape = true;
        private Tween motion;
        private Vector2 origin;
        private Vector3 scale;
        private bool initialized;
        private bool opening;
        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            Initialize();
            if (CloseButton != null) CloseButton.onClick.AddListener(Close);
            if (Dimmer != null) Dimmer.onClick.AddListener(Close);
        }

        private bool Initialize()
        {
            if (Group == null) return false;
            if (initialized) return true;
            if (Frame != null)
            {
                origin = Frame.anchoredPosition;
                scale = Frame.localScale;
            }
            initialized = true;
            return true;
        }

        public void Open()
        {
            if (!Initialize() || (opening && gameObject.activeInHierarchy)) return;
            bool reversing = motion != null && motion.IsActive();
            UIV2Motion.Cancel(ref motion);
            opening = true;
            gameObject.SetActive(true);
            if (!reversing)
            {
                Group.alpha = 0;
                if (Frame != null)
                {
                    Frame.anchoredPosition = origin + Vector2.down * 24;
                    Frame.localScale = scale * UIV2Motion.PanelScale;
                }
            }
            Group.blocksRaycasts = true;
            Group.interactable = true;
            var sequence = DOTween.Sequence().SetUpdate(true)
                .Join(Group.DOFade(1, UIV2Motion.Enter).SetEase(Ease.OutQuad));
            if (Frame != null)
                sequence.Join(Frame.DOAnchorPos(origin, UIV2Motion.Enter).SetEase(Ease.OutCubic))
                    .Join(Frame.DOScale(scale, UIV2Motion.Enter).SetEase(Ease.OutBack, 1.1f));
            motion = sequence;
        }

        public void Close()
        {
            if (!IsOpen || !Initialize()) return;
            if (!opening && motion != null && motion.IsActive()) return;
            opening = false;
            UIV2Motion.Cancel(ref motion);
            Group.interactable = false;
            var sequence = DOTween.Sequence().SetUpdate(true)
                .Join(Group.DOFade(0, UIV2Motion.Exit));
            if (Frame != null)
                sequence.Join(Frame.DOAnchorPos(origin + Vector2.down * 18, UIV2Motion.Exit).SetEase(Ease.InCubic))
                    .Join(Frame.DOScale(scale * UIV2Motion.PanelScale, UIV2Motion.Exit).SetEase(Ease.InCubic));
            motion = sequence.OnComplete(CloseImmediate);
        }

        public void CloseImmediate()
        {
            ResetPresentation();
            gameObject.SetActive(false);
        }

        private void ResetPresentation()
        {
            UIV2Motion.Cancel(ref motion);
            opening = false;
            if (Group != null) { Group.alpha = 0; Group.blocksRaycasts = false; Group.interactable = false; }
            if (!initialized || Frame == null) return;
            Frame.anchoredPosition = origin;
            Frame.localScale = scale;
        }

        private void Update() { if (HandleEscape && Input.GetKeyDown(KeyCode.Escape)) Close(); }
        private void OnDisable() => ResetPresentation();
        private void OnDestroy() => UIV2Motion.Cancel(ref motion);
    }
}
