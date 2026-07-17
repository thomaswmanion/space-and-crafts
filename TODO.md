# Space & Crafts Production TODO

This is the living production backlog for turning the current Unity prototype into an original, commercial-quality 3D platformer. Check items only after implementation and appropriate verification.

## Status legend

- `[ ]` Not completed
- `[x]` Completed and verified
- `P0` Required for the Rainbow Kingdom vertical slice
- `P1` Required for the full game
- `P2` Optional, stretch, or post-launch

## Current prototype inventory

- [x] Unity 3D project and URP foundation
- [x] WebGL development build pipeline and local test server
- [x] Home screen and level transition
- [x] Third-person Splat prototype with movement and jumping
- [x] Prototype punch, stomp, invisibility, phasing, and water gun
- [x] Prototype crystals, enemies, boss, Rainbow King rescue, and spaceship gate
- [x] Prototype robot, Doom Orb, scissor, and pencil enemies
- [x] Prototype space backdrop, stars, comets, shooting stars, robots, and lasers
- [x] Rainbow Kingdom and Rainbow Castle blockout
- [ ] Replace prototype presentation and architecture with production-quality systems and authored assets

## Milestone 1 — P0 vertical-slice planning

- [ ] Lock the 10–20 minute vertical-slice experience
- [ ] Define its beginning, teaching sequence, climax, boss, rescue, and ending
- [ ] Write measurable acceptance criteria for gameplay, art, audio, performance, and stability
- [ ] Define target audience and desired age rating
- [ ] Define lead platform and minimum WebGL hardware/browser targets
- [ ] Establish frame, memory, download-size, draw-call, and loading budgets
- [ ] Produce a complete vertical-slice dependency map
- [ ] Estimate work and identify required disciplines or contractors
- [ ] Establish milestone reviews and playtest schedule
- [ ] Create a feature-cut list for schedule protection

## Creative direction and worldbuilding

- [ ] Write the complete game-design document
- [ ] Define the game's unique pillars beyond its inspirations
- [ ] Define Splat's unique movement and ability identity
- [ ] Establish universe rules and tone
- [ ] Finish the complete story outline
- [ ] Write biographies for Splat, Scrap-Bot, Sir Scribbles, Rainbow King, General Snip, and the Scissor Crew
- [ ] Define villain motivations and character relationships
- [ ] Define all five worlds and their gameplay identities
- [ ] Define final level, boss, crystal, and collectible counts
- [ ] Create visual, animation, VFX, UI, and audio style guides
- [ ] Establish quality benchmarks and review checklists
- [ ] Conduct an originality and IP-distinction review

## P0 player controller and traversal

- [ ] Refactor the controller into production-ready states
- [ ] Finalize walk, jog, run, acceleration, deceleration, and turn feel
- [ ] Add analog input and controller dead-zone tuning
- [ ] Finalize slopes, stairs, ground snapping, and moving-platform behavior
- [ ] Add reliable ledge and edge correction
- [x] Add basic coyote time and jump buffering
- [ ] Finalize variable jump height, apex behavior, fall speed, and landing recovery
- [ ] Add the final advanced traversal move set
- [ ] Add ground pound and bounce chaining
- [ ] Add wall and ledge interactions if retained in scope
- [ ] Finalize knockback, recovery, hazards, death, and restart behavior
- [ ] Add surface-aware footsteps and movement feedback
- [ ] Add input remapping and gameplay assists
- [ ] Complete repeated movement-only playtests and tuning

## P0 camera

- [ ] Replace the prototype follow camera with a production camera system
- [ ] Add smooth manual mouse and controller rotation
- [ ] Add sensitivity, inversion, and recenter options
- [ ] Add collision avoidance and obstruction transparency
- [ ] Add camera zones and landmark framing
- [ ] Add boss, aiming, tight-space, and cinematic camera modes
- [ ] Add speed-sensitive field of view and restrained impulses
- [ ] Prevent clipping, sudden rotation, motion sickness, and lost-player framing
- [ ] Perform a camera failure pass over the entire vertical slice

## P0 Splat abilities and combat

