using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceAndCrafts.CameraSystem
{
    public sealed class PlatformCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(1f)] private float distance = 8.5f;
        [SerializeField, Min(0f)] private float targetHeight = 1.1f;
        [SerializeField] private float yaw = 0f;
        [SerializeField] private float pitch = 22f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.14f;
        [SerializeField, Min(0f)] private float gamepadSensitivity = 115f;
        [SerializeField, Min(0f)] private float smoothTime = 0.08f;
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField, Min(0f)] private float recenterDelay = 1.15f;
        [SerializeField, Min(0f)] private float recenterSpeed = 2.4f;
        [SerializeField, Min(0f)] private float speedFovBoost = 5f;

        private Vector3 velocity;
        private float lastManualOrbitTime;
        private Camera controlledCamera;
        private float baseFieldOfView;
        private float shakeStrength;
        private float shakeUntil;

        public static PlatformCameraFollow Instance { get; private set; }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void Awake()
        {
            Instance = this;
            controlledCamera = GetComponent<Camera>();
            if (controlledCamera != null)
            {
                baseFieldOfView = controlledCamera.fieldOfView;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            ReadOrbitInput();
            AutoRecenter();
            Vector3 focusPoint = target.position + Vector3.up * targetHeight;
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = focusPoint - orbit * Vector3.forward * distance;

            Vector3 castDirection = (desiredPosition - focusPoint).normalized;
            RaycastHit[] hits = Physics.SphereCastAll(focusPoint, 0.25f, castDirection, distance, collisionMask, QueryTriggerInteraction.Ignore);
            float nearestDistance = distance;
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == target || hit.transform.IsChildOf(target))
                {
                    continue;
                }

                nearestDistance = Mathf.Min(nearestDistance, hit.distance);
            }

            if (nearestDistance < distance)
            {
                desiredPosition = focusPoint + castDirection * Mathf.Max(0.5f, nearestDistance - 0.2f);
            }

            if (Time.time < shakeUntil)
            {
                desiredPosition += Random.insideUnitSphere * shakeStrength;
                shakeStrength = Mathf.MoveTowards(shakeStrength, 0f, 5f * Time.deltaTime);
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref velocity,
                smoothTime);

            transform.LookAt(focusPoint);

            SpaceAndCrafts.Player.PlayerController3D controller = target.GetComponent<SpaceAndCrafts.Player.PlayerController3D>();
            if (controlledCamera != null && controller != null)
            {
                float planarSpeed = new Vector2(controller.Velocity.x, controller.Velocity.z).magnitude;
                float targetFov = baseFieldOfView + Mathf.Clamp01(planarSpeed / 6f) * speedFovBoost;
                controlledCamera.fieldOfView = Mathf.Lerp(controlledCamera.fieldOfView, targetFov, 5f * Time.deltaTime);
            }
        }

        private void ReadOrbitInput()
        {
            Vector2 orbitInput = Vector2.zero;
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                orbitInput = Mouse.current.delta.ReadValue() * mouseSensitivity;
            }

            if (Gamepad.current != null)
            {
                orbitInput += Gamepad.current.rightStick.ReadValue() * gamepadSensitivity * Time.deltaTime;
            }

            yaw += orbitInput.x;
            pitch = Mathf.Clamp(pitch - orbitInput.y, 8f, 65f);
            if (orbitInput.sqrMagnitude > 0.001f)
            {
                lastManualOrbitTime = Time.time;
            }
        }

        private void AutoRecenter()
        {
            if (target == null || Time.time - lastManualOrbitTime < recenterDelay)
            {
                return;
            }

            SpaceAndCrafts.Player.PlayerController3D controller = target.GetComponent<SpaceAndCrafts.Player.PlayerController3D>();
            if (controller == null || new Vector2(controller.Velocity.x, controller.Velocity.z).sqrMagnitude < 1f)
            {
                return;
            }

            float desiredYaw = target.eulerAngles.y;
            yaw = Mathf.LerpAngle(yaw, desiredYaw, recenterSpeed * Time.deltaTime);
        }

        public void AddImpulse(float strength, float duration = 0.14f)
        {
            shakeStrength = Mathf.Max(shakeStrength, strength);
            shakeUntil = Mathf.Max(shakeUntil, Time.time + duration);
        }
    }
}
