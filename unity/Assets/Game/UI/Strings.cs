#nullable enable
using System.Collections.Generic;
using System.Globalization;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// Every word the player reads, by key, in English for now. Task 2.8 adds Brazilian Portuguese, Latin-American
    /// Spanish and Indonesian behind the same keys, so no screen changes when it does. Keys never change; texts may.
    /// </summary>
    public static class Strings
    {
        static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            ["home.play"] = "Play",
            ["home.continue"] = "Continue",
            ["home.daily"] = "Daily",
            ["home.daily_streak"] = "Daily · {0}-day streak",
            ["home.daily_continue"] = "Daily · continue",
            ["home.daily_done"] = "Daily · done",
            ["home.settings"] = "Settings",
            ["common.home"] = "Home",
            ["pause.title"] = "Paused",
            ["pause.resume"] = "Resume",
            ["game_over.title"] = "Game over",
            ["game_over.daily_title"] = "Daily done",
            ["game_over.play_again"] = "Play again",
            ["game_over.share"] = "Share",
            ["game_over.scores"] = "Score {0}\nBest {1}",
            ["game_over.streak"] = "{0}-day streak",
            ["hud.best"] = "Best {0}",
            ["hud.streak"] = "Streak {0}",
            ["praise.great"] = "Great!",
            ["praise.amazing"] = "Amazing!",
            ["praise.incredible"] = "Incredible!",
            ["praise.board_clear"] = "Board clear!",
            ["praise.streak"] = "Streak {0}!",
            ["tutorial.drag"] = "Drag the piece onto the board",
            ["tutorial.row"] = "Fill a row to clear it",
            ["tutorial.column"] = "Columns clear too",
            ["tutorial.skip"] = "Skip",
            ["settings.title"] = "Settings",
            ["settings.sound"] = "Sound",
            ["settings.music"] = "Music",
            ["settings.haptics"] = "Haptics",
            ["settings.reduced_motion"] = "Reduced motion",
            ["settings.version"] = "Version {0}",
            ["settings.back"] = "Back",
            ["share.result"] = "{0} Daily {1}: {2} points",
            ["share.streak"] = ", {0}-day streak",
            ["share.chooser"] = "Share",
        };

        public static IEnumerable<string> Keys => English.Keys;

        /// <summary>The text for a key. A missing key shows itself, so a gap is visible on screen, never blank.</summary>
        public static string Get(string key) => English.TryGetValue(key, out var text) ? text : key;

        /// <summary>The text for a key with its {0}, {1}… filled in, numbers written the same way everywhere.</summary>
        public static string Format(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, Get(key), args);
    }
}
