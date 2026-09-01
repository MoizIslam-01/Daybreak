using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Daybreak.Sim;
using UnityEngine;
#if DAYBREAK_UGS_CLOUDSAVE
using Unity.Services.CloudSave;
#endif

namespace Daybreak.Client
{
    /// <summary>
    /// Cloud Save wrapper (the guide's DataService): stores today's locked squad and reads back the
    /// player's own day result. The squad DTO is the wire format shared with the server module.
    ///
    /// Guarded by DAYBREAK_UGS_CLOUDSAVE so the project builds and runs (against an in-memory stub)
    /// before the Cloud Save package is installed — same pattern as AuthService.
    /// </summary>
    public static class DataService
    {
        public const string LockedSquadKey = "lockedSquad";
        public const string DayResultKey = "dayResult";

#if !DAYBREAK_UGS_CLOUDSAVE
        // In-memory stand-in so local play works without the backend.
        private static SquadDto _stubSquad;
#endif

        public static async Task SaveLockedSquadAsync(Squad squad, int day)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var dto = SquadCodec.ToDto(squad, day, now);

#if DAYBREAK_UGS_CLOUDSAVE
            var data = new Dictionary<string, object> { { LockedSquadKey, dto } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log("[Daybreak] Locked squad saved for day " + day + ".");
#else
            await Task.Yield();
            _stubSquad = dto;
            Debug.LogWarning("[Daybreak] Cloud Save not installed — locked squad kept in memory only.");
#endif
        }

        public static async Task<SquadDto> LoadLockedSquadAsync()
        {
#if DAYBREAK_UGS_CLOUDSAVE
            var keys = new HashSet<string> { LockedSquadKey };
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            if (result.TryGetValue(LockedSquadKey, out var item))
                return item.Value.GetAs<SquadDto>();
            return null;
#else
            await Task.Yield();
            return _stubSquad;
#endif
        }

        public static async Task<DayResultDto> LoadDayResultAsync()
        {
#if DAYBREAK_UGS_CLOUDSAVE
            var keys = new HashSet<string> { DayResultKey };
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            if (result.TryGetValue(DayResultKey, out var item))
                return item.Value.GetAs<DayResultDto>();
            return null;
#else
            await Task.Yield();
            return null;
#endif
        }
    }
}
