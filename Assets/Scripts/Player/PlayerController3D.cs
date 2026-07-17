using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceAndCrafts.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController3D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0f)] private float groundAcceleration = 28f;
        [SerializeField, Min(0f)] private float groundDeceleration = 36f;
        [SerializeField, Min(0f)] private float airAcceleration = 10f;
        [SerializeField, Min(0f)] private float rotationSpeed = 14f;

        [Header("Jumping")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.8f;
        [SerializeField, Min(0f)] private float gravity = 28f;
        [SerializeField, Min(0f)] private float fallGravityMultiplier = 1.35f;
        [SerializeField, Range(0.1f, 1f)] private float apexGravityMultiplier = 0.55f;
        [SerializeField, Range(1f, 5f)] private float jumpReleaseGravityMultiplier = 2.2f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 24f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float lastGroundedTime = float.NegativeInfinity;
        private float lastJumpPressedTime = float.NegativeInfinity;

        public bool IsGrounded => controller != null && controller.isGrounded;
        public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            bool isGrounded = controller.isGrounded;
            if (isGrounded)
            {
                lastGroundedTime = Time.time;
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2f;
                }
            }

            if (WasJumpPressed())
            {
                lastJumpPressedTime = Time.time;
            }

            Vector2 input = ReadMoveInput();

            Vector3 moveDirection = GetCameraRelativeDirection(input);
            Vector3 targetVelocity = moveDirection * moveSpeed;
            float acceleration = isGrounded
                ? (input.sqrMagnitude > 0f ? groundAcceleration : groundDeceleration)
                : airAcceleration;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                acceleration * Time.deltaTime);

            bool canUseCoyoteTime = Time.time - lastGroundedTime <= coyoteTime;
            bool hasBufferedJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
            if (canUseCoyoteTime && hasBufferedJump)
            {
                verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
                lastGroundedTime = float.NegativeInfinity;
                lastJumpPressedTime = float.NegativeInfinity;
            }

            bool jumpHeld = IsJumpHeld();
            float activeGravity = gravity;
            if (verticalVelocity < 0f)
            {
                activeGravity *= fallGravityMultiplier;
            }
            else if (!jumpHeld)
            {
                activeGravity *= jumpReleaseGravityMultiplier;
            }
            else if (Mathf.Abs(verticalVelocity) < 2.2f)
            {
                activeGravity *= apexGravityMultiplier;
            }
            verticalVelocity = Mathf.Max(verticalVelocity - activeGravity * Time.deltaTime, -maxFallSpeed);

            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime);
            }
        }

        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            if (cameraTransform == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            return (right * input.x + forward * input.y).normalized;
        }

        private static Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                input.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                input.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            }

            if (Gamepad.current != null && Gamepad.current.leftStick.ReadValue().sqrMagnitude > input.sqrMagnitude)
            {
                input = Gamepad.current.leftStick.ReadValue();
            }

            return Vector2.ClampMagnitude(input, 1f);
        }

        private static bool WasJumpPressed()
        {
            bool keyboardJump = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool gamepadJump = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            return keyboardJump || gamepadJump;
        }

        private static bool IsJumpHeld()
        {
            bool keyboardJump = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            bool gamepadJump = Gamepad.current != null && Gamepad.current.buttonSouth.isPressed;
            return keyboardJump || gamepadJump;
        }

        public void LaunchUpward(float speed)
        {
            verticalVelocity = Mathf.Max(verticalVelocity, speed);
            lastGroundedTime = float.NegativeInfinity;
        }

        public void KnockBack(Vector3 direction, float horizontalSpeed, float upwardSpeed)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = -transform.forward;
            }

            horizontalVelocity = direction.normalized * horizontalSpeed;
            verticalVelocity = Mathf.Max(verticalVelocity, upwardSpeed);
            lastGroundedTime = float.NegativeInfinity;
        }
    }
}
