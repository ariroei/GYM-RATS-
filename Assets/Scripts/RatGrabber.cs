using UnityEngine;
using UnityEngine.InputSystem;

namespace GymRats
{
    [RequireComponent(typeof(RatMotor))]
    [DefaultExecutionOrder(30)]
    public sealed class RatGrabber : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float grabDistance = 2f;
        [SerializeField, Range(5f, 85f)] private float grabHalfAngle = 65f;
        [Tooltip("Throw velocity in meters per second; Rigidbody props use VelocityChange.")]
        [SerializeField, Min(0f)] private float throwForce = 10f;
        [SerializeField, Min(0f)] private float upwardForce = 5f;
        [SerializeField, Min(0.05f)] private float cooldown = 0.45f;
        [SerializeField, Min(1.1f)] private float holdDistance = 1.4f;
        [SerializeField, Min(1.6f)] private float holdHeight = 2.5f;
        private RatMotor motor;
        private RatCombat combat;
        private Animator animator;
        private InputActionAsset actions;
        private InputAction grabAction;
        private InputAction throwAction;
        private float nextAction;
        private float animationUntil;
        private Vector3 previousPosition;

        public Grabbable HeldTarget { get; private set; }
        public bool IsHolding => HeldTarget != null;
        public bool IsAnimating => IsHolding || Time.time < animationUntil;
        public float GrabDistance => grabDistance;
        public Vector3 HoldCenter => transform.position + Vector3.up * holdHeight + motor.Facing.forward * holdDistance;

        private void Awake()
        {
            motor = GetComponent<RatMotor>();
            combat = GetComponent<RatCombat>();
            animator = GetComponentInChildren<Animator>();
            previousPosition = transform.position;
        }

        private void OnEnable()
        {
            if (!motor.AcceptsPlayerInput || motor.InputActions == null) return;
            var owner = GetComponent<RatInputOwner>();
            actions = owner != null ? owner.CreateActions(motor.InputActions) : Instantiate(motor.InputActions);
            grabAction = actions.FindAction("Player/Grab", true);
            throwAction = actions.FindAction("Player/Throw", true);
            grabAction.performed += OnGrab;
            throwAction.performed += OnThrow;
            grabAction.Enable();
            throwAction.Enable();
        }

        private void OnGrab(InputAction.CallbackContext context)
        {
            if (IsHolding) { if (Time.time >= nextAction) ReleaseHeld(false); }
            else TryGrab();
        }
        private void OnThrow(InputAction.CallbackContext context)
        {
            if (Time.time >= nextAction && !motor.IsRecovering && !motor.IsHeld) ReleaseHeld(true);
        }

        public bool TryGrab()
        {
            if (!isActiveAndEnabled || !motor.AcceptsPlayerInput || IsHolding || motor.IsHeld
                || motor.IsRecovering || Time.time < nextAction || (combat != null && combat.IsPunching)) return false;
            Vector3 origin = transform.position + Vector3.up * 1.55f;
            Grabbable nearest = null;
            float best = float.PositiveInfinity;
            foreach (var collider in Physics.OverlapSphere(origin, grabDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                var candidate = collider.GetComponentInParent<Grabbable>();
                if (candidate == null || !candidate.CanAcquire(this)) continue;
                Vector3 delta = candidate.Center - origin;
                Vector3 planar = Vector3.ProjectOnPlane(delta, Vector3.up);
                if (Vector3.Dot(motor.Facing.forward, planar.normalized) < Mathf.Cos(grabHalfAngle * Mathf.Deg2Rad)) continue;
                float distance = (collider.ClosestPoint(origin) - origin).sqrMagnitude;
                if (distance >= best || !ClearLine(origin, candidate.Center, candidate) || !candidate.CanMoveTo(HoldCenter, this)) continue;
                best = distance;
                nearest = candidate;
            }
            if (nearest == null || !nearest.Acquire(this, HoldCenter)) return false;
            HeldTarget = nearest;
            nextAction = Time.time + cooldown;
            Animate("Grab", 0.25f);
            return true;
        }

        private bool ClearLine(Vector3 start, Vector3 end, Grabbable candidate)
        {
            Vector3 delta = end - start;
            foreach (var hit in Physics.RaycastAll(start, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(candidate.transform)) return false;
            return true;
        }

        public bool ReleaseHeld(bool thrown)
        {
            if (!IsHolding) return false;
            var released = HeldTarget;
            HeldTarget = null;
            Vector3 direction = Vector3.ProjectOnPlane(motor.Facing.forward, Vector3.up).normalized;
            released.Release(thrown ? direction * throwForce + Vector3.up * upwardForce : Vector3.zero, thrown);
            nextAction = Time.time + cooldown;
            Animate(thrown ? "Throw" : "Release", 0.4f);
            return true;
        }

        private void Animate(string state, float duration)
        {
            animationUntil = Time.time + duration;
            if (animator != null) animator.CrossFadeInFixedTime(state, 0.05f, animator.GetLayerIndex("Carry"));
        }

        private void LateUpdate()
        {
            if (IsHolding)
            {
                // Drop at the last safe position on obstruction, hit, or teleport. Never pull through walls.
                if (motor.IsRecovering || motor.IsHeld || Vector3.Distance(previousPosition, transform.position) > 3f
                    || !ClearLine(transform.position + Vector3.up * 1.55f, HoldCenter, HeldTarget)
                    || !HeldTarget.CanMoveTo(HoldCenter, this)) ReleaseHeld(false);
                else HeldTarget.MoveHeld(HoldCenter);
            }
            previousPosition = transform.position;
        }

        private void OnDisable()
        {
            ReleaseHeld(false);
            if (grabAction != null) grabAction.performed -= OnGrab;
            if (throwAction != null) throwAction.performed -= OnThrow;
            if (actions != null) { actions.Disable(); Destroy(actions); }
            actions = null;
        }
    }
}
