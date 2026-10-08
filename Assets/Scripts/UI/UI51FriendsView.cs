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
    /// si aggiunge per nome (il codice #51-... arriva col server), stato e inviti con Photon Chat (FriendsChat). Giro Android 08/10:
    /// amicizia reciproca col server (FriendsService): AGGIUNGI manda una richiesta, l'altro la vede in Richieste con Accetta e
    /// Rifiuta; le richieste mandate stanno in fondo agli amici "in attesa". Il tocco su una riga apre la scheda dell'amico:
    /// profilo, Invita, Rimuovi dagli amici (o Annulla, Accetta, Rifiuta) e Blocca.
    /// Invita apre "Crea stanza" e, creata la stanza, manda il codice all'amico; chi riceve l'invito vede il dialog con ENTRA,
    /// che entra nella stanza come "Entra con codice". Costruita da UI51SocialBuilder.
    /// </summary>
    public sealed class UI51FriendsView : MonoBehaviour
    {
        public const string LoadingText = "Caricamento…";
        public const string NoRequestsText = "Nessuna richiesta per ora.";
        const string ErrorText = "Non è stato possibile caricare gli amici.\nControlla la connessione e riprova.";
        const float InviteWindowSeconds = 120f;
        // Secondo giro Android 08/10: la lista si rilegge appena un amico avvisa (FriendsChat.OnFriendsChanged); senza Chat, o se
        // l'avviso si perde (lui o io offline), comunque ogni 20 s con Amici aperta e ogni 90 s in Home (badge delle richieste).
        const float PollOpenSeconds = 20f, PollClosedSeconds = 90f;
        const string SeenKey = "Amici.RichiesteViste.";

        [SerializeField] private CanvasGroup view;
        [SerializeField] private Button back;
        [SerializeField] private HomeScreenV2 home;
        [SerializeField] private RoomFlowV2 roomFlow;
        [SerializeField] private QuickSelectionPanels quickPanels;
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

        [Header("Scheda amico (UI51SocialBuilder.BuildFriendCard)")]
        [SerializeField] private RectTransform card, cardPanel;
        [SerializeField] private Button cardBackdrop, cardClose, cardPrimary, cardSecondary, cardBlock;
        [SerializeField] private AvatarFrame cardAvatar;
        [SerializeField] private UI51Shape cardBanner;
        [SerializeField] private TMP_Text cardName, cardLevel, cardStatus, cardGames, cardWins, cardScope, cardPrimaryLabel, cardSecondaryLabel;

        private readonly List<UI51FriendItem> spawned = new List<UI51FriendItem>();
        // B16 (L1): invito mandato -> stanza e ora. Vale solo in quella stanza e finche' l'avviso dell'amico e' aperto (20 s): poi INVITA
        // torna (prima restava "Invitato" finche' non si ricaricava la Home).
        private readonly Dictionary<string, (string room, float at)> invited = new Dictionary<string, (string room, float at)>();
        private List<FriendEntry> friends = new List<FriendEntry>();
        private int tab;
        private bool loaded, failed, waitingForAuth, adding, emptyShown, cardBusy, confirmRemove;
        private string cardId;
        private string pendingInvite;
        private float pendingInviteAt, lastLoad;
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
            if (card != null)
            {
                cardBackdrop.onClick.AddListener(CloseCard);
                cardClose.onClick.AddListener(CloseCard);
                cardPrimary.onClick.AddListener(CardPrimary);
                cardSecondary.onClick.AddListener(CardSecondary);
                cardBlock.onClick.AddListener(CardBlock);
                card.gameObject.SetActive(false);
            }
            SetVisible(false, true);
        }

        // Come la Posta: si parte appena la sessione PlayFab e' pronta, cosi' gli inviti arrivano anche senza aprire Amici. B15: e di
        // nuovo dopo "Accedi" o la registrazione (profilo del nuovo account caricato), senza aspettare l'apertura di Amici.
        private void Start()
        {
            Load();
            if (AuthBootstrapper.Instance?.Profile != null) AuthBootstrapper.Instance.Profile.OnProfileLoaded += Load;
        }

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
            CloseCard();
            SetVisible(false, false);
        }

        private void Load()
        {
            lastLoad = Time.unscaledTime;
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
            list = list ?? new List<FriendEntry>();
            bool same = loaded && !failed && Same(friends, list); // rilettura senza novita': niente righe ricostruite
            friends = list;
            loaded = true;
            failed = false;
            var chat = FriendsChat.Ensure();
            chat.OnPresenceChanged -= OnPresence;
            chat.OnPresenceChanged += OnPresence;
            chat.OnInvite -= OnInvite;
            chat.OnInvite += OnInvite;
            chat.OnFriendsChanged -= Load;
            chat.OnFriendsChanged += Load;
            var ids = new List<string>();
            foreach (var f in friends) if (f.State == FriendState.Friend) ids.Add(f.PlayFabId);
            chat.Watch(ids);
            if (same) return;
            Render();
            if (cardId != null) { var open = Find(cardId); if (open.HasValue) BindCard(open.Value); else CloseCard(); }
        }

        private static bool Same(List<FriendEntry> a, List<FriendEntry> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (!a[i].Equals(b[i])) return false;
            return true;
        }

        private void OnPresence()
        {
            if (this != null) Render();
        }

        private static FriendPresence Presence(string id) => FriendsChat.Instance != null ? FriendsChat.Instance.PresenceOf(id) : FriendPresence.Offline;

        private void Render()
        {
            var mutual = Of(FriendState.Friend);
            var sent = Of(FriendState.Sent);
            var received = Of(FriendState.Received);
            int online = 0;
            foreach (var f in mutual) if (Presence(f.PlayFabId) != FriendPresence.Offline) online++;
            onlineLabel.text = online + " online";
            tabCounts[0].text = mutual.Count.ToString();
            tabCounts[1].text = received.Count.ToString();
            // Test 10 (terzo giro 08/10): stesso stato del pallino di Amici in Home. Il numero delle richieste e' rosso solo se ce n'e' una
            // non ancora vista; aprendo la scheda Richieste torna neutro (le richieste restano), una richiesta nuova lo riaccende.
            // Visto solo a elenco caricato: un elenco vuoto per errore o in caricamento non cancella quelle gia' viste.
            if (IsOpen && tab == 1 && loaded && !failed) MarkSeen(received);
            int unseen = Unseen(received);
            if (tabCounts[1].transform.parent.TryGetComponent<UI51Shape>(out var badge))
                badge.color = unseen > 0 ? UI51Tokens.Danger : UI51Tokens.WhiteA(0.1f);
            for (int i = 0; i < tabs.Length; i++) StyleTab(i, i == tab);
            if (home != null) home.SetFriendsBadge(unseen);
            if (!IsOpen) return;

            Clear();
            var shown = tab == 0 ? mutual : received;
            bool showFriends = loaded && (shown.Count > 0 || (tab == 0 && sent.Count > 0));
            bool empty = tab == 0 && loaded && !showFriends;
            listBlock.SetActive(showFriends);
            status.gameObject.SetActive(!showFriends && !empty);
            status.text = failed ? ErrorText : !loaded ? LoadingText : NoRequestsText;
            emptyBlock.gameObject.SetActive(empty);
            if (empty && !emptyShown) UIAnim.Pop(emptyBlock, 0.8f, 1.04f, 0.3f);
            emptyShown = empty;
            if (!showFriends) return;

            var sorted = new List<FriendEntry>(shown);
            sorted.Sort((a, b) =>
            {
                int pa = Order(Presence(a.PlayFabId)), pb = Order(Presence(b.PlayFabId));
                if (pa != pb) return pa.CompareTo(pb);
                // Offline: visto piu' di recente in cima (mockup: 2 ore fa, ieri, 3 giorni fa).
                if (pa == 2 && a.LastLogin != b.LastLogin) return Nullable.Compare(b.LastLogin, a.LastLogin);
                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCultureIgnoreCase);
            });
            if (tab == 0) sorted.AddRange(sent); // richieste mandate in fondo, "in attesa"
            var now = DeviceModeration.UtcNow;
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
            string id = f.PlayFabId;
            row.open.onClick.AddListener(() => OpenCard(id));
            if (f.State != FriendState.Friend) { BindRequest(row, f, now); return; }
            var p = Presence(f.PlayFabId);
            bool off = p == FriendPresence.Offline;
            row.avatar.SetAvatar(AvatarFor(f));
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
            bool wasInvited = WasInvited(f.PlayFabId);
            row.invite.gameObject.SetActive(p == FriendPresence.Online && !wasInvited);
            row.invited.SetActive(p == FriendPresence.Online && wasInvited);
            row.busy.SetActive(p != FriendPresence.Online);
            row.busyLabel.text = off ? "Offline" : "Occupato";
            row.invite.onClick.AddListener(() => Invite(id));
        }

        /// <summary>Richiesta ricevuta (Rifiuta e Accetta) o mandata ("In attesa"): niente stato online, non e' ancora un amico.</summary>
        private void BindRequest(UI51FriendItem row, FriendEntry f, DateTime now)
        {
            bool received = f.State == FriendState.Received;
            row.avatar.SetAvatar(AvatarFor(f));
            row.avatar.SetFrame(FrameStyle.Oro);
            row.avatar.SetTint(Color.white);
            row.dot.gameObject.SetActive(false);
            row.title.text = f.DisplayName;
            row.level.transform.parent.gameObject.SetActive(f.Level > 0);
            row.level.text = "Liv. " + f.Level;
            row.status.text = received ? "Vuole essere tuo amico" : "Richiesta inviata";
            row.status.color = UI51Tokens.CreamA(0.55f);
            row.invite.gameObject.SetActive(false);
            row.invited.SetActive(false);
            row.busy.SetActive(!received);
            row.busyLabel.text = "In attesa";
            row.accept.gameObject.SetActive(received);
            row.decline.gameObject.SetActive(received);
            string id = f.PlayFabId, name = f.DisplayName;
            row.accept.onClick.AddListener(() => Accept(id, name));
            row.decline.onClick.AddListener(() => Remove(id, "Richiesta rifiutata"));
        }

        private List<FriendEntry> Of(FriendState state) => friends.FindAll(f => f.State == state);

        // Badge di Amici in Home (secondo giro Android 08/10): richieste ricevute non ancora viste nella scheda Richieste, per account.
        private static string SeenPrefsKey => SeenKey + (AuthBootstrapper.Instance?.PlayFabAuth?.PlayFabId ?? "");

        private static int Unseen(List<FriendEntry> received)
        {
            var seen = new HashSet<string>(PlayerPrefs.GetString(SeenPrefsKey, "").Split(','));
            int n = 0;
            foreach (var f in received) if (!seen.Contains(f.PlayFabId)) n++;
            return n;
        }

        private static void MarkSeen(List<FriendEntry> received)
        {
            string value = string.Join(",", received.ConvertAll(f => f.PlayFabId));
            if (PlayerPrefs.GetString(SeenPrefsKey, "") == value) return;
            PlayerPrefs.SetString(SeenPrefsKey, value);
            PlayerPrefs.Save();
        }

        private FriendEntry? Find(string id)
        {
            foreach (var f in friends) if (f.PlayFabId == id) return f;
            return null;
        }

        // --- Richieste e scheda dell'amico (giro Android 08/10)

        private void Accept(string id, string name)
        {
            if (cardBusy) return;
            cardBusy = true;
            FriendsService.Accept(id, () =>
            {
                cardBusy = false;
                Feedback("Tu e " + name + " ora siete amici");
                Load();
            }, () => { cardBusy = false; Feedback("Non è stato possibile accettare. Riprova.", false); Load(); });
        }

        private void Remove(string id, string done)
        {
            if (cardBusy) return;
            cardBusy = true;
            FriendsService.RemoveFriend(id, () =>
            {
                cardBusy = false;
                friends.RemoveAll(f => f.PlayFabId == id); // subito, senza aspettare la lettura
                if (cardId == id) CloseCard();
                Feedback(done);
                Render();
                Load();
            }, () => { cardBusy = false; Feedback("Non è stato possibile. Riprova.", false); });
        }

        private void OpenCard(string id)
        {
            var f = Find(id);
            if (!f.HasValue || card == null) return;
            cardId = id;
            confirmRemove = false;
            bool opening = !card.gameObject.activeSelf;
            card.gameObject.SetActive(true);
            BindCard(f.Value);
            if (!opening) return;
            UIAnim.FadeIn(card, 0.2f);
            UIAnim.PopDialog(cardPanel);
        }

        private void BindCard(FriendEntry f)
        {
            var p = Presence(f.PlayFabId);
            bool mutual = f.State == FriendState.Friend;
            cardAvatar.SetAvatar(AvatarFor(f));
            // #11 (secondo giro Android 08/10): cornice e banner che l'amico ha scelto (FriendsService.FormatLook), come al tavolo.
            Project51.UIV2.Data.ProfileCosmetics.ApplyFrame(cardAvatar, Project51.UIV2.Data.ProfileCosmetics.FrameIndex(f.FrameId), 3f, 3f);
            if (cardBanner != null)
                UI51Banners.Apply(cardBanner, Project51.UIV2.Data.ProfileCosmetics.Banner(Project51.UIV2.Data.ProfileCosmetics.BannerIndex(f.BannerId)));
            cardName.text = f.DisplayName;
            cardLevel.text = f.Level > 0 ? "Liv. " + f.Level + " · " + Project51.Core.PlayerXp.Title(f.Level) : "";
            cardStatus.text = f.State == FriendState.Received ? "Vuole essere tuo amico" : f.State == FriendState.Sent ? "Richiesta inviata"
                : p == FriendPresence.Online ? "Online" : p == FriendPresence.Playing ? "In partita"
                : f.LastLogin.HasValue ? LastSeen(f.LastLogin.Value, DeviceModeration.UtcNow) : "Offline";
            cardGames.text = f.Games.ToString();
            cardWins.text = Project51.UIV2.Data.ProfileCosmetics.WinRate(f.Wins, f.Games);
            cardScope.text = f.Scope.ToString();
            // Principale: Invita (amico online) o Accetta (richiesta ricevuta); niente per gli altri casi.
            bool canInvite = mutual && p == FriendPresence.Online && !WasInvited(f.PlayFabId);
            cardPrimary.gameObject.SetActive(canInvite || f.State == FriendState.Received);
            cardPrimaryLabel.text = f.State == FriendState.Received ? "Accetta" : "Invita a giocare";
            cardSecondaryLabel.text = f.State == FriendState.Received ? "Rifiuta" : f.State == FriendState.Sent ? "Annulla richiesta"
                : confirmRemove ? "Tocca ancora per rimuoverlo" : "Rimuovi dagli amici";
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardPanel);
        }

        private void CloseCard()
        {
            cardId = null;
            if (card != null) card.gameObject.SetActive(false);
        }

        private void CardPrimary()
        {
            var f = cardId != null ? Find(cardId) : null;
            if (!f.HasValue) return;
            if (f.Value.State == FriendState.Received) { Accept(f.Value.PlayFabId, f.Value.DisplayName); return; }
            CloseCard();
            Invite(f.Value.PlayFabId);
        }

        private void CardSecondary()
        {
            var f = cardId != null ? Find(cardId) : null;
            if (!f.HasValue) return;
            if (f.Value.State == FriendState.Received) { Remove(f.Value.PlayFabId, "Richiesta rifiutata"); return; }
            if (f.Value.State == FriendState.Sent) { Remove(f.Value.PlayFabId, "Richiesta annullata"); return; }
            // Rimuovere un amico chiede un secondo tocco (niente finestra in piu').
            if (!confirmRemove) { confirmRemove = true; BindCard(f.Value); return; }
            Remove(f.Value.PlayFabId, f.Value.DisplayName + " non è più tra i tuoi amici");
        }

        private void CardBlock()
        {
            var f = cardId != null ? Find(cardId) : null;
            if (!f.HasValue) return;
            BlockList.RememberName(f.Value.PlayFabId, f.Value.DisplayName);
            if (!BlockList.SetBlocked(f.Value.PlayFabId, true)) return; // toglie anche l'amicizia (da entrambi gli elenchi)
            friends.RemoveAll(x => x.PlayFabId == f.Value.PlayFabId);
            CloseCard();
            UI51Toast.Show("Giocatore bloccato: niente emoticon, inviti o amicizia da lui");
            Render();
        }

        /// <summary>"Ultimo accesso poco fa / N ore fa / ieri / N giorni fa": e' l'ora dell'ultimo login PlayFab (B15, non "visto").</summary>
        public static string LastSeen(DateTime thenUtc, DateTime nowUtc)
        {
            var span = nowUtc - thenUtc;
            if (span.TotalHours < 1) return "Ultimo accesso poco fa";
            if (span.TotalHours < 24) { int h = (int)span.TotalHours; return h == 1 ? "Ultimo accesso 1 ora fa" : "Ultimo accesso " + h + " ore fa"; }
            return "Ultimo accesso " + NewsService.RelativeTime(thenUtc, nowUtc);
        }

        /// <summary>#9 (giro Android 08/10): l'avatar che l'amico ha scelto (AvatarUrl del suo profilo), se l'ha pubblicato.</summary>
        private Sprite AvatarFor(FriendEntry f) =>
            (f.AvatarId != null ? HomeV2Integration.AvatarById(f.AvatarId) : null) ?? PortraitFor(f.PlayFabId, avatars);

        private Sprite AvatarFor(string id)
        {
            var f = Find(id);
            return f.HasValue ? AvatarFor(f.Value) : PortraitFor(id, avatars);
        }

        // ponytail: ritratto scelto dal PlayFab ID per chi non ha pubblicato l'avatar (versioni vecchie). Lo usa anche la Classifica.
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
            FriendsService.AddFriendByName(name, now =>
            {
                if (this == null) return;
                adding = false;
                add.interactable = true;
                nameInput.text = "";
                Feedback(now ? "Tu e " + name + " ora siete amici" : "Richiesta inviata a " + name);
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

        /// <summary>
        /// Invita: Modalita' sulla scheda Stanza privata (formato e CREA STANZA); appena la stanza c'e', il codice parte verso
        /// l'amico. Amici si chiude: Modalita' sta sotto (UIV2_Home).
        /// </summary>
        private void Invite(string friendId)
        {
            if (quickPanels == null || MatchmakingManager.Instance == null) return;
            Subscribe();
            pendingInvite = friendId;
            pendingInviteAt = Time.unscaledTime;
            Close();
            quickPanels.OpenPrivateRoom();
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
                invited[id] = (code, Time.unscaledTime);
                Render();
            }
            else Feedback("Invito non inviato: riprova tra poco", false);
        }

        // --- Sala privata (mockup SalaPrivata, INVITA AMICI ONLINE)

        /// <summary>Amici online adesso, piu' quelli gia' entrati nella stanza (a un tavolo non risultano piu' online).</summary>
        public List<FriendEntry> RoomFriends(ICollection<string> roomNames) =>
            friends.FindAll(f => f.State == FriendState.Friend && (Presence(f.PlayFabId) == FriendPresence.Online || roomNames.Contains(f.DisplayName)));

        public Sprite Portrait(string friendId) => AvatarFor(friendId);

        public bool WasInvited(string friendId) =>
            PhotonNetwork.InRoom && invited.TryGetValue(friendId, out var i) && i.room == PhotonNetwork.CurrentRoom.Name
            && Time.unscaledTime - i.at < UI51InviteBanner.Seconds;

        /// <summary>Manda il codice della stanza in cui sono all'amico; format = "1 vs 1", "2 vs 2", "1 vs 3".</summary>
        public void InviteToRoom(string friendId, string format)
        {
            if (!PhotonNetwork.InRoom) return;
            if (FriendsChat.Instance != null && FriendsChat.Instance.SendInvite(friendId, PhotonNetwork.CurrentRoom.Name, format, MyName))
            {
                invited[friendId] = (PhotonNetwork.CurrentRoom.Name, Time.unscaledTime);
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
            if (loaded && Time.unscaledTime - lastLoad > (IsOpen ? PollOpenSeconds : PollClosedSeconds)) Load();
            if (!IsOpen || !Input.GetKeyDown(KeyCode.Escape)) return;
            if (card != null && card.gameObject.activeSelf) CloseCard();
            else Close();
        }

        private void OnDestroy()
        {
            fade?.Kill();
            StopWaitingForAuth();
            if (AuthBootstrapper.Instance?.Profile != null) AuthBootstrapper.Instance.Profile.OnProfileLoaded -= Load;
            if (home != null) home.OnFriendsPressed -= Open;
            if (manager != null) manager.OnRoomCreated -= RoomCreated;
            if (FriendsChat.Instance != null)
            {
                FriendsChat.Instance.OnPresenceChanged -= OnPresence;
                FriendsChat.Instance.OnInvite -= OnInvite;
                FriendsChat.Instance.OnFriendsChanged -= Load;
            }
        }
    }
}
