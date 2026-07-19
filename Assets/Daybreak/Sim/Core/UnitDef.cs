namespace Daybreak.Sim
{
    /// <summary>
    /// Static definition of a roster unit. Loaded from shared config (units.json) so the
    /// client and the server read byte-identical numbers. Never hardcode stats elsewhere.
    /// </summary>
    public sealed class UnitDef
    {
        public string Id;
        public string Name;
        public Archetype Arch;
        public Tag Tag;
        public int HP;
        public int Atk;
        public int Def;
        public int Spd;

        /// <summary>Marksmen are ranged; Vanguards and Skirmishers are melee.</summary>
        public Reach AttackReach => Arch == Archetype.Marksman ? Reach.Ranged : Reach.Melee;
    }
}
