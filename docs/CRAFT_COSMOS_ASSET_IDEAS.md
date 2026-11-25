## Iteration 2228 — prepare existing Sproutlet ally for later-world integration (2026-11-16)

- Audited the existing Meshy Sproutlet, correcting the Unity audit's false Humanoid-Avatar requirement for Generic clips. The skinned mesh has 23 bones; Idle/Listening/Happy Jump/Hit paths match 25/25 and Walk/Run match 23/23. Added a review-only prefab/controller with locomotion blend and one-shot action states.
- This turns an existing rigged candidate into a reusable Unity asset, but it is not yet a game ally: no scene references it, playback/contact are unreviewed, its Jump and Listening clips are 9.83/9.33 seconds, and Leaf Lands is currently blocked by `systemsPlayable: 0`. Avoid placing it until the world is enabled and the animation/contact/camera gate passes. No new concept or Meshy task; connected balance 0 and Usage 403.

## Iteration 2226 — Buttonhole Admiral clean model-input refinement (2026-11-16)

- Saved `Assets/SpaceAndCrafts/Art/Enemies/ButtonholeAdmiral/ButtonholeAdmiral_ModelInput_Iter2226.png` as a sibling reference, preserving Iter2010. The refinement removes the loose coral thread, opens all four eyelets, and broadens the attached feet into visible flat-bottomed contacts. It remains an ImageGen reference, not Meshy geometry or a rigged boss.
- This prepares the already-proposed button boss for a potential future image-to-3D task; it does not justify adding a new encounter or score. Meshy balance is 0 and Usage 403 on the connected account; no task/purchase. See the character README for exact prompt and limitations.

## Iteration 2192 — stop concept expansion; prioritize the existing Sellsword rig

Parallel read-only audits found no distinct craft-enemy/ally/decor archetype gap: the art tree already has 68 enemy folders and broad scissors, paper, and adhesive coverage. That count is folder breadth, not production integration. No new concept or Meshy task was made. Snip-Snap Sellsword is the strongest existing enemy candidate for the next review: the model and same-rig Idle/Walk/Run/Attack/Hit/Dead clips are present, and the Unity review prefab records exact animation transform paths. The bundled “Spin Down and Stand Up” arrives in Unity as `Rightward_Spin` and intersects the review floor by about 34 cm at normalized 0.20; treat that motion as rejected until repaired. Review evidence and limitations: `Assets/SpaceAndCrafts/Art/Enemies/SnipSnapSellsword/Meshy_Iter1900/README.md`.

Fresh Meshy status 2026-11-16 08:39 UTC: **0 total (0 monthly + 0 permanent)**, reset `2026-12-10T21:11:17Z`; Usage 403; no credits spent. Pulpal attack contact still fails, Pomperstein has not had Unity animation/contact review, and the Orbit Postbox conversion remains rejected. No player-facing gain or score change. Next: review continuous Idle→Walk/Run→Attack→Hit→Dead transitions for Sellsword with Spin excluded; promote only if normal-camera readability and support contact hold.

## Iteration 2185 — reuse the Rocket-Patch Quilt in the home workshop

Reused an existing cohesive Meshy-made home-decor asset instead of expanding the already crowded cast: installed `RocketPatchQuilt_30k.fbx` as a static hanging route map in `WorkshopHub`. The quilt shows the Rainbow Kingdom, Leaf Lands, Mountain Lands, and Splat's rocket route in one stitched piece. Its first measured orientation exposed the source FBX's Z-up axis; the corrected Unity instance is vertical and measures 2.20 × 1.90 × 0.16 m. The saved-camera capture is `Logs/score-comparison/workshop-rocket-patch-quilt-iter2185/frame.png`; it is visible and textured, though modest in size and behind a dominant UI overlay. Prefab and report: `Assets/SpaceAndCrafts/Prefabs/HomeWorkshop/RocketPatchQuilt.prefab` and `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/RocketPatchQuilt/UnityIntegration_Iter2185/placement-review.txt`.

Static only: no unlock stars, collection state, interaction, collider, or reward behavior. Unity batch authoring/capture were performed; no player build, runtime test, or human play. No score change. The 30k remesh target is unmeasured. This uses **0 Meshy credits**; connected balance remains 0 and Usage 403. Next: inspect map readability in the normal room flow and keep it only if it does not compete with World Select.

## Iteration 2184 — Pulpal rig-motion review before new concepts

Instead of adding another overlapping monster/ally/decor concept, reused the existing Leaf Lands Pulpal Meshy rig to test the weak attack read found in Iter2180/2183. A Blender 5.1 IK-baked 90-frame press trial and five pose captures live under `Logs/score-comparison/pulpal-meshy-animation-review-iter2180/authored-ik-trial-iter2184/`. The sampled motion gives a clearer raised-arm anticipation → compressed downward press → recovery silhouette, but an all-frame geometry audit measures 4.7–23.2 cm foot/toe-vertex penetration below the reference plane and press hand-bone tails 10.9 cm above it. This candidate fails the contact gate; retain it outside `Assets/` and do not promote. See `REVIEW.txt` and the top of `docs/ODYSSEY_SCORECARD.md`.

Live Meshy check: **0 total credits**, Usage API **403**; no paid tasks or purchase. The expanded connected task list returned no rigging/animation/text-to-motion tasks and does not reconcile the user's reported website pool. Continue useful offline review; don't invent more generic concepts while the cast and decor roster remain saturated.

## Iteration 2183 — Pulpal keyframe trial rejected

Tried a Blender-keyed arm/spine overlay on a copy of Pulpal's existing rig to make the Iter2182 attack board into motion. Five Blender pose samples showed some arm opening, but no readable leaf-lane press or strong impact/recovery; the exported FBX also lost the textured look on reimport. The FBX and .blend were moved out of `Assets/` to `Logs/score-comparison/pulpal-meshy-animation-review-iter2180/authored-trial-iter2183-rejected/`. The failed trial is explicitly not an accepted clip and should not be integrated. This evidence suggests a pose-target/IK approach and Unity import review are needed before another animation pass.

No production change, Unity import, build/test, or human play. Scores unchanged. Meshy balance is 0 and Usage 403; no paid task was submitted.
## Craft-cosmos iteration 2182 — Pulpal Leaf-Lane Press action reference

The Iter2180 captures showed the existing catalog Attack turning side-on and the custom press reading as a mild arm-forward crouch. Added a grounded three-beat reference for anticipation → double-arm leaf-lane press → braced recovery, with unchanged camera and scale. This is a timing/silhouette target, not an animation asset or implementation. It explicitly preserves Splat's gather → build Leaf-Press Wedge → choose/use the crossing loop and avoids a magical projectile/automatic bridge. Image and motion guardrails: `Assets/SpaceAndCrafts/Art/Enemies/Pulpal/Concept/Pulpal_LeafLanePress_ActionBoard_Iter2182.png` and Pulpal README.

ImageGen-only, no Meshy spend, no Unity import/playback or gameplay. Pulpal remains an unintegrated candidate; scores remain unchanged. Connected Meshy account is still unavailable for paid work.
## Iteration 2181 — isolated Unity import audit deferred

Authored a read-only Unity Editor audit script for Pulpal's Avatar validity and exact animation Transform paths. Its batch invocation produced no report or usable exit result while a Unity Editor process was already live; I left that process untouched. The script remains **uncompiled and unrun**; no Unity import result is claimed. Use the completed Blender Z-up review as the current evidence, then run the Unity audit when a safe isolated editor session is available. Meshy balance rechecked: 0 total; Usage 403; no paid operation.
## Craft-cosmos iteration 2180 — Pulpal candidate animation preflight

Reviewed existing Leaf Lands miniboss Pulpal instead of creating another overlapping concept. Blender 5.1 imported its rig/action FBXs; the character is one 21,015-vertex skinned mesh, 31,203 triangles, and a 24-bone armature. The catalog action bundle contains Idle/Attack/Hit/Dead/Rightward_Spin; Walk/Run are separate. The custom paper-press file imports as one 90-frame action. Sparse five-point weighted foot-vertex proxies stay within roughly 5 cm of their respective Idle references, but do not establish real soles or collider contact. Rendered captures show the catalog Attack turning side-on then crouching, with no obvious broad lane-slam pose; the custom clip reads as a modest arm-forward crouch, not a fully clear anticipation/impact/recovery sequence. Full limitations and measurements: `Logs/score-comparison/pulpal-meshy-animation-review-iter2180/REVIEW.txt`.

An initial Y-up pass was wrong; the imported rig is Z-up. Its early frames/report were superseded and excluded. The corrected Z-up reports and captures are the valid offline evidence. No Unity import/gameplay/build/test/human play, no production scene changes, no score change. Fresh connected Meshy balance is 0 and Usage is 403; no task/purchase. Next: review a genuinely integrated candidate or perform a Unity review-only import of Pulpal after correcting the attack silhouette; do not add more generic concepts while the roster is saturated.
## Roster and integration triage — 2026-11-16

Two read-only audits checked the character/enemy roster and Home Workshop decorations after Iteration 2179. Current top-level folder counts are **39 characters, 68 enemies, and 47 workshop-decoration folders** (folder counts include concept and review candidates, not production-ready content). The Leaf Lands already have Sproutlet and the Pulpal boss candidate; Mountain Lands now has Knotley plus Pomperstein. Rainbow Kingdom already has a dense craft-themed cast and the workshop has maps, keepsakes, lighting, furniture, storage, and progression-display concepts. Neither audit found a distinct next ally/enemy/decor gap that would justify another concept without overlap.

A local reference check found Meshy model/rig/action files for both Pulpal (`Assets/SpaceAndCrafts/Art/Enemies/Pulpal/Meshy_Iter1994/`) and Pomperstein (`Assets/SpaceAndCrafts/Art/Enemies/Pomperstein/Meshy_Iter1997/`), but no textual references to either in production Scenes/Prefabs/Scripts and no `*World*02*.unity` scene beyond `World02_CorkscrewCanopy.unity`. Their README files say clips, contact, Unity import, encounter logic and gameplay remain unverified. This is evidence they are local candidates, not proof they're absent from every possible runtime path or ready for integration.

Fresh connected Meshy check: **0 total (0 monthly + 0 permanent; reset 2026-12-10T21:11:17Z)**; Usage returns **403**. The website-reported pool remains unreconciled. No paid task, purchase, or concept was made in this audit. **Next useful action:** reconcile the intended Meshy account, then prioritize review/import of the existing world boss candidates before expanding the already-large concept roster. Game scores remain unchanged.

## Iteration 2179 — Knotley, Mountain Lands trail-loop guide

The character roster had a clear destination gap: Mountain Lands has the Pomperstein boss candidate but no named local guide ally in its character briefs. Added Knotley as one continuous braided trail-loop creature with compact loop horns, two short stable loop-feet, and a single embroidered face patch. The proposed optional craft hook is gather cord fiber + a metal ring → build a Loop-Lock Clasp → choose where to secure a temporary felted handline, while preserving a broad safe route and recovery. Reference and modeling guardrails: `Assets/SpaceAndCrafts/Art/Characters/KnotleyMountainTrailLoopGuide/Concept_Iter2179/README.md`. This is ImageGen-only concept art; no Meshy model, topology, rig, animation, Unity prefab, or gameplay exists.

Fresh Meshy balance: **0 total (0 monthly + 0 permanent; reset 2026-12-10T21:11:17Z)**; Usage endpoint **403**, so plan allowance and month-to-date usage remain unavailable. The separately reported website pool is unreconciled. No Meshy task/purchase and no score change. Continue prioritizing existing asset integration while the connected account is empty.
## Craft-cosmos iteration 2074 — Star-Stencil Projector workshop placement (2026-11-16)

Moved one existing Meshy-generated decoration into production scene content: the Iter1971 textured T2 FBX now has a reusable prefab at `Assets/SpaceAndCrafts/Prefabs/HomeWorkshop/StarStencilProjector.prefab` and an `ART_StarStencilProjector` instance in WorkshopHub. Its display width is 0.52 m and renderer bottom matches the workbench top at Y=0.934 m. No collider, interaction, projection light, badge progression, or logic was added. The [staged WorkshopHub context capture](Logs/score-comparison/workshop-star-stencil-projector-iter2074/frame.png) shows it in context; it reads as a compact dark-indigo projector, though the gold details remain small at this distance. Installer/report: `Assets/SpaceAndCrafts/Scripts/Editor/StarStencilProjectorWorkshopInstallerIter2074.cs` and the folder `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarStencilProjector/UnityIntegration_Iter2074/`.

Unity batch installer and capture exited 0; logs also show license-handshake warnings and the Unity MCP asset-refresh assertion during shutdown. This is static decor, not interaction/usability/human-play evidence. No Meshy credits spent, no game-score change. Next: confirm its size in the regular player/room camera before retaining; keep the projection behavior as a separate future feature request.

## Craft-cosmos iteration 2073 — Snip-Snap Sellsword rig/import preflight (2026-11-16)

Reused the existing Meshy Sellsword candidate to advance one craft-themed monster toward production, rather than adding another concept. Unity batch authoring created an isolated review-only prefab/controller/material with a valid Generic Avatar, one skinned mesh (20,833 vertices, 23 bones), and exact Transform binding-path coverage for Idle, Walk, Run, Prep, Attack, Hit, Dead, and `Rightward_Spin`. Detailed report: `Assets/SpaceAndCrafts/Art/Enemies/SnipSnapSellsword/Meshy_Iter1900/UnityCandidate_ReviewOnly_Iter2073/import-structure-review.txt`.

The package README said “Spin Down and Stand Up,” but Unity imports its final action as `Rightward_Spin`; no distinct get-up clip exists. The five-point strip shows the motion upright at t=0.80, but at t=0.20 the renderer extends about 0.34 m under the Idle-calibrated floor. At each Walk sample one weighted boot bottom is within -0.1 to +0.9 cm; at each Run sample the lower boot is within -0.7 to +2.3 cm, with the other boot lifting up to 21.6 cm. The strips show subtle Walk, clearer Run, articulated Prep/Attack, and a prone Dead pose. These rendered samples are not collision or continuous-play evidence; Spin's deep floor intersection makes it unsuitable for ground use as-is. The authoring callback wrote its isolated outputs; the batch process needed termination after failing to exit, and an explicit batch exit is now added. The separate capture completed with exit code 0; its log records the Unity MCP refresh assertion during shutdown. The asset remains unpromoted. No Meshy spend and no score change (connected balance 0; Usage 403). Next: inspect the spin body-contact region and decide whether a safe pose correction is feasible or this clip should be excluded from a ground encounter.

## Craft-cosmos iteration 2072 — Sniplet, the paper-trimmer ally (2026-11-16)

Added Sniplet as a friendly, child-safe paper-trimmer helper to complement the scissors-themed enemies. The single broad casing, integrated guarded cutting arm, printed face, attached mittens, and planted boots create a cohesive silhouette. Proposed role: show Splat how to align cardstock at the Rainbow Kingdom pattern table while leaving gathering, invention-building, traversal, and route choice to the player. Full brief: `Assets/SpaceAndCrafts/Art/Characters/SnipletPaperTrimmerAlly/README.md`; concept sheet: `Assets/SpaceAndCrafts/Art/Characters/SnipletPaperTrimmerAlly/Concept/Sniplet_PaperTrimmerAlly_Iter2072.png`.

This is built-in ImageGen concept art only, not a Meshy asset, rig, animation, Unity prefab, or playable ally. Fresh connected Meshy balance is 0 (0 monthly + 0 permanent; reset `2026-12-10T21:11:17Z`); Usage returns 403. The separately reported website balance remains unreconciled; no paid task/purchase and no game score change. Prefer integrating/reviewing existing candidates over further overlapping concepts while the account is inaccessible. Next: reconcile the Meshy account; only then choose a production candidate by gameplay value and verify topology, rig, and contact.

## Craft-cosmos iteration 2071 — five-point Meshy animation sampling (2026-11-16)

- Extended the isolated Unity capture utility to sample all nine action/locomotion clips at normalized times 0.00 / 0.20 / 0.40 / 0.60 / 0.80. Each sample uses a fresh Animator instance; the output contains one five-frame strip per clip plus joint/bounds metrics: `Logs/score-comparison/adhesive-cartographer-animation-sweep-iter2071/`.
- The Unity batch capture completed and the strips were visually reviewed. Walk poses change subtly; Run has more visible leg movement; Attack and Dead have distinct poses. Foot/toe joint pivots vary (left toe Y reaches 0.468 m in one sampled Run pose), but renderer bounds and bone pivots are only proxies, not boot-sole contact. Crossfades/transitions, continuous playback, root motion, physics, AI, gameplay, and human play were not assessed. Keep the candidate review-only; no score change.
- Meshy recheck at 2026-11-16 05:44 UTC: 0 connected credits, Usage 403, no generation/purchase. Continue asset integration rather than making more overlapping concepts. Next: compare baked skinned boot-sole vertices to the calibrated floor and repair or reject problem clips before AI integration.

## Craft-cosmos iteration 2070 — Adhesive Cartographer review and production triage (2026-11-16)

- The existing Meshy tape-roller candidate now has an isolated Unity review prefab/controller/material. Unity-generated import evidence covers exact Transform curve paths for all nine clips (100% per clip); this verifies binding names only, not quality.
- The first yaw-0 pose sheet is `Logs/score-comparison/adhesive-cartographer-animation-review-iter2069-yaw0/AdhesiveCartographer_3x3_PoseSheet.png`; the idle-calibrated follow-up is `Logs/score-comparison/adhesive-cartographer-animation-review-iter2069-yaw0-idlefloor/AdhesiveCartographer_3x3_PoseSheet.png`. Renderer-bound deltas are Walk +0.025 m, Run +0.054 m, Alert +0.130 m, Attack -0.091 m, Roll Dodge -0.109 m, Hit +0.017 m, Dead -0.155 m, and Stand Up -0.223 m. These coarse bounds are not boot-sole measurements. Idle initially shows its plain-looking back; sampled attack/recovery poses are canted. Object-reference animation curves are absent. The batch log confirms nine rendered poses but records a licensing handshake error and Unity MCP asset-refresh assertion during shutdown. Single samples do not verify continuous motion/contact; keep this candidate unpromoted pending playback/contact review.
- Read-only roster audits found >40 Home Workshop decoration folders and broad enemy/ally coverage; no distinct decoration gap justified adding another concept-only asset. Meshy recheck: 0 connected credits, Usage 403, no paid task/purchase. No score change; broader asset and 90/100 goals remain active.

### Iteration 2069 follow-up — import compatibility gate

The isolated review authoring utility now checks each clip's exact Transform binding paths against the imported rig before creating its review prefab/controller. Missing or empty paths should stop authoring and write diagnostics. This is a fail-closed source safeguard; it remains uncompiled/unrun and does not establish that the clips deform or play correctly.## Iteration 2069 — production tooling for Adhesive Cartographer (2026-11-16)

The roster audit recommends production over more similar concepts. Added a Unity Editor-only, review-only authoring utility for the existing Adhesive Cartographer Meshy rig/animation bundle. It is intended to generate a separate review prefab/controller and structural report; it does not place the character in a production scene or claim that its motion works. Source is present but has not been compiled/run; avatar import, clip discovery, skin deformation, transitions, foot contact, and rendered quality remain open. No new character concept/model and no Meshy spend. Next: inspect this candidate's actual Unity playback before any production promotion.
## Production gate — 2026-11-16

