# GYM RATS

A stylized 3D PC party fighting game built with Unity **6000.6.3f1** and
Universal Render Pipeline **17.6.0**. Input System **1.20.0** is enabled.

## Open the prototype

1. Add this repository folder in Unity Hub and open it with the recorded Editor version.
2. Open `Assets/Scenes/GymPrototype.unity` and select a 16:9 Game view.
3. Press Play and focus the Game view. Move with WASD / arrow keys or the gamepad
   left stick. Jump with Space or the south face button (A / Cross).

The teal-shirted rat is playable, with camera-relative movement, smooth turning,
analog speed, jumping, gravity, a follow camera, and a basic punch. Walk toward
the coral-shirted **Practice Rat**, face it, and press **Enter**, **left mouse**,
or the gamepad **west face button (X / Square)**. Close punches cause a flash,
animated recoil, knockback, and brief recovery. Punches outside the forward
range or behind a solid wall miss. Holding Attack does not automatically repeat.
The target has no AI or player input; it only moves when hit or falling.
Both rats return to their own starting positions after falling off the arena.
Grabbing, weapons, multiplayer, health/knockout rules, and menus are not implemented.

The original stylized rat design uses an articulated **primitive-based prototype**,
not a finished character mesh. A generic rig and Animator provide idle, run,
jump, fall, landing, punch, and hit clips, plus blinking and tail motion.
See `CHARACTER_ART.md` for the rig, authoring workflow, and remaining art work.

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

Move, Jump, and Attack are active. The other existing bindings are reserved for
later work. Movement and combat own their input-action copies, leaving the
project-wide UI actions independent. Keyboard and gamepad
work without selecting a scheme; device pairing and multiplayer joining are
future work. The camera follows automatically with a fixed world heading.

## Tune the prototype

Select `Rat Player` in GymPrototype:

- **Rat Motor:** move speed, acceleration, deceleration, turn speed, air control,
  jump height, gravity, terminal speed, coyote time, jump buffer, ground probe,
  fall recovery height, and knockback drag. **Accepts Player Input** is disabled
  on Practice Rat so gravity and knockback still run without responding to controls.
- **Character Controller:** capsule dimensions, slope limit, step offset, and
  skin width. Keep the player root at unit scale; resize the visual child rather
  than scaling the root.
- **Rat Animator Driver:** locomotion speed damping and minimum airborne time
  before a landing reaction. Root motion is disabled.
- **Rat Combat:** range (chest to target collider surface), facing cone, punch
  duration, cooldown, horizontal/upward knockback velocity, and hit recovery.
  The selected-object gizmo shows the range and cone boundaries. Contact occurs
  once, one third of the way through the punch; animation playback scales with
  punch duration. Effective cooldown cannot be shorter than the punch duration.
- **Rat Hit Receiver:** flash color and duration. Recovery briefly prevents new
  hits and suppresses player movement/jump/punch input on a struck player.

Select `Main Camera` to tune **Rat Follow Camera**: offset, look height,
follow smoothing, teleport threshold, and obstruction radius/layers. The camera
pulls forward when geometry obstructs the view and snaps after large teleports.
`Assets/Characters/RatPrototype.prefab` is the reusable character; a missing
camera reference falls back to the scene's MainCamera at startup.

## Asset organization

Keep `.meta` files alongside their assets. The existing tutorial and settings
assets are preserved.

| Folder under Assets | Purpose |
| --- | --- |
| Scenes | Playable scenes and the original sample scene |
| Scripts | Movement, camera, animation drivers, and combat |
| Characters | RatPrototype prefab and future character assets |
| Animations/Rat | Generic Avatar, Animator Controller, upper-body mask, and seven clips |
| Materials | Shared materials, including the prototype palette |
| Prefabs | Reusable objects, including GymArena |
| Audio | Music, sound effects, and mixers |
| UI | Menus, HUD assets, and UI layouts |
| Tests/Editor | Editor validation for the arena and input setup |
| Tests/PlayMode | Movement, animation, camera, combat, and recovery integration tests |

## Validation and workflow

Open Window > General > Test Runner, select EditMode, and run
`GymRats.Editor.Tests`. Then select PlayMode and run `GymRats.PlayMode.Tests`.
The checks cover scene setup, camera-relative movement, turning, stopping,
analog gamepad input, jumping and landing, prevention of repeated airborne
jumps, ceiling collision, camera tracking/obstruction, and falling/respawning.
They also check locomotion/airborne animation transitions, face/tail motion,
keyboard/gamepad punches, impact timing and arm motion, knockback and recovery,
cooldown, out-of-range/behind-target misses, wall blocking, and movement while punching.
Play Mode tests use isolated virtual devices and restore the real Input System
afterward. These checks do not substitute for testing a physical gamepad or a
built player. See `VALIDATION.md` for recorded results and limitations.

Follow `AGENTS.md`: review changes, run relevant validation, commit descriptively,
and push the current working branch after each completed development prompt.
