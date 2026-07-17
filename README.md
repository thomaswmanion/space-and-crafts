# Space & Crafts

A 3D platformer for Unity and WebGL. The player travels through five worlds,
collects crystals, and frees Crystal Town from General Snip.

## Recommended editor setup

- Install Unity Hub and Unity 6 LTS with the **Web Build Support** module.
- In Unity Hub, choose **Add > Add project from disk** and select this folder.
- This project is currently aligned with Unity `6000.5.4f1`.
- Use the Universal 3D render pipeline when creating production materials. Keep
  shaders simple and baked lighting modest for WebGL performance.
- Set **File > Build Profiles > Web** and use texture compression appropriate to
  the target browsers. Start with a 960x540 development build.

## First test scene

1. Create a scene at `Assets/Scenes/World01_Test.unity`.
2. Add a plane or cubes for the ground and ensure they have non-trigger colliders.
3. Add a capsule named `Player`, tag it `Player`, and add a `CharacterController`
   plus `PlayerController3D`. The default controls are WASD/arrow keys and Space.
4. Add an empty object named `GameManager` and attach `GameManager`.
5. Create a crystal from a simple mesh, add a trigger collider, then attach
   `CrystalCollectible`. Assign any short pickup clip when one is available.
6. Add the scene to the active build profile's scene list.

The first level uses Unity's current Input System directly.

- Move: WASD, arrow keys, or gamepad left stick
- Jump and stomp: Space or gamepad south button
- Punch: F, left mouse, or right trigger
- Turn invisible and phase through purple walls: Left Shift or right shoulder
- Spray the water gun after collecting it: Q or left trigger
- Interact: E or gamepad west button
- Orbit the third-person camera: hold right mouse or use the right stick

Splat begins with five health. Invisibility grants temporary immunity and lets
Splat cross objects on the `PhaseWall` layer, followed by a cooldown. The Rainbow
Star Gate opens after enough crystals are collected and every enemy is defeated.

Rainbow World is the opening hub. Clearing its exterior, defeating the Rainbow
Warden inside the castle, and rescuing the Rainbow King unlocks the parked
Rainbow Rider spaceship. Its four rainbow launch routes lead toward Worlds 2–5.

The WebGL build opens on `Assets/Scenes/Home.unity`. The home screen includes
Start Adventure, World Select, Controls, and three browser graphics presets.
During gameplay, press Escape or the gamepad Start button for Resume, Restart,
and Home Menu options.

## Art direction from the concept drawing

Preserve the drawing's bold silhouettes and childlike handmade charm when moving
to 3D: rounded low-poly forms, chunky proportions, simple faces, saturated hero
colors, and bright red/pink villain eyes. Splat should read as soft and bouncy,
Scrap-Bot as broad and weighty, Sir Scribbles as a cheerful pencil-shaped figure,
and General Snip as an oversized angular scissor silhouette. Pixel-art textures
can be used on low-poly 3D models with point filtering to retain the intended
retro style without making the game itself 2D.

## Suggested asset layout

```text
Assets/
  Animations/
  Art/
    Characters/
    Environments/
    UI/
  Audio/
    Music/
    SFX/
  Materials/
  Prefabs/
    Characters/
    Collectibles/
    Enemies/
  Scenes/
  Scripts/
    Collectibles/
    Core/
    Player/
  UI/
```
