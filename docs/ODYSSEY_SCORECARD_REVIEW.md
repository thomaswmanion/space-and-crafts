- [x] **Iteration 664 — check planted feet through a slow run without leaving the test pad (2026-10-20):** the first post-Iter660 run capture at 0.65 m/s walked off the 2 m pad near its end, so its later low-support frames could not establish contact on the pad. Reduced only the capture harness speed to 0.45 m/s and re-rendered a 12-frame run sequence that stays within `ART_TraversalPad_0`. The runtime diagnostics logged **7 foot-plant events**. Across the sequence the lower leg-weighted foot stayed roughly within **6 mm** of the target plane; the opposite foot rose by up to about 4 cm during swing, as expected. The conservative whole-skin renderer bound remained far below the pad and is not a sole measurement. Reviewed side-contact frame 6 shows alternating foot lift with a grounded support foot, though the slow gait and temporary lime pad remain staged evidence. No player-facing source change in this capture-only iteration, no score increase, and no human-play review. Scores remain Splat **9**, enemies **7**, Graphics **10**, Story/Characters **7**, Fun/Controls **5**, Overall **7**. Unity batch compiled and rendered 12 frames; the shutdown refresh reentrancy assertion followed image saving. No formal tests or human play. No Meshy credits spent; usage remains unverifiable.
  Evidence: `Assets/SpaceAndCrafts/Scripts/Editor/ArrivalVisualCaptureCli.cs`, `Logs/score-comparison/capture-splat-leg-weighted-slow-run-contact-iter663.log`, `Logs/score-comparison/capture-splat-run-contact-within-pad-iter664.log`, and `Logs/score-comparison/splat-run-contact-within-pad-iter664/frame_06.png`.
- [x] **Iteration 662 — seat the first grounded landing frame and remove rebound wobble (2026-10-20):** changed Splat's landing envelope from a cosine recoil to a monotonic critically damped settle, and removed the flipper-tip cosine recoil so the landing no longer adds a second oscillation. Raised only the visual-root sole-correction cap from 5 cm to 10 cm; gameplay root and CharacterController remain untouched. In the 24-frame Unity World 02 jump/landing capture, the first captured grounded landing pose has both leg-weighted minima around **0.630–0.631 m** for target **0.631 m**; following recovery frames remain at target, while the main renderer envelope is still conservatively about 8.6 cm lower and is not used as sole evidence. Reviewed frame 18 shows both feet contacting the pad. This is one staged landing on a flat lime test pad; no real gameplay or human feel review. Keep all scores unchanged (Splat **9**, enemies **7**, Graphics **10**, Story/Characters **7**, Fun/Controls **5**, Overall **7**). Unity batch compiled/rendered 24 frames; the known refresh reentrancy assertion followed image saving during shutdown. No formal tests or human play. No Meshy credits spent; usage remains unverifiable.
  Evidence: `Assets/SpaceAndCrafts/Scripts/Player/SplatSkeletonAnimator.cs`, `Assets/SpaceAndCrafts/Scripts/Art/SplatVisualRepair.cs`, `Assets/SpaceAndCrafts/Scripts/Editor/ArrivalVisualCaptureCli.cs`, `Logs/score-comparison/capture-splat-exact-landing-contact-iter662.log`, and `Logs/score-comparison/splat-exact-landing-contact-iter662/frame_18.png`.

- [x] **Iteration 661 — replace the damped landing cosine with a single settle (2026-10-20):** changed the torso/hip/antenna landing envelope and flipper-tip response to stop the cosine rebound from continuing after touchdown. The 24-frame staged sequence captured takeoff, apex, touchdown, and recovery. It also exposed a remaining contact limit: the first grounded pose had a weighted-foot point about **7 cm** above target, and the former 5 cm visual correction cap left about 2 cm; later frames settled onto the pad. Iter662 adds enough correction range and recaptures touchdown. This is not human-play evidence; no score increase. Unity batch compiled and rendered; the shutdown refresh reentrancy assertion followed saved output. No formal tests or human play. No Meshy credits spent; usage remains unverifiable.
  Evidence: `Assets/SpaceAndCrafts/Scripts/Player/SplatSkeletonAnimator.cs`, `Assets/SpaceAndCrafts/Scripts/Editor/ArrivalVisualCaptureCli.cs`, `Logs/score-comparison/capture-splat-single-settle-landing-iter661.log`, and `Logs/score-comparison/splat-single-settle-landing-iter661/frame_18.png` through `frame_20.png`.
- [x] **Iteration 660 — align platform seating to both leg-weighted foot regions (2026-10-20):** the combined skin has no Boot/Sole renderer (`boots=0`), but its visible rig has named `Bone_LegL` and `Bone_LegR` weights. `SplatVisualRepair` now caches vertices with at least 0.2 weight to each leg, bakes the posed skin, and aligns from the lower current foot region; it falls back to the complete baked skin for rigs without those bones and still prefers dedicated boot renderers when available. A 12-frame World 02 capture reports both leg-weighted minima at about **0.980 m**, against target **0.981 m** on `ART_TraversalPad_1` (top **0.9832 m**), with only **1 mm** correction. The broad renderer bounds remain about **56.5 mm below** the pad; the capture harness now labels that as `rendererBoundsGap` and reports per-foot contact separately. Reviewed contextual frame 6 shows contact, but prototype props and overlaid staging text still obstruct the shot. This is one idle pose on one flat pad; walk/run, jumps, slopes, moving supports, and human play remain unverified. No score increase: Splat **9/100**, enemies **7/100**, Graphics **10/100**, Story/Characters **7/100**, Fun/Controls **5/100**, Overall **7/100**. Unity batch compiled and rendered 12 frames; the refresh reentrancy assertion followed saved images at shutdown. No formal tests or human play. No Meshy credits spent; usage remains unverifiable.
  Evidence: `Assets/SpaceAndCrafts/Scripts/Art/SplatVisualRepair.cs`, `Assets/SpaceAndCrafts/Scripts/Editor/ArrivalVisualCaptureCli.cs`, `Logs/score-comparison/capture-splat-leg-weighted-platform-contact-iter660.log`, and `Logs/score-comparison/splat-leg-weighted-platform-contact-iter660/frame_06.png`.
- [x] **Iteration 659 — render the jump arc after hardening jump scale (2026-10-20):** added and ran a 24-frame World 02 Unity batch capture through takeoff, apex, landing, and recovery after Iter658's uniform-scale guard. The sequence compiled and rendered; reviewed apex (`frame_12.png`) and landing (`frame_20.png`) show the torso remaining proportionate, with the feet meeting the lime test pad on the landing frame. The capture does not reproduce the user's reported 3x whole-body stretch: renderer local/lossy scales stay 100/95. Main-skin baked Y bounds vary from about 1.00 to 1.24 m across the posed sequence, but that aggregate includes articulated geometry and cannot isolate torso stretch. Therefore mark the reported problem **not reproduced in this staged sequence, not resolved for live play**. The pose is still procedural and the capture pad is temporary. No score increase; Splat 9/100, enemies 7/100, Graphics 10/100, Story/Characters 7/100, Fun/Controls 5/100, Overall 7/100. Unity's refresh reentrancy assertion followed frame saving during shutdown. No formal tests or human play. No Meshy credits spent; usage remains unverifiable.
  Evidence: `Assets/SpaceAndCrafts/Scripts/Player/SplatFeelPresenter.cs`, `Assets/SpaceAndCrafts/Scripts/Editor/ArrivalVisualCaptureCli.cs`, `Logs/score-comparison/capture-splat-jump-scale-guard-iter659.log`, `Logs/score-comparison/splat-jump-scale-guard-iter659/frame_12.png`, and `frame_20.png`.
## Iteration 658 — clamp legacy jump scale to a uniform, near-neutral pulse (2026-10-20)

`SplatFeelPresenter.ClampJumpShape` now averages serialized jump scale values and clamps the result to a uniform **0.98–1.02**. An old scene override with a large Y value can no longer make this presenter stretch Splat vertically; the bone-based jump pose remains in `SplatSkeletonAnimator`. This is a targeted source safeguard, not proof that the reported 3x stretch is fully resolved: no Unity build, new capture, runtime diagnostics, or player verification was performed. The existing Iter645 staged apex image did not display a 3x body elongation, but it predates this edit and cannot establish current behavior. Splat stays **9/100**; Graphics **10/100**, Story/Characters **7/100**, Fun/Controls **5/100**, Overall **7/100** remain unchanged. No Meshy credits spent; usage is unverifiable.

Evidence: `Assets/SpaceAndCrafts/Scripts/Player/SplatFeelPresenter.cs`; existing `Logs/score-comparison/splat-coordinated-jump-flippers-iter645/frame_10.png`.

## Iterations 655–657 — contextual Splat platform seating check (2026-10-20)

The first full-scene attempt aimed Splat at `ART_Zone_Mid_Floor`. It did not remain supported by that floor: the log records the named floor top at **0.2250 m**, but the CharacterController bottom later sat at **0.9632 m** because another platform in the stacked scene became the active support. Treat that capture as a scene/harness diagnostic, not evidence of contact with the named floor.

The Iter656 follow-up targeted `ART_TraversalPad_1`. Renderer bounds appeared 2 mm below the **0.9832 m** pad top, but the diagnostic had not sampled the skin. A baked-vertex check then found the visible mesh bottom at **1.0369 m**, **53.7 mm above** the pad. This disproved the Iter653–656 bounds-only contact claims: the shoes were hovering despite the broad renderer envelope extending below the surface.

Iteration 657 aligns the lowest visible skinned vertices after the animated pose using a reusable baked mesh and list, and refreshes the late-bound repair component reference. The 12-frame full-World-02 capture reports the baked skin bottom at **0.9803 m**, about **2.9 mm below** the **0.9832 m** pad top. The renderer bound still reads **56.6 mm below** the pad, so it is not valid contact evidence. This supports close contact for one grounded idle pose on one flat pad; there is no separately named boot renderer, and run/jump, slope, moving-platform, and human-play contact remain unreviewed. The frame still contains prototype props and staging text.

Scores remain Splat **9/100**, enemies **7/100**, Graphics **10/100**, Story/Characters **7/100**, Fun/Controls **5/100**, Overall **7/100**. No score increase: one corrected idle contact case does not resolve Splat's prototype form or animation quality. Unity batch compiled and rendered 12 frames; the refresh reentrancy assertion followed saved frames during shutdown. No formal tests or human play. No Meshy credits used; monthly usage remains unverifiable.

Evidence: `Assets/SpaceAndCrafts/Scripts/Art/SplatVisualRepair.cs`, `Assets/SpaceAndCrafts/Scripts/Player/SplatSkeletonAnimator.cs`, `Assets/SpaceAndCrafts/Scripts/Editor/ArrivalVisualCaptureCli.cs`, Iter655–657 capture logs, and `Logs/score-comparison/splat-world-traversal-pad-idle-contact-iter657/frame_06.png`.
## Iterations 652–654 — greeting motion and grounded pad contact (2026-10-20)

Iteration 652 increased the idle greeting's existing right-flipper-tip motion to a 34° sinusoidal beat at 8.5 rad/s. The 40-frame Unity sequence log records the tip traversing about 62° while the greeting is active. In the reviewed frame, Splat's arm is raised, but the gesture still reads more like a held greeting than a clear wave because the head partly occludes the flipper. This is a measured procedural pose change, not a new imported animation or evidence that the full character feels natural in play.

Iteration 653 reduced the grounded visual inset from 18 mm to 2 mm. Unity batch captures on pads at 0.6332 m and 0.9832 m show the skinned bound 2 mm below the corresponding top face, and both reviewed low-angle frames show the rounded shoes meeting the capture pad more closely. The active skin has no dedicated boot renderer (`boots=0`), so the measurement still uses the whole skinned renderer bounds rather than boot vertices. The prototype shoe shape and temporary lime capture pad make these staged frames only a limited visual contact check; the ground capture still looks soft, and this does not establish reliable contact on varied platform geometry or during human play.

Iteration 654 refined the same greeting after the largest Iter652 tip beat left the arm nearly horizontal in its strongest frame. The shoulder now lifts farther and the flipper-tip beat is smaller/slower. Frames 4 and 7 from the new 40-frame sequence show the arm moving from tucked alongside the head into an outward greeting. The gesture is easier to read as an action, but the single rounded flipper and partial overlap with the oversized head still keep it far below authored-character animation quality.

Keep Splat at **9/100**, Graphics **10/100**, Story/Characters **7/100**, Fun/Controls **5/100**, and Overall **7/100**. The still-low scores reflect the unresolved generic character, subtle procedural animation, and jelly-like movement report. The project remains far below the **90/100** Odyssey-relative acceptance target. Unity batch compiled and rendered the greeting cycle plus lower/elevated 12-frame idle contact captures; the existing `gRefreshReentrancyCount > 0` assertion followed saved images during shutdown. No formal tests or human play occurred. No Meshy credits were spent; monthly usage remains unverifiable.

Evidence: `Assets/SpaceAndCrafts/Scripts/Player/SplatSkeletonAnimator.cs`, `Assets/SpaceAndCrafts/Scripts/Art/SplatVisualRepair.cs`, `Assets/SpaceAndCrafts/Scripts/Editor/ArrivalVisualCaptureCli.cs`, `Logs/score-comparison/capture-splat-idle-greeting-front-cycle-iter652-wave.log`, `Logs/score-comparison/capture-splat-idle-greeting-front-cycle-iter654.log`, both Iter653 contact capture logs, and their Iter652–654 frame folders under `Logs/score-comparison/`.

## Iteration 314–317 — Splat candidate rejected in gameplay view (2026-10-19)

After fixing the Iter310 shader, the FBX was captured in a rest-pose front view and an isolated run-pose view. The front rest pose is more coherent than Iter263, but it still has a generic egg-like torso and simple face. The gameplay camera capture at `Logs/score-comparison/arrival-gameplay-rear-iter316.png` reveals long, pointed arm and leg shapes that read as spikes from behind; reject the candidate for active use. Restored Iter263 as the first resource and verified it with the fresh `Logs/score-comparison/arrival-gameplay-rear-iter317.png` capture.

Corrected the previous deformation diagnosis. The FBX body's imported renderer has local scale `(100,100,100)` and lossy scale `(95,95,95)`, while `sharedMesh.vertices` remain tiny import-space coordinates. The old raw comparison against `BakeMesh` was invalid because it failed to scale the rest vertices. After scale compensation, the run-pose max delta is **0.0677** and average **0.0380** units. Reconstructed bind-pose matrices have maximum error **0.0815** at `Bone_ArmTipL`. These values support a plausible skin bind and modest sampled run pose; they do not establish natural motion. The gameplay-view silhouette is the reason for rejection.

Scores remain Graphics **10**, Story/Characters **7**, Fun/Controls **5**, Overall **7**. No human play occurred. The Unity batch images were rendered and inspected; no formal gameplay test occurred. No Meshy credits were spent.

## Iteration 311 — initial Unity shader failure (2026-10-19)

The Iter310 FBX export was imported and rendered in a Unity batch Play Mode capture. It failed visibly: magenta missing-shader body, severe top clipping, no readable face. The custom shader emitted compilation errors for the `point` parameter token and missing `ApplyShadowBias`; these source errors were corrected afterward. The original raw vertex-delta numbers in `arrival-candidate-iter311-front.log` (3.2292 max / 2.4858 average) were invalid because the comparison ignored the FBX renderer local scale of 100; see the corrected scale-compensated Iteration 315 measurement. Iter263 is again the first active resource candidate. Scores remain 10 / 7 / 5 / 7 because no player-facing gain was made. Later captures at Iterations 314–317 resolved the render-scale mismatch and rejected the gameplay silhouette; the candidate remains out of the active game.

Iteration 312 rendered `Logs/score-comparison/splat-current-front-iter312.png` after restoring Iter263. Its shader and framing render correctly; visually the character remains generic with broad blunt arms, an oversized smooth body, a simple face, and little visible wayfinding identity. This confirms the rollback, not a character improvement. The earlier 0.2062-unit raw displacement comparison was also scale-sensitive and is not used to rate deformation. Iterations 314–317 contain the corrected diagnostic and gameplay-view decision.

## Iteration 305 — face painted into fused skin (2026-10-18)

Iteration 305 removes the separate eye whites, irises, pupils, glints, and smile from the Iter285 source rig, then paints those details, soft cheek color, and a curved smile into the continuous body's vertex-color layer. Blender rendered both front and rear candidate previews. The front now reads warmer and less like a set of facial buttons; the rear keeps the map panel on the skin. The main silhouette remains generic and the map icon remains soft. Treat this as a promising isolated design study only: no Unity export/import, player-facing capture, skin deformation or animation review, or human play occurred. Do not replace the active Iter263 game model yet.

Evidence: `docs/art/splat-candidate-iter305/previews/front-three-quarter.png` and `docs/art/splat-candidate-iter305/previews/back-pack.png`. The current-game image is still Iter296. Scores remain Graphics **10**, Story/Characters **7**, Fun/Controls **5**, Overall **7**; the attractive front render cannot prove in-game quality, natural motion, or character appeal during play.
## Iteration 303 — remove stacked locomotion easing (2026-10-18)

The source audit traced one plausible “jelly” contributor in `SplatMotor`: planar acceleration had a second 0.72 soft-start blend, facing rotation applied `RotateTowards` and then an exponential Slerp, and gravity was reduced to 72% for a wide vertical-speed band near the apex. Removed the extra planar blend, the second rotation smoother, and the broad apex gravity reduction. Configured acceleration/turn rate remain the single ground response controls; ordinary gravity now governs the jump arc. This is a code-level hypothesis aimed at more direct starts, turns, and apex timing, not proof the movement now feels good.

No Unity build, runtime, capture, automated test, or human play occurred. Keep Fun/Controls at **5/100**, Overall at **7/100**, and all scores unchanged until the creator reviews actual movement. There is also a risk that the jump now feels too sharp or arcs too briefly; assess controller and keyboard movement plus low/high jumps at normal frame rate before considering further tuning.
## Iteration 302 — Splat face-scale candidate review (2026-10-18)

Rendered isolated Blender candidate previews for Iterations 301 and 302; these are design studies, not Unity captures. Iteration 301's vertex-color map patch reads as a soft, blurry oval and its eye shells still dominate the face. Iteration 302 reduces eye whites, iris, and pupil dimensions and seats the eyes deeper. The face is somewhat less wide-eyed, but the overall model remains a generic pear-shaped blob with side knobs, a thin smile, and a rear patch that does not clearly read as a map. This does not satisfy the creator's character direction and must not replace the active Unity model.

Evidence: `docs/art/splat-candidate-iter301/previews/front-three-quarter.png`, `docs/art/splat-candidate-iter301/previews/back-pack.png`, `docs/art/splat-candidate-iter302/previews/front-three-quarter.png`, and `docs/art/splat-candidate-iter302/previews/back-pack.png`. Blender generated these preview images and saved the candidate files. No Unity import, in-game render, deformation review, animation test, or human play occurred. Meshy remains unavailable for paid work because this month's allowance and usage cannot both be verified.

| Required category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **10/100** | No player-facing environment or UI changed; latest valid Unity evidence remains Iteration 296. |
| Story, characters, and emotional appeal | **7/100** | Iteration 302 is an isolated candidate and still misses the requested authored, expressive character quality. Active Splat is unchanged. |
| Fun, controls, and moment-to-moment play | **5/100** | No gameplay or movement evidence changed; the jelly-like feel report is still unaddressed through human play. |
| Overall quality | **7/100** | No player-facing improvement supports raising any score; all remain far below the 90/100 target. |

Decision: keep Iteration 302 out of the game. The next useful character pass needs stronger authored facial expression and a recognizable art/mapmaking motif that survives the rear gameplay camera, then a rig deformation check before Unity integration. Do not keep polishing the present smooth blob through minor proportions alone.
## Iteration 297 — Meshy character asset availability check (2026-10-18)

Rechecked Meshy before any generation or rigging work. The account balance endpoint reports 2,935 monthly credits plus 20 permanent credits, but the Oct 1–19 usage query returns 403 (“Studio and Enterprise only”), so the monthly allowance and credits already spent this month cannot be verified. Under the project budget rule, no paid generation or rigging was started.

The Meshy inventory contains one completed “Splat Character 3D” image-to-3D task (`38338327-5dd1-41ad-b394-9208a1964ffb`), available only as GLB, reporting zero task credits. Queries across image-to-3D, rigging, and animation show no Meshy rig or animation output. Its signed GLB and texture URLs expired on 2026-08-31; a free download attempt returned 403, so the model cannot be inspected or integrated from this task. The active Arrival character remains the Blender-authored Iteration 263 skinned candidate with procedural poses. This audit provides no player-facing improvement and does not change any score.

| Required category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **10/100** | The Iteration 296 capture still shows a slab causeway with hard water seams, detached pads, loose props, angular cliffs, and a generic field-note panel. The asset audit changes no rendered game content. |
| Story, characters, and emotional appeal | **7/100** | The active Splat still reads as the purple shape the creator rated 7/100. The old Meshy task has no accessible file or listed rig/animation output, so it cannot support a character-quality claim. |
| Fun, controls, and moment-to-moment play | **5/100** | No movement or gameplay changed; the creator’s jelly-like feel report remains unaddressed by human play. |
| Overall quality | **7/100** | No player-facing evidence changed; the result remains an early prototype far below the 90/100 target. |

Next character step requires either a retrievable cohesive source asset that can be inspected and rigged, or verifiable Meshy allowance and month-to-date usage before any paid work. Keep scores unchanged until a build and player-facing review show a real improvement.

## Iteration 296 — Arrival HUD hierarchy and objective context (2026-10-18)

Removed the redundant “TRAILS” caption and decorative divider from the objective card, gave the primary goal more vertical room, and reduced the card height. The optional discoveries now sit in one quiet footer line. Objective copy connects the gathering and crafting steps to the established Vesper relay story: find parts to reach Vesper, build the Boing-O-Matic to reach Vesper, then follow Rattle's relay. This is a focused readability/context improvement; it does not fix the scene, character design, or movement.

Unity 6000.5.4f1 batch Play Mode saved `Logs/score-comparison/arrival-gameplay-rear-iter296.png`. The first attempt used `-nographics`, produced a blank image due to an invalid render target, and was discarded; the same capture was rerun with rendering enabled and visually inspected. The valid image shows a compact 3-row hierarchy and a legible two-line objective. The fjord still reads as a slab causeway with water seams and detached pads, and Splat is still a simple purple rear silhouette. The log has no C# compiler errors, but UnityMCPServer logs a `StopAssetEditing` assertion during shutdown; this is a capture, not a clean automated gameplay test. No human play or formal tests were performed.

