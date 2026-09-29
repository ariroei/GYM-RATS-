using UnityEngine;
using UnityEngine.InputSystem;

namespace GymRats
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class RatMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform movementCamera;
        [SerializeField] private Transform visualRoot;

        [Header("Movement")]
        [Tooltip("Disable for a stationary practice target; gravity and hit knockback remain active.")]
        [SerializeField] private bool acceptsPlayerInput = true;
        [SerializeField, Min(0.1f)] private float moveSpeed = 6f;
        [SerializeField, Min(0.1f)] private float acceleration = 40f;
        [SerializeField, Min(0.1f)] private float deceleration = 55f;
        [SerializeField, Min(1f)] private float turnSpeed = 720f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.8f;

        [Header("Jump and grounding")]
        [SerializeField, Min(0.1f)] private float jumpHeight = 1.65f;
        [SerializeField, Min(0.1f)] private float gravity = 28f;
        [SerializeField, Min(1f)] private float terminalSpeed = 35f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;
        [SerializeField, Range(0.01f, 0.3f)] private float groundProbeDistance = 0.1f;
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Recovery")]
        [Tooltip("Return to the starting position after falling below this world height.")]
        [SerializeField] private float respawnHeight = -8f;
        [SerializeField, Min(0.1f)] private float knockbackDrag = 16f;

        private Grabbable grabbable;
        private CharacterController controller;
        private InputActionAsset runtimeActions;
        private InputAction moveAction;
        private InputAction jumpAction;
        private readonly RaycastHit[] groundHits = new RaycastHit[16];
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Vector3 planarVelocity;
        private Vector3 knockbackVelocity;
        private float recoveryUntil;
        private float verticalSpeed;
        private float lastGroundedTime = float.NegativeInfinity;
        private float lastJumpTime = float.NegativeInfinity;

        public bool IsHeld => grabbable != null && grabbable.IsHeld;
        public bool IsGrounded { get; private set; }
        public Vector3 PlanarVelocity => planarVelocity;
        public float VerticalSpeed => verticalSpeed;
        public float MoveSpeed => moveSpeed;
        public bool AcceptsPlayerInput => acceptsPlayerInput;
        public bool IsRecovering => Time.time < recoveryUntil;
        public Transform Facing => visualRoot != null ? visualRoot : transform;
        public InputActionAsset InputActions => inputActions;

        private void Awake()
        {
            grabbable = GetComponent<Grabbable>();
            controller = GetComponent<CharacterController>();
            spawnPosition = transform.position;
            spawnRotation = visualRoot != null ? visualRoot.rotation : transform.rotation;
            if (movementCamera == null && Camera.main != null)
                movementCamera = Camera.main.transform;
        }

        private void OnEnable()
        {
            if (!acceptsPlayerInput)
                return;
            if (inputActions == null)
            {
                Debug.LogError("RatMotor requires an Input Action Asset.", this);
                enabled = false;
                return;
            }

            // Own a copy so disabling this character never disables project-wide UI input.
            var owner = GetComponent<RatInputOwner>();
            runtimeActions = owner != null ? owner.CreateActions(inputActions) : Instantiate(inputActions);
            moveAction = runtimeActions.FindAction("Player/Move", true);
            jumpAction = runtimeActions.FindAction("Player/Jump", true);
            jumpAction.performed += QueueJump;
            moveAction.Enable();
            jumpAction.Enable();
        }

        private void OnDisable()
        {
            if (jumpAction != null)
                jumpAction.performed -= QueueJump;
            if (runtimeActions != null)
            {
                runtimeActions.Disable();
                Destroy(runtimeActions);
            }
            runtimeActions = null;
            moveAction = null;
            jumpAction = null;
            planarVelocity = Vector3.zero;
            knockbackVelocity = Vector3.zero;
            recoveryUntil = 0f;
            verticalSpeed = 0f;
            lastGroundedTime = lastJumpTime = float.NegativeInfinity;
            IsGrounded = false;
        }

        private void QueueJump(InputAction.CallbackContext context)
        {
            if (IsRecovering || IsHeld)
                return;
            lastJumpTime = Time.time;
        }

        private void Update()
        {
            if (!controller.enabled)
                return;
            if (transform.position.y < respawnHeight)
            {
                Respawn();
                return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
                return;

            IsGrounded = verticalSpeed <= 0f && ProbeGround();
            if (IsGrounded)
            {
                lastGroundedTime = Time.time;
                verticalSpeed = -2f;
            }

            Vector2 input = !IsRecovering && moveAction != null
                ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f) : Vector2.zero;
            Vector3 forward = movementCamera != null ? movementCamera.forward : Vector3.forward;
            forward = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 desiredVelocity = (forward * input.y + right * input.x) * moveSpeed;
            float rate = input.sqrMagnitude > 0.001f ? acceleration : deceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity,
                rate * (IsGrounded ? 1f : airControl) * deltaTime);

            if (desiredVelocity.sqrMagnitude > 0.01f)
            {
                Transform facing = visualRoot != null ? visualRoot : transform;
                facing.rotation = Quaternion.RotateTowards(facing.rotation,
                    Quaternion.LookRotation(desiredVelocity, Vector3.up), turnSpeed * deltaTime);
            }

            if (!IsRecovering && Time.time - lastJumpTime <= jumpBufferTime && Time.time - lastGroundedTime <= coyoteTime)
            {
                verticalSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
                lastJumpTime = lastGroundedTime = float.NegativeInfinity;
                IsGrounded = false;
            }

            verticalSpeed = Mathf.Max(verticalSpeed - gravity * deltaTime, -terminalSpeed);
            CollisionFlags flags = controller.Move((planarVelocity + knockbackVelocity + Vector3.up * verticalSpeed) * deltaTime);
            knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, knockbackDrag * deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0f)
                verticalSpeed = 0f;
            if ((flags & CollisionFlags.Below) != 0 && verticalSpeed <= 0f)
            {
                IsGrounded = true;
                verticalSpeed = -2f;
                lastGroundedTime = Time.time;
            }
        }

        private bool ProbeGround()
        {
            // Use a slightly inset sphere and reject the player, triggers, and steep walls.
            Vector3 center = transform.TransformPoint(controller.center);
            float radius = controller.radius * 0.9f;
            const float lift = 0.05f;
            Vector3 origin = center - Vector3.up * (controller.height * 0.5f - controller.radius - lift);
            float distance = groundProbeDistance + lift + controller.radius - radius;
            int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundHits,
                distance, groundLayers, QueryTriggerInteraction.Ignore);
            float minimumNormalY = Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad);
            for (int i = 0; i < count; i++)
            {
                if (groundHits[i].collider.transform.IsChildOf(transform))
                    continue;
                if (groundHits[i].normal.y >= minimumNormalY)
                    return true;
            }
            return false;
        }

        public void ApplyKnockback(Vector3 velocity, float recoveryDuration)
        {
            knockbackVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            verticalSpeed = Mathf.Max(verticalSpeed, velocity.y);
            recoveryUntil = Time.time + Mathf.Max(0f, recoveryDuration);
            planarVelocity = Vector3.zero;
            lastJumpTime = lastGroundedTime = float.NegativeInfinity;
            IsGrounded = false;
        }

        public void Respawn()
        {
            controller.enabled = false;
            transform.position = spawnPosition;
            if (visualRoot != null)
                visualRoot.rotation = spawnRotation;
            controller.enabled = true;
            planarVelocity = Vector3.zero;
            knockbackVelocity = Vector3.zero;
            recoveryUntil = 0f;
            verticalSpeed = 0f;
            lastGroundedTime = lastJumpTime = float.NegativeInfinity;
            IsGrounded = false;
        }
    }
}
