using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using Project51.Auth;
using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Impostazioni > Privacy e sociale > Giocatori bloccati (scelta dell'utente 02/10): chi hai bloccato dal profilo rapido, con Sblocca.
    /// La lista salva solo gli id (BlockList): i nomi li chiede a PlayFab (il nome visibile e' leggibile dai client di serie).
    /// Pagina a tutto schermo sopra alle Impostazioni, costruita da UI51MetaBuilder.BuildBlocked.
    /// </summary>
    public sealed class UI51BlockedView : MonoBehaviour
    {
        [SerializeField] private Button back;
        [SerializeField] private RectTransform rowTemplate; // spento: Row/Texts/Title (nome) e Unblock (pulsante)
        [SerializeField] private GameObject empty;
        [SerializeField] private ScrollRect scroll;

        private readonly List<GameObject> rows = new List<GameObject>();

        private void Awake() => back.onClick.AddListener(Close);

        public void Open()
        {
            gameObject.SetActive(true);
            foreach (var row in rows) Destroy(row);
            rows.Clear();
            foreach (string id in BlockList.All()) AddRow(id);
            empty.SetActive(rows.Count == 0);
            scroll.verticalNormalizedPosition = 1f;
            UIAnim.FadeIn((RectTransform)transform, 0.2f);
        }

        public void Close() => gameObject.SetActive(false);

        private void AddRow(string id)
        {
            var row = Instantiate(rowTemplate, rowTemplate.parent);
            row.gameObject.SetActive(true);
            rows.Add(row.gameObject);
            var label = row.Find("Row/Texts/Title").GetComponent<TMP_Text>();
            label.text = BlockList.KnownName(id) ?? "Giocatore";
            PlayFabClientAPI.GetPlayerProfile(new GetPlayerProfileRequest
                {
                    PlayFabId = id,
                    ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true }
                },
                r =>
                {
                    string name = r.PlayerProfile?.DisplayName;
                    if (string.IsNullOrEmpty(name)) return;
                    BlockList.RememberName(id, name);
                    if (label != null) label.text = name;
                },
                _ => { });
            row.Find("Row/Unblock").GetComponent<Button>().onClick.AddListener(() =>
            {
                if (!BlockList.SetBlocked(id, false)) return;
                rows.Remove(row.gameObject);
                Destroy(row.gameObject);
                empty.SetActive(rows.Count == 0);
                UI51Toast.Show("Hai sbloccato " + label.text);
            });
        }
    }
}
