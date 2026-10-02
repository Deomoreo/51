using System;
using System.Collections.Generic;
using Project51.Auth;
using Project51.UI51;
using Project51.UIV2.Core;
using Project51.UIV2.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 15: Classifica (mockup Classifica) dal pulsante Classifica della Home. Classifiche di PlayFab (LeaderboardService):
    /// Settimana = XP della settimana, Amici = la stessa tra gli amici, Sempre = XP totali. Podio, posizioni dalla 4 alla 50 che
    /// scorrono, la propria riga fissata in basso. Niente premi per fascia (scelta 02/10). Grafica: UI51ProgressBuilder.
    /// </summary>
    public sealed class UI51RankingView : MonoBehaviour
    {
        [SerializeField] private HomeScreenV2 home;
        [Tooltip("La pagina che si accende: questo oggetto resta acceso perche' Awake si iscriva al pulsante della Home.")]
        [SerializeField] private GameObject page;
        [SerializeField] private Button back;
        [SerializeField] private TMP_Text subtitle, status;
        [SerializeField] private SegmentedTabs tabs;
        [Tooltip("Podio: 0 primo, 1 secondo, 2 terzo. Ognuno ha Avatar (AvatarFrame), Name e Block/Value.")]
        [SerializeField] private RectTransform[] podium = new RectTransform[0];
        [SerializeField] private GameObject list;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform rowTemplate; // spento: Pos, Avatar, Texts/Name, Texts/Level, Value
        [Header("Tu")]
        [SerializeField] private TMP_Text myPosition, myName, myNote, myValue;
        [SerializeField] private AvatarFrame myAvatar;
        [SerializeField] private Sprite[] avatars = new Sprite[0];

        static readonly string[] Subtitles = { null, "I tuoi amici questa settimana", "Classifica di sempre" };

        private readonly List<GameObject> rows = new List<GameObject>();
        private int request;

        private void Awake()
        {
            back.onClick.AddListener(() => page.SetActive(false));
            tabs.onTabChanged.AddListener(Load);
            if (home != null) home.OnRankingPressed += Open;
        }

        private void OnDestroy()
        {
            if (home != null) home.OnRankingPressed -= Open;
        }

        public void Open()
        {
            page.SetActive(true);
            UIAnim.FadeIn((RectTransform)page.transform, 0.2f);
            tabs.Select(0, false);
            Load(0);
        }

        private void Load(int tab)
        {
            int id = ++request;
            string stat = tab == 2 ? LeaderboardService.AllTime : LeaderboardService.Weekly;
            subtitle.text = Subtitles[tab] ?? "Classifica della settimana";
            Show(null);
            status.text = "Caricamento…";
            BindMe(null, null);
            Action<List<RankEntry>, DateTime?> done = (entries, reset) =>
            {
                if (this == null || id != request) return;
                if (tab == 0 && reset.HasValue) subtitle.text = LeaderboardService.ResetText(reset.Value - DateTime.UtcNow);
                Show(entries);
                status.text = entries.Count == 0 ? (tab == 1 ? "Nessun amico in classifica questa settimana." : "Nessuno in classifica per ora.") : "";
                int tenth = entries.Count >= 10 ? entries[9].Value : -1;
                if (tab == 1) BindMe(entries.Find(e => e.PlayFabId == MyId()) ?? new RankEntry(), entries.Count >= 10 ? tenth : -1);
                else LeaderboardService.Me(stat, me => { if (this != null && id == request) BindMe(me ?? new RankEntry(), tenth); },
                    () => { if (this != null && id == request) BindMe(new RankEntry(), tenth); });
            };
            Action fail = () =>
            {
                if (this == null || id != request) return;
                status.text = "Classifica non disponibile. Riprova più tardi.";
            };
            if (tab == 1) LeaderboardService.Friends(stat, done, fail);
            else LeaderboardService.Top(stat, done, fail);
        }

        /// <summary>Podio e righe; null = tutto spento (caricamento).</summary>
        private void Show(List<RankEntry> entries)
        {
            foreach (var row in rows) Destroy(row);
            rows.Clear();
            for (int i = 0; i < podium.Length; i++)
            {
                bool on = entries != null && i < entries.Count;
                podium[i].gameObject.SetActive(on);
                if (!on) continue;
                var e = entries[i];
                podium[i].Find("Avatar").GetComponent<AvatarFrame>().SetAvatar(UI51FriendsView.PortraitFor(e.PlayFabId, avatars));
                podium[i].Find("Name").GetComponent<TMP_Text>().text = e.Name;
                podium[i].Find("Block/Value").GetComponent<TMP_Text>().text = LeaderboardService.Format(e.Value);
                UIAnim.FadeUp(podium[i], 0.1f * (2 - i));
            }
            list.SetActive(entries != null && entries.Count > podium.Length);
            if (entries == null) return;
            var shown = new List<RectTransform>();
            for (int i = podium.Length; i < entries.Count; i++)
            {
                var e = entries[i];
                var row = Instantiate(rowTemplate, rowTemplate.parent);
                row.gameObject.SetActive(true);
                rows.Add(row.gameObject);
                row.Find("Pos").GetComponent<TMP_Text>().text = e.Position.ToString();
                row.Find("Avatar").GetComponent<AvatarFrame>().SetAvatar(UI51FriendsView.PortraitFor(e.PlayFabId, avatars));
                row.Find("Texts/Name").GetComponent<TMP_Text>().text = e.Name;
                var level = row.Find("Texts/Level");
                level.gameObject.SetActive(e.Level > 0);
                level.GetComponent<TMP_Text>().text = "Liv. " + e.Level;
                row.Find("Value").GetComponent<TMP_Text>().text = LeaderboardService.Format(e.Value);
                if (shown.Count < 8) shown.Add(row);
            }
            scroll.verticalNormalizedPosition = 1f;
            UIAnim.RowsIn(shown, 0.06f, 0.15f);
        }

        /// <summary>La riga fissata: me null = in attesa; Position 0 = fuori classifica. Gli ospiti non entrano in classifica.</summary>
        private void BindMe(RankEntry me, int? tenth)
        {
            var auth = AuthBootstrapper.Instance;
            bool guest = auth == null || auth.PlayFabAuth == null || !auth.PlayFabAuth.HasRealLogin;
            string name = auth != null && auth.Profile != null ? auth.Profile.DisplayName : null;
            myName.text = string.IsNullOrEmpty(name) || guest ? "Tu" : "Tu · " + name;
            myAvatar.SetAvatar(HomeV2Integration.LocalAvatar != null ? HomeV2Integration.LocalAvatar : UI51FriendsView.PortraitFor(MyId(), avatars));
            bool ranked = me != null && me.Position > 0 && !guest;
            myPosition.text = ranked ? "#" + LeaderboardService.Format(me.Position) : "-"; // il trattino lungo puo' mancare nell'atlante del font
            myValue.text = ranked ? LeaderboardService.Format(me.Value) : "";
            myNote.text = guest ? "Registrati per entrare in classifica" : me == null ? "" : LeaderboardService.Note(me.Position, me.Value, tenth ?? -1);
        }

        private static string MyId()
        {
            var auth = AuthBootstrapper.Instance;
            return auth != null && auth.PlayFabAuth != null ? auth.PlayFabAuth.PlayFabId : null;
        }
    }
}
