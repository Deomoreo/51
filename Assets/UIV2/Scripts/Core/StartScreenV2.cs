using Project51.Auth;
using Project51.Core;
using Project51.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    [DefaultExecutionOrder(200)]
    public sealed class StartScreenV2 : MonoBehaviour
    {
        public CanvasGroup View;
        public AuthUIController AuthUI;
        [Tooltip("UI51 Fase 12: Benvenuto al primo ingresso e Regole al ritorno da \"Leggi tutte le regole\" del tutorial.")]
        public Project51.Unity.UI.UI51WelcomeView Welcome;
        public Project51.Unity.UI.UI51RulesView Rules;
        [Tooltip("UI51 Fase 11, moderazione: Esito segnalazione e Gioco online sospeso all'arrivo in Home (solo account veri).")]
        public Project51.Unity.UI.UI51ReportOutcomeView ReportOutcome;
        public Project51.Unity.UI.UI51SuspensionView Suspension;
        [Tooltip("UI51 Fase 15: LivelloSu al ritorno in Home dopo partite che hanno fatto salire di livello.")]
        public Project51.Unity.UI.UI51LevelUpView LevelUp;
        private bool entered;

        /// <summary>Vero dopo che si e' entrati in Home: la schermata iniziale non tornera' piu'.</summary>
        public bool HasEntered => entered;

        private void Start()
        {
            AuthUI.OnPlayPressed += Enter;
            AuthUI.OnLoginSuccess += Enter;
            AuthUI.OnRegistrationSuccess += Enter;
            AuthUI.OnClosed += AuthClosed;
            // UI51 Fase 2: la prima schermata e' il Login (mockup Main); la vecchia vista resta spenta.
            if (AppFlowManager.ConsumeReturnToHome()) CompleteEntrance();
            else { View.alpha = 0; View.blocksRaycasts = false; View.interactable = false; Login(); }
        }

        /// <summary>Identita' ospite temporanea e ingresso in Home. Anche dal pulsante "Accedi come ospite" della schermata Accesso (mockup 23).</summary>
        public void PlayAsGuest()
        {
            AuthBootstrapper.Instance?.PlayFabAuth?.ForceGuestIdentity();
            Enter();
        }
        private void Login() { AuthUI.ShowAuthUI(); AuthUI.ShowLoginPanel(); }
        private void AuthClosed() { if (!entered) Login(); }
        private void Enter()
        {
            if (AppLoadingView.Instance != null) AppLoadingView.Instance.EnterHome(CompleteEntrance);
            else CompleteEntrance();
        }
        private void CompleteEntrance()
        {
            if (this == null) return; // rientro al tavolo finito prima: MainMenu non c'e' piu'
            entered = true; AuthUI.HideAuthUI();
            View.alpha = 0; View.blocksRaycasts = false; View.interactable = false;
            if (Project51.Unity.UI.UI51TutorialView.OpenRulesOnReturn && Rules != null)
            {
                Project51.Unity.UI.UI51TutorialView.OpenRulesOnReturn = false;
                Rules.Open();
            }
            else if (Welcome != null && Project51.Unity.UI.UI51WelcomeView.Pending) Welcome.Open();
            else if (LevelUp != null && Project51.Unity.UI.UI51LevelUpView.Pending) LevelUp.Open();
            ModerationService.Refresh((outcome, showSuspension) =>
            {
                if (this == null) return;
                if (outcome != null && ReportOutcome != null) ReportOutcome.Open(outcome);
                else if (showSuspension && Suspension != null) Suspension.Open();
            });
        }
        private void OnDestroy()
        {
            if (AuthUI == null) return;
            AuthUI.OnPlayPressed -= Enter; AuthUI.OnLoginSuccess -= Enter;
            AuthUI.OnRegistrationSuccess -= Enter; AuthUI.OnClosed -= AuthClosed;
        }
    }
}