Latest connected Meshy check: **0 credits total**; Usage API **403**; the reported website balance is not reconciled. Do not submit a paid task until the intended account has a verified usable balance. The current character/ally/decor folder audit counted 68/37/47 respectively, but many entries are concepts or unintegrated candidates. Prioritize producing existing assets over expanding this roster. The most promising reviewed character is Adhesive Cartographer (`Assets/SpaceAndCrafts/Art/Enemies/AdhesiveCartographer/Meshy_Iter1953/`): 29,184 triangles, one mesh/material, reported 24-joint rig and seven clips; none verified in Unity playback or gameplay. Next: isolate its import/animation review and only then decide whether its distinctive roller silhouette belongs in an encounter.
## Craft-cosmos iteration 2067 — Corner Tuck, the folded-seam runner

Added a model-sheet concept for a recurring beginner paper enemy, a role missing from the otherwise boss-heavy roster. Corner Tuck peeks from an obvious, safe seam, telegraphs a short ground slide by brightening one crease, then holds a planted recovery. Splat can jump or counter it; defeat leaves one optional cardstock-scrap pickup. The image and simplification/production guardrails are at `Assets/SpaceAndCrafts/Art/Enemies/CornerTuck/`. The generated face reads somewhat fierce and needs softening before any modeling. Concept art only: no Meshy model, rig, clips, prefab, encounter, or play test.

Live connected Meshy check on 2026-11-15: 0 credits (0 monthly + 0 permanent); Usage returned 403. Model listing showed only the already-existing Splat image-to-3D GLB among the queried model types. The website-reported credit pool is still unreconciled with this account. No Meshy task or purchase was made. Next production step is to reconcile the account or integrate a suitable existing Meshy model; avoid adding more similar concepts meanwhile.

## Iteration 2055 — Hinge Muffler fit board for the Cut-and-Paste Crew

Added `Assets/SpaceAndCrafts/Art/Commissioned/Inventions/HingeMuffler/Concept/HingeMuffler_CutAndPasteCrew_FitBoard_Iter2055.png`, showing the same existing padded spring clamp in three proposed recovery contexts: Grand Snip's open blades, Great Paste Pot's rounded lid/nozzle, and PuffPatch's scored fold. A roster audit found that the informal Iter2054 name “Clip-Guard” duplicated the existing Hinge Muffler; the encounter proposal now reuses one canonical tool. This fit board is a design hypothesis, not dimensional evidence that the same jaw fits all targets. No Meshy model or implementation. Balance 0; Usage 403.

## Iteration 2054 — The Cut-and-Paste Crew encounter

Created a shared Rainbow Kingdom boss encounter board at `Assets/SpaceAndCrafts/Art/Enemies/CutAndPasteCrew/` using the existing Grand Snip, Great Paste Pot, and PuffPatch designs. The same existing Hinge Muffler is proposed as the shared invention. The characters act one at a time on a connected, level arena. This is an encounter composition using existing characters—not new models or gameplay. ImageGen only. Meshy recheck is 0 credits and Usage 403; no paid task.

## Iteration 2053 — Starloop Window Drapes

Roster scan found no Home Workshop curtains or soft window dressing. Added a paired quilted-fabric curtain concept at `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarloopWindowDrapes/`, sized to the round observatory porthole and shown open/tied and closed with front/profile construction views. Proposed optional two-pivot dressing that stays above the floor lane. This is a model-sheet concept only; no cloth mesh, rig, animation, or Unity placement exists. Meshy rechecked at 0; Usage 403; no task or purchase.

## Iteration 2052 — Patchwork Peddler Workshop Corner

Combined the Patchwork Peddler (Iteration 2050) and Swatch Counter (Iteration 2051) references into one wide Home Workshop nook at `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/PatchworkPeddlerCorner/`. The folio and three cosmetic swatches remain readable, the waist-height counter and Peddler share a level floor, and a clear foreground lane remains open. This checks visual scale/composition only; no Meshy model, Unity placement, interaction, economy, or playtest exists. The trade pitch stays optional and cosmetic-only. ImageGen concept only. Meshy rechecked at 0; Usage API 403; no paid task or purchase.

## Iteration 2033 — PuffPatch model-sheet refinement

Replaced the star-like perimeter as the preferred PuffPatch reference with a broad asymmetric sheet outline and one clearly connected violet fold. Front/profile insets emphasize thickness and the single authored pivot; the face remains printed on one panel. This is a modelability-oriented concept refinement, not geometry. Meshy rechecked 0 balance, Usage 403; no credits spent. Review face/cutout at gameplay scale before modeling. Full notes: `Assets/SpaceAndCrafts/Art/Enemies/PuffPatch/README.md`.

## Iteration 2032 — PuffPatch, the Foam-Fold Mimic

PuffPatch is a single thick EVA foam sheet with one scored fold, one printed face and a small integrated tab. The creature folds into a wall, then Splat uses a gathered-foam/brad Fold-Lock Clip to pin it open as a stable ramp to the counter switch. Keep the broad ground route safe and animate one mechanical fold, not jelly flapping. Foam material is distinct from the paper/origami roster, though the current star-like perimeter needs trimming. ImageGen concept only; no Meshy job. Balance 0, Usage 403; no credits spent. Full brief: `Assets/SpaceAndCrafts/Art/Enemies/PuffPatch/README.md`.

## Iteration 2035 — Grand Snip mechanical model sheet

Added a consistent front/side/rear/three-quarter sheet at `Assets/SpaceAndCrafts/Art/Enemies/GrandSnip/Concept/GrandSnip_ModelSheet_Iter2035.png`. It preserves the revised face and scissors-only identity, and clarifies the two-blade overlap/pivot for image-guided modeling. Exact dimensions and rear detail remain illustrative; no Meshy model was generated. Connected balance is 0, Usage 403. The older humanoid candidate remains separate and unpromoted. Full brief: `Assets/SpaceAndCrafts/Art/Enemies/GrandSnip/README.md`.

## Iteration 2034 — Grand Snip legacy Meshy candidate review

An older Meshy-generated Grand Snip already exists in Iter1846, with a rig and seven-action bundle. Its inspected preview is a humanoid armored character carrying scissors motifs and a multicolor plume; it does **not** match the newer one-piece scissors-object boss in the Iter2030/2031 concept. Keep it isolated as a separate candidate; do not present it as the new concept's model or reuse it without a deliberate redesign decision. The clean concept still needs its own image-guided geometry and authored blade pivot. Connected Meshy reports 0 credits and Usage 403, so no new job was submitted. Details: `Assets/SpaceAndCrafts/Art/Enemies/GrandSnip/README.md`.

## Iteration 2031 — Grand Snip face refinement

Saved a non-destructive face revision for Grand Snip at `Assets/SpaceAndCrafts/Art/Enemies/GrandSnip/Concept/GrandSnip_BossConcept_Iter2031.png`. It removes cheek marks and replaces oversized anime eyes with small dark-plum eyes and one amber highlight, while preserving the single scissors-tool silhouette and boss-pose cues. The full Hinge Muffler encounter proposal is in the character README. ImageGen only; Meshy balance is 0, Usage 403, no credits spent. Game-scale/readability review and 3D remain open.

## Iteration 2030 — Grand Snip, the Pattern-Cutter boss

Grand Snip is one oversized pair of child-safe scissors: blunt lavender blades, teal/coral loops, and one face on its coral hinge. The encounter asks Splat to gather felt pads and a brass brad, craft a Hinge Muffler, hold a blade open during recovery, and cross the marked paper lane to a counter switch. Keep the arena floor and perimeter safe; animate the two-blade pivot instead of humanoid auto-rigging. The silhouette reads clearly, but eye/cheek styling needs revision and the side inset is not a full turnaround. ImageGen concept only, not 3D or integrated. Meshy balance 0, Usage 403; no credits spent. Full brief: `Assets/SpaceAndCrafts/Art/Enemies/GrandSnip/README.md`.

## Latest workshop decoration — Iteration 2029: Starloop Offcut Recycler (2026-11-12)

- Audited the HomeWorkshop prop folders; the room has maps, trophies, clocks, mobiles, lanterns, organizers, chests, a kiln and display pieces, but no paper-offcut recycler. Added a coherent tabletop punch-and-confetti machine with a clear intake, attached crank, transparent cover and star tray.
- Proposed optional WorkshopHub interaction: crank the machine once for a small contained paper-to-stars animation and soft sound; do not consume inventory or alter quest resources. This is a design pitch only, not an implemented interaction.
- Concept and brief: `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarloopOffcutRecycler/`. It is ImageGen-only; no Meshy model, measured topology, Unity prefab, collision, sound or runtime placement. Keep the casing silhouette and simplify the tiny internal rollers/prints for a future model.
- Meshy recheck: 0 credits, Usage 403, website-reported pool unreconciled; no paid task or purchase. Proposed static smart-topology T2/N1 model route: 15 credits if funds become available and the paid task is approved.

## Latest free boss concept — Iteration 2028: Filament Fizz, the Doodle Extruder (2026-11-12)

- Scanned the enemy/ally roster; no 3D-printing-pen character was present. Filament Fizz is one cohesive violet pen casing with an integrated nozzle, recessed temperature display, flush rainbow spool, short attached arms and planted boots.
- Boss loop: explore the Drawing Pit, gather cool filament and folded-card guide clips, build a Loop-Lock Gauge, then route one clearly telegraphed extruded line through it to make a stable short crossing/handrail. Use the new route to reach Fizz's cooling nozzle during a readable recovery and counter at the side feed latch. Keep a broad safe floor/recovery rails; avoid obscuring the ground or random projectiles.
- Concept and brief: `Assets/SpaceAndCrafts/Art/Enemies/FilamentFizz/`. The hero is readable as a 3D pen, but eyebrows are rather villainous, the spool/nozzle detail is busy, and side-view proportions need simplifying before modeling.
- ImageGen only: no Meshy model, topology, rig, action clips, Unity scene, boss logic, or gameplay. Fresh connected Meshy balance: 0; Usage 403; website-reported credits unreconciled; no credits spent. Proposed first image-to-3D/rig/three-action slice: 29 credits, pending usable balance and task-specific confirmation.

## Latest free ally concept — Iteration 2027: Blu, the Sunprint Scout (2026-11-12)

- Roster scan found no camera/cyanotype character among current enemy and ally folders. Blu is a friendly pinhole-camera explorer with a broad cyanotype body, small brass lens, cream face panel, short integrated violet limbs, planted boots, and botanical/planet printwork.
- Gameplay pitch: Blu notices a faint silhouette but does not solve the route. Splat explores and gathers blue print-card, a leaf rubbing, and star foil; builds a Sunprint Viewfinder; then aims it at a matching wall to expose a temporary high-contrast route outline. Splat still chooses and executes the traversal. This is a navigation ally, separate from Vitrail's beam-combat boss.
- Refined concept saved at `Assets/SpaceAndCrafts/Art/Characters/BluSunprintScout/Concept/Blu_SunprintScout_Concept_Iter2027.png`; status and modeling guardrails are in `Assets/SpaceAndCrafts/Art/Characters/BluSunprintScout/README.md`. The large lens and close-set face panel need game-scale review and should be simplified before modeling.
- ImageGen concept only; no Meshy model, topology, rig, animation, Unity import, or implemented ability. Meshy plugin balance and the authenticated CLI both verify **0 credits**; Usage API still returns 403; the website-reported expiring pool is not reflected in the connected account. No credits spent.

## Latest free boss concept — Iteration 2025: Vitrail, the Windowlight Keeper (2026-11-12)

- The roster already covers sewing machines/thread and an extensive set of scissors, paper, glue/tape, stamps, punches, erasers, pencils, and brushes. Avoid a near-duplicate.
- Added a distinct stained-glass suncatcher boss concept: one sturdy enclosed frame and broad jewel panes around a friendly opalescent face. It differs from Prism Moth because Vitrail controls an arena-scale light-beam traversal encounter; the moth is a small optional-path hint ally.
- Proposed loop: explore for translucent color-film and vellum, gather both, build a padded diffuser frame, place it over a telegraphed beam to make a stable rainbow crossing, then use Vitrail's dimmed recovery to reach and counter the exposed core.
- ImageGen front-three-quarter and consistent front/profile/back references: `Assets/SpaceAndCrafts/Art/Enemies/VitrailWindowlightKeeper/Concept/Vitrail_Concept_Iter2025.png` and `Assets/SpaceAndCrafts/Art/Enemies/VitrailWindowlightKeeper/Concept/Vitrail_Turnaround_Iter2026.png`; brief/status: `Assets/SpaceAndCrafts/Art/Enemies/VitrailWindowlightKeeper/README.md`. The profile is very shallow, so a future model still needs proportion review. Concept only; no Meshy model, rig/animation, Unity import, arena, AI, or encounter.
- Fresh Meshy balance is **0 total**; Usage API returns 403 and the website-reported 2,600-credit pool remains unreconciled. No Meshy credits spent.

## Workshop decoration integration note — Comet Kiln (Iteration 1955)

The existing textured Meshy kiln prefab is placed on the WorkshopHub workbench. A 1600×900 graphics-enabled Play Mode capture exposed an unassigned albedo texture; the kiln now uses a dedicated matte URP/Lit material. The reviewed frame shows its navy body with cream/coral features seated on the bench. It remains static and non-interactive, and the small camera read plus neighboring-prop clearance still need a wider composition review; see the asset README and `docs/ODYSSEY_SCORECARD.md`.

## Current free-concept pass — Iterations 2022–2024 (2026-11-12)

- **Parallax, the Orbit Divider (2022):** a drafting-compass miniboss reference at `Assets/SpaceAndCrafts/Art/Enemies/ParallaxOrbitDivider/`. The roster audit found no compass-bodied foe; existing scissors, paper and adhesive rosters are already dense. Pitch: trace one dotted arc, telegraph a blunt-foot sweep, and let Splat use a crafted Arc Catcher to redirect it during recovery. ImageGen concept only; no Meshy task or encounter.
- **Bubble, the Level Sprite (2023):** an integrated spirit-level ally at `Assets/SpaceAndCrafts/Art/Characters/BubbleLevelSprite/`. Bubble signals when a player-built crossing tilts or needs a shim; Splat still gathers, chooses and places the fix. This is separate from Calip's small-part fit check. ImageGen concept only; no model, rig or behavior.
- **Starseed Terrarium (2024):** a cloche-and-paper-sprout workshop ornament at `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarseedTerrarium/`. Optional subtle leaf/bud growth can reflect Leaf Lands discovery and its boss milestone, without replacing quest/map UI. ImageGen concept only; no Meshy model or scene placement.
- Connected Meshy balance is now **0 total = 0 monthly + 0 permanent**. Usage API is still 403, and the website-reported pool is unreconciled. No credits were spent on these three ImageGen concepts; paid Meshy work cannot continue on the connected balance.

## Historical Meshy assets — Iterations 1992–1994

- **Splat approved cap-and-eye candidate:** Iteration 1992 used the approved white-ball, purple-limbed, backward-cap concept as an image-to-3D source. Meshy returned a textured T2.5/N1 FBX candidate (target 30k; actual face count not reported in the status response), then a successful rig task and separate free Walk/Run FBXs. Two swift motions were generated and retargeted: a small breathing/weight-shift idle and a compact hop with soft landing/recovery. A second action task bundles catalog Idle (ID 0) and Regular Jump (ID 466). Preview reads as the approved character, but dark artifacts around the eyes/brow and cap seam remain visible. No clip playback, retarget deformation, sole contact, Unity import, or gameplay use is verified; not canonical/integrated yet. Cost 38 credits. Full receipt: `Assets/SpaceAndCrafts/Art/Characters/Splat/MeshyWork/Iter1992/README.md`.

## Iteration 1993 — Pinna, Starlight Safety-Pin Helper

- New ally concept: a closed, visibly blunt brass safety-pin loop with a small cream face plate, coral star clasp, purple arms and boots. Pinna marks safe fabric-route anchors; Splat still gathers and builds the actual fix. Concept `Assets/SpaceAndCrafts/Art/Characters/Pinna/Concept/Pinna_Concept.png` and textured T2.5/N1 FBX candidate are saved; requested 30k target, actual topology unreported.
- Meshy reports a successful rig and separate free Walk/Run FBXs. Two swift custom motions were generated and retargeted: a restrained idle sway and a clasp-pointing anchor demonstration. Candidate preview is cohesive and preserves the closed-pin silhouette. No animation playback, shape deformation, ground contact, Unity import, route interaction or story behavior is verified. Not integrated.
- Cost **41 credits**; live balance after Iteration 1993: **354 total = 0 monthly + 354 permanent**. Usage API 403; the website-reported expiring pool remains unreconciled. Full receipt and task IDs: `Assets/SpaceAndCrafts/Art/Characters/Pinna/README.md`.

## Iteration 1994 — Pulpal, the Pressed-Paper Paladin

- New Leaf Lands miniboss gap: a broad single-body papier-mâché knight with pressed-leaf markings, distinct from the existing glue-gun, adhesive-blob, shears and folded-paper characters. Proposed counter: after a marked-lane slam, Splat uses a crafted leaf-press wedge to catch Pulpal during recovery and leave a stable crossing. Arena, behavior and counter are design-only.
- GPT Image 2 concept and textured T2.5/N1 FBX candidate (requested 30k; actual count unreported) are saved under `Assets/SpaceAndCrafts/Art/Enemies/Pulpal/`. Preview preserves the pulp silhouette and large leaf motif; small arm/foot dark marks remain.
- Meshy reports successful rig and free Walk/Run. The folder includes a custom Paper-Press Attack FBX and catalog Idle, Attack, Hit, Dead and Spin-Down/Stand-Up clips. No playback, deformation, grounded contact, Unity integration or encounter behavior is verified. Candidate only.
- Cost **50 credits**; live balance after Iteration 1994: **304 total = 0 monthly + 304 permanent**. Usage API 403; the website-reported pool remains unreconciled. See `Assets/SpaceAndCrafts/Art/Enemies/Pulpal/README.md`.

Historical balance checkpoint after Iteration 1992: **395 total = 0 monthly + 395 permanent**. Usage endpoint 403; website-reported expiring credits remain unreconciled.

## Historical generated ideas — Iterations 1988–1991

- **Invention Memory Board (Iteration 1988):** one mounted indigo/coral panel with three empty sewn pockets and large star/paste/tool motifs. It is intended to show only inventions Splat has actually crafted, making it a physical record of the explore→gather→craft loop rather than another map or generic decoration. Concept and textured T2/N1 FBX candidate are saved under `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/InventionMemoryBoard/`; cost 24 credits. Preview retains the single-panel silhouette and pockets; a small black edge artifact remains, and component icons need game-scale review. No crafting-state integration or Unity placement.
- **Junk Magnet Gauntlet (Iteration 1989):** planned Magnet Marsh boss, deliberately distinct from Margo's small U-magnet ally: a broad upside-down horseshoe body, two colored poles, central storm core, printed face, integrated arms and boots. Concept and textured T2.5/N1 FBX are under `Assets/SpaceAndCrafts/Art/Enemies/JunkMagnetGauntlet/`. The 28k target is unmeasured. Meshy rig task succeeded and supplied separate Walk/Run FBXs; a 3-second Magnetic Pull motion was generated and retargeted to an FBX. The inspected model preview is cohesive/readable but its one-sided armor plate feels bulky. Motion playback, foot contact, Unity import and the mud-pit counter mechanic are unverified. Total cost 35 credits; see asset README.

