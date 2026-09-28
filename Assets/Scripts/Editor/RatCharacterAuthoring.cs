using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GymRats.Editor
{
    /// <summary>Reproducible source for the articulated primitive character and its clips.</summary>
    public static class RatCharacterAuthoring
    {
        private const string PrefabPath = "Assets/Characters/RatPrototype.prefab";
        private const string Output = "Assets/Animations/Rat";
        private const string Palette = "Assets/Materials/RatCharacter";
        private static readonly Dictionary<string, Transform> Joints = new Dictionary<string, Transform>();
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Vector3> RestPositions = new Dictionary<string, Vector3>();
        private static Transform visual;

        [MenuItem("GYM RATS/Character/Rebuild Primitive Rat Rig and Animations")]
        public static void RebuildWithConfirmation()
        {
            if (EditorUtility.DisplayDialog("Rebuild primitive rat assets",
                "This replaces the generated visual rig, palette, and animation clips. It preserves the player motor and camera setup. Commit hand-edited visual assets before rebuilding.",
                "Rebuild", "Cancel"))
                Build();
        }

        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before rebuilding the character.");
            EnsureFolder(Output);
            EnsureFolder(Palette);
            Joints.Clear();
            Materials.Clear();
            RestPositions.Clear();
            CreatePalette();

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                // Keep this transform so RatMotor's visual reference and instance overrides survive.
                visual = root.transform.Find("Muscular Rat");
                if (visual == null)
                    throw new InvalidOperationException("The player prefab must contain Muscular Rat.");
                foreach (Transform child in visual.Cast<Transform>().ToArray())
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                var oldMotion = root.GetComponent<RatVisualMotion>();
                if (oldMotion != null)
                    UnityEngine.Object.DestroyImmediate(oldMotion);
                visual.localPosition = Vector3.zero;
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one;
                var oldAnimator = visual.GetComponent<Animator>();
                if (oldAnimator != null)
                    UnityEngine.Object.DestroyImmediate(oldAnimator);
                // Animator owns a child, leaving the motor's facing transform untouched.
                var animationRoot = new GameObject("Character Rig").transform;
                animationRoot.SetParent(visual, false);
                visual = animationRoot;
                BuildRig();
                BuildGeometry();
                foreach (var item in Joints)
                    RestPositions[item.Key] = item.Value.localPosition;

                AnimationClip idle = CreateClip("Idle", 2f, true);
                AnimationClip run = CreateClip("Run", 0.65f, true);
                AnimationClip jump = CreateClip("Jump", 0.3f, false);
                AnimationClip fall = CreateClip("Fall", 0.7f, true);
                AnimationClip land = CreateClip("Land", 0.28f, false);
                var controller = CreateController(idle, run, jump, fall, land);
                AddCombatLayer(controller, CreateClip("Punch", 0.42f, false), CreateClip("Hit", 0.4f, false));

                AddCarryLayer(controller);
                var animator = visual.GetComponent<Animator>();
                if (animator == null)
                    animator = visual.gameObject.AddComponent<Animator>();
                var avatar = AvatarBuilder.BuildGenericAvatar(visual.gameObject, "Rig");
                avatar.name = "Rat Primitive Generic Rig";
                if (!avatar.isValid)
                    throw new InvalidOperationException("The generated generic Avatar is invalid.");
                animator.avatar = SaveAsset(avatar, Output + "/RatGenericAvatar.asset");
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var driver = root.GetComponent<RatAnimatorDriver>();
                if (driver == null)
                    driver = root.AddComponent<RatAnimatorDriver>();
                var serialized = new SerializedObject(driver);
                serialized.FindProperty("motor").objectReferenceValue = root.GetComponent<RatMotor>();
                serialized.FindProperty("animator").objectReferenceValue = animator;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (root.GetComponent<Grabbable>() == null) root.AddComponent<Grabbable>();
                if (root.GetComponent<RatGrabber>() == null) root.AddComponent<RatGrabber>();
                if (root.GetComponent<RatCombat>() == null) root.AddComponent<RatCombat>();
                if (root.GetComponent<RatHitReceiver>() == null) root.AddComponent<RatHitReceiver>();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildRig()
        {
            Joint("Rig", null, Vector3.zero);
            Joint("Hips", "Rig", new Vector3(0, 1.01f, 0));
            Joint("Spine", "Hips", new Vector3(0, 0.30f, 0));
            Joint("Chest", "Spine", new Vector3(0, 0.42f, 0));
            Joint("Neck", "Chest", new Vector3(0, 0.26f, 0));
            Joint("Head", "Neck", new Vector3(0, 0.32f, 0.07f));
            Joint("EarL", "Head", new Vector3(-0.48f, 0.34f, -0.035f));
            Joint("EarR", "Head", new Vector3(0.48f, 0.34f, -0.035f));
            Joint("BrowL", "Head", new Vector3(-0.28f, 0.18f, 0.43f));
            Joint("BrowR", "Head", new Vector3(0.28f, 0.18f, 0.43f));
            Joint("EyeL", "Head", new Vector3(-0.29f, 0.065f, 0.43f));
            Joint("EyeR", "Head", new Vector3(0.29f, 0.065f, 0.43f));
            Joint("Jaw", "Head", new Vector3(0, -0.16f, 0.40f));
            foreach (string side in new[] { "L", "R" })
            {
                float sign = side == "L" ? -1f : 1f;
                Joint("UpperArm" + side, "Chest", new Vector3(sign * 0.71f, 0.10f, 0));
                Joint("Forearm" + side, "UpperArm" + side, new Vector3(sign * 0.20f, -0.37f, 0.03f));
                Joint("Hand" + side, "Forearm" + side, new Vector3(0, -0.34f, 0.06f));
                Joint("Thigh" + side, "Hips", new Vector3(sign * 0.29f, -0.16f, 0));
                Joint("Shin" + side, "Thigh" + side, new Vector3(0, -0.36f, 0));
                Joint("Foot" + side, "Shin" + side, new Vector3(0, -0.34f, 0.05f));
            }
            Joint("TailBase", "Hips", new Vector3(0, -0.10f, -0.35f));
            Joint("TailMid", "TailBase", new Vector3(0, -0.28f, -0.52f));
            Joint("TailCurl", "TailMid", new Vector3(0.16f, -0.36f, -0.55f));
            Joint("TailTip", "TailCurl", new Vector3(0.39f, -0.09f, -0.42f));
        }

        private static void BuildGeometry()
        {
            Shape("Power Torso", "Chest", new Vector3(0, -0.17f, 0), new Vector3(1.43f, 1.06f, 0.83f), "Fur");
            Shape("Tapered Waist", "Spine", new Vector3(0, -0.05f, 0), new Vector3(0.91f, 0.75f, 0.69f), "Fur");
            Shape("Singlet", "Chest", new Vector3(0, -0.20f, 0.02f), new Vector3(1.46f, 1.10f, 0.92f), "Teal");
            Shape("Chest Bib", "Chest", new Vector3(0, -0.16f, 0.45f), new Vector3(0.70f, 0.51f, 0.065f), "DarkTeal");
            Shape("Back Panel", "Chest", new Vector3(0, -0.20f, -0.42f), new Vector3(0.65f, 0.57f, 0.09f), "DarkTeal");
            Shape("Back Stripe", "Chest", new Vector3(0, -0.19f, -0.475f), new Vector3(0.11f, 0.47f, 0.025f), "Orange", PrimitiveType.Cube);
            Shape("Gym Emblem Bar", "Chest", new Vector3(0, -0.13f, 0.493f), new Vector3(0.34f, 0.055f, 0.035f), "Cream", PrimitiveType.Cube);
            foreach (int side in new[] { -1, 1 })
            {
                Shape("Gym Emblem Plate " + side, "Chest", new Vector3(side * 0.15f, -0.13f, 0.5f), new Vector3(0.055f, 0.20f, 0.035f), "Orange", PrimitiveType.Cube);
                Shape("Singlet Strap " + side, "Chest", new Vector3(side * 0.45f, 0.10f, 0.19f), new Vector3(0.16f, 0.40f, 0.45f), "Orange");
            }
            Shape("Sport Shorts", "Hips", new Vector3(0, -0.12f, 0), new Vector3(1.02f, 0.47f, 0.76f), "Purple");
            Shape("Waistband", "Hips", new Vector3(0, 0.045f, 0), new Vector3(1.0f, 0.13f, 0.78f), "Orange");
            Shape("Belt Tab", "Hips", new Vector3(0, 0.01f, 0.41f), new Vector3(0.12f, 0.20f, 0.05f), "Cream", PrimitiveType.Cube);
            Shape("Neck", "Neck", Vector3.zero, new Vector3(0.60f, 0.43f, 0.56f), "Fur");
            Shape("Head", "Head", Vector3.zero, new Vector3(1.01f, 0.79f, 0.89f), "Fur");
            Shape("Forehead Patch", "Head", new Vector3(0, 0.23f, 0.30f), new Vector3(0.48f, 0.32f, 0.34f), "LightFur");
            Shape("Long Tapered Muzzle", "Head", new Vector3(0, -0.13f, 0.57f), new Vector3(0.67f, 0.40f, 0.86f), "Cream");
            Shape("Nose", "Head", new Vector3(0, -0.075f, 0.985f), new Vector3(0.25f, 0.20f, 0.17f), "Nose");
            Shape("Nose Glint", "Head", new Vector3(-0.045f, -0.025f, 1.054f), new Vector3(0.075f, 0.036f, 0.024f), "Pink");
            Shape("Smile", "Jaw", new Vector3(0, -0.11f, 0.15f), new Vector3(0.51f, 0.13f, 0.41f), "Ink");
            Shape("Chin", "Jaw", new Vector3(0, -0.18f, 0.08f), new Vector3(0.49f, 0.17f, 0.38f), "Cream");
            foreach (string side in new[] { "L", "R" })
            {
                float sign = side == "L" ? -1f : 1f;
                Shape("Ear Rim " + side, "Ear" + side, Vector3.zero, new Vector3(0.60f, 0.65f, 0.20f), "Fur");
                Shape("Ear Inner " + side, "Ear" + side, new Vector3(0, 0, 0.091f), new Vector3(0.44f, 0.49f, 0.08f), "Pink");
                Shape("Ear Warm Center " + side, "Ear" + side, new Vector3(0, -0.075f, 0.127f), new Vector3(0.24f, 0.25f, 0.025f), "DeepPink");
                Shape("Cheek " + side, "Head", new Vector3(sign * 0.37f, -0.13f, 0.33f), new Vector3(0.42f, 0.34f, 0.34f), "LightFur");
                Shape("Eye White " + side, "Eye" + side, Vector3.zero, new Vector3(0.30f, 0.32f, 0.16f), "Cream");
                Shape("Amber Iris " + side, "Eye" + side, new Vector3(-sign * 0.025f, 0, 0.073f), new Vector3(0.165f, 0.20f, 0.10f), "Orange");
                Shape("Pupil " + side, "Eye" + side, new Vector3(-sign * 0.025f, 0, 0.122f), new Vector3(0.087f, 0.14f, 0.032f), "Ink");
                Shape("Eye Spark " + side, "Eye" + side, new Vector3(-0.035f, 0.055f, 0.144f), new Vector3(0.042f, 0.05f, 0.02f), "White");
                var brow = Shape("Expressive Brow " + side, "Brow" + side, Vector3.zero, new Vector3(0.35f, 0.10f, 0.16f), "Ink");
                brow.localRotation = Quaternion.Euler(0, 0, side == "L" ? -12f : 5f);
                Shape("Incisor " + side, "Jaw", new Vector3(sign * 0.063f, -0.12f, 0.32f), new Vector3(0.105f, 0.18f, 0.075f), "White", PrimitiveType.Cube);
                for (int i = 0; i < 3; i++)
                    Segment("Whisker " + side + i, "Head", new Vector3(sign * 0.29f, -0.13f - i * 0.035f, 0.77f),
                        new Vector3(sign * (0.66f + i * 0.045f), -0.09f - i * 0.085f, 0.72f), 0.015f, "Ink");

                Shape("Deltoid " + side, "UpperArm" + side, new Vector3(sign * 0.01f, -0.02f, 0), new Vector3(0.65f, 0.63f, 0.60f), "Fur");
                Shape("Biceps " + side, "UpperArm" + side, new Vector3(sign * 0.12f, -0.23f, 0.07f), new Vector3(0.56f, 0.52f, 0.53f), "LightFur");
                Shape("Forearm " + side, "Forearm" + side, new Vector3(0, -0.16f, 0.02f), new Vector3(0.47f, 0.49f, 0.45f), "Fur");
                Shape("Sweatband " + side, "Hand" + side, new Vector3(0, 0.07f, 0), new Vector3(0.48f, 0.16f, 0.47f), "Orange");
                Shape("Band Edge " + side, "Hand" + side, new Vector3(0, 0.12f, 0), new Vector3(0.485f, 0.045f, 0.475f), "Cream");
                Shape("Paw " + side, "Hand" + side, new Vector3(0, -0.11f, 0.01f), new Vector3(0.45f, 0.36f, 0.41f), "Pink");
                Shape("Thumb " + side, "Hand" + side, new Vector3(-sign * 0.19f, -0.055f, 0.10f), new Vector3(0.16f, 0.24f, 0.18f), "Pink");
                for (int i = -1; i <= 1; i++)
                    Shape("Knuckle " + side + i, "Hand" + side, new Vector3(i * 0.115f, -0.08f, 0.18f), new Vector3(0.11f, 0.14f, 0.10f), "DeepPink");
                Shape("Thigh " + side, "Thigh" + side, new Vector3(0, -0.12f, 0), new Vector3(0.52f, 0.58f, 0.53f), "Fur");
                Shape("Short Leg " + side, "Thigh" + side, new Vector3(0, 0.03f, 0), new Vector3(0.57f, 0.27f, 0.58f), "Purple");
                Shape("Short Stripe " + side, "Thigh" + side, new Vector3(sign * 0.27f, 0.03f, 0), new Vector3(0.055f, 0.27f, 0.42f), "Orange");
                Shape("Calf " + side, "Shin" + side, new Vector3(0, -0.14f, 0), new Vector3(0.32f, 0.43f, 0.36f), "LightFur");
                Shape("Ankle Wrap " + side, "Foot" + side, new Vector3(0, 0.10f, -0.03f), new Vector3(0.35f, 0.15f, 0.36f), "Teal");
                Shape("Foot " + side, "Foot" + side, new Vector3(0, 0.00f, 0.11f), new Vector3(0.42f, 0.26f, 0.59f), "Pink");
                for (int toe = -1; toe <= 1; toe++)
                {
                    Shape("Toe " + side + toe, "Foot" + side, new Vector3(toe * 0.12f, -0.01f, 0.36f), new Vector3(0.12f, 0.15f, 0.23f), "Pink");
                    Shape("Claw " + side + toe, "Foot" + side, new Vector3(toe * 0.12f, 0.01f, 0.455f), new Vector3(0.07f, 0.065f, 0.11f), "Cream");
                }
            }
            // An asymmetric ear tape accent makes the silhouette original without borrowing a costume.
            Shape("Ear Tape", "EarL", new Vector3(-0.16f, 0.12f, 0.11f), new Vector3(0.09f, 0.24f, 0.075f), "Orange", PrimitiveType.Cube);
            TailSegment("TailBase", "TailMid", 0.24f);
            TailSegment("TailMid", "TailCurl", 0.19f);
            TailSegment("TailCurl", "TailTip", 0.13f);
            Segment("Tail Tip", "TailTip", Vector3.zero, new Vector3(0.30f, 0.15f, -0.20f), 0.065f, "Pink");
        }

        private static AnimationClip CreateClip(string name, float duration, bool loop)
        {
            var clip = new AnimationClip { name = "Rat " + name, frameRate = 30f };
            var times = Enumerable.Range(0, 13).Select(index => duration * index / 12f).ToArray();
            foreach (var item in Joints)
            {
                string path = AnimationUtility.CalculateTransformPath(item.Value, visual);
                for (int axis = 0; axis < 3; axis++)
                {
                    int component = axis;
                    Curve(clip, path, "localEulerAnglesRaw." + "xyz"[axis], times,
                        times.Select(time => Pose(name, item.Key, time / duration)[component]).ToArray());
                }
            }
            string hips = AnimationUtility.CalculateTransformPath(Joints["Hips"], visual);
            Curve(clip, hips, "m_LocalPosition.y", times, times.Select(time =>
                RestPositions["Hips"].y + HipOffset(name, time / duration)).ToArray());

            // Animate eyelids with the eye assembly so irises and highlights stay aligned.
            foreach (string eye in new[] { "EyeL", "EyeR" })
            {
                string path = AnimationUtility.CalculateTransformPath(Joints[eye], visual);
                if (name == "Idle")
                    Curve(clip, path, "m_LocalScale.y", new[] { 0f, 1.45f, 1.51f, 1.59f, duration }, new[] { 1f, 1f, 0.12f, 1f, 1f });
                else
                    Curve(clip, path, "m_LocalScale.y", new[] { 0f, duration }, new[] { name == "Land" ? 0.7f : 1f, 1f });
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.loopBlend = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return SaveAsset(clip, Output + "/Rat" + name + ".anim");
        }

        private static Vector3 Pose(string state, string bone, float t)
        {
            float wave = Mathf.Sin(t * Mathf.PI * 2f);
            float side = bone.EndsWith("L") ? -1f : 1f;
            bool arm = bone.StartsWith("UpperArm");
            bool forearm = bone.StartsWith("Forearm");
            bool thigh = bone.StartsWith("Thigh");
            bool shin = bone.StartsWith("Shin");
            bool tail = bone.StartsWith("Tail");
            if (state == "Grab" || state == "Hold" || state == "Release" || state == "Throw")
            {
                float lift = state == "Grab" ? Mathf.SmoothStep(0f, 1f, t) : state == "Release" ? 1f - t : 1f;
                if (arm) return new Vector3((-95f + (state == "Throw" ? -55f * Mathf.Sin(t * Mathf.PI * 2f) : wave * 2f)) * lift, 0, side * 12f);
                if (forearm) return new Vector3(-45f * lift, 0, 0);
                if (bone == "Chest") return new Vector3(state == "Throw" ? -18f * Mathf.Sin(t * Mathf.PI * 2f) : -6f * lift, 0, 0);
                if (bone == "Head") return new Vector3(-8f * lift, 0, 0);
                return Vector3.zero;
            }
            if (state == "Punch")
            {
                float extension = t <= 1f / 3f ? Mathf.SmoothStep(0f, 1f, t * 3f)
                    : 1f - Mathf.SmoothStep(0f, 1f, (t - 1f / 3f) * 1.5f);
                if (bone == "Chest") return new Vector3(5f * extension, -18f + 38f * extension, 0);
                if (bone == "Head") return new Vector3(-3f, 12f - 25f * extension, 0);
                if (bone == "UpperArmR") return new Vector3(-35f - 65f * extension, -15f * extension, 12f);
                if (bone == "ForearmR") return new Vector3(-80f * (1f - extension), 0, 0);
                if (bone == "UpperArmL") return new Vector3(-35f, 0, -15f);
                if (bone == "ForearmL") return new Vector3(-85f, 0, 0);
                if (bone.StartsWith("Brow")) return new Vector3(0, 0, side * 12f);
                if (bone.StartsWith("Ear")) return new Vector3(-12f * extension, 0, side * 12f);
                return Vector3.zero;
            }
            if (state == "Hit")
            {
                float recoil = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * 1.4f));
                if (bone == "Chest") return new Vector3(-28f * recoil, -10f * recoil, 5f * recoil);
                if (bone == "Head") return new Vector3(24f * recoil, 12f * recoil, -8f * recoil);
                if (arm) return new Vector3(-20f * recoil, 0, side * 48f * recoil);
                if (forearm) return new Vector3(-55f * recoil, 0, 0);
                if (bone == "Jaw") return new Vector3(20f * recoil, 0, 0);
                if (bone.StartsWith("Brow")) return new Vector3(0, 0, -side * 18f * recoil);
                if (bone.StartsWith("Ear")) return new Vector3(30f * recoil, 0, side * 25f * recoil);
                return Vector3.zero;
            }
            if (state == "Idle")
            {
                if (bone == "Chest") return new Vector3(1.5f * wave, 0, 0);
                if (bone == "Head") return new Vector3(-2f + wave, wave * 5f, 1.5f);
                if (arm) return new Vector3(2f * wave, 0, side * 6f);
                if (forearm) return new Vector3(-12f, 0, 0);
                if (bone.StartsWith("Ear")) return new Vector3(wave * 4f, side * 7f, side * 8f + wave * 3f);
                if (bone.StartsWith("Brow")) return new Vector3(0, 0, side * (3f + wave * 3f));
                if (tail) return new Vector3(wave * 2f, wave * (bone == "TailBase" ? 8f : 12f), 0);
            }
            if (state == "Run")
            {
                if (bone == "Chest") return new Vector3(8f, wave * 7f, wave * 4f);
                if (bone == "Head") return new Vector3(-6f, -wave * 5f, -wave * 2f);
                if (arm) return new Vector3(side * wave * 36f, 0, side * 8f);
                if (forearm) return new Vector3(-25f - Mathf.Max(0f, -side * wave) * 18f, 0, 0);
                if (thigh) return new Vector3(-side * wave * 34f, 0, 0);
                if (shin) return new Vector3(Mathf.Max(0f, side * wave) * 44f, 0, 0);
                if (bone.StartsWith("Foot")) return new Vector3(Mathf.Max(0f, side * wave) * -15f, 0, 0);
                if (bone.StartsWith("Ear")) return new Vector3(-10f + wave * 7f, side * 8f, side * 12f);
                if (tail) return new Vector3(-5f + wave * 4f, wave * 14f, 0);
            }
            if (state == "Jump")
            {
                float stretch = Mathf.Clamp01(t * 2f);
                if (bone == "Chest") return new Vector3(Mathf.Lerp(12f, -8f, stretch), 0, 0);
                if (bone == "Head") return new Vector3(-8f, 0, 0);
                if (arm) return new Vector3(Mathf.Lerp(-15f, -105f, stretch), 0, side * 18f);
                if (forearm) return new Vector3(-18f, 0, 0);
                if (thigh) return new Vector3(-22f, 0, side * 5f);
                if (shin) return new Vector3(40f, 0, 0);
                if (bone.StartsWith("Ear")) return new Vector3(-20f, 0, side * 10f);
                if (tail) return new Vector3(12f, 0, 0);
                if (bone == "Jaw") return new Vector3(9f, 0, 0);
            }
            if (state == "Fall")
            {
                if (bone == "Chest") return new Vector3(-3f, 0, 0);
                if (bone == "Head") return new Vector3(5f, 0, wave * 2f);
                if (arm) return new Vector3(-35f, 0, side * 47f);
                if (forearm) return new Vector3(-35f + wave * 4f, 0, 0);
                if (thigh) return new Vector3(-10f, 0, side * 8f);
                if (shin) return new Vector3(17f, 0, 0);
                if (bone.StartsWith("Ear")) return new Vector3(15f + wave * 7f, 0, side * 22f);
                if (bone.StartsWith("Brow")) return new Vector3(0, 0, -side * 12f);
                if (bone == "Jaw") return new Vector3(13f, 0, 0);
                if (tail) return new Vector3(-12f, wave * 8f, 0);
            }
            if (state == "Land")
            {
                float squash = Mathf.Sin(Mathf.PI * t);
                if (bone == "Chest") return new Vector3(17f * squash, 0, 0);
                if (bone == "Head") return new Vector3(-12f * squash, 0, 0);
                if (arm) return new Vector3(-18f * squash, 0, side * (8f + squash * 12f));
                if (forearm) return new Vector3(-20f * squash, 0, 0);
                if (thigh) return new Vector3(-33f * squash, 0, 0);
                if (shin) return new Vector3(60f * squash, 0, 0);
                if (bone.StartsWith("Foot")) return new Vector3(-27f * squash, 0, 0);
                if (bone.StartsWith("Ear")) return new Vector3(18f * squash, 0, side * 15f * squash);
                if (tail) return new Vector3(13f * squash, -8f * squash, 0);
            }
            return Vector3.zero;
        }

        private static float HipOffset(string state, float t)
        {
            if (state == "Idle") return Mathf.Sin(t * Mathf.PI * 2f) * 0.012f;
            if (state == "Run") return Mathf.Sin(t * Mathf.PI * 4f) * 0.045f + 0.025f;
            if (state == "Jump") return 0.06f;
            if (state == "Fall") return 0.035f;
            return -Mathf.Sin(t * Mathf.PI) * 0.15f;
        }

        private static AnimatorController CreateController(AnimationClip idle, AnimationClip run,
            AnimationClip jump, AnimationClip fall, AnimationClip land)
        {
            string path = Output + "/RatLocomotion.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            foreach (var transition in machine.anyStateTransitions)
                machine.RemoveAnyStateTransition(transition);
            foreach (var state in machine.states)
                machine.RemoveState(state.state);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("StrideRate", AnimatorControllerParameterType.Float);
            var idleState = machine.AddState("Idle", new Vector3(250, 30)); idleState.motion = idle;
            var runState = machine.AddState("Run", new Vector3(500, 30)); runState.motion = run;
            runState.speedParameter = "StrideRate"; runState.speedParameterActive = true;
            var jumpState = machine.AddState("Jump", new Vector3(250, 180)); jumpState.motion = jump;
            var fallState = machine.AddState("Fall", new Vector3(500, 180)); fallState.motion = fall;
            var landState = machine.AddState("Land", new Vector3(380, 330)); landState.motion = land;
            machine.defaultState = idleState;
            foreach (var state in machine.states) state.state.writeDefaultValues = false;
            var toRun = Transition(idleState.AddTransition(runState), 0.12f);
            toRun.AddCondition(AnimatorConditionMode.Greater, 0.10f, "Speed");
            var toIdle = Transition(runState.AddTransition(idleState), 0.15f);
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.07f, "Speed");
            var toJump = Transition(machine.AddAnyStateTransition(jumpState), 0.06f);
            toJump.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            toJump.AddCondition(AnimatorConditionMode.Greater, 0.10f, "VerticalSpeed");
            var toFall = Transition(machine.AddAnyStateTransition(fallState), 0.10f);
            toFall.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            toFall.AddCondition(AnimatorConditionMode.Less, 0.10f, "VerticalSpeed");
            var toLand = Transition(machine.AddAnyStateTransition(landState), 0.04f);
            toLand.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            toLand.AddCondition(AnimatorConditionMode.If, 0, "Land");
            var landIdle = Transition(landState.AddTransition(idleState), 0.09f);
            landIdle.hasExitTime = true; landIdle.exitTime = 0.85f;
            landIdle.AddCondition(AnimatorConditionMode.Less, 0.10f, "Speed");
            var landRun = Transition(landState.AddTransition(runState), 0.09f);
            landRun.hasExitTime = true; landRun.exitTime = 0.65f;
            landRun.AddCondition(AnimatorConditionMode.Greater, 0.099f, "Speed");
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorStateTransition Transition(AnimatorStateTransition transition, float duration)
        {
            transition.duration = duration;
            transition.hasFixedDuration = true;
            transition.hasExitTime = false;
            transition.canTransitionToSelf = false;
            return transition;
        }

        private static void AddCombatLayer(AnimatorController controller, AnimationClip punch, AnimationClip hit)
        {
            int existing = Array.FindIndex(controller.layers, layer => layer.name == "Combat");
            if (existing >= 0) controller.RemoveLayer(existing);
            controller.AddParameter("Punch", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("PunchRate", AnimatorControllerParameterType.Float);
            controller.AddParameter("HitRate", AnimatorControllerParameterType.Float);
            var mask = new AvatarMask { name = "Rat Upper Body" };
            var transforms = visual.GetComponentsInChildren<Transform>(true);
            mask.transformCount = transforms.Length;
            for (int i = 0; i < transforms.Length; i++)
            {
                string path = AnimationUtility.CalculateTransformPath(transforms[i], visual);
                mask.SetTransformPath(i, path);
                mask.SetTransformActive(i, path.StartsWith("Rig/Hips/Spine"));
            }
            mask = SaveAsset(mask, Output + "/RatUpperBody.mask");
            var machine = new AnimatorStateMachine { name = "Combat" };
            AssetDatabase.AddObjectToAsset(machine, controller);
            var ready = machine.AddState("Ready", new Vector3(200, 30));
            var punchState = machine.AddState("Punch", new Vector3(420, 30)); punchState.motion = punch;
            punchState.speedParameter = "PunchRate"; punchState.speedParameterActive = true;
            var hitState = machine.AddState("Hit", new Vector3(420, 170)); hitState.motion = hit;
            hitState.speedParameter = "HitRate"; hitState.speedParameterActive = true;
            machine.defaultState = ready;
            foreach (var state in machine.states) state.state.writeDefaultValues = false;
            var toHit = Transition(machine.AddAnyStateTransition(hitState), 0.035f);
            toHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            var toPunch = Transition(machine.AddAnyStateTransition(punchState), 0.025f);
            toPunch.AddCondition(AnimatorConditionMode.If, 0, "Punch");
            foreach (var state in new[] { punchState, hitState })
            {
                var finish = Transition(state.AddTransition(ready), 0.05f);
                finish.hasExitTime = true; finish.exitTime = 1f;
            }
            controller.AddLayer(new AnimatorControllerLayer
            {
                name = "Combat", stateMachine = machine, avatarMask = mask,
                blendingMode = AnimatorLayerBlendingMode.Override, defaultWeight = 0f
            });
            EditorUtility.SetDirty(controller);
        }

        private static void AddCarryLayer(AnimatorController controller)
        {
            int existing = Array.FindIndex(controller.layers, layer => layer.name == "Carry");
            if (existing >= 0) controller.RemoveLayer(existing);
            controller.AddParameter("Holding", AnimatorControllerParameterType.Bool);
            var machine = new AnimatorStateMachine { name = "Carry" };
            AssetDatabase.AddObjectToAsset(machine, controller);
            var ready = machine.AddState("Ready");
            var hold = machine.AddState("Hold"); hold.motion = CreateClip("Hold", 1f, true);
            machine.defaultState = ready;
            foreach (string name in new[] { "Grab", "Release", "Throw" })
            {
                var state = machine.AddState(name); state.motion = CreateClip(name, name == "Grab" ? 0.25f : 0.4f, false);
                var finish = Transition(state.AddTransition(name == "Grab" ? hold : ready), 0.05f);
                finish.hasExitTime = true; finish.exitTime = 1f;
            }
            var stop = Transition(hold.AddTransition(ready), 0.05f);
            stop.AddCondition(AnimatorConditionMode.IfNot, 0, "Holding");
            foreach (var state in machine.states) state.state.writeDefaultValues = false;
            controller.AddLayer(new AnimatorControllerLayer { name = "Carry", stateMachine = machine,
                avatarMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(Output + "/RatUpperBody.mask"),
                blendingMode = AnimatorLayerBlendingMode.Override, defaultWeight = 0f });
            EditorUtility.SetDirty(controller);
        }

        private static void Curve(AnimationClip clip, string path, string property, float[] times, float[] values)
        {
            var curve = new AnimationCurve(times.Select((time, index) => new Keyframe(time, values[index])).ToArray());
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
        }

        private static void Joint(string name, string parent, Vector3 position)
        {
            var bone = new GameObject(name).transform;
            bone.SetParent(parent == null ? visual : Joints[parent], false);
            bone.localPosition = position;
            Joints.Add(name, bone);
        }

        private static Transform Shape(string name, string bone, Vector3 position, Vector3 scale,
            string material, PrimitiveType type = PrimitiveType.Sphere)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(Joints[bone], false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = Materials[material];
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            return part.transform;
        }

        private static void Segment(string name, string bone, Vector3 start, Vector3 end, float width, string material)
        {
            Vector3 delta = end - start;
            var part = Shape(name, bone, (start + end) * 0.5f, new Vector3(width, width, delta.magnitude + width * 0.7f), material);
            part.localRotation = Quaternion.LookRotation(delta);
        }

        private static void TailSegment(string bone, string child, float width)
        {
            Segment(bone + " Skin", bone, Vector3.zero, Joints[child].localPosition, width, "Pink");
            Shape(bone + " Joint", bone, Vector3.zero, Vector3.one * width, "DeepPink");
        }

        private static void CreatePalette()
        {
            var colors = new Dictionary<string, Color>
            {
                { "Fur", new Color(0.34f, 0.29f, 0.44f) },
                { "LightFur", new Color(0.48f, 0.42f, 0.56f) },
                { "Cream", new Color(0.94f, 0.84f, 0.67f) },
                { "Pink", new Color(0.94f, 0.45f, 0.50f) },
                { "DeepPink", new Color(0.67f, 0.23f, 0.34f) },
                { "Nose", new Color(0.33f, 0.08f, 0.20f) },
                { "Ink", new Color(0.055f, 0.028f, 0.10f) },
                { "White", new Color(1f, 0.98f, 0.90f) },
                { "Teal", new Color(0.025f, 0.69f, 0.63f) },
                { "DarkTeal", new Color(0.02f, 0.26f, 0.29f) },
                { "Orange", new Color(1f, 0.54f, 0.09f) },
                { "Purple", new Color(0.24f, 0.09f, 0.38f) }
            };
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
            foreach (var item in colors)
            {
                var material = new Material(shader) { name = "Rat " + item.Key };
                material.SetColor("_BaseColor", item.Value);
                material.SetFloat("_Smoothness", item.Key == "Ink" || item.Key == "Nose" ? 0.5f : 0.22f);
                Materials[item.Key] = SaveAsset(material, Palette + "/Rat" + item.Key + ".mat");
            }
        }

        private static T SaveAsset<T>(T asset, string path) where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }
            EditorUtility.CopySerialized(asset, existing);
            UnityEngine.Object.DestroyImmediate(asset);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
    }
}
