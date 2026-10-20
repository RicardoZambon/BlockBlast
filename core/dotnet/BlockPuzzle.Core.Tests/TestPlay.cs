namespace BlockPuzzle.Core.Tests;

static class TestPlay
{
    /// <summary>Plays the first legal move, scanning slots in order and positions top-left first.</summary>
    public static void FirstFit(GameSession game)
    {
        var tray = game.Tray.ToArray();
        for (int slot = 0; slot < tray.Length; slot++)
        {
            var shape = tray[slot];
            if (shape is null) continue;
            for (int i = 0; i < shape.PlacementCount; i++)
            {
                if (game.TryPlace(slot, shape.AnchorRow(i), shape.AnchorCol(i), out _)) return;
            }
        }
    }
}
