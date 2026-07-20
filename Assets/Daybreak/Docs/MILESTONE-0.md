# Milestone 0 — Foundations ✅

**Goal (from the design guide):** you can sign in and call a trivial server module from the app.

**Status: done** (20 Jul 2026). Verified end to end — anonymous sign-in returns a real UGS player
id, and the deployed Cloud Code module reports `server sees 12 units`, proving it compiled
`Daybreak.Sim` and read the same `units.json` the client did.

Steps below are kept as the rebuild/onboarding path.

---

## What's already in the repo

```
Assets/Daybreak/
  Sim/                        Daybreak.Sim — NO UnityEngine reference (asmdef enforces it)
    Core/Enums.cs             Archetype, Tag, Tactic, Reach, EventType
    Core/UnitDef.cs           Unit stat block
    Core/Squad.cs             Placement, Squad, grid validation
    Core/BattleEvent.cs       Replay log entry
    Core/BattleResult.cs      Result + WeeklyModifier
    Core/DeterministicRng.cs  xorshift32 + per-battle seed derivation
    Core/BattleSimulator.cs   API shape + counter triangle (Simulate lands in M1)
    Config/UnitCatalog.cs     Order-stable unit lookup
  Client/                     Daybreak.Client — references Daybreak.Sim
    Services/ConfigService.cs Loads Resources/units.json
    Services/AuthService.cs   UGS anonymous sign-in
    Services/CloudCodeService.cs  Calls the SayHello module endpoint
    Bootstrap/GameBootstrap.cs    Boot sequence
  Resources/units.json        The 12-unit roster — the shared config, single source of truth
  Tests/EditMode/             Determinism, boundary, triangle, and squad-validation tests

CloudCode/Daybreak/           Server module (compiles the same Sim source + same units.json)
```

The UGS calls in `AuthService` / `CloudCodeService` are behind `#if DAYBREAK_UGS_*` defines that
switch on **automatically** via `versionDefines` the moment the packages are installed. Until then
the project compiles and boots against local stubs — so nothing is blocked on the dashboard.

---

## Step 1 — Install the UGS packages (Unity Editor)

`Window → Package Manager → + → Install package by name`:

| Package name | Why |
|---|---|
| `com.unity.services.core` | required by everything else |
| `com.unity.services.authentication` | player identity |
| `com.unity.services.cloudsave` | locked squads, results (used from M4) |
| `com.unity.services.cloudcode` | calling server modules |
| `com.unity.services.deployment` | deploys the Cloud Code C# module from the Editor |

Take the latest verified version of each for Unity 6. After they resolve, the `DAYBREAK_UGS_*`
defines light up on their own — check that `AuthService.cs` is no longer greyed out in your IDE.

## Step 2 — Create and link the UGS project

1. Sign in to <https://cloud.unity.com> and create an organization if you don't have one.
2. Create a project named **Daybreak**.
3. In Unity: `Edit → Project Settings → Services` → link to that project.
4. On the dashboard, go to **LiveOps → Authentication** and enable the service if prompted.

   There is **no anonymous sign-in toggle** — anonymous requires no identity provider, so it works
   the moment the project is linked and the service is on. The *Identity Providers* page is for
   external providers (Google, Apple, Steam), which a friend group doesn't need.

   Do check **LiveOps → Environments**: `production` exists by default, and the Editor's
   environment (`Project Settings → Services → Environment`) must match wherever you deploy the
   Cloud Code module in step 4 — otherwise the module call 404s.

   Sanity check after step 3: your anonymous player should appear under
   **LiveOps → Authentication → Players** with the same id the Console printed.

## Step 3 — Create the Boot scene

1. `File → New Scene` (Basic 2D) → save as `Assets/Daybreak/Scenes/Boot.unity`.
2. Create an empty GameObject named `Bootstrap` and add the **GameBootstrap** component.
3. `File → Build Profiles → Scene List` → add `Boot` and drag it to index 0.
4. Press Play. The Console should show:
   - `Loaded 12 unit definitions.`
   - `Player id: <a real UGS id>` — if it says `local-dev-player`, the packages aren't installed.

