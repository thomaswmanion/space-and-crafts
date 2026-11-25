# Initial Scene Structure

## Prefabs to author first

| Prefab | Contents |
|---|---|
| `Prefabs/Characters/Splat_TEMP` | CharacterController, SplatInputReader, SplatMotor, SplatFeelPresenter, SplatInteractor, InventoryComponent, TEMP ART visual |
| `Prefabs/Gameplay/CameraRig` | PlatformingCameraRig, CameraCollision, child Camera |
| `Prefabs/Gameplay/CollectiblePickup` | Trigger + CollectiblePickup + TEMP ART |
| `Prefabs/Gameplay/BuildSite_BoingOMatic` | BuildSite + assembly anchors + inactive BoingOMatic |

## PlayerPlayground layout

1. Flat ground + gentle slopes
2. Jump height / gap rulers
3. Soft wall for camera collision
4. One collectible for pickup smoke test

## MechanicsPlayground layout

1. Three labeled components
2. One build site
3. One launch gap that requires Boing-O-Matic
4. Restart volume

## RainbowWorld_Arrival

Follow `docs/ARRIVAL_PLAN.md` zones. Graybox only until art pass.

## HomeMenu

`HomeMenuController` builds the save-slot title UI and a full-screen modal camera-controls panel at runtime. Mouse look is adjustable in degrees per pixel; controller look is adjustable by multiplier. Preferences persist across scene loads with `PlayerPrefs`, and the modal overlay blocks clicks from reaching title actions underneath it.
