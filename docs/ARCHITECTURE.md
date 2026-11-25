# Space & Crafts — Foundation Architecture

Reference: `GOAL.md`

This document defines the first-milestone architecture only. Implement systems incrementally; do not dump unfinished features into production scenes.

## 1. Project Architecture

### Stack

| Concern | Choice |
|---|---|
| Engine | Unity 6 |
| Render pipeline | URP |
| Input | Unity Input System (controller-first) |
| Tunable data | ScriptableObjects |
| Targets | Desktop, WebGL, controllers; console-ready later |

Do not couple gameplay to WebGL-only APIs. Prefer inspector/data-driven values over hardcoded constants.

### Folder layout

```text
Assets/
  SpaceAndCrafts/
    Art/
    Audio/
    Characters/Splat|Enemies/
    Materials/
    Prefabs/Gameplay|Environment|Characters|VFX/
    Scenes/Development|RainbowWorld/
    Scripts/Core|Player|Camera|Gameplay|Crafting|AI|UI/
    ScriptableObjects/
    UI/
    VFX/
```

Third-party packages stay outside `SpaceAndCrafts/` when practical.

### Core runtime responsibilities

| Module | Owns |
|---|---|
| `Scripts/Core` | Game flow, level lifecycle, events, service locators (minimal) |
| `Scripts/Player` | Splat movement, feel, presentation hooks |
| `Scripts/Camera` | Third-person platforming camera (C# namespace: `SpaceAndCrafts.Cameras` — avoids clash with `UnityEngine.Camera`) |
| `Scripts/Gameplay` | Collectibles, interaction, triggers, carrying |
| `Scripts/Crafting` | Build sites, assembly presentation, inventions |
| `Scripts/AI` | Enemies / mini-boss (later) |
| `Scripts/UI` | HUD / prompts (minimal for slice) |

### Game state (foundation)

Keep state small and serializable-friendly later:

- `LevelSession` — current slice progress (components held, build completed, boss state)
- `GameEvents` — decoupled signals (Collected, BuildCompleted, LevelCompleted, RestartRequested)
- Avoid a god `GameManager`. Prefer a thin `LevelDirector` for Arrival only when needed.

### Development scenes

| Scene | Purpose |
|---|---|
| `PlayerPlayground` | Movement, camera, jump feel, slopes |
| `MechanicsPlayground` | Collect / craft / invention tests |
| `ArtPlayground` | Materials, lighting, props, scale |
| `RainbowWorld_Arrival` | Vertical slice production scene |

Never prototype every system directly inside Arrival.

---

## 2. Initial Scene Structure

### Shared bootstrap (all gameplay scenes)

1. `Systems` — input, level session, events
2. `Player` — Splat root + controller + visual (TEMP ART until model exists)
3. `CameraRig` — follow camera + collision
4. `Lighting` — URP lighting / volumes
5. `Environment` — graybox or authored kit
6. `Gameplay` — collectibles, build sites, triggers
7. `Audio` — listeners / ambience (later)

### Scene ownership rules

- Player and camera are prefabs reused across scenes.
- Tunables live on ScriptableObjects (`SplatMotorSettings`, `CameraSettings`, `CollectibleDefinition`).
- Scene objects reference prefabs/data assets; avoid name-based `Find`.

### Arrival beat structure (blockout later)

1. Vista / entry
2. Movement safe zone
3. Gateway sightline
4. Material + 3 components
5. Enemy + traversal
6. Craft sequence (Boing-O-Matic)
7. Optional secret
8. Final challenge + mini-boss
9. Multi-gateway reveal

---

## 3. Splat Player-Controller Architecture

### Goals

Make RUN / JUMP / CAMERA / LANDING / COLLECTING excellent before ability sprawl.

### Components (split by concern)

| Component | Responsibility |
|---|---|
| `SplatInputReader` | Reads Input System actions → normalized move / look / jump / interact |
| `SplatMotor` | CharacterController (or Rigidbody if chosen later) locomotion: accel, jump, coyote, buffer, air control |
| `SplatMotorSettings` | ScriptableObject: speeds, gravity, jump height, coyote/buffer times |
| `SplatFeelPresenter` | Squash/stretch, landing FX hooks (presentation only) |
| `SplatInteractor` | Proximity interact / collect / carry entry point; resolves child colliders to their nearest `Interactable` parent and focuses by collider surface distance |
| `InteractPromptHud` | Screen-space focus prompt with binding labels from the active input asset |

### Motor contract (Phase 2)

Required feel features:

- camera-relative analog move
- intentional accel / decel
- jump + variable height
- coyote time + jump buffer
- air control
- landing feedback hooks

Deferred abilities: double jump, ground pound, belly bounce, ledge grab, wall bounce, glide, craft-powered moves.

### Placeholder art

Use a clearly labeled TEMP ART capsule/sphere mesh under `Characters/Splat` until the real model exists. Never treat it as final.

---

## 4. Camera Architecture

### Goals

Readable third-person platforming camera that never fights the player.

### Components

| Component | Responsibility |
|---|---|
| `PlatformingCameraRig` | Orbit / follow / pitch limits / player look input |
| `CameraCollision` | SphereCast/ray pull-in to avoid walls |
| `CameraSettings` | ScriptableObject: distances, smoothing, FOV, pitch clamps |
| `CameraFramingHint` (optional later) | Soft landmark bias without stealing control |

### Rules

- Smooth follow, intelligent obstruction avoidance
- Player retains rotation control whenever possible
- Minimal shake; avoid motion-sickness patterns
- Important moments may bias framing briefly, then release

Player motor reads camera yaw for move direction; camera does not own locomotion.

---

## 5. Collectible / Component Architecture

### Categories for the slice

Use only what helps Arrival:

| Type | Role in slice |
|---|---|
| Material | One primary collectible (identity / teaching) |
| Component | Three unique pieces for Boing-O-Matic |
| Sparks / Badges / Memory Scraps | Deferred unless a secret needs a light reward |

### Components

| Piece | Responsibility |
|---|---|
| `CollectibleDefinition` | SO: id, display name, category, VFX/SFX hooks |
| `CollectiblePickup` | World pickup: trigger, feedback, raise Collected event |
| `InventoryComponent` | Tracks held ids/counts for the session (serializable-friendly) |
| `CarryableComponent` | Optional physical carry for large parts |
| `Interactable` | Generic prompt + activate interface |
| `BuildSite` | Requires component set → plays assembly → spawns invention |
| `Invention` / `BoingOMatic` | Gameplay effect (launch) separate from assembly presentation |

### Flow

```text
Pickup → Inventory/Event → BuildSite validates set → Assembly presentation → Invention enabled → Traversal / boss use
```

Crafting is adventure interaction, not a survival crafting menu.

### Events (suggested)

- `OnCollectibleAcquired(CollectibleDefinition)`
- `OnBuildCompleted(InventionId)`
- `OnLevelObjectiveChanged`

Presentation listens; gameplay does not depend on UI objects existing.

---

## 6. Immediate implementation order

Aligned with `GOAL.md` phases:

1. Unity 6 URP project + Input System wiring (when Unity project is created)
2. Splat motor + playground feel
3. Platforming camera
4. Collect / interact / carry
5. One craft → Boing-O-Matic
6. Arrival graybox
7. Art / personality / mini-boss / polish

### Temporary until Unity project exists

- Folder scaffolding under `Assets/SpaceAndCrafts/`
- Architecture scripts as stubs with clear TODOs
- No claim that URP/Input packages are configured until a real Unity project is initialized

### Non-goals for foundation checkpoint

- Full Rainbow World
- Complete save system
- Workshop hub
- Full enemy AI suite
- Final character art
- Procedural meshes presented as finished art
