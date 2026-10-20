#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// A saved game can't be resumed: it is corrupt, from another snapshot version, or names a piece
    /// the catalog doesn't have. Catch this to start a new game. Any other exception from
    /// GameSession.FromSnapshot means the settings or catalog passed in are wrong, not the save.
    /// </summary>
    public sealed class InvalidSnapshotException : Exception
    {
        public InvalidSnapshotException(string message) : base(message) { }
    }
}
