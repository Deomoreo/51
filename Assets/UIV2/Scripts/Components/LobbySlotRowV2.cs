using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Riga di un posto al tavolo in sala d'attesa / ricerca partita: occupata (medaglione, nome,
    /// ruolo colorato, spunta) oppure libera (bordo tratteggiato, cerchio vuoto, +BOT per l'host).
    /// </summary>
    public sealed class LobbySlotRowV2 : MonoBehaviour
    {
        public GameObject Filled;
        public GameObject Empty;
        public Image Avatar;
        public TMP_Text Name;
        public TMP_Text Role;
        public GameObject Check;
        public TMP_Text EmptyLabel;
        public Button BotButton;
        public TMP_Text BotButtonLabel;
        public Sprite PlayerAvatar;
        public Sprite BotAvatar;

        public static readonly Color HostColor = new Color32(242, 184, 64, 255);
        public static readonly Color ReadyColor = new Color32(61, 214, 140, 255);
        public static readonly Color BotColor = new Color32(120, 170, 235, 255);

        public void ShowPlayer(string playerName, string role, Color roleColor, bool isBot = false, bool canRemoveBot = false)
        {
            gameObject.SetActive(true);
            Filled.SetActive(true);
            Empty.SetActive(false);
            Name.text = playerName;
            Role.text = role;
            Role.color = roleColor;
            if (Avatar != null) Avatar.sprite = isBot && BotAvatar != null ? BotAvatar : PlayerAvatar;
            if (Check != null) Check.SetActive(!canRemoveBot);
            SetBotButton(canRemoveBot, "− BOT");
        }

        public void ShowEmpty(string label, bool canAddBot)
        {
            gameObject.SetActive(true);
            Filled.SetActive(false);
            Empty.SetActive(true);
            EmptyLabel.text = label;
            SetBotButton(canAddBot, "+ BOT");
        }

        public void Hide() => gameObject.SetActive(false);

        private void SetBotButton(bool visible, string label)
        {
            if (BotButton == null) return;
            BotButton.gameObject.SetActive(visible);
            if (BotButtonLabel != null) BotButtonLabel.text = label;
        }
    }
}