- [ ] Finalize ability rules, costs, cooldowns, and upgrade possibilities
- [ ] Build polished punch anticipation, combo, recovery, hitboxes, and hit-stop
- [ ] Add directional and aerial attacks if retained
- [ ] Finalize stomp targeting, bounce, and enemy reactions
- [ ] Finalize invisibility duration, cooldown, feedback, and enemy response
- [ ] Finalize invulnerability rules and communicate them clearly
- [ ] Clearly identify phase-compatible walls
- [ ] Prevent unintended phasing and sequence breaks
- [ ] Add polished water-gun aiming and firing
- [ ] Define ammunition, pressure, recharge, or cooldown rules
- [ ] Add wet reactions and water/environment interactions
- [ ] Add attack buffering and responsive cancel rules
- [ ] Add combat tutorials through level design
- [ ] Balance combat for children and experienced players

## Character art

- [ ] Turn the original drawing into professional concept sheets
- [ ] Create front, side, back, expression, color, and scale references
- [ ] Model, retopologize, UV, and texture production-quality Splat
- [ ] Create a custom Splat shader and facial controls
- [ ] Model Scrap-Bot and Sir Scribbles
- [ ] Model production Rainbow King and General Snip
- [ ] Create final robot, Doom Orb, scissor, pencil, alien, and UFO families
- [ ] Create clear silhouettes and variants for each family
- [ ] Create damage states and boss phase variants
- [ ] Create LODs and platform-appropriate model variants
- [ ] Validate readability at gameplay-camera distance

## Character animation

- [ ] Build production rigs and deformation tests
- [ ] Create Splat idle, walk, jog, run, start, stop, and turn sets
- [ ] Create jump anticipation, rise, apex, fall, and landing sets
- [ ] Create advanced traversal animations
- [ ] Create punch, combo, stomp, water-gun, invisibility, phase, damage, defeat, respawn, and celebration animations
- [ ] Add animation blending and state transitions
- [ ] Add foot placement, look-at, facial, and secondary motion systems
- [ ] Eliminate foot sliding, popping, and hand/prop mismatch
- [ ] Create full movement, attack, telegraph, reaction, and defeat sets for every enemy
- [ ] Create complete boss and cinematic animation libraries

## Enemy systems and encounter design

- [ ] Refactor enemy behavior into reusable state machines
- [ ] Add patrol, awareness, investigate, chase, attack, retreat, stagger, and defeat states
- [ ] Add reliable navigation, obstacle avoidance, ledge safety, and unstuck handling
- [ ] Standardize hitboxes, hurtboxes, telegraphs, counters, and damage feedback
- [ ] Add ranged, support, flying, armored, ambush, and environmental roles
- [ ] Give robot, Doom Orb, scissor, and pencil families distinct mechanics
- [ ] Add coordinated group behavior without unfair overlap
- [ ] Create encounter composition and difficulty rules
- [ ] Add accessible enemy audiovisual tells
- [ ] Test every Splat ability against every enemy family

## P0 Rainbow Warden boss and King rescue

- [ ] Write a complete boss design with a unique central mechanic
- [ ] Create final boss model, rig, materials, VFX, and audio
- [ ] Add cinematic introduction and skippable transition
- [ ] Implement multiple readable phases
- [ ] Add boss arena transformations and phase checkpoints if appropriate
- [ ] Add a production boss HUD
- [ ] Make the boss's control of the King visually and mechanically explicit
- [x] Link the prototype boss defeat to the prototype energy-cage release
- [ ] Create the final Rainbow King cage, struggle, rescue, and celebration sequence
- [ ] Transform the castle state after rescue
- [ ] Unlock the spaceship through a clear interactive event
- [ ] Add retry flow, exploit prevention, and softlock recovery
- [ ] Run boss comprehension and difficulty playtests

## P0 Rainbow Kingdom level design

- [ ] Replace the blockout with a documented critical path
- [ ] Introduce movement and abilities one concept at a time
- [ ] Develop and combine mechanics across the route
- [ ] Add optional exploration, secrets, challenge rooms, and speed routes
- [ ] Add meaningful platforming rather than flat traversal
- [ ] Add authored combat arenas and pacing breaks
- [ ] Use landmarks, light, color, motion, and composition for guidance
- [ ] Add fair hazards and recovery opportunities
- [ ] Place collectibles to reward mastery and curiosity
- [ ] Add environmental storytelling about the invasion
- [ ] Test navigation without developer explanation
- [ ] Record completion times, deaths, confusion points, and missed mechanics

## P0 Rainbow Castle

