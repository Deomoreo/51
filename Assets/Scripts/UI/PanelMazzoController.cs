using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Apre/chiude PanelMazzoOverlay (fade semplice, stessa lezione imparata su Panel
    /// Modalita: uno scale sul frame faceva risaltare troppo il bordo dorato). Aggiorna il
    /// ventaglio/didascalia in anteprima quando si seleziona una
    /// cella (live, senza aspettare conferma) e scrive sul ValueText del DeckSelector in
    /// Home SOLO alla conferma "USA QUESTO" (vedi UI_SPEC_PanelMazzo.md §5).
    ///
    /// Solo Mazzo, niente tab: Accuso/Emoticon vivono nella pagina CARTE dedicata
    /// (DeckPageController/DeckPageBuilder), NON in questo pannello modale - un tentativo
    /// precedente li aveva messi qui per errore, la scorciatoia su Home deve restare
    /// esclusivamente per la scelta rapida del mazzo.
    ///
    /// Vive su un GameObject SEMPRE ATTIVO (sibling dell'overlay, non dentro di esso) per lo
    /// stesso motivo di PanelModalitaController: dentro un overlay disattivato di default,
    /// Awake() non girerebbe finche' il pannello non si apre gia' una volta, e il listener
    /// che deve APRIRLO (sul DeckSelector di Home) non verrebbe mai agganciato.
    /// </summary>
    public class PanelMazzoController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private SelectableToggleGroup deckGroup;
        [SerializeField] private TMP_Text deckSelectorValueText;

        [Header("Animazione apertura/chiusura")]
        [SerializeField] private float animDuration = 0.22f;

        [Header("Anteprima ventaglio")]
        [Tooltip("Le 3 Image del ventaglio (CardArt), stesso ordine con cui vanno aggiornate quando cambia il mazzo selezionato.")]
        [SerializeField] private Image[] fanCardArt;
        [SerializeField] private TMP_Text captionText;
        [SerializeField] private string cardCountSuffix = " · 40 carte";

        [Header("Dati mazzi selezionabili")]
        [Tooltip("Nomi dei soli mazzi SBLOCCATI (stesso ordine/indice degli item in deckGroup) - i bloccati non sono cliccabili quindi non generano mai una selezione.")]
        [SerializeField] private string[] deckLabels;
        [Tooltip("Sprite dorso carta per mazzo sbloccato, stesso ordine di deckLabels. Oggi tutti i mazzi condividono lo stesso sprite placeholder (nessun art dedicato ancora) ma il meccanismo e' gia' dinamico: quando arriveranno sprite dedicati per mazzo bastera' popolare questo array, nessuna riscrittura di logica.")]
        [SerializeField] private Sprite[] deckCardBackSprites;

        private Sequence _sequence;
        private int _selectedDeckIndex;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);

            // Delegate diretto (stesso pattern di PanelModalitaController/SelectableToggleGroup):
            // niente wiring Editor-side per questo collegamento.
            if (deckGroup != null)
            {
                deckGroup.onSelectedRuntime = OnDeckSelected;
            }
        }

        public void Open()
        {
            if (panelRoot == null) return;

            panelRoot.SetActive(true);

            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }

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

        /// <summary>
        /// Agganciato a deckGroup.onSelectedRuntime. Aggiorna SOLO l'anteprima dentro il
        /// pannello (spec §5: la scelta diventa effettiva su Home solo con "USA QUESTO").
        /// </summary>
        public void OnDeckSelected(int index)
        {
            _selectedDeckIndex = index;

            if (deckLabels == null || index < 0 || index >= deckLabels.Length) return;

            if (captionText != null)
            {
                captionText.text = deckLabels[index] + cardCountSuffix;
            }

            var sprite = (deckCardBackSprites != null && index < deckCardBackSprites.Length)
                ? deckCardBackSprites[index]
                : null;
            if (sprite != null && fanCardArt != null)
            {
                foreach (var card in fanCardArt)
                {
                    if (card != null) card.sprite = sprite;
                }
            }
        }

        /// <summary>
        /// "USA QUESTO": conferma la selezione corrente sul ValueText del DeckSelector in
        /// Home e chiude il pannello.
        /// </summary>
        public void OnConfirm()
        {
            if (deckSelectorValueText != null && deckLabels != null
                && _selectedDeckIndex >= 0 && _selectedDeckIndex < deckLabels.Length)
            {
                deckSelectorValueText.text = deckLabels[_selectedDeckIndex];
            }

            Close();
        }
    }
}
