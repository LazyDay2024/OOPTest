# Tower Defense — OOP Implementation

C# / Unity 6 (6000.3.12f1, URP) implementation of the attached class diagram and
spec. Build order 1–11 (no object pooling). Everything lives under
`Assets/Scripts` in the `TowerDefense.*` namespaces, split into three assemblies:

| Assembly | Path | Purpose |
|---|---|---|
| `TowerDefense.Runtime` | `Assets/Scripts` | all gameplay code |
| `TowerDefense.Editor` | `Assets/Editor` | asset/scene generator (editor only) |
| `TowerDefense.Tests.EditMode` / `.PlayMode` | `Assets/Tests` | NUnit tests |

## How to run

### Option A — zero setup (recommended first look)
1. Open the project in Unity.
2. New empty scene → create an empty GameObject → add the **`GameBootstrap`**
   component → press **Play**.
   It builds the path, grid, managers, ScriptableObject data and primitive
   "prefabs" entirely in code, adds the interactive build layer, and starts
   wave 1. A small on-screen HUD (driven only by `GameEvents`) shows currency /
   base HP / wave / enemy count.

### Interactive controls (both Option A and Option B scenes)

| Action | How |
|---|---|
| Choose a tower to build | Click **`1 NormalTower $60`** / **`2 SlowTower $80`** on the bottom-left bar, or press number keys `1` / `2` |
| Place it | Move the mouse — a ghost + range ring follows (green = ok, red = can't afford / on the path). **Left-click** a cell. Stays in build mode so you can drop several. |
| Leave build mode | **Esc**, right-click, or the **Cancel** button |
| Select a placed tower | **Left-click** it (when not in build mode) → blue range ring + panel bottom-right |
| Upgrade | **`Upgrade $X`** in the panel — cost scales with level; `+25%` damage, `+10%` range |
| Sell | **`Sell`** in the panel — refunds 50% of cost |

`BuildController` only translates input into `Player.PlaceTower` / `Player.TryUpgrade` /
`Player.SellTower` calls — all economy and grid rules stay in `Player` / `MapManager`.
Input is read through IMGUI (`Event.current`) so it works with the project's
"Input System (New)" setting with no extra package wiring.

### Option B — generate real project assets
Menu **Tools ▸ Tower Defense ▸ Build Demo Project**. This writes:
- `Assets/Prefabs/**` — primitive prefabs for each enemy / tower / projectile
- `Assets/ScriptableObjects/**` — `EnemyData`, `TowerData`, `SlowTowerData`, `WaveData`
- `Assets/Scenes/TowerDefense.unity` — a fully wired scene

Open that scene and press Play. Now you can tune every number in the Inspector.

### Option C — headless (no editor window)
```bash
"D:\For Backup\6000.3.12f1\Editor\Unity.exe" -batchmode -quit -nographics \
  -projectPath "D:\OOP101" \
  -executeMethod TowerDefense.EditorTools.TDSceneBuilder.BuildDemoProject
```

## OOP mapping

| Principle | Where |
|---|---|
| **Abstraction** | `Enemy` and `Tower` are `abstract`; the base holds the full contract (`Tower` owns `range`, `targetLayer`, cooldown, `FindTarget`; subclasses only implement `Shoot`). |
| **Encapsulation** | `Player.currency`, `Enemy.hp/speed`, `Tower._currentCooldown` are private; outside code goes through methods/read-only properties. ScriptableObjects expose read-only properties, not public fields, so a shared asset can't be mutated at runtime. |
| **Inheritance (meaningful)** | `SlowTower` / `SlowTowerData` add `slowPercent` + `slowDuration` that genuinely belong to the subtype — nothing is copy-pasted from the parent. |
| **Polymorphism / LSP** | `Enemy.TakeDamage()` is identical for every subtype — no override that silently ignores a hit. `AirEnemy` does **not** override it. Whether a tower may hit an enemy is decided once, in `Tower.FindTarget()`, via `TargetLayer.CanTarget(EnemyLayer)`. |
| **Open/closed** | `Buff` is an extension point; adding a new status effect (poison, stun…) needs a new `Buff` subclass and nothing changed in `Enemy`. |
| **Single source of truth** | `MapManager` owns the path and the build grid; `Enemy`, `WaveManager` and `Player` all read from it. |
| **Decoupling** | `GameEvents` is a one-way notification hub; core systems raise events, UI/audio can subscribe later without the core referencing them. |

## Tests

`Assets/Tests/EditMode/CoreLogicTests.cs` — layer eligibility truth table, `SlowBuff`
math and expiry, `SpawnGroup`.

`Assets/Tests/PlayMode/LspTargetingTests.cs`:
- `GroundTower_NeverTargets_AirEnemy_EvenWhenAirIsCloser` — the core LSP proof.
- `BothTower_CanTarget_AirEnemy`.
- `TakeDamage_BehavesIdentically_ForGroundAndAir`.
- `EnemyReachingEndOfPath_ReducesBaseHp`.

Run headless:
```bash
"D:\For Backup\6000.3.12f1\Editor\Unity.exe" -batchmode -projectPath "D:\OOP101" \
  -runTests -testPlatform EditMode -testResults "D:\OOP101\editmode-results.xml"
```
(swap `EditMode` for `PlayMode` for the second suite).

## Deliberate deviations from the spec (all for correctness / clarity)

1. **`Tower.Shoot(Enemy target)`** takes the validated target instead of calling
   `FindTarget()` again inside itself — avoids targeting twice per shot and a
   frame-to-frame mismatch.
2. **`Tower.FindTarget()` is a shared `virtual` in the base**, not `abstract`.
   Both towers target identically, so the LSP-critical layer filter has exactly
   one implementation.
3. **ScriptableObject fields are `[SerializeField] private` + read-only
   properties** instead of public fields, with an `internal Configure(...)` for
   code/editor creation. Stronger encapsulation.
4. **`SlowTowerData : TowerData`** so slow numbers stay data-driven (spec §3)
   rather than living only on the component.
5. Added **`GameBootstrap` / `DebugHud` / `BuildController` / `RangeIndicator`**
   (and an optional `DemoTowerPlacer`) — not in the diagram; they exist so the
   game runs, is playable, and is verifiable without hand-authoring assets in the
   editor. `Player` gained `TrySpend` / `TryUpgrade`; `Tower` gained a scaling
   `UpgradeCost`; `MapManager` gained `SnapToCell`.

## Not done (per agreed scope)

- Object pooling (build-order step 12) — `Instantiate`/`Destroy` is used directly.
  `Enemy`/`Projectile` already funnel destruction through single methods
  (`Die`, `Despawn`), so pooling can be slotted in later without touching callers.
- Real UI — only the debug HUD + `GameEvents` hooks.
