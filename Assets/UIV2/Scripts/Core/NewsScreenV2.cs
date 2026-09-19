using System;
using System.Collections.Generic;
using DG.Tweening;
using Project51.Auth;
using Project51.UIV2.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Pagina Novita' (C3, mockup 25), aperta dal pulsante "Novita'" della schermata iniziale. Pagina a
    /// tutto schermo come Accesso/Registrazione, non un modal. Il contenuto arriva da NewsService
    /// (PlayFab Title News); qui solo stati a schermo: caricamento, elenco, vuoto, errore.
    /// Grafica costruita da Tools/UIV2/Build News Screen.
    /// </summary>
    public sealed class NewsScreenV2 : MonoBehaviour
    {
        public CanvasGroup View;
        public Button Back;
        public ScrollRect Scroll;
        public RectTransform Content;
        public NewsItemViewV2 ItemTemplate;
        public GameObject EndLabel;
        public TMP_Text Status;

        public const string LoadingText = "Caricamento…";
        public const string EmptyText = "Nessuna novità per ora.\nTorna a trovarci presto!";
        public const string ErrorText = "Non è stato possibile caricare le novità.\nControlla la connessione e riprova.";

        private readonly List<NewsItemViewV2> items = new List<NewsItemViewV2>();
        private bool waitingForAuth;
        private Tween fade;

        public bool IsOpen => View != null && View.blocksRaycasts;

        private void Awake()
        {
            Back.onClick.AddListener(Close);
            ItemTemplate.gameObject.SetActive(false);
            SetVisible(false, instant: true);
        }

        public void Open()
        {
            SetVisible(true, instant: false);
            ShowStatus(LoadingText);
            Load();
        }

        public void Close()
        {
            if (!IsOpen) return;
            NewsService.MarkAllSeen(DateTime.UtcNow);
            StopWaitingForAuth();
            SetVisible(false, instant: false);
        }

        private void Load()
        {
            // All'avvio la sessione PlayFab puo' essere ancora in apertura: si riprova appena e' pronta.
            var bootstrapper = AuthBootstrapper.Instance;
            if (bootstrapper != null && !bootstrapper.IsReady && !bootstrapper.HasError)
            {
                if (!waitingForAuth) { waitingForAuth = true; bootstrapper.OnAuthReady += OnAuthReady; }
                return;
            }
            NewsService.Fetch(Show, () => { if (this != null && IsOpen) ShowStatus(ErrorText); });
        }

        private void OnAuthReady()
        {
            StopWaitingForAuth();
            if (IsOpen) Load();
        }

        private void StopWaitingForAuth()
        {
            if (!waitingForAuth) return;
            waitingForAuth = false;
            if (AuthBootstrapper.Instance != null) AuthBootstrapper.Instance.OnAuthReady -= OnAuthReady;
        }

        private void Show(List<NewsEntry> news)
        {
            if (this == null || !IsOpen) return;
            Clear();
            if (news.Count == 0) { ShowStatus(EmptyText); return; }

            Status.gameObject.SetActive(false);
            var now = DateTime.UtcNow;
            var lastSeen = NewsService.LastSeenUtc;
            foreach (var entry in news)
            {
                var item = Instantiate(ItemTemplate, Content);
                item.name = "News_" + entry.Id;
                item.gameObject.SetActive(true);
                item.Bind(entry.Title, entry.Body, NewsService.RelativeTime(entry.TimestampUtc, now),
                    NewsService.IsNew(entry.TimestampUtc, lastSeen, now));
                items.Add(item);
            }
            EndLabel.SetActive(true);
            EndLabel.transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            Scroll.verticalNormalizedPosition = 1f;
        }

        private void ShowStatus(string text)
        {
            Clear();
            Status.text = text;
            Status.gameObject.SetActive(true);
        }

        private void Clear()
        {
            foreach (var item in items) if (item != null) Destroy(item.gameObject);
            items.Clear();
            EndLabel.SetActive(false);
        }

        private void SetVisible(bool visible, bool instant)
        {
            fade?.Kill();
            View.blocksRaycasts = visible;
            View.interactable = visible;
            if (instant) View.alpha = visible ? 1f : 0f;
            else fade = View.DOFade(visible ? 1f : 0f, 0.2f).SetUpdate(true).SetLink(gameObject);
        }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private void OnDestroy()
        {
            fade?.Kill();
            StopWaitingForAuth();
        }
    }
}
