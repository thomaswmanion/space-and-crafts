# Playable Loop Scorecard (player-visible, ignore unfinished placeholders)

> **Historical snapshot — Iteration 14, 2026-10-11.** The scores, “pixel gap” notes, and screenshots below describe that earlier build only; they are not current assessments of the Arrival scene. Use `docs/ODYSSEY_SCORECARD.md` for the current four-category scorecard and `TODO.md` for active work. The Iteration 14 captures remain useful only as evidence of that build.

**Rule:** Score only what a human sees and understands on screen. Do **not** credit smoke tests, wiring validators, or proxy Galaxy cards. Gate = every axis ≥ **90**.

| Axis | Iter 12 | Iter 13 | Iter 14 (pixels) | Target | Why |
|---|---:|---:|---:|---:|---|
| Home menu / new-game slots | 84 | 88 | 88 | 90 | Untouched this iter (Meshy title stage + Our Way Galaxy remain) |
| Opening story beat | 82 | 82 | 82 | 90 | Untouched |
| Arrival playable feel | 80 | 84 | **87** | 90 | Walk-through Meshy gateway owns first viewport; Meshy Splat_Hero reads; soft-blob renderers off. ShelfFloor still flat magenta; yellow sky wash; far magenta sphere peeks |
| Meshy assets in play | 82 | 86 | **89** | 90 | Playable `Splat_Hero` + ~9.6–10.2m path gateways + denser Tree/Rock on islands (21). Not 90: ShelfFloor slab + gothic gateway read still compete with “living craft” fantasy |
| Levels playable | 78 | 82 | **86** | 90 | Spawn→path through gateway clearer; mid islands dressed/hidden. Side island slabs + ShelfFloor still graybox |
| Story clarity at start | 82 | 82 | 82 | 90 | Our Way Galaxy + coach — untouched |
| UI / HUD clarity | 80 | 81 | 81 | 90 | Functional bars — untouched |
| **Overall (honest)** | **~81** | **~84** | **~87** | **90** | Three Meshy levers landed with pixel evidence. **Not 90.** |

## Pixel gap to 90 (exact)

| Axis | Gap | What still loses player pixels |
|---|---:|---|
| Home | **−2** | Specular blow on Meshy hero; primitive title gateway bands |
| Arrival feel | **−3** | Flat ShelfFloor magenta slab + yellow wash; far magenta sphere |
| Meshy in play | **−1** | Gateway reads dark gothic more than rainbow craft; floor slab under feet |
| Levels | **−4** | Side islands / ShelfFloor still unmasked graybox |
| Opening story | **−8** | Card UI unchanged |
| UI / HUD | **−9** | Functional bars, not craft chrome |

## Next non-recolor lever

1. **Dress or break ShelfFloor** so the spawn pad is not a flat magenta slab under Meshy Splat.
2. **Kill remaining far placeholder spheres / yellow wash** that still peek past the gateway.
3. **Home specular + story/HUD craft chrome** (untouched axes still below 90).

## Iteration log

### Iter 12 — Meshy path dress + Organic silhouettes → ~81
### Iter 13 — 2026-10-11 (hide slabs, gateway landmarks, Home Meshy, exposure)
- Honest overall **~84**. Still short of 90.
### Iter 14 — 2026-10-11 (playable Meshy Splat, walk-through gateways, island Tree/Rock)
- Levers (existing Meshy only, no new generation):
  1. `ApplyMeshySplatHero` — `Splat_Hero_URP` under `Splat_TEMP`; motor/collider kept; soft-blob renderers disabled only when Meshy reads; `SplatVisualRepair` skips soft-blob revive when Meshy present.
  2. Walk-through `ART_PathGatewayLandmark_*` closer/larger (~9.6–10.2m) on path; pink PathArch not planted; far `GatewayArchTube` / clay blobs hidden.
  3. Mid-path islands (Glade/Workshop/PaintPool/Nurseries/VistaLedge) hide slab MeshRenderers + denser Tree/Rock Meshy (21 instances).
- Evidence (`Logs/iter14/`):
  - `arrival_first_viewport.png` — Game View: Meshy gateway owns frame; Meshy Splat in cyan ring; coach readable; ShelfFloor still magenta.
  - `splat_meshy_hero_close.png` — Meshy Splat_Hero with goggles (not soft-blob); soft-blob `ART_SplatBody.enabled == false`.
  - `midpath_islands.png` — Tree line through gateway; island dress present; side slabs still visible.
- Honest overall **~87**. Still short of 90 — see gaps + next lever.
