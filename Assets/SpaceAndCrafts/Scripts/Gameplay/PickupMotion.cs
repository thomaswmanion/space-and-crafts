using Unity.Profiling;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// TEMP personality: collectibles bob and spin so they read as interactive.
    /// </summary>
    public sealed class PickupMotion : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new("PickupMotion.Update");
        [SerializeField] private float bobAmplitude = 0.025f;
        [SerializeField] private float bobSpeed = 0.9f;
        [SerializeField] private float spinDegreesPerSecond = 10f;

        private Vector3 _origin;

        private void Awake()
        {
            _origin = transform.localPosition;
        }

        private void Update()
        {
            using (UpdateMarker.Auto()) UpdateCore();
        }

        private void UpdateCore()
        {
            var bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.localPosition = _origin + Vector3.up * bob;
            transform.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.World);
        }
    }
}
