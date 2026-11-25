## Iteration 2238 — Warden's delivered slam needs repair before production

A 19-frame normalized-time sweep of the existing Warden `Jump_and_Slam_Back_Down` clip places the likely landing transition near 0.70, but the weighted-foot proxy passes up to 3.92 cm below the Idle floor reference at 0.80. The rendered pose reads as a long tumble and folded, face-obscured settle rather than a crisp planted stomp. This is still an unintegrated asset; the proxy does not prove collision. Next: compare one review-only grounded crease-stomp against the delivered motion. Capture and measurements: `Logs/score-comparison/perforation-warden-slam-sweep-iter2238/`.
## Iteration 2237 — Perforation Warden has usable-looking actions but unresolved contact

The existing paper Warden should be reviewed before another paper enemy is commissioned. Unity imports its delivered character/action/walk/run FBXs as **Generic** (not Humanoid), with matching same-hierarchy bindings: seven action clips 25/25 and locomotion 23/23. The isolated 18-pose sheet shows distinct Alert, Attack, Slam, Hit, Dead, and Stand Up silhouettes. Yet Run raises a weighted-foot proxy 32.75 cm, and the chosen normalized 0.52 Slam sample is still 27.9 cm above the Idle floor proxy. This is not proof the action cannot work; it means the contact frame has not yet been found. No production integration, gameplay, collision, build, or human play. Evidence: `Assets/SpaceAndCrafts/Art/Enemies/PerforationWarden/README.md` and `Logs/score-comparison/perforation-warden-review-iter2237/`.
## Asset runway update — 2026-11-16 (Iteration 2234)

A read-only roster cross-check found that the craft-tool enemy categories already have candidates, but remain unevenly realized: **Grand Snip** has a clear one-piece scissors-boss concept and Hinge Muffler encounter, while the older Meshy model is a different humanoid design; **Great Paste Pot / Captain PuddlePaste / Gloopstick** already cover glue concepts; **Perforation Warden / Ream Keeper** already cover paper guardians. Prefer reviewing and integrating an existing candidate before commissioning another near-duplicate. See `Assets/SpaceAndCrafts/Art/Enemies/GrandSnip/README.md`, `Assets/SpaceAndCrafts/Art/Enemies/PerforationWarden/README.md`, and their existing local assets. This is a roster decision only, not new gameplay or a score gain.

The connected Meshy account currently reports 0 credits and Usage is 403, so generation is unavailable on this connection. The user's reported website pool remains unreconciled; do not infer an available balance from old receipts.
## Latest concept — Iteration 2025: Vitrail, the Windowlight Keeper

A stained-glass suncatcher boss concept and front/profile/back turnaround are saved under `Assets/SpaceAndCrafts/Art/Enemies/VitrailWindowlightKeeper/Concept/`. The proposed vellum/color-film → diffuser-frame → beam-crossing → recovery-counter loop is design-only. This is not a Meshy model or gameplay feature. Balance check: 0 connected credits; Usage 403; no credits spent. README records remaining gates.

## Latest tentative ally — Iteration 1908: Squat Mail-Runner

No prior Squat art was in the repo. A tentative model now exists with Meshy-reported rig, free Walk/Run, and six action clips. Thumbnail inspected; faint cyan body mark. Squat's visual, personality, dialogue, and role are not canon; no animation playback, grounded contact, skinning, Unity import, mailbox interaction, story reward, or production integration is verified. Cost 44. Connected balance 2,918 (1,198 monthly + 1,720 permanent), checked 2026-11-11 04:29 UTC; Usage 403, website-expiring balance unresolved. Candidate only; no ratings change; 90/100 unmet. See Iter1908 README/tracker.

## Latest home props — Iterations 1905–1907

Star Scrap Chest has concept, T2.5 draft, Meshy 7.1 model, N1 retexture, and 30k remesh (20.23 MB → 4.82 MB); lid/texture imperfections remain. Orbit Postbox has textured Meshy 7.1 model plus 30k remesh (62.15 MB → 10.12 MB); thumbnail inspected, flag edge-on. No storage/mail interactions, Squat notes, inventory rewards, home scene placement, Unity import, build, test, or play verification. Combined cost 101 credits. Connected balance 2,962 (1,242 monthly + 1,720 permanent), checked 2026-11-11 04:21 UTC. Usage 403; separate website expiry unreconciled. Candidates only; ratings unchanged; 90/100 unmet. See Iteration 1905–1907 READMEs/tracker.

