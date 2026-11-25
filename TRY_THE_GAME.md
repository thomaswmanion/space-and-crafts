# Try Space & Crafts

## Fresh stability check — November 12, 2026

The current source rebuilt successfully with Unity 6000.5.4f1 as a 488 MB windowed player. Evidence: `Logs/user-requested-stability-rebuild-retry-20261112.log` (`Build Finished, Result: Success`). The first batch attempt stopped during editor startup; the clean retry completed. Unity MCP prints an editor-shutdown assertion after the successful build.

The latest scripted Arrival smoke reached restart and passed checkpoint/fall recovery, movement/jump, camera follow/orbit/collision, coyote/buffered jumps, gathering/crafting, Boing-O-Matic launch, boss completion, and restart. It still exits **failed** on seven Splat presentation/input checks (refused-build reaction, near-miss pickup assist, spring reaction, component discovery/eye expression, hidden-wonder reaction, and gateway listening pose). Evidence: `Logs/user-requested-arrival-stability-smoke-20261112.log`. The rendered Arrival reference `Logs/score-comparison/game-current-splat-feet.png` contains the level and HUD, but it predates this rebuild and is not a capture of your Unity window. I did not inspect the visible Editor/Game view or perform human play, so the reported blank display is not confirmed fixed on your machine.

To try World 1 from Hub, open this project, wait for compilation to finish, then choose **Space & Crafts → Play Arrival (Visual)** in the Editor. Hub's **Open** button only opens the Editor; it does not start the game. For the title, choose **Space & Crafts → Play Game (Home Menu)** / **Ctrl+Shift+R**. Or double-click `Play Space & Crafts.cmd` for the standalone player. If Play is highlighted but the screen is blank, switch to **Window → General → Game**.

## Quick start from Unity Hub — November 12, 2026

Unity Hub is only the launcher; its **Open** button opens the Editor and does not start the game. Open the `space-and-crafts-3` project at `E:\Users\thoma\git\space-and-crafts-3` (not its older sibling `space-and-crafts`), wait for import/compilation, then use **Space & Crafts → Play Arrival (Visual)** for Rainbow Kingdom World 1. This command loads the scene, enters Play Mode, and selects the Game view. For the title, use **Space & Crafts → Play Game (Home Menu)** or **Ctrl+Shift+R**. If you press the toolbar Play button while Unity has restored only its default camera/light scene, the editor startup hook now replaces that empty stub with Home Menu without a save prompt; authored scenes still use Unity's save guard. If Play Mode is active but another tab is visible, choose **Window → General → Game**. You can also double-click `Play Space & Crafts.cmd` to try the existing standalone player.

**Fresh rebuild for this report:** Unity 6000.5.4f1 completed a 488 MB Windows player build. Evidence: `Logs/user-requested-hub-tryout-20261112.log`. For the least ambiguous tryout, double-click `Play Space & Crafts.cmd`; to run inside the Editor, choose **Space & Crafts → Play Arrival (Visual)** after compilation. The build succeeded, but I did not inspect a visible Unity/Game window, so the reported blank view is not visually confirmed fixed.

**Latest tryout check (November 12, 2026):** the current editor scripts compiled and Unity reported a successful 488 MB Windows player build in `Logs/user-requested-stability-refresh-20261112.log`. The Arrival scripted smoke confirms checkpoint/fall recovery, movement/jumps, camera follow/orbit/collision, crafting, invention launch, boss completion, and restart. It still fails seven Splat reaction/near-miss pickup checks; see `Logs/user-requested-arrival-smoke-20261112.log`. This verifies the scripted main route, not a visible Unity Hub/Game window or human play. If the exact blank view persists after using **Play Arrival (Visual)**, the editor's Game tab and Console are needed to diagnose that machine-specific state.

**Latest stability refresh (November 12, 2026):** I rebuilt the windowed Windows player from this project with Unity 6000.5.4f1. `Logs/user-requested-unity-hub-stability-20261112.log` reports **Build Finished, Result: Success** (488 MB) and no C# compile errors. Unity MCP prints an asset-editing assertion during batch shutdown after the successful build; the build result remains successful. I did not open the player or inspect the visible Game view, so the original blank display is not visually confirmed fixed.

