using System;
using UnityEngine;

namespace GymRats
{
    /// <summary>Uses authored primitive shapes even while their colliders are disabled for carrying.</summary>
    public static class CarryCollision
    {
        public static bool IsPathClear(Collider shape, Vector3 delta, Func<Collider, bool> blocks)
        {
            Transform t = shape.transform;
            Vector3 scale = t.lossyScale;
            scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            RaycastHit[] sweep;
            Collider[] destination;
            const float inset = 0.015f;
            if (shape is BoxCollider box)
            {
                Vector3 center = t.TransformPoint(box.center);
                Vector3 half = Vector3.Scale(box.size * 0.5f, scale);
                Vector3 probe = Vector3.Max(Vector3.one * 0.01f, half - Vector3.one * inset);
                sweep = Physics.BoxCastAll(center, probe, delta.normalized, t.rotation, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
                destination = Physics.OverlapBox(center + delta, half, t.rotation, ~0, QueryTriggerInteraction.Ignore);
            }
            else
            {
                Vector3 center;
                Vector3 segment = Vector3.zero;
                float radius;
                if (shape is SphereCollider sphere)
                {
                    center = t.TransformPoint(sphere.center);
                    radius = sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
                }
                else if (shape is CapsuleCollider capsule)
                {
                    center = t.TransformPoint(capsule.center);
                    int axis = capsule.direction;
                    radius = capsule.radius * Mathf.Max(scale[(axis + 1) % 3], scale[(axis + 2) % 3]);
                    Vector3 localAxis = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
                    segment = t.TransformDirection(localAxis) * Mathf.Max(0f, capsule.height * scale[axis] * 0.5f - radius);
                }
                else if (shape is CharacterController controller)
                {
                    center = t.TransformPoint(controller.center);
                    radius = controller.radius * Mathf.Max(scale.x, scale.z);
                    segment = t.up * Mathf.Max(0f, controller.height * scale.y * 0.5f - radius);
                }
                else return false; // Unsupported mesh shapes must be authored with primitive carry colliders.
                sweep = Physics.CapsuleCastAll(center - segment, center + segment, Mathf.Max(0.01f, radius - inset),
                    delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
                destination = Physics.OverlapCapsule(center + delta - segment, center + delta + segment,
                    radius, ~0, QueryTriggerInteraction.Ignore);
            }
            foreach (var hit in sweep)
            {
                if (!blocks(hit.collider)) continue;
                // A just-landed prop can have a tiny solver penetration. Allow lifting out
                // of that contact, but never moving farther into it or ending inside it.
                if (hit.distance <= 0.001f && Physics.ComputePenetration(shape, t.position, t.rotation,
                    hit.collider, hit.collider.transform.position, hit.collider.transform.rotation,
                    out Vector3 separation, out float depth) && depth < 0.05f && Vector3.Dot(delta, separation) > 0f)
                    continue;
                return false;
            }
            foreach (var hit in destination) if (blocks(hit)) return false;
            return true;
        }
    }
}