Balance after Iteration 1989: **503 (0 monthly + 503 permanent)**. Usage endpoint 403; the 2,600-credit website report remains unverified/unreconciled. The Iteration 1988–1989 assets were candidates, not integrated. Overall craft-cosmos and 90/100 goals remain active.

- **Thimby, the Starlight Pincushion (Iteration 1990):** a friendly teal felt ally with embroidered constellation paths, a sewn face panel, spool emblem, short plush limbs and attached thread tail. Its suggested role is to model safe route-stitching without solving the puzzle for Splat. Concept and T2.5/N1 FBX candidate are under `Assets/SpaceAndCrafts/Art/Characters/Thimby/`; Meshy reports a successful rig and separate free Walk/Run FBXs, plus a custom Greet-and-Stitch action FBX retargeted from a 3-second swift motion. Cost: 35 credits. Model preview is warm and cohesive but shows a dark triangular face seam and a raised chest spool; the topology target is unmeasured. No motion playback, Unity placement, behavior, or story integration. Receipt: `Assets/SpaceAndCrafts/Art/Characters/Thimby/README.md`.

Latest balance after Iteration 1990: **468 (0 monthly + 468 permanent)**. Usage endpoint 403; the user's 2,600-credit website figure remains unreconciled. Three candidates were unintegrated at that checkpoint.

- **Hemline Harrier, the Pinking-Shear Trickster (Iteration 1991):** a distinct large-scale pinking-shear silhouette built around a central face/pivot, integrated handle loops and blunt zigzag blades. The proposed counter is to tie a short sewn safety-stitch through the pivot after it telegraphs and lays a cut ribbon lane. Concept and textured T2.5/N1 FBX (28k target unmeasured) are saved in `Assets/SpaceAndCrafts/Art/Enemies/HemlineHarrier/`; Meshy reports a successful rig, separate free Walk/Run FBXs, and a custom Seam-Snap attack FBX retargeted from a 3-second swift motion. The model preview is cohesive and recognizable, though at thumbnail scale the blades resemble tall ears and have several dark edge marks. No animation playback, encounter, Unity use, or counter mechanic is verified. Total cost 35 credits; full receipt: `Assets/SpaceAndCrafts/Art/Enemies/HemlineHarrier/README.md`.

Latest live balance after Iteration 1991: **433 (0 monthly + 433 permanent)**; Usage endpoint 403; website-reported expiring pool remains unreconciled. All current iteration assets are unintegrated candidates.

## Historical generated ideas — Iterations 1959–1987

- **Tessera, Puzzle Navigator (Iteration 1987):** first Meshy concept task `01a1d05c-6c4b-7100-bedd-18e1dc89e7c3` missed the jigsaw silhouette. Refined concept `01a1d05d-8bec-71c9-83d4-66812a6519d2` (9 credits), textured T2.5/N1 model `01a1d05e-4fd4-77c7-84db-c8c480baa4db` (15; 24k target unmeasured), conversion (1), rig (5), free Walk/Run, and custom point-to-slot motion + retarget (6) are saved in `Assets/SpaceAndCrafts/Art/Characters/Tessera/Meshy_Iter1987/`. Preview reads as a four-tab puzzle body but has slim arms/legs and a top tab that resembles a head bump. Gameplay hook: point at a matching missing slot; Splat still gathers/builds the panel. No motion playback or Unity behavior verified. Total iteration cost 45 credits.

- **Fortune Fold (Iteration 1985):** folded-paper fortune-teller foe with broad colored flaps, a printed-face concept, short purple arms, and boots. Concept/model, GLB conversion, Meshy-reported rig, free Walk/Run, and one custom motion source + retarget are saved in `Assets/SpaceAndCrafts/Art/Enemies/FortuneFold/Meshy_Iter1985/` (30 credits). Preview has a small triangular flap artifact and mask-like facial reading; rig/action playback, contact, Unity import, and encounter are unverified. Encounter idea: flap colors telegraph a route pattern that Splat must read and counter with a crafted stable crossing.
- **Celestial Scrapwork Rug (Iteration 1986):** broad embroidered indigo workshop mat with orbit, paper-star, scissors, glue, and folded-rocket motifs. Concept task `01a1d054-a7c5-77de-a595-a4a248ab6bd9` (9 credits); textured T2/N1 model `01a1d055-9efa-74d4-ac83-5af8e1af7417` (15 credits, 15k target unmeasured). Concept is clear; model preview fragments the central star, distorts the scissors, and adds dark seam holes. Reject the model; retain the concept as a reference for a flatter, mostly printed rug. Receipt: `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/CelestialScrapworkRug/README.md`.

- World-Window Lightbox (Iteration 1984): wall-mounted decor candidate with one indigo frame and three layered destination panes (Rainbow, Leaf, Mountain). Concept task 01a1d04c-e785-7075-bfe1-5c1bebadcd61 (3 credits) and textured T2/N1 FBX task 01a1d04d-5a58-77c4-85ad-f1addefe4932 (15 credits; 15k target unmeasured). Preview retains the three distinct landscapes and attached lower star slider. No Unity placement or unlock-state swapping. Receipt: Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/WorldWindowLightbox/Meshy_Iter1984/README.md.
- Cadence, Folding Music-Box Ally (Iteration 1983): a compact printed-face music-box friend. Concept/model tasks 01a1d047-0825-748a-b312-d3320f55c7bd and 01a1d047-75bf-7155-be3e-71f48fb331ec (18 credits); GLB conversion 1, rig 5, swift rhythm-tap motion + retarget 6 credits. Meshy reports rig success and free Walk/Run. The body reads cohesive in the model preview, but animation playback/contact are unreviewed. Gameplay hook: show an optional three-beat rhythm puzzle and let Splat repeat it; do not auto-solve. Receipt: Assets/SpaceAndCrafts/Art/Characters/CadenceMusicBoxAlly/Meshy_Iter1983/README.md.
- Earlier backlog item Invention Memory Board is now a concept/model candidate in Iteration 1988 (see current status above).
- Kink the Chenille Comet (Iteration 1982): new material/shape gap, distinct from existing tape, thread and spider foes. Meshy concept task 01a1d044-7abc-747e-85a9-4c1d0fe81dbd (3 credits) and textured T2.5/N1 model task 01a1d045-5ca2-74a4-aaf4-fe4c4bb63043 (15 credits; 24k target unmeasured). Preview reads as a continuous striped coil with a friendly face; surface is ridged rather than fuzzy and a dark mark sits by the face. Possible mechanic: cinch a marked lane, then let Splat pin the loop open with a crafted clothespin. No rig or behavior. Receipt: Assets/SpaceAndCrafts/Art/Enemies/KinkChenilleComet/Meshy_Iter1982/README.md.
- Grommet Gecko (Iteration 1981): image-guided textured T2.5/N1 FBX from the existing cardstock-lizard concept, task 01a1d040-f50d-7582-b624-40ee5a931c86 (15 credits; 28k target unmeasured). The preview retains the rolled tail, brass eyelets and continuous paper body; the tail center is open. Potential obstacle role: pin a cloth crossing with the eyelets until Splat crafts a brad-puller. No rig, behavior or Unity integration. Receipt: Assets/SpaceAndCrafts/Art/Enemies/GrommetGecko/Meshy_Iter1981/README.md.
- Palette Piper (Iteration 1980): reused the existing painter-palette ally concept for a T2.5/N1 textured FBX attempt (task 01a1d03b-f8b2-74d1-b671-d10f3363e2c5, 15 credits, 28k target unmeasured). The inspected thumbnail has a large black cavity in one eye and a diagonal crack across the palette face; reject before rigging. Keep the concept only, and change geometry/material separation before any future retry. No Unity use. Files/receipt: Assets/SpaceAndCrafts/Art/Characters/PalettePiper/Meshy_Iter1980/.
- **Rainbow Kingdom Shadowbox (Iteration 1978):** tabletop workshop trophy with layered rainbow falls, indigo cardstock arch, folded base and a boss souvenir. Concept task `01a1d031-2d63-74c4-9d9e-a5cf6541f97a` (3 credits) and textured T2.5/N1 FBX task `01a1d034-af32-7526-bdc0-7eb2f09158b3` (15 credits; 18k target unmeasured). Thumbnail preserves the frame/rainbow but shows dark voids, side scratches, and a generic star. Candidate only; no Unity placement or boss unlock. Receipt: `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/RainbowShadowbox/`.
- **Lami-Luxe, Celestial Laminator boss (Iteration 1979):** roster scan found no laminator enemy. The concept has a compact indigo desktop laminator body, coral face, brass roller, feed slot and close-curling clear-film sash. Its textured T2.5/N1 FBX preview retains that silhouette; film thickness/attachment and a small underside artifact need review. 28k target is unmeasured; no rig/animation or Unity integration. Design hook: telegraph a film lane, then let Splat squeegee/redirect it during the recovery. Concept/model cost 18 credits. Receipt: `Assets/SpaceAndCrafts/Art/Enemies/LamiLuxe/Meshy_Iter1979/README.md`.


- **Zip-Zag (Iteration 1974):** revised zipper-pouch concept advanced to a textured T2.5/N1 FBX candidate. Its preview keeps one squat blue pouch body, coral piping, zipper/pull hardware and attached lavender feet. The narrow eyes and sharp smile read sly, and the surface lacks a strong fabric read; not final production art. The 28k topology target is unmeasured; no rig/animation, Unity placement or route-sealing behavior. Receipt: `Assets/SpaceAndCrafts/Art/Enemies/ZipZag/Meshy_Iter1974/README.md`.
- **Tiney the Tuning-Fork Firefly (Iteration 1975):** the inspected textured T2.5/N1 preview keeps a single upright gold U-fork body, friendly face, mint tips, wings and boots. The wings look opaque/pale rather than translucent and there is a small forehead mark. 28k target unmeasured; no rig/animation, Unity placement or hidden-cache behavior. Receipt: `Assets/SpaceAndCrafts/Art/Characters/TineyTuningForkAlly/Meshy_Iter1975/README.md`.
- **Pigment Prism Mixer (Iteration 1976):** image-guided textured model kept the palette wells, color wheel and crank but has broken-looking black gaps around the front basin and an empty dark chamber. Rejected; no repair or Unity use. Task cost 15 credits. Receipt: `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/PigmentPrismMixer/Meshy_Iter1976/README.md`.
- **Margo the Magnet-Moon (Iteration 1977):** textured T2.5/N1 model thumbnail retains the U-magnet, pole caps, center band, face and boots. It reads cleanly at preview scale; target topology and game-scale readability are unmeasured. No rig/behavior/Unity integration. Cost 15 credits. Receipt: `Assets/SpaceAndCrafts/Art/Characters/MargoMagnetAlly/Meshy_Iter1977/README.md`.

- **Star-Stencil Projector (Iterations 1970–1971):** the folded-indigo-cardstock mini planetarium concept and textured Meshy T2/N1 FBX are saved under `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarStencilProjector/`. The 15,000-face target is unmeasured; the inspected thumbnail preserves the shell/lens/crank but the gold star details look raised, not cut through. A workshop projection scrapbook is a design idea, not implemented. Total cost: 18 credits. Candidate only; no Unity import.

- **Pumice, the Star Sponge (Iterations 1969 and 1973):** the concept was advanced to a textured T2.5/N1 FBX, successful Meshy humanoid rig, separate free Walk/Run files, and six same-rig action clips. The preview keeps the teal star silhouette and coral wrap. Model topology target is unmeasured; animation deformation, contact and transitions are unreviewed. Goo-spill cleanup / temporary grip-pad behavior is unimplemented. Candidate and receipts: `Assets/SpaceAndCrafts/Art/Characters/PumiceStarSpongeAlly/Meshy_Iter1973/`.

- **Solstice Press (Iteration 1972):** textured T2.5/N1 smart-topology model candidate saved under `Assets/SpaceAndCrafts/Art/Enemies/SolsticePress/Meshy_Iter1972/`. It preserves the press silhouette and print bed, but the thumbnail shows two faces and thin stick-like side arms; target count/rigability are unverified. Cost 15 credits; the exposure-lane/blocker counter is still design-only.
- **Stitchway Loom:** useful progression-decor idea, but the textured model lost its world-route symbols; use a clearer reference before replacement.
- **Zip-Zag** and **Badgeback:** distinguishable roster roles, but both direct text-to-3D previews became humanoids and were rejected. Do not texture/rig those drafts.
- **Zip-Zag revised Meshy concept:** `Assets/SpaceAndCrafts/Art/Enemies/ZipZag/Concept/ZipZag_Concept.png` restores a squat, single pouch body with attached zipper/star pull and sewn limbs. Its Iteration 1974 textured model is now saved; the earlier hooded-human draft remains rejected.
- **Pigment Prism Mixer:** `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/PigmentPrismMixer/Concept/PigmentPrismMixer_Concept.png` shows three wells, a turning color wheel, central prism and crank; it could expose hidden routes after Splat feeds it pigments. The initial 5-credit text mesh was unclear and remains untextured. Concept cost 3; image-to-3D awaits user approval.
- **Margo the Magnet-Moon:** U-shaped plum horseshoe magnet with contrasting pole caps. She helps Splat find hidden metal brads/fasteners; this is a resource-search ally rather than a route guide. Concept cost 3 and awaits model approval.
- **Tiney the Tuning-Fork Firefly:** friendly gold tuning fork with illuminated tips and small wings; rings near hollow walls to hint at optional scrap caches, while Splat still discovers the route. The Iteration 1975 textured model exists; the wings and forehead artifact need review before promotion.
- **Grommet Gecko:** one continuous cardstock gecko with brass eyelets pins a crossing; Splat's crafted brad-puller releases it into a safe route. The roster scan found no existing eyelet/grommet character. Meshy concept cost 3; 3D awaits approval. A lower-priority alternative, **The Seam Unpicker**, clamps and peels a bright cardstock seam for a crafted-fastener crossing, but risks reading too much like scissors/binder clips.
- **Grommet Gecko direct-text Meshy draft:** task `01a1cffe-0391-7731-bf46-c81e68197aa8` cost 5 credits. The untextured preview became an ordinary organic frog/lizard and omitted the paper material and grommets. Rejected before texture/rig. Prefer the concept-image route after user approval.

## Palette Piper — ally concept awaiting approval for 3D

- Concept: `Assets/SpaceAndCrafts/Art/Characters/PalettePiper/Concept/PalettePiper_Concept.png`. It reads clearly as one asymmetrical painter's palette with a direct friendly face, inset wells, integrated purple arms/legs, and mint boots. Proposed ally role: help Splat mix pigments to reveal marked routes or craft instructions; behavior is design-only.
- The first T2.5 smart-topology text draft (`01a1cfd7-73fa-778d-9cbc-1eb187ad9edb`, **5 credits**) became a generic astronaut-like humanoid. It is retained for audit but rejected before texturing or rigging.
- Replacement concept task `01a1cfd9-dbd1-761c-b922-6a14354aeb1b` cost **3 credits**. The image-to-3D pass is pending user approval of this concept. No textured 3D model, rig, animation, Unity placement, or gameplay behavior exists yet. Full receipt: `Assets/SpaceAndCrafts/Art/Characters/PalettePiper/`.

## Cutline Captain — paper-trimmer boss draft rejected

- Intended role: a rounded safety paper-trimmer boss telegraphs a marked cut lane; Splat crafts a stop-block to arrest the rail and make a safe crossing. This is separate from enemy scissors but the model must keep the cutter rail dominant.
- Two direct-text Meshy tasks cost **15 credits** total (`01a1cfdd-8bd4-706f-9c99-00c66cf0ba09` preview, `01a1cfde-99a4-7799-8e27-a3a2d29b5c3e` texture). The result is a generic paper-machine humanoid, with triangles in the front slot and a stray line at the right hand; it overlaps FeedSlotShredderWarden. Rejected; no model download, rig, animation, Unity use, or behavior. Do not retry without a clearer approved concept image. Receipt: `Assets/SpaceAndCrafts/Art/Enemies/CutlineCaptain/Meshy_Iter1957/README.md`.

## Tessera, the Puzzle Navigator — Meshy drafts rejected

**Roster check:** the current character/enemy folders contain 20 ally/player directories and 38 enemy directories. Searching the art README/docs for jigsaw or puzzle-piece characters returned no match. Existing allies already cover patch repair (Mendle), thread guidance (Threadwing), and folding paper routes (Kiri), so Tessera should have a distinct, tightly bounded role.

- **Silhouette:** one softly rounded, oversized interlocking puzzle-piece body in warm paper-cream and deep-indigo edge ink; a single large central face with star-map print; short purple mitten arms and feet integrated into the body. Avoid floating puzzle pieces or lots of separate shapes; this needs to read as one premium game character at the camera's normal scale.
- **Ally role:** Tessera points out one clearly outlined missing-piece slot. When Splat has gathered matching cardstock and places the related invention, Tessera helps seat one broad, stable panel in the slot. This supports gather → craft/use → safe crossing without granting arbitrary bridges or fall bypasses.
- **Animation needs:** subtle idle inspection, point-to-slot, hop/set-piece, excited cheer, hit, defeat, and recover; ordinary Walk/Run. Rig playback/contact remain required review gates.
- **Concept made:** the polished front-three-quarter Tessera concept is saved at `Assets/SpaceAndCrafts/Art/Characters/Tessera/Concept/Tessera_Concept.png` (Unity-importable PNG). It keeps one connected ivory puzzle body, indigo printed edge, central star-map motif, soft purple integrated mitten arms and boots. This is a non-Meshy concept image, not a 3D model.
- **Meshy drafts:** T2.5/N1 textured smart-topology attempts `01a1cf84-62eb-73cb-b9c1-050afd1ddfb6` and `01a1cf87-f13b-75b8-ad86-de4ec677d1a6` both succeeded at 15 credits each; retexture `01a1cf89-4ec7-73a0-beb8-721e699b254f` consumed another 10. All three thumbnails retain a conspicuous black stripe/crack across the face despite a cleaned reference; the retexture adds more surface damage. Total **40 credits**, all drafts rejected. No rigging or Unity integration was attempted. Keep files for audit; stop reworking this face until the modeling reference separates blank geometry from a separately authored face texture.

## Moonphase Paper Clock — textured Meshy prop candidate

- Concept preview saved at `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/MoonphasePaperClock/Concept/MoonphasePaperClock_Concept.png`: one attached wall clock with folded-card construction, paper-fiber texture, readable crescent/star pointers, and restrained indigo/coral/turquoise palette. Textured T2.5/N1 FBX `Meshy_Iter1952/MoonphasePaperClock.fbx` and thumbnail are now saved; the inspected preview preserves the bracket, layered face and hands, but simplifies phase markers. This is a candidate only, not integrated.
- Workshop hook: the moon hand can advance with world unlocks as a low-noise progress marker. This is optional and not implemented.
- Meshy task `01a1cf97-019d-714d-80c8-c8a2fc3a64c4` cost **15 credits**. The task targeted 18k polygons; actual FBX topology has not been independently measured. No prefab, placement, scale/material setup, animation or world-unlock behavior exists.

