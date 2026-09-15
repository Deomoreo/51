using TMPro;
using UnityEngine;
using Project51.UIV2.Data;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Riga giocatore riusabile per Lobby, Matchmaking, Classifica, fine mano/fine
    /// partita: stessa struttura (avatar + nome + valore a destra), cambia solo il testo
    /// trailing (pronto/punteggio/posizione) passato dal chiamante.
    /// </summary>
    public class UIV2PlayerRow : MonoBehaviour
    {
        [SerializeField] private UIV2AvatarBadge avatar;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text trailingLabel;
        [SerializeField] private GameObject currentPlayerHighlight;

        public void Bind(PlayerSummaryViewData player, string trailingText, bool highlight = false)
        {
            if (player == null) return;

            if (avatar != null) avatar.SetAvatar(player.Avatar, player.Level);
            if (nameLabel != null) nameLabel.text = player.DisplayName;
            if (trailingLabel != null) trailingLabel.text = trailingText;
            if (currentPlayerHighlight != null) currentPlayerHighlight.SetActive(highlight || player.IsLocalPlayer);
        }
    }
}
