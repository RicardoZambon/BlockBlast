#nullable enable
using BlockPuzzle.Core;
using NUnit.Framework;
using UnityEngine;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>Which tuning a resumed Classic game deals with (master plan 3.1).</summary>
    public class ClassicModeTests
    {
        static GameSnapshot AGame() => new GameSession(PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings(), 7).ToSnapshot();

        [Test]
        public void A_damaged_saved_tuning_resumes_with_the_baseline()
        {
            var save = new SaveData
            {
                HasGame = true,
                Game = AGame(),
                GameGenerator = new GeneratorSettings { Name = "broken", Bands = new DifficultyBand[0] },
                GameScoring = new ScoringSettings { LineBase = -5 },
            };

            Assert.IsTrue(ClassicMode.CanContinue(save));
            Assert.AreEqual("baseline", new ClassicMode(save).Resume()!.GeneratorProfile);
        }

        [Test]
        public void A_save_from_before_Remote_Config_resumes_with_the_baseline()
        {
            string json = "{\"Version\":1,\"HasGame\":true,\"Game\":" + JsonUtility.ToJson(AGame()) + "}";
            var save = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual("baseline", save.GameGenerator.Name);
            Assert.AreEqual("baseline", new ClassicMode(save).Resume()!.GeneratorProfile);
        }

        [Test]
        public void A_new_game_saves_the_tuning_it_starts_with()
        {
            var hard = RemoteTuning.From(key => key == "gen_profile" ? "hard" : null);
            var save = new SaveData();

            var game = new ClassicMode(save, () => hard).StartNew();

            Assert.AreEqual("hard", game.GeneratorProfile);
            Assert.AreEqual("hard", save.GameGenerator.Name);
        }
    }
}
