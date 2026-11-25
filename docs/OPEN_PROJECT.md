# Opening the Unity Project

## Status

This repo is a Unity **6000.5.4f1** project with:

- URP (`com.unity.render-pipelines.universal`)
- Input System (`com.unity.inputsystem`) — `activeInputHandler: Both`
- Game content under `Assets/SpaceAndCrafts/`
- Input actions: `Assets/SpaceAndCrafts/Input/SplatInputActions.inputactions`
- Arrival vertical slice graybox + systems wired (TEMP ART); latest green: `Logs/arrival-playmode-smoke48.log`, `Logs/arrival-validate9.log`

## Open

1. Open Unity Hub (the Hub itself is only the launcher; it does not display or run the game).
2. Add project: `E:\Users\thoma\git\space-and-crafts-3`
3. Open with **6000.5.4f1** and wait until imports/compilation finish.
4. In the Unity Editor, open the **Game** tab and press the top toolbar's **Play** button. A fresh session loads the Home Menu automatically, including when Unity starts on its untitled default scene or restores an untouched Unity default-camera/light backup. Default-only scenes are replaced without a save prompt; authored scenes remain protected by Unity's save guard. If the Game tab stays hidden, use **Space & Crafts → Play Game (Home Menu)** or **Ctrl+Shift+R**: it loads Home Menu, opens the Game tab, then enters Play Mode. The **Play Arrival (Visual)** and **Play Home Menu (Visual)** shortcuts also bring the Game tab forward. To open the scene without starting play, use **Open Game Start Scene** followed by Play.
5. For the fastest tryout outside the Editor, double-click `Play Space & Crafts.cmd` in the project root. It launches `Builds/Windows/SpaceAndCrafts.exe`. Unity 6000.5.4f1 most recently reported a successful 484 MB Windowed Review build in `Logs/windows-review-build-tryout-visibility.log`; player data files were refreshed. The window is configured resizable at 1600×900. The player has not been launched or visually reviewed.
6. To rebuild after source changes, use **Space & Crafts → Build Windows Player (Windowed Review)**; it builds enabled scenes to `Builds/Windows/SpaceAndCrafts.exe`. The latest batch rebuild succeeded despite an initial Unity Licensing Client warning (`Logs/windows-review-build-current.log`, 484 MB). The player has not been visibly launched or reviewed.
8. Optional rebuild menus (if scenes look stale):
   - **Space & Crafts → Bootstrap PlayerPlayground (TEMP ART)**
   - **Space & Crafts → Bootstrap Arrival Graybox (TEMP ART)**
9. **P0 feel pass:** open `PlayerPlayground`, Play Mode, gamepad preferred — walk the labeled jump-feel lane
10. **P1 path pass:** open `RainbowWorld_Arrival`, Play Mode — goo → craft → gulch → secret → boss → gateway reveal (R restarts)

## Batch checks

- Validate: `ArrivalSceneValidator.ValidateOrThrow` → `Logs/arrival-validate*.log`
- Smoke: `ArrivalPlayModeSmokeMenu.Run` (no `-quit`; exits via SessionState) → `Logs/arrival-playmode-smoke*.log`

## Note

Do not present Unity primitives as finished art. Label placeholders TEMP ART.  
See `docs/MISSING_ASSETS.md` for commissioning priorities.
