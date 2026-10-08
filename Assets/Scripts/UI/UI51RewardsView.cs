using System;
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
    /// UI51 Fase 8, Premi giornalieri (mockup Premi e PremiRiscattato), aperta dal pulsante Premi della Home: serie di accessi col
    /// tempo che resta, giorni 1-6, gran premio del giorno 7, RISCATTA IL PREMIO DI OGGI / TORNA A GIOCARE.
    /// Serie e premi li tiene il server (RewardsService, CloudScript "statoPremi" / "riscattaPremio"; il giorno cambia a mezzanotte
    /// italiana). Senza risposta dal server si vede il giorno 1 e RISCATTA dice "non disponibile". Show(giorno, riscattato) serve
    /// anche per le prove. Costruita da UI51SocialBuilder.
    /// </summary>
    public sealed class UI51RewardsView : MonoBehaviour
    {
        public const int Days = 7;
        const string ClaimText = "RISCATTA IL PREMIO DI OGGI";

        /// <summary>
        /// Premio di un giorno come lo mostra la pagina: kind indice in rewardIcons (0 monete, 1 gemme, 2 forziere verde, 3 viola),
        /// larghezza dell'icona dal mockup. ponytail: copia della tabella "settimana" del server (DEFAULTS in 51.js); se la si
        /// cambia in Title Data "Economia", qui va cambiata a mano.
        /// </summary>
        public readonly struct Reward
        {
            public readonly int kind, amount;
            public readonly float iconWidth;
            public Reward(int kind, int amount, float iconWidth) { this.kind = kind; this.amount = amount; this.iconWidth = iconWidth; }
            public string Label => kind == 2 ? "Forziere" : amount.ToString();
        }

        /// <summary>Giorni 1-6 del mockup; il 7 (forziere viola + 25 gemme) e' la scheda grande.</summary>
        public static readonly Reward[] Week =
        {
            new Reward(0, 50, 40f), new Reward(0, 100, 44f), new Reward(1, 5, 28f),
            new Reward(0, 150, 48f), new Reward(2, 1, 58f), new Reward(1, 10, 32f),
        };

        [SerializeField] private CanvasGroup view;
        [SerializeField] private Button back;
        [SerializeField] private HomeScreenV2 home;
        [SerializeField] private TMP_Text streakLabel, timerCaption, timerLabel;
        [SerializeField] private UI51RewardDay[] days = new UI51RewardDay[0];

        [Header("Giorno 7")]
        [SerializeField] private UI51Shape grandPulse;
        [SerializeField] private RectTransform grandChest;
        [SerializeField] private TMP_Text grandNote;

        [Header("Pulsanti")]
        [SerializeField] private Button claim;
        [SerializeField] private TMP_Text claimLabel;
        [SerializeField] private Button playAgain;

        [Header("Riscatto")]
        [SerializeField] private GameObject celebration;
        [SerializeField] private RectTransform burst, rise;
        [SerializeField] private Image riseIcon;
        [SerializeField] private TMP_Text riseLabel;
        [SerializeField] private Sprite[] rewardIcons = new Sprite[0];
        [Tooltip("UI51 Fase 15: Forziere, per i premi con un forziere (giorni 5 e 7).")]
        [SerializeField] private UI51ChestView chest;

        private int today = 1;
        private bool claimed, claiming;
        private bool known; // 26b: dati del server per l'account attuale (dopo Accedi la pagina non mostra ne' riscatta quelli di prima)
        private int shownSecond = -1;
        private Tween fade;

        public bool IsOpen => view != null && view.blocksRaycasts;

        private void Awake()
        {
            back.onClick.AddListener(Close);
            claim.onClick.AddListener(Claim);
            playAgain.onClick.AddListener(Close);
            foreach (var d in days) d.button.onClick.AddListener(Claim);
            celebration.SetActive(false);
            if (home != null) home.OnRewardsPressed += Open;
            RewardsService.DailyChanged += FromServer;
            SetVisible(false, true);
        }

        // Pallino sulla Home appena il server dice com'e' messa la serie (dopo il login, come la Posta).
        private void Start() => FromServer();

        public void Open()
        {
            SetVisible(true, false);
            FromServer();
            RewardsService.RefreshDaily(null, null); // il giorno puo' essere cambiato mentre l'app era aperta
        }

        private void FromServer()
        {
            if (this == null) return;
            var d = RewardsService.Daily;
            if (home != null) home.SetRewardsBadge(d != null && !d.riscattato ? 1 : 0);
            if (d != null) { known = true; Show(d.giorno, d.riscattato); return; }
            known = false;
            today = 1;
            claimed = false;
            if (IsOpen) Render();
        }

        public void Close()
        {
            if (!IsOpen) return;
            SetVisible(false, false);
        }

        /// <summary>Giorno della serie (1-7) e se il premio di oggi e' gia' stato riscattato.</summary>
        public void Show(int day, bool claimedToday)
        {
            today = Mathf.Clamp(day, 1, Days);
            claimed = claimedToday;
            Render();
        }

        private void Render()
        {
            int streak = claimed ? today : today - 1;
            streakLabel.text = streak == 1 ? "1 giorno di fila" : streak + " giorni di fila";
            timerCaption.text = claimed ? "Prossimo premio tra" : "Il premio scade tra";
            shownSecond = -1;

            for (int i = 0; i < days.Length && i < Week.Length; i++) BindDay(days[i], i + 1, Week[i]);

            int left = Days - today;
            grandNote.text = today == Days ? (claimed ? "Riscattato · si riparte domani!" : "È oggi · riscatta il gran premio!")
                : (left == 1 ? "Manca 1 giorno" : "Mancano " + left + " giorni") + " · non saltare un giorno!";
            UIAnim.Stop(grandPulse);
            if (today == Days && !claimed) UIAnim.Pulse(grandPulse, 8f);
            UIAnim.Stop(grandChest);
            grandChest.anchoredPosition = Vector2.zero;
            UIAnim.Float(grandChest);

            claim.gameObject.SetActive(!claimed);
            claim.interactable = known && !claiming;
            claimLabel.DOKill();
            // un aggiornamento a meta' riscatto non deve far sembrare perso il tocco; senza dati del server si aspetta
            claimLabel.text = claiming || !known ? "…" : ClaimText;
            playAgain.gameObject.SetActive(claimed);
        }

        private void BindDay(UI51RewardDay d, int n, Reward r)
        {
            bool isToday = n == today && !claimed && known, done = known && (n < today || (n == today && claimed));
            d.button.interactable = isToday && !claiming;
            d.caption.text = isToday ? "OGGI" : "GIORNO " + n;
            d.caption.font = UI51Tokens.Font(isToday ? FontFace.CinzelBold : FontFace.CinzelSemiBold);
            d.caption.color = isToday ? UI51Tokens.Gold : UI51Tokens.CreamA(done ? 0.5f : 0.6f);
            d.label.text = r.Label;
            d.label.fontSize = isToday ? 14f : 13f;
            d.label.color = done ? UI51Tokens.CreamA(0.4f) : UI51Tokens.Cream;

            if (isToday)
            {
                d.face.fill = UI51Shape.Linear((UI51Tokens.GoldA(0.22f), 0f), (UI51Tokens.Rgba(12, 26, 50, 0.95f), 1f));
                d.face.borderWidth = 2f;
                d.face.borderColor = UI51Tokens.Gold;
            }
            else
            {
                d.face.fill = UI51Shape.Solid(UI51Tokens.Rgba(6, 13, 27, done ? 0.7f : 0.6f));
                d.face.borderWidth = 1f;
                d.face.borderColor = done ? UI51Tokens.Rgba(127, 224, 184, 0.35f) : UI51Tokens.GoldA(0.2f);
            }
            d.claimPill.SetActive(isToday);
            d.check.SetActive(done);

            d.icon.sprite = r.kind < rewardIcons.Length ? rewardIcons[r.kind] : null;
            float h = d.icon.sprite != null ? r.iconWidth * d.icon.sprite.rect.height / d.icon.sprite.rect.width : r.iconWidth;
            // L'icona fluttua: sta centrata in un posto della colonna (un figlio animato di un LayoutGroup verrebbe rimesso a posto).
            var slot = d.icon.transform.parent.GetComponent<LayoutElement>();
            slot.preferredWidth = r.iconWidth;
            slot.preferredHeight = h;
            d.icon.rectTransform.sizeDelta = new Vector2(r.iconWidth, h);
            // grayscale(.6) del mockup non c'e': grigio chiaro alla stessa trasparenza.
            d.icon.color = done ? new Color(0.72f, 0.72f, 0.72f, 0.35f) : new Color(1f, 1f, 1f, isToday ? 1f : 0.85f);

            var iconRt = d.icon.rectTransform;
            UIAnim.Stop(iconRt);
            iconRt.anchoredPosition = Vector2.zero;
            UIAnim.Stop(d.pulse);
            if (!isToday) return;
            UIAnim.Pulse(d.pulse, 8f);
            UIAnim.Float(iconRt);
        }

        private void Claim()
        {
            if (claimed || claiming || !known) return;
            claiming = true;
            Render(); // "…" e pulsanti spenti finche' il server non risponde
            // Secondo giro 08/10: si festeggia solo quello che il server ha dato davvero. Prima si festeggiava il premio previsto e, se il
            // server diceva no, il premio tornava riscattabile: sembrava riscattato piu' volte.
            RewardsService.ClaimDaily(r =>
            {
                claiming = false;
                if (this == null) return;
                if (r.ok)
                {
                    claimed = true;
                    RewardsService.PlaySound(r);
                    if (chest == null || !chest.Open(r)) Celebrate(r);
                    Render(); // Show arriva anche da DailyChanged (FromServer)
                    return;
                }
                // Terzo giro 08/10: preso ma l'accredito non e' ancora confermato: il server lo ritenta (registro "Consegne").
                if (r.inConsegna) UI51Toast.Show("Premio preso: arriva appena il server conferma");
                if (r.riscattato) { claimed = true; Render(); return; } // gia' riscattato (altro tocco, altro telefono): TORNA A GIOCARE
                Render();
                Say(r.ospite ? "SOLO CON UN ACCOUNT" : "NON DISPONIBILE");
                Resync();
            }, _ =>
            {
                claiming = false;
                if (this == null) return;
                Render();
                Say("RIPROVA PIÙ TARDI");
                Resync();
            });
        }

        // B22 (M3): dopo un no o nessuna risposta, serie e saldo si rileggono dal server (il riscatto puo' essere passato lo stesso).
        private static void Resync()
        {
            RewardsService.RefreshDaily(null, null);
            WalletService.Refresh();
        }

        private void Say(string text)
        {
            claimLabel.DOKill();
            claimLabel.text = text;
            DOVirtual.DelayedCall(2f, () => claimLabel.text = ClaimText, true).SetTarget(claimLabel).SetLink(claimLabel.gameObject);
        }

        /// <summary>Bagliore e premio che sale (2.7 s): il forziere (aperto dal server) o la valuta, col numero delle monete o gemme.</summary>
        public void Celebrate(ServerReward r)
        {
            int kind = r.forzieri.Length > 0 ? (r.forzieri[0].colore == "verde" ? 2 : 3) : r.monete > 0 ? 0 : 1;
            riseIcon.sprite = kind < rewardIcons.Length ? rewardIcons[kind] : null;
            riseLabel.text = "+" + (r.monete > 0 ? r.monete : r.gemme);
            celebration.SetActive(true);
            UIAnim.RewardBurst(burst);
            UIAnim.RewardRise(rise);
            DOVirtual.DelayedCall(UIAnim.RewardRiseSeconds + 0.1f, () => celebration.SetActive(false), true).SetLink(celebration);
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            // Fino alla mezzanotte italiana detta dal server; senza server, mezzanotte del telefono.
            var d = RewardsService.Daily;
            TimeSpan left;
            if (d != null) left = TimeSpan.FromSeconds(Mathf.Max(0f, d.secondi - (Time.realtimeSinceStartup - RewardsService.DailyAt)));
            else { var now = DateTime.Now; left = now.Date.AddDays(1) - now; }
            int second = (int)left.TotalSeconds;
            if (d != null && second == 0 && shownSecond != 0) RewardsService.RefreshDaily(null, null); // giorno nuovo
            if (second == shownSecond) return;
            shownSecond = second;
            timerLabel.text = $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
        }

        private void SetVisible(bool visible, bool instant)
        {
            fade?.Kill();
            view.blocksRaycasts = visible;
            view.interactable = visible;
            if (instant) view.alpha = visible ? 1f : 0f;
            else fade = view.DOFade(visible ? 1f : 0f, 0.2f).SetUpdate(true).SetLink(gameObject);
        }

        private void OnDestroy()
        {
            fade?.Kill();
            if (home != null) home.OnRewardsPressed -= Open;
            RewardsService.DailyChanged -= FromServer;
        }
    }
}
