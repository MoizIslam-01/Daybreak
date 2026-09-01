namespace Daybreak.Sim
{
    /// <summary>
    /// A unit's live state during one battle. Instance indices are fixed: side A = 0..4,
    /// side B = 5..9, assigned in placement order — these are exactly the indices the event log
    /// references, so the replay renderer can map an event back to a sprite.
    /// Effective stats are computed once at battle start and never recomputed (integer, final).
    /// </summary>
    public sealed class UnitInstance
    {
        public int Index;      // 0..9
        public int Side;       // 0 = A, 1 = B
        public string UnitId;
        public Archetype Arch;
        public Tag Tag;
        public Reach Reach;

        public int Row;        // 0 = front, 1 = back
        public int Col;        // 0..2

        public int MaxHp;
        public int CurHp;
        public int Atk;
        public int Def;
        public int Spd;

        public bool Alive => CurHp > 0;
    }
}
