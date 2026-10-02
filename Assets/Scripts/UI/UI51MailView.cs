using System;
using System.Collections.Generic;
using DG.Tweening;
using Project51.Auth;
using Project51.UI51;
using Project51.UIV2.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 8, pagina Posta (mockup Posta e PostaMessaggio), aperta dal pulsante Posta della Home. Messaggi da MailService;
    /// il tocco su una riga apre il messaggio dal basso e lo segna letto. Il numero sul pulsante della Home = messaggi non letti.
    /// RISCATTA e Raccogli tutto chiedono i premi al server (RewardsService, CloudScript): il pulsante dice per un attimo cosa e'
    /// arrivato ("+250 monete · +5 gemme"), poi il messaggio diventa Riscattato. Costruita da UI51SocialBuilder.
    /// </summary>
    public sealed class UI51MailView : MonoBehaviour
    {
        public const string LoadingText = "Caricamento…";
        public const string ErrorText = "Non è stato possibile caricare la posta.\nControlla la connessione e riprova.";
        const string ClaimText = "RISCATTA", ClaimAllText = "Raccogli tutto";

        /// <summary>Tipi di messaggio del mockup, nell'ordine di kindIcons: tessera e larghezza dell'icona.</summary>
        public static readonly string[] Kinds = { "team", "stagione", "amico", "avviso", "torneo" };
        static readonly float[] KindIconWidth = { 26f, 34f, 40f, 24f, 30f };
        /// <summary>Allegati, nell'ordine di giftIcons: icona nella pillola (w, h) e nel messaggio aperto (w).</summary>
        public static readonly string[] GiftKinds = { "monete", "gemme", "forziere" };
        static readonly Vector2[] ChipIconSize = { new Vector2(14f, 14f), new Vector2(10f, 13f), new Vector2(18f, 14f) };
        static readonly float[] GiftIconWidth = { 34f, 26f, 52f };

        [SerializeField] private CanvasGroup view;
        [SerializeField] private Button back;
        [SerializeField] private HomeScreenV2 home;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private TMP_Text status, unreadLabel;
        [SerializeField] private Button claimAll;
        [SerializeField] private TMP_Text claimAllLabel;

        [Header("Elenco")]
        [SerializeField] private GameObject listBlock;
        [SerializeField] private RectTransform rows;
        [SerializeField] private UI51MailItem rowTemplate;

        [Header("Nessun messaggio (mockup PostaVuota)")]
        [SerializeField] private RectTransform emptyBlock;
        [SerializeField] private Button backToPlay;

        [Header("Messaggio")]
        [SerializeField] private BottomSheet sheet;
        [SerializeField] private UI51MailItem detail;
        [SerializeField] private GameObject giftsBlock;
        [SerializeField] private CanvasGroup giftsGroup;
        [SerializeField] private GameObject[] giftSlots = new GameObject[0];
        [SerializeField] private Image[] giftImages = new Image[0];
        [SerializeField] private TMP_Text[] giftLabels = new TMP_Text[0];
        [SerializeField] private Button claim, closeButton;
        [SerializeField] private TMP_Text claimLabel;
        [SerializeField] private GameObject claimedBadge;

        [Header("Icone")]
        [SerializeField] private Sprite[] kindIcons = new Sprite[0];
        [SerializeField] private Sprite[] giftIcons = new Sprite[0];
        [SerializeField] private UI51ChestView chest;

        private readonly List<UI51MailItem> spawned = new List<UI51MailItem>();
        private List<MailMessage> messages = new List<MailMessage>();
        private MailMessage opened;
        private bool waitingForAuth, claiming;
        private Tween fade;

        public bool IsOpen => view != null && view.blocksRaycasts;

        private void Awake()
        {
            back.onClick.AddListener(Close);
            claimAll.onClick.AddListener(ClaimAll);
            claim.onClick.AddListener(Claim);
            backToPlay.onClick.AddListener(Close);
            closeButton.onClick.AddListener(() => sheet.Close());
            rowTemplate.gameObject.SetActive(false);
            sheet.Hide();
            if (home != null) home.OnMailPressed += Open;
            SetVisible(false, true);
        }

        // Numero sulla Home appena la sessione PlayFab e' pronta.
        private void Start() => Load();

        public void Open()
        {
            SetVisible(true, false);
            scroll.verticalNormalizedPosition = 1f;
            Load();
        }

        public void Close()
        {
            if (!IsOpen) return;
            sheet.Hide();
            SetVisible(false, false);
        }

        private void Load()
        {
            if (IsOpen && spawned.Count == 0) ShowStatus(LoadingText);
            var auth = AuthBootstrapper.Instance;
            if (auth != null && !auth.IsReady && !auth.HasError)
            {
                if (!waitingForAuth) { waitingForAuth = true; auth.OnAuthReady += OnAuthReady; }
                return;
            }
            // Prima il server consegna la Posta per tutti (una volta per sessione), poi si legge.
            RewardsService.Start(() => MailService.Fetch(Fill, () => { if (this != null && IsOpen && spawned.Count == 0) ShowStatus(ErrorText); }));
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

        /// <summary>Anche per le prove: mostra questi messaggi come se arrivassero da PlayFab.</summary>
        public void Fill(List<MailMessage> list)
        {
            if (this == null) return;
            messages = list ?? new List<MailMessage>();
            Render();
        }

        private void Render()
        {
            int unread = MailService.UnreadCount(messages);
            if (home != null) home.SetMailBadge(unread);
            unreadLabel.text = unread == 0 ? "Tutto letto" : unread == 1 ? "1 messaggio non letto" : unread + " messaggi non letti";
            int claimable = 0;
            foreach (var m in messages) if (m.CanClaim) claimable++;
            claimAll.gameObject.SetActive(claimable > 1);
            if (!IsOpen) return;

            Clear();
            status.gameObject.SetActive(false);
            bool wasEmpty = emptyBlock.gameObject.activeSelf;
            emptyBlock.gameObject.SetActive(messages.Count == 0);
            if (messages.Count == 0 && !wasEmpty) UIAnim.Pop(emptyBlock, 0.8f, 1.04f, 0.3f);
            listBlock.SetActive(messages.Count > 0);
            var now = DateTime.UtcNow;
            foreach (var m in messages)
            {
                var row = Instantiate(rowTemplate, rows);
                row.name = "Mail_" + m.id;
                row.gameObject.SetActive(true);
                BindRow(row, m, MailService.IsRead(m), now);
                var msg = m;
                row.button.onClick.AddListener(() => OpenMessage(msg));
                spawned.Add(row);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        }

        private void BindRow(UI51MailItem row, MailMessage m, bool read, DateTime now)
        {
            Tile(row, m);
            row.icon.color = new Color(1f, 1f, 1f, read && !m.HasGifts ? 0.6f : 1f);
            row.unread.SetActive(!read);
            row.title.text = m.titolo;
            row.title.font = UI51Tokens.Font(read ? FontFace.NunitoBold : FontFace.NunitoExtraBold);
            row.title.color = read ? UI51Tokens.CreamA(0.75f) : UI51Tokens.Cream;
            row.time.text = NewsService.RelativeTime(m.DateUtc, now);
            row.snippet.text = (m.testo ?? string.Empty).Replace('\n', ' ');
            for (int i = 0; i < row.chips.Length; i++)
            {
                bool used = i < m.allegati.Length;
                row.chips[i].SetActive(used);
                if (!used) continue;
                int g = GiftIndex(m.allegati[i]);
                row.chipIcons[i].sprite = Get(giftIcons, g);
                var size = row.chipIcons[i].GetComponent<LayoutElement>();
                size.preferredWidth = ChipIconSize[g].x;
                size.preferredHeight = ChipIconSize[g].y;
                row.chipLabels[i].text = g == 2 ? "Forziere" : "+" + m.allegati[i].quantita;
            }
            row.chipsGroup.alpha = m.riscattato ? 0.4f : 1f;
            row.claimed.SetActive(m.HasGifts && m.riscattato);
            string expiry = MailService.ExpiryLabel(m, now);
            row.expiry.gameObject.SetActive(expiry.Length > 0);
            row.expiry.text = expiry;
            // Senza allegati ne' scadenza la fila delle pillole non c'e' (mockup: titolo e anteprima centrati nella riga).
            var chipsRow = row.chipsGroup.transform.parent;
            chipsRow.gameObject.SetActive(m.HasGifts || expiry.Length > 0);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)chipsRow.parent);
        }

        private void OpenMessage(MailMessage m)
        {
            opened = m;
            bool wasRead = MailService.IsRead(m);
            MailService.MarkRead(m, messages);
            Tile(detail, m);
            detail.title.text = m.titolo;
            detail.time.text = m.da + " · " + NewsService.RelativeTime(m.DateUtc, DateTime.UtcNow);
            detail.snippet.text = m.testo;
            giftsBlock.SetActive(m.HasGifts);
            for (int i = 0; i < giftSlots.Length; i++)
            {
                bool used = i < m.allegati.Length;
                giftSlots[i].SetActive(used);
                if (!used) continue;
                int g = GiftIndex(m.allegati[i]);
                SetIcon(giftImages[i], Get(giftIcons, g), GiftIconWidth[g]);
                giftLabels[i].text = g == 2 ? "Forziere" : "+" + m.allegati[i].quantita;
            }
            giftsGroup.alpha = m.riscattato ? 0.5f : 1f;
            claim.gameObject.SetActive(m.CanClaim);
            claimLabel.DOKill();
            claimLabel.text = ClaimText;
            claimedBadge.SetActive(m.HasGifts && m.riscattato);
            closeButton.gameObject.SetActive(!m.HasGifts);
            sheet.Open();
            if (!wasRead) Render();
        }

        private void Tile(UI51MailItem item, MailMessage m)
        {
            int k = Mathf.Max(0, Array.IndexOf(Kinds, (m.tipo ?? string.Empty).Trim().ToLowerInvariant()));
            Color bg, border;
            switch (k)
            {
                case 1: bg = UI51Tokens.Rgba(124, 58, 237, 0.18f); border = UI51Tokens.Rgba(167, 139, 250, 0.5f); break;
                case 2: bg = UI51Tokens.Rgba(39, 181, 133, 0.12f); border = UI51Tokens.Rgba(127, 224, 184, 0.45f); break;
                case 3: bg = UI51Tokens.WhiteA(0.05f); border = UI51Tokens.CreamA(0.2f); break;
                default: bg = UI51Tokens.GoldA(0.12f); border = UI51Tokens.GoldA(0.45f); break;
            }
            item.tile.fill = UI51Shape.Solid(bg);
            item.tile.borderColor = border;
            SetIcon(item.icon, Get(kindIcons, k), KindIconWidth[k]);
        }

        /// <summary>Larghezza dal mockup, altezza dalla proporzione dello sprite (height:auto).</summary>
        private static void SetIcon(Image image, Sprite sprite, float width)
        {
            image.sprite = sprite;
            float h = sprite != null ? width * sprite.rect.height / sprite.rect.width : width;
            image.rectTransform.sizeDelta = new Vector2(width, h);
        }

        private static int GiftIndex(MailGift g) => Mathf.Max(0, Array.IndexOf(GiftKinds, (g?.tipo ?? string.Empty).Trim().ToLowerInvariant()));

        private static Sprite Get(Sprite[] sprites, int i) => i < sprites.Length ? sprites[i] : null;

        private void Claim()
        {
            if (claiming || opened == null || !opened.CanClaim) return;
            claiming = true;
            var m = opened;
            Say(claimLabel, "…", ClaimText, 0f);
            RewardsService.ClaimMail(m.id, r =>
            {
                claiming = false;
                if (this == null) return;
                if (!r.ok) { Say(claimLabel, "NON DISPONIBILE", ClaimText, 2f); return; }
                MarkClaimed(r);
                if (chest != null) chest.Open(r); // UI51 Fase 15: un forziere si apre sulla sua schermata
                // Per un attimo cosa e' arrivato (i forzieri si aprono sul server), poi RISCATTATO.
                Say(claimLabel, RewardsService.Summary(r).ToUpperInvariant(), ClaimText, 1.6f, () => { if (sheet.isOpen && opened == m) OpenMessage(m); });
            }, _ =>
            {
                claiming = false;
                if (this != null) Say(claimLabel, "RIPROVA PIÙ TARDI", ClaimText, 2f);
            });
        }

        private void ClaimAll()
        {
            if (claiming) return;
            claiming = true;
            Say(claimAllLabel, "…", ClaimAllText, 0f);
            RewardsService.ClaimAllMail(r =>
            {
                claiming = false;
                if (this == null) return;
                if (!r.ok) { Say(claimAllLabel, "Niente da raccogliere", ClaimAllText, 2f); return; }
                MarkClaimed(r, false); // Raccogli tutto sparirebbe subito: l'elenco si aggiorna dopo il messaggio
                Say(claimAllLabel, RewardsService.Summary(r), ClaimAllText, 2f, () => { if (this != null) Render(); });
            }, _ =>
            {
                claiming = false;
                if (this != null) Say(claimAllLabel, "Riprova più tardi", ClaimAllText, 2f);
            });
        }

        private void MarkClaimed(ServerReward r, bool render = true)
        {
            foreach (var m in messages) if (Array.IndexOf(r.riscattati, m.id) >= 0) m.riscattato = true;
            if (render) Render();
        }

        /// <summary>Il pulsante dice text per seconds (0 = finche' non cambia di nuovo), poi torna a rest e chiama then.</summary>
        private static void Say(TMP_Text label, string text, string rest, float seconds, Action then = null)
        {
            label.DOKill();
            label.text = text;
            if (seconds <= 0f) return;
            DOVirtual.DelayedCall(seconds, () => { label.text = rest; then?.Invoke(); }, true).SetTarget(label).SetLink(label.gameObject);
        }

        private void ShowStatus(string text)
        {
            Clear();
            listBlock.SetActive(false);
            emptyBlock.gameObject.SetActive(false);
            status.text = text;
            status.gameObject.SetActive(true);
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
            if (sheet.isOpen) sheet.Close();
            else Close();
        }

        private void OnDestroy()
        {
            fade?.Kill();
            StopWaitingForAuth();
            if (home != null) home.OnMailPressed -= Open;
        }
    }
}
