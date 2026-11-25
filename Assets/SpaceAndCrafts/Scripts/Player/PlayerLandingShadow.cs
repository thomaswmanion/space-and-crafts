using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;

namespace SpaceAndCrafts.Player
{
    /// <summary>Projects a soft ground marker below Splat so jump landings stay readable.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerLandingShadow : MonoBehaviour
    {
        private static readonly ProfilerMarker LateUpdateMarker = new ProfilerMarker("PlayerLandingShadow.LateUpdate");
        private const int Segments = 32;
        private const float MaximumProjectionDistance = 60f;
        private static readonly Color ShadowTint = new(0.035f, 0.055f, 0.09f, 1f);

        [SerializeField, Min(0.05f)] private float groundRadius = 0.42f;
        [SerializeField, Min(0.1f)] private float maximumVisibleHeight = 18f;
        [SerializeField, Range(0f, 1f)] private float maximumOpacity = 0.42f;

        private SplatMotor _motor;
        private Transform _shadowTransform;
        private MeshRenderer _renderer;
        private Material _material;
        private Mesh _mesh;
        private Collider _groundSurface;
        private readonly RaycastHit[] _groundHits = new RaycastHit[16];

        public bool IsVisibleForDiagnostics => _renderer != null && _renderer.enabled;
        public Vector3 GroundPointForDiagnostics => _shadowTransform != null
            ? _shadowTransform.position
            : Vector3.zero;
        public string GroundSurfaceNameForDiagnostics => _groundSurface != null
            ? _groundSurface.name
            : "none";
        public float VerticalGapForDiagnostics => _shadowTransform != null
            ? transform.position.y - _shadowTransform.position.y
            : 0f;

        private void Awake()
        {
            _motor = GetComponent<SplatMotor>();
            CreateShadow();
        }

        private void LateUpdate()
        {
            using var markerScope = LateUpdateMarker.Auto();
            if (_renderer == null || _motor == null)
            {
                return;
            }

            var origin = transform.position + Vector3.up * 0.2f;
            var hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, _groundHits,
                MaximumProjectionDistance, _motor.GroundSupportMask, QueryTriggerInteraction.Ignore);
            var nearestDistance = float.PositiveInfinity;
            var nearestHit = default(RaycastHit);
            for (var i = 0; i < hitCount; i++)
            {
                var hit = _groundHits[i];
                if (hit.collider == null || hit.collider.GetComponentInParent<SplatMotor>() != null
                    || hit.normal.y < 0.45f || hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                nearestHit = hit;
            }

            if (float.IsPositiveInfinity(nearestDistance))
            {
                _groundSurface = null;
                _renderer.enabled = false;
                return;
            }

            var height = Mathf.Max(0f, nearestHit.distance - 0.2f);
            if (height > maximumVisibleHeight)
            {
                _renderer.enabled = false;
                return;
            }

            _renderer.enabled = true;
            _groundSurface = nearestHit.collider;
            _shadowTransform.position = nearestHit.point + nearestHit.normal * 0.025f;
            _shadowTransform.rotation = Quaternion.FromToRotation(Vector3.up, nearestHit.normal);
            var height01 = Mathf.Clamp01(height / maximumVisibleHeight);
            var size = groundRadius * Mathf.Lerp(1f, 1.7f, height01);
            _shadowTransform.localScale = new Vector3(size * 1.25f, 1f, size);
            var opacity = maximumOpacity * (1f - height01 * 0.72f);
            if (_motor.IsGrounded)
            {
                opacity *= 0.8f;
            }

            _material.color = new Color(ShadowTint.r, ShadowTint.g, ShadowTint.b, opacity);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }

            if (_material != null)
            {
                Destroy(_material);
            }

            if (_shadowTransform != null)
            {
                Destroy(_shadowTransform.gameObject);
            }
        }

        private void CreateShadow()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return;
            }

            var shadow = new GameObject("Splat Landing Shadow");
            _shadowTransform = shadow.transform;
            _shadowTransform.SetParent(null, true);
            _mesh = BuildSoftDisc();
            var filter = shadow.AddComponent<MeshFilter>();
            filter.sharedMesh = _mesh;
            _renderer = shadow.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _material = new Material(shader) { name = "Splat Landing Shadow (Runtime)" };
            _material.color = new Color(ShadowTint.r, ShadowTint.g, ShadowTint.b, 0f);
            _renderer.sharedMaterial = _material;
            _renderer.enabled = false;
        }

        private static Mesh BuildSoftDisc()
        {
            var mesh = new Mesh { name = "Splat Soft Landing Shadow" };
            var vertices = new Vector3[Segments + 2];
            var colors = new Color[vertices.Length];
            var triangles = new int[Segments * 3];
            vertices[0] = Vector3.zero;
            colors[0] = Color.white;
            for (var i = 0; i <= Segments; i++)
            {
                var angle = i * Mathf.PI * 2f / Segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f,
                    Mathf.Sin(angle) * 0.5f);
                colors[i + 1] = new Color(1f, 1f, 1f, 0f);
                if (i < Segments)
                {
                    var triangle = i * 3;
                    triangles[triangle] = 0;
                    triangles[triangle + 1] = i + 2;
                    triangles[triangle + 2] = i + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