- [ ] Design a cohesive, explorable castle layout
- [ ] Give every room a gameplay and narrative purpose
- [ ] Add doors, staircases, elevators, passages, shortcuts, and secrets
- [ ] Create polished interior/exterior transitions
- [ ] Add castle NPCs and evidence of the takeover
- [ ] Build a readable route to the boss
- [x] Make the prototype Rainbow King visible in the boss arena
- [ ] Foreshadow the King before the final encounter
- [ ] Add post-rescue castle changes and NPC reactions
- [ ] Complete collision, camera, navigation, and softlock passes

## Environment art

- [ ] Replace procedural primitives with modular authored kits
- [ ] Sculpt and texture production terrain
- [ ] Build Rainbow Kingdom architecture, roads, bridges, gardens, waterways, caves, and ruins
- [ ] Build the final castle exterior and interior kits
- [ ] Create prop, foliage, rock, decal, and set-dressing libraries
- [ ] Add wind and environmental animation
- [ ] Add terrain material blending and vertex-painted variation
- [ ] Build distant vistas and world-specific space skies
- [ ] Add destruction and post-battle states
- [ ] Create LODs, occlusion data, light probes, and reflection probes

## VFX

- [ ] Establish a consistent VFX language and performance budget
- [ ] Add surface-specific movement and landing effects
- [ ] Add attack trails, hit sparks, hit-stop accents, and defeat effects
- [ ] Add water splashes, droplets, mist, wetness, and impacts
- [ ] Add invisibility refraction and phasing distortion
- [ ] Add crystal attraction and collection effects
- [ ] Add boss, cage, rescue, spaceship, and rainbow-travel effects
- [x] Add prototype stars, shooting stars, comets, robots, and background lasers
- [ ] Replace background-space prototypes with polished trails, particles, shaders, parallax, and composition
- [ ] Add weather and atmospheric particles
- [ ] Optimize transparency and particle overdraw for WebGL

## Lighting and rendering

- [ ] Define final lighting keys for every world and interior
- [ ] Add baked or mixed global illumination where appropriate
- [ ] Add reflection probes, light probes, and efficient ambient occlusion
- [ ] Build custom stylized sky and fog rendering
- [ ] Tune world-specific color grading, bloom, and anti-aliasing
- [ ] Add scalable quality presets and WebGL fallbacks
- [ ] Profile shadow cost, overdraw, shader cost, and variant count
- [ ] Validate combat readability under every lighting condition

## Collectibles, progression, and saving

- [ ] Define crystal types, requirements, and narrative meaning
- [ ] Define major, minor, secret, and completionist rewards
- [ ] Add polished collectible visuals, animations, audio, and summaries
- [ ] Build a versioned persistent save system
- [ ] Add autosaving, multiple slots, backup recovery, and corruption handling
- [ ] Add browser-storage support and clear-data warnings
- [ ] Track level, world, boss, crystal, secret, and completion progress
- [ ] Add ability or cosmetic unlocks if retained
- [ ] Add save migration tests

## Story, dialogue, and cinematics

- [ ] Write the complete script and optional NPC dialogue
- [ ] Define character voices and vocalization direction
- [ ] Build dialogue and cinematic systems
- [ ] Create opening, world, boss, rescue, spaceship, and ending cinematics
- [ ] Add contextual dialogue and environmental narrative
- [ ] Add replayable and skippable cinematics
- [ ] Add subtitles and subtitle accessibility options
- [ ] Prepare text for localization
- [ ] Test story comprehension with new players

## UI and UX

- [ ] Replace prototype GUI with a unified authored UI system
- [ ] Create final title, save selection, world selection, HUD, pause, settings, progress, and loading screens
- [ ] Create health, ability, crystal, objective, interaction, and boss displays
- [ ] Add controller glyph switching and remapping UI
- [ ] Add polished transitions and feedback
- [ ] Support keyboard, mouse, controller, aspect ratios, and safe areas
- [ ] Validate font size, hierarchy, contrast, and child readability

## Audio and music

- [ ] Define the original robotic-chiptune musical identity
- [ ] Compose the main, world, level, castle, boss, rescue, spaceship, and ending themes
- [ ] Add dynamic music layers and smooth transitions
- [ ] Create character vocalizations
- [ ] Create movement, surface, combat, enemy, boss, environment, collectible, spaceship, and UI sound libraries
- [ ] Add spatial audio and ambience
- [ ] Mix the game and add music, SFX, voice, ambience, and master controls
- [ ] Add dynamic-range options
- [ ] Conduct originality review for all music and sound assets

