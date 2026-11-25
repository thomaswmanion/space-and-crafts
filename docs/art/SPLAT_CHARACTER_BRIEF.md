# Splat character production brief

![User-approved Splat concept](../../Assets/SpaceAndCrafts/Art/Characters/Splat/Splat_ApprovedCapEyePortrait.png)

## Approved hero design — source of truth (2026-11-01)

The current approved Splat image is `Assets/SpaceAndCrafts/Art/Characters/Splat/Splat_ApprovedCapEyePortrait.png`. Follow that image for new art and stop using the earlier purple-body/antenna/mapmaker-satchel studies as visual targets. Splat is a cute, youthful alien with one continuous, smooth white ball-shaped head and body, purple mitten-like hands and feet that join the body cleanly, a jaunty backwards purple cap, tall deep-violet single-color eyes with one small white catchlight each, simple purple brows, and a small black smile. Keep the face readable and friendly. No colored face patch, blush marks, antenna, backpack, layered teal eye color, or prominent eye-white crescents.

The approved design is now installed as a compatible skinned visual beneath the active `Splat_TEMP` player root in the saved prefab and World 1 Arrival scene. The existing physics/collision root is retained, while marker-aware repair keeps the Meshy rig and preserves Organic as a rollback visual. This establishes saved production wiring, but not a verified in-game result: no gameplay-camera capture or human play confirms the appearance, animation, or contact yet.

## Iteration 1801 — saved player integration

The 298,054-face, 24-joint Meshy asset now drives the player's Idle/Walk/Run blend and motor-triggered Jump/Land. The reviewed one-arm hello is reachable from interaction and the idle personality timer; respawn returns the Animator to Locomotion. The short no-scale landing is authored on the same rig. Rejected Meshy landing, punch, and hit takes remain outside the controller. Marker-aware repair, Arrival dressing and editor refresh retain the marked Meshy child; `SplatFeelPresenter` resolves the Meshy visual and footsteps use the timer while the Organic animator is disabled.

Unity batch authoring saved the prefab and scene. This is not yet proof that the character looks or moves well during play. Ground contact across actual varied platforms, scale stability through runtime transitions, cap/face stability, responsive interaction, live scene lighting, and cost/performance remain to be captured or measured. No build, gameplay test, render of the integrated scene, or human play was performed. Keep the Splat score at 9/100 pending that review.

## Iteration 1800 — rigged Meshy candidate and expressive idle (2026-11-10)

Created a textured Meshy model from the approved design, remeshed it to **298,054 faces**, and rigged it to a 24-joint Generic Avatar with bundled Walk and Run plus Meshy Idle/Jump clips. The Unity review prefab fits the visual to the existing 1.15 m character height and statically seats rendered soles to a flat plane. Two generated landing attempts roll Splat onto his back and are rejected; a small authored no-scale landing remains review-only. Three further Meshy idle variants were compared: only `Idle_15` reads as a friendly one-arm hello on the round silhouette. An automatic candidate driver is wired to request that one-shot only after Splat has been grounded and still for 4.5 seconds, then on a 7–13 second idle interval. A second action set was also rendered: its two-fist punch lacks a clear impact silhouette, its hit reaction turns Splat away, and the victory cheer duplicates the hello. Those are not kept in the candidate controller. A separate Meshy Victory Fist Pump clip has a modest no-scale pose and is wired as a candidate-only state for a future event hook.

Static Unity renders show that the approved look remains cute at game scale and that the hello/fist-pump poses do not stretch the shell after the candidate driver restores bind scales. These are sampled single frames with a flat support and editor root correction; they do not establish actual motor transitions, live idle timing, action responsiveness, natural running/jumping/landing, or planted feet on narrow/sloped platforms. The existing `Splat_TEMP` visual, scene, and repair hooks remain unchanged, so this is not yet the playable Splat. Keep the **Splat 9/100** rating unchanged until the candidate is integrated and reviewed in motion.

### Production gate

- Model as one cohesive character with smoothly connected appendages and deformation-friendly topology; do not approximate the design by stacking separate spheres/capsules.
- Preserve the reference's overall white-body / purple-limbs / backwards-cap read and face proportions. Keep both hands and both feet visibly attached through animation; do not add unrelated marks, gear, or extra color zones.
- Bind a genuine compatible skeleton and use distinct, readable Idle, Run, Jump, and Land motion. Keep whole-body scale stable; use only restrained impact squash if the model deforms cleanly.
- Land with both feet aligned to the actual support plane across flat, thin, and tilted platforms. Check front, three-quarter, and follow-camera views, not just a bind-pose portrait.
- The approved candidate is in the saved player assets; keep it only if a gameplay-camera review confirms it looks right. The portrait and isolated stills alone do not prove motor-driven animation or planted feet. Capture idle, run, jump, land, interactions, and contact on flat, thin, and sloped supports before raising the Splat rating.

