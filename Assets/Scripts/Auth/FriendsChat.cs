using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Chat;
using Photon.Pun;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.SceneManagement;
using AuthenticationValues = Photon.Chat.AuthenticationValues;
using CustomAuthenticationType = Photon.Chat.CustomAuthenticationType;

namespace Project51.Auth
{
    /// <summary>Stato di un amico visto da Photon Chat.</summary>
    public enum FriendPresence { Offline, Online, Playing }

    /// <summary>Invito a un tavolo privato ricevuto da un amico.</summary>
    public readonly struct FriendInvite
    {
        public readonly string FromId, FromName, RoomCode, Format;
        public FriendInvite(string fromId, string fromName, string roomCode, string format)
        {
            FromId = fromId; FromName = fromName; RoomCode = roomCode; Format = format;
        }
    }

    /// <summary>
    /// Photon Chat per gli amici (scelta dell'utente, 01/10): chi e' online o in partita (stato dei "friends" di Chat, spinto dal
    /// server) e gli inviti a un tavolo privato (messaggio privato col codice). Solo per chi ha un account vero.
    /// Serve l'AppId Chat in PhotonServerSettings (AppIdChat) e, sull'app Chat della dashboard Photon, la stessa Custom
    /// Authentication PlayFab dell'app PUN: senza AppId resta spento (tutti offline, Invita non manda niente).
    /// Vive sotto AuthBootstrapper (persistente), creato al primo uso con Ensure().
    /// </summary>
    public sealed class FriendsChat : MonoBehaviour, IChatClientListener
    {
        const string Prefix = "51|invito|";
        // Secondo giro Android 08/10: "rileggi gli amici" (richiesta, accetta, rifiuta, rimuovi, avatar o banner cambiati).
        public const string ChangedMessage = "51|amici";
        const double InviteMaxAgeSeconds = 120;

        public static FriendsChat Instance { get; private set; }

        public event Action OnPresenceChanged;
        public event Action<FriendInvite> OnInvite;
        /// <summary>Un amico (o chi mi manda una richiesta) ha cambiato qualcosa: la lista va riletta dal server.</summary>
        public event Action OnFriendsChanged;

        public bool IsAvailable => !string.IsNullOrEmpty(AppId);
        public bool IsConnected => client != null && client.CanChat;

        private ChatClient client;
        private readonly Dictionary<string, FriendPresence> presence = new Dictionary<string, FriendPresence>();
        private readonly Dictionary<string, string> presenceNote = new Dictionary<string, string>();
        private readonly HashSet<string> friends = new HashSet<string>();
        private bool connecting;
        private string playingNote;
        // B15 (P1): caduta non voluta -> nuovo tentativo da solo, 5 s poi il doppio fino a 30 s; si riparte da 5 a collegamento riuscito.
        private float retryDelay = 5f;

        static string AppId => PhotonNetwork.PhotonServerSettings != null ? PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat : null;

        public static FriendsChat Ensure()
        {
            if (Instance != null) return Instance;
            var host = AuthBootstrapper.Instance != null ? AuthBootstrapper.Instance.transform : null;
            var go = new GameObject(nameof(FriendsChat));
            if (host != null) go.transform.SetParent(host, false);
            else DontDestroyOnLoad(go);
            return go.AddComponent<FriendsChat>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            client?.Disconnect();
            if (Instance == this) Instance = null;
        }

        private void Update() => client?.Service();

        // B15 (P1/P3, scelta 07/10): in background offline subito (gli amici lo vedono), al ritorno ci si ricollega.
        private void OnApplicationPause(bool paused)
        {
            if (paused) { CancelInvoke(nameof(Connect)); client?.Disconnect(); }
            else Connect();
        }

        /// <summary>Si collega (una volta) e segue questi amici: lo stato arriva con OnPresenceChanged.</summary>
        public void Watch(IEnumerable<string> friendIds)
        {
            var added = new List<string>();
            foreach (var id in friendIds) if (!string.IsNullOrEmpty(id) && friends.Add(id)) added.Add(id);
            if (IsConnected && added.Count > 0) client.AddFriends(added.ToArray());
            Connect();
        }

        public FriendPresence PresenceOf(string id) => presence.TryGetValue(id, out var p) ? p : FriendPresence.Offline;

        /// <summary>Nota dello stato "in partita" (per esempio "2 vs 2"), vuota se non c'e'.</summary>
        public string NoteOf(string id) => presenceNote.TryGetValue(id, out var n) ? n : string.Empty;

        /// <summary>Manda l'invito col codice del tavolo privato. False se Chat non e' collegata.</summary>
        public bool SendInvite(string friendId, string roomCode, string format, string myName)
        {
            if (!IsConnected) return false;
            return client.SendPrivateMessage(friendId, FormatInvite(roomCode, format, myName, DateTime.UtcNow));
        }

        /// <summary>Avvisa quel giocatore di rileggere gli amici. Senza Chat (o lui offline) niente: lo prende al prossimo giro (UI51FriendsView).</summary>
        public void Nudge(string playFabId)
        {
            if (IsConnected && !string.IsNullOrEmpty(playFabId)) client.SendPrivateMessage(playFabId, ChangedMessage);
        }

        /// <summary>Avvisa tutti gli amici seguiti (avatar, cornice o banner cambiati).</summary>
        public void NudgeAll()
        {
            foreach (var id in friends) Nudge(id);
        }