## Accessibility

- [ ] Add full control remapping
- [ ] Add hold/toggle alternatives
- [ ] Add camera sensitivity and inversion
- [ ] Add reduced shake, flashing, and motion options
- [ ] Add colorblind-safe non-color cues
- [ ] Add subtitle size, background, and speaker options
- [ ] Add text-size and high-contrast options
- [ ] Add difficulty and gameplay-assist options
- [ ] Consider invincibility or story mode
- [ ] Playtest with players who use accessibility features

## Engineering and tools

- [ ] Separate runtime game systems from procedural scene generation
- [ ] Replace generated gameplay objects with reusable prefabs
- [ ] Store characters, abilities, enemies, encounters, and worlds in data assets
- [ ] Build reusable state, interaction, event, VFX, and audio systems
- [ ] Add object pooling
- [ ] Add additive loading or streaming if required
- [ ] Add automated edit-mode and play-mode tests
- [ ] Add build validation and continuous integration
- [ ] Add debug menus, cheats, telemetry, and bug-reproduction tools
- [ ] Document all major systems
- [ ] Eliminate compiler warnings and obsolete API use
- [ ] Maintain clean builds on every supported target

## Performance and WebGL

- [ ] Define representative test devices and browsers
- [ ] Profile CPU, GPU, memory, network, storage, and loading
- [ ] Eliminate avoidable allocations and garbage-collection spikes
- [ ] Pool projectiles, enemies, VFX, and repeated objects
- [ ] Use batching, instancing, atlases, compression, and LODs appropriately
- [ ] Optimize shaders, transparent effects, particles, lights, and shadows
- [ ] Reduce initial WebGL download size
- [ ] Add loading progress and failure messaging
- [ ] Maintain stable frame pacing and 60 FPS on target hardware
- [ ] Test browser memory limits, tab suspension, focus loss, and storage failure

## QA

- [ ] Create a complete test plan and compatibility matrix
- [ ] Test every ability/enemy/environment combination
- [ ] Test all boundaries, camera zones, collectibles, boss phases, deaths, and restarts
- [ ] Test save/load at every progression state and during interruption
- [ ] Test controllers, browsers, operating systems, resolutions, and frame rates
- [ ] Test accessibility and localization
- [ ] Run child-friendly usability tests
- [ ] Run long-session, regression, and external QA passes
- [ ] Eliminate crashes, softlocks, progression blockers, and save corruption
- [ ] Prioritize polish on frequent player actions

## Full-game production after vertical-slice approval

- [ ] Lock Worlds 2, 3, and 4 themes and mechanics
- [ ] Build the complete spaceship overworld and rainbow travel system
- [ ] Build all remaining levels using vertical-slice standards
- [ ] Build Stretchy Man, The Great Shear, Professor Snipper, and General Snip encounters
- [ ] Build Crystal Town's destroyed and restored states
- [ ] Complete full-game story, cinematics, progression, collectibles, audio, and art
- [ ] Complete all platform optimization and certification work

## Release preparation

- [ ] Finalize name, logo, key art, screenshots, trailer, demo, store page, and press kit
- [ ] Verify trademark and legal requirements
- [ ] Prepare privacy documentation and age ratings
- [ ] Complete platform certification and external QA
- [ ] Complete localization QA
- [ ] Prepare launch monitoring, rollback, patching, and support plans
- [ ] Create a realistic post-launch roadmap

## Vertical-slice acceptance criteria

- [ ] A new player can complete the slice without developer guidance
- [ ] Movement and camera remain enjoyable during repeated play
- [ ] Every critical mechanic is taught, tested, and combined
- [ ] Art, animation, lighting, VFX, UI, audio, and writing feel intentionally authored and consistent
- [ ] The boss, King rescue, and spaceship unlock form a satisfying climax
- [ ] No known crashes, softlocks, progression blockers, or save-loss bugs remain
- [ ] Performance meets the documented WebGL budget on reference hardware
- [ ] Accessibility basics are implemented and tested
- [ ] External playtesters consistently understand the goals, controls, threats, and rewards
- [ ] The team agrees this slice represents the quality bar for Worlds 2–5
