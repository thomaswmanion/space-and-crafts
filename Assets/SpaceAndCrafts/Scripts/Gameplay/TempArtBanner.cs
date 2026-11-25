using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Visible TEMP ART marker so graybox never reads as finished art.
    /// </summary>
    public sealed class TempArtBanner : MonoBehaviour
    {
        [SerializeField] private string label = "Art Preview";
        [SerializeField] private Color color = new Color(1f, 0.92f, 0.25f, 1f);
        [SerializeField] private float characterSize = 0.12f;

        private TextMesh _text;
        private Transform _camera;

        private void Awake()
        {
            EnsureText();
            if (Application.isPlaying)
            {
                // Temporary authoring labels are useful in Scene view but obscure
                // the player and route when a world is actually played or captured.
                var renderer = GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }

        public void SetLabel(string value)
        {
            label = value;
            EnsureText();
            if (_text != null)
            {
                _text.text = label;
            }
        }

        private void EnsureText()
        {
            if (_text != null)
            {
                return;
            }

            _text = GetComponent<TextMesh>();
            if (_text == null)
            {
                _text = gameObject.AddComponent<TextMesh>();
            }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (font != null)
            {
                _text.font = font;
                var renderer = GetComponent<MeshRenderer>();
                if (renderer != null && font.material != null)
                {
                    renderer.sharedMaterial = font.material;
                }
            }

            _text.text = label;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.characterSize = characterSize;
            _text.fontSize = 48;
            _text.color = color;
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                var main = UnityEngine.Camera.main;
                if (main != null)
                {
                    _camera = main.transform;
                }
            }

            if (_camera == null)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(transform.position - _camera.position, Vector3.up);
        }
    }
}
