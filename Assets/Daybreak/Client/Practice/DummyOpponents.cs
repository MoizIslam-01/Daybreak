using Daybreak.Sim;

namespace Daybreak.Client
{
    /// <summary>
    /// Preset opponent squads for local practice (M3). Not the real matchmaking — just something
    /// to fight so the build → watch loop works offline. Each is a valid 5-unit squad with a tactic.
    /// </summary>
    public static class DummyOpponents
    {
        private struct Preset
        {
            public string Name;
            public Tactic Tactic;
            public string[] Ids;
        }

        private static readonly Preset[] _presets =
        {
            new Preset { Name = "Wall of Shields", Tactic = Tactic.ProtectBackline,
                Ids = new[] { "aegis", "runeshield", "charger", "arcanist", "houndmaster" } },
            new Preset { Name = "Rushdown", Tactic = Tactic.Berserk,
                Ids = new[] { "ripper", "flicker", "sentinel", "hexblade", "warhound" } },
            new Preset { Name = "Snipers' Nest", Tactic = Tactic.FocusFire,
                Ids = new[] { "pavise", "houndmaster", "arcanist", "windrunner", "aegis" } },
            new Preset { Name = "Balanced Band", Tactic = Tactic.Balanced,
                Ids = new[] { "warhound", "sentinel", "windrunner", "charger", "ripper" } },
        };

        public static int Count => _presets.Length;

        public static string NameOf(int index) => _presets[Mod(index)].Name;

        public static Squad At(int index)
        {
            var p = _presets[Mod(index)];
            int[,] slots = { { 0, 0 }, { 0, 1 }, { 0, 2 }, { 1, 0 }, { 1, 1 } };
            var placements = new Placement[5];
            for (int i = 0; i < 5; i++)
                placements[i] = new Placement(p.Ids[i], slots[i, 0], slots[i, 1]);
            return new Squad { OwnerId = "cpu:" + p.Name, Tactic = p.Tactic, Units = placements };
        }

        private static int Mod(int i)
        {
            int m = i % _presets.Length;
            return m < 0 ? m + _presets.Length : m;
        }
    }
}