## Additional candidate backlog — roster-checked encounter directions

The current roster already contains multiple scissors (`SnipSnap`, `SnipSnapSellsword`), paper (`PaperJamMarquis`, `ReamKeeper`, `CarbonCopyMimic`), and paste/glue/tape foes (`CaptainPuddlePaste`, `GloopstickGlueStickJester`, `GreatPastePot`, `MeltMarshal`, `LoopLockTapeDispenser`). Avoid another generic shear biped or adhesive blob.

- **Perforation Warden — paper miniboss candidate:** T2.5/N1 source model, Meshy-reported humanoid rig/Walk/Run, and seven named action clips now exist under `Assets/SpaceAndCrafts/Art/Enemies/PerforationWarden/Meshy_Iter1951/`. The textured GLB measures **29,429 triangles** in one primitive; rigged/action GLBs contain one skin and 24 joints. The inspected thumbnail keeps the folded-star silhouette, purple limbs and planted boots. Model/rig/actions cost **41 credits**. Clip names and skin metadata are evidence of file contents only; no playback, deformation, grounded boot contact, Unity import, encounter, or behavior is verified. The crease-lane/folded barrier and card-stay counter remain design-only; large-body deformation is a risk.
- **Adhesive Cartographer — glue-roller enemy candidate:** a coral route-marked roller housing with integrated amber drum, purple face window, lilac limbs and mint boots. The inspected Meshy Iter1953 front render reads as a cohesive tape-dispenser-like enemy; it differs from the concept's coral/lilac and retains a broad T-pose. A **29,184-triangle** one-mesh GLB, successful 24-joint rig, free Walk/Run FBXs and seven-action bundle are saved under `Assets/SpaceAndCrafts/Art/Enemies/AdhesiveCartographer/Meshy_Iter1953/`. File metadata shows seven clip names and 72 channels each, not playback or good deformation. Proposed paper-duster counter/route-to-grip-strip encounter is still design-only; no Unity placement or AI. Full receipt: `Meshy_Iter1953/README.md`.
- Historical Hemline Harrier backlog entry (now modeled as Iteration 1991) proposed cutting a marked ribbon lane and stitching the recovery; the encounter remains design only.
- **Arcwright, the Chart Compass — enemy candidate:** the Meshy A-pose concept and textured T2.5/N1 model are saved at `Assets/SpaceAndCrafts/Art/Enemies/Arcwright/`. The inspected model preview keeps the compass arc, friendly face, purple limbs and grounded boots, with one dark slash artifact near the brow. Its converted GLB measures **28,557 triangles** in one mesh; Meshy returned a 24-joint rig, free Walk/Run, six catalog actions, and a custom Compass Sweep retarget. These files do not prove playback, circle-sweep leg motion, foot planting or Unity behavior. Splat's cardstock pivot wedge counter remains design-only. Full receipt: `Meshy_Iter1954/README.md`.
- **Comet Kiln — Meshy Iteration 1955 candidate:** a static workshop prop generated directly as a T2.5 smart-topology FBX and textured for **15 credits**. Its inspected thumbnail reads as a rounded plum paper-mâché kiln with cream-rimmed star window and coral lid/trim; small dark marks remain around the lid seam and little protrusions at the base. Target was 18k polygons; actual count is unmeasured. Saved in `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/CometKiln/Meshy_Iter1955/`. No Unity import/placement, camera-scale review or crafting behavior. A short finish animation/display beat remains only an idea.

## Additional workshop prop backlog

- **Starlight Drying Rail (Iteration 1958; static workshop dressing):** textured T2.5 FBX and preview are saved at `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarlightDryingRail/Meshy_Iter1958/`. The reusable prefab is installed on the `WorkshopHub` workbench at `Assets/SpaceAndCrafts/Prefabs/HomeWorkshop/StarlightDryingRail.prefab`; placement/bounds are recorded in `Logs/starlight-drying-rail-workshop-install-20261112.log`. Busy map texture and unmeasured 18k target remain limitations. It has no collider, interaction or progression behavior.
- **Boss-Souvenir Diorama Jar:** a stable single glass-and-card display case with a tiny connected world vignette and one collected boss emblem. Reward-display hook, distinct from existing map, mobile, and quilt props. Keep labels absent or large enough to read in-game.

## Iteration 1950 — Pointy the Pencil Sharpener (enemy candidate)

- Concept and textured low-poly model preserve one recognizable desktop-sharpener torso: coral triangular casing, large inlet, integrated deep-violet shaving drawer, small brass side screw, friendly face, lilac arms, purple legs, mint boots. Preview shows a dark inlet and slight drawer-texture noise; review-only.
- Meshy rigging returned a humanoid rig and free Walk/Run. Rigged and action GLB metadata confirms one skinned mesh, 24 joints, and seven clips: `Idle`, `Alert`, `Charged_Spell_Cast`, `Attack`, `Hit_Reaction`, `Dead`, `Rightward_Spin`. Playback/contact/Unity integration are not verified.
- Design-only hook: a marked shaving spray creates a temporary safe, low path, then Pointy pauses with its drawer open. No behavior is implemented. Full receipt: `Assets/SpaceAndCrafts/Art/Enemies/PointyPencilSharpener/Meshy_Iter1950/README.md`.
- Cost **44 credits** (3 + 15 + 5 + 21). Balance **1,227 total = 0 monthly + 1,227 permanent**; Usage API 403; no purchase.

## Iteration 1949 — Staple Hopper (concept-guided enemy redesign, candidate)

- Reworked the two 1888 failures by defining the stapler casing as one continuous character torso, with an integrated hinged arm, face window, purple limbs and planted mint boots. The model thumbnail now reads as a friendly stapler rather than a bug or blank blob; small black side marks, uneven arm/face proportions and visually small feet remain.
- T2.5/N1 smart-topology GLB measures **27,880 triangles** in one primitive. Meshy reports a humanoid rig plus Walk/Run and seven catalog actions: Idle, Alert, Jump-and-Slam, Attack, Hit, Dead, Spin Down/Stand Up. Clip names are present in the action GLB, but no playback, deformation or ground-contact review was performed. Proposed compress→marked hop/sweep→recovery is design-only; no enemy code or encounter exists.
- Candidate only, not adopted or integrated. Full assets and receipts: `Assets/SpaceAndCrafts/Art/Enemies/StapleHopper/Meshy_Iter1949/README.md`. Cost **50 credits**; balance **1,271 = 0 monthly + 1,271 permanent**. The concept task response said 0 consumed while the live balance fell 9, so spend is reconciled to balance change; subsequent task charges were 15 + 5 + 21. Usage API 403; no purchase.

## Iteration 1948 — Tiny Scrapbook Observatory (workshop decoration candidate)

- A single connected keepsake: open stitched midnight-blue atlas, lilac base, mounted brass/card telescope, and raised rainbow/leaf/mountain emblems. It could record explored-world souvenirs or decorate Splat's workshop, but that behavior is not implemented.
- The 4:3 concept and T2.5/N1 model thumbnail were inspected. The model remains a single open-book silhouette with attached scope and emblems, but the telescope points nearly straight up and the emblems shift to the opposite page and read less clearly. GLB is **24,904 triangles**, one primitive; no correction was generated. Candidate only; no Unity placement, lighting/material import review, interaction, build or human play. Receipt: `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/TinyScrapbookObservatory/Meshy_Iter1948/README.md`.
- Cost **18 credits** (3 concept + 15 model). Balance **1,321 = 0 monthly + 1,321 permanent**, reset 2026-12-10 21:11:17 UTC; Usage endpoint 403. No purchase.

## Iteration 1947 — Dabbler, paintbrush gremlin (enemy candidate)

- The roster lacked a paintbrush-bodied enemy, though Brushstroke Pathfinder is a paint-themed ally. Dabbler is one cohesive indigo brush-handle character with an integrated cream ferrule, teal/coral bristles, friendly black face, compact lilac arms and planted purple/coral boots. Concept and model thumbnails were inspected; model/rigged GLBs measure **25,125 faces** (one mesh instance). Eyes are slightly uneven and arms look long from the thumbnail.
- Meshy reports a humanoid rig plus free Walk/Run outputs and a same-rig seven-clip bundle: Idle, Alert, Charged Spell Cast, Attack, Hit Reaction, Dead, Stand Up 1. The action GLB metadata confirms those seven clip names; playback, skinning/contact and transitions remain unchecked.
- A clearly telegraphed bristle flick and temporary broad paint-daub footholds could give Splat a safe route across a gap; idea only, not implemented. Files/task IDs: `Assets/SpaceAndCrafts/Art/Enemies/DabblerPaintbrushGremlin/Meshy_Iter1947/README.md`. Cost **50 credits**; balance **1,339** (all permanent), Usage API 403. No Unity import or player-facing gameplay evidence.

## Iteration 1946 — Orbit-Maker's Chair (workshop furniture candidate)

- Concept and textured T2/N1 model are saved under `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/OrbitMakersChair/Meshy_Iter1946/`. The concept is a cozy crescent-back work chair with cream star patch, turquoise orbit stitch, coral side pocket, and mint legs. The model thumbnail loses the pocket, shows dark face gaps, and obscures one of four legs. Face count unmeasured; keep unapproved.
- Cost 24 credits (9 + 15); balance 1,389, all permanent. Usage API 403. No Unity placement/function/build/render/play verified.

## Iteration 1945 — Comet-Cuddle Floor Pouf (rejected drafts)

- Concept image shows a cozy crescent-shaped indigo/lavender floor cushion with a cream star patch, turquoise orbit stitch and sewn coral pocket. The T2/N1 15k-target output measures 15,805 faces and the thumbnail loses the crescent/pocket details. A Meshy 7.1 4K PBR retry measures 554,986 faces and still reads as a flattened U with details obscured. Keep both as rejected/review-only drafts; don't promote or remesh these meshes. Cost 54 credits. Balance 1,413 total, all permanent. Full notes: `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/CometCuddlePouf/Meshy_Iter1945/README.md`.

## Iteration 1944 — Crayon Comet

- Developed the planned wax-crayon creature into a Rainbow Kingdom miniboss candidate. Textured 29,076-face T2.5/N1 model, Meshy-reported humanoid rig/Walk/Run, and seven same-rig catalog actions (Idle, Alert, Charged Spell Cast, Attack, Hit Reaction, Dead, Stand Up) are saved. The inspected previews keep a distinct crayon-body silhouette, cream star wrapper, wax tip, friendly face, lavender limbs and turquoise comet swoosh; a small dark facial mark remains.
- Encounter seed: trace a bright color lane → make one readable wax dash → cool/dull recovery lets Splat redirect it into a safe crossing. Idea only; no boss logic/arena/invention use. Playback, skinning/contact, Unity integration, build/render and human play are unverified. Cost 50 credits; current balance 1,467 total (0 monthly + 1,467 permanent). See `Assets/SpaceAndCrafts/Art/Enemies/CrayonComet/Meshy_Iter1944/README.md`.

## Iteration 1943 — Loop-Lock, the Tape-Dispenser Troublemaker

- New tape-themed miniboss candidate with a turquoise dispenser body, visible coral spool/window in the concept, cream face-label, folded star crest, lilac arms and purple legs. Meshy delivered a 29,076-face textured T2.5/N1 model, reported humanoid rig/Walk/Run, and seven clips (Idle/Alert/Charged Spell Cast/Attack/Hit/Dead/Stand Up). The thumbnail keeps the friendly face and compact silhouette but loses spool visibility from the front; art review remains open.
- Encounter seed: spin-up telegraph → short tape-lane snap → pause for Splat to peel/redirect a safe strip. Design only; no AI/arena/ability/integration. Cost 50 credits; live balance 1,517 (0 monthly + 1,517 permanent). No playback/contact verification. See `Assets/SpaceAndCrafts/Art/Enemies/LoopLockTapeDispenser/Meshy_Iter1943/README.md`.

## Iteration 1942 — Cometglass Patchwork Suncatcher

- Home workshop decoration candidate: an indigo circular paper frame around eight stitched pastel tissue wedges and a layered cream comet-star. The source preview reads clearly; dark slivers remain between wedges and the stitch details are uneven.
- Textured Meshy 7.1 PBR source model measured 1,128,898 faces; a 30k-target remesh measured **27,652 faces**. Its remeshed GLB is still about 47 MB due to 4K textures. The milestone-reactive rainbow-light effect is only a design idea. No Unity placement, interaction, progression, build/render, or human play. Cost 44 credits; live balance 1,567 total (0 monthly + 1,567 permanent). See `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/PrismPatchworkSunCatcher/Meshy_Iter1942/README.md`.

## Iteration 1941 — Whirl, folded-paper wind scout

- New Rainbow Kingdom ally candidate: a cohesive four-petal pinwheel body with cream printed face hub, tiny star cap, integrated lavender arms, purple legs and coral boots. Meshy delivered a textured 29,189-face GLB, rigged FBX/GLB, Walk/Run outputs and a six-action bundle (Idle, Big Wave Hello, Charged Spell Cast, Happy Jump, Victory Cheer, Pick Up). Concept/model thumbnail review: the silhouette reads cleanly, but a dark crease across the face, a dark petal opening, and wide wing-like arms need art review.
- Gameplay seed: show the safe gust route with a short spin-up and marked wind lane. Design-only; no dialogue, interaction, gust, route puzzle, Unity integration or behavior is implemented. Animation playback, skinning/contact and transitions are unverified. Cost 47 credits; verified balance 1,611 (0 monthly + 1,611 permanent). Full evidence: `Assets/SpaceAndCrafts/Art/Characters/WhirlPaperPinwheelScout/Meshy_Iter1941/README.md`.

## Iteration 1940 — Melt Marshal (hot-glue-gun boss candidate)

- Added a textured 29,000-triangle Meshy T2.5/N1 model from a reviewed concept, plus Meshy-reported humanoid rig, free Walk/Run, and Idle/Alert/Charged Spell Cast/Attack/Hit/Dead/Stand Up actions. It fills a distinct hot-glue-gun silhouette gap among the existing scissors/paper/glue cast. The thumbnail retains its coral/cream body, glue nozzle/bead, trigger and broad boots, but only one eye is visible in profile and the T-pose arms are wide.
- Hook: visibly charge and lay a ribbon across a marked lane, then create a recovery window for Splat to cool/redirect the glue into a safe bridge. Design-only; no boss, AI, arena, sound/VFX or Unity behavior implemented. Playback/contact/import unverified. Cost 50 credits; balance 1,658, all permanent. See `Assets/SpaceAndCrafts/Art/Enemies/MeltMarshal/Meshy_Iter1940/README.md`.

## Iteration 1939 — Foldscope Starfinder

- New workshop-prop candidate: a rolled-cardboard indigo telescope with coral focus collar, turquoise folded tripod cradle, tiny constellation dial and cream star fin. Textured T2/N1 FBX/GLB under `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/FoldscopeStarfinder/Meshy_Iter1939/`. Measured 14,864 triangles. Thumbnail retains the palette and overall connected prop, but its lens detail is hard to read side-on.
- Possible use: give Splat a way to preview distant route silhouettes or optional craft-material glints from the workshop. Design-only; no Unity placement/function. Cost 24 credits.

## Iteration 1938 — Lumi, the Star Lantern Scout (ally)

- Compact folded-paper lantern character with amber body, star window, simple black face, integrated lilac limbs, broad boots and a woven sash. Textured T2.5/N1 model and Meshy-reported rig/Walk/Run plus Idle/Jump/Wave/Pick Up/Cheer/Agree clips are saved under `Assets/SpaceAndCrafts/Art/Characters/LumiStarLanternScout/Meshy_Iter1938/`.
- Measured rigged GLB: **29,344 triangles**. Thumbnail caveats: star window appears dark, red thread protrudes from sash, arms extend wide. Role idea: help Splat restore Rainbow Kingdom night-waymarks and reveal optional craft scraps. Not implemented; no playback/contact/import verified.

## Iteration 1937 — Rubberstar, the Starlight Eraser

- Miniboss candidate: a single coral eraser-body humanoid with a star/planet paper wrapper, expressive face and integrated lilac limbs/boots. Concept and model thumbnail reviewed; the shaving mantle is mostly lost and dark side marks remain. T2.5/N1 model, measured 28,505 triangles, Meshy-reported humanoid rig/Walk/Run and six action clips are saved in `Assets/SpaceAndCrafts/Art/Enemies/RubberstarStarlightEraser/Meshy_Iter1937/`.
- Hook: telegraph a glowing erase lane, leave a visible safe lane, then create a counter window to ink back a route. Idea only; no boss behavior, story, arena or reward is built. Cost 47 credits. Playback, contact, Unity import and gameplay unverified.

## Iteration 1933 — Piñata Comet Marshal

- Tissue-fringe Rainbow Kingdom miniboss candidate with broad feet, concept starburst chest, and colorful craft materials. Concept/model thumbnail reviewed; model thumbnail loses the starburst and has dark lower-jaw marks. Textured T2.5/N1 28k-target FBX, Meshy-reported rig/Walk/Run, and six same-rig Idle/Alert/Charged Ground Slam/Hit/Dead/Stand Up actions are saved. The requested topology target, playback, skinning/contact, Unity import, and combat are unverified.
- Hook: chest-star charge, marked slam lane, badge-dim recovery; design only. Cost 47 credits. Historical balance after generation 1,992 (272 monthly + 1,720 permanent); Usage endpoint 403. Review-only, no score change. See `Assets/SpaceAndCrafts/Art/Enemies/PinataCometMarshal/Meshy_Iter1933/README.md`.

## Iteration 1934 — The Great Paste Pot (rigged redesign)

- A fresh, more mascot-like pass on the older Iteration 1899 glue-pot boss: pearlescent turquoise pot body, attached cream star-map label/face, coral lid, integrated lavender limbs, and broad soles. Meshy concept and model thumbnails inspected; the model keeps the overall silhouette, with dark/creased marks on the label and elongated hands.
- Textured T2.5/N1 FBX and conversion GLB are local. Source GLB measures **28,605 triangles** (28k target; 605 over, below the 300k rig limit). Meshy reports a genuine humanoid rig, separate Walk/Run files, and same-rig Idle/Alert/Charged Ground Slam/Hit/Dead/Stand Up actions.
- Encounter hook: lid-glow telegraph → paste arc into a marked lane → dim-lid recovery; Splat could redirect/peel the ribbon into a safe traversal strip. No behavior, sound, story, arena, counter, or reward is implemented. Playback, deformation, planted contact, Unity import, and game rendering remain unverified. 48 credits. See `Assets/SpaceAndCrafts/Art/Enemies/GreatPastePot/Meshy_Iter1934/README.md`.

## Latest candidate — Iteration 1928: Rollo, the Runaway Rule

- Golden measuring-tape humanoid with a printed black face, visible graduated ticks, and coral/mint tip details. Concept and textured-model thumbnail inspected. Rigged model, separate Walk/Run, and six-action Idle/Alert/Attack/Hit/Dead/Stand Up bundle saved; rigged GLB measures 29,240 triangles. Proposed tape-lane snap/recoil counter is only an idea. No playback, Unity integration, encounter behavior, or game render verified. Total cost 47 credits; live balance 2,219. See `Assets/SpaceAndCrafts/Art/Enemies/RolloRunawayRule/Meshy_Iter1928/README.md` and tracker.

