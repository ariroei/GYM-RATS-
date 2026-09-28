using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GymRats.Tests
{
    public class GymEquipmentTests
    {
        private InputTestFixture input;
        private Keyboard keyboard;
        private Gamepad gamepad;
        private RatMotor player;
        private RatMotor rat;
        private RatGrabber grabber;
        private ThrownEquipment[] props;
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
            props = Object.FindObjectsByType<ThrownEquipment>();
            Assert.That(props.Length, Is.EqualTo(3));
            PlaceRat(rat, new Vector3(6, 0.05f, 4));
            yield return new WaitForSeconds(0.25f);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (player != null) Object.Destroy(player.gameObject);
            if (rat != null) Object.Destroy(rat.gameObject);
            foreach (var prop in props) if (prop != null) Object.Destroy(prop.gameObject);
            if (wall != null) Object.Destroy(wall);
            yield return null;
            input.TearDown();
        }

        [UnityTest] public IEnumerator DumbbellCanTumbleCarryReleaseThrowHitAndBeReused() => ExerciseProp("Dumbbell");
        [UnityTest] public IEnumerator MedicineBallCanTumbleCarryReleaseThrowHitAndBeReused() => ExerciseProp("Medicine Ball");
        [UnityTest] public IEnumerator FoamRollerCanTumbleCarryReleaseThrowHitAndBeReused() => ExerciseProp("Foam Roller");

        private IEnumerator ExerciseProp(string name)
        {
            var prop = props.Single(x => x.name == name);
            var body = prop.GetComponent<Rigidbody>();
            var item = prop.GetComponent<Grabbable>();
            // Drop with a nontrivial rotation first: subsequent pickup must use current collider orientation.
            MoveProp(body, new Vector3(0, 1f, 1.4f), Quaternion.Euler(15, 25, 40));
            yield return new WaitForSeconds(1.4f);
            Assert.That(body.position.y, Is.InRange(0.1f, 0.8f), name + " rests on floor");
            Approach(item);
            input.Press(keyboard.eKey);
            yield return new WaitForSeconds(0.5f);
            Assert.That(grabber.HeldTarget, Is.EqualTo(item), name + " pickup after floor contact");
            Assert.That(prop.GetComponentsInChildren<Collider>().All(c => !c.enabled), Is.True);
            input.Press(keyboard.dKey);
            yield return new WaitForSeconds(0.2f);
            input.Release(keyboard.dKey);
            // Process the queued key release before synthesizing another full keyboard state.
            yield return new WaitForSeconds(0.15f);
            yield return new WaitForEndOfFrame();
            Assert.That(grabber.IsHolding, Is.True);
            Assert.That(Vector3.Distance(item.Center, grabber.HoldCenter), Is.LessThan(0.02f));
            input.Release(keyboard.eKey);
            yield return null;
            input.Press(keyboard.eKey);
            yield return new WaitForSeconds(1.4f);
            input.Release(keyboard.eKey);
            Assert.That(body.isKinematic, Is.False);
            Assert.That(prop.IsArmed, Is.False, "Release without throw must not arm impacts");
            Assert.That(prop.GetComponentsInChildren<Collider>().All(c => c.enabled), Is.True);
            Assert.That(body.position.y, Is.GreaterThan(0f));

            // Use the other device for the next pickup/throw and observe real rat feedback.
            MoveProp(body, new Vector3(0, 0.7f, 1.4f), Quaternion.identity);
            PlaceRat(player, new Vector3(0, 0.05f, 0)); player.Facing.rotation = Quaternion.identity;
            yield return new WaitForSeconds(0.3f);
            Assert.That(keyboard.eKey.isPressed, Is.False, "Keyboard grab released before changing device");
            input.Press(gamepad.buttonNorth);
            yield return new WaitForSeconds(0.5f);
            Assert.That(grabber.HeldTarget, Is.EqualTo(item), name + " gamepad pickup; position=" + item.Center + " player=" + player.transform.position + " facing=" + player.Facing.forward + " acquire=" + item.CanAcquire(grabber) + " path=" + item.CanMoveTo(grabber.HoldCenter, grabber));
            PlaceRat(rat, new Vector3(0, 0.05f, 2.9f));
            var receiver = rat.GetComponent<RatHitReceiver>();
            Vector3 before = rat.transform.position;
            input.Press(gamepad.rightShoulder);
            float deadline = Time.time + 1f;
            while (receiver.ReceivedHitCount == 0 && Time.time < deadline) yield return null;
            Assert.That(receiver.ReceivedHitCount, Is.EqualTo(1), name + " thrown impact");
            Assert.That(prop.SuccessfulHits, Is.EqualTo(1));
            Assert.That(rat.IsRecovering, Is.True);
            yield return new WaitForSeconds(0.07f);
            Assert.That(rat.transform.position.z, Is.GreaterThan(before.z + 0.05f));
            var block = new MaterialPropertyBlock(); rat.GetComponentInChildren<Renderer>().GetPropertyBlock(block);
            Assert.That(block.isEmpty, Is.False, "Impact flashes the rat");
            var animator = rat.GetComponentInChildren<Animator>();
            Assert.That(animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Combat")).IsName("Hit"), Is.True);
            input.Release(gamepad.rightShoulder); input.Release(gamepad.buttonNorth);
            float settleDeadline = Time.time + 5f;
            while ((body.position.y > 0.8f || body.position.y < 0f || body.linearVelocity.magnitude > 0.5f) && Time.time < settleDeadline)
            {
                if (Mathf.Abs(body.position.x) < 9f && Mathf.Abs(body.position.z) < 6f)
                    Assert.That(body.position.y, Is.GreaterThan(-0.1f), "No tunneling through the arena floor");
                yield return new WaitForFixedUpdate();
            }
            Assert.That(receiver.ReceivedHitCount, Is.EqualTo(1), "One rat hit per throw");
            Assert.That(player.GetComponent<RatHitReceiver>().ReceivedHitCount, Is.Zero, "Thrower immunity");
            Assert.That(body.position.y, Is.GreaterThan(-0.1f));
            PlaceRat(rat, new Vector3(6, 0.05f, 4));
            Approach(item);
            Assert.That(grabber.TryGrab(), Is.True, name + " can be picked up after rat impact and floor landing");
            Assert.That(prop.IsArmed, Is.False);
            grabber.ReleaseHeld(false);
        }

        [UnityTest]
        public IEnumerator EachPropHitsSolidWallWithoutHittingRatBehindItAndRemainsUsable()
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "Test Wall";
            wall.transform.position = new Vector3(0, 2.5f, 3f); wall.transform.localScale = new Vector3(5, 5, 0.2f);
            PlaceRat(rat, new Vector3(0, 0.05f, 3.9f));
            foreach (var prop in props)
            {
                PlaceRat(player, new Vector3(0, 0.05f, 0)); player.Facing.rotation = Quaternion.identity;
                var body = prop.GetComponent<Rigidbody>();
                MoveProp(body, new Vector3(0, 0.7f, 1.4f), Quaternion.identity);
                yield return new WaitForSeconds(0.5f);
                Assert.That(grabber.TryGrab(), Is.True, prop.name);
                yield return new WaitForSeconds(0.5f);
                Assert.That(grabber.ReleaseHeld(true), Is.True);
                float maxZ = body.position.z;
                float until = Time.time + 1.8f;
                while (Time.time < until) { maxZ = Mathf.Max(maxZ, body.position.z); yield return new WaitForFixedUpdate(); }
                Assert.That(maxZ, Is.LessThan(2.95f), prop.name + " must not tunnel through wall");
                Assert.That(rat.GetComponent<RatHitReceiver>().ReceivedHitCount, Is.Zero);
                Assert.That(body.position.y, Is.GreaterThan(-0.1f));
                Approach(prop.GetComponent<Grabbable>());
                Assert.That(grabber.TryGrab(), Is.True, prop.name + " pickup after wall and floor contacts");
                grabber.ReleaseHeld(false);
                MoveProp(body, new Vector3(-7, 0.8f, -4 + System.Array.IndexOf(props, prop) * 2), Quaternion.identity);
                yield return new WaitForSeconds(0.5f);
            }
        }

        [UnityTest]
        public IEnumerator RestingAndUnthrownEquipmentDoNotDamageRats()
        {
            foreach (var prop in props)
            {
                var body = prop.GetComponent<Rigidbody>();
                MoveProp(body, rat.transform.position + Vector3.back * 0.85f + Vector3.up, Quaternion.identity);
                body.linearVelocity = Vector3.forward * 3f;
                yield return new WaitForSeconds(0.6f);
                Assert.That(prop.IsArmed, Is.False);
            }
            Assert.That(rat.GetComponent<RatHitReceiver>().ReceivedHitCount, Is.Zero);
        }

        private void Approach(Grabbable item)
        {
            PlaceRat(player, new Vector3(item.Center.x, 0.05f, item.Center.z - 1.4f));
            player.Facing.rotation = Quaternion.identity;
        }
        private static void PlaceRat(RatMotor motor, Vector3 position)
        {
            var capsule = motor.GetComponent<CharacterController>(); capsule.enabled = false;
            motor.transform.position = position; capsule.enabled = true; Physics.SyncTransforms();
        }
        private static void MoveProp(Rigidbody body, Vector3 position, Quaternion rotation)
        {
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.position = position; body.rotation = rotation; Physics.SyncTransforms();
        }
    }
}
