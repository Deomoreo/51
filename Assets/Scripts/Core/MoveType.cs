namespace Project51.Core
{
    /// <summary>
    /// Types of moves a player can make.
    /// </summary>
    public enum MoveType
    {
        /// <summary>
        /// Play a card without capturing anything (forced discard).
        /// </summary>
        PlayOnly,

        /// <summary>
        /// Capture exactly one card of equal value.
        /// </summary>
        CaptureEqual,

        /// <summary>
        /// Capture multiple cards whose sum equals the played card value.
        /// </summary>
        CaptureSum,

        /// <summary>
        /// Capture cards whose sum + played card value = 15.
        /// </summary>
        Capture15,

        /// <summary>
        /// Special Ace capture rule.
        /// </summary>
        AceCapture
    }
}