## Latest candidate — Iteration 1904: Punchline the Hole-Puncher

Concept, textured model, Meshy-reported genuine rig, free Walk/Run and six-action bundle are local. Model preview inspected; one small dark handle artifact. Animation playback, ground contact, skinning, scale, Unity import, AI, telegraph, encounter, and repair-patch mechanic remain unverified. Cost 41; current connected balance 3,063 (1,343 monthly + 1,720 permanent), checked 2026-11-11 03:55 UTC. Usage endpoint 403; website-expiry amount unresolved. Candidate only; no ratings change; 90/100 remains unmet. See Iter1904 README/tracker.

## Previous candidate — Iteration 1903: Mica the Moon-Rabbit Cartographer

Local concept/model, genuine humanoid rig, free Walk/Run, and six-action same-rig bundle. Concept/unrigged thumbnail inspected, but playback, planted feet, skinning, Unity import, AI, and safe-route cartography are not verified. Dark cheek artifact; face count not measured. Candidate only; 41 credits.

## Latest candidate — Iteration 1902: Cosmic Journey Keepsake

Textured shadowbox FBX/concept are local. No Unity import, wall placement, collision, or boss-unlock/star-reward logic. Candidate only; 33 credits; balance 3,145 (1,425 monthly + 1,720 permanent), checked 2026-11-11 03:41 UTC; Usage 403 and website expiry unverified.

## Latest candidate — Iteration 1901: Washi Star-Spider

Textured PBR FBX and three-view concept are local. Preview is cohesive but does not clearly show all eight intended legs; no rig, locomotion, foot contact, or enemy behavior. Candidate only. Cost 33; connected balance 3,178 at 2026-11-11 03:35 UTC; Usage 403 and website-expiring amount unresolved.

## Latest candidate — Iteration 1900: Snip-Snap Sellsword

Local concept/model, rigged FBX, free Walk/Run, and six-action FBX bundle. The unrigged model thumbnail looks cohesive. No rigged-preview review, animation playback, foot contact, import setup, AI, or scene integration; face count not measured. 41 credits. No score change; no build/test/render/human play.

## Iteration 1897 — Kiri Foldstar candidate only

Textured 31,003-face GLB, genuine 1.2 m humanoid rig, free Walk/Run, and six same-rig action clips are local. Playback, foot contact, skinning, Unity import, ally behavior, causeway repair, production placement, build, test and human play remain unverified. See candidate README and tracker.

## Iterations 1892–1896 — candidates only

- **StarSeal Press:** textured FBX/PBR maps and inspected thumbnail are staged. Its star came out turquoise, and its paper tag obscures the stamp bed. No interaction, collectible card, stable tabletop placement, import, or production use.
- **Snip-Snap Safety Shears:** Iter1894 keeps the scissors as one connected non-humanoid model, with no added feet. A few blade marks are visible in the preview. No rig, hinge animation, enemy AI, telegraph, counter window, or scene integration. Iter1893's detached-foot version is rejected.
- **Ribbonwake:** continuous washi-serpent shape with original and brightened textured FBXs; the bright edge trim is clearer, but face and game-scale readability remain weak. No rig, deformer, boss behavior, or arena.
- **Nebula Noodle:** cohesive single-mesh glue creature with textured FBX and inspected preview. No natural deformation/rig, behavior, telegraph, or scene integration.
- See both asset READMEs and `docs/MESHY_CREDIT_TRACKER.md`. Candidate previews are not gameplay evidence; no build, test, or human play.

## Iteration 1891 — Brushstroke Pathfinder candidate only

Locally staged 29,267-face textured remesh, genuine rig, free Walk/Run, and five ally clips. Preview is cohesive but the brush identity is modest. No playback, foot contact, path guidance, AI, or production integration. See README and credit tracker.
## Iteration 1890 — Ribbon Drake remains unfulfilled

