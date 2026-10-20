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

        public virtual string Title => "Game over";

        public virtual bool CanPlayAgain => true;

        public virtual bool CanShare => false;

        /// <summary>What Share posts for a finished game.</summary>
        public virtual string ShareText(GameSession finished) => "";

        protected static GameSession? ResumeSnapshot(GameSnapshot snapshot)
        {
            try
            {
                return GameSession.FromSnapshot(snapshot, PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings());
            }
            catch (InvalidSnapshotException)
            {
                return null;   // a save we can't resume: start a new game, keeping the counters
            }
        }
    }

    /// <summary>Classic: one game after another, a best score, and the counters the next game starts with.</summary>
    public sealed class ClassicMode : GameMode
    {
        readonly SaveData _save;

        public ClassicMode(SaveData save)
        {
            _save = save;
            // A damaged save can hold impossible counters; the GameSession constructor would refuse them on every launch.
            save.NextGameIndex = Math.Max(0, save.NextGameIndex);
            save.NextRecentQuickLosses = Math.Max(0, save.NextRecentQuickLosses);
            save.BestScore = Math.Max(0, save.BestScore);
        }

        /// <summary>Whether a save holds a Classic game that resumes and isn't over: what Home offers as Continue.</summary>
        public static bool CanContinue(SaveData save)
        {
            if (!save.HasGame) return false;
            var game = ResumeSnapshot(save.Game);
            return game != null && !game.IsGameOver;
        }

        public override GameSession? Resume() => _save.HasGame ? ResumeSnapshot(_save.Game) : null;

        public override GameSession StartNew() =>
            new GameSession(PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings(), SeedSource.NewSeed(),
                _save.NextGameIndex, _save.NextRecentQuickLosses);

        public override void InProgress(GameSession game)
        {
            if (game.Score > _save.BestScore) _save.BestScore = game.Score;
            _save.HasGame = true;
            _save.Game = game.ToSnapshot();
        }

        public override void Finished(GameSession game)
        {
            if (game.Score > _save.BestScore) _save.BestScore = game.Score;
            _save.HasGame = false;
            _save.NextGameIndex = game.GameIndex + 1;
            _save.NextRecentQuickLosses = game.NextRecentQuickLosses;
        }

        public override int Best => _save.BestScore;
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

        public override string Title => "Daily done";

        public override bool CanPlayAgain => false;

        public override bool CanShare => true;

        public override string ShareText(GameSession finished) => DailyShare.Text(_product, _today, finished.Score, Streak);
    }
}
