using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    public class ModeSelectPopupUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button vsBotButton;
        [SerializeField] private Button onlineButton;
        [SerializeField] private Button closeButton; // opzionale
        [SerializeField] private CanvasGroup canvasGroup;

        private HomePanelUI _home;
        private bool _isOpen;

        public void Init(HomePanelUI home)
        {
            _home = home;

            if (!vsBotButton || !onlineButton || canvasGroup == null)
            {
                Debug.LogError("[ModeSelectPopupUI] Missing references in the Inspector.", this);
                enabled = false;
                return;
            }

            vsBotButton.onClick.AddListener(() => Select(GameMode.VsBot));
            onlineButton.onClick.AddListener(() => Select(GameMode.Online));
            if (closeButton) closeButton.onClick.AddListener(Close);

            Close();
        }

        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        public void Open()
        {
            _isOpen = true;
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        public void Close()
        {
            _isOpen = false;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            gameObject.SetActive(false);
        }

        private void Select(GameMode mode)
        {
            if (_home == null)
            {
                Debug.LogError("[ModeSelectPopupUI] HomePanel reference is null (Init not called).", this);
                return;
            }

            Debug.Log($"[ModeSelectPopupUI] Mode selected: {mode}", this);
            _home.SetMode(mode);
            Close();
        }
    }
}
