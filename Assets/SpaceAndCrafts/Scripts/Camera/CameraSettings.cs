using UnityEngine;

namespace SpaceAndCrafts.Cameras
{
    [CreateAssetMenu(
        fileName = "CameraSettings",
        menuName = "Space & Crafts/Camera/Camera Settings")]
    public sealed class CameraSettings : ScriptableObject
    {
        [Header("Framing")]
        public float Distance = 7f;
        public float Height = 2.2f;
        [Tooltip("Small shoulder offset moves the orbit arm away from centered scenery while the lens stays aimed at Splat.")]
        public float HorizontalOffset = 0f;
        [Tooltip("Vertical look target above the player's root, so the body stays comfortably framed.")]
        public float FocusHeight = 0.78f;
        public float FieldOfView = 55f;
        public float MinPitch = -20f;
        public float MaxPitch = 55f;

        [Header("Feel")]
        public float FollowSmoothTime = 0.1f;
        [Tooltip("Seconds to ease look stick toward the latest input (0 = raw).")]
        public float LookSmoothTime = 0.045f;
        public float OrbitSensitivity = 140f;
        public float PitchSensitivity = 100f;
        [Tooltip("Mouse degrees per pixel; mouse delta bypasses stick shaping and frame-time scaling.")]
        public float MouseLookSensitivity = 0.11f;
        [Tooltip("Multiplier applied when look input comes from a gamepad stick.")]
        public float GamepadLookMultiplier = 1.35f;

        [Header("Auto Framing")]
        [Tooltip("Gently yaw behind Splat while running when the player isn't looking.")]
        public bool AutoYawEnabled = true;
        [Tooltip("Planar move stick magnitude required before auto-yaw engages.")]
        public float AutoYawMoveThreshold = 0.35f;
        [Tooltip("Look stick magnitude that cancels auto-yaw.")]
        public float AutoYawLookCancel = 0.08f;
        [Tooltip("Seconds of sustained move (with look idle) before assist starts.")]
        public float AutoYawDelay = 0.18f;
        [Tooltip("Exponential blend rate toward velocity-facing yaw.")]
        public float AutoYawStrength = 2.4f;

        [Header("Collision")]
        public float CollisionRadius = 0.25f;
        public float CollisionBuffer = 0.15f;
        [Tooltip("Keeps the camera far enough from the focus to avoid clipping into Splat when scenery crowds the orbit arm.")]
        public float MinimumCollisionDistance = 3f;
        public LayerMask CollisionMask = ~0;
    }
}