| Required category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **10/100** | The objective card is shorter and less cluttered, but the captured scene remains dominated by flat slate slabs, hard water edges, detached pads, scattered props, and angular cliffs. A UI refinement does not materially change the whole-scene comparison. |
| Story, characters, and emotional appeal | **7/100** | The goal now gives a clearer reason to collect and build: reaching Vesper. It still supplies no on-screen relationship beat, and the active Splat remains the purple shape the creator rates 7/100. |
| Fun, controls, and moment-to-moment play | **5/100** | No movement behavior changed or received human-play review. The user's jelly-like movement report remains stronger evidence than the motor settings or a still capture. |
| Overall quality | **7/100** | Mean of the three category scores rounds to 7. The narrow HUD gain does not address the weak character, environment, and reported feel. |

The UI diagnostic moves **18→20/100** for a cleaner hierarchy and story-connected objective; the four Odyssey-relative scores stay **10 / 7 / 5 / 7**. The comparison page includes the Iteration 295/296 side-by-side. Meshy usage is still unavailable (403), so no generation was attempted. Highest-impact next work remains replacing Splat with cohesive, natural-feeling character art, then rebuilding the fjord route and validating movement/loop through human play.

## Baseline reassessment — candid calibration before HUD iteration 296 against the current build (2026-10-18)

This review supersedes the previous *current* scores below; those scores were too generous for the player-facing result. The latest available Unity Play Mode image is `Logs/score-comparison/arrival-gameplay-rear-iter295.png` (captured in the earlier Iteration 295 batch run). The close front view is Iteration 283. No new build, render, test, or human play occurred during this reassessment. The user's firsthand reports—Splat looks assembled and creepy, the interface feels like translucent labels, and movement feels jelly-like—are evidence and are weighted alongside the actual captures. Odyssey images on the comparison page are official promotional art, not matched gameplay shots.

| Required category | Reassessed score | Reasons and evidence |
|---|---:|---|
| Graphics and visual polish | **10/100** | Iteration 295 shows a coherent cool fjord palette and pink sky, but the walkable world reads as a broad slate causeway laid across blue water, with abrupt cut edges, detached side pads, loose debris, angular cliffs, distant clutter, and a generic translucent mission card. Iteration 293 reduced the card; it still lacks a distinct visual system. This is prototype dressing, far below the layered composition and finish visible in Odyssey's promotional reference. |
| Story, characters, and emotional appeal | **7/100** | The creator rates Splat itself 7/100; the scorecard preserves that direct character judgment. The Iteration 283 front render supports it: a simple purple head/body mass, very large inset-looking eyes, blunt arms, tiny feet, and a separate-looking antenna. Iteration 295's rear view reads as a smooth purple blob with little maker identity. The current visible goal, “Find spring, metal dish, and power crystal,” supplies errands but no visible stakes, relationship, or emotional story beat. |
| Fun, controls, and moment-to-moment play | **5/100** | The user reports jelly-like movement; recent motor tuning has no human-play evidence that it fixed that feel. The capture is a still and cannot demonstrate responsive starts/stops, satisfying jumps, readable landings, or an enjoyable build/use/boss loop. Code and automated assertions establish that systems exist, not that the play is fun. |
| Overall quality | **7/100** | Calibrated from the three category scores (rounded mean 7.3). The build has a recognizable setting and an objective, but the central character, UI, terrain composition, reported movement, and unproven loop all remain prototype-level. No category approaches 30/100, much less the 80/90 acceptance bar. |

**What changed:** no game feature improved in this reassessment. The score correction reflects better calibration to the player's direct review and the actual Iteration 295 image. Previous 12/7/6/8 values are retained only as historical scores and should not be used as the current rating. The Iteration 295 saddle connection is not counted as a visual improvement: the captured route still has visible water seams and detached platforms.

**Meshy evidence:** the connected account lists one succeeded `image-to-3d` asset, “Splat Character 3D”; its task reports 0 consumed credits. The model listing/status has no corresponding rigging or animation task. In the project, `SplatVisualRepair`/`OrganicSplatBuilder` install the Blender-authored `Splat_Rigged_Candidate_Iter263` resource and `SplatSkeletonAnimator` drives poses. The separate `Splat_Hero_URP` prefab has a `MeshFilter` and `MeshRenderer`, not a `SkinnedMeshRenderer` or Animator, and it is not the active Arrival hero. So Meshy has been used for a Splat model, but not for the active, naturally moving character. The balance endpoint reports 2,935 monthly plus 20 permanent credits; the Usage endpoint returns 403 (Studio/Enterprise only), so monthly allowance and current-month spending remain unverifiable. No paid task was started in this reassessment.

**Highest-impact work:** first, replace Splat with one cohesive, expressive, character-specific design and a rig/animation pipeline that visibly moves the whole body naturally; do not keep layering shapes onto the current blob. Second, redesign the Arrival route and horizon as connected authored fjord terrain with safe recoveries and no visible slab gaps. Third, replace the label-card treatment with a small, deliberate HUD. Then validate the complete gather→build→use→boss loop and movement through a human play session. Keep all scores below 90 until player-facing evidence warrants a change.

## Iteration 284 — candid assessment against the Odyssey quality bar (2026-10-18)

This reassessment responds to the creator's direct review and the latest actual Unity captures from Iteration 283. No build changes were made. The four required categories all remain below 30/100. The evidence does not support treating the Nordic palette or a legible objective card as signs of a polished game: they are isolated strengths inside a prototype-level presentation.

| Required category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **12** | `arrival-gameplay-rear-iter283.png` has a recognizable fjord palette and sunset, but the playable foreground is a set of broad, flat-topped slate plates with exposed sheer sides, abrupt water cuts, and scattered piles. The distant scene is crowded with small colorful objects and a gate, while Splat and the objective card compete with the landscape. This reads as assembled graybox dressing, not an authored, cohesive platform-adventure world. |
| Story, characters, and emotional appeal | **7** | The creator's direct Splat rating is 7/100. `splat-current-front-iter283.png` supports it: a smooth purple torso, outsized ringed eyes, blunt horizontal arms, little costume or maker identity, and a star-tipped antenna that reads as an attachment. The face is friendly at a glance but not distinctive or emotionally expressive enough to carry a lead character. The field-note gives a gather objective, but these captures do not show a compelling character relationship, conflict, or story moment. |
| Fun, controls, and moment-to-moment play | **6** | The creator reports that movement feels jelly-like. That firsthand experience outweighs any inference from a still image. The frame shows a dotted route hint across a gap and collectible/prop clutter, but cannot establish responsive acceleration, satisfying jumps, clear landing feedback, or a fun build-use-boss loop. Movement code damping from earlier iterations has not been verified in continuous human play. |
| Overall quality | **8** | Rounded mean of 12, 7, and 6. The captures show a prototype with a recognizable setting and readable task, but weak character design, disconnected-looking traversal space, clutter, generic UI, and reported poor movement. There is no basis for a score above 30 in any required category. |

The custom Atkinson typeface is a small improvement to text readability; it does not materially raise these overall judgments. Relative to Iteration 283, the scores do not change because this is a more explicit reading of the same build evidence and direct user feedback, not a player-facing implementation. The side-by-side board at `docs/ODYSSEY_VISUAL_COMPARISON.html` contains the latest Unity captures and Odyssey promotional references; those images are not matched gameplay cameras, so the comparison is about polish, silhouette, composition, and clarity rather than frame-for-frame content.

Highest-impact next work: replace the active Splat with a genuinely cohesive, expressive, naturally moving character; rebuild the Arrival playable route as connected authored terrain with safe recoveries and no visible slab voids; replace the label-card HUD with a designed in-world-feeling interface; and validate movement plus the gather→build→use→boss loop through continuous human play. Do not spend Meshy credits until monthly allowance and current-cycle usage are verifiable.

Diagnostics stay World **11**, UI **15**, Splat **7**, animation/motion **5**, controls **6**, traversal **8**, story/loop **8**. No human play or formal tests were performed in this reassessment; Iteration 283's Unity batch captures are the rendered-build evidence. No Meshy credits were spent.
## Iteration 283 — readable runtime UI font

Added the SIL Open Font License Atkinson Hyperlegible regular/bold faces and wired `RuntimeUiFactory.Label` to select a face by requested weight, retaining the built-in Unity fallback. Unity batch Play Mode compiled and rendered `Logs/score-comparison/splat-current-front-iter283.png` and `arrival-gameplay-rear-iter283.png`; the front and follow captures confirm the custom font is in use. The task text is easier to distinguish, but the large field-note panel and generic framing remain, so this is a small legibility improvement, not a finished interface.

Core scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. UI diagnostic rises **14→15** based on the actual Unity renders; World **11**, Splat **7**, animation/motion **5**, controls **6**, traversal **8**, and story/loop **8** remain. No Unity gameplay logic, Splat model, route, or dialogue changed. No human play or formal tests were performed; no Meshy credits were used.

## Iteration 282 — consolidated Arrival field note

The primary objective and optional trails now appear in one shared field-note panel. The note keeps the three recipe icons, gives the next step a larger text area, and separates optional discoveries with a restrained divider. Unity batch Play Mode compiled and rendered `Logs/score-comparison/splat-current-front-iter282.png` and `arrival-gameplay-rear-iter282.png`; the front and follow views show the recipe and side-trail progress together without a second card. The panel still uses generic system typography, takes a large upper-left area, and reads as an overlay. This is a modest UI improvement, not a complete redesign.

Core scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. The UI diagnostic rises **13→14** for the improved task hierarchy; World **11**, Splat **7**, animation/motion **5**, controls **6**, traversal **8**, and story/loop **8** remain. Splat, terrain, and movement did not change. No human play or formal tests were performed, and no Meshy credits were used.

## Iteration 281 — candidate silhouette review, no player-facing change

Rendered `docs/art/splat-candidate-iter281/previews/front-three-quarter.png` and `back-pack.png` from the separate Blender candidate. The front confirms that the current modeling approach is not yet solving the design problem: the fused body is still a near-oval, arms form blunt side knobs, eyes and mouth look layered onto the surface, and the thin antenna plus leaf look attached rather than grown from the character. The rear pocket is a useful mapmaker cue, but reads as a patch and does not rescue the silhouette. Do not promote this candidate to Unity.

The active build's latest evidence remains the Iteration 270 Unity front of Splat and Iteration 278 Arrival follow view. The new Blender previews are not gameplay screenshots. Scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; diagnostics remain World **11**, UI **13**, Splat **7**, animation/motion **5**, controls **6**, traversal **8**, story/loop **8**. Every score is below 30. Graphics stays low because the actual frame has disconnected platforms, hard water seams, scattered props, and generic overlay panels. Character stays at the creator's 7 because the in-game model is still generic and the candidate did not pass silhouette review. Fun stays low because the jelly-motion report is unrefuted by stills or source changes. Overall is the rounded mean of the three required sub-scores.

No candidate import, Unity capture, human play, formal test, or Meshy generation occurred. Current-cycle Meshy usage and allowance remain unverified; no credits were spent.

## Iteration 280 — original Splat art direction target, not implemented

Added a new, versioned four-view concept reference at `docs/art/splat-character-concept-iter280.png`. It explores a softly tapered purple teen-alien mapmaker with a leaf-shaped navigation marker, restrained contour-map skin marks, integrated feet, and a rear satchel. The old spherical/boots/star study is no longer the lead reference. This is still an illustration, not a new model, rig, or gameplay capture; scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. The newest Unity evidence is still Iteration 278 for Arrival and Iteration 270 for Splat's front. Meshy check: balance **2,935 monthly + 20 permanent**, resets 2026-11-10; the usage endpoint returned 403, so the allowance and cycle spend remain unverified. The only listed character task is the old unrigged Splat image-to-3D model; no rigging or animation tasks are listed. No Meshy credits were used.

## Iteration 279 — Splat motion damping, source-only

Scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. The new code pass removes the continuous idle/run body pulse from the skinned rig, reduces small idle/run motion and jump flutter, and narrows airborne arm spread. This addresses one plausible contributor to the creator's jelly-like motion report, but no Iteration 279 capture or human play was performed. The latest actual Unity screenshots remain the Iteration 270 Splat front idle and Iteration 278 Arrival follow-camera frames; those are shown beside official Odyssey art in `docs/ODYSSEY_VISUAL_COMPARISON.html`. No score increase is supported by source changes alone. Diagnostics remain World **11**, UI **13**, Splat **7**, motion **5**, controls **6**, traversal **8**, story/loop **8**, all below 30. No formal tests, human play, or Meshy credits were used; Meshy allowance/current-cycle usage remain unverified.

## Iteration 278 — enemy visual motion decoupling (2026-10-18)

A source audit found EnemyVisualTell moving and scaling the same transform used by Scrap AI. That overwrote AI-controlled root motion such as the Scrap Floater orbit and Roller charge rotation. Abs(Sin) also doubled the cosmetic hop rate, and the root scale pulsed by 10%. The visual tell now moves a child pivot containing rendered children, leaving root movement and colliders under AI ownership. Secondary hops, hover, halo rotation, and glow pulse were reduced. Floater-ring, roller-stripe, spitter-mouth, and face lookup now traverses the pivot hierarchy.

A final Unity batch Play Mode camera render saved Logs/score-comparison/arrival-gameplay-rear-iter278.png; the final capture log contains the saved screenshot message and no C# compiler errors. The frame still shows interrupted slate platforms, the broad water crossing, prop clutter, and the compact but generic HUD. It is a still, so it does not verify that patrol, orbit, charge, or jump now feel natural. No formal tests or human play were performed.

Scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; diagnostics remain World **11**, UI **13**, Splat **7**, motion **5**, controls **6**, traversal **8**, story/loop **8**. No score rises without continuous player-facing evidence. Meshy credits remain unused.
## Iteration 277 — rejected slate causeway review (2026-10-18)

Unity batch rendering produced `Logs/score-comparison/arrival-gameplay-rear-iter277.png`. The causeway candidate read as a narrow slate slab laid across a large flat water sheet, with abrupt joins to the already broken shelf. It did not create the requested Norway-like continuous terrain or a forgiving path with no dangerous falls, so the candidate was removed from `ArrivalMeshyPathDress`; the capture remains as rejected evidence. This is a visual review of a static frame, not a human playthrough or proof of fall recovery.

Scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. Diagnostics remain World **11**, UI **13**, Splat **7**, motion **5**, controls **6**, traversal **8**, story/loop **8**. The frame still shows a generic stacked HUD and Splat's unconvincing rear silhouette; the creator's jelly-like movement report is still the only direct feel evidence. No Meshy credits were spent and no formal tests or human play were performed.

The next route attempt needs the terrain surface and fjord edge to meet as one intentionally shaped landform, with broad forgiving play space and hidden fall recovery. Do not use another flat bridge overlay as a visual fix. Compare current evidence in `docs/ODYSSEY_VISUAL_COMPARISON.html`.
# Odyssey-relative scorecard reviews

## Iteration 276 — Splat asset and rig audit (2026-10-18)

This was a source and asset audit, not a player-facing improvement. Scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; diagnostics remain World **11**, UI **13**, Splat **7**, motion **5**, controls **6**, traversal **8**, story/loop **8**. All categories remain below 30.

The active Arrival visual is the Blender-authored Iteration 263 skinned candidate driven by the project's procedural skeleton animator. The separate Meshy `Splat_Hero` visual is explicitly destroyed by `ArrivalMeshyPathDress.UseOriginalSplatVisual()`; its imported prefab is static and an older capture supports the prior judgment that the glossy, long-limbed result is creepy and wrong. Meshy account history contains one “Splat Character 3D” image-to-3D task, succeeded on 2026-08-28 with 0 reported credits, but no rigging or animation task. Its output expired on 2026-08-31 and its free download now returns 403, so the current Blender model's precise lineage from Meshy cannot be verified.

The Meshy balance query returned 2,935 monthly + 20 permanent credits, refreshing 2026-11-10. The usage endpoint returned 403; allowance and current-cycle spend are unverified, so no paid job was attempted. A reuse rigging attempt would cost 5 credits; a fresh textured `meshy-7.1` image-to-3D output plus rigging would cost 35 credits total. Both remain behind the user's 80% cap and explicit operation/cost confirmation. The next useful player-facing gate is a cohesive turntable-readable silhouette, an imported skinned rig, grounded locomotion, and distinct airborne/landing poses—not another still-only face tweak.

## Iteration 274 — calm fjord-water scale pass (2026-10-18)

The existing calm water texture now repeats every 40 world units instead of 22. The latest Unity follow-camera capture shows a broader, less frequent wave pattern. This is a subtle aesthetic improvement; the rough foreground shelf, crossing gap, clutter, and weak character remain. Scores stay Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; all diagnostics remain below 30. No gameplay mesh or rescue system was changed. No formal tests, human play, or Meshy credits were used.

Before/after: `Logs/score-comparison/arrival-gameplay-rear-iter271.png`, `Logs/score-comparison/arrival-gameplay-rear-iter274.png`.

## Iterations 272–273 — route-floor visibility experiment rejected (2026-10-18)

I tried restoring existing mid-path floor renderers and texturing them with the spawn shelf's slate material. The first render showed an oversized purple slab; excluding the island names from the secondary graybox palette did not fix it. Both implementation changes were reverted, preserving the prior world dressing and all colliders. Candidate captures: `arrival-gameplay-rear-iter272.png` and `arrival-gameplay-rear-iter273.png`. The evidence rules out simply re-enabling these graybox floors as a good-looking route solution. Scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. No Meshy credits were used.

## Iteration 275 — candid reassessment after creator critique (2026-10-18)

No game changes were made. I re-inspected the current Unity front capture (`Logs/score-comparison/splat-current-front-iter271.png`) and follow-camera capture (`Logs/score-comparison/arrival-gameplay-rear-iter274.png`) in light of the creator's direct feedback. The low scores are justified; prior small adjustments to Splat's smile/pose and the water texture do not change the build's prototype-level quality.

| Required category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **12** | The fjord palette and pink sky suggest a setting, but the actual frame is dominated by a broad cracked shelf with visible vertical seams, an abrupt water crossing, piles of unrelated props, repeated floating pickups/markers, and low-detail angular cliffs. The HUD occupies substantial screen space. This is a recognizable art direction without a coherent finished environment or polished presentation. |
| Story, characters, and emotional appeal | **7** | Splat has a unified purple body, large eyes, and a smile, but the silhouette remains head-heavy with a blank narrow torso, broad flipper-like arms, basic feet, and an awkward default pose. He does not yet read as a distinctive teenage inventor with expressive personality. The current frame also provides little story context or emotional connection. This supports the creator's 7/100 Splat assessment. |
| Fun, controls, and moment-to-moment play | **6** | The creator's firsthand report that movement feels jelly-like is the strongest available feel evidence and is negative. The UI communicates movement/jump and a gather objective, but screenshots cannot establish responsiveness, animation timing, traversal satisfaction, or the craft-to-boss payoff. No human playthrough was performed during this reassessment. |
| Overall quality | **8** | Rounded mean of 12, 7, and 6. The project has a recognizable Nordic direction and a readable immediate task, but the core character, environment, and feel are far below a polished commercial benchmark. |

Diagnostic scores: World **11**, UI **13**, Splat **7**, motion **5**, controls/feel **6**, traversal **8**, story/loop **8**. Every category is under 30. The comparison board uses official Odyssey promotional imagery rather than matched gameplay captures, so it is a directional quality comparison, not a controlled visual test. Current Unity screenshots are real batch renders; they do not show continuous movement. No Meshy credits were used; monthly allowance and usage remain unverified.

Highest-impact work: (1) replace/rework Splat as one cohesive, authored, rigged character with appealing silhouette and properly weighted natural movement; (2) redesign the Arrival route as a continuous, safe, authored-looking environment with terrain that meets the water naturally; (3) redesign the HUD hierarchy and visual language; (4) human-play and tune movement/animation before expanding the level. Do not raise any score until these player-facing gaps are improved and captured or played.

## Iteration 271 — Arrival gateway spacing pass (2026-10-18)

I reduced the two large repeated Meshy gateway copies near the Arrival entrance to one smaller decorative marker farther along the route. The authored scene gateway and gameplay collision systems remain unchanged. The current Unity rear capture reads less like stacked arches and reveals more of the mountain backdrop. It also makes the broad water crossing and thin route markers more obvious; the gap still feels unfinished, so this does not justify a graphics or traversal score increase.

Scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; diagnostics remain World **11**, UI **13**, Splat **7**, Motion **5**, Controls **6**, Traversal **8**, Story/loop **8**. These are static images, with no human play. The immediate next art task is an authored, safe-looking continuous route over/through the fjord that does not rely on a clutter of repeated objects. No Meshy credits were used.

Before/after: `Logs/score-comparison/arrival-gameplay-rear-iter270.png`, `Logs/score-comparison/arrival-gameplay-rear-iter271.png`; front capture: `Logs/score-comparison/splat-current-front-iter271.png`; Unity log: `Logs/arrival-capture-iter271-gateway-spacing.log`.

## Iteration 270 — warmer Splat idle expression (2026-10-18)

I enlarged only the active character's existing idle smile transform (1.18× width, 1.3× height) and captured the actual Unity front and follow-camera views. The mouth reads more clearly and warmly at gameplay scale. The body proportions, eye treatment, and rear identity remain weak; no new geometry or external assets were added.

Scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. Diagnostics remain World **11**, UI **13**, Splat **7**, Motion **5**, Controls **6**, Traversal **8**, Story/loop **8**. A still cannot prove natural motion or contradict the jelly-like feel report. The score remains unchanged because the face pass is narrow and character identity is not solved. No formal tests, human play, or Meshy credits were used.

Captures: `Logs/score-comparison/splat-idle-pose-iter270.png`, `Logs/score-comparison/arrival-gameplay-rear-iter270.png`; logs: `Logs/arrival-capture-iter270-idle-smile.log`, `Logs/arrival-capture-iter270-gameplay.log`.

## Iteration 269 — Splat idle rest-pose pass (2026-10-18)

I relaxed Splat's grounded idle arm angle on the active skinned rig from ±6° to ±20° and rendered current front and follow-camera frames through Unity batch Play Mode. The front still shows a head-heavy design, narrow blank torso, thin smile, and rudimentary feet; the new arm rest just makes the pose slightly less rigid. The rear view is nearly unchanged, and the world still reads as a cracked shelf leading to a cluttered gate. Scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; diagnostic scores remain World **11**, UI **13**, Splat **7**, Motion **5**, Controls **6**, Traversal **8**, Story/loop **8**. All remain below 30.

No human playthrough or tests were run, so this pose capture cannot prove natural motion or answer the jelly-like feel report. No Meshy credits were used. Captures: `Logs/score-comparison/splat-idle-pose-iter269.png`, `Logs/score-comparison/arrival-gameplay-rear-iter269.png`.

## Iteration 268 — candid reassessment and current side-by-side board (2026-10-18)

I agree with the creator's assessment that no current category should approach 30/100. The current Odyssey-relative scores are Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. Diagnostics are World **11**, UI **13**, Splat **7**, Motion **5**, Controls **6**, Traversal **8**, Story/loop **8**. UI 13 only credits the fact that the task and side goals can be read; their generic stacked-card styling remains visibly unfinished.

