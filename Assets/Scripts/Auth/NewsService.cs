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

    /// <summary>Notizia letta per la pagina UI51 (NewsService.Parse).</summary>
    public sealed class NewsStory
    {
        public string Id, Title, Subtitle, Tag, Cta;
        public bool Featured;
        public string[] Paragraphs = new string[0];
        public DateTime TimestampUtc;
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

        // --- Pagina UI51 (mockup Notizie): intestazione scritta nel testo della notizia su Game Manager

        /// <summary>Etichette del mockup, nell'ordine dei colori della pagina.</summary>
        public static readonly string[] Tags = { "NOVITÀ", "TORNEO", "COLLEZIONE", "AGGIORNAMENTO", "EVENTO", "AVVISO", "CONSIGLI" };
        public const string DefaultCta = "HO CAPITO";
        public const int MaxFeatured = 3;

        private static readonly string[] MonthsLong =
            { "gennaio", "febbraio", "marzo", "aprile", "maggio", "giugno", "luglio", "agosto", "settembre", "ottobre", "novembre", "dicembre" };

        /// <summary>
        /// Testo della notizia: righe iniziali "chiave: valore" (etichetta, sottotitolo, evidenza, pulsante), poi i paragrafi
        /// separati da una riga vuota. Senza intestazione: NOVITÀ, sottotitolo = primo paragrafo, pulsante HO CAPITO.
        /// </summary>
        public static NewsStory Parse(NewsEntry entry)
        {
            var story = new NewsStory { Id = entry.Id, Title = entry.Title, TimestampUtc = entry.TimestampUtc, Tag = Tags[0], Cta = DefaultCta };
            var lines = (entry.Body ?? string.Empty).Replace("\r", string.Empty).Split('\n');
            int i = 0;
            for (; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                int colon = line.IndexOf(':');
                if (colon <= 0) break;
                string key = line.Substring(0, colon).Trim().ToLowerInvariant(), value = line.Substring(colon + 1).Trim();
                if (key == "etichetta")
                {
                    string tag = value.ToUpperInvariant() == "NOVITA" ? Tags[0] : value.ToUpperInvariant();
                    if (Array.IndexOf(Tags, tag) >= 0) story.Tag = tag;
                }
                else if (key == "sottotitolo") story.Subtitle = value;
                else if (key == "evidenza") story.Featured = Array.IndexOf(new[] { "sì", "si", "yes", "true", "1" }, value.ToLowerInvariant()) >= 0;
                else if (key == "pulsante") { if (value.Length > 0) story.Cta = value.ToUpperInvariant(); }
                else break; // una riga normale con i due punti: da qui e' testo
            }

            var paragraphs = new List<string>();
            var current = new System.Text.StringBuilder();
            for (; i <= lines.Length; i++)
            {
                string line = i < lines.Length ? lines[i].Trim() : string.Empty;
                if (line.Length > 0) { if (current.Length > 0) current.Append(' '); current.Append(line); continue; }
                if (current.Length > 0) paragraphs.Add(current.ToString());
                current.Clear();
            }
            story.Paragraphs = paragraphs.ToArray();
            if (string.IsNullOrEmpty(story.Subtitle) && paragraphs.Count > 0) story.Subtitle = paragraphs[0];
            return story;
        }

        /// <summary>
        /// In evidenza (carosello): quelle con "evidenza: sì", al massimo MaxFeatured; se nessuna, la piu' recente.
        /// Le altre vanno nell'elenco. L'ordine (dalla piu' recente) resta quello di stories.
        /// </summary>
        public static void Split(List<NewsStory> stories, List<NewsStory> featured, List<NewsStory> rest)
        {
            featured.Clear();
            rest.Clear();
            foreach (var s in stories) if (s.Featured && featured.Count < MaxFeatured) featured.Add(s);
            if (featured.Count == 0 && stories.Count > 0) featured.Add(stories[0]);
            foreach (var s in stories) if (!featured.Contains(s)) rest.Add(s);
        }

        /// <summary>"24 settembre" (carosello e articolo) o "24 set" (elenco), come nel mockup.</summary>
        public static string Date(DateTime date, bool shortMonth)
        {
            string month = MonthsLong[date.Month - 1];
            return date.Day + " " + (shortMonth ? month.Substring(0, 3) : month);
        }

        private static void DevLog(string message)
        {
            if (Debug.isDebugBuild) Debug.LogWarning("[News] " + message);
        }
    }
}
