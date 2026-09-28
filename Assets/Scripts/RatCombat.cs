using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GymRats
{
    [RequireComponent(typeof(RatMotor))]
    [DefaultExecutionOrder(20)]
    public sealed class RatCombat : MonoBehaviour
    {
        [Header("Punch")]
        [Tooltip("Maximum distance from the chest to a target's collider surface, in meters.")]
        [SerializeField, Min(0.1f)] private float range = 1.65f;
        [SerializeField, Range(5f, 85f)] private float halfAngle = 60f;
        [SerializeField, Min(0f)] private float hitHeight = 1.55f;
        [Tooltip("Knockback velocity in meters per second, applied through the CharacterController.")]
        [SerializeField, Min(0f)] private float force = 6.5f;
        [SerializeField, Min(0f)] private float upwardForce = 2.4f;
        [SerializeField, Min(0.05f)] private float cooldown = 0.55f;
        [SerializeField, Min(0.15f)] private float punchDuration = 0.42f;
        [SerializeField, Min(0f)] private float hitRecovery = 0.4f;
        [SerializeField] private LayerMask hitLayers = ~0;
        [SerializeField] private LayerMask obstructionLayers = ~0;

        // The authored clip extends the fist at one third of its duration.
        private const float ContactFraction = 1f / 3f;
        private readonly Collider[] overlaps = new Collider[32];
        private readonly RaycastHit[] blockers = new RaycastHit[32];
        private readonly HashSet<RatHitReceiver> struck = new HashSet<RatHitReceiver>();
        private RatMotor motor;
        private Animator animator;
        private InputActionAsset runtimeActions;
        private InputAction attackAction;
        private float punchStarted;
        private float nextPunchTime;
        private bool pendingContact;
        private Vector3 punchDirection;
        private static readonly int Punch = Animator.StringToHash("Punch");
        private static readonly int PunchRate = Animator.StringToHash("PunchRate");

        public bool IsPunching { get; private set; }
        public float Range => range;
        public float Cooldown => Mathf.Max(cooldown, punchDuration);
        public float PunchDuration => punchDuration;
        public int PunchesStarted { get; private set; }
        public int SuccessfulHits { get; private set; }

        private void Awake()
        {
            motor = GetComponent<RatMotor>();
            animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            if (!motor.AcceptsPlayerInput)
                return;
            if (motor.InputActions == null)
            {
                Debug.LogError("RatCombat requires the motor's Input Action Asset.", this);
                enabled = false;
                return;
            }
            runtimeActions = Instantiate(motor.InputActions);
            attackAction = runtimeActions.FindAction("Player/Attack", true);
            attackAction.performed += AttackPerformed;
            attackAction.Enable();
        }

        private void OnDisable()
        {
            if (attackAction != null)
                attackAction.performed -= AttackPerformed;
            if (runtimeActions != null)
            {
                runtimeActions.Disable();
                Destroy(runtimeActions);
            }
            runtimeActions = null;
            attackAction = null;
            IsPunching = pendingContact = false;
            nextPunchTime = 0f;
        }

        private void AttackPerformed(InputAction.CallbackContext context) => TryPunch();

        public bool TryPunch()
        {
            if (!isActiveAndEnabled || !motor.AcceptsPlayerInput || motor.IsRecovering || motor.IsHeld || (GetComponent<RatGrabber>() != null && GetComponent<RatGrabber>().IsAnimating) || IsPunching || Time.time < nextPunchTime)
                return false;
            punchStarted = Time.time;
            nextPunchTime = Time.time + Cooldown;
            punchDirection = Vector3.ProjectOnPlane(motor.Facing.forward, Vector3.up).normalized;
            if (punchDirection.sqrMagnitude < 0.5f) punchDirection = transform.forward;
            IsPunching = pendingContact = true;
            PunchesStarted++;
            if (animator != null)
            {
                animator.SetFloat(PunchRate, 0.42f / punchDuration);
                animator.ResetTrigger(Punch);
                animator.SetTrigger(Punch);
            }
            return true;
        }

        private void Update()
        {
            if (!IsPunching) return;
            if (motor.IsRecovering || motor.IsHeld)
            {
                IsPunching = pendingContact = false;
                return;
            }
            float elapsed = Time.time - punchStarted;
            // Resolve once, even if a slow frame crosses the whole contact window.
            if (pendingContact && elapsed >= punchDuration * ContactFraction)
            {
                pendingContact = false;
                ResolveContact();
            }
            if (elapsed >= punchDuration) IsPunching = false;
        }

        private void ResolveContact()
        {
            Vector3 origin = transform.position + Vector3.up * hitHeight;
            // Use the displayed facing at impact so movement/turning remain responsive.
            punchDirection = Vector3.ProjectOnPlane(motor.Facing.forward, Vector3.up).normalized;
            int count = Physics.OverlapSphereNonAlloc(origin, range, overlaps, hitLayers, QueryTriggerInteraction.Ignore);
            struck.Clear();
            for (int i = 0; i < count; i++)
            {
                var receiver = overlaps[i].GetComponentInParent<RatHitReceiver>();
                if (receiver == null || receiver.gameObject == gameObject || !struck.Add(receiver)) continue;
                Vector3 direction = Vector3.ProjectOnPlane(receiver.transform.position - transform.position, Vector3.up);
                if (direction.sqrMagnitude < 0.001f || Vector3.Dot(punchDirection, direction.normalized) < Mathf.Cos(halfAngle * Mathf.Deg2Rad)) continue;
                Vector3 point = overlaps[i].ClosestPoint(origin);
                if (Vector3.Distance(origin, point) > range || IsBlocked(origin, point, receiver)) continue;
                if (receiver.ReceiveHit(direction.normalized * force + Vector3.up * upwardForce, hitRecovery))
                    SuccessfulHits++;
            }
        }

        private bool IsBlocked(Vector3 origin, Vector3 point, RatHitReceiver receiver)
        {
            Vector3 delta = point - origin;
            if (delta.sqrMagnitude < 0.001f) return false;
            int count = Physics.RaycastNonAlloc(origin, delta.normalized, blockers, delta.magnitude,
                obstructionLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Transform hit = blockers[i].collider.transform;
                if (!hit.IsChildOf(transform) && !hit.IsChildOf(receiver.transform)) return true;
            }
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * hitHeight;
            Transform facing = motor != null ? motor.Facing : transform.Find("Muscular Rat");
            Vector3 forward = facing != null ? facing.forward : transform.forward;
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.8f);
            Gizmos.DrawWireSphere(origin, range);
            Gizmos.DrawRay(origin, Quaternion.AngleAxis(halfAngle, Vector3.up) * forward * range);
            Gizmos.DrawRay(origin, Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward * range);
        }
    }
}