| Required category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **12** | The current Iteration 267 follow-camera capture has a recognizable fjord palette and sky. A broad cracked floor, abrupt water edge, loose prop piles, crowded gateway, distant visual clutter, and generic HUD dominate the frame. It looks like an early prototype with a color direction, not a polished level. |
| Story, characters, and emotional appeal | **7** | The Iteration 267 front capture shows a saturated purple body and large eyes, but also an outstretched default pose, head-heavy silhouette, narrow blank torso, thin smile, and rudimentary feet. The rear gameplay view offers no maker identity. The opening task can communicate what to gather, but the captured moment does not establish emotional attachment or a memorable character voice. This aligns with the creator's Splat rating of 7/100. |
| Fun, controls, and moment-to-moment play | **6** | On-screen controls and a parts list show intended interaction, not quality of feel. The creator's direct report of jelly-like movement is negative evidence; the still captures cannot test movement, recovery, invention payoff, or boss enjoyment. No human playthrough was performed in this review. |
| Overall quality | **8** | Rounded mean of the three required category scores. The game has a distinguishable setting and a clear immediate task, but the player-facing art, character appeal, and reported feel remain at prototype level. |

The comparison board now puts the Iteration 267 front and follow-camera captures beside official Odyssey character and kingdom imagery, and shows the four scores with their justifications. The Nintendo assets are promotional art rather than matched gameplay; they are useful for comparing visual communication, not a controlled image-quality test. These current stills are actual Unity batch renders, but they do not establish animation, responsiveness, or fun. No gameplay changed, no tests or human play were run, and no Meshy credits were spent.

## Iteration 267 — current loader-chain and visual-state check (2026-10-18)

Core scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. Fresh Unity front and follow-camera captures after the Iteration 265 rollback confirm that Iteration 263 is active, the previous eye-rim fit remains, the compact Iteration 264 side-trail copy is present, and the bad sash is absent. The front still shows the character's oversized head/eyes, thin smile, empty torso, and rudimentary flippers; the follow view has no inventor accessory. World composition and the stacked HUD remain rough. This is a current-state confirmation, not a quality gain.

| Required category | Score | Current evidence and reason |
|---|---:|---|
| Graphics and visual polish | **12** | Current follow frame shows the recognizable fjord palette but also the fractured flat shelf, hard water boundary, cluttered gateway, and generic stacked cards. |
| Story, characters, and emotional appeal | **7** | Current front shows the plain oversized face and blank body; current rear offers no inventor identity. This remains consistent with the creator's 7/100 Splat rating. |
| Fun, controls, and moment-to-moment play | **6** | These are static frames. Existing smoke99 confirms scripted systems, but no human motion review validates the reported jelly-like feel. Source-level acceleration/gait concerns remain hypotheses. |
| Overall quality | **8** | Rounded mean of 12, 7, and 6; the current-state capture does not support raising any category. |

Captures: `Logs/score-comparison/splat-current-front-iter267.png`, `arrival-gameplay-rear-iter267.png`; logs: `Logs/arrival-capture-iter267-current-front.log`, `arrival-capture-iter267-current-gameplay.log`. No human play, tests, or Meshy credits were used.
## Iteration 266 — source-level movement risk audit (2026-10-18)

The core scores stay Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. The current serialized motor values are MaxSpeed 7.75, Acceleration 62, Deceleration 96, JumpHeight 2.52, Gravity -46. The animator's top-speed run swing is approximately ±12°, with 0.015m vertical bob. These values suggest sharp velocity changes and a gait that may be too restrained to sell weight. That is a source-level risk to review, not proof of the user's jelly-like-motion report or a score improvement.

| Required category | Score | Current evidence and reason |
|---|---:|---|
| Graphics and visual polish | **12** | No image or world change. Existing Unity frames still show a broad fractured slab, abrupt water edge, cluttered gateway, and generic stacked HUD. |
| Story, characters, and emotional appeal | **7** | No character or story change. Splat remains the simple purple model rated 7/100 by its creator. |
| Fun, controls, and moment-to-moment play | **6** | Automated smoke 99 confirms the scripted pickup→craft→launch→boss system path. Current source values show fast acceleration/deceleration and small run-pose swing, but no human run/jump/landing review verifies how that feels. |
| Overall quality | **8** | Rounded mean remains 8 from 12, 7, and 6; no player-facing behavior changed. |

No gameplay code, tests, captures, or Meshy generation were performed for this source audit. Movement remains a high-priority human Play Mode review and tuning task.
## Iteration 265 — rejected map-sash fit (2026-10-18)

A mesh-only reuse trial brought the old authored continuous map sash into a copy of the current rig. The actual Unity front capture shows the sash becoming a dark, floating belt around Splat's waist. The old rig's rest proportions differ from the current fused body, so the part does not align. I removed the candidate from the runtime chain and archived its Blender source and construction script in `Logs/score-comparison/splat-candidate-iter265/`. Iteration 263 remains active; no changes reach the production character.

| Required category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **12** | The trial worsened the front silhouette and was rejected. Current build remains the Iteration 264 UI pass with a fractured platform, abrupt edges, clutter, and generic HUD. |
| Story, characters, and emotional appeal | **7** | The accessory did not fit and is not active. The current Splat still lacks a distinctive inventor identity. |
| Fun, controls, and moment-to-moment play | **6** | No gameplay changed; human movement review is still absent. |
| Overall quality | **8** | Rounded mean stays 8; the rejected candidate provides no player-facing gain. |

## Iteration 264 — compact optional quest copy (2026-10-18)

The core Odyssey-relative scores stay Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. The optional quest board now says “SIDE TRAILS” and shortens its two goals to “Rainbow wonders (0/2)” and “Secret Nook badge (0/1)”; its card is slightly narrower and shorter. Actual Unity front and gameplay frames show the counts remain legible and the board occupies less space. The UI diagnostic rises **12→13** for clearer, shorter progress copy only. It remains a dark framed label card with default system type, stacked above the primary objective, and the control strip still looks generic. Graphics overall stays 12 because the broader screen composition and world remain prototype-like.

| Required category | Score | Current evidence and reason |
|---|---:|---|
| Graphics and visual polish | **12** | The side-quest copy is cleaner, but the full follow-camera scene still has a fractured shelf, sharp water boundaries, prop clusters, crowded gateway, and a generic HUD. |
| Story, characters, and emotional appeal | **7** | No story or character content changed in this UI pass; Splat remains the same simple purple form. |
| Fun, controls, and moment-to-moment play | **6** | No movement behavior changed, and there was no human play evidence. Existing automated smoke 99 confirms pickups→Boing-O-Matic craft→pad launch→boss completion, but scripted systems passing does not establish feel or enjoyment. The jelly-like movement report remains relevant. |
| Overall quality | **8** | Rounded mean remains 8 from 12, 7, and 6. A small interface clarity fix cannot materially alter the wider game assessment. |

Captures: `Logs/score-comparison/splat-current-front-iter264.png`, `arrival-gameplay-rear-iter264.png`; Unity batch logs: `Logs/arrival-capture-iter264-hud-front.log`, `arrival-capture-iter264-hud-gameplay.log`. No human play, formal tests, or Meshy credits were used.

## Iteration 263 — Splat eye integration candidate (2026-10-18)

The core scores stay Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. The direct front comparison documents the change: Iteration 263 brings the existing eye layers closer to the head, reducing the pale outer rim and their floating appearance. The current follow-camera frame shows the rear silhouette and route but cannot establish front expression. Splat still has a largely blank purple torso, thin smile, and basic flippers/feet, so the creator's **7/100** character score remains appropriate.

| Required category | Score | Current evidence and reason |
|---|---:|---|
| Graphics and visual polish | **12** | The candidate makes one face detail sit more cohesively. The follow-camera frame still shows the broad fractured shelf, hard water and slab boundaries, prop clusters, crowded gateway, and generic stacked HUD. |
| Story, characters, and emotional appeal | **7** | Smaller eye rims improve the front slightly, but Splat remains a simple purple shape with a thin mouth and little inventor identity. Neither still frame shows a relationship or emotional story beat. |
| Fun, controls, and moment-to-moment play | **6** | These are static camera views. The player's jelly-like movement report remains unaddressed by human play evidence; a face mesh edit cannot raise movement or fun scores. |
| Overall quality | **8** | Rounded mean of 12, 7, and 6. This narrow adjustment does not change the rough environment, character silhouette, movement feel, or unproven adventure loop. |

The candidate is `Assets/SpaceAndCrafts/Resources/Characters/Splat_Rigged_Candidate_Iter263.blend`; the Iteration 251 source remains intact. Unity batch Play Mode imported and rendered both views: `Logs/score-comparison/splat-current-front-iter263.png` and `arrival-gameplay-rear-iter263.png`; logs: `Logs/arrival-capture-iter263-face-front.log` and `arrival-capture-iter263-face-follow.log`. No human play, formal tests, or Meshy credits were used. The blank gray first capture from a `-nographics` run was discarded and replaced by normal-renderer batch captures.

## Iteration 261 — current reassessment after a speed-responsive jump pose pass (2026-10-18)

The primary scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. The pose code now responds to Splat's vertical velocity instead of holding the same broad flipper angle throughout the airborne state. Batch captures from a scripted jump impulse show a tucked launch pose, open apex pose, and extended descent pose. This is better staged than a single static jump silhouette, but does not prove natural timing or control feel. The user-reported jelly-like movement remains consistent with the still-limited motion evidence.

| Required category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **12** | The Iteration 260 field-note HUD makes the objective and optional-task cards fit the Nordic direction more closely. The actual follow-camera scene still has the broad fractured shelf, harsh shoreline edge, crowded gateway and prop clusters; the character's weak silhouette remains prominent. |
| Story, characters, and emotional appeal | **7** | Splat remains a purple skinned form with ringed eyes, a thin smile, and a simple fused silhouette. The pose arcs add movement staging, but not the requested cute, artistic, teen-inventor personality or emotional story. |
| Fun, controls, and moment-to-moment play | **6** | The motor's configured jump impulse now drives three differently staged static frames. They are individually rendered after a scripted physics launch, not a captured playthrough. The player's jelly-like-motion report remains the only direct feel feedback; no full explore/build/use/boss human playthrough is available. |
| Overall quality | **8** | Rounded mean of 12, 7, and 6. UI and jump-pose presentation have improved in narrow areas without changing the weak character, rough world composition, or unverified moment-to-moment feel. |

Diagnostics remain world art **11**, UI **12**, Splat **7**, animation/motion **5**, controls/game feel **6**, traversal **8**, story/loop **8**. The updated images are `Logs/score-comparison/splat-jump-ascent-iter261.png`, `splat-jump-apex-iter261.png`, and `splat-jump-descent-iter261.png`. Each capture was rendered through Unity batch Play Mode with the motor's launch impulse. No human play or formal tests were run; no Meshy credits were used.

## Iteration 260 — current candid reassessment after the Arrival HUD pass (2026-10-18)

The user's criticism remains accurate: all four primary categories are below 30/100. Scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, and Overall **8**. The only scored sub-area change is UI **11→12**: the primary objective and optional task now share authored Nordic corner artwork with the collectible ribbon, and current Unity front/follow captures show that the copy remains legible. This creates a little more identity, but does not make the entire overlay feel fully art-directed.

| Required category | Score | Current evidence and reason |
|---|---:|---|
| Graphics and visual polish | **12** | The objective frame now supports the Nordic direction. The follow-camera image still shows a broad cracked platform, abrupt water edges, scattered colorful and gray prop clusters, and a crowded, layered gateway. The character's rear view remains weak. A small HUD improvement cannot lift the full-scene score. |
| Story, characters, and emotional appeal | **7** | The objective names Vesper and the three parts, but the character remains a simple purple bulb with ringed eyes, broad fused side lobes, small feet, and little explorer identity. The current captures show no relationship or emotional story beat. |
| Fun, controls, and moment-to-moment play | **6** | The creator's jelly-like motion report remains unchallenged. The latest pose tests are forced stills; no human playthrough verifies character weight, acceleration, jump arc, landing, or the full build/use/boss loop. |
| Overall quality | **8** | The rounded mean of the three main scores. Nordic UI framing is a small gain; the world frame, main character, motion feel, and loop remain far from the Odyssey benchmark. |

Diagnostics: world art **11**, UI **12**, Splat **7**, animation/motion **5**, controls/game feel **6**, traversal **8**, story/loop **8**. UI is the only diagnostic changed in this iteration. The evidence does not justify raising any primary score.

The front and gameplay captures are `Logs/score-comparison/splat-current-front-iter260.png` and `arrival-gameplay-rear-iter260.png`; batch render logs are `Logs/arrival-capture-iter260-ui-front.log` and `Logs/arrival-capture-iter260-ui-gameplay.log`. They show a front pose and normal follow view, not a playthrough. Meshy usage and monthly allowance remain unverified, so no credits were spent.

## Iteration 259 — candid current-build reassessment (2026-10-18)

The creator's assessment is well supported: **every required category remains below 30/100**. Current scores are Graphics **12**, Story/Characters **7**, Fun/Controls **6**, and Overall **8** (rounded mean). These are severe scores against the explicitly ambitious Odyssey polish benchmark, based on the current build and limited evidence; they are not a claim about the game's potential. Splat's 7 also matches the creator's direct rating.

| Required category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **12** | The actual Unity Arrival frames have a recognizable Nordic twilight/fjord palette and distant mountain backdrop. The foreground still reads as a broad cracked slab, with abrupt shoreline/material transitions, crowded gate props, and a layered generic HUD. The main character is visibly unfinished and weakens the whole composition. |
| Story, characters, and emotional appeal | **7** | Splat is a purple, fused skinned form, but the face, body, and silhouette still do not communicate a charming teen inventor or a memorable personality. The objective says Vesper and names parts; a screen capture shows no meaningful relationship, emotional beat, or reason to care about the destination. This aligns with the creator's 7/100 Splat assessment. |
| Fun, controls, and moment-to-moment play | **6** | The creator reports jelly-like movement. Unity can bind the visible skin to the action rig, but the current forced Idle and Jump captures remain close in silhouette; stills cannot show natural timing, weight, acceleration, landing, camera behavior, or responsiveness. No human playthrough of the craft-to-boss loop was available for this assessment. |
| Overall quality | **8** | Rounded mean of 12, 7, and 6. The palette and readable parts objective are limited strengths, but they do not offset the weak character, rough composition/UI, reported motion feel, and unverified loop. |

Diagnostics: world art **11**, UI **11**, Splat **7**, animation/motion **5**, controls/game feel **6**, traversal **8**, story/loop **8**. These are review estimates, not measured metrics. Fall protection exists in the inspected code, but safe recovery has not been played through. Static captures and source inspection are not player validation.

Current evidence is in `Logs/score-comparison/splat-current-front-iter251.png`, `arrival-gameplay-rear-iter251.png`, `splat-current-idle-pose-iter259.png`, and `splat-current-jump-pose-iter259.png`; corresponding batch-render records are `Logs/arrival-capture-iter259-idle-pose-review.log` and `Logs/arrival-capture-iter259-jump-pose-review.log`. The comparison board places these actual Unity captures next to official Odyssey promotional images. They are unmatched in-game/marketing views and should be treated as a polish/communication comparison, not a controlled screenshot benchmark. No human play or Meshy generation was done; Meshy usage/allowance remains unverified.

## Iteration 251 — stricter assessment after the HUD and Splat face captures (2026-10-18)

The creator's assessment holds: **none of the required categories earns 30/100**. Scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8** (rounded mean of 12, 7, and 6). The current render supports these low scores. The world has a coherent Norwegian-fjord color direction, but the player stands on a wide, visibly fractured slab above hard water seams and faces an overbuilt gate surrounded by scattered props. Composition and materials still look assembled from systems rather than directed as one scene. Iteration 251 reduces the front eye rims and lowers the eyes; the front is a little friendlier, but Splat still has a thin smile and empty torso, and a pale eye surface remains visible above the crown from the follow camera. The user reports jelly-like movement; rigging and posed mesh deformation do not show natural timing or good feel.

The 1600×900 front Unity capture `Logs/score-comparison/splat-current-front-iter251.png` and follow-camera capture `arrival-gameplay-rear-iter251.png` show the updated objective: it says “Vesper's Signal” and uses brighter spring, dish, and crystal icons with inventory-based dimming instead of generic progress dots. This improves what the marks mean, but the frame still has multiple dark cards, thin cyan borders, generic typography, a side-quest list and a bottom control strip. UI remains **11/100**; one label and icon row do not transform its visual language. The UI score is a diagnostic within graphics, not an additional required category.

| Required Odyssey category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **12** | Actual front and rear Arrival renders show a recognizable twilight fjord palette, yet also a broad cracked shelf, abrupt water/material boundaries, busy gateway and generic overlay. Individual attractive colors do not make the composition feel finished. |
| Story, characters, and emotional appeal | **7** | Splat still looks like a generic purple creature with attached oversized eyes and minimal expression. The task names Vesper but shows no relationship, personality beat, or reason to care about reaching them. This matches the creator's direct 7/100 character rating. |
| Fun, controls, and moment-to-moment play | **6** | The creator reports jelly-like motion. Captures verify a skinned visible rig and changed forced poses, but do not establish natural movement or responsive controls. The core craft-to-boss loop was not played in this review. |
| Overall quality | **8** | Rounded mean of 12, 7, and 6. The interface now communicates a specific destination and component set, but this small gain does not outweigh the weak character, composition, motion feel, and unproven loop. |

Diagnostic ratings: World art **11**, UI **11**, Splat **7**, animation/motion **5**, controls/game feel **6**, traversal **8**, story/loop **8**. All remain below 30. Traversal 8 reflects a route that is not yet an authored platforming sequence; source inspection found a 520×520 fall trigger and checkpoint-relative rescue, but no fall was played through. These are critical estimates against the stated Odyssey polish benchmark, not a calibrated percentage of the game's potential or standalone value.

Unity batch logs `Logs/arrival-capture-iter249-ui-icon-scale.log`, `Logs/arrival-capture-iter249-ui-icons-gameplay.log`, `Logs/arrival-capture-iter251-splat-face-front.log`, and `Logs/arrival-capture-iter251-splat-face-rear.log` show successful compilation and actual front/follow-camera Arrival renders. Existing obsolete API and unused-field warnings remain. No human play or tests were run. No Meshy credits were spent because account monthly usage and allowance are still not verifiable. Odyssey references on the comparison board are official promotional images and do not share a gameplay camera with the Unity captures.

## Iteration 246 — assess current build and verify visible Splat pose binding (2026-10-18)

The user's assessment is fair: no category is above 30. The current Arrival build remains Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. These scores compare the actual 1600×900 Unity captures and direct player feedback with the goal's Odyssey polish target; they do not measure the game's worth as a prototype.

| Required Odyssey category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **12** | The fjord palette, painted twilight, and teal water give Arrival a recognizable Nordic direction. The actual follow-camera capture still shows a broad fractured shelf, hard water seams, a congested gateway, scattered props, and generic label-card HUD. Splat's one-piece body is a small improvement, but the whole frame still reads as a rough prototype. |
| Story, characters, and emotional appeal | **7** | Splat now has a unified soft silhouette, visible flippers, short feet, lavender skin, and a simple smile. The front Unity capture still shows oversized ringed eyes and a plain body with almost no explorer identity; the rear camera exposes the eye pieces above the crown. No character beat or emotional purpose appears in the gameplay frame. This remains aligned with the creator's direct 7/100 character rating. |
| Fun, controls, and moment-to-moment play | **6** | The creator reports jelly-like movement. The opening frame gives gather and movement instructions but shows no crafted invention being used or boss encounter. Idle and forced-jump captures show different limb silhouettes, and the skinned body deforms under the pose rig, but static captures cannot establish grounded timing, responsive control, or enjoyment. |
| Overall quality | **8** | The rounded mean of 12, 7, and 6 is 8. The authored palette and clearer task are real, narrow strengths; the user-reported movement feel, unconvincing mascot design, scene composition, and unproven craft-to-boss loop keep the slice far below the target. |

### Diagnostic breakdown

| Area | Score | Evidence |
|---|---:|---|
| World art | **11** | Cool fjord palette and distant mountains; abrupt fractured floor edges, repeated hard seams, scattered objects, and a crowded gateway dominate the real follow-camera frame. |
| UI | **11** | The objective, side quest, and control hints are readable. Their thin cyan frames, system typography, stacked labels, and dark panels still look generic and pasted over the world. |
| Splat | **7** | One continuous skinned body improves on the old shell and disconnected accessories; the front eye rings remain oversized, the mouth is thin, and the follow-camera view reveals white eye pieces over the crown. |
| Animation / motion | **5** | Source capture confirms the visible renderer is bound to the action skeleton (`visibleSkinBound=True`); the jump pose changes arm-bone rotation and the baked body mesh by up to 0.3764 world units (average vertex delta 0.1275). The two stills show only a modest pose difference, and natural timing has not been observed. The user's jelly-motion report remains the strongest feel evidence. |
| Controls / feel | **6** | Move/jump hints are visible, but there is no continuous play capture or user feedback that contradicts the reported jelly-like control feel. |
| Level design / traversal | **8** | The gateway and collectible route are legible in one frame, but the approach is broad and cluttered. Source inspection confirms checkpoint-relative rescue below the active checkpoint (six-unit emergency margin), plus an Arrival water catch with a trigger; this prevents ordinary falls from becoming deaths in code. No traversal or rescue was played through, so the safeguard's live behavior and the route's fun remain unverified. |
| Story / loop | **8** | The task names three parts; no visible invention build, traversal payoff, memorable character exchange, or boss resolution appears in the current gameplay capture. |

#

## Iteration 246 change and limits

`SplatVisualRepair.PrepareSkinnedCharacter` now explicitly rebinds `SplatSkeletonAnimator` to the visible skinned visual after repair/replacement. The editor capture diagnostic confirms the animator's arm transform is one of the visible skin's bones. The active runtime candidate is `Assets/SpaceAndCrafts/Resources/Characters/Splat_Rigged_Candidate_Iter245.blend`, a unified remesh built from the existing authored rig. It imports as a skinned body with 12,890 Unity vertices and 17 rig bones; the authored source was not overwritten. The candidate removes the diagonal sash and separate boots/pouch, but the rear-camera eye protrusion remains a clear defect. Iterations 242 and 244 were rejected after their face placements looked worse in the preview; Iteration 245 is still a review candidate, not a finished character.

Fall safety was source-checked: Arrival's `ART_FallVolume` spans 520×520 units and is positioned below the water; `PlayerRespawn.LateUpdate` also returns Splat to the active checkpoint whenever he drops more than six units below it, including outside the water trigger. The scenic water rescue adds a splash in Arrival. Every other world scene contains `PlayerRespawn`, even when it does not have an `ART_FallVolume`. This is code-path evidence that routine falls are caught, not an observed fall/recovery playthrough.

Current actual Unity captures: `Logs/score-comparison/splat-current-front-iter246.png`, `arrival-gameplay-rear-iter246.png`, `splat-idle-pose-iter246.png`, and `splat-jump-pose-iter246.png`. The jump pose is forced for a still; it is not continuous gameplay. The comparison board places the current front/rear captures beside official Nintendo Odyssey promotional imagery and separately shows idle/jump. Those images do not share a camera or gameplay context. Official Nintendo art is a visual reference only.

