# Space & Crafts

Current Unity project snapshot from the `space-and-crafts-3` workspace. This branch replaces the old prototype previously on `master` and retains the current game source, all authored non-review scenes, scene and resource dependencies, project settings, package manifest/lock, and current development documentation.

## Open and play

- Unity version: **6000.5.4f1** (`ProjectSettings/ProjectVersion.txt`).
- Open this folder from Unity Hub.
- The enabled build scenes start at `HomeMenu`, then World 1 `RainbowWorld_Arrival`, then `WorkshopHub`.
- To launch the existing Windows player from the workspace, run `Play Space & Crafts.cmd` after building.

## Controls

See `TRY_THE_GAME.md` for current launch and control notes.

## Project scope in this branch

All project C# scripts and authored non-review scenes are included. Unity dependency data was used to include referenced assets for all authored scenes, resources, ScriptableObjects, and scripts. Historical Meshy generation/review archives and build/cache outputs remain in the original workspace and are not required by the authored game scenes; they are excluded from this runnable source snapshot to keep the GitHub repo within normal repository limits.

The old upstream `master` remains untouched. This branch is prepared as `main-2` for review and subsequent game iteration.