### Iteration 1705 — local rigged candidate (not yet promoted)

An isolated locally authored candidate now follows the approved colors and cap design with a continuous voxel-remeshed, skinned body-and-limb surface, plus fitted face and cap details. Its Blender source contains a 12-bone armature and authored Idle, Run, Jump, and Land actions; GLB and FBX exports were produced. A Blender FBX round-trip recovers all four animation stacks and the weighted unified body, but this does not verify Unity import. Blender stills show a clearer two-arm jump pose after correcting mirrored shoulder-axis conversion, alternating run legs, and a grounded-looking landing settle on the review plane. The airborne still applies a separate 0.24 m rig offset; the Jump action is pose-only and leaves vertical travel to the controller. This is evidence about the candidate's authored poses only. It is not Meshy output and is not integrated into the active Splat prefab. The approved concept image above remains the art source of truth; do not promote this candidate until Unity import, action playback/transitions, support contact on flat/thin/tilted surfaces, and gameplay-camera readability have been reviewed. No production rating change follows from this offline candidate.

Evidence: `splat-unified-candidate-iter1705/Splat_UnifiedRig_Candidate_Iter1705.blend` and `.glb`; `../../Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1705/Splat_UnifiedRig_Candidate_Iter1705.fbx`; `../../Logs/score-comparison/splat-unified-candidate-iter1705.png`; action stills at `../../Logs/score-comparison/splat-unified-candidate-iter1705/`; Unity pose captures in `../../Logs/score-comparison/splat-unified-candidate-unity-import-iter1705/`.

### Iteration 1706–1707 — Unity import and candidate clip loop settings

The FBX now imports in Unity 6 as a Generic rig with a valid Avatar and four clips, and an isolated batch Editor capture displays distinct sampled Idle/Run/Jump/Land poses. Idle and Run are configured to Loop Time/Loop Pose; Jump and Land stay one-shots. This establishes importability and sampled pose changes for the candidate. It is still separate from the active prefab: controller state transitions, controller-driven jump travel, production camera/materials, and measured platform contact are not yet verified. The batch reported a known shutdown assertion after it wrote its captures. Keep the approved concept above as the design reference; do not treat this as an accepted final asset.

Evidence: `../../Assets/SpaceAndCrafts/Scripts/Editor/SplatUnifiedCandidateUnityReviewIter1705.cs`; `../../Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1705/Splat_UnifiedRig_Candidate_Iter1705.fbx.meta`; `../../Logs/score-comparison/splat-unified-candidate-unity-import-iter1705/unity-import-review.txt`; `../../Logs/splat-unified-candidate-unity-loop-settings-iter1707.log`.

### Iteration 1708 — isolated animation controller wiring

Added a separate candidate AnimatorController using the current `SplatMeshyAnimatorDriver` contract. It blends the looping Idle and Run clips by `MoveSpeed`, enters Jump on the Jump trigger, enters Land on the Land trigger, and returns to locomotion after the landing clip. Unity Editor generated the controller and a report confirming its states, transitions, parameters, and referenced clips. The controller is not attached to the active Splat prefab, and state timing/feel has not been exercised in live play. Do not mark animation integration complete yet.

Evidence: `../../Assets/SpaceAndCrafts/Scripts/Editor/SplatUnifiedCandidateControllerAuthoringIter1708.cs`; `../../Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1705/Splat_UnifiedRig_Candidate_Iter1705.controller`; `../../Logs/score-comparison/splat-unified-candidate-controller-iter1708/controller-review.txt`.

### Iteration 1709 — isolated prefab handoff

The FBX and controller now live together in a separate review prefab with a valid Generic Avatar and `SplatMeshyAnimatorDriver`. The Unity authoring report confirms the controller/Avatar assignments and disabled root motion. This makes the candidate easier to evaluate with the existing player motor, but does not attach it to Splat_TEMP or a production scene. Live parameter delivery, movement integration, transition feel, jumping, platform contact, and gameplay-camera readability are still open. Unity wrote the prefab/report before a known shutdown assertion; no game build/test or human play was performed.

