# Rainbow Kingdom — The Arrival

Development plan for the first vertical slice (~10–15 minutes).

Reference: `GOAL.md`, `docs/ARCHITECTURE.md`, `docs/STORY_BIBLE.md`

## Design north star

Create curiosity: *"I wonder where THAT goes."*

### Arrival landscape direction

The spawn shelf has a runtime-generated faceted slate foundation below its exposed edges. It sinks beneath the fjord surface and has no collider, so it roots the rectangular shelf visually without changing the authored jumps or rescue path. Iteration 124 reduces the existing fractured slate albedo to 4 × 3 repeats across the shelf to give the 18 × 14 unit surface a broader stone scale; the fallback procedural grain keeps its old scale. Iteration 125 shifts the shelf tint toward cooler, darker slate and removes one redundant perimeter rock cluster. Iteration 126 adds six irregular slate plates at the shelf margins. Iteration 148 replaces them with eight larger, irregular beveled outcrops. Both passes are render-only and leave the center lane and floor collider unchanged. A live 16:9 view confirms visible low profiles against the fjord vista; the shelf still reads broad and flat. The smoke verifies checkpoint recovery inside and outside the catch, but a human edge-fall playthrough remains necessary to assess the feel.

Arrival uses a Nordic fjord-inspired landscape: slow-moving deep blue-green water below the route, steep slate cliffs with a restrained repeating albedo surface, layered distant snow-tipped peaks, narrow waterfalls, and faint aqua/lilac aurora curtains above the ridge line. A 2:1 equirectangular twilight fjord panorama supplies the distant skybox; the existing procedural twilight sky remains the fallback if the panorama or built-in panoramic shader is unavailable. The generated aurora meshes add a subtle foreground layer, and the beacon remains the focal accent. The handmade cosmic props and rainbow gateways retain the local identity inside that landscape. A low-contrast third ridge sits behind the main peaks, and a distant circular ridge with small snow accents closes the horizon in every direction. The side-ridge span remains 460 units; water covers 528 units, extending 4 units beyond each edge of the 520-unit fall trigger. The trigger now tracks the water surface, so falls from higher checkpoints can reach the fjord and trigger the splash instead of snapping back above the water. Within the scenic catch footprint, the emergency backup sits below the trigger; outside it, the ordinary six-unit checkpoint-relative rescue still protects Splat. Arrival's exponential-squared fog is a cooler blue-gray at density 0.003 to keep the nearby route clear while blending the distant ridges. The live 16:9 view confirms the spawn basin and nearby ridge framing. Catch coverage and timing, fog and panorama blend, cliff-texture tiling, and full-horizon composition still need dedicated visual and edge-fall review.

The animated Arrival water surface uses a softened blue-teal fjord-water albedo at a roughly 22-unit world repeat, adding low-contrast ripples below the route and giving the splash catch a more tangible destination. The texture drifts slowly across the surface to complement its gentle vertex waves. The first generated version read as rough water, so it was replaced with broader, quieter ripples and no white crest streaks. Appearance, drift speed, and mirrored repetition still need a current capture.

Iteration 127 changes the cloned rock cluster tint multipliers toward cool blue-gray to reduce their clash with the Norway-inspired slate and fjord palette. This change is source-only until Unity refreshes and renders the updated copies; review the result before adjusting Graphics.

The player's fall safety is also present in every playable world scene (Worlds 02–10 and Arrival): a source audit confirms `PlayerRespawn` is serialized in all ten scene files and returns Splat to the active checkpoint after a six-unit drop, including falls outside a scene's scenic catch volume. Arrival adds the water catch and splash as a presentation layer over that backup. Keep the visible basin and enclosing ridges continuous around the route so this forgiving recovery reads as part of a beautiful world, not a fall into an empty void. This coverage was checked in scene source only; edge falls and the complete player-camera view still need a human run.

The runtime-generated fjord owns its meshes and materials through `ArrivalFjordResourceOwner`, which releases them when the backdrop unloads. Repeated Arrival entry should preserve the vista without accumulating those generated resources; confirm in a runtime reload pass.

`ArrivalStyleAtmosphere` owns and releases its runtime procedural skybox material, restoring the previous camera clear and skybox when Arrival unloads. Confirm the dusk gradient, aurora contrast, and scene-transition restoration in-game.

`ArrivalFjordAmbience` adds a very quiet procedural wind/water bed for the Arrival scene. It is a placeholder for an authored field recording and needs a mix check against world music, character voice, and gameplay cues.

Checkpoint recovery camera polish: `PlatformingCameraRig` now recenters immediately on `PlayerRespawned`, clears stale follow/look smoothing and impact punch, and cancels an active cinematic guide while preserving the current orbit. Verify that the fall → splash → checkpoint sequence feels settled in a human playthrough.

