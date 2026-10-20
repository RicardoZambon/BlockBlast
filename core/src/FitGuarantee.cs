namespace BlockPuzzle.Core
{
    /// <summary>What the generator promises about a freshly dealt tray.</summary>
    public enum FitGuarantee
    {
        /// <summary>No promise: a fresh tray may end the game.</summary>
        None = 0,
        /// <summary>At least one of the three pieces fits somewhere.</summary>
        AtLeastOne = 1,
        /// <summary>All three can be placed in some order, counting line clears in between.</summary>
        WholeTray = 2,
    }
}
