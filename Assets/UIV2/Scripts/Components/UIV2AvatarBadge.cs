using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Components
{
    public class UIV2AvatarBadge : MonoBehaviour
    {
        [SerializeField] private Image avatarImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private GameObject levelBadgeRoot;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private GameObject onlineDot;

        private const float PlaceholderInsetMargin = 8f;

        public void SetAvatar(Sprite avatar, int level = -1, bool? online = null)
        {
            bool hasRealAvatar = avatar != null;

            if (avatarImage != null)
            {
                var avatarRect = avatarImage.rectTransform;
                avatarRect.anchorMin = Vector2.zero;
                avatarRect.anchorMax = Vector2.one;

                if (hasRealAvatar)
                {
                    avatarImage.sprite = avatar;
                    avatarRect.offsetMin = Vector2.zero;
                    avatarRect.offsetMax = Vector2.zero;
                }
                else
                {
                    avatarRect.offsetMin = new Vector2(PlaceholderInsetMargin, PlaceholderInsetMargin);
                    avatarRect.offsetMax = new Vector2(-PlaceholderInsetMargin, -PlaceholderInsetMargin);
                }
            }

            // I ritratti reali (avatar_01..08) includono gia' una propria cornice ornata cotta
            // nell'immagine: la cornice separata di questo componente andrebbe in doppione, quindi
            // disabilitiamo solo il rendering dell'Image (non l'intero GameObject - frameImage
            // vive sulla RADICE del badge, insieme ad Avatar/LevelBadge/OnlineDot come fratelli:
            // un SetActive(false) qui spegnerebbe anche loro).
            if (frameImage != null) frameImage.enabled = !hasRealAvatar;

            if (levelBadgeRoot != null)
            {
                bool showLevel = level >= 0;
                levelBadgeRoot.SetActive(showLevel);
                if (showLevel && levelLabel != null) levelLabel.text = level.ToString();
            }

            if (onlineDot != null && online.HasValue)
            {
                onlineDot.SetActive(online.Value);
            }
        }
    }
}
