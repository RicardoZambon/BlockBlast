#nullable enable
using BlockPuzzle.Core;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// The first-time tutorial: three guided moves on one board, then Classic. Each step has one piece, and it may only
    /// land on its target, so every step's board is exactly what the player just made: a square anywhere, then a bar
    /// that completes row 7, then one that completes column 7. Nothing is scored or saved until it is done.
    /// </summary>
    public sealed class TutorialMode : GameMode
    {
        const int Slot = 1;   // the middle of the tray

        static readonly (string piece, int row, int col, string hint)[] Steps =
        {
            ("o2", 2, 2, "tutorial.drag"),
            ("i3h", 7, 5, "tutorial.row"),
            ("i3v", 5, 7, "tutorial.column"),
        };

        static readonly Board Start = Board.Parse(
            ".......#",
            ".......#",
            ".......#",
            ".......#",
            ".......#",
            "........",
            "........",
            "#####...");

        readonly SaveData _save;
        int _step;

        public TutorialMode(SaveData save)
        {
            _save = save;
        }

        /// <summary>Only for a new player: no tutorial done, and no Classic or Daily progress, as in a save from an older version.</summary>
        public static bool ShouldShow(SaveData save) =>
            !save.TutorialDone && !save.HasGame && save.NextGameIndex == 0 && save.BestScore == 0 &&
            !save.Daily.HasGame && save.Daily.Record.LastDay < 0;

        public override Guide? CurrentGuide =>
            IsComplete ? (Guide?)null : new Guide(Slot, Steps[_step].row, Steps[_step].col, Strings.Get(Steps[_step].hint));

        public override bool IsComplete => _step >= Steps.Length;

        public override GameSession? Resume() => null;   // it always starts over

        public override GameSession StartNew()
        {
            _step = 0;
            return Position(Start, Steps[0].piece);
        }

        public override GameSession? NextStep(GameSession game)
        {
            _step++;
            if (IsComplete)
            {
                _save.TutorialDone = true;
                return null;
            }
            return Position(game.Board, Steps[_step].piece);   // the engine already dealt a new tray; this replaces it
        }

        public override void Skip()
        {
            _step = Steps.Length;
            _save.TutorialDone = true;
        }

        public override void InProgress(GameSession game)
        {
        }

        public override void Finished(GameSession game)
        {
        }

        public override int Best => 0;

        public override bool CanPlayAgain => false;

        /// <summary>A game on this board with one piece in the middle slot.</summary>
        static GameSession Position(Board board, string piece)
        {
            var rng = new Pcg32(1);
            var snapshot = new GameSnapshot
            {
                Board = board.Cells,
                Tray = new[] { "", piece, "" },
                RngState = rng.State,
                RngIncrement = rng.Increment,
            };
            return GameSession.FromSnapshot(snapshot, PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings());
        }
    }
}