A colorful 29,047-face dragon candidate is staged, but the one-piece washi ribbon silhouette was not produced. No creature rig/actions or gameplay behavior. Candidate only; custom spine/wing rig and clearer paper detailing would be required before production review. See README and credit tracker.
## Iterations 1888–1889 status

Staple Hopper: two failed/rejected generated forms; no rig, animation or production enemy. Orbit Supply Caddy: textured rocket decoration candidate with inspected preview; it does not visibly provide storage, and scale/placement/material import remain unreviewed. Neither changes gameplay. See their READMEs and credit tracker.
## Iteration 1887 — Starstitch Sentry candidate only

The 31,156-face textured candidate has a genuine humanoid rig, free Walk/Run and six same-rig clips. Preview is coherent but the intended colorful stitching is faint; owl/bat read and prominent eyes need review. No skinning/playback, foot contact, combat tell, AI, arena or production integration. See its README and credit tracker.
## Iteration 1886 — Arc Scout candidate only

Arc Scout has an inspected refined concept/model preview, textured FBX/GLB, measured 286,910-triangle remesh, genuine rig, free Walk/Run and five same-rig action clips. Skinning/playback, sole contact, scale/camera readability, path guidance behavior, AI, and scene integration remain unverified. It does not yet guide the player in production. See its README and `docs/MESHY_CREDIT_TRACKER.md`.
## Iteration 1885 — Rocket Patch Quilt candidate only

Textured static quilt candidate has an inspected preview and a measured 30,913-triangle GLB, with minor dark edge artifacts. Hanging, world completion unlock, room placement, and production integration remain unreviewed. See its README and credit tracker.
## Iteration 1884 — Carbon-Copy Mimic candidate only

Carbon-Copy Mimic now has inspected concept/model/remesh previews, measured 283,048-triangle GLB, genuine rig, free Walk/Run, and six action clips. Skinning, playback, ground contact, safe route copying, AI, and combat integration are unverified. This candidate does not yet make production enemies animated. See `Assets/SpaceAndCrafts/Art/Enemies/CarbonCopyMimic/Meshy_Iter1884/README.md` and the credit tracker.
## Iterations 1882–1883 — home props staged only

Orbit Desk Lamp (30,219 triangles) and Fold-Out World Atlas (28,053 triangles) have inspected Meshy concept/model previews and local textured FBX/GLB files. Neither is imported, placed, lit, interacted with, or wired to world-select. Atlas preview partly occludes Mountain Lands. See candidate READMEs and `docs/MESHY_CREDIT_TRACKER.md`.
## Iteration 1881 — Ream Keeper boss candidate only

The paper-stack guardian now has a locally measured 276,580-triangle remesh, genuine humanoid rig, free Walk/Run, and six same-rig action clips. Meshy previews were inspected. No Unity import, skinning/playback review, foot contact, arena, telegraph, boss logic, or production integration. The marked-lane stamp/slam/recovery hook remains a design note. Asset folder: `Assets/SpaceAndCrafts/Art/Enemies/ReamKeeper/Meshy_Iter1881/`; see `docs/MESHY_CREDIT_TRACKER.md`.
## Iteration 1880 status — candidates only

Eraser Pup has a local 250k-target remesh measured at 259,429 triangles, a genuine humanoid rig, free Walk/Run, and four same-rig ally action clips. Its skinning, playback, grounded contact, companion logic, and production integration remain unreviewed. Constellation Star Mobile has a local textured Meshy FBX; scale, attachment, lighting, and room placement are unreviewed. Both assets remain outside production.
## Iteration 1879 asset status — candidate only

SnipSnap Sellsword and Captain PuddlePaste now have local textured Meshy models, genuine humanoid rigs, matching Walk/Run, and role-specific action FBXs. Meshy previews were inspected. Neither is imported or reviewed in the game. These candidates do not yet fix the production enemies' static poses or establish foot contact, attack telegraphs, hit reactions, defeat/recovery, or boss combat. SnipSnap's 250k remesh is a target and its post-remesh count is unmeasured; PuddlePaste measured 257,052 triangles. See docs/MESHY_CREDIT_TRACKER.md.

