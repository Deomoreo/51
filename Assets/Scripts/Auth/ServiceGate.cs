using System;
using System.Collections.Generic;
using System.Globalization;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Project51.Auth
{
    /// <summary>
    /// Stato del servizio (mockup Aggiornamento e Manutenzione), scritto su PlayFab Game Manager (Content -> Title Data) senza build:
    ///   Aggiornamento = {"minima":"2.50","novita":["...","..."],"link":"https://..."}: sotto la versione minima si deve aggiornare.
    ///     link facoltativo: senza, su Android si apre la scheda del Play Store dell'app (iOS non ha ancora un id dello store).
    ///   Manutenzione = {"fine":"2026-10-02T04:00:00Z"}: fino a quell'ora (UTC) i server sono in manutenzione. Togliere la chiave
    ///     (o lasciare un'ora passata) per riaprire.
    /// L'aggiornamento conta prima della manutenzione (si puo' aggiornare intanto). Se la lettura fallisce si gioca: meglio un
    /// controllo saltato che un'app chiusa per un errore di rete.
    /// </summary>
    public sealed class ServiceGate
    {
        public enum State { Open, Update, Maintenance }

        public const string UpdateKey = "Aggiornamento", MaintenanceKey = "Manutenzione";

        public State state;
        public string version = "";
        public string link = "";
        public string[] news = new string[0];
        public DateTime endUtc;

        public static readonly ServiceGate Open = new ServiceGate();

        [Serializable] private class UpdateData { public string minima; public string[] novita; public string link; }
        [Serializable] private class MaintenanceData { public string fine; }

        /// <summary>Chiede le due chiavi a PlayFab (sessione gia' aperta) e risponde sempre, Open se qualcosa non va.</summary>
        public static void Check(Action<ServiceGate> done)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn()) { done?.Invoke(Open); return; }
            PlayFabClientAPI.GetTitleData(new GetTitleDataRequest { Keys = new List<string> { UpdateKey, MaintenanceKey } },
                result =>
                {
                    var data = result.Data ?? new Dictionary<string, string>();
                    data.TryGetValue(UpdateKey, out var update);
                    data.TryGetValue(MaintenanceKey, out var maintenance);
                    done?.Invoke(Parse(update, maintenance, Application.version, DateTime.UtcNow));
                },
                error =>
                {
                    if (Debug.isDebugBuild) Debug.LogWarning("[ServiceGate] GetTitleData fallita: " + error.GenerateErrorReport());
                    done?.Invoke(Open);
                });
        }

        public static ServiceGate Parse(string updateJson, string maintenanceJson, string installed, DateTime nowUtc)
        {
            var update = Read<UpdateData>(updateJson);
            if (update != null && IsOlder(installed, update.minima))
                return new ServiceGate
                {
                    state = State.Update,
                    version = update.minima.Trim(),
                    link = update.link ?? "",
                    news = update.novita ?? new string[0],
                };

            var maintenance = Read<MaintenanceData>(maintenanceJson);
            if (maintenance != null && DateTime.TryParse(maintenance.fine, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var end) && end > nowUtc)
                return new ServiceGate { state = State.Maintenance, endUtc = end };

            return Open;
        }

        /// <summary>
        /// Versioni come le numera il progetto (2.43 -> 2.44 -> ... -> 2.50): numeri decimali, quindi 2.5 = 2.50 &gt; 2.43.
        /// Una versione illeggibile non blocca nessuno.
        /// </summary>
        public static bool IsOlder(string installed, string minimum)
        {
            const NumberStyles Style = NumberStyles.AllowDecimalPoint;
            return decimal.TryParse((installed ?? "").Trim(), Style, CultureInfo.InvariantCulture, out var have)
                && decimal.TryParse((minimum ?? "").Trim(), Style, CultureInfo.InvariantCulture, out var need)
                && have < need;
        }

        /// <summary>"42:18" come nel mockup; da un'ora in su "1:42:18".</summary>
        public static string Countdown(TimeSpan left)
        {
            if (left < TimeSpan.Zero) left = TimeSpan.Zero;
            int s = (int)Math.Ceiling(left.TotalSeconds);
            return s >= 3600 ? $"{s / 3600}:{s / 60 % 60:00}:{s % 60:00}" : $"{s / 60:00}:{s % 60:00}";
        }

        private static T Read<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonUtility.FromJson<T>(json); }
            catch (ArgumentException) { return null; }
        }
    }
}
