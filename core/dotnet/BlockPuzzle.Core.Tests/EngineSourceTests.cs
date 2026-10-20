namespace BlockPuzzle.Core.Tests;

public class EngineSourceTests
{
    [Fact]
    public void Every_engine_file_starts_with_nullable_enable()
    {
        // The engine project turns nullable off, as Unity does. A file without the directive still compiles
        // unless it uses annotations, and silently loses its nullable checks, so every file must opt in.
        var missing = Directory.GetFiles(EngineSourceFolder(), "*.cs", SearchOption.AllDirectories)
            .Where(file => File.ReadLines(file).FirstOrDefault() != "#nullable enable")
            .Select(file => Path.GetFileName(file));

        Assert.Empty(missing);
    }

    static string EngineSourceFolder()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            string candidate = Path.Combine(dir.FullName, "core", "src");
            if (File.Exists(Path.Combine(candidate, "Board.cs"))) return candidate;
        }
        throw new DirectoryNotFoundException("Can't find core/src above the test output folder.");
    }
}
