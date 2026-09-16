using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    public sealed class AnimatedModalV2 : MonoBehaviour
    {
        public CanvasGroup Group;
        public RectTransform Frame;
        public Button CloseButton;
        public Button Dimmer;
        private Tween fade;
        private Tween slide;
        private Vector2 origin;
        public bool IsOpen => gameObject.activeSelf;
        private void Awake()
        {
            origin = Frame.anchoredPosition;
            CloseButton.onClick.AddListener(Close);
            if (Dimmer != null) Dimmer.onClick.AddListener(Close);
        }
        public void Open()
        {
            gameObject.SetActive(true);
            fade?.Kill(); slide?.Kill();
            Group.alpha = 0; Group.blocksRaycasts = true; Group.interactable = true;
            Frame.anchoredPosition = origin + Vector2.down * 35;
            fade = Group.DOFade(1, .2f).SetUpdate(true);
            slide = Frame.DOAnchorPos(origin, .25f).SetEase(Ease.OutCubic).SetUpdate(true);
        }
        public void Close()
        {
            if (!IsOpen) return;
            fade?.Kill(); slide?.Kill(); Group.interactable = false;
            slide = Frame.DOAnchorPos(origin + Vector2.down * 25, .16f).SetUpdate(true);
            fade = Group.DOFade(0, .16f).SetUpdate(true).OnComplete(() => { Frame.anchoredPosition = origin; gameObject.SetActive(false); });
        }
        private void Update() { if (Input.GetKeyDown(KeyCode.Escape)) Close(); }
        private void OnDestroy() { fade?.Kill(); slide?.Kill(); }
    }
}
