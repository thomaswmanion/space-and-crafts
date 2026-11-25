# Space & Crafts — iterative quality goal and desk-review prompt

## Goal

Build Space & Crafts into a distinctive, polished 3D action-adventure for families, with the clarity, craft, responsiveness, and presentation quality associated with top-tier Nintendo platform adventures. Preserve the 90/100 Odyssey-relative acceptance target. Do not claim that target is reached until the evidence and scorecard support it.

The game centers on teenage alien inventor Splat and an original, motivating story. World 1 is the Rainbow Kingdom; World 2 is the Beeping Caves Kingdom; Leaf Lands (first reached as the leafy Magnet Marsh) and Mountain Lands remain later destinations. The Caves' two-note beeps must have visible pulse-ring cues so route information is never audio-only. The core loop must read and work as **explore → gather → build an invention → use it to traverse or solve a problem → test it against a boss**. Preserve responsive controls and camera, cohesive readable world art, safe connected traversal and fall recovery, a deliberate low-clutter HUD, and clear sound/VFX feedback. Splat's approved design is a cute white ball body, black facial features, purple limbs, and backwards cap. Prioritize natural readable idle/run/jump/landing transitions and planted feet; enemies need expressive idle, locomotion, anticipation, attack, hit, defeat, and recovery.

## The five-minute desk score (no play session needed)

This is the default quick check. It scores the quality demonstrated by the latest project evidence, without launching the game or waiting for someone to play. Reuse existing evidence; never create a screenshot just to fill a row.

1. Record the date, enabled scene/flow, and latest evidence cutoff. Read the newest scorecard entry and TODO. Inspect only the most relevant recent capture plus one relevant source/audit/log; follow a link only when the evidence is unclear.
2. Keep all nine category rows and weights fixed. For each row, either (a) retain the last score and mark it **stale** if no comparable new evidence exists, or (b) write a provisional **desk estimate** supported by the enabled-game evidence. Give each row a confidence grade (**High** = repeatable ordinary runtime/player-camera evidence; **Medium** = partial/staged capture or focused automated trace; **Low** = source/configuration only), and one short evidence link. A row marked stale keeps its previous score; it does not imply a new assessment.
3. Show the weighted total to two decimals and one decimal, plus evidence coverage (for example, `4/9 current; 5/9 stale`). This is a **carried-forward desk estimate** when any rows are stale. If a category has never had a basis, mark it **N/A** and call the total partial. Do not change the weights or omit a weak category to improve the total.
4. State the strongest current blocker and choose one checkable next improvement. If no new evidence or meaningful change exists, record **no score change** and stop; don't manufacture a task.

Use the shared anchors below consistently: **0–19** absent/placeholder or severely blocked; **20–39** early prototype with major gaps; **40–59** recognizable but inconsistent/incomplete; **60–74** coherent and mostly dependable; **75–89** polished and distinctive with limited demonstrated gaps; **90–100** exceptional across broad, ordinary-context evidence. A screenshot can support a visual judgment; source can support wiring/configuration; neither proves fun, comfort, human input feel, comprehension, difficulty, or timing. Without human evidence those qualities are **not assessed**, even when the nine-category desk estimate can still be calculated. The desk score tracks demonstrated progress; it never certifies the 90/100 acceptance target.

## The repeatable quality loop (quick to run, honest about what it proves)

At every iteration, use this short loop before doing more work:

1. **Take a snapshot without playing:** read the latest scorecard and TODO, identify the enabled scene/flow and source revision, then inspect the smallest relevant set of existing screenshots, short capture sequences, Unity scene/prefab references, automated logs, and build records. Reuse evidence; do not launch or control the user's PC merely to score the game.
2. **Score what the artifacts show:** keep the same nine categories and weights below. Call new judgments **desk estimates**. For reassessed rows give a score, evidence grade, one evidence link, and the clearest demonstrated strength and gap. For rows without comparable evidence, carry the prior score and mark it **stale**; use **N/A** only when the category has never had a basis. Never use asset counts or concept art as a proxy for enabled-game quality.
3. **Make uncertainty visible:** state the age and type of evidence (source audit, automated trace, staged/editor capture, build, or human play). Without a human session, mark fun, comfort, input feel, comprehension, difficulty, and human timing **not assessed**. Screenshots can show composition and animation poses; they cannot prove feel, fairness, fun, or that players understand the objective.
4. **Pick one next fix:** choose the highest-impact evidenced problem affecting Splat, the main loop, traversal, clarity, or stability. Write one observable pass/fail check before editing. A good check is specific and repeatable, such as: “In three consecutive ordinary-camera jump/land captures on two platform heights, both soles meet the surface, no body stretch exceeds the agreed limit, and the landing state returns to idle.”
5. **Change, check, and log:** make one bounded improvement, run only the relevant available check, and record its result—including failure or “not run.” Update TODO and the top scorecard entry with the cutoff, changed files, evidence links, score movement (or why unchanged), remaining unknowns, and one next action.

