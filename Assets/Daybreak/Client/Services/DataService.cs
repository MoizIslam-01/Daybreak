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
        public const string ProfileKey = "profile";

#if !DAYBREAK_UGS_CLOUDSAVE
        // In-memory stand-ins so local play works without the backend.
        private static SquadDto _stubSquad;
        private static ProfileDto _stubProfile;
#endif

        public static async Task SaveLockedSquadAsync(Squad squad, int day)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var dto = SquadCodec.ToDto(squad, day, now);

#if DAYBREAK_UGS_CLOUDSAVE
            // Store as a JSON STRING (JsonUtility reads our public-field DTOs). The server writes
            // and reads these the same way, so the two sides share one format. Reading back with
            // GetAs<DTO>() would fail — the stored value is a string, not an object.
            var data = new Dictionary<string, object> { { LockedSquadKey, JsonUtility.ToJson(dto) } };
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
                return JsonUtility.FromJson<SquadDto>(item.Value.GetAs<string>());
            return null;
#else
            await Task.Yield();
            return _stubSquad;
#endif
        }

        public static async Task SaveProfileAsync(ProfileDto profile)
        {
            var clean = ProfileRules.Sanitize(profile);
#if DAYBREAK_UGS_CLOUDSAVE
            var data = new Dictionary<string, object> { { ProfileKey, JsonUtility.ToJson(clean) } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log("[Daybreak] Profile saved: " + clean.displayName);
#else
            await Task.Yield();
            _stubProfile = clean;
#endif
        }

        /// <summary>
        /// Dev/testing: write a genuinely empty profile (bypassing sanitize) so the first-run
        /// onboarding gate triggers again. Normal saves never produce an empty display name.
        /// </summary>
        public static async Task ClearProfileAsync()
        {
            var empty = new ProfileDto { displayName = "", colorHex = "", emoji = "", teamId = "", title = "" };
#if DAYBREAK_UGS_CLOUDSAVE
            var data = new Dictionary<string, object> { { ProfileKey, JsonUtility.ToJson(empty) } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
#else
            await Task.Yield();
            _stubProfile = empty;
#endif
        }

        public static async Task<ProfileDto> LoadProfileAsync()
        {
#if DAYBREAK_UGS_CLOUDSAVE
            var keys = new HashSet<string> { ProfileKey };
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            if (result.TryGetValue(ProfileKey, out var item))
                return JsonUtility.FromJson<ProfileDto>(item.Value.GetAs<string>());
            return null;
#else
            await Task.Yield();
            return _stubProfile;
#endif
        }

        public const string WalletKey = "wallet";

        public static async Task<WalletDto> LoadWalletAsync()
        {
#if DAYBREAK_UGS_CLOUDSAVE
            var keys = new HashSet<string> { WalletKey };
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            if (result.TryGetValue(WalletKey, out var item))
                return JsonUtility.FromJson<WalletDto>(item.Value.GetAs<string>());
            return null;
#else
            await Task.Yield();
            return null;
#endif
        }

        public static async Task<DayResultDto> LoadDayResultAsync()
        {
#if DAYBREAK_UGS_CLOUDSAVE
            var keys = new HashSet<string> { DayResultKey };
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            if (result.TryGetValue(DayResultKey, out var item))
                return JsonUtility.FromJson<DayResultDto>(item.Value.GetAs<string>());
            return null;
#else
            await Task.Yield();
            return null;
#endif
        }
    }
}