        /// <summary>"51|invito|codice|formato|ora unix|nome" (il nome per ultimo: puo' contenere "|").</summary>
        public static string FormatInvite(string roomCode, string format, string name, DateTime utc) =>
            Prefix + roomCode + "|" + format + "|" + new DateTimeOffset(utc).ToUnixTimeSeconds() + "|" + name;

        /// <summary>Legge un invito; false se non lo e', se e' malformato o piu' vecchio di 2 minuti.</summary>
        public static bool TryParseInvite(string fromId, object message, DateTime nowUtc, out FriendInvite invite)
        {
            invite = default;
            var s = message as string;
            if (s == null || !s.StartsWith(Prefix, StringComparison.Ordinal)) return false;
            var parts = s.Substring(Prefix.Length).Split(new[] { '|' }, 4);
            if (parts.Length < 4 || parts[0].Length == 0 || !long.TryParse(parts[2], out var unix)) return false;
            var age = (nowUtc - DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime).TotalSeconds;
            if (age > InviteMaxAgeSeconds || age < -InviteMaxAgeSeconds) return false;
            invite = new FriendInvite(fromId, parts[3], parts[0], parts[1]);
            return true;
        }

        private void Connect()
        {
            if (connecting || IsConnected || !IsAvailable) return;
            var auth = AuthBootstrapper.Instance != null ? AuthBootstrapper.Instance.PlayFabAuth : null;
            if (auth == null || string.IsNullOrEmpty(auth.PlayFabId) || !auth.HasRealLogin) return;
            connecting = true;
            // Stesso schema dell'app PUN (PhotonAuthConnector): token PlayFab chiesto per l'AppId Chat. Chiamata diretta:
            // PlayFabAuthService conserverebbe il token Chat al posto di quello di PUN.
            string id = auth.PlayFabId;
            PlayFabClientAPI.GetPhotonAuthenticationToken(new GetPhotonAuthenticationTokenRequest { PhotonApplicationId = AppId }, result =>
            {
                if (this == null) return;
                client = new ChatClient(this) { ChatRegion = "EU", EnableProtocolFallback = true }; // come PUN: UDP bloccato -> TCP
                var values = new AuthenticationValues { AuthType = CustomAuthenticationType.Custom };
                values.AddAuthParameter("username", id);
                values.AddAuthParameter("token", result.PhotonCustomAuthenticationToken);
                values.UserId = id;
                if (!client.Connect(AppId, PhotonAuthConnector.AppVersion, values)) connecting = false;
            }, error =>
            {
                connecting = false;
                if (Debug.isDebugBuild) Debug.LogWarning("[FriendsChat] Token Photon Chat non ottenuto: " + error.GenerateErrorReport());
            });
        }

        private void OnSceneChanged(Scene from, Scene to)
        {
            playingNote = to.name == "GameScene" ? "In partita" : null;
            Connect();
            PublishStatus();
        }

        private void PublishStatus()
        {
            if (!IsConnected) return;
            if (playingNote != null) client.SetOnlineStatus(ChatUserStatus.Playing, playingNote);
            else client.SetOnlineStatus(ChatUserStatus.Online);
        }

        // --- IChatClientListener

        public void OnConnected()
        {
            connecting = false;
            retryDelay = 5f;
            if (friends.Count > 0) client.AddFriends(new List<string>(friends).ToArray());
            PublishStatus();
        }

        public void OnDisconnected()
        {
            connecting = false;
            presence.Clear();
            presenceNote.Clear();
            OnPresenceChanged?.Invoke();
            var cause = client != null ? client.DisconnectedCause : ChatDisconnectCause.None;
            if (Debug.isDebugBuild) Debug.Log("[FriendsChat] Disconnected: " + cause);
            // Uscita voluta (pausa, cambio account) o credenziali rifiutate: nessun nuovo tentativo.
            if (cause == ChatDisconnectCause.DisconnectByClientLogic || cause == ChatDisconnectCause.InvalidAuthentication
                || cause == ChatDisconnectCause.CustomAuthenticationFailed) return;
            Invoke(nameof(Connect), retryDelay);
            retryDelay = Mathf.Min(retryDelay * 2f, 30f);
        }

        public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
        {
            presence[user] = status == ChatUserStatus.Playing ? FriendPresence.Playing
                : status == ChatUserStatus.Offline || status == ChatUserStatus.Invisible ? FriendPresence.Offline : FriendPresence.Online;
            if (gotMessage) presenceNote[user] = message as string ?? string.Empty;
            OnPresenceChanged?.Invoke();
        }

        public void OnPrivateMessage(string sender, object message, string channelName)
        {
            if (client != null && sender == client.UserId) return; // la copia del messaggio che ho mandato io
            if (BlockList.IsBlocked(sender)) return; // giocatore bloccato: niente inviti
            if (message as string == ChangedMessage) { OnFriendsChanged?.Invoke(); return; }
            // Anche da chi non e' nella mia lista: l'amicizia PlayFab e' a senso unico (chi mi ha aggiunto puo' invitarmi).
            if (TryParseInvite(sender, message, DateTime.UtcNow, out var invite)) OnInvite?.Invoke(invite);
        }

        public void OnChatStateChange(ChatState state) { }
        public void DebugReturn(DebugLevel level, string message) { if (level == DebugLevel.ERROR) Debug.LogWarning("[FriendsChat] " + message); }
        public void OnGetMessages(string channelName, string[] senders, object[] messages) { }
        public void OnSubscribed(string[] channels, bool[] results) { }
        public void OnUnsubscribed(string[] channels) { }
        public void OnUserSubscribed(string channel, string user) { }
        public void OnUserUnsubscribed(string channel, string user) { }
    }
}