### Quick rating rule

Use the shared 0–100 anchors and fixed weights below. Pick the lowest band fully supported by the evidence, then move within that band only when a cited observation justifies it. The total is `sum(category score × category weight)`; show the raw value to two decimals and the display value to one decimal. If a row is stale, retain its previous rating and show that its evidence is stale; if a row is N/A, label the total partial. **Do not lower a rating simply because evidence is missing, and do not raise one because work was done.** Keep the score stable until comparable evidence of the enabled player-facing result supports a change.

This makes a useful score possible without playing: it is a quick, evidence-linked **desk estimate**, not a claim about the whole experience. Only human play can settle player feel, fun, comprehension, comfort, and difficulty. Keep the **90/100 Odyssey-relative target** as a goal, never a forecast or badge; it requires broad evidence across the game plus human-play evidence for those human-only qualities.

### Copyable one-minute review card

```text
Cutoff / enabled slice / source revision:
Desk estimate: __/100 (raw __.__); previous __; coverage: __/9 current
Rows (score / current-or-stale / confidence / evidence): Splat __ | World __ | Animation __ | Controls-camera __ | Combat __ | Story-loop __ | HUD __ | Sound-VFX __ | Stability __
Rows changed: __ (or none; reason __)
Human-only: fun __ | comfort __ | input feel __ | comprehension __ | difficulty __ | timing __
Strongest evidence-backed blocker:
Next acceptance check (write before editing):
Change + files (or no change):
Check result: pass / fail / not run — evidence link:
Remaining unknowns:
Next single action:
```

If there is no meaningful change or no new evidence, say **no change** and keep the prior scores. This is a valid iteration; inventing progress is not.

## Progress accountability (the score is a tracking instrument, not a victory badge)

Keep one stable nine-category scorecard and one short iteration record. At every review, say what the score measures, what evidence is new, what remains unknown, and which concrete change is next. The target remains **90/100**, but neither effort, asset count, concept art, nor a successful build earns points by itself. Do not rewrite prior evidence or quietly reset the baseline.

For routine wakes, the five-minute desk score is enough: reuse the latest artifacts, carry old rows forward as stale, report current-vs-stale coverage, and select one evidenced fix. Do not force a full rescore or a code change when the evidence says no change. Reserve a full iteration for a meaningful change, a new comparable capture/trace, or a scheduled broader review.

### Easy no-play review: 10 minutes, no game session required

Use the latest existing evidence; do not launch or control the user's PC just to get a score. A no-play review can still assess visible composition, project wiring, authored state coverage, reproducible automated traces, build records, and saved captures. It cannot assess fun, comfort, human input feel, comprehension, difficulty, or timing; mark those **not assessed**.

