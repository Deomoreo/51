using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Project51.Auth
{
    /// <summary>Un amico come lo vede la UI: niente tipi PlayFab fuori da questo file.</summary>
    public readonly struct FriendEntry
    {
        public readonly string PlayFabId;
        public readonly string DisplayName;
        /// <summary>0 = sconosciuto (profilo non leggibile).</summary>
        public readonly int Level;
        /// <summary>Ultimo accesso (UTC), null se PlayFab non lo da'.</summary>
        public readonly DateTime? LastLogin;

        public FriendEntry(string playFabId, string displayName, int level = 0, DateTime? lastLogin = null)
        {
            PlayFabId = playFabId;
            DisplayName = displayName;
            Level = level;
            LastLogin = lastLogin;
        }
    }

    /// <summary>
    /// Amici e segnalazioni via PlayFab Client API. PlayFab non ha richieste d'amicizia native:
    /// AddFriend aggiunge subito (unidirezionale). Stato online e livello degli amici non arrivano da qui.
    /// </summary>
    public static class FriendsService
    {
        /// <summary>
        /// Lista degli amici con livello e ultimo accesso dal profilo. Se il titolo non permette quei campi
        /// (Game Manager, Client Profile Options) riprova senza: solo nomi.
        /// </summary>
        public static void GetFriends(Action<List<FriendEntry>> onSuccess, Action onError)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn()) { onError?.Invoke(); return; }
            var withProfile = new PlayerProfileViewConstraints { ShowDisplayName = true, ShowStatistics = true, ShowLastLogin = true };
            PlayFabClientAPI.GetFriendsList(new GetFriendsListRequest { ProfileConstraints = withProfile },
                result => onSuccess?.Invoke(ToEntries(result)),
                _ => PlayFabClientAPI.GetFriendsList(new GetFriendsListRequest(),
                    result => onSuccess?.Invoke(ToEntries(result)),
                    error => Fail("GetFriendsList", error, onError)));
        }

        private static List<FriendEntry> ToEntries(GetFriendsListResult result)
        {
            var list = new List<FriendEntry>();
            if (result.Friends == null) return list;
            foreach (var f in result.Friends)
            {
                // "Level" si scrive solo quando cambia (al livello 1 non c'e'): il livello viene dagli XP.
                int level = 0;
                if (f.Profile?.Statistics != null)
                    foreach (var stat in f.Profile.Statistics) if (stat.Name == "XP") level = Project51.Core.PlayerXp.LevelOf(stat.Value);
                string name = f.TitleDisplayName ?? f.Profile?.DisplayName ?? f.Username ?? f.FriendPlayFabId;
                list.Add(new FriendEntry(f.FriendPlayFabId, name, level, f.Profile?.LastLogin));
            }
            return list;
        }

        /// <summary>Aggiunge per nome visualizzato (scelta dell'utente, 01/10; il codice #51-... arriva col server). onError riceve il messaggio per la UI.</summary>
        public static void AddFriendByName(string displayName, Action onSuccess, Action<string> onError)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn()) { onError?.Invoke("Accedi per aggiungere amici."); return; }
            PlayFabClientAPI.AddFriend(new AddFriendRequest { FriendTitleDisplayName = displayName },
                _ => onSuccess?.Invoke(),
                e => onError?.Invoke(AddError(e.Error)));
        }

        public static string AddError(PlayFabErrorCode code) =>
            code == PlayFabErrorCode.UsersAlreadyFriends ? "È già tra i tuoi amici."
            : code == PlayFabErrorCode.AccountNotFound || code == PlayFabErrorCode.InvalidParams ? "Nessun giocatore con questo nome."
            : "Non è stato possibile aggiungerlo. Riprova.";

        public static void AddFriend(string playFabId, Action onSuccess, Action onError) =>
            Call(() => PlayFabClientAPI.AddFriend(new AddFriendRequest { FriendPlayFabId = playFabId },
                _ => onSuccess?.Invoke(), e => Fail("AddFriend", e, onError)), onError);

        public static void RemoveFriend(string playFabId, Action onSuccess, Action onError) =>
            Call(() => PlayFabClientAPI.RemoveFriend(new RemoveFriendRequest { FriendPlayFabId = playFabId },
                _ => onSuccess?.Invoke(), e => Fail("RemoveFriend", e, onError)), onError);

        private static void Call(Action call, Action onError)
        {
            if (PlayFabClientAPI.IsClientLoggedIn()) call();
            else onError?.Invoke();
        }

        private static void Fail(string what, PlayFabError error, Action onError)
        {
            if (Debug.isDebugBuild) Debug.LogWarning($"[FriendsService] {what} fallita: {error.GenerateErrorReport()}");
            onError?.Invoke();
        }
    }

    /// <summary>"Silenzia emoticon" di un giocatore: solo su questo dispositivo, per PlayFab ID.</summary>
    public static class EmoticonMute
    {
        private const string Key = "Social.MutedEmoticons";

        public static bool IsMuted(string playFabId) =>
            !string.IsNullOrEmpty(playFabId) && (Load().Contains(playFabId) || BlockList.IsBlocked(playFabId));

        public static void SetMuted(string playFabId, bool muted)
        {
            if (string.IsNullOrEmpty(playFabId)) return;
            var set = Load();
            if (muted ? set.Add(playFabId) : set.Remove(playFabId))
            {
                PlayerPrefs.SetString(Key, string.Join("|", set));
                PlayerPrefs.Save();
            }
        }

        private static HashSet<string> Load() =>
            new HashSet<string>(PlayerPrefs.GetString(Key, "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// "Blocca giocatore" (scelta dell'utente 01/10): le sue emoticon spente, i suoi inviti scartati (FriendsChat), tolto dagli amici;
    /// le interazioni social che arriveranno (richieste d'amicizia col server, messaggi) vanno filtrate qui. Salvato nei dati privati
    /// del giocatore su PlayFab ("Bloccati"), quindi vale su ogni telefono dell'account. Solo per chi ha un account.
    /// </summary>
    public static class BlockList
    {
        private static string owner;
        private static HashSet<string> set = new HashSet<string>();

        public static bool IsBlocked(string playFabId) => !string.IsNullOrEmpty(playFabId) && Current().Contains(playFabId);

        /// <summary>Copia della lista (Impostazioni > Giocatori bloccati).</summary>
        public static string[] All() => new List<string>(Current()).ToArray();

        /// <summary>Ultimo nome visto (profilo rapido, PlayFab) per la pagina Giocatori bloccati; null se mai visto su questo telefono.</summary>
        public static string KnownName(string playFabId)
        {
            string name = PlayerPrefs.GetString("BloccatoNome_" + playFabId, "");
            return name.Length > 0 ? name : null;
        }

        public static void RememberName(string playFabId, string name)
        {
            if (!string.IsNullOrEmpty(playFabId) && !string.IsNullOrWhiteSpace(name)) PlayerPrefs.SetString("BloccatoNome_" + playFabId, name.Trim());
        }

        /// <summary>Falso se non e' cambiato niente (senza profilo, o gia' cosi').</summary>
        public static bool SetBlocked(string playFabId, bool blocked)
        {
            var profile = AuthBootstrapper.Instance != null ? AuthBootstrapper.Instance.Profile : null;
            if (string.IsNullOrEmpty(playFabId) || profile == null) return false;
            var ids = Current();
            if (!(blocked ? ids.Add(playFabId) : ids.Remove(playFabId))) return false;
            profile.SetPlayerData(ProfileService.DATA_BLOCKED, ids.Count > 0 ? string.Join("|", ids) : null, UserDataPermission.Private);
            if (blocked) FriendsService.RemoveFriend(playFabId, null, null);
            return true;
        }

        /// <summary>Copia locale della lista dell'account collegato (cambia account: si rilegge dal profilo).</summary>
        private static HashSet<string> Current()
        {
            var auth = AuthBootstrapper.Instance;
            bool loaded = auth != null && auth.Profile != null && auth.Profile.IsLoaded;
            string id = (auth != null && auth.PlayFabAuth != null ? auth.PlayFabAuth.PlayFabId : null) + (loaded ? "" : "?");
            if (id != owner)
            {
                owner = id;
                string raw = auth != null && auth.Profile != null ? auth.Profile.Blocked : string.Empty;
                set = new HashSet<string>((raw ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
            }
            return set;
        }
    }
}
