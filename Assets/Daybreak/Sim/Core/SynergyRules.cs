using System.Collections.Generic;

namespace Daybreak.Sim
{
    /// <summary>
    /// Synergy tag buffs (guide §4.3). A squad that fields 2 or 3+ copies of a tag gets a
    /// team-wide buff at battle start. All values are permille or flat integers — no floats.
    ///
    /// | Tag      | 2 copies            | 3 copies            |
    /// | Guardian | +15% team Defense   | +30% team Defense   |
    /// | Pack     | +12% team Attack    | +25% team Attack    |
    /// | Arcane   | +4 true damage      | +9 true damage      |
    /// | Swift    | +15% team Speed     | +30% team Speed     |
    /// </summary>
    public struct SynergyBuffs
    {
        public int AtkPermille;   // Pack
        public int DefPermille;   // Guardian
        public int SpdPermille;   // Swift
        public int ArcaneTrueDamage; // flat, ignores DEF

        public static SynergyBuffs Neutral => new SynergyBuffs
        {
            AtkPermille = 1000,
            DefPermille = 1000,
            SpdPermille = 1000,
            ArcaneTrueDamage = 0
        };
    }

    public static class SynergyRules
    {
        /// <summary>
        /// Counts each tag across the squad's units and returns the resulting team buffs.
        /// A tag needs 2+ copies to do anything; 3+ hits the higher tier (4+ still counts as the
        /// 3-tier — there are only ever 5 slots so 4 or 5 of a tag is possible and lands here).
        /// </summary>
        public static SynergyBuffs Compute(Squad squad, UnitCatalog defs, bool doubleArcane)
        {
            int guardian = 0, pack = 0, arcane = 0, swift = 0;

            for (int i = 0; i < squad.Units.Length; i++)
            {
                var def = defs.Get(squad.Units[i].UnitId);
                switch (def.Tag)
                {
                    case Tag.Guardian: guardian++; break;
                    case Tag.Pack: pack++; break;
                    case Tag.Arcane: arcane++; break;
                    case Tag.Swift: swift++; break;
                }
            }

            var buffs = SynergyBuffs.Neutral;
            buffs.DefPermille = Tier(guardian, 1150, 1300, 1000);
            buffs.AtkPermille = Tier(pack, 1120, 1250, 1000);
            buffs.SpdPermille = Tier(swift, 1150, 1300, 1000);

            int arcaneBase = arcane >= 3 ? 9 : arcane >= 2 ? 4 : 0;
            buffs.ArcaneTrueDamage = doubleArcane ? arcaneBase * 2 : arcaneBase;

            return buffs;
        }

        private static int Tier(int copies, int two, int threePlus, int none)
        {
            if (copies >= 3) return threePlus;
            if (copies >= 2) return two;
            return none;
        }
    }
}
