using UnityEngine;

namespace GymRats
{
    /// <summary>Exclusive ownership and collision-safe carrying for upright rats and spherical gym props.</summary>
    [DisallowMultipleComponent]
    public sealed class Grabbable : MonoBehaviour
    {
        private CharacterController controller;
        private Rigidbody body;
        private Collider shape;
        private RatMotor motor;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private bool wasKinematic;
        private RigidbodyInterpolation previousInterpolation;
        private Vector3 centerOffset;
        private float radius;
        private float halfSegment;

        public RatGrabber Holder { get; private set; }
        public bool IsHeld => Holder != null;
        public Vector3 Center => transform.position + centerOffset;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            body = GetComponent<Rigidbody>();
            shape = GetComponent<Collider>();
            motor = GetComponent<RatMotor>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            if (shape == null) { enabled = false; return; }
            // Capture bounds before disabling the collider; disabled collider bounds are empty.
            centerOffset = shape.bounds.center - transform.position;
            radius = controller != null ? controller.radius : shape.bounds.extents.magnitude;
            if (shape is SphereCollider sphere) radius = sphere.radius * Mathf.Max(transform.lossyScale.x,
                Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
            halfSegment = controller != null ? Mathf.Max(0f, controller.height * 0.5f - radius) : 0f;
        }

        public bool CanAcquire(RatGrabber holder)
        {
            return isActiveAndEnabled && !IsHeld && holder != null && !holder.IsHolding && holder.gameObject != gameObject
                && shape != null && shape.enabled && (controller != null || (body != null && !body.isKinematic))
                && (motor == null || (!motor.IsRecovering && (GetComponent<RatGrabber>() == null || !GetComponent<RatGrabber>().IsHolding)));
        }

        public bool CanMoveTo(Vector3 destination, RatGrabber holder)
        {
            Vector3 delta = destination - Center;
            Vector3 segment = Vector3.up * halfSegment;
            // A small inset avoids treating ordinary resting floor contact as an obstruction.
            float probeRadius = Mathf.Max(0.01f, radius - 0.015f);
            foreach (var hit in Physics.CapsuleCastAll(Center - segment, Center + segment, probeRadius,
                delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (Blocks(hit.collider, holder)) return false;
            foreach (var hit in Physics.OverlapCapsule(destination - segment, destination + segment,
                radius, ~0, QueryTriggerInteraction.Ignore))
                if (Blocks(hit, holder)) return false;
            return true;
        }

        private bool Blocks(Collider other, RatGrabber holder) => !other.transform.IsChildOf(transform)
            && !other.transform.IsChildOf(holder.transform);

        public bool Acquire(RatGrabber holder, Vector3 center)
        {
            if (!CanAcquire(holder) || !CanMoveTo(center, holder)) return false;
            Holder = holder;
            if (body != null)
            {
                wasKinematic = body.isKinematic;
                previousInterpolation = body.interpolation;
                body.interpolation = RigidbodyInterpolation.None;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }
            shape.enabled = false;
            if (motor != null) motor.ApplyKnockback(Vector3.zero, 0f);
            MoveHeld(center);
            return true;
        }

        public void MoveHeld(Vector3 center)
        {
            if (!IsHeld) return;
            transform.position = center - centerOffset;
            if (body != null) body.position = transform.position;
        }

        public void Release(Vector3 velocity, bool thrown)
        {
            if (!IsHeld) return;
            Holder = null;
            shape.enabled = true;
            if (body != null)
            {
                body.isKinematic = wasKinematic;
                body.interpolation = previousInterpolation;
                if (!body.isKinematic)
                {
                    body.WakeUp();
                    body.AddForce(velocity, ForceMode.VelocityChange);
                    if (thrown) body.AddTorque(Vector3.right * 3f, ForceMode.VelocityChange);
                }
            }
            if (motor != null) motor.ApplyKnockback(velocity, thrown ? 0.5f : 0.15f);
        }

        private void Update()
        {
            if (body == null || IsHeld || transform.position.y >= -8f) return;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.position = spawnPosition;
            body.rotation = spawnRotation;
        }

        private void OnDisable()
        {
            if (Holder != null) Holder.ReleaseHeld(false);
        }
    }
}
