using DG.Tweening;
using Project51.Core;
using Project51.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Impostazioni in partita (mockup 26_impostazioni_ingame), aperte dall'ingranaggio del tavolo.
    /// Animazioni veloci e suggerimenti mosse (GamePreferences), musica ed effetti
    /// (GameAudioPreferences), abbandona partita con doppio tocco di conferma.
    /// La partita non si ferma: dietro al pannello c'e' una foto sfocata del tavolo.
    /// Grafica costruita da Tools/UIV2/Build In-Game Settings.
    /// </summary>
    public sealed class InGameSettingsV2 : MonoBehaviour
    {
        public GameObject Panel;
        public CanvasGroup Group;
        public RectTransform Window;
        public BackdropBlur Blur;
        public Button OpenButton;
        public Button Close;
        [Tooltip("Tocco fuori dalla cornice: chiude come la X.")]
        public Button Backdrop;
        public SimpleToggleSwitch FastAnimations;
        public SimpleToggleSwitch MoveHints;
        public SimpleToggleSwitch Music;
        public SimpleToggleSwitch Effects;
        public Button Abandon;
        public TMP_Text AbandonTitle;
        public TMP_Text AbandonSubtitle;

        private const float ConfirmSeconds = 3f;
        private const string AbandonTitleText = "Abbandona partita";
        private const string AbandonSubtitleText = "Conta come sconfitta";

        private bool opening;
        private float confirmUntil = -1f;

        public bool IsOpen => Panel != null && Panel.activeSelf;

        private void Awake()
        {
            if (OpenButton != null) OpenButton.onClick.AddListener(Open);
            Close.onClick.AddListener(Hide);
            if (Backdrop != null) Backdrop.onClick.AddListener(Hide);
            Abandon.onClick.AddListener(OnAbandon);
            FastAnimations.OnChanged += GamePreferences.SetFastAnimations;
            MoveHints.OnChanged += GamePreferences.SetMoveHints;
            Music.OnChanged += GameAudioPreferences.SetMusicEnabled;
            Effects.OnChanged += GameAudioPreferences.SetEffectsEnabled;
            Panel.SetActive(false);
        }

        public void Open()
        {
            if (IsOpen || opening) return;
            // Subito, nello stesso frame del tocco: sostituisce il click dell'ingranaggio.
            GameAudio.PlayUi(SoundId.PopupOpen);
            StartCoroutine(OpenRoutine());
        }

        private System.Collections.IEnumerator OpenRoutine()
        {
            opening = true;
            FastAnimations.SetOn(GamePreferences.FastAnimations);
            MoveHints.SetOn(GamePreferences.MoveHints);
            Music.SetOn(GameAudioPreferences.MusicChoice);
            Effects.SetOn(GameAudioPreferences.EffectsChoice);
            ResetAbandon();

            // Il pannello c'e' gia' (blocca i tocchi) ma e' trasparente: la foto del tavolo non lo contiene.
            Group.DOKill();
            Group.alpha = 0f;
            Group.blocksRaycasts = true;
            Panel.SetActive(true);
            if (Blur != null) yield return Blur.Capture();

            Group.DOFade(1f, 0.18f).SetUpdate(true).SetLink(Panel);
            if (Window != null)
            {
                Window.DOKill();
                Window.localScale = Vector3.one * 0.94f;
                Window.DOScale(1f, 0.22f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(Panel);
            }
            opening = false;
        }

        public void Hide()
        {
            if (!IsOpen) return;
            StopAllCoroutines();
            opening = false;
            GameAudio.PlayUi(SoundId.PopupClose);
            Group.blocksRaycasts = false;
            Group.DOKill();
            Group.DOFade(0f, 0.15f).SetUpdate(true).SetLink(Panel).OnComplete(() => Panel.SetActive(false));
        }

        private void OnAbandon()
        {
            // Primo tocco: chiede conferma. Secondo tocco entro 3 secondi: esce dalla partita.
            if (Time.unscaledTime > confirmUntil)
            {
                confirmUntil = Time.unscaledTime + ConfirmSeconds;
                AbandonTitle.text = "Tocca di nuovo per uscire";
                AbandonSubtitle.text = "La partita conta come sconfitta";
                ((RectTransform)Abandon.transform).DOKill(true);
                ((RectTransform)Abandon.transform).DOPunchScale(Vector3.one * 0.04f, 0.25f, 6, 0.6f).SetUpdate(true);
                return;
            }

            confirmUntil = -1f;
            Abandon.interactable = false;
            AppFlowManager.LeaveGameAndGoToMenu();
        }

        private void ResetAbandon()
        {
            confirmUntil = -1f;
            Abandon.interactable = true;
            AbandonTitle.text = AbandonTitleText;
            AbandonSubtitle.text = AbandonSubtitleText;
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (confirmUntil > 0f && Time.unscaledTime > confirmUntil) ResetAbandon();
            if (Input.GetKeyDown(KeyCode.Escape)) Hide();
        }

        private void OnDestroy()
        {
            FastAnimations.OnChanged -= GamePreferences.SetFastAnimations;
            MoveHints.OnChanged -= GamePreferences.SetMoveHints;
            Music.OnChanged -= GameAudioPreferences.SetMusicEnabled;
            Effects.OnChanged -= GameAudioPreferences.SetEffectsEnabled;
            if (Group != null) Group.DOKill();
            if (Window != null) Window.DOKill();
        }
    }
}