No Meshy credits were spent. Its usage endpoint still returns 403, so the account's monthly allowance and usage cannot be verified; the available GLB has no rig or animation. No tests or human playthrough were run. Unity 6000.5.4f1 batch mode compiled the updated scripts and rendered the four captures; logs are in `Logs/arrival-capture-iter246-final-front.log`, `Logs/arrival-capture-iter246-final-gameplay.log`, `Logs/arrival-capture-iter246-visible-skin-binding.log`, and `Logs/arrival-capture-iter246-idle-pose.log`.

Next high-impact work: resolve the follow-camera eye protrusion, author an expressive teen-inventor face as one coherent design, and capture run/jump/landing/interaction continuously. Recompose Arrival's shelf, water boundaries, props, and gateway, then validate the collect→build→use→boss loop through human play. Keep every category below 30 until player-facing evidence supports a change.

## Iteration 239 — verify Splat GLB skeleton and animation data (2026-10-18)

Re-parsed the local `Assets/SpaceAndCrafts/Art/Commissioned/Hero/Splat_Hero.glb` JSON chunk directly. The file declares one mesh and one node, but has no `skins` or `animations` arrays. It therefore contains no glTF skeletal skin or authored animation. This corrects an earlier mistaken summary that called this GLB rigged and animated. Separately, the Meshy account inventory previously showed one image-to-3D task and no rigging or animation tasks; the usage API remains unavailable, so no credits were spent. This is direct evidence that the available Meshy asset does not solve the production character's rig-and-natural-motion need.

No production files changed and no score changes: Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; diagnostics remain World **11**, UI **11**, Splat **7**, Motion **5**, Controls **6**, Traversal **8**, Story/loop **8**. The current character and scene captures still support the visual scores. Motion and controls remain low based on the creator's direct report, with independent evaluation limited because no continuous human playthrough is available.

## Iteration 238 — remove ambient spring/gear spinners at Arrival spawn (2026-10-18)

Removed the four runtime-generated spring/gear icons from `ArrivalStyleAtmosphere`. They had no objective or traversal role and bobbed by 0.2 world units while rotating, duplicating actual collectibles and adding more animated primitives around Splat's opening vista. The Nordic sky and all existing landmarks, pickups, checkpoints, water, fall catch, and safe-rescue systems stay in place.

Unity 6000.5.4f1 batch Play Mode compiled and saved a new 1600×900 camera frame at `Logs/score-comparison/game-current-splat-feet.png`; the prior frame is archived at `game-current-splat-feet-pre-remove-craft-icons-iter238.png`. The current composition remains crowded with the slab, gate, prop piles, and quest cards. The removed icons are not prominent in this particular camera, so the pair does not establish a meaningful visual score increase. Scores stay Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; diagnostics stay below 30. No tests, human play, or Meshy credits were used. Log: `Logs/arrival-capture-iter238-remove-craft-icons.log`.

## Iteration 237 — eliminate competing antenna writers, 2026-10-18

Source tracing found a concrete source of inconsistent secondary motion: both `SplatSkeletonAnimator.Update()` and `SplatFeelPresenter.Update()` wrote the same `Bone_Antenna.localRotation`. The character animator already owns the idle/run/jump/action antenna pose, while the feel presenter also applied a second jump wiggle and settle. Which one won depended on Unity component update order, causing the action pose to be suppressed or replaced.

Removed the antenna lookup and rotation writes from `SplatFeelPresenter`; `SplatSkeletonAnimator` is now the single owner. Unity 6000.5.4f1 batch compilation completed with exit code 0, no C# errors, existing obsolete-API and unused-field warnings, and the existing Unity MCP shutdown warning. No dynamic motion capture or human play was performed, so this source fix does not justify a motion, controls, or fun score increase. Scores stay Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**, with every diagnostic below 30. Next: capture and review continuous in-game run, jump, landing, and antenna response when human play is available.

## Iteration 236 — Splat art direction study (2026-10-18)

I generated and saved `docs/art/splat-character-concept-iter236.png` as a visual-development reference. It aims for an original soft purple teen alien with a more readable upright explorer silhouette, visibly distinct arms and feet, expressive integrated eyes, and a restrained aqua antenna. It avoids copying a specific mascot and avoids the separate-piece look in both prior Unity candidates. The board shows this study beside the active production Splat and official Odyssey promotional character art.

This is not a game asset: it has not been modeled, rigged, animated, integrated, or viewed in the game. It cannot prove natural movement or raise the scores. The current player-facing assessment stays Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; all diagnostic scores remain below 30. No Meshy credits were used. Next step is a production-ready character with this clearer identity, but Meshy's usage endpoint is inaccessible for this account and the 80% allowance guard cannot presently be verified.

## Iteration 235 — whole-silhouette Splat trial, 2026-10-18

The stable production Splat remains the active character. I rendered a second isolated candidate through the actual Unity Arrival camera and then restored the production FBX, texture, metadata, and canonical gameplay captures byte-for-byte. The trial removes the sash, pouch, and separate shoe shapes, joining body and side forms into one smooth skinned volume. It also uses surface-set eyes, a softened small mouth, blush, toe marks, and a tiny gold star accent. Unity rendered both `Logs/score-comparison/splat-candidate-iter235/unity-front-gameplay.png` and `unity-rear-gameplay.png`.

This is an improvement in *assembly* but not yet in character design. The front Unity frame still reads as a plain purple bean with oversized pale eye rings and a tiny angular mouth. The eyes look applied to the surface rather than painted or naturally integrated; the side arms are broad lobes and barely read as limbs. The rear frame is a generic smooth back with no clear maker/explorer identity; the tiny star is nearly lost at gameplay distance. The removed accessories reduce the pile-of-shapes look, but also remove Splat's only readable role cues. I did not promote this candidate. It remains an archived trial, separate from the stable character.

Scores stay Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; diagnostics stay World **11**, UI **11**, Splat **7**, motion **5**, controls **6**, traversal **8**, story/loop **8**. The trial does not justify raising the character score: it solves a mesh-assembly problem while weakening face integration and identity. No new motion evidence was captured, and the user-reported jelly-like movement remains a serious unresolved issue. The updated comparison board includes the candidate's actual Unity front and rear frames, the stable production character, and official Nintendo promotional references. The promotional art is not matched gameplay. No Meshy credits, human playthrough, or tests were used.

Meshy was checked through its read-only tools. The listed inventory contains one generated asset, “Splat Character 3D,” an image-to-3D task dated 2026-08-28 with GLB output; the task list returned no rigging or animation tasks. The balance tool reports 2,935 monthly credits plus 20 permanent credits and a 2026-11-10 refresh, but the usage endpoint returns 403 because it requires Studio or Enterprise. That balance does not tell us the month's original allowance or how much has already been used. The 80% budget therefore cannot be verified, and no generation, rigging, or animation credits were spent.

Next character pass should start with a distinctive, readable silhouette and an expressive face as one authored design, then add only a few role cues that survive the actual camera distance. The candidate should not be promoted until front, rear, side, run, jump, landing, and interaction views read as the same appealing explorer; natural motion still needs a continuous in-game capture and human review.

## Iteration 234 — strict visual reassessment and character candidate review, 2026-10-18

The latest stable gameplay rear frame is `Logs/score-comparison/game-current-splat-feet.png`; the stable Splat front capture is `Logs/score-comparison/game-current-splat-front-iter212.png`. The Iteration 234 eye/silhouette candidate was captured in Unity at `Logs/score-comparison/splat-candidate-iter234/unity-front-gameplay-after-normals.png`, but it was not promoted. Its eyes render clearly after a surface-winding correction; its smooth bulb body, awkward side bulges, tiny mouth, and star-tipped antenna still fail to form a distinctive, cohesive mascot. The production FBX and metadata were restored from the saved stable backup.

The user's judgment that no category should exceed 30/100 is supported by these player-facing views and the direct report of jelly-like motion. Scores remain **Graphics 12, Story/Characters 7, Fun/Controls 6, Overall 8**. The teal-and-lavender fjord palette creates an initial mood, but the wide cracked slab, sharp floor/water boundaries, prop piles, clustered gateway, and generic dark task cards compete for attention. Splat's purple color and friendly face details are present, but its bulb-like silhouette, prominent rings, awkward sash, and weak rear view do not sell a curious mapmaker. The objective is readable, while the capture shows no crafting payoff, invention use, or boss interaction. The user directly reports poor jelly-like movement; this review has no continuous play footage and cannot independently score responsiveness or animation quality. Scores therefore stay low and unchanged.

| Category | Score / 100 | Assessment |
|---|---:|---|
| Graphics and visual polish | **12** | A coherent color direction is visible, but most of the frame reads as rough geometry, busy prop clusters, repeated gateway pieces, and generic overlays. |
| Story, characters, and emotional appeal | **7** | Splat is recognizable as a purple mascot but not as a memorable authored character; neither the silhouette nor the visible objective creates much personality or emotional stake. |
| Fun, controls, and moment-to-moment play | **6** | The task is legible, yet the user's motion report is negative and screenshots cannot prove responsive controls, fun traversal, or a satisfying gather/build/boss loop. |
| Overall quality | **8** | Rounded mean of the three primary category scores; evidence supports an early prototype, not a polished game. |

Diagnostic values remain World art **11**, UI **11**, Splat **7**, animation/motion **5**, controls/feel **6**, level/traversal **8**, and story/loop **8**. They are all below 30. The visual board places several actual Unity captures beside official Nintendo promotional images and labels those pairs as illustrative, since marketing shots are not matched gameplay frames. No score increases are warranted. No tests, human playthrough, or Meshy credits were used; the Unity candidate capture is a static image.

## Iteration 233 — remove redundant floating route ornaments, 2026-10-18

Source review found that Arrival generated 22 additional floating spring/gear/star crumbs between spawn, the nearest recipe pickup, and the Build Site, then added 10 more procedural gear/spring/star forms along the route shoulders. These decorative forms duplicated the actual pickups and the objective HUD. I removed the crumb spawner and the shoulder-icon placement call; the original pickups, commissioned Meshy landmarks, route, checkpoint and safe-fall systems remain.

Unity batch Play Mode re-imported the restored stable Splat asset, compiled the C# change without compiler errors, and saved a fresh 1600×900 Arrival camera render. The earlier frame `Logs/score-comparison/game-current-splat-feet-pre-breadcrumb-reduction-iter233.png` predates that FBX re-import and shows Splat without the diagonal sash, so it is not a controlled same-character before/after comparison. The new capture `Logs/score-comparison/game-current-splat-feet.png` verifies that the project loads with the stable asset and the deleted path decorations. The large fractured shelf, hard water seams, busy gateway, side clutter, and shell-like character are still prominent. No core score rises: Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. Diagnostics remain World/UI **11**, Splat **7**, Motion **5**, Controls **6**, Traversal/Story-loop **8**. The log retains a licensing handshake error and the known Unity shutdown warning; no C# compiler errors were found. No tests, human playthrough, or Meshy credits were used.

## Iteration 230 — stabilize Splat pose easing, 2026-10-18

Source review found bone and accessory transitions interpolated with speed times deltaTime. At a long frame this factor could exceed one and overshoot the pose target. SplatSkeletonAnimator and SplatFeelPresenter now use exponential interpolation, which remains bounded and frame-rate consistent. The pickup response's map-pouch scale pulse drops from 20/13/18% to 8/5/7%; the pack pulse drops from 11/6/14% to 4/2.5/5%. Main action poses, jump and landing feedback, and the skeleton are preserved.

The latest Iteration 230 Unity batch capture confirms compilation and scene render, but it is a static idle frame and cannot verify the motion fix. Graphics **12**, Story/Characters **7**, Fun/Controls **6**, and Overall **8** remain unchanged; the user-reported jelly-like feel is still unverified in continuous play. Existing StopAssetEditing shutdown assertion remains. No tests, human play, or Meshy credits were used.

## Iteration 229 — unify the objective card treatment, 2026-10-18

The primary objective and optional-goal cards now share the compact painted-fabric panel, thin cyan outline, and a slim priority accent (amber for the primary step, teal for side goals). Removing illustrated corner motifs opens more space around labels. Screen positions and quest logic are unchanged. Same-camera 1600×900 captures: Logs/score-comparison/game-current-splat-feet-pre-ui-restyle-iter229.png and Logs/score-comparison/game-current-splat-feet.png; the latter was captured in Unity batch Play Mode. Text is easier to scan and the cards feel more related, though the system font and screen coverage remain generic.

Graphics rises **11→12** and UI **9→11** from the visible capture; Story/Characters **7**, Fun/Controls **6**, and Overall **8** stay unchanged. No category exceeds 30. The frame still shows the noisy slab, scattered clutter, busy gate, and shell-like Splat. No human play, tests, or Meshy credits; the existing StopAssetEditing shutdown assertion remains.

## Iteration 228 — strict current-build reassessment, 2026-10-18

This pass revisits the saved Iteration 227 rear gameplay frame and the latest available front-facing Splat capture (Iteration 212) against Nintendo's official Odyssey promotional images. The comparison board places the current Arrival frame beside the official kingdom image and the front-facing Splat capture beside the official Mario & Cappy character image. These are not matched gameplay captures.

| Required Odyssey category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **11** | The teal fjord, mountain layers, and lavender sky create a recognizable mood. The current frame is still dominated by a broad noisy slab, abrupt floor edges, scattered props, harsh shadows, and a congested gateway. |
| Story, characters, and emotional appeal | **7** | Splat remains a smooth egg-like blob with large eyes, a tiny mouth, and a diagonal strap. The opening objective identifies components, but the image cannot establish emotional stakes or memorable character performance. |
| Fun, controls, and moment-to-moment play | **6** | The creator reports jelly-like movement. No recent continuous footage or human play evidence demonstrates grounded motion, responsive jumps, or satisfying traversal. |
| Overall quality | **8** | Mean of 11, 7, and 6. The game has an original Nordic direction, but the captured presentation and reported feel remain very early-stage beside Odyssey. |

Diagnostics: World art **11**, UI **9**, Splat **7**, animation/motion **5**, controls/feel **6**, level/traversal **8**, story/loop **8**. The UI's objective is readable, but its ornamental corners, generic cards, multiple labels, and control strip lack a cohesive designed hierarchy. The lower bank, corrected opening task, and HUD placement are real usability/art improvements; prior scores over-weighted these narrow gains. This is a score reassessment, not a new implementation milestone. No controls, scenes, or assets changed; no tests, Unity render, human play, or Meshy credits were used.

Evidence: Logs/score-comparison/game-current-splat-feet.png (Iteration 227 rear gameplay); Logs/score-comparison/game-current-splat-front-iter212.png (latest saved front capture); side-by-side official-image references are embedded in docs/ODYSSEY_VISUAL_COMPARISON.html. No continuous animation evidence exists, and safe-fall source code does not prove a good traversal experience.

## Iteration 227 — open the near fjord skyline, 2026-10-18

The current panorama already contains snowy ridges, aurora, and a warm horizon, but the gameplay camera saw only a tight band over the near walls. `ArrivalFjordBackdrop` lowered the non-colliding near wall range from 8–18 m to 5–12 m and matched waterfall crest height to the banks. The distant mountain layers, mesh water, safety catch, and playable route collision remain unchanged. The same-camera before/after images are `Logs/score-comparison/game-current-splat-feet-pre-lower-fjord-bank-iter227.png` and `Logs/score-comparison/game-current-splat-feet.png`; the new view shows more of the mountain backdrop on both sides.

Graphics and World art rise **13→14** based on that broad visible landscape change. Story/Characters stays **7**, Fun/Controls **6**, Overall **9**; the rounded core mean is 9 and every category remains below 30. The foreground shelf is cluttered, route slabs are still broad and interrupted, the gateway remains busy, Splat still reads as a shell from behind, and motion was not reviewed continuously. Unity 6000.5.4f1 batch Play Mode compiled and saved the 1600×900 capture without C# errors; the existing `StopAssetEditing` shutdown warning remains. No tests, traversal review, human play, or Meshy credits.

## Iteration 226 — stop off-path pickup and startup checkpoint feedback, 2026-10-18

The parked `ART_MeshyCriticalShowcase` retained active pickup physics. Its imported crystal's extreme scale could make the trigger reach Splat's spawn. `ArrivalMeshyPathDress` now strips the showcase's gameplay components and disables colliders immediately while preserving the visual models. The real route pickup remains available. The capture confirms the opening objective now lists all three parts instead of showing the power crystal as already acquired.

The spawn checkpoint also announced itself and replaced the safe lifted respawn pose with the floor marker. `PlayerRespawn.SetCheckpoint` now considers the floor marker and player-height pose the same checkpoint column when their XZ positions match within 0.35 m and their heights differ by no more than 0.75 m. The before image `game-current-splat-feet-pre-spawn-checkpoint-fix-iter226.png` shows “Checkpoint!”; the after `game-current-splat-feet.png` has no startup toast and lists spring, metal dish, and power crystal. Toasts are already positioned lower-right since Iteration 225.

UI rises **13→14**, and Story/Loop **8→9** for preserving the intended three-part gather step and showing the correct objective. The four main scores stay Graphics **13**, Story/Characters **7**, Fun/Controls **6**, Overall **9**; the latter remains the rounded mean 8.67. The creator's jelly-like movement report remains; no human play or full loop verification was performed. Unity 6000.5.4f1 batch Play Mode compiled and rendered the image without C# compile errors; the existing `StopAssetEditing` warning remains. No Meshy credits were used.

## Iteration 225 — place timed messages at lower-right, 2026-10-18

`GameplayToastHud` moved from upper-center to the lower-right, opposite the control hint. The real capture shows the gateway and sky no longer covered by the message. UI rises **12→13** for this visible focus improvement; all four main scores stay **13 / 7 / 6 / 9**. Iteration 226 removes the redundant checkpoint toast during initial spawn. Captures are `game-current-splat-feet-pre-toast-layout-iter225.png` and `game-current-splat-feet-pre-spawn-checkpoint-fix-iter226.png`. Batch Unity compiled and rendered; no tests, human play, or Meshy credits.

## Iteration 224 — Arrival objective hierarchy, 2026-10-18

`QuestBoardHud` now places optional goals beneath the main field note in the same left column. The previous upper-right panel made both corners compete with the skyline; the new Unity image groups primary and secondary tasks and leaves the upper-right sky open. Before/after 1600×900 captures: `Logs/score-comparison/game-current-splat-feet-pre-sidequest-layout-iter224.png` and `Logs/score-comparison/game-current-splat-feet.png`.

This is a limited UI gain. A timed startup pickup toast still spans the upper center, the base type and panel language remain generic, the route is cluttered, and Splat's rear still reads as a shell. UI rises **11→12**; Graphics stays **13**, Story/Characters **7**, Fun/Controls **6**, and Overall **9**. Other diagnostics remain World art 13, Splat 7, motion 5, controls 7, level/traversal 10, and story/loop 8. The core mean remains 8.67, rounded to 9; every category remains below 30.

Unity 6000.5.4f1 batch Play Mode compiled and rendered the screenshot. The first run with `-nographics` produced a blank image and is not used as evidence; the graphics-enabled rerun produced the reviewed capture and no C# compile errors. `Logs/arrival-capture-iter224-sidequest-layout-render.log` contains the render/save confirmations; the existing Unity `StopAssetEditing` shutdown warning remains. No tests, human play, or Meshy credits were used. The timed pickup toast, Splat's silhouette/motion, and the rough route remain high-impact work.

## Iteration 223 — slow four decorative spawn icons, 2026-10-18

`ArrivalStyleAtmosphere` now rotates its four non-interactive spring/gear icons at 8, 10, 12, and 14 degrees per second, down from 40, 52, 64, and 76. Their positions, shapes, and role as decorative scene cues remain unchanged. This reduces an obvious fast spinner in the opening area, but a single static Unity capture cannot show motion quality. The latest real 1600×900 Arrival camera image is `Logs/score-comparison/game-current-splat-feet.png`, and the baseline is `game-current-splat-feet-pre-slow-icons-iter223.png`; the two static frames do not prove reduced angular speed. Unity batch Play Mode compiled and rendered without C# compiler errors (log `Logs/arrival-capture-iter223-slow-icons.log`); existing `StopAssetEditing` assertion and Unity MCP shutdown warnings remain. No continuous capture, tests, Meshy credits, or human play were used.

The scorecard remains **13 / 7 / 6 / 9** with diagnostics World 13, UI 11, Splat 7, motion 5, controls 7, level 10, story/loop 8. No score rises without player-facing motion evidence. The creator's firsthand report of jelly-like movement remains. The highest-impact remaining work is a cohesive Splat redesign, a continuous beautiful and forgiving route, and direct play review.

## Iteration 222 — soften the Nordic panorama; reject three Splat arm trials, 2026-10-18

Added `ArrivalNordicFjordPanoramaPaintedV1.png` as a preferred resource for both the Arrival skybox and opening-story background, retaining the original as a fallback. The actual 1600×900 Unity capture `Logs/score-comparison/game-current-splat-feet.png` shows a warmer lavender dusk band above the Arrival cliffs. Because the tall route walls hide most of the panorama and the foreground remains unchanged, this creates a small mood variation but not a proven broad graphics improvement; Graphics and World art stay **13**, and the core scores stay **13 / 7 / 6 / 9**. The paired baseline is `game-current-splat-feet-pre-painted-panorama-iter222.png`.

Three offline, fused-metaball skin candidates explore tapered arms at different heights. Candidate 219 reads as lower side knobs, candidate 220 as oversized horizontal fins, and candidate 221 as small bumps still concealed by the sash. All three were left out of the live model; the stable character remains 7/100. The trials use one continuous body mesh with a real skeleton, but Blender stills do not establish Unity skin deformation or natural motion. Preview folders are `Logs/score-comparison/splat-candidate-iter219/`, `iter220/`, and `iter221/`. The repeated shape approach did not solve the character-read problem and should not be promoted. Meshy generation remains withheld because the account's monthly allowance and current-month use are unverifiable; no credits were spent.

Unity 6000.5.4f1 batch Play Mode saved the current Arrival camera capture without C# compile errors; existing `StopAssetEditing` and Unity MCP shutdown warnings remain. The comparison board now contains a controlled Iteration 218/222 sky pair. No tests or human play were run. Every score remains under 30.

## Iteration 218 — deepen the fjord water palette, 2026-10-18

Arrival previously rendered its broad catch-water surface in bright cyan-blue horizontal bands. The original `ArrivalFjordWaterCalmV2.png` image creates broad, painterly arctic-teal currents with subdued highlights. `ArrivalFjordBackdrop` now prefers this texture, applies a cooler-neutral tint and lower smoothness, and falls back to the original texture if the new resource is absent. Its scene capture shows a deeper, calmer surface that fits the Norway-inspired setting better. The water and catch meshes remain non-colliding scenic/recovery geometry; the existing fall and checkpoint systems were not changed.

