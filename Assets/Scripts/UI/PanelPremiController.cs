using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Apre/chiude PremiOverlay (stesso pattern fade-only degli altri pannelli). Nessun
    /// sistema di ricompense giornaliere reale nel progetto (verificato: nessuna classe
    /// DailyReward/Reward) - la griglia 7 giorni e' presentazione statica (stessi valori del
    /// mockup), "RISCUOTI" e' uno stub (Debug.Log) invece di finger di accreditare monete
    /// che non esistono in nessun sistema economico reale ancora.
    /// </summary>
    public class PanelPremiController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimmerButton;
        [SerializeField] private Button openButton;
        [SerializeField] private Button claimButton;
        [SerializeField] private float animDuration = 0.22f;

        private Sequence _sequence;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (dimmerButton != null) dimmerButton.onClick.AddListener(Close);
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (claimButton != null) claimButton.onClick.AddListener(() =>
                Debug.Log("[PanelPremiController] Riscuoti: nessun sistema economico/premi giornalieri reale collegato ancora."));
        }

        public void Open()
        {
            if (panelRoot == null) return;

            panelRoot.SetActive(true);

            _sequence?.Kill();
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            _sequence = DOTween.Sequence();
            if (canvasGroup != null) _sequence.Join(canvasGroup.DOFade(1f, animDuration));
            _sequence.SetUpdate(true);
        }

        public void Close()
        {
            if (panelRoot == null) return;

            _sequence?.Kill();

            _sequence = DOTween.Sequence();
            if (canvasGroup != null) _sequence.Join(canvasGroup.DOFade(0f, animDuration));
            _sequence.SetUpdate(true).OnComplete(() => panelRoot.SetActive(false));
        }
    }
}
