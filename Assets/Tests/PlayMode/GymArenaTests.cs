using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GymRats.Tests
{
    public class GymArenaTests
    {
        private InputTestFixture input;
        private Gamepad gamepad;
        private RatMotor player;
        private GymArenaLayout arena;
        private Camera camera;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            input = new InputTestFixture(); input.Setup(); gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("GymPrototype");
            SoloGameplayFixture.Configure();
            player = Object.FindObjectsByType<RatMotor>().Single(x => x.GetComponent<RatInputOwner>().PlayerNumber == 1);
            arena = Object.FindFirstObjectByType<GymArenaLayout>(); camera = Camera.main;
            yield return new WaitForSeconds(0.2f);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (var rat in Object.FindObjectsByType<RatMotor>()) Object.Destroy(rat.gameObject);
            yield return null; input.TearDown();
        }

        [UnityTest]
        public IEnumerator FourSpawnPositionsHaveFloorAndRoomForRats()
        {
            Assert.That(arena.SpawnPoints.Length, Is.EqualTo(4));
            foreach (var rat in Object.FindObjectsByType<RatMotor>()) rat.GetComponent<CharacterController>().enabled = false;
            Physics.SyncTransforms();
            foreach (var spawn in arena.SpawnPoints)
            {
                Assert.That(Physics.Raycast(spawn.position + Vector3.up, Vector3.down, out var floor, 2f), Is.True);
                Assert.That(floor.point.y, Is.EqualTo(0f).Within(0.03f));
                Assert.That(Physics.CheckCapsule(spawn.position + Vector3.up * 0.6f,
                    spawn.position + Vector3.up * 2.5f, 0.55f, ~0, QueryTriggerInteraction.Ignore), Is.False, spawn.name);
            }
            Assert.That(arena.TryGetFreeSpawn(out var position), Is.True);
            Assert.That(arena.Contains(position), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CameraFramesRatInZonesCornersAndBehindCover()
        {
            foreach (var point in new[] { new Vector3(0,0,0), new Vector3(11,0,-10), new Vector3(-11,0,-10),
                new Vector3(11,0,0), new Vector3(-11,0,0), new Vector3(11,0,9), new Vector3(-11,0,9),
                new Vector3(0,0,10), new Vector3(-10,0,8), new Vector3(4.1f,0,11), new Vector3(-4.1f,0,11),
                new Vector3(0,0,-10), new Vector3(-5.5f,0,-2.2f), new Vector3(5.5f,0,6.5f) })
            {
                PlacePlayer(point + Vector3.up * 0.05f);
                yield return new WaitForSeconds(0.12f);
                camera.GetComponent<RatFollowCamera>().SnapToTarget();
                yield return new WaitForEndOfFrame();
                foreach (float height in new[] { 0.15f, 1.5f, 2.95f })
                {
                    Vector3 world = player.transform.position + Vector3.up * height;
                    Vector3 view = camera.WorldToViewportPoint(world);
                    Assert.That(view.z, Is.GreaterThan(camera.nearClipPlane), point.ToString());
                    Assert.That(view.x, Is.InRange(0.02f, 0.98f), point.ToString());
                    Assert.That(view.y, Is.InRange(0.02f, 0.98f), point + " height=" + height);
                    if (height < 1f) continue; // Low cover may hide feet; the face and upper body must remain visible.
                    Vector3 delta = world - camera.transform.position;
                    var blockers = Physics.RaycastAll(camera.transform.position, delta.normalized, delta.magnitude,
                        ~(1 << 2), QueryTriggerInteraction.Ignore).Where(hit => !hit.transform.IsChildOf(player.transform));
                    Assert.That(blockers.Any(), Is.False, point + " blocked by " + string.Join(",", blockers.Select(hit => hit.collider.name)));
                }
            }
        }

        [UnityTest]
        public IEnumerator PlayerCanRunAroundFightMatAndJumpOverLowCover()
        {
            foreach (var goal in new[] { new Vector3(0,0,-7.5f), new Vector3(7.7f,0,-7.5f), new Vector3(7.7f,0,7.5f),
                new Vector3(-7.7f,0,7.5f), new Vector3(-7.7f,0,-7.5f), Vector3.zero })
            {
                float deadline = Time.time + 5f;
                while (Vector3.ProjectOnPlane(goal - player.transform.position, Vector3.up).magnitude > 0.3f && Time.time < deadline)
                {
                    Vector3 delta = goal - player.transform.position;
                    input.Set(gamepad.leftStick, new Vector2(delta.x, delta.z).normalized);
                    Assert.That(arena.Contains(player.transform.position), Is.True);
                    Assert.That(player.transform.position.y, Is.GreaterThan(-0.1f));
                    yield return null;
                }
                input.Set(gamepad.leftStick, Vector2.zero);
                Assert.That(Vector3.ProjectOnPlane(goal - player.transform.position, Vector3.up).magnitude, Is.LessThan(0.5f), goal.ToString());
                yield return new WaitForSeconds(0.15f);
            }
            PlacePlayer(new Vector3(-5.5f, 0.05f, -5.7f));
            yield return new WaitForSeconds(0.2f);
            input.Set(gamepad.leftStick, Vector2.up);
            yield return new WaitForSeconds(0.04f);
            input.Press(gamepad.buttonSouth);
            yield return new WaitForSeconds(0.8f);
            Assert.That(player.transform.position.z, Is.GreaterThan(-2.5f));
        }

        [UnityTest]
        public IEnumerator EveryPropCanBeGrabbedAtItsPlacedTrainingZone()
        {
            var grabber = player.GetComponent<RatGrabber>();
            foreach (var prop in Object.FindObjectsByType<ThrownEquipment>())
            {
                var item = prop.GetComponent<Grabbable>();
                PlacePlayer(new Vector3(item.Center.x, 0.05f, item.Center.z - 1.4f));
                player.Facing.rotation = Quaternion.identity;
                yield return new WaitForSeconds(0.5f);
                Assert.That(grabber.TryGrab(), Is.True, prop.name);
                yield return new WaitForSeconds(0.5f);
                Assert.That(grabber.HeldTarget, Is.EqualTo(item));
                Assert.That(grabber.ReleaseHeld(true), Is.True);
                yield return new WaitForSeconds(0.5f);
                Assert.That(arena.Contains(prop.transform.position), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator PerimeterContainsFastPropsAndThrownRatWithoutRecovery()
        {
            var rat = Object.FindObjectsByType<RatMotor>().Single(x => x.GetComponent<RatInputOwner>().PlayerNumber == 2);
            var capsule = rat.GetComponent<CharacterController>(); capsule.enabled = false;
            rat.transform.position = new Vector3(0, 0.05f, 10f); capsule.enabled = true;
            rat.ApplyKnockback(Vector3.forward * 20f + Vector3.up * 7f, 0.5f);
            int index = 0;
            foreach (var prop in Object.FindObjectsByType<ThrownEquipment>())
            {
                var body = prop.GetComponent<Rigidbody>();
                body.position = new Vector3(index == 0 ? 11f : index == 1 ? -11f : 0, 5f, index == 2 ? -10f : 0);
                body.linearVelocity = (index == 0 ? Vector3.right : index == 1 ? Vector3.left : Vector3.back) * 30f + Vector3.up * 12f;
                index++;
            }
            float until = Time.time + 3f;
            while (Time.time < until)
            {
                foreach (var item in Object.FindObjectsByType<Grabbable>()) Assert.That(arena.Contains(item.transform.position), Is.True, item.name);
                yield return new WaitForFixedUpdate();
            }
            Assert.That(arena.RecoveryCount, Is.Zero, "Normal containment must come from physical walls, not teleporting.");
        }

        [UnityTest]
        public IEnumerator SafetyRecoveryRestoresRatAndPropAfterForcedEscape()
        {
            PlacePlayer(new Vector3(30, -3, 0));
            var prop = Object.FindObjectsByType<ThrownEquipment>().First();
            prop.GetComponent<Rigidbody>().position = new Vector3(-30, -3, 0);
            yield return new WaitForSeconds(0.2f);
            Assert.That(arena.Contains(player.transform.position), Is.True);
            Assert.That(arena.Contains(prop.transform.position), Is.True);
            Assert.That(arena.RecoveryCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(player.IsGrounded, Is.True);
        }

        private void PlacePlayer(Vector3 p)
        {
            var capsule = player.GetComponent<CharacterController>(); capsule.enabled = false;
            player.transform.position = p; capsule.enabled = true; Physics.SyncTransforms();
        }
    }
}