The 1600×900 pre-change capture `Logs/score-comparison/game-current-splat-feet-pre-calm-water-iter218.png` is paired with the new capture `Logs/score-comparison/game-current-splat-feet.png` in the visual comparison board. Props are in slightly different idle phases, but the large water region shows the material change clearly. Unity 6000.5.4f1 batch Play Mode compiled the new C# and rendered the actual Arrival camera with no C# compiler errors; the existing `StopAssetEditing` assertion and Unity MCP shutdown warnings remain. This verifies appearance in one camera image, not tiling quality at all distances, water movement quality, or a playthrough. No tests, human play, or Meshy credits were used.

Graphics and World art rise **12→13** for the visible improvement across the fjord surface. The broad hard-edged route, repeated debris, crowded gate, basic UI, and disconnected character silhouette remain. Story/Characters stays **7**, Fun/Controls **6**, and Overall rises **8→9** (rounded mean of 13, 7, and 6). No category exceeds 30. Splat and motion still require cohesive authored improvement and continuous gameplay review. Meshy generation remains withheld because the usage API did not expose the monthly allowance or current-month usage; no credits were spent.

## Iteration 217 — bring the illustrated Arrival frame back to the persistent notes, 2026-10-18

The objective and optional-journal panels now reuse the project's illustrated Nordic HUD frame. Their content was pushed inward to clear the four decorative corners; the objective's recipe pips were moved inward as well, and the optional journal widened modestly to retain useful text width. A new 1600×900 Unity batch Play Mode image, `Logs/score-comparison/game-current-splat-feet.png`, shows the clear insets and the frame accents. The plain-center prior capture is `game-current-splat-feet-pre-field-note-frame-iter217.png`; both are included in the visual comparison board.

The screenshot shows a small improvement in material identity and breathing room, while the text hierarchy, number of labels, panel boxes, and overall world remain generic. UI rises **10→11**. Graphics stays **12** because this narrow pass does not offset the broad rough terrain, crowded gate, clutter, and harsh environment. Story/Characters remains **7**, Fun/Controls **6**, and Overall **8**. No core score rises and no category exceeds 30. Unity batch capture saved the image without a C# compiler error; existing shutdown warnings remain. No tests, human play, or Meshy credits were used.

## Iteration 216 — ease the darkest Arrival shadows, 2026-10-18

The Arrival key light's serialized shadow strength is now 0.72, down from 1.0. A fresh same-camera Unity render `Logs/score-comparison/game-current-splat-feet.png` shows lighter shadow patches around the gateway and on the slate. Its before image, `game-current-splat-feet-pre-shadow-tune-iter216.png`, already uses Iteration 215's full-character framing, so this pair isolates the lighting pass more closely; moving props may still be at different idle phases. The directional shadow edges remain crisp, and the reduced strength is a small presentation adjustment rather than a scene redesign.

Graphics and World art remain **12** because the effect is limited: the flat disconnected platform forms, clutter, hard edges, and gate still dominate the screenshot. Story/Characters stays **7**, Fun/Controls **6**, and Overall **8**. The full scores remain Graphics 12 / Story-Characters 7 / Fun-Controls 6 / Overall 8, with diagnostics World 12 / UI 10 / Splat 7 / Animation-Motion 5 / Controls-Feel 7 / Level-Traversal 10 / Story-Loop 8; no category exceeds 30. Unity 6000.5.4f1 batch Play Mode confirms the image rendered and saved, with no C# compiler errors; the pre-existing `StopAssetEditing` and Unity MCP shutdown warnings remain. No tests or human play were run.

Meshy read-only queries show a 2,935 monthly / 20 permanent balance. The Usage endpoint again returns 403 for this account, so allowance and current-month usage are unverified. Inventory shows one unrigged Splat image-to-3D model and no rigging or animation tasks/models. No credits were used; generating remains off-limits until the 80% monthly-use cap can be checked.

## Iteration 215 — frame Splat fully in the Arrival gameplay view, 2026-10-18

The latest follow-camera capture cropped the bottom of Splat's shoes and ground ring. Lowering `CameraSettings.asset`'s `FocusHeight` from 0.78 m to 0.50 m shifts the follow-camera aim down and brings both details into the 1600×900 Unity gameplay capture. Before and after images are saved as `Logs/score-comparison/game-current-splat-feet-pre-camera-framing-iter215.png` and `Logs/score-comparison/game-current-splat-feet.png`; they are also paired in the visual comparison board. Moving props can occupy different idle phases between independent batch captures.

This is a narrow framing improvement. The rear silhouette still reads as a glossy shell with a strap, and the full scene retains its broad interrupted platform layout, scattered props, hard shadows, crowded gateway, and generic UI. The creator's direct jelly-like movement report remains the strongest motion/feel evidence. Core scores stay **Graphics 12, Story/Characters 7, Fun/Controls 6, Overall 8**; diagnostics remain World art 12, UI 10, Splat 7, Animation/Motion 5, Controls/Feel 7, Level/Traversal 10, and Story/Loop 8. No category exceeds 30.

Unity 6000.5.4f1 batch Play Mode confirms the new camera frame rendered and the screenshot was saved. No C# compiler errors appear; the existing shutdown warning remains. No tests, human play, or Meshy use were performed.

Meshy read-only checks return a balance of 2,935 monthly credits and 20 permanent credits, with the monthly balance refreshing 2026-11-10. The Usage API returns 403 because the account key is not on a Studio or Enterprise team; therefore the monthly allowance and prior usage cannot be verified. Inventory contains one unrigged Splat image-to-3D model, with no rigging or animation tasks/models. No credits were spent. Per the 80% monthly-use limit, do not generate until usage and allowance can be verified.

## Iteration 214 — reuse the Nordic HUD texture, 2026-10-18

`RuntimeUiFactory.ApplyCompactHudFrame` previously filled compact cards with a flat near-black color. It now crops the quiet fabric center from the existing `arrival-hud-frame` texture and uses that as the background for objective, optional-journal, toast, interaction, and compact inventory cards. The ornate corners remain excluded from the panel sprite, preserving the clear text insets introduced earlier.

Unity rendered a fresh same-camera frame to `Logs/score-comparison/game-current-splat-feet.png`; the previous frame is `game-current-splat-feet-pre-textured-hud.png`. The toast text differs between captures, but the HUD card treatment is visible: the new fabric grain adds a small authored material cue without changing text readability. UI rises **9→10**. Core scores remain **Graphics 12, Story/Characters 7, Fun/Controls 6, Overall 8**; the full HUD still has too many generic labels and basic typography. No C# compiler errors appeared in the Unity log; the existing shutdown warning remains. No tests, human play, or Meshy credits were used.

## Iteration 213 — unify the spur platforms with Nordic slate, 2026-10-18

Arrival's two large side platforms, `ART_SpurGround`, were only muted to a flat gray while the main shelf had a fractured slate texture. `ArrivalMeshyPathDress` now applies the existing `ArrivalShelfSlatePainted` material and top-down mesh UVs to each spur, with a slightly darker tint to keep it visually distinct. A new Unity capture, `Logs/score-comparison/game-current-splat-feet.png`, shows the platforms now visually relate to the main floor. Their broad disc silhouette and detached placement remain obvious, so this is a small cohesion gain rather than a finished environment.

Graphics and World art rise **11→12** based on that visible in-game material change. Story/Characters **7**, Fun/Controls **6**, and Overall **8** stay fixed; the rounded mean is still 8. UI remains 9, Splat 7, animation/motion 5, controls/feel 7, level/traversal 10, and story/loop 8. No other category rises. The latest capture has no C# compiler errors in its Unity log; the existing `StopAssetEditing` shutdown warning remains. No test suite or human play was performed, and no Meshy credits were used.

## Iteration 212 — quieter world and invention motion, 2026-10-18

Source inspection found that invention idles still bobbed by 8 cm and spun at 22°/s, with an activation multiplier that could push the spin above 70°/s. Arrival collectibles bobbed 5.5 cm and spun at 20°/s; ambient scrap bobbed 3.5 cm. Iteration 212 reduced these to 2 cm / 5°/s for inventions, 2.5 cm / 10°/s for pickups, and 1.5 cm / 2.5°/s for scrap, with a gentler invention activation pulse. Serialized Arrival values and all ten invention prefabs were updated with the shared defaults.

A new Unity batch capture, `Logs/score-comparison/game-current-splat-feet.png`, confirms the Arrival scene and HUD still render. A single frame cannot validate movement, and the creator's report that objects and Splat feel jelly-like remains stronger evidence than this code change. No continuous capture or human play was performed, so animation/motion stays **5**, controls/feel stays **7**, and the core scores remain **Graphics 11, Story/Characters 7, Fun/Controls 6, Overall 8**. The same current rear capture shows Splat's purple shell, diagonal teal strap, starbud, and feet; the front Unity capture `game-current-splat-front-iter212.png` shows very large white-ringed eyes and a small mouth. The 7/100 Splat assessment remains appropriate.

No C# compiler errors appeared in the Unity log; its existing `StopAssetEditing` shutdown warning remains. No test suite or human play was run. No Meshy credits were spent. Highest-impact remaining work: author Splat as a cohesive original character with natural motion, redesign the HUD as a system, and turn the broad rough shelf and disconnected platforms into a safe, visually continuous route.

## Iteration 211 — clearer HUD copy; character candidate rejected, 2026-10-18

The Arrival objective and optional journal now use the compact dark frame without the illustrated corner sprites. Their latest same-camera 1600×900 Unity capture, `Logs/score-comparison/game-current-splat-feet.png`, shows the objective and quest copy clear of the decorative artwork that previously pressed against the first letters. The preceding image is preserved at `Logs/score-comparison/game-current-iter209-score-review.png`. The panels remain generic and the overall UI still needs art direction, but this is a confirmed clarity improvement. UI diagnostic rises **7→9**; the four core scores remain **Graphics 11, Story/Characters 7, Fun/Controls 6, Overall 8**. The mean remains 8 and no current category exceeds 30.

The same iteration's Blender Splat test enlarged the fused side flippers and opened the smile. The render made the side lobes read as round knobs and the eyes/mouth still felt uncanny. It was rejected before Unity import; the stable source and FBX were restored. The rejected Blender image is preserved at `Logs/score-comparison/splat-shape-candidate-rejected-iter211.png`. Splat remains **7/100** and no character or motion score rises. Unity batch mode rendered the HUD change and reported no C# compiler errors; the pre-existing `StopAssetEditing` shutdown warning remains. No human play or test suite was run. No Meshy credits were used.

## Iteration 210 — strict reassessment, 2026-10-18

After reviewing the latest actual Unity follow-camera image and applying the creator's firsthand movement feedback, the Iteration 209 assessment was too generous. The current scores are **Graphics 11, Story/Characters 7, Fun/Controls 6, Overall 8**. Diagnostics: World art 11, UI 7, Splat 7, animation/motion 5, controls/feel 7, level design 10, story/loop 8. No current category exceeds 30.

The 1600×900 Unity capture `Logs/score-comparison/game-current-splat-feet.png` shows a wide, rough shelf; disjoint gray platforms; scattered props; hard shadows; a crowded gateway; and HUD decorations that intrude into objective text. Its Nordic slate material and fjord background are real strengths, but only limited ones. In the rear follow view, Splat is a smooth purple egg with a separate-looking pack and feet; its face and personality are not visible. The creator's report that movement looks jelly-like is direct play evidence and is weighted more heavily than unreviewed source tuning or Blender pose stills.

The former 14/100 UI rating mainly credited larger type and missed the full-frame readability problems. The former motion 8/100 similarly gave too much credit to code without seeing play. Iterations 205–209 document actual changes, but those changes do not support optimistic quality scores. Historical high scores earlier in this file were superseded and should not be read as current.

The Odyssey comparison uses Nintendo promotional images rather than matched in-game frames; it helps compare destination art direction and character clarity but is not a controlled visual benchmark. Scores remain judgment estimates.

## Iteration 209 — floating pickup shadows, 2026-10-18

Arrival's active collectible renderers now stop casting and receiving shadows, so their floating motion cannot create moving dark cutouts on the ground. The player, gateway, shelf stones, and environment retain their shadows. A fresh full Unity camera render `Logs/score-comparison/game-current-splat-feet.png` still shows strong shadows from the gate and other landmarks and does not establish a visible gain for this particular change. Core scores remain **Graphics 15, Story/Characters 11, Fun/Controls 10, Overall 12**; diagnostic scores remain World art 15, UI 14, Splat 7, animation/motion 8, controls/feel 10, level design 13, story/loop 12. Every score remains below 30. Unity reports no C# compiler errors; its existing `StopAssetEditing` warning persists. No tests or Meshy credits used.

## Iteration 208 — Nordic shelf material, 2026-10-18

Added an original painterly slate albedo texture to the playable Arrival shelf. The generated tile is stored in `Assets/SpaceAndCrafts/Resources/Environment/ArrivalShelfSlatePainted.png`; `ArrivalMeshyPathDress` applies it only to the shelf. The shared organic slab had no UV0, so the first render showed only a flat tint. A per-floor XZ projection and mirrored texture wrapping fixed that. The Unity camera now shows cool, irregular slate blocks across the foreground where the earlier capture showed a flat blue-gray shelf. This adds a Nordic surface language and roots the approach toward the fjord walls, while the noisy gateway, strong shadows, repeated props, and clutter remain.

The player-facing scores are now **Graphics 15, Story/Characters 11, Fun/Controls 10, Overall 12**; the rounded core mean is 12. Diagnostics: World art 15, UI 14, Splat 7, animation/motion 8, controls/feel 10, level design 13, story/loop 12. Every score remains under 30. Graphics and World art rise one point for a visible ground-material change; the game remains far below Odyssey quality and the color/tile still needs human art review. New 1600×900 Unity batch capture: `Logs/score-comparison/game-current-splat-feet.png`. Compared with the same-camera earlier capture `game-current-ui-frame-iter198.png`, the route surface now reads as fractured stone; the HUD labels also differ in scale, so this is illustrative evidence, not a controlled comparison. Unity reports no C# compiler errors; existing `StopAssetEditing` shutdown warning persists. No tests, Meshy credits, or human play were used.

Next high-impact gaps remain Splat's full design, a bespoke UI system, clutter reduction, better shadow/material cohesion, and a continuous player movement review.

## Iteration 207 — HUD type scale, 2026-10-18

Raised the text sizes in Arrival's objective card, optional quest card, and control strip. A new 1600×900 Unity Play Mode frame `Logs/score-comparison/game-current-splat-feet.png` shows all three still fit; goal and side-quest instructions are easier to read at a glance. The UI diagnostic moves from **13 to 14** for this small legibility gain. The core scores stay **Graphics 14, Story/Characters 11, Fun/Controls 10, Overall 12**; every score remains below 30. The board still looks like a generic collection of panels, so this is not a full interface redesign. No narrow-screen, controls, or human review was performed. Unity reports no C# compiler errors and retains its existing `StopAssetEditing` shutdown warning. No tests or Meshy credits used.

## Iteration 206 — Splat silhouette candidate rejected, 2026-10-18

The candidate raised Splat's eyes and broadened the fused crown while adding lateral flipper lobes. A head-on Unity capture showed the raised whites narrowing into pinched shapes and the lobes reading as separate side knobs, weakening the friendly mascot direction. The candidate was rejected, the authored Blender source restored to its earlier fused body/eye layout, and a restored Unity frame was rendered. The rejected view remains available as `Logs/score-comparison/splat-shape-candidate-rejected-iter206.png`; the restored current face is `Logs/score-comparison/game-current-splat-front-iter206.png`.

No required score changes: **Graphics 14, Story/Characters 11, Fun/Controls 10, Overall 12**. Diagnostic scores remain World 14, UI 13, Splat 7, animation/motion 8, controls/feel 10, level design 13, and story/loop 12; none exceeds 30. The rejected attempt does not qualify as improvement. Unity 6000.5.4f1 batch Play Mode rendered the restored image without C# compiler errors; the existing asset-editing shutdown warning remains. No tests, Meshy credits, or human play were used. Next character work needs a cohesive full-body design and gameplay-scale evaluation, not another small placement tweak.

## Iteration 205 — restrained character motion, 2026-10-18

The user gave a direct assessment that Splat looks like loosely assembled shapes, the HUD looks like transparent labels, and the movement feels jelly-like. The assessment remains **Graphics 14, Story/Characters 11, Fun/Controls 10, Overall 12**; Splat stays **7/100**. These scores reflect the actual Unity frames and the creator's firsthand feel report. None exceeds 30. The earlier smaller UI and facial passes should not be read as evidence that the game has escaped rough-prototype quality.

This source pass reduced Splat's idle and running bob, run swing, jump and action antenna oscillations, craft spin, discovery pulses, and celebration bounce. It makes the procedural pose driver less twitchy on paper, but no new rendered motion capture or human play was performed, so **animation/motion remains 8** and **controls/feel remains 10**. The current side-by-side board still uses Iteration 204 Unity captures because those are the latest real game frames; they show the weak silhouette, HUD hierarchy, and world composition, but cannot show the Iteration 205 code change. No test or Meshy credit was used.

Highest-impact gaps remain: rebuild Splat as one distinctive, cohesive cute character with natural authored movement; redesign the HUD hierarchy; give the gameplay route a finished visual language; and review player motion in a continuous session before changing feel scores.

## Iteration 204 — shallower eye geometry, 2026-10-18

The required scores stay **Graphics 14, Story/Characters 11, Fun/Controls 10, Overall 12**. The Unity front capture `Logs/score-comparison/game-current-splat-front-iter204.png` shows the current face after shortening the eye-white, iris, pupil, and glint depths. The eyes remain rigged for blinking, and painted blush from Iteration 203 remains visible. The change is modest; the white ovals still dominate the face, and the character's plain silhouette and oversized forehead remain. Splat stays **7/100**.

The latest normal rear capture `Logs/score-comparison/game-current-splat-feet.png` confirms the map pack and shoes remain legible. The face is not visible from that camera, and the wide, cluttered Arrival path is unchanged. Unity 6000.5.4f1 batch Play Mode imported and rendered front/rear frames without C# compiler errors; its existing MCP shutdown warning remains. No score rises, no tests or Meshy credits were used, and no human playthrough occurred. The [comparison board](ODYSSEY_VISUAL_COMPARISON.html) contains the current rear and face views beside official Nintendo images. Highest-impact work remains a distinctive, cohesive character, naturally weighted Unity animation, and a clearer safe route.

## Iteration 203 — painted cheek color, 2026-10-18

The four-category scores stay **Graphics 14, Story/Characters 11, Fun/Controls 10, Overall 12**; all diagnostic values remain below 30. A front Unity capture `Logs/score-comparison/game-current-splat-front-iter203.png` confirmed that blush painted into the seamless body albedo survives FBX import. Removing the raised cheek patches makes the face slightly more integrated. The rest of Splat still reads as a simple egg with protruding eyes, so the creator's **7/100** rating and story/character 11 remain. The latest actual scene still shows a flat shelf, crowded gate approach, and opaque HUD.

Blender regenerated the texture and 17-bone source; Unity rendered the front face with no C# compiler errors. This is a detail-level visual improvement, not a change to movement or story. No tests, Meshy credits, or human play were used. The next character pass reduced the eye projection; see Iteration 204 below.

## Iteration 202 — prop and enemy motion reduction, 2026-10-18

Scores remain **Graphics 14, Story/Characters 11, Fun/Controls 10, Overall 12**. Source inspection found 22 cm of collectible bob paired with 85°/s spin, 18 cm scrap-prop bob, and startled enemy bob multiplied by 1.6. That provided a concrete match for the creator's weightless/jelly-like motion report. `PickupMotion` was reduced to 5.5 cm bob, 1.35 speed and 20°/s spin in its defaults and all 36 serialized scene overrides. `TempFloatBob` was reduced to 3.5 cm bob, 0.65 speed and 5°/s spin in its defaults and the six Arrival overrides. `ScrapMite` bob was reduced from 8 cm to 2.5 cm in scene data, its default lowered, and its fast doubled bob was replaced by slower, modestly stronger startled movement.

Unity batch Play Mode rendered the updated rear view `Logs/score-comparison/game-current-splat-feet.png` and front view `Logs/score-comparison/game-current-splat-front-iter202.png`. The frames preserve pickup readability, the map pack, and shoes. A static image cannot confirm the motion change or player feel; no score rises, and no human playthrough was performed. Unity 6000.5.4f1 reported no C# compiler errors; its existing MCP shutdown warning remains. No tests or Meshy credits were used. See the [visual comparison board](ODYSSEY_VISUAL_COMPARISON.html), which now contains the latest game frame and front-facing Splat pair beside official Mario & Cappy art.

## Iteration 201 — Splat face redesign, 2026-10-18

The required scores remain **Graphics 14, Story/Characters 11, Fun/Controls 10, Overall 12**; every category remains below 30. The latest Unity front capture `Logs/score-comparison/game-current-splat-front-iter201.png` shows a clearer, calmer face: dark brows are gone, the mouth is a small closed smile, eye whites and irises are slightly larger, cheek color is flush with the surface, and the front chest token is removed. This is a real improvement over the Iteration 200 close-up, where the frowning brows, black oval mouth, and teal token made Splat look worried and assembled.

The improvement does not resolve the character. Blender and Unity both show a plain egg-like body with a large unmarked forehead; the normal rear Unity capture `Logs/score-comparison/game-current-splat-feet.png` still shows the body, pack and shoes as distinct features on a purple orb. Splat remains **7/100**. The [visual comparison board](ODYSSEY_VISUAL_COMPARISON.html) pairs current Unity views with official Mario & Cappy promotional art. These are illustrative comparisons, not matched camera shots.

`tools/splat/author_splat.py` regenerated the skinned Blender source, 17-bone FBX and preview. Unity 6000.5.4f1 batch Play Mode imported it and rendered both front and rear captures; no C# compiler errors were reported. The existing Unity MCP `StopAssetEditing` shutdown warning remains. No tests, Meshy generation, or human playthrough were performed. The remaining major gaps are cohesive silhouette, distinctive mapmaker identity, grounded motion, camera-friendly facial readability, and the Arrival world's cluttered path.

## Iteration 200 — front-facing Unity character read, 2026-10-18

The scores remain **Graphics 14, Story/Characters 11, Fun/Controls 10, Overall 12**; no category exceeds 30. A new front-facing 1600×900 Unity capture, `Logs/score-comparison/game-current-splat-front-iter200.png`, checks the actual imported Splat face after the eye/cheek/brow color correction. Brighter eye whites improve basic contrast, but the raised face parts still look like pieces placed on an egg-shaped purple body. The dark angled brows read worried, the oval mouth reads as a black cutout, and the teal chest badge looks detached from the character's design. Splat remains **7/100**, consistent with the creator's rating.

This puts visual grounds behind the critique instead of relying on a Blender preview. The normal rear follow-camera capture, `Logs/score-comparison/game-current-splat-iter199.png`, still reads as an orb with accessories and does not show the face. The [comparison board](ODYSSEY_VISUAL_COMPARISON.html) now pairs the Unity front view with Nintendo's official Mario & Cappy promotional image, and retains the broader world and UI comparisons. These are illustrative pairs, not matched camera shots.

