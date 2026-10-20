namespace BlockPuzzle.Core
{
    /// <summary>What the generator promises about a freshly dealt tray.</summary>
    public enum FitGuarantee
    {
        /// <summary>No promise: a fresh tray may end the game.</summary>
        None = 0,
        /// <summary>At least one of the three pieces fits somewhere.</summary>
        AtLeastOne = 1,
        /// <summary>
        /// Re-deals until all three can be placed in some order (counting line clears in between), up to
        /// GeneratorSettings.MaxWholeTrayAttempts times. After that it falls back to AtLeastOne, so a very
        /// small share of fresh trays can still end a game.
        /// </summary>
        WholeTray = 2,
    }
}