Finale dialogue order: after Rattle releases the relay, it admits it feared the road would break and recognizes Splat's repair idea. The gateways brighten during that line; the beacon then sends Vesper's three-tap answer. The 5.8-second camera hold and 6.1-second hub delay provide room for the toast exchange and signal. Confirm both lines remain legible and the camera frames the signal in a playthrough.

Primary idea of the level: **answer Vesper's signal by gathering odd parts, building a launch invention, and resetting Rattle's gateway relay.** Rattle's weak point accepts a hit only during the short airborne window after Boing-O-Matic launches Splat; a normal jump or fall cannot substitute for the crafted traversal tool. If an ordinary airborne contact reaches the exposed point, a throttled toast explains the required launch.

First invention (prototype): **Boing-O-Matic** (spring + metal dish + power crystal). Arrival also has an optional badge recipe, **Turbo Boing**; it is an upgrade of the same traversal tool and must satisfy the standard Boing objective, pad, and boss gates while retaining its separate side-quest ID. At the build site, Q / right shoulder explicitly cycles recipe choices before Interact commits the selected craft; the prompt shows the active bindings and each selection reports whether its parts are ready. Each unique recipe can be assembled once, so if Splat builds Standard before finding the Secret Nook badge, the unbuilt Turbo recipe remains selectable afterward. Collecting the badge immediately explains that it unlocks Turbo; the field note gives the selection instruction once the main parts are gathered, and points back to the Workshop if Standard was already built. Exact recipe may change if a stronger idea appears, but keep “three parts → traversal tool → boss exam.”

---

## Beat sheet

| # | Beat | Player experience | Systems used |
|---|---|---|---|
| 1 | Opening vista | The first story card introduces teenage inventor Splat and names his brother Squat, then shows Vesper beside their shared three-tap promise; the final card returns to both portraits as Splat answers. On Arrival, the distant terrace beacon quietly pulses three times as the camera finds the gate. | Opening story / portraits / beacon cue / scene art |
| 2 | Entry | Splat arrives; safe read of silhouette + camera | Motor, camera |
| 3 | Movement nursery | Learn run / jump / land without threat | PlayerPlayground lessons in-level |
| 4 | Gateway tease | Distant Rainbow Gateway visible early | Landmark framing |
| 5 | Primary material | One clear collectible that teaches pickup | CollectiblePickup |
| 6 | Three components | Spring, dish, crystal (or renamed equivalents) | Inventory |
| 7 | Simple enemy | One readable foe; teaches caution, not combat depth | AI lite |
| 8 | Traversal beat | Gap / height that foreshadows the invention | Level geometry |
| 9 | Craft sequence | Build site assembles Boing-O-Matic visibly | BuildSite + presentation |
| 10 | Optional secret | Off-path reward for curiosity | Badge/Spark light or vista |
| 11 | Landmark | Authored memorable space (not generic rainbow fill) | Environment kit |
| 12 | Final challenge | Short mastery of launch + prior lessons | Invention use |
| 13 | Mini-boss | Final exam for Boing-O-Matic | Boss phases |
| 14 | Final invention payoff | Use the Boing-O-Matic to reach Rattle's exposed relay and calm its storm | Invention / boss exam |
| 15 | Signal + multi-gateway reveal | Vesper answers from beyond the gate with the promised three taps; a brief portrait hologram appears above the beacon alongside the written “I’m here, Splat. Keep looking.” reply; newly lit rainbows point to the other worlds | Gateway beacon / chime / hologram / reply toast |

---

## Teaching sequence (INTRODUCE → TEST)

Build completion waits for the assembly presenter's actual end, and the selected recipe flavor is applied before the invention is revealed. This keeps the completion toast and boss unlock behind the craft presentation, with Turbo launch strength set before first use.

1. **Introduce** — see a springy/crafty object in the world.
2. **Teach** — pick up spring; short bounce pad or demo interact.
3. **Expand** — find dish + crystal; inventory UI/prompt stays light.
4. **Combine** — assemble Boing-O-Matic at workshop-ish build site.
5. **Twist** — optional secret requiring a precise launch.
6. **Test** — mini-boss that telegraphs windows for launch / stun / finish.

---

## Space plan (graybox first)

Rough zones (names temporary):

1. **Arrival Shelf** — vista + nursery platforms.
2. **Paintfall Glade** — material + first component; soft enemy intro.
3. **Junk Arch Workshop** — remaining components + build site.
4. **Launch Gulch** — mandatory Boing-O-Matic traversal.
5. **Secret Nook** — optional, readable from Gulch if curious.
6. **Gateway Terrace** — mini-boss arena + reveal of multiple rainbows.