## Latest tentative ally — Iteration 1908: Squat Mail-Runner

- Repo search found no Squat concept/model; the Story Bible only establishes that he is Splat's brother. A tentative sibling visual was made from the approved white-body/purple-limbs language, with a coral backwards cap, mint star, and black features. The first draft's hands/arms were too long, so a compact mitten-limb version was modeled. Textured T2.5/N1 GLB, Meshy-reported genuine rig, free Walk/Run, and six actions are staged. Small cyan body mark; playback/contact/import unverified. **Not canon; needs Mac/user approval.** A mailbox exchange role is only an idea. Cost 44 credits. Files: `Assets/SpaceAndCrafts/Art/Characters/Squat/Meshy_Iter1908/`.

## Latest candidates — Iterations 1905–1907

- **Star Scrap Chest (1905/1907):** cream/violet constellation cardboard chest with coral straps and a mint star latch. Concept plus T2.5 draft, Meshy 7.1 model, N1 texture revision, and 30k-target remesh. The 4.82 MB remesh retains the inspected thumbnail shape, though the lid/texture still trail the concept; the higher-detail variants are ~20 MB. Keep review-only; no storage/reward function or home placement. Cost 63 credits. Files: `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarScrapChest/Meshy_Iter1905/`.
- **Orbit Postbox (1906):** folded-cardboard rocket mailbox with mint star slot, postcard, coral signal flag, and constellation stitching. Textured Meshy 7.1 model plus a 30k remesh (10.12 MB vs 62.15 MB source). Preview inspected; flag appears edge-on. Could carry postcards and boss/world souvenirs between Splat and brother Squat, but the story/message/reward system is only an idea. Cost 38 credits. Files: `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/OrbitPostbox/Meshy_Iter1906/`.

## Latest candidate — Iteration 1904: Punchline the Hole-Puncher

- A rounded violet hole-punch body with coral top handle, mint grips, lavender boots, and a star-shaped punch on its chest. Textured T2.5/N1 model, genuine humanoid rig, free Walk/Run, and same-rig idle/prep/attack/hit/dead/recovery clips are staged. Thumbnail inspected; a small dark mark remains by the handle. Playback and integration are unverified. Proposed encounter: marked star-punch lanes and a patchable floor during recovery; mechanic is not implemented. Cost 41 credits. Files: `Assets/SpaceAndCrafts/Art/Enemies/PunchlineHolePuncher/Meshy_Iter1904/`.

## Latest candidate — Iteration 1903: Mica the Moon-Rabbit Cartographer

- Pearl-white/lavender folded-paper ally with map sash and coral compass. Textured smart-topology GLB, genuine Meshy humanoid rig, free Walk/Run, and same-rig Idle/Jump/Wave/Agree/Pick-Up/Cheer clips saved under `Assets/SpaceAndCrafts/Art/Characters/MicaMoonRabbit/Meshy_Iter1903/`. One dark cheek artifact remains. No playback, foot-contact, route-guidance behavior or integration verified. Cost 41 credits.

## Latest candidate — Iteration 1902: Cosmic Journey Keepsake

- Textured PBR wall shadowbox FBX showing the Rainbow Kingdom, Leaf Lands, and Mountain Lands as layered paper scenes with three empty star slots. Both concept and model previews inspected. Possible home display for one star per defeated world; that progression/reward behavior is not built. 33 credits. Files under `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/CosmicJourneyKeepsake/Meshy_Iter1902/`.

## Latest candidate — Iteration 1901: Washi Star-Spider

- Textured PBR Meshy 7.1 multi-image FBX from a three-view washi concept. Body is one folded-paper shell with smooth curved legs, printed face and coral bow; thumbnail inspected. Only about six legs read clearly, rainbow seams are subtle, no rig or behavior. 33 credits; candidate only. Files and task receipts under `Assets/SpaceAndCrafts/Art/Enemies/WashiStarSpider/Meshy_Iter1901/`.

## Latest candidate — Iteration 1900: Snip-Snap Sellsword

- Rigged biped enemy built around a paper tunic, rounded scissor-crown silhouette, mint handle-loop shoulders, and wide boots. Meshy supplied free Walk/Run plus same-rig Idle, attack wind-up, attack, hit, defeat, and get-up clips. Concept/model thumbnails inspected; no playback or game integration. 41 credits. See asset README.

## Iteration 1897 — ally concept

- **Kiri Foldstar:** cream/indigo/coral one-sheet origami engineer. Genuine 1.2 m humanoid rig and Walk/Run plus Idle/Jump/Jumping Down/Wave/Victory/Pick-Up clips are saved. Hook: teach Splat to pin/fold torn paper causeways into safe crossings. Concept and FBX/GLB candidate: `Assets/SpaceAndCrafts/Art/Characters/KiriFoldstar/Meshy_Iter1897/`. No playback or game behavior verified.

## Iterations 1892–1896 — workshop and enemy concepts

- **StarSeal Press (1892):** compact ringed-planet clamshell press for stamping world-completion stars onto a travel keepsake card. The candidate thumbnail has a turquoise star, not the intended rainbow; turning the lever and collecting/placing the card are unimplemented. Preview and textured FBX/PBR maps: `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarSealPress/Meshy_Iter1892/`.
- **Snip-Snap Safety Shears (1894):** a living, friendly pair of scissors, with the two rounded blades and mint handle loops joined at an expressive coral hinge. Game hook: widen into a clear clamp tell, snap along a marked lane, then pause open for a safe counter window. Model is non-humanoid and has no rig; manual hinge animation is only proposed. Preview/model: `Assets/SpaceAndCrafts/Art/Enemies/SnipSnap/Meshy_Iter1894/`.
- **Ribbonwake (1895):** continuous folded washi ribbon serpent with constellation pattern and stitched coral/turquoise borders. Its brighter retexture improves edge visibility, but the thumbnail still does not show a readable face and the model is too small in frame; keep as a rough visual candidate. The swept-stitch lane / coiled recovery bridge is only a boss idea. Files: `Assets/SpaceAndCrafts/Art/Enemies/Ribbonwake/Meshy_Iter1895/`.
- **Nebula Noodle (1896):** a single S-shaped school-glue creature, with a lavender translucent-looking body, embedded face and small flecks. Candidate hook: telegraph a sticky lane that Splat can scrape or redirect into a short bridge. Candidate FBX/preview: `Assets/SpaceAndCrafts/Art/Enemies/NebulaNoodle/Meshy_Iter1896/`. It has no deformation rig or implemented behavior.
- Iter1893 was rejected: extra origami feet detached from the scissors. Future craft enemy/boss idea: **The Great Paste Pot**, a single squeeze-bottle glue creature that telegraphs a glue arc, then leaves temporary adhesive lanes that Splat can redirect into a traversal bridge. This is a design seed, not a generated asset or implemented encounter.

## Iteration 1891 — Brushstroke Pathfinder

- **Brushstroke Pathfinder** — friendly chibi artist ally with violet smock, turquoise cuffs, coral footwear and rainbow brushstroke chest emblem. T2.5 remesh measures **29,267 faces**; genuine 1.2 m humanoid rig, free Walk/Run, and same-rig Idle/Wave/Agree/Pick Up/Victory Cheer clips are local. Preview inspected; reads as a generic artist, not brush-shaped. Design hook: mark safe route segments with colorful paint, not implemented. See `Assets/SpaceAndCrafts/Art/Characters/BrushstrokePathfinder/Meshy_Iter1891/README.md` and receipts.
## Iteration 1890 — Washi Paper Drake (Ribbon Drake concept unresolved)

- **Washi Paper Drake** — candidate reinterpreted from the intended continuous Washi Tape Ribbon Drake after Meshy generated a cohesive small horned dragon/lizard instead. Textured render is coral/aqua/white/yellow; washi/origami details are faint, so it is not accepted as the ribbon brief. T2.5 30k model, texture and remesh are local; final mesh measures **29,047 faces**. No compatible creature rig/actions. See `Assets/SpaceAndCrafts/Art/Enemies/WashiTapeRibbonDrake/Meshy_Iter1890/README.md` and receipts.
## Iterations 1888–1889 — Staple Hopper rejected; Orbit rocket decor

- **Staple Hopper:** the two T2.5 drafts and texture pass in 1888 failed (bug-like first model, blank blob second); see `Meshy_Iter1888/README.md`. A concept-guided redesign was generated in Iteration 1949; it now reads clearly as a stapler creature and has a reported rig/clip bundle, but remains a candidate pending playback/contact and gameplay-scale review. Do not treat either iteration as production-ready.
- **Orbit Supply Caddy:** textured Meshy output reads as a clean deep-violet/coral/cream toy rocket with an aqua porthole, but the open organizer well is not shown. Keep as a decorative rocket candidate, not a functional organizer. Files: `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/OrbitSupplyCaddy/Meshy_Iter1889/`; 15-credit receipts in tracker.
## Iteration 1887 — Starstitch Sentry

- **Starstitch Sentry** — a single-body violet quilt-comet midboss with broad sewn mantle sleeves, amber eyes and a star seam. T2.5 Smart Topology model measures 31,156 faces; genuine rig, free Walk/Run and same-rig Idle/Jump-and-Slam/Charged-Slam/Hit/Dead/Recovery bundle are local. Previews inspected. Visual caveat: reads owl/bat-like, and the rainbow seam is faint; do not treat as final without art review.
- Encounter hook: mark a clear stitch lane, visibly charge, slam, then expose a loose seam as the counter window. Not implemented. Receipts in `docs/MESHY_CREDIT_TRACKER.md`; files under `Assets/SpaceAndCrafts/Art/Enemies/StarstitchSentry/Meshy_Iter1887/`.
## Iteration 1886 — Arc Scout ally

- **Arc Scout** — friendly Rainbow Kingdom guide with a pale aqua protractor-arch body, integrated violet face/hands/feet and engraved angle marks. The refined three-view concept removes the initial display base. Textured Meshy model, 286,910-triangle remesh, genuine humanoid rig, free Walk/Run, and same-rig Idle/Wave/Agree/Pick Up/Victory Cheer are local; concept/model previews inspected.
- Design hook: indicate a safe jump arc and landing zone, then wave/celebrate when Splat reaches it. This behavior is not implemented or play-tested. Candidate only; task receipts and limits in `Assets/SpaceAndCrafts/Art/Characters/ArcScout/Meshy_Iter1886/README.md` and credit tracker.
## Iteration 1885 — Rocket Patch Quilt

- **Rocket Patch Quilt** — a padded indigo wall keepsake embroidered with the three destination patches and a stitched star/rocket route. Concept and textured static Meshy preview inspected; small dark edge artifacts remain. GLB measures 30,913 triangles, above 30k. Could add a world patch after each boss to make progress visible at home; unlock/interaction/hanging/placement are not implemented. Candidate: `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/RocketPatchQuilt/Meshy_Iter1885/`; receipts in the credit tracker.
## Iteration 1884 — Carbon-Copy Mimic

- **Carbon-Copy Mimic** — a connected ivory ruled worksheet humanoid with curled paper corners, graphite face, violet binder tab. Visual design and Meshy preview inspected; 283,048-triangle remesh, genuine humanoid rig, matching Walk/Run, Idle, Jump, Attack, Hit, Dead and recovery action files are staged. Hook: copy Splat's last safe route after a visible graphite trace. No AI, route-history system, attack telegraph, animation playback or game integration. Candidate at `Assets/SpaceAndCrafts/Art/Enemies/CarbonCopyMimic/Meshy_Iter1884/`; receipts in tracker.
## Iterations 1882–1883 — workshop and world-select props

- **Orbit Desk Lamp** — a cohesive folded-vellum lantern over a violet planet base, silver support, crescent cutouts and ringed-planet finial. Local T2.5/N1 FBX/GLB measured at 30,219 triangles; static prop candidate. Lamp emission, stability, scale, lighting and room use are unreviewed.
- **Fold-Out World Atlas** — clothbound interactive-storybook concept with map panels for Rainbow Kingdom, Leaf Lands and Mountain Lands. Local T2.5/N1 FBX/GLB measured at 28,053 triangles. Generated view partly occludes Mountain Lands, so readability must be fixed/reviewed before adoption. World-select interaction is a design idea only.
- Candidates at `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/OrbitDeskLamp/Meshy_Iter1882/` and `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/FoldOutWorldAtlas/Meshy_Iter1883/`; exact receipts in tracker. Neither is in production.
## Iteration 1881 — Ream Keeper paper guardian

- **The Ream Keeper** — folded-paper stack guardian in ivory and deep-indigo layered cape armor. Boss hook: mark a broad floor lane, show a raised-arm stamp wind-up, strike, then visibly unstack vulnerable corners for a counter window. Meshy turnaround, textured model, 276,580-triangle remesh, genuine rig, free Walk/Run, and same-rig Idle/Attack/Charged Ground Slam/Hit/Dead/Spin Down and Stand Up clips are staged under `Assets/SpaceAndCrafts/Art/Enemies/ReamKeeper/Meshy_Iter1881/`. No playback, skinning, arena, AI, safe-lane tell, weak point, reward, or boss integration is verified. Candidate only; receipts in `docs/MESHY_CREDIT_TRACKER.md`.
# Craft-Cosmos Asset Ideas

## Iteration 1936 — Orbiting Craft Mobile (workshop decor, review-only)

- A compact hanging centerpiece: purple crescent hook, curved indigo arm, and short strings holding a coral ringed planet, turquoise moon, golden star, and small rocket charm. Concept and model thumbnails inspected; model preserves the composition but strings cross near the rocket and the ringed planet appears edge-on.
- Textured T2/N1 smart-topology FBX/GLB saved under `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/OrbitingCraftMobile/Meshy_Iter1936/`. Measured GLB 16,191 triangles against a 15k target; no remesh. Cost 24 credits. Placement and gentle rotation in the workshop are ideas only; no Unity import or scene use.



A living, overlap-checked idea list for Space & Crafts. Keep the handmade materials legible as crafted space-age characters, not disconnected primitive pieces. The current game includes scissors, glue, tape, paper/origami, spiders, and scrap characters, so new work should add a readable role or mechanic.

## Iteration 1931 — Purl, the Sock-Puppet Showstopper

- Added a knitted-material boss candidate to diversify the cast beyond paper, scissors, glue, and tape. The inspected concept/model thumbnail show a one-piece indigo sock puppet with integrated limbs, embroidered face, cream repair patch, and soft planted-looking feet. Textured T2.5/N1 28k-target model, Meshy-reported humanoid rig/Walk/Run, and same-rig Idle/Alert/Attack/Hit/Dead/Stand Up action bundle are saved.
- Possible Echo Atelier boss: wind both sleeves inward, sweep a padded arm along a visible sewn lane, then slump as the chest star seam opens for a counter. The encounter is only an idea. One small dark mark appears below an eye; face count, playback, skinning, contact, Unity integration, and behavior are unverified. Cost 47 credits; balance 2,086. See README and tracker.

## Iteration 1930 — Rainbow Kingdom Pop-Up Keepsake

- Created a concept and textured Meshy 7.1 FBX for a compact open storybook decoration: attached layered-paper castle, continuous rainbow bridge, hills, teal fjord ribbon, and small star details. The concept and model thumbnail were inspected; the main pop-up silhouette and world landmarks survive in the generated preview.
- Possible workshop progression: place one earned world-star sticker after each boss. That reward loop, inventory, sticker placement, scale, lighting, and room integration are not implemented or reviewed. Static candidate, 73.79 MB FBX, topology unmeasured. Cost 39 credits; balance 2,133. See README and tracker.

## Iteration 1929 — Gloopstick, the Cap-Flip Caper

- Made a concept, textured T2.5/N1 smart-topology model (28k target), Meshy-reported humanoid rig with Walk/Run, and same-rig Idle/Alert/Attack/Hit/Dead/Stand Up bundle. Concept and model thumbnail inspected: one connected lavender glue-stick figure with integrated limbs, cream tip, swept cap, printed face, twist control, and broad feet.
- Proposed attack: wind the twist base, paint an S-shaped tacky lane, skate it, then recover with a visibly skewed cap. This is a hook only; AI, tell, floor hazard, counter, story, and integration are not implemented. Target face count, animation playback, skinning, feet, Unity import, and in-game view remain unverified. Total cost 47 credits; balance 2,172. See README and tracker.

## Iteration 1927 — Stencil Squire

- Created a one-piece indigo cardstock humanoid mini-boss with printed face, stitched seams, and a coral star chest patch. Concept/model thumbnails were inspected. Textured 28k-target T2.5/N1 FBX, Meshy-reported humanoid rig, free Walk/Run, and same-rig Idle/Charged Ground Slam/Attack/Hit/Dead/Spin Down-and-Stand-Up actions are saved in `Assets/SpaceAndCrafts/Art/Enemies/StencilSquire/Meshy_Iter1927/`.
- Proposed tell→star-lane sweep→exposed chest-star recovery is design-only. 28k target is not measured; animation playback, skinning, foot contact, Unity import, controller, arena, rewards, and narrative remain unverified. Cost 47 credits; see README/tracker.

## Iteration 1926 — Nightstitch, the Star-Web Weaver

- Created three views and a textured Meshy 7.1/PBR FBX for a friendly, folded-washi spider. Inspected views show one domed body and eight countable, tapered folded legs; the model thumbnail retains the face, stitched star-web abdomen, and unified silhouette. Files: `Assets/SpaceAndCrafts/Art/Enemies/NightstitchStarWebWeaver/Meshy_Iter1926/`.
- This intentionally replaces the previous spider visual direction's disconnected primitive read. It's still only a static model candidate: 51.69 MiB FBX, topology unmeasured, no rig/animation, Unity import, grounded contact, or enemy logic. Use an authored eight-leg rig, not whole-object wobble.
- Encounter idea: visibly tensioned web pads telegraph a safe route; after a web sweep, the spider exposes its unspooled star-web abdomen for a counter window. Mechanics are not implemented. Cost 39 credits; balance 2,313. See the credit tracker and candidate README.

## Iteration 1925 — Craft-Memory Garland

- Four stitched destination pennants (Rainbow Kingdom, Leaf Lands, Mountain Lands, and the star route home) hang from one continuous golden yarn cord with end loops. Concept and textured `meshy-7.1` PBR FBX thumbnails are saved and reviewed under `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/CraftMemoryGarland/Meshy_Iter1925/`.
- The decoration could show visited worlds and earned route stars in the workshop. This progression hook, wall attachment, scale, lighting, material import, and placement are not implemented or verified. Static candidate; cost 39 credits. See `docs/MESHY_CREDIT_TRACKER.md`.

## Iteration 1879 — scissors and glue enemy candidates

