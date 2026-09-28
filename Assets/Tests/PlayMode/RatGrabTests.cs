using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GymRats.Tests
{
    public class RatGrabTests
    {
        private InputTestFixture input;
        private Keyboard keyboard;
        private Gamepad gamepad;
        private RatMotor player;
        private RatMotor rat;
        private RatGrabber grabber;
        private Grabbable ball;
        private GameObject wall;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            input = new InputTestFixture(); input.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>(); gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("GymPrototype");
            var rats = Object.FindObjectsByType<RatMotor>();
            player = rats.Single(x => x.AcceptsPlayerInput); rat = rats.Single(x => !x.AcceptsPlayerInput);
            grabber = player.GetComponent<RatGrabber>();
            ball = Object.FindObjectsByType<Grabbable>().Single(x => x.name == "Medicine Ball");
            PlaceRat(new Vector3(0, 0.05f, 1.7f));
            player.Facing.rotation = Quaternion.identity;
            yield return new WaitForSeconds(0.2f);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (player != null) Object.Destroy(player.gameObject);
            if (rat != null) Object.Destroy(rat.gameObject);
            if (ball != null) Object.Destroy(ball.gameObject);
            if (wall != null) Object.Destroy(wall);
            yield return null;
            input.TearDown();
        }

        [UnityTest]
        public IEnumerator KeyboardRatGrabHoldReleaseRestoresPhysicsAndPunch()
        {
            input.Press(keyboard.eKey);
            yield return new WaitForSeconds(0.12f);
            Assert.That(grabber.HeldTarget, Is.EqualTo(rat.GetComponent<Grabbable>()));
            Assert.That(rat.GetComponent<CharacterController>().enabled, Is.False);
            AssertState("Grab");
            Assert.That(grabber.TryGrab(), Is.False);
            Assert.That(player.GetComponent<RatCombat>().TryPunch(), Is.False);
            yield return new WaitForSeconds(0.4f);
            AssertState("Hold");
            Assert.That(Vector3.Distance(grabber.HoldCenter, grabber.HeldTarget.Center), Is.LessThan(0.02f));
            input.Release(keyboard.eKey); input.Press(keyboard.eKey);
            yield return new WaitForSeconds(0.1f);
            AssertState("Release");
            Assert.That(grabber.IsHolding, Is.False);
            Assert.That(rat.GetComponent<CharacterController>().enabled, Is.True);
            yield return new WaitForSeconds(0.8f);
            Assert.That(rat.IsGrounded, Is.True);
            Assert.That(player.GetComponent<RatCombat>().TryPunch(), Is.True);
        }

        [UnityTest]
        public IEnumerator KeyboardThrowsRatForwardAndItLands()
        {
            input.Press(keyboard.eKey);
            yield return new WaitForSeconds(0.5f);
            Vector3 before = rat.transform.position;
            input.Press(keyboard.rKey);
            yield return new WaitForSeconds(0.15f);
            AssertState("Throw");
            Assert.That(grabber.IsHolding, Is.False);
            Assert.That(rat.transform.position.z, Is.GreaterThan(before.z + 0.6f));
            Assert.That(rat.transform.position.y, Is.GreaterThan(before.y));
            yield return new WaitForSeconds(1.2f);
            Assert.That(rat.IsGrounded, Is.True);
            Assert.That(rat.IsRecovering, Is.False);
        }

        [UnityTest]
        public IEnumerator GamepadCarriesAndThrowsBallInNewFacingDirection()
        {
            PlaceRat(new Vector3(6, 0.05f, 4));
            var body = ball.GetComponent<Rigidbody>(); body.position = new Vector3(0, 0.5f, 1.4f);
            Physics.SyncTransforms();
            input.Press(gamepad.buttonNorth);
            yield return new WaitForSeconds(0.5f);
            Assert.That(grabber.HeldTarget, Is.EqualTo(ball));
            Assert.That(body.isKinematic, Is.True);
            input.Set(gamepad.leftStick, Vector2.right); input.Press(gamepad.buttonSouth);
            yield return new WaitForSeconds(0.3f);
            Assert.That(player.transform.position.x, Is.GreaterThan(0.5f));
            Assert.That(player.transform.position.y, Is.GreaterThan(0.6f));
            Assert.That(grabber.IsHolding, Is.True);
            yield return new WaitForEndOfFrame();
            Assert.That(Vector3.Distance(ball.Center, grabber.HoldCenter), Is.LessThan(0.05f));
            Vector3 view = Camera.main.WorldToViewportPoint(ball.Center);
            Assert.That(view.x, Is.InRange(0.05f, 0.95f)); Assert.That(view.y, Is.InRange(0.05f, 0.95f));
            Vector3 before = ball.transform.position;
            input.Press(gamepad.rightShoulder);
            yield return new WaitForSeconds(0.15f);
            Assert.That(body.isKinematic, Is.False);
            Assert.That(ball.GetComponent<Collider>().enabled, Is.True);
            Assert.That(ball.transform.position.x, Is.GreaterThan(before.x + 0.7f));
            Assert.That(body.linearVelocity.y, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator BallReleaseSettlesAndCanBeGrabbedAgain()
        {
            PlaceRat(new Vector3(6, 0.05f, 4));
            var body = ball.GetComponent<Rigidbody>(); body.position = new Vector3(0, 0.5f, 1.4f);
            Physics.SyncTransforms();
            Assert.That(grabber.TryGrab(), Is.True);
            yield return new WaitForSeconds(0.5f);
            input.Press(gamepad.buttonNorth);
            yield return new WaitForSeconds(1.5f);
            Assert.That(grabber.IsHolding, Is.False);
            Assert.That(body.isKinematic, Is.False);
            Assert.That(ball.transform.position.y, Is.InRange(0.4f, 0.6f));
            Assert.That(body.linearVelocity.magnitude, Is.LessThan(0.2f));
            Assert.That(grabber.TryGrab(), Is.True);
        }

        [UnityTest]
        public IEnumerator SelfRangeWallAndExclusiveOwnershipAreEnforced()
        {
            Assert.That(player.GetComponent<Grabbable>().CanAcquire(grabber), Is.False);
            PlaceRat(new Vector3(0, 0.05f, 5));
            Assert.That(grabber.TryGrab(), Is.False);
            PlaceRat(new Vector3(0, 0.05f, 1.7f));
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0, 2f, 0.9f); wall.transform.localScale = new Vector3(3, 4, 0.15f);
            Physics.SyncTransforms();
            Assert.That(grabber.TryGrab(), Is.False);
            Object.Destroy(wall); yield return null;
            Assert.That(grabber.TryGrab(), Is.True);
            Assert.That(rat.GetComponent<Grabbable>().CanAcquire(grabber), Is.False);
            Assert.That(grabber.TryGrab(), Is.False);
            Assert.That(player.GetComponent<Grabbable>().CanAcquire(rat.GetComponent<RatGrabber>()), Is.False);
        }

        [UnityTest]
        public IEnumerator CarryPathWallDropsTargetAndCooldownRejectsRegrab()
        {
            Assert.That(grabber.TryGrab(), Is.True);
            yield return new WaitForSeconds(0.5f);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0, 2f, 2.5f); wall.transform.localScale = new Vector3(6, 4, 0.1f);
            Physics.SyncTransforms(); input.Press(keyboard.wKey);
            yield return new WaitForSeconds(0.25f);
            Assert.That(grabber.IsHolding, Is.False);
            Assert.That(rat.transform.position.z, Is.LessThan(2.1f));
            Assert.That(rat.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(grabber.TryGrab(), Is.False);
        }

        [UnityTest]
        public IEnumerator HolderHitAndDisableRestoreHeldTargets()
        {
            Assert.That(grabber.TryGrab(), Is.True);
            player.GetComponent<RatHitReceiver>().ReceiveHit(Vector3.back * 2, 0.2f);
            yield return null;
            Assert.That(grabber.IsHolding, Is.False);
            Assert.That(rat.GetComponent<CharacterController>().enabled, Is.True);
            yield return new WaitForSeconds(0.7f);
            PlaceRat(player.transform.position + Vector3.forward * 1.7f);
            Assert.That(grabber.TryGrab(), Is.True);
            grabber.enabled = false;
            Assert.That(grabber.IsHolding, Is.False);
            Assert.That(rat.GetComponent<Grabbable>().IsHeld, Is.False);
            Assert.That(rat.GetComponent<CharacterController>().enabled, Is.True);
        }

        private void PlaceRat(Vector3 position)
        {
            var capsule = rat.GetComponent<CharacterController>(); capsule.enabled = false;
            rat.transform.position = position; capsule.enabled = true; Physics.SyncTransforms();
        }

        private void AssertState(string name)
        {
            var animator = player.GetComponentInChildren<Animator>();
            Assert.That(animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Carry")).IsName(name), Is.True, name);
        }
    }
}
