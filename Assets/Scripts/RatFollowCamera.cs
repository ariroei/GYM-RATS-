using UnityEngine;

namespace GymRats
{
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(100)]
    public sealed class RatFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("World-space offset keeps the viewing direction stable as the rat turns.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 5.5f, -8f);
        [SerializeField, Min(0f)] private float lookHeight = 1.3f;
        [SerializeField, Min(0.01f)] private float followSmoothTime = 0.12f;
        [SerializeField, Min(1f)] private float teleportDistance = 12f;
        [SerializeField, Min(0.05f)] private float obstructionRadius = 0.25f;
        [SerializeField] private LayerMask obstructionLayers = ~0;

        private Vector3 followVelocity;
        private Vector3 previousTargetPosition;
        private readonly RaycastHit[] obstructionHits = new RaycastHit[16];

        public Transform Target => target;

        private void OnEnable()
        {
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (target == null)
                return;
            followVelocity = Vector3.zero;
            previousTargetPosition = target.position;
            transform.position = ResolveObstruction(target.position + offset);
            AimAtTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
                return;
            if (Vector3.Distance(previousTargetPosition, target.position) > teleportDistance)
            {
                SnapToTarget();
                return;
            }

            Vector3 desired = ResolveObstruction(target.position + offset);
            Vector3 position = Vector3.SmoothDamp(transform.position, desired, ref followVelocity,
                followSmoothTime, Mathf.Infinity, Time.deltaTime);
            // Recheck after damping so smoothing cannot carry the camera through an obstruction.
            transform.position = ResolveObstruction(position);
            previousTargetPosition = target.position;
            AimAtTarget();
        }

        private Vector3 ResolveObstruction(Vector3 desired)
        {
            Vector3 focus = target.position + Vector3.up * lookHeight;
            Vector3 ray = desired - focus;
            float distance = ray.magnitude;
            if (distance < 0.001f)
                return desired;
            Vector3 direction = ray / distance;
            int count = Physics.SphereCastNonAlloc(focus, obstructionRadius, direction,
                obstructionHits, distance, obstructionLayers, QueryTriggerInteraction.Ignore);
            float nearest = distance;
            for (int i = 0; i < count; i++)
            {
                if (obstructionHits[i].collider.transform.IsChildOf(target))
                    continue;
                nearest = Mathf.Min(nearest, Mathf.Max(0.1f, obstructionHits[i].distance - 0.05f));
            }
            return focus + direction * nearest;
        }

        private void AimAtTarget()
        {
            Vector3 direction = target.position + Vector3.up * lookHeight - transform.position;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
