using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GymRats.Tests
{
    public class LocalMultiplayerTests
    {
        private InputTestFixture input;
        private Keyboard keyboard;
        private Gamepad pad1;
        private Gamepad pad2;
        private LocalMultiplayerSession session;
        private RatMotor p1;
        private RatMotor p2;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            input = new InputTestFixture(); input.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            pad1 = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("GymPrototype");
            session = Object.FindFirstObjectByType<LocalMultiplayerSession>();
            p1 = session.Players[0].GetComponent<RatMotor>(); p2 = session.Players[1].GetComponent<RatMotor>();
            yield return new WaitForSeconds(0.25f);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (session != null) Object.Destroy(session.gameObject);
            if (p1 != null) Object.Destroy(p1.gameObject);
            if (p2 != null) Object.Destroy(p2.gameObject);
            yield return null; input.TearDown();
        }

        [UnityTest]
        public IEnumerator KeyboardAndGamepadHaveIndependentMovementJumpAndPunch()
        {
            Assert.That(session.Players[0].Device, Is.EqualTo(keyboard));
            Assert.That(session.Players[1].Device, Is.EqualTo(pad1));
            Vector3 secondStart = p2.transform.position;
            input.Press(keyboard.wKey);
            yield return new WaitForSeconds(0.3f);
            Assert.That(p1.transform.position.z, Is.GreaterThan(1f));
            Assert.That(Vector3.Distance(secondStart, p2.transform.position), Is.LessThan(0.04f));
            input.Release(keyboard.wKey); yield return new WaitForSeconds(0.2f);
            Vector3 firstStart = p1.transform.position;
            input.Set(pad1.leftStick, Vector2.left); input.Press(pad1.buttonSouth);
            yield return new WaitForSeconds(0.2f);
            Assert.That(p2.transform.position.x, Is.LessThan(secondStart.x - 0.3f));
            Assert.That(p2.transform.position.y, Is.GreaterThan(0.7f));
            Assert.That(Vector3.Distance(firstStart, p1.transform.position), Is.LessThan(0.04f));
            input.Press(keyboard.enterKey); yield return new WaitForSeconds(0.1f);
            Assert.That(p1.GetComponent<RatCombat>().PunchesStarted, Is.EqualTo(1));
            Assert.That(p2.GetComponent<RatCombat>().PunchesStarted, Is.Zero);
            input.Press(pad1.buttonWest); yield return new WaitForSeconds(0.1f);
            Assert.That(p2.GetComponent<RatCombat>().PunchesStarted, Is.EqualTo(1));
            input.Press(keyboard.spaceKey); yield return new WaitForSeconds(0.15f);
            Assert.That(p1.transform.position.y, Is.GreaterThan(0.6f));
        }

        [UnityTest]
        public IEnumerator TwoGamepadsAreExclusiveAndKeyboardIsInactive()
        {
            pad2 = InputSystem.AddDevice<Gamepad>();
            session.SetLayout(LocalMultiplayerSession.ControlLayout.TwoGamepads);
            Assert.That(session.Players[0].Device, Is.EqualTo(pad1));
            Assert.That(session.Players[1].Device, Is.EqualTo(pad2));
            Vector3 first = p1.transform.position, second = p2.transform.position;
            input.Press(keyboard.wKey); input.Press(keyboard.enterKey);
            yield return new WaitForSeconds(0.25f);
            Assert.That(Vector3.Distance(first, p1.transform.position), Is.LessThan(0.04f));
            Assert.That(p1.GetComponent<RatCombat>().PunchesStarted, Is.Zero);
            input.Set(pad1.leftStick, Vector2.left); input.Set(pad2.leftStick, Vector2.right);
            yield return new WaitForSeconds(0.25f);
            Assert.That(p1.transform.position.x, Is.LessThan(first.x - 0.5f));
            Assert.That(p2.transform.position.x, Is.GreaterThan(second.x + 0.5f));
            input.Press(pad1.buttonSouth); yield return new WaitForSeconds(0.1f);
            Assert.That(p1.IsGrounded, Is.False); Assert.That(p2.IsGrounded, Is.True);
            input.Press(pad2.buttonWest); yield return new WaitForSeconds(0.1f);
            Assert.That(p1.GetComponent<RatCombat>().PunchesStarted, Is.Zero);
            Assert.That(p2.GetComponent<RatCombat>().PunchesStarted, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BothPlayersCanPunchGrabReleaseAndThrowEachOther()
        {
            Place(p1, Vector3.zero); Place(p2, Vector3.forward * 1.6f);
            p1.Facing.rotation = Quaternion.identity; p2.Facing.rotation = Quaternion.Euler(0,180,0);
            input.Press(keyboard.enterKey); yield return new WaitForSeconds(0.25f);
            Assert.That(p2.GetComponent<RatHitReceiver>().ReceivedHitCount, Is.EqualTo(1));
            Assert.That(p1.GetComponent<RatHitReceiver>().ReceivedHitCount, Is.Zero);
            yield return new WaitForSeconds(0.8f);
            Place(p1, Vector3.zero); Place(p2, Vector3.forward * 1.6f);
            input.Press(pad1.buttonWest); yield return new WaitForSeconds(0.25f);
            Assert.That(p1.GetComponent<RatHitReceiver>().ReceivedHitCount, Is.EqualTo(1));
            yield return new WaitForSeconds(0.8f);
            Place(p1, Vector3.zero); Place(p2, Vector3.forward * 1.7f);
            yield return new WaitForSeconds(0.1f);
            input.Press(keyboard.eKey); yield return new WaitForSeconds(0.5f);
            Assert.That(p1.GetComponent<RatGrabber>().HeldTarget, Is.EqualTo(p2.GetComponent<Grabbable>()));
            input.Press(pad1.buttonSouth); input.Press(pad1.buttonNorth);
            yield return new WaitForSeconds(0.1f);
            Assert.That(p2.IsHeld, Is.True); Assert.That(p2.GetComponent<RatGrabber>().IsHolding, Is.False);
            input.Press(keyboard.rKey); yield return new WaitForSeconds(0.2f);
            Assert.That(p2.IsHeld, Is.False); Assert.That(p2.transform.position.z, Is.GreaterThan(2.5f));
            yield return new WaitForSeconds(1.2f);
            input.Release(pad1.buttonNorth); yield return null;
            Place(p1, Vector3.zero); Place(p2, Vector3.forward * 1.7f);
            yield return new WaitForSeconds(0.1f);
            input.Press(pad1.buttonNorth); yield return new WaitForSeconds(0.5f);
            Assert.That(p2.GetComponent<RatGrabber>().HeldTarget, Is.EqualTo(p1.GetComponent<Grabbable>()));
            input.Release(pad1.buttonNorth); yield return null; input.Press(pad1.buttonNorth);
            yield return new WaitForSeconds(0.6f);
            Assert.That(p1.IsHeld, Is.False);
            Place(p1, Vector3.zero); Place(p2, Vector3.forward * 1.7f);
            input.Release(pad1.buttonNorth); yield return null; input.Press(pad1.buttonNorth);
            yield return new WaitForSeconds(0.5f);
            input.Press(pad1.rightShoulder); yield return new WaitForSeconds(0.2f);
            Assert.That(p1.IsHeld, Is.False); Assert.That(p1.transform.position.z, Is.LessThan(-0.7f));
        }

        [UnityTest]
        public IEnumerator AutomaticTwoGamepadSetupAndIndependentPropHandling()
        {
            pad2 = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("GymPrototype");
            session = Object.FindFirstObjectByType<LocalMultiplayerSession>();
            p1 = session.Players[0].GetComponent<RatMotor>(); p2 = session.Players[1].GetComponent<RatMotor>();
            Assert.That(session.ActiveLayout, Is.EqualTo(LocalMultiplayerSession.ControlLayout.TwoGamepads));
            Place(p1,new Vector3(-3,0,-2)); Place(p2,new Vector3(3,0,-2));
            p1.Facing.rotation = p2.Facing.rotation = Quaternion.identity;
            var ball = Object.FindObjectsByType<ThrownEquipment>().Single(x=>x.name=="Medicine Ball");
            var roller = Object.FindObjectsByType<ThrownEquipment>().Single(x=>x.name=="Foam Roller");
            var ballBody=ball.GetComponent<Rigidbody>(); var rollerBody=roller.GetComponent<Rigidbody>();
            ballBody.position=new Vector3(-3,0.5f,-0.6f); ballBody.transform.position=ballBody.position;
            rollerBody.position=new Vector3(3,0.4f,-0.6f); rollerBody.transform.position=rollerBody.position;
            Physics.SyncTransforms(); yield return new WaitForSeconds(0.3f);
            input.Press(pad1.buttonNorth); input.Press(pad2.buttonNorth);
            yield return new WaitForSeconds(0.5f);
            Assert.That(p1.GetComponent<RatGrabber>().HeldTarget,Is.EqualTo(ball.GetComponent<Grabbable>()));
            Assert.That(p2.GetComponent<RatGrabber>().HeldTarget,Is.EqualTo(roller.GetComponent<Grabbable>()));
            AssertVisible(p1,Camera.main); AssertVisible(p2,Camera.main);
            input.Press(pad1.rightShoulder); yield return new WaitForSeconds(0.15f);
            Assert.That(p1.GetComponent<RatGrabber>().IsHolding,Is.False);
            Assert.That(p2.GetComponent<RatGrabber>().IsHolding,Is.True);
            Assert.That(ball.IsArmed,Is.True); Assert.That(roller.IsArmed,Is.False);
            input.Release(pad2.buttonNorth); yield return null; input.Press(pad2.buttonNorth);
            yield return new WaitForSeconds(0.15f);
            Assert.That(p2.GetComponent<RatGrabber>().IsHolding,Is.False);
            Assert.That(rollerBody.isKinematic,Is.False);
            Assert.That(roller.IsArmed,Is.False);
        }

        [UnityTest]
        public IEnumerator DisconnectNeverTransfersTheOtherPlayersDevice()
        {
            input.Set(pad1.leftStick, Vector2.left); yield return new WaitForSeconds(0.2f);
            InputSystem.RemoveDevice(pad1); yield return new WaitForSeconds(0.3f);
            Assert.That(session.Players[1].Device, Is.Null);
            Assert.That(session.Players[0].Device, Is.EqualTo(keyboard));
            Vector3 stopped = p2.transform.position;
            input.Press(keyboard.wKey); yield return new WaitForSeconds(0.3f);
            Assert.That(Vector3.Distance(stopped, p2.transform.position), Is.LessThan(0.04f));
            pad2 = InputSystem.AddDevice<Gamepad>(); yield return null;
            Assert.That(session.Players[1].Device, Is.EqualTo(pad2));
            input.Set(pad2.leftStick, Vector2.down); yield return new WaitForSeconds(0.2f);
            Assert.That(p2.transform.position.z, Is.LessThan(stopped.z - 0.3f));
        }

        [UnityTest]
        public IEnumerator FirstGamepadDisconnectDoesNotStealSecondPlayersPad()
        {
            pad2 = InputSystem.AddDevice<Gamepad>();
            session.SetLayout(LocalMultiplayerSession.ControlLayout.TwoGamepads);
            InputSystem.RemoveDevice(pad1); yield return null;
            Assert.That(session.Players[0].Device, Is.Null);
            Assert.That(session.Players[1].Device, Is.EqualTo(pad2));
            Vector3 first = p1.transform.position, second = p2.transform.position;
            input.Set(pad2.leftStick, Vector2.right); yield return new WaitForSeconds(0.25f);
            Assert.That(Vector3.Distance(first, p1.transform.position), Is.LessThan(0.04f));
            Assert.That(p2.transform.position.x, Is.GreaterThan(second.x + 0.5f));
            pad1 = InputSystem.AddDevice<Gamepad>(); yield return null;
            Assert.That(session.Players[0].Device, Is.EqualTo(pad1));
            Assert.That(session.Players[1].Device, Is.EqualTo(pad2));
        }

        [UnityTest]
        public IEnumerator SharedCameraFitsOppositeCornersJumpingAndRegrouping()
        {
            var camera = Camera.main; var shared = camera.GetComponent<RatSharedCamera>();
            float close = shared.Distance;
            foreach (var pair in new[] { new[] {new Vector3(-11,0,-10),new Vector3(11,0,9)},
                new[] {new Vector3(11,0,-10),new Vector3(-11,0,9)}, new[] {new Vector3(-4,0,11),new Vector3(4,0,11)} })
            {
                Place(p1,pair[0]); Place(p2,pair[1]);
                yield return new WaitForEndOfFrame();
                AssertVisible(p1,camera); AssertVisible(p2,camera);
            }
            Place(p1,new Vector3(-11,0,-10)); Place(p2,new Vector3(11,0,9));
            input.Press(keyboard.spaceKey); input.Press(pad1.buttonSouth);
            yield return new WaitForSeconds(0.2f);
            AssertVisible(p1,camera); AssertVisible(p2,camera);
            Assert.That(shared.Distance, Is.GreaterThan(close + 4f));
            Place(p1,Vector3.zero); Place(p2,Vector3.right * 2);
            yield return new WaitForSeconds(1.5f);
            Assert.That(shared.Distance, Is.LessThan(close + 1f));
            AssertVisible(p1,camera); AssertVisible(p2,camera);
        }

        private static void AssertVisible(RatMotor rat, Camera camera)
        {
            foreach (float y in new[] {0.1f,3.5f})
            {
                var point=camera.WorldToViewportPoint(rat.transform.position+Vector3.up*y);
                Assert.That(point.z,Is.GreaterThan(0)); Assert.That(point.x,Is.InRange(0.02f,0.98f)); Assert.That(point.y,Is.InRange(0.02f,0.98f));
            }
        }
        private static void Place(RatMotor motor,Vector3 point)
        {
            motor.Respawn(); var capsule=motor.GetComponent<CharacterController>(); capsule.enabled=false;
            motor.transform.position=point+Vector3.up*0.05f; capsule.enabled=true; Physics.SyncTransforms();
        }
    }
}
