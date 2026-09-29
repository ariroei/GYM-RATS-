# GYM RATS character art prototype

## Current asset

`Assets/Characters/RatPrototype.prefab` is the reusable player prefab. Its original
asset GUID and `Muscular Rat` facing transform are preserved. The practice target
is an instance of this same prefab with input disabled and a coral singlet override.

This is an original primitive-based design: broad lavender shoulders and biceps,
a narrow waist, teal singlet with a simple barbell emblem, plum shorts, orange
sweatbands, cream muzzle, expressive amber eyes/brows, asymmetric ear tape,
incisors, whiskers, pink paws, and a long articulated tail.

It is functional prototype art, **not a finished production character**. It uses
100 rigid primitive renderers rather than a continuous skinned mesh. No external
model was generated: the available generation providers were unconfigured and
Blender was not found. No other game's character was used as a reference.

## Rig and animation

The motor controls `Rat Player/Muscular Rat` for facing. The Animator lives on its
`Character Rig` child so animation cannot overwrite the motor's turning.
The generic transform rig has hips, spine, chest, neck, head, jaw, ears, brows,
eyes, upper arms, forearms, hands, thighs, shins, feet, and four tail joints.
Primitive geometry is rigidly parented to joints; there are no skin weights.

`Assets/Animations/Rat/RatLocomotion.controller` contains:

- **Base Layer:** Idle, Run, Jump, Fall, Land. Speed is damped, run stride rate
  follows movement speed, and grounded/vertical-speed parameters drive airtime.
  Landing has a short squash-and-recover pose. Idle includes a blink and tail sway.
- **Combat:** masked upper-body Punch and Hit over the locomotion layer. The
  animation driver fades layer weight in/out while movement and jumping continue.
  Punch contact is at one third of its duration. Hit animation playback scales
  with recovery time. Neither animation moves the root or physics capsule.

`RatAnimatorDriver` reads `RatMotor` state. `RatCombat` triggers Punch; the hit
receiver triggers Hit. The prior `RatVisualMotion` component is removed from the
prefab to avoid competing writes; its source is retained for reference.

- **Carry:** masked upper-body Grab, Hold, Release, and Throw states over locomotion.
  These are temporary rigid-joint poses. The carried target stays at a fixed chest/front
  attachment; there is no hand IK, target-specific contact alignment, or ragdoll.
  Thrown rats remain upright CharacterControllers with gravity and knockback;
  the medicine ball is a physical Rigidbody. Thrown equipment now triggers rat hit reactions; thrown rats do not deal impact hits.

## Editing

The editable `.anim`, `.controller`, `.mask`, Avatar, materials, and prefab are
checked in. `Assets/Scripts/Editor/RatCharacterAuthoring.cs` reproduces the generated
rig, geometry, palette, and animation assets while preserving their asset GUIDs.
The menu **GYM RATS > Character > Rebuild Primitive Rat Rig and Animations** warns
before replacing generated visual work. Commit hand-edited assets before using it.
No rebuilding is required to open or play the project.

## Remaining production assets

- A sculpted, retopologized, UV-mapped rat mesh with deformation-ready topology.
- Skin weights, optimized mesh/material counts, and LODs for several characters.
- Authored textures and more polished fur, clothing, paws, teeth, and facial shapes.
- Facial blend shapes, refined squash/stretch, contact poses, foot locking/IK,
  and hand-polished locomotion, attack, and reaction animation.
- Dedicated punch/impact audio and polished impact VFX; the current hit flash and
  rigid-joint recoil are functional placeholders.

## Gym equipment placeholders

Medicine Ball, Dumbbell, and Foam Roller use original colored primitive geometry.
The dumbbell uses compound end-weight/grip colliders; the roller uses a capsule
without simulating individual grooves. These are recognizable playable blockouts,
not final modeled/textured assets. Final bevels, optimized meshes, textures,
contact-specific hand poses, and equipment impact audio/VFX remain.

## Arena and local multiplayer additions

The gym's mats, cutaway walls, cover benches, lockers, rack, training bags,
water cooler, and pixel lettering are original primitive blockout art.
GymArenaAuthoring preserves editable authoring code and bakes static visuals by
material into reusable prefabs. Final modeled environment assets, textures,
audio, and impact VFX remain future work. Two copies of the existing articulated
rat use teal/orange indicators and P1/P2 labels; neither is a finished skinned mesh.
