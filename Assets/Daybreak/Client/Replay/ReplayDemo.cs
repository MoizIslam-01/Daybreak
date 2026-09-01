using Daybreak.Sim;
using UnityEngine;

namespace Daybreak.Client
{
    /// <summary>
    /// The only component you add to the Replay scene. On Play it loads the shared roster, builds
    /// two demo squads, runs the deterministic sim locally, configures the camera, and hands the
    /// result to a ReplayPlayer. This is the M2 stand-in for "feed the renderer sim output";
    /// M3 will replace the hardcoded squads with the squad builder.
    /// </summary>
    [RequireComponent(typeof(ReplayPlayer))]
    public sealed class ReplayDemo : MonoBehaviour
    {
        [Header("Demo battle")]
        [SerializeField] private int _seed = 42;
        [SerializeField] private Tactic _tacticA = Tactic.Balanced;
        [SerializeField] private Tactic _tacticB = Tactic.FocusFire;

        [Tooltip("Five unit ids for side A (see Resources/units.json).")]
        [SerializeField] private string[] _squadA = { "aegis", "warhound", "houndmaster", "arcanist", "flicker" };

        [Tooltip("Five unit ids for side B.")]
        [SerializeField] private string[] _squadB = { "sentinel", "ripper", "pavise", "windrunner", "charger" };

        private void Start()
        {
            ConfigureCamera();

            var defs = ConfigService.Units;
            var a = BuildSquad("A", _tacticA, _squadA);
            var b = BuildSquad("B", _tacticB, _squadB);

            if (!a.IsValid(out var ea)) { Debug.LogError("[Daybreak] Squad A invalid: " + ea); return; }
            if (!b.IsValid(out var eb)) { Debug.LogError("[Daybreak] Squad B invalid: " + eb); return; }

            var result = BattleSimulator.Simulate(a, b, WeeklyModifier.None, _seed, defs);
            Debug.Log("[Daybreak] Demo battle resolved: winner side " + (result.WinnerSide == 0 ? "A" : "B")
                      + " (" + result.Log.Length + " events)");

            GetComponent<ReplayPlayer>().Play(a, b, WeeklyModifier.None, result, defs);
        }

        private static Squad BuildSquad(string owner, Tactic tactic, string[] ids)
        {
            // Grid slots front-then-back: (0,0)(0,1)(0,2)(1,0)(1,1).
            int[,] slots = { { 0, 0 }, { 0, 1 }, { 0, 2 }, { 1, 0 }, { 1, 1 } };
            var placements = new Placement[5];
            for (int i = 0; i < 5; i++)
                placements[i] = new Placement(ids[i], slots[i, 0], slots[i, 1]);
            return new Squad { OwnerId = owner, Tactic = tactic, Units = placements };
        }

        private static void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 6.8f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.11f, 0.12f, 0.15f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
    }
}
