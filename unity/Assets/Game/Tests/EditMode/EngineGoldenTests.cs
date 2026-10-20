#nullable enable
using System.Linq;
using BlockPuzzle.Core;
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>
    /// The engine's golden values, copied verbatim from core/dotnet/BlockPuzzle.Core.Tests/DeterminismTests.cs,
    /// checked again inside Unity: a seed must deal the same pieces in Unity's runtime as in .NET.
    /// </summary>
    public class EngineGoldenTests
    {
        const ulong Seed = 20261020;

        /// <summary>From DailyPuzzleTests: every phone must deal 2026-10-20's Daily the same way.</summary>
        [Test]
        public void A_daily_deals_the_same_first_tray_as_dotnet()
        {
            const int October20 = 20746;

            var game = DailyPuzzle.NewGame(October20);

            Assert.AreEqual(4023608590332759023UL, DailyPuzzle.Seed(October20));
            Assert.AreEqual("c3d,t4a,c3a", TestGames.TrayIds(game));
        }

        [TestCase("gentle", "i2h,i2h,t4a c3c,i2v,i3v c3b,t4d,c3a i3h,i2h,i3v s4v,o3,l4d j4b,c5c,l4d j4a,l4d,l4c c3d,i3h,z4v")]
        [TestCase("baseline", "i2h,i2h,t4a o2,i3v,i2v i4h,o2,t4b l4a,j4a,i3h i4v,o3,c3c c3c,i5h,i3h i2v,c3d,s4h i3h,i4h,c3a")]
        [TestCase("hard", "i2v,l4b,i2h dot,j4a,c3c r23,s4h,t4b c3b,o3,j4b i3h,t4b,i3v o3,o3,c5a j4b,c5c,i5h c5c,c5a,c5a")]
        public void A_seed_always_deals_the_same_trays(string profile, string expected)
        {
            var generator = new PieceGenerator(PieceCatalog.Standard, GeneratorSettings.ForProfile(profile), new Pcg32(Seed));

            string actual = string.Join(" ", Enumerable.Range(0, 8).Select(i =>
                string.Join(",", generator.Deal(Board.Empty, placements: i * 15, mercyGame: i % 2 == 1).Select(p => p.Id))));

            Assert.AreEqual(expected, actual);
        }

        [TestCase("gentle", "i2h,i2h,t4a t4b,t4d,i3v c3b,t4d,j4b i3h,i2h,i3v j4b,c5c,l4d j4a,l4d,l4c c3d,i3h,z4v c5c,j4b,c3c c5c,dot,j4c dot,c5c,c5a l4c,i4h,dot z4h,i2h,dot c3c,c3b,i2v")]
        [TestCase("baseline", "i2h,i2h,t4a o2,i3v,t4d i4h,o2,t4b l4a,j4a,i3h i4v,o3,c3c c3c,i5h,i3h i2v,c3d,s4h i3h,i4h,c3a dot,i5v,t4b i2h,dot,s4v dot,c5b,j4a i3v,c3c,c3b i4v,c3a,i2v")]
        [TestCase("hard", "i2v,l4b,i2h i3v,c5b,t4b r23,s4h,t4b c3b,o3,j4b i3h,t4b,i3v o3,o3,c5a j4b,c5c,i5h c5c,c5a,c5a c3d,dot,z4v i5h,dot,c5c dot,l4a,c3b c3b,i4v,c3a i2v,c3c,c3c")]
        public void A_seed_always_deals_the_same_trays_on_a_crowded_board(string profile, string expected)
        {
            var generator = new PieceGenerator(PieceCatalog.Standard, GeneratorSettings.ForProfile(profile), new Pcg32(Seed));

            var trays = Enumerable.Range(0, 8).Select(i => generator.Deal(TestBoards.Crossroads, placements: 10 + i * 15, mercyGame: false))
                .Concat(Enumerable.Range(0, 3).Select(i => generator.Deal(TestBoards.Checkerboard, placements: 50 + i, mercyGame: false)))
                .Concat(Enumerable.Range(0, 2).Select(_ => generator.DealRescue(TestBoards.Crossroads)));
            string actual = string.Join(" ", trays.Select(tray => string.Join(",", tray.Select(p => p.Id))));

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void A_seeded_game_played_first_fit_always_ends_the_same_way()
        {
            const string golden = "placements=43 score=459 lines=15 board=22663EFEC7D03F24";

            Assert.AreEqual(golden, EngineChecks.PlayFirstFit(Seed));
            Assert.AreEqual(golden, EngineChecks.FirstFitGolden);
            Assert.AreEqual(Seed, EngineChecks.GoldenSeed);
        }
    }
}
