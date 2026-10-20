namespace BlockPuzzle.Sim.Tests;

public class BotsTests
{
    [Fact]
    public void Names_lists_the_bots_the_simulator_can_run()
    {
        Assert.Equal(new[] { "greedy", "random" }, Bots.Names);
    }

    [Fact]
    public void Names_cannot_be_changed_through_a_cast()
    {
        Assert.Throws<NotSupportedException>(() => ((IList<string>)Bots.Names)[0] = "greedy");
    }

    [Fact]
    public void Create_makes_the_bot_with_that_name()
    {
        Assert.IsType<GreedyBot>(Bots.Create("greedy", seed: 1));
        Assert.IsType<RandomBot>(Bots.Create("random", seed: 1));
    }

    [Fact]
    public void Create_rejects_an_unknown_name()
    {
        Assert.Throws<ArgumentException>(() => Bots.Create("nope", seed: 1));
    }
}
