using UnityEngine;

namespace SpaceAndCrafts.Enemies
{
    public sealed class EnemyVisualMotion : MonoBehaviour
    {
        [SerializeField] private CharacterController controller;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform leftMover;
        [SerializeField] private Transform rightMover;
        [SerializeField] private bool flying;
        [SerializeField] private bool scissors;

        private Vector3 basePosition;
        private Quaternion baseRotation;
        private Quaternion leftBaseRotation;
        private Quaternion rightBaseRotation;
        private Vector3 leftBasePosition;
        private Vector3 rightBasePosition;
        private float animationOffset;

        public void Configure(CharacterController character, Transform root, Transform left, Transform right, bool isFlying, bool isScissors)
        {
            controller = character;
            visualRoot = root;
            leftMover = left;
            rightMover = right;
            flying = isFlying;
            scissors = isScissors;
            animationOffset = Mathf.Abs(transform.position.x * 0.37f + transform.position.z * 0.19f);
            CapturePose();
        }

        private void Start() => CapturePose();

        private void CapturePose()
        {
            if (visualRoot != null)
            {
                basePosition = visualRoot.localPosition;
                baseRotation = visualRoot.localRotation;
            }
            if (leftMover != null)
            {
                leftBasePosition = leftMover.localPosition;
                leftBaseRotation = leftMover.localRotation;
            }
            if (rightMover != null)
            {
                rightBasePosition = rightMover.localPosition;
                rightBaseRotation = rightMover.localRotation;
            }
        }

        private void LateUpdate()
        {
            if (controller == null || visualRoot == null)
            {
                return;
            }

            Vector3 velocity = controller.velocity;
            float flatSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            float run = Mathf.Clamp01(flatSpeed / 2f);
            float phase = Time.time * Mathf.Lerp(4f, 10f, run) + animationOffset;
            float stride = Mathf.Sin(phase);

            if (flying)
            {
                visualRoot.localPosition = basePosition + Vector3.up * Mathf.Sin(phase * 0.7f) * 0.12f;
                visualRoot.localRotation = baseRotation * Quaternion.Euler(0f, 0f, -Mathf.Sin(phase) * 9f - velocity.x * 3f);
            }
            else
            {
                visualRoot.localPosition = basePosition + Vector3.up * Mathf.Abs(Mathf.Sin(phase * 2f)) * run * 0.1f;
                visualRoot.localRotation = baseRotation * Quaternion.Euler(stride * run * 4f, 0f, -stride * run * 5f);
            }

            if (scissors)
            {
                float snip = (Mathf.Sin(phase * 1.35f) * 0.5f + 0.5f) * 22f;
                if (leftMover != null) leftMover.localRotation = leftBaseRotation * Quaternion.Euler(0f, 0f, snip);
                if (rightMover != null) rightMover.localRotation = rightBaseRotation * Quaternion.Euler(0f, 0f, -snip);
            }
            else if (!flying)
            {
                if (leftMover != null) leftMover.localPosition = leftBasePosition + Vector3.forward * stride * run * 0.18f;
                if (rightMover != null) rightMover.localPosition = rightBasePosition - Vector3.forward * stride * run * 0.18f;
            }
        }
    }
}
