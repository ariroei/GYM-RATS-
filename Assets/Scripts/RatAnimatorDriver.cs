using UnityEngine;

namespace GymRats
{
    /// <summary>Feeds the visual rig without changing movement or applying root motion.</summary>
    [DefaultExecutionOrder(50)]
    public sealed class RatAnimatorDriver : MonoBehaviour
    {
        [SerializeField] private RatMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0.01f)] private float speedDamping = 0.08f;
        [SerializeField, Min(0f)] private float minimumLandingAirTime = 0.06f;

        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
        private static readonly int Land = Animator.StringToHash("Land");
        private static readonly int StrideRate = Animator.StringToHash("StrideRate");
        private float airborneTime;
        private bool wasGrounded;
        private RatCombat combat;
        private RatGrabber grabber;
        private int carryLayer;
        private int combatLayer = -1;

        private void Awake()
        {
            combat = GetComponent<RatCombat>();
            grabber = GetComponent<RatGrabber>();
            carryLayer = animator != null ? animator.GetLayerIndex("Carry") : -1;
            if (animator != null) combatLayer = animator.GetLayerIndex("Combat");
        }

        private void OnEnable()
        {
            airborneTime = 0f;
            wasGrounded = false;
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.ResetTrigger(Land);
            }
        }

        private void Update()
        {
            if (motor == null || animator == null || !animator.isActiveAndEnabled)
                return;

            if (carryLayer >= 0)
            {
                animator.SetLayerWeight(carryLayer, Mathf.MoveTowards(animator.GetLayerWeight(carryLayer),
                    grabber != null && grabber.IsAnimating && !motor.IsRecovering ? 1f : 0f, Time.deltaTime / 0.06f));
                animator.SetBool("Holding", grabber != null && grabber.IsHolding);
            }
            float speed = Mathf.Clamp01(motor.PlanarVelocity.magnitude / motor.MoveSpeed);
            animator.SetFloat(Speed, speed, speedDamping, Time.deltaTime);
            animator.SetFloat(StrideRate, Mathf.Lerp(0.6f, 1.25f, speed));
            animator.SetBool(Grounded, motor.IsGrounded);
            animator.SetFloat(VerticalSpeed, motor.VerticalSpeed);
            if (combatLayer >= 0)
            {
                float desiredWeight = motor.IsRecovering || (combat != null && combat.IsPunching) ? 1f : 0f;
                animator.SetLayerWeight(combatLayer, Mathf.MoveTowards(animator.GetLayerWeight(combatLayer),
                    desiredWeight, Time.deltaTime / 0.06f));
            }

            if (!motor.IsGrounded)
            {
                airborneTime += Time.deltaTime;
                animator.ResetTrigger(Land);
            }
            else
            {
                if (!wasGrounded && airborneTime >= minimumLandingAirTime)
                    animator.SetTrigger(Land);
                airborneTime = 0f;
            }
            wasGrounded = motor.IsGrounded;
        }
    }
}