Evidence: `../../Assets/SpaceAndCrafts/Art/Characters/Splat/UnifiedCandidate_Iter1705/Splat_UnifiedRig_Candidate_Iter1705.prefab`; `../../Assets/SpaceAndCrafts/Scripts/Editor/SplatUnifiedCandidatePrefabAuthoringIter1709.cs`; `../../Logs/score-comparison/splat-unified-candidate-prefab-iter1709/prefab-review.txt`.

### Splat candidate variant 1711 — tighter shoulder color, still candidate-only

This Blender revision narrows the purple transition at the shoulders and increases purple saturation while retaining the round white body, fused skinned limbs, cap, and four authored Idle/Run/Jump/Land actions. The settle still appears planted on the Blender review plane, but this is not a Unity/platform contact check. The arms remain rounded paddles, the cap looks lavender in the bright preview, and the FBX is not imported into the game. Keep the existing candidate isolated; do not raise the character score from these stills.

Evidence: `../../tools/splat/author_splat_unified_candidate_iter1711.py`; `../../Logs/score-comparison/splat-unified-candidate-iter1711.png`; pose stills under `../../Logs/score-comparison/splat-unified-candidate-iter1711/`.

### Splat candidate variant 1710 — rounder body/longer fused arms, still rejected for production

This separate Blender variant narrows the gap between the approved round white body and the gameplay candidate, and gives the purple arms more length so they read less like side knobs. In the front render, the shoulder color transition still looks mottled, the hands/feet are chunky rounded shapes, and the cap/face lack the reference's material richness. The jump image relies on an external review lift. It is not Unity-imported or attached to the controller/motor and has no platform-contact or gameplay-camera review. Keep it separate; do not promote it or raise the Splat score based on still renders.

Evidence: `../../tools/splat/author_splat_unified_candidate_iter1710.py`; `../../Logs/score-comparison/splat-unified-candidate-iter1710.png`; pose stills under `../../Logs/score-comparison/splat-unified-candidate-iter1710/`.

## Earlier concept studies

![Superseded Iteration 280 study](splat-character-concept-iter280.png)

## Iteration 280 — superseded mapmaker concept study (2026-10-18)

This is a historical study, superseded by the user-approved cap-and-white-body design above. Created `splat-character-concept-iter280.png` as a four-view art direction study. Compared with the prior reference, it replaces the orange ball/star antenna tip and coral boots with a translucent mint navigation leaf and integrated coral feet, adds a softly tapered pear silhouette, introduces subtle hand-painted map-contour markings in the skin, and puts the compass-rose map satchel on the rear so it contributes to the gameplay silhouette. It also keeps the eyes expressive without making them the entire head. The character remains recognizably soft and cute, while having an original mapmaker identity.

This image is only a concept reference. It is not a Unity asset, not a rig, and not evidence that the current Splat improved. Its smooth forms still need an authored, deformation-friendly 3D model and in-game camera review; do not reproduce it as a stack of separate primitives. Core scores remain Graphics **12**, Story/Characters **7**, Fun/Controls **6**, Overall **8**. No Meshy credits were used.

## Iteration 276 — Meshy asset and rig audit (2026-10-18)

The mismatch is now clear. Arrival's `OrganicSplatBuilder` loads the Blender-authored `Splat_Rigged_Candidate_Iter263.blend` visual, and `SplatSkeletonAnimator` supplies procedural poses. The separate imported `Splat_Hero` Meshy visual is disabled and destroyed by `ArrivalMeshyPathDress.UseOriginalSplatVisual()`; the project explicitly rejected it because its shiny, long-limbed first-shot read felt creepy and wrong. Its imported URP prefab has a static `MeshFilter`/`MeshRenderer` setup rather than the player-facing skinned rig. So “use Meshy” alone is not the fix: the new/retargeted model must solve the silhouette and arrive with a usable rig.

The account's saved library has one “Splat Character 3D” image-to-3D task (ID `38338327-5dd1-41ad-b394-9208a1964ffb`), succeeded on 2026-08-28 and reporting 0 consumed credits. It has no rigging or animation result. Its output expired on 2026-08-31; the free download attempt now returns 403, so I cannot compare this mesh with the local Blender source or prove the exact lineage. The old task is a reuse lead, not evidence of a usable rigged character.

