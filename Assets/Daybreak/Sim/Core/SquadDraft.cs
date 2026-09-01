namespace Daybreak.Sim
{
    /// <summary>
    /// Mutable, in-progress squad the builder UI edits: six grid slots (2 rows × 3 cols), of which
    /// exactly five end up filled, plus a tactic. Pure and Unity-free so the placement rules live
    /// in one place and can be unit-tested — the UI is just a view onto this.
    ///
    /// Slot index = row * Cols + col, with row 0 = front. TryBuild produces a validated Squad.
    /// </summary>
    public sealed class SquadDraft
    {
        public const int Rows = 2;
        public const int Cols = 3;
        public const int SlotCount = Rows * Cols; // 6
        public const int MaxUnits = 5;

        private readonly string[] _slots = new string[SlotCount];

        public Tactic Tactic = Tactic.Balanced;

        public static int SlotIndex(int row, int col) => row * Cols + col;
        public static int RowOf(int slot) => slot / Cols;
        public static int ColOf(int slot) => slot % Cols;

        public string GetSlot(int slot) => (slot >= 0 && slot < SlotCount) ? _slots[slot] : null;

        public int FilledCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < SlotCount; i++) if (_slots[i] != null) n++;
                return n;
            }
        }

        public bool IsComplete => FilledCount == MaxUnits;

        public int SlotOf(string unitId)
        {
            if (string.IsNullOrEmpty(unitId)) return -1;
            for (int i = 0; i < SlotCount; i++) if (_slots[i] == unitId) return i;
            return -1;
        }

        public bool Contains(string unitId) => SlotOf(unitId) >= 0;

        /// <summary>
        /// Put a unit in a slot. Rejects: bad input, a unit already placed elsewhere, or adding a
        /// sixth unit (the grid must always keep one slot empty). Overwriting a filled slot with a
        /// new unit is allowed and keeps the count the same.
        /// </summary>
        public bool Place(int slot, string unitId, out string error)
        {
            if (slot < 0 || slot >= SlotCount) { error = "Slot out of range."; return false; }
            if (string.IsNullOrEmpty(unitId)) { error = "No unit given."; return false; }

            int existing = SlotOf(unitId);
            if (existing >= 0 && existing != slot) { error = "That unit is already placed."; return false; }

            if (_slots[slot] == null && FilledCount >= MaxUnits)
            {
                error = "Squad is full (" + MaxUnits + "). Remove one first.";
                return false;
            }

            _slots[slot] = unitId;
            error = null;
            return true;
        }

        public void Clear(int slot)
        {
            if (slot >= 0 && slot < SlotCount) _slots[slot] = null;
        }

        public void ClearUnit(string unitId)
        {
            int s = SlotOf(unitId);
            if (s >= 0) _slots[s] = null;
        }

        public void ClearAll()
        {
            for (int i = 0; i < SlotCount; i++) _slots[i] = null;
        }

        /// <summary>Build a validated Squad, or fail with a reason (shared with Squad.IsValid).</summary>
        public bool TryBuild(string owner, out Squad squad, out string error)
        {
            squad = null;
            if (FilledCount != MaxUnits) { error = "Place exactly " + MaxUnits + " units."; return false; }

            var placements = new Placement[MaxUnits];
            int k = 0;
            for (int slot = 0; slot < SlotCount; slot++)
            {
                var id = _slots[slot];
                if (id == null) continue;
                placements[k++] = new Placement(id, RowOf(slot), ColOf(slot));
            }

            var built = new Squad { OwnerId = owner, Tactic = Tactic, Units = placements };
            if (!built.IsValid(out error)) return false;

            squad = built;
            error = null;
            return true;
        }
    }
}
