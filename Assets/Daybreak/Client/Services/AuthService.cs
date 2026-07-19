using System;
using System.Threading.Tasks;
using UnityEngine;
#if DAYBREAK_UGS_CORE
using Unity.Services.Core;
#endif
#if DAYBREAK_UGS_AUTH
using Unity.Services.Authentication;
#endif

namespace Daybreak.Client
{
    /// <summary>
    /// Wraps UGS Authentication. Anonymous sign-in is enough for a friend group; the player id
    /// it returns is the key everything else (squads, results, leaderboards) hangs off.
    ///
    /// The UGS calls are compiled out until the packages are installed, so the project always
    /// builds. Once com.unity.services.authentication is in the manifest, the versionDefines on
    /// Daybreak.Client.asmdef switch these blocks on automatically — no manual define juggling.
    /// </summary>
    public static class AuthService
    {
        public static bool IsSignedIn { get; private set; }
        public static string PlayerId { get; private set; }

        public static async Task<string> SignInAnonymouslyAsync()
        {
#if DAYBREAK_UGS_CORE && DAYBREAK_UGS_AUTH
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            PlayerId = AuthenticationService.Instance.PlayerId;
            IsSignedIn = true;
            Debug.Log("[Daybreak] Signed in as " + PlayerId);
            return PlayerId;
#else
            await Task.Yield();
            PlayerId = "local-dev-player";
            IsSignedIn = false;
            Debug.LogWarning(
                "[Daybreak] UGS packages are not installed yet — running with a stub player id. " +
                "See Assets/Daybreak/Docs/MILESTONE-0.md.");
            return PlayerId;
#endif
        }

        public static void SignOut()
        {
#if DAYBREAK_UGS_CORE && DAYBREAK_UGS_AUTH
            if (AuthenticationService.Instance.IsSignedIn)
                AuthenticationService.Instance.SignOut();
#endif
            IsSignedIn = false;
            PlayerId = null;
        }
    }
}