## Step 4 — Deploy the Cloud Code module

The module project already exists at `CloudCode/`, matched to Unity's template. You just need to
point Unity at it:

1. `Assets → Create → Cloud Code C# Module Reference`, name it **Daybreak**. The name must match
   `CloudCodeService.ModuleName` exactly — it's case-sensitive.
2. Select the `.ccmr` asset. Its **Path** field wants a **`.sln`**, not a `.csproj`. Browse to
   `CloudCode/Daybreak.sln` and hit **Apply**.
3. `Window → Deployment` → tick Daybreak → **Deploy Selected**.

**Do not click "Generate Solution"** unless you want to inspect a fresh template — it scaffolds a
throwaway project at `../new_module_reference/` that has nothing to do with ours.

### What the template taught us (already applied to `CloudCode/Daybreak/Daybreak.csproj`)

| Setting | Value | Note |
|---|---|---|
| `TargetFramework` | `net9.0` | not net8.0 |
| `Com.Unity.Services.CloudCode.Apis` | `0.0.26` | note the capital `Com.` prefix |
| `Com.Unity.Services.CloudCode.Core` | **`0.0.4`** | the template pins `0.0.5`, which was never published to nuget.org — restore fails with NU1102. Don't "correct" this back. |
| `Microsoft.Extensions.Logging.Abstractions` | `7.0.1` | for the injected `ILogger` |

There is no `Unity.Services.CloudCode.Shared` package, despite what older docs suggest.

`Nullable` and `ImplicitUsings` are **off** in our project (the template turns them on) because the
shared sim source uses explicit usings and no nullable annotations.

## Step 5 — Prove the round trip

Press Play again. The Console should print:

```
[Daybreak] Server said: Hello, Daybreak! Daybreak's server module is alive. (server sees 12 units)
```

`server sees 12 units` is the real prize: it means the server compiled `Daybreak.Sim` and read the
same `units.json` the client did. **That's Milestone 0 done.**

## Step 6 — Run the tests

`Window → General → Test Runner → EditMode → Run All`. 22 tests, all green. The important one
is `SimAssembly_ReferencesNothingUnity` — if that ever goes red, the server can no longer run the
sim and the whole architecture is broken. Treat it as a build blocker.

---

## Troubleshooting

**`local-dev-player` in the log** — UGS packages missing, or the versionDefines didn't trigger.
Check Package Manager, then `Edit → Project Settings → Player → Scripting Define Symbols` and add
`DAYBREAK_UGS_CORE;DAYBREAK_UGS_AUTH;DAYBREAK_UGS_CLOUDCODE` manually as a fallback.

**Module deploys but the client gets a deserialization error** — the module returns PascalCase
(`Message`) and the client expects camelCase (`message`). The SDK matches case-insensitively, but
if your SDK version doesn't, rename the fields in `CloudCodeService.HelloWorldResponse` to match.

**Sim won't compile inside the module** — something in `Assets/Daybreak/Sim` picked up a
`using UnityEngine`. That's the boundary test failing for real; remove the dependency rather than
working around it.

---

## Before Milestone 4, you owe answers to these (guide §14)

1. Resolve time and time zone (a fixed UTC hour is recommended) — which hour?
2. How many friends now, and expected? (Round-robin is fine up to ~30.)
3. iOS, Android, or both?
4. OK to stand up a small Firebase project for push notifications?
5. Confirm squad size 5, 2×3 grid, one tactic per squad.
6. Who makes the art? (Placeholder shapes assumed until told otherwise.)
7. Keep local practice mode? (Recommended: yes.)

Not urgent now — but #1 and #3 are worth deciding early since they shape the build setup.
