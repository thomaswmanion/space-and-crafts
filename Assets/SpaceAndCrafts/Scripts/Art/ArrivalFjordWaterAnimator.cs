using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Adds slow wave motion and a subtle albedo drift to Arrival's non-colliding fjord surface.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public sealed class ArrivalFjordWaterAnimator : MonoBehaviour
    {
        private const float UpdateInterval = 0.05f;
        private const float TextureDriftUPerSecond = 0.0015f;
        private const float TextureDriftVPerSecond = -0.0009f;
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        private Mesh _mesh;
        private Vector3[] _restVertices;
        private Vector3[] _waveVertices;
        private Material _waterMaterial;
        private int _albedoPropertyId;
        private Vector2 _initialAlbedoOffset;
        private bool _hasAnimatedAlbedo;
        private float _nextUpdateAt;

        private void Awake()
        {
            var filter = GetComponent<MeshFilter>();
            _mesh = filter != null ? filter.sharedMesh : null;
            if (_mesh == null)
            {
                return;
            }

            _mesh.MarkDynamic();
            _restVertices = _mesh.vertices;
            _waveVertices = new Vector3[_restVertices.Length];
            CacheWaterAlbedo();
            _nextUpdateAt = Time.time;
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
                var longWave = Mathf.Sin(p.x * 0.035f + p.z * 0.018f + time * 0.62f) * 0.025f;
                var crossWave = Mathf.Sin(p.z * 0.029f - p.x * 0.014f - time * 0.41f) * 0.016f;
                p.y += longWave + crossWave;
                _waveVertices[i] = p;
            }

            _mesh.vertices = _waveVertices;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            UpdateAlbedoDrift(time);
        }

        private void CacheWaterAlbedo()
        {
            var renderer = GetComponent<MeshRenderer>();
            _waterMaterial = renderer != null ? renderer.sharedMaterial : null;
            if (_waterMaterial == null)
            {
                return;
            }

            if (_waterMaterial.HasProperty(BaseMapId) && _waterMaterial.GetTexture(BaseMapId) != null)
            {
                _albedoPropertyId = BaseMapId;
                _hasAnimatedAlbedo = true;
            }
            else if (_waterMaterial.HasProperty(MainTexId) && _waterMaterial.GetTexture(MainTexId) != null)
            {
                _albedoPropertyId = MainTexId;
                _hasAnimatedAlbedo = true;
            }

            if (_hasAnimatedAlbedo)
            {
                _initialAlbedoOffset = _waterMaterial.GetTextureOffset(_albedoPropertyId);
            }
        }

        private void UpdateAlbedoDrift(float time)
        {
            if (_waterMaterial == null || !_hasAnimatedAlbedo)
            {
                return;
            }

            var drift = new Vector2(time * TextureDriftUPerSecond, time * TextureDriftVPerSecond);
            _waterMaterial.SetTextureOffset(_albedoPropertyId, _initialAlbedoOffset + drift);
        }

    }
}
