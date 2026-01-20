using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Project51.Core;

namespace Project51.Unity
{
    /// <summary>
    /// UI per mostrare lo stato del matchmaking (ricerca, connessione, ecc.)
    /// </summary>
    public class MatchmakingStatusUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Image loadingSpinner;

        [Header("Animation")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float spinnerSpeed = 180f;

        public event Action OnCancelRequested;

        private bool _isSpinning;

        private void Awake()
        {
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_isSpinning && loadingSpinner != null)
            {
                loadingSpinner.transform.Rotate(0f, 0f, -spinnerSpeed * Time.deltaTime);
            }
        }

        public void Show(string status, string detail = "")
        {
            gameObject.SetActive(true);

            if (statusText != null)
                statusText.text = status;

            if (detailText != null)
            {
                detailText.text = detail;
                detailText.gameObject.SetActive(!string.IsNullOrEmpty(detail));
            }

            StartSpinner();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, 0.2f);
            }
        }

        public void UpdateStatus(string status, string detail = "")
        {
            if (statusText != null)
                statusText.text = status;

            if (detailText != null)
            {
                detailText.text = detail;
                detailText.gameObject.SetActive(!string.IsNullOrEmpty(detail));
            }
        }

        public void Hide()
        {
            StopSpinner();

            if (canvasGroup != null)
            {
                DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0f, 0.15f)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void ShowError(string error)
        {
            StopSpinner();

            if (statusText != null)
                statusText.text = "Errore";

            if (detailText != null)
            {
                detailText.text = error;
                detailText.gameObject.SetActive(true);
            }
        }

        private void StartSpinner()
        {
            _isSpinning = true;
            if (loadingSpinner != null)
                loadingSpinner.gameObject.SetActive(true);
        }

        private void StopSpinner()
        {
            _isSpinning = false;
            if (loadingSpinner != null)
                loadingSpinner.gameObject.SetActive(false);
        }

        private void OnCancelClicked()
        {
            OnCancelRequested?.Invoke();
        }
    }
}
