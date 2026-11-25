using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Soft horizon ribbon motion for background aesthetic presence.
    /// </summary>
    public sealed class AmbientHorizonBob : MonoBehaviour
    {
        [SerializeField] private float bobAmp = 0.35f;
        [SerializeField] private float bobSpeed = 0.35f;
        [SerializeField] private float yawSpeed = 4f;

        private Vector3 _rest;

        private void Awake()
        {
            _rest = transform.localPosition;
        }

        private void Update()
        {
            var y = Mathf.Sin(Time.time * bobSpeed) * bobAmp;
            transform.localPosition = _rest + Vector3.up * y;
            transform.Rotate(0f, yawSpeed * Time.deltaTime, 0f, Space.World);
        }
    }
}
