# GYM RATS

A stylized 3D PC party fighting game built with Unity **6000.6.3f1** and
Universal Render Pipeline **17.6.0**. Input System **1.20.0** is enabled.

## Open the prototype

1. Add this repository folder in Unity Hub and open it with the recorded Editor version.
2. Open `Assets/Scenes/GymPrototype.unity` and select a 16:9 Game view.
3. Connect one gamepad for keyboard + gamepad, or two gamepads, then press Play.
   Focus the Game view. The panel shows each player's device and lets you switch layouts.

Two rats start at separate safe positions: **P1 teal** and **P2 orange**, with
colored floor markers and overhead labels. With one gamepad, P1 owns the keyboard
and P2 owns the gamepad. With two connected gamepads at startup, P1 owns the first
and P2 the second. Each rat's movement, jump, punch, grab, release, and throw actions
are filtered to its assigned device. A missing device leaves that player idle.
Reconnecting fills an empty slot without taking the other player's device.
The selected layout remains stable when devices are connected during play; use
the panel to switch from keyboard + gamepad to two gamepads.

Face the other rat and punch for animated recoil, a flash, knockback, and brief
recovery. Close forward punches connect; distant or wall-blocked punches miss.
Grab either rat or the Dumbbell, Medicine Ball, or Foam Roller. Grab again to
release, or throw in the direction you face. You can move and jump while carrying;
punching resumes after release/throw recovery. Blocked carry paths and hits on
the holder release the target. Intentional equipment throws knock back rats;
dropped or nudged props do not attack. No online networking, weapons, health,
knockout rules, or ragdolls are implemented.

The elevated shared camera follows both rats and carried targets. It immediately
zooms out as they separate and eases in when they regroup, preserving a fixed
world heading for movement. Arena walls limit separation; there is no split screen
or artificial player tether. Characters become smaller at opposite corners.

The original stylized rat design uses an articulated **primitive-based prototype**,
not a finished character mesh. A generic rig and Animator provide idle, run,
jump, fall, landing, punch, hit, grab, hold, release, and throw clips, plus blinking and tail motion.
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

| Player action | Keyboard (P1 in keyboard layout) | Assigned gamepad |
| --- | --- | --- |
| Move | WASD or arrow keys | Left stick |
| Attack | Enter | West face button (X / Square) |
| Jump | Space | South face button (A / Cross) |
| Grab / release | E | North face button (Y / Triangle) |
| Throw | R | Right shoulder (RB / R1) |
| Interact (reserved, unused) | Hold E | Hold north face button (Y / Triangle) |
| Crouch | C | East face button (B / Circle) |
| Sprint | Left Shift | Left stick press |
| Pause | Escape | Start / Menu |

Move, Jump, Attack, Grab, and Throw are active. Other bindings are reserved.
RatInputOwner filters all per-character action copies to the same assigned device;
LocalMultiplayerSession exclusively assigns devices. Mouse attack is retained in
the source input asset for compatibility but the multiplayer keyboard slot does
not own the mouse. Use Enter to punch. Mouse clicks operate the layout panel.

## Tune the prototype

Select `Rat Player` in GymPrototype:

- **Rat Motor:** move speed, acceleration, deceleration, turn speed, air control,
  jump height, gravity, terminal speed, coyote time, jump buffer, ground probe,
  fall recovery height, and knockback drag. Both scene rats accept player input;
  their Rat Input Owner determines which device can control them.
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
- **Rat Grabber:** grab distance (chest to collider surface), facing cone, throw
  velocity, upward velocity, cooldown, and hold height/distance. `Grabbable`
  supports upright unit-scale rat capsules and compound box, sphere, or capsule
  prop colliders, including their current rotation after tumbling.
  Held colliders are suspended and volume-swept against obstacles every frame;
  release restores each collider's enabled state. Rats use motor knockback; props
  use Rigidbody velocity impulses with continuous collision detection.
- **Rat Hit Receiver:** flash color and duration. Recovery briefly prevents new
  hits and suppresses player movement/jump/punch input on a struck player.

Select Main Camera to tune **Rat Shared Camera**: pitch, minimum distance,
viewport margin, and smoothing. The previous single-player follow component is
retained but disabled. Select Local Multiplayer Session to set the initial layout,
and either rat's Rat Player Indicator to adjust its label color.
Assets/Characters/RatPrototype.prefab remains the reusable character. Its input
owner starts unassigned; a session must bind a device before it responds.

## Gym arena

