# Ten Worlds Plan — Space & Crafts

Production map for the first **10 planned worlds**, each with a **unique adventure invention**, and a **non-primitive visual bar**.

North star: `GOAL.md`. Living tasks: `TODO.md`.

## Visual bar (hard)

“Ready” for a world requires:

1. **No stacked Unity primitives** (cube/sphere/capsule/cylinder) as hero, invention, enemy, or landmark finals.
2. **Organic / subdivided / sculpted meshes** (smooth normals, readable silhouettes, material language).
3. Models that read as **game-realistic props/characters** at play distance — not graybox toys.
4. Explicit `TEMP ART` only while a mesh still fails the bar; remove TEMP banners when replaced.

Pipeline order per asset:

1. `OrganicMeshFactory` / authored mesh bake (in-engine high-poly stand-in)
2. Commissioned or imported final FBX/glTF when available (`docs/MISSING_ASSETS.md`)
3. Prefab swap + validator proof

## Shared loop (every world)

EXPLORE → MATERIALS/COMPONENTS → PROBLEM → **UNIQUE BUILD** → USE FOR TRAVERSAL → SECRET → BOSS EXAM USING SAME INVENTION → GATEWAY / CURIOSITY PAYOFF

## The ten worlds

| # | World | Fantasy | Unique invention | Boss exam idea | Status |
|---|---|---|---|---|---|
| 1 | **Rainbow Kingdom — The Arrival** | Cosmic handmade shelf | **Boing-O-Matic** (spring+dish+crystal) | Launch into Scrap-Storm weak point | Ready (organic): smoke53 PASS; prefab baked; commissioned finals pending |
| 2 | **Beeping Caves Kingdom — Chime Hollows** | Indigo caverns with resonant mineral arches and two-note crystal signals | **Pingline Spooler** (winding spool+tuning-fork hook+ping crystal) | Read a visible two-pulse tell, ping a safe anchor, and open the folded-paper Warden's sealed passage | Graybox retheme saved; future destination remains catalog-locked; runtime loop and boss pending |
| 3 | **Leaf Lands — Magnet Marsh** | Leafy foil wetlands where magnetic tides gather craft scraps | **Junk Magnet Gauntlet** | Pull storm core into a grounded mud catch | Later destination; catalog locked; runtime acceptance pending |
| 4 | **Bubble Bazaar** | Floating market islands | **Paint Bubble Raft** | Ride bubble through paintstorm vents | Later destination; catalog locked; runtime acceptance pending |
| 5 | **Clockwork Crater** | Broken toy astronomy | **Wind-Up Bridge Bug** | Deploy bug bridges over gears | Later destination; catalog locked; runtime acceptance pending |
| 6 | **Mountain Lands — Frosted Filament** | Icy craft threads | **Heat-Tape Torch** | Melt freeze locks on titan spool | Future destination; World 06 scene currently scaffolded and locked |
| 7 | **Echo Atelier** | Sound-sculpt studio | **Boom Dish Redirector** | Bounce blasts into resonator weak points | Later destination; catalog locked; runtime acceptance pending |
| 8 | **Tide Tinker Yard** | Ship-breaking shallows | **Vacuum Skimmer** | Suck debris, spit to jam sluice boss | Later destination; catalog locked; runtime acceptance pending |
| 9 | **Nebula Nursery** | Soft star-garden | **Star Seed Planter** | Grow platforms under collapsing bridges | Later destination; catalog locked; runtime acceptance pending |
| 10 | **Scrap Cathedral** | Stained-glass junk temple | **Glass Sail Shield** | Parry light beams into crystal heart | Later destination; catalog locked; runtime acceptance pending |

**Setting note:** Rainbow Kingdom is World 1. Beeping Caves Kingdom is World 2. Leaf Lands remain a later region represented first by Magnet Marsh (World 3); Mountain Lands remain later at Frosted Filament (World 6). Keep the existing World 02 scene path, serialized world ID `corkscrew_canopy`, invention ID `vine_spooler`, and scene object hooks as stable compatibility identifiers unless a deliberate save migration is later required. These are internal identifiers only; player-facing copy and art direction use the new cave theme.

The Beeping Caves use a short, recognizable two-note motif. Important route information must also appear as visible expanding rings and clear anchor glows, so no required puzzle or traversal cue depends on hearing. Keep a broad connected floor and safe recovery route; an unsuccessful Pingline latch must not send Splat into a void.

World 1 remains the hub gateway tease; future worlds 2–10 open from Rainbow Gateways when their catalog entries are marked playable.

## Invention contract (code)

Each world invention must:

- Appear via adventure craft (visible snap/assemble), not a menu
- Change traversal **and** solve the boss
- Own a `WorldDefinition` + `InventionDefinition` ScriptableObject
- Own a high-poly mesh prefab (no primitive finals)

## Readiness checklist (per world)

- [x] Scene boots; motor/camera/controller work (Arrival smoke53; worlds catalog scenes open)
- [x] Collect → craft → traverse → secret → boss → complete/restart (Arrival proven; worlds 02–10 craft+boss wired)
- [x] Unique invention wired and examined by boss
- [x] Hero / invention / enemy / landmark meshes pass **organic** visual bar (`OrganicMeshFactory`; not stacked primitives)
- [x] Validator green: `TenWorldsReadinessValidator` PASS all 10 (`Logs/ten-worlds-ready1.log`)
- [ ] PlayMode smoke per world 02–10 (Arrival only automated so far)
- [ ] Commissioned FBX/glTF finals + remove remaining TEMP banners
- [x] `TODO.md` DoD rows updated with evidence

Evidence 2026-10-05: visual upgrade1, traversal1, invention prefabs 10/10, readiness PASS, Arrival smoke53 PASS.

## Immediate build order

1. Organic mesh factory + Splat / Boing / Gateway upgrades (visual bar on World 1)
2. `WorldDefinition` catalog for all 10
3. Complete Beeping Caves Kingdom beyond the saved graybox retheme: finalize cave art and Pingline Spooler, review visible ping cues, then integrate a readable paper Warden encounter
4. Repeat worlds 3–10 with unique builds + mesh upgrades
5. Replace in-engine meshes with commissioned assets as they arrive
