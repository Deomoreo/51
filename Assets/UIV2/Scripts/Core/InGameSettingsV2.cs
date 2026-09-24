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
    /// Grafica ridotta e suggerimenti mosse (GamePreferences), musica ed effetti
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
        public SimpleToggleSwitch Vibration;
        public Button Abandon;
        public TMP_Text AbandonTitle;
        public TMP_Text AbandonSubtitle;

        private const float ConfirmSeconds = 3f;
        private const string AbandonTitleText = "Abbandona partita";
        private const string AbandonSubtitleText = "Conta come sconfitta";

        private bool opening;
        private float confirmUntil = -1f;
        private AnimatedModalV2 panelMotion;
        private bool lastReducedGraphics;
        private CanvasGroup captureMask;
        private Coroutine backdropRefresh;

        public bool IsOpen => Panel != null && Panel.activeSelf;

        private void Awake()
        {
            if (OpenButton != null) OpenButton.onClick.AddListener(Open);
            Close.onClick.AddListener(Hide);
            if (Backdrop != null) Backdrop.onClick.AddListener(Hide);
            Abandon.onClick.AddListener(OnAbandon);
            EnsureReducedGraphicsToggle();
            FastAnimations.OnChanged += GamePreferences.SetReducedGraphics;
            GamePreferences.Changed += RefreshGraphicsChoice;
            MoveHints.OnChanged += GamePreferences.SetMoveHints;
            Music.OnChanged += GameAudioPreferences.SetMusicEnabled;
            Effects.OnChanged += GameAudioPreferences.SetEffectsEnabled;
            EnsureVibrationToggle();
            if (Vibration != null) Vibration.OnChanged += GamePreferences.SetVibrationEnabled;
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
            RefreshGraphicsChoice();
            MoveHints.SetOn(GamePreferences.MoveHints);
            Music.SetOn(GameAudioPreferences.MusicChoice);
            Effects.SetOn(GameAudioPreferences.EffectsChoice);
            if (Vibration != null) Vibration.SetOn(GamePreferences.VibrationEnabled);
            ResetAbandon();

            // Il pannello c'e' gia' (blocca i tocchi) ma e' trasparente: la foto del tavolo non lo contiene.
            if (panelMotion == null)
            {
                panelMotion = Panel.GetComponent<AnimatedModalV2>();
                if (panelMotion == null) panelMotion = Panel.AddComponent<AnimatedModalV2>();
                panelMotion.Group = Group;
                panelMotion.Frame = Window;
                panelMotion.HandleEscape = false;
            }
            Group.alpha = 0f;
            Group.blocksRaycasts = true;
            Group.interactable = false;
            Panel.SetActive(true);
            if (Blur != null) yield return Blur.Capture();

            panelMotion.Open();
            opening = false;
        }

        public void Hide()
        {
            if (!IsOpen) return;
            StopAllCoroutines();
            RestoreCaptureVisibility();
            opening = false;
            GameAudio.PlayUi(SoundId.PopupClose);
            if (panelMotion != null) panelMotion.Close();
            else Panel.SetActive(false);
        }

        private void OnAbandon()
        {
            // Primo tocco: chiede conferma. Secondo tocco entro 3 secondi: esce dalla partita.
            if (Time.unscaledTime > confirmUntil)
            {
                confirmUntil = Time.unscaledTime + ConfirmSeconds;
                AbandonTitle.text = "Tocca di nuovo per uscire";
                AbandonSubtitle.text = "La partita conta come sconfitta";
                return;
            }

            confirmUntil = -1f;
            Abandon.interactable = false;
            MatchResultsV2.RecordAbandon();
            AppFlowManager.LeaveGameAndGoToMenu();
        }

        private void ResetAbandon()
        {
            confirmUntil = -1f;
            Abandon.interactable = true;
            AbandonTitle.text = AbandonTitleText;
            AbandonSubtitle.text = AbandonSubtitleText;
        }

        private void RefreshGraphicsChoice()
        {
            bool reduced = GamePreferences.ReducedGraphics;
            bool restore = lastReducedGraphics && !reduced;
            lastReducedGraphics = reduced;
            if (FastAnimations != null) FastAnimations.SetOn(reduced);
            // A panel opened while reduced has no snapshot to restore. Hide its parent
            // for the capture so the modal's own entrance tween cannot enter the photo.
            if (Application.isPlaying && isActiveAndEnabled && restore && IsOpen && !opening && Blur != null && !Blur.HasSnapshot)
            {
                if (backdropRefresh != null) StopCoroutine(backdropRefresh);
                backdropRefresh = StartCoroutine(RefreshBackdrop());
            }
        }

        private System.Collections.IEnumerator RefreshBackdrop()
        {
            if (captureMask == null) captureMask = GetComponent<CanvasGroup>();
            if (captureMask == null) captureMask = gameObject.AddComponent<CanvasGroup>();
            captureMask.alpha = 0f;
            yield return Blur.Capture();
            RestoreCaptureVisibility();
        }

        private void RestoreCaptureVisibility()
        {
            if (captureMask != null) captureMask.alpha = 1f;
            backdropRefresh = null;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            opening = false;
            RestoreCaptureVisibility();
        }

        // Keep the serialized FastAnimations reference so existing scenes remain wired.
        public void EnsureReducedGraphicsToggle()
        {
            if (FastAnimations == null) return;
            var row = FastAnimations.transform.parent;
            row.Find("Title").GetComponent<TMP_Text>().text = "Grafica ridotta";
            row.Find("Subtitle").GetComponent<TMP_Text>().text = "Meno effetti e animazioni più brevi";
            RefreshGraphicsChoice();
        }

        /// <summary>Targeted upgrade shared by the builder and existing scenes at runtime.</summary>
        public void EnsureVibrationToggle()
        {
            if (Vibration != null || Effects == null) return;
            var source = (RectTransform)Effects.transform.parent;
            var existing = source.parent.Find("Vibration");
            if (existing != null) { Vibration = existing.GetComponentInChildren<SimpleToggleSwitch>(true); return; }
            const float step = 118f;
            var clone = Instantiate(source.gameObject, source.parent);
            clone.name = "Vibration";
            var row = (RectTransform)clone.transform;
            row.anchoredPosition = source.anchoredPosition + Vector2.down * step;
            foreach (Transform child in source.parent)
            {
                if (child == row) continue;
                var rect = child as RectTransform;
                if (rect == null) continue;
                if (child.name == "Frame")
                {
                    rect.sizeDelta += Vector2.up * step;
                    rect.anchoredPosition += Vector2.down * step * (1f - rect.pivot.y);
                }
                else if (rect.anchoredPosition.y < source.anchoredPosition.y - 1f)
                    rect.anchoredPosition += Vector2.down * step;
            }
            row.Find("Title").GetComponent<TMP_Text>().text = "Vibrazione";
            row.Find("Subtitle").GetComponent<TMP_Text>().text = "Tocchi e momenti importanti";
            Vibration = row.GetComponentInChildren<SimpleToggleSwitch>(true);
            Vibration.SetOn(GamePreferences.VibrationEnabled);
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (confirmUntil > 0f && Time.unscaledTime > confirmUntil) ResetAbandon();
            if (Input.GetKeyDown(KeyCode.Escape)) Hide();
        }

        private void OnDestroy()
        {
            if (FastAnimations != null) FastAnimations.OnChanged -= GamePreferences.SetReducedGraphics;
            GamePreferences.Changed -= RefreshGraphicsChoice;
            MoveHints.OnChanged -= GamePreferences.SetMoveHints;
            Music.OnChanged -= GameAudioPreferences.SetMusicEnabled;
            Effects.OnChanged -= GameAudioPreferences.SetEffectsEnabled;
            if (Vibration != null) Vibration.OnChanged -= GamePreferences.SetVibrationEnabled;
        }
    }
}
