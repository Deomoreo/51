using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Riferimenti di una riga della Posta (o della testata del messaggio aperto, che usa solo tessera, icona e testi).
    /// I valori li mette UI51MailView. Costruita da UI51SocialBuilder.
    /// </summary>
    public sealed class UI51MailItem : MonoBehaviour
    {
        public Button button;
        public UI51Shape tile;
        public Image icon;
        public GameObject unread;
        public TMP_Text title, time, snippet, expiry;
        public CanvasGroup chipsGroup;
        public GameObject[] chips = new GameObject[0];
        public Image[] chipIcons = new Image[0];
        public TMP_Text[] chipLabels = new TMP_Text[0];
        public GameObject claimed;
    }
}