1. **Set the cutoff and slice:** record today's date, exact enabled scene/flow, current source/build identity if available, and the newest evidence date. Label each artifact as source/config, automated trace, staged/editor capture, build, or human play. Stale evidence stays visibly stale.
2. **Fill the same nine rows:** give each a 0–100 **desk estimate**, confidence grade, one cited positive observation, one strongest demonstrated gap, and one next improvement. If the evidence cannot support a new judgment, carry the previous score forward and mark it **stale / not reassessed**—do not invent precision. If a category has never been reviewed, use **N/A**, and label the total partial.
3. **Calculate, don't eyeball:** use the fixed weights below and `sum(score × weight)`. Show the raw weighted total to two decimals and displayed total to one decimal. Keep confidence separate from score. A low confidence score is not a reason to force a lower rating; missing evidence is a reason to mark the row stale.
4. **Choose exactly one next quality slice:** prioritize (impact on Splat/core loop/traversal/stability) × (strength of evidence for the gap) ÷ (scope/risk). Before editing, write one observable acceptance check, e.g. “at the ordinary camera scale, three consecutive jump-land samples show the feet on the platform and no extreme body stretch.” Avoid adding another asset when the blocker is integration, animation, contact, readability, or stability.
5. **Close the record:** after the change, cite the least costly relevant check and its result. Record failures and contradictory evidence too. Rescore only the categories directly supported by comparable new evidence; otherwise state **no score change**. Update TODO.md and the top of docs/ODYSSEY_SCORECARD.md with the evidence cutoff, score delta, remaining unknowns, and one next action.

### Accountability gates

- **Evidence gate:** every score must point to an artifact or be explicitly marked stale. Keep “what exists in the project” separate from “what is enabled in the reviewed game slice.”
- **Change gate:** every iteration names one player-facing blocker, one acceptance check, the changed files, and whether the check passed, failed, or was not run. If nothing meaningful changed, say that; don't pad the log.
- **Score gate:** only comparable evidence of changed enabled-game quality can move a category. A source edit, imported asset, staged hero shot, or build success alone is not a player-facing quality gain. A failed check may lower a score if it establishes a real regression/blocker.
- **90-point gate:** 90/100 is not reached until the scorecard is broadly supported across all nine categories, relevant edge cases and ordinary gameplay context, and human-play evidence establishes feel, comprehension, and fun. A desk estimate may track progress toward 90; it cannot certify the target.
- **Unknowns gate:** list missing play, capture, audio, runtime, or platform evidence explicitly. Do useful source/artifact review while waiting; never present unknown as pass.

### Asset pipeline gate (to prevent concept accumulation)

When considering a new boss, monster, ally, or craft-themed decoration, first search the existing roster, Meshy outputs, scene references, and asset READMEs. State the unmet game need the new asset would serve (world, encounter, quest, invention loop, or workshop storytelling) and why an existing asset cannot fill it. Prefer improving or integrating a strong existing candidate when that is the more direct quality gain. If a new concept is justified, save one concise production brief with silhouette/material cues, target role, required rig/animation, intended Unity destination, and acceptance checks. Keep concept, generated model, rigged/animated model, imported candidate, enabled content, and verified player-facing content as distinct readiness states; never count one state as another or award score for an unused asset.

Before a paid Meshy task, verify the connected balance and exact task cost; verify allowance/usage too whenever the spending cap requires them. Record the pre/post balance, task ID, task status, cost, and downloaded artifact path. If the connected balance is zero, account identity is uncertain, or a required spend value is unavailable, submit no task and make progress through roster triage, prompt preparation, or integration of existing assets. Do not treat an unreconciled website balance as available credits.

### Tiny review template

```text
Evidence cutoff / enabled slice:
Overall: __/100 desk estimate (raw __.__) | previous __ | evidence coverage: __/9 current
Human-only: fun __; comfort __; input feel __; comprehension __; difficulty __; timing __
Biggest evidenced blocker:
One next fix + pass/fail check:
What changed / evidence / what was not verified:
Scores changed: __ (or none, because __)
```

The scorecard is a compact decision tool: it should make progress and stagnation equally visible. Keep the detailed proof in small linked captures/logs, not a sprawling narrative. Do not claim the game improved just because work was done.

## How to run each iteration without a play session

Do one evidence-led desk review per iteration. Inspect the current project and active build scenes, source, serialized scene/prefab wiring, imported assets/rigs/clips, recent captures, audit/smoke/build logs, TODO.md, and the latest scorecard entry. Use existing project tools to capture representative player-camera images or deterministic traces when available. Do not use the user's PC UI unless explicitly authorized for that task. A screenshot, source audit, scripted smoke, and build each prove different things; label them accurately.

### Fast desk pass when nobody can play

Reuse the newest reliable evidence first; do not create a new build or capture just to fill a slot. A compact review can be done from up to five evidence groups:

1. **Splat:** ordinary-distance front/side gameplay captures and any authored/rigged animation review.
2. **World and contact:** a connected route view, platform approach/landing views if available, and the scene/collider/fall-recovery audit.
3. **Enemy and combat:** ordinary-camera tell, attack, hit, defeat/recovery captures plus state/clip wiring and the latest scripted combat trace.
4. **Progression:** evidence for exploration, gathering, invention build, actual use, boss test, outcome and restart/progression state.
5. **HUD, audio, and technical:** menu/HUD captures at intended aspect ratios, audio files or a labeled mix capture, and the latest relevant build/audit/smoke logs.

If a group has no current evidence, record that gap; do not fabricate a screenshot or infer behavior from a concept. For a quick pass, apply the shared score anchors directly: select the band the evidence supports, start at its lower end, and move upward only when a cited observation warrants it. Reuse the previous score if no new player-facing evidence changes the judgment. Record the evidence cutoff date so old captures cannot masquerade as the current build.

Copy this nine-row desk-review card into each new scorecard entry. `Previous → current` makes score drift visible; `Evidence` should link to a small capture/log/source audit, not a folder of unreviewed files. A category without enough evidence stays at its prior rating and is marked **stale / not reassessed**; a genuinely new category with no basis is **N/A**, and the overall score must then be labeled partial rather than game-wide.

| Category | Previous → current | Evidence grade | Evidence link(s) | Strongest observed result | Largest demonstrated gap | Next concrete improvement |
|---|---:|---|---|---|---|---|
| Splat design/presentation (18%) | | | | | | |
| World/level/readability (14%) | | | | | | |
| Animation/grounding/contact (14%) | | | | | | |
| Controls/camera (14%) | | | | | | |
| Combat/enemy behavior (12%) | | | | | | |
| Story/progression/loop (12%) | | | | | | |
| HUD/menus/usability (6%) | | | | | | |
| Sound/VFX (5%) | | | | | | |
| Stability/performance (5%) | | | | | | |

This is a desk estimate, not a substitute for play. In particular, no-play reviews must mark fun, comfort, input feel, player comprehension, difficulty, and human timing **not assessed**. A high technical score cannot compensate for missing player-facing evidence, and concepts or unused Meshy assets do not raise production/gameplay scores.

1. **State the slice.** Name the exact scene, character, encounter, route, or flow reviewed and the current evidence cutoff/iteration. Check that the feature is actually enabled/referenced by the intended build; distinguish active content from unused assets and concepts.
2. **Use a compact evidence pack.** Prefer a small fixed set of ordinary gameplay-camera captures (start/readability, traversal/contact, interaction or combat, outcome/recovery) plus relevant source/scene checks and the latest automated evidence. Record capture viewpoint and whether movement/input was editor-staged, synthetic, player-window, or human-driven. Never call a staged preview a playtest.
3. **Score the same nine categories every time** using the weights below. For each, keep a 0–100 score, evidence grade (**Low / Medium / High**), one or two direct evidence links, a short observed strength, the largest demonstrated weakness, and the next specific improvement. Preserve category definitions and weights between iterations. Calculate overall as `sum(category score × weight)` and show one decimal plus the unrounded value. Current baseline: **33.08/100 (33.1 displayed)**; do not silently reset it.
4. **Choose one high-impact next slice.** Rank candidate fixes by player-facing impact, evidence that the problem exists, and likely scope. Prefer fixing the biggest visible/functional blocker over adding another concept or polishing a detail. Define the observable before/after signal before editing.
5. **Make and document the change.** Update source/assets plus TODO.md and the top/latest section of docs/ODYSSEY_SCORECARD.md. Record what changed, exact files, captures/logs, what passed or failed, what was not checked, score movement (or why it stays unchanged), and the next ranked action. Keep iteration notes concise; link supporting detail instead of duplicating large logs.
6. **Rescore conservatively.** Increase a category only when new, direct evidence demonstrates a meaningful player-facing improvement in that category. Source edits, concept art, a successful build, or more objects alone do not prove better play or appeal. Reduce a score when evidence exposes a regression. Otherwise retain it. Never change category weights to make the overall number look better.
7. **Close the loop.** Check that the final capture actually shows the intended improvement at ordinary gameplay scale and that the applicable audit/smoke/build still passes when performed. If it looks unchanged, is ambiguous, breaks another route, or fails verification, say so, revert or fix it, and do not award points.