The required category rationale remains: **Graphics 14** because the fjord establishes a mood but the playable foreground is flat, hard-edged and cluttered; **Story/Characters 11** because objectives are mostly HUD text and Splat is not yet coherent or expressive; **Fun/Controls 10** because the creator reports jelly-like movement, which is not disproved by a still; **Overall 12**, the rounded mean of those three scores. UI remains 13, world art 14, animation 8, controls/feel 10, level design 13, story loop 12. No human playthrough or continuous animation review was performed for this iteration.

## Iteration 199 — Splat silhouette and expression, 2026-10-18

The required scores remain **Graphics 14, Story/Characters 11, Fun/Controls 10, Overall 12**. A reauthored fused Blender body is less spherical, with a tapered crown, broader lower torso, smaller flippers, longer curved antenna, and a small tooth highlight. The new Unity 1600×900 frame `Logs/score-comparison/game-current-splat-iter199.png` shows the candidate from behind: the face is hidden and the body still reads as an egg-like orb with a pack. The change is not enough to raise Splat above **7/100** or the broader story/character score.

Blender regenerated the `.blend`, 17-bone FBX, albedo, and front/back previews; these are authoring evidence, not natural-animation or gameplay evidence. Unity 6000.5.4f1 batch Play Mode imported and rendered the FBX successfully with no C# compiler errors. The still does not establish motion or human feel, and the creator's jelly-like-movement report remains unresolved. The HUD pass from Iteration 198 is present; its diagnostic score remains 13. No tests or human playthrough were performed.

Meshy was rechecked read-only: balance **2,955** (2,935 monthly and 20 permanent), one finished image-to-3D model, and no rigging or animation tasks. Usage again returned **403**, as the endpoint only supports Studio/Enterprise team keys; the 80% monthly cap therefore cannot be verified. No credits were spent. See the [comparison board](ODYSSEY_VISUAL_COMPARISON.html) for the newest capture paired with official Odyssey imagery; those promotional-image pairs are illustrative, not matched gameplay shots.

## Iteration 198 — Arrival HUD hierarchy, 2026-10-18

The core scores remain **Graphics 14, Story/Characters 11, Fun/Controls 10, Overall 12**. The diagnostic UI score moves from 11 to **13/100** after the current Unity capture at `Logs/score-comparison/game-current-ui-frame-iter198.png`: the existing Nordic artwork now appears as corner accents over a readable dark surface, and the redundant one-item inventory ribbon is hidden while objective pips already show progress. This is a narrow improvement, not a polished HUD. The labels still use a generic font, the optional journal is small, and multiple panels plus the center toast remain visible.

The first full-frame slice attempt was discarded when its margins collapsed and left the text over the world. The final version crops the source artwork's corner motifs and layers them over the proven dark panel. Unity 6000.5.4f1 batch Play Mode compiled and rendered successfully with no C# errors; the log retains the existing obsolete API warnings and the Unity MCP `StopAssetEditing` shutdown warning. The render is a still, not a human playthrough; movement, Splat, level composition, and story did not change, so their scores stay put. No tests or Meshy generation were performed.

## Iteration 197 — current-build reassessment, 2026-10-17

The required scores remain **Graphics 14, Story/Characters 11, Fun/Controls 10, Overall 12**. The score is intentionally harsh: in the actual Unity capture `Logs/score-comparison/game-current-splat-iter197.png`, Splat is still an orb-like purple body seen from behind, while a large gate, floating pickups, debris, flat side shelves, and corner HUD panels crowd the view. The 17-bone Blender rig and more expressive front render are authoring evidence only. The game capture does not show the face, and no continuous Unity animation review proves natural motion. The player's jelly-like movement report remains the best feel evidence.

The UI diagnostic score is **11** (revised down from 16): dark fills improve contrast over the earlier sliced-frame failure, but the panels remain generic, cover much of the screen, and rely on small labels. World art stays 14, Splat 7, animation 8, controls/feel 10, level design 13, and story/game loop 12. No category exceeds 30.

The candidate is a Blender-authored, shape-based mesh with a 17-bone rig; it is not a Meshy-generated character and the rig does not itself mean it moves naturally. Meshy remains unused because the monthly allowance and usage could not be verified. A fresh batch Play Mode render succeeded, but no human playthrough or test was performed. See the updated [visual comparison board](ODYSSEY_VISUAL_COMPARISON.html) for side-by-side gameplay frames and official Odyssey promotional images. Those pairs show the gap in composition and finish, but are illustrative rather than matched shots.

## Iteration 195 — route prop reduction, 2026-10-17

The required four-category scores remain **Graphics 14, Story/Characters 11, Fun/Controls 10, Overall 12**. The overall mean is 11.7 rounded to 12. Diagnostic detail remains world art 14, UI 16, Splat 7, animation 8, controls/feel 12, level design 13, and story/game loop 14. No score changed from this local environment edit.

The updated Unity screenshot at `Logs/score-comparison/game-current-iter195-path-trim.png` shows the spawn shelf with one small, off-center authored rock cluster instead of mirrored piles, and only one distant path-side landmark instead of four repeated clones. The first approach reads a little more open. Repeated rubble still collects around the gateway, and the broad gray shelf and side platforms remain weak. Checkpoints, walkable floor, water, and fall recovery were untouched. A trial doubling the slate texture repeats did not visibly improve the actual camera frame, so its original 4×3 scale was restored.

