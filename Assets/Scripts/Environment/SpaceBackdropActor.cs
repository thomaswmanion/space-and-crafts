using UnityEngine;

namespace SpaceAndCrafts.Environment
{
    public sealed class SpaceBackdropActor : MonoBehaviour
    {
        public enum MotionType { ShootingStar, Comet, RobotDrone, Laser }

        [SerializeField] private MotionType motionType;
        [SerializeField] private Vector3 velocity;
        [SerializeField] private Vector3 resetPosition;
        [SerializeField] private float lifetime = 6f;
        [SerializeField] private float phase;
        [SerializeField] private Renderer targetRenderer;

        private Vector3 basePosition;
        private Quaternion baseRotation;
        private Vector3 baseScale;
        private float age;

        public void Configure(MotionType type, Vector3 movement, Vector3 reset, float duration, float animationPhase, Renderer renderer = null)
        {
            motionType = type;
            velocity = movement;
            resetPosition = reset;
            lifetime = Mathf.Max(0.5f, duration);
            phase = animationPhase;
            targetRenderer = renderer;
            basePosition = transform.position;
            baseRotation = transform.rotation;
            baseScale = transform.localScale;
            age = phase % lifetime;
        }

        private void Start()
        {
            basePosition = transform.position;
            baseRotation = transform.rotation;
            baseScale = transform.localScale;
        }

        private void Update()
        {
            age += Time.deltaTime;
            switch (motionType)
            {
                case MotionType.ShootingStar:
                case MotionType.Comet:
                    transform.position += velocity * Time.deltaTime;
                    transform.Rotate(0f, 0f, motionType == MotionType.Comet ? 18f * Time.deltaTime : 0f, Space.Self);
                    if (age >= lifetime)
                    {
                        transform.position = resetPosition;
                        age = 0f;
                    }
                    break;

                case MotionType.RobotDrone:
                    transform.position = basePosition + new Vector3(Mathf.Sin(Time.time * 0.45f + phase) * 5f, Mathf.Sin(Time.time * 1.5f + phase) * 1.2f, 0f);
                    transform.rotation = baseRotation * Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.6f + phase) * 18f, Mathf.Sin(Time.time + phase) * 5f);
                    break;

                case MotionType.Laser:
                    float shot = Mathf.Repeat(Time.time * 0.75f + phase, 1f);
                    bool firing = shot > 0.72f;
                    if (targetRenderer != null) targetRenderer.enabled = firing;
                    float beamLength = firing ? Mathf.Lerp(0.15f, 1f, (shot - 0.72f) / 0.28f) : 0.15f;
                    transform.localScale = new Vector3(baseScale.x, baseScale.y * beamLength, baseScale.z);
                    break;
            }
        }
    }
}