## Fixed category weights

| Category | Weight | Desk-review focus |
|---|---:|---|
| Splat's design and presentation | 18% | Approved silhouette, proportions, face readability, cohesion, visibility at gameplay scale, and whether the character feels expressive in evidence. |
| World art, level design, and visual readability | 14% | Distinctive composition/materials, route readability, landmark scale, coherent environments, safe connected paths, optional routes, and fall recovery. |
| Character/enemy animation, grounding, and contact | 14% | Correct rig/clip bindings, transition coverage, motion range, visible poses, foot/platform contact, squash/stretch control, and enemy tells/reactions. Separate genuinely joint-driven clips from whole-mesh transforms or pose overlays. |
| Controls and camera | 14% | Input/action wiring, movement/jump/landing traces, camera framing/occlusion, and consistency. Source or synthetic input cannot establish human feel or latency. |
| Combat and enemy behavior | 12% | State transitions, anticipation, attack windows, hit/defeat/recovery, counters, feedback, fairness, and repeatability. Scripted contacts do not establish human timing/readability. |
| Story, progression, and exploration-to-boss loop | 12% | Goal clarity, route and discovery evidence, gathering, invention construction, useful traversal/problem-solving, boss test, reward, and restart/progression state. |
| HUD, menus, and usability | 6% | Hierarchy, clutter, legibility at target resolutions/aspect ratios, contextual prompts, objective updates, and menu flow. Captures do not establish accessibility or comprehension by players. |
| Sound and visual feedback | 5% | Presence, timing, distinction, mix/readability of sound and effects for pickup, crafting, movement, hits, rewards, and transitions. Require audio evidence to judge sound. |
| Technical stability and performance | 5% | Reproducible build, errors/warnings, automated coverage, load/restart behavior, frame/performance evidence, and asset/import health. A successful build alone is not proof of runtime stability. |

## Shared 0–100 anchors

- **0:** absent, unusable, or broken in the reviewed slice.
- **1–19:** placeholder or severe blockers dominate; intent is difficult to read.
- **20–39:** early prototype; partial function or identity, with major visible/functional weaknesses.
- **40–59:** recognizable and partly dependable, but inconsistent, generic, or incomplete.
- **60–74:** coherent and mostly dependable; remaining issues are apparent in ordinary evidence.
- **75–89:** polished and distinctive across the reviewed slice; only limited gaps remain, with relevant behavior corroborated by more than one evidence type.
- **90–100:** exceptional and consistently convincing at a quality comparable to the stated Odyssey reference; requires broad direct evidence across normal routes, edge cases, presentation, and player-facing behavior—not a single showcase frame.

These anchors are not automatic point formulas. Explain the judgment using observed evidence. A category may be scored provisionally without playing, but must say **desk estimate** and use a lower evidence grade when player feel, appeal, comprehension, timing, or sustained behavior has not been observed.

## Evidence and no-play rules

- **Source/serialized scene review** can establish references, configuration, and potential wiring; it cannot establish what renders or feels good.
- **Automated audit** can establish only the assertions it actually checks. Report gaps and flaky/failed runs.
- **Scripted smoke/synthetic input** can establish a reproducible path through selected mechanics; disclose teleports, forced states, scripted aim, generated devices, retries, and other assistance.
- **Editor/player capture** can establish visible composition for that frame/sequence and the stated camera/input path; it does not prove ordinary discovery, delight, usability, fun, or human control feel.
- **Build log** can establish build completion for that configuration; it does not establish a working launch or performance unless those were observed separately.
- **Human play/usability evidence** is needed for claims about fun, comfort, clarity, input feel, difficulty, or whether the intended audience understands the loop. If no human session occurred, write **not assessed** rather than infer.
- Record failures and contradictory evidence beside successes. Do not cherry-pick only the clean retry or attractive frame.
- Concepts and unintegrated assets do not raise player-facing category scores. Meshy credit availability is separate from quality progress; never generate when spending rules/account usage cannot be verified.

## Copyable iteration request

### Fast, no-play score pass (about 10 minutes)

