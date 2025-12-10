namespace Project51.Core
{
    public static class GameModeService
    {
        private static IGameModeProvider _provider;

        /// <summary>
        /// The current game mode provider. Returns a default single-player provider if none is set.
        /// </summary>
        public static IGameModeProvider Current
        {
            get => _provider ?? SinglePlayerProvider.Instance;
            set => _provider = value;
        }

        /// <summary>
        /// Reset the provider (useful for tests or scene changes).
        /// </summary>
        public static void Reset()
        {
            _provider = null;
        }
    }

    /// <summary>
    /// Default single-player implementation of IGameModeProvider.
    /// Used when no multiplayer GameManager is present.
    /// </summary>
    public class SinglePlayerProvider : IGameModeProvider
    {
        public static readonly SinglePlayerProvider Instance = new SinglePlayerProvider();

        private SinglePlayerProvider() { }

        public bool IsMultiplayer => false;
        public bool IsMasterClient => true;
        public int LocalPlayerIndex => 0;

        public bool IsLocalPlayer(int playerIndex) => playerIndex == 0;
        public bool IsHumanPlayer(int playerIndex) => playerIndex == 0;
        public bool IsBotPlayer(int playerIndex) => playerIndex != 0;
    }
}
