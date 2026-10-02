using System.Collections.Generic;
using Project51.Auth;
using Project51.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 15: sezione TROFEI del Profilo (mockup Profilo): "N / 15 · Vedi tutti" apre la pagina Trofei, le ultime 4 medaglie
    /// ottenute (Trophies.All va dal piu' facile) e IN CORSO i prossimi 2. Solo account (sta nel gruppo Account). Grafica: UI51ProgressBuilder.
    /// </summary>
    public sealed class UI51TrophySummary : MonoBehaviour
    {
        [SerializeField] private Button link;
        [SerializeField] private TMP_Text linkLabel, empty;
        [SerializeField] private GameObject[] slots = new GameObject[0]; // Medal (Image) e Name
        [SerializeField] private GameObject[] rows = new GameObject[0];  // Name, Value, Track/Fill
        [SerializeField] private GameObject inProgress;
        [SerializeField] private Sprite[] medals = new Sprite[0];
        [SerializeField] private UI51TrophiesView page;

        private ProfileService profile;

        private void Awake() => link.onClick.AddListener(page.Open);

        private void OnEnable()
        {
            profile = AuthBootstrapper.Instance != null ? AuthBootstrapper.Instance.Profile : null;
            if (profile != null) { profile.OnProfileLoaded += Bind; profile.OnProfileUpdated += Bind; }
            Bind();
        }

        private void OnDisable()
        {
            if (profile != null) { profile.OnProfileLoaded -= Bind; profile.OnProfileUpdated -= Bind; }
        }

        private void Bind()
        {
            if (this == null) return;
            int games = profile != null ? profile.TotalGames : 0, wins = profile != null ? profile.Wins : 0;
            int scope = profile != null ? profile.TotalScope : 0, level = profile != null ? profile.Level : 1;
            var all = Trophies.All;
            linkLabel.text = Trophies.CountEarned(games, wins, scope, level) + " / " + all.Length + " · Vedi tutti";

            var shown = new List<Trophy>();
            for (int i = all.Length - 1; i >= 0 && shown.Count < slots.Length; i--)
                if (Trophies.Earned(all[i], games, wins, scope, level)) shown.Insert(0, all[i]);
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].SetActive(i < shown.Count);
                if (i < shown.Count) Fill(slots[i], shown[i]);
            }
            empty.gameObject.SetActive(shown.Count == 0);

            int row = 0;
            foreach (var r in rows) r.SetActive(false);
            foreach (var t in all)
            {
                if (row >= rows.Length) break;
                if (Trophies.Earned(t, games, wins, scope, level)) continue;
                int value = Trophies.Progress(t, games, wins, scope, level);
                var r = rows[row++].transform;
                r.gameObject.SetActive(true);
                r.Find("Name").GetComponent<TMP_Text>().text = t.Name + " <size=11><color=#F5E9D080>· " + t.Description.ToLowerInvariant() + "</color></size>";
                r.Find("Value").GetComponent<TMP_Text>().text = value + "/" + t.Target;
                var fill = (RectTransform)r.Find("Track/Fill");
                var max = fill.anchorMax; max.x = (float)value / t.Target; fill.anchorMax = max;
            }
            inProgress.SetActive(row > 0);
        }

        private void Fill(GameObject slotGo, Trophy t)
        {
            slotGo.SetActive(true);
            slotGo.transform.Find("Medal").GetComponent<Image>().sprite = t.Medal < medals.Length ? medals[t.Medal] : null;
            slotGo.transform.Find("Name").GetComponent<TMP_Text>().text = t.Name;
        }
    }
}
