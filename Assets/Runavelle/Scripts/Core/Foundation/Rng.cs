using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>
    /// Deterministic seeded PRNG (SplitMix64 seeding + xoshiro128** generator). Same seed and same call
    /// sequence produce the same values on every platform (integer-only state).
    /// </summary>
    public sealed class Rng
    {
        private uint _s0, _s1, _s2, _s3;

        public Rng(ulong seed)
        {
            ulong x = seed;
            _s0 = (uint)SplitMix(ref x);
            _s1 = (uint)SplitMix(ref x);
            _s2 = (uint)SplitMix(ref x);
            _s3 = (uint)SplitMix(ref x);
            if ((_s0 | _s1 | _s2 | _s3) == 0) _s0 = 1;
        }

        /// <summary>Derive an independent stream for a sub-system without disturbing this stream.</summary>
        public static Rng Derive(ulong seed, ulong salt) => new Rng(seed * 0x9E3779B97F4A7C15UL ^ (salt + 0xD1B54A32D192ED03UL));

        private static ulong SplitMix(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        private static uint Rotl(uint x, int k) => (x << k) | (x >> (32 - k));

        public uint NextUInt()
        {
            uint result = Rotl(_s1 * 5, 7) * 9;
            uint t = _s1 << 9;
            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = Rotl(_s3, 11);
            return result;
        }

        /// <summary>Uniform float in [0,1).</summary>
        public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            uint span = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % span);
        }

        public float Range(float min, float max) => min + (max - min) * NextFloat();

        public bool Chance(float p) => NextFloat() < p;

        public T Pick<T>(IReadOnlyList<T> list) => list[Range(0, list.Count)];

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>Weighted index pick. Returns -1 if all weights are zero.</summary>
        public int PickWeighted(IReadOnlyList<float> weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++) total += Math.Max(0f, weights[i]);
            if (total <= 0f) return -1;
            float r = NextFloat() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                float w = Math.Max(0f, weights[i]);
                if (r < w) return i;
                r -= w;
            }
            return weights.Count - 1;
        }
    }
}
