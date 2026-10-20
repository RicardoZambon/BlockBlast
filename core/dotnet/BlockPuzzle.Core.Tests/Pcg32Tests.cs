namespace BlockPuzzle.Core.Tests;

public class Pcg32Tests
{
    [Fact]
    public void Matches_the_reference_implementation()
    {
        // pcg32_srandom_r(&rng, 42u, 54u) followed by pcg32_random_r, from the PCG reference demo.
        var rng = new Pcg32(42, 54);
        uint[] expected = { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };

        foreach (uint value in expected) Assert.Equal(value, rng.NextUInt());
    }

    [Fact]
    public void The_same_seed_gives_the_same_sequence()
    {
        var a = new Pcg32(2026);
        var b = new Pcg32(2026);

        for (int i = 0; i < 1000; i++) Assert.Equal(a.NextUInt(), b.NextUInt());
    }

    [Fact]
    public void NextInt_stays_in_range_and_reaches_every_value()
    {
        var rng = new Pcg32(5);
        var seen = new bool[7];

        for (int i = 0; i < 10_000; i++)
        {
            int value = rng.NextInt(7);
            Assert.InRange(value, 0, 6);
            seen[value] = true;
        }

        Assert.All(seen, Assert.True);
    }

    [Fact]
    public void NextInt_rejects_bounds_below_one()
    {
        var rng = new Pcg32(5);

        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(-1));
    }

    [Fact]
    public void A_restored_generator_continues_the_same_sequence()
    {
        var original = new Pcg32(123);
        for (int i = 0; i < 10; i++) original.NextUInt();

        var restored = Pcg32.FromState(original.State, original.Increment);

        for (int i = 0; i < 100; i++) Assert.Equal(original.NextUInt(), restored.NextUInt());
    }

    [Fact]
    public void FromState_rejects_an_even_increment()
    {
        Assert.Throws<ArgumentException>(() => Pcg32.FromState(1, 2));
    }
}
