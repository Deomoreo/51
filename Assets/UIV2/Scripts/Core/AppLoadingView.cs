using System;
using System.Collections;
using DG.Tweening;
using Project51.Auth;
using Project51.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    public sealed class AppLoadingView : MonoBehaviour
    {
        public static AppLoadingView Instance { get; private set; }
        public CanvasGroup View;
        public TMP_Text Status;
        public Image Progress;
        public RectTransform[] Cards;
        public Button RetryButton;
        public Button CancelButton;
        [Min(0)] public float MinimumDuration = 3f;
        public bool IsVisible => View != null && View.gameObject.activeSelf;
        private Coroutine operation;
        private Tween fade;
        private bool loadingScene;
        private bool indeterminate;
        private Action entranceReady;
        private string failedScene;
        private Vector2[] cardPositions;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            AppLoading.SceneRequested += LoadScene;
            AppLoading.AuthenticationBusy += AuthenticationBusy;
            RetryButton.onClick.AddListener(Retry);
            CancelButton.onClick.AddListener(Cancel);
            cardPositions = new Vector2[Cards.Length];
            for (int i = 0; i < Cards.Length; i++) cardPositions[i] = Cards[i].anchoredPosition;
            View.gameObject.SetActive(false);
        }
        private void Show(string text)
        {
            fade?.Kill(); View.gameObject.SetActive(true); View.alpha = 1;
            View.blocksRaycasts = true; View.interactable = true; Status.text = text;
            RetryButton.gameObject.SetActive(false); CancelButton.gameObject.SetActive(false);
            AppLoading.SetCovering(true);
        }
        private void Hide()
        {
            fade?.Kill();
            // Il velo conta come "alzato" finche' non e' davvero sparito: chi aspetta di poter
            // partire (l'intro del tavolo) non deve cominciare durante la dissolvenza.
            fade = View.DOFade(0, .2f).SetUpdate(true)
                .OnComplete(() => { View.gameObject.SetActive(false); AppLoading.SetCovering(false); });
        }
        private void AuthenticationBusy(bool busy, string message)
        {
            if (loadingScene || operation != null) return;
            if (busy) { Show(string.IsNullOrEmpty(message) ? "Accesso in corso…" : message); indeterminate = true; }
            else Hide();
        }
        public void EnterHome(Action ready)
        {
            if (operation != null || loadingScene) return;
            entranceReady = ready; failedScene = null;
            operation = StartCoroutine(Entrance());
        }
        private IEnumerator Entrance()
        {
            Show("Preparazione del profilo…"); indeterminate = true;
            float started = Time.realtimeSinceStartup;
            while (AuthBootstrapper.Instance == null || !AuthBootstrapper.Instance.IsReady)
            {
                var auth = AuthBootstrapper.Instance;
                if (auth != null && auth.HasError || Time.realtimeSinceStartup - started > 30)
                {
                    operation = null; Fail("Connessione non riuscita. Puoi riprovare."); yield break;
                }
                Status.text = auth != null && auth.CurrentState == AuthState.ConnectingPhoton ? "Connessione al gioco…" : "Accesso in corso…";
                yield return null;
            }
            Status.text = "Preparazione delle carte…";
            yield return PreloadDeck();
            indeterminate = false;
            while (Time.realtimeSinceStartup - started < MinimumDuration)
            {
                Progress.fillAmount = Mathf.Clamp01((Time.realtimeSinceStartup-started)/MinimumDuration);
                yield return null;
            }
            Progress.fillAmount = 1;
            var ready = entranceReady; entranceReady = null; operation = null;
            ready?.Invoke(); Hide();
        }
        private IEnumerator PreloadDeck()
        {
            var entry = CardDecks.Catalog?.Find(CardDecks.SelectedId);
            if (entry != null) yield return Resources.LoadAsync<CardDeckDefinition>(entry.ResourcePath);
        }
        private void LoadScene(string scene)
        {
            if (loadingScene || operation != null) return;
            failedScene = scene; loadingScene = true;
            operation = StartCoroutine(SceneTransition(scene));
        }
        private IEnumerator SceneTransition(string scene)
        {
            Show(scene == "MainMenu" ? "Ritorno alla Home…" : "Preparazione del tavolo…");
            // Gli effetti della schermata che si sta lasciando (fine partita, accusi, carte) non
            // devono accompagnare il caricamento e riaffiorare sulla schermata nuova.
            Project51.Unity.GameAudio.StopAllEffects();
            indeterminate = false; Progress.fillAmount = 0;
            yield return null; // Paint the overlay before loading assets.
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                loadingScene = false; operation = null; Fail("Impossibile aprire questa schermata."); yield break;
            }
            float started = Time.realtimeSinceStartup;
            if (scene == "GameScene") yield return PreloadDeck();
            var load = SceneManager.LoadSceneAsync(scene);
            load.allowSceneActivation = false;
            while (load.progress < .9f || Time.realtimeSinceStartup - started < MinimumDuration)
            {
                Progress.fillAmount = Mathf.Min(Mathf.Clamp01(load.progress / .9f), Mathf.Clamp01((Time.realtimeSinceStartup-started)/Mathf.Max(.01f,MinimumDuration))) * .95f;
                yield return null;
            }
            load.allowSceneActivation = true;
            yield return load; yield return null; yield return null;
            Progress.fillAmount = 1; loadingScene = false; operation = null; Hide();
        }
        private void Fail(string message)
        {
            indeterminate = false; Status.text = message; Progress.fillAmount = 0;
            RetryButton.gameObject.SetActive(true); CancelButton.gameObject.SetActive(true);
        }
        private void Retry()
        {
            if (failedScene != null) { LoadScene(failedScene); return; }
            AuthBootstrapper.Instance?.StartAuthentication();
            operation = StartCoroutine(Entrance());
        }
        private void Cancel()
        {
            if (loadingScene) return;
            if (operation != null) StopCoroutine(operation);
            operation = null; entranceReady = null; Hide();
        }
        private void Update()
        {
            if (!IsVisible) return;
            if (indeterminate) Progress.fillAmount = .25f + .35f * Mathf.PingPong(Time.unscaledTime * .45f, 1);
            for (int i = 0; i < Cards.Length; i++) Cards[i].anchoredPosition = cardPositions[i] + Vector2.up * (Mathf.Sin(Time.unscaledTime * 3 + i * .7f) * 9);
        }
        private void OnDestroy()
        {
            if (Instance != this) return;
            AppLoading.SceneRequested -= LoadScene; AppLoading.AuthenticationBusy -= AuthenticationBusy;
            fade?.Kill(); Instance = null;
        }
    }
}
