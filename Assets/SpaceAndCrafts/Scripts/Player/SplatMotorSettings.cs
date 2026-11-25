using UnityEngine;

namespace SpaceAndCrafts.Player
{
    [CreateAssetMenu(
        fileName = "SplatMotorSettings",
        menuName = "Space & Crafts/Player/Splat Motor Settings")]
    public sealed class SplatMotorSettings : ScriptableObject
    {
        [Header("Move")]
        public float MaxSpeed = 7.5f;
        public float Acceleration = 55f;
        public float Deceleration = 85f;
        public float AirControlPercent = 0.75f;
        public float TurnSpeedDegrees = 900f;

        [Header("Jump")]
        public float JumpHeight = 2.4f;
        public float Gravity = -45f;
        public float JumpCutGravityMultiplier = 3f;
        public float CoyoteTime = 0.12f;
        public float JumpBufferTime = 0.14f;
        [Range(0f, 1f)] public float GroundPoundPlanarCarry = 0.82f;
        [Range(0f, 1f)] public float GroundPoundLandingCarry = 0.65f;

        [Header("Ground")]
        public float GroundedStickForce = -6f;
        public LayerMask GroundMask = 1 << 0;
    }
}
