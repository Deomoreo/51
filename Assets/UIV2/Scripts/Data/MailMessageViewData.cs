using System;

namespace Project51.UIV2.Data
{
    public class MailMessageViewData
    {
        public string Id;
        public string Sender;
        public string Subject;
        public string Body;
        public DateTime ReceivedAt;
        public bool IsRead;
        public RewardViewData AttachedReward;
        public bool Claimed;
    }
}
