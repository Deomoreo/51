using System;
using DG.Tweening;
using Project51.Core;
using Project51.UI51;
using Project51.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Impostazioni in partita, aperte dall'ingranaggio del tavolo: musica, effetti, vibrazione
    /// (GameAudioPreferences, GamePreferences), grafica ridotta e suggerimenti mosse.
    /// La partita non si ferma: dietro al pannello c'e' una foto sfocata del tavolo.
    /// UI51 Fase 5 (S9, Tools/UI51/Build Fase 5): foglio dal basso UI51Options dentro Panel e finestra
    /// "Abbandonare la partita?" (OpenLeave, dal pulsante Abbandona della barra in alto).
    /// </summary>
    public sealed class InGameSettingsV2 : MonoBehaviour
    {
        public GameObject Panel;
        public CanvasGroup Group;
        public BackdropBlur Blur;
        public Button OpenButton;
        public Button Close;
        [Tooltip("Tocco fuori dalla cornice: chiude come la X.")]
        public Button Backdrop;

        [Header("UI51")]
        public RectTransform Sheet;
        public UI51Toggle MusicSwitch;
        public UI51Toggle EffectsSwitch;
        public UI51Toggle VibrationSwitch;
        public UI51Toggle GraphicsSwitch;
        public UI51Toggle HintsSwitch;
        public GameObject LeaveDialog;
        public RectTransform LeaveCard;
        public TMP_Text LeaveBody;

        public const string LeaveLost = "La partita verrà contata come persa.";
        /// <summary>Avviso di moderazione (scelta dell'utente 01/10: si dice e basta, senza contare quanti ne mancano).</summary>
        public const string AbandonWarning = "Chi abbandona spesso le partite online viene sospeso per un po’.";

        private bool opening;
        private AnimatedModalV2 panelMotion;
        private bool lastReducedGraphics;
        private CanvasGroup captureMask;
        private Coroutine backdropRefresh;
        private bool leaving;

        public bool IsOpen => Panel != null && Panel.activeSelf;
        public bool IsLeaveOpen => LeaveDialog != null && LeaveDialog.activeSelf;

        private void Awake()
        {
            if (MusicSwitch != null) MusicSwitch.onValueChanged.AddListener(GameAudioPreferences.SetMusicEnabled);
            if (EffectsSwitch != null) EffectsSwitch.onValueChanged.AddListener(GameAudioPreferences.SetEffectsEnabled);
            if (VibrationSwitch != null) VibrationSwitch.onValueChanged.AddListener(GamePreferences.SetVibrationEnabled);
            if (GraphicsSwitch != null) GraphicsSwitch.onValueChanged.AddListener(GamePreferences.SetReducedGraphics);
            if (HintsSwitch != null) HintsSwitch.onValueChanged.AddListener(GamePreferences.SetMoveHints);
            if (OpenButton != null) OpenButton.onClick.AddListener(Open);
            Close.onClick.AddListener(Hide);
            if (Backdrop != null) Backdrop.onClick.AddListener(Hide);
            GamePreferences.Changed += RefreshGraphicsChoice;
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
            if (MusicSwitch != null)
            {
                GameAudioPreferences.FoldMasterIntoChannels(); // come le Impostazioni della Home
                MusicSwitch.SetIsOn(GameAudioPreferences.MusicChoice, false, false);
            }
            if (EffectsSwitch != null) EffectsSwitch.SetIsOn(GameAudioPreferences.EffectsChoice, false, false);
            if (VibrationSwitch != null) VibrationSwitch.SetIsOn(GamePreferences.VibrationEnabled, false, false);
            if (HintsSwitch != null) HintsSwitch.SetIsOn(GamePreferences.MoveHints, false, false);

            // Il pannello c'e' gia' (blocca i tocchi) ma e' trasparente: la foto del tavolo non lo contiene.
            if (panelMotion == null)
            {
                panelMotion = Panel.GetComponent<AnimatedModalV2>();
                if (panelMotion == null) panelMotion = Panel.AddComponent<AnimatedModalV2>();
                panelMotion.Group = Group;
                panelMotion.Frame = null; // solo dissolvenza: il foglio UI51 sale da se' (UIAnim.SheetUp)
                panelMotion.HandleEscape = false;
            }
            Group.alpha = 0f;
            Group.blocksRaycasts = true;
            Group.interactable = false;
            Panel.SetActive(true);
            if (Blur != null) yield return Blur.Capture();

            panelMotion.Open();
            if (Sheet != null)
            {
                // Sopra la fascia dell'indicatore home, come i fogli della Home: l'abbondanza sotto il foglio copre lo stacco.
                var holder = (RectTransform)Sheet.parent;
                var canvas = holder.GetComponentInParent<Canvas>();
                float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
                holder.anchoredPosition = new Vector2(0f, Mathf.Max(0f, Screen.safeArea.yMin) / scale);
                LayoutRebuilder.ForceRebuildLayoutImmediate(Sheet); // la prima apertura scorre per l'altezza vera
                UIAnim.SheetUp(Sheet);
            }
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

        /// <summary>Pulsante Abbandona della barra in alto: finestra di conferma.</summary>
        public void OpenLeave()
        {
            if (IsLeaveOpen || leaving) return;
            var mode = GameModeService.Current;
            var cfg = GameSceneInitializer.ActiveConfig;
            // Stessa fonte di TurnController.StartNewGame: vale anche prima che arrivi lo stato della partita.
            int players = cfg != null ? cfg.PlayerCount : 4;
            bool teams = cfg != null && cfg.Format == GameFormat.TwoVsTwo;
            // Chi e' nella finestra di rientro gioca gia' come bot ma e' ancora nella stanza: conta come persona, col suo nome
            // (PlayerName direbbe "Bot N").
            string NameOf(int p)
            {
                var owner = GameSocialV2.PlayerAt(p);
                string nick = owner != null ? (owner.NickName ?? string.Empty).Replace("<", string.Empty).Trim() : string.Empty;
                return nick.Length > 0 ? nick : GameSocialV2.PlayerName(p);
            }
            LeaveBody.text = LeaveMessage(mode.LocalPlayerIndex, players, teams, mode.IsMultiplayer,
                p => mode.IsHumanPlayer(p) || GameSocialV2.PlayerAt(p) != null, NameOf);
            if (Project51.Auth.ModerationService.AbandonCounts) LeaveBody.text += " " + AbandonWarning;
            GameAudio.PlayUi(SoundId.PopupOpen);
            LeaveDialog.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(LeaveCard);
            // Pivot al centro (il pop del mockup cresce dal centro), bordo alto dove l'ha messo il builder.
            LeaveCard.anchoredPosition = new Vector2(0f, -LeaveCard.rect.height * 0.5f);
            UIAnim.Pop(LeaveCard);
        }

        public void CloseLeave()
        {
            if (!IsLeaveOpen || leaving) return;
            GameAudio.PlayUi(SoundId.PopupClose);
            LeaveDialog.SetActive(false);
        }

        public void ConfirmLeave()
        {
            if (leaving) return;
            leaving = true;
            MatchResultsV2.RecordAbandon();
            AppFlowManager.LeaveGameAndGoToMenu();
        }

        /// <summary>
        /// Testo della finestra Abbandona. Sempre: conta come persa (RecordAbandon). Online con un'altra persona al tavolo
        /// il posto passa a un bot e la partita va avanti; altrimenti in 1 contro 1 vince l'avversario.
        /// </summary>
        public static string LeaveMessage(int local, int players, bool teams, bool online, Func<int, bool> isHuman, Func<int, string> nameOf)
        {
            bool others = false;
            for (int p = 0; p < players; p++) if (p != local && isHuman(p)) others = true;
            if (online && others)
            {
                int keeper = teams ? (local + 2) % 4 : players == 2 ? 1 - local : -1;
                return LeaveLost + " Il tuo posto verrà preso da un bot e " +
                       (keeper >= 0 && isHuman(keeper) ? nameOf(keeper) + " continuerà la partita." : "la partita continuerà.");
            }
            return players == 2 ? LeaveLost + " La vittoria andrà a " + nameOf(1 - local) + "." : LeaveLost;
        }

        private void RefreshGraphicsChoice()
        {
            bool reduced = GamePreferences.ReducedGraphics;
            bool restore = lastReducedGraphics && !reduced;
            lastReducedGraphics = reduced;
            if (GraphicsSwitch != null) GraphicsSwitch.SetIsOn(reduced, false);
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

        private void Update()
        {
            if (IsLeaveOpen && Input.GetKeyDown(KeyCode.Escape)) { CloseLeave(); return; }
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Hide();
        }

        private void OnDestroy()
        {
            GamePreferences.Changed -= RefreshGraphicsChoice;
        }
    }
}
