using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GymRats.Tests
{
    public class RatMovementTests
    {
        private InputTestFixture input;
        private RatMotor rat;
        private Keyboard keyboard;
        private Gamepad gamepad;
        private Camera camera;
        private RatFollowCamera follow;
        private GameObject obstacle;

        [UnitySetUp]
        public IEnumerator LoadPrototype()
        {
            // Keep input reset and cleanup in the same coroutine lifecycle as scene loading.
            input = new InputTestFixture();
            input.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("GymPrototype");
            rat = Object.FindFirstObjectByType<RatMotor>();
            camera = Camera.main;
            follow = camera.GetComponent<RatFollowCamera>();
            Assert.That(rat, Is.Not.Null);
            yield return new WaitForSeconds(0.2f);
            Assert.That(rat.IsGrounded, Is.True, "Rat must settle onto the floor.");
        }

        [UnityTearDown]
        public IEnumerator RemoveSceneObjects()
        {
            // Destroy cloned actions before the fixture restores the real input devices.
            if (rat != null)
                Object.Destroy(rat.gameObject);
            if (obstacle != null)
                Object.Destroy(obstacle);
            yield return null;
            input?.TearDown();
            input = null;
        }

        [UnityTest]
        public IEnumerator KeyboardMovementUsesCameraHeadingTurnsAndStops()
        {
            follow.enabled = false;
            camera.transform.rotation = Quaternion.Euler(25f, 45f, 0f);
            Vector3 direction = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            Vector3 start = rat.transform.position;
            Press(keyboard.wKey);
            yield return new WaitForSeconds(0.4f);
            Vector3 displacement = rat.transform.position - start;
            Assert.That(Vector3.Dot(displacement, direction), Is.GreaterThan(1.2f));
            Assert.That(Vector3.Cross(displacement, direction).magnitude, Is.LessThan(0.15f));
            Assert.That(Vector3.Angle(rat.transform.Find("Muscular Rat").forward, direction), Is.LessThan(8f));
            Release(keyboard.wKey);
            yield return new WaitForSeconds(0.2f);
            Vector3 stopped = rat.transform.position;
            yield return new WaitForSeconds(0.2f);
            Assert.That(Vector3.Distance(stopped, rat.transform.position), Is.LessThan(0.03f));
        }

        [UnityTest]
        public IEnumerator KeyboardJumpLandsAndHoldingDoesNotAutoJump()
        {
            float baseline = rat.transform.position.y;
            Press(keyboard.spaceKey);
            float peak = baseline;
            float until = Time.time + 1.3f;
            while (Time.time < until)
            {
                peak = Mathf.Max(peak, rat.transform.position.y);
                AssertRatVisible();
                yield return null;
            }
            Assert.That(peak - baseline, Is.InRange(1.1f, 1.85f));
            Assert.That(rat.IsGrounded, Is.True);
            Assert.That(rat.transform.position.y, Is.EqualTo(baseline).Within(0.08f));
            yield return new WaitForSeconds(0.3f);
            Assert.That(rat.IsGrounded, Is.True, "Holding jump must not repeatedly jump.");
            Release(keyboard.spaceKey);
            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(0.12f);
            Assert.That(rat.transform.position.y, Is.GreaterThan(baseline + 0.5f));
        }

        [UnityTest]
        public IEnumerator GamepadSupportsAnalogMovementAndSingleJump()
        {
            Set(gamepad.leftStick, new Vector2(0.5f, 0f));
            yield return new WaitForSeconds(0.3f);
            Assert.That(rat.PlanarVelocity.magnitude, Is.InRange(1f, rat.MoveSpeed - 0.5f));
            Set(gamepad.leftStick, Vector2.right);
            yield return new WaitForSeconds(0.2f);
            Assert.That(rat.PlanarVelocity.magnitude, Is.EqualTo(rat.MoveSpeed).Within(0.1f));
            Set(gamepad.leftStick, Vector2.zero);
            yield return new WaitForSeconds(0.2f);
            float baseline = rat.transform.position.y;
            Press(gamepad.buttonSouth);
            yield return new WaitForSeconds(0.12f);
            Release(gamepad.buttonSouth);
            Press(gamepad.buttonSouth);
            float peak = rat.transform.position.y;
            float until = Time.time + 1.2f;
            while (Time.time < until)
            {
                peak = Mathf.Max(peak, rat.transform.position.y);
                yield return null;
            }
            Assert.That(peak - baseline, Is.InRange(1.1f, 1.85f), "A second airborne press must not add another jump.");
            Assert.That(rat.IsGrounded, Is.True);
        }

        [UnityTest]
        public IEnumerator DiagonalInputCannotExceedMovementSpeedAndCameraFollows()
        {
            Vector3 cameraStart = camera.transform.position;
            Press(keyboard.wKey);
            Press(keyboard.dKey);
            yield return new WaitForSeconds(0.5f);
            Assert.That(rat.PlanarVelocity.magnitude, Is.EqualTo(rat.MoveSpeed).Within(0.05f));
            Assert.That(Vector3.Distance(cameraStart, camera.transform.position), Is.GreaterThan(1f));
            AssertRatVisible();
            Release(keyboard.wKey);
            Release(keyboard.dKey);
            yield return new WaitForSeconds(0.5f);
            Vector3 settled = camera.transform.position;
            yield return new WaitForSeconds(0.2f);
            Assert.That(Vector3.Distance(settled, camera.transform.position), Is.LessThan(0.05f));
        }

        [UnityTest]
        public IEnumerator CameraAvoidsObstructionAndRestoresDistance()
        {
            Vector3 focus = rat.transform.position + Vector3.up * 1.3f;
            float originalDistance = Vector3.Distance(focus, camera.transform.position);
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Test Camera Obstruction";
            obstacle.transform.position = Vector3.Lerp(focus, camera.transform.position, 0.55f);
            obstacle.transform.localScale = new Vector3(6f, 5f, 0.3f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.3f);
            Assert.That(Vector3.Distance(focus, camera.transform.position), Is.LessThan(originalDistance * 0.7f));
            AssertRatVisible();
            Object.Destroy(obstacle);
            yield return new WaitForSeconds(0.7f);
            Assert.That(Vector3.Distance(focus, camera.transform.position), Is.EqualTo(originalDistance).Within(0.15f));
        }

        [UnityTest]
        public IEnumerator WalkingOffArenaFallsAndRespawns()
        {
            Press(keyboard.dKey);
            bool fell = false;
            bool recovered = false;
            float until = Time.time + 5f;
            while (Time.time < until)
            {
                fell |= rat.transform.position.y < -2f;
                if (fell && Mathf.Abs(rat.transform.position.x) < 0.5f && rat.transform.position.y > -0.1f)
                {
                    recovered = true;
                    break;
                }
                yield return null;
            }
            Release(keyboard.dKey);
            Assert.That(fell, Is.True);
            Assert.That(recovered, Is.True);
            yield return new WaitForSeconds(0.4f);
            Assert.That(rat.IsGrounded, Is.True);
            AssertRatVisible();
        }

        [UnityTest]
        public IEnumerator JumpHitsCeilingAndReturnsToGround()
        {
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Test Ceiling";
            obstacle.transform.position = new Vector3(0f, 3.2f, 0f);
            obstacle.transform.localScale = new Vector3(4f, 0.2f, 4f);
            Physics.SyncTransforms();
            Press(keyboard.spaceKey);
            float peak = rat.transform.position.y;
            float until = Time.time + 0.8f;
            while (Time.time < until)
            {
                peak = Mathf.Max(peak, rat.transform.position.y);
                yield return null;
            }
            Assert.That(peak, Is.LessThan(0.4f));
            Assert.That(rat.IsGrounded, Is.True);
        }

        private void AssertRatVisible()
        {
            foreach (float height in new[] { 0.15f, 2.95f })
            {
                Vector3 point = camera.WorldToViewportPoint(rat.transform.position + Vector3.up * height);
                Assert.That(point.z, Is.GreaterThan(camera.nearClipPlane));
                Assert.That(point.x, Is.InRange(0.05f, 0.95f));
                Assert.That(point.y, Is.InRange(0.02f, 0.98f));
            }
        }

        private void Press(ButtonControl button) => input.Press(button);
        private void Release(ButtonControl button) => input.Release(button);
        private void Set(StickControl stick, Vector2 value) => input.Set(stick, value);
    }
}
