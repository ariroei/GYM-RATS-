using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GymRats.Tests
{
    public class PrototypeSetupTests
    {
        private const string ScenePath = "Assets/Scenes/GymPrototype.unity";

        [Test]
        public void PrototypeHasSolidFloorLightingAndVisibleRat()
        {
            // A preview scene avoids replacing or saving the user's open scenes.
            var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                var objects = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Select(transform => transform.gameObject).ToArray();
                var floor = objects.Single(obj => obj.name == "Gym Floor");
                var collider = floor.GetComponent<BoxCollider>();
                Assert.That(collider, Is.Not.Null);
                Assert.That(collider.enabled && !collider.isTrigger, Is.True);
                Assert.That(collider.Raycast(new Ray(Vector3.up * 5f, Vector3.down),
                    out var hit, 10f), Is.True, "The floor must support physics at the arena center.");
                Assert.That(hit.point.y, Is.EqualTo(0f).Within(0.01f));

                var cameras = objects.Select(obj => obj.GetComponent<Camera>())
                    .Where(camera => camera != null && camera.enabled).ToArray();
                Assert.That(cameras.Length, Is.EqualTo(1));
                Assert.That(cameras[0].CompareTag("MainCamera"), Is.True);
                cameras[0].aspect = 16f / 9f;
                var rat = objects.Select(obj => obj.GetComponent<RatMotor>()).First(motor => motor != null && motor.AcceptsPlayerInput);
                Assert.That(rat.GetComponent<CharacterController>(), Is.Not.Null);
                Assert.That(cameras[0].GetComponent<RatFollowCamera>().Target, Is.EqualTo(rat.transform));
                var animator = rat.GetComponentInChildren<Animator>();
                Assert.That(animator, Is.Not.Null);
                Assert.That(animator.avatar.isValid && !animator.avatar.isHuman, Is.True);
                Assert.That(animator.applyRootMotion, Is.False);
                Assert.That(rat.GetComponent<RatAnimatorDriver>(), Is.Not.Null);
                Assert.That(rat.GetComponent<RatVisualMotion>(), Is.Null);
                Assert.That(animator.runtimeAnimatorController.animationClips.Length, Is.EqualTo(7));
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    Assert.That(clip.length, Is.GreaterThan(0.2f));
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                        Assert.That(animator.transform.Find(binding.path), Is.Not.Null, clip.name + ": " + binding.path);
                }
                foreach (var height in new[] { 0.1f, 2.95f })
                {
                    var point = cameras[0].WorldToViewportPoint(rat.transform.position + Vector3.up * height);
                    Assert.That(point.z, Is.GreaterThan(0));
                    Assert.That(point.x, Is.InRange(0f, 1f));
                    Assert.That(point.y, Is.InRange(0f, 1f));
                }

                Assert.That(objects.Select(obj => obj.GetComponent<Light>()).Any(light => light != null
                    && light.enabled && light.type == LightType.Directional), Is.True);
                Assert.That(objects.Count(obj => obj.GetComponent<AudioListener>() != null), Is.EqualTo(1));
                foreach (var obj in objects)
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj), Is.Zero, obj.name);
                foreach (var renderer in objects.Select(obj => obj.GetComponent<Renderer>()).Where(r => r != null))
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.That(material, Is.Not.Null);
                    Assert.That(material.shader.isSupported, Is.True, material.name);
                    Assert.That(material.shader.name, Does.StartWith("Universal Render Pipeline/"));
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void PrototypeIsFirstEnabledBuildScene()
        {
            Assert.That(EditorBuildSettings.scenes.First(scene => scene.enabled).path, Is.EqualTo(ScenePath));
            Assert.That(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline, Is.Not.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GameplayBindingsResolveForKeyboardAndGamepad(bool useGamepad)
        {
            InputDevice device = useGamepad
                ? (InputDevice)InputSystem.AddDevice<Gamepad>()
                : InputSystem.AddDevice<Keyboard>();
            InputActionAsset actions = null;
            try
            {
                var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
                Assert.That(source, Is.Not.Null);
                actions = Object.Instantiate(source);
                actions.devices = new[] { device };
                var scheme = actions.controlSchemes.First(item => item.name == (useGamepad ? "Gamepad" : "Keyboard&Mouse"));
                using (var match = scheme.PickDevicesFrom(new[] { device }))
                    Assert.That(match.isSuccessfulMatch, Is.True, "The keyboard scheme must not require a mouse.");
                actions.bindingMask = InputBinding.MaskByGroup(scheme.bindingGroup);
                var player = actions.FindActionMap("Player", true);
                player.Enable();
                foreach (var name in new[] { "Move", "Attack", "Jump", "Interact", "Pause" })
                {
                    var action = player.FindAction(name, true);
                    Assert.That(action.controls.Count, Is.GreaterThan(0), name);
                    Assert.That(action.controls.All(control => control.device == device), Is.True, name);
                }
                var pause = player.FindAction("Pause", true);
                Assert.That(pause.controls.Any(control => control.name == (useGamepad ? "start" : "escape")), Is.True);
            }
            finally
            {
                if (actions != null)
                {
                    actions.Disable();
                    Object.DestroyImmediate(actions);
                }
                InputSystem.RemoveDevice(device);
            }
        }
    }
}
