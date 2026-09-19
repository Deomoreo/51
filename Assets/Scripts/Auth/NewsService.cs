using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Project51.Auth
{
    /// <summary>Una novita' pronta da mostrare: niente tipi PlayFab fuori da questo file.</summary>
    public readonly struct NewsEntry
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Body;
        public readonly DateTime TimestampUtc;

        public NewsEntry(string id, string title, string body, DateTime timestampUtc)
        {
            Id = id;
            Title = title;
            Body = body;
            TimestampUtc = timestampUtc;
        }
    }

    /// <summary>
    /// Novita' (C3, mockup 25). Il contenuto viene da PlayFab Title News, che si scrive da Game Manager
    /// (Content -> Title News) senza una nuova build. La lettura usa la sessione PlayFab gia' aperta
    /// all'avvio, quindi non chiede nulla all'utente e non dipende da ospite/login.
    ///
    /// "Nuovo" = pubblicata dopo l'ultima volta che l'utente ha chiuso la pagina (o, la prima volta,
    /// negli ultimi FirstVisitNewDays giorni). Il segno si salva sul dispositivo.
    /// </summary>
    public static class NewsService
    {
        private const string LastSeenKey = "News.LastSeenUtcTicks";
        private const int MaxItems = 20;
        public const int FirstVisitNewDays = 14;

        /// <summary>
        /// Chiede le novita' a PlayFab, dalla piu' recente. onError riceve solo un motivo per il log di
        /// sviluppo: a schermo va un testo fisso.
        /// </summary>
        public static void Fetch(Action<List<NewsEntry>> onSuccess, Action onError)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                DevLog("sessione PlayFab non ancora pronta.");
                onError?.Invoke();
                return;
            }

            PlayFabClientAPI.GetTitleNews(new GetTitleNewsRequest { Count = MaxItems },
                result =>
                {
                    var list = new List<NewsEntry>();
                    if (result.News != null)
                    {
                        foreach (var item in result.News)
                        {
                            if (item == null || string.IsNullOrWhiteSpace(item.Title)) continue;
                            var utc = DateTime.SpecifyKind(item.Timestamp, DateTimeKind.Utc);
                            list.Add(new NewsEntry(item.NewsId, item.Title.Trim(), (item.Body ?? string.Empty).Trim(), utc));
                        }
                    }
                    list.Sort((a, b) => b.TimestampUtc.CompareTo(a.TimestampUtc));
                    onSuccess?.Invoke(list);
                },
                error =>
                {
                    DevLog("GetTitleNews fallita: " + error.GenerateErrorReport());
                    onError?.Invoke();
                });
        }

        /// <summary>Ultima visita salvata, o null se la pagina non e' mai stata chiusa su questo dispositivo.</summary>
        public static DateTime? LastSeenUtc
        {
            get
            {
                string raw = PlayerPrefs.GetString(LastSeenKey, string.Empty);
                return long.TryParse(raw, out long ticks) ? new DateTime(ticks, DateTimeKind.Utc) : (DateTime?)null;
            }
        }

        public static bool IsNew(DateTime publishedUtc, DateTime? lastSeenUtc, DateTime nowUtc)
        {
            if (lastSeenUtc.HasValue) return publishedUtc > lastSeenUtc.Value;
            return nowUtc - publishedUtc <= TimeSpan.FromDays(FirstVisitNewDays);
        }

        /// <summary>Da chiamare quando si chiude la pagina: le novita' viste smettono di essere "nuove".</summary>
        public static void MarkAllSeen(DateTime nowUtc)
        {
            PlayerPrefs.SetString(LastSeenKey, nowUtc.Ticks.ToString());
            PlayerPrefs.Save();
        }

        /// <summary>"oggi", "ieri", "3 giorni fa", "1 settimana fa", "2 mesi fa", "1 anno fa".</summary>
        public static string RelativeTime(DateTime thenUtc, DateTime nowUtc)
        {
            int days = (int)Math.Floor((nowUtc - thenUtc).TotalDays);
            if (days <= 0) return "oggi";
            if (days == 1) return "ieri";
            if (days < 7) return days + " giorni fa";
            if (days < 30) { int w = days / 7; return w == 1 ? "1 settimana fa" : w + " settimane fa"; }
            if (days < 365) { int m = days / 30; return m == 1 ? "1 mese fa" : m + " mesi fa"; }
            int y = days / 365;
            return y == 1 ? "1 anno fa" : y + " anni fa";
        }

        private static void DevLog(string message)
        {
            if (Debug.isDebugBuild) Debug.LogWarning("[News] " + message);
        }
    }
}
