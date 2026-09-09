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
        public string teamId;    // empty means no team
        public string title;     // equipped title cosmetic id (M6 shop), empty means none
    }

    /// <summary>A purchasable cosmetic. Purely visual — no power, ever.</summary>
    [Serializable]
    public sealed class CosmeticDto
    {
        public string id;
        public string name;   // display flair, e.g. "Tactician"
        public string kind;   // "title" for now
        public int cost;      // in Sparks
    }

    [Serializable]
    public sealed class CosmeticListDto
    {
        public CosmeticDto[] items;
    }

    /// <summary>A weekly champion record — the "hall of fame" entry granted on reset.</summary>
    [Serializable]
    public sealed class ChampionDto
    {
        public int week;
        public string playerId;
        public string name;
        public int wins;
        public string topTeamId;
        public string topTeamName;
    }

    [Serializable]
    public sealed class ChampionListDto
    {
        public ChampionDto[] champions;
    }

    /// <summary>A team: a named group whose members' wins roll up to a team score.</summary>
    [Serializable]
    public sealed class TeamDto
    {
        public string id;
        public string name;
        public string colorHex;  // banner color
        public string[] memberIds;
    }

    /// <summary>Wrapper so a list of teams round-trips through Unity's JsonUtility (no top-level arrays).</summary>
    [Serializable]
    public sealed class TeamListDto
    {
        public TeamDto[] teams;
    }

    [Serializable]
    public sealed class StandingDto
    {
        public int rank;
        public string playerId;
        public string name;
        public string colorHex;
        public string teamId;
        public string title;
        public int wins;
        public int remainingHp;
    }

    [Serializable]
    public sealed class TeamStandingDto
    {
        public int rank;
        public string teamId;
        public string name;
        public string colorHex;
        public int totalWins;
        public int memberCount;
    }

    [Serializable]
    public sealed class StandingsDto
    {
        public int week;
        /// <summary>
        /// False when <see cref="week"/> is a fallback — the current week has no results yet (the
        /// window between a week rolling over and that night's resolve), so these are the previous
        /// week's final standings and the UI should say so.
        /// </summary>
        public bool isCurrentWeek = true;
        public StandingDto[] players;
        public TeamStandingDto[] teams;
    }

    /// <summary>A player's cosmetic currency and the guards that keep awards idempotent.</summary>
    [Serializable]
    public sealed class WalletDto
    {
        public int sparks;
        public int lastLockDay = -1; // last day a lock bonus was granted
        public int lastWinDay = -1;  // last day win bonuses were granted
        public string[] owned;       // owned cosmetic ids (M6 shop)
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
