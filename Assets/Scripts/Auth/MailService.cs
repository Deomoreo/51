using System;
using System.Collections.Generic;
using System.Globalization;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Project51.Auth
{
    /// <summary>Allegato di un messaggio: tipo "monete", "gemme" o "forziere".</summary>
    [Serializable]
    public sealed class MailGift
    {
        public string tipo;
        public int quantita;
    }

    /// <summary>
    /// Messaggio della Posta, come si scrive nel JSON (campi in italiano). tipo: team, stagione, amico, avviso, torneo
    /// (decide icona e colore). data e scade: "2026-10-01" o con l'ora, UTC.
    /// </summary>
    [Serializable]
    public sealed class MailMessage
    {
        public string id, tipo, titolo, da, testo, data, scade;
        public MailGift[] allegati = new MailGift[0];
        public bool riscattato;

        [NonSerialized] public DateTime DateUtc;
        [NonSerialized] public DateTime? ExpiresUtc;

        public bool HasGifts => allegati != null && allegati.Length > 0;
        public bool CanClaim => HasGifts && !riscattato;
    }

    /// <summary>
    /// Posta (UI51 Fase 8). I messaggi sono nei dati del giocatore in sola lettura su PlayFab, chiave "Posta": il client li legge
    /// soltanto, li scrive Game Manager (Players -> Player Data, Read Only) o, nel giro del server, il CloudScript, che dara'
    /// anche i premi. "Letto" si salva sul dispositivo. Formato: un array JSON di MailMessage (anche dentro {"messaggi": [...]}).
    /// </summary>
    public static class MailService
    {
        public const string DataKey = "Posta";
        public const int KeepDays = 30;
        private const string ReadKey = "Mail.ReadIds";

        [Serializable]
        private sealed class Wrapper { public MailMessage[] messaggi; }

        /// <summary>Chiede i messaggi a PlayFab, dal piu' recente.</summary>
        public static void Fetch(Action<List<MailMessage>> onSuccess, Action onError)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                DevLog("sessione PlayFab non ancora pronta.");
                onError?.Invoke();
                return;
            }
            try
            {
                PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest { Keys = new List<string> { DataKey } },
                    result =>
                    {
                        UserDataRecord record = null;
                        result.Data?.TryGetValue(DataKey, out record);
                        onSuccess?.Invoke(record == null ? new List<MailMessage>()
                            : Parse(record.Value, DateTime.SpecifyKind(record.LastUpdated, DateTimeKind.Utc), DateTime.UtcNow));
                    },
                    error =>
                    {
                        DevLog("GetUserReadOnlyData fallita: " + error.GenerateErrorReport());
                        onError?.Invoke();
                    });
            }
            catch (PlayFabException e)
            {
                DevLog(e.Message);
                onError?.Invoke();
            }
        }

        /// <summary>
        /// JSON -> messaggi visibili, dal piu' recente: niente quelli piu' vecchi di KeepDays ne' quelli scaduti senza essere
        /// riscattati. Senza data vale fallbackUtc (l'ultima modifica del dato), senza id il titolo.
        /// </summary>
        public static List<MailMessage> Parse(string json, DateTime fallbackUtc, DateTime nowUtc)
        {
            var list = new List<MailMessage>();
            json = (json ?? string.Empty).Trim();
            if (json.Length == 0) return list;
            if (json[0] == '[') json = "{\"messaggi\":" + json + "}";
            Wrapper wrapper;
            try { wrapper = JsonUtility.FromJson<Wrapper>(json); }
            catch (ArgumentException e) { DevLog("JSON della Posta non valido: " + e.Message); return list; }
            if (wrapper?.messaggi == null) return list;

            foreach (var m in wrapper.messaggi)
            {
                if (m == null || string.IsNullOrWhiteSpace(m.titolo)) continue;
                m.DateUtc = ParseUtc(m.data) ?? fallbackUtc;
                m.ExpiresUtc = ParseUtc(m.scade);
                if (string.IsNullOrEmpty(m.id)) m.id = m.titolo;
                if (string.IsNullOrWhiteSpace(m.da)) m.da = "Team 51";
                if (m.allegati == null) m.allegati = new MailGift[0];
                if (nowUtc - m.DateUtc > TimeSpan.FromDays(KeepDays)) continue;
                if (m.CanClaim && m.ExpiresUtc.HasValue && m.ExpiresUtc.Value <= nowUtc) continue;
                list.Add(m);
            }
            list.Sort((a, b) => b.DateUtc.CompareTo(a.DateUtc));
            return list;
        }

        /// <summary>"Scade oggi", "Scade domani", "Scade tra 5 giorni"; vuoto se non scade o e' gia' riscattato.</summary>
        public static string ExpiryLabel(MailMessage m, DateTime nowUtc)
        {
            if (!m.CanClaim || !m.ExpiresUtc.HasValue) return string.Empty;
            int days = (int)Math.Floor((m.ExpiresUtc.Value - nowUtc).TotalDays);
            return days <= 0 ? "Scade oggi" : days == 1 ? "Scade domani" : "Scade tra " + days + " giorni";
        }

        public static bool IsRead(MailMessage m) => ReadIds().Contains(m.id);

        public static int UnreadCount(List<MailMessage> messages)
        {
            var read = ReadIds();
            int n = 0;
            foreach (var m in messages) if (!read.Contains(m.id)) n++;
            return n;
        }

        /// <summary>Segna letto; tiene solo gli id ancora presenti, cosi' la lista sul dispositivo non cresce.</summary>
        public static void MarkRead(MailMessage m, List<MailMessage> current)
        {
            var read = ReadIds();
            read.Add(m.id);
            var kept = new List<string>();
            foreach (var c in current) if (read.Contains(c.id)) kept.Add(c.id);
            PlayerPrefs.SetString(ReadKey, string.Join("\n", kept));
            PlayerPrefs.Save();
        }

        private static HashSet<string> ReadIds() =>
            new HashSet<string>(PlayerPrefs.GetString(ReadKey, string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));

        private static DateTime? ParseUtc(string s) =>
            DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d)
                ? d : (DateTime?)null;

        private static void DevLog(string message)
        {
            if (Debug.isDebugBuild) Debug.LogWarning("[Posta] " + message);
        }
    }
}
