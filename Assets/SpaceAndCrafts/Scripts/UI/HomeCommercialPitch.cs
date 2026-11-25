using UnityEngine;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Home-menu commercial pitch beat — tagline + soft parallax so the title reads as a $50 product, not a prototype.
    /// </summary>
    public sealed class HomeCommercialPitch : MonoBehaviour
    {
        [SerializeField] private string tagline = "Our Way Galaxy. One crafty hero. Ten wild worlds to invent through.";
        [SerializeField] private Transform backdrop;
        [SerializeField] private float parallax = 0.04f;

        private Vector3 _backdropRest;
        private float _showUntil;

        public static HomeCommercialPitch Ensure(Transform parent = null)
        {
            var existing = FindAnyObjectByType<HomeCommercialPitch>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("SYS_HomeCommercialPitch");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            return go.AddComponent<HomeCommercialPitch>();
        }

        private void Awake()
        {
            if (backdrop != null)
            {
                _backdropRest = backdrop.localPosition;
            }

            _showUntil = Time.time + 6f;
            Debug.Log($"[HomePitch] {tagline}");
        }

        private void Update()
        {
            if (backdrop != null)
            {
                var ox = Mathf.Sin(Time.time * 0.35f) * parallax;
                var oy = Mathf.Cos(Time.time * 0.28f) * parallax * 0.6f;
                backdrop.localPosition = _backdropRest + new Vector3(ox, oy, 0f);
            }
        }

        private void OnGUI()
        {
            if (Time.time > _showUntil)
            {
                return;
            }

            var alpha = Mathf.Clamp01(_showUntil - Time.time);
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha * 0.92f);
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.LowerCenter,
                wordWrap = true,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(40f, Screen.height - 120f, Screen.width - 80f, 70f), tagline, style);
            GUI.color = prev;
        }
    }
}
