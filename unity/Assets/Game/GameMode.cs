#nullable enable
using System;
using BlockPuzzle.Core;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// What a game screen plays, and where its game lives in the save data: Classic's endless games, or one Daily
    /// attempt. GameController asks it for games, tells it about moves and game over, and writes the file itself.
    /// </summary>
    public abstract class GameMode
    {
        /// <summary>The saved game in progress, or null when there is none that resumes.</summary>
        public abstract GameSession? Resume();

        /// <summary>A new game: Classic's next one, or today's Daily.</summary>
        public abstract GameSession StartNew();

        /// <summary>Records a game in progress, after a move.</summary>
        public abstract void InProgress(GameSession game);

        /// <summary>Records a game that has just ended.</summary>
        public abstract void Finished(GameSession game);

        /// <summary>The best score so far, not counting the game on screen until it is recorded.</summary>
        public abstract int Best { get; }

        /// <summary>The streak game over shows; 0 shows none.</summary>
        public virtual int Streak => 0;

        public virtual string Title => Strings.Get("game_over.title");

        public virtual bool CanPlayAgain => true;

        public virtual bool CanShare => false;

        /// <summary>Whether game over may offer a rewarded revive: Classic only, as the Daily is one fair attempt.</summary>
        public virtual bool CanRevive => false;

        /// <summary>Whether the game Finished last recorded beat the best score from before it started.</summary>
        public virtual bool FinishedWithNewBest => false;

        /// <summary>The analytics "mode" (classic or daily); empty for modes that log no game events, like the tutorial.</summary>
        public virtual string AnalyticsName => "";

        /// <summary>What Share posts for a finished game.</summary>
        public virtual string ShareText(GameSession finished) => "";

        /// <summary>The move the player is guided to make next, as in the tutorial; null for free play.</summary>
        public virtual Guide? CurrentGuide => null;

        /// <summary>After a guided move: the next step's game, or null once the guided part is over and IsComplete.</summary>
        public virtual GameSession? NextStep(GameSession game) => null;

        /// <summary>Whether a guided mode has run out of steps, or was skipped.</summary>
        public virtual bool IsComplete => false;

        /// <summary>Ends a guided mode early.</summary>
        public virtual void Skip()
        {
        }

        protected static GameSession? ResumeSnapshot(GameSnapshot snapshot) =>
            ResumeSnapshot(snapshot, GeneratorSettings.Baseline(), new ScoringSettings());

        /// <summary>Resumes with the settings the game started with; damaged ones fall back to the baseline.</summary>
        protected static GameSession? ResumeSnapshot(GameSnapshot snapshot, GeneratorSettings? generator, ScoringSettings? scoring)
        {
            try
            {
                return GameSession.FromSnapshot(snapshot, PieceCatalog.Standard, Valid(generator) ?? GeneratorSettings.Baseline(),
                    Valid(scoring) ?? new ScoringSettings());
            }
            catch (InvalidSnapshotException)
            {
                return null;   // a save we can't resume: start a new game, keeping the counters
            }
        }

        static GeneratorSettings? Valid(GeneratorSettings? settings)
        {
            try
            {
                settings?.Validate();
                return settings;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        static ScoringSettings? Valid(ScoringSettings? settings)
        {
            try
            {
                settings?.Validate();
                return settings;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }

    /// <summary>A guided move: the piece in Slot may only land with its top-left on (Row, Col), and Hint says why.</summary>
    public readonly struct Guide
    {
        public readonly int Slot;
        public readonly int Row;
        public readonly int Col;
        public readonly string Hint;

        public Guide(int slot, int row, int col, string hint)
        {
            Slot = slot;
            Row = row;
            Col = col;
            Hint = hint;
        }
    }

    /// <summary>
    /// Classic: one game after another, a best score, and the counters the next game starts with. New games follow
    /// Remote Config's tuning; a game in progress keeps the settings it started with, which the save holds.
    /// </summary>
    public sealed class ClassicMode : GameMode
    {
        readonly SaveData _save;
        readonly Func<RemoteTuning> _tuning;

        /// <param name="tuning">The tuning as it stands when a new game starts; the bundled defaults if null.</param>
        public ClassicMode(SaveData save, Func<RemoteTuning>? tuning = null)
        {
            _save = save;
            _tuning = tuning ?? (() => RemoteTuning.Defaults);
            // A damaged save can hold impossible counters; the GameSession constructor would refuse them on every launch.
            save.NextGameIndex = Math.Max(0, save.NextGameIndex);
            save.NextRecentQuickLosses = Math.Max(0, save.NextRecentQuickLosses);
            save.BestScore = Math.Max(0, save.BestScore);
        }

        /// <summary>Whether a save holds a Classic game that resumes and isn't over: what Home offers as Continue.</summary>
        public static bool CanContinue(SaveData save)
        {
            if (!save.HasGame) return false;
            var game = ResumeSnapshot(save.Game, save.GameGenerator, save.GameScoring);
            return game != null && !game.IsGameOver;
        }

        // The best before this game started: InProgress raises the saved best as the game goes, so the comparison at the
        // end needs the value from before. A game resumed after setting a best mid-way doesn't count again.
        int _bestBeforeGame;
        bool _finishedWithNewBest;

        public override GameSession? Resume()
        {
            _bestBeforeGame = _save.BestScore;
            return _save.HasGame ? ResumeSnapshot(_save.Game, _save.GameGenerator, _save.GameScoring) : null;
        }

        public override GameSession StartNew()
        {
            _bestBeforeGame = _save.BestScore;
            var tuning = _tuning();
            _save.GameGenerator = tuning.Generator.Clone();
            _save.GameScoring = tuning.Scoring.Clone();
            return new GameSession(PieceCatalog.Standard, tuning.Generator, tuning.Scoring, SeedSource.NewSeed(),
                _save.NextGameIndex, _save.NextRecentQuickLosses);
        }

        public override bool FinishedWithNewBest => _finishedWithNewBest;

        public override void InProgress(GameSession game)
        {
            if (game.Score > _save.BestScore) _save.BestScore = game.Score;
            _save.HasGame = true;
            _save.Game = game.ToSnapshot();
        }

        public override void Finished(GameSession game)
        {
            _finishedWithNewBest = game.Score > _bestBeforeGame;
            if (game.Score > _save.BestScore) _save.BestScore = game.Score;
            _save.HasGame = false;
            _save.NextGameIndex = game.GameIndex + 1;
            _save.NextRecentQuickLosses = game.NextRecentQuickLosses;
        }

        public override int Best => _save.BestScore;

        public override bool CanRevive => true;

        public override string AnalyticsName => "classic";
    }

    public enum DailyState
    {
        Start,
        Continue,
        Done,
    }

    /// <summary>
    /// The Daily: today's deal, one scored attempt, and the streak. Only an attempt from today resumes; one left from an
    /// earlier day is dropped without a score.
    /// </summary>
    public sealed class DailyMode : GameMode
    {
        readonly DailySave _daily;
        readonly int _today;
        readonly string _product;

        public DailyMode(SaveData save, int today, string product)
        {
            _daily = save.Daily;
            _today = today;
            _product = product;
        }

        /// <summary>What Home offers today. Done also covers a clock set back before the last day played.</summary>
        public static DailyState StateOn(SaveData save, int today)
        {
            var daily = save.Daily;
            if (daily.HasGame && daily.GameDay == today) return DailyState.Continue;
            return daily.Record.CanStart(today) ? DailyState.Start : DailyState.Done;
        }

        public override GameSession? Resume() => _daily.HasGame && _daily.GameDay == _today ? ResumeSnapshot(_daily.Game) : null;

        public override GameSession StartNew() => DailyPuzzle.NewGame(_today);

        public override void InProgress(GameSession game)
        {
            _daily.HasGame = true;
            _daily.GameDay = _today;
            _daily.Game = game.ToSnapshot();
        }

        public override void Finished(GameSession game)
        {
            _daily.HasGame = false;
            _daily.Record.Finish(_today, game.Score);
        }

        public override int Best => _daily.Record.BestScore;

        public override int Streak => _daily.Record.StreakOn(_today);

        public override string Title => Strings.Get("game_over.daily_title");

        public override bool CanPlayAgain => false;

        public override bool CanShare => true;

        public override string AnalyticsName => "daily";

        public override string ShareText(GameSession finished) => DailyShare.Text(_product, _today, finished.Score, Streak);
    }
}
