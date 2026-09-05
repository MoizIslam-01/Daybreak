using System;

namespace Daybreak.Sim
{
    /// <summary>
    /// Plain data-transfer objects for storage and transport. Public fields, no logic, no Unity —
    /// so the Unity client (Cloud Save, via Newtonsoft) and the Cloud Code module (System.Text.Json
    /// with IncludeFields) serialize the identical shapes. These are the wire format; the sim's own
    /// types (Squad, PlayerDayResult) stay clean.
    /// </summary>
    [Serializable]
    public sealed class PlacementDto
    {
        public string unitId;
        public int row;
        public int col;
    }

    [Serializable]
    public sealed class SquadDto
    {
        public string ownerId;
        public string tactic;        // Tactic name
        public int day;              // the resolve day this lock targets
        public long lockedAtUnixMs;  // informational
        public PlacementDto[] units;
    }

    [Serializable]
    public sealed class BattleRecordDto
    {
        public string opponentId;
        public bool won;
        public int seed;
        public string modifierId;     // the weekly modifier in force (for exact replay)
        public SquadDto opponentSquad; // snapshot, so the client can regenerate the fight offline
    }

    [Serializable]
    public sealed class DayResultDto
    {
        public int day;
        public int wins;
        public int losses;
        public int remainingHpAcrossWins;
        public SquadDto mySquad;        // the squad this player fielded that day
        public BattleRecordDto[] battles;
    }
}
