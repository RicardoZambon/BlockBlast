#nullable enable
using System;
using BlockPuzzle.Core;

namespace BlockPuzzle.Game
{
    /// <summary>What the save file holds: the game in progress, if there is one, and what the next game starts with.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;

        /// <summary>Whether Game is a game in progress. JsonUtility never writes null, so Game alone can't say.</summary>
        public bool HasGame;

        public GameSnapshot Game = new GameSnapshot();

        /// <summary>The next game's index: how many games this player started before it.</summary>
        public int NextGameIndex;

        /// <summary>The next game's quick losses in a row, from GameSession.NextRecentQuickLosses.</summary>
        public int NextRecentQuickLosses;

        public int BestScore;

        /// <summary>Whether the first-time tutorial was finished or skipped.</summary>
        public bool TutorialDone;

        /// <summary>The day the app last asked for a rating, as a DailyPuzzle.DayNumber; −1 if never.</summary>
        public int RatingAskedDay = -1;

        /// <summary>Whether the player was asked about Daily reminders (only once), and whether they said yes.</summary>
        public bool RemindersAsked;

        public bool RemindersOn;

        /// <summary>The Daily Puzzle, kept apart from Classic: a save from before the Daily loads with an empty one.</summary>
        public DailySave Daily = new DailySave();
    }
}
