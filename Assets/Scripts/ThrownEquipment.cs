using System.Collections.Generic;
using UnityEngine;

namespace GymRats
{
    [RequireComponent(typeof(Rigidbody), typeof(Grabbable))]
    public sealed class ThrownEquipment : MonoBehaviour
    {
        [Header("Throw response")]
        [SerializeField, Min(0.1f)] private float launchMultiplier = 1f;
        [Tooltip("Reduce the upward arc so equipment thrown from chest height strikes a nearby rat.")]
        [SerializeField, Range(0f, 1f)] private float upwardLaunchMultiplier = 0.4f;
        [Header("Rat impact")]
        [SerializeField, Min(0.1f)] private float minimumHitSpeed = 2f;
        [SerializeField, Min(0f)] private float knockbackPerSpeed = 0.6f;
        [SerializeField, Min(0f)] private float maximumKnockback = 8f;
        [SerializeField, Min(0f)] private float upwardKnockback = 2f;
        [SerializeField, Min(0.05f)] private float recovery = 0.4f;
        [SerializeField, Min(0.1f)] private float activeSeconds = 3f;
        private Rigidbody body;
        private Grabbable grabbable;
        private RatGrabber thrower;
        private float armedUntil;
        private Vector3 incomingVelocity;
        private readonly HashSet<RatHitReceiver> struck = new HashSet<RatHitReceiver>();
        public int SuccessfulHits { get; private set; }
        public bool IsArmed => Time.time < armedUntil && !grabbable.IsHeld;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            grabbable = GetComponent<Grabbable>();
        }

        public Vector3 PrepareRelease(RatGrabber source, Vector3 velocity, bool thrown)
        {
            Disarm();
            if (!thrown) return velocity;
            thrower = source;
            armedUntil = Time.time + activeSeconds;
            incomingVelocity = velocity * launchMultiplier;
            incomingVelocity.y *= upwardLaunchMultiplier;
            return incomingVelocity;
        }

        public void Disarm()
        {
            armedUntil = 0f;
            thrower = null;
            struck.Clear();
        }

        private void FixedUpdate()
        {
            if (!IsArmed || body.isKinematic) return;
            incomingVelocity = body.linearVelocity;
            float speed = incomingVelocity.magnitude;
            if (speed < minimumHitSpeed) return;
            // Sweep only the next physics step. The nearest solid collider blocks targets behind it.
            // This also covers CharacterController contacts that do not send Rigidbody callbacks.
            var hits = body.SweepTestAll(incomingVelocity.normalized, speed * Time.fixedDeltaTime,
                QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            Collider contact = null;
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
                nearest = hit.distance;
                contact = hit.collider;
            }
            if (contact != null) TryHit(contact, incomingVelocity);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsArmed) TryHit(collision.collider, incomingVelocity);
        }

        private void TryHit(Collider collider, Vector3 velocity)
        {
            var receiver = collider.GetComponentInParent<RatHitReceiver>();
            float speed = velocity.magnitude;
            if (receiver == null || speed < minimumHitSpeed || (thrower != null && receiver.gameObject == thrower.gameObject)
                || struck.Contains(receiver)) return;
            Vector3 direction = Vector3.ProjectOnPlane(velocity, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector3.ProjectOnPlane(receiver.transform.position - transform.position, Vector3.up).normalized;
            if (receiver.ReceiveHit(direction * Mathf.Min(maximumKnockback, speed * knockbackPerSpeed)
                + Vector3.up * upwardKnockback, recovery))
            {
                struck.Add(receiver);
                SuccessfulHits++;
            }
        }

        private void OnDisable() => Disarm();
    }
}
