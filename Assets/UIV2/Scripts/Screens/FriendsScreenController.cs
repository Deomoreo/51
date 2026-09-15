using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Riferimento per il pattern data-driven richiesto per Amici/Classifica/Posta/Premi/
    /// Negozio/Collezione/Lobby/Matchmaking/fine mano/fine partita: nessun nome amico e' nel
    /// prefab, tutto arriva da Populate(). Non esiste ancora un FriendsManager reale nel
    /// progetto: il chiamante costruisce la lista (mock o reale) fuori da questo script, che
    /// resta ignaro della fonte dei dati.
    /// </summary>
    public class FriendsScreenController : MonoBehaviour
    {
        [SerializeField] private Transform onlineListContainer;
        [SerializeField] private Transform offlineListContainer;
        [SerializeField] private FriendRowView friendRowPrefab;

        private readonly List<FriendRowView> _spawned = new List<FriendRowView>();

        public void Populate(IReadOnlyList<FriendViewData> friends)
        {
            ClearSpawned();

            if (friends == null || friendRowPrefab == null) return;

            foreach (var friend in friends.Where(f => f.Status != FriendOnlineStatus.Offline))
            {
                SpawnRow(friend, onlineListContainer);
            }

            foreach (var friend in friends.Where(f => f.Status == FriendOnlineStatus.Offline))
            {
                SpawnRow(friend, offlineListContainer);
            }
        }

        private void SpawnRow(FriendViewData data, Transform parent)
        {
            if (parent == null) return;
            var row = Instantiate(friendRowPrefab, parent);
            row.Bind(data);
            _spawned.Add(row);
        }

        private void ClearSpawned()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
            }
            _spawned.Clear();
        }
    }
}
