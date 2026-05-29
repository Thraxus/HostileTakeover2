# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

This is a Space Engineers mod — a .NET 4.8 class library targeting the SE modding API. There are no tests; validation is manual in-game.

```bash
# Build (AnyCPU)
msbuild HostileTakeover2.sln /p:Configuration=Release /p:Platform=AnyCPU

# Build (x64 — enforces LangVersion=6)
msbuild HostileTakeover2.sln /p:Configuration=Release /p:Platform=x64
```

**Every new `.cs` file must be added to `HostileTakeover2.csproj` as an explicit `<Compile>` entry — the project does not use globbing.**

## Language Constraints (C# 6)

- No inline `out` variable declarations. Pre-declare: `Type x; Method(out x);`
- No expression-bodied members on multi-statement methods.
- `entity.DefinitionId` is `MyDefinitionId?` (nullable struct) — use `?.SubtypeId.String`.
- `IMyModContext.ModId` is `string` — compare as `== "3154371364"`, not as ulong.

## Architecture

### Entry Point

`HostileTakeover2Core.cs` is a `BaseSessionComp` with priority `int.MinValue + 1`. Init sequence:

1. `EarlyInit()` — register `DebugType` log categories
2. `SuperEarlySetup()` — wire `UserConfigController`, run `BlockClassifier.Populate()` and `BlockClassificationOverridesReader.Read()`, register `OnEntityAdd`
3. `BeforeStart()` — build NPC identity cache, register faction events
4. `UpdateBeforeSim()` — tick `ActionQueue`

### Mediator (central service locator)

`Infrastructure/Mediator.cs` holds every shared service: `ActionQueue`, the three controllers (`ConstructController`, `GrinderController`, `HighlightController`), the three object pools (`Construct`, `Block`, `HighlightSettings`), `BlockClassificationData`, and the NPC identity cache. All cross-component communication goes through `Mediator` — never wire controllers to each other directly.

### Object Model

- **`Construct`** (pooled) — one per logical grid group. Owns `GridOwnershipController`, `BlockController`, and `GridGroupManager`. The unit of ownership reasoning.
- **`Block`** (pooled) — one per important block (Control/Medical/Weapon/Trap). Fires `BlockHasBeenDisabledAction` when ground down.
- **`HighlightSettings`** (pooled) — carries display parameters for one active highlight.

Pool return (`ReturnBlock`, `ReturnConstruct`) calls `Reset()` which nulls fields. Never return a pooled object while still inside a delegate that uses those fields — defer via `ActionQueue`.

### Ownership Flow

```
OnEntityAdd → ActionQueue (EntityAddTickDelay ticks) → ValidateGrid
  → ConstructFactory → Construct.Init → Construct.Evaluate
    → CalculateGroupOwnerId (tally NPC-owned blocks across group)
    → SetGroupOwnership → per-grid ApplyOwnership
      → GridOwnershipController.SetOwnership → TakeOverGrid
        → BlockController.AddGrid → per-block AddBlock (deferred 10 ticks)
```

`GridOwnershipController` owns all SE API ownership calls. `Construct.ApplyOwnership()` and `Construct.DisownGrid()` are thin coordinators — they do not call SE API directly.

### Block Classification

`BlockClassificationData` holds four `HashSet<string>` keyed by `block.BlockDefinition.Id.ToString()`. Populated once at init by `BlockClassifier.Populate()`. `BlockController.AssignBlock()` does a single O(1) lookup against those sets.

Override flow: `BlockClassifier.Populate()` → `BlockClassificationWriter.Write()` (writes XML to world storage) → `BlockClassificationOverridesReader.Read()` (applies user overrides from world storage). XML files live per-save, not in global storage.

### Highlight Flow

All highlight triggering routes through `Construct.TriggerHighlights(grinder)` — `GrinderController` never calls `HighlightController` directly. Selection strategy (nearest / all / group-priority / tier-limited) is resolved inside `HighlightController`. Alpha=0 on the highlight color renders outline-only (no fill); `HighlightFillAlpha` user setting maps 0–100% → 0–255.

### ActionQueue

Tick-based deferred scheduler (`Common/Generics/ActionQueue.cs`). Wraps every action in try-catch — safe. Gated diagnostic logging behind `DebugType.ActionQueue`. Baseline: ~15k actions at init (NPC grid processing), 0–4 at steady state.

### NPC Identity Cache

`HashSet<long>` in `Mediator`, built at `BeforeStart()`. Kept current via `FactionStateChanged` (`FactionMemberAcceptJoin` + `TryGetSteamId == 0`). Exposed as `Func<long, bool> IsNpcIdentity` passed into `GridOwnershipController.Init()`.

## Key SE API Gotchas

- `OnEntityAdd` fires before grid physics/ownership is fully populated — always defer processing by at least one tick.
- `GridLinkTypeEnum.NoContactDamage` with `MyCubeGrid.GetGridGroup()` returns null even for rotor-connected grids. Use `Logical` only.
- `MyCubeBlock.Name` is populated for NPC blocks — `SetHighlight` name lookup is reliable on NPC grids.
- `MyVisualScriptLogicProvider.SetHighlight` can throw during session teardown — always guard highlight API calls.
- On a DS, `grinder.GetPosition()` is unreliable at equip time. Resolve character position via `MyAPIGateway.Entities.GetEntityById(grinder.OwnerId)?.GetPosition()`. (`grinder.OwnerId` = character entity ID; `grinder.OwnerIdentityId` = player identity ID.)
- SE does not fire `OnBlockOwnershipChanged` during world load. SE does not wrap event delegates in try-catch — unhandled exceptions propagate to the game loop.
- `MyFactionStateChange.FactionMemberAcceptJoin`: `fromFactionId` = faction joined, `playerId` = joining identity. `TryGetSteamId(playerId) == 0` means NPC.

## Code Style

- Using order: System → HostileTakeover2 (alphabetical) → Sandbox → VRage (alphabetical).
- `var` in foreach and when type is obvious from RHS; explicit types for primitives.
- Aligned columns in settings/assignment blocks.
- `WriteGeneral` log calls at Verbose level only for high-frequency paths (per-block, per-grid init).
- Swearing in comments is fine.

## Log File

`D:\SpaceEngineers\AppData\Storage\HostileTakeover2_Thraxus\HostileTakeover2Core.log`

## Workflow Notes

- Always present a plan and wait for approval before making any code changes.
- Work in small, focused chunks.
- Never use `cd /path && git ...` — use `git -C /path ...`.
- Never use `$()` substitution in git commit messages. Use multiple `-m` flags for multi-paragraph messages; use `*` not `- ` for bullets in commit bodies.
- Never chain commands with `&&` — use two separate Bash calls.