Sightline rule: from Arrival Shelf, player should see Gateway Terrace rainbow (incomplete/dormant) so the destination is always emotionally present.

---

## Mini-boss sketch (prototype)

**Scrap-Storm Sentinel, nicknamed Rattle** (environmental gatekeeper, not a Nintendo homage).

| Phase | Intent |
|---|---|
| 1 | Read pattern: wind / debris / slam with clear anticipation |
| 2 | Use Boing-O-Matic to reach weak perch / interrupt |
| 3 | Activate or overcharge a nearby invention socket |
| 4 | Memorable launch finish; arena calms; Vesper's three-tap signal answers and rainbows awaken |

Keep timing forgiving; readable for kids; no huge opaque health bar as the fantasy.

---

## Production order

### A. Playgrounds (before Arrival blockout)

- [x] `PlayerPlayground` — motor + camera feel baseline (human sign-off still open)
- [x] `MechanicsPlayground` — 3 pickups → build → launch
- [x] `ArtPlayground` — material language samples (foil, cardboard, paint, plastic)

### B. Graybox Arrival

- [x] Block zones + critical path (TEMP ART graybox)
- [x] Place gateway sightline and landmark volumes
- [ ] Tune jump distances to motor settings (human playtest)
- [x] Fall-death / restart volumes + extra checkpoints

### C. Systems integration

- [x] Wire components + build site
- [x] Enemy v1 (ScrapMite)
- [x] Secret (Curious Nook discovery badge)
- [x] Mini-boss v1 using invention (Scrap-Storm Sentinel + terrace Boing pad)
- [x] Fall/respawn + scene restart (R)
- [x] Multi-gateway reveal on level complete (TEMP brighten)

### D. Art & personality

- [ ] Replace TEMP/graybox with modular authored kit
- [x] Assembly VFX/SFX, squash/stretch, small world reactions (TEMP sparks/dust/SFX; still needs authored)
- [x] Label remaining TEMP ART explicitly (world banners + HUD watermark)
- [x] Spring teach bounce near first spring pickup (INTRODUCE → TEACH)
- [x] Shelf sightline to Gateway Terrace (open workshop arch + tall rainbow/beacon)
- [x] Craft assembly sequential snap presentation (TEMP)
- [x] TEMP procedural SFX (collect/craft/boing/jump/land/complete)
- [x] TEMP silhouettes: collectibles, Boing-O-Matic, gateway pillars/keystone, Shelf vista, boss arena ring
- [x] TEMP zone landmarks: upward paintfall, floating scrap field, abandoned workshop invention, secret pedestal/stars
- [x] TEMP personality juice: craft sputter/admire, slam/boing/land camera punches, mite startle, antenna wiggle

### E. Polish gate (Definition of Done)

Checklist from `GOAL.md` milestone DoD:

- [ ] Movement joyful; camera trustworthy; controller works *(systems char OK in smoke38; need human)*
- [ ] Clear visual identity; readable collectibles *(TEMP silhouettes/HUD/nursery labels; need authored + human)*
- [x] Crafting feels physical; invention changes gameplay *(TEMP sparks/snap + CraftAdmireBeat + smoke systems OK)*
- [x] Mini-boss uses invention; one secret *(systems OK; slam punch/dust/SFX TEMP)*
- [ ] No finished-looking primitive spam; no compiler errors *(compiler OK; primitives still labeled TEMP)*
- [x] Colliders solid; restart + complete level work *(validator7 + smoke38 OK; human still wise)*
- [x] Curiosity about other Rainbow Gateways *(opening look + reveal toast/sparks)*

---

## Asset needs (document, don’t fake)

| Asset | Status | Notes |
|---|---|---|
| Splat hero model/rig | Missing | TEMP ART placeholder until commissioned/authored |
| Boing-O-Matic prop set | Missing | Spring, dish, crystal + assembled hero prop |
| Arrival modular kit | Missing | Platforms, flora, workshop bits — cosmic/handmade |
| Rainbow Gateway landmark | Missing | Hero silhouette readable at distance |
| Simple enemy | Missing | One silhouette, readable tells |
| Mini-boss | Missing | Prefer environment-scale construct |

Until real art exists: graybox + visible `TEMP ART` labels only.

---

## Current next steps

The original foundation checklist below has been completed and superseded by the living backlog. Follow `TODO.md` for the active priority order. The current highest-value Arrival work is to review the updated fjord panorama, story layout, fall recovery, and Rattle encounter in a human playthrough, then replace the remaining TEMP presentation assets with authored art. The current benchmark and evidence belong in `docs/ODYSSEY_SCORECARD.md`; keep all categories below 90 until a player-facing review supports a higher score.
