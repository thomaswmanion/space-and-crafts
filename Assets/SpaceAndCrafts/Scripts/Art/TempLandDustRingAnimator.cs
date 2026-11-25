using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>Expands and fades the short landing contact ring.</summary>
    public sealed class TempLandDustRingAnimator : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _properties;
        private Vector3 _maximumScale;
        private float _initialScale;
        private float _duration;
        private float _elapsed;
        private Color _color;

        public float DurationForDiagnostics => _duration;
        public float ProgressForDiagnostics => _duration > 0f
            ? Mathf.Clamp01(_elapsed / _duration)
            : 1f;

        public void Initialize(MeshRenderer targetRenderer, Vector3 maximumScale,
            float initialScale, float duration, Color color)
        {
            _renderer = targetRenderer;
            _maximumScale = maximumScale;
            _initialScale = Mathf.Clamp(initialScale, 0.1f, 1f);
            _duration = Mathf.Max(0.05f, duration);
            _color = color;
            _properties ??= new MaterialPropertyBlock();
            ApplyVisual(0f);
        }

        private void Update()
        {
            if (_renderer == null || _duration <= 0f)
            {
                enabled = false;
                return;
            }

            _elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(_elapsed / _duration);
            ApplyVisual(progress);
            if (progress >= 1f)
            {
                _renderer.enabled = false;
                enabled = false;
            }
        }

        private void ApplyVisual(float progress)
        {
            var eased = 1f - Mathf.Pow(1f - progress, 2f);
            var scale = Mathf.Lerp(_initialScale, 1f, eased);
            transform.localScale = _maximumScale * scale;
            var alpha = _color.a * Mathf.Pow(1f - progress, 1.1f);
            var tint = new Color(_color.r, _color.g, _color.b, alpha);
            _renderer.GetPropertyBlock(_properties);
            _properties.SetColor(ColorId, tint);
            _properties.SetColor(BaseColorId, tint);
            _renderer.SetPropertyBlock(_properties);
        }
    }
}
