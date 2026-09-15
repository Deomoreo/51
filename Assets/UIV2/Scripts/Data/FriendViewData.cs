namespace Project51.UIV2.Data
{
    public enum FriendOnlineStatus
    {
        Offline,
        Online,
        InMatch
    }

    public class FriendViewData
    {
        public PlayerSummaryViewData Player;
        public FriendOnlineStatus Status;
        public string StatusText;
        public bool CanInvite;
    }
}