The current balance query reports 2,935 monthly credits plus 20 permanent credits, with refresh on 2026-11-10. `meshy_list_usage` is denied with 403 for this account, so the monthly allowance and current-cycle spend are not verifiable. Do not call any paid Meshy generation or rigging tool until the user supplies those two values and confirms the exact operation/model cost; 80% of the allowance must remain an enforceable cap.

**Next character gate:** prefer reusing the saved Splat task only if Meshy can rig its existing output; otherwise prepare a fresh model from this document's turnaround reference. In either route, the acceptance evidence is a Unity-imported skinned character with cohesive cute-mapmaker proportions, a readable front and rear silhouette, grounded walk/run with foot contact, and distinct jump/land/action poses. Do not restore the rejected glossy humanoid as-is. Required credit approval for the smallest reuse probe would be 5 credits for Meshy rigging; a fresh textured `meshy-7.1` image-to-3D model (30 credits) plus rigging (5) would total 35 credits. These are proposed costs, not approved or spent.

## Current in-game assessment — Iteration 264 (2026-10-18)

The runtime builder now prefers the rigged Iteration 263 Blender candidate. The existing eye-white, iris, pupil, and glint meshes sit 0.23m closer to the face; the Unity front capture shows less pale rim but still a plain purple body, thin smile, and basic flippers. The gameplay follow view shows no former pale eye peek, but Splat has little rear-facing inventor identity. The creator's character score remains **7/100** and motion remains unverified in continuous human play.

A trial reused the older authored map sash on the current rig. Its Unity front capture showed a detached horizontal band around the waist; Iteration 265 was rejected and archived in `Logs/score-comparison/splat-candidate-iter265/`. The active model remains Iteration 263. Any mapmaker gear needs to be refit to the current body shape and move with the rig as a coherent, authored detail. Do not add disconnected primitive-looking accessories.

Current front/rear renders: `Logs/score-comparison/splat-current-front-iter263.png` and `arrival-gameplay-rear-iter264.png`. The score review is in `docs/ODYSSEY_SCORECARD_REVIEW.md`; paired game/Odyssey visual evidence is in `docs/ODYSSEY_VISUAL_COMPARISON.html`. Meshy credits remain unused because monthly allowance and current usage are not verifiable. No human movement review has occurred.

The in-game Splat remains 7/100 by the creator's assessment. A cohesive, skinned prototype and FBX exist at `Assets/SpaceAndCrafts/Resources/Characters/Splat_Rigged.fbx`; the shared visual builder prefers it. Unity imported the FBX and skin image, and an actual 16:9 Play Mode capture shows the candidate from behind with two small coral feet beneath the body. From that angle he still reads as a large purple orb with a small antenna tip and teal star badge; the face remains hidden. Blender's front renders hide the repositioned boots, while rear static run poses show one clearly and the far foot only intermittently. Skin deformation and natural gait remain unverified. The images below are Blender design-review renders, not gameplay captures.

### Iteration 199 update — 2026-10-18

The fused surface was reauthored with a tapered crown, broader lower torso, and smaller integrated flippers. The antenna now extends in a visible curve, and the small open smile has a restrained tooth highlight. The latest Unity capture shows a slightly taller, less spherical body from behind, but it still reads as an egg-like orb with its face hidden. The Blender front still lacks visible boots and the facial pieces remain layered, so the creator's 7/100 rating stands. The existing Meshy account model is an unrigged image-to-3D GLB; no rigging or animation tasks are listed. The account balance is 2,955 credits, but the monthly usage endpoint returns 403, so no Meshy generation/rigging credits were spent.

### Iteration 201 update — face simplification and Unity review

The actual Unity close-up showed that the dark raised brows, open black mouth, bead-like cheeks and front chest token made Splat look worried and assembled. The authoring pass removes the brows, uses a small closed curved smile, conforms blush patches to the skin, enlarges the eyes slightly, and removes the front pouch/token while keeping the rear map pack and coral shoes. The new Unity face is calmer and less creepy. Front and rear gameplay captures still show an egg-like body with a large blank forehead and a cluster of separate rear accessories, so the creator's 7/100 rating remains. Splat still needs a distinctive, cohesive silhouette and a clearer mapmaker identity; animation and player-scale readability also need review.

### Iterations 219–221 — fused arm silhouette trials, 2026-10-18

