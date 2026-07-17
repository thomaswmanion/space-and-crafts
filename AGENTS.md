# Space & Crafts Agent Guide

## Project goal

Build **Space & Crafts** into an original, highly polished 3D mascot platformer. The long-term quality target is the responsiveness, readability, charm, density, and finish associated with top-tier 3D platformers, while keeping the characters, mechanics, worlds, music, and presentation legally and creatively distinct.

The authoritative production backlog is [TODO.md](TODO.md). Read it before making substantial changes and update it whenever work completes, scope changes, or new requirements are discovered.

## Current priority

Do not spread effort evenly across the entire imagined game. First produce a final-quality 10–20 minute **Rainbow Kingdom vertical slice** containing:

1. Splat's production-quality movement, camera, animation, combat, invisibility, phasing, and water gun.
2. A polished outdoor route with exploration, platforming, enemies, secrets, collectibles, and strong visual guidance.
3. Several production-quality enemy families, including robots, Doom Orbs, scissors, and pencils.
4. Rainbow Castle, a memorable robot boss battle, the visible Rainbow King hostage rescue, and the spaceship unlock.
5. Final-quality UI, audio, VFX, accessibility basics, saving, performance, and WebGL stability for this slice.

Only scale production into Worlds 2–5 after the vertical slice meets its acceptance criteria in `TODO.md`.

## Working rules

- Treat `TODO.md` as a living plan. Mark tasks complete only when implemented and verified.
- Prefer completing the highest-priority unblocked vertical-slice task over adding unrelated breadth.
- Preserve the original child-created character concepts and their playful handmade personality.
- Never copy protected characters, levels, music, dialogue, branding, or distinctive assets from other games.
- Replace prototype primitives with authored assets incrementally; do not break playable functionality while upgrading presentation.
- Keep gameplay systems data-driven, testable, and separated from editor scene-generation code.
- Prefer reusable prefabs and ScriptableObject data over permanently expanding procedural scene-builder methods.
- Maintain keyboard, mouse, and controller support.
- Keep WebGL playable, but document features that require a higher-fidelity desktop/console target.
- Target stable 60 FPS on the agreed reference hardware and test changes proportionally to their risk.
- For gameplay changes, verify compilation, regenerate affected scenes when required, build WebGL, and smoke-test the browser build.
- Do not claim top-tier production quality based only on more primitives or effects. Judge quality by control feel, authored art, animation, level design, audio, usability, and polish together.
- Record newly discovered bugs and production tasks in `TODO.md` instead of leaving them implicit.

## Definition of done

A task is complete only when its implementation exists, works in its intended context, has no known blocking regressions, and has been tested at the appropriate level. A generated scene or successful compile alone is not sufficient for player-facing features.

