using System.Collections.Generic;
using UnityEngine;

namespace GymRats
{
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(110)]
    public sealed class RatSharedCamera : MonoBehaviour
    {
        [SerializeField] private Transform[] targets;
        [SerializeField, Range(40f, 75f)] private float pitch = 55f;
        [SerializeField, Min(5f)] private float minimumDistance = 12f;
        [SerializeField, Range(0.5f, 0.95f)] private float viewportUsage = 0.78f;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.2f;
        private readonly List<Vector3> points = new List<Vector3>(32);
        private Camera view;
        private Vector3 focus;
        private Vector3 focusVelocity;
        private float distance;
        private float zoomVelocity;
        public float Distance => distance;

        private void Awake() => view = GetComponent<Camera>();
        private void OnEnable() { distance = minimumDistance; focus = Vector3.up * 1.5f; }
        private void LateUpdate()
        {
            points.Clear();
            foreach (var target in targets)
            {
                if (target == null) continue;
                AddCharacter(target.position);
                var grabber = target.GetComponent<RatGrabber>();
                if (grabber != null && grabber.HeldTarget != null) AddCharacter(grabber.HeldTarget.transform.position);
            }
            if (points.Count == 0) return;
            Bounds bounds = new Bounds(points[0], Vector3.zero);
            foreach (var point in points) bounds.Encapsulate(point);
            focus = Vector3.SmoothDamp(focus, bounds.center, ref focusVelocity, smoothTime);
            Quaternion rotation = Quaternion.Euler(pitch, 0, 0);
            Quaternion inverse = Quaternion.Inverse(rotation);
            float tangent = Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * 0.5f) * viewportUsage;
            float required = minimumDistance;
            foreach (var point in points)
            {
                Vector3 local = inverse * (point - focus);
                required = Mathf.Max(required, Mathf.Abs(local.x) / (tangent * view.aspect) - local.z,
                    Mathf.Abs(local.y) / tangent - local.z);
            }
            // Zoom out immediately to keep both players in frame; ease back in after they regroup.
            if (required > distance) { distance = required; zoomVelocity = 0f; }
            else distance = Mathf.Max(required, Mathf.SmoothDamp(distance, required, ref zoomVelocity, smoothTime));
            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
        }

        private void AddCharacter(Vector3 p)
        {
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    points.Add(p + new Vector3(x * 1.1f, 0, z * 0.8f));
                    points.Add(p + new Vector3(x * 1.1f, 3.8f, z * 0.8f));
                }
        }
    }
}