- **SnipSnap Sellsword** — purple/coral armored scissor-raider with a tapered silver shear-blade crest and scissor-hinge chest insignia. Three turnaround views and a textured Meshy model preview were inspected. Local FBX, PBR maps, GLBs, 250k-target remesh, genuine humanoid rig, free Walk/Run, and six same-rig actions (Idle, Alert, Attack, Hit, Dead, Spin Down/Stand Up) exist. Post-remesh count, skinning, playback, telegraphs, and encounter behavior remain unverified. Encounter hook: two-step clamp tell; blades flare, snap shut along a marked lane, then the raider recoils with a clear opening.
- **Captain PuddlePaste** — friendly single-body pearly-lavender glue outlaw with violet gloves/boots and a small golden droplet crest. Three turnaround views and a textured model preview were inspected. Local FBX/maps/GLB, measured 257,052-triangle mesh, genuine rig, free Walk/Run, and seven same-rig actions (Idle, Alert, Spell Cast, Ground Slam, Hit, Dead, Spin Down/Stand Up) exist. Playback, ground contact, glue hazard, and encounter integration remain unverified. Encounter hook: an adhesive puddle and slow arcing drops telegraph its attack; its crest dims during recovery.
- Asset folders: Assets/SpaceAndCrafts/Art/Enemies/SnipSnapSellsword/Meshy_Iter1879/ and Assets/SpaceAndCrafts/Art/Enemies/CaptainPuddlePaste/Meshy_Iter1879/. Exact receipts are in docs/MESHY_CREDIT_TRACKER.md. Neither is in a production scene.
## Iteration 1880 — ally and home keepsake

- **Eraser Pup** — a soft ivory/coral bipedal puppy-like helper with teal scarf, violet boots, and charcoal nose. Its concept and textured Meshy model preview were inspected. Local FBX/maps, 250k-target GLB remesh measured at 259,429 triangles, genuine rig, free Walk/Run, and same-rig Idle 15, Wave, Pick Up, and Victory Cheer are staged. Still needs skinning/playback review and game behavior. Intended help: erase temporary pencil/chalk hazards and point toward safe paths.
- **Constellation Star Mobile** — balanced brushed-paperclip frame suspending folded Rainbow, Leaf, and Mountain stars plus a ringed planet. Concept and textured Meshy model preview were inspected; FBX/maps are local. Scale, attachment/hanging point, room lighting, interaction, and scene placement are unreviewed.
- Candidate paths: Assets/SpaceAndCrafts/Art/Characters/EraserPup/Meshy_Iter1880/ and Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/ConstellationStarMobile/Meshy_Iter1880/. Receipts: docs/MESHY_CREDIT_TRACKER.md. Neither is in production.
## Newly generated candidates — Iteration 1876

- **Pencil Comet** — yellow pencil hero-villain with purple boots. Proposed enemy tell: it draws a dotted comet-path on the floor, then dashes along that path and needs a short eraser-stop recovery. Model, 150k-target GLB remesh, genuine humanoid rig, free Walk/Run, and Idle candidate exist; five action clips are still processing.
- **Paper-Jam Marquis** — cobalt and ivory folded-paper boss with a theatrical collar and cape. Proposed attack: a paper fan announces the safe lane, then folded panels sweep across the arena; unfolded, visibly weak state is the damage window. Model, 150k-target GLB remesh, genuine humanoid rig, free Walk/Run, and Idle candidate exist; six boss action clips are still processing.
- **Kite Manta** — gentle lavender-blue paper kite/manta guide. Proposed help: wing tilt points out a safe wind lane or hidden route; should orbit near Splat rather than become an unresponsive follower. Model candidate exists; no flight rig yet.
- **Orbit Supply Caddy** — rocket-shaped workshop organizer with craft supplies. Proposed home use: the silhouette makes storage readable and its compartments can reveal collected materials; static prop candidate only.

## Next concepts to explore

### Margin, the Storykeeper (Iteration 1932; review-only ally candidate)

- A friendly character shaped as one softly rounded, clothbound sketchbook with a single visible bookmark that trails into an integrated scarf; pages are layered into the body silhouette rather than floating around it. A tiny ink-drawn face and stitched corners keep the craft theme legible without another tool-as-a-biped design.
- Story role: Margin remembers Splat's experiments and retells each world boss's lesson as a short optional page at the workshop. It supports teenage inventor Splat's arc and gives Squat a reason to send short notes home, without providing required instructions or replacing discovery.
- Animation brief if selected: quiet page-edge breathing in idle, small nods during dialogue, grounded walk/run with the book body held stable, a gentle landing with both feet planted, and a page-turn gesture for story reveals. The bookmark can lag subtly, but the whole body should not flap like a prop.
- Concept and textured T2.5/N1 FBX are saved; Meshy reports a successful humanoid rig with Walk/Run and a same-rig six-action FBX bundle (Idle, Happy Jump, Wave, Pick Up, Cheer, Stand Up). The concept/model thumbnail reads as a single friendly book-shaped character with stitched corners and a coral bookmark wrap. Arms/hands are noticeably long and finger-like, so do not promote without reviewing or improving proportions.
- Proposed workshop story role: optionally retell a boss's lesson or Splat's invention experiments. Dialogue and story-page behavior are design-only. Candidate is not canon or production art; playback, contact/foot planting, topology, import and gameplay are unverified.
- Files: `Assets/SpaceAndCrafts/Art/Characters/MarginStorykeeper/Meshy_Iter1932/`. Cost **47 credits**; see README and `docs/MESHY_CREDIT_TRACKER.md`.

### Enemies and bosses

- **Crayon Comet** — now developed as Iteration 1944 (candidate only; the paint-lane dash/cooling counter remains a design hook, not implemented). See the entry above and its asset README.
- **Staple Hopper** — a small, springy binder-staple creature. It visibly compresses and points its legs before a short hop, lands on marked floor pads, then pauses. A floor enemy to contrast the existing spider's legs.
- **Washitape Ribbon Drake** — one continuous paper-tape ribbon creature that loops around columns and temporarily binds a route. It telegraphs with a slack loop before tightening; the player can use the invention to redirect it. Avoid a segmented “shapes shoved together” body.
- **Carbon-Copy Mimic** — a flat folded worksheet enemy that copies the player's last safe movement pattern, leaving a visible pencil guide first. It teaches timing through readable repetition, not sudden damage.
- **The Ream Keeper** — a giant paper-stack guardian for a later optional vault. Sheets slide as a single layered body; it stamps broad, readable lanes into a paper arena and pauses while its corners unstack. Distinct from the Marquis by a heavy, block-like silhouette and orderly paper mechanics.
- **Eraser Pup ally** — a rounded pale eraser-creature with a single colored sleeve and pencil-dust freckles. It clears a short-lived drawn hazard after Splat marks it, without fighting for player attention or replacing Splat's actions.

### Allies and home decorations

- **Constellation Star Mobile** — a ceiling-hung handmade mobile from folded paper stars, thread and one tiny planet. Slow rotation catches home lighting; stars can be earned as world souvenirs.
- **Craft-Memory Garland** — a wall display whose pennants are made from collected world materials. A persistent visual record of exploration and crafting rather than another inventory panel.
- **Orbit Desk Lamp** — a soft, paper-lantern spacecraft lamp with an adjustable crescent shade. It can change warmth as Splat completes milestones; keep it a single cohesive lamp asset.
- **Fold-Out World Atlas** — a tabletop paper map that opens into little crafted silhouettes for Rainbow Kingdom, Leaf Lands and Mountain Lands. It can host the world-select interaction and story annotations.
- **Rocket Patch Quilt** — sewn patches and star routes form a readable home-world map; new patches appear after boss victories. Distinct from another bed model by emphasizing collectible story progression.
- **Tiny Scrapbook Observatory** — a home corner vignette with a telescope built from rolled card and a craft-paper star chart. Use as a landmark prop cluster only if the actual room has room; not a replacement for a coherent single hero asset.

## Production gates before adoption

For a character: verify readable silhouette at game camera distance, texture/material import, mesh scale and topology, a compatible rig, and authored or same-rig idle/locomotion/anticipation/attack/hit/defeat/recovery clips. For a boss, also verify the complete telegraph→attack→recovery loop in the actual encounter. For a home prop: verify scale, attachment/contact, room lighting, interaction, and texture memory. Meshy preview art alone is not evidence of any of those gates.
Binder Clip Sentinel now has a genuine rig, Idle/Walk/Run, and a downloaded six-action combat bundle; Paperclip Courier v2 now has a genuine rig, Idle/Walk/Run, and a downloaded five-action ally bundle. Playback and production use remain unreviewed. Craft Memory Garland remains a static prop candidate.








## Iteration 1899 — The Great Paste Pot

- Glue-bottle boss candidate: one cohesive lilac squeeze bottle with coral nozzle-cap, readable charcoal face, integrated small limbs, and paper scraps. Concept and textured T2.5/N1 FBX saved under `Assets/SpaceAndCrafts/Art/Enemies/GreatPastePot/Meshy_Iter1899/`; both previews inspected. Static mesh only; actual face count, deformation, boss telegraph, safe scrape window, and traversal-bridge behavior are unverified/unimplemented.
- Hook: squeeze a marked adhesive lane, pause for a safe scrape counter, then redirect collected paste into a temporary crossing. Idea only.

## Iteration 1898 — Starfold Workbench

- Textured Meshy 7.1 FBX for a lavender star-map workbench with turquoise edge, assembly pad, paper-roll slot, and coral pull. Candidate files under `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldWorkbench/Meshy_Iter1898/`. Front thumbnail hides tabletop features; no scene replacement or interaction is implemented.

## Latest candidates — Iterations 1909–1910

- **Prism Moth (1909):** friendly folded-paper moth with a compact white body and broad purple/coral/mint/gold wings. Textured Meshy 7.1 GLB is 18.38 MB; preview inspected. Could flutter toward hidden wonders and cue them with one short shimmer/chirp instead of a permanent marker. Nonhumanoid; wings are not rigged or animated and the role is not implemented. 33 credits. See `Assets/SpaceAndCrafts/Art/Characters/PrismMoth/Meshy_Iter1909/README.md`.
- **Threadwright (1910):** indigo sewing-machine character with visible needle arm, bobbin, friendly face, and trailing thread. Textured 7.1 GLB/FBX and preview inspected; a 30k-target remesh is 45.56 MB, but actual faces were not independently measured. A stitch-lane tell, temporary paper-sewing lane, and thread-cut counter window would add a new encounter mechanic, but are design-only. Static candidate, not rigged or integrated. 38 credits. See `Assets/SpaceAndCrafts/Art/Enemies/Threadwright/Meshy_Iter1910/README.md`.

## Latest candidates — Iterations 1909–1913

- **Prism Moth (1909):** folded-paper moth with a white central body and four broad colored wings. Textured 7.1 GLB is 18.38 MB; preview inspected. Could flutter/chirp near hidden wonders without a permanent HUD marker. No wing rig, sound, AI, or integration. 33 credits. See `Assets/SpaceAndCrafts/Art/Characters/PrismMoth/Meshy_Iter1909/README.md`.
- **Threadwright (1910):** friendly sewing-machine boss with visible needle arm and trailing thread. The 30k-target remesh measures 29,651 triangles but remains 45.56 MB. Thumbnail retains the face/machine shape; thread overlaps body and a scrap platform under its feet may be unwanted. Stitch-lane/cut-thread encounter is design-only; no rig or integration. 38 credits. See README.
- **Hand-crank Shadow Story Theater (1911):** one purple folio display showing layered Rainbow Kingdom, Leaf Lands, and Mountain Lands plus a coral side crank. Its 30k-target GLB measures 22,201 triangles and is 31.29 MB; remesh preview inspected. Could play a short paper-shadow vignette after boss rewards, but neither crank motion nor progression behavior exists. 38 credits. See `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/ShadowStoryTheater/Meshy_Iter1911/README.md`.
- **Feed-Slot Shredder Warden (1912):** cute lavender shredder biped candidate, 23,607 measured triangles and 3.02 MB; concept/model previews inspected. The Meshy humanoid rig attempt failed pose estimation (422) and yielded no rig. Proposed marked-paper-lane attack/recovery is only an idea. 18 credits. See `Assets/SpaceAndCrafts/Art/Enemies/FeedSlotShredderWarden/Meshy_Iter1912/README.md`.
- **Peelback Sticker Bandit (1913):** one-piece star sticker with a printed face and curled backing strip. Textured 24k-target model measures 23,098 triangles, 3.11 MB; preview inspected. Peel/re-stick motion needs custom deformation and is not implemented. 18 credits. See `Assets/SpaceAndCrafts/Art/Enemies/PeelbackStickerBandit/Meshy_Iter1913/README.md`.

## Latest candidates — Iterations 1914–1915

- **Patchshell (1914):** friendly paper-fiber tortoise under a quilt shell. Textured T2.5/N1 GLB is 3.93 MB and measures 25,150 triangles; preview inspected. Could offer a calm ledge rest or unfolding soft landing flap, though that feature is not visible/rigged. No quadruped rig or game behavior. 18 credits. See `Assets/SpaceAndCrafts/Art/Characters/Patchshell/Meshy_Iter1914/README.md`.
- **Orbit Workshop Organizer (1915):** open folded-cardboard tray with visible compartments, tall arch handle and attached star chart. Textured 7.1 FBX/GLB and remesh preview inspected. Source measured 1,251,710 triangles; remesh 24,362 triangles and 40.55 MB. The handle still obscures part of the interior. Could sort gathered craft parts, but storage/placement are not implemented. 38 credits. See `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/OrbitWorkshopOrganizer/Meshy_Iter1915/README.md`.

## Iteration 1916 — Threadwing traversal ally

- Lavender spool-bodied glider with a cream face label, folded-paper wings, needle tip, and loop of coral/mint thread. Textured T2.5/N1 GLB is **24,368 triangles**, 3.78 MB; preview inspected. Possible use: show a temporary safe route across a gap with a flying thread line. No rig, wing-flap, AI, route behavior, rescue, or sound exists, and the thread's far end does not visibly connect. Keep as a candidate; see `Assets/SpaceAndCrafts/Art/Characters/Threadwing/Meshy_Iter1916/README.md`. Cost 18 credits.

## Iteration 1917 — Starfold Waystation

- Rainbow Kingdom tent/rest-stop candidate with one folded paper shell, rainbow roof ribs, warm open entry, and broad floor. Textured T2.5/N1 model measures 23,601 triangles/4.73 MB; concept and preview inspected. Proposed safe checkpoint/rest behavior is unimplemented; interior reads dark in thumbnail and scale/collider/platform contact are unverified. 18 credits. See `Assets/SpaceAndCrafts/Art/Environment/RainbowKingdom/StarfoldWaystation/Meshy_Iter1917/README.md`.

- **Niblet, the Wax-Star Scout (1919):** one-piece golden wax-crayon ally, sky-blue star label, teal mitts/boots and indigo scarf. Textured T2.5/N1 GLB measures 28,567 triangles; genuine Meshy humanoid rig, free Walk/Run and same-rig Idle/Happy Jump/Wave/Pick-Up/Cheer actions are saved. Model thumbnail inspected. Ability to draw brief safe-route color trails is only a hook; no playback, foot contact, Unity import or companion behavior checked. Cost 38. See `Assets/SpaceAndCrafts/Art/Characters/Niblet/Meshy_Iter1919/README.md`.
- **Rocket-Patch Quilt (1920):** handmade workshop quilt with a stitched gold route, coral rocket, and Rainbow Kingdom/Leaf Lands/Mountain Lands patches. Meshy 7.1 textured PBR FBX and 30k-target remesh saved; remesh is 14.05 MB versus 42.43 MB original and the inspected preview retains key detail. Actual triangle count and home placement/progression integration are unknown. Cost 38. See `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/RocketPatchQuilt/Meshy_Iter1920/README.md`.

- **Bandy the Bungee Baron (1921):** open-loop magenta elastic enemy/boss with integrated face and short limbs. The concept was revised before 3D so its empty loop would distinguish it from the round Splat silhouette. Textured T2.5/N1 GLB measures 28,692 triangles; Meshy reports a successful humanoid rig, free Walk/Run, and same-rig Idle/Charged Ground Slam/Attack/Hit/Dead/Stand-up bundle. Preview preserves the open loop but has dark surface marks and tiny hands; keep review-only pending material/rig review. Suggested telegraph→snap-lane→recoil counter using the Boing-O-Matic remains design-only. Cost 44. See `Assets/SpaceAndCrafts/Art/Enemies/BandyBungeeBaron/Meshy_Iter1921/README.md`.

- **Moonbeam Quilt Lantern (1922):** one connected folded-card crescent shade with stitched orbit seams, a glowing star window, and petal base. Textured Meshy 7.1 FBX and 30k-target remesh are saved; the remesh thumbnail preserves the crescent and details but has a dark reflective inner rim and angular base. Actual triangle count and Unity lighting/scale/placement are unknown. Milestone-reactive workshop lighting is a design hook, not implemented. Cost 38. See `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/MoonbeamQuiltLantern/Meshy_Iter1922/README.md`.

- **Stampede, the Ink-Star Warden (1923):** cohesive stamp-handle humanoid with mint mitts, coral ink-pad collar, and star-shaped stamp base. The 28k-target T2.5/N1 textured model, Meshy-reported 1.45 m rig, free Walk/Run and six same-rig boss action clips are saved. Thumbnail reveals dark artifacts beneath both arms; keep review-only. The marked-lane slam/recoil counter using the Boing-O-Matic is unimplemented. Cost 47. See `Assets/SpaceAndCrafts/Art/Enemies/StampedeInkStarWarden/Meshy_Iter1923/README.md`.

- **Mendle, the Quilt-Keeper (1924):** a plush, asymmetric quilt-star ally with integrated face patch, broad sewn fabric panels and separated soft limbs. Textured T2.5/N1 model, Meshy-reported 1.25 m humanoid rig, free Walk/Run, and six same-rig actions are saved. Preview stays close to the concept; rig playback, contact, skinning, import and 28k target count remain unverified. Could patch a damaged paper path with a temporary soft landing pad; that ability is not implemented. Cost 47. See `Assets/SpaceAndCrafts/Art/Characters/MendleQuiltKeeper/Meshy_Iter1924/README.md`.


## Iteration 1935 — Pipette, Star-Ink Salamander ally

- Friendly blue/lavender salamander with amber eyes, cream muzzle, constellation sash, curved tail and broad feet. A possible role is to leave a temporary dotted path toward nearby optional craft materials/secrets; behavior is not implemented.
- Concept, textured T2.5/N1 FBX/GLB, Meshy-reported rig/Walk/Run, and six-action bundle are saved under `Assets/SpaceAndCrafts/Art/Characters/PipetteStarInkSalamander/Meshy_Iter1935/`. Thumbnail has small dark jaw/arm artifacts. Measured source GLB 29,211 triangles vs 28k target. No playback, foot contact, companion logic, or integration verified. See candidate README.

## New character/prop experiments — Iterations 1959–1961

- **Zip-Zag, zipper-pouch gatekeeper:** closes a route with its zipper; Splat crafts a slider to open a safe path. The 5-credit Meshy draft became a hooded humanoid and was rejected before texture/rig. Do not retry without an approved reference image.
- **Badgeback, boss-souvenir ally:** converts a boss emblem into a workshop badge and collectible story marker. The 5-credit Meshy draft became a big-headed humanoid instead of a single badge body; rejected before texture/rig.
- **Stitchway Loom:** makes route progress visible by weaving the Rainbow Kingdom, Leaf Lands and Mountain Lands into one cloth panel. Meshy produced a static loom candidate (5-credit mesh + 10-credit texture) but lost the symbols against blank cloth and added a star-like texture artifact and many thin hanging loops. Do not integrate this version; use a cleaner approved reference before a replacement.