Three Blender-only versions moved small fused-skin arm sweeps to different heights and adjusted the eye-to-pupil ratio. Candidate 219 read as hip knobs beneath the map sash, 220 expanded into long horizontal fins, and 221 still produced small concealed bumps. The front and back previews are preserved under `Logs/score-comparison/splat-candidate-iter219/`, `iter220/`, and `iter221/`; none was imported into Unity or copied over the active FBX. These trials confirm that moving metablob lobes around the same egg body does not establish the requested cute character read. Keep Splat at 7/100; the next art direction needs a distinct whole-body design and actual follow-camera review, while Meshy usage remains unverified and generation stays withheld.

### Rigged candidate preview

![Current rigged Splat candidate, front three-quarter Blender render](splat-rigged-preview/front-three-quarter.png)

![Rigged Splat candidate, backpack view Blender render](splat-rigged-preview/back-pack.png)

## 3D and rig requirements

Use the production gate above as the current acceptance checklist. The older prototype notes below document past assets and observations; where they mention an amethyst body, antenna, map sash, coral boots, teal eyes, or blush, those are superseded design directions and must not be used to redefine the approved Splat.
### Static rig-pose review

![Static Blender run pose of the rigged Splat candidate](splat-rigged-preview/poses/pose-run.png)

![Opposite static Blender run pose of the rigged Splat candidate](splat-rigged-preview/poses/pose-run-reverse.png)

![Static Blender jump pose of the rigged Splat candidate](splat-rigged-preview/poses/pose-jump.png)

These are one-frame Blender authoring poses, not gameplay animation. The front-biased run stills use the runtime animator's maximum 18-degree swing and reverse phase, but hide the rear-shifted boots. The follow-camera run stills below show one foot clearly and the far foot intermittently. Blender's static poses do not establish natural in-game motion.

![Rear follow-camera run pose A](splat-rigged-preview/poses/pose-run-rear.png)

![Rear follow-camera run pose B](splat-rigged-preview/poses/pose-run-rear-reverse.png)

Iteration 189 corrected the UV comparison so the material samples the mesh UV map instead of Blender's generated coordinates. The cylindrical layout removes most front-facing patch boundaries and was promoted into the authoring source and regenerated FBX. The rear seam is hidden from the front and the texture remains smooth across the back. The far boot still needs silhouette and gait work before this prototype is production-ready.

## Asset and budget status

- Reference image: `splat-character-turnaround-concept.png`, generated with the built-in image-generation tool using the existing `SplatMapmakerPortrait.png` as a loose visual reference.
- Current Blender/FBX candidate: `Assets/SpaceAndCrafts/Art/Characters/Splat/Splat_Rigged.blend` and `Assets/SpaceAndCrafts/Resources/Characters/Splat_Rigged.fbx`, with `Assets/SpaceAndCrafts/Resources/Characters/Splat_Skin_Albedo.png`; authoring source: `tools/splat/author_splat.py`. The shared builder now loads this FBX when available and keeps the procedural builder as fallback.
- Unity has current front and rear Play Mode renders. The front view shows a small closed smile, painted blush, and shallower rigged eye layers; the rear view shows coral shoes and the map pack but still reads as an egg-shaped purple body. Continuous skin deformation and natural gait remain unreviewed, so Splat stays 7/100.
- Meshy inspection on 2026-10-18 found one Splat image-to-3D model and no rigging or animation models/tasks. Balance is 2,955 credits (2,935 expiring monthly and 20 permanent), refreshing 2026-11-10. The Usage API returned 403, so monthly allowance and credits used this cycle remain unverified; no generation has started. Do not generate until the missing usage values are available and the 80% limit can be proven.
- The existing glossy humanoid Meshy hero remains unsuitable for gameplay. Do not activate it unchanged.
## Iteration 201 — face simplification review (2026-10-18)

The Unity close-up showed that dark raised brows, an open black mouth, bead-like cheeks and a front chest token made the face read as worried and assembled. The authoring pass removes the brows, uses a small closed curved smile, conforms blush patches to the skin, enlarges the eyes slightly, and removes the front pouch/token while retaining the rear map pack and coral shoes. The current Unity face is calmer and less creepy. The front and rear gameplay captures still reveal an egg-like body with a large blank forehead and a rear accessory cluster; the seven out of 100 rating remains. The model needs a more distinctive, cohesive silhouette and character identity, with Unity animation and player-scale readability still to be reviewed.

## Iterations 203–204 — face integration review (2026-10-18)

