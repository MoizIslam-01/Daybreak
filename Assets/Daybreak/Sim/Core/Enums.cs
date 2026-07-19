namespace Daybreak.Sim
{
    /// <summary>Counter triangle: Vanguard > Skirmisher > Marksman > Vanguard.</summary>
    public enum Archetype
    {
        Vanguard = 0,
        Skirmisher = 1,
        Marksman = 2
    }

    /// <summary>Synergy tag. 2 or 3 copies grant a team-wide buff at battle start.</summary>
    public enum Tag
    {
        Guardian = 0,
        Pack = 1,
        Arcane = 2,
        Swift = 3
    }

    /// <summary>One tactic per squad; shapes targeting (and stats, for Berserk).</summary>
    public enum Tactic
    {
        Balanced = 0,
        FocusFire = 1,
        ProtectBackline = 2,
        Berserk = 3
    }

    /// <summary>Reach determines which enemy rows a unit may target.</summary>
    public enum Reach
    {
        Melee = 0,
        Ranged = 1
    }

    public enum EventType
    {
        BattleStart = 0,
        SynergyApplied = 1,
        ModifierApplied = 2,
        RoundStart = 3,
        Action = 4,
        Attack = 5,
        Damage = 6,
        Death = 7,
        Ability = 8, // reserved for post-MVP active abilities
        BattleEnd = 9
    }
}
