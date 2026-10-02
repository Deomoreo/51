using System;
using System.Collections.Generic;
using System.Globalization;
using PlayFab;
using PlayFab.ClientModels;
using Project51.Core;

namespace Project51.Auth
{
    public sealed class RankEntry
    {
        public string PlayFabId, Name;
        /// <summary>Posizione da 1.</summary>
        public int Position, Value;
        /// <summary>Dagli XP del profilo; 0 = non arrivato (il titolo non pubblica le statistiche del profilo).</summary>
        public int Level;
    }

    /// <summary>
    /// Classifica (UI51 Fase 15, scelta utente 02/10): classifiche native di PlayFab sulle statistiche che scrive solo il server.
    /// Settimana = XPSettimana (premioPartita, azzeramento settimanale impostato nel Game Manager), Sempre = XP.
    /// Livello come in FriendsService: dagli XP del profilo, e se il titolo non permette le statistiche si riprova senza.
    /// </summary>
    public static class LeaderboardService
    {
        public const string Weekly = "XPSettimana", AllTime = "XP";
        public const int Size = 50;

        static readonly CultureInfo Italian = new CultureInfo("it-IT");

        public static void Top(string stat, Action<List<RankEntry>, DateTime?> onDone, Action onError) =>
            Query((p, ok, fail) => PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
                { StatisticName = stat, StartPosition = 0, MaxResultsCount = Size, ProfileConstraints = p }, ok, fail), onDone, onError);

        public static void Friends(string stat, Action<List<RankEntry>, DateTime?> onDone, Action onError) =>
            Query((p, ok, fail) => PlayFabClientAPI.GetFriendLeaderboard(new GetFriendLeaderboardRequest
                { StatisticName = stat, StartPosition = 0, MaxResultsCount = 100, ProfileConstraints = p }, ok, fail), onDone, onError);

        /// <summary>La propria riga (posizione e valore), anche fuori dai primi 50.</summary>
        public static void Me(string stat, Action<RankEntry> onDone, Action onError)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn()) { onError?.Invoke(); return; }
            PlayFabClientAPI.GetLeaderboardAroundPlayer(new GetLeaderboardAroundPlayerRequest { StatisticName = stat, MaxResultsCount = 1 },
                r => { var list = ToEntries(r.Leaderboard); onDone?.Invoke(list.Count > 0 ? list[0] : null); }, _ => onError?.Invoke());
        }

        /// <summary>"Si azzera tra 2 giorni e 4 ore" (sotto un'ora: "tra pochi minuti").</summary>
        public static string ResetText(TimeSpan left)
        {
            if (left.TotalHours < 1) return "Si azzera tra pochi minuti";
            int d = left.Days, h = left.Hours;
            string hours = h == 1 ? "1 ora" : h + " ore";
            if (d == 0) return "Si azzera tra " + hours;
            string days = d == 1 ? "1 giorno" : d + " giorni";
            return h == 0 ? "Si azzera tra " + days : "Si azzera tra " + days + " e " + hours;
        }

        /// <summary>Riga sotto "Tu": dentro i primi 10, oppure quanto manca al decimo (tenth = valore del decimo, -1 se non c'e').</summary>
        public static string Note(int position, int value, int tenth)
        {
            if (position <= 0) return "Gioca una partita per entrare in classifica";
            if (position <= 10) return position <= 3 ? "Sei sul podio!" : "Sei nella top 10!";
            if (tenth < 0) return "Continua a giocare per salire";
            return "Ti mancano " + Format(Math.Max(1, tenth - value + 1)) + " XP per la top 10";
        }

        public static string Format(int n) => n.ToString("N0", Italian);

        /// <summary>Prima col livello (statistiche del profilo); se il titolo non le permette, di nuovo coi soli nomi.</summary>
        private static void Query(Action<PlayerProfileViewConstraints, Action<GetLeaderboardResult>, Action<PlayFabError>> call,
            Action<List<RankEntry>, DateTime?> onDone, Action onError)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn()) { onError?.Invoke(); return; }
            Action<GetLeaderboardResult> ok = r => onDone?.Invoke(ToEntries(r.Leaderboard), r.NextReset);
            call(new PlayerProfileViewConstraints { ShowDisplayName = true, ShowStatistics = true }, ok, e =>
            {
                if (e.Error == PlayFabErrorCode.RequestViewConstraintParamsNotAllowed)
                    call(new PlayerProfileViewConstraints { ShowDisplayName = true }, ok, _ => onError?.Invoke());
                else onError?.Invoke();
            });
        }

        private static List<RankEntry> ToEntries(List<PlayerLeaderboardEntry> list)
        {
            var result = new List<RankEntry>();
            if (list == null) return result;
            foreach (var e in list)
            {
                int level = 0;
                if (e.Profile?.Statistics != null)
                    foreach (var s in e.Profile.Statistics) if (s.Name == "XP") level = PlayerXp.LevelOf(s.Value);
                result.Add(new RankEntry
                {
                    PlayFabId = e.PlayFabId, Name = string.IsNullOrEmpty(e.DisplayName) ? "Giocatore" : e.DisplayName,
                    Position = e.Position + 1, Value = e.StatValue, Level = level,
                });
            }
            return result;
        }
    }
}
