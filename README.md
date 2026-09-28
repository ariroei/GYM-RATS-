# GYM RATS

A stylized 3D PC party fighting game built with Unity **6000.6.3f1** and
Universal Render Pipeline **17.6.0**. Input System **1.20.0** is enabled.

## Open the prototype

1. Add this repository folder in Unity Hub and open it with the recorded Editor version.
2. Open `Assets/Scenes/GymPrototype.unity` and select a 16:9 Game view.
3. Press Play to view the static arena. This setup contains a solid floor, border,
   floor markings, lighting, and camera; fighters, movement, combat, and menus
   are future work. The configured actions do not yet control a character.

The target is Windows 64-bit, with a 1920 x 1080 default resolution. The existing
PC URP quality asset is retained and also assigned as the graphics fallback.
GymPrototype is the first build scene; the original SampleScene remains available.
Use File > Build Profiles to build for Windows. Keep output under `Builds/`.

## Input

`Assets/InputSystem_Actions.inputactions` remains the project-wide input asset.
Existing Player and UI actions and their identifiers are preserved. The
Keyboard&Mouse scheme now accepts a keyboard without a mouse. Gamepad bindings
use generic controls for compatible controllers.

| Player action | Keyboard / mouse | Gamepad |
| --- | --- | --- |
| Move | WASD or arrow keys | Left stick |
| Attack | Enter or left mouse button | West face button (X / Square) |
| Jump | Space | South face button (A / Cross) |
| Interact | Hold E | Hold north face button (Y / Triangle) |
| Crouch | C | East face button (B / Circle) |
| Sprint | Left Shift | Left stick press |
| Pause | Escape | Start / Menu |

Device pairing and local multiplayer joining will be implemented with player
gameplay. Pause is an input action only; it does not implement a pause menu yet.

## Asset organization

Keep `.meta` files alongside their assets. The existing tutorial and settings
assets are preserved.

| Folder under Assets | Purpose |
| --- | --- |
| Scenes | Playable scenes and the original sample scene |
| Scripts | Runtime gameplay code |
| Characters | Character meshes and character-specific assets |
| Animations | Animation clips and controllers |
| Materials | Shared materials, including the prototype palette |
| Prefabs | Reusable objects, including GymArena |
| Audio | Music, sound effects, and mixers |
| UI | Menus, HUD assets, and UI layouts |
| Tests/Editor | Editor validation for the arena and input setup |

## Validation and workflow

Open Window > General > Test Runner, select EditMode, and run
`GymRats.Editor.Tests`. The checks cover floor collision, camera framing at 16:9,
lighting, missing scripts, supported URP materials, build scene order, and
keyboard/gamepad binding resolution using temporary virtual devices. These
checks do not substitute for testing a physical gamepad or a built player.

Follow `AGENTS.md`: review changes, run relevant validation, commit descriptively,
and push the current working branch after each completed development prompt.