## Iteration 1878 status update

- Binder Clip Sentinel: 30k-target Smart Topology FBX, N1 texture and inspected Meshy preview are local; genuine rigging is pending. No action clips/AI.
- Craft Memory Garland: 30k-target Smart Topology FBX, N1 texture and inspected preview are local. Scale, wall mounting and actual home-scene placement are unreviewed.
- Paperclip Courier: v2 concept has a friendly dark-violet eye treatment, and the 30k-target T2.5/N1 model task is pending. The v1 concept with cross/screw-like eyes was rejected.
- Models are candidates. None has Unity/gameplay integration.
## Iteration 1877 update — Pencil Comet and Marquis now rigged

The 1876 notes below describe the initial unrigged state. Iter1877 has since produced a 150k-target GLB remesh and a successful Meshy humanoid rig for both Pencil Comet and Paper-Jam Marquis. Their genuine rigged FBXs and Meshy's free matching Walk/Run clips are local; matching Idle clips have also succeeded and been downloaded. Two role-specific action bundles remain IN_PROGRESS (95% on last poll). None has been imported or played in Unity, and no AI, boss loop, hitbox, or encounter integration is complete.
## Iteration 1876 additions — review-only candidates

- Pencil Comet: textured Meshy FBX and PBR maps. Distinct pencil-bodied humanoid enemy candidate; no rig, actions, AI, or encounter integration.
- Paper-Jam Marquis: textured Meshy FBX and PBR maps. Folded-paper boss candidate; no rig, attacks, arena, or boss loop integration.
- Kite Manta: textured Meshy FBX and PBR maps. Friendly kite-shaped flying ally candidate; no flight rig/animation or guidance behavior.
- Orbit Supply Caddy: textured Meshy FBX and PBR maps. Rocket-shaped home/workshop storage prop; scale, mounting, child-safe tool detail, and in-room placement unreviewed.
- All four FBXs are large (about 45.69–150.03 MB each); optimization/import review is open. They are not production assets yet.

# Missing / TEMP assets

Document needed art instead of faking finished models with primitives. `Art/Commissioned/` contains Meshy models/prefabs used by Arrival, while newer Iter1870–1875 gateway/causeway/spider/Vesper/Splat animation candidates are staged outside production gameplay. These assets are not a finished art set, and most still need in-game review, authoring, or compatible animation.

**Commercial bar check (1M copies @ $50):** NOT met. Organic stand-ins + procedural voice/BGM/skeleton are mid-dev scaffolding, not shippable Nintendo-tier product.