Unity 6000.5.4f1 batch Play Mode compiled the project (Tundra build success, zero C# errors) and rendered the frame; `Logs/arrival-capture-iter195-final.log` contains existing obsolete API warnings and a Unity MCP `StopAssetEditing` shutdown warning. This was a rendered still, not a human playthrough or test. No Meshy credits were used. See the updated [visual comparison board](ODYSSEY_VISUAL_COMPARISON.html).

## Full category reassessment — Iteration 194

The earlier three-category scorecard compressed separate problems and gave the whole build too much credit for the attractive fjord palette and functioning systems. This pass scores the player-facing result by discipline. Scale: 30 is a rough prototype with recognizable direction, 50 a competent but uneven indie release, and 80+ polished commercial quality. All scores are Odyssey-relative estimates, and every category stays under 30.

| Category | Score | Why |
|---|---:|---|
| World art and visual polish | **14** | Unity captures show a cohesive cool fjord palette and a readable gate landmark. Foreground composition undermines it: broad flat shelf, abrupt geometry, repeated rubble piles, distracting hard shadows, and scattered/floating objects. Distant panorama and playable foreground do not feel like one authored scene. |
| UI and information design | **16** | Latest capture has better dark panel contrast and a compact toast. Multiple large boxes remain onscreen, the type is generic and small, and layout competes with play. It conveys information but has little hierarchy or visual craft. |
| Main character — Splat | **7** | This preserves the creator's direct 7/100 assessment. In the rear gameplay frame, Splat reads as a purple sphere with a badge, antenna, and small feet; face and expression are hidden. Add-on shapes do not yet resolve into a cute, expressive, cohesive silhouette. |
| Animation and motion | **8** | The creator reports jelly-like player and object motion. Static captures cannot quantify it, and Blender pose sheets are not a continuous in-game animation review. No current footage establishes weight, foot contact, or controlled secondary motion. |
| Controls and moment-to-moment feel | **12** | Direct player feedback describes loose jelly-like movement. Code and smoke runs establish that input paths and completion work, but do not rebut the report about acceleration, jump/land feel, camera trust, or object response. |
| Level design and traversal | **13** | The gate provides a clear destination and source audits show fall recovery/checkpoints. In the actual frame the route is wide and cluttered, with weak path hierarchy and repeated dressing; no shot establishes an engaging traversal rhythm. Safe recovery earns credit for robustness only. |
| Story and game loop | **14** | The current objective and craft-to-gate premise are understandable. Most context is delivered in HUD text, and the captures show little character acting or emotional payoff. Automated route completion does not establish a satisfying loop. |
| **Overall quality** | **12** | The seven category scores average exactly 12. The project has a coherent starting idea and substantial working systems, while its visible presentation and direct feel report remain rough-prototype quality beside Odyssey. |

### What this assessment can and cannot establish

- Visual evidence: latest 1600×900 Unity capture `Logs/score-comparison/game-current-splat-feet.png`, plus archived Unity Arrival views `game-panorama.jpg`, `game-wide.jpg`, and `game-detail.jpg`.
- Feel and character judgment: the creator's direct play feedback is primary evidence. The new review did not run a human playthrough or capture a continuous motion sequence.
- Functional validators and smoke runs count only as evidence that systems can complete; they do not raise animation, usability, level-design, or fun scores.
- Official Odyssey promotional imagery is polished marketing material and is not a matched camera capture. Comparisons are illustrative, not a controlled test.

The original three-category estimate follows for historical context; it is superseded by the expanded scores above.

> **Correction to interpretation:** The iteration 167 values are internal progress scores from implementation changes and automated smoke evidence, not credible Odyssey comparisons. The creator's direct play feedback identifies three major failures: Splat's design is assembled from shapes (rated 7/100), the HUD feels like unstyled labels, and movement has loose jelly-like motion. The review below uses that hands-on evidence with current screenshots and code inspection. The visual score is still an estimate, but fun/feel is no longer called unreviewed.

| Category | Provisional benchmark score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **18** | The fjord palette and gateway provide a direction, but the current captures show broad bare surfaces, repeated props, rough edges and oversized rectangular overlays. The result lacks authored composition, typography and finish compared with Odyssey. |
| Story, characters, and emotional appeal | **11** | The creator rates the main character at 7/100. The newest gameplay frame now shows small coral feet beneath Splat, but the back still reads as a large purple orb with a teal badge and the face remains hidden. A Vesper objective exists, but the story depends heavily on text and has little visible character performance. |
| Fun, controls, and moment-to-moment play | **15** | The creator directly reports jelly-like movement and loose object motion. Automated smoke proves the route completes, not that movement feels good. This direct hands-on report outweighs functional smoke coverage for player enjoyment. |
| Overall quality | **15** | Mean (18 + 11 + 15) / 3 = 14.7, rounded to 15. No category clears 30; previous numbers materially overstated the player-facing build. |

### Screenshot evidence

See [ODYSSEY_VISUAL_COMPARISON.html](ODYSSEY_VISUAL_COMPARISON.html) for side-by-side pairs of Arrival screenshots and official Super Mario Odyssey imagery. The latest 1600×900 Unity frame is paired with an official kingdom image and labeled as an unmatched comparison; older panels use earlier Arrival captures.

### Asset and implementation audit

- The newest Arrival capture is a 1600×900 Unity Play Mode camera render saved at `Logs/score-comparison/game-current-headless.png`. It shows Splat from behind, the fjord/gateway, a center-screen pickup toast, and the older sliced HUD. It predates the compact-corner HUD source edit and therefore does not verify that fix.
- The shared visual builder prefers `Resources/Characters/Splat_Rigged.fbx`. Its main body and flippers form a fused skinned surface; face and maker gear use named bones. Unity imported the FBX and albedo and completed C# compilation, but the current gameplay screenshot does not show Splat's face or feet and cannot establish natural animation. Blender still previews are not in-game performance evidence.
- The existing glossy humanoid Meshy hero remains unsuitable for gameplay. Meshy inspection on 2026-10-18 found one Splat image-to-3D model and no rigging or animation tasks. The balance is 2,955 credits (2,935 monthly credits and 20 permanent; refresh 2026-11-10), but the Usage API returns 403 on this account, so the monthly allowance and current-cycle usage cannot be verified. No generation occurred.
- Meshy returned one account model named `Splat Character 3D`, task type image-to-3D. The account lists no rigging or animation tasks. A separate glossy humanoid FBX is present in the project but is not the in-game Splat; the bootstrap explicitly avoids swapping it in because its design was wrong.
- Arrival UI uses built-in Unity `Text` and runtime `Image` panels with hardcoded anchors. In the latest screenshot, sliced ornaments stretch through text, quest labels are tiny, and the pickup toast blocks the play route. A newer compact-corner frame is in source but has not yet been compiled or rendered, so no HUD score increase is warranted.
- The same current Meshy balance and usage limitation are recorded above; verify current-cycle usage and the plan allowance before any future generation. Never spend at or beyond 80% of monthly allowance.

### Highest-impact next changes

1. Import and review the skinned Splat candidate in Unity; fix scale, orientation, materials, skin movement, camera framing, and any mismatch between the model and procedural poses.
2. Render the updated HUD at common aspect ratios and check hierarchy, text fit, contrast, and screen coverage.
3. Human-play running, jumping, landing, camera, and object reactions; tune them until they feel grounded and intentional.
4. Give the Arrival route specific landmarks and varied safe paths; preserve the existing water/catch and checkpoint recovery.

### Change since iteration 167

- Added a screenshot comparison artifact and a more candid Odyssey-relative assessment based on creator feedback and source inspection.
- Corrected the meaning of iteration 167 internal progress values; they are not player-facing quality evidence.
- Audited the Meshy account and current active Splat path. Generation is deferred because the account's monthly usage and allowance cannot be verified and no explicit generation budget/model confirmation has been provided.
- A source-only HUD pass now shortens the persistent objective copy and reduces the brand, objective, side-quest, control-hint, and collectible panel footprints. The comparison screenshots predate this pass; current rendering and responsive layout remain unverified, so the scores stay at 18 / 11 / 15 / 15.
- A second source-only pass reduces jump/landing squash, fixes the ground-pound impact from stretching taller, and tones down the run bounce across the prefab and all serialized scenes with Splat. The screenshots predate both passes; no current render or play review exists, so the scores stay at 18 / 11 / 15 / 15.
- A third source-only pass removes bob/spin from decorative path markers, reduces generic scenery wobble, and keeps generic transform animation off Meshy gameplay actors. The current comparison remains the pre-pass capture; no visual or play evidence supports a score change.
- Added an original four-view Splat production reference and a rig/integration brief in `docs/art/`. It is not in the game yet, so Splat's 7/100 rating and all four comparative scores remain unchanged.
- Iteration 172 adds an original illustrated frame texture to the Arrival HUD panels and tightens their source bounds. This is an uncompiled, unrendered source change; the current screenshots show the prior flat layout, so the 18 / 11 / 15 / 15 scores remain unchanged pending review.
- Iterations 174–175 author and refine a skinned Splat prototype, then wire the FBX into the shared character-builder path with the procedural visual as fallback. Blender previews show a round purple character with fused flippers, expressive eyes/brows, boots, antenna, and a star-marked mapmaker pouch. The FBX has not been reviewed in Unity, so the screenshot-based ratings remain 18 / 11 / 15 / 15 and Splat remains 7/100 in the verified game captures.
- Iteration 176 calms large repeating body/antenna motion and applies small skinned landing/jump squash to the spine bone so the game-object root and feet stay steady. This is source-only and remains unverified in Unity; the Fun score is still based on the creator's direct jelly-motion feedback.
- Iteration 177 adds counter-swinging leg poses to the skinned Splat run cycle and resets the legs for other actions. The fallback avatar safely omits this because it has no leg bones. Timing and foot contact remain unreviewed, so scores are unchanged.
- Iteration 178 adds a subtle seamless amethyst albedo to the fused body and updates the Blender preview/FBX. It is connected in the authoring source and saved beside the FBX, but Unity material import and texture scale remain unverified. Scores stay at 18 / 11 / 15 / 15.
- Iteration 179 aligns the alternating leg gait phase with the existing speed-based footstep cadence. The timing is still source-only and unobserved in the game; scores remain unchanged.
- Iteration 180 shortens each Arrival objective to its immediate next action, nests the three progress pips in the objective header, and reduces the always-on controls to move/jump while interaction stays contextual. The current captures predate this source edit; no render or play review supports a score change (18 / 11 / 15 / 15).
- Iteration 181 removes continuous bob and spin from the craft-path spring/gear/star markers, shrinks them by 28%, and reduces their emissive intensity. This targets visible source-level clutter and motion, but no new capture or player review is available; scores remain 18 / 11 / 15 / 15.
- Iteration 182 brings the contextual interact panel into the Nordic frame system, reduces its width, and highlights the binding separately from the action. The older gameplay captures cannot confirm the revised presentation; no score change (18 / 11 / 15 / 15).
- Iteration 183 labels the quest board `OPTIONAL JOURNAL`, removes repeated `[Side]` prefixes, and narrows its panel. The quest names and progress counts remain in source; the older captures cannot confirm the visual hierarchy, so scores remain 18 / 11 / 15 / 15.
- Iteration 184 makes Arrival hard props stay fixed by disabling the generic root bob, yaw drift, and squash; trees/plants receive only a minimal slow sway. This code change directly targets the reported jelly-like scenery, but no runtime capture verifies it. Scores remain 18 / 11 / 15 / 15.
- Iteration 185 audited source coverage: the 520-unit FallVolume is sized to the fjord catch, the water spans 528 units, and checkpoint-relative plus high-altitude PlayerRespawn fallbacks exist. This confirms recovery logic is present in code; every route edge and camera view still needs player review. Scores remain 18 / 11 / 15 / 15.
- Iteration 186 slightly increases broad watercolor contrast on the authored Splat skin and renders the FBX again. An offline Blender helper also produces idle/run/jump stills for the model; the poses are not Unity-calibrated and cannot verify game motion. No gameplay image changed; scores remain 18 / 11 / 15 / 15 and Splat remains 7/100 in the active captures.
- Iteration 187 restores the Splat authoring script to the proportions used by its last saved FBX after an unrendered wider-arm trial. It also exposed a duplicate local variable in `OrganicSplatBuilder`, fixed and verified by a Unity 6000.5.4f1 headless compile (zero errors, 46 warnings). Its initial cylindrical UV comparison was inconclusive; iteration 189 established that the preview was using generated coordinates instead of the test UVs, so the earlier unwrap conclusion was withdrawn.
- Iteration 188 changes the bottom move/jump hint to fade after sustained grounded movement, with a short reading delay and no dismissal on an idle start. Unity 6000.5.4f1 compiles the change with zero errors; no game capture shows the new behavior, so scores remain 18 / 11 / 15 / 15 and Splat remains 7/100 in the active captures.
- Iteration 189 explicitly connects the saved and cylindrical mesh UV maps in a valid Blender comparison. The saved smart-project layout produces stepped patches; rear-seamed cylindrical UVs substantially smooth the front render. The new FBX/blend/texture were regenerated, Blender re-imported the 17-bone rig and weighted body, and Unity's batch asset pipeline imported the blend and FBX with zero C# errors. A matching-material test confirmed the Splat albedo is referenced. Unity logged obsolete external-material-location warnings, and no game scene render or playthrough occurred. No comparative scores changed: 18 / 11 / 15 / 15, Splat 7/100 in the active captures.
- Iteration 190 adds an actual 1600×900 Unity Play Mode frame to the comparison board. It shows the fjord and gateway, but also the orb-like rear character read, broad shelf, repeated rock piles, center-blocking pickup toast, and HUD border bands crossing copy.
- Iteration 191 moved/narrowed the toast and switched small panels to a compact HUD frame. Its first batch capture showed that independently animated panel tints washed the inventory and quest backgrounds to white; the cyan corner marks also looked like plus signs. Scores remained 18 / 11 / 15 / 15.
- Iteration 192 gives compact panels a dedicated dark backplate and 2px perimeter, then moves the shortened pickup toast into the top-center sky gap. The actual 16:9 Unity render is `Logs/score-comparison/game-current-compact-hud-v4.png`; the gate remains visible beneath the toast and the panels have clear contrast. No human movement playthrough occurred, so scores remain 18 / 11 / 15 / 15 and Splat remains 7/100.
- Iteration 193 repositions the existing coral boots. Blender's rear static run poses make the near foot visible, but the far foot overlaps the body in one phase and the front design preview hides both boots. A fresh Unity import and follow-camera capture at `Logs/score-comparison/game-current-splat-feet.png` confirms both feet at idle. Unity reported zero C# errors and 34 warnings, including obsolete FBX material-location warnings and a Unity MCP asset-editing shutdown warning. This improves rear silhouette readability only; no score changed and no human gait review occurred.
- This review did not control the PC or start Meshy generation. Camera source changes aim from a configurable body-height focus point and begin collision casts from the pivot-rotated camera height; their player-facing effect remains unverified, so they do not change the scores.

## Iteration 232 — candid assessment and Splat candidate review, 2026-10-18

The current build is not close to the Odyssey benchmark. The four core scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; diagnostics are World **11**, UI **11**, Splat **7**, Motion **5**, Controls **6**, Traversal **8**, Story-loop **8**. No category exceeds 30, and none comes close. The details and per-category rationale are recorded in `ODYSSEY_SCORECARD.md` and pictured on the comparison board.

The actual Unity rear frame supports that low graphics score: the fjord colors and skyline give the scene some identity, but a huge fractured stone shelf, exposed water seams, disconnected platform edges, piles of loose props, many bright floating items, and a centered gate crowded with trim and effects compete for attention. The HUD is more legible than older captures, yet still looks like two dark rectangles and a control label rather than an integrated game interface. The front Unity frame shows why Splat stays at the user's 7/100: the eye rings dominate a blank round purple body, the mouth has little expression, and the diagonal sash crosses the body awkwardly. From behind, the character is a plain purple dome with tiny feet.

The score of 6 for fun/controls is based on the creator's direct report that motion feels jelly-like, plus the absence of a continuous play capture demonstrating better feel. It is not inferred from the rig or code. The opening screen only proves that the player is told to find three parts; it does not prove a satisfying invention build, use, or boss payoff. No human playthrough was performed during this review.

Iterations 231–232 were character art trials, not accepted game improvements. Removing the sash helped one line of clutter, but the character still looked like a smooth ball with unreadable limbs and large mechanical eye rings. A flatter face-surface trial was captured during a random blink; its eyes are therefore inconclusive. Both are retained as rejected candidates. The stable Splat FBX and prior front-capture target have been restored from the recorded pre-trial copy. The actual player build still corresponds to the Iteration 230 gameplay capture.

The visual board pairs the actual Arrival Unity image against Nintendo's official Odyssey kingdom promotional still and Splat against official Mario & Cappy promotional art. This gives a useful quality-direction comparison, but it is not a matched gameplay screenshot comparison. These scores are intentionally severe because the goal defines Odyssey-like polish as the target, and the user has now explicitly affirmed that nothing should score above 30. The existing top scores already stay far below that ceiling.

No game quality score rises. No tests, Meshy credits, or human play were used. Highest-impact next work: design a cohesive Splat silhouette and expressive authored face, simplify/redress the main route and gate, and capture continuous play to judge controls and the complete crafting loop.

## Iteration 285 — Splat design trial, rejected before Unity

The isolated authoring asset is `docs/art/splat-candidate-iter285/Splat_Character_Iter285.blend` and its front/rear previews are under `docs/art/splat-candidate-iter285/previews/`. Higher mesh density removes the low-resolution base-skin risk and the curved mint leaf gives a clearer mapmaker cue. The candidate still looks like a large egg, the shorter fused arms barely separate from the torso, and the cross-body sash reads as a horizontal belt. The first version had no readable arms at all; the second only partially corrected that. Do not promote this candidate. The active Unity Splat remains unchanged.

Core scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; diagnostics remain World **11**, UI **15**, Splat **7**, motion **5**, controls **6**, traversal **8**, story/loop **8**. This Blender preview is art-authoring evidence only, not gameplay evidence. No Unity import, compilation, game render, human play, formal tests, or Meshy credits occurred. Meshy usage and allowance remain unverified.


## Iteration 286 — blunt review of current Arrival quality (2026-10-18)

The user's assessment is supported. The current candid scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. No required category is above 30.

The latest 1600×900 Unity batch frame at `Logs/score-comparison/arrival-gameplay-rear-iter286.png` shows a recognizable fjord palette but a broad tiled stone causeway, exposed water cuts, disconnected-looking terrain edges, piles of debris, bright floating clutter, and a crowded gate. The large field-note panel is readable yet visually generic and takes up a substantial upper-left area. Splat's latest front capture at `Logs/score-comparison/splat-current-front-iter283.png` shows the same sparse, assembly-like character the user rated 7/100. From behind, he becomes a smooth purple dome. The images support criticism of design and composition, not claims about frame rate or continuous controls.

The Iteration 286 landform has a MeshCollider and rendered as a bridge across route gaps, but it reads as a huge causeway and still has hard water cuts. It is rejected as an art-direction result and does not raise graphics or traversal scores. The scorecard page now places this current game frame beside Nintendo's official Odyssey Explore image, and Splat's front view beside official Mario & Cappy art. These are promotional references and not matched gameplay camera shots.

The active Splat source is Blender-authored (Splat_Rigged_Candidate_Iter263) and has named bones driven by a procedural pose script. It is rigged in implementation, but it is not Meshy-generated and does not use an authored animation-clip set. The current model still looks assembled; the presence of a skeleton has not made it feel natural. Meshy generation was not run because monthly usage and allowance are unverified.

No human playthrough or formal tests were performed. The direct report of jelly-like motion keeps Fun/Controls at 6; static frames cannot verify movement either way. The opening HUD proves only that a gather objective is stated, not that the invention-to-boss loop is satisfying. No Meshy credits were used because monthly allowance and current-cycle usage remain unverified. No score increased.

## Iteration 287 — compact objective hierarchy (2026-10-18)

A no-cost HUD adjustment shrinks the objective card's normalized screen box from 33.5% × 22.5% to 30% × 20%, reduces its corner accents and part icons, and places the two optional-trail summaries on one row. In the fresh 1600×900 Unity batch render (`Logs/score-comparison/arrival-gameplay-rear-iter287.png`), the card is about 15% smaller than Iteration 286 and the objective remains legible. Its dark illustrated panel still feels generic and occupies too much of the top-left view, so this is a modest cleanup rather than a designed interface.

Core scorecard remains Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; diagnostics are World **11**, UI **17**, Splat **7**, motion **5**, controls **6**, traversal **8**, story-loop **8**. Only diagnostic UI changes, from 15 to 17, for the captured reduction and improved one-line hierarchy. The route, character, story, movement, and loop have not changed; no human play or formal tests occurred.

The account inventory contains one prior image-to-3D task named `Splat Character 3D`, but its downloadable output expired and the free download call returned 403. The task inventory found no rigging or animation result. The current Unity Splat remains a separate Blender-authored rigged candidate with procedural bone posing. Meshy reports 2,935 monthly credits and 20 permanent credits available; the current-month usage API is blocked with 403 and does not reveal the allowance. No credit was spent. We cannot safely calculate whether the 80% cap is respected, so no generation or rig task was started.

## Iteration 288 — causeway remains a failed visual direction (2026-10-18)

A Unity batch capture after narrowing the route mesh (`Logs/score-comparison/arrival-gameplay-rear-iter288.png`) shows a 5–8 m walkable crest and additional visible water. This reduces the enormous paved area from Iteration 286, but the crossing still reads as an artificial causeway, with repeated rock texture, flat side pads, sharp transitions, and scattered debris. No category score changes: Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. Diagnostics remain World **11**, UI **17**, Splat **7**, Motion **5**, Controls **6**, Traversal **8**, Story-loop **8**.

The mesh keeps its MeshCollider and existing floor/recovery safeguards; this render alone does not prove route-wide fall safety. No human play or formal tests were performed. The Iteration 287 HUD remains smaller with its optional trail list on one line; the rest of the frame, Splat, UI art direction, controls, and story loop are unchanged.

The existing Meshy inventory includes one old `Splat Character 3D` image-to-3D result, but the signed download URL has expired and the no-cost download attempt returned 403. Balance shows 2,935 monthly credits and 20 permanent credits; usage is unavailable through the 403-restricted endpoint, so allowance and current-month spend remain unverified. No credits were spent.

## Iteration 290 — spur collision now matches visible ground

The runtime audit found both `ART_SpurGround` meshes much wider than their colliders: 19.2 m and 21.6 m visible widths against 8 m and 9 m BoxCollider widths. Arrival now raises the thin mesh to a more substantial profile, assigns a MeshCollider from the exact visible MeshFilter mesh, and disables the undersized box. The editor bootstrap now creates future spur grounds with the same mesh-matched collision. A first attempt raised a `MissingComponentException`; an explicit Unity-object null guard corrected it. The corrected Unity batch render is saved at `Logs/score-comparison/arrival-gameplay-rear-iter290.png`, with no C# errors or runtime exceptions in its log.

This is a source/runtime safety correction, not proof from human play. It does not fix the causeway appearance. Core scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; diagnostics remain World **11**, UI **17**, Splat **7**, Motion **5**, Controls **6**, Traversal **8**, Story-loop **8**. No human play or formal tests occurred.

Meshy inventory shows one old `Splat Character 3D` image-to-3D task whose result has expired; the no-cost download returned 403. Balance is **2,935 monthly + 20 permanent credits**, while usage/allowance remain unverifiable because the usage API is 403-restricted. No credits were spent.

## Iteration 291 — movement response tuning candidate

The creator's jelly-like movement report prompted a source audit. Configured values were 62 m/s² acceleration, 96 m/s² deceleration, 0.84 air control, and 980°/s maximum turn speed. At the 7.75 m/s top speed, those imply about 0.125 s to accelerate from rest and 0.081 s to stop, before the controller's discrete update behavior. The serialized asset now uses 38 m/s² acceleration, 50 m/s² deceleration, 0.64 air control, and 680°/s turning. Top speed and the jump arc are unchanged. This should make starts, braking, reversals, and steering more deliberate, but it remains a tuning hypothesis without human play.

No score changes: Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. Diagnostics stay World **11**, UI **17**, Splat **7**, Motion **5**, Controls **6**, Traversal **8**, Story-loop **8**. No gameplay capture, compilation, formal test, or human play was performed. Compare the settings in actual continuous movement before accepting them; restore the prior values if the added inertia makes control less enjoyable. No Meshy credits were used because monthly usage and allowance remain unavailable.

## Iteration 292 — calmer fjord slate material

Added `Assets/SpaceAndCrafts/Resources/Environment/ArrivalNordicSlateAlbedoV2.png` as a versioned, hand-painted blue-gray slate base-color tile. Arrival's cliff, central route, and fjord backdrop now prefer it, with the original `ArrivalSlateCliffAlbedo` still available as a fallback. A headless Unity batch Play Mode capture was rendered at `Logs/score-comparison/arrival-gameplay-rear-iter292.png` and visually compared with same-camera Iteration 290. The new rock surface is calmer and less finely cracked, but the route silhouette remains an obvious causeway with disconnected wide pads and water cuts; clutter and the generic HUD remain. No score change: Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. Diagnostics remain World **11**, UI **17**, Splat **7**, Motion **5**, Controls **6**, Traversal **8**, Story-loop **8**. The batch log contains no C# compilation errors or runtime exceptions, but this was not a formal test or human play. No Meshy credits were spent; current-month usage and allowance are still unavailable through the 403-restricted endpoint.

## Iteration 293 — reduce HUD obstruction

The objective card was reduced from 30% × 20% to 26.5% × 16% of the canvas and its opaque panel treatment was softened. The side-task summary and item icons remain visible; the primary objective still fits on one line in the actual Unity 1600×900 capture, `Logs/score-comparison/arrival-gameplay-rear-iter293.png`. Compared with Iteration 292, the panel covers roughly 29% less area and reveals more of the fjord. It still looks like a generic dark note, so UI art direction remains unfinished.

The diagnostic UI score increases **17→18** for that rendered space/readability gain. Core scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; the scene still has major landform and clutter problems, Splat is unchanged, and the new motor response has not been evaluated by a human. No formal tests or human play were performed. Batch compilation/render completed with no C# errors or runtime exceptions. Meshy monthly allowance and current usage remain unverifiable; no credits were spent.

## Iteration 294 — consistent slate across side trails

The side-trail `ART_SpurGround` meshes used the older shelf material while the new continuous path and cliff walls used the Iteration 292 slate. Arrival now loads the new material there too and preserves the older shelf tile as fallback. In `arrival-gameplay-rear-iter294.png`, the center ridge and broad side islands share the same blue-gray layered slate. This makes the palette more coherent, but the terrain remains split into flat shelves with water seams and hard silhouettes. No core or diagnostic score changes: Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**; World **11**, UI **18**, Splat **7**, Motion **5**, Controls **6**, Traversal **8**, Story-loop **8**. Unity batch rendering completed without C# errors/runtime exceptions. No formal tests, human play, or Meshy credits.






## Iteration 310 — fused-skin rig pose refinement (2026-10-18)

Follow-up pose review found the single-bone arm weights pinched at shoulders, and the leaf had a Bone_Antenna weight group without an Armature modifier. Iteration 307 split each arm chain into upper and tip bones; Iteration 309 tapered arm and leg weights along their bone segments, reducing the obvious hip crease in the runtime-scale run pose; Iteration 310 adds the missing antenna modifier to the leaf and reviews a smaller jump lift (25° upper arm + 12° tip) for the fused flipper silhouette. The leaf now follows the stem and the compact jump pose stays connected in the still.

The Iteration 310 Blender stills are `docs/art/splat-candidate-iter310/previews/front-three-quarter.png`, `back-pack.png`, `runtime-scale-run-pose.png`, and `jump-pose-review.png`. These show isolated poses only; no continuous motion, Unity import, in-game capture, or human play occurred. Arm/leg weighting and attachment binding are more complete, but the candidate still has a generic pear silhouette and no validated natural gait. Keep it isolated. The live game and current scores remain unchanged: Graphics **10**, Story/Characters **7**, Fun/Controls **5**, Overall **7**.
## Iteration 318–320 — candidate has a cleaner silhouette, but remains unproven (2026-10-19)

The isolated candidate was built from a fused skinned body rather than assembled primitive parts. Unity's front run-pose and rear gameplay-camera captures show a compact rounded body, broad short arms, integrated feet, painted facial details, and a small sprout. The actual rear gameplay view is less spiky than Iter310, but the back still reads as a smooth blank body with a detached emblem, and the feet look slightly lifted above the selection ring. The HUD remains oversized and the causeway/clutter dominate the frame.

Unity logged 19 bones. The separate unposed capture reports bind-pose matrix maximum error **0.0000**; the run capture reports post-pose matrix reconstruction error **0.1118**, which is not a bind-pose measurement. Its scale-compensated run-pose displacement sample is max **0.0579** / average **0.0321** across 127,052 vertices. These are sampled rig diagnostics, not evidence of a natural gait or animation quality. Candidate stays isolated and Iter263 remains active. Scores remain Graphics **10**, Story/Characters **7**, Fun/Controls **5**, Overall **7**. No human play, formal gameplay test, or Meshy spend occurred.
## Iteration 321–323 — grounded run review; candidate silhouette still weak (2026-10-19)

The capture tool's original editor-frame delay recorded only ~0.1 s of game time. It now waits on actual Play Mode time. At 0.45 s, Unity reported `grounded=True` and a planar speed of 7.75 m/s. The lavender puffs around the original spawn ring are the temporary landing-dust effect; they are not disconnected feet. From the rear, Iter318's compact oval body hides its limbs in the sampled run pose. That weak silhouette is enough to keep the candidate isolated, but the frame does not establish skinning failure. Active Iter263's body remains together, with visibly separated appendages.

The Iter322 active run image used an exploratory 21° leg swing. Afterwards, the active gait was retuned from 8–14 to 6–9.2 radians/sec, with a 12° maximum leg swing (baseline 10°), 6° arm-tip swing, and 5 mm speed-scaled body bob. The Iter323 capture uses the retuned values and appears nearly identical to Iter322 at this single phase; continuous motion still needs a multi-phase review and human feel signoff. Scores remain Graphics **10**, Story/Characters **7**, Fun/Controls **5**, Overall **7**. No human play, formal test, or Meshy spend.
After reviewing the active capture, the run phase was reduced from 8–14 to 6–9.2 radians/sec, leg swing is capped at 12°, arm-tip swing at 6°, and stride bob at 5 mm. These are source changes after the capture and remain unverified. The temporary 21° / 12 mm tuning was backed down based on the rendered frame. Keep Graphics **10**, Story/Characters **7**, Fun/Controls **5**, Overall **7**. No human play, formal test, or Meshy spend.
## Iteration 324–325 — stronger rest silhouette; initial movement sample invalid (2026-10-19)

Iteration 324 makes the torso slightly narrower and lets integrated flippers protrude clearly from its single fused skin. Blender and Unity rest views read better from the front. The normal gameplay rear capture at logged 7.75 m/s contains only the leaf/stem; the body is missing. The candidate is not usable until the cause is identified as renderer culling/bounds or skin deformation and corrected. Its attractive rest still is insufficient evidence. Iter263 remains active. No score increase, human play, or Meshy spend.

## Iteration 329–342 — corrected Splat movement and stride review (2026-10-19)

The earlier movement captures started before the initial landing finished, so they sampled a landing pose rather than an established run. They do not establish that the renderer or skin failed. The helper now waits for a stable grounded interval, applies movement, reapplies the requested pose, and then waits 0.45, 0.70, or 1.00 seconds of Play Mode time. Unity logs confirm `grounded=True` and `planarSpeed=7.75`. The complete fused Iter324 body is visible in the corrected rear captures. The 0.70- and 1.00-second frames look very similar, and the recorded limb angles shift by only a few degrees. The candidate's rear still lacks a compelling design: it is a plain round body with a solitary mint emblem and antenna, and the stride is weakly readable. Keep Iter263 active until a deliberate art comparison and human motion review. Scores remain Graphics **10**, Characters **7**, Fun/Controls **5**, Overall **7**. No Meshy credits were used.

The first viewport projection calculation incorrectly applied the FBX's 95× scale twice; its projected bounds were invalid. The corrected transform puts all sampled skinned vertices inside the camera frustum for the settled moving capture. Captures: `arrival-gameplay-idle-move-candidate-iter326.png`, `arrival-gameplay-run-candidate-iter325.png`, `arrival-gameplay-run-candidate-iter339-phase-b.png`, and `arrival-gameplay-run-candidate-iter340-phase-c.png`; batch logs: `arrival-candidate-iter337-settled-movement.log`, `arrival-candidate-iter338-settled-run.log`, `arrival-candidate-iter341-run-joints-b.log`, and `arrival-candidate-iter342-run-joints-c.log`.

## Iteration 344–347 — targeted run-stride pass (2026-10-19)

In response to the weakly readable run, increased alternating limb swing from 12° to a maximum 18° and added up to 3.5 cm of world-up lift on the stepping foot. Torso bob stays at 5 mm. Non-run poses interpolate the feet back to their rest positions. Settled Unity captures at 0.45, 0.70, and 1.00 seconds show complete candidate and active characters at 7.75 m/s, but the stills still read mostly upright; they support that the rig is being driven, not that the result feels natural. Keep scores unchanged and seek a human play review before rating the movement higher. No Meshy credits were used.


## Iteration 348–357 review — face improvement does not yet fix Splat

The best isolated face so far is Iter355: the navy oval pupils and visible coral cheeks read softer and closer to the requested Kirby-like style than Iter349's narrow cyan rings. Its geometry is a single fused skin with 19 bones. At rest, Unity confirms a 0.0000 bind-matrix error; a live-idle candidate capture looks compact, and two grounded run samples confirm the rig deforms at 7.75 m/s. These are static frames only. The body's pear shape is still generic, the back has little character identity, and the run poses change modestly.

The attempt to use Iter355 through the normal default visual loader produced a stretched T-like Idle capture, unlike the separately rebound candidate. The default selection was reverted, and a fresh Iter356 frame confirms Iter263 remains active. Treat the loader/animator discrepancy as unresolved; do not promote Iter355 until its full runtime path is stable and front/rear gameplay captures pass review.

All four scores stay Graphics **10** / Story-Characters **7** / Fun-Controls **5** / Overall **7**. The direct Splat rating remains 7/100; there was no human play, and no score is raised by these captures. Meshy balance is 2,935 monthly remaining + 20 permanent, but `/usage/tasks` returns 403 for this account, so allowance and month-to-date usage are not both verifiable. No generation or credits were used. See `docs/ODYSSEY_VISUAL_COMPARISON.html` for the actual Unity images.

## Iteration 358–365 review — modest procedural stride pass

Splat's run stride now has a maximum 24° alternating arm/leg swing, 6 cm foot lift, 11 cm fore/aft leg travel, and the same 5 mm torso bob. The Iter362 rear capture misleadingly appeared unchanged because the helper skipped the active animator; Iter363 corrected the capture and confirms the requested Run pose at 7.75 m/s while grounded. Active Iter263 front samples at 0.45 and 0.70 seconds show different arm angles; the normal rear view still reads nearly upright. This is evidence that the procedural pose changes, not proof of a natural gait or a better feel. Keep Fun/Controls at 5 until a human session.

The user-rated 7/100 active Iter263 character remains the default. Iter355 is still isolated after its normal-loader Idle capture stretched into a T-like shape; the current stride tune does not solve that integration issue. All core scores stay 10 / 7 / 5 / 7, every category below 30. No formal test suite or human play was performed.

## Iteration 366–369 review — default-loader deformation remains a blocker

The isolated 19-bone Iter355 skin reads friendlier in front view and stays compact when the capture diagnostic explicitly rebinds the gameplay pose driver. Moving the same candidate into the ordinary Arrival resource path reproduces the defect: the front/back captures show a giant cropped T-like body, and the baked mesh's vertical dimension grows to 2.84 units from 1.58 in isolated Idle. The posed-skin matrix residual is about 0.49 in that path versus 0.1346 in Iter357; neither value is a bind-pose test. I briefly changed resource priority to reproduce the defect, then restored Iter263 and captured it at Iter369. The direct default-loader visual is not acceptable, so Iter355 stays inactive.

Added a guard in `SplatVisualRepair.PrepareSkinnedCharacter` to avoid rebinding the same visible skin on repeated repair callbacks. This protects the animator's captured rest pose; the Iter368 recheck proves it does not solve Iter355's stretching. A capture-helper guard now permits diagnostics without a candidate resource to leave the default visual untouched. Iter369 confirms the old Iter263 model is back; its 0.8409 posed residual and unfriendly mechanical face remain. No score changes: Graphics 10, Story/Characters 7, Fun/Controls 5, Overall 7. Unity batch compilation and rendered captures were performed, but no formal gameplay test or human play was performed.

Next: instrument normal and diagnostic candidate setup at the moment of bind to identify which transform or action diverges; only then adjust the FBX hierarchy/scale or animator. Keep Iter263 active until a correctly loaded front, rear, idle, and continuous movement comparison passes. Scores remain below 30; no Meshy credits were spent because allowance and month-to-date usage remain unverifiable.

## Iteration 370 review — candidate face now uses the intended shader

The ordinary Iter355 loader path had rendered a uniform purple body because only the isolated-capture helper assigned `SpaceAndCrafts/SplatVertexColorLit`. Added the same material path to `OrganicSplatBuilder`, with neutral eye blink/expression/gaze defaults. Iter370's Unity image shows the painted face and cheek color in the ordinary Arrival capture. The first capture was under-settled and its 2.8382-unit extent / 0.4900 posed residual did not represent steady Idle. Iter372 waited 0.70 seconds of actual Play Mode time and produced a compact 2.004 × 1.020 × 1.756 extent; this supersedes the T-like deformation conclusion. Iter355 is now selected before Iter263 in resource priority. The character score stays 7/100 and core scores remain 10 / 7 / 5 / 7. No human play or formal test was done.

Final rollback confirmation: Iter371 captured Iter263 after the earlier rollback trial; that historical frame is superseded by the current Iter355 resource priority and settled Iter372–374 images. No score increase is warranted.

## Iterations 372–374 — settled normal-loader review

The T-like Iter366–370 diagnosis came from taking captures after editor updates but before the requested pose had settled in Play Mode time. A corrected Idle sample (Iter372) shows Iter355 compact in the ordinary Arrival loader with its face shader. Two actual grounded run samples (Iter373/374, 0.45 and 0.70 seconds, 7.75 m/s) retain the fused body and differ in pose. The character is now selected in `OrganicSplatBuilder`.

This corrects the implementation assessment but does not justify a rating increase: the warm rounded expression is friendlier than Iter263, while the back remains an isolated map emblem and the run cadence/foot planting remain unclear from still images. Keep the user's Splat score at 7/100, fun/controls at 5/100, graphics at 10/100, and overall at 7/100. Evidence is in `splat-candidate-default-shader-iter370.png`, `splat-candidate-default-loader-run-rear-iter373.png`, and `splat-candidate-default-loader-run-rear-iter374.png`. These are batch captures, not human-play or natural-animation verification. No Meshy generation; allowance and MTD usage remain unavailable.

## Iterations 375–377 — modest Splat identity pass (2026-10-19)

Iter375 is a fused single skinned mesh built from the existing 19-bone character, with a soft lilac color treatment, a more readable painted smile, a small gold compass/map pin painted on the chest, and a winding route with two waypoints painted on the back. No separate primitive parts were added. `OrganicSplatBuilder` now prioritizes Iter375 ahead of Iter355 and Iter263; it applies the same vertex-color material path to both candidate generations.

Unity batch Play Mode captures through the ordinary Arrival loader show Iter375 settled in Idle (`splat-default-375-front-settled-idle-iter376.png`) and in a grounded rear Run sample (`splat-default-375-rear-grounded-run-iter377.png`). The latter log records 7.75 m/s at 0.55 seconds and `grounded=True`. The images confirm that the authored skin/details appear in the regular loader and remain intact in these sampled poses. They do not establish a natural continuous gait, foot planting, or player feel. The design remains generic and small at gameplay scale.

| Required category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **10/100** | This is a local character texture/identity pass. The wider fjord, traversal surfaces, and HUD still need substantial cohesive art direction. |
| Story, characters, and emotional appeal | **7/100** | The face and mapmaker details are clearer and friendlier than the previous default, but the user's direct character score remains 7/100; the silhouette and expression range remain limited. |
| Fun, controls, and moment-to-moment play | **5/100** | A grounded movement sample is not a continuous animation review or human play session. The full exploration-to-boss loop remains unverified. |
| Overall quality | **7/100** | Mean remains (10 + 7 + 5) / 3 = 7.33, rounded to **7**. No score is raised on still-image evidence. |

This supersedes TODO/scorecard notes saying Iter355 remained inactive after Iter372–374; Iter355 was active then, and Iter375 is now the selected resource. No human play, formal gameplay test, or continuous animation recording was performed. No Meshy generation occurred; allowance and month-to-date usage remain unverifiable, so no credits were spent.

## Iteration 378 — downturned flipper silhouette promoted through Unity (2026-10-19)

The separate Iter378 Blender candidate lowers and tapers the fused flipper arms while retaining the 19-bone skeleton, painted chest compass, and rear route map. The first Unity `-nographics` capture attempt was unusable because URP could not create its render targets; it is superseded by subsequent graphics-enabled batch captures. Those normal-loader captures show the selected Iter378 skin settled in Idle at 2.019 × 1.019 × 1.754 baked bounds and in two grounded rear Run samples at 0.55 and 0.90 seconds (7.75 m/s; the first has 1.882 × 1.122 × 1.835 bounds). Unity batch compilation succeeded before each capture. The arms now angle downward at rest, softening the T-shaped silhouette. The two stills show pose changes, but the legs and torso remain nearly rigid at gameplay scale; the character remains simple, his feet hover above the spawn ring in Idle, and neither sample demonstrates natural cadence or foot planting.

The active resource priority is now Iter378, followed by Iter375 and Iter355. Keep scores unchanged: Graphics **10**, Story/Characters **7**, Fun/Controls **5**, Overall **7**. These captures establish the regular-loader visual and one grounded movement sample only. No continuous animation recording, formal gameplay test, or human play occurred. No Meshy generation or credit spend occurred; allowance and month-to-date usage remain unverifiable.

## Iteration 379 — run-stride experiment rejected (2026-10-19)

Raised stride frequency and limb swing, extended the feet, and added small pelvis/torso roll and lateral shift to try to make the run read more clearly without increasing vertical bounce. Unity compiled the change and captured grounded 7.75 m/s samples at 0.45 and 0.70 seconds, but the skin deformed into long sideways tails; baked width expanded to 2.746–2.878 m, versus about 1.88 m in the accepted Iter378 pose. Rejected the tune and restored Iter378's prior run values: 6–9.2 rad/s phase, 24° maximum leg/arm swing, 6 cm foot lift, 11 cm fore/aft leg travel, and 5 mm torso bob. A post-revert graphics-enabled capture compiled successfully and returned to the compact 1.882 × 1.122 × 1.836 m bounds. Keep Iter378 active. This confirms the current motion score remains weak; do not treat ground state or a changing still as evidence of natural animation. Scores remain Graphics 10 / Story-Characters 7 / Fun-Controls 5 / Overall 7. No human play or formal test.

## Iterations 380–381 — fjord skyline alternatives rejected (2026-10-19)

Tested two smoother peak-height profiles (layered sine and seeded Perlin) in the generated backdrop. Both reduced the near wall to an overly level band and made the landform read more artificial, despite their broader skyline. Restored the original ridge formula. The final graphics-enabled Unity capture compiled and rendered with the restored profile; no visual score change is claimed. The existing angular fjord still needs authored landscape art or a stronger mesh treatment. Core scores remain Graphics 10 / Story-Characters 7 / Fun-Controls 5 / Overall 7. No human play or formal gameplay test.

## Iterations 382 and 383 — Splat surface study and commissioned asset audit (2026-10-19)

The Iter382 isolated Blender study increased the visibility of painted topographic contours and masked the face, but its front preview rendered them as broken, scratch-like dashes; it was not exported to the game or promoted. Separately inspected `Assets/SpaceAndCrafts/Art/Commissioned/Hero/Splat_Hero.fbx`: Blender imported one 4,299-vertex FBX mesh and one 6,921-vertex GLB mesh; neither contains an armature or animation actions. Its thumbnail has goggles, prominent red-ring eyes, and a small frown; it is more detailed but does not meet the rigged-natural-motion requirement and is not selected. Iter378 remains the current default. Scores remain Graphics 10 / Story-Characters 7 / Fun-Controls 5 / Overall 7. No Meshy generation or credits used.

## Iteration 384 — softer Splat eye color and cheek read (2026-10-19)

Adjusted `SplatVertexColorLit` to shrink the cream eye surround slightly, shift the iris ring from bright teal toward muted blue-violet, and strengthen the rosy cheek color. The Unity graphics-enabled batch capture at `Logs/score-comparison/splat-default-378-front-settled-idle.png` compiled successfully and renders through the regular Arrival loader. At gameplay framing, the difference is subtle: eyes still occupy a large part of the face, the cheeks remain shadowed, and Splat is still a simple mascot. Keep Iter378 active with this material; no score increase. Graphics 10 / Story-Characters 7 / Fun-Controls 5 / Overall 7. No human play or formal gameplay test.

## Iteration 385 — legacy rig resource checked (2026-10-19)

The fallback `Splat_Rigged.fbx` does have a 17-bone armature and a 53,332-vertex body mesh, but Blender found no embedded animation actions. It also contains separate eye, brow, boot, sash, and map-roll meshes, which makes it less cohesive than the selected fused Iter378 body and does not supply the requested naturally moving clips. Do not switch to it as a shortcut. Keep Iter378 and the existing procedural pose driver; motion remains a major open weakness. Scores stay Graphics 10 / Story-Characters 7 / Fun-Controls 5 / Overall 7. No human play or Meshy generation.

## Iteration 386 — conservative leg-stride readability pass (2026-10-19)

Changed only the run legs from Iter378's 24° maximum swing, 6 cm lift, and 11 cm fore/aft travel to 27°, 7 cm, and 13 cm; arm swing, 5 mm torso bob, and 6–9.2 rad/s stride phase remain unchanged. Graphics-enabled Unity batch captures at 0.45 and 0.70 seconds log grounded movement at 7.75 m/s. Baked body bounds stay compact (1.909 × 1.108 × 1.858 m and 1.961 × 1.079 × 1.779 m), and all sampled vertices remain in the camera frustum. The two poses show clearer leg separation than Iter378, without Iter379's lateral skin collapse. They remain isolated stills; natural timing, foot planting, and player feel are unverified. Keep this restrained tune, but hold Fun/Controls at 5 and all core scores at Graphics 10 / Story-Characters 7 / Fun-Controls 5 / Overall 7. No formal gameplay test or human play.

## Iteration 387 — authored slate albedo on the walkable route (2026-10-19)

Added a new versioned hand-painted slate albedo (`ArrivalNordicSlateAlbedoV3.png`) and applied it to the spawn shelf, spur grounds, and continuous fjord landform top. The cliff walls keep the older fractured V2 material so the broad plates do not turn the mountains into paving. The 1254×1254 texture's mean opposite-edge difference is about 9/255; Unity repeat sampling shows no distracting seam in the inspected captures. Graphics-enabled Unity batch compilation succeeded and rendered both settled front Idle and grounded rear Run views. Broad cool-gray plates make the playable surface more coherent and readable, though the route still has slab geometry, abrupt water cuts, and foreground clutter. This is a local material improvement only; keep Graphics at 10/100 and overall at 7/100. No human play or formal gameplay test. No Meshy generation or credits used.

## Iteration 388 — continuous fjord bedrock albedo candidate (2026-10-19)

Generated a new versioned, seamless cool slate texture (`ArrivalNordicSlateAlbedoV4.png`) with flowing mineral strata instead of the V3 image's large cracked paving plates. Arrival's walkable shelf, spur grounds, and continuous landform tops now prefer V4; V3 remains a fallback and cliffs retain V2. The V4 tile's measured opposite-edge mean difference is about 13.4/255 horizontally and 14.1/255 vertically, similar to the prior texture, so repeat seams remain a visual check rather than a mathematical guarantee. Graphics-enabled Unity batch captures compiled successfully and rendered the settled front Idle view (`arrival-slate-v4-front-iter388.png`) and grounded rear Run view (`arrival-slate-v4-rear-iter388.png`); the run log records `grounded=True` at 7.75 m/s and 0.45 seconds. In these stills, V4 reads more like continuous layered rock than V3's cracked paving. The follow view still exposes a slab-shaped causeway, abrupt water cuts, loose debris, angular cliffs, and generic dressing; no world-scale or traversal claim is made.

| Required category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **10/100** | V4 softens the paving pattern in two Unity batch stills, but the route geometry, debris, angular backdrop, and temporary-looking props still dominate. This isolated texture does not justify a category increase. |
| Story, characters, and emotional appeal | **7/100** | No character or story change. Splat remains the selected Iter378 fused 19-bone candidate; the creator's direct character rating remains 7/100. |
| Fun, controls, and moment-to-moment play | **5/100** | One simulated grounded run sample confirms capture state only. It does not establish responsive controls, natural gait, satisfying traversal, or the complete craft-to-boss loop. |
| Overall quality | **7/100** | Mean remains (10 + 7 + 5) / 3 = 7.33, rounded to **7**. No score is raised on material-only evidence. |

No human playthrough or formal gameplay test occurred. Unity batch compilation and two still captures were performed; the captures are not a playtest. No Meshy generation or credits were used; monthly allowance and current-month usage remain unverifiable. Next high-impact work: replace the disconnected-looking route silhouette with continuous authored terrain and reduce corridor debris, then evaluate movement and fall recovery in human play. Keep all scores below 30 until broad player-facing gains are evidenced.

## Iteration 391 — move decorative island clusters away from route center (2026-10-19)

Arrival had been placing a decorative Meshy rock/tree clone roughly 2 m from each hidden island floor center, so clusters spilled into the running lane. Iter389 tried a fixed world-side offset; Iter390 widened that offset and disabled shadow casting for these clones, but the capture still showed one cluster close behind Splat. Iter391 now projects each island center onto the start→build-site route segment and moves the clone outward until its center is at least 5.5 m from that route line (unless the island already lies farther out). The graphics-enabled Unity batch build succeeded and the grounded follow-camera capture `arrival-shoulder-dress-rear-iter391.png` logged 7.75 m/s at 0.45 seconds. The near/mid running corridor reads clearer, with the clusters placed to either side. This is a single static run sample, not traversal proof; substantial rubble remains on the foreground and side pads, and long dark marks across the water remain visible. The shadow change did not remove those marks, so their source is unresolved.

| Required category | Score | Evidence and reason |
|---|---:|---|
| Graphics and visual polish | **10/100** | The follow-view corridor is less visually obstructed, but the slabs, rubble, dark marks, angular walls, and temporary-looking props remain prominent. Keep the score unchanged. |
| Story, characters, and emotional appeal | **7/100** | No character or story changes; Splat's design remains the user's 7/100 reference concern. |
| Fun, controls, and moment-to-moment play | **5/100** | Grounded simulated movement in one capture does not show obstacle traversal, responsiveness, or player enjoyment. |
| Overall quality | **7/100** | Mean remains 7.33, rounded to **7**. No score increase from a single camera still. |

No human playthrough or formal gameplay test occurred. Unity batch compilation and a rendered still were performed. No Meshy assets were generated and no Meshy credits were spent. The broad 90/100 target remains unmet; next art priority is removing remaining path clutter and resolving the slab/causeway silhouette, followed by human review of safe recovery and the craft-to-boss loop.

## Iteration 392 — landform shadow hypothesis rejected (2026-10-19)

Temporarily disabled shadow casting on the continuous fjord landform and its two spur saddles to check whether their below-water skirts caused the long dark marks across the water. The graphics-enabled Unity batch build succeeded and rendered `arrival-landform-no-cast-shadows-iter392.png` at grounded 7.75 m/s after 0.45 seconds. The dark marks remain visible with that shadow change; the hypothesis is rejected and the source renderer settings are restored. The marks' actual source is still unknown. Scores unchanged: Graphics 10 / Story-Characters 7 / Fun-Controls 5 / Overall 7. This was a rendered diagnostic still, not a playtest; no Meshy credits were used.

## Iteration 393 — realtime-shadow cause ruled out (2026-10-19)

Unity `QualitySettings.shadows` was disabled for a diagnostic Arrival capture. The same long dark strips remain in `arrival-no-shadows-diagnostic-iter393.png`, so neither the fjord landform/saddle caster toggle in Iter392 nor Unity realtime shadows explain them. Their source is likely visible geometry or a material/mesh artifact, but the responsible object is not identified; no visual code fix is claimed. Scores remain Graphics 10 / Story-Characters 7 / Fun-Controls 5 / Overall 7. Unity batch compilation and a grounded 7.75 m/s run still capture succeeded; no human play or formal gameplay test occurred. The diagnostic flag was used only in the batch capture; the game renderer settings remain restored. No Meshy credits were used.

## Iteration 394 — respond to grounded-character and enemy-motion review (2026-10-19)

The creator's current direct ratings are now recorded as Splat **9/100** (supersedes the prior 7/100 character-specific note) and enemies **7/100**. The saved Iter376 front Idle still visibly floats above the spawn marker; Iter377 rear Run shows a stiff, generic silhouette. Source review confirms the active Splat uses a 19-bone skin driven by procedural poses, with no authored animation clips wired into the runtime. The jump path used a small 2% spine scale and a deep torso pitch; it now removes that stretch, reduces pitch and hips travel, and moderates arm opening to limit skin distortion. SplatVisualRepair offsets the current skin down by 7 cm to address the foot gap visible in the saved Idle frame. These changes are source-only and not yet rendered, so their visual result remains unverified.

The common ScrapMite has patrol movement but no leg articulation; its root bob also lifts the feet. It now keeps the root at its patrol height and animates its existing six leg meshes with an alternating gait, plus small body, shell, and antenna motion. This adds procedural movement to one common enemy archetype, not authored clips or a genuine skinned rig, and does not resolve every enemy's static presentation.

| Required category | Score | Current evidence and reason |
|---|---:|---|
| Graphics and visual polish | **10/100** | No new render. The latest saved Arrival views still show fractured-looking platforms, debris, angular fjord walls, and a generic HUD. |
| Story, characters, and emotional appeal | **7/100** | The creator now rates Splat 9/100 and enemies 7/100; the broader category also includes story and emotional connection, which remain weak and unverified. The code edits have no rendered result yet. |
| Fun, controls, and moment-to-moment play | **5/100** | The creator reports extreme jump stretching and little readable enemy animation. Source changes target those issues but have not been compiled or played. |
| Overall quality | **7/100** | Mean of 10, 7, and 5 is 7.33, rounded to 7. The new changes are unrendered and unplayed. |

No Unity build, render, test, or human play occurred for Iter394. No Meshy credits were spent; allowance and month-to-date usage remain unverifiable. Next: compile and capture grounded Splat and the ascent/apex/landing poses, inspect foot contact, and capture an active ScrapMite gait before changing motion or category scores.
## Iteration 395 — Meshy animation delivery and runtime-use audit (2026-10-19)

The creator asked whether Meshy supplied Splat's animation and whether the game uses it. A fresh non-billable Meshy inventory query found one succeeded task: “Splat Character 3D,” task 38338327-5dd1-41ad-b394-9208a1964ffb, type image-to-3d, output GLB, 0 credits consumed. No rigging, animation, or text-to-motion tasks exist in the queried inventory. The task's signed output URL expired on 2026-08-30; the local project notes its older download attempt returned 403.

The current runtime selects the separate Blender-authored Iter378 fused skin in OrganicSplatBuilder. SplatSkeletonAnimator directly poses its bones for idle, run, jump, land, interaction, craft, listen, and celebration. Splat_TEMP has this procedural driver but no Unity Animator/controller binding for Meshy clips; the project does not import or play Meshy walk/run animations. The existing procedural poses are weak in the saved gameplay views, which explains why the user experiences Splat as having no convincing animation. This is a real pipeline gap: no Meshy rigging/animation deliverable was created, and the only Meshy model is not the active runtime character.

The current Meshy balance query reports 2,935 monthly and 20 permanent credits (2,955 total), refreshing 2026-11-10. The month-to-date usage request still returns 403 (“Studio and Enterprise only”), so the monthly allowance and spend cannot be verified. No rigging (5 credits) or animation (3 credits per action) was submitted under the 80% budget rule. No build, render, or gameplay test occurred for this audit.

Core scores remain Graphics **10**, Story/Characters **7**, Fun/Controls **5**, Overall **7**. The Splat and enemy specific user ratings remain **9/100** and **7/100** respectively. Next animation milestone: recover an inspectable humanoid source, verify account allowance and month-to-date spend, rig only after the 80% guard is calculable, then import Meshy's walk/run outputs into an Animator state machine/blend tree and validate the in-game transitions. Until that source and usage data are available, keep the present procedural path and do not spend Meshy credits.
## Iteration 396 — Spitter attack readability (2026-10-19)

The existing ScrapSpitter is a single soft-body mesh with a separate mouth blob; it has no rigged limbs or animation clips. Its script now turns toward Splat and animates the existing mouth with a short anticipatory push/open and a brief post-shot recoil. The main body scale stays fixed so this does not add more jelly-like deformation. This improves one enemy's attack tell in source only; whether the motion reads well in play is unverified.

| Required category | Score | Current evidence and reason |
|---|---:|---|
| Graphics and visual polish | **10/100** | No new render; the latest saved views still show the fractured floor, angular fjord walls, debris, and generic HUD. |
| Story, characters, and emotional appeal | **7/100** | User ratings remain Splat 9/100 and enemies 7/100; this one procedural tell has not been visually reviewed. |
| Fun, controls, and moment-to-moment play | **5/100** | No new human gameplay evidence; the latest movement/animation changes remain uncompiled and unplayed. |
| Overall quality | **7/100** | No new player-facing evidence justifies a score change. |

No build, render, automated test, or human play was performed for Iter396. No Meshy credits were spent; monthly allowance and month-to-date usage remain unverifiable (usage API 403).

## Iteration 397 — contact height audit and source correction (2026-10-19)

Source inspection found that every generated Worlds 02–10 traversal pad used a shared BoxCollider top at +0.225 m, despite the central walkable geometry tops ranging roughly from -0.039 m (dish/bowl interiors) to +0.175 m (gear hub). That could put Splat's soles visibly above smaller pads. The bootstrap now derives per-world collider tops from each authored base mesh and refreshes existing named traversal-pad colliders whenever the bootstrap is rerun. It also changes SplatVisualRepair from a fixed -0.07 m skin offset to align the skinned renderer's lowest bound with the CharacterController bottom plus 1 cm. This is a source hypothesis backed by serialized dimensions, not an observed fix: no Unity scenes were refreshed, and there was no compile, render, or play review.

| Required category | Score | Current evidence and reason |
|---|---:|---|
| Graphics and visual polish | **10/100** | No new render; world and HUD issues remain. |
| Story, characters, and emotional appeal | **7/100** | User ratings remain Splat 9/100 and enemies 7/100; the grounding change is unrendered. |
| Fun, controls, and moment-to-moment play | **5/100** | The contact change has not been played or checked for jumps, slopes, or pad transitions. |
| Overall quality | **7/100** | No player-facing evidence supports a score increase. |

No Unity bootstrap, build, render, or gameplay test occurred. No Meshy credits were spent; usage/allowance remain unverifiable.

## Iteration 398 — saved traversal collider alignment (2026-10-19)

The earlier source correction would not alter already-saved scenes until the editor bootstrap ran. The saved YAML confirms all nine worlds already contain 12–14 named pads each, for 117 total. Updated each root BoxCollider to the per-world top described in Iteration 397, using a 0.32 m collider depth, and verified per-scene counts and expected serialized centers. Backups of the nine scene files are in the system temp directory `space-crafts-iter398-scenes`.

This is a data-level check only. Unity was not opened, and no import/compile, render, collision check, jump traversal, or human play occurred. The estimated surface heights may need adjustment after a scene capture, especially bowl interiors and raised decorative details.

| Required category | Score | Current evidence and reason |
|---|---:|---|
| Graphics and visual polish | **10/100** | No new render; overall world and HUD issues remain. |
| Story, characters, and emotional appeal | **7/100** | Splat and enemy ratings remain 9/100 and 7/100; no new animation review. |
| Fun, controls, and moment-to-moment play | **5/100** | Collider values are aligned in serialized scenes, but player contact and jump reach are unverified. |
| Overall quality | **7/100** | No player-facing evidence supports a score increase. |

No Meshy credits were spent; usage/allowance remain unverifiable.

## Iteration 399 — ScrapRoller anti-jelly charge pose (2026-10-19)

Removed the ScrapRoller's 15% whole-body stretch during charge. Its existing stripe still expands for the warning, and existing body roll motion remains the charge cue. Added a subtle idle wobble, a small player-facing anticipation pose, and eased reorientation after the charge. This uses existing meshes only and does not add an authored rig or animation clip. Source-only; there was no compile, render, or play review.

| Required category | Score | Current evidence and reason |
|---|---:|---|
| Graphics and visual polish | **10/100** | No new render. |
| Story, characters, and emotional appeal | **7/100** | User ratings remain Splat 9/100 and enemies 7/100; the enemy motion change is unreviewed. |
| Fun, controls, and moment-to-moment play | **5/100** | No human gameplay review; charge windup timing and collision remain unverified. |
| Overall quality | **7/100** | No player-facing evidence supports a score increase. |

No Meshy credits were spent; usage/allowance remain unverifiable.

## Iteration 400 — Spitter mouth-origin shot and Splat clip inventory (2026-10-19)

The Spitter now spawns its projectile from the existing mouth transform and calculates aim direction from that point. Previously, the new player-facing turn was paired with a fixed world-forward spawn offset, so the shot could appear to detach from the mouth when the player was off-axis.

A fresh source inventory also finds six standalone Unity clips under `Assets/SpaceAndCrafts/Art/Animations`: Idle, Run, Jump, Land, Interact, and Craft. Each animates only `Bone_Hips` and `Bone_Spine`; no Animator component/controller is attached to `Splat_TEMP`, and no `.controller` asset exists in the project. Meshy inventory still contains no rigging/animation tasks. This qualifies the earlier audit: Unity clip files exist, but they are not integrated and they have no limb gait, so they do not provide the full-body natural motion requested. The active Splat uses the procedural `SplatSkeletonAnimator` code, which directly poses hips, spine, chest, head, arms, legs, antenna, and face. The user’s report that this reads as no animation remains the relevant visual evidence.

No build, compile, render, or play review occurred for this pass.

| Required category | Score | Current evidence and reason |
|---|---:|---|
| Graphics and visual polish | **10/100** | No new render. |
| Story, characters, and emotional appeal | **7/100** | User ratings remain Splat 9/100 and enemies 7/100; motion changes unreviewed. |
| Fun, controls, and moment-to-moment play | **5/100** | No human play or visual review; projectile source and animation readability remain unverified. |
| Overall quality | **7/100** | No new player-facing evidence supports a score increase. |

No Meshy credits spent; allowance/month-to-date use remain unverified.
