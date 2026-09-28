using System.Linq;
using UnityEngine;

namespace GymRats
{
    /// <summary>Exclusive ownership and collision-safe carrying for upright rats and compound primitive gym props.</summary>
    [DisallowMultipleComponent]
    public sealed class Grabbable : MonoBehaviour
    {
        private CharacterController controller;
        private Rigidbody body;
        private Collider[] shapes;
        private bool[] enabledShapes;
        private ThrownEquipment equipment;
        private RatMotor motor;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private bool wasKinematic;
        private RigidbodyInterpolation previousInterpolation;
        private Vector3 centerOffset;

        public RatGrabber Holder { get; private set; }
        public bool IsHeld => Holder != null;
        public Vector3 Center => transform.TransformPoint(centerOffset);

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            body = GetComponent<Rigidbody>();
            shapes = GetComponentsInChildren<Collider>().Where(c => !c.isTrigger && (c.attachedRigidbody == body)).ToArray();
            enabledShapes = new bool[shapes.Length];
            equipment = GetComponent<ThrownEquipment>();
            motor = GetComponent<RatMotor>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            if (shapes.Length == 0) { enabled = false; return; }
            Bounds bounds = shapes[0].bounds;
            foreach (var shape in shapes) bounds.Encapsulate(shape.bounds);
            centerOffset = transform.InverseTransformPoint(bounds.center);
        }

        public bool CanAcquire(RatGrabber holder)
        {
            return isActiveAndEnabled && !IsHeld && holder != null && !holder.IsHolding && holder.gameObject != gameObject
                && shapes != null && shapes.Any(c => c != null && c.enabled) && (controller != null || (body != null && !body.isKinematic))
                && (motor == null || (!motor.IsRecovering && (GetComponent<RatGrabber>() == null || !GetComponent<RatGrabber>().IsHolding)));
        }

        public bool CanMoveTo(Vector3 destination, RatGrabber holder)
        {
            Vector3 delta = destination - Center;
            for (int i = 0; i < shapes.Length; i++)
            {
                if (shapes[i] == null || (IsHeld ? !enabledShapes[i] : !shapes[i].enabled)) continue;
                if (!CarryCollision.IsPathClear(shapes[i], delta, hit => Blocks(hit, holder))) return false;
            }
            return true;
        }

        private bool Blocks(Collider other, RatGrabber holder) => !other.transform.IsChildOf(transform)
            && !other.transform.IsChildOf(holder.transform);

        public bool Acquire(RatGrabber holder, Vector3 center)
        {
            if (!CanAcquire(holder) || !CanMoveTo(center, holder)) return false;
            Holder = holder;
            if (equipment != null) equipment.Disarm();
            if (body != null)
            {
                wasKinematic = body.isKinematic;
                previousInterpolation = body.interpolation;
                body.interpolation = RigidbodyInterpolation.None;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }
            for (int i = 0; i < shapes.Length; i++)
            {
                enabledShapes[i] = shapes[i].enabled;
                shapes[i].enabled = false;
            }
            if (motor != null) motor.ApplyKnockback(Vector3.zero, 0f);
            MoveHeld(center);
            return true;
        }

        public void MoveHeld(Vector3 center)
        {
            if (!IsHeld) return;
            transform.position += center - Center;
            if (body != null) body.position = transform.position;
        }

        public void Release(Vector3 velocity, bool thrown)
        {
            if (!IsHeld) return;
            var thrower = Holder;
            Holder = null;
            for (int i = 0; i < shapes.Length; i++)
                if (shapes[i] != null) shapes[i].enabled = enabledShapes[i];
            if (body != null)
            {
                body.isKinematic = wasKinematic;
                body.interpolation = previousInterpolation;
                if (!body.isKinematic)
                {
                    body.WakeUp();
                    if (equipment != null) velocity = equipment.PrepareRelease(thrower, velocity, thrown);
                    body.AddForce(velocity, ForceMode.VelocityChange);
                    if (thrown) body.AddTorque(Vector3.right * 3f, ForceMode.VelocityChange);
                }
            }
            if (motor != null) motor.ApplyKnockback(velocity, thrown ? 0.5f : 0.15f);
        }

        private void Update()
        {
            if (body == null || IsHeld || transform.position.y >= -8f) return;
            if (equipment != null) equipment.Disarm();
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
