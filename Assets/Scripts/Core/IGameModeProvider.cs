namespace Project51.Core
{
    public interface IGameModeProvider
    {
        bool IsMultiplayer { get; }

        bool IsLocalPlayer(int playerIndex);

        bool IsHumanPlayer(int playerIndex);

        bool IsBotPlayer(int playerIndex);

        bool IsMasterClient { get; }

        int LocalPlayerIndex { get; }
    }
}
