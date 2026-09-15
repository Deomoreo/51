using System.Collections.Generic;

namespace Project51.UIV2.Data
{
    /// <summary>
    /// Contratto dati mock per HomeScreenV2. Player/Resources sono di competenza di
    /// UIV2_TopBar (HomeScreenV2 non ricostruisce un secondo TopBar) - viaggiano insieme qui
    /// solo perche' la futura integrazione produce un unico HomeViewData da un adapter e lo
    /// smista (TopBar / HomeScreenV2 / BottomNav): il chiamante instrada i campi giusti a
    /// ciascuno, HomeScreenV2.Bind() legge solo Selected*/BadgeCount.
    /// </summary>
    public class HomeViewData
    {
        public PlayerSummaryViewData Player;
        public List<ResourceViewData> Resources;
        public SelectorOptionViewData SelectedMode;
        public SelectorOptionViewData SelectedDeck;
        public int RewardsBadgeCount;
        public int RankingBadgeCount;
        public int MailBadgeCount;
    }
}
