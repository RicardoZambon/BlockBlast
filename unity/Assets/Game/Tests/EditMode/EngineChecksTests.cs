#nullable enable
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    public class EngineChecksTests
    {
        [Test]
        public void CheckDeterminism_passes_in_the_editor()
        {
            Assert.AreEqual("determinism OK", EngineChecks.CheckDeterminism());
        }

        [Test]
        public void BenchmarkDeals_reports_max_and_mean()
        {
            StringAssert.StartsWith("deals: max ", EngineChecks.BenchmarkDeals(30));
            StringAssert.Contains(" ms, mean ", EngineChecks.BenchmarkDeals(30));
        }
    }
}
