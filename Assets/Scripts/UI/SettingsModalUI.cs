using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Project51.Unity
{
    public class SettingsModalUI : ModalWindowBaseUI
    {
        [Header("Refs")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform window;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button dimmerButton;

        //[Header("Anim")]
        //[SerializeField] private float duration = 0.25f;

        private void Awake()
        {
            if (btnClose) btnClose.onClick.AddListener(Close);
            if (dimmerButton) dimmerButton.onClick.AddListener(Close);

            // Stato iniziale
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            window.localScale = Vector3.one * 0.9f;

            gameObject.SetActive(false);
        }
    }
}
