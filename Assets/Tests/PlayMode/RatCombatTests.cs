using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GymRats.Tests
{
    public class RatCombatTests
    {
        private InputTestFixture input;
        private Keyboard keyboard;
        private Gamepad gamepad;
        private RatMotor player;
        private RatMotor target;
        private RatCombat combat;
        private RatHitReceiver receiver;
        private GameObject wall;

        [UnitySetUp]
        public IEnumerator LoadCombatScene()
        {
            input = new InputTestFixture();
            input.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("GymPrototype");
            SoloGameplayFixture.Configure();
            var rats = Object.FindObjectsByType<RatMotor>(FindObjectsSortMode.None);
            player = rats.Single(rat => rat.GetComponent<RatInputOwner>().PlayerNumber == 1);
            target = rats.Single(rat => rat.GetComponent<RatInputOwner>().PlayerNumber == 2);
            combat = player.GetComponent<RatCombat>();
            receiver = target.GetComponent<RatHitReceiver>();
            PositionTarget(1.6f);
            yield return new WaitForSeconds(0.2f);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (player != null) Object.Destroy(player.gameObject);
            if (target != null) Object.Destroy(target.gameObject);
            if (wall != null) Object.Destroy(wall);
            yield return null;
            input?.TearDown();
            input = null;
        }

        [UnityTest]
        public IEnumerator KeyboardPunchAnimatesHitsOnceFlashesAndKnocksBack()
        {
            var animator = player.GetComponentInChildren<Animator>();
            var arm = animator.transform.Find("Rig/Hips/Spine/Chest/UpperArmR");
            Quaternion rest = arm.localRotation;
            Vector3 before = target.transform.position;
            input.Press(keyboard.enterKey);
            yield return new WaitForSeconds(0.09f);
            Assert.That(combat.IsPunching, Is.True, "Punch is active during windup.");
            Assert.That(receiver.ReceivedHitCount, Is.Zero, "Contact must wait for the extending fist.");
            Assert.That(Quaternion.Angle(rest, arm.localRotation), Is.GreaterThan(12f), "The upper-body mask must actually animate the arm.");
            yield return new WaitForSeconds(0.11f);
            Assert.That(receiver.ReceivedHitCount, Is.EqualTo(1));
            Assert.That(combat.SuccessfulHits, Is.EqualTo(1));
            Assert.That(target.IsRecovering, Is.True, "Target recovers after impact.");
            Assert.That(target.transform.position.z, Is.GreaterThan(before.z + 0.1f));
            var block = new MaterialPropertyBlock();
            target.GetComponentInChildren<Renderer>().GetPropertyBlock(block);
            Assert.That(block.isEmpty, Is.False, "A hit should flash the target's materials.");
            var targetAnimator = target.GetComponentInChildren<Animator>();
            // Coroutine assertions can run before Animator evaluation; allow the authored 35 ms blend.
            float hitDeadline = Time.time + 0.1f;
            while (!targetAnimator.GetCurrentAnimatorStateInfo(targetAnimator.GetLayerIndex("Combat")).IsName("Hit") && Time.time < hitDeadline)
                yield return null;
            Assert.That(targetAnimator.GetCurrentAnimatorStateInfo(targetAnimator.GetLayerIndex("Combat")).IsName("Hit"), Is.True, "Target Animator enters Hit.");
            Assert.That(combat.TryPunch(), Is.False, "Cooldown must reject another punch.");
            yield return new WaitForSeconds(0.8f);
            Assert.That(receiver.ReceivedHitCount, Is.EqualTo(1));
            Assert.That(target.IsRecovering, Is.False);
            Assert.That(target.IsGrounded, Is.True, "Target lands after knockback.");
            Vector3 stopped = target.transform.position;
            input.Press(keyboard.dKey);
            yield return new WaitForSeconds(0.3f);
            Assert.That(Vector3.Distance(stopped, target.transform.position), Is.LessThan(0.03f), "The practice target must not follow player input.");
        }

        [UnityTest]
        public IEnumerator GamepadWestButtonPunches()
        {
            input.Press(gamepad.buttonWest);
            yield return new WaitForSeconds(0.22f);
            Assert.That(combat.PunchesStarted, Is.EqualTo(1));
            Assert.That(receiver.ReceivedHitCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator OutsideRangeDoesNotConnect()
        {
            PositionTarget(combat.Range + target.GetComponent<CharacterController>().radius + 0.2f);
            Vector3 before = target.transform.position;
            input.Press(keyboard.enterKey);
            yield return new WaitForSeconds(0.3f);
            Assert.That(combat.PunchesStarted, Is.EqualTo(1));
            Assert.That(receiver.ReceivedHitCount, Is.Zero);
            Assert.That(Mathf.Abs(target.transform.position.z - before.z), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator TargetsBehindThePunchAreNotHit()
        {
            PositionTarget(-1.6f);
            input.Press(keyboard.enterKey);
            yield return new WaitForSeconds(0.25f);
            Assert.That(receiver.ReceivedHitCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator SolidWallBlocksAnOtherwiseInRangePunch()
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 1.5f, 0.8f);
            wall.transform.localScale = new Vector3(2f, 3f, 0.1f);
            Physics.SyncTransforms();
            input.Press(keyboard.enterKey);
            yield return new WaitForSeconds(0.25f);
            Assert.That(receiver.ReceivedHitCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator HoldingAttackDoesNotRepeatAndNewPressWorksAfterCooldown()
        {
            PositionTarget(5f);
            input.Press(keyboard.enterKey);
            yield return new WaitForSeconds(combat.Cooldown + 0.1f);
            Assert.That(combat.PunchesStarted, Is.EqualTo(1));
            input.Release(keyboard.enterKey);
            yield return null;
            input.Press(keyboard.enterKey);
            yield return new WaitForSeconds(0.08f);
            Assert.That(combat.PunchesStarted, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator PunchingKeepsMovementJumpAndCameraResponsive()
        {
            PositionTarget(6f);
            Vector3 start = player.transform.position;
            input.Press(keyboard.wKey);
            input.Press(keyboard.spaceKey);
            input.Press(keyboard.enterKey);
            yield return new WaitForSeconds(0.25f);
            Assert.That(player.transform.position.z, Is.GreaterThan(start.z + 0.6f));
            Assert.That(player.transform.position.y, Is.GreaterThan(start.y + 0.7f));
            Assert.That(combat.IsPunching, Is.True);
            var animator = player.GetComponentInChildren<Animator>();
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"), Is.True);
            Assert.That(animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Combat")).IsName("Punch"), Is.True);
            Vector3 view = Camera.main.WorldToViewportPoint(player.transform.position + Vector3.up * 1.4f);
            Assert.That(view.x, Is.InRange(0.1f, 0.9f));
            Assert.That(view.y, Is.InRange(0.1f, 0.9f));
        }

        [UnityTest]
        public IEnumerator RecoveryRejectsRepeatedHitsAndAllowsAnotherAfterward()
        {
            Assert.That(receiver.ReceiveHit(Vector3.forward * 3f + Vector3.up, 0.3f), Is.True);
            Assert.That(receiver.ReceiveHit(Vector3.forward * 3f, 0.3f), Is.False);
            yield return new WaitForSeconds(0.4f);
            Assert.That(receiver.ReceiveHit(Vector3.forward * 3f + Vector3.up, 0.3f), Is.True);
            Assert.That(receiver.ReceivedHitCount, Is.EqualTo(2));
        }

        private void PositionTarget(float distance)
        {
            var capsule = target.GetComponent<CharacterController>();
            capsule.enabled = false;
            target.transform.position = new Vector3(0f, 0.05f, distance);
            capsule.enabled = true;
            player.Facing.rotation = Quaternion.identity;
            Physics.SyncTransforms();
        }
    }
}