| Asset | Status | Needed for | Brief |
|---|---|---|---|
| Splat hero model + rig + animations | Production Arrival still uses the Blender-authored fused skinned Splat and pose driver. Meshy Iter1869 has the approved design, genuine Generic rig, Walk/Run, and ten actions; Iter1875 adds ten catalog actions and one custom invention-celebration retarget, all ReviewOnly. The custom Iter1869 jump and Iter1875 celebration retarget both fail isolated support-height review; several new catalog actions penetrate the diagnostic support by 9–18 cm. | Final player | Verify selected same-rig Idle/Run/Jump/Land clip transitions against the real motor and sole contact over varied platforms before replacing production art |
| Splat character VO | Partial (`CharacterVoicePlayer`) | Personality | Commissioned VO for jump/land/collect/craft/hurt/complete |
| Vesper in-world presence | Existing 2D portrait remains the production reveal. Meshy Iter1872 has a textured biped, valid Generic rig, free Walk/Run, five catalog clips and a custom map-pointing clip in ReviewOnly. The current presenter shows the portrait for only about 1.8 s before destroying it. | Story payoff | Deliberately extend the reveal and add an optional prefab with sprite fallback; review timing, facing and signal pulses in a gameplay capture before promotion |
| Per-world BGM + stingers | Partial (`WorldMusicDirector`) | Mood | Authored score per world + craft/boss/gateway stingers |
| Environmental ambience | Procedural wind/water bed added for Arrival; authored field ambience still missing | Arrival immersion | Replace/augment the quiet procedural bed with authored fjord wind, distant waterfalls, and soft shoreline wash; loop cleanly beneath music and dialogue |
| Boing-O-Matic authored prop set | Meshy model/prefab exists; craft parts and assembly presentation remain procedural | Craft fantasy | Cohesive authored parts + assembled launcher silhouette |
| Rainbow World modular env kit | Partial Meshy trees/rocks, panorama/material stand-ins, procedural dress/backdrop, and Iter1870/1874 gateway/causeway candidates; these new pieces remain staged rather than used in the Arrival scene. | Arrival art | Compose and review a cohesive authored zone kit, terrain/lighting blend and scenic landmarks in the actual game view |
| Rainbow Causeway route surface | Iter1874 broad slate-lavender slab candidate has a relatively clean top in Meshy's preview; no Unity route staging or seam/scale review yet. | Main foreground walk surface | Compare in a route-only scene with the original support collider unchanged; verify seams and sole contact before substitution |
| Rainbow Gateway landmark | Older Meshy gateway prefab is in use; Iter1870 adds a cohesive cloudstone arch candidate with clear opening and integrated feet, not yet substituted. | Opening vista | Compare both in-scene; preserve the current threshold/collider/camera hooks while reviewing dormant/awake state treatment |
| Rainbow Crystal Spider | Iter1871 introduces a polished eight-legged, single-body static FBX candidate. No spider-compatible rig has been produced; Meshy's available rig tool only accepts humanoids. | Enemy readability | Import as visual-only candidate, then author/test an eight-leg rig or keep the existing behavior mesh until a compatible rig exists |
| ScrapMite / ScrapStorm finals | Meshy models/prefabs are present, but the spider Iter1871 candidate is unrigged and current enemy action/grounding behavior remains incomplete. | Enemy readability | Use compatible rigs with expressive tells, attack anticipation, hit, defeat, and recovery; do not describe static meshes as animated |
| Collectible icons / props | HUD now shows named part chips with color markers; bespoke icons remain missing | Pickup clarity | Distinct item silhouettes and icons that match the in-world pickup models |
| Runtime UI typography | Atkinson Hyperlegible regular/bold is bundled under the SIL OFL and used by runtime labels; it improves readability but does not finish the large panel system | Interface identity and scanability | Art-direct type scale, spacing, and component hierarchy around a cohesive field-note HUD |
| Authored VFX/SFX pack | Missing | Personality | Replace TEMP procedural |
| Title / home menu art | Partial (`HomeMenu` scene) | Packaging | Authored title + world select |
| Timeline cinematics | Missing | Narrative | Opening / boss / gateway cutscenes |

## Current mid-dev stand-ins

- Bone hierarchy Splat + action poses (Idle/Run/Jump/Land/Interact/Craft)
- Encounter mite packs escalate across worlds; boss hits escalate 3→6
- Procedural voice + BGM pads; HomeMenu build index 0
- ImageGen-created Arrival fjord panorama at `Resources/Environment/ArrivalNordicFjordPanorama.png`, loaded as a runtime skybox with the procedural sky as fallback; it is an environment stand-in pending rendered review, not a substitute for an authored modular kit
- ImageGen-created slate albedo at `Resources/Environment/ArrivalSlateCliffAlbedo.png`, applied to the runtime fjord ridges with mirrored UV wrapping; it is a terrain-material stand-in pending rendered review
- ImageGen-created, softened fjord-water albedo at `Resources/Environment/ArrivalFjordWaterAlbedo.png`, applied to the animated catch surface with mirrored UV wrapping and a 22-unit repeat; brightness, blend, and repetition need rendered review
- Meshy FBX/glTF models and URP prefabs in the `Commissioned` directory; this directory name does not mean the models are commissioned final art
- Organic inventions/enemies/landmarks/horizons and procedural combat tells; final authored rigs, environment kits, and VFX remain open

## Commission priority

1. Splat rig + animations + VO  
2. Music + environmental ambience + SFX pack  
3. Arrival modular kit + Gateway  
4. Enemies + inventions FBX swap  
5. Title/cinematics  

Evidence: systems smokes green; commercial 1M@$50 bar still open. See `docs/TEN_WORLDS_PLAN.md`.

