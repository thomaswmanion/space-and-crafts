using SpaceAndCrafts.Core;
using UnityEngine;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Brief edge vignette when combat starts — readable “fight!” without toast spam.
    /// </summary>
    public sealed class CombatStartFlash : MonoBehaviour
    {
        [SerializeField] private float flashSeconds = 0.55f;

        private float _until;
        private GUIStyle _style;

        private void OnEnable()
        {
            GameEvents.CombatStarted += HandleStart;
        }

        private void OnDisable()
        {
            GameEvents.CombatStarted -= HandleStart;
        }

        private void HandleStart()
        {
            _until = Time.unscaledTime + flashSeconds;
        }

        private void OnGUI()
        {
            if (Time.unscaledTime > _until)
            {
                return;
            }

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 22,
                    normal = { textColor = new Color(1f, 0.85f, 0.4f, 1f) }
                };
            }

            var t = 1f - Mathf.InverseLerp(_until - flashSeconds, _until, Time.unscaledTime);
            var alpha = Mathf.Clamp01(t) * 0.35f;
            var prev = GUI.color;
            GUI.color = new Color(1f, 0.45f, 0.2f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, 18f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, Screen.height - 18f, Screen.width, 18f), Texture2D.whiteTexture);
            GUI.color = prev;
            if (t > 0.35f)
            {
                GUI.Label(new Rect((Screen.width - 220f) * 0.5f, 28f, 220f, 36f), "Scrap noticed you!", _style);
            }
        }
    }
}
