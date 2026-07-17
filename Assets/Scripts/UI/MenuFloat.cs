using UnityEngine;

namespace SpaceAndCrafts.UI
{
    public sealed class MenuFloat : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 12f, 0f);
        [SerializeField, Min(0f)] private float bobHeight = 0.35f;
        [SerializeField, Min(0f)] private float bobSpeed = 1.2f;

        private Vector3 startPosition;
        private float phase;

        public void Configure(Vector3 rotation, float height, float speed, float offset)
        {
            rotationSpeed = rotation;
            bobHeight = height;
            bobSpeed = speed;
            phase = offset;
        }

        private void Start()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
            transform.position = startPosition + Vector3.up * Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight;
        }
    }
}
