using UnityEngine;

namespace SpaceAndCrafts.Player
{
    public sealed class SplatVisualMotion : MonoBehaviour
    {
        [SerializeField] private PlayerController3D controller;
        [SerializeField] private Transform body;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightFoot;
        [SerializeField] private PlayerCombat combat;

        private Vector3 bodyBaseScale;
        private Vector3 bodyBasePosition;
        private bool wasGrounded;
        private float landingPulse;
        private Vector3 leftArmBasePosition;
        private Vector3 rightArmBasePosition;
        private Vector3 leftFootBasePosition;
        private Vector3 rightFootBasePosition;
        private Quaternion leftArmBaseRotation;
        private Quaternion rightArmBaseRotation;

        public void Configure(PlayerController3D playerController, Transform bodyTransform, Transform rootTransform, Transform leftArmTransform, Transform rightArmTransform, Transform leftFootTransform, Transform rightFootTransform, PlayerCombat playerCombat)
        {
            controller = playerController;
            body = bodyTransform;
            visualRoot = rootTransform;
            leftArm = leftArmTransform;
            rightArm = rightArmTransform;
            leftFoot = leftFootTransform;
            rightFoot = rightFootTransform;
            combat = playerCombat;
            bodyBaseScale = body.localScale;
            bodyBasePosition = body.localPosition;
            leftArmBasePosition = leftArm.localPosition;
            rightArmBasePosition = rightArm.localPosition;
            leftFootBasePosition = leftFoot.localPosition;
            rightFootBasePosition = rightFoot.localPosition;
            leftArmBaseRotation = leftArm.localRotation;
            rightArmBaseRotation = rightArm.localRotation;
        }

        private void Start()
        {
            if (body != null)
            {
                bodyBaseScale = body.localScale;
                bodyBasePosition = body.localPosition;
            }
        }

        private void Update()
        {
            if (controller == null || body == null)
            {
                return;
            }

            float speed = new Vector2(controller.Velocity.x, controller.Velocity.z).magnitude;
            float runAmount = Mathf.Clamp01(speed / 4.5f);
            float runPhase = Time.time * Mathf.Lerp(5f, 11f, runAmount);
            float stride = Mathf.Sin(runPhase);
            float bob = controller.IsGrounded ? Mathf.Abs(Mathf.Sin(runPhase * 2f)) * runAmount * 0.075f : 0f;
            float fallStretch = Mathf.Clamp(-controller.Velocity.y * 0.015f, 0f, 0.18f);
            if (controller.IsGrounded && !wasGrounded)
            {
                landingPulse = 1f;
            }
            landingPulse = Mathf.MoveTowards(landingPulse, 0f, 6f * Time.deltaTime);
            float landingSquash = Mathf.Sin(landingPulse * Mathf.PI) * 0.18f;
            body.localScale = new Vector3(
                bodyBaseScale.x * (1f - fallStretch * 0.5f + landingSquash),
                bodyBaseScale.y * (1f + fallStretch + bob - landingSquash),
                bodyBaseScale.z * (1f - fallStretch * 0.5f + landingSquash));
            body.localPosition = bodyBasePosition + Vector3.up * (controller.IsGrounded ? Mathf.Abs(bob) * 0.15f : 0f);

            if (visualRoot != null)
            {
                Vector3 localVelocity = transform.InverseTransformDirection(controller.Velocity);
                Quaternion lean = Quaternion.Euler(
                    Mathf.Clamp(-localVelocity.z * 1.8f, -10f, 10f),
                    0f,
                    Mathf.Clamp(localVelocity.x * 1.8f, -10f, 10f));
                visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, lean, 9f * Time.deltaTime);
            }

            float armSwing = stride * runAmount * 34f;
            if (leftArm != null)
            {
                leftArm.localRotation = leftArmBaseRotation * Quaternion.Euler(armSwing, 0f, 0f);
                leftArm.localPosition = leftArmBasePosition;
            }
            if (rightArm != null)
            {
                bool punching = combat != null && Time.time < combat.PunchFlashUntil;
                rightArm.localRotation = rightArmBaseRotation * Quaternion.Euler(punching ? -75f : -armSwing, 0f, 0f);
                rightArm.localPosition = rightArmBasePosition + (punching ? Vector3.forward * 0.5f : Vector3.zero);
            }

            if (leftFoot != null)
            {
                leftFoot.localPosition = leftFootBasePosition + new Vector3(0f, Mathf.Max(0f, stride) * 0.12f, stride * 0.24f) * runAmount;
                leftFoot.localRotation = Quaternion.Euler(-stride * runAmount * 25f, 0f, 0f);
            }
            if (rightFoot != null)
            {
                rightFoot.localPosition = rightFootBasePosition + new Vector3(0f, Mathf.Max(0f, -stride) * 0.12f, -stride * 0.24f) * runAmount;
                rightFoot.localRotation = Quaternion.Euler(stride * runAmount * 25f, 0f, 0f);
            }
            wasGrounded = controller.IsGrounded;
        }
    }
}
