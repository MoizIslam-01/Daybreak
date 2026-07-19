namespace Daybreak.Sim
{
    /// <summary>
    /// xorshift32. Small, explicit, and identical on every runtime — unlike System.Random,
    /// whose internals are not contractually stable across .NET versions.
    /// Never use System.Random or UnityEngine.Random inside the sim.
    /// </summary>
    public struct DeterministicRng
    {
        private uint _state;

        public DeterministicRng(int seed)
        {
            // 0 is a fixed point of xorshift, so map it to a non-zero constant.
            _state = seed == 0 ? 0x9E3779B9u : unchecked((uint)seed);
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Uniform in [0, exclusiveMax). Rejection-sampled, so no modulo bias.</summary>
        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 1) return 0;
            uint bound = (uint)exclusiveMax;
            uint limit = uint.MaxValue - (uint.MaxValue % bound) - 1;
            uint r;
            do { r = NextUInt(); } while (r > limit);
            return (int)(r % bound);
        }

        /// <summary>Deterministic coin flip, used to break exact HP ties at the round cap.</summary>
        public bool NextBool()
        {
            return (NextUInt() & 1u) == 1u;
        }

        /// <summary>
        /// Per-battle seed. Pure function of the day and the two player ids, so any machine
        /// re-deriving a replay gets the same fight. Order the ids before calling for symmetry.
        /// </summary>
        public static int BattleSeed(int day, string playerIdA, string playerIdB)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = Mix(h, (uint)day);
                h = MixString(h, playerIdA);
                h = MixString(h, playerIdB);
                return (int)h;
            }
        }

        private static uint MixString(uint h, string s)
        {
            if (s == null) return Mix(h, 0u);
            for (int i = 0; i < s.Length; i++) h = Mix(h, s[i]);
            return h;
        }

        private static uint Mix(uint h, uint value)
        {
            unchecked
            {
                h ^= value;
                h *= 16777619u;
                return h;
            }
        }
    }
}
