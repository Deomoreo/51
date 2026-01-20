using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Project51.Core;

namespace Project51.Unity
{
    /// <summary>
    /// Sotto-pannello per le opzioni del Tavolo Privato.
    /// Mostra: "Crea Stanza" o "Unisciti con Codice".
    /// </summary>
    public class PrivateRoomOptionsUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button backButton;

        [Header("Format Selection")]
        [SerializeField] private Button format1v1Button;
        [SerializeField] private Button format4pButton;
        [SerializeField] private Button format2v2Button;
        [SerializeField] private TMP_Text formatLabel;

        [Header("Animation")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelTransform;

        // Eventi
        public event Action<MatchConfig> OnCreateRoomRequested;
        public event Action<MatchConfig> OnJoinRoomRequested;
        public event Action OnBackRequested;

        private GameFormat _selectedFormat = GameFormat.FourPlayers;

        private void Awake()
        {
            if (createRoomButton != null)
                createRoomButton.onClick.AddListener(OnCreateClicked);
            if (joinRoomButton != null)
                joinRoomButton.onClick.AddListener(OnJoinClicked);
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            // Format buttons
            if (format1v1Button != null)
                format1v1Button.onClick.AddListener(() => SelectFormat(GameFormat.OneVsOne));
            if (format4pButton != null)
                format4pButton.onClick.AddListener(() => SelectFormat(GameFormat.FourPlayers));
            if (format2v2Button != null)
                format2v2Button.onClick.AddListener(() => SelectFormat(GameFormat.TwoVsTwo));

            UpdateFormatUI();
        }

        public void Show()
        {
            gameObject.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, 0.25f);
            }

            if (panelTransform != null)
            {
                panelTransform.localScale = Vector3.one * 0.9f;
                panelTransform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
            }

            UpdateFormatUI();
        }

        public void Hide()
        {
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

        private void SelectFormat(GameFormat format)
        {
            _selectedFormat = format;
            UpdateFormatUI();
            Debug.Log($"[PrivateRoomOptions] Format selected: {format}");
        }

        private void UpdateFormatUI()
        {
            if (formatLabel != null)
            {
                formatLabel.text = _selectedFormat switch
                {
                    GameFormat.OneVsOne => "1 vs 1",
                    GameFormat.FourPlayers => "4 Giocatori",
                    GameFormat.TwoVsTwo => "2 vs 2",
                    _ => ""
                };
            }

            // Highlight bottone selezionato
            SetButtonHighlight(format1v1Button, _selectedFormat == GameFormat.OneVsOne);
            SetButtonHighlight(format4pButton, _selectedFormat == GameFormat.FourPlayers);
            SetButtonHighlight(format2v2Button, _selectedFormat == GameFormat.TwoVsTwo);
        }

        private void SetButtonHighlight(Button btn, bool selected)
        {
            if (btn == null) return;

            var colors = btn.colors;
            colors.normalColor = selected ? new Color(0.3f, 0.6f, 0.9f, 1f) : Color.white;
            btn.colors = colors;
        }

        private MatchConfig CreateConfig()
        {
            return new MatchConfig
            {
                Intent = MatchIntent.PrivateRoom,
                Format = _selectedFormat,
                TargetScore = 51
            };
        }

        private void OnCreateClicked()
        {
            var config = CreateConfig();
            config.IsHost = true;
            OnCreateRoomRequested?.Invoke(config);
        }

        private void OnJoinClicked()
        {
            var config = CreateConfig();
            config.IsHost = false;
            OnJoinRoomRequested?.Invoke(config);
        }

        private void OnBackClicked()
        {
            Hide();
            OnBackRequested?.Invoke();
        }
    }
}
