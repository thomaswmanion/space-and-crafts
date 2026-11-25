using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Gives the generated fjord aurora curtains a restrained, slow drifting motion.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public sealed class ArrivalFjordAuroraAnimator : MonoBehaviour
    {
        private const float UpdateInterval = 0.08f;
        private const float Tau = Mathf.PI * 2f;

        private Mesh _mesh;
        private Vector3[] _restVertices;
        private Vector3[] _animatedVertices;
        private Vector3 _axis = Vector3.forward;
        private float _phase;
        private int _segments = 1;
        private float _nextUpdateAt;

        private void Awake()
        {
            _mesh = GetComponent<MeshFilter>()?.sharedMesh;
            if (_mesh == null)
            {
                enabled = false;
                return;
            }

            _mesh.MarkDynamic();
            _restVertices = _mesh.vertices;
            _animatedVertices = new Vector3[_restVertices.Length];
            _nextUpdateAt = Time.time;
        }

        public void Configure(Vector3 axis, float phase, int segments)
        {
            if (axis.sqrMagnitude > 0.001f)
            {
                _axis = axis.normalized;
            }

            _phase = phase;
            _segments = Mathf.Max(1, segments);
        }

        private void LateUpdate()
        {
            if (_mesh == null || _restVertices == null || Time.time < _nextUpdateAt)
            {
                return;
            }

            _nextUpdateAt = Time.time + UpdateInterval;
            var time = Time.time;
            for (var i = 0; i < _restVertices.Length; i++)
            {
                var p = _restVertices[i];
                var u = Mathf.Clamp01((i / 2) / (float)_segments);
                // Clamp the endpoint roundoff from sin(pi) before applying a fractional power.
                var envelope = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(Mathf.PI * u)), 0.72f);
                var drift = Mathf.Sin((u * 2.3f + time * 0.07f) * Tau + _phase)
                    + 0.36f * Mathf.Sin((u * 4.7f - time * 0.045f) * Tau - _phase * 0.6f);
                var displacement = drift * envelope;
                p += _axis * (displacement * 0.55f);
                p.y += displacement * 0.65f;
                _animatedVertices[i] = p;
            }

            _mesh.vertices = _animatedVertices;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }
    }
}
