#nullable enable
using System;
using System.Security.Cryptography;

namespace BlockPuzzle.Game
{
    /// <summary>Seeds for new games. Game logic never uses System.Random: the engine's Pcg32 starts from one of these.</summary>
    public static class SeedSource
    {
        public static ulong NewSeed()
        {
            var bytes = new byte[8];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }
    }
}
