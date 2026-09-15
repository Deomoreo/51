using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Data;
using Project51.UIV2.Components;

namespace Project51.UIV2.Screens
{
    public class FriendRowView : MonoBehaviour
    {
        [SerializeField] private UIV2AvatarBadge avatar;
        [SerializeField] private TMP_Text playerNameLabel;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Image statusIndicator;
        [SerializeField] private UIV2Button inviteButton;
        [SerializeField] private Color onlineColor = new Color(0.25f, 0.85f, 0.45f);
        [SerializeField] private Color offlineColor = new Color(0.5f, 0.5f, 0.5f);

        public void Bind(FriendViewData data)
        {
            if (data?.Player == null) return;

            if (avatar != null)
                avatar.SetAvatar(data.Player.Avatar, data.Player.Level, data.Status != FriendOnlineStatus.Offline);
            if (playerNameLabel != null) playerNameLabel.text = data.Player.DisplayName;
            if (statusText != null) statusText.text = data.StatusText;
            if (statusIndicator != null)
                statusIndicator.color = data.Status == FriendOnlineStatus.Offline ? offlineColor : onlineColor;

            if (inviteButton != null) inviteButton.gameObject.SetActive(data.CanInvite);
        }
    }
}