The original 26 x 24 gym has a central teal fighting mat, orange strength zone,
purple mobility zone, coral rear training bay, low padded cover benches, lockers,
training bags, a rack, water cooler, wall lights, and GYM RATS lettering. Props are
reachable in the side zones. Tall equipment sits against the rear wall; low front
walls keep the shared camera readable. Four safe spawn markers include the two
active player starts.

Invisible physical perimeter walls and a ceiling keep rats and props inside even
when thrown over the decorative cutaway walls. GymArenaLayout provides an extra
out-of-bounds reset, safely releasing held targets first. Reusable GymArena,
PaddedCoverBench, and StrengthRack prefabs use shared URP materials. Arena static
visuals are combined by material (20 environment renderers, 16 colliders in this
layout); this is an optimization, not a measured frame-rate guarantee.
GymArenaAuthoring regenerates the original primitive blockout and baked meshes.
These are recognizable prototype assets, not final environment art.

## Interactive gym equipment

The three original stylized props are **primitive-based placeholders**, with
editable prefab geometry and physics under `Assets/Prefabs`. No external model
or copied game design is used. The dumbbell has blocky teal rubber weights and
orange caps; the orange ball has a dark grip band; the roller has teal ribs and
orange end rims.

| Prop | Mass | Collision | Throw speed multiplier | Physical character |
| --- | --- | --- | --- | --- |
| Dumbbell | 8 kg | Two end boxes and capsule grip | 0.72 | Heavy, grippy, low bounce, strong impact |
| Medicine Ball | 4 kg | Sphere | 1.0 | Moderate bounce and rolling |
| Foam Roller | 1.2 kg | Horizontal capsule | 1.15 | Light, rolling, gentle impact |

Select a prop to tune Rigidbody mass/damping and its shared Physics Material's
friction/bounce. **Thrown Equipment** exposes launch and upward-arc multipliers, minimum hit
speed, knockback per speed, maximum knockback, upward impulse, recovery, and
active lifetime. Mass controls ordinary object collisions; launch multiplier
separately sets the arcade throw response to the player's common throw force.

Only intentional throws arm impacts, for three seconds by default. A rat can be
hit once per throw, the thrower is immune to its own throw, and rat recovery still
rejects repeated hits. Pickup, release without throwing, disabling, and falling
out of bounds disarm the prop. Sweep checks stop at the nearest solid collider;
physics handles floor/wall bounce. Rat hits reuse the existing flash and recoil.
There are no health/knockout rules, ragdolls, impact audio, or polished VFX yet.

## Asset organization

Keep `.meta` files alongside their assets. The existing tutorial and settings
assets are preserved.

| Folder under Assets | Purpose |
| --- | --- |
| Scenes | Playable scenes and the original sample scene |
| Scripts | Movement, camera, animation drivers, and combat |
| Characters | RatPrototype prefab and future character assets |
| Animations/Rat | Generic Avatar, Animator Controller, upper-body mask, and eleven clips |
| Materials | Shared materials, including the prototype palette |
| Prefabs | Reusable objects, including GymArena, MedicineBall, Dumbbell, and FoamRoller |
| Audio | Music, sound effects, and mixers |
| UI | Menus, HUD assets, and UI layouts |
| Tests/Editor | Editor validation for the arena and input setup |
| Tests/PlayMode | Movement, animation, camera, combat, carry/throw, and recovery integration tests |

## Validation and workflow

Open Window > General > Test Runner, select EditMode, and run
`GymRats.Editor.Tests`. Then select PlayMode and run `GymRats.PlayMode.Tests`.
The checks cover scene setup, camera-relative movement, turning, stopping,
analog gamepad input, jumping and landing, prevention of repeated airborne
jumps, ceiling collision, camera tracking/obstruction, and falling/respawning.
They also check locomotion/airborne animation transitions, face/tail motion,
keyboard/gamepad punches, impact timing and arm motion, knockback and recovery,
cooldown, out-of-range/behind-target misses, wall blocking, and movement while punching.
Grab tests cover rats and the ball, keyboard/gamepad input, carrying while
moving/jumping, animation states, release/throw, restored physics, ownership,
range, wall blocking, cooldown, and holder interruption.
Play Mode tests use isolated virtual devices and restore the real Input System
afterward. These checks do not substitute for testing a physical gamepad or a
built player. See `VALIDATION.md` for recorded results and limitations.

Follow `AGENTS.md`: review changes, run relevant validation, commit descriptively,
and push the current working branch after each completed development prompt.
