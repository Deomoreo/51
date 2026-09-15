using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Apre/chiude AmiciOverlay (stesso pattern fade-only di PanelImpostazioniController).
    /// Nessun sistema amici reale nel progetto ancora (verificato: nessuna classe
    /// Friend/FriendsManager) - la lista e' un roster statico di mock (stessi nomi gia'
    /// usati altrove nei mockup: Marco_88, Luca_02, Giulia_R, Andrea99, Paolo_C), Aggiungi/
    /// Invita/Condividi codice sono stub (Debug.Log). Vive su un GameObject sempre attivo,
    /// stesso motivo degli altri controller pannello (Awake non girerebbe mai se il bottone
    /// che apre il pannello vivesse dentro l'overlay disattivato di default).
    /// </summary>
    public class PanelAmiciController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimmerButton;
        [SerializeField] private Button openButton;
        [SerializeField] private Button addFriendButton;
        [SerializeField] private Button shareCodeButton;
        [SerializeField] private float animDuration = 0.22f;

        private Sequence _sequence;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (dimmerButton != null) dimmerButton.onClick.AddListener(Close);
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (addFriendButton != null) addFriendButton.onClick.AddListener(() =>
                Debug.Log("[PanelAmiciController] Aggiungi amico: nessun sistema amici reale collegato ancora."));
            if (shareCodeButton != null) shareCodeButton.onClick.AddListener(() =>
                Debug.Log("[PanelAmiciController] Condividi codice: nessuna integrazione share nativa collegata ancora."));
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