Distinct unmodeled ideas from the roster scan: **Solstice Press**, a cyanotype miniboss that telegraphs a light lane for Splat to block/turn; **Zip-Zag** remains a promising concept only if a reference image can preserve its pouch silhouette; and **Stitchway Loom** remains the strongest progression-decor direction if a clearer cloth motif is designed. These are design briefs, not game features.

## Solstice Press — boss concept awaiting approval for 3D

- Concept: `Assets/SpaceAndCrafts/Art/Enemies/SolsticePress/Concept/SolsticePress_Concept.png`. A compact clamshell cyanotype press with an indigo sun-ray lid, coral print bed, star-print sheet, attached handles and integrated feet. The press itself is the boss, with a friendly face on its surfaces. Meshy text-to-image task `01a1cfee-400b-74f7-a03c-c781318ca5f7` cost **3 credits**.
- Encounter hook: telegraph a bright exposure lane, then let Splat block or turn the press to create a safe crossing. Design only; no 3D model, rig, animation or gameplay. User approval is required before image-to-3D.


## Iteration 2002 — Splat & Squat Sketch-Flip Frame (concept only)

- Current HomeWorkshop roster audit found no sibling-drawing display. Added a folded-cardstock easel concept showing the approved Splat and Squat looks in a childlike drawing. It is distinct from the existing atlas/observatory/keepsake/memory-board props because it celebrates their relationship and creative work.
- Proposed small interaction: flip between drawings as their journey progresses. No state integration is implemented. The image is a project concept reference, not a Meshy model or game asset.
- Meshy balance remains **0 total** (0 monthly + 0 permanent), Usage API 403. No Meshy task or purchase. Review and production limits: `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/SplatAndSquatSketchFlipFrame/README.md`.

## Iteration 2003 — Floatlet, folded-paper fall-rescue companion (concept only)

- Saved the previously generated two-state concept under `Assets/SpaceAndCrafts/Art/Characters/Floatlet/`. It depicts one lavender folded-tissue body with a compact follow state and a coral pleated canopy glide state.
- Roster comparison: Kite Manta scouts wind lanes and Threadwing suggests a visible thread-route. Floatlet's distinct proposed job is a limited, steerable descent toward a nearby safe landing. This remains a design hook; the concept sheet must be separated into a single folded idle reference before image-to-3D so Meshy does not fuse both poses.
- No model, rig, animation, Unity behavior, audio/VFX, build, test or play verification was created. Avoid treating a glider ally as a replacement for safe level design or fall recovery.
- Meshy balance checked live: **0 total = 0 monthly + 0 permanent**; next reset `2026-12-10T21:11:17Z`. Usage endpoint is 403 and the website-reported expiring credits remain unreconciled. No Meshy task or purchase.

## Iteration 2004 — Floatlet single-pose reference (ImageGen only)

- Created `Assets/SpaceAndCrafts/Art/Characters/Floatlet/Concept/Floatlet_ModelInput_Iter2004.png` from the approved concept sheet: a single three-quarter folded-idle figure, with the canopy stowed, isolated on white. The original two-state design sheet remains in place for deployment intent.
- This improves readiness for later image-to-3D but is not Meshy output. It remains a concept candidate pending design approval; no geometry, rig, animation or Unity behavior exists. Meshy balance was checked before this work and is 0; usage endpoint returns 403. No Meshy task or purchase.

## Iteration 2005 — Vellum, the Blueprint Buddy (concept only)

- Added a single folded tracing-paper ally concept at `Assets/SpaceAndCrafts/Art/Characters/VellumBlueprintBuddy/Concept/Vellum_Concept_Iter2005.png`. Vellum's proposed workshop role is to briefly sketch the missing part of one selected unfinished invention; no automatic crafting or persistent HUD marker.
- Roster check found this specific build-bench cue distinct by intent from Margin's boss-story pages, Tiney's resonance cue, and the route/secret scouts. The image is an ImageGen concept, not a 3D asset. Translucent surface and fine plan lines are risks for image-to-3D and need a thumbnail/material test before a paid model.
- No Meshy generation, rig, animation, Unity behavior, build, test, or play verification. Live connected Meshy balance is **0 total = 0 monthly + 0 permanent** (reset `2026-12-10T21:11:17Z`); Usage endpoint 403. No spend or purchase.

## Iteration 2006 — Moldstar refined single-body star concept

- Roster/source audit found the existing Moldstar Meshy concept reads as a generic sculpting-clay blob. Saved a separate alternative: one broad plum-and-teal clay star with integrated lower points/feet, side points/arms, and a chest relief. Older concept/model files remain preserved.
- A rolling-lane boss tell and cardboard-brake counter are a design proposal only. The new concept's five small ivory stars read raised; simplify them to shallow stamped/painted marks before any future 3D pass. No mesh, rig, animation, Unity behavior, encounter, build, test, render, or human play was created/verified.
- Meshy balance checked live this turn: **0 total (0 monthly + 0 permanent)**; next reset `2026-12-10T21:11:17Z`. Usage endpoint 403. No task or purchase.

## Iteration 2007 — Moldstar model-intake refinement

- Saved a cleaner single-pose image at `Assets/SpaceAndCrafts/Art/Enemies/MoldstarSculptedStar/Concept/Moldstar_ModelInput_Iter2007.png`. The small star ornaments are removed; the chest emblem is one shallow relief and surface flecks are paint/press marks. Older concept and Meshy files remain preserved.
- Remaining risk: eyebrows and face read raised. Keep those as painted/debossed details rather than separate geometry. Whole-body lean/spin/recovery may be more appropriate than a humanoid rig for this silhouette.
- No Meshy task, model, rig, animation, Unity integration, build, test, render, or human play. Live Meshy balance is 0; Usage endpoint is 403. No spend or purchase.

## Iteration 2008 — Roller Riff foam-roller boss concept

- Added `Assets/SpaceAndCrafts/Art/Enemies/RollerRiff/Concept/RollerRiff_Concept_Iter2008.png`, a polished single-body foam-roller character reference. The craft material provides a distinct silhouette/mechanic from the existing brush and palette characters.
- Proposed boss loop: gather roller sleeve/grip/pigment, build the Squeegee Slider, redirect a telegraphed color lane into a safe ramp, then use the brief grounded recovery opening. This is design-only and preserves nonlethal traversal recovery.
- Concept has four visible feet, a fused roller shell, flat face markings, and a curled tail. Meshy readiness, actual topology, rig, animation, gameplay, and human review are unverified; the feet/tail must be kept integrated rather than loose parts.
- Live Meshy balance is **0 total = 0 monthly + 0 permanent**. Usage endpoint returned **403**. No paid task or purchase.

## Iteration 2009 — Splat & Squat milestone height chart

- Added `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/MilestoneHeightChart/SplatSquat_MilestoneHeightChart_Iter2009.png`, a wall-mounted paper chart concept with world markers and sibling height doodles.
- The proposed role is a quiet progress memento updated after boss/world milestones. This is distinct by function from the existing Splat-and-Squat sketch-flip frame, invention memory board, travel atlas, trophy shadowbox, and craft-memory garland.
- It remains 2D concept art only; no model, Unity placement, unlocks, build, test, render, or human review.
- Fresh Meshy balance remains **0 / 0 / 0** and Usage is **403**. No Meshy task or purchase.

## Backlog candidates from roster audits — not approved/implemented

- **Buttonhole Admiral:** one fused mother-of-pearl button creature with four integrated eyelets and broad feet. Telegraphs a circular floor-sewing pattern; the player builds a button press to set the marked pattern into a safe route. Distinct craft material, but this is still only an idea.
- **Squat as optional test pilot:** the existing brother should react to Splat’s crafted invention in optional safe test runs; avoid adding another scout/hint ally. Character behavior and story implementation remain unverified.

## Iteration 2010 — Buttonhole Admiral boss reference

- Saved a one-body mother-of-pearl button boss concept at `Assets/SpaceAndCrafts/Art/Enemies/ButtonholeAdmiral/ButtonholeAdmiral_Concept_Iter2010.png`. The four eyelets create the silhouette; its face is centered on the same body. This is distinct from Tessera's puzzle-piece ally hint role.
- Proposed encounter: a legible, nonlethal stitch-pattern lane; gather button blanks and cord; build a Pattern Press to reverse the marked route; use the boss's planted inspection pause as the attack opening. All encounter mechanics remain design-only.
- Review risks: image shows a thread bridging the upper holes, and the feet blend into the shell. Keep thread flush and improve visible contact if a future approved 3D candidate is made.
- No Meshy model, rig, animation, Unity implementation, build, test, render, or human play. Live balance 0; Usage 403; no spend or purchase.

## Meshy final-balance asset review — Iteration 2001 folders

Current task receipts reconcile the final **71 connected API credits**: Sproutlet concept/model/rig/action bundle (**35**), Palette Piper concept/model (**18**), and Starfold Shuttle maquette concept/model (**18**). Live total/monthly/permanent is **0/0/0**; aggregate Usage is 403. Sproutlet is a promising review candidate with saved rig/action files but no proof of playback or foot contact. Palette Piper's second model repeats a face cavity and diagonal crack and is rejected. The shuttle reads as a static display prop; scale/stand support and Unity placement are unverified. Full receipts and limits: the three per-asset READMEs plus `docs/MESHY_CREDIT_TRACKER.md`.

## Iteration 2011 — Loop-Latch, the Velcro Vortex (ImageGen concept only)

- Roster audit found 54 enemy folders and no Velcro/hook-and-loop enemy. Existing close craft material identities include tape/sticker, ribbon/thread, buttons/grommets, paper, glue, scissors, staples, stamping, and erasing.
- Saved `Assets/SpaceAndCrafts/Art/Enemies/LoopLatch/Concept/LoopLatch_Concept_Iter2011.png`: one soft curled fastener body with printed face on smooth backing and loop pile on the outside. The reference is a cohesive, cute design, though the high-detail loop nap could collapse into a fluffy generic blob in 3D.
- Encounter pitch: telegraphed attachment to marked fabric pads; gather felt/thread; build Fuzz-Brush; cover hooks on one safe route; approach during the planted peel-off recovery. Keep a wide alternate lane and all fall recovery intact. The scene and behavior are design-only.
- Candidate art only. No Meshy model, rig, animation, gameplay, or Unity integration. Live connected balance 0; Usage 403; no Meshy task/purchase.

## Iteration 2012 — Prototype Flight Hoop (Home Workshop concept only)

- Saved `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/PrototypeFlightHoop/Concept/PrototypeFlightHoop_Concept_Iter2012.png`: one folding cardstock ring, trajectory guide mat, adjustable star slider, and padded cradle. Current workshop inventory has benches, storage, world displays, trophies, and lightboxes but no dedicated invention flight-test rig.
- Proposed use is an optional home-hub ghost arc to preview range/landing; Squat can cheer or suggest a re-aim. Keep the actual build/use test in levels as the core loop.
- ImageGen only. No Meshy model, collision, Unity interaction, trigger, build/test/render, or human play. Balance 0 / Usage 403; no paid task or purchase.

## Iteration 2013 — Squat test-pilots the Prototype Flight Hoop (ImageGen concept only)

- The ally-role audit found no compelling additional helper function without duplicating route scouts, hidden-cache/material cues, crafting diagrams, repair/rescue, story memory, rhythm puzzles, or Squat's existing sibling/test-pilot hook. Extend the existing Squat instead of adding a generic hint NPC.
- Saved `Assets/SpaceAndCrafts/Art/Characters/Squat/Concept/Squat_PrototypeFlightTest_Concept_Iter2013.png`: the tentative Meshy_Iter1908 character cheers as a paper glider travels through the Iteration 2012 hoop to its padded cradle. This gives the workshop asset a character-facing use while preserving the light sibling relationship.
- The scene proposes only a safe optional test and cheerful reaction. No new dialogue, hint system, progression or canonical appearance is established. The visual reference remains tentative and requires Mac/user approval before promotion.
- ImageGen only: no Meshy cost, model, rig, animation, Unity interaction, build/test/render or human play. Connected Meshy balance remains 0 and Usage 403.

## Iteration 2014 — Snip-Snap safety-scissors modeling reference (ImageGen only)

- The kid-requested scissors archetype already exists in Meshy_Iter1894 as a cohesive model candidate, while its face/material readability and motion still need review. Saved a new single-pivot-face refinement at `Assets/SpaceAndCrafts/Art/Enemies/SnipSnap/Concept/SnipSnap_Refined_Iter2014.png`; earlier model and concept files are preserved.
- The character stays a pair of rounded safety-scissor halves around one coral hinge, with mint handle loops and one face only on the pivot. A first concept pass introduced extra blade-eye marks; the saved revision removes those while retaining restrained flat craft stitching.
- Proposed motion/counter: guarded hover idle → wide open telegraph → one short safe lane snip → planted/open recovery; gather felt/thread to craft a Hinge Muffler that holds the blades open. The alternate route remains traversable; no unsafe falls. Design only.
- No Meshy task/model or Unity integration. Connected balance 0; Usage 403; no task or purchase. The Meshy_Iter1894 candidate is not replaced, and the new art is not proof of animation/production quality.

## Iteration 2015 — Bubblewrap Bouncer (concept only)

- Roster audit found no bubble-wrap enemy among the current 55 enemy folders. Added a single cohesive creature concept: a low crescent made from one continuous air-cell membrane, with one face patch and broad grounded ends. ImageGen result: `Assets/SpaceAndCrafts/Art/Enemies/BubblewrapBouncer/Concept/BubblewrapBouncer_Concept_Iter2015.png`.
- Encounter pitch: sequential telegraphed pop-wave; craft a Quiet Pad from felt + flat card to damp one row and open a safe crossing; pops are nonfatal knockback; keep a side route and fall recovery. This is design-only.
- No Meshy model or rig was attempted: connected balance is 0 and aggregate Usage API returns 403. The user's separately reported website pool remains unreconciled. No Unity import, implementation, build, test, runtime render, or human play. See the asset README.

## Iteration 2016 — Bubblewrap Bouncer pose sequence (ImageGen)

- Saved a three-pose storyboard at `Assets/SpaceAndCrafts/Art/Enemies/BubblewrapBouncer/Concept/BubblewrapBouncer_PoseSequence_Iter2016.png`: compress/anticipate, rise into the pop beat, deflate/recover. The character's crescent silhouette, palette, and single face patch stay consistent; its lower edge remains at the ground line.
- The middle pose is visually larger but the image does not truly animate sequential cell popping; treat it as a key-pose reference only. A future animation should light/move the integrated cells in a clear lane sequence and preserve contact/knockback safety.
- ImageGen only. Meshy balance remains 0 and Usage API 403; no generation or purchase. No rig, motion clip, Unity integration, build, runtime render, test, or human play.

## Iteration 2017 — Quiet Pad invention concept

- Saved `Assets/SpaceAndCrafts/Art/Commissioned/Inventions/QuietPad/Concept/QuietPad_Concept_Iter2017.png`: a soft mint-faced, plum-felt pressure pad with coral stitching. It is designed as a grounded placed tool, not a weapon/shield: craft it from felt + flat cardstock, place it over one telegraphed Bubblewrap Bouncer row, and stand on it during a wave to damp that row and open a safe crossing.
- This links exploration/gathering→build→use→boss on paper and stays distinct from the Velcro Fuzz-Brush and scissors Hinge Muffler counters. Pops remain nonfatal knockback and a side route/fall recovery is required.
- ImageGen concept only; scale/contact/place behavior are not shown or verified. No Meshy task/model, Unity integration, build, test, runtime render, or human play. Connected Meshy balance is 0; Usage API 403. See the invention README.

## Iteration 2018 — Bubblewrap Bouncer Atrium environment concept

- Saved `Assets/SpaceAndCrafts/Art/Environment/RainbowKingdom/BubblewrapBouncerArena/Concept/BubblewrapBouncerArena_Concept_Iter2018.png`, linking the Bouncer and Quiet Pad designs in one environment reference: grounded approach/craft station, three readable bubble lanes, pad over the center row, broad platform, and side bypass.
- The image's soft-looking under-edge cushion is atmosphere only; it does not establish a usable recovery route. Any implementation needs a continuous playable floor, explicit return ramps/checkpoints, nonfatal knockback, and a bypass that reconnects.
- ImageGen concept only. No Unity scene, collision, encounter scripting, Meshy task, build, runtime render, test, or human play. Connected Meshy balance 0; Usage API 403. Full limits in the asset README.

## Iteration 2019 — Cap'n Chromacap, washable-marker rival (ImageGen concept only)

- A scan of the 56 enemy folders and the concept index found no washable-marker character. Saved `Assets/SpaceAndCrafts/Art/Enemies/CapnChromacap/Concept/CapnChromacap_Concept_Iter2019.png`: one marker-barrel body, attached mint cap, printed face label, compact integrated limbs, and grounded boots. This fills a material identity/mechanic gap rather than adding another generic blob or paper humanoid.
- Boss pitch: the marker telegraphs a bright stripe, then makes a short dash along that lane. Gather soft felt + a clear water bead, build the **Rinse Roller**, and sweep the stripe to redirect the dash into a harmless skid; approach during the planted cap-wobble recovery. Keep a broad bypass, nonfatal attacks, and fall recovery. This proposes the gather→build→use→boss loop but implements none of it.
- The workshop decoration scan found broad functional coverage and no need for a redundant new wall display right now; integrating one existing flight-test or progression prop would add more than another concept. No decor was created this iteration.
- ImageGen concept only. No Meshy task, model, rig, animation, Unity integration, build, test, runtime render, or human play. Connected Meshy balance is **0/0/0** and Usage returns **403**; no spend or purchase. See the per-asset README.

## Iteration 2020 — Calip, the Fit Check Buddy (ImageGen concept only)

- The ally roster review found broad coverage of route scouts, secret/material cues, repair, story, rhythm, and test-pilot roles; no small vernier-caliper workshop helper was present. Saved `Assets/SpaceAndCrafts/Art/Characters/Calip/Concept/Calip_Concept_Iter2020.png`: one open C-frame caliper body, single mint slider jaw, surface scale, printed plum face, and planted boots.
- Proposed optional bench role: compare a selected invention's width with an opening and give a subtle “close / needs room” expression. The player still chooses parts and solves the route; Calip does not gather, auto-craft, reveal secrets or solve puzzles.
- ImageGen concept only; no Meshy model, rig, animation, Unity interaction, build, test, render, or human play. Connected balance is 0; Usage API 403.

## Iteration 2021 — Rinse Roller, Chromacap counter-invention (ImageGen concept only)

- Added `Assets/SpaceAndCrafts/Art/Commissioned/Inventions/RinseRoller/Concept/RinseRoller_Concept_Iter2021.png`: a compact mint felt roller in a plum folded-card frame with coral grip pad and recessed amber water-bead window. It is the companion craft-item reference for Cap'n Chromacap.
- The proposed boss use is to wipe a clearly telegraphed washable pigment lane and turn a short dash into a harmless skid/recovery. It should not become a permanent bridge or remove the broad alternate route/checkpoint recovery. No recipe, inventory entry, held-item rig, collision or boss behavior exists.
- ImageGen concept only. No Meshy task or purchase; current connected balance is 0 and Usage is 403. No Unity integration/build/test/runtime render/human play.

