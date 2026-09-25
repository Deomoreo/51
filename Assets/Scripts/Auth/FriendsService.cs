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

        public FriendEntry(string playFabId, string displayName)
        {
            PlayFabId = playFabId;
            DisplayName = displayName;
        }
    }

    /// <summary>
    /// Amici e segnalazioni via PlayFab Client API. PlayFab non ha richieste d'amicizia native:
    /// AddFriend aggiunge subito (unidirezionale). Stato online e livello degli amici non arrivano da qui.
    /// </summary>
    public static class FriendsService
    {
        public static void GetFriends(Action<List<FriendEntry>> onSuccess, Action onError)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn()) { onError?.Invoke(); return; }

            PlayFabClientAPI.GetFriendsList(new GetFriendsListRequest(),
                result =>
                {
                    var list = new List<FriendEntry>();
                    if (result.Friends != null)
                        foreach (var f in result.Friends)
                            list.Add(new FriendEntry(f.FriendPlayFabId, f.TitleDisplayName ?? f.Username ?? f.FriendPlayFabId));
                    onSuccess?.Invoke(list);
                },
                error => Fail("GetFriendsList", error, onError));
        }

        public static void AddFriend(string playFabId, Action onSuccess, Action onError) =>
            Call(() => PlayFabClientAPI.AddFriend(new AddFriendRequest { FriendPlayFabId = playFabId },
                _ => onSuccess?.Invoke(), e => Fail("AddFriend", e, onError)), onError);

        public static void RemoveFriend(string playFabId, Action onSuccess, Action onError) =>
            Call(() => PlayFabClientAPI.RemoveFriend(new RemoveFriendRequest { FriendPlayFabId = playFabId },
                _ => onSuccess?.Invoke(), e => Fail("RemoveFriend", e, onError)), onError);

        /// <summary>"Segnala giocatore": finisce nei report del titolo su Game Manager.</summary>
        public static void ReportPlayer(string playFabId, string reason, Action onSuccess, Action onError) =>
            Call(() => PlayFabClientAPI.ReportPlayer(new ReportPlayerClientRequest { ReporteeId = playFabId, Comment = reason },
                _ => onSuccess?.Invoke(), e => Fail("ReportPlayer", e, onError)), onError);

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
            !string.IsNullOrEmpty(playFabId) && Load().Contains(playFabId);

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
}
