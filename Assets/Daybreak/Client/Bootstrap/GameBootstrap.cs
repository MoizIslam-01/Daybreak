using System;
using UnityEngine;

namespace Daybreak.Client
{
    /// <summary>
    /// Drop this on a single GameObject in the Boot scene. Milestone 0's acceptance test lives
    /// here: sign in, load the shared config, and call a trivial server module.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string _greetingName = "Daybreak";
        [SerializeField] private bool _callCloudCodeOnBoot = true;

        private async void Start()
        {
            DontDestroyOnLoad(gameObject);

            try
            {
                var catalog = ConfigService.Units;
                Debug.Log("[Daybreak] Loaded " + catalog.Count + " unit definitions.");

                var playerId = await AuthService.SignInAnonymouslyAsync();
                Debug.Log("[Daybreak] Player id: " + playerId);

                if (_callCloudCodeOnBoot)
                {
                    var hello = await CloudCodeService.SayHelloAsync(_greetingName);
                    Debug.Log("[Daybreak] Server said: " + hello.message +
                              " (server sees " + hello.simUnitCount + " units)");
                }

                Debug.Log("[Daybreak] Boot complete.");
            }
            catch (Exception e)
            {
                Debug.LogError("[Daybreak] Boot failed: " + e);
            }
        }
    }
}
