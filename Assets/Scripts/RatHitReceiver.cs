using UnityEngine;

namespace GymRats
{
    [RequireComponent(typeof(RatMotor))]
    public sealed class RatHitReceiver : MonoBehaviour
    {
        [SerializeField] private Color flashColor = new Color(1f, 0.88f, 0.35f);
        [SerializeField, Min(0.01f)] private float flashDuration = 0.14f;
        private RatMotor motor;
        private Animator animator;
        private Renderer[] renderers;
        private MaterialPropertyBlock[] originalBlocks;
        private Color[] originalColors;
        private MaterialPropertyBlock flashBlock;
        private float flashUntil;
        private bool flashing;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Hit = Animator.StringToHash("Hit");

        public int ReceivedHitCount { get; private set; }
        public bool IsRecovering => motor != null && motor.IsRecovering;

        private void Awake()
        {
            flashBlock = new MaterialPropertyBlock();
            motor = GetComponent<RatMotor>();
            animator = GetComponentInChildren<Animator>();
            renderers = GetComponentsInChildren<Renderer>();
            originalBlocks = new MaterialPropertyBlock[renderers.Length];
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalBlocks[i] = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(originalBlocks[i]);
                originalColors[i] = renderers[i].sharedMaterial.GetColor(BaseColor);
            }
        }

        public bool ReceiveHit(Vector3 velocity, float recoveryDuration)
        {
            if (!isActiveAndEnabled || motor.IsRecovering) return false;
            ReceivedHitCount++;
            motor.ApplyKnockback(velocity, recoveryDuration);
            if (animator != null)
            {
                animator.ResetTrigger("Punch");
                animator.SetFloat("HitRate", 0.4f / Mathf.Max(0.05f, recoveryDuration));
                animator.SetTrigger(Hit);
            }
            flashUntil = Time.time + flashDuration;
            flashing = true;
            return true;
        }

        private void LateUpdate()
        {
            if (!flashing) return;
            float strength = Mathf.Clamp01((flashUntil - Time.time) / flashDuration);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (strength <= 0f) renderers[i].SetPropertyBlock(originalBlocks[i]);
                else
                {
                    renderers[i].GetPropertyBlock(flashBlock);
                    flashBlock.SetColor(BaseColor, Color.Lerp(originalColors[i], flashColor, strength));
                    renderers[i].SetPropertyBlock(flashBlock);
                }
            }
            if (strength <= 0f) flashing = false;
        }

        private void OnDisable()
        {
            if (renderers == null || originalBlocks == null) return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].SetPropertyBlock(originalBlocks[i]);
            flashing = false;
        }
    }
}
