using System;

namespace Daybreak.Sim
{
    /// <summary>Maps between the sim's runtime types and the storage DTOs. Pure and shared.</summary>
    public static class SquadCodec
    {
        public static SquadDto ToDto(Squad squad, int day, long lockedAtUnixMs)
        {
            if (squad == null) throw new ArgumentNullException(nameof(squad));

            var units = new PlacementDto[squad.Units.Length];
            for (int i = 0; i < squad.Units.Length; i++)
            {
                var p = squad.Units[i];
                units[i] = new PlacementDto { unitId = p.UnitId, row = p.Row, col = p.Col };
            }

            return new SquadDto
            {
                ownerId = squad.OwnerId,
                tactic = squad.Tactic.ToString(),
                day = day,
                lockedAtUnixMs = lockedAtUnixMs,
                units = units
            };
        }

        public static Squad FromDto(SquadDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.units == null) throw new ArgumentException("Squad DTO has no units.");

            var placements = new Placement[dto.units.Length];
            for (int i = 0; i < dto.units.Length; i++)
            {
                var u = dto.units[i];
                placements[i] = new Placement(u.unitId, u.row, u.col);
            }

            Tactic tactic = Enum.TryParse<Tactic>(dto.tactic, false, out var t) ? t : Tactic.Balanced;
            return new Squad { OwnerId = dto.ownerId, Tactic = tactic, Units = placements };
        }

        public static DayResultDto ToDto(PlayerDayResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            var battles = new BattleRecordDto[result.Battles.Count];
            for (int i = 0; i < result.Battles.Count; i++)
            {
                var b = result.Battles[i];
                battles[i] = new BattleRecordDto { opponentId = b.OpponentId, won = b.Won, seed = b.Seed };
            }

            return new DayResultDto
            {
                day = 0,
                wins = result.Wins,
                losses = result.Losses,
                remainingHpAcrossWins = result.RemainingHpAcrossWins,
                battles = battles
            };
        }
    }
}
