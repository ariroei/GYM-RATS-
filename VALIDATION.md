# Prototype setup validation

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
The scene is a static foundation; fighters, movement, combat, and pause behavior
are not implemented. Build output, screenshots, and generated performance-test
data are excluded from Git. Automatic build-only URP serialization and Unity
connection changes were reverted after validation.
