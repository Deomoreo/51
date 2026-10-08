using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>Riferimenti di una riga di Amici, impostati da UI51SocialBuilder. Lo stato lo decide UI51FriendsView.</summary>
    public sealed class UI51FriendItem : MonoBehaviour
    {
        public AvatarFrame avatar;
        public UI51Shape dot;
        public TMP_Text title, level, status;
        public Button invite;
        /// <summary>Giro Android 08/10: tocco sulla riga (scheda dell'amico), Accetta e Rifiuta delle richieste ricevute.</summary>
        public Button open, accept, decline;
        public GameObject invited, busy;
        public TMP_Text busyLabel;
    }
}