- Unity 6000.5.4f1 completed a fresh windowed Windows player build from this project: `Logs/user-requested-unity-stability-refresh-20261112.log` reports **Build Finished, Result: Success** (488 MB; no C# compile errors). Unity's MCP package emits an assertion during batch shutdown after the successful build.
- The latest scripted Arrival smoke is `Logs/user-requested-arrival-stability-smoke-20261112.log`. Checkpoint/fall recovery, movement/jump, camera follow/orbit/collision, buffered/coyote jumps, gathering/crafting, invention launch, boss completion, and restart passed. Seven Splat feedback/pickup checks still fail: refused-build reaction, spring reaction persistence, near-miss pickup, component-specific reactions/eye expression, hidden-wonder reaction, and post-boss listening pose.
- This verifies a successful build and substantial gameplay systems, not a visible Unity Game view or a human play session. I did not interact with the PC UI, so the reported blank display is not visually confirmed resolved. The startup hook loads the Home Menu for empty/recovery scenes and the custom World 1 play command selects Game view, but use the steps above to start Play Mode.

## Unity Hub blank Game view follow-up — November 12, 2026

- Fixed one startup edge case in `GameStartSceneBootstrap`: Unity's `Temp/__Backupscenes` scene is now classified by its contents even when marked dirty. If it contains only Unity's default camera/light, the fallback routes to Home Menu and uses Unity's existing save/cancel prompt before replacing it. Authored content in a recovery scene is still preserved.
- Refreshed the Windows player from this `E:` project with Unity `6000.5.4f1`. `Logs/user-requested-unity-stability-final-20261112.log` reports **Build Finished, Result: Success** (488 MB); no C# compile errors. Unity MCP still prints an editor-shutdown assertion after the successful build.
- Re-ran the Arrival Play Mode smoke immediately before the final Editor-only recovery-guard edit. Movement, camera, checkpoint/fall recovery, buffered/coyote jumps, craft completion, invention launch, boss completion and restart passed. It still **fails seven Splat behavior/presentation checks**: refused-build reaction, spring reaction persistence, near-miss pickup assist, component-specific reactions, eye expression, hidden-wonder reaction, and post-boss listening pose. Evidence: `Logs/user-requested-unity-stability-smoke-20261112.log`. The final guard edit was included in the subsequent successful Unity build.
- To actually start World 1 from Unity Hub: open the project, wait until import/compilation finishes, then in the **Unity Editor** choose **Space & Crafts → Play Arrival (Visual)**. Hub's **Open** button opens the Editor; it does not start the game. The command loads Rainbow Kingdom and switches to the Game view. For the Home Menu use **Space & Crafts → Play Game (Home Menu)** / **Ctrl+Shift+R**; for the standalone player, double-click `Play Space & Crafts.cmd`.
- I did not interact with the Editor UI or inspect a visible player window. The dirty-backup fix compiled and the player build succeeded, but the user's exact blank display is **not visually confirmed resolved**.

## Latest Unity Hub recovery check — November 12, 2026

- Refreshed the Windows player with Unity 6000.5.4f1. `Logs/user-requested-unity-stability-build-20261112.log` ends with **Build Finished, Result: Success** (485 MB); no C# compiler errors were reported. The player data directory was refreshed.
- Re-ran the Arrival Play Mode smoke against the current source. Checkpoint/fall recovery, movement/jump, camera follow/orbit/collision, coyote/buffered jumps, pickup sparkle, gathering/crafting, invention launch, boss completion, and restart passed. It still fails seven Splat response/pickup checks: refused-build reaction, near-miss pickup, spring reaction persistence, component-specific discovery reactions, eye expression, hidden-wonder reaction, and Vesper listening pose. Evidence: `Logs/user-requested-unity-arrival-smoke-20261112.log`. This is enough to try the main loop, but not a clean gameplay pass.
- In Unity Hub, **Open** opens the Editor; it does not run the game. After the project finishes importing, choose **Space & Crafts → Play Arrival (Visual)** for Rainbow Kingdom, or **Space & Crafts → Play Game (Home Menu)** / **Ctrl+Shift+R** for the title. If Play is active but the window still appears empty, select **Window → General → Game**.
- The startup fallback that replaces Unity's default/recovery scene is present in `GameStartSceneBootstrap.cs`. I did not inspect a visible Editor/player window or human-play, so I cannot confirm the exact blank display is fixed on your machine. If these steps still show blank, a screenshot of the Editor with Game and Console visible will identify whether it's a tab/layout or runtime error.

## Fresh launch check — November 12, 2026

I rebuilt the latest project with Unity 6000.5.4f1. `Logs/user-requested-stability-build-20261112-final.log` reports **Build Finished, Result: Success** (485 MB). The Arrival Play Mode smoke passes checkpoint/fall recovery, movement and jumping, camera follow/orbit/collision, coyote/buffered jumps, gathering/crafting, invention launch, boss completion and restart. It **fails overall** on seven Splat reaction/pickup assertions (refused-build reaction, near-miss pickup, spring reaction persistence, component discovery reactions/eye expression, hidden-wonder reaction, and gateway listening pose); see `Logs/user-requested-arrival-smoke-20261112-final.log`.

Unity Hub's **Open** button only opens the Editor; it does not run the game. To see World 1, wait for Editor import/compilation, then choose **Space & Crafts → Play Arrival (Visual)**. For the title/menu, use **Space & Crafts → Play Game (Home Menu)** or press **Ctrl+Shift+R**. For the standalone player, double-click `Play Space & Crafts.cmd` in the project folder. If Play Mode is active but the view is blank, choose **Window → General → Game**.

The player build and scripted smoke completed, but I did not inspect a visible Editor/player window. The user's exact blank display is therefore **not visually confirmed fixed**; the seven Splat feedback issues also remain open.

## Fastest route from Unity Hub

Unity Hub's **Open** button only opens the Unity Editor; it does not start the game. After the project finishes importing and compiling:

1. In Unity's top menu, choose **Space & Crafts → Play Arrival (Visual)**. This opens Rainbow Kingdom World 1, starts Play Mode, and selects the Game view.
2. To start at the title instead, choose **Space & Crafts → Play Game (Home Menu)** or press **Ctrl+Shift+R**.
3. If Play Mode is active (the toolbar Play button is highlighted) but the view is still blank, choose **Window → General → Game**.

The project also has a startup fallback for Unity's empty default/recovery scene. The Windows player is available by double-clicking `Play Space & Crafts.cmd` in the project folder; that starts `Builds/Windows/SpaceAndCrafts.exe` without opening Unity Hub.

## Current stability evidence — November 12, 2026

- Unity 6000.5.4f1 completed a fresh windowed Windows build. `Logs/user-launch-stability-build-20261112.log` records **Build Finished, Result: Success** (484 MB).
- The existing Arrival PlayMode smoke reached the main route: checkpoint and fall recovery, movement/jump, camera follow/orbit/collision, coyote time, buffered jump, gathering/crafting, launch, boss completion, and restart all passed. It still exits with seven Splat presentation/input failures: refused-build reaction, spring reaction being overwritten by input, near-miss pickup assist, component-specific discovery reactions, eye expression, hidden-wonder reaction, and the post-boss listening pose. Full gameplay stability is not established. Details: `Logs/arrival-smoke-user-launch-20261112.log`.
- A real Arrival Play Mode camera frame was rendered headlessly at 1600×900 and inspected: `Logs/score-comparison/game-current-splat-feet.png`. The image shows the scene, camera, level, HUD, and Splat rather than a blank frame. This is a scripted capture, not a screenshot of the user's Editor window; the user's precise blank-tab state and normal desktop play remain unverified.

If the steps above still show a blank Game view, send a screenshot of the Editor with its Play button and Game tab visible, plus the Console errors. That will distinguish a view/layout issue from an actual player-render error.

## Latest verification — November 12, 2026

Unity 6000.5.4f1 rebuilt the windowed Windows player successfully (484 MB; `Logs/CodexWindowedBuild.log`). A headless no-graphics run reached Home Menu startup and logged the title Splat visual installation (`Logs/CodexHeadlessPlayerSmoke.log`); no visible window or human play was checked. The Arrival wiring validator now passes after it began transforming its authored sample coordinates through the level's 0.6 root scale (`Logs/CodexArrivalValidationAfterFix.log`).

For Editor play after Hub opens the project, choose **Space & Crafts → Play Game (Home Menu)** / **Ctrl+Shift+R**, or **Play Arrival (Visual)** for World 1. For standalone play, double-click `Play Space & Crafts.cmd` or run `Builds/Windows/SpaceAndCrafts.exe`. The Unity MCP package still emits a shutdown assertion in batch logs. Visible blank-view resolution is not claimed until a visible session is checked.

## Latest Unity Hub play check — 2026-11-12

- Rebuilt the windowed Windows player with Unity 6000.5.4f1. `Logs/CodexUserRequestedWindowedBuild_Retry.log` reports **Build Finished, Result: Success** and records the player at `Builds/Windows/SpaceAndCrafts.exe` (484 MB).
- Launched the player with `-batchmode -nographics`; the log reached engine initialization and `[SplatVisualRepair] Installed the skinned Splat visual on ART_TitleSplat`. The no-graphics player does not honor `-quit`, so I stopped that process after startup. This was not a visible rendering check.
- To try it now, double-click `Play Space & Crafts.cmd` in the project folder. In Unity Editor, use **Space & Crafts → Play Arrival (Visual)** to enter World 1, or **Space & Crafts → Play Game (Home Menu)** / **Ctrl+Shift+R**. Hub's **Open** button opens the Editor; it does not itself start Play Mode. If Play is active but the tab is blank, select **Window → General → Game**.
- The earlier Arrival gameplay smoke still has seven known Splat reaction/input failures documented above. No visible desktop session or human play was checked, so the user's exact blank-view symptom is not confirmed resolved.

## Fresh user-requested stability run — 2026-11-12

- Rebuilt the current windowed Windows review player with Unity 6000.5.4f1. `Logs/user-requested-fresh-windowed-build.log` reports **Build Finished, Result: Success** (484 MB).
- Re-ran the existing Arrival Play Mode smoke. It confirmed checkpoint and fall recovery, movement/jump, camera follow/orbit/collision, coyote time, buffered jump, gathering/crafting, Boing-O-Matic launch, boss completion, and restart. It still **fails overall** on seven Splat reaction/input assertions: refused-build reaction, spring reaction persistence, near-miss pickup assist, component discovery reactions, eye expression, hidden-wonder reaction, and gateway listening pose. Evidence: `Logs/user-requested-arrival-smoke.log`. Complete gameplay stability is not established.
- To try it from Unity Hub: open the project, wait for compilation, then use **Space & Crafts → Play Arrival (Visual)** to go directly into World 1, or **Space & Crafts → Play Game (Home Menu)** / **Ctrl+Shift+R**. Hub's **Open** action opens the Editor; it does not itself run the game. The standalone player is also available through `Play Space & Crafts.cmd`.
- No Unity UI, rendered window, or human play was checked, so I cannot confirm the specific blank Game tab is visually resolved. If the Game tab still looks blank after using a Play command, capture the Editor with the Play button and Game tab visible plus the Console errors.

## Unity Hub launch follow-up — 2026-11-12

- Rebuilt the current Windows review player with Unity 6000.5.4f1; `Logs/user-requested-stability-build-20261112.log` reports `Build Finished, Result: Success` and a 484 MB player. No C# compile errors were reported. Unity MCP emits a shutdown assertion after the successful build; it did not change the build result.
- `EditorBuildSettings.asset` puts `HomeMenu.unity` first, followed by `RainbowWorld_Arrival.unity`. `GameStartSceneBootstrap` routes an empty/default editor scene to Home Menu before Play and focuses the Game view. Unity Hub's Open action only opens the Editor; use **Space & Crafts → Play Arrival (Visual)** for World 1, or **Space & Crafts → Play Game (Home Menu)** / **Ctrl+Shift+R** for the title.
- No visible Editor/Game window or human play was checked. If the user's Game view still appears blank, the exact UI/layout symptom remains unverified; select **Window → General → Game** and share the Console error if needed.

## Fresh Unity tryout handoff — November 12, 2026

- Rebuilt the windowed Windows player from the current project in Unity 6000.5.4f1. `Logs/user-requested-stability-build-20261112.log` ends with **Build Finished, Result: Success**; the player data payload was refreshed. Unity still prints its known `gRefreshReentrancyCount` assertion during batch shutdown after success.
- To try World 1 inside the Editor opened from Unity Hub: wait for import/compilation, then choose **Space & Crafts → Play Arrival (Visual)**. For the title, choose **Space & Crafts → Play Game (Home Menu)** or press **Ctrl+Shift+R**. Hub's **Open** button only opens the Editor; it does not start Play Mode. `Play Space & Crafts.cmd` starts the standalone Windows player.
- I did not open the Editor/Game tab or a visible player window, so the user's exact blank display is **not visually verified as fixed**. If Play is highlighted but the view is blank, select **Window → General → Game** and check the Console.
- Latest available Arrival smoke log (`Logs/user-requested-arrival-smoke.log`, 12:56 p.m.) reports seven failing Splat reaction/near-miss pickup checks. Core movement/jump, camera follow/orbit/collision, coyote/buffered jump, gathering/crafting, invention launch, boss completion, and restart checks passed. This remains a known gameplay polish defect; it did not prevent the later player build from succeeding.

## Latest user-requested Unity tryout refresh — November 12, 2026

- Rebuilt the windowed Windows player from the current `space-and-crafts-3` project with Unity 6000.5.4f1. `Logs/user-requested-unity-hub-stability-20261112.log` reports **Build Finished, Result: Success** (488 MB); no C# compile errors were found. Unity MCP emits a shutdown asset-editing assertion after the build succeeds.
- Unity Hub's **Open** opens the Editor; it does not start gameplay. In the Editor use **Space & Crafts → Play Arrival (Visual)** for World 1. If Play is highlighted but the view is blank, switch to **Window → General → Game**. Or double-click `Play Space & Crafts.cmd` to launch the refreshed standalone player.
- No visible Game view/player window or human play was inspected, so the user's exact blank-display symptom remains visually unverified. The previous scripted Arrival smoke still has seven Splat reaction/pickup failures (see `Logs/user-requested-arrival-stability-smoke-20261112.log`); the build is runnable, but full gameplay polish/stability is not established. Ratings unchanged; 90/100 remains unmet.