## Historical Meshy library and credit gate — superseded by Meshy use on 2026-11-10

This subsection is retained as an audit trail only. It predates the approved Meshy asset work and should not be read as the current authorization state. The current connected balance and spend receipts are in `docs/MESHY_CREDIT_TRACKER.md`; usage remains unavailable through the API (403), so the website's reported expiration timing is not independently confirmed.

- A read-only query across image-to-3D, multi-image-to-3D, rigging, and animation returned one account model: **Splat Character 3D** (GLB), succeeded 2026-08-28 with 0 consumed credits. No rigging or animation result is available. Its output URLs expired 2026-08-31; a free download now returns 403. The current custom skinned player visual is sourced from a Blender candidate, while the separate static Meshy `Splat_Hero` is intentionally removed at runtime because its first-shot appearance was judged creepy/lanky.
- `meshy_check_balance` reports 2,935 monthly credits and 20 permanent credits available, with the next monthly reset at 2026-11-10 21:11 UTC. This is current balance, not verified monthly allowance.
- `meshy_list_usage` returned 403 because this account endpoint is limited to Studio/Enterprise API keys. The monthly allowance and current-cycle usage therefore remain unverified.
- No credits were spent during this check. Do not invoke paid rigging or generation until the monthly allowance and current-cycle usage are known and the user confirms the chosen model and cost; keep total monthly usage below 80% of the allowance.









## Iterations 1898–1899 — staged, not integrated

- **Starfold Workbench:** textured FBX and concept saved; front thumbnail shows a coherent table silhouette but not the work surface. Current WorkshopHub cube remains until top-view inspection, dimensions/material/collider setup, placement, and interaction are addressed.
- **Great Paste Pot:** cohesive glue-bottle concept and textured smart-topology FBX saved; preview inspected. It has no rig, squeeze/ooze deformation, attacks, hit/defeat/recovery, arena, or gameplay. Model face count not checked.
- Combined spend **51** credits; connected balance 3,252 (1,532 monthly + 1,720 permanent), checked 2026-11-11 03:16 UTC. Usage endpoint 403; website-expiring figure remains unresolved. Candidate assets do not justify a score change.

## Latest Meshy candidates — Iterations 1909–1915 (2026-11-11 05:11 UTC)

- Prism Moth is a folded-paper secret-finder ally; model preview inspected, but wing rig/AI/sound are missing.
- Threadwright is a sewing-machine boss candidate with 29,651 measured triangles; its 45.56 MB remesh still has thread crossing the body and a scrap platform. No rig or boss behavior.
- Hand-crank Shadow Story Theater keeps all three world layers in a 22,201-triangle remesh; no crank interaction or progression.
- Feed-Slot Shredder Warden is a 23,607-triangle cute shredder character; humanoid rig attempt failed Meshy's pose estimation (422), no animations.
- Peelback Sticker Bandit is a 23,098-triangle one-piece sticker; peel deformation/behavior are missing.
- Patchshell is a 25,150-triangle quilt tortoise; no quadruped rig, and landing-pad shell flap is only a hook.
- Orbit Workshop Organizer remesh measures 24,362 triangles but is 40.55 MB; tall handle obscures some tray compartments. No placement/storage logic.

These candidates add visual variety but do not change current gameplay or reduce the production gaps below. User's Splat art gap remains most important: Iter1869 is the strongest approved-design Meshy candidate, yet remains ReviewOnly until grounded animation and varied-platform contact are demonstrated. New assets must be integrated selectively; do not keep adding visuals without playable behavior.

## Iteration 1916 — Threadwing candidate

Textured 24,368-triangle/3.78 MB model and concept are staged. Spool-wing silhouette is readable, but the far end of its thread loop is not visibly attached. Safe-route guide behavior and wing motion are ideas only; no compatible rig, audio, AI, integration, or test.

## Iteration 1917 — Starfold Waystation candidate

Rainbow Kingdom now has a textured low-poly shelter candidate (23,601 triangles, 4.73 MB), with a broad floor and visible folded rainbow ribs. The model preview shows a dark interior. It is not integrated and has no checkpoint/save/respawn or safe-zone logic; in-game placement, scale, and contact remain unverified.