## Iteration 2036 — Washi Star-Spider cohesive redesign reference

- Audited the existing `RainbowCrystalSpider` and `WashiStarSpider` Meshy previews. The crystal spider has a cute face but its limb/body joins look like separate inflated pieces; the washi candidate has a more material-specific silhouette but several legs disappear in its front view.
- Saved `Assets/SpaceAndCrafts/Art/Enemies/WashiStarSpider/Concept/WashiStarSpider_CohesiveModelRef_Iter2036.png`. The revised indigo washi-paper shell folds continuously into eight tapering, bent legs with broad grounded feet. A front three-quarter hero view and overhead view expose all four leg pairs; one flush face and a restrained coral washi tab preserve the existing character identity.
- ImageGen concept only. The reference suggests cohesive topology and foot placement, but cannot prove either. No Meshy model/rig/animation, Unity import, gameplay, build, render, or human review.
- Fresh connected Meshy balance check: **0 total = 0 monthly + 0 permanent**, reset `2026-12-10T21:11:17Z`; Usage endpoint **403**. No credits spent or purchased. The reported website pool remains unreconciled. Preserve the current scorecard ratings: this is not player-facing gameplay evidence.

## Iteration 2037 — Washi Star-Spider grounded gait board

- Added `Assets/SpaceAndCrafts/Art/Enemies/WashiStarSpider/Concept/WashiStarSpider_GroundedGaitBoard_Iter2037.png`: a four-phase visual brief showing planted idle, small anticipation, alternating four-leg groups, and planted settle for the eight-legged redesign.
- The character stays compact in the drawings and the step phases change leg placement rather than stretching the body. This is static art, not evidence of a rig, animation, actual contact timing, or locomotion quality.
- No Meshy spend: connected balance remains 0 and Usage is 403. Existing model stays unchanged. Scores unchanged; 90/100 remains unmet.

## Iteration 2038 — Grand Snip mechanical animation reference

- Saved `Assets/SpaceAndCrafts/Art/Enemies/GrandSnip/Concept/GrandSnip_AnimationKeyposes_Iter2038.png`: four key poses for a closed idle, hinge-only anticipation, controlled blade swing, and held-open recovery.
- This supports the scissors-as-the-character silhouette with mechanical pivot motion rather than a humanoid auto-rig or whole-body stretch. The drawn lock pictogram only represents the proposed Hinge Muffler; it is not a finished prop or implemented counter.
- ImageGen only. No Meshy task, hinge rig, motion clip, Unity import, encounter, build, render, or human play. Live balance was **0 total**; Usage returned **403**; no credits spent or purchased. Ratings unchanged; 90/100 remains unmet.

## Iteration 2039 — Hinge Muffler invention prop

- Saved `Assets/SpaceAndCrafts/Art/Commissioned/Inventions/HingeMuffler/Concept/HingeMuffler_Concept_Iter2039.png`: a folded-card spring clamp with felt pads, brass brad and squeeze tab, plus an in-use inset at Grand Snip's hinge.
- This makes the scissors boss's proposed gather → craft → use loop more tangible: collect felt/brad, build the clamp, then hold the blades open during recovery to make the already-flat route safe. No recipe, item, collision, boss code or encounter is implemented.
- The image looks cohesive at concept scale, but jaw dimensions, hinge clearance and player use pose remain unsolved. No Meshy task/model or Unity integration. Live Meshy balance 0; Usage 403; no credits spent or bought. Scores unchanged; 90/100 remains unmet.

## Iteration 2040 — Hinge Muffler model-intake refinement

- Added `Assets/SpaceAndCrafts/Art/Commissioned/Inventions/HingeMuffler/Concept/HingeMuffler_ModelSheet_Iter2040.png` with three views of the same spring clamp, clarifying the open jaw gap, connected felt pads, folded-card spine, single brad pivot, and coral tab.
- This improves future modeling direction, but the tab's slide track/locking action is still not dimensioned. It is reference art, not production geometry or a working invention.
- No Meshy task or purchase: live connected balance 0; Usage 403. No Unity integration or gameplay; scorecard unchanged and 90/100 unmet.

## Iteration 2041 — Grand Snip Pattern Studio arena

- Saved `Assets/SpaceAndCrafts/Art/Environment/RainbowKingdom/GrandSnipPatternStudio/Concept/PatternStudioArena_Iter2041.png`. The wide composition shows a broad cutting-mat boss floor, Grand Snip and marked attack lane, a Hinge Muffler workbench, felt/brad pickup alcoves, and an opposite counter switch. A continuous pastel fjord-side landscape and rounded curbs surround the play space; no exposed void is drawn.
- The proposed route links gathering → build → use → boss and gives the arena readable landmarks. The image cannot prove safe collision, path dimensions, fall recovery, encounter logic or actual camera readability; those require implementation and review.
- Meshy live balance remains 0 (0 monthly + 0 permanent), reset 2026-12-10T21:11:17Z; Usage returned 403. No paid task or purchase. No score change; 90/100 remains unmet.

## Iteration 2042 — Coro, the Carton Keep

Added `Assets/SpaceAndCrafts/Art/Enemies/CoroCartonKeep/Concept/CoroCartonKeep_Turnaround_Iter2042.png`, a four-view concept of a friendly corrugated-cardboard Pattern Studio boss. The visual hook is one kraft shell with a hinged roof flap and broad flute channels; the proposed Flute Brace makes the safe traversal counter tactile. The front reads cleanly, but the arms still look like separate cardboard blocks and the side profile is too narrow; simplify both before any modeling. This is ImageGen reference art only—no Meshy task/model, rig, animation, arena, Unity integration, or encounter. Live Meshy balance: 0 total (0 monthly + 0 permanent); Usage endpoint 403; no credits used or bought. Scores unchanged; 90/100 remains unmet.

## Iteration 2043 — Coro motion storyboard

Added `Assets/SpaceAndCrafts/Art/Enemies/CoroCartonKeep/Concept/CoroCartonKeep_ActionBoard_Iter2043.png` with idle, roof-flap anticipation, one planted step, and seam-exposed recovery. This clarifies readable boss timing and ground contacts in a static reference only; it is not a rig, animation clip, or gameplay test. The arms remain blocky and need a cleaner continuous-fold design. Meshy remains 0 credits / Usage 403; no spend. Scores unchanged; 90/100 remains unmet.

## Iteration 2044 — Stardock Reading Nook (ImageGen concept)

- Added a four-view Home Workshop furniture concept at `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StardockReadingNook/Concept/StardockReadingNook_ModelSheet_Iter2044.png`: a continuous folded-card rocket pod with wide grounded base, opening front hatch, quilted crescent seat, porthole and small star lantern.
- The roster contains a shuttle maquette and multiple chairs/poufs but no dedicated reading nook. The rear view is plain and seat-entry/hinge clearances still need dimensions. This is concept art only: no Meshy model, prefab, collision, animation, interaction, build or human review.
- Meshy balance rechecked at 0; Usage returns 403; no credits spent or bought. Ratings unchanged; 90/100 remains unmet.

## Iteration 2045 — Coro cohesion revision

Revised the Coro model-reference sheet at `Assets/SpaceAndCrafts/Art/Enemies/CoroCartonKeep/Concept/CoroCartonKeep_CohesionRevision_Iter2045.png`. The widened shell profile, small fold-integrated arm tabs and continuous broad base address the previous disconnected-block silhouette and foot-contact risk. Keep earlier views for comparison; the new image is the preferred concept, not production topology. No Meshy spend, model, rig, gameplay, build or test. Balance 0; Usage 403; ratings unchanged and 90/100 unmet.

## Iteration 2046 — Flute Brace invention concept

- Saved `Assets/SpaceAndCrafts/Art/Commissioned/Inventions/FluteBrace/Concept/FluteBrace_ModelSheet_Iter2046.png`, a four-view folded-card gusset with a felt-lined canopy cradle, broad base and in-use inset on Coro.
- Clarified the Coro boss counter: gather a corrugated offcut + felt strip, fold the gusset, seat it beneath Coro's raised roof flap, walk the supported short ramp and tap the exposed hinge. The connected arena floor remains the safe route. This wedge differs mechanically from Grand Snip's padded blade clamp.
- Art/design only. No mesh, recipe, pickup, craft, collision, Unity prefab or encounter exists. Front-view cradle and dimensions need engineering. Meshy balance rechecked: 0; Usage 403; no spend. Ratings unchanged; 90/100 remains unmet.

## Iteration 2047 — Coro Flutefall Studio arena concept

- Added `Assets/SpaceAndCrafts/Art/Environment/RainbowKingdom/CoroFlutefallStudio/Concept/CoroFlutefallArena_Iter2047.png`, showing Coro and the Flute Brace in a fjord-side Pattern Studio with a broad connected mat, rounded perimeter curb, material alcoves, nearby craft bench, dashed attack lane, hinge target and exit route.
- This illustrates exploration → gathering → building → use → boss on a visible continuous route. It remains ImageGen layout art; no measured dimensions, collision, Unity implementation, safe-recovery test, enemy AI or camera review exists. Do not treat drawn connected surfaces as proof of no fall voids.
- Meshy balance checked: 0 total; Usage 403; no spend. Ratings unchanged; 90/100 remains unmet.

## Iteration 2048 — Coro motion-board cohesion revision

Saved `Assets/SpaceAndCrafts/Art/Enemies/CoroCartonKeep/Concept/CoroCartonKeep_ActionBoard_Iter2048.png`, matching the preferred 2045 shell design and 2046 Flute Brace. The four beats are grounded idle, canopy-hinge anticipation, a small rigid-body sidestep, and planted supported recovery. Static storyboard only; no mesh rig or animation clip, and foot timing still needs real playback review. Meshy balance 0; Usage 403; no spend. Scores unchanged; 90/100 unmet.

## Iteration 2049 — Starfold Stepstool (Home Workshop gap fill)

- A roster scan found chairs and soft seating but no stool or ladder. Saved a three-view fold-flat corrugated stepstool reference at `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/StarfoldStepstool/Concept/StarfoldStepstool_ModelSheet_Iter2049.png`.
- Optional, non-progression use: step up to inspect a workshop curiosity or high porthole. The wide felt top, integrated gusset and broad contact feet are concept direction; load stability and dimensions require review. No model/prefab/gameplay exists.
- Meshy current balance needs a fresh check before any task; this image used ImageGen only. Scores unchanged; 90/100 remains unmet.

## Iteration 2050 — Patchwork Peddler ally concept

- Roster audit found no vendor/barter role among 34 ally folders and 62 enemy folders. Added a four-view quilted folio ally at `Assets/SpaceAndCrafts/Art/Characters/PatchworkPeddler/Concept/PatchworkPeddler_Concept_Iter2050.png`; its single flap opens to three stitched pattern pockets.
- Proposed optional fixed trade: duplicate common scraps for a previewed cosmetic swatch/workshop tint only. No unique or quest-critical items, random reward, hidden price or progress gate. Concept only; no trade system, model/rig, Unity interaction or economy implementation.
- Meshy balance checked this turn: 0 (0 monthly + 0 permanent); Usage returns 403. No paid task. Scores unchanged; 90/100 remains unmet.

## Iteration 2051 — Patchwork Swatch Counter

- Added a fold-flat Workshop counter concept at `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/PatchworkSwatchCounter/Concept/PatchworkSwatchCounter_ModelSheet_Iter2051.png`, matching the Peddler's three swatches and textile palette.
- The display proposes one shallow duplicate-common-scrap return pocket and three visible fixed cosmetic options; no coins, hidden/random trades, unique materials or progress gate. This is concept art only; no mesh, prefab, trade UI/economy or runtime behavior exists.
- Meshy balance is 0; Usage endpoint 403; no task or spend. Scores unchanged; 90/100 remains unmet.
## Latest ally concept — Iteration 2056: Tessell, the Set-Square Scout (2026-11-12)

- Roster scan found no set-square character; existing construction helpers include a compass, ruler, caliper, and level. Tessell is a friendly, broad amber acrylic drafting triangle with a real inner opening, small integrated face panel, compact violet arms, and wide grounded boots.
- Proposed role: hint at build-angle options and a matching support edge. Splat still gathers material, constructs the Tri-Line Brace, and chooses where to place it for a sloped route; the ally does not solve or auto-build the crossing.
- Reference and production constraints: `Assets/SpaceAndCrafts/Art/Characters/TessellSetSquareAlly/`. The narrow edge-on profile needs a thickness decision and camera-scale check. Concept only; no model, rig, clips, Unity use, or player-facing feature.
- Meshy connected balance 0; Usage 403; website-reported balance unreconciled. No credits spent. The larger asset goal remains active.

## Latest enemy concept — Iteration 2057: Glintlet, the Glitter Shaker (2026-11-12)

- Current enemy folders had no glitter-shaker character. Glintlet is one rounded translucent bottle body with an attached flip-cap, recessed label face, compact limbs, and broad planted feet; it is an enemy concept, not a model.
- Proposed encounter: the cap opens as a tell, then one bounded glitter sprinkle makes a lane slippery. Splat gathers felt and folded card, builds a **Felt Skimmer**, clears the lane, and uses the safe route to reach the cap hinge during a planted recovery. Keep a connected floor and no projectile cloud/void knockback.
- Model sheet/brief: `Assets/SpaceAndCrafts/Art/Enemies/GlintletGlitterShaker/`. The concept's dense confetti, transparent shell, and raised face decoration need simplification and readability review before modeling. No rig, animations, Unity encounter, collision, build, or human-play evidence.
- Meshy balance rechecked at 0 total (0 monthly + 0 permanent); Usage 403; website-reported balance remains unreconciled. No task or purchase; broader asset goal continues.

## Latest Home Workshop decoration — Iteration 2058: Fjordlight Weaving (2026-11-12)

- Inventory scan found quilts/drapes, mobiles, lamps, maps, and keepsakes, but no woven fjord wall panel. The proposed compact tapestry shows broad teal mountains/water, a restrained rainbow and three stars, bound edging, sewn loops, and a short dowel.
- Static placement above the workbench or reading nook preserves the walk lane. Optional world-milestone border colors remain a visual memento only.
- ImageGen reference and modeling guards: `Assets/SpaceAndCrafts/Art/Environment/HomeWorkshop/FjordlightWeaving/`. No Meshy geometry, prefab, placement, runtime logic, build, or player evaluation. Keep yarn detail broad for camera readability.
- Connected Meshy account 0 credits; Usage 403; website pool unreconciled. No task or purchase.

## Latest paper miniboss — Iteration 2059: Scrip, the Return-to-Sender (2026-11-12)

- The existing Orbit Postbox has an unimplemented postcard exchange with Squat but no related enemy/character. Scrip is one rounded envelope body with a hinged rear flap and a blank return corner.
- Story encounter: gather ink and cardstock, build a Return-Route Stamp, mark Scrip's corner, then follow one telegraphed grounded slide to the postbox and receive the letter. It is a puzzle obstacle, not a violent fight; the broad safe floor remains available.
- Concept and constraints: `Assets/SpaceAndCrafts/Art/Enemies/ScripReturnToSender/`. Side thickness, animation, and game-scale readability need review; no model, rig, Unity content, or playable system exists.
- Meshy balance 0; Usage 403; website pool unreconciled. No task or purchase; asset work continues.

## Scrip model-intake preparation — Iteration 2060 (2026-11-12)

Made a clean one-character neutral three-quarter input from the mixed Iter2059 story/action sheet: `Assets/SpaceAndCrafts/Art/Enemies/ScripReturnToSender/Concept/Scrip_ModelInput_Iter2060.png`. This is preparation for possible image-to-3D, not a Meshy output. Face features are still raised, and Meshy/model topology, rig, animation, Unity import, and contact remain unverified. The connected balance is 0 and Usage 403; no paid task.

## Glintlet encounter counter-tool — Iteration 2061: Felt Skimmer (2026-11-12)

- Added the companion tool to the Iter2057 glitter-shaker enemy pitch: a hand-held shallow cardstock scoop with a folded spine and broad felt edge. It sweeps only a clearly bounded temporary slick off the route so Splat can approach the open cap during recovery.
- Proposed recipe: cardstock vane, felt strip, coral thread. This is a non-violent traversal invention; no damage or void knockback.
- Reference and modeling limits: `Assets/SpaceAndCrafts/Art/Commissioned/Inventions/FeltSkimmer/`. It reads as a compact scoop, not a broom, and needs game-scale/clearance review. ImageGen-only, not Meshy geometry, Unity prefab, crafting UI, or interaction.
- Meshy balance rechecked at 0 total; Usage 403; website pool unreconciled. No task or purchase.

## Grand Snip image-to-3D preparation — Iteration 2062 (2026-11-12)

Made a single-object neutral three-quarter model intake from the preferred safety-scissors design at `Assets/SpaceAndCrafts/Art/Enemies/GrandSnip/Concept/GrandSnip_ModelInput_Iter2062.png`. It avoids the previous four-view/action collage and preserves the two blunt blades, loops, one face hinge, and small signature star. Still ImageGen-only; the raised eye/star details and actual hinge geometry need production review; no 3D model, pivot rig, or Unity boss. Connected Meshy balance remains 0 and Usage 403.

## Latest ally concept — Iteration 2063: Meltie, the Bead-Shell Pathfinder (2026-11-12)

- The current enemy/ally roster has no fuse-bead character. Meltie is a friendly, nonhumanoid snail with one cohesive molded body and broad heat-fused color cells that form a spiral shell motif.
- The first image draft looked like a pile of round beads, so it was rejected. The preferred Iter2063 sheet uses larger, shallower fused cells; residual seams still require review. Concept/brief: `Assets/SpaceAndCrafts/Art/Characters/MeltieBeadShellPathfinder/`.
- Proposed use: Meltie hints at motif families; Splat gathers three colors, makes a Pattern Press Patch, and places a stable route tile. This preserves player agency and a wide safe floor. No model/rig/animation/Unity behavior.
- Meshy balance rechecked at 0; Usage 403; separate website pool unreconciled. No paid task or purchase.


## Iterations 2064–2065 — optional route tile and spider cohesion pass

Pattern Press Patch is a single low fuse-bead tile that pairs with Meltie, with a side-by-side safe route and a flush floor inset rather than a fall-gap bridge. The Washi Star-Spider revision keeps the same character but clarifies eight continuous paper limbs and broad grounded contacts. Both are static concept references only. No new Meshy generation occurred: connected balance 0, Usage 403, website pool unreconciled. Mesh/topology, rigs, animations, Unity use, and play behavior remain unverified.
## Iteration 2066 — Muddle, the Watercolor Mischief

A roster check found no watercolor-palette boss. Muddle's proposed floor-safe counter loop is gather felt + a clear water bead → build a Blotter Paddle → wick one telegraphed low-friction lane → use the dry path and approach its planted recovery. The image and production limits are stored in `Assets/SpaceAndCrafts/Art/Enemies/MuddleWatercolorPalette/`. Concept only; no Meshy or Unity output. This must reuse an existing absorption invention if the new tool would duplicate one.






