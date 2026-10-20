#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// PCG32 (XSH-RR) random generator by Melissa O'Neill. Pure integer math, so it produces the
    /// same sequence on every platform. Use this, never System.Random, in game logic.
    /// </summary>
    public sealed class Pcg32
    {
        public const ulong DefaultSequence = 54;
        const ulong Multiplier = 6364136223846793005UL;

        ulong _state;
        readonly ulong _increment;

        public Pcg32(ulong seed, ulong sequence = DefaultSequence)
        {
            _state = 0;
            _increment = (sequence << 1) | 1UL;
            NextUInt();
            _state = unchecked(_state + seed);
            NextUInt();
        }

        Pcg32(ulong state, ulong increment, bool restoring)
        {
            _state = state;
            _increment = increment;
        }

        /// <summary>The internal state; save it with Increment to resume the sequence later.</summary>
        public ulong State => _state;

        public ulong Increment => _increment;

        public static Pcg32 FromState(ulong state, ulong increment)
        {
            if ((increment & 1UL) == 0) throw new ArgumentException("The increment must be odd.", nameof(increment));
            return new Pcg32(state, increment, restoring: true);
        }

        public uint NextUInt()
        {
            ulong old = _state;
            _state = unchecked(old * Multiplier + _increment);
            uint xorShifted = unchecked((uint)(((old >> 18) ^ old) >> 27));
            int rotation = (int)(old >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        /// <summary>A uniform integer in [0, bound), without modulo bias.</summary>
        public int NextInt(int bound)
        {
            if (bound <= 0) throw new ArgumentOutOfRangeException(nameof(bound), "The bound must be positive.");
            uint unsignedBound = (uint)bound;
            uint threshold = unchecked((uint)(-(long)unsignedBound)) % unsignedBound;
            while (true)
            {
                uint value = NextUInt();
                if (value >= threshold) return (int)(value % unsignedBound);
            }
        }
    }
}
