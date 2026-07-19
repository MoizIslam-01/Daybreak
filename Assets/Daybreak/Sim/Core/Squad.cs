namespace Daybreak.Sim
{
    /// <summary>A unit placed on the 2x3 grid. Row 0 = front, Row 1 = back. Col 0..2.</summary>
    public struct Placement
    {
        public string UnitId;
        public int Row;
        public int Col;

        public Placement(string unitId, int row, int col)
        {
            UnitId = unitId;
            Row = row;
            Col = col;
        }
    }

    /// <summary>Exactly five placements plus one tactic. This is what gets locked each day.</summary>
    public sealed class Squad
    {
        public const int UnitCount = 5;
        public const int Rows = 2;
        public const int Cols = 3;

        public string OwnerId;
        public Placement[] Units;
        public Tactic Tactic;

        /// <summary>
        /// Structural validation only (no balance rules). Returns false with a reason so the
        /// builder UI and the server can share one rulebook.
        /// </summary>
        public bool IsValid(out string error)
        {
            if (Units == null || Units.Length != UnitCount)
            {
                error = "A squad must contain exactly " + UnitCount + " units.";
                return false;
            }

            var occupied = new bool[Rows * Cols];
            for (int i = 0; i < Units.Length; i++)
            {
                var p = Units[i];
                if (string.IsNullOrEmpty(p.UnitId))
                {
                    error = "Placement " + i + " has no unit id.";
                    return false;
                }
                if (p.Row < 0 || p.Row >= Rows || p.Col < 0 || p.Col >= Cols)
                {
                    error = "Placement " + i + " is off the grid (row " + p.Row + ", col " + p.Col + ").";
                    return false;
                }
                int slot = p.Row * Cols + p.Col;
                if (occupied[slot])
                {
                    error = "Two units share slot (row " + p.Row + ", col " + p.Col + ").";
                    return false;
                }
                occupied[slot] = true;

                for (int j = 0; j < i; j++)
                {
                    if (Units[j].UnitId == p.UnitId)
                    {
                        error = "Duplicate unit '" + p.UnitId + "'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }
    }
}