Iteration 203 moved blush from raised surface patches into the seamless watercolor body texture, so the color follows the fused skin. Iteration 204 reduced the depth of the white eyes, irises, pupils, and glints while preserving their rig roots for blinking. Unity's current front capture confirms the blush and closed smile remain visible after FBX import; the white eye shapes still dominate, and the rear view still reads as an egg-like orb with backpack and shoes. This improves the face detail without changing the 7/100 character assessment. The next substantial pass should change the full silhouette and character identity, then review the animated FBX in Unity.

## Iteration 206 — silhouette experiment rejected (2026-10-18)

A wider crown, higher eye placement, and fused lateral flipper lobes were regenerated and checked in Blender and in an actual Unity batch Play Mode capture. In Unity, the raised eye whites were pinched by the upper body curvature, while the flippers appeared as round side knobs; the candidate moved away from the requested cute, unified silhouette. Those geometry and face-placement edits were reverted. The rejected game capture is `Logs/score-comparison/splat-shape-candidate-rejected-iter206.png`; the current stable front capture is `Logs/score-comparison/game-current-splat-front-iter206.png`. Splat remains 7/100. Next redesign should be assessed as a whole silhouette with a camera-matched Unity preview before it replaces the stable asset.

## Iteration 211 — tapered-flipper and open-smile attempt rejected (2026-10-18)

A second authored test rotated elongated fused ellipsoids downward for paddle-like arms, increased the pupil proportion, and opened the smile with a small tongue. The Blender front three-quarter render still read as uncanny: the arms became pronounced side knobs and the eyes remained overly mechanical. The test was rejected before Unity import. The authoring script, version marker, FBX, blend, skin texture, and previews were restored from the stable design. The Blender candidate remains at `Logs/score-comparison/splat-shape-candidate-rejected-iter211.png`; it is not the active asset and has no player-facing effect. Splat remains 7/100. This failed test reinforces that the next design needs a fundamentally different, art-directed whole-body concept rather than another proportional tweak.


## Iteration 301–302 — rejected fused-body proportions study (2026-10-18)

The fused metaball skin avoids the obvious separate-primitive assembly, and the existing authored rig receives explicit arm/leg/body weights. However, the rendered shape remains a smooth pear/blob with blunt side flippers, small feet, layered eyes and a very thin mouth. The rear map color panel is low-detail and blurry. Iteration 302 makes the eye group smaller and seats it farther into the face; it softens the initial startled look but does not deliver enough expressive appeal or maker identity. Keep both candidates isolated; neither is ready for Unity integration. Blender stills are not evidence of skin deformation or natural motion. See candidate previews under `splat-candidate-iter301/previews/` and `splat-candidate-iter302/previews/`.



## Iteration 305 — painted face on continuous skin (2026-10-18)

The face study removes the protruding eye and smile meshes and paints warm eye whites, teal irises, deep plum pupils, small highlights, muted cheek color, and a curved smile into the fused body's vertex-color layer. Its isolated front render reads friendlier and less assembled. A soft teal map panel remains on the back. The pear-shaped torso and blunt side limbs still lack a distinct maker silhouette; this is an art-direction improvement, not the finished hero. The source keeps the existing skeleton and assigns arm/leg/body groups, but no Unity import, posed deformation, animation, or gameplay review occurred. Keep it isolated pending shape refinement and rig review. See `splat-candidate-iter305/previews/`.


**Rig data check, Iteration 305:** saved Blender candidate has one 124,300-vertex skin and an Armature modifier bound to `Splat_SkinRig`. Arm groups each cover 5,352 vertices above 0.35 weight, leg groups 8,961 each. No separate eye, iris, pupil, glint, or smile objects remain. This is structural evidence only; posed deformation and animation quality are unknown.

## Iterations 306–310 — rig-weight and antenna pose review (2026-10-18)

An isolated Blender pose review showed the first painted-face candidate pinched when its one-bone arms rotated. Iteration 307 split each arm into upper and flipper-tip bones; Iteration 309 tapered influence near the shoulder and hip, producing cleaner run-scale deformation. Iteration 310 added an Armature modifier to the leaf, whose Bone_Antenna group had previously gone unused, and reduced the jump arm lift for the fused flipper shape. The front, rear, runtime-scale run pose, and compact jump pose are in `splat-candidate-iter310/previews/`. These are rendered stills, not animation or gameplay evidence. The candidate is not in Unity, the body remains a generic pear shape, and the natural gait is unknown. Keep it isolated pending a stronger silhouette and actual skinned animation review.

