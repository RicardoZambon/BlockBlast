#nullable enable
using System;
using BlockPuzzle.Core;

namespace BlockPuzzle.Game
{
    /// <summary>The Daily's part of the save: the attempt in progress, if there is one, and the record of finished days.</summary>
    [Serializable]
    public sealed class DailySave
    {
        /// <summary>Whether Game is an attempt in progress.</summary>
        public bool HasGame;

        /// <summary>The day the attempt in progress belongs to, as a DailyPuzzle.DayNumber.</summary>
        public int GameDay = -1;

        public GameSnapshot Game = new GameSnapshot();

        public DailyRecord Record = new DailyRecord();
    }
}
