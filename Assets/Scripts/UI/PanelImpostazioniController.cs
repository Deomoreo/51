using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Apre/chiude ImpostazioniOverlay (stesso pattern fade-only di PanelMazzoController/
    /// PanelModalitaController). Le 5 righe toggle (Musica/Effetti sonori/Vibrazione/
    /// Animazioni veloci/Notifiche) persistono in PlayerPrefs - nessun AudioManager/
    /// SoundManager esiste ancora nel progetto (verificato), quindi qui salviamo solo la
    /// preferenza reale invece di far finta di controllare un sistema audio che non c'e'.
    /// Lingua/Supporto/Esci dall'account sono stub (Debug.Log) - fuori scope per questa
    /// passata, nessun sistema di localizzazione o account reale da collegare ancora.
    /// Vive su un GameObject sempre attivo (sibling dell'overlay), stesso motivo degli altri
    /// controller pannello: dentro un overlay disattivato di default Awake() non gira finche'
    /// il pannello non si apre gia' una volta, e il bottone che deve APRIRLO non si aggancerebbe mai.
    /// </summary>
    public class PanelImpostazioniController : MonoBehaviour
    {
        private const string PrefPrefix = "Settings_";

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton;
        [SerializeField] private Button dimmerButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private float animDuration = 0.22f;

        [Header("Toggle (chiave PlayerPrefs, default on/off)")]
        [SerializeField] private SimpleToggleSwitch musicaToggle;
        [SerializeField] private SimpleToggleSwitch effettiToggle;
        [SerializeField] private SimpleToggleSwitch vibrazioneToggle;
        [SerializeField] private SimpleToggleSwitch animazioniVelociToggle;
        [SerializeField] private SimpleToggleSwitch notificheToggle;

        private Sequence _sequence;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (dimmerButton != null) dimmerButton.onClick.AddListener(Close);
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (logoutButton != null) logoutButton.onClick.AddListener(() =>
                Debug.Log("[PanelImpostazioniController] Esci dall'account: nessun sistema di autenticazione reale collegato ancora."));

            LoadToggle(musicaToggle, "Musica", true);
            LoadToggle(effettiToggle, "Effetti", true);
            LoadToggle(vibrazioneToggle, "Vibrazione", false);
            LoadToggle(animazioniVelociToggle, "AnimazioniVeloci", false);
            LoadToggle(notificheToggle, "Notifiche", true);
        }

        private void LoadToggle(SimpleToggleSwitch toggle, string key, bool defaultOn)
        {
            if (toggle == null) return;

            bool value = PlayerPrefs.GetInt(PrefPrefix + key, defaultOn ? 1 : 0) == 1;
            toggle.SetOn(value);
            toggle.OnChanged = on => PlayerPrefs.SetInt(PrefPrefix + key, on ? 1 : 0);
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
