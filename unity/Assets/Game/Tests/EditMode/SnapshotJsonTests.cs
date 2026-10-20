#nullable enable
using System.Linq;
using BlockPuzzle.Core;
using NUnit.Framework;
using UnityEngine;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>Saves go through Unity's JsonUtility, so it must keep every snapshot field exact, 64-bit ones included.</summary>
    public class SnapshotJsonTests
    {
        [Test]
        public void JsonUtility_round_trips_64_bit_fields_exactly()
        {
            var snapshot = new GameSnapshot
            {
                Board = 0x8000_0000_0000_0001UL,
                Tray = new[] { "o3", "", "i5h" },
                Score = 12345,
                Streak = 2,
                MissesSinceClear = 1,
                MaxStreak = 4,
                Placements = 30,
                LinesCleared = 9,
                RngState = 0xFEDC_BA98_7654_3210UL,
                RngIncrement = 0xFFFF_FFFF_FFFF_FFFFUL,
                GameIndex = 7,
                RecentQuickLosses = 1,
                Revived = true,
            };

            var copy = JsonUtility.FromJson<GameSnapshot>(JsonUtility.ToJson(snapshot));

            Assert.AreEqual(snapshot.Version, copy.Version);
            Assert.AreEqual(snapshot.Board, copy.Board);
            CollectionAssert.AreEqual(snapshot.Tray, copy.Tray);
            Assert.AreEqual(snapshot.Score, copy.Score);
            Assert.AreEqual(snapshot.Streak, copy.Streak);
            Assert.AreEqual(snapshot.MissesSinceClear, copy.MissesSinceClear);
            Assert.AreEqual(snapshot.MaxStreak, copy.MaxStreak);
            Assert.AreEqual(snapshot.Placements, copy.Placements);
            Assert.AreEqual(snapshot.LinesCleared, copy.LinesCleared);
            Assert.AreEqual(snapshot.RngState, copy.RngState);
            Assert.AreEqual(snapshot.RngIncrement, copy.RngIncrement);
            Assert.AreEqual(snapshot.GameIndex, copy.GameIndex);
            Assert.AreEqual(snapshot.RecentQuickLosses, copy.RecentQuickLosses);
            Assert.AreEqual(snapshot.Revived, copy.Revived);
        }

        [Test]
        public void A_game_resumed_from_JsonUtility_deals_the_same_pieces()
        {
            var original = new GameSession(PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings(), seed: 42);
            for (int i = 0; i < 10 && !original.IsGameOver; i++) EngineChecks.PlayFirstFitMove(original);

            string json = JsonUtility.ToJson(original.ToSnapshot());
            var restored = GameSession.FromSnapshot(JsonUtility.FromJson<GameSnapshot>(json), PieceCatalog.Standard,
                GeneratorSettings.Baseline(), new ScoringSettings());

            for (int i = 0; i < 40 && !original.IsGameOver; i++)
            {
                Assert.AreEqual(TrayIds(original), TrayIds(restored));
                EngineChecks.PlayFirstFitMove(original);
                EngineChecks.PlayFirstFitMove(restored);
                Assert.AreEqual(original.Board, restored.Board);
                Assert.AreEqual(original.Score, restored.Score);
            }
            Assert.AreEqual(TrayIds(original), TrayIds(restored));
        }

        static string TrayIds(GameSession game) => string.Join(",", game.Tray.ToArray().Select(p => p?.Id ?? "-"));
    }
}
