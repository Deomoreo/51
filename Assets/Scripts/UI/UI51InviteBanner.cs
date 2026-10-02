using System;
using DG.Tweening;
using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Invito ricevuto (mockup InvitoRicevuto): banner in alto che scende, avatar e nome di chi invita, secondi rimasti e barra
    /// che si accorcia, Rifiuta / ACCETTA. Dopo 20 s sparisce da solo. Costruito da UI51SocialBuilder, usato da UI51FriendsView.
    /// </summary>
    public sealed class UI51InviteBanner : MonoBehaviour
    {
        public const float Seconds = 20f;

        [SerializeField] private RectTransform panel;
        [SerializeField] private AvatarFrame avatar;
        [SerializeField] private TMP_Text title, subtitle, countdown;
        [SerializeField] private RectTransform fill;
        [SerializeField] private Button accept, decline;

        private Action onAccept;
        private float endsAt;

        public bool IsShown { get; private set; }

        private void Awake()
        {
            accept.onClick.AddListener(Accept);
            decline.onClick.AddListener(Hide);
            panel.gameObject.SetActive(false);
        }

        /// <summary>Mostra (o sostituisce) l'invito e riparte da 20 s.</summary>
        public void Show(string titleText, string subtitleText, Sprite portrait, Action onAccepted)
        {
            onAccept = onAccepted;
            title.text = titleText;
            subtitle.text = subtitleText;
            avatar.SetAvatar(portrait);
            endsAt = Time.unscaledTime + Seconds;
            Tick();
            bool wasShown = IsShown;
            IsShown = true;
            panel.gameObject.SetActive(true);
            // slideDown: translateY(-120%) -> 0 in .4 s, cubic(.2,.8,.3,1).
            if (!wasShown) new UIKeyframes(0.4f, UIEase.Sheet).Track(AnimProp.Y, 0f, -1.2f * panel.rect.height, 1f, 0f).Play(panel);
        }

        public void Hide()
        {
            if (!IsShown) return;
            IsShown = false;
            onAccept = null;
            new UIKeyframes(0.25f, UIEase.EaseIn).Track(AnimProp.Y, 0f, 0f, 1f, -1.2f * panel.rect.height).Play(panel)
                ?.OnComplete(() => panel.gameObject.SetActive(false));
        }

        private void Accept()
        {
            var action = onAccept;
            Hide();
            action?.Invoke();
        }

        private void Update()
        {
            if (!IsShown) return;
            if (Time.unscaledTime >= endsAt) { Hide(); return; }
            Tick();
        }

        private void Tick()
        {
            float left = Mathf.Max(0f, endsAt - Time.unscaledTime);
            countdown.text = Mathf.CeilToInt(left) + "s";
            fill.anchorMax = new Vector2(left / Seconds, 1f);
        }
    }
}
