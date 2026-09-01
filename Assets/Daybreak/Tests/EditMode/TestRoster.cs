using System.Collections.Generic;
using Daybreak.Sim;

namespace Daybreak.Tests
{
    /// <summary>
    /// Builds the 12-unit starter roster directly in code, mirroring Resources/units.json.
    /// Keeps the sim tests dependency-free (no Unity Resources / file IO) and fully deterministic.
    /// If units.json changes, update these to match — a divergence test guards the intent.
    /// </summary>
    public static class TestRoster
    {
        public static UnitCatalog Catalog()
        {
            return new UnitCatalog(new List<UnitDef>
            {
                Def("aegis", "Aegis", Archetype.Vanguard, Tag.Guardian, 130, 13, 10, 4),
                Def("warhound", "Warhound Alpha", Archetype.Vanguard, Tag.Pack, 118, 16, 8, 5),
                Def("runeshield", "Runeshield", Archetype.Vanguard, Tag.Arcane, 120, 14, 8, 4),
                Def("charger", "Charger", Archetype.Vanguard, Tag.Swift, 110, 15, 7, 6),
                Def("sentinel", "Sentinel Blade", Archetype.Skirmisher, Tag.Guardian, 78, 20, 5, 8),
                Def("ripper", "Ripper", Archetype.Skirmisher, Tag.Pack, 70, 24, 3, 9),
                Def("hexblade", "Hexblade", Archetype.Skirmisher, Tag.Arcane, 70, 21, 3, 9),
                Def("flicker", "Flicker", Archetype.Skirmisher, Tag.Swift, 64, 22, 2, 11),
                Def("pavise", "Pavise", Archetype.Marksman, Tag.Guardian, 62, 24, 4, 5),
                Def("houndmaster", "Houndmaster", Archetype.Marksman, Tag.Pack, 55, 28, 2, 6),
                Def("arcanist", "Arcanist", Archetype.Marksman, Tag.Arcane, 55, 25, 2, 6),
                Def("windrunner", "Windrunner", Archetype.Marksman, Tag.Swift, 50, 26, 1, 8),
            });
        }

        private static UnitDef Def(string id, string name, Archetype a, Tag t, int hp, int atk, int def, int spd)
            => new UnitDef { Id = id, Name = name, Arch = a, Tag = t, HP = hp, Atk = atk, Def = def, Spd = spd };

        /// <summary>A quick valid squad from five unit ids, filling the grid front-then-back.</summary>
        public static Squad Squad(string owner, Tactic tactic, params string[] ids)
        {
            // Grid slots in a fixed order: (0,0)(0,1)(0,2)(1,0)(1,1).
            int[,] slots = { { 0, 0 }, { 0, 1 }, { 0, 2 }, { 1, 0 }, { 1, 1 } };
            var placements = new Placement[5];
            for (int i = 0; i < 5; i++)
                placements[i] = new Placement(ids[i], slots[i, 0], slots[i, 1]);
            return new Squad { OwnerId = owner, Tactic = tactic, Units = placements };
        }
    }
}
