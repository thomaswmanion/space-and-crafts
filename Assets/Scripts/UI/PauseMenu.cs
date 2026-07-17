using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.UI
{
    public sealed class PauseMenu : MonoBehaviour
    {
        private bool paused;

        private void Update()
        {
            bool pressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            pressed |= Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
            if (pressed)
            {
                SetPaused(!paused);
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private void SetPaused(bool value)
        {
            paused = value;
            Time.timeScale = paused ? 0f : 1f;
        }

        private void OnGUI()
        {
            if (!paused)
            {
                return;
            }

            GUIStyle panel = new GUIStyle(GUI.skin.box);
            GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUIStyle button = new GUIStyle(GUI.skin.button) { fontSize = 21, fontStyle = FontStyle.Bold };
            Rect rect = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 210f, 440f, 420f);
            GUI.Box(rect, string.Empty, panel);
            GUI.Label(new Rect(rect.x, rect.y + 30f, rect.width, 52f), "ADVENTURE PAUSED", title);
            if (GUI.Button(new Rect(rect.x + 70f, rect.y + 110f, rect.width - 140f, 58f), "RESUME", button)) SetPaused(false);
            if (GUI.Button(new Rect(rect.x + 70f, rect.y + 188f, rect.width - 140f, 58f), "RESTART AREA", button))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            if (GUI.Button(new Rect(rect.x + 70f, rect.y + 266f, rect.width - 140f, 58f), "HOME MENU", button))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("Home");
            }
        }
    }
}
