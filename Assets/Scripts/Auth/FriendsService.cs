using System;
using System.Collections.Generic;
using System.Globalization;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Project51.Auth
{
    /// <summary>Giro Android 08/10: amico (reciproco), richiesta che ho mandato, richiesta che ho ricevuto.</summary>
    public enum FriendState { Friend, Sent, Received }

    /// <summary>Un amico come lo vede la UI: niente tipi PlayFab fuori da questo file.</summary>
    public readonly struct FriendEntry
    {
        public readonly string PlayFabId;
        public readonly string DisplayName;
        /// <summary>0 = sconosciuto (profilo non leggibile).</summary>
        public readonly int Level;
        /// <summary>Ultimo accesso (UTC), null se PlayFab non lo da'.</summary>
        public readonly DateTime? LastLogin;
        public readonly FriendState State;
        /// <summary>Avatar scelto (nome dello sprite, come ProfileService.AvatarId); null se non l'ha mai pubblicato.</summary>
        public readonly string AvatarId;
        public readonly int Games, Wins, Scope;
        /// <summary>Cornice e banner scelti (id di ProfileCosmetics); null se non li ha pubblicati (versioni vecchie).</summary>
        public readonly string FrameId, BannerId;

        public FriendEntry(string playFabId, string displayName, int level = 0, DateTime? lastLogin = null, FriendState state = FriendState.Friend,
            string avatarId = null, int games = 0, int wins = 0, int scope = 0, string frameId = null, string bannerId = null)
        {
            FrameId = frameId;
            BannerId = bannerId;
            PlayFabId = playFabId;
            DisplayName = displayName;
            Level = level;
            LastLogin = lastLogin;
            State = state;
            AvatarId = avatarId;
            Games = games;
            Wins = wins;
            Scope = scope;
        }
    }

    /// <summary>Voce di "amici" (CloudScript).</summary>
    [Serializable]
    public class ServerFriend
    {
        public string id, nome, stato, avatar, ultimo;
        public int xp, partite, vittorie, scope;
    }

    /// <summary>
    /// Amici (giro Android 08/10): richiesta, Accetta o Rifiuta, amicizia reciproca, Rimuovi. Gli elenchi PlayFab li cambia solo il
    /// CloudScript ("amici", "richiestaAmico", "accettaAmico", "rimuoviAmico" in Server/CloudScript/51.js): prima AddFriend del telefono
    /// aggiungeva a senso unico e l'altro non vedeva niente. Stato online e inviti restano di Photon Chat (FriendsChat).
    /// </summary>
    public static class FriendsService
    {
        /// <summary>
        /// Aspetto pubblicato nel profilo PlayFab (AvatarUrl), letto dagli amici: "avatar:id|cornice|banner" (secondo giro Android
        /// 08/10: prima solo "avatar:id", la scheda dell'amico non aveva il suo banner).
        /// </summary>
        public const string AvatarPrefix = "avatar:";

        const string SendError = "Non è stato possibile mandare la richiesta. Riprova.";

        /// <summary>Amici, richieste ricevute e inviate, con livello, avatar, statistiche e ultimo accesso.</summary>
        public static void GetFriends(Action<List<FriendEntry>> onSuccess, Action onError)
        {
            RewardsService.Call("amici", null, r =>
            {
                if (!r.ok) { onError?.Invoke(); return; }
                onSuccess?.Invoke(ToEntries(r.amici));
            }, _ => onError?.Invoke());
        }

        public static List<FriendEntry> ToEntries(ServerFriend[] friends)
        {
            var list = new List<FriendEntry>();
            if (friends == null) return list;
            foreach (var f in friends)
            {
                if (f == null || string.IsNullOrEmpty(f.id)) continue;
                var state = f.stato == "ricevuta" ? FriendState.Received : f.stato == "inviata" ? FriendState.Sent : FriendState.Friend;
                DateTime? last = DateTime.TryParse(f.ultimo, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t) ? t : (DateTime?)null;
                ParseLook(f.avatar, out var avatar, out var frame, out var banner);
                // "Level" si scrive solo quando cambia (al livello 1 non c'e'): il livello viene dagli XP.
                list.Add(new FriendEntry(f.id, string.IsNullOrEmpty(f.nome) ? f.id : f.nome, Project51.Core.PlayerXp.LevelOf(f.xp), last, state,
                    avatar, f.partite, f.vittorie, f.scope, frame, banner));
            }
            return list;
        }

        public static string FormatLook(string avatarId, string frameId, string bannerId) => AvatarPrefix + avatarId + "|" + frameId + "|" + bannerId;

        /// <summary>Legge FormatLook (anche il vecchio "avatar:id"); campi vuoti o assenti = null.</summary>
        public static void ParseLook(string url, out string avatarId, out string frameId, out string bannerId)
        {
            avatarId = frameId = bannerId = null;
            if (url == null || !url.StartsWith(AvatarPrefix, StringComparison.Ordinal)) return;
            var parts = url.Substring(AvatarPrefix.Length).Split('|');
            avatarId = parts[0].Length > 0 ? parts[0] : null;
            frameId = parts.Length > 1 && parts[1].Length > 0 ? parts[1] : null;
            bannerId = parts.Length > 2 && parts[2].Length > 0 ? parts[2] : null;
        }

        /// <summary>Richiesta per nome visualizzato (scelta dell'utente, 01/10). onSuccess(true) = amici subito (l'aveva chiesto anche lui).</summary>
        public static void AddFriendByName(string displayName, Action<bool> onSuccess, Action<string> onError)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn()) { onError?.Invoke("Accedi per aggiungere amici."); return; }
            PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest { TitleDisplayName = displayName },
                r => AddFriend(r.AccountInfo?.PlayFabId, onSuccess, onError),
                e => onError?.Invoke(AddError(e.Error)));
        }

        public static string AddError(PlayFabErrorCode code) =>
            code == PlayFabErrorCode.AccountNotFound || code == PlayFabErrorCode.InvalidParams ? "Nessun giocatore con questo nome." : SendError;

        /// <summary>Richiesta d'amicizia (l'altro la vede in Richieste). onSuccess(true) = gia' amici o amici subito.</summary>
        public static void AddFriend(string playFabId, Action<bool> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrEmpty(playFabId)) { onError?.Invoke("Nessun giocatore con questo nome."); return; }
            RewardsService.Call("richiestaAmico", new Dictionary<string, object> { { "id", playFabId } }, r =>
            {
                if (r.ok) { Nudge(playFabId); onSuccess?.Invoke(r.stato == "amico"); }
                else onError?.Invoke(r.errore == "ospite" ? "Gli ospiti non si possono aggiungere agli amici."
                    : r.ospite ? "Accedi per aggiungere amici." : SendError);
            }, _ => onError?.Invoke(SendError));
        }

        public static void Accept(string playFabId, Action onSuccess, Action onError) =>
            RewardsService.Call("accettaAmico", new Dictionary<string, object> { { "id", playFabId } },
                r => { if (r.ok) { Nudge(playFabId); onSuccess?.Invoke(); } else onError?.Invoke(); }, _ => onError?.Invoke());

        /// <summary>Rifiuta, annulla la richiesta o rimuovi dagli amici: via da entrambi gli elenchi.</summary>
        public static void RemoveFriend(string playFabId, Action onSuccess, Action onError) =>
            RewardsService.Call("rimuoviAmico", new Dictionary<string, object> { { "id", playFabId } },
                r => { if (r.ok) { Nudge(playFabId); onSuccess?.Invoke(); } else onError?.Invoke(); }, _ => onError?.Invoke());

        // Secondo giro Android 08/10: l'altro rilegge subito la lista (FriendsChat.OnFriendsChanged).
        static void Nudge(string playFabId) => FriendsChat.Instance?.Nudge(playFabId);

        /// <summary>
        /// Avatar, cornice e banner nel profilo PlayFab (AvatarUrl, FormatLook), cosi' gli amici li vedono (#9: prima la lista
        /// mostrava un ritratto scelto dal PlayFab ID). Solo se cambiati da quelli gia' pubblicati da questo telefono per l'account;
        /// gli amici collegati rileggono subito.
        /// </summary>
        public static void PublishLook(string playFabId, string avatarId, string frameId, string bannerId)
        {
            if (string.IsNullOrEmpty(playFabId) || string.IsNullOrEmpty(avatarId) || !PlayFabClientAPI.IsClientLoggedIn()) return;
            string key = "AvatarPubblicato." + playFabId, look = FormatLook(avatarId, frameId, bannerId);
            if (PlayerPrefs.GetString(key, "") == look) return;
            PlayFabClientAPI.UpdateAvatarUrl(new UpdateAvatarUrlRequest { ImageUrl = look },
                _ => { PlayerPrefs.SetString(key, look); PlayerPrefs.Save(); FriendsChat.Instance?.NudgeAll(); },
                e => Debug.LogWarning("[FriendsService] UpdateAvatarUrl fallita: " + e.GenerateErrorReport()));
        }
    }

    /// <summary>
    /// "Silenzia emoticon" di un giocatore, solo su questo dispositivo. B12 (E1/E2): salvato per account; quello messo da un ospite, o
    /// verso un ospite (il suo id vale una sessione), resta in memoria fino alla chiusura dell'app.
    /// </summary>
    public static class EmoticonMute
    {
        public const string Key = "Social.MutedEmoticons.";
        private static readonly HashSet<string> session = new HashSet<string>();

        private static string AccountKey
        {
            get
            {
                var auth = AuthBootstrapper.Instance?.PlayFabAuth;
                return auth != null && auth.HasRealLogin && !string.IsNullOrEmpty(auth.PlayFabId) ? Key + auth.PlayFabId : null;
            }
        }

        public static bool IsMuted(string playFabId) =>
            !string.IsNullOrEmpty(playFabId) && (session.Contains(playFabId) || Load().Contains(playFabId) || BlockList.IsBlocked(playFabId));

        public static void SetMuted(string playFabId, bool muted, bool guestTarget = false)
        {
            if (string.IsNullOrEmpty(playFabId)) return;
            string key = AccountKey;
            if (muted && (key == null || guestTarget)) { session.Add(playFabId); return; }
            session.Remove(playFabId);
            if (key == null) return;
            var set = Load();
            if (muted ? set.Add(playFabId) : set.Remove(playFabId))
            {
                PlayerPrefs.SetString(key, string.Join("|", set));
                PlayerPrefs.Save();
            }
        }

        private static HashSet<string> Load()
        {
            string key = AccountKey;
            return key == null ? new HashSet<string>()
                : new HashSet<string>(PlayerPrefs.GetString(key, "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
        }
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