Use this when nobody can play or time is short; it is a valid progress check, not a substitute for play. Reuse the latest reliable artifacts and do not launch or control the user's PC:

1. Read the latest scorecard entry and TODO.md. Set an evidence cutoff date and identify the exact enabled scene/flow being reviewed.
2. Open only the newest representative captures/logs and inspect relevant scene/prefab/asset wiring. Prefer ordinary gameplay-camera captures. Confirm assets are referenced by the enabled slice before counting them.
3. Fill the same nine-row card below. For each category, write one directly observed strength, one demonstrated weakness, the evidence link, an evidence grade, and the next fix. Label the scores **desk estimates**. Mark human-dependent qualities as **not assessed**.
4. Keep each previous score unless comparable before/after player-facing evidence shows a real change. A source edit, concept, newly imported asset, or passing build is not a score increase by itself. If evidence is missing or stale, keep the old score and mark it **stale / not reassessed**; do not guess a new number.
5. Recalculate the weighted total with the fixed weights: `SUMPRODUCT(scores, weights)`. Show the raw result to two decimals and the display result to one decimal. If any category is N/A, call the total **partial** and do not compare it as a full-game score.
6. Choose one next improvement by asking: (a) is the blocker visible or reproducible in current evidence, (b) does it affect the main character, core loop, traversal, or stability, and (c) can the change be checked? Fix the strongest evidenced blocker first; do not add content merely to make the log look busy.
7. Update TODO.md and the top of docs/ODYSSEY_SCORECARD.md with the score delta (or why it stayed flat), the exact change/evidence, failed checks, limitations, and one next action. Never claim a build, test, render, or play session unless it happened.

### Simple score-change rule

Score the result visible in the reviewed, enabled game slice—not effort or potential. A score increase requires a comparable capture, trace, or other direct evidence showing the improvement in ordinary context; cite both sides when available. A score decrease requires evidence of regression or a newly exposed blocker. When evidence is mixed, stale, or isolated from the gameplay context, retain the prior score and describe the uncertainty. No-play reviews must explicitly say that fun, comfort, input feel, player comprehension, difficulty, and human timing are **not assessed**. Keep confidence/evidence grade separate from score: low confidence is not permission to invent a low or high number.

### Copyable quick-review request

> Do a no-play Space & Crafts quality iteration from `docs/GAME_QUALITY_ITERATION_PROMPT.md`, including its progress-accountability gates. Inspect the latest enabled game slice, TODO.md, and docs/ODYSSEY_SCORECARD.md; reuse recent captures, audits, smoke/build logs, and source/scene wiring. Do not control the PC UI or claim evidence you did not produce. Fill all nine fixed score rows using existing weights, scores, evidence grades, and links; mark stale rows honestly and human-only qualities not assessed. Label the result a desk estimate (partial if any category is N/A) and calculate the weighted total exactly. Name the biggest evidenced blocker, write one observable acceptance check before editing, implement the single highest-impact verifiable fix, then record whether the check passed, failed, or was not run. Update TODO.md and the top scorecard entry with cutoff date, exact change/evidence, score movement or reason unchanged, limitations, and one next action. Keep the 90/100 target active; only broad player-facing evidence plus human-play evidence for feel, comprehension, and fun can certify it.

### Full iteration request

> Continue the Space & Crafts quality goal using this document and its progress-accountability gates. Inspect the current project, enabled slice, latest captures/logs, TODO.md, and docs/ODYSSEY_SCORECARD.md without controlling the PC UI. Score all nine fixed categories with the existing weights, evidence grades, and weighted formula; retain ratings unless comparable direct evidence warrants a change. Separate source/audit, automated smoke, staged capture, build, and human-play evidence; mark human-only qualities not assessed if nobody played. Identify the strongest evidenced blocker and define one observable acceptance check before making the single highest-impact verifiable improvement. Capture/check at ordinary gameplay scale where possible, then report pass/fail/not-run candidly. Update TODO.md and the top scorecard entry with evidence cutoff, links, exact changes, score movement or why unchanged, remaining unknowns, and one next action. Never claim builds/tests/renders/play unless performed. Keep the 90/100 Odyssey-relative target active until its evidence gates are met.
