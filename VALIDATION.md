# GYM RATS validation

## Character animation and first combat prototype - 2026-09-28

Completed the interrupted character/Animator setup and added combat in the
existing Unity 6000.6.3f1 project. The original player prefab GUID, input bindings,
camera controls, and sample scene are preserved.

- C# compilation completed without errors.
- All **4 Edit Mode checks** passed, including the generic Avatar, seven clips,
  valid animation paths, camera framing, materials, and existing input setup.
- All **17 Play Mode checks passed across a suite run and a targeted rerun**:
  the suite passed 16 tests; its stop-to-idle timing assertion was updated to
  allow the actual deceleration/damping/blend to finish within 0.7 seconds, and
  the complete locomotion/jump/fall/landing transition test then passed.
- The **8 combat checks** verified keyboard and gamepad punching; actual upper-arm
  animation before contact; delayed, single-hit impact; flash, recoil, knockback,
  and recovery; cooldown and no automatic repeat when held; out-of-range misses;
  behind-target misses; solid-wall blocking; movement/jumping/camera while punching;
  and rejection of repeated hits during recovery.
- Existing movement/jump/camera checks passed with both rats in the scene.
  Additional checks verified Idle/Run/Jump/Fall/Land transitions and actual idle
  blinking/tail motion. The target remained stationary under player movement input.
- Normal Play Mode visual inspection confirmed both characters and a valid rig,
  with no console errors or warnings. A paused impact capture showed the target's
  hit flash. The practice target's coral material assignment was corrected after
  the visual review. Play Mode was stopped and GymPrototype left open.
- A Test Runner startup exception required clearing an interrupted job and
  retrying. A material-block initialization/cleanup issue and an incorrect
  upper-body mask path were fixed before the successful checks.
- Git whitespace, asset metadata, and duplicate-GUID checks passed.

Not tested: physical keyboard/gamepad feel and a new standalone player build.
These are primitive-based, rigid-joint art assets, not a finished skinned character.
Production mesh/skin weights, animation polish, and impact audio/VFX remain;
see `CHARACTER_ART.md`. No grabbing, weapons, multiplayer, or knockout system
was added.

## Playable movement prototype - 2026-09-28

Validated in the existing Unity 6000.6.3f1 Editor with URP and Input System 1.20.0.

- C# compilation completed without errors.
- All **4 Edit Mode tests** passed: scene floor/lighting/material integrity,
  rat camera framing and target, build scene order, and device binding resolution.
- All **7 Play Mode integration tests** passed in the real GymPrototype scene:
  - Keyboard movement relative to a rotated camera, visual turning, and stopping.
  - Keyboard jumping, landing, and holding jump without automatic repeat jumps.
  - Analog/full-speed gamepad movement, gamepad jumping, and no second airborne jump.
  - Normalized diagonal speed, camera tracking, and camera settling after stopping.
  - Camera obstruction avoidance and restoration of follow distance.
  - Walking off the arena, falling under gravity, respawning, and camera recovery.
  - Ceiling collision followed by a return to the ground.
- Input tests use isolated virtual keyboard/gamepad devices and restore the
  previous Input System after cleanup. An initial fixture lifecycle failure was
  fixed before the final successful run.
- Normal Play Mode was also opened separately. The rat stood grounded at rest,
  the camera followed the correct target, and a front/three-quarter screenshot
  confirmed the temporary muscular rat is recognizable. The console contained
  no errors or warnings in that session. Play Mode was stopped afterward.
- The previous sample scene, input asset, package versions, and rendering
  configuration were preserved. Git whitespace and asset metadata checks passed.

Limitations: no physical gamepad was operated, and subjective controller feel
still needs human playtesting. Coyote time and jump buffering are implemented
but their timing windows were not individually asserted by these tests. No new
standalone build was made in this step; the build described below predates the
playable character. Combat, grabbing, multiplayer, and camera orbit controls
are outside this step.

## Earlier static foundation validation

Validated on 2026-09-28 in Unity 6000.6.3f1 on Windows.

- The existing repository, Unity version, packages, sample scene, input asset
  GUID, and PC quality configuration were preserved.
- Unity compiled the new Editor test assembly without C# errors.
- All four tests in `GymRats.Editor.Tests` passed: floor/camera/light/material
  checks, build scene order, keyboard binding resolution, and gamepad binding
  resolution. Virtual devices were removed after testing.
- A camera screenshot was inspected: the full arena and floor markings are visible.
- A Windows 64-bit development build with strict error checking succeeded:
  `Builds/Windows/GYM RATS.exe`, 199.83 MB, zero errors.
- The build reported 511 warnings: 508 package shader compilation warnings
  (including the existing AI inference shaders), two stripped Core debug shader
  warnings, and one warning about pending uncompiled Editor changes at build
  start. No custom runtime scripts or asset postprocessors were introduced.
  The new Editor test assembly had compiled and passed before the build.
- The Editor also contained pre-existing Unity AI generator `NoSubscription`
  errors. AI generation is not required for this prototype.
- Git whitespace checks and asset metadata/GUID checks passed.

The build executable was produced but not launched. Physical keyboard/gamepad
playtesting and multiplayer device pairing remain for future gameplay work.
At that stage the scene was a static foundation without character movement.
Build output, screenshots, and generated performance-test
data are excluded from Git. Automatic build-only URP serialization and Unity
connection changes were reverted after validation.
