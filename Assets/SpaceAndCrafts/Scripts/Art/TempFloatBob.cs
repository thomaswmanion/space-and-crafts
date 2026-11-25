using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// TEMP ambient float/spin for scrap props. Replace with authored animation later.
    /// </summary>
    public sealed class TempFloatBob : MonoBehaviour
    {
        [SerializeField] private float bobAmplitude = 0.015f;
        [SerializeField] private float bobSpeed = 0.5f;
        [SerializeField] private float spinDegreesPerSecond = 2.5f;

        private Vector3 _restLocal;
        private float _phase;

        private void Awake()
        {
            _restLocal = transform.localPosition;
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            var y = Mathf.Sin((Time.time * bobSpeed) + _phase) * bobAmplitude;
            transform.localPosition = _restLocal + Vector3.up * y;
            transform.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
        }
    }
}
