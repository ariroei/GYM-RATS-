using UnityEngine;

namespace GymRats
{
    public sealed class RatVisualMotion : MonoBehaviour
    {
        [SerializeField] private RatMotor motor;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform tail;
        [SerializeField, Min(0f)] private float strideFrequency = 10f;
        [SerializeField, Range(0f, 45f)] private float strideAngle = 22f;
        private float phase;

        private void LateUpdate()
        {
            if (motor == null)
                return;
            float speed = Mathf.Clamp01(motor.PlanarVelocity.magnitude / motor.MoveSpeed);
            phase += Time.deltaTime * strideFrequency * speed;
            float swing = motor.IsGrounded ? Mathf.Sin(phase) * strideAngle * speed : 0f;
            Pose(leftLeg, swing);
            Pose(rightLeg, -swing);
            Pose(leftArm, motor.IsGrounded ? -swing * 0.7f : -30f);
            Pose(rightArm, motor.IsGrounded ? swing * 0.7f : -30f);
            if (tail != null)
                tail.localRotation = Quaternion.Euler(0f, Mathf.Sin(phase * 0.6f) * 9f * speed, 0f);
        }

        private static void Pose(Transform joint, float angle)
        {
            if (joint != null)
                joint.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
