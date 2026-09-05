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

    /// <summary>A player's public identity: what everyone else sees instead of a raw id.</summary>
    [Serializable]
    public sealed class ProfileDto
    {
        public string displayName;
        public string colorHex;  // accent color, e.g. "#4A90D9"
        public string emoji;     // short avatar glyph, e.g. "🐉" (optional)
        public string teamId;    // set in the teams phase; empty means no team
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
        public string opponentName;   // snapshot of the opponent's display name
        public string opponentColor;  // snapshot of their accent color
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
        public string myName;           // this player's display name at resolve time
        public string myColor;
        public SquadDto mySquad;        // the squad this player fielded that day
        public BattleRecordDto[] battles;
    }
}
