using System;
using System.Collections.Generic;
using DG.Tweening;
using Photon.Pun;
using Project51.Auth;
using Project51.Networking;
using Project51.UI51;
using Project51.UIV2.Core;
using Project51.UIV2.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 8, Amici (mockup Amici e AmiciRichieste), aperta dal pulsante Amici della Home. Scelte dell'utente del 01/10:
    /// si aggiunge per nome (il codice #51-... arriva col server), amicizia a senso unico (PlayFab AddFriend: le richieste da
    /// accettare arrivano col server, la scheda Richieste per ora e' vuota), stato e inviti con Photon Chat (FriendsChat).
    /// Invita apre "Crea stanza" e, creata la stanza, manda il codice all'amico; chi riceve l'invito vede il dialog con ENTRA,
    /// che entra nella stanza come "Entra con codice". Costruita da UI51SocialBuilder.
    /// </summary>
    public sealed class UI51FriendsView : MonoBehaviour
    {
        public const string LoadingText = "Caricamento…";
        public const string NoRequestsText = "Nessuna richiesta per ora.";
        const string ErrorText = "Non è stato possibile caricare gli amici.\nControlla la connessione e riprova.";
        const float InviteWindowSeconds = 120f;

        [SerializeField] private CanvasGroup view;
        [SerializeField] private Button back;
        [SerializeField] private HomeScreenV2 home;
        [SerializeField] private RoomFlowV2 roomFlow;
        [SerializeField] private TMP_Text onlineLabel;

        [Header("Aggiungi")]
        [SerializeField] private TMP_Text myLabel;
        [SerializeField] private Button copy;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private Button add;

        [Header("Schede")]
        [SerializeField] private Button[] tabs = new Button[0];
        [SerializeField] private UI51Shape[] tabShapes = new UI51Shape[0];
        [SerializeField] private TMP_Text[] tabLabels = new TMP_Text[0];
        [SerializeField] private TMP_Text[] tabCounts = new TMP_Text[0];

        [Header("Elenco")]
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private GameObject listBlock;
        [SerializeField] private RectTransform rows;
        [SerializeField] private UI51FriendItem rowTemplate;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Sprite[] avatars = new Sprite[0];

        [Header("Nessun amico (mockup AmiciVuoto)")]
        [SerializeField] private RectTransform emptyBlock;
        [SerializeField] private Button share;

        [Header("Invito ricevuto")]
        [SerializeField] private UI51InviteBanner inviteBanner;

        private readonly List<UI51FriendItem> spawned = new List<UI51FriendItem>();
        private readonly HashSet<string> invited = new HashSet<string>();
        private List<FriendEntry> friends = new List<FriendEntry>();
        private int tab;
        private bool loaded, failed, waitingForAuth, adding, emptyShown;
        private string pendingInvite;
        private float pendingInviteAt;
        private MatchmakingManager manager;
        private Tween fade;

        public bool IsOpen => view != null && view.blocksRaycasts;

        private static string MyName
        {
            get
            {
                var auth = AuthBootstrapper.Instance != null ? AuthBootstrapper.Instance.PlayFabAuth : null;
                return auth != null && !string.IsNullOrEmpty(auth.DisplayName) ? auth.DisplayName : "";
            }
        }

        private void Awake()
        {
            back.onClick.AddListener(Close);
            copy.onClick.AddListener(CopyMyName);
            share.onClick.AddListener(ShareMyName);
            add.onClick.AddListener(AddFriend);
            nameInput.onSubmit.AddListener(_ => AddFriend());
            for (int i = 0; i < tabs.Length; i++) { int index = i; tabs[i].onClick.AddListener(() => SelectTab(index)); }
            rowTemplate.gameObject.SetActive(false);
            if (home != null) home.OnFriendsPressed += Open;
            SetVisible(false, true);
        }

        // Come la Posta: si parte appena la sessione PlayFab e' pronta, cosi' gli inviti arrivano anche senza aprire Amici.
        private void Start() => Load();

        public void Open()
        {
            tab = 0;
            emptyShown = false;
            SetVisible(true, false);
            scroll.verticalNormalizedPosition = 1f;
            ShowMine(MyName);
            Render();
            Load();
        }

        public void Close()
        {
            if (!IsOpen) return;
            nameInput.DeactivateInputField();
            SetVisible(false, false);
        }

        private void Load()
        {
            var auth = AuthBootstrapper.Instance;
            if (auth != null && !auth.IsReady && !auth.HasError)
            {
                if (!waitingForAuth) { waitingForAuth = true; auth.OnAuthReady += OnAuthReady; }
                return;
            }
            if (auth == null || auth.PlayFabAuth == null || !auth.PlayFabAuth.HasRealLogin) return; // l'ospite non ha amici
            FriendsService.GetFriends(Fill, () =>
            {
                if (this == null) return;
                failed = !loaded;
                Render();
            });
        }

        private void OnAuthReady()
        {
            StopWaitingForAuth();
            Load();
        }

        private void StopWaitingForAuth()
        {
            if (!waitingForAuth) return;
            waitingForAuth = false;
            if (AuthBootstrapper.Instance != null) AuthBootstrapper.Instance.OnAuthReady -= OnAuthReady;
        }

        /// <summary>Anche per le prove: mostra questi amici come se arrivassero da PlayFab.</summary>
        public void Fill(List<FriendEntry> list)
        {
            if (this == null) return;
            friends = list ?? new List<FriendEntry>();
            loaded = true;
            failed = false;
            var chat = FriendsChat.Ensure();
            chat.OnPresenceChanged -= OnPresence;
            chat.OnPresenceChanged += OnPresence;
            chat.OnInvite -= OnInvite;
            chat.OnInvite += OnInvite;
            var ids = new List<string>();
            foreach (var f in friends) ids.Add(f.PlayFabId);
            chat.Watch(ids);
            Render();
        }

        private void OnPresence()
        {
            if (this != null) Render();
        }

        private static FriendPresence Presence(string id) => FriendsChat.Instance != null ? FriendsChat.Instance.PresenceOf(id) : FriendPresence.Offline;

        private void Render()
        {
            int online = 0;
            foreach (var f in friends) if (Presence(f.PlayFabId) != FriendPresence.Offline) online++;
            onlineLabel.text = online + " online";
            tabCounts[0].text = friends.Count.ToString();
            tabCounts[1].text = "0"; // richieste vere col giro del server
            for (int i = 0; i < tabs.Length; i++) StyleTab(i, i == tab);
            if (!IsOpen) return;

            Clear();
            bool showFriends = tab == 0 && loaded && friends.Count > 0;
            bool empty = tab == 0 && loaded && friends.Count == 0;
            listBlock.SetActive(showFriends);
            status.gameObject.SetActive(!showFriends && !empty);
            status.text = tab == 1 ? NoRequestsText : failed ? ErrorText : LoadingText;
            emptyBlock.gameObject.SetActive(empty);
            if (empty && !emptyShown) UIAnim.Pop(emptyBlock, 0.8f, 1.04f, 0.3f);
            emptyShown = empty;
            if (!showFriends) return;

            var sorted = new List<FriendEntry>(friends);
            sorted.Sort((a, b) =>
            {
                int pa = Order(Presence(a.PlayFabId)), pb = Order(Presence(b.PlayFabId));
                if (pa != pb) return pa.CompareTo(pb);
                // Offline: visto piu' di recente in cima (mockup: 2 ore fa, ieri, 3 giorni fa).
                if (pa == 2 && a.LastLogin != b.LastLogin) return Nullable.Compare(b.LastLogin, a.LastLogin);
                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCultureIgnoreCase);
            });
            var now = DateTime.UtcNow;
            foreach (var f in sorted)
            {
                var row = Instantiate(rowTemplate, rows);
                row.name = "Friend_" + f.PlayFabId;
                row.gameObject.SetActive(true);
                BindRow(row, f, now);
                spawned.Add(row);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        }

        private static int Order(FriendPresence p) => p == FriendPresence.Online ? 0 : p == FriendPresence.Playing ? 1 : 2;

        private void BindRow(UI51FriendItem row, FriendEntry f, DateTime now)
        {
            var p = Presence(f.PlayFabId);
            bool off = p == FriendPresence.Offline;
            row.avatar.SetAvatar(AvatarFor(f.PlayFabId));
            if (off) row.avatar.SetRing(UI51Tokens.CreamA(0.25f));
            else row.avatar.SetFrame(FrameStyle.Oro);
            // grayscale(.6) brightness(.8) del mockup: ritratto spento.
            row.avatar.SetTint(off ? new Color(0.62f, 0.62f, 0.62f, 1f) : Color.white);
            row.dot.color = p == FriendPresence.Online ? UI51Tokens.Success : p == FriendPresence.Playing ? UI51Tokens.Gold : UI51Tokens.Hex("#6B7280");
            row.title.text = f.DisplayName;
            row.level.transform.parent.gameObject.SetActive(f.Level > 0);
            row.level.text = "Liv. " + f.Level;
            string note = FriendsChat.Instance != null ? FriendsChat.Instance.NoteOf(f.PlayFabId) : "";
            row.status.text = p == FriendPresence.Online ? "Online" : p == FriendPresence.Playing ? (note.Length > 0 ? note : "In partita")
                : f.LastLogin.HasValue ? LastSeen(f.LastLogin.Value, now) : "Offline";
            row.status.color = p == FriendPresence.Online ? UI51Tokens.SuccessText : p == FriendPresence.Playing ? UI51Tokens.Gold : UI51Tokens.CreamA(0.45f);
            bool wasInvited = invited.Contains(f.PlayFabId);
            row.invite.gameObject.SetActive(p == FriendPresence.Online && !wasInvited);
            row.invited.SetActive(p == FriendPresence.Online && wasInvited);
            row.busy.SetActive(p != FriendPresence.Online);
            row.busyLabel.text = off ? "Offline" : "Occupato";
            string id = f.PlayFabId;
            row.invite.onClick.AddListener(() => Invite(id));
        }

        /// <summary>"Visto poco fa / N ore fa / ieri / N giorni fa" (mockup: "Visto 2 ore fa").</summary>
        public static string LastSeen(DateTime thenUtc, DateTime nowUtc)
        {
            var span = nowUtc - thenUtc;
            if (span.TotalHours < 1) return "Visto poco fa";
            if (span.TotalHours < 24) { int h = (int)span.TotalHours; return h == 1 ? "Visto 1 ora fa" : "Visto " + h + " ore fa"; }
            return "Visto " + NewsService.RelativeTime(thenUtc, nowUtc);
        }

        private Sprite AvatarFor(string id) => PortraitFor(id, avatars);

        // ponytail: ritratto scelto dal PlayFab ID (come al tavolo, per posto): l'avatar del profilo non e' pubblicato.
        // Lo usa anche la Classifica, cosi' lo stesso giocatore ha lo stesso ritratto.
        public static Sprite PortraitFor(string id, Sprite[] avatars)
        {
            if (avatars == null || avatars.Length == 0) return null;
            int h = 0;
            foreach (char c in id ?? "") h = h * 31 + c;
            return avatars[(h & 0x7fffffff) % avatars.Length];
        }

        private void StyleTab(int i, bool on)
        {
            tabShapes[i].color = on ? Color.white : Color.clear;
            tabLabels[i].font = UI51Tokens.Font(on ? FontFace.NunitoExtraBold : FontFace.NunitoBold);
            tabLabels[i].color = on ? UI51Tokens.Gold : UI51Tokens.CreamA(0.7f);
        }

        private void SelectTab(int index)
        {
            if (tab == index) return;
            tab = index;
            Render();
        }

        // --- Aggiungi per nome

        private void ShowMine(string name) => myLabel.text = "Il tuo: <color=#F5E9D0><b>" + (name.Length > 0 ? name : "—") + "</b></color>";

        private void CopyMyName()
        {
            if (MyName.Length == 0) return;
            GUIUtility.systemCopyBuffer = MyName;
            Feedback("Nome copiato negli appunti");
        }

        /// <summary>"Condividi il tuo ID": per ora l'ID e' il nome (il codice #51-... arriva col server).</summary>
        private void ShareMyName()
        {
            if (MyName.Length == 0) return;
            if (!NativeShare.ShareText("Gioca a 51 con me! Aggiungimi agli amici col nome " + MyName, "Condividi il tuo nome"))
                Feedback("Nome copiato negli appunti");
        }

        private void AddFriend()
        {
            string name = (nameInput.text ?? "").Trim();
            if (adding || name.Length == 0) return;
            if (string.Equals(name, MyName, StringComparison.CurrentCultureIgnoreCase)) { Feedback("Sei tu!", false); return; }
            adding = true;
            add.interactable = false;
            FriendsService.AddFriendByName(name, () =>
            {
                if (this == null) return;
                adding = false;
                add.interactable = true;
                nameInput.text = "";
                Feedback(name + " aggiunto agli amici");
                Load();
            }, error =>
            {
                if (this == null) return;
                adding = false;
                add.interactable = true;
                Feedback(error, false);
            });
        }

        /// <summary>Com'e' andata, nel toast (mockup Toast).</summary>
        private static void Feedback(string text, bool ok = true) =>
            UI51Toast.Show(text, ok ? UI51Toast.Kind.Success : UI51Toast.Kind.Error);

        // --- Inviti

        /// <summary>Invita: "Crea stanza" (scelta del formato); appena la stanza c'e', il codice parte verso l'amico.</summary>
        private void Invite(string friendId)
        {
            if (roomFlow == null || MatchmakingManager.Instance == null) return;
            Subscribe();
            pendingInvite = friendId;
            pendingInviteAt = Time.unscaledTime;
            roomFlow.OpenCreate();
        }

        private void Subscribe()
        {
            if (manager != null) return;
            manager = MatchmakingManager.Instance;
            manager.OnRoomCreated += RoomCreated;
        }

        private void RoomCreated(string code)
        {
            // ponytail: l'invito vale per la prima stanza creata entro 2 minuti dal tocco su Invita; annullando prima, scade da solo.
            if (pendingInvite == null || Time.unscaledTime - pendingInviteAt > InviteWindowSeconds) { pendingInvite = null; return; }
            string id = pendingInvite;
            pendingInvite = null;
            var format = manager.CurrentConfig != null ? RoomFlowV2.FormatName(manager.CurrentConfig.Format) : "";
            if (FriendsChat.Instance != null && FriendsChat.Instance.SendInvite(id, code, format, MyName))
            {
                invited.Add(id);
                Render();
            }
        }

        // --- Sala privata (mockup SalaPrivata, INVITA AMICI ONLINE)

        /// <summary>Amici online adesso, piu' quelli gia' entrati nella stanza (a un tavolo non risultano piu' online).</summary>
        public List<FriendEntry> RoomFriends(ICollection<string> roomNames) =>
            friends.FindAll(f => Presence(f.PlayFabId) == FriendPresence.Online || roomNames.Contains(f.DisplayName));

        public Sprite Portrait(string friendId) => AvatarFor(friendId);

        public bool WasInvited(string friendId) => invited.Contains(friendId);

        /// <summary>Manda il codice della stanza in cui sono all'amico; format = "1 vs 1", "2 vs 2", "1 vs 3".</summary>
        public void InviteToRoom(string friendId, string format)
        {
            if (!PhotonNetwork.InRoom) return;
            if (FriendsChat.Instance != null && FriendsChat.Instance.SendInvite(friendId, PhotonNetwork.CurrentRoom.Name, format, MyName))
            {
                invited.Add(friendId);
                Render();
            }
            else Feedback("Invito non inviato: riprova tra poco", false);
        }

        private void OnInvite(FriendInvite invite)
        {
            // Gia' a un tavolo o in un flusso online: l'invito non interrompe. Un secondo invito prende il posto del primo.
            if (this == null || inviteBanner == null || PhotonNetwork.InRoom || (roomFlow != null && roomFlow.IsOpen)) return;
            string text = "Stanza privata" + (invite.Format.Length > 0 ? " · " + invite.Format : "");
            inviteBanner.Show(invite.FromName + " ti invita a giocare", text, AvatarFor(invite.FromId), () =>
            {
                if (PhotonNetwork.InRoom || (roomFlow != null && roomFlow.IsOpen)) return;
                Close();
                if (roomFlow != null) roomFlow.JoinCode(invite.RoomCode);
            });
        }

        private void Clear()
        {
            foreach (var item in spawned) if (item != null) Destroy(item.gameObject);
            spawned.Clear();
        }

        private void SetVisible(bool visible, bool instant)
        {
            fade?.Kill();
            view.blocksRaycasts = visible;
            view.interactable = visible;
            if (instant) view.alpha = visible ? 1f : 0f;
            else fade = view.DOFade(visible ? 1f : 0f, 0.2f).SetUpdate(true).SetLink(gameObject);
        }

        private void Update()
        {
            if (!IsOpen || !Input.GetKeyDown(KeyCode.Escape)) return;
            Close();
        }

        private void OnDestroy()
        {
            fade?.Kill();
            StopWaitingForAuth();
            if (home != null) home.OnFriendsPressed -= Open;
            if (manager != null) manager.OnRoomCreated -= RoomCreated;
            if (FriendsChat.Instance != null)
            {
                FriendsChat.Instance.OnPresenceChanged -= OnPresence;
                FriendsChat.Instance.OnInvite -= OnInvite;
            }
        }
    }
}
