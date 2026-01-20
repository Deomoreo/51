using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Project51.Core;

namespace Project51.Unity
{
    /// <summary>
    /// UI per il popup "Unisciti a Stanza Privata".
    /// Permette di inserire il codice stanza.
    /// </summary>
    public class JoinRoomPopupUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private TMP_Text titleText;

        [Header("Animation")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelTransform;
        [SerializeField] private float animDuration = 0.25f;

        public event Action<string> OnJoinRequested;
        public event Action OnCancelled;

        private void Awake()
        {
            if (joinButton != null)
                joinButton.onClick.AddListener(OnJoinClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);

            if (roomCodeInput != null)
            {
                roomCodeInput.characterLimit = 6;
                roomCodeInput.onValueChanged.AddListener(OnCodeInputChanged);
                roomCodeInput.onSubmit.AddListener(_ => OnJoinClicked());
            }

            if (errorText != null)
                errorText.gameObject.SetActive(false);

            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);

            // Reset
            if (roomCodeInput != null)
            {
                roomCodeInput.text = "";
                roomCodeInput.Select();
                roomCodeInput.ActivateInputField();
            }

            if (errorText != null)
                errorText.gameObject.SetActive(false);

            if (joinButton != null)
                joinButton.interactable = false;

            // Animazione
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, animDuration);
            }

            if (panelTransform != null)
            {
                panelTransform.localScale = Vector3.one * 0.8f;
                panelTransform.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack);
            }
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0f, animDuration * 0.5f)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
                
                // Shake del pannello
                if (panelTransform != null)
                {
                    panelTransform.DOShakePosition(0.3f, 10f, 10, 90f, false, true);
                }
            }
        }

        private void OnCodeInputChanged(string code)
        {
            // Converti in uppercase
            if (roomCodeInput != null && !string.IsNullOrEmpty(code))
            {
                roomCodeInput.text = code.ToUpper();
            }

            // Abilita bottone join se il codice ha lunghezza valida
            if (joinButton != null)
            {
                joinButton.interactable = !string.IsNullOrEmpty(code) && code.Length >= 4;
            }

            // Nascondi errore quando l'utente modifica il codice
            if (errorText != null)
                errorText.gameObject.SetActive(false);
        }

        private void OnJoinClicked()
        {
            if (roomCodeInput == null) return;

            string code = roomCodeInput.text.Trim().ToUpper();
            if (string.IsNullOrEmpty(code) || code.Length < 4)
            {
                ShowError("Codice troppo corto!");
                return;
            }

            OnJoinRequested?.Invoke(code);
        }

        private void OnCancelClicked()
        {
            Hide();
            OnCancelled?.Invoke();
        }
    }
}
